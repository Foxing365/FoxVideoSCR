namespace FoxVideoSCR {
 internal static class Theme {
  internal const string Xaml = @"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

<Style TargetType='{x:Type Button}'>
 <Setter Property='Background' Value='#232936'/>
 <Setter Property='Foreground' Value='#E2E8F0'/>
 <Setter Property='BorderBrush' Value='#323B4D'/>
 <Setter Property='BorderThickness' Value='1'/>
 <Setter Property='Padding' Value='13,7'/>
 <Setter Property='FontSize' Value='13'/>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type Button}'>
    <Border x:Name='Frame' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='7' Padding='{TemplateBinding Padding}'>
     <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#2C3445'/>
      <Setter TargetName='Frame' Property='BorderBrush' Value='#475569'/>
     </Trigger>
     <Trigger Property='IsPressed' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#1D222E'/>
     </Trigger>
     <Trigger Property='IsEnabled' Value='False'>
      <Setter TargetName='Frame' Property='Opacity' Value='0.45'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style x:Key='PrimaryButton' TargetType='{x:Type Button}' BasedOn='{StaticResource {x:Type Button}}'>
 <Setter Property='Background' Value='#3B82F6'/>
 <Setter Property='Foreground' Value='#FFFFFF'/>
 <Setter Property='BorderBrush' Value='#3B82F6'/>
 <Setter Property='FontWeight' Value='SemiBold'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type Button}'>
    <Border x:Name='Frame' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='7' Padding='{TemplateBinding Padding}'>
     <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#4F8FF7'/>
      <Setter TargetName='Frame' Property='BorderBrush' Value='#60A5FA'/>
     </Trigger>
     <Trigger Property='IsPressed' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#2563EB'/>
      <Setter TargetName='Frame' Property='BorderBrush' Value='#2563EB'/>
     </Trigger>
     <Trigger Property='IsEnabled' Value='False'>
      <Setter TargetName='Frame' Property='Opacity' Value='0.45'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style x:Key='DangerButton' TargetType='{x:Type Button}' BasedOn='{StaticResource {x:Type Button}}'>
 <Setter Property='Background' Value='#261C22'/>
 <Setter Property='Foreground' Value='#F87171'/>
 <Setter Property='BorderBrush' Value='#4C262E'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type Button}'>
    <Border x:Name='Frame' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='7' Padding='{TemplateBinding Padding}'>
     <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#3B2128'/>
      <Setter TargetName='Frame' Property='BorderBrush' Value='#7F1D1D'/>
     </Trigger>
     <Trigger Property='IsPressed' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#4C1D24'/>
     </Trigger>
     <Trigger Property='IsEnabled' Value='False'>
      <Setter TargetName='Frame' Property='Opacity' Value='0.45'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type ComboBoxItem}'>
 <Setter Property='Foreground' Value='#E2E8F0'/>
 <Setter Property='FontSize' Value='13'/>
 <Setter Property='Padding' Value='10,8'/>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type ComboBoxItem}'>
    <Border x:Name='Frame' Background='Transparent' CornerRadius='5' Padding='{TemplateBinding Padding}' Margin='2,1'>
     <ContentPresenter/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsHighlighted' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#293347'/>
     </Trigger>
     <Trigger Property='IsSelected' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#1E3A5F'/>
      <Setter Property='Foreground' Value='#FFFFFF'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type ComboBox}'>
 <Setter Property='Background' Value='#202531'/>
 <Setter Property='Foreground' Value='#F1F5F9'/>
 <Setter Property='BorderBrush' Value='#30384A'/>
 <Setter Property='FontSize' Value='13'/>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type ComboBox}'>
    <Grid>
     <ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' ClickMode='Press'>
      <ToggleButton.Template>
       <ControlTemplate TargetType='{x:Type ToggleButton}'>
        <Border x:Name='Box' Background='#202531' BorderBrush='#30384A' BorderThickness='1' CornerRadius='7'/>
        <ControlTemplate.Triggers>
         <Trigger Property='IsMouseOver' Value='True'>
          <Setter TargetName='Box' Property='Background' Value='#252C3B'/>
          <Setter TargetName='Box' Property='BorderBrush' Value='#475569'/>
         </Trigger>
         <Trigger Property='IsChecked' Value='True'>
          <Setter TargetName='Box' Property='BorderBrush' Value='#3B82F6'/>
         </Trigger>
        </ControlTemplate.Triggers>
       </ControlTemplate>
      </ToggleButton.Template>
     </ToggleButton>
     <ContentPresenter IsHitTestVisible='False' Margin='11,8,30,8' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}' VerticalAlignment='Center'/>
     <Path Data='M 0 0 L 4.5 4.5 L 9 0' Stroke='#94A3B8' StrokeThickness='1.5' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,1,12,0' IsHitTestVisible='False'/>
     <Popup x:Name='PART_Popup' IsOpen='{TemplateBinding IsDropDownOpen}' Placement='Bottom' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>
      <Border Background='#181C24' BorderBrush='#334155' BorderThickness='1' CornerRadius='8' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}' Margin='0,4,0,0' Padding='3'>
       <ScrollViewer MaxHeight='260' CanContentScroll='True'>
        <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/>
       </ScrollViewer>
      </Border>
     </Popup>
    </Grid>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type CheckBox}'>
 <Setter Property='Foreground' Value='#E2E8F0'/>
 <Setter Property='FontSize' Value='13'/>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type CheckBox}'>
    <StackPanel Orientation='Horizontal' VerticalAlignment='Center'>
     <Border x:Name='Track' Width='40' Height='22' CornerRadius='11' Background='#2D3546' BorderBrush='#3F4A60' BorderThickness='1' Margin='0,0,10,0'>
      <Border x:Name='Knob' Width='16' Height='16' CornerRadius='8' Background='#CBD5E1' HorizontalAlignment='Left' Margin='2,0,2,0'/>
     </Border>
     <ContentPresenter VerticalAlignment='Center'/>
    </StackPanel>
    <ControlTemplate.Triggers>
     <Trigger Property='IsChecked' Value='True'>
      <Setter TargetName='Track' Property='Background' Value='#3B82F6'/>
      <Setter TargetName='Track' Property='BorderBrush' Value='#3B82F6'/>
      <Setter TargetName='Knob' Property='Background' Value='#FFFFFF'/>
      <Setter TargetName='Knob' Property='HorizontalAlignment' Value='Right'/>
     </Trigger>
     <Trigger Property='IsMouseOver' Value='True'>
      <Setter TargetName='Track' Property='Opacity' Value='0.9'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type Slider}'>
 <Setter Property='Height' Value='24'/>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type Slider}'>
    <Grid VerticalAlignment='Center'>
     <Border Height='4' CornerRadius='2' Background='#2C3445' VerticalAlignment='Center'/>
     <Track x:Name='PART_Track'>
      <Track.DecreaseRepeatButton>
       <RepeatButton Command='Slider.DecreaseLarge' Focusable='False'>
        <RepeatButton.Template>
         <ControlTemplate TargetType='{x:Type RepeatButton}'>
          <Border Height='4' CornerRadius='2' Background='#3B82F6' VerticalAlignment='Center'/>
         </ControlTemplate>
        </RepeatButton.Template>
       </RepeatButton>
      </Track.DecreaseRepeatButton>
      <Track.IncreaseRepeatButton>
       <RepeatButton Command='Slider.IncreaseLarge' Focusable='False'>
        <RepeatButton.Template>
         <ControlTemplate TargetType='{x:Type RepeatButton}'>
          <Border Height='4' Background='Transparent' VerticalAlignment='Center'/>
         </ControlTemplate>
        </RepeatButton.Template>
       </RepeatButton>
      </Track.IncreaseRepeatButton>
      <Track.Thumb>
       <Thumb>
        <Thumb.Template>
         <ControlTemplate TargetType='{x:Type Thumb}'>
          <Border x:Name='Dot' Width='16' Height='16' CornerRadius='8' Background='#FFFFFF' BorderBrush='#3B82F6' BorderThickness='2'/>
          <ControlTemplate.Triggers>
           <Trigger Property='IsMouseOver' Value='True'>
            <Setter TargetName='Dot' Property='Width' Value='18'/>
            <Setter TargetName='Dot' Property='Height' Value='18'/>
            <Setter TargetName='Dot' Property='CornerRadius' Value='9'/>
           </Trigger>
          </ControlTemplate.Triggers>
         </ControlTemplate>
        </Thumb.Template>
       </Thumb>
      </Track.Thumb>
     </Track>
    </Grid>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type ListBoxItem}'>
 <Setter Property='Foreground' Value='#F1F5F9'/>
 <Setter Property='HorizontalContentAlignment' Value='Stretch'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type ListBoxItem}'>
    <Border x:Name='Frame' CornerRadius='8' Background='#1D222D' BorderBrush='#272E3D' BorderThickness='1' Padding='12,9' Margin='0,0,4,6'>
     <ContentPresenter/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#232A38'/>
      <Setter TargetName='Frame' Property='BorderBrush' Value='#364156'/>
     </Trigger>
     <Trigger Property='IsSelected' Value='True'>
      <Setter TargetName='Frame' Property='Background' Value='#1D2E49'/>
      <Setter TargetName='Frame' Property='BorderBrush' Value='#3B82F6'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type TabControl}'>
 <Setter Property='Background' Value='Transparent'/>
 <Setter Property='BorderThickness' Value='0'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type TabControl}'>
    <Grid>
     <Grid.RowDefinitions>
      <RowDefinition Height='Auto'/>
      <RowDefinition Height='*'/>
     </Grid.RowDefinitions>
     <Border Background='#11141B' BorderBrush='#252B38' BorderThickness='1' CornerRadius='9' Padding='4' Margin='0,0,0,14'>
      <UniformGrid IsItemsHost='True' Rows='1'/>
     </Border>
     <ContentPresenter Grid.Row='1' ContentSource='SelectedContent'/>
    </Grid>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type TabItem}'>
 <Setter Property='Foreground' Value='#94A3B8'/>
 <Setter Property='FontSize' Value='13'/>
 <Setter Property='FontWeight' Value='Medium'/>
 <Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type TabItem}'>
    <Border x:Name='Pill' Background='Transparent' CornerRadius='6' Padding='10,8'>
     <ContentPresenter ContentSource='Header' HorizontalAlignment='Center' VerticalAlignment='Center'/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'>
      <Setter Property='Foreground' Value='#E2E8F0'/>
     </Trigger>
     <Trigger Property='IsSelected' Value='True'>
      <Setter TargetName='Pill' Property='Background' Value='#263042'/>
      <Setter Property='Foreground' Value='#FFFFFF'/>
      <Setter Property='FontWeight' Value='SemiBold'/>
     </Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

<Style TargetType='{x:Type ScrollBar}'>
 <Setter Property='Width' Value='7'/>
 <Setter Property='MinWidth' Value='7'/>
 <Setter Property='Background' Value='Transparent'/>
 <Setter Property='Template'>
  <Setter.Value>
   <ControlTemplate TargetType='{x:Type ScrollBar}'>
    <Grid Background='Transparent'>
     <Track x:Name='PART_Track' IsDirectionReversed='True'>
      <Track.Thumb>
       <Thumb>
        <Thumb.Template>
         <ControlTemplate TargetType='{x:Type Thumb}'>
          <Border x:Name='Bar' Background='#334155' CornerRadius='3.5' Margin='1,2'/>
          <ControlTemplate.Triggers>
           <Trigger Property='IsMouseOver' Value='True'>
            <Setter TargetName='Bar' Property='Background' Value='#475569'/>
           </Trigger>
          </ControlTemplate.Triggers>
         </ControlTemplate>
        </Thumb.Template>
       </Thumb>
      </Track.Thumb>
     </Track>
    </Grid>
   </ControlTemplate>
  </Setter.Value>
 </Setter>
</Style>

</ResourceDictionary>";
 }
}
