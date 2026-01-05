namespace MauiTestTree;

public partial class TransparentEntry : ContentView
{
    public TransparentEntry()
    {
        InitializeComponent();

        // Defaults
        TextColor = XamlHelpers.GetDynamicRessource("Backstage_Labels", Colors.White);
        BorderColor = XamlHelpers.GetDynamicRessource("Backstage_Frame", Colors.White);
        FontSize = -1; // platform default
    }

    // -------- Text --------

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(TransparentEntry),
            default(string),
            BindingMode.TwoWay);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // -------- TextColor --------

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(TransparentEntry),
            Colors.White);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    // -------- BorderColor --------

    public static readonly BindableProperty BorderColorProperty =
        BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(TransparentEntry),
            Colors.White);

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    // -------- IsReadOnly --------

    public static readonly BindableProperty IsReadOnlyProperty =
        BindableProperty.Create(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(TransparentEntry),
            false);

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    // -------- FontSize --------

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            nameof(FontSize),
            typeof(double),
            typeof(TransparentEntry),
            -1d);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    // -------- FontFamily --------

    public static readonly BindableProperty FontFamilyProperty =
        BindableProperty.Create(
            nameof(FontFamily),
            typeof(string),
            typeof(TransparentEntry),
            default(string));

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    // -------- FontAttributes --------

    public static readonly BindableProperty FontAttributesProperty =
        BindableProperty.Create(
            nameof(FontAttributes),
            typeof(FontAttributes),
            typeof(TransparentEntry),
            FontAttributes.None);

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    // -------- HorizontalTextAlignment --------

    public static readonly BindableProperty HorizontalTextAlignmentProperty =
        BindableProperty.Create(
            nameof(HorizontalTextAlignment),
            typeof(TextAlignment),
            typeof(TransparentEntry),
            TextAlignment.Start);

    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }

    // -------- VerticalTextAlignment --------

    public static readonly BindableProperty VerticalTextAlignmentProperty =
        BindableProperty.Create(
            nameof(VerticalTextAlignment),
            typeof(TextAlignment),
            typeof(TransparentEntry),
            TextAlignment.Center);

    public TextAlignment VerticalTextAlignment
    {
        get => (TextAlignment)GetValue(VerticalTextAlignmentProperty);
        set => SetValue(VerticalTextAlignmentProperty, value);
    }

    // -------- Events --------

    public event EventHandler<TextChangedEventArgs>? TextChanged;
    public event EventHandler? Completed;

    void OnInnerTextChanged(object? sender, TextChangedEventArgs e)
        => TextChanged?.Invoke(this, e);

    void OnInnerCompleted(object? sender, EventArgs e)
        => Completed?.Invoke(this, e);
}