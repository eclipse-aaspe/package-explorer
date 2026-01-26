using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;

#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Security.Authentication.OnlineId;
using Windows.System;
using Windows.UI.Core;
#endif

namespace MauiTestTree;

public partial class SelectFromListFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    protected AnyUiDisplayContextMaui _dcMaui;

    public AnyUiDialogueDataSelectFromList DiaData { get; set; } = new AnyUiDialogueDataSelectFromList();

    //
    // Integration logic for Outer
    //

    public SelectFromListFlyoutPage(
            AnyUiDisplayContextMaui displayContextMaui,
            AnyUiDialogueDataSelectFromList? preset = null)
    {
        _dcMaui = displayContextMaui;
        InitializeComponent();
        if (preset != null)
            DiaData = preset;
        BindingContext = DiaData;
        SetSelectEnabled(false);
        // PlatformSpecificHandleTabKey();
    }

    private TaskCompletionSource<bool>? _tcs;

    public Task<bool> MauiShowPageAsync(INavigation navigation)
    {
        _tcs = new TaskCompletionSource<bool>();
        navigation.PushModalAsync(this);
        return _tcs.Task;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            ItemsView.Focus();
        });
    }

    private void SetSelectEnabled(bool enable)
    {
        if (!enable)
        {
            OkButton.TextColor = Color.FromUint((OkButton.TextColor.ToUint() & 0x00ffffff) | 0x40000000);
            OkButton.BorderColor = Color.FromUint((OkButton.BorderColor.ToUint() & 0x00ffffff) | 0x40000000);
        }
        else
        {
            OkButton.TextColor = Color.FromUint((OkButton.TextColor.ToUint() & 0x00ffffff) | 0xff000000);
            OkButton.BorderColor = Color.FromUint((OkButton.BorderColor.ToUint() & 0x00ffffff) | 0xff000000);
        }
        OkButton.IsEnabled = enable;
    }

    private async void OnOuterButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();
        if (sender == CancelButton)
        {
            DiaData.Result = false;
            _tcs?.TrySetResult(false);
            ControlClosed?.Invoke();
        }

        if (sender == OkButton)
        {
            PrepareResult();
            DiaData.Result = true;
            _tcs?.TrySetResult(true);
            ControlClosed?.Invoke();
        }
    }

    //
    // Further integration with Outer
    //

    public event IFlyoutControlAction? ControlClosed;

    public void ControlStart()
    {
        // called by main window
    }

    public void ControlPreviewKeyDown(EventArgs e)
    {
        // may be useless for MAUI
    }

    public void LambdaActionAvailable(AnyUiLambdaActionBase la)
    {
        // called by main window
    }

    //
    // Inner (custom) logic
    //

    protected void PrepareResult()
    {
        DiaData.ResultItem = ItemsView.SelectedItem as AnyUiDialogueListItem;
        DiaData.ResultIndex = DiaData.ListOfItems.ToList().FindIndex(
                    (m) => m?.Tag == (ItemsView.SelectedItem as AnyUiDialogueListItem)?.Tag);
    }

    private async void OnInnerButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();

        if (sender == AddFileButton)
        {
            // try to derive a options
            var options = new PickOptions
            {
                PickerTitle = DiaData.Caption,
                FileTypes = AnyUiDisplayContextMaui.GetMauiFilePickerFileTypeFromWpfFilter(DiaData.FileFilter)
            };

            // get a result?
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                // add
                DiaData.ListOfItems.Add(new AnyUiDialogueListItem() { 
                    Text = result.FullPath,
                    Tag = result.FullPath
                });

                // make a little visual
                ItemsView.SelectedItem = DiaData.ListOfItems.LastOrDefault();
            }
        }

        if (sender == RemoveFileButton && ItemsView.SelectedItem != null)
        {
            var i = DiaData.ListOfItems.ToList().FindIndex(
                        (m) => m?.Tag == (ItemsView.SelectedItem as AnyUiDialogueListItem)?.Tag);
            if (i >= 0)
            {
                DiaData.ListOfItems.RemoveAt(i);
            }
        }
    }

    private void ItemsView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        if (DiaData.SelectFiles)
        {
            // ok, if any files (aside from selection!) 
            SetSelectEnabled(DiaData.ListOfItems.Count > 0);
        }
        else
        {
            // ok, if the one is selected
            SetSelectEnabled(ItemsView.SelectedItem != null);
        }
    }

    protected List<string> _draggedFilePathes = new();

    protected async void OnDragOver(object sender, DragEventArgs e)
    {
        // see: https://www.youtube.com/watch?v=x6ku0V44GFc
        // https://github.com/YBTopaz8/DragAndDropMAUISample

        _draggedFilePathes = new();

#if WINDOWS
        var WindowsDragEventArgs = e.PlatformArgs?.DragEventArgs;

        if (WindowsDragEventArgs == null)
            return;

        var DraggedOverItems = await WindowsDragEventArgs.DataView.GetStorageItemsAsync();
        e.AcceptedOperation = DataPackageOperation.None;

        if (DraggedOverItems.Count > 0)
        {
            foreach (var item in DraggedOverItems)
            {
                if (item is Windows.Storage.StorageFile file)
                {
                    _draggedFilePathes.Add(file.Path);
                }
            }
        }
#endif

        // Accept only files: not possible in .NET MAUI 9
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    protected async void OnDrop(object sender, DropEventArgs e)
    {
        await Task.Yield();

        if (_draggedFilePathes != null && _draggedFilePathes.Count > 0)
            foreach (var fp in _draggedFilePathes)
            {
                // add
                DiaData.ListOfItems.Add(new AnyUiDialogueListItem()
                {
                    Text = fp,
                    Tag = fp
                });

                // make a little visual
                ItemsView.SelectedItem = DiaData.ListOfItems.LastOrDefault();
            }

        Trace.WriteLine("" + _draggedFilePathes?.FirstOrDefault());
    }
}