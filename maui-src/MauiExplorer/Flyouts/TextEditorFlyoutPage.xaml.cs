using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
#endif

namespace MauiTestTree;

public partial class TextEditorFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    protected AnyUiDisplayContextMaui _dcMaui;

    public AnyUiDialogueDataTextEditor DiaData { get; set; } = new AnyUiDialogueDataTextEditor();

    // for defining a context menu directly attached to the Flyout
    public Func<AasxMenu>? ContextMenuCreate = null;
    public AasxMenuActionDelegate? ContextMenuAction = null;

    //
    // Integration logic for Outer
    //

    public TextEditorFlyoutPage(AnyUiDisplayContextMaui displayContextMaui, AnyUiDialogueDataTextEditor? preset = null)
    {
        _dcMaui = displayContextMaui;
        InitializeComponent();
        if (preset != null)
            DiaData = preset;
        BindingContext = DiaData;
        SetSelectEnabled(true);

        PlatformSpecificHandleTabKey();
    }

    private void PlatformSpecificHandleTabKey()
    {
#if WINDOWS
        if (DefaultEditor == null)
            return;
        DefaultEditor.HandlerChanged += (_, _) =>
        {
            if (DefaultEditor.Handler?.PlatformView is TextBox tb)
            {
                tb.KeyDown += (s, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Tab)
                    {
                        e.Handled = true;

                        var pos = tb.SelectionStart;
                        tb.Text = tb.Text.Insert(pos, "\t");
                        tb.SelectionStart = pos + 1;
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

    private void SetMimeTypeAndText()
    {
        //if (this.pluginInstance == null && textControl is TextBox tb)
        //    tb.Text = DiaData.Text;
        //if (this.pluginInstance != null && this.pluginInstance.HasAction("set-content"))
        //    this.pluginInstance.InvokeAction("set-content", DiaData.MimeType, DiaData.Text);
    }

    private void ComboBoxPreset_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender == ComboBoxPreset
            && DiaData.Presets != null
            && ComboBoxPreset.SelectedIndex >= 0
            && ComboBoxPreset.SelectedIndex < DiaData.Presets.Count)
        {
            DiaData.Text = DiaData.Presets[ComboBoxPreset.SelectedIndex].Text;
            SetMimeTypeAndText();
        }
    }

    private async void OnInnerButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();

        if (sender == ContextMenuButton)
        {
            // first attempt: directly attached to the flyout
            var am = ContextMenuCreate?.Invoke();
            var cma = ContextMenuAction;

            // 2nd attempt: within the dia data?
            if (DiaData is AnyUiDialogueDataTextEditorWithContextMenu ddcm)
            {
                am = ddcm.ContextMenuCreate?.Invoke();
                if (am != null)
                {
                    cma = ddcm.ContextMenuAction;
                }
            }

            // not?
            if (am == null)
                return;

            // create dedicated view model out of AasxMenu
            var cm = ContextMenuSubstituteViewModel.CreateNew(am);

            // update data
            PrepareResult();

            // show
            var selNdx = await _dcMaui.MauiShowContextMenuForControlWrapper(ContextMenuButton, cm);
            if (selNdx.HasValue && selNdx.Value >= 0 && selNdx.Value < am.Count)
            {
                var mi = am[selNdx.Value];
                cma?.Invoke(mi.Name?.ToLower(), mi, null);
            }
        }
    }
}