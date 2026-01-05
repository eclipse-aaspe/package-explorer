using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;
using System.Diagnostics;

#if WINDOWS
using Windows.System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
#endif

namespace MauiTestTree;

public partial class TextBoxFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    public AnyUiDialogueDataTextBox DiaData { get; set; } = new();

    //
    // Integration logic for Outer
    //

    public TextBoxFlyoutPage(AnyUiDialogueDataTextBox? preset = null)
    {
        InitializeComponent();
        if (preset != null)
            DiaData = preset;
        BindingContext = DiaData;
        SetSelectEnabled(true);

        PlatformSpecificHandleTabKey();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {             
            TextEntry.Focus();
        });
    }

    private void PlatformSpecificHandleTabKey()
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
            ControlClosed?.Invoke();
        }

        if (sender == OkButton)
        {
            PrepareResult();
            DiaData.Result = true;
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
    }
}