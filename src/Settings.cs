using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

namespace FoxVideoSCR {
 [DataContract] public sealed class Settings {
  [DataMember] public List<string> Videos = new List<string>();
  [DataMember] public string Order = "Sequential";
  [DataMember] public string Fit = "Fit";
  [DataMember] public string Screens = "Primary";
  [DataMember] public double Volume = 50;
  [DataMember] public bool Mute = false;
  [DataMember] public double Speed = 1;
  public static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FoxVideoSCR");
  public static readonly string FilePath = Path.Combine(DirectoryPath, "config.json");
  public static Settings Load() {
   if (File.Exists(FilePath)) {
    try { using (var f = File.OpenRead(FilePath)) { var s = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(f); s.Normalize(); return s; } }
    catch (Exception e) { Log.Write("Config load failed: " + e.Message); }
   }
   var fallback = new Settings();
   using (var key = Registry.CurrentUser.OpenSubKey("Software\\ZD Soft\\Movie Screensaver")) {
    string previous = key == null ? "" : Convert.ToString(key.GetValue("Movie File Path", ""));
    if (File.Exists(previous)) fallback.Videos.Add(previous);
   }
   return fallback;
  }
  public void Normalize() {
   Videos = (Videos ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
   if (Order != "Sequential" && Order != "Shuffle" && Order != "Single") Order = "Sequential";
   if (Fit != "Fit" && Fit != "Crop" && Fit != "Stretch") Fit = "Fit";
   if (Screens != "All" && Screens != "Primary") Screens = "Primary";
   Volume = double.IsNaN(Volume) ? 50 : Math.Max(0, Math.Min(100, Volume));
   Speed = double.IsNaN(Speed) ? 1 : Math.Max(0.5, Math.Min(2, Speed));
  }
  public void Save() {
   Normalize(); Directory.CreateDirectory(DirectoryPath);
   string temp = FilePath + ".tmp";
   using (var f = File.Create(temp)) new DataContractJsonSerializer(typeof(Settings)).WriteObject(f, this);
   if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak"); else File.Move(temp, FilePath);
  }
  public Settings Copy() { return new Settings { Videos = new List<string>(Videos), Order = Order, Fit = Fit, Screens = Screens, Volume = Volume, Mute = Mute, Speed = Speed }; }
 }
 internal static class Log {
  internal static void Write(string message) {
   try {
    Directory.CreateDirectory(Settings.DirectoryPath); string path = Path.Combine(Settings.DirectoryPath, "playback.log");
    if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024) { if (File.Exists(path + ".old")) File.Delete(path + ".old"); File.Move(path, path + ".old"); }
    File.AppendAllText(path, DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
   } catch { }
  }
 }
 internal sealed class Playlist {
  private readonly List<string> files;
  private readonly string order;
  private readonly Random random = new Random();
  private readonly HashSet<int> failed = new HashSet<int>();
  private int index = -1;
  internal Playlist(IEnumerable<string> paths, string mode) { files = paths.Where(File.Exists).ToList(); order = mode; }
  internal string Next(bool failure) {
   if (failure && index >= 0) failed.Add(index);
   var candidates = Enumerable.Range(0, files.Count).Where(i => !failed.Contains(i)).ToList();
   if (candidates.Count == 0) return null;
   if (order == "Single" && index >= 0 && !failure) return files[index];
   if (order == "Shuffle") {
    var pool = candidates.Where(i => i != index).ToList(); if (pool.Count == 0) pool = candidates;
    index = pool[random.Next(pool.Count)];
   } else { int next = candidates.FirstOrDefault(i => i > index); if (!candidates.Any(i => i > index)) next = candidates[0]; index = next; }
   return files[index];
  }
 }
}
