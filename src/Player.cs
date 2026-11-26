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

 internal sealed class VideoSurface : Border {
  private sealed class MediaSlot {
   internal readonly Border Host;
   internal readonly MediaElement Media;
   internal string Path;
   internal bool Ready;
   internal bool Loading;

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

  private readonly MediaSlot slotA;
  private readonly MediaSlot slotB;
  private MediaSlot active;
  private MediaSlot standby;
  private readonly TextBlock status;
  private readonly Playlist playlist;
  private readonly DispatcherTimer openingTimeout;
  private readonly DispatcherTimer loopMonitor;
  private readonly DispatcherTimer preloadDelay;
  private readonly Settings settings;
  private readonly string fitMode;
  private readonly double targetVolume;
  private bool closed;
  private bool started;
  private bool swapping;

  internal VideoSurface(Settings value, IEnumerable<string> videoList, string fit, bool silent) {
   settings = value;
   fitMode = fit;
   targetVolume = silent || settings.Mute ? 0 : settings.Volume / 100.0;
   Background = Brushes.Black;
   BorderThickness = new Thickness(0);
   Padding = new Thickness(0);
   Margin = new Thickness(0);
   ClipToBounds = true;

   playlist = new Playlist(videoList, settings.Order);
   Stretch stretch = fitMode == "Crop" ? Stretch.UniformToFill : fitMode == "Stretch" ? Stretch.Fill : Stretch.Uniform;

   slotA = new MediaSlot(stretch);
   slotB = new MediaSlot(stretch);
   active = slotA;
   standby = slotB;
   Panel.SetZIndex(active.Host, 2);
   Panel.SetZIndex(standby.Host, 1);

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
   grid.Children.Add(slotA.Host);
   grid.Children.Add(slotB.Host);
   grid.Children.Add(status);
   Child = grid;

   openingTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
   openingTimeout.Tick += delegate {
    openingTimeout.Stop();
    HandleSlotFailure(active, "打开视频超时");
   };

   loopMonitor = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
   loopMonitor.Tick += OnLoopMonitorTick;

   preloadDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
   preloadDelay.Tick += delegate {
    preloadDelay.Stop();
    if (!closed) PreloadStandby();
   };

   WireSlot(slotA);
   WireSlot(slotB);

   Loaded += delegate {
    if (!closed && !started) {
     started = true;
     StartInitial();
    }
   };
  }

  private void WireSlot(MediaSlot slot) {
   slot.Media.MediaOpened += delegate {
    if (closed) return;
    slot.Loading = false;
    if (slot == active) {
     openingTimeout.Stop();
     status.Visibility = Visibility.Collapsed;
     slot.Ready = true;
     slot.Media.SpeedRatio = settings.Speed;
     slot.Media.Volume = targetVolume;
     Log.Write("Opened " + slot.Media.NaturalVideoWidth + "x" + slot.Media.NaturalVideoHeight + " (" + fitMode + "): " + slot.Path);
     loopMonitor.Start();
     PreloadStandby();
    } else {
     slot.Media.Volume = 0;
     slot.Media.Pause();
     slot.Media.Position = TimeSpan.Zero;
     slot.Ready = true;
    }
   };

   slot.Media.MediaEnded += delegate {
    if (closed || slot != active) return;
    AdvanceActive();
   };

   slot.Media.MediaFailed += delegate(object s, ExceptionRoutedEventArgs e) {
    if (closed) return;
    HandleSlotFailure(slot, e.ErrorException == null ? "无法解码视频" : e.ErrorException.Message);
   };
  }

  private void OnLoopMonitorTick(object sender, EventArgs e) {
   if (closed || swapping || !standby.Ready) return;
   if (!active.Media.NaturalDuration.HasTimeSpan) return;
   TimeSpan duration = active.Media.NaturalDuration.TimeSpan;
   if (duration.TotalSeconds < 1.2) return;
   TimeSpan remaining = duration - active.Media.Position;
   double leadMs = Math.Max(60.0, 90.0 * settings.Speed);
   if (remaining <= TimeSpan.FromMilliseconds(leadMs)) {
    loopMonitor.Interval = TimeSpan.FromMilliseconds(500);
    AdvanceActive();
    return;
   }
   TimeSpan desiredInterval = remaining.TotalSeconds <= 1.5
    ? TimeSpan.FromMilliseconds(25)
    : TimeSpan.FromMilliseconds(500);
   if (loopMonitor.Interval != desiredInterval) loopMonitor.Interval = desiredInterval;
  }

  private void StartInitial() {
   string next = playlist.Next(false);
   if (next == null) {
    ShowNoVideo();
    return;
   }
   LoadIntoActive(next);
  }

  private void LoadIntoActive(string path) {
   openingTimeout.Stop();
   loopMonitor.Stop();
   active.Media.Stop();
   active.Media.Close();
   active.Path = path;
   active.Ready = false;
   active.Loading = true;
   status.Text = "正在加载视频…";
   status.Visibility = Visibility.Visible;
   try {
    active.Media.Volume = targetVolume;
    active.Media.Source = new Uri(Path.GetFullPath(path));
    openingTimeout.Start();
    active.Media.Play();
   } catch (Exception e) {
    HandleSlotFailure(active, e.Message);
   }
  }

  private void PreloadStandby() {
   if (closed || standby.Ready || standby.Loading) return;
   string next = playlist.Next(false);
   if (next == null) return;

   if (string.Equals(next, standby.Path, StringComparison.OrdinalIgnoreCase) && standby.Media.Source != null) {
    try {
     standby.Media.Volume = 0;
     standby.Media.Pause();
     standby.Media.Position = TimeSpan.Zero;
     standby.Ready = true;
     return;
    } catch { }
   }

   standby.Media.Stop();
   standby.Media.Close();
   standby.Path = next;
   standby.Ready = false;
   standby.Loading = true;
   try {
    standby.Media.Volume = 0;
    standby.Media.Source = new Uri(Path.GetFullPath(next));
    standby.Media.Play();
   } catch (Exception e) {
    HandleSlotFailure(standby, e.Message);
   }
  }

  private void AdvanceActive() {
   if (closed || swapping) return;
   swapping = true;
   try {
    if (standby.Ready) {
     var oldActive = active;
     var newActive = standby;

     newActive.Media.SpeedRatio = settings.Speed;
     newActive.Media.Volume = targetVolume;
     newActive.Media.Play();
     Panel.SetZIndex(newActive.Host, 2);
     Panel.SetZIndex(oldActive.Host, 1);
     oldActive.Media.Volume = 0;

     active = newActive;
     standby = oldActive;
     standby.Ready = false;
     standby.Loading = false;
     status.Visibility = Visibility.Collapsed;
     loopMonitor.Start();

     preloadDelay.Stop();
     preloadDelay.Start();
     return;
    }

    // Fallback if standby hasn't finished preloading yet
    string next = standby.Loading && !string.IsNullOrEmpty(standby.Path) ? standby.Path : playlist.Next(false);
    if (next == null) {
     ShowNoVideo();
     return;
    }
    if (string.Equals(next, active.Path, StringComparison.OrdinalIgnoreCase) && active.Media.Source != null) {
     active.Media.Position = TimeSpan.Zero;
     active.Media.SpeedRatio = settings.Speed;
     active.Media.Volume = targetVolume;
     active.Media.Play();
     return;
    }
    LoadIntoActive(next);
   } finally {
    swapping = false;
   }
  }

  private void HandleSlotFailure(MediaSlot slot, string reason) {
   if (closed) return;
   string failedPath = slot.Path;
   slot.Loading = false;
   slot.Ready = false;
   Log.Write("Playback failed: " + failedPath + "; " + reason);
   playlist.MarkFailed(failedPath);
   slot.Path = null;
   slot.Media.Stop();
   slot.Media.Close();

   if (slot == active) {
    Dispatcher.BeginInvoke(new Action(delegate {
     if (closed) return;
     if (standby.Ready) {
      AdvanceActive();
      return;
     }
     string next = playlist.Next(false);
     if (next == null) ShowNoVideo(); else LoadIntoActive(next);
    }));
   } else {
    Dispatcher.BeginInvoke(new Action(delegate {
     if (!closed) PreloadStandby();
    }));
   }
  }

  private void ShowNoVideo() {
   openingTimeout.Stop();
   loopMonitor.Stop();
   preloadDelay.Stop();
   status.Text = "FoxVideoSCR\n没有可播放的视频\n请在屏幕保护程序设置中添加视频，或检查 64 位解码组件。";
   status.Visibility = Visibility.Visible;
  }

  internal void Close() {
   closed = true;
   openingTimeout.Stop();
   loopMonitor.Stop();
   preloadDelay.Stop();
   slotA.Media.Close();
   slotB.Media.Close();
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
   video = new VideoSurface(settings, settings.Videos, settings.Fit, true);
   source.RootVisual = video;
   timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
   timer.Tick += delegate { if (!Native.IsWindow(parent)) { Dispose(); app.Shutdown(); } };
   timer.Start();
  }

  public void Dispose() {
   if (disposed) return;
   disposed = true;
   timer.Stop();
   video.Close();
   source.Dispose();
  }
 }
}
