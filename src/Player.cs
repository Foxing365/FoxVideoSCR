using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace FoxVideoSCR {
 internal static class Native {
  internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
  internal const uint SWP_NOACTIVATE = 0x0010;
  internal const uint SWP_SHOWWINDOW = 0x0040;
  internal const int WM_DPICHANGED = 0x02E0;

  [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }

  [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr h, out Rect r);
  [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point p);
  [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int t, uint flags);
  [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

  internal static void EnableDarkTitleBar(Window window) {
   try {
    var handle = new WindowInteropHelper(window).Handle;
    if (handle == IntPtr.Zero) return;
    int dark = 1;
    if (DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)) != 0) {
     DwmSetWindowAttribute(handle, 19, ref dark, sizeof(int));
    }
   } catch { }
  }
 }

 internal interface IMediaSlotHost {
  void LoadAndPlay(int slot, string path, int generation, bool activeNow);
  void RewindAndPlay(int slot);
  void PauseAtStart(int slot);
  void PromoteVisual(int activeSlot, int standbySlot);
  void MuteSlot(int slot);
  void ResetSlot(int slot);
  void StartOpeningTimeout(int generation);
  void StopOpeningTimeout();
  void SchedulePreload();
  void StopPreloadTimer();
  void ShowLoading();
  void HideStatus();
  void ShowNoVideo();
 }

 internal sealed class DualSlotPlaybackController {
  private sealed class SlotState {
   internal readonly int Index;
   internal string Path;
   internal int Generation;
   internal bool Loading;
   internal bool Ready;
   internal bool HasMedia;

   internal SlotState(int index) { Index = index; }
  }

  private readonly Playlist playlist;
  private readonly IMediaSlotHost host;
  private readonly SlotState[] slots;
  private int activeIndex;
  private bool closed;
  private bool transitioning;

  internal int ActiveIndex { get { return activeIndex; } }

  internal DualSlotPlaybackController(Playlist list, IMediaSlotHost slotHost) {
   playlist = list;
   host = slotHost;
   slots = new[] { new SlotState(0), new SlotState(1) };
   activeIndex = 0;
  }

  internal void Start() {
   if (closed) return;
   string first = playlist.Next(false);
   if (first == null) {
    host.ShowNoVideo();
    return;
   }
   LoadIntoActive(first);
  }

  internal void Close() {
   closed = true;
   host.StopOpeningTimeout();
   host.StopPreloadTimer();
   for (int i = 0; i < slots.Length; i++) {
    slots[i].Generation++;
    slots[i].Loading = false;
    slots[i].Ready = false;
    slots[i].HasMedia = false;
    slots[i].Path = null;
   }
  }

  internal void PreloadStandby() {
   if (closed) return;
   var active = slots[activeIndex];
   var standby = slots[1 - activeIndex];
   if (!active.Ready || standby.Ready || standby.Loading) return;

   string next = playlist.Next(false);
   if (next == null) return;

   if (standby.HasMedia && string.Equals(next, standby.Path, StringComparison.OrdinalIgnoreCase)) {
    standby.Ready = true;
    standby.Loading = false;
    host.PauseAtStart(standby.Index);
    return;
   }

   standby.Generation++;
   standby.Path = next;
   standby.Loading = true;
   standby.Ready = false;
   standby.HasMedia = false;
   host.ResetSlot(standby.Index);
   host.LoadAndPlay(standby.Index, next, standby.Generation, false);
  }

  internal void OnSlotOpened(int slotIndex, int generation) {
   if (closed) return;
   var slot = slots[slotIndex];
   if (generation != slot.Generation) return;

   slot.Loading = false;
   slot.HasMedia = true;
   slot.Ready = true;

   if (slotIndex == activeIndex) {
    var standby = slots[1 - activeIndex];
    host.StopOpeningTimeout();
    host.HideStatus();
    host.PromoteVisual(activeIndex, standby.Index);
    if (standby.HasMedia && !standby.Loading) {
     host.PauseAtStart(standby.Index);
    }
    PreloadStandby();
   } else {
    host.PauseAtStart(slotIndex);
   }
  }

  internal void OnSlotEnded(int slotIndex, int generation) {
   if (closed || transitioning || slotIndex != activeIndex) return;
   var active = slots[activeIndex];
   if (generation != active.Generation) return;

   transitioning = true;
   try {
    var standby = slots[1 - activeIndex];

    if (standby.Ready) {
     host.StopOpeningTimeout();
     host.StopPreloadTimer();

     activeIndex = standby.Index;
     active.Ready = false;
     active.Loading = false;

     host.PromoteVisual(standby.Index, active.Index);
     if (active.HasMedia) host.PauseAtStart(active.Index);
     host.HideStatus();
     host.SchedulePreload();
     return;
    }

    if (standby.Loading) {
     // Promote the in-flight preload slot instead of consuming/opening the same item twice.
     host.StopOpeningTimeout();
     host.StopPreloadTimer();

     activeIndex = standby.Index;
     active.Ready = false;
     active.Loading = false;
     host.MuteSlot(active.Index);
     host.StartOpeningTimeout(standby.Generation);
     return;
    }

    host.StopPreloadTimer();
    string next = playlist.Next(false);
    if (next == null) {
     host.ShowNoVideo();
     return;
    }
    if (active.HasMedia && string.Equals(next, active.Path, StringComparison.OrdinalIgnoreCase)) {
     host.RewindAndPlay(active.Index);
     PreloadStandby();
     return;
    }
    LoadIntoActive(next);
   } finally {
    transitioning = false;
   }
  }

  internal void OnOpeningTimeout(int generation) {
   if (closed) return;
   var active = slots[activeIndex];
   if (!active.Loading || active.Generation != generation) return;
   OnSlotFailed(activeIndex, generation, "打开视频超时");
  }

  internal void OnSlotFailed(int slotIndex, int generation, string reason) {
   if (closed) return;
   var slot = slots[slotIndex];
   if (generation != slot.Generation) return;

   string failedPath = slot.Path;
   Log.Write("Playback failed: " + failedPath + "; " + reason);
   playlist.MarkFailed(failedPath);

   slot.Generation++;
   slot.Path = null;
   slot.Loading = false;
   slot.Ready = false;
   slot.HasMedia = false;
   host.ResetSlot(slotIndex);

   if (slotIndex == activeIndex) {
    host.StopOpeningTimeout();
    var standby = slots[1 - activeIndex];
    if (standby.Ready) {
     activeIndex = standby.Index;
     host.HideStatus();
     host.PromoteVisual(standby.Index, slotIndex);
     host.SchedulePreload();
     return;
    }
    if (standby.Loading) {
     activeIndex = standby.Index;
     host.ShowLoading();
     host.StartOpeningTimeout(standby.Generation);
     return;
    }
    string next = playlist.Next(false);
    if (next == null) host.ShowNoVideo(); else LoadIntoActive(next);
   } else {
    PreloadStandby();
   }
  }

  private void LoadIntoActive(string path) {
   host.StopOpeningTimeout();
   host.StopPreloadTimer();
   var active = slots[activeIndex];
   active.Generation++;
   active.Path = path;
   active.Loading = true;
   active.Ready = false;
   active.HasMedia = false;
   host.ResetSlot(activeIndex);
   host.ShowLoading();
   host.StartOpeningTimeout(active.Generation);
   host.LoadAndPlay(activeIndex, path, active.Generation, true);
  }
 }

 internal sealed class VideoSurface : Border, IMediaSlotHost {
  private sealed class MediaSlot {
   internal readonly Border Host;
   internal readonly MediaElement Media;
   internal string Path;
   internal int Generation;

   internal MediaSlot(Stretch stretch) {
    Media = new MediaElement {
     LoadedBehavior = MediaState.Manual,
     UnloadedBehavior = MediaState.Close,
     ScrubbingEnabled = true,
     Volume = 0,
     Stretch = stretch,
     HorizontalAlignment = HorizontalAlignment.Stretch,
     VerticalAlignment = VerticalAlignment.Stretch,
     Margin = new Thickness(0)
    };
    RenderOptions.SetBitmapScalingMode(Media, BitmapScalingMode.HighQuality);
    Host = new Border {
     Background = Brushes.Black,
     BorderThickness = new Thickness(0),
     Padding = new Thickness(0),
     Margin = new Thickness(0),
     ClipToBounds = true,
     Child = Media
    };
   }
  }

  private readonly MediaSlot[] slots;
  private readonly TextBlock status;
  private readonly DualSlotPlaybackController controller;
  private readonly DispatcherTimer openingTimeout;
  private readonly DispatcherTimer preloadDelay;
  private readonly Settings settings;
  private readonly string fitMode;
  private readonly double targetVolume;
  private int openingTimeoutGeneration;
  private bool closed;
  private bool started;

  internal VideoSurface(Settings value, IEnumerable<string> videoList, string fit, bool silent) {
   settings = value;
   fitMode = fit;
   targetVolume = silent || settings.Mute ? 0 : settings.Volume / 100.0;
   Background = Brushes.Black;
   BorderThickness = new Thickness(0);
   Padding = new Thickness(0);
   Margin = new Thickness(0);
   ClipToBounds = true;

   Stretch stretch = fitMode == "Crop" ? Stretch.UniformToFill : fitMode == "Stretch" ? Stretch.Fill : Stretch.Uniform;
   slots = new[] { new MediaSlot(stretch), new MediaSlot(stretch) };
   Panel.SetZIndex(slots[0].Host, 2);
   Panel.SetZIndex(slots[1].Host, 1);

   status = new TextBlock {
    Text = "正在加载视频…",
    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
    FontSize = 15,
    FontFamily = new FontFamily("Microsoft YaHei UI"),
    TextAlignment = TextAlignment.Center,
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center,
    TextWrapping = TextWrapping.Wrap,
    MaxWidth = 600,
    Margin = new Thickness(16)
   };
   Panel.SetZIndex(status, 3);

   var grid = new Grid { Background = Brushes.Black, ClipToBounds = true };
   grid.Children.Add(slots[0].Host);
   grid.Children.Add(slots[1].Host);
   grid.Children.Add(status);
   Child = grid;

   controller = new DualSlotPlaybackController(new Playlist(videoList, settings.Order), this);

   openingTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
   openingTimeout.Tick += delegate {
    openingTimeout.Stop();
    if (!closed) controller.OnOpeningTimeout(openingTimeoutGeneration);
   };

   preloadDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
   preloadDelay.Tick += delegate {
    preloadDelay.Stop();
    if (!closed) controller.PreloadStandby();
   };

   WireSlot(0);
   WireSlot(1);

   Loaded += delegate {
    if (!closed && !started) {
     started = true;
     controller.Start();
    }
   };
  }

  private void WireSlot(int index) {
   var slot = slots[index];
   slot.Media.MediaOpened += delegate {
    if (closed) return;
    if (index == controller.ActiveIndex) {
     Log.Write("Opened " + slot.Media.NaturalVideoWidth + "x" + slot.Media.NaturalVideoHeight + " (" + fitMode + "): " + slot.Path);
    }
    controller.OnSlotOpened(index, slot.Generation);
   };

   slot.Media.MediaEnded += delegate {
    if (!closed) controller.OnSlotEnded(index, slot.Generation);
   };

   slot.Media.MediaFailed += delegate(object s, ExceptionRoutedEventArgs e) {
    if (closed) return;
    string reason = e.ErrorException == null ? "无法解码视频" : e.ErrorException.Message;
    controller.OnSlotFailed(index, slot.Generation, reason);
   };
  }

  public void LoadAndPlay(int slotIndex, string path, int generation, bool activeNow) {
   if (closed) return;
   var slot = slots[slotIndex];
   slot.Generation = generation;
   slot.Path = path;
   try {
    slot.Media.SpeedRatio = settings.Speed;
    slot.Media.Volume = activeNow ? targetVolume : 0;
    slot.Media.Source = new Uri(Path.GetFullPath(path));
    slot.Media.Play();
   } catch (Exception e) {
    Dispatcher.BeginInvoke(new Action(delegate {
     if (!closed) controller.OnSlotFailed(slotIndex, generation, e.Message);
    }));
   }
  }

  public void RewindAndPlay(int slotIndex) {
   if (closed) return;
   var slot = slots[slotIndex];
   slot.Media.Position = TimeSpan.Zero;
   slot.Media.SpeedRatio = settings.Speed;
   slot.Media.Volume = targetVolume;
   slot.Media.Play();
  }

  public void PauseAtStart(int slotIndex) {
   if (closed) return;
   var slot = slots[slotIndex];
   slot.Media.Volume = 0;
   if (slot.Media.Source != null) {
    slot.Media.Pause();
    slot.Media.Position = TimeSpan.Zero;
   }
  }

  public void PromoteVisual(int activeSlot, int standbySlot) {
   if (closed) return;
   var active = slots[activeSlot];
   var standby = slots[standbySlot];
   active.Media.SpeedRatio = settings.Speed;
   active.Media.Volume = targetVolume;
   active.Media.Play();
   Panel.SetZIndex(active.Host, 2);
   Panel.SetZIndex(standby.Host, 1);
   standby.Media.Volume = 0;
  }

  public void MuteSlot(int slotIndex) {
   if (!closed) slots[slotIndex].Media.Volume = 0;
  }

  public void ResetSlot(int slotIndex) {
   var slot = slots[slotIndex];
   slot.Path = null;
   slot.Media.Volume = 0;
   slot.Media.Stop();
   slot.Media.Close();
   slot.Media.Source = null;
  }

  public void StartOpeningTimeout(int generation) {
   openingTimeoutGeneration = generation;
   openingTimeout.Stop();
   openingTimeout.Start();
  }

  public void StopOpeningTimeout() {
   openingTimeout.Stop();
  }

  public void SchedulePreload() {
   preloadDelay.Stop();
   preloadDelay.Start();
  }

  public void StopPreloadTimer() {
   preloadDelay.Stop();
  }

  public void ShowLoading() {
   status.Text = "正在加载视频…";
   status.Visibility = Visibility.Visible;
  }

  public void HideStatus() {
   status.Visibility = Visibility.Collapsed;
  }

  public void ShowNoVideo() {
   status.Text = "FoxVideoSCR\n没有可播放的视频\n请在屏幕保护程序设置中添加视频，或检查 64 位解码组件。";
   status.Visibility = Visibility.Visible;
  }

  internal void Close() {
   closed = true;
   controller.Close();
   slots[0].Media.Close();
   slots[1].Media.Close();
  }
 }

 internal sealed class PlaybackSession {
  private readonly Application app;
  private readonly bool standalone;
  private readonly List<Window> windows = new List<Window>();
  private readonly List<VideoSurface> videos = new List<VideoSurface>();
  private DispatcherTimer input, testTimer;
  private DateTime started;
  private Native.Point mouse;
  private bool exiting;

  internal PlaybackSession(Application application, bool ownsApplication) {
   app = application;
   standalone = ownsApplication;
  }

  internal void Start(Settings settings, int testSeconds) {
   var displays = DisplayEnumerator.GetAll();
   for (int i = 0; i < displays.Count; i++) {
    var display = displays[i];
    string mode, fit;
    List<string> list;
    settings.ResolveMonitor(display, out mode, out fit, out list);
    if (mode == "Off") continue;

    UIElement content;
    if (mode == "Black") {
     content = new Border { Background = Brushes.Black };
    } else {
     var surface = new VideoSurface(settings.Copy(), list, fit, !display.IsPrimary);
     videos.Add(surface);
     content = surface;
    }

    var window = new Window {
     Title = "FoxVideoSCR",
     WindowStyle = WindowStyle.None,
     ResizeMode = ResizeMode.NoResize,
     WindowState = WindowState.Normal,
     Background = Brushes.Black,
     BorderThickness = new Thickness(0),
     Padding = new Thickness(0),
     Topmost = true,
     ShowInTaskbar = false,
     Content = content,
     Cursor = System.Windows.Input.Cursors.None
    };

    var target = display;
    Action applyBounds = delegate {
     var h = new WindowInteropHelper(window).Handle;
     if (h != IntPtr.Zero) {
      Native.SetWindowPos(h, Native.HWND_TOPMOST, target.Left, target.Top, target.Width, target.Height, Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
     }
    };

    window.SourceInitialized += delegate {
     var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
     if (hwndSource != null) {
      hwndSource.AddHook(delegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
       if (msg == Native.WM_DPICHANGED) {
        applyBounds();
        handled = true;
       }
       return IntPtr.Zero;
      });
     }
     applyBounds();
    };
    window.Loaded += delegate { applyBounds(); };
    window.PreviewKeyDown += delegate { Stop(); };
    window.PreviewMouseDown += delegate { Stop(); };
    window.Closed += delegate { if (!exiting) Stop(); };

    windows.Add(window);
    window.Show();
   }

   if (windows.Count == 0) {
    if (standalone) app.Shutdown();
    return;
   }

   started = DateTime.UtcNow;
   Native.GetCursorPos(out mouse);
   input = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
   input.Tick += delegate {
    Native.Point p;
    if (!Native.GetCursorPos(out p)) return;
    if ((DateTime.UtcNow - started).TotalSeconds < 2) { mouse = p; return; }
    if (Math.Abs(p.X - mouse.X) > 8 || Math.Abs(p.Y - mouse.Y) > 8) Stop();
   };
   input.Start();

   if (testSeconds > 0) {
    testTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(testSeconds) };
    testTimer.Tick += delegate { Stop(); };
    testTimer.Start();
   }
  }

  internal void Stop() {
   if (exiting) return;
   exiting = true;
   if (input != null) input.Stop();
   if (testTimer != null) testTimer.Stop();
   foreach (var video in videos) video.Close();
   foreach (var window in windows) window.Close();
   if (standalone) app.Shutdown();
  }
 }

 internal sealed class PreviewHost : IDisposable {
  private readonly HwndSource source;
  private readonly VideoSurface video;
  private readonly DispatcherTimer timer;
  private bool disposed;

  internal PreviewHost(IntPtr parent, Settings settings, Application app) {
   Native.Rect rect;
   if (!Native.GetClientRect(parent, out rect)) throw new ArgumentException("预览窗口无效");
   var parameters = new HwndSourceParameters("FoxVideoSCR Preview") {
    ParentWindow = parent,
    WindowStyle = 0x50000000,
    Width = Math.Max(1, rect.R - rect.L),
    Height = Math.Max(1, rect.B - rect.T)
   };
   source = new HwndSource(parameters);

   string mode, fit;
   List<string> list;
   settings.ResolvePrimaryPreview(DisplayEnumerator.GetAll(), out mode, out fit, out list);
   if (mode == "Black" || mode == "Off") {
    video = null;
    source.RootVisual = new Border { Background = Brushes.Black };
   } else {
    video = new VideoSurface(settings.Copy(), list, fit, true);
    source.RootVisual = video;
   }

   timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
   timer.Tick += delegate { if (!Native.IsWindow(parent)) { Dispose(); app.Shutdown(); } };
   timer.Start();
  }

  public void Dispose() {
   if (disposed) return;
   disposed = true;
   timer.Stop();
   if (video != null) video.Close();
   source.Dispose();
  }
 }
}
