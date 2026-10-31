using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;

namespace FoxVideoSCR {
 internal static class SelfTest {
  private static void Assert(bool condition, string text) { if (!condition) throw new Exception("SELFTEST: " + text); }
  internal static void Run() {
   string dir = Path.Combine(Path.GetTempPath(), "FoxVideoSCR-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
   try {
    string a = Path.Combine(dir, "a.mp4"), b = Path.Combine(dir, "b.mp4"); File.WriteAllText(a, "test"); File.WriteAllText(b, "test");
    var sequence = new Playlist(new[] { a, b, Path.Combine(dir, "missing.mp4") }, "Sequential");
    Assert(sequence.Next(false) == a && sequence.Next(false) == b && sequence.Next(false) == a, "sequence and missing-file handling");
    var failures = new Playlist(new[] { a, b }, "Sequential"); Assert(failures.Next(false) == a && failures.Next(true) == b && failures.Next(true) == null, "failed videos must terminate instead of retrying forever");
    var shuffle = new Playlist(new[] { a, b }, "Shuffle"); string last = shuffle.Next(false); for (int i = 0; i < 30; i++) { string next = shuffle.Next(false); Assert(last != next, "shuffle immediate repeat"); last = next; }
    var single = new Playlist(new[] { a, b }, "Single"); Assert(single.Next(false) == a && single.Next(false) == a && single.Next(true) == b, "single loop and failed-item fallback");
    var s = new Settings { Videos = new List<string> { a, a, "" }, Volume = 200, Speed = 0, Order = "invalid", Fit = "invalid", Screens = "invalid" }; s.Normalize();
    Assert(s.Videos.Count == 1 && s.Volume == 100 && s.Speed == 0.5 && s.Order == "Sequential" && s.Fit == "Fit" && s.Screens == "Primary", "settings validation");
    using (var stream = new MemoryStream()) { var serializer = new DataContractJsonSerializer(typeof(Settings)); serializer.WriteObject(stream, s); stream.Position = 0; var copy = (Settings)serializer.ReadObject(stream); Assert(copy.Videos[0] == a && copy.Volume == 100, "JSON round trip"); }
    Directory.CreateDirectory(Settings.DirectoryPath);
    File.WriteAllText(Path.Combine(Settings.DirectoryPath, "selftest-result.txt"), "PASS: 6 checks");
   } finally { foreach (var f in Directory.GetFiles(dir)) File.Delete(f); Directory.Delete(dir); }
  }
 }
}
