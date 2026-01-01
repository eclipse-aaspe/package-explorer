using CommunityToolkit.Maui.Views;

namespace MauiTestTree;

public partial class ContextMenuPopup : Popup
{
    public ContextMenuSubstituteViewModel ViewModel { get; set; } = new();

    public ContextMenuSubstituteMenuItem? Result { get; set; } = null;

    public ContextMenuPopup(ContextMenuSubstituteViewModel? preset = null)
	{
		InitializeComponent();
        if (preset != null)
            ViewModel = preset;
        BindingContext = ViewModel;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async void ItemsView_ItemTapped(object sender, TappedEventArgs e)
    {
        if (!(sender is Grid g && g.BindingContext is ContextMenuSubstituteMenuItem mi))
            return;

        Result = mi;
        await CloseAsync();
    }
}