namespace MauiTestTree;

public partial class NavIconButton : ContentView
{
	public NavIconButton()
	{
		InitializeComponent();
	}

    // Glyph property

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(
            nameof(Glyph),
            typeof(string),
            typeof(NavIconButton),
            default(string));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    // Clicked event (Button-like)
    
    public event EventHandler? Clicked;

    private void OnTapped(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    // Hover visuals (desktop)

    void OnEnter(object s, EventArgs e)
    {
        ((Border)s).BackgroundColor = Color.FromArgb("#33000000");
    }

    void OnExit(object s, EventArgs e)
    {
        ((Border)s).BackgroundColor = Colors.Transparent;
    }
}