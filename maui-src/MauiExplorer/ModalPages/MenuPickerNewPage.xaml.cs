using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;
using System.Diagnostics;

namespace MauiTestTree;

public partial class MenuPickerNewPage : ContentPage
{
    //
    // Members
    //

    protected MenuPickerViewModel _viewModel { get; set; } = new();

    public string? SelectedHeader { get => _viewModel.SelectedItem?.Header; }

    //
    // Integration logic for Outer
    //

    public MenuPickerNewPage(MenuPickerViewModel? preset = null)
    {
        InitializeComponent();
        if (preset == null)
            _viewModel.FillDemoData();
        else
            _viewModel = preset;
        BindingContext = _viewModel;
        SetSelectEnabled(false);
    }

    private TaskCompletionSource<string?>? _tcs;

    public Task<string?> MauiShowPageAsync(INavigation navigation)
    {
        _tcs = new();
        navigation.PushModalAsync(this);
        return _tcs.Task;
    }

    private void SetSelectEnabled(bool enable)
    {
        //if (!enable)
        //{
        //    OkButton.TextColor = Color.FromUint((OkButton.TextColor.ToUint() & 0x00ffffff) | 0x40000000);
        //    OkButton.BorderColor = Color.FromUint((OkButton.BorderColor.ToUint() & 0x00ffffff) | 0x40000000);
        //}
        //else
        //{
        //    OkButton.TextColor = Color.FromUint((OkButton.TextColor.ToUint() & 0x00ffffff) | 0xff000000);
        //    OkButton.BorderColor = Color.FromUint((OkButton.BorderColor.ToUint() & 0x00ffffff) | 0xff000000);
        //}
        //OkButton.IsEnabled = enable;
    }

    private async void OnOuterButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();
        if (sender == CancelButton)
        {
            _viewModel.SelectedItem = null;
            _tcs?.TrySetResult(_viewModel.SelectedItem?.Name);
        }

        if (sender == OkButton && _viewModel.SelectedItem != null)
        {
            _tcs?.TrySetResult(_viewModel.SelectedItem?.Name);
        }
    }

    private void ItemsView_ItemTapped(object sender, TappedEventArgs e)
    {
        if (!(sender is Grid g && g.BindingContext is MenuPickerItem mpi))
            return;

        _viewModel.SelectedItem = mpi;

        foreach (var it in _viewModel.Items)
            it.IsSelected = it == _viewModel.SelectedItem;

        SetSelectEnabled(true);
    }
}