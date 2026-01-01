using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using AdminShellNS;
using AasxIntegrationBase;

namespace MauiTestTree;

public partial class ContextMenuSubstitute : ContentPage
{
    public ContextMenuSubstituteViewModel ViewModel { get; set; } = new();

    public ContextMenuSubstitute(ContextMenuSubstituteViewModel? preset = null)
    {
        InitializeComponent();
        if (preset != null)
            ViewModel = preset;
        BindingContext = ViewModel;
    }

    private async void OnOptionClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void ItemsView_ItemTapped(object sender, TappedEventArgs e)
    {
        if (!(sender is Grid g && g.BindingContext is ContextMenuSubstituteMenuItem mi))
            return;        
    }
}

//
// View model
//

public class ContextMenuSubstituteMenuItem
{
    /// <summary>
    /// Text header for the context menu
    /// </summary>
    public string Header { get; set; } = "";

    /// <summary>
    /// Possible heklp text for the context menu
    /// </summary>
    public string HelpText { get; set; } = "";

    /// <summary>
    /// Checks if help text is present
    /// </summary>
    public bool ShowHelpText { get => HelpText?.HasContent() == true; }

    /// <summary>
    /// Glyph (e.g. unicode) for a possible icon
    /// </summary>
    public string IconGlyph { get; set; } = "";
    
    /// <summary>
    /// Font family for the icon
    /// </summary>
    public string IconFontAlias { get; set; } = "";
    
    /// <summary>
    /// Font size for the icon
    /// </summary>
    public int IconFontSize { get; set; } = 16;

    /// <summary>
    /// Checks if icon is present
    /// </summary>
    public bool ShowIcon { get => IconGlyph?.HasContent() == true && IconFontAlias?.HasContent() == true; }
}

public class ContextMenuSubstituteViewModel
{
    /// <summary>
    /// If set, title for the dialog panel
    /// </summary>
    public string DialogHeader { get; set; } = "Select an context option";

    /// <summary>
    /// Items to be edited.
    /// </summary>
    public ObservableCollection<ContextMenuSubstituteMenuItem> Items { get; set; } = new();

    /// <summary>
    /// Creates and initializes a context menu from a list of pair of strings.
    /// First of pair is the icon (or empty), second is the textual header of the menu item.
    /// Format of icon could be e.g. "{awe}\uef50", then the AwesomeFont is being used.
    /// For these preset, the display context must be given.
    /// </summary>
    /// <param name="pairs">Pairs of strings</param>
    /// <param name="dc">MAUI display context with icon fonts</param>
    public static ContextMenuSubstituteViewModel GetFromPairsOfString(
        IEnumerable<string> pairs,
        AnyUiDisplayContextMaui? dc = null,
        double? scaleFontSize = null)
    {
        // start
        var res = new ContextMenuSubstituteViewModel();
        var workPairs = pairs.ToArray();

        // loop
        var nmi = workPairs.Length / 2;
        for (int i = 0; i < nmi; i++)
        {
            // menu item itself
            var menuItem = new ContextMenuSubstituteMenuItem
            {
                Header = "" + workPairs[2 * i + 1],
            };

            // try find icon font
            var input = workPairs[2 * i + 0];
            AnyUiIconFont? fo = null;
            string? glyph = null;
            if (input.StartsWith("{") && input.Contains("}"))
            {
                var p = input.IndexOf('}');
                fo = dc?.FindIconFont(input.Substring(1, p - 1));
                glyph = input.Substring(p + 1);
            }
            else
            {
                fo = dc?.FindIconFont("uc");
                glyph = input;
            }

            // fill icon info?
            if (fo?.FontAlias != null)
            {
                menuItem.IconGlyph = glyph;
                menuItem.IconFontAlias = fo.FontAlias;
                menuItem.IconFontSize = (!scaleFontSize.HasValue) ? fo.FontSize 
                    : (int) (1.0 * scaleFontSize.Value * fo.FontSize);
            }

            // add
            res.Items.Add(menuItem);
        }

        // ok
        return res;
    }
}
