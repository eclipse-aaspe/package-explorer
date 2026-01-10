using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyUi;
using Microsoft.Maui.Controls;

namespace AasxIntegrationBase
{
    public interface IFlyoutProvider
    {
        /// <summary>
        /// Returns true, if already an flyout is shown
        /// </summary>
        /// <returns></returns>
        bool IsInFlyout();

        /// <summary>
        /// Starts an Flyout based on an instantiated UserControl. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        /// <param name="uc"></param>
        Task StartFlyoverAsync(VisualElement uc);

        /// <summary>
        /// Initiate closing an existing flyout
        /// </summary>
        Task CloseFlyoverAsync(bool threadSafe = false);

#if TO_DELETE
        /// <summary>
        /// Start UserControl as modal flyout. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        void StartFlyoverModal(VisualElement uc, Action? closingAction = null);
#endif

        /// <summary>
        /// Start UserControl as modal flyout. The UserControl has to implement
        /// the interface IFlyoutControl
        /// </summary>
        Task StartFlyoverModalAsync(VisualElement uc, Action? closingAction = null);


        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        Task<AnyUiMessageBoxResult> MessageBoxFlyoutShowAsync(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image);

        /// <summary>
        /// Returns the window for advanced modal dialogues
        /// </summary>
        Window GetWin32Window();

        /// <summary>
        /// Gets the display context, e.g. to use UI-abstracted forms of dialogues
        /// </summary>
        AnyUiContextBase GetDisplayContext();

    }
}
