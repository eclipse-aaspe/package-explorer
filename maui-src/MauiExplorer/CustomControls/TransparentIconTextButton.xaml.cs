namespace MauiTestTree;

public partial class TransparentIconTextButton : ContentView
{
	public TransparentIconTextButton()
	{
		InitializeComponent();

        // post init
        TextColor = XamlHelpers.GetDynamicRessource("Backstage_Labels", defValue: Colors.LightGray);
        BorderColor = XamlHelpers.GetDynamicRessource("Backstage_Frame", defValue: Colors.LightGray);
        BorderWidth = 1.2;
    }

    /* =====================
     * Text (Button-like)
     * ===================== */

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(TransparentIconTextButton),
            default(string));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty FontFamilyProperty =
    BindableProperty.Create(
        nameof(FontFamily),
        typeof(string),
        typeof(TransparentIconTextButton));

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            nameof(FontSize),
            typeof(double),
            typeof(TransparentIconTextButton),
            14d);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty TextColorProperty =
    BindableProperty.Create(
        nameof(TextColor),
        typeof(Color),
        typeof(TransparentIconTextButton),
        Colors.White);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty =
        BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(TransparentIconTextButton),
            Colors.White);

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty BorderWidthProperty =
    BindableProperty.Create(
        nameof(BorderWidth),
        typeof(double),
        typeof(TransparentIconTextButton),
        2d);

    public double BorderWidth
    {
        get => (double)GetValue(BorderWidthProperty);
        set => SetValue(BorderWidthProperty, value);
    }

    /* =====================
     * Icon-related
     * ===================== */

    public static readonly BindableProperty IconFontFamilyProperty =
        BindableProperty.Create(
            nameof(IconFontFamily),
            typeof(string),
            typeof(TransparentIconTextButton));

    public string IconFontFamily
    {
        get => (string)GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }

    public static readonly BindableProperty IconGlyphProperty =
        BindableProperty.Create(
            nameof(IconGlyph),
            typeof(string),
            typeof(TransparentIconTextButton));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(TransparentIconTextButton),
            18d);

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /* =====================
     * Clicked (EXACT Button parity)
     * ===================== */

    public event EventHandler? Clicked;

    private void OnInnerButtonClicked(object sender, EventArgs e)
    {
        Clicked?.Invoke(this, e);
    }
}