using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;
using System.Diagnostics;
using AasxPackageLogic;
using AdminShellNS;
using AasxPackageLogic.PackageCentral;

#if WINDOWS
using Windows.System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
#endif

namespace MauiTestTree;

public partial class SelectFromRepositoryFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    public AnyUiDialogueDataSelectFromRepository DiaData { get; set; } = new();

    //
    // Integration logic for Outer
    //

    public SelectFromRepositoryFlyoutPage(AnyUiDialogueDataSelectFromRepository? preset = null)
    {
        InitializeComponent();
        if (preset != null)
            DiaData = preset;
        BindingContext = DiaData;
        SetOkEnabled(false);

        PlatformSpecificHandleKey();
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
            TextEntry.Focus();
        });
    }

    private void PlatformSpecificHandleKey()
    {
#if WINDOWS
        if (TextEntry == null)
            return;

        TextEntry.HandlerChanged += (_, _) =>
        {
            if (TextEntry.Handler?.PlatformView is TextBox tb)
            {
                tb.KeyDown += (s, e) =>
                {
                    bool ctrlDown = InputKeyboardSource
                            .GetKeyStateForCurrentThread(VirtualKey.Control)
                            .HasFlag(CoreVirtualKeyStates.Down);

                    if (ctrlDown && e.Key == Windows.System.VirtualKey.Enter)
                    {
                        e.Handled = true;

                        Trace.WriteLine("OK!");

                        PrepareResult();
                        DiaData.Result = true;
                        ControlClosed?.Invoke();
                    }

                    if (e.Key == Windows.System.VirtualKey.Escape)
                    {
                        e.Handled = true;

                        Trace.WriteLine("Escape!");

                        DiaData.Result = false;
                        ControlClosed?.Invoke();
                    }
                };
            }
        };
#endif
    }

    private void SetOkEnabled(bool enable)
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
            DiaData.ResultId = null;
            DiaData.ResultItem = null;
            _tcs?.TrySetResult(false);
            ControlClosed?.Invoke();
        }

        if (sender == OkButton)
        {
            PrepareResult();
            DiaData.Result = true;
            DiaData.ResultItem = null;
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
    }

    private async void OnInnerButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();
        
        if (sender is Microsoft.Maui.Controls.Button btn && btn.BindingContext is PackageContainerRepoItem ri)
        {
            DiaData.ResultItem = ri;
            DiaData.ResultId = null;
            ControlClosed?.Invoke();
        }
    }

    private void TextEntry_TextChanged(object sender, Microsoft.Maui.Controls.TextChangedEventArgs e)
    {
        var ok = DiaData.ResultId?.HasContent() == true;
        SetOkEnabled(ok);
    }
}