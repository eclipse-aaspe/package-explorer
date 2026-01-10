namespace MauiTestTree;

public partial class TreeElemChevron : ContentView
{
    //
    // Constructor
    //

    public TreeElemChevron()
    {
        InitializeComponent();
        // HOMI
        // TheImage.Source = new FontImageSource() { Glyph = "\uf04b", FontFamily = "FASolid", Color = Colors.Red, Size = 40 };
    }

    // Glyph property

    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string), typeof(TreeElemChevron), default(string), BindingMode.TwoWay, null, GlyphChanged);

    public string Glyph
    {
        get { return (string)GetValue(GlyphProperty); }
        set { SetValue(GlyphProperty, value); }
    }

    // TextColor property

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(TreeElemChevron),
            Colors.White);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    // Events

    private static void GlyphChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        var self = (TreeElemChevron)bindable;

        var glyphSpec = self.Glyph;
        var glyphTuple = glyphSpec.Split('|');
        if (glyphTuple.Length != 4)
            return;

        var ndx = (Application.Current?.RequestedTheme == AppTheme.Light) ? 2 : 3;

        if (!Color.TryParse(glyphTuple[ndx], out var color))
            return;

        self.TheImage.Source = new FontImageSource() { 
            Glyph = glyphTuple[0], 
            FontFamily = glyphTuple[1], 
            Color = color, 
            Size = 40 
        };
    }

    // 
    // Properties
    //

    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(TreeElemChevron), false, BindingMode.TwoWay, null, IsExpandedChanged);

    public bool IsExpanded
    {
        get { return (bool)GetValue(IsExpandedProperty); }
        set { SetValue(IsExpandedProperty, value); }
    }

    private static void IsExpandedChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        var self = (TreeElemChevron)bindable;

        self.Rotation = self.IsExpanded ? 90 : 0;
    }

    public static readonly BindableProperty ShowChevronProperty = BindableProperty.Create(nameof(ShowChevron), typeof(bool), typeof(TreeElemChevron), true, BindingMode.OneWay, null, ShowChevronChanged);

    public bool ShowChevron
    {
        get { return (bool)GetValue(ShowChevronProperty); }
        set { SetValue(ShowChevronProperty, value); }
    }

    private static void ShowChevronChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        var self = (TreeElemChevron)bindable;

        //if (self.ShowChevron)
        //    self.FadeTo(1);
        //else
        //    self.FadeTo(0);

        if (self.ShowChevron)
            self.Opacity = 1;
        else
            self.Opacity = 0;
    }

    //
    // Rest
    //

    private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        IsExpanded = !IsExpanded;
    }
}