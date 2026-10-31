namespace FoxVideoSCR {
 internal static class Theme {
  internal const string Xaml = @"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='{x:Type Button}'>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type Button}'>
  <Border x:Name='Frame' Background='{TemplateBinding Background}' CornerRadius='6' Padding='{TemplateBinding Padding}'>
   <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
  </Border><ControlTemplate.Triggers>
   <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Frame' Property='Opacity' Value='0.82'/></Trigger>
   <Trigger Property='IsPressed' Value='True'><Setter TargetName='Frame' Property='Opacity' Value='0.65'/></Trigger>
   <Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Frame' Property='BorderBrush' Value='#50D3C1'/><Setter TargetName='Frame' Property='BorderThickness' Value='1'/></Trigger>
  </ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type ComboBoxItem}'>
 <Setter Property='Foreground' Value='#E5EBF6'/><Setter Property='Padding' Value='10,8'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ComboBoxItem}'>
  <Border x:Name='Frame' Background='Transparent' Padding='{TemplateBinding Padding}'><ContentPresenter/></Border>
  <ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Frame' Property='Background' Value='#324861'/></Trigger><Trigger Property='IsSelected' Value='True'><Setter TargetName='Frame' Property='Background' Value='#294C50'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type ComboBox}'>
 <Setter Property='Background' Value='#26364C'/><Setter Property='BorderBrush' Value='#3B506B'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ComboBox}'>
  <Grid>
   <ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' ClickMode='Press'>
    <ToggleButton.Template><ControlTemplate TargetType='{x:Type ToggleButton}'><Border Background='#26364C' BorderBrush='#3B506B' BorderThickness='1' CornerRadius='6'/></ControlTemplate></ToggleButton.Template>
   </ToggleButton>
   <ContentPresenter IsHitTestVisible='False' Margin='10,9,30,9' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}' VerticalAlignment='Center'/>
   <TextBlock Text='⌄' Foreground='#92A2B8' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,12,0' IsHitTestVisible='False'/>
   <Popup x:Name='PART_Popup' IsOpen='{TemplateBinding IsDropDownOpen}' Placement='Bottom' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>
    <Border Background='#1E2C40' BorderBrush='#3B506B' BorderThickness='1' CornerRadius='6' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}' Padding='3'>
     <ScrollViewer MaxHeight='260' CanContentScroll='True'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer>
    </Border>
   </Popup>
  </Grid>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type CheckBox}'>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type CheckBox}'>
  <StackPanel Orientation='Horizontal'><Border Width='17' Height='17' Background='#26364C' BorderBrush='#5A718C' BorderThickness='1' CornerRadius='4' Margin='0,0,9,0'><TextBlock x:Name='Tick' Text='✓' Foreground='#50D3C1' HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='13' Visibility='Collapsed'/></Border><ContentPresenter VerticalAlignment='Center'/></StackPanel>
  <ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Tick' Property='Visibility' Value='Visible'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type ListBoxItem}'>
 <Setter Property='Foreground' Value='#E5EBF6'/><Setter Property='HorizontalContentAlignment' Value='Stretch'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ListBoxItem}'>
  <Border x:Name='Frame' CornerRadius='5' Background='Transparent' Padding='4' Margin='0,1'><ContentPresenter/></Border>
  <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Frame' Property='Background' Value='#26364C'/></Trigger><Trigger Property='IsSelected' Value='True'><Setter TargetName='Frame' Property='Background' Value='#294C50'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
</ResourceDictionary>";
 }
}
