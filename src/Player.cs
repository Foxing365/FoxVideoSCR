using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace FoxVideoSCR {
 internal static class Native {
  [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
  [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr h, out Rect r);
  [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point p);
  [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int t, uint flags);
 }
 internal sealed class VideoSurface : Border {
  private readonly MediaElement media;
  private readonly TextBlock status;
  private readonly Playlist playlist;
  private readonly DispatcherTimer openingTimeout;
  private readonly Settings settings;
  private bool closed;
  private string current;
  internal VideoSurface(Settings value, bool silent) {
   settings = value; Background = Brushes.Black;
   playlist = new Playlist(settings.Videos, settings.Order);
   media = new MediaElement { LoadedBehavior = MediaState.Manual, UnloadedBehavior = MediaState.Close, Volume = silent || settings.Mute ? 0 : settings.Volume / 100,
    Stretch = settings.Fit == "Crop" ? Stretch.UniformToFill : settings.Fit == "Stretch" ? Stretch.Fill : Stretch.Uniform };
   status = new TextBlock { Text = "正在加载视频…", Foreground = Brushes.White, FontSize = 16, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 600, Margin = new Thickness(16) };
   var grid = new Grid(); grid.ClipToBounds = true; grid.Children.Add(media); grid.Children.Add(status); Child = grid;
   openingTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
   openingTimeout.Tick += delegate { openingTimeout.Stop(); Fail("打开视频超时"); };
   media.MediaOpened += delegate {
    openingTimeout.Stop(); status.Visibility = Visibility.Collapsed; media.SpeedRatio = settings.Speed;
    Log.Write("Opened " + media.NaturalVideoWidth + "x" + media.NaturalVideoHeight + ": " + current);
   };
   media.MediaEnded += delegate { QueueNext(false); };
   media.MediaFailed += delegate(object s, ExceptionRoutedEventArgs e) { Fail(e.ErrorException == null ? "无法解码视频" : e.ErrorException.Message); };
   Loaded += delegate { if (!closed && current == null) Next(false); };
  }
  private void Fail(string reason) { if (closed) return; Log.Write("Playback failed: " + current + "; " + reason); QueueNext(true); }
  private void QueueNext(bool failure) { if (!closed) Dispatcher.BeginInvoke(new Action(delegate { if (!closed) Next(failure); })); }
  private void Next(bool failure) {
   openingTimeout.Stop(); media.Stop(); media.Close();
   current = playlist.Next(failure);
   if (current == null) {
    status.Text = "FoxVideoSCR\n没有可播放的视频\n请在屏幕保护程序设置中添加视频，或检查解码组件。";
    status.Visibility = Visibility.Visible; return;
   }
   status.Text = "正在加载视频…"; status.Visibility = Visibility.Visible;
   try { media.Source = new Uri(Path.GetFullPath(current)); openingTimeout.Start(); media.Play(); }
   catch (Exception e) { Fail(e.Message); }
  }
  internal void Close() { closed = true; openingTimeout.Stop(); media.Close(); }
 }
 internal sealed class PlaybackSession {
  private readonly Application app;
  private readonly bool standalone;
  private readonly System.Collections.Generic.List<Window> windows = new System.Collections.Generic.List<Window>();
  private readonly System.Collections.Generic.List<VideoSurface> videos = new System.Collections.Generic.List<VideoSurface>();
  private DispatcherTimer input, testTimer;
  private DateTime started;
  private Native.Point mouse;
  private bool exiting;
  internal PlaybackSession(Application application, bool ownsApplication) { app = application; standalone = ownsApplication; }
  internal void Start(Settings settings, int testSeconds) {
   var screens = settings.Screens == "All" ? Forms.Screen.AllScreens : new[] { Forms.Screen.PrimaryScreen };
   for (int i = 0; i < screens.Length; i++) {
    var screen = screens[i]; var surface = new VideoSurface(settings.Copy(), !screen.Primary);
    var window = new Window { Title = "FoxVideoSCR", WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, Background = Brushes.Black, Topmost = true, ShowInTaskbar = false, Content = surface, Cursor = System.Windows.Input.Cursors.None };
    window.SourceInitialized += delegate(object s, EventArgs e) {
     var h = new WindowInteropHelper((Window)s).Handle; var b = screen.Bounds;
     Native.SetWindowPos(h, new IntPtr(-1), b.X, b.Y, b.Width, b.Height, 0x0040);
    };
    window.KeyDown += delegate { Stop(); }; window.MouseDown += delegate { Stop(); };
    window.Closed += delegate { if (!exiting) Stop(); };
    videos.Add(surface); windows.Add(window); window.Show();
   }
   started = DateTime.UtcNow; Native.GetCursorPos(out mouse);
   input = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
   input.Tick += delegate {
    Native.Point p; if (!Native.GetCursorPos(out p)) return;
    if ((DateTime.UtcNow - started).TotalSeconds < 2) { mouse = p; return; }
    if (Math.Abs(p.X - mouse.X) > 8 || Math.Abs(p.Y - mouse.Y) > 8) Stop();
   }; input.Start();
   if (testSeconds > 0) { testTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(testSeconds) }; testTimer.Tick += delegate { Stop(); }; testTimer.Start(); }
  }
  internal void Stop() {
   if (exiting) return; exiting = true;
   if (input != null) input.Stop(); if (testTimer != null) testTimer.Stop();
   foreach (var video in videos) video.Close(); foreach (var window in windows) window.Close();
   if (standalone) app.Shutdown();
  }
 }
 internal sealed class PreviewHost : IDisposable {
  private readonly HwndSource source;
  private readonly VideoSurface video;
  private readonly DispatcherTimer timer;
  private bool disposed;
  internal PreviewHost(IntPtr parent, Settings settings, Application app) {
   Native.Rect rect; if (!Native.GetClientRect(parent, out rect)) throw new ArgumentException("预览窗口无效");
   var parameters = new HwndSourceParameters("FoxVideoSCR Preview") { ParentWindow = parent, WindowStyle = 0x50000000, Width = Math.Max(1, rect.R - rect.L), Height = Math.Max(1, rect.B - rect.T) };
   source = new HwndSource(parameters); video = new VideoSurface(settings, true); source.RootVisual = video;
   timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
   timer.Tick += delegate { if (!Native.IsWindow(parent)) { Dispose(); app.Shutdown(); } }; timer.Start();
  }
  public void Dispose() { if (disposed) return; disposed = true; timer.Stop(); video.Close(); source.Dispose(); }
 }
}
