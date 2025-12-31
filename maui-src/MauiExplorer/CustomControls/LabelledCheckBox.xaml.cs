namespace MauiTestTree;

public partial class LabelledCheckBox : ContentView
{
    public LabelledCheckBox()
    {
        InitializeComponent();
    }

    #region Bindable Properties

    // Text
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(LabelledCheckBox), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // IsChecked
    public static readonly BindableProperty IsCheckedProperty =
        BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(LabelledCheckBox), false);

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    // Background
    public static new readonly BindableProperty BackgroundProperty =
        BindableProperty.Create(nameof(Background), typeof(Color), typeof(LabelledCheckBox), Colors.Transparent);

    public new Color Background
    {
        get => (Color)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    // Foreground
    public static readonly BindableProperty ForegroundProperty =
        BindableProperty.Create(nameof(Foreground), typeof(Color), typeof(LabelledCheckBox), Colors.Black);

    public Color Foreground
    {
        get => (Color)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    // Padding
    public static new readonly BindableProperty PaddingProperty =
        BindableProperty.Create(nameof(Padding), typeof(double), typeof(LabelledCheckBox), 5.0);

    /// <summary>
    /// Padding between Checkbox and Label
    /// </summary>
    public new double Padding
    {
        get => (double)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    // FontSize
    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            nameof(FontSize),
            typeof(double),
            typeof(LabelledCheckBox),
            Device.GetNamedSize(NamedSize.Default, typeof(Label)));

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    // FontFamily
    public static readonly BindableProperty FontFamilyProperty =
        BindableProperty.Create(
            nameof(FontFamily),
            typeof(string),
            typeof(LabelledCheckBox),
            default(string));

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    // FontAttributes
    public static readonly BindableProperty FontAttributesProperty =
        BindableProperty.Create(
            nameof(FontAttributes),
            typeof(FontAttributes),
            typeof(LabelledCheckBox),
            FontAttributes.None);

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    // VerticalTextAlignment
    public static readonly BindableProperty VerticalTextAlignmentProperty =
        BindableProperty.Create(
            nameof(VerticalTextAlignment),
            typeof(TextAlignment),
            typeof(LabelledCheckBox),
            TextAlignment.Center);

    public TextAlignment VerticalTextAlignment
    {
        get => (TextAlignment)GetValue(VerticalTextAlignmentProperty);
        set => SetValue(VerticalTextAlignmentProperty, value);
    }

    // HorizontalTextAlignment
    public static readonly BindableProperty HorizontalTextAlignmentProperty =
        BindableProperty.Create(
            nameof(HorizontalTextAlignment),
            typeof(TextAlignment),
            typeof(LabelledCheckBox),
            TextAlignment.Start);

    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler? Checked;
    public event EventHandler? Unchecked;

    private void CheckBoxControl_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
            Checked?.Invoke(this, EventArgs.Empty);
        else
            Unchecked?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}