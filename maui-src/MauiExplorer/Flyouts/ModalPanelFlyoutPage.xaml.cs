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
using Windows.UI.Core;
#endif

namespace MauiTestTree;

public partial class ModalPanelFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    public AnyUiDisplayContextMaui? DisplayContext;

    public AnyUiDialogueDataModalPanel DiaData { get; set; } = new();

    // a little bit Frankenstein: the view model is not the DiaData, but the 'local' on from
    // the extraodinary message bo xdialog
    protected MessageBoxFlyoutViewModel _viewModel = new();

    protected View? _renderedContentContainer = null;

    //
    // Integration logic for Outer
    //

    public ModalPanelFlyoutPage(AnyUiDisplayContextMaui dcMaui, AnyUiDialogueDataModalPanel? preset = null)
    {
        DisplayContext = dcMaui;
        InitializeComponent();
        if (preset != null)
            DiaData = preset;

        _viewModel = new MessageBoxFlyoutViewModel()
        {
            Symbol = AnyUiMessageBoxImage.None, // not rendered here
            Caption = DiaData.Caption,
            Message = DiaData.Message,
            Layout = MessageBoxFlyoutViewModel.ModalFooterButtonLayout.Create(DiaData.Buttons, extraButtons: DiaData.ExtraButtons)
        };

        // a littel voodo (item collection is row reverse)
        _viewModel.Layout.Reverse();

        BindingContext = _viewModel;

        AttachPlatformSpecificHandleKeys();
    }

    private TaskCompletionSource<bool>? _tcs;

    public Task<bool> MauiShowPageAsync(INavigation navigation)
    {
        _tcs = new TaskCompletionSource<bool>();
        navigation.PushModalAsync(this);
        return _tcs.Task;
    }

    // a little bit later
    protected override void OnAppearing()
    {
        base.OnAppearing();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            CreateMauiPanel();
        });
    }

    protected void AttachPlatformSpecificHandleKeys()
    {
        // access
        if (ButtonLayout == null)
            return;

        foreach (var child in ButtonLayout.Children)
        {
#if WINDOWS
            if (child is ContentView cv && cv.Content is Button mauiButton)
            {
                mauiButton.HandlerChanged += (_, _) =>
                {
                    if (mauiButton.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button nativeButton)
                    {
                        if (mauiButton.BindingContext is ModalFooterButton mfb)
                        {
                            // Ok or Yes
                            if (mfb.FinalResult == AnyUiMessageBoxResult.OK || mfb.FinalResult == AnyUiMessageBoxResult.Yes)
                                nativeButton.KeyboardAccelerators.Add(
                                    new Microsoft.UI.Xaml.Input.KeyboardAccelerator
                                    {
                                        Key = VirtualKey.Enter,
                                        Modifiers = VirtualKeyModifiers.Control
                                    });

                            // Cancel or No
                            if (mfb.FinalResult == AnyUiMessageBoxResult.Cancel || mfb.FinalResult == AnyUiMessageBoxResult.No)
                                nativeButton.KeyboardAccelerators.Add(
                                    new Microsoft.UI.Xaml.Input.KeyboardAccelerator
                                    {
                                        Key = VirtualKey.Escape,
                                    });
                        }
                    }
                };
            }
#endif
        }
    }

    private async void OnOuterButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();
        if (sender == CancelButton)
        {
            DiaData.Result = false;
            DiaData.ResultButton = AnyUiMessageBoxResult.None;
            _tcs?.TrySetResult(false);
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
        if (sender is Button btn && btn.BindingContext is ModalFooterButton mfb)
        {
            DiaData.Result = true;
            DiaData.ResultButton = mfb.FinalResult;
            _tcs?.TrySetResult(true);
            ControlClosed?.Invoke();
        }
    }

    protected void CreateMauiPanel(bool forceRenderPanel = false)
    {
        // access
        if (BindingContext == null || DiaData == null || DisplayContext == null)
            return;

        // re-render?
        if (forceRenderPanel)
            DiaData.Panel = DiaData.RenderPanel?.Invoke(DiaData);

        // create some render defaults matching the visual style of the flyout
        var rd = new AnyUiDisplayContextMaui.RenderDefaults()
        {
            FontSizeRel = 0.9,
            ForegroundSelfStand = AnyUiBrushes.White,
            ForegroundControl = null, // leave untouched                
            WidgetToolSet = AnyUiDisplayContextMaui.RenderWidgetToolSet.Transparent
        };

        // direct or scrollable?
        var panelCnt = DisplayContext.GetOrCreateMauiElement(DiaData.Panel, renderDefaults: rd) as View;
        if (!DiaData.DisableScrollArea)
        {
            // use the existing the scroll viewer
            ContentScrollView.Content = null;
            ContentScrollView.Content = panelCnt;
            _renderedContentContainer = panelCnt;
            panelCnt?.InvalidateMeasure();
            this.InvalidateMeasure();
        }
        else
        {
            // kill the Scrollview, use directly
            ContentParentBorder.Content = null;
            ContentParentBorder.Content = panelCnt;
            _renderedContentContainer = panelCnt;
            panelCnt?.InvalidateMeasure();
            this.InvalidateMeasure();
        }
    }
}