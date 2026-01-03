using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;

namespace MauiTestTree;

public partial class EmptyFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    public AnyUiDialogueDataEmpty DiaData { get; set; } = new();

    //
    // Integration logic for Outer
    //

    public EmptyFlyoutPage(AnyUiDialogueDataEmpty? preset = null)
    {
        InitializeComponent();
        if (preset != null)
            DiaData = preset;
        BindingContext = DiaData;
        SetSelectEnabled(true);
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
}