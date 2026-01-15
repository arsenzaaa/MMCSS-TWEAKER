using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MMCSSTweaker.Core;

namespace MMCSSTweaker.Controls;

public partial class IntUpDown : UserControl
{
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(IntUpDown),
            new PropertyMetadata(0, OnRangeChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(IntUpDown),
            new PropertyMetadata(int.MaxValue, OnRangeChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(IntUpDown),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty IsUnsetProperty =
        DependencyProperty.Register(nameof(IsUnset), typeof(bool), typeof(IntUpDown),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsUnsetChanged));

    private bool _isUpdatingText;

    public event RoutedPropertyChangedEventHandler<int>? ValueChanged;

    public IntUpDown()
    {
        InitializeComponent();
        UpdateTextFromValue();
        UpdateEnabledState();
        IsEnabledChanged += (_, _) => UpdateEnabledState();
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsUnset
    {
        get => (bool)GetValue(IsUnsetProperty);
        set => SetValue(IsUnsetProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not IntUpDown control)
            return;

        int clamped = control.Clamp(control.Value);
        if (clamped != control.Value)
        {
            control.Value = clamped;
            return;
        }

        control.IsUnset = false;
        control.UpdateTextFromValue();
        control.ValueChanged?.Invoke(control, new RoutedPropertyChangedEventArgs<int>((int)e.OldValue, (int)e.NewValue));
    }

    private static void OnIsUnsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not IntUpDown control)
            return;

        control.UpdateTextFromValue();
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not IntUpDown control)
            return;

        control.Value = control.Clamp(control.Value);
        control.UpdateTextFromValue();
    }

    private int Clamp(int value)
    {
        int min = Minimum;
        int max = Maximum;
        if (min > max)
            max = min;

        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private void UpdateEnabledState()
    {
        double opacity = IsEnabled ? 1.0 : 0.6;
        RootBorder.Opacity = opacity;
        ValueTextBox.IsReadOnly = !IsEnabled;
        UpButton.IsEnabled = IsEnabled;
        DownButton.IsEnabled = IsEnabled;
    }

    private void UpdateTextFromValue()
    {
        _isUpdatingText = true;
        ValueTextBox.Text = IsUnset ? UiConstants.UnsetValue : Value.ToString(CultureInfo.InvariantCulture);
        _isUpdatingText = false;
    }

    private void ValueTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ValueTextBox.SelectAll();
    }

    private void ValueTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        foreach (char c in e.Text)
        {
            if (!char.IsDigit(c))
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void ValueTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText)
            return;

        if (string.Equals(ValueTextBox.Text, UiConstants.UnsetValue, StringComparison.Ordinal))
        {
            IsUnset = true;
            return;
        }

        if (!int.TryParse(ValueTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            return;

        IsUnset = false;
        Value = Clamp(parsed);
    }

    private void UpButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsUnset)
        {
            IsUnset = false;
            Value = Clamp(Value);
            return;
        }

        Value = Clamp(Value + 1);
    }

    private void DownButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsUnset)
        {
            IsUnset = false;
            Value = Clamp(Value);
            return;
        }

        Value = Clamp(Value - 1);
    }
}
