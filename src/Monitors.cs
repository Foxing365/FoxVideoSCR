using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FoxVideoSCR {
 internal sealed class DisplayInfo {
  internal string DeviceName;
  internal bool IsPrimary;
  internal int Left;
  internal int Top;
  internal int Width;
  internal int Height;
  internal int Index;
  internal bool IsPortrait { get { return Height > Width; } }
  internal string DisplayTitle {
   get {
    string role = IsPrimary ? "主屏" : "副屏";
    string orient = IsPortrait ? "竖屏" : "横屏";
    return "显示器 " + Index + " · " + role + " · " + Width + " × " + Height + " (" + orient + ")";
   }
  }
 }

 internal static class DisplayEnumerator {
  private const uint MONITORINFOF_PRIMARY = 1;

  [StructLayout(LayoutKind.Sequential)]
  private struct RECT {
   public int Left;
   public int Top;
   public int Right;
   public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  private struct MONITORINFOEX {
   public int Size;
   public RECT Monitor;
   public RECT WorkArea;
   public uint Flags;
   [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
   public string DeviceName;
  }

  private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

  [DllImport("user32.dll")]
  private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

  [DllImport("user32.dll", CharSet = CharSet.Auto)]
  private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

  internal static List<DisplayInfo> GetAll() {
   var result = new List<DisplayInfo>();
   EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data) {
    var info = new MONITORINFOEX();
    info.Size = Marshal.SizeOf(typeof(MONITORINFOEX));
    if (GetMonitorInfo(hMonitor, ref info)) {
     result.Add(new DisplayInfo {
      DeviceName = info.DeviceName ?? "",
      IsPrimary = (info.Flags & MONITORINFOF_PRIMARY) != 0,
      Left = info.Monitor.Left,
      Top = info.Monitor.Top,
      Width = Math.Max(1, info.Monitor.Right - info.Monitor.Left),
      Height = Math.Max(1, info.Monitor.Bottom - info.Monitor.Top)
     });
    }
    return true;
   }, IntPtr.Zero);

   result.Sort(delegate(DisplayInfo a, DisplayInfo b) {
    if (a.IsPrimary != b.IsPrimary) return a.IsPrimary ? -1 : 1;
    if (a.Left != b.Left) return a.Left.CompareTo(b.Left);
    return a.Top.CompareTo(b.Top);
   });
   for (int i = 0; i < result.Count; i++) result[i].Index = i + 1;
   return result;
  }
 }
}
