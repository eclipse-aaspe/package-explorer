using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;

namespace MauiTestTree;

public partial class MenuCheckableOptions : ContentPage
{
    public MenuCheckableOptionsViewModel ViewModel { get; set; } = new();

    public MenuCheckableOptions(AasxMenu? preset = null)
    {
        InitializeComponent();
        if (preset != null)
            ViewModel.AddFromMainMenu(preset);
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
        if (!(sender is Grid g && g.BindingContext is AasxMenuItem mi))
            return;

        mi.IsChecked = !mi.IsChecked;
    }
}

//
// View model
//

public class MenuCheckableOptionsViewModel
{
    /// <summary>
    /// If set, title for the dialog panel
    /// </summary>
    public string DialogHeader { get; set; } = "Modify edit settings";

    /// <summary>
    /// Items to be edited.
    /// Note: This is a duplication of the original collection, but the single items might be the same.
    /// </summary>
    public ObservableCollection<AasxMenuItem> Items { get; set; } = new();

    /// <summary>
    /// Filters a menu for applicable items.
    /// </summary>
    public void AddFromMainMenu(AasxMenu menu)
    {
        foreach (var x in menu.FindAll<AasxMenuItem>((mib) => mib is AasxMenuItem mi && mi.IsCheckable))
            Items.Add(x);
    }
}

// <summary>
/// Removes any "_" from the Header of a AasxMenuItem
/// </summary>
public class MenuCheckableOptionsRemoveAcceleratorKeyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is string str) ? str.Replace("_","") : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}