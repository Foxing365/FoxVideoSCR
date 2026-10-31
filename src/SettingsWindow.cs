using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace FoxVideoSCR {
 internal sealed class SettingsWindow : Window {
  private readonly Application app;
  private readonly ObservableCollection<string> files;
  private readonly ListBox list;
  private readonly ComboBox order, fit, screens, speed;
  private readonly Slider volume;
  private readonly CheckBox mute;
  private readonly TextBlock feedback;
  private PlaybackSession preview;
  private static Brush Ink = new SolidColorBrush(Color.FromRgb(229, 235, 246));
  private static Brush Dim = new SolidColorBrush(Color.FromRgb(146, 162, 184));
  private static Brush Panel = new SolidColorBrush(Color.FromRgb(24, 33, 49));
  internal SettingsWindow(Application application) {
   app = application; Title = "FoxVideoSCR · 视频屏保"; Width = 1040; Height = 740; MinWidth = 900; MinHeight = 650; WindowStartupLocation = WindowStartupLocation.CenterScreen;
   Background = new SolidColorBrush(Color.FromRgb(13, 20, 33)); Foreground = Ink; FontFamily = new FontFamily("Microsoft YaHei UI"); FontSize = 14;
   SourceInitialized += delegate { var target = System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle); if (target != null) target.CompositionTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly; };
   Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(Theme.Xaml);
   var saved = Settings.Load(); files = new ObservableCollection<string>(saved.Videos);
   var root = new Grid { Margin = new Thickness(30) }; Content = root;
   root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); root.RowDefinitions.Add(new RowDefinition()); root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
   var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 24) };
   heading.Children.Add(new TextBlock { Text = "FOX / VIDEO SCREENSAVER", Foreground = new SolidColorBrush(Color.FromRgb(80, 211, 193)), FontSize = 12, FontWeight = FontWeights.Bold });
   heading.Children.Add(new TextBlock { Text = "FoxVideoSCR", FontSize = 34, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 4) });
   heading.Children.Add(new TextBlock { Text = "让喜欢的视频，成为桌面休息时的风景。", Foreground = Dim }); root.Children.Add(heading);
   var body = new Grid(); body.ColumnDefinitions.Add(new ColumnDefinition()); body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) }); Grid.SetRow(body, 1); root.Children.Add(body);
   var playlistPanel = new Grid(); playlistPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); playlistPanel.RowDefinitions.Add(new RowDefinition()); playlistPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
   var left = new Border { Background = Panel, CornerRadius = new CornerRadius(12), Padding = new Thickness(20), Margin = new Thickness(0, 0, 18, 0), Child = playlistPanel }; body.Children.Add(left);
   playlistPanel.Children.Add(new TextBlock { Text = "视频列表", FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
   list = new ListBox { ItemsSource = files, Background = Brushes.Transparent, Foreground = Ink, BorderThickness = new Thickness(0), SelectionMode = SelectionMode.Extended, HorizontalContentAlignment = HorizontalAlignment.Stretch };
   var template = new DataTemplate(); var label = new FrameworkElementFactory(typeof(TextBlock)); label.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding()); label.SetBinding(TextBlock.ToolTipProperty, new System.Windows.Data.Binding()); label.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis); label.SetValue(TextBlock.MarginProperty, new Thickness(4, 10, 4, 10)); template.VisualTree = label; list.ItemTemplate = template;
   Grid.SetRow(list, 1); playlistPanel.Children.Add(list);
   var tools = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) }; tools.Children.Add(Button("添加视频", AddFiles, true)); tools.Children.Add(Button("添加文件夹", AddFolder, false)); tools.Children.Add(Button("移除", Remove, false)); tools.Children.Add(Button("上移", delegate { Move(-1); }, false)); tools.Children.Add(Button("下移", delegate { Move(1); }, false)); Grid.SetRow(tools, 2); playlistPanel.Children.Add(tools);
   var options = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; var right = new Border { Background = Panel, CornerRadius = new CornerRadius(12), Padding = new Thickness(20), Child = new ScrollViewer { Content = options, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } }; Grid.SetColumn(right, 1); body.Children.Add(right);
   options.Children.Add(new TextBlock { Text = "播放偏好", FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
   order = Select(options, "播放顺序", new[] { "顺序循环", "随机播放", "单曲循环" }, new[] { "Sequential", "Shuffle", "Single" }, saved.Order);
   fit = Select(options, "画面适配", new[] { "保持比例 · 完整显示", "保持比例 · 裁切铺满", "拉伸铺满" }, new[] { "Fit", "Crop", "Stretch" }, saved.Fit);
   screens = Select(options, "显示器", new[] { "仅主显示器", "所有显示器" }, new[] { "Primary", "All" }, saved.Screens);
   speed = Select(options, "播放速度", new[] { "0.5 ×", "0.75 ×", "1 ×", "1.25 ×", "1.5 ×", "2 ×" }, new[] { "0.5", "0.75", "1", "1.25", "1.5", "2" }, saved.Speed.ToString(System.Globalization.CultureInfo.InvariantCulture));
   options.Children.Add(new TextBlock { Text = "音量", Foreground = Dim, Margin = new Thickness(0, 6, 0, 8) });
   volume = new Slider { Minimum = 0, Maximum = 100, Value = saved.Volume, TickFrequency = 5, Margin = new Thickness(0, 0, 0, 14), ToolTip = "0–100" }; options.Children.Add(volume);
   mute = new CheckBox { Content = "静音播放", IsChecked = saved.Mute, Foreground = Ink, Margin = new Thickness(0, 0, 0, 18) }; options.Children.Add(mute);
   options.Children.Add(new TextBlock { Text = "多屏时仅主显示器出声。\n移动鼠标、点击或按键退出屏保。\n预览最长播放 25 秒。", Foreground = Dim, FontSize = 12, TextWrapping = TextWrapping.Wrap, LineHeight = 21 });
   var footer = new Grid { Margin = new Thickness(0, 22, 0, 0) }; footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); Grid.SetRow(footer, 2); root.Children.Add(footer);
   feedback = new TextBlock { Text = "本地配置 · 视频文件不会上传", Foreground = Dim, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 16, 0) }; footer.Children.Add(feedback);
   var actions = new WrapPanel(); actions.Children.Add(Button("系统屏保设置", SystemSettings, false)); actions.Children.Add(Button("预览", Preview, false)); actions.Children.Add(Button("保存设置", Save, true)); Grid.SetColumn(actions, 1); footer.Children.Add(actions);
   Closed += delegate { if (preview != null) preview.Stop(); };
  }
  private static Button Button(string text, Action action, bool accent) {
   var b = new Button { Content = text, Padding = new Thickness(13, 9, 13, 9), Margin = new Thickness(0, 0, 7, 7), Background = accent ? new SolidColorBrush(Color.FromRgb(61, 210, 183)) : new SolidColorBrush(Color.FromRgb(41, 55, 76)), Foreground = accent ? new SolidColorBrush(Color.FromRgb(9, 30, 30)) : Ink, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
   b.Click += delegate { try { action(); } catch (Exception e) { MessageBox.Show(e.Message, "FoxVideoSCR", MessageBoxButton.OK, MessageBoxImage.Warning); } }; return b;
  }
  private static ComboBox Select(Panel target, string label, string[] names, string[] values, string selected) {
   target.Children.Add(new TextBlock { Text = label, Foreground = Dim, Margin = new Thickness(0, 0, 0, 7) });
   var c = new ComboBox { Margin = new Thickness(0, 0, 0, 17), Padding = new Thickness(10, 8, 10, 8), Foreground = Ink };
   for (int i = 0; i < names.Length; i++) { var item = new ComboBoxItem { Content = names[i], Tag = values[i] }; c.Items.Add(item); if (values[i] == selected) c.SelectedItem = item; }
   if (c.SelectedIndex < 0) c.SelectedIndex = 0; target.Children.Add(c); return c;
  }
  private static string Value(ComboBox box) { return Convert.ToString(((ComboBoxItem)box.SelectedItem).Tag); }
  private Settings Current() { return new Settings { Videos = files.ToList(), Order = Value(order), Fit = Value(fit), Screens = Value(screens), Speed = double.Parse(Value(speed), System.Globalization.CultureInfo.InvariantCulture), Volume = volume.Value, Mute = mute.IsChecked == true }; }
  private void Add(string path) { if (!files.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase))) files.Add(path); }
  private void AddFiles() { var d = new Microsoft.Win32.OpenFileDialog { Title = "添加视频", Filter = "视频文件|*.mp4;*.mkv;*.avi;*.wmv;*.mov;*.mpg;*.mpeg;*.webm;*.m4v|所有文件|*.*", Multiselect = true }; if (d.ShowDialog(this) == true) foreach (var p in d.FileNames) Add(p); }
  private void AddFolder() {
   using (var d = new Forms.FolderBrowserDialog { Description = "选择视频文件夹（仅添加该文件夹中的视频）" }) {
    if (d.ShowDialog() == Forms.DialogResult.OK) foreach (string p in Directory.GetFiles(d.SelectedPath).OrderBy(x => x)) if (new[] { ".mp4", ".mkv", ".avi", ".wmv", ".mov", ".mpg", ".mpeg", ".webm", ".m4v" }.Contains(Path.GetExtension(p).ToLowerInvariant())) Add(p);
   }
  }
  private void Remove() { foreach (string p in list.SelectedItems.Cast<string>().ToArray()) files.Remove(p); }
  private void Move(int offset) { int from = list.SelectedIndex, to = from + offset; if (from >= 0 && to >= 0 && to < files.Count) { files.Move(from, to); list.SelectedIndex = to; } }
  private void Save() { Current().Save(); feedback.Text = "已保存 · " + files.Count + " 个视频"; }
  private void Preview() { if (preview != null) preview.Stop(); preview = new PlaybackSession(app, false); preview.Start(Current(), 25); }
  private void SystemSettings() { Process.Start(new ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32\\control.exe"), Arguments = "desk.cpl,,@screensaver", UseShellExecute = true }); }
 }
}
