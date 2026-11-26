using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;

namespace FoxVideoSCR {
 internal static class SelfTest {
  private sealed class FakeMediaHost : IMediaSlotHost {
   internal readonly string[] SlotPath = new string[2];
   internal readonly int[] SlotGeneration = new int[2];
   internal readonly List<string> Played = new List<string>();
   internal readonly List<string> OpenedLoads = new List<string>();
   internal bool PreloadScheduled;
   internal bool NoVideoShown;
   internal int VisualActiveSlot;

   public void LoadAndPlay(int slot, string path, int generation, bool activeNow) {
    SlotPath[slot] = path;
    SlotGeneration[slot] = generation;
    OpenedLoads.Add(path);
   }

   public void RewindAndPlay(int slot) {
    Played.Add(SlotPath[slot]);
   }

   public void PauseAtStart(int slot) { }

   public void PromoteVisual(int activeSlot, int standbySlot) {
    VisualActiveSlot = activeSlot;
    Played.Add(SlotPath[activeSlot]);
   }

   public void MuteSlot(int slot) { }

   public void ResetSlot(int slot) {
    SlotPath[slot] = null;
   }

   public void StartOpeningTimeout(int generation) { }
   public void StopOpeningTimeout() { }
   public void SchedulePreload() { PreloadScheduled = true; }
   public void StopPreloadTimer() { PreloadScheduled = false; }
   public void ShowLoading() { }
   public void HideStatus() { }
   public void ShowNoVideo() { NoVideoShown = true; }
  }

  private static void Assert(bool condition, string text) { if (!condition) throw new Exception("SELFTEST: " + text); }

  private static void TestSingleLoopWithoutEarlyCutoff() {
   string dir = Path.Combine(Path.GetTempPath(), "FoxVideoSCR-loop-" + Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(dir);
   try {
    string a = Path.Combine(dir, "a.mp4");
    File.WriteAllText(a, "test");
    var host = new FakeMediaHost();
    var ctrl = new DualSlotPlaybackController(new Playlist(new[] { a }, "Single"), host);
    ctrl.Start();
    Assert(host.OpenedLoads.Count == 1 && host.SlotPath[0] == a, "initial load");

    // Slot 0 opens -> preloads slot 1 with a
    ctrl.OnSlotOpened(0, host.SlotGeneration[0]);
    Assert(host.SlotPath[1] == a, "standby preloaded");
    ctrl.OnSlotOpened(1, host.SlotGeneration[1]);

    // End slot 0 -> hands off to preloaded slot 1 at actual end
    int endGen0 = host.SlotGeneration[0];
    ctrl.OnSlotEnded(0, endGen0);
    Assert(ctrl.ActiveIndex == 1 && host.PreloadScheduled, "swapped to slot 1 on MediaEnded");
    ctrl.PreloadStandby();
    Assert(host.OpenedLoads.Count == 2, "single loop reuses opened slot without reopening file");

    ctrl.OnSlotEnded(1, host.SlotGeneration[1]);
    Assert(ctrl.ActiveIndex == 0, "swapped back to slot 0");
    Assert(host.Played.Count == 3 && host.Played[0] == a && host.Played[1] == a && host.Played[2] == a, "single loop playback sequence");
   } finally {
    foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
    Directory.Delete(dir);
   }
  }

  private static void TestSlowPreloadDoesNotDuplicate() {
   string dir = Path.Combine(Path.GetTempPath(), "FoxVideoSCR-slow-" + Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(dir);
   try {
    string a = Path.Combine(dir, "a.mp4"), b = Path.Combine(dir, "b.mp4"), c = Path.Combine(dir, "c.mp4");
    File.WriteAllText(a, "test");
    File.WriteAllText(b, "test");
    File.WriteAllText(c, "test");

    var host = new FakeMediaHost();
    var ctrl = new DualSlotPlaybackController(new Playlist(new[] { a, b, c }, "Sequential"), host);
    ctrl.Start();
    int genA = host.SlotGeneration[0];
    ctrl.OnSlotOpened(0, genA);

    // Slot 1 is now loading B, not yet opened
    int genB = host.SlotGeneration[1];
    Assert(host.SlotPath[1] == b, "standby started loading B");

    // A ends while B is still loading
    ctrl.OnSlotEnded(0, genA);
    Assert(ctrl.ActiveIndex == 1, "B promoted to active while loading");
    Assert(host.OpenedLoads.Count == 2, "B must not be opened twice");

    // B finishes loading -> plays once and preloads C (not B again)
    ctrl.OnSlotOpened(1, genB);
    Assert(host.SlotPath[0] == c, "standby preloads C after B opens");
    int genC = host.SlotGeneration[0];
    ctrl.OnSlotOpened(0, genC);

    // B ends -> plays C
    ctrl.OnSlotEnded(1, genB);
    Assert(ctrl.ActiveIndex == 0, "C becomes active after B ends");

    // Verify exact playback order A -> B -> C
    Assert(host.OpenedLoads.Count == 3 && host.OpenedLoads[0] == a && host.OpenedLoads[1] == b && host.OpenedLoads[2] == c, "A -> B -> C loaded once each");
    Assert(host.Played.Count == 3 && host.Played[0] == a && host.Played[1] == b && host.Played[2] == c, "A -> B -> C played once each");

    // Also test stale generation callback is ignored
    ctrl.OnSlotFailed(1, genB - 1, "stale failure");
    Assert(ctrl.ActiveIndex == 0 && !host.NoVideoShown, "stale generation callback ignored");
   } finally {
    foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
    Directory.Delete(dir);
   }
  }

  private static void TestSlowPreloadFailureSkipsToNext() {
   string dir = Path.Combine(Path.GetTempPath(), "FoxVideoSCR-fail-" + Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(dir);
   try {
    string a = Path.Combine(dir, "a.mp4"), b = Path.Combine(dir, "b.mp4"), c = Path.Combine(dir, "c.mp4");
    File.WriteAllText(a, "test");
    File.WriteAllText(b, "test");
    File.WriteAllText(c, "test");

    var host = new FakeMediaHost();
    var ctrl = new DualSlotPlaybackController(new Playlist(new[] { a, b, c }, "Sequential"), host);
    ctrl.Start();
    ctrl.OnSlotOpened(0, host.SlotGeneration[0]);

    // A ends while B is still loading -> B promoted
    int genB = host.SlotGeneration[1];
    ctrl.OnSlotEnded(0, host.SlotGeneration[0]);
    Assert(ctrl.ActiveIndex == 1, "B promoted");

    // Promoted B fails -> skips to C
    ctrl.OnSlotFailed(1, genB, "decode error");
    Assert(host.SlotPath[ctrl.ActiveIndex] == c, "failed promoted B skips to C");
    ctrl.OnSlotOpened(ctrl.ActiveIndex, host.SlotGeneration[ctrl.ActiveIndex]);
    Assert(host.Played.Count == 2 && host.Played[0] == a && host.Played[1] == c, "failed promoted B plays A -> C");

    // Also verify preload failure before A ends skips B -> preloads C
    var host2 = new FakeMediaHost();
    var ctrl2 = new DualSlotPlaybackController(new Playlist(new[] { a, b, c }, "Sequential"), host2);
    ctrl2.Start();
    ctrl2.OnSlotOpened(0, host2.SlotGeneration[0]);
    ctrl2.OnSlotFailed(1, host2.SlotGeneration[1], "preload decode error");
    Assert(host2.SlotPath[1] == c, "failed preload B immediately preloads C");
   } finally {
    foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
    Directory.Delete(dir);
   }
  }

  internal static int Run() {
   string resultPath = Path.Combine(Settings.DirectoryPath, "selftest-result.txt");
   try {
    Directory.CreateDirectory(Settings.DirectoryPath);
    if (File.Exists(resultPath)) File.Delete(resultPath);
   } catch { }

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

    // Primary preview resolution checks (custom video, fit, black, off)
    var previewSettings = new Settings {
     Videos = new List<string>(),
     Fit = "Crop",
     Screens = "Custom",
     Monitors = new List<MonitorSetting> {
      new MonitorSetting { DeviceName = "\\\\.\\DISPLAY1", Mode = "CustomVideo", Fit = "Stretch", Videos = new List<string> { a } },
      new MonitorSetting { DeviceName = "\\\\.\\DISPLAY2", Mode = "Black", Fit = "Global" }
     }
    };
    previewSettings.ResolvePrimaryPreview(new[] { secondary, primary }, out mode, out fit, out list);
    Assert(mode == "CustomVideo" && fit == "Stretch" && list.Count == 1 && list[0] == a, "primary preview custom video and fit");

    previewSettings.Monitors[0].Mode = "Black";
    previewSettings.ResolvePrimaryPreview(new[] { secondary, primary }, out mode, out fit, out list);
    Assert(mode == "Black", "primary preview black mode");

    previewSettings.Monitors[0].Mode = "Off";
    previewSettings.ResolvePrimaryPreview(new[] { secondary, primary }, out mode, out fit, out list);
    Assert(mode == "Off", "primary preview off mode");

    TestSingleLoopWithoutEarlyCutoff();
    TestSlowPreloadDoesNotDuplicate();
    TestSlowPreloadFailureSkipsToNext();

    using (var stream = new MemoryStream()) {
     var serializer = new DataContractJsonSerializer(typeof(Settings));
     serializer.WriteObject(stream, s);
     stream.Position = 0;
     var copy = (Settings)serializer.ReadObject(stream);
     Assert(copy.Videos[0] == a && copy.Volume == 100 && copy.Monitors.Count == 1 && copy.Monitors[0].Videos[0] == b, "JSON round trip");
    }

    Directory.CreateDirectory(Settings.DirectoryPath);
    File.WriteAllText(resultPath, "PASS: 8 checks");
    return 0;
   } catch (Exception ex) {
    try {
     Directory.CreateDirectory(Settings.DirectoryPath);
     File.WriteAllText(resultPath, "FAIL: " + ex.Message);
    } catch { }
    Console.Error.WriteLine("FAIL: " + ex.Message);
    Log.Write("SELFTEST FAILED: " + ex.ToString());
    return 1;
   } finally {
    try {
     if (Directory.Exists(dir)) {
      foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
      Directory.Delete(dir);
     }
    } catch { }
   }
  }
 }
}
