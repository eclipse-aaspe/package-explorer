using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;
using AnyUi;
using System.Diagnostics;

#if WINDOWS
using Windows.System;
#endif

namespace MauiTestTree;

/// <summary>
/// Single description of a button in a modal dialogue
/// </summary>
public class ModalFooterButton
{
    /// <summary>
    /// Title of the button to display
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Indicate the "hero" button
    /// </summary>
    public bool Primary { get; set; } = false;

    /// <summary>
    /// Result this very button shall trigger
    /// </summary>
    public AnyUiMessageBoxResult FinalResult = new AnyUiMessageBoxResult();

    public ModalFooterButton() { }

    public ModalFooterButton(string title, AnyUiMessageBoxResult result, bool primary = false)
    {
        Title = title;
        FinalResult = result;
        Primary = primary;
    }
}

public class MessageBoxFlyoutViewModel
{
    public AnyUiMessageBoxImage Symbol { get; set; }
    public string Caption { get; set; } = "";
    public string Message { get; set; } = "";

    public ModalFooterButtonLayout Layout { get; set; } = new();


    /// <summary>
    /// Layout of all buttons in a modal dialogue
    /// </summary>
    public class ModalFooterButtonLayout : List<ModalFooterButton>
    {
        public static ModalFooterButtonLayout Create(
            AnyUiMessageBoxButton dialogButtons,
            string[]? extraButtons = null)
        {
            var res = new ModalFooterButtonLayout();

            // build up from left to right!

            if (extraButtons != null)
                for (int i = 0; i < extraButtons.Length; i++)
                    res.Add(new ModalFooterButton(extraButtons[i], AnyUiMessageBoxResult.Extra0 + i));

            if (dialogButtons == AnyUiMessageBoxButton.OK)
            {
                res.Add(new ModalFooterButton("OK", AnyUiMessageBoxResult.OK, primary: true));
            }
            if (dialogButtons == AnyUiMessageBoxButton.OKCancel)
            {
                res.Add(new ModalFooterButton("Cancel", AnyUiMessageBoxResult.Cancel, primary: true));
                res.Add(new ModalFooterButton("OK", AnyUiMessageBoxResult.OK));
            }
            if (dialogButtons == AnyUiMessageBoxButton.YesNo)
            {
                res.Add(new ModalFooterButton("No", AnyUiMessageBoxResult.No, primary: true));
                res.Add(new ModalFooterButton("Yes", AnyUiMessageBoxResult.Yes));
            }
            if (dialogButtons == AnyUiMessageBoxButton.YesNoCancel)
            {
                res.Add(new ModalFooterButton("Cancel", AnyUiMessageBoxResult.Cancel, primary: true));
                res.Add(new ModalFooterButton("No", AnyUiMessageBoxResult.No));
                res.Add(new ModalFooterButton("Yes", AnyUiMessageBoxResult.Yes));
            }

            return res;
        }

        public bool ContainsResult(AnyUiMessageBoxResult res)
        {
            foreach (var x in this)
                if (x.FinalResult == res)
                    return true;
            return false;
        }
    }
}

public partial class MessageBoxFlyoutPage : ContentPage, IFlyoutControl
{
    //
    // Members
    //

    public AnyUiMessageBoxResult Result = AnyUiMessageBoxResult.None;

    protected MessageBoxFlyoutViewModel _viewModel = new();

    //
    // Integration logic for Outer
    //

    public MessageBoxFlyoutPage(
        string message, string caption,
        AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage symbol)
    {
        InitializeComponent();

        _viewModel = new MessageBoxFlyoutViewModel()
        {
            Symbol = symbol,
            Caption = caption,
            Message = message,
            Layout = MessageBoxFlyoutViewModel.ModalFooterButtonLayout.Create(buttons)
        };

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
                        Windows.System.VirtualKey? vk = null;

                        if (mauiButton.BindingContext is ModalFooterButton mfb)
                        {
                            if (mfb.FinalResult == AnyUiMessageBoxResult.OK)
                                vk = VirtualKey.O;
                            if (mfb.FinalResult == AnyUiMessageBoxResult.Cancel)
                                vk = VirtualKey.Escape;
                            if (mfb.FinalResult == AnyUiMessageBoxResult.Yes)
                                vk = VirtualKey.Y;
                            if (mfb.FinalResult == AnyUiMessageBoxResult.No)
                                vk = VirtualKey.N;
                        }

                        if (vk.HasValue)
                        {
                            nativeButton.KeyboardAccelerators.Add(
                                new Microsoft.UI.Xaml.Input.KeyboardAccelerator
                                {
                                    Key = vk.Value
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
            Result = AnyUiMessageBoxResult.None;
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
            Result = mfb.FinalResult;
            _tcs?.TrySetResult(true);
            ControlClosed?.Invoke();
        }
    }
}