using System.Collections.Generic;
using AnyUi;

namespace BlazorExplorer.Data
{
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

    /// <summary>
    /// Layout of all buttons in a modal dialogue
    /// </summary>
    public class ModalFooterButtonLayout : List<ModalFooterButton>
    {
        public static ModalFooterButtonLayout Create(
            AnyUiMessageBoxButton dialogButtons,
            string[] extraButtons = null)
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
