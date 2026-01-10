using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyUi;
using Microsoft.Maui.Controls;

namespace AasxIntegrationBase
{
    public delegate void IFlyoutControlAction();

    /// <summary>
    /// Marks an user control, which is superimposed on top of the application
    /// </summary>
    public interface IFlyoutControl
    {
        /// <summary>
        /// Event emitted by the Flyout in order to end the dialogue.
        /// </summary>
        event IFlyoutControlAction ControlClosed;

        /// <summary>
        /// Called by the main window immediately after start
        /// </summary>
        void ControlStart();

        /// <summary>
        /// Called by main window, as soon as a keyboard input is avilable
        /// </summary>
        /// <param name="e"></param>
        void ControlPreviewKeyDown(EventArgs e);

        /// <summary>
        /// Called by the main window immediately to hand over a selected range
        /// of lambda actions.
        /// </summary>
        void LambdaActionAvailable(AnyUiLambdaActionBase la);

        /// <summary>
        /// Returns a TaskCompletionSource to be waited for.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        Task<bool> MauiShowPageAsync(INavigation navigation);

    }
}
