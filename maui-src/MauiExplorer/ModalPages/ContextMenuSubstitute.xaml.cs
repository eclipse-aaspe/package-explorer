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

