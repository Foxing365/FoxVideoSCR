using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;

namespace FoxVideoSCR {
 internal static class SelfTest {
  private static void Assert(bool condition, string text) { if (!condition) throw new Exception("SELFTEST: " + text); }
  internal static void Run() {
   string dir = Path.Combine(Path.GetTempPath(), "FoxVideoSCR-test-" + Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(dir);
   try {
    Assert(IntPtr.Size == 8, "process must be 64-bit");
    string a = Path.Combine(dir, "a.mp4"), b = Path.Combine(dir, "b.mp4");
    File.WriteAllText(a, "test");
    File.WriteAllText(b, "test");

    var sequence = new Playlist(new[] { a, b, Path.Combine(dir, "missing.mp4") }, "Sequential");
    Assert(sequence.Next(false) == a && sequence.Next(false) == b && sequence.Next(false) == a, "sequence and missing-file handling");

    var failures = new Playlist(new[] { a, b }, "Sequential");
    Assert(failures.Next(false) == a && failures.Next(true) == b && failures.Next(true) == null, "failed videos must terminate instead of retrying forever");

    var markFailed = new Playlist(new[] { a, b }, "Single");
    Assert(markFailed.Next(false) == a, "markFailed initial item");
    markFailed.MarkFailed(a);
    Assert(markFailed.Next(false) == b, "markFailed fallback to next playable item");
    markFailed.MarkFailed(b);
    Assert(markFailed.Next(false) == null, "markFailed all failed returns null");

    var shuffle = new Playlist(new[] { a, b }, "Shuffle");
    string last = shuffle.Next(false);
    for (int i = 0; i < 30; i++) {
     string next = shuffle.Next(false);
     Assert(last != next, "shuffle immediate repeat");
     last = next;
    }

    var single = new Playlist(new[] { a, b }, "Single");
    Assert(single.Next(false) == a && single.Next(false) == a && single.Next(true) == b, "single loop and failed-item fallback");

    var s = new Settings {
     Videos = new List<string> { a, a, "" },
     Volume = 200,
     Speed = 0,
     Order = "invalid",
     Fit = "invalid",
     Screens = "invalid",
     Monitors = new List<MonitorSetting> {
      new MonitorSetting { DeviceName = "\\\\.\\DISPLAY2", Mode = "CustomVideo", Fit = "Stretch", Videos = new List<string> { b, b } },
      new MonitorSetting { DeviceName = "\\\\.\\DISPLAY2", Mode = "Black", Fit = "Global" }
     }
    };
    s.Normalize();
    Assert(s.Videos.Count == 1 && s.Volume == 100 && s.Speed == 0.5 && s.Order == "Sequential" && s.Fit == "Crop" && s.Screens == "All", "settings validation");
    Assert(s.Monitors.Count == 1 && s.Monitors[0].Videos.Count == 1 && s.Monitors[0].Mode == "CustomVideo", "monitor settings deduplication");

    var primary = new DisplayInfo { DeviceName = "\\\\.\\DISPLAY1", IsPrimary = true, Width = 2560, Height = 1440 };
    var secondary = new DisplayInfo { DeviceName = "\\\\.\\DISPLAY2", IsPrimary = false, Width = 1080, Height = 1920 };
    string mode, fit;
    List<string> list;

    s.Screens = "PrimaryBlackout";
    s.ResolveMonitor(primary, out mode, out fit, out list);
    Assert(mode == "Video" && fit == "Crop", "primary blackout primary screen");
    s.ResolveMonitor(secondary, out mode, out fit, out list);
    Assert(mode == "Black", "primary blackout secondary screen");

    s.Screens = "Custom";
    s.ResolveMonitor(secondary, out mode, out fit, out list);
    Assert(mode == "CustomVideo" && fit == "Stretch" && list.Count == 1 && list[0] == b, "custom monitor resolution");

    using (var stream = new MemoryStream()) {
     var serializer = new DataContractJsonSerializer(typeof(Settings));
     serializer.WriteObject(stream, s);
     stream.Position = 0;
     var copy = (Settings)serializer.ReadObject(stream);
     Assert(copy.Videos[0] == a && copy.Volume == 100 && copy.Monitors.Count == 1 && copy.Monitors[0].Videos[0] == b, "JSON round trip");
    }

    Directory.CreateDirectory(Settings.DirectoryPath);
    File.WriteAllText(Path.Combine(Settings.DirectoryPath, "selftest-result.txt"), "PASS: 8 checks");
   } finally {
    foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
    Directory.Delete(dir);
   }
  }
 }
}
