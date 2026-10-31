using System;
using System.Reflection;
using System.Windows;

[assembly: AssemblyTitle("FoxVideoSCR")]
[assembly: AssemblyProduct("FoxVideoSCR")]
[assembly: AssemblyDescription("FoxVideoSCR · 视频屏幕保护程序")]
[assembly: AssemblyVersion("0.1.0.0")]
namespace FoxVideoSCR {
 internal static class Program {
  [STAThread] private static void Main(string[] args) {
   try {
    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    app.DispatcherUnhandledException += delegate(object s, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) { Log.Write(e.Exception.ToString()); e.Handled = true; app.Shutdown(1); };
    string raw = args.Length == 0 ? "/c" : args[0];
    string mode = raw.Split(':')[0].ToLowerInvariant();
    if (mode == "/selftest") { SelfTest.Run(); return; }
    if (mode == "/p") {
     string value = raw.Contains(":") ? raw.Substring(raw.IndexOf(':') + 1) : args.Length > 1 ? args[1] : "0";
     long handle; if (!long.TryParse(value.Trim(), out handle) || handle == 0) return;
     using (var host = new PreviewHost(new IntPtr(handle), Settings.Load(), app)) app.Run(); return;
    }
    if (mode == "/s" || mode == "/test") {
     var session = new PlaybackSession(app, true); session.Start(Settings.Load(), mode == "/test" ? 25 : 0); app.Run(); return;
    }
    var settings = new SettingsWindow(app); settings.Closed += delegate { app.Shutdown(); }; app.Run(settings);
   } catch (Exception e) { Log.Write(e.ToString()); MessageBox.Show("FoxVideoSCR 启动失败：\n" + e.Message, "FoxVideoSCR", MessageBoxButton.OK, MessageBoxImage.Error); }
  }
 }
}
