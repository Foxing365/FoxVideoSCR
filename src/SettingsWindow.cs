using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace FoxVideoSCR {
 internal sealed class VideoItem {
  public string FilePath { get; set; }
  public string FileName { get; set; }
  public string DirectoryName { get; set; }
  public string ExtensionBadge { get; set; }
  public bool Exists { get; set; }
  public Visibility MissingVisibility { get { return Exists ? Visibility.Collapsed : Visibility.Visible; } }

  internal static VideoItem FromPath(string path) {
   string ext = Path.GetExtension(path);
   return new VideoItem {
    FilePath = path,
    FileName = Path.GetFileName(path),
    DirectoryName = Path.GetDirectoryName(path) ?? "",
    ExtensionBadge = string.IsNullOrEmpty(ext) ? "VIDEO" : ext.TrimStart('.').ToUpperInvariant(),
    Exists = File.Exists(path)
   };
  }
 }

 internal sealed class SettingsWindow : Window {
  private readonly Application app;
  private readonly ObservableCollection<VideoItem> files = new ObservableCollection<VideoItem>();
  private readonly ListBox list;
  private readonly Border emptyHint;
  private readonly TextBlock countBadge;
  private readonly ComboBox order, fit, screens, speed;
  private readonly Slider volume;
  private readonly TextBlock volumeValue;
  private readonly CheckBox mute;
  private readonly StackPanel monitorCardsHost;
  private readonly TextBlock feedback;
  private readonly Dictionary<string, MonitorSetting> monitorConfigs = new Dictionary<string, MonitorSetting>(StringComparer.OrdinalIgnoreCase);
  private List<DisplayInfo> displays = new List<DisplayInfo>();
  private PlaybackSession preview;

  private static readonly Brush BgWindow = new SolidColorBrush(Color.FromRgb(15, 17, 23));
  private static readonly Brush BgCard = new SolidColorBrush(Color.FromRgb(24, 28, 36));
  private static readonly Brush BgSubCard = new SolidColorBrush(Color.FromRgb(30, 35, 46));
  private static readonly Brush BorderSubtle = new SolidColorBrush(Color.FromRgb(40, 46, 58));
  private static readonly Brush InkPrimary = new SolidColorBrush(Color.FromRgb(241, 245, 249));
  private static readonly Brush InkSecondary = new SolidColorBrush(Color.FromRgb(148, 163, 184));
  private static readonly Brush InkMuted = new SolidColorBrush(Color.FromRgb(100, 116, 139));
  private static readonly Brush AccentBlue = new SolidColorBrush(Color.FromRgb(59, 130, 246));
  private static readonly Brush AccentSoftBg = new SolidColorBrush(Color.FromRgb(29, 46, 73));
  private static readonly Brush AccentSoftText = new SolidColorBrush(Color.FromRgb(96, 165, 250));
  private static readonly Brush BadgeNeutralBg = new SolidColorBrush(Color.FromRgb(38, 46, 61));
  private static readonly Brush WarnBg = new SolidColorBrush(Color.FromRgb(69, 26, 33));
  private static readonly Brush WarnText = new SolidColorBrush(Color.FromRgb(248, 113, 113));

  internal SettingsWindow(Application application) {
   app = application;
   Title = "FoxVideoSCR · 视频屏保设置";
   Width = 1080;
   Height = 740;
   MinWidth = 920;
   MinHeight = 640;
   WindowStartupLocation = WindowStartupLocation.CenterScreen;
   Background = BgWindow;
   Foreground = InkPrimary;
   FontFamily = new FontFamily("Microsoft YaHei UI");
   FontSize = 13;
   UseLayoutRounding = true;
   SnapsToDevicePixels = true;
   TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

   SourceInitialized += delegate { Native.EnableDarkTitleBar(this); };
   Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(Theme.Xaml);

   var saved = Settings.Load();
   foreach (var path in saved.Videos) files.Add(VideoItem.FromPath(path));
   if (saved.Monitors != null) {
    foreach (var m in saved.Monitors) {
     if (m != null && !string.IsNullOrEmpty(m.DeviceName)) monitorConfigs[m.DeviceName] = m.Copy();
    }
   }

   var root = new Grid { Margin = new Thickness(24, 20, 24, 18) };
   root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
   root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
   root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
   Content = root;

   // Top Header Bar
   var header = new Grid { Margin = new Thickness(0, 0, 0, 18) };
   header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

   var brandRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
   var iconBadge = new Border {
    Width = 40,
    Height = 40,
    CornerRadius = new CornerRadius(10),
    Background = AccentBlue,
    Margin = new Thickness(0, 0, 12, 0),
    Child = new TextBlock { Text = "▶", Foreground = Brushes.White, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) }
   };
   brandRow.Children.Add(iconBadge);

   var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
   var titleLine = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
   titleLine.Children.Add(new TextBlock { Text = "FoxVideoSCR", FontSize = 21, FontWeight = FontWeights.SemiBold, Foreground = InkPrimary });
   var versionPill = new Border {
    Background = AccentSoftBg,
    CornerRadius = new CornerRadius(5),
    Padding = new Thickness(7, 2, 7, 2),
    Margin = new Thickness(10, 0, 0, 0),
    VerticalAlignment = VerticalAlignment.Center,
    Child = new TextBlock { Text = "v0.2.1 · 64位", Foreground = AccentSoftText, FontSize = 11, FontWeight = FontWeights.SemiBold }
   };
   titleLine.Children.Add(versionPill);
   titleStack.Children.Add(titleLine);
   titleStack.Children.Add(new TextBlock { Text = "Windows 本地视频屏保 · 多显示器独立控制", Foreground = InkSecondary, FontSize = 12, Margin = new Thickness(0, 2, 0, 0) });
   brandRow.Children.Add(titleStack);
   header.Children.Add(brandRow);

   var topActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
   topActions.Children.Add(MakeButton("系统屏保设置", SystemSettings, null, new Thickness(0, 0, 8, 0)));
   topActions.Children.Add(MakeButton("预览 25 秒", Preview, null, new Thickness(0, 0, 8, 0)));
   topActions.Children.Add(MakeButton("保存设置", Save, "PrimaryButton", new Thickness(0)));
   Grid.SetColumn(topActions, 1);
   header.Children.Add(topActions);
   root.Children.Add(header);

   // Main Body (Left: Playlist Card, Right: Tabbed Settings Card)
   var body = new Grid();
   body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(415) });
   Grid.SetRow(body, 1);
   root.Children.Add(body);

   // Left Card: Video Playlist
   var leftGrid = new Grid();
   leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
   leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
   leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

   var listHeader = new Grid { Margin = new Thickness(0, 0, 0, 14) };
   listHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   listHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   var listTitleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
   listTitleStack.Children.Add(new TextBlock { Text = "主视频列表", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = InkPrimary });
   countBadge = new TextBlock { Text = "", Foreground = AccentSoftText, FontSize = 12, FontWeight = FontWeights.SemiBold };
   var countPill = new Border { Background = AccentSoftBg, CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Child = countBadge };
   listTitleStack.Children.Add(countPill);
   listHeader.Children.Add(listTitleStack);

   var addButtons = new StackPanel { Orientation = Orientation.Horizontal };
   addButtons.Children.Add(MakeButton("+ 添加视频", AddFiles, "PrimaryButton", new Thickness(0, 0, 8, 0)));
   addButtons.Children.Add(MakeButton("添加文件夹", AddFolder, null, new Thickness(0)));
   Grid.SetColumn(addButtons, 1);
   listHeader.Children.Add(addButtons);
   leftGrid.Children.Add(listHeader);

   var listContainer = new Grid { AllowDrop = true };
   listContainer.DragOver += OnListDragOver;
   listContainer.Drop += OnListDrop;

   list = new ListBox {
    ItemsSource = files,
    Background = Brushes.Transparent,
    Foreground = InkPrimary,
    BorderThickness = new Thickness(0),
    SelectionMode = SelectionMode.Extended,
    HorizontalContentAlignment = HorizontalAlignment.Stretch,
    ItemTemplate = BuildVideoItemTemplate()
   };
   ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
   listContainer.Children.Add(list);

   emptyHint = new Border {
    Background = new SolidColorBrush(Color.FromRgb(20, 24, 32)),
    BorderBrush = BorderSubtle,
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(10),
    Padding = new Thickness(24),
    IsHitTestVisible = false,
    Child = new StackPanel {
     VerticalAlignment = VerticalAlignment.Center,
     HorizontalAlignment = HorizontalAlignment.Center,
     Children = {
      new TextBlock { Text = "还没有添加视频", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = InkPrimary, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) },
      new TextBlock { Text = "将本地视频或文件夹拖拽到这里，或点击右上角「+ 添加视频」", Foreground = InkSecondary, FontSize = 13, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap }
     }
    }
   };
   listContainer.Children.Add(emptyHint);
   Grid.SetRow(listContainer, 1);
   leftGrid.Children.Add(listContainer);

   var listFooter = new Grid { Margin = new Thickness(0, 12, 0, 0) };
   listFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   listFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   var orderTools = new StackPanel { Orientation = Orientation.Horizontal };
   orderTools.Children.Add(MakeButton("上移", delegate { MoveSelected(-1); }, null, new Thickness(0, 0, 8, 0)));
   orderTools.Children.Add(MakeButton("下移", delegate { MoveSelected(1); }, null, new Thickness(0, 0, 8, 0)));
   orderTools.Children.Add(MakeButton("移除选中", RemoveSelected, "DangerButton", new Thickness(0)));
   listFooter.Children.Add(orderTools);

   var clearBtn = MakeButton("清空列表", ClearFiles, null, new Thickness(0));
   Grid.SetColumn(clearBtn, 1);
   listFooter.Children.Add(clearBtn);
   Grid.SetRow(listFooter, 2);
   leftGrid.Children.Add(listFooter);

   var leftCard = new Border {
    Background = BgCard,
    BorderBrush = BorderSubtle,
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(12),
    Padding = new Thickness(18),
    Margin = new Thickness(0, 0, 16, 0),
    Child = leftGrid
   };
   body.Children.Add(leftCard);

   // Right Card: Tabs (Playback Preferences | Multi-Monitor Settings)
   var tabs = new TabControl();

   // Tab 1: Playback Preferences
   var prefStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
   order = AddComboField(prefStack, "播放顺序", new[] { "顺序循环", "随机播放（不连续重复）", "单曲循环" }, new[] { "Sequential", "Shuffle", "Single" }, saved.Order);
   fit = AddComboField(prefStack, "默认画面适配", new[] { "裁切铺满（无黑边 · 推荐）", "保持比例（完整显示，可能有黑边）", "拉伸铺满" }, new[] { "Crop", "Fit", "Stretch" }, saved.Fit);
   speed = AddComboField(prefStack, "播放速度", new[] { "0.5 ×", "0.75 ×", "1.0 ×（正常）", "1.25 ×", "1.5 ×", "2.0 ×" }, new[] { "0.5", "0.75", "1", "1.25", "1.5", "2" }, saved.Speed.ToString(CultureInfo.InvariantCulture));

   var volHeader = new Grid { Margin = new Thickness(0, 4, 0, 6) };
   volHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   volHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   volHeader.Children.Add(new TextBlock { Text = "音量", Foreground = InkSecondary, FontSize = 12, FontWeight = FontWeights.Medium });
   volumeValue = new TextBlock { Text = Math.Round(saved.Volume) + "%", Foreground = InkPrimary, FontSize = 12, FontWeight = FontWeights.SemiBold };
   Grid.SetColumn(volumeValue, 1);
   volHeader.Children.Add(volumeValue);
   prefStack.Children.Add(volHeader);

   volume = new Slider { Minimum = 0, Maximum = 100, Value = saved.Volume, TickFrequency = 1, IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 16) };
   volume.ValueChanged += delegate { volumeValue.Text = Math.Round(volume.Value) + "%"; };
   prefStack.Children.Add(volume);

   mute = new CheckBox { Content = "静音播放", IsChecked = saved.Mute, Margin = new Thickness(0, 0, 0, 18) };
   prefStack.Children.Add(mute);

   var tipBox = new Border {
    Background = BgSubCard,
    BorderBrush = BorderSubtle,
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(8),
    Padding = new Thickness(12, 10, 12, 10),
    Child = new TextBlock {
     Text = "• 多显示器播放时仅主显示器出声。\n• 移动鼠标、点击或按任意键立即退出屏保。\n• 已启用 Per-Monitor DPI v2，适配多屏不同缩放。\n• 如需给副屏单独设置视频或黑屏，请切换到「显示器设置」。",
     Foreground = InkSecondary,
     FontSize = 12,
     LineHeight = 20,
     TextWrapping = TextWrapping.Wrap
    }
   };
   prefStack.Children.Add(tipBox);

   tabs.Items.Add(new TabItem {
    Header = "播放偏好",
    Content = new ScrollViewer { Content = prefStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }
   });

   // Tab 2: Multi-Monitor Settings
   var monStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
   screens = AddComboField(monStack, "多显示器策略", new[] {
    "所有显示器播放视频",
    "仅主屏播放，副屏纯黑遮罩",
    "仅主屏（副屏不遮挡）",
    "自定义每块显示器（独立视频 / 黑屏 / 适配）"
   }, new[] { "All", "PrimaryBlackout", "Primary", "Custom" }, saved.Screens);
   screens.SelectionChanged += delegate { RebuildMonitorCards(); };

   var monHeaderRow = new Grid { Margin = new Thickness(0, 2, 0, 10) };
   monHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   monHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   monHeaderRow.Children.Add(new TextBlock { Text = "已检测到的显示器", Foreground = InkSecondary, FontSize = 12, FontWeight = FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center });
   var refreshBtn = MakeButton("刷新", delegate { RefreshDisplays(); }, null, new Thickness(0));
   refreshBtn.Padding = new Thickness(10, 4, 10, 4);
   refreshBtn.FontSize = 12;
   Grid.SetColumn(refreshBtn, 1);
   monHeaderRow.Children.Add(refreshBtn);
   monStack.Children.Add(monHeaderRow);

   monitorCardsHost = new StackPanel();
   monStack.Children.Add(monitorCardsHost);

   tabs.Items.Add(new TabItem {
    Header = "显示器设置",
    Content = new ScrollViewer { Content = monStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }
   });

   var rightCard = new Border {
    Background = BgCard,
    BorderBrush = BorderSubtle,
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(12),
    Padding = new Thickness(18),
    Child = tabs
   };
   Grid.SetColumn(rightCard, 1);
   body.Children.Add(rightCard);

   // Bottom Status Bar
   var footer = new Grid { Margin = new Thickness(2, 14, 2, 0) };
   footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
   footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
   feedback = new TextBlock { Text = "本地配置 · 支持拖拽视频文件或文件夹到列表", Foreground = InkSecondary, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
   footer.Children.Add(feedback);
   var archText = new TextBlock { Text = "x64 · 64位原生解码 · 无联网", Foreground = InkMuted, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
   Grid.SetColumn(archText, 1);
   footer.Children.Add(archText);
   Grid.SetRow(footer, 2);
   root.Children.Add(footer);

   files.CollectionChanged += delegate { UpdateListState(); };
   UpdateListState();
   RefreshDisplays();

   Closed += delegate { if (preview != null) preview.Stop(); };
  }

  private static DataTemplate BuildVideoItemTemplate() {
   const string xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
    <Grid>
     <Grid.ColumnDefinitions>
      <ColumnDefinition Width='*'/>
      <ColumnDefinition Width='Auto'/>
     </Grid.ColumnDefinitions>
     <StackPanel Margin='0,0,10,0'>
      <TextBlock Text='{Binding FileName}' FontSize='13.5' FontWeight='SemiBold' Foreground='#F1F5F9' TextTrimming='CharacterEllipsis' ToolTip='{Binding FilePath}'/>
      <TextBlock Text='{Binding DirectoryName}' FontSize='11.5' Foreground='#94A3B8' Margin='0,2,0,0' TextTrimming='CharacterEllipsis' ToolTip='{Binding FilePath}'/>
     </StackPanel>
     <StackPanel Grid.Column='1' Orientation='Horizontal' VerticalAlignment='Center'>
      <Border Visibility='{Binding MissingVisibility}' Background='#451A21' CornerRadius='4' Padding='6,2' Margin='0,0,6,0'>
       <TextBlock Text='文件缺失' FontSize='11' Foreground='#F87171' FontWeight='SemiBold'/>
      </Border>
      <Border Background='#262E3D' CornerRadius='4' Padding='6,2'>
       <TextBlock Text='{Binding ExtensionBadge}' FontSize='11' Foreground='#94A3B8' FontWeight='SemiBold'/>
      </Border>
     </StackPanel>
    </Grid>
   </DataTemplate>";
   return (DataTemplate)System.Windows.Markup.XamlReader.Parse(xaml);
  }

  private void UpdateListState() {
   countBadge.Text = files.Count + " 个视频";
   emptyHint.Visibility = files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
  }

  private void RefreshDisplays() {
   displays = DisplayEnumerator.GetAll();
   RebuildMonitorCards();
  }

  private MonitorSetting EnsureMonitorConfig(string deviceName) {
   MonitorSetting cfg;
   if (!monitorConfigs.TryGetValue(deviceName, out cfg)) {
    cfg = new MonitorSetting { DeviceName = deviceName, Mode = "Video", Fit = "Global", Videos = new List<string>() };
    monitorConfigs[deviceName] = cfg;
   }
   return cfg;
  }

  private void RebuildMonitorCards() {
   monitorCardsHost.Children.Clear();
   string strategy = ComboValue(screens);

   for (int i = 0; i < displays.Count; i++) {
    var d = displays[i];
    var cardStack = new StackPanel();

    // Card header: Monitor index, Primary/Secondary badge, Resolution & Orientation
    var head = new Grid { Margin = new Thickness(0, 0, 0, 8) };
    head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

    var titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    titleRow.Children.Add(new TextBlock { Text = "显示器 " + d.Index, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = InkPrimary });
    var rolePill = new Border {
     Background = d.IsPrimary ? AccentSoftBg : BadgeNeutralBg,
     CornerRadius = new CornerRadius(4),
     Padding = new Thickness(6, 1.5, 6, 1.5),
     Margin = new Thickness(8, 0, 0, 0),
     VerticalAlignment = VerticalAlignment.Center,
     Child = new TextBlock { Text = d.IsPrimary ? "主屏" : "副屏", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = d.IsPrimary ? AccentSoftText : InkSecondary }
    };
    titleRow.Children.Add(rolePill);
    head.Children.Add(titleRow);

    var resText = new TextBlock {
     Text = d.Width + " × " + d.Height + " · " + (d.IsPortrait ? "竖屏" : "横屏"),
     FontSize = 12,
     Foreground = InkSecondary,
     VerticalAlignment = VerticalAlignment.Center
    };
    Grid.SetColumn(resText, 1);
    head.Children.Add(resText);
    cardStack.Children.Add(head);

    if (strategy != "Custom") {
     string summary;
     if (strategy == "All") summary = "播放主视频列表" + (d.IsPrimary ? "（有声）" : "（静音）");
     else if (strategy == "PrimaryBlackout") summary = d.IsPrimary ? "播放主视频列表" : "纯黑遮罩";
     else summary = d.IsPrimary ? "播放主视频列表" : "不显示屏保（保持桌面）";

     var statusRow = new Grid();
     statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
     statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
     statusRow.Children.Add(new TextBlock { Text = "当前行为：" + summary, Foreground = InkSecondary, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
     if (!d.IsPrimary) {
      var switchBtn = MakeButton("单独设置", delegate { SelectComboValue(screens, "Custom"); }, null, new Thickness(0));
      switchBtn.Padding = new Thickness(9, 4, 9, 4);
      switchBtn.FontSize = 11.5;
      Grid.SetColumn(switchBtn, 1);
      statusRow.Children.Add(switchBtn);
     }
     cardStack.Children.Add(statusRow);
    } else {
     var cfg = EnsureMonitorConfig(d.DeviceName);

     var modeBox = AddComboField(cardStack, "显示行为", new[] {
      "播放主视频列表",
      "播放独立视频列表",
      "纯黑遮罩",
      "不显示屏保（保持桌面）"
     }, new[] { "Video", "CustomVideo", "Black", "Off" }, cfg.Mode, 10);

     ComboBox fitBox = null;
     if (cfg.Mode == "Video" || cfg.Mode == "CustomVideo") {
      fitBox = AddComboField(cardStack, "画面适配", new[] {
       "跟随全局设置",
       "裁切铺满（无黑边）",
       "保持比例（完整显示）",
       "拉伸铺满"
      }, new[] { "Global", "Crop", "Fit", "Stretch" }, cfg.Fit, 10);
      fitBox.SelectionChanged += delegate { cfg.Fit = ComboValue(fitBox); };
     }

     if (cfg.Mode == "CustomVideo") {
      var customBox = new Border {
       Background = BgCard,
       BorderBrush = BorderSubtle,
       BorderThickness = new Thickness(1),
       CornerRadius = new CornerRadius(7),
       Padding = new Thickness(10, 8, 10, 8),
       Margin = new Thickness(0, 0, 0, 2)
      };
      var customStack = new StackPanel();
      string desc = cfg.Videos.Count == 0
       ? "尚未选择独立视频（将显示提示）"
       : "已选 " + cfg.Videos.Count + " 个视频：" + Path.GetFileName(cfg.Videos[0]) + (cfg.Videos.Count > 1 ? " 等" : "");
      customStack.Children.Add(new TextBlock { Text = desc, Foreground = cfg.Videos.Count == 0 ? WarnText : InkSecondary, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 0, 8) });

      var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
      var pickBtn = MakeButton("选择视频…", delegate { PickMonitorVideos(cfg); }, "PrimaryButton", new Thickness(0, 0, 6, 0));
      pickBtn.Padding = new Thickness(10, 5, 10, 5);
      pickBtn.FontSize = 12;
      var folderBtn = MakeButton("添加文件夹", delegate { PickMonitorFolder(cfg); }, null, new Thickness(0, 0, 6, 0));
      folderBtn.Padding = new Thickness(10, 5, 10, 5);
      folderBtn.FontSize = 12;
      var clrBtn = MakeButton("清空", delegate { cfg.Videos.Clear(); RebuildMonitorCards(); }, null, new Thickness(0));
      clrBtn.Padding = new Thickness(10, 5, 10, 5);
      clrBtn.FontSize = 12;
      btnRow.Children.Add(pickBtn);
      btnRow.Children.Add(folderBtn);
      btnRow.Children.Add(clrBtn);
      customStack.Children.Add(btnRow);
      customBox.Child = customStack;
      cardStack.Children.Add(customBox);
     }

     modeBox.SelectionChanged += delegate {
      cfg.Mode = ComboValue(modeBox);
      RebuildMonitorCards();
     };
    }

    var card = new Border {
     Background = BgSubCard,
     BorderBrush = BorderSubtle,
     BorderThickness = new Thickness(1),
     CornerRadius = new CornerRadius(9),
     Padding = new Thickness(12, 11, 12, 11),
     Margin = new Thickness(0, 0, 0, 10),
     Child = cardStack
    };
    monitorCardsHost.Children.Add(card);
   }
  }

  private void PickMonitorVideos(MonitorSetting cfg) {
   var d = new Microsoft.Win32.OpenFileDialog {
    Title = "选择该显示器的独立视频",
    Filter = "视频文件|*.mp4;*.mkv;*.avi;*.wmv;*.mov;*.mpg;*.mpeg;*.webm;*.m4v|所有文件|*.*",
    Multiselect = true
   };
   if (d.ShowDialog(this) == true) {
    foreach (var p in d.FileNames) {
     if (!cfg.Videos.Any(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) cfg.Videos.Add(p);
    }
    RebuildMonitorCards();
   }
  }

  private void PickMonitorFolder(MonitorSetting cfg) {
   using (var d = new Forms.FolderBrowserDialog { Description = "选择该显示器的视频文件夹" }) {
    if (d.ShowDialog() == Forms.DialogResult.OK) {
     foreach (string p in Directory.GetFiles(d.SelectedPath).OrderBy(x => x)) {
      if (Settings.VideoExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()) &&
          !cfg.Videos.Any(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) {
       cfg.Videos.Add(p);
      }
     }
     RebuildMonitorCards();
    }
   }
  }

  private Button MakeButton(string text, Action action, string styleKey, Thickness margin) {
   var b = new Button { Content = text, Margin = margin };
   if (!string.IsNullOrEmpty(styleKey)) b.Style = (Style)Resources[styleKey];
   b.Click += delegate {
    try { action(); }
    catch (Exception e) { MessageBox.Show(e.Message, "FoxVideoSCR", MessageBoxButton.OK, MessageBoxImage.Warning); }
   };
   return b;
  }

  private static ComboBox AddComboField(Panel target, string label, string[] names, string[] values, string selected, double bottomMargin = 14) {
   target.Children.Add(new TextBlock { Text = label, Foreground = InkSecondary, FontSize = 12, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 6) });
   var c = new ComboBox { Margin = new Thickness(0, 0, 0, bottomMargin) };
   for (int i = 0; i < names.Length; i++) {
    var item = new ComboBoxItem { Content = names[i], Tag = values[i] };
    c.Items.Add(item);
    if (values[i] == selected) c.SelectedItem = item;
   }
   if (c.SelectedIndex < 0) c.SelectedIndex = 0;
   target.Children.Add(c);
   return c;
  }

  private static string ComboValue(ComboBox box) {
   var item = box.SelectedItem as ComboBoxItem;
   return item == null ? "" : Convert.ToString(item.Tag);
  }

  private static void SelectComboValue(ComboBox box, string value) {
   for (int i = 0; i < box.Items.Count; i++) {
    var item = box.Items[i] as ComboBoxItem;
    if (item != null && string.Equals(Convert.ToString(item.Tag), value, StringComparison.OrdinalIgnoreCase)) {
     box.SelectedIndex = i;
     return;
    }
   }
  }

  private Settings BuildCurrentSettings() {
   var s = new Settings {
    Videos = files.Select(x => x.FilePath).ToList(),
    Order = ComboValue(order),
    Fit = ComboValue(fit),
    Screens = ComboValue(screens),
    Speed = double.Parse(ComboValue(speed), CultureInfo.InvariantCulture),
    Volume = Math.Round(volume.Value),
    Mute = mute.IsChecked == true,
    Monitors = monitorConfigs.Values.Select(m => m.Copy()).ToList()
   };
   s.Normalize();
   return s;
  }

  private bool AddPath(string path) {
   if (string.IsNullOrWhiteSpace(path)) return false;
   if (files.Any(x => string.Equals(x.FilePath, path, StringComparison.OrdinalIgnoreCase))) return false;
   files.Add(VideoItem.FromPath(path));
   return true;
  }

  private void OnListDragOver(object sender, DragEventArgs e) {
   e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
   e.Handled = true;
  }

  private void OnListDrop(object sender, DragEventArgs e) {
   if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
   var dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
   if (dropped == null) return;
   int added = 0;
   foreach (var item in dropped) {
    if (Directory.Exists(item)) {
     foreach (string p in Directory.GetFiles(item).OrderBy(x => x)) {
      if (Settings.VideoExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()) && AddPath(p)) added++;
     }
    } else if (File.Exists(item)) {
     if (AddPath(item)) added++;
    }
   }
   feedback.Text = added > 0 ? "已拖入添加 " + added + " 个视频（记得点击保存设置）" : "没有新增视频（可能已存在）";
  }

  private void AddFiles() {
   var d = new Microsoft.Win32.OpenFileDialog {
    Title = "添加视频",
    Filter = "视频文件|*.mp4;*.mkv;*.avi;*.wmv;*.mov;*.mpg;*.mpeg;*.webm;*.m4v|所有文件|*.*",
    Multiselect = true
   };
   if (d.ShowDialog(this) == true) {
    int added = 0;
    foreach (var p in d.FileNames) if (AddPath(p)) added++;
    feedback.Text = "已添加 " + added + " 个视频";
   }
  }

  private void AddFolder() {
   using (var d = new Forms.FolderBrowserDialog { Description = "选择视频文件夹（添加该文件夹中的所有视频）" }) {
    if (d.ShowDialog() == Forms.DialogResult.OK) {
     int added = 0;
     foreach (string p in Directory.GetFiles(d.SelectedPath).OrderBy(x => x)) {
      if (Settings.VideoExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()) && AddPath(p)) added++;
     }
     feedback.Text = "从文件夹添加了 " + added + " 个视频";
    }
   }
  }

  private void RemoveSelected() {
   var selected = list.SelectedItems.Cast<VideoItem>().ToArray();
   foreach (var item in selected) files.Remove(item);
   if (selected.Length > 0) feedback.Text = "已移除 " + selected.Length + " 个视频";
  }

  private void ClearFiles() {
   if (files.Count == 0) return;
   files.Clear();
   feedback.Text = "已清空主视频列表";
  }

  private void MoveSelected(int offset) {
   int from = list.SelectedIndex;
   int to = from + offset;
   if (from >= 0 && to >= 0 && to < files.Count) {
    files.Move(from, to);
    list.SelectedIndex = to;
   }
  }

  private void Save() {
   BuildCurrentSettings().Save();
   feedback.Text = "已保存设置 · 主列表 " + files.Count + " 个视频";
  }

  private void Preview() {
   if (preview != null) preview.Stop();
   preview = new PlaybackSession(app, false);
   preview.Start(BuildCurrentSettings(), 25);
  }

  private void SystemSettings() {
   Process.Start(new ProcessStartInfo {
    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32\\control.exe"),
    Arguments = "desk.cpl,,@screensaver",
    UseShellExecute = true
   });
  }
 }
}
