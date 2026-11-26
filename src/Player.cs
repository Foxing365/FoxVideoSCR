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
  private readonly MediaElement media;
  private readonly TextBlock status;
  private readonly Playlist playlist;
  private readonly DispatcherTimer openingTimeout;
  private readonly Settings settings;
  private bool closed;
  private string current;

  internal VideoSurface(Settings value, IEnumerable<string> videoList, string fitMode, bool silent) {
   settings = value;
   Background = Brushes.Black;
   BorderThickness = new Thickness(0);
   Padding = new Thickness(0);
   Margin = new Thickness(0);
   ClipToBounds = true;

   playlist = new Playlist(videoList, settings.Order);
   media = new MediaElement {
    LoadedBehavior = MediaState.Manual,
    UnloadedBehavior = MediaState.Close,
    Volume = silent || settings.Mute ? 0 : settings.Volume / 100.0,
    Stretch = fitMode == "Crop" ? Stretch.UniformToFill : fitMode == "Stretch" ? Stretch.Fill : Stretch.Uniform,
    HorizontalAlignment = HorizontalAlignment.Stretch,
    VerticalAlignment = VerticalAlignment.Stretch,
    Margin = new Thickness(0)
   };
   RenderOptions.SetBitmapScalingMode(media, BitmapScalingMode.HighQuality);

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

   var grid = new Grid { Background = Brushes.Black, ClipToBounds = true };
   grid.Children.Add(media);
   grid.Children.Add(status);
   Child = grid;

   openingTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
   openingTimeout.Tick += delegate { openingTimeout.Stop(); Fail("打开视频超时"); };

   media.MediaOpened += delegate {
    openingTimeout.Stop();
    status.Visibility = Visibility.Collapsed;
    media.SpeedRatio = settings.Speed;
    Log.Write("Opened " + media.NaturalVideoWidth + "x" + media.NaturalVideoHeight + " (" + fitMode + "): " + current);
   };
   media.MediaEnded += delegate { QueueNext(false); };
   media.MediaFailed += delegate(object s, ExceptionRoutedEventArgs e) {
    Fail(e.ErrorException == null ? "无法解码视频" : e.ErrorException.Message);
   };
   Loaded += delegate { if (!closed && current == null) Next(false); };
  }

  private void Fail(string reason) {
   if (closed) return;
   Log.Write("Playback failed: " + current + "; " + reason);
   QueueNext(true);
  }

  private void QueueNext(bool failure) {
   if (!closed) Dispatcher.BeginInvoke(new Action(delegate { if (!closed) Next(failure); }));
  }

  private void Next(bool failure) {
   openingTimeout.Stop();
   media.Stop();
   media.Close();
   current = playlist.Next(failure);
   if (current == null) {
    status.Text = "FoxVideoSCR\n没有可播放的视频\n请在屏幕保护程序设置中添加视频，或检查 64 位解码组件。";
    status.Visibility = Visibility.Visible;
    return;
   }
   status.Text = "正在加载视频…";
   status.Visibility = Visibility.Visible;
   try {
    media.Source = new Uri(Path.GetFullPath(current));
    openingTimeout.Start();
    media.Play();
   } catch (Exception e) {
    Fail(e.Message);
   }
  }

  internal void Close() {
   closed = true;
   openingTimeout.Stop();
   media.Close();
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
