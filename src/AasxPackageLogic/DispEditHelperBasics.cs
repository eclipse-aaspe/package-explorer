/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AdminShellNS.DiaryData;
using AdminShellNS.Extensions;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_1;

namespace AasxPackageLogic
{
    //
    // Hinting (will be used below)
    //

    public class HintCheck
    {
        public enum Severity { High, Notice };

        public Func<bool> CheckPred = null;
        public string TextToShow = null;
        public bool BreakIfTrue = false;
        public Severity SeverityLevel = Severity.High;

        /// <summary>
        /// Formulate a check, which can cause a hint.
        /// </summary>
        /// <param name="check">Lambda to check. If returns true, trigger the hint.</param>
        /// <param name="text">Hint in plain text form.</param>
        /// <param name="breakIfTrue">If check was true, abort checking of further hints.
        /// Use: avoid checking of null for every hint.</param>
        /// <param name="severityLevel">Display high/red or normal/blue</param>
        public HintCheck(
            Func<bool> check, string text, bool breakIfTrue = false, Severity severityLevel = Severity.High)
        {
            this.CheckPred = check;
            this.TextToShow = text;
            this.BreakIfTrue = breakIfTrue;
            this.SeverityLevel = severityLevel;
        }
    }

    //
    // Highlighting
    //

    public static class DispEditHighlight
    {
        public class HighlightFieldInfo
        {
            public object containingObject;
            public object fieldObject;
            public int fieldHash;

            public HighlightFieldInfo() { }

            public HighlightFieldInfo(object containingObject, object fieldObject, int fieldHash)
            {
                this.containingObject = containingObject;
                this.fieldObject = fieldObject;
                this.fieldHash = fieldHash;
            }
        }
    }

    //
    // Color palette
    //    

    public class DispLevelColors
    {
        public AnyUiBrushTuple
            MainSection, SubSection, SubSubSection,
            HintSeverityHigh, HintSeverityNotice;

        public static DispLevelColors GetLevelColorsFromOptions(OptionsInformation opt)
        {
            // access
            if (opt == null)
                return null;

            // ReSharper disable CoVariantArrayConversion            
            var res = new DispLevelColors()
            {
                MainSection = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkestAccentColor)),
                    AnyUiBrushes.White),
                SubSection = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkAccentColor)),
                    AnyUiBrushes.Black),
                SubSubSection = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkAccentColor)),
                    AnyUiBrushes.Black),
                HintSeverityHigh = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.FocusErrorBrush)),
                    AnyUiBrushes.White),
                HintSeverityNotice = new AnyUiBrushTuple(
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkAccentColor)),
                    new AnyUiBrush(opt.GetColor(OptionsInformation.ColorNames.DarkestAccentColor)))
            };
            // ReSharper enable CoVariantArrayConversion
            return res;
        }
    }

    /// <summary>
    /// This class provides hints, how different controls are placed in the UI in terms of layout.
    /// </summary>
    public class UILayoutHints
    {
        public enum PosOfControl { Top, Context, Bottom }

        /// <summary>
        /// In conventional Package Explorer, particular grid for enumerables had buttons in the top row.
        /// Pro: very logical distinction between enumerables collection and single items
        /// Cons: A lot vertical space added for e.g. mobile UIs
        /// In ADX Hub, the button is on bottom, which seems to be more logical
        /// </summary>
        public PosOfControl PlacementAdd = PosOfControl.Top;

        /// <summary>
        /// In conventional Package Explorer, text fields provided an explicit multi line edit button (3 horizontal lines).
        /// Pros: Speed up editing of text content
        /// Cons: Takes horizontal space for each text field
        /// </summary>
        public bool ExplicitMultiLineEdit = true;

        // some styles

        /// <summary>
        /// This style is to be taken for Buttons, which are pretty regular and might come
        /// in numbers. Idea is, that they are recognizable as Buttons, but are visually light.
        /// </summary>
        public AnyUiButtonOverStyle StyleButtonStandard = new();

        /// <summary>
        /// This style is to be taken for Buttons, which are an important action for the user, 
        /// enabling him to do something relevant.
        /// </summary>
        public AnyUiButtonOverStyle StyleButtonAction = new();

        /// <summary>
        /// This style is for Buttons, which acknowledge a whole transaction, so a set of many
        /// actions.
        /// </summary>
        public AnyUiButtonOverStyle StyleButtonHero = new();

        /// <summary>
        /// Display text and/or image for: medium clear/ obvious button
        /// </summary>
        public AnyUiButtonPreference ButtonPrefMediumClear = AnyUiButtonPreference.Both;

        /// <summary>
        /// Display text and/or image for: buttons, which icon is rather unclear
        /// </summary>
        public AnyUiButtonPreference ButtonPrefLowClear = AnyUiButtonPreference.Both;
    }

    //
    // Helpers
    //

    // ReSharper disable once UnusedType.Global
    public class DispEditHelperBasics : AnyUiSmallWidgetToolkit
    {
        //
        // Members
        //

        public PackageCentral.PackageCentral packages = null;
        public IPushApplicationEvent appEventsProvider = null;

        public DispLevelColors levelColors = null;

        public enum FirstColumnWidth { No, Standard, Small, Large }

        public enum KeyLabelHandling { Standard, No, Above }

        public const int valueFieldsMinWidth = 50;

        public bool editMode = false;
        public bool hintMode = false;

        public ModifyRepo repo = null;

        public DispEditHighlight.HighlightFieldInfo highlightField = null;
        private AnyUiFrameworkElement lastHighlightedField = null;

        public AnyUiContextBase context = null;

        public UILayoutHints LayoutHints = new();

        /// <summary>
        /// This tag value identifies controls, which can be substituted in order to 
        /// reduce complexity.
        /// </summary>
        public const string TAG_ControlToBeSubstituted = nameof(TAG_ControlToBeSubstituted);

        //
        // Width of first column
        // (not always identical to maintain space efficiency)
        //

        public int GetWidth(FirstColumnWidth cw)
        {
            if (cw == FirstColumnWidth.No)
                return 0;
            if (cw == FirstColumnWidth.Small)
                return 40;
            if (cw == FirstColumnWidth.Large)
                return 220;
            return 130;
        }

        //
        // Highlighting
        //

        public void HighligtStateElement(AnyUiFrameworkElement fe, bool highlighted)
        {
            // access
            if (fe == null)
                return;

            // save
            if (highlighted)
                this.lastHighlightedField = fe;

        }

        /// <summary>
        /// During renderig, the last highlighted field will be identified.
        /// This function will perform the rendering; presuming that the controls
        /// are already displayed by the implementation technology.
        /// </summary>
        public void ShowLastHighlights()
        {
            // any highlighted?
            if (this.lastHighlightedField == null)
                return;

            // execute
            // be a little careful
            try
            {
                this.context?.HighlightElement(this.lastHighlightedField, true);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

        }

        public void ClearHighlights()
        {
            try
            {
                if (this.lastHighlightedField == null)
                    return;
                HighligtStateElement(this.lastHighlightedField, highlighted: false);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }

        //
        // size management
        //

        public AnyUiThickness NormalOrCapa(AnyUiThickness normal, params object[] args)
        {
            // access
            if (args == null || !(context is AnyUiContextPlusDialogs cpd))
                return normal;

            // look at args
            for (int i = 0; (i + 1) < args.Length; i += 2)
            {
                // schema fulfilled?
                if (!(args[i] is AnyUiContextCapability capaEn) || !(args[i + 1] is AnyUiThickness ath))
                    continue;
                if (cpd.HasCapability(capaEn))
                    return ath;
            }

            // nope
            return normal;
        }

        //
        // Helpers for GUI building blocks
        //

        public static T RegisterSmallControl<T>(
                            T cntl,
                            AasxMenu superMenu,
                            AasxMenuItemBase menuItem,
                            Func<object, AasxMenuActionTicket, Task<AnyUiLambdaActionBase>> setValueWithTicketAsync = null,
                            AnyUiLambdaActionBase takeOverLambda = null,
                            object setValue = null)
        where T : AnyUiUIElement
        {
            // just go thru
            AnyUiUIElement.RegisterControl(cntl, 
                setValueAsync: async (o) => {
                    if (setValueWithTicketAsync != null)
                        return await setValueWithTicketAsync(o, new AasxMenuActionTicket());
                    return new AnyUiLambdaActionNone();
                }, 
                takeOverLambda);

            // add the ticketMenu items to the super menu
            // an re-route lambdas
            if (superMenu != null && menuItem != null && setValueWithTicketAsync != null)
            {
                menuItem.ActionAsync = async (name, item, ticket) =>
                {
                    if (ticket != null)
                        ticket.UiLambdaAction = await setValueWithTicketAsync(setValue == null ? ((int) 0) : setValue, ticket);
                };

                superMenu.Add(menuItem);
            }

            // give back
            return cntl;
        }

        /// <summary>
        /// Display the key and some buttons in a wrap panel
        /// </summary>
        public AnyUiWrapPanel AddKeyButtons(
            AnyUiStackPanel view, string key, IEnumerable<AnyUiControl> buttons,
            KeyLabelHandling keyLabel = KeyLabelHandling.Standard)
        {
            // access
            if (view == null || buttons == null)
                return null;

            if (keyLabel == KeyLabelHandling.Standard)
            {
                // Grid with two columns
                var g = AddSmallGrid(1, 2, new[] { "#", "*" });
                g.ColumnDefinitions[0].MinWidth = GetWidth(FirstColumnWidth.Standard);

                view.Add(g);

                AddSmallLabelTo(g, 0, 0, content: key, padding: new AnyUiThickness(5, 0, 0, 0), verticalCenter: true);

                // make a panel for the buttons
                var panel = AddSmallWrapPanelTo(g, 0, 1, margin: new AnyUiThickness(5, 0, 0, 0));
                foreach (var b in buttons)
                    panel.Add(b);
                return panel;
            }
            else if (keyLabel == KeyLabelHandling.Above)
            {
                throw new NotImplementedException("AddKeyButtons::KeyLabelHandling.Above");
            }
            else
            {
                var panel = new AnyUiWrapPanel() { Orientation = AnyUiOrientation.Horizontal };
                foreach (var b in buttons)
                    panel.Add(b);
                view.Add(panel);
                return panel;
            }

            return null;
        }

        public void AddInfoText(
            AnyUiStackPanel view, string text)
        {
            // Grid
            var g = AddSmallGrid(1, 1, new[] { "*" });

            // Label for key
            var klb = AddSmallLabelTo(g, 0, 0, 
                padding: new AnyUiThickness(5, 0, 0, 0), 
                content: "" + text,
                wrapping: AnyUiTextWrapping.WrapWithOverflow);

            // in total
            view.Children.Add(g);
        }

#if ONLY_INFO
                // This was used in former times and now replaced by using a set lambda in all times

                public void AddKeyValueRef(
                    AnyUiStackPanel view, string key, object containingObject, ref string value, string nullValue = null,
                    ModifyRepo repo = null, Func<object, AnyUiLambdaActionBase> setValue = null,
                    string[] comboBoxItems = null, bool comboBoxIsEditable = false,
                    string auxButtonTitle = null, Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
                    string auxButtonToolTip = null,
                    string[] auxButtonTitles = null,
                    string[] auxButtonToolTips = null,
                    AnyUiLambdaActionBase takeOverLambdaAction = null,
                    bool limitToOneRowForNoEdit = false)
                {
                    AddKeyValue(
                        view, key, value, nullValue, repo, setValue, comboBoxItems, comboBoxIsEditable,
                        auxButtonTitle, auxButtonLambda, auxButtonToolTip,
                        auxButtonTitles, auxButtonToolTips, takeOverLambdaAction,
                        (value == null) ? 0 : value.GetHashCode(), containingObject: containingObject,
                        limitToOneRowForNoEdit: limitToOneRowForNoEdit);
                }
#endif

        /// <summary>
        /// This is a plain wrapper for <c>AddKeyValue</c> and <c>AddKeyValueRef</c>.
        /// Background is that in former times a reference was required; however, this is now
        /// for years done with the <c>setValue</c> lambda.
        /// Both this function and <c>AddKeyValue</c> are functionally equivalent.
        /// </summary>
        /// <param name="view">The <c>AnyUiView</c> the widget shall be added to</param>
        /// <param name="key">Label to be displayed in fron of editing field</param>
        /// <param name="containingObject">Contiaing object (for find/replace function)</param>
        /// <param name="value">Stringified value of the variable</param>
        /// <param name="nullValue">String if the value happens to be null</param>
        /// <param name="repo">Repository link. Used to mark the edit mode.</param>
        /// <param name="setValue">Lambda activiated, if variable is changed</param>
        /// <param name="comboBoxItems">If <c>null</c> displays a combo box</param>
        /// <param name="comboBoxIsEditable">True, if combobox choices can also be editied</param>
        /// <param name="auxButtonTitle">Legacy. If there is a single auxiliary button, name of the button</param>
        /// <param name="auxButtonLambda">Legacy. Lambda for that single button</param>
        /// <param name="auxButtonToolTip">Legacy. Tooltip for that single button.</param>
        /// <param name="auxButtonTitles">Array of button titles to be offered.</param>
        /// <param name="auxButtonToolTips">Array of tool tips for that buttons.</param>
        /// <param name="takeOverLambdaAction">Lambda called at the end of a modification.</param>
        /// <param name="limitToOneRowForNoEdit">Limitation for displaying multiple lines of value</param>
        [Obsolete("use variant with AnyUiButtonHeaderList")]
        public void AddKeyValueExRef(
            AnyUiStackPanel view, string key, object containingObject, string value, string nullValue = null,
            ModifyRepo repo = null, 
            // Func<object, AnyUiLambdaActionBase> setValue = null, 
            Func<object, Task<AnyUiLambdaActionBase>> setValueAsync = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            string auxButtonTitle = null, 
            Func<int, Task<AnyUiLambdaActionBase>> auxButtonLambdaAsync = null,
            string auxButtonToolTip = null,
            string[] auxButtonTitles = null,
            string[] auxButtonToolTips = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            bool limitToOneRowForNoEdit = false,
            int comboBoxMinWidth = -1,
            FirstColumnWidth? firstColumnWidth = null,
            int maxLines = -1,
			bool keyVertCenter = false,
            bool auxButtonOverride = false,
            bool isValueReadOnly = false,
            AnyUiButtonOverStyle buttonOverStyle = null)
        {
            var auxButtons = new AnyUiButtonHeaderList(
                                    headers: auxButtonTitles,
                                    toolTips: auxButtonToolTips);

            if (auxButtonTitle?.HasContent() == true)
                auxButtons.Insert(0, new AnyUiButtonHeader(
                    text: auxButtonTitle,
                    toolTip: auxButtonToolTip));

            AddKeyValue(
                view, key, value, nullValue, repo, setValueAsync, comboBoxItems, comboBoxIsEditable,
                auxButtons, 
                auxButtonLambdaAsync, 
                takeOverLambdaAction,
                (value == null) ? 0 : value.GetHashCode(), containingObject: containingObject,
                limitToOneRowForNoEdit: limitToOneRowForNoEdit,
                comboBoxMinWidth: comboBoxMinWidth,
                firstColumnWidth: firstColumnWidth,
                maxLines: maxLines,
                keyVertCenter: keyVertCenter,
                isValueReadOnly: isValueReadOnly,
                buttonOverStyle: buttonOverStyle);
        }

        /// <summary>
        /// This is a plain wrapper for <c>AddKeyValue</c> and <c>AddKeyValueRef</c>.
        /// Background is that in former times a reference was required; however, this is now
        /// for years done with the <c>setValue</c> lambda.
        /// Both this function and <c>AddKeyValue</c> are functionally equivalent.
        /// </summary>
        /// <param name="view">The <c>AnyUiView</c> the widget shall be added to</param>
        /// <param name="key">Label to be displayed in fron of editing field</param>
        /// <param name="containingObject">Contiaing object (for find/replace function)</param>
        /// <param name="value">Stringified value of the variable</param>
        /// <param name="nullValue">String if the value happens to be null</param>
        /// <param name="repo">Repository link. Used to mark the edit mode.</param>
        /// <param name="setValue">Lambda activiated, if variable is changed</param>
        /// <param name="comboBoxItems">If <c>null</c> displays a combo box</param>
        /// <param name="comboBoxIsEditable">True, if combobox choices can also be editied</param>
        /// <param name="auxButtons">Definition of a number of buttons</param>
        /// <param name="takeOverLambdaAction">Lambda called at the end of a modification.</param>
        /// <param name="limitToOneRowForNoEdit">Limitation for displaying multiple lines of value</param>
        /// <param name="auxButtonOverride">Show buttons, even if <c>repo == null</c></param>
        public void AddKeyValueExRefNew(
            AnyUiStackPanel view, string key, object containingObject, string value, string nullValue = null,
            ModifyRepo repo = null,
            // Func<object, AnyUiLambdaActionBase> setValue = null, 
            Func<object, Task<AnyUiLambdaActionBase>> setValueAsync = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            Func<int, Task<AnyUiLambdaActionBase>> auxButtonLambdaAsync = null,
            AnyUiButtonHeaderList auxButtons = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            bool limitToOneRowForNoEdit = false,
            int comboBoxMinWidth = -1,
            FirstColumnWidth? firstColumnWidth = null,
            int maxLines = -1,
            bool keyVertCenter = false,
            bool auxButtonOverride = false,
            bool isValueReadOnly = false,
            AnyUiButtonOverStyle buttonOverStyle = null)
        {
            AddKeyValue(
                view, key, value, nullValue, repo, setValueAsync, comboBoxItems, comboBoxIsEditable,
                auxButtons, 
                auxButtonLambdaAsync, 
                takeOverLambdaAction,
                (value == null) ? 0 : value.GetHashCode(), containingObject: containingObject,
                limitToOneRowForNoEdit: limitToOneRowForNoEdit,
                comboBoxMinWidth: comboBoxMinWidth,
                firstColumnWidth: firstColumnWidth,
                maxLines: maxLines,
                keyVertCenter: keyVertCenter,
                isValueReadOnly: isValueReadOnly,
                buttonOverStyle: buttonOverStyle,
                auxButtonOverride: auxButtonOverride);
        }

        /// <summary>
        /// Allow editing a plain content variable. The variable content is fed into by <c>value</c>.
        /// If the variable is changed, the lambda <c>setValue</c> is activated.
        /// </summary>
        /// <param name="view">The <c>AnyUiView</c> the widget shall be added to</param>
        /// <param name="key">Label to be displayed in fron of editing field</param>
        /// <param name="value">Stringified value of the variable</param>
        /// <param name="nullValue">String if the value happens to be null</param>
        /// <param name="repo">Repository link. Used to mark the edit mode.</param>
        /// <param name="setValue">Lambda activiated, if variable is changed</param>
        /// <param name="comboBoxItems">If <c>null</c> displays a combo box</param>
        /// <param name="comboBoxIsEditable">True, if combobox choices can also be editied</param>
        /// <param name="auxButtons">Definition of a number of buttons</param>
        /// <param name="auxButtonLambda">Legacy. Lambda for that single button</param>
        /// <param name="takeOverLambdaAction">Lambda called at the end of a modification.</param>
        /// <param name="valueHash">Hash value of the variable (for find/replace function)</param>
        /// <param name="containingObject">Contiaing object (for find/replace function)</param>
        /// <param name="limitToOneRowForNoEdit">Limitation for displaying multiple lines of value</param>
        /// <param name="comboBoxMinWidth">Minimal width if value is edited by combo box</param>
        /// <param name="topContextMenu">Allow many (direct) buttons on top row or put them into context menu</param>
        public void AddKeyValue(
            AnyUiStackPanel view, string key, string value, string nullValue = null,
            ModifyRepo repo = null, 
            // Func<object, AnyUiLambdaActionBase> setValue = null,
            Func<object, Task<AnyUiLambdaActionBase>> setValueAsync = null,
            string[] comboBoxItems = null, bool comboBoxIsEditable = false,
            AnyUiButtonHeaderList auxButtons = null, 
            Func<int, Task<AnyUiLambdaActionBase>> auxButtonLambdaAsync = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Nullable<int> valueHash = null,
            object containingObject = null,
            bool limitToOneRowForNoEdit = false,
            int comboBoxMinWidth = -1,
            FirstColumnWidth? firstColumnWidth = null,
            int maxLines = -1,
            bool keyVertCenter = false,
            bool auxButtonOverride = false,
            bool isValueReadOnly = false,
            bool topContextMenu = true,
            AnyUiButtonOverStyle buttonOverStyle = null)
        {
            // draw anyway?
            if (repo != null && value == null)
            {
                // generate default value
                value = "";
            }
            else
            {
                // normal handling
                if (value == null && nullValue == null)
                    return;
                if (value == null)
                    value = nullValue;
            }

            // aux buttons
            auxButtons = auxButtons ?? new();
            var auxButton = auxButtonOverride
                || (repo != null && auxButtons.Count > 0 && auxButtonLambdaAsync != null);

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 1, 0, 1);
            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = AnyUiGridLength.Auto;
            g.ColumnDefinitions.Add(gc1);
            var gc2 = new AnyUiColumnDefinition();
            gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc2);
            // 2024-05-09: add a minimum width to these kinds of fields
            gc2.MinWidth = valueFieldsMinWidth;

            if (firstColumnWidth != null)
                MaintainFirstColumnWidth(g, firstColumnWidth);
            else
                g.ColumnDefinitions[0].MinWidth = GetWidth(FirstColumnWidth.Standard);

            if (auxButton)
                for (int i = 0; i < auxButtons.Count; i++)
                {
                    var gc3 = new AnyUiColumnDefinition();
                    gc3.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                    g.ColumnDefinitions.Add(gc3);
                }

            // Label for key
            if (firstColumnWidth != FirstColumnWidth.No)
            {
                var klb = AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(5, 0, 0, 0), content: "" + key + ":");
                if (keyVertCenter)
                {
                    klb.VerticalAlignment = AnyUiVerticalAlignment.Center;
                    klb.VerticalContentAlignment = AnyUiVerticalAlignment.Center;
                    klb.Margin = new AnyUiThickness(0, -1, 0, 0);
                }
            }

            // Label / TextBox for value
            if (repo == null)
            {
                if (limitToOneRowForNoEdit)
                    value = AdminShellUtil.RemoveNewLinesAndLimit("" + value, 120, ellipsis: "\u2026");
                AddSmallLabelTo(g, 0, 1, padding: new AnyUiThickness(4, 0, 0, 0), content: "" + value,
                    verticalCenter: true);
            }
            else if (comboBoxItems != null)
            {
                // guess some max width, in order
                var maxc = 5;
                foreach (var c in comboBoxItems)
                    if (c.Length > maxc)
                        maxc = c.Length;
                var maxWidth = 12 * maxc; // about one em
                if (comboBoxMinWidth > maxWidth)
                    maxWidth = comboBoxMinWidth;

                // use combo box
                var cb = AddSmallComboBoxTo(
                    g, 0, 1,
                    margin: NormalOrCapa(
                        new AnyUiThickness(4, 2, 2, 2),
                        AnyUiContextCapability.Blazor, new AnyUiThickness(4, 0, 2, 0)),
                    padding: NormalOrCapa(
                        new AnyUiThickness(2, 0, 2, 0),
                        AnyUiContextCapability.Blazor, new AnyUiThickness(2, 3, 2, 3)),
                    text: "" + value,
                    minWidth: Math.Max(60, comboBoxMinWidth),
                    maxWidth: maxWidth,
                    items: comboBoxItems,
                    isEditable: comboBoxIsEditable);
                AnyUiUIElement.RegisterControl(cb, setValueAsync: setValueAsync, takeOverLambda: takeOverLambdaAction);

                // check here, if to hightlight
                if (cb != null && this.highlightField != null && valueHash != null &&
                        this.highlightField.fieldHash == valueHash.Value &&
                        (containingObject == null || containingObject == this.highlightField.containingObject))
                    this.HighligtStateElement(cb, true);
            }
            else
            {
                // use plain text box
                var tb = AddSmallTextBoxTo(g, 0, 1, margin: new AnyUiThickness(4, 2, 2, 2), text: "" + value, isValReadOnly: isValueReadOnly);
                // multiple lines
                if (maxLines > 0)
                    tb.MaxLines = maxLines;
                // events
                AnyUiUIElement.RegisterControl(tb,
                    setValueAsync: setValueAsync, takeOverLambda: takeOverLambdaAction);

                // check here, if to hightlight
                if (tb != null && this.highlightField != null && valueHash != null &&
                        this.highlightField.fieldHash == valueHash.Value &&
                        (containingObject == null || containingObject == this.highlightField.containingObject))
                    this.HighligtStateElement(tb, true);
            }

            if (auxButton)
            {
                if (topContextMenu)
                {
                    for (int i = 0; i < auxButtons.Count; i++)
                    {
                        Func<object, Task<AnyUiLambdaActionBase>> lmbAsync = null;
                        int closureI = i;

                        if (auxButtonLambdaAsync != null)
                            lmbAsync = async (o) =>
                            {
                                return await auxButtonLambdaAsync(closureI); // exchange o with i !!
                            };

                        var b = AnyUiUIElement.RegisterControl(
                            AddSmallButtonTo(
                                g, 0, 2 + i,
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                buttonOverStyle: buttonOverStyle,
                                header: auxButtons[i]),
                                setValueAsync: lmbAsync) as AnyUiButton;
                    }
                }
                else
                {
                    var menuHeaders = new AnyUiContextMenuHeaderList();
                    int i = 0;
                    foreach (var ab in auxButtons)
                        menuHeaders.Add(new AnyUiContextMenuHeaderIconSource(
                            i++, ab.ImageSource as AnyUiImageSourceFont, ab.Text));

                    AddSmallContextMenuItemTo(
                                g, 0, 2,
                                header: new AnyUiButtonHeader(IconPool.MoreVert, "More",
                                        "More options in context menu."),
                                buttonOverStyle: buttonOverStyle.Modify(preference: AnyUiButtonPreference.Image),
                                menuHeaders: menuHeaders,
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                horizontalAlignment: AnyUiHorizontalAlignment.Center,
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                menuItemLambdaAsync: async (o) =>
                                {
                                    return await auxButtonLambdaAsync((o is int ii) ? ii : 0);
                                });
                }
            }

            // in total
            view.Children.Add(g);
        }

        public void AddKeyDropTarget(
            AnyUiStackPanel view, string key, string value, string nullValue = null,
            ModifyRepo repo = null, 
            Func<object, Task<AnyUiLambdaActionBase>> setValueAsync = null, 
            int minHeight = 0)
        {
            // draw anyway?
            if (repo != null && value == null)
            {
                // generate default value
                value = "";
            }
            else
            {
                // normal handling
                if (value == null && nullValue == null)
                    return;
                if (value == null)
                    value = nullValue;
            }

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 1, 0, 1);
            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = new AnyUiGridLength(
                this.GetWidth(FirstColumnWidth.Standard), AnyUiGridUnitType.Pixel);
            g.ColumnDefinitions.Add(gc1);
            var gc2 = new AnyUiColumnDefinition();
            gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc2);

            // Label for key
            AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(5, 0, 0, 0), content: "" + key + ":");

            // Label / TextBox for value
            if (repo == null)
            {
                // view only
                AddSmallLabelTo(g, 0, 1, padding: new AnyUiThickness(2, 0, 0, 0), content: "" + value);
            }
            else
            {
                // interactive
                var brd = AddSmallDropBoxTo(g, 0, 1, margin: new AnyUiThickness(4, 2, 2, 2),
                    borderThickness: new AnyUiThickness(1), text: "" + value, minHeight: minHeight);
                AnyUiUIElement.RegisterControl(brd,
                    setValueAsync);
            }

            // in total
            view.Children.Add(g);
        }

        public void AddKeyMultiValue(AnyUiStackPanel view, string key, string[][] value, string[] widths)
        {
            // draw anyway?
            if (value == null)
                return;

            // get some dimensions
            var rows = value.Length;
            var cols = 1;
            foreach (var r in value)
                if (r.Length > cols)
                    cols = r.Length;

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 0, 0, 0);

            var gc1 = new AnyUiColumnDefinition();
            gc1.Width = AnyUiGridLength.Auto;
            gc1.MinWidth = this.GetWidth(FirstColumnWidth.Standard);
            g.ColumnDefinitions.Add(gc1);

            for (int c = 0; c < cols; c++)
            {
                var gc2 = new AnyUiColumnDefinition();
                if (widths[c] == "*")
                    gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
                else
                if (widths[c] == "#")
                    gc2.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                else
                {
                    if (Int32.TryParse(widths[c], out int i))
                        gc2.Width = new AnyUiGridLength(i);
                }
                g.ColumnDefinitions.Add(gc2);
            }

            for (int r = 0; r < rows; r++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // Label for key
            var l1 = new AnyUiLabel();
            l1.Margin = new AnyUiThickness(0, 0, 0, 0);
            l1.Padding = new AnyUiThickness(5, 0, 0, 0);
            l1.Content = "" + key + ":";
            AnyUiGrid.SetRow(l1, 0);
            AnyUiGrid.SetColumn(l1, 0);
            g.Children.Add(l1);

            // Label for any values
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var l2 = new AnyUiLabel();
                    l2.Margin = new AnyUiThickness(0, 0, 0, 0);
                    l2.Padding = new AnyUiThickness(2, 0, 0, 0);
                    l2.Content = "" + value[r][c];
                    AnyUiGrid.SetRow(l2, 0 + r);
                    AnyUiGrid.SetColumn(l2, 1 + c);
                    g.Children.Add(l2);
                }

            // in total
            view.Children.Add(g);
        }

        public void AddSmallCheckBox(
            AnyUiStackPanel panel, string key, 
            bool value,             
			Func<bool, Task<AnyUiLambdaActionBase>> setValueAsync = null,
			string additionalInfo = "",
			string[] boolTexts = null,
            bool verticalCenter = false)
        {
            // make grid
            var g = this.AddSmallGrid(1, 2, new[] { "" + this.GetWidth(FirstColumnWidth.Standard) + ":", "*" },
                    margin: new AnyUiThickness(0, 2, 0, 0));

            // Column 0 = Key
            this.AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(5, 0, 0, 0), content: key, verticalCenter: verticalCenter);

            // Column 1 = Check box or info
            if (repo == null || setValueAsync == null)
            {
				// label
				var strVal = (value) ? "True" : "False";
				if (boolTexts != null && boolTexts.Length >= 2)
					strVal = (value) ? boolTexts[1] : boolTexts[0];

				this.AddSmallLabelTo(g, 0, 1, padding: new AnyUiThickness(2, 0, 0, 0),
                        content: strVal, verticalCenter: verticalCenter);
            }
            else
            {
                AnyUiUIElement.RegisterControl(this.AddSmallCheckBoxTo(g, 0, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    content: additionalInfo, verticalCenter: verticalCenter,
                    isChecked: value),
                        setValueAsync: async (o) =>
                        {
                            if (o is bool && setValueAsync != null)
                                return await setValueAsync((bool)o);
                            return new AnyUiLambdaActionNone();
                        });
            }

            // add
            panel.Children.Add(g);
        }

        /// <summary>
        /// Generates a button with a context menu attached, which can also
        /// be commended by tickets.
        /// </summary>
        public IEnumerable<AnyUiButton> GenerateActionButton(
            AnyUiButtonHeader buttonHeader,
            ModifyRepo repo = null,
            bool[] addWoEdit = null,
            AasxMenu superMenu = null,
            AasxMenu ticketMenu = null,
            Func<int, AasxMenuActionTicket, Task<AnyUiLambdaActionBase>> ticketActionAsync = null,
            AnyUiButtonOverStyle buttonOverStyle = null,
            AnyUiThickness padding = null)
        {
            // result
            var res = new List<AnyUiButton>();

            // add the ticketMenu items to the super menu
            // an re-route lambdas
            if (superMenu != null && ticketMenu != null && ticketActionAsync != null)
            {
                for (int i = 0; i < ticketMenu.Count; i++)
                {
                    // get
                    var tmi = ticketMenu[i];
                    var currentI = i;

                    // check if allowed
                    if (repo == null && addWoEdit != null && i < addWoEdit.Length && !addWoEdit[i])
                        continue;

                    // may be async
                    if (ticketActionAsync != null)
                    {
                        tmi.ActionAsync = async (name, item, ticket) =>
                        {
                            if (ticket != null)
                                ticket.UiLambdaAction = await ticketActionAsync(currentI, ticket);
                        };
                    }

                    superMenu.Add(tmi);
                }
            }

            // construct menu headers
            var menuHeaders = new AnyUiContextMenuHeaderList();
            if (ticketMenu != null && ticketActionAsync != null)
                for (int i = 0; i < ticketMenu.Count; i++)
                {
                    // get
                    var tmi = ticketMenu[i];
                    var currentI = i;

                    // check if allowed
                    if (repo == null && addWoEdit != null && i < addWoEdit.Length && !addWoEdit[i])
                        continue;

                    // add
                    if (tmi is AasxMenuItem mi)
                    {
                        var cmh = new AnyUiContextMenuHeaderIconSource(
                                currentI, icon: mi.Icon as AnyUiImageSourceFont, header: mi.Header);
                        menuHeaders.Add(cmh);
                    }
                }

            // no context menu -> show no button!
            if (menuHeaders.Count < 1)
                return res;

            // make a context menu
            var but = AddSmallContextMenuItem(
                menuHeaders: menuHeaders,
                buttonHeader: buttonHeader,
                buttonOverStyle: buttonOverStyle,
                padding: padding,
                menuItemLambdaAsync: async (o) =>
                {
                    if (ticketActionAsync != null)
                        return await ticketActionAsync.Invoke((o is int ii) ? (int) o : 0, new AasxMenuActionTicket());
                    return new AnyUiLambdaActionNone();
                });
            res.Add(but);

            // ok
            return res;
        }

        public void AddActionPanel(
            AnyUiPanel view, string key,
            string[] actionStrXX = null, 
            ModifyRepo repo = null,
            string[] actionTags = null,
            bool[] addWoEdit = null,
            AasxMenu superMenu = null,
            AasxMenu ticketMenu = null,
            Func<int, Task<AnyUiLambdaActionBase>> actionAsync = null,
			Func<int, AasxMenuActionTicket, Task<AnyUiLambdaActionBase>> ticketActionAsync = null,
			FirstColumnWidth firstColumnWidth = FirstColumnWidth.Standard,
            AnyUiButtonOverStyle buttonOverStyle = null,
            bool useWrapFlexPanel = true,
            KeyLabelHandling keyLabel = KeyLabelHandling.Standard)
        {
            // generate actionStr from ticketMenu
            //if (actionStr == null && ticketMenu != null)
            //    actionStr = ticketMenu.Select((tmi) => (tmi is AasxMenuItem mi) ? mi.Header : "").ToArray();

            var buttonList = ticketMenu?.Where((tmi) => tmi is AasxMenuItem)
                                        .Select((tmi) => (tmi as AasxMenuItem).ToButtonHeader())?.ToList();

            if (actionStrXX != null)
            {
                buttonList = buttonList ?? new();
                foreach (var a in actionStrXX)
                    buttonList.Add(new AnyUiButtonHeader(text: "§§ " + a));
            }

            // access 
            if ((actionAsync == null && ticketActionAsync == null) || buttonList == null)
                return;
            if (repo == null && addWoEdit == null)
                return;
            var numButton = buttonList.Count;

            // add the ticketMenu items to the super menu
            // an re-route lambdas
            if (superMenu != null && ticketMenu != null && ticketActionAsync != null)
            {
                for (int i = 0; i < ticketMenu.Count; i++)
                {
                    var tmi = ticketMenu[i];
                    var currentI = i;

                    // may be async
                    if (ticketActionAsync != null)
                    {
                        tmi.ActionAsync = async (name, item, ticket) =>
                        {
                            if (ticket != null)
                                ticket.UiLambdaAction = await ticketActionAsync(currentI, ticket);
                        };
                    }

					superMenu.Add(tmi);
                }
            }

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 5, 0, 5);

            // 0 key
            var gc = new AnyUiColumnDefinition();
            gc.Width = AnyUiGridLength.Auto;
            if (keyLabel == KeyLabelHandling.Standard)
                gc.MinWidth = GetWidth(firstColumnWidth);
            g.ColumnDefinitions.Add(gc);

            // 1+x button
            for (int i = 0; i < (useWrapFlexPanel ? 1 : numButton); i++)
            {
                gc = new AnyUiColumnDefinition();
                gc.Width = new AnyUiGridLength(1.0, useWrapFlexPanel ? AnyUiGridUnitType.Star : AnyUiGridUnitType.Auto);
                g.ColumnDefinitions.Add(gc);
            }

            // 0 row
            var gr = new AnyUiRowDefinition();
            gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.RowDefinitions.Add(gr);

            // key label
            if (keyLabel != KeyLabelHandling.No)
            {
                var x = AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0),
                    setNoWrap: true,
                    content: "" + key);
                x.VerticalAlignment = AnyUiVerticalAlignment.Center;
            }

            // 1 + action button
            var wp = !useWrapFlexPanel ? null : AddSmallWrapPanelTo(g, 0, 1, margin: new AnyUiThickness(4, 0, 4, 0));
            for (int i = 0; i < numButton; i++)
            {
                // render?
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (!((repo != null)
                      || (addWoEdit != null && addWoEdit.Length > i && addWoEdit[i])
                     ))
                    continue;

                // prepare button header data


                // render?
                int currentI = i;
                AnyUiButton but = null;
                if (!useWrapFlexPanel)
                {
                    but = AddSmallButtonTo(
                        g, 0, 1 + i,
                        header: buttonList[i],
                        // content: "" + actionStr[i],
                        margin: new AnyUiThickness(0, 2, 4, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        buttonOverStyle: buttonOverStyle);
                }
                else
                {
                    // flex panel
                    but = new AnyUiButton();
                    but.Margin = new AnyUiThickness(0, 2, 4, 2);
                    but.Padding = new AnyUiThickness(5, 0, 5, 0);
                    but.ApplyHeader(buttonList[i], buttonOverStyle);
                    if (buttonOverStyle?.Style != null)
                        but.ApplyAsStyle(buttonOverStyle.Style);
                    wp.Children.Add(but);
                }

                if (but == null)
                    continue;

                // register callback
                if (actionAsync != null || ticketActionAsync != null)
				    AnyUiUIElement.RegisterControl(but,
                        setValueAsync: async (o) =>
                        {
						    // button # as argument!
						    if (ticketActionAsync != null)
							    return await ticketActionAsync.Invoke(currentI, null);
						    else
							    return await actionAsync?.Invoke(currentI);
					    });

                // can set a tool tip?
                if (ticketMenu != null && ticketMenu.Count > i
                    && ticketMenu[i] is AasxMenuItem mii
                    && mii.HelpText?.HasContent() == true)
                    but.ToolTip = mii.HelpText;
            }

            // in total
            view.Children.Add(g);
        }

        public void AddAction(
            AnyUiStackPanel view, string key, string actionStr, ModifyRepo repo = null,
            Func<int, Task<AnyUiLambdaActionBase>> actionAsync = null,
            FirstColumnWidth firstColumnWidth = FirstColumnWidth.Standard)
        {
            AddActionPanel(view, key, new[] { actionStr }, repo, actionAsync: actionAsync, firstColumnWidth: firstColumnWidth);
        }

        public void AddKeyListLangStr<T>(
            AnyUiStackPanel view, string key, List<T> langStr, ModifyRepo repo = null,
            Aas.IReferable relatedReferable = null,
            Action setNullList = null,
			Func<Aas.IReferable, AnyUiLambdaActionBase> emitCustomEvent = null,
            AnyUiButtonOverStyle buttonOverStyleLo = null,
            AnyUiButtonOverStyle buttonOverStyleHi = null,
            AnyUiButtonPreference buttonPreferenceLo = AnyUiButtonPreference.None,
            AnyUiButtonPreference buttonPreferenceHi = AnyUiButtonPreference.None) where T : IAbstractLangString
        {
            // sometimes needless to show
            if (repo == null && (langStr == null || langStr.Count < 1))
                return;
            int rows = 1; // default!
            if (langStr != null && langStr.Count > 1)
                rows = langStr.Count;
            int rowOfs = 0;
            int rowsAdd = 0;
            if (repo != null && LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Top)
            {
                rowOfs = 1; rowsAdd = 1;
            }
            if (repo != null && LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Bottom)
            {
                rowsAdd = 1;
            }

            // default
            if (emitCustomEvent == null)
				emitCustomEvent = (rf) => { 
                    this.AddDiaryEntry(rf, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                };

            // Grid
            var g = new AnyUiGrid();
            g.Margin = new AnyUiThickness(0, 0, 0, 0);

            // 0 key
            var gc = new AnyUiColumnDefinition();
            gc.Width = AnyUiGridLength.Auto;
            gc.MinWidth = GetWidth(FirstColumnWidth.Standard);
            g.ColumnDefinitions.Add(gc);

            // 1 langs
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
            g.ColumnDefinitions.Add(gc);

            // 2 values
            gc = new AnyUiColumnDefinition();
            gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);
            g.ColumnDefinitions.Add(gc);

            // 3++ buttons behind it
            for (int i = 0; i < 2; i++)
            {
                gc = new AnyUiColumnDefinition();
                gc.Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.ColumnDefinitions.Add(gc);
            }

            // rows
            for (int r = 0; r < rows + rowsAdd; r++)
            {
                var gr = new AnyUiRowDefinition();
                gr.Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto);
                g.RowDefinitions.Add(gr);
            }

            // populate key
            AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0),
                setNoWrap: true,
                verticalCenter: true,
                content: "" + key + ":");

            // contents?
            if (!langStr.IsNullOrEmpty())
                for (int i = 0; i < langStr.Count; i++)
                    if (repo == null)
                    {
                        // lang
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 1,
                            margin: new AnyUiThickness(4, 0, 0, 0),
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            setNoWrap: true,
                            content: "[" + langStr[i].Language + "]");

                        // str
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 2,
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            content: "" + langStr[i].Text);
                    }
                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // lang
                        var tbLang = Set(
                                AddSmallComboBoxTo(
                                    g, 0 + i + rowOfs, 1,
                                    margin: NormalOrCapa(
                                        new AnyUiThickness(4, 2, 2, 2),
                                        AnyUiContextCapability.Blazor, new AnyUiThickness(4, 2, 2, 0)),
                                    padding: NormalOrCapa(
                                        new AnyUiThickness(0, 0, 0, 0),
                                        AnyUiContextCapability.Blazor, new AnyUiThickness(0, 4, 0, 4)),
                                    text: "" + langStr[currentI].Language,
                                    minWidth: 60,
                                    items: AasxLanguageHelper.Languages.GetAllLanguages(nullForAny: true).ToArray(),
                                    isEditable: true),
                                verticalAlignment: AnyUiVerticalAlignment.Top,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center
                            );
                        AnyUiUIElement.RegisterControl(
                            tbLang,
                            async (o) =>
                            {
                                await Task.Yield();
                                langStr[currentI].Language = o as string;
								var evt = emitCustomEvent?.Invoke(relatedReferable);
								if (evt != null && !(evt is AnyUiLambdaActionNone))
									return evt;
								return new AnyUiLambdaActionNone();
                            });
                        // check here, if to hightlight
                        if (tbLang != null && this.highlightField != null &&
                                this.highlightField.fieldHash == langStr[currentI].Language.GetHashCode() &&
                                //(this.highlightField.containingObject == langStr[currentI]))
                                //TODO (jtikekar, 0000-00-00): need to test
                                CompareUtils.Compare<IAbstractLangString>((IAbstractLangString)this.highlightField.containingObject, langStr[currentI]))
                            this.HighligtStateElement(tbLang, true);

                        // str
                        var tbStr = AddSmallTextBoxTo(
                            g, 0 + i + rowOfs, 2,
                            margin: NormalOrCapa(
                                new AnyUiThickness(2, 2, 2, 2),
                                AnyUiContextCapability.Blazor, new AnyUiThickness(6, 2, 2, 2)),
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center,
                            text: "" + langStr[currentI].Text);
                        AnyUiUIElement.RegisterControl(
                            tbStr,
                            async (o) =>
                            {
                                await Task.Yield();
                                langStr[currentI].Text = o as string;
								var evt = emitCustomEvent?.Invoke(relatedReferable);
								if (evt != null && !(evt is AnyUiLambdaActionNone))
									return evt;
								return new AnyUiLambdaActionNone();
                            });
                        // check here, if to hightlight
                        if (tbStr != null && this.highlightField != null &&
                                this.highlightField.fieldHash == langStr[currentI].Text.GetHashCode() &&
                                (this.highlightField.containingObject == (object) langStr[currentI]))
                                //TODO (jtikekar, 0000-00-00): need to test
                                // CompareUtils.Compare<IAbstractLangString>((IAbstractLangString)this.highlightField.containingObject, langStr[currentI]))
                            this.HighligtStateElement(tbStr, true);

                        // button [≡]
                        if (LayoutHints.ExplicitMultiLineEdit)
                        {
                            AnyUiUIElement.RegisterControl(
                                AddSmallButtonTo(
                                    g, 0 + i + rowOfs, 3,
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    padding: new AnyUiThickness(5, 0, 5, 0),
                                    verticalAlignment: AnyUiVerticalAlignment.Top,
                                    header: new AnyUiButtonHeader(IconPool.MultiLineEdit, "Multi line",
                                        "Edit value with multi-line text editor."),
                                    buttonOverStyle: buttonOverStyleLo),
                                setValueAsync: async (o) =>
                                {
                                    var uc = new AnyUiDialogueDataTextEditor(
                                        caption: $"Edit Text @ {langStr[currentI].Language} ...",
                                        mimeType: "text/markdown",
                                        text: langStr[currentI].Text);

                                    if (await this.context.StartFlyoverModalAsync(uc))
                                    {
                                        langStr[currentI].Text = uc.Text;
                                        emitCustomEvent?.Invoke(relatedReferable);
                                        return new AnyUiLambdaActionRedrawEntity();
                                    }
                                    return new AnyUiLambdaActionNone();
                                });
                        }

                        // button [⋮]

                        Set(
                            AddSmallContextMenuItemTo(
                                    g, 0 + i + rowOfs, 4,
                                    header: new AnyUiButtonHeader(IconPool.MoreVert, "More",
                                        "More options in context menu."),
                                    menuHeaders: new AnyUiContextMenuHeaderList(new[] {
                                        new AnyUiContextMenuHeaderIconSource(0, IconPool.Delete, "Delete"),
                                        new AnyUiContextMenuHeaderIconSource(1, IconPool.MoveUp, "Move Up"),
                                        new AnyUiContextMenuHeaderIconSource(1, IconPool.MoveDown, "Move Down"),
                                    })
                                    .InsertBeforeIf(LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Context, 
                                        new AnyUiContextMenuHeaderIconSource(100, IconPool.AddBlank, "Add blank"))
                                    .AddIf(!LayoutHints.ExplicitMultiLineEdit,
                                        new AnyUiContextMenuHeaderIconSource(101, IconPool.MultiLineEdit, "Edit multiline")),
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    padding: new AnyUiThickness(5, 0, 5, 0),
                                    buttonOverStyle: LayoutHints.StyleButtonStandard.Modify(preference: buttonPreferenceLo),
                                    menuItemLambdaAsync: async (o) =>
                                    {
                                        await Task.Yield();
                                        var action = false;

                                        if (o is int ti)
                                            switch (ti)
                                            {
                                                case 0:
                                                    langStr.RemoveAt(currentI);
                                                    if (langStr.Count < 1)
                                                        setNullList?.Invoke();
                                                    action = true;
                                                    break;
                                                case 1:
                                                    MoveElementInListUpwards<T>(langStr, langStr[currentI]);
                                                    action = true;
                                                    break;
                                                case 2:
                                                    MoveElementInListDownwards<T>(langStr, langStr[currentI]);
                                                    action = true;
                                                    break;

                                                case 100:
                                                    langStr.Add<T>(language: AdminShellUtil.GetDefaultLngIso639(), text: "");
                                                    action = true;
                                                    break;

                                                case 101:
                                                    {
                                                        var uc = new AnyUiDialogueDataTextEditor(
                                                                        caption: $"Edit Text @ {langStr[currentI].Language} ...",
                                                                        mimeType: "text/markdown",
                                                                        text: langStr[currentI].Text);

                                                        if (await this.context.StartFlyoverModalAsync(uc))
                                                        {
                                                            langStr[currentI].Text = uc.Text;
                                                            emitCustomEvent?.Invoke(relatedReferable);
                                                            return new AnyUiLambdaActionRedrawEntity();
                                                        }
                                                    }
                                                    action = true;
                                                    break;
                                            }

                                        emitCustomEvent?.Invoke(relatedReferable);

                                        if (action)
                                            return new AnyUiLambdaActionRedrawEntity();
                                        return new AnyUiLambdaActionNone();
                                    }),
                            verticalContentAlignment: AnyUiVerticalAlignment.Center,
                            verticalAlignment: AnyUiVerticalAlignment.Top
                        );
                    }

            // populate top row with: [+]
            // .. or on bottom?
            if (repo != null && (LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Top
                                 || LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Bottom))
            {
                AnyUiUIElement.RegisterControl(
                    Set(
                        AddSmallButtonTo(
                            g, 
                            row: (LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Bottom) ? langStr.Count : 0, 
                            col: (LayoutHints.PlacementAdd == UILayoutHints.PosOfControl.Bottom) ? 1 : 3,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.Add, "Add", "Add language string", preference: buttonPreferenceHi),
                            buttonOverStyle: buttonOverStyleHi),
                        verticalCenter: true,
                        colSpan: 1),
                        async (o) =>
                        {
                            await Task.Yield();
                            langStr.Add<T>(
                                language: AasxLanguageHelper.GetFirstLangCodeNotTused<T>(Options.Curr.DefaultLangs, langStr), 
                                text: Options.Curr.DefaultEmptyLangText);
                            emitCustomEvent?.Invoke(relatedReferable);
                            return new AnyUiLambdaActionRedrawEntity();
                        });
            }

            // in total
            view.Children.Add(g);
        }

        public async Task<List<Aas.IKey>> SmartSelectAasEntityKeysAsync(
            PackageCentral.PackageCentral packages,
            PackageCentral.PackageCentral.Selector selector, string filter = null)
        {
            var uc = new AnyUiDialogueDataSelectAasEntity(
                caption: "Select entity of AAS ..",
                selector: selector, filter: filter);
            await context.StartFlyoverModalAsync(uc);
            if (uc.Result && uc.ResultKeys != null)
                return uc.ResultKeys;

            return null;
        }

        public async Task <VisualElementGeneric> SmartSelectAasEntityVisualElementAsync(
            PackageCentral.PackageCentral packages,
            PackageCentral.PackageCentral.Selector selector,
            string filter = null)
        {
            var uc = new AnyUiDialogueDataSelectAasEntity(
                caption: "Select entity of AAS ..",
                selector: selector, filter: filter);
            await this.context.StartFlyoverModalAsync(uc);
            if (uc.Result && uc.ResultVisualElement != null)
                return uc.ResultVisualElement;

            return null;
        }

        public bool SmartSelectEclassEntity(
            AnyUiDialogueDataSelectEclassEntity.SelectMode mode, ref string resIRDI,
            ref Aas.ConceptDescription resCD)
        {
            var res = false;

            // TODO (MIHO, 2020-12-21): function & if-clause is obsolete
            var uc = new AnyUiDialogueDataSelectEclassEntity("Select ECLASS entity ..",
                mode: mode);
            this.context.StartFlyoverModal(uc);
            resIRDI = uc.ResultIRDI;
            resCD = uc.ResultCD;
            res = resIRDI != null;

            return res;
        }

        /// <summary>
        /// Asks the user for SME element type, allowing exclusion of types.
        /// </summary>
        public async Task<Aas.AasSubmodelElements> SelectAdequateEnum(
            string caption, Aas.AasSubmodelElements[] excludeValues = null,
            Aas.AasSubmodelElements[] includeValues = null,
            AasxMenuActionTicket ticket = null)
        {
            // prepare a list
            var fol = new AnyUiDialogueListItemList();
            foreach (var en in AdminShellUtil.GetAdequateEnums(excludeValues, includeValues))
                fol.Add(new AnyUiDialogueListItem(Enum.GetName(typeof(Aas.AasSubmodelElements), en), en));

            // argument in ticket?
            var arg = ticket?["Kind"] as string;
            if (arg != null)
                foreach (var foli in fol)
                    if (foli.Text.Trim().ToLower() == arg.Trim().ToLower())
                        return (Aas.AasSubmodelElements)foli.Tag;

            // prompt for this list
            var uc = new AnyUiDialogueDataSelectFromList(
                caption: caption);
            uc.ListOfItems = fol;
            await this.context.StartFlyoverModalAsync(uc);
            if (uc.Result && uc.ResultItem != null && uc.ResultItem.Tag != null &&
                    uc.ResultItem.Tag is Aas.AasSubmodelElements)
            {
                // to which?
                var en = (Aas.AasSubmodelElements)uc.ResultItem.Tag;
                return en;
            }

            return Aas.AasSubmodelElements.SubmodelElement;
        }

        public async Task<bool> SmartRefactorSme_PostProcess(
            AdminShellPackageEnvBase packEnv,
            Aas.ISubmodelElement oldSme,
            Aas.ISubmodelElement newSme)
        {
            if (newSme is Aas.IBlob newBlob && oldSme is Aas.IFile oldFile)
            {
                // get file contents from a file
                var ba = await packEnv?.GetBytesFromPackageOrExternalAsync(oldFile.Value);
                if (ba == null || ba.Length < 1)
                    return false;

                // ask back
                if (AnyUiMessageBoxResult.Yes != await this.context.MessageBoxFlyoutShowAsync(
                    $"Local file contents with a len of {ba.Length} bytes found. " +
                    $"Convert these to BLOB contents? " +
                    $"Note: This will significantly increase the size of serialized Submodel " +
                    $"in total",
                    "Convert local file to BLOB contents?", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question))
                    return false;

                // work & tell
                newBlob.Value = ba;
                Log.Singleton.Info(StoredPrint.Color.Blue,
                    "When refactoring File to Blob, the following file was converted to BLOB contents of {0} bytes: {1}",
                    ba.Length, oldFile.Value);

                // ok!
                return true;
            }

            // nope
            return false;
        }

        /// <summary>
        /// Asks the user, to which SME to refactor to, create the new SME and returns it.
        /// </summary>
        public async Task<Aas.ISubmodelElement> SmartRefactorSme(
            AdminShellPackageEnvBase packEnv,
            Aas.ISubmodelElement oldSme)
        {
            // access
            if (oldSme == null)
                return null;

            // ask
            var en = await SelectAdequateEnum(
                $"Refactor {oldSme.GetSelfDescription().AasElementName} '{"" + oldSme.IdShort}' to new element type ..",
                excludeValues: new[] {
                    Aas.AasSubmodelElements.DataElement,
                    Aas.AasSubmodelElements.EventElement,
                });
            if (en == Aas.AasSubmodelElements.SubmodelElement)
                return null;

            if (AnyUiMessageBoxResult.Yes == await this.context.MessageBoxFlyoutShowAsync(
                "Recfactor selected entity? " +
                    "This operation will change the selected submodel element and " +
                    "delete specific attributes. It can not be reverted!",
                "AASX", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
            {
                try
                {
                    {
                        // which?
                        var refactorSme = AdminShellUtil.CreateSubmodelElementFromEnum(en, oldSme, 
                            defaultHelper: Options.Curr.GetCreateDefaultHelper());

                        // post work?
                        await SmartRefactorSme_PostProcess(packEnv, oldSme, refactorSme);

                        // ok
                        return refactorSme;
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Executing refactoring");
                }
            }

            return null;
        }

        public enum AddKeyListKeys_Button { 
            None = 0x0,
            Blank = 0x01,
            Existing = 0x02,
            Eclass = 0x04,
            Known = 0x08,
            KnownSemanticId = 0x10,
            Presets = 0x20,
        };

        /// <summary>
        /// Add a list of keys
        /// </summary>
        /// <param name="view"></param>
        /// <param name="key">Label to be displayed on left/ top of edit field. Note: No key valuw!!</param>
        /// <param name="keys">List of keys to edit</param>
        /// <param name="setKeysNull">Lambda to set the keys</param>
        /// <param name="repo"><c>Repo != null</c> means edit mode</param>
        /// <param name="packages">AASX packages to select from</param>
        /// <param name="selector">What to select from the packages</param>
        /// <param name="addButton">Which buttons to add for adding</param>
        /// <param name="highlightButton">Which buttons to highlight</param>
        /// <param name="addExistingEntities">Filter for existing entities from the packages</param>
        /// <param name="modifyAddExistingKey">Lambda to modify selected existing entity reference</param>
        /// <param name="addEclassIrdi">Select from ECLASS</param>
        /// <param name="addFromKnown">Select from known (system) references</param>
        /// <param name="addPresetNames">Names of presets for key lists, given</param>
        /// <param name="addPresetKeyLists">Values (key lists) of presets for key lists, given</param>
        /// <param name="jumpLambda">Lambda to activate for jumping</param>
        /// <param name="takeOverLambdaAction">Lambda actiated, when editing completed</param>
        /// <param name="noEditJumpLambda">Lambda to activate for jumping, when in no edit mode</param>
        /// <param name="relatedReferable">Referable the value is in</param>
        /// <param name="emitCustomEvent">Custom lambda, when vales were changed</param>
        /// <param name="frontPanel">Panel to hook in at top of values</param>
        /// <param name="footerPanel">Panel to hook in at bottom of values</param>
        /// <param name="topContextMenu">Allow many (direct) buttons on top row or put them into context menu</param>
        /// <param name="auxButtonLambda"></param>
        /// <param name="auxButtonTitles"></param>
        /// <param name="auxButtonToolTips"></param>
        /// <param name="auxContextHeader"></param>
        /// <param name="auxContextLambda"></param>
        /// <param name="maxNumOfKey">Maximum number of keys allow</param>
        /// <param name="addKnownSemanticId">For adding known system references, take the semanticId instead of id</param>
        /// <param name="firstColumnWidth">Width of column for the ´kwy label</param>
        /// <param name="buttonOverStyleLo">Override button style for lowly important buttons</param>
        /// <param name="buttonOverStyleHi">Override button style for highly important buttons</param>
        /// <param name="buttonPreferenceLo">Preference to show icons/ texts for lowly important buttons</param>
        public void AddKeyListKeys(
            AnyUiStackPanel view, string key,
            List<Aas.IKey> keys,
            Action setKeysNull = null,
            ModifyRepo repo = null,
            PackageCentral.PackageCentral packages = null,
            PackageCentral.PackageCentral.Selector selector = PackageCentral.PackageCentral.Selector.Main,
            AddKeyListKeys_Button addButton = AddKeyListKeys_Button.Existing,
            AddKeyListKeys_Button highlightButton = AddKeyListKeys_Button.None,
            string addExistingEntities = null,
            Func<Aas.IReference, Aas.IReference> modifyAddExistingKey = null,
            string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
            Func<List<Aas.IKey>, AnyUiLambdaActionBase> jumpLambda = null,
            AnyUiLambdaActionBase takeOverLambdaAction = null,
            Func<List<Aas.IKey>, AnyUiLambdaActionBase> noEditJumpLambda = null,
            Aas.IReferable relatedReferable = null,
            Action<Aas.IReferable> emitCustomEvent = null,
            AnyUiPanel frontPanel = null,
            AnyUiPanel footerPanel = null,
            bool topContextMenu = false,
            Func<int, AnyUiLambdaActionBase> auxButtonLambda = null,
            string[] auxButtonTitles = null, string[] auxButtonToolTips = null,
            AnyUiContextMenuHeaderList auxContextHeader = null, 
            Func<int, AnyUiLambdaActionBase> auxContextLambda = null,
            int maxNumOfKey = int.MaxValue,
            FirstColumnWidth? firstColumnWidth = null,
            AnyUiButtonOverStyle buttonOverStyleLo = null,
            AnyUiButtonOverStyle buttonOverStyleHi = null,
            AnyUiButtonPreference buttonPreferenceLo = AnyUiButtonPreference.None)
        {
            // sometimes needless to show
            if (repo == null && (keys == null || keys.Count < 1))
                return;
            int rows = 1; // default!
            if (keys != null && keys.Count >= 1)
                rows += keys.Count;
            if (footerPanel != null)
                rows++;
            int rowOfs = 0;
            if (repo != null)
                rowOfs = 1;
            if (repo != null && jumpLambda != null)
                rowOfs = 1;

            // default
            if (emitCustomEvent == null)
                emitCustomEvent = (rf) => { this.AddDiaryEntry(rf, new DiaryEntryStructChange()); };

            // Grid
            var g = AddSmallGrid(rows + rowOfs, 6, new[] { "#", "#", "#", "#", "*", "#", },
                margin: new AnyUiThickness(0, 0, 0, 0));

            if (firstColumnWidth != null)
                MaintainFirstColumnWidth(g, firstColumnWidth);
            else
                g.ColumnDefinitions[0].MinWidth = GetWidth(FirstColumnWidth.Standard);

            // populate key
            if (firstColumnWidth != FirstColumnWidth.No)
                AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0),
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    content: "" + key + ":");

            // presets?
            var presetNo = 0;
            if (addPresetNames != null && addPresetKeyLists != null
                && addPresetNames.Length == addPresetKeyLists.Length)
                presetNo = addPresetNames.Length;

            if (repo == null)
            {
                // TODO (Michael Hoffmeister, 2020-08-01): possibly [Jump] button??
            }
            else
            if (keys != null)
            {
                //
                // First row:
                // populate [+], [Select], [ECLASS], [Copy] buttons
                //

                // quite many columns in the first row; separately managed
                var colDescs = new List<string>(new[] { "*", "#", "#", "#", "#", "#", "#", "#", "#" });
                for (int i = 0; i < presetNo; i++)
                    colDescs.Add("#");
                if (auxButtonTitles != null)
                    for (int i = 0; i < auxButtonTitles.Length; i++)
                        colDescs.Add("#");

                // add this first row grid to the overall grid
                var g2 = AddSmallGrid(1, colDescs.Count, colDescs.ToArray());
                g2.HorizontalAlignment = AnyUiHorizontalAlignment.Stretch;
                AnyUiGrid.SetRow(g2, 0);
                AnyUiGrid.SetColumn(g2, 1);
                AnyUiGrid.SetColumnSpan(g2, 7);
                g.Children.Add(g2);

                // hook in the front panel, if given
                if (frontPanel != null)
                {
                    AnyUiGrid.SetRow(frontPanel, 0);
                    AnyUiGrid.SetColumn(frontPanel, 0);
                    g2.Children.Add(frontPanel);
                }

                //
                // Define lambdas for double use (first row / context menu)
                //

                Func<object, Task<AnyUiLambdaActionBase>> lambdaEclassIrdiAsync = async (o) =>
                {
                    await Task.Yield();
                    string resIRDI = null;
                    Aas.ConceptDescription resCD = null;
                    if (this.SmartSelectEclassEntity(
                            AnyUiDialogueDataSelectEclassEntity.SelectMode.IRDI, ref resIRDI, ref resCD))
                    {
                        keys.Add(
                            new Aas.Key(Aas.KeyTypes.GlobalReference, resIRDI));
                    }

                    emitCustomEvent?.Invoke(relatedReferable);

                    if (takeOverLambdaAction != null)
                        return takeOverLambdaAction;
                    else
                        return new AnyUiLambdaActionRedrawEntity();
                };

                Func<object, Task<AnyUiLambdaActionBase>> lambdaClipboardAsync = async (o) =>
                {
                    var st = keys.ToStringExtended(delimiter: "\r\n");
                    await context.ClipboardSetAsync(new AnyUiClipboardData(st));
                    Log.Singleton.Info("Keys written to clipboard.");
                    return new AnyUiLambdaActionNone();
                };

                // 
                // populate top row
                //

                if ((addButton & AddKeyListKeys_Button.Known) > 0)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 2,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.AddKnown, "Add known", 
                                        "Add reference from the internal library of known references."),
                            buttonOverStyle: ((highlightButton & AddKeyListKeys_Button.Known) > 0) 
                                             ? buttonOverStyleHi : buttonOverStyleLo),
                        setValueAsync: async (o) =>
                        {
                            var uc = new AnyUiDialogueDataSelectReferableFromPool(
                                caption: "Select known entity");
                            await context.StartFlyoverModalAsync(uc);

                            if (uc.Result &&
                                uc.ResultItem is AasxPredefinedConcepts.DefinitionsPoolReferableEntity pe)
                            {
                                // dedicated semanticId proposed?
                                if (((addButton & AddKeyListKeys_Button.KnownSemanticId) > 0) 
                                    && pe.Ref is Aas.IHasSemantics sem
                                    && sem.SemanticId?.IsValid() == true)
                                {
                                    keys.Clear();
                                    keys.AddRange(sem.SemanticId.Keys);
                                }
                                // else take the Id
                                else if (pe.Ref is Aas.IIdentifiable id
                                    && id.Id != null)
                                {
                                    // DECISION: references to concepts are always GlobalReferences
                                    keys.AddCheckBlank(new Aas.Key(Aas.KeyTypes.GlobalReference, id.Id));
                                }
                            }

                            emitCustomEvent?.Invoke(relatedReferable);

                            if (takeOverLambdaAction != null)
                                return takeOverLambdaAction;
                            else
                                return new AnyUiLambdaActionRedrawEntity();
                        });

                if (!topContextMenu && ((addButton & AddKeyListKeys_Button.Eclass) > 0))
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 3,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.AddExisting, "Add ECLASS",
                                        "Add reference to a ECLASS concept via IRDI."),
                            buttonOverStyle: ((highlightButton & AddKeyListKeys_Button.Eclass) > 0)
                                             ? buttonOverStyleHi : buttonOverStyleLo),
                        lambdaEclassIrdiAsync);

                if (packages.MainAvailable && ((addButton & AddKeyListKeys_Button.Existing) > 0))
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 4,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.AddExisting, "Add existing",
                                        "Add reference to an existing element in packages."),
                            buttonOverStyle: ((highlightButton & AddKeyListKeys_Button.Existing) > 0)
                                             ? buttonOverStyleHi : buttonOverStyleLo),
                            setValueAsync: async (o) =>
                            {
                                var k2 = await SmartSelectAasEntityKeysAsync(packages, selector, addExistingEntities);

                                if (modifyAddExistingKey != null)
                                {
                                    var outRefs = ExtendReference.CreateNew(k2);
                                    var inRefs = modifyAddExistingKey.Invoke(outRefs);
                                    if (inRefs != null)
                                        k2 = inRefs.Keys;
                                }                                                      

                                // some special cases
                                if (!Options.Curr.ModelRefCd && k2 != null && k2.Count == 1
                                    && k2[0].Type == Aas.KeyTypes.ConceptDescription)
                                    k2[0].Type = Aas.KeyTypes.GlobalReference;

                                if (k2 != null)
                                    foreach (var k2k in k2)
                                        keys.AddCheckBlank(k2k);

                                emitCustomEvent?.Invoke(relatedReferable);

                                if (takeOverLambdaAction != null)
                                    return takeOverLambdaAction;
                                else
                                    return new AnyUiLambdaActionRedrawEntity();
                            });

                if ((addButton & AddKeyListKeys_Button.Blank) > 0)
                {
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 5,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.AddBlank, "Add blank",
                                            "Add blank reference."),
                            buttonOverStyle: ((highlightButton & AddKeyListKeys_Button.Blank) > 0)
                                             ? buttonOverStyleHi : buttonOverStyleLo),
                            async (o) =>
                            {
                                await Task.Yield();
                                var k = new Aas.Key(Aas.KeyTypes.GlobalReference, ""); //TODO (jtikekar, 0000-00-00): default key
                                keys.Add(k);

                                emitCustomEvent?.Invoke(relatedReferable);

                                if (takeOverLambdaAction != null)
                                    return takeOverLambdaAction;
                                else
                                    return new AnyUiLambdaActionRedrawEntity();
                            });
                }

                if (!topContextMenu && jumpLambda != null)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 6,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.Jump, "Jump",
                                        "Jump to AAS element in package."),
                            buttonOverStyle: buttonOverStyleLo.Modify(preference: buttonPreferenceLo)),
                        async (o) =>
                        {
                            await Task.Yield();
                            return jumpLambda(keys);
                        });

                if (!topContextMenu)
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 7,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.CopyToClipboard, "Clipboard",
                                        "Copy reference as JSON to clipboard."),
                            buttonOverStyle: buttonOverStyleLo?.Modify(preference: buttonPreferenceLo)),
                        setValueAsync: lambdaClipboardAsync);

                //
                // Presets
                //

                for (int i = 0; i < presetNo; i++)
                {
                    var closureKey = addPresetKeyLists[i];
                    AnyUiUIElement.RegisterControl(
                        AddSmallButtonTo(
                            g2, 0, 8 + i,
                            margin: new AnyUiThickness(2, 2, 2, 2),
                            padding: new AnyUiThickness(5, 0, 5, 0),
                            header: new AnyUiButtonHeader(IconPool.AddPreset, "" + addPresetNames[i],
                                        "Add preset: " + addPresetNames[i]).Modify(AnyUiButtonPreference.Both),
                            buttonOverStyle: ((highlightButton & AddKeyListKeys_Button.Presets) > 0)
                                             ? buttonOverStyleHi : buttonOverStyleLo),
                        async (o) =>
                        {
                            await Task.Yield();
                            keys.AddRange(closureKey);
                            emitCustomEvent?.Invoke(relatedReferable);
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                //
                // Aux Buttons
                //

                int currCol = 8 + presetNo;

                if (auxButtonTitles != null)
                    for (int i = 0; i < auxButtonTitles.Length; i++)
                    {
                        Func<object, Task<AnyUiLambdaActionBase>> lmb = null;
                        int closureI = i;
                        if (auxButtonLambda != null)
                            lmb = async (o) =>
                            {
                                await Task.Yield();
                                return auxButtonLambda(closureI); // exchange o with i !!
                            };
                        var b = AnyUiUIElement.RegisterControl(
                            AddSmallButtonTo(
                                g2, 0, currCol++,
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                header: new AnyUiButtonHeader(IconPool.AddPreset, "" + auxButtonTitles[i],
                                        "" + auxButtonTitles[i]).Modify(AnyUiButtonPreference.Both),
                                buttonOverStyle: buttonOverStyleLo),
                            lmb) as AnyUiButton;
                        if (auxButtonToolTips != null && i < auxButtonToolTips.Length)
                            b.ToolTip = auxButtonToolTips[i];
                    }

                //
                // Top Row Context Menue
                // (those functions less frequent use)
                //

                if (topContextMenu)
                {
                    AnyUiContextMenuHeaderList contextHeaders = new();
                    contextHeaders.Add(new AnyUiContextMenuHeaderIconSource(1, IconPool.Delete, "Delete keys completely"));
                    contextHeaders.Add(new AnyUiContextMenuHeaderIconSource(0, IconPool.ClearAll, "Set all keys \u2192 1 blank"));

                    if ((addButton & AddKeyListKeys_Button.Eclass) > 0)
                        contextHeaders.Add(new AnyUiContextMenuHeaderIconSource(2, IconPool.EclassOrg, "Add ECLASS"));
                    if (jumpLambda != null)
                        contextHeaders.Add(new AnyUiContextMenuHeaderIconSource(3, IconPool.Jump, "Jump")); 
                    if (true)
                        contextHeaders.Add(new AnyUiContextMenuHeaderIconSource(4, IconPool.CopyToClipboard, "Copy to clipboard"));

                    // the aux receive an index > 100
                    contextHeaders.AddRangeWithOffet(auxContextHeader, 100);

                    AddSmallContextMenuItemTo(
                        g2, 0, currCol++,      
                        header: new AnyUiButtonHeader(IconPool.MoreVert, "More",
                                        "More options in context menu."),
                        buttonOverStyle: buttonOverStyleLo?.Modify(preference: buttonPreferenceLo),
                        menuHeaders: contextHeaders,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        padding: new AnyUiThickness(5, 0, 5, 0),
                        horizontalAlignment: AnyUiHorizontalAlignment.Center,
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        menuItemLambdaAsync: async (o) =>
                        {
                            if (o is int oi)
                            {
                                if (oi >= 100 && auxContextLambda != null)
                                    return auxContextLambda(oi - 100);

                                if (oi == 2)
                                    return await lambdaEclassIrdiAsync(o);

                                if (oi == 3)
                                    return jumpLambda(keys);

                                if (oi == 4)
                                    return await lambdaClipboardAsync(o);

                                if (oi == 0 || oi == 1)
                                {
                                    // re-init
                                    if (oi == 0)
                                    {
                                        keys.Clear();
                                        keys.Add(Options.Curr.GetDefaultEmptyReference()?.Keys?.FirstOrDefault());
                                    }

                                    if (oi == 1)
                                    {
                                        keys = null;
                                        setKeysNull?.Invoke();
                                    }

                                    // change to the outside
                                    emitCustomEvent?.Invoke(relatedReferable);

                                    // visualize
                                    if (takeOverLambdaAction != null)
                                        return takeOverLambdaAction;
                                    else
                                        return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }
            }

            // contents?
            if (keys != null)
                for (int i = 0; i < keys.Count; i++)
                    if (repo == null)
                    {
                        // type
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 1,
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            setNoWrap: true,
                            content: "(" + keys[i].Type + ")",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        // value
                        AddSmallLabelTo(
                            g, 0 + i + rowOfs, 4,
                            padding: new AnyUiThickness(2, 0, 0, 0),
                            content: "" + keys[i].Value,
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        // jump
                        /* TODO (MIHO, 2021-02-16): this mechanism is ugly and only intended to be temporary!
                           It shall be replaced (after intergrating AnyUI) by a better repo handling */
                        if (noEditJumpLambda != null && i == 0)
                        {
                            AnyUiUIElement.RegisterControl(
                                AddSmallButtonTo(
                                    g, 0 + +rowOfs, 5,
                                    margin: new AnyUiThickness(2, 2, 2, 2),
                                    padding: new AnyUiThickness(5, 0, 5, 0),
                                    content: "Jump"),
                                    async (o) =>
                                    {
                                        await Task.Yield();
                                        return noEditJumpLambda(keys);
                                    });
                        }
                    }
                    else
                    {
                        // save in current context
                        var currentI = 0 + i;

                        // TODO (Michael Hoffmeister, 2020-08-01): Needs to be revisited

                        // type
                        var cbType = AnyUiUIElement.RegisterControl(
                            AddSmallComboBoxTo(
                                g, 0 + i + rowOfs, 1,
                                margin: NormalOrCapa(
                                    new AnyUiThickness(4, 2, 2, 2),
                                    AnyUiContextCapability.Blazor, new AnyUiThickness(4, 1, 2, -1)),
                                padding: NormalOrCapa(
                                    new AnyUiThickness(2, -1, 0, -1),
                                    AnyUiContextCapability.Blazor, new AnyUiThickness(2, 4, 0, 4)),
                                text: "" + keys[currentI].Type,
                                minWidth: 100,
                                items: Enum.GetNames(typeof(Aas.KeyTypes)),
                                isEditable: false,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                            async (o) =>
                            {
                                await Task.Yield();
                                keys[currentI].Type = (Aas.KeyTypes)Aas.Stringification.KeyTypesFromString((string)o);
                                emitCustomEvent?.Invoke(relatedReferable);
                                return new AnyUiLambdaActionNone();
                            },
                            takeOverLambda: takeOverLambdaAction);
                        SmallComboBoxSelectNearestItem(cbType, cbType.Text);

                        // check here, if to hightlight
                        if (cbType != null && this.highlightField != null &&
                                this.highlightField.fieldHash == keys[currentI].Type.GetHashCode() &&
                                keys[currentI] == this.highlightField.containingObject)
                            this.HighligtStateElement(cbType, true);

                        // dead-csharp off
                        //// check here, if to hightlight
                        //if (cbIdType != null && this.highlightField != null && keys[currentI].idType != null &&
                        //        this.highlightField.fieldHash == keys[currentI].idType.GetHashCode() &&
                        //        keys[currentI] == this.highlightField.containingObject)
                        //    this.HighligtStateElement(cbIdType, true);
                        // dead-csharp on
                        // value
                        var tbValue = AddSmallTextBoxTo(
                            g, 0 + i + rowOfs, 4,
                            margin: NormalOrCapa(
                                new AnyUiThickness(2, 2, 2, 2),
                                AnyUiContextCapability.Blazor, new AnyUiThickness(6, 1, 2, 1)),
                            text: "" + keys[currentI].Value,
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);
                        AnyUiUIElement.RegisterControl(
                            tbValue,
                            async (o) =>
                            {
                                await Task.Yield();
                                keys[currentI].Value = o as string;
                                emitCustomEvent?.Invoke(relatedReferable);
                                return new AnyUiLambdaActionNone();
                            }, takeOverLambda: takeOverLambdaAction);

                        // check here, if to hightlight
                        if (tbValue != null && this.highlightField != null && keys[currentI].Value != null &&
                                this.highlightField.fieldHash == keys[currentI].Value.GetHashCode() &&
                                keys[currentI] == this.highlightField.containingObject)
                            this.HighligtStateElement(tbValue, true);

                        // button [hamburger]
                        AddSmallContextMenuItemTo(
                                g, 0 + i + rowOfs, 5,
                                header: new AnyUiButtonHeader(IconPool.MoreVert, "More",
                                        "More options in context menu."),
                                buttonOverStyle: buttonOverStyleLo?.Modify(preference: buttonPreferenceLo),
                                menuHeaders: new AnyUiContextMenuHeaderList(new[] {
                                    new AnyUiContextMenuHeaderIconSource(0, IconPool.Delete, "Delete"),
                                    new AnyUiContextMenuHeaderIconSource(1, IconPool.MoveUp, "Move Up"),
                                    new AnyUiContextMenuHeaderIconSource(1, IconPool.MoveDown, "Move Down"),
                                }),
                                margin: new AnyUiThickness(2, 2, 2, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                horizontalAlignment: AnyUiHorizontalAlignment.Center,
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                menuItemLambdaAsync: async (o) =>
                                {
                                    await Task.Yield();
                                    var action = false;

                                    if (o is int ti)
                                        switch (ti)
                                        {
                                            case 0:
                                                keys.RemoveAt(currentI);
                                                if (keys.Count < 1)
                                                {
                                                    keys = null;
                                                    setKeysNull?.Invoke();
                                                }
                                                action = true;
                                                break;
                                            case 1:
                                                MoveElementInListUpwards<Aas.IKey>(keys, keys[currentI]);
                                                action = true;
                                                break;
                                            case 2:
                                                MoveElementInListDownwards<Aas.IKey>(keys, keys[currentI]);
                                                action = true;
                                                break;
                                        }

                                    emitCustomEvent?.Invoke(relatedReferable);

                                    if (action)
                                        if (takeOverLambdaAction != null)
                                            return takeOverLambdaAction;
                                        else
                                            return new AnyUiLambdaActionRedrawEntity();
                                    return new AnyUiLambdaActionNone();
                                });

                    }

            //
            // Footer
            //

            if (footerPanel != null)
            {
                AnyUiGrid.SetRow(footerPanel, 0 + keys.Count + rowOfs);
                AnyUiGrid.SetColumn(footerPanel, 1);
                AnyUiGrid.SetColumnSpan(footerPanel, 7);
                g.Children.Add(footerPanel);
            }

            //
            // in total
            //

            view.Children.Add(g);
        }

        //
        // Safeguarding functions (checking if somethingis null and doing ..)
        //

        public bool SafeguardAccess(
            AnyUiStackPanel view, ModifyRepo repo, object data, string key, string actionStr,
            Func<int, Task<AnyUiLambdaActionBase>> actionAsync,
            FirstColumnWidth firstColumnWidth = FirstColumnWidth.Standard)
        {
            if (repo != null && data == null)
            {
                AddActionPanel(view, key,
                    superMenu: null,
                    ticketMenu: new AasxMenu(new[] { new AasxMenuItem(
                                        IconPool.Add,
                                        name: AdminShellUtil.FilterFriendlyName("add-" + key, fixMoreBlanks: true).ToLower(),
                                        header: "Add",
                                        helpText: "Add empty attribute data") }),
                    buttonOverStyle: LayoutHints.StyleButtonAction,
                    repo: repo, actionAsync: actionAsync, firstColumnWidth: firstColumnWidth);
                // AddAction(view, key, actionStr, repo, actionAsync: actionAsync, firstColumnWidth: firstColumnWidth);
            }
            return (data != null);
        }

        /// <summary>
        /// Generates a string for the <c>AddSmallGrid</c> function to represent the first column
        /// </summary>
        [Obsolete]
        public string DetermineFirstColumnWidth(FirstColumnWidth? firstColumnWidth = FirstColumnWidth.Standard)
        {
            if (firstColumnWidth != null)
            {
                var w = GetWidth(FirstColumnWidth.Standard);
                return $"{w}:";
            }
            return "#";
        }


        /// <summary>
        /// Generates a string for the <c>AddSmallGrid</c> function to represent the first column
        /// </summary>
        public AnyUiGrid MaintainFirstColumnWidth(
            AnyUiGrid g, 
            FirstColumnWidth? firstColumnWidth = FirstColumnWidth.Standard)
        {
            // access
            if (g?.ColumnDefinitions == null || g.ColumnDefinitions.Count < 1 || firstColumnWidth == null)
                return g;

            // apply
            g.ColumnDefinitions[0].MinWidth = GetWidth(firstColumnWidth.Value);

            // done
            return g;
        }

        public bool SafeguardAccessNew(
            AnyUiStackPanel view, ModifyRepo repo, 
            Func<bool> lambdaIsNone,
            Action lambdaSetNull,
            string key, AnyUiButtonHeader buttonCreate,
            Func<int, Task<AnyUiLambdaActionBase>> lambdaCreate,
            Action<AnyUiStackPanel> lambdaSuccess,
            FirstColumnWidth firstColumnWidth = FirstColumnWidth.Standard,
            AnyUiThickness margin = null)
        {
            // new approach: make a two column grid for key and the rest
            var g = MaintainFirstColumnWidth(
                        AddSmallGrid(1, 2, new[] { "#", "*" }, margin: margin),
                        firstColumnWidth);
            view?.Children?.Add(g);

            // key
            var x = AddSmallLabelTo(g, 0, 0, margin: new AnyUiThickness(5, 0, 0, 0),
                setNoWrap: true,
                content: "" + key,
                verticalCenter: true);

            // the old data == 0 ?
            if (lambdaIsNone != null && lambdaIsNone.Invoke() == true)
            {
                // unify to null
                lambdaSetNull?.Invoke();

                // display a button for action?
                if (repo != null && buttonCreate != null)
                {
                    AnyUiUIElement.RegisterControl(
                        Set(
                            AddSmallButtonTo(
                                g, 0, 1,
                                margin: new AnyUiThickness(0, 2, 4, 2),
                                padding: new AnyUiThickness(5, 0, 5, 0),
                                header: new AnyUiButtonHeader(IconPool.Add.SetIntense(), "Add", "Add empty element data"),
                                buttonOverStyle: LayoutHints.StyleButtonAction),
                            horizontalAlignment: AnyUiHorizontalAlignment.Left),
                        async (o) =>
                        {
                            if (lambdaCreate != null)
                                return await lambdaCreate.Invoke((int)0);
                            return new AnyUiLambdaActionNone();
                        });
                }

                // nope is negative result
                return false;
            }
            else
            {
                // make a panel to put childs to it
                var childPanel = AddSmallStackPanelTo(g, 0, 1, setVertical: true);

                // mark to be potentially be substituted
                childPanel.Tag = TAG_ControlToBeSubstituted;

                // put contents to 
                lambdaSuccess?.Invoke(childPanel);

                // success is positive result
                return true;
            }
        }


        public bool SafeguardAccess(
            AnyUiStackPanel view, ModifyRepo repo, object data, string key,
            AasxMenu superMenu = null,
            AasxMenu ticketMenu = null,
            Func<int, AasxMenuActionTicket, Task<AnyUiLambdaActionBase>> ticketActionAsync = null,
            FirstColumnWidth firstColumnWidth = FirstColumnWidth.Standard)
        {
            if (repo != null && data == null)
                AddActionPanel(
                    view, key, 
                    repo: repo, 
                    superMenu: superMenu,
                    ticketMenu: ticketMenu, 
                    ticketActionAsync: ticketActionAsync,
                    firstColumnWidth: firstColumnWidth);
            return (data != null);
        }

        //
        // List manipulations (single entities)
        //

        public int MoveElementInListUpwards<T>(IList<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 1)
                return -1;
            list.RemoveAt(ndx);
            var newndx = Math.Max(ndx - 1, 0);
            list.Insert(newndx, entity);
            return newndx;
        }

        public int MoveElementInListDownwards<T>(IList<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 0 || ndx >= list.Count - 1)
                return -1;
            list.RemoveAt(ndx);
            var newndx = Math.Min(ndx + 1, list.Count);
            list.Insert(newndx, entity);
            return newndx;
        }

        public int MoveElementToTopOfList<T>(IList<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 1)
                return -1;
            list.RemoveAt(ndx);
            var newndx = 0;
            list.Insert(newndx, entity);
            return newndx;
        }

        public int MoveElementToBottomOfList<T>(IList<T> list, T entity)
        {
            if (list == null || list.Count < 2 || entity == null)
                return -1;
            int ndx = list.IndexOf(entity);
            if (ndx < 0)
                return -1;
            list.RemoveAt(ndx);
            var newndx = list.Count;
            list.Insert(newndx, entity);
            return newndx;
        }

        public object DeleteElementInList<T>(IList<T> list, T entity, object alternativeReturn)
        {
            if (list == null || entity == null)
                return alternativeReturn;
            int ndx = list.IndexOf(entity);
            if (ndx < 0)
                return alternativeReturn;
            list.RemoveAt(ndx);
            if (ndx > 0)
                return list.ElementAt(ndx - 1);
            return alternativeReturn;
        }

        public int AddElementInListBefore<T>(IList<T> list, T entity, T existing)
        {
            if (list == null || list.Count < 1 || entity == null)
                return -1;
            int ndx = list.IndexOf(existing);
            if (ndx < 0 || ndx > list.Count - 1)
                return -1;
            list.Insert(ndx, entity);
            return ndx;
        }

        public int AddElementInListAfter<T>(IList<T> list, T entity, T existing)
        {
            if (list == null || list.Count < 1 || entity == null)
                return -1;
            int ndx = list.IndexOf(existing);
            if (ndx < 0 || ndx > list.Count)
                return -1;
            list.Insert(ndx + 1, entity);
            return ndx + 1;
        }

        //
        // List manipulations (multiple entities)
        //

        public int MoveElementsToStartingIndex<T>(IList<T> list, List<T> entities, int startingIndex)
        {
            // check
            if (list == null || list.Count < 1 || entities == null)
                return -1;
            if (startingIndex < 0)
                return -1;
            // remove all from list
            foreach (var e in entities)
                if (list.Contains(e))
                    list.Remove(e);
            // now, insert sequentially starting from index
            var si2 = startingIndex;
            if (si2 > list.Count)
                si2 = list.Count;
            var ndx = si2;
            foreach (var e in entities)
                list.Insert(ndx++, e);
            // return something
            return si2;
        }

        public int DeleteElementsInList<T>(IList<T> list, List<T> entities)
        {
            // check
            if (list == null || list.Count < 1 || entities == null)
                return -1;
            // remove all from list
            foreach (var e in entities)
                if (list.Contains(e))
                    list.Remove(e);
            // return something
            return 0;
        }

        //
        // manipulations for list of SME wrappers
        //

        public int AddElementInSmeListBefore<T>(
            IList<T> list,
            T entity, T existing,
            bool makeUniqueIfNeeded = false)
            where T : Aas.ISubmodelElement
        {
            // access
            if (list == null || list.Count < 1 || entity == null)
                return -1;

            // make unqiue
            if (makeUniqueIfNeeded && !(list as List<Aas.ISubmodelElement>)
                .CheckIdShortIsUnique(entity.IdShort))
                this.MakeNewReferableUnique(entity);

            // delegate
            return AddElementInListBefore<T>(list, entity, existing);
        }

        public int AddElementInSmeListAfter<T>(
            IList<T> list,
            T entity, T existing,
            bool makeUniqueIfNeeded = false)
            where T : Aas.ISubmodelElement
        {
            // access
            if (list == null || list.Count < 1 || entity == null)
                return -1;

            // make unqiue
            if (makeUniqueIfNeeded && !(list as List<Aas.ISubmodelElement>)
                .CheckIdShortIsUnique(entity.IdShort))
                this.MakeNewReferableUnique(entity);

            // delegate
            return AddElementInListAfter<T>(list, entity, existing);
        }

        //
        // Helper
        //

        public void EntityListUpDownDeleteHelper<T>(
            AnyUiPanel stack, ModifyRepo repo,
            IList<T> list, Action<List<T>> setOutputList,
            T entity,
            object alternativeFocus, string label = "Entities:",
            object nextFocus = null, PackCntChangeEventData sendUpdateEvent = null, bool preventMove = false,
            Aas.IReferable explicitParent = null,
            AasxMenu superMenu = null,
            Func<string, AasxMenuActionTicket, Task> postActionHookAsync = null,
            AasxMenu extraMenu = null,
            Func<int, AnyUiLambdaActionBase> lambdaExtraMenu = null,
            Func<int, Task<AnyUiLambdaActionBase>> lambdaExtraMenuAsync = null,
            bool moveDoesNotModify = false,
            AnyUiButtonOverStyle buttonOverStyle = null,
            KeyLabelHandling keyLabel = KeyLabelHandling.Standard)
        {
            if (nextFocus == null)
                nextFocus = entity;

            // pick out referable
            Aas.IReferable entityRf = null;
            if (entity is Aas.ISubmodelElement smw)
                entityRf = smw;
            if (entity is Aas.IReferable rf)
                entityRf = rf;

            var theMenu = new AasxMenu()
                    .AddAction("aas-elem-move-up", "Move up",
                        "Moves the currently selected element up in containing collection.",
                        IconPool.MoveUp,
                        inputGesture: "Shift+Ctrl+Up")
                    .AddAction("aas-elem-move-down", "Move down",
                        "Moves the currently selected element down in containing collection.",
                        IconPool.MoveDown,
                        inputGesture: "Shift+Ctrl+Down")
                    .AddAction("aas-elem-move-top", "Move top",
                        "Moves the currently selected element to the top in containing collection.",
                        IconPool.MoveTop,
                        inputGesture: "Shift+Ctrl+Home")
                    .AddAction("aas-elem-move-end", "Move end",
                        "Moves the currently selected element to the end in containing collection.",
                        IconPool.MoveBottom,
                        inputGesture: "Shift+Ctrl+End")
                    .AddAction("aas-elem-delete", "Delete",
                        "Deletes the currently selected element.",
                        IconPool.Delete,
                        inputGesture: "Ctrl+Shift+Delete");

            if (extraMenu != null)
                theMenu.AddRange(extraMenu);

            AddActionPanel(
                stack, label,
                repo: repo,
                superMenu: superMenu,
                ticketMenu: theMenu,
                keyLabel: keyLabel,
                useWrapFlexPanel: false,
                buttonOverStyle: (buttonOverStyle ?? LayoutHints.StyleButtonAction).Modify(
                                    preference: AnyUiButtonPreference.Image),
                ticketActionAsync: async (buttonNdx, ticket) =>
                {
                    if (buttonNdx >= 0 && buttonNdx <= 3)
                    {
                        if (preventMove)
                        {
                            await context.MessageBoxFlyoutShowAsync(
                                "Moving within list is not possible, as list of entities has dynamic " +
                                "sort order.",
                                "Move entities", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                            return new AnyUiLambdaActionNone();
                        }

                        var newndx = -1;
                        if (buttonNdx == 0) newndx = MoveElementInListUpwards<T>(list, entity);
                        if (buttonNdx == 1) newndx = MoveElementInListDownwards<T>(list, entity);
                        if (buttonNdx == 2) newndx = MoveElementToTopOfList<T>(list, entity);
                        if (buttonNdx == 3) newndx = MoveElementToBottomOfList<T>(list, entity);
                        if (newndx >= 0)
                        {
                            if (postActionHookAsync != null)
                                await postActionHookAsync.Invoke(theMenu?.ElementAt(buttonNdx)?.Name, ticket);

                            if (entityRf != null && !moveDoesNotModify)
                                this.AddDiaryEntry(entityRf,
                                    new DiaryEntryStructChange(StructuralChangeReason.Modify, createAtIndex: newndx),
                                    explicitParent: explicitParent);

                            if (sendUpdateEvent != null)
                            {
                                sendUpdateEvent.Reason = PackCntChangeEventReason.MoveToIndex;
                                sendUpdateEvent.NewIndex = newndx;
                                sendUpdateEvent.DisableSelectedTreeItemChange = true;
                                return new AnyUiLambdaActionPackCntChange(sendUpdateEvent);
                            }
                            else
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: null);
                        }
                        else
                            return new AnyUiLambdaActionNone();
                    }

                    if (buttonNdx == 4)

                        if (this.context.ActualShiftState
                            || ticket?.ScriptMode == true
                            || AnyUiMessageBoxResult.Yes == await context.MessageBoxFlyoutShowAsync(
                                "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        {
                            var ret = DeleteElementInList<T>(list, entity, alternativeFocus);

                            if (list.Count < 1)
                                setOutputList?.Invoke(null);

                            if (postActionHookAsync != null)
                            {
                                await postActionHookAsync?.Invoke(theMenu?.ElementAt(buttonNdx)?.Name, ticket);
                            }
                            this.AddDiaryEntry(entityRf,
                                new DiaryEntryStructChange(StructuralChangeReason.Delete),
                                explicitParent: explicitParent);

                            if (sendUpdateEvent != null)
                            {
                                sendUpdateEvent.Reason = PackCntChangeEventReason.Delete;
                                return new AnyUiLambdaActionPackCntChange(sendUpdateEvent, nextFocus: ret);
                            }
                            else
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: ret, isExpanded: null);
                        }

                    if (buttonNdx > 4)
                    {
                        // invoke extra menu
                        if (lambdaExtraMenu != null)
                            return lambdaExtraMenu?.Invoke(buttonNdx - 5);
                        if (lambdaExtraMenuAsync != null)
                            return await lambdaExtraMenuAsync?.Invoke(buttonNdx - 5);
                    }

                    return new AnyUiLambdaActionNone();
                });
        }

        //
        // Identify ECLASS properties to be imported
        //

        public void IdentifyTargetsForEclassImportOfCDs(
            Aas.IEnvironment env, List<Aas.ISubmodelElement> elems,
            ref List<Aas.ISubmodelElement> targets)
        {
            if (env == null || targets == null || elems == null)
                return;
            foreach (var elem in elems)
            {
                // sort all the non-fitting
                if (elem.SemanticId != null && !elem.SemanticId.IsEmpty() && elem.SemanticId.Keys.Count == 1
                    && elem.SemanticId.Keys[0].Type == Aas.KeyTypes.ConceptDescription
                    && elem.SemanticId.Keys[0].Value.StartsWith("0173"))
                {
                    // already in CDs?
                    var x = env.FindConceptDescriptionByReference(elem.SemanticId);
                    if (x == null)
                        // this one has the potential to get imported ECLASS CD
                        targets.Add(elem);
                }

                // recursion?
                if (elem is Aas.SubmodelElementCollection elemsmc && elemsmc.Value != null)
                {
                    var childs = new List<Aas.ISubmodelElement>(elemsmc.Value);
                    IdentifyTargetsForEclassImportOfCDs(env, childs, ref targets);
                }
            }
        }

        public async Task<bool> ImportEclassCDsForTargetsAsync(Aas.IEnvironment env, object startMainDataElement,
                List<Aas.ISubmodelElement> targets)
        {
            // need dialogue and data
            if (env == null || targets == null)
                return false;

            // use ECLASS utilities
            var fullfn = System.IO.Path.GetFullPath(Options.Curr.EclassDir);
            var jobData = new EclassUtils.SearchJobData(fullfn);
            foreach (var t in targets)
                if (t != null && t.SemanticId != null && t.SemanticId.Keys.Count == 1)
                    jobData.searchIRDIs.Add(t.SemanticId.Keys[0].Value.ToLower().Trim());
            // still valid?
            if (jobData.searchIRDIs.Count < 1)
                return false;

            // make a progress flyout
            var uc = new AnyUiDialogueDataProgress(
                "Import ConceptDescriptions from ECLASS",
                info: "Preparing ...", symbol: AnyUiMessageBoxImage.Information);
            uc.Progress = 0.0;
            
            // show this
            await context.StartFlyoverAsync(uc);

            // setup worker
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                // job data
                System.Threading.Thread.Sleep(10);

                // longrunnig task for searching IRDIs ..
                uc.Info = "Collecting ECLASS Data ..";
                EclassUtils.SearchForIRDIinEclassFiles(jobData, (frac) =>
                {
                    uc.Progress = frac;
                });

                // apply to targets
                uc.Info = "Adding missing ConceptDescriptions ..";
                uc.Progress = 0.0;
                for (int i = 0; i < targets.Count; i++)
                {
                    // progress
                    uc.Progress = (1.0 / targets.Count) * i;

                    // access
                    var t = targets[i];
                    if (t.SemanticId == null || t.SemanticId.Keys.Count != 1)
                        continue;

                    // CD
                    var newcd = EclassUtils.GenerateConceptDescription(jobData.items, t.SemanticId.Keys[0].Value);
                    if (newcd == null)
                        continue;

                    // add?
                    if (null == env.FindConceptDescriptionByReference(
                            new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.ConceptDescription, newcd.Id) })))
                    {
                        env.Add(newcd);

                        this.AddDiaryEntry(newcd, new DiaryEntryStructChange(StructuralChangeReason.Create));
                    }
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                // in any case, close flyover
                this.context.CloseFlyover();

                // redraw everything
                this.context.EmitOutsideAction(new AnyUiLambdaActionRedrawAllElements(startMainDataElement));
            };
            worker.RunWorkerAsync();

            // ok
            return true;
        }

        /// <summary>
        /// Creates CDs depending on semanticId of SM or SME.
        /// </summary>
        /// <param name="env">Environment</param>
        /// <param name="root">Submodel or SME</param>
        /// <param name="recurseChilds">Recurse on child elements</param>
        /// <param name="repairSemIds">Will check if the type/ local of the semanticId of
        /// the source SMEs shall be adopted.</param>
        /// <returns>Tuple (#no valid id, #already present, #added) </returns>
        public Tuple<int, int, int> ImportCDsFromSmSme(
            Aas.IEnvironment env,
            Aas.IReferable root,
            bool recurseChilds = false,
            bool repairSemIds = false,
            bool adaptive61360 = false)
        {
            // access
            var noValidId = 0;
            var alreadyPresent = 0;
            var added = 0;

            if (env == null || root == null)
                return new Tuple<int, int, int>(noValidId, alreadyPresent, added);

            //
            // Part 0 : define a lambda
            //

            Action<Aas.IReference, Aas.IReferable> actionAddCD = (newid, rf) =>
            {
                if (newid == null || newid.Keys.Count() < 1)
                {
                    noValidId++;
                }
                else
                {
                    // repair semanticId
                    if (repairSemIds)
                    {
                        if (rf is Aas.Submodel rfsm && rfsm.SemanticId != null
                            && rfsm.SemanticId.Keys.Count() >= 1)
                        {
                            rfsm.SemanticId.Keys[0].Type = Aas.KeyTypes.Submodel;
                        }

                        if (rf is Aas.ISubmodelElement rfsme && rfsme.SemanticId != null
                            && rfsme.SemanticId.Keys.Count() >= 1)
                        {
                            rfsme.SemanticId.Keys[0].Type = Aas.KeyTypes.GlobalReference;
                        }
                    }

                    // ok?
                    if (newid.Keys.Count != 1)
                        return;

                    // id of new CD
                    var cdid = newid.Keys[0].Value;

                    // check if existing
                    var exCd = env.FindConceptDescriptionById(cdid);
                    if (exCd != null)
                    {
                        alreadyPresent++;
                    }
                    else
                    {
                        // create such CD
                        var cd = new Aas.ConceptDescription(cdid);
                        if (rf != null)
                        {
                            cd.IdShort = rf.IdShort;
                            if (rf.Description != null)
                                cd.Description = rf.Description.Copy();
                        }

                        // add more data?
                        if (adaptive61360)
                        {
                            cd.SetIEC61360Spec(
                                preferredNames: new[] { "en", "" + rf.IdShort },
                                definition: new[] { "en", "" + rf.Description?.GetDefaultString("en") });
                        }

                        // store in AAS enviroment
                        env.Add(cd);

                        // count and emit event
                        added++;
                        this.AddDiaryEntry(root, new DiaryEntryStructChange());
                    }
                }
            };

            //
            // Part 1 : semanticId of root
            //


            if (root is Aas.IHasSemantics rsmid)
                actionAddCD(rsmid.SemanticId, root as Aas.IReferable);


            //
            // Part 2 : semanticId of all children
            //

            if (recurseChilds)
                foreach (var child in root.Descend().OfType<Aas.ISubmodelElement>())
                    if (child is Aas.IHasSemantics rsmid2)
                        actionAddCD(rsmid2.SemanticId, child);

            // done
            return new Tuple<int, int, int>(noValidId, alreadyPresent, added);
        }

        //
        // Hinting
        //

        public void AddHintBubble(AnyUiStackPanel view, bool hintMode, HintCheck[] hints)
        {
            // access
            if (!hintMode || view == null || hints == null)
                return;

            // check, if something to do. Execute all predicates
            var textsToShow = new List<Tuple<string, HintCheck.Severity>>();
            foreach (var hc in hints)
                if (hc.CheckPred != null && hc.TextToShow != null)
                {
                    try
                    {
                        if (hc.CheckPred())
                        {
                            textsToShow.Add(new Tuple<string, HintCheck.Severity>(hc.TextToShow, hc.SeverityLevel));
                            if (hc.BreakIfTrue)
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        textsToShow.Add(new Tuple<string, HintCheck.Severity>(
                            $"Error while checking hints: {ex.Message} at {AdminShellUtil.ShortLocation(ex)}", 
                            HintCheck.Severity.High));
                    }
                }

            // some?
            if (textsToShow.Count < 1)
                return;

            // show!
            foreach (var tts in textsToShow)
            {
                var bubble = new AnyUiHintBubble();
                bubble.FontSize = 0.8f;
                bubble.Margin = new AnyUiThickness(2, 4, 2, 0);
                bubble.Text = tts.Item1;
                if (tts.Item2 == HintCheck.Severity.High)
                {
                    bubble.Background = levelColors?.HintSeverityHigh.Bg;
                    bubble.Foreground = levelColors?.HintSeverityHigh.Fg;
                }
                if (tts.Item2 == HintCheck.Severity.Notice)
                {
                    bubble.Background = levelColors?.HintSeverityNotice.Bg;
                    bubble.Foreground = levelColors?.HintSeverityNotice.Fg;
                }
                view.Children.Add(bubble);
            }
        }

        public void AddHintBubble(AnyUiStackPanel view, bool hintMode, HintCheck hint)
        {
            AddHintBubble(view, hintMode, new[] { hint });
        }

        public T[] ConcatArrays<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a == null && b == null)
                return null;

            if (a == null)
                return b.ToArray();

            if (b == null)
                return a.ToArray();

            var l = new List<T>();
            l.AddRange(a);
            l.AddRange(b);
            return l.ToArray();
        }

        public HintCheck[] ConcatHintChecks(IEnumerable<HintCheck> a, IEnumerable<HintCheck> b)
        {
            return ConcatArrays<HintCheck>(a, b);
        }

        //
        // Intermediate layer to handle modifications, event generation, marking of time stamps
        //

        /// <summary>
        /// Care for a pseudo-unqiue identification of the IReferable.
        /// Unique identification will be established by adding something such as "---74937434739"
        /// Note: not in <c>cs</c>, as considered part of business logic.
        /// Note: if <c>idShort</c> has content, will add unique content.
        /// </summary>
        /// <param name="rf">given IReferable</param>
        public void MakeNewReferableUnique(Aas.IReferable rf)
        {
            // access
            if (rf == null)
                return;

            // random add
            var r = new Random();
            var addStr = "---" + r.Next(0, 0x7fffffff).ToString("X8");

            // completely blank?
            if (rf.IdShort?.HasContent() != true)
            {
                // empty!
                rf.IdShort = rf.GetSelfDescription().AasElementName + addStr;
                return;
            }

            // already existing?
            var p = rf.IdShort.LastIndexOf("---", StringComparison.Ordinal);
            if (p >= 0)
            {
                rf.IdShort = rf.IdShort.Substring(0, p) + addStr;
                return;
            }

            // simply add
            rf.IdShort += addStr;
        }

        /// <summary>
        /// Care for a pseudo-unqiue identification of the Identifiable.
        /// Unique identification will be established by adding something such as "---74937434739"
        /// Note: not in <c>cs</c>, as considered part of business logic.
        /// Note: if <c>identification == null</c>, will create one.
        /// Note: if <c>identification</c> has content, will add unique content.
        /// </summary>
        /// <param name="idf">given Identifiable</param>
        public void MakeNewIdentifiableUnique(Aas.IIdentifiable idf)
        {
            // access
            if (idf == null)
                return;

            // random add
            var r = new Random();
            var addStr = "---" + r.Next(0, 0x7fffffff).ToString("X8");

            // completely blank?
            if (string.IsNullOrEmpty(idf.Id))
            {
                // empty!
                idf.Id = idf.GetSelfDescription().AasElementName + addStr;
                return;
            }

            // already existing?
            var p = idf.Id.LastIndexOf("---", StringComparison.Ordinal);
            if (p >= 0)
            {
                idf.Id = idf.Id.Substring(0, p) + addStr;
                return;
            }

            // simply add
            idf.Id += addStr;
        }

        /// <summary>
        /// This class tries to acquire element reference information, which is used by 
        /// <c>AddDiaryEntry</c>
        /// </summary>
        public class DiaryReference
        {
            public List<Aas.IKey> OriginalPath;

            public DiaryReference() { }

            public DiaryReference(Aas.IReferable rf)
            {
                OriginalPath = rf?.GetReference()?.Keys;
            }
        }

        /// <summary>
        /// Base class for diary entries, which are recorded with respect to a AAS element
        /// Diary entries contain a minimal set of information to later produce AAS events or such.
        /// </summary>
        public class DiaryEntryBase
        {
            public DateTime Timestamp;
        }

        /// <summary>
        /// Structural change of that AAS element
        /// </summary>
        public class DiaryEntryStructChange : DiaryEntryBase
        {
            public StructuralChangeReason Reason;
            public int CreateAtIndex = -1;

            public DiaryEntryStructChange(
                StructuralChangeReason reason = StructuralChangeReason.Modify,
                int createAtIndex = -1)
            {
                Reason = reason;
                CreateAtIndex = createAtIndex;
            }
        }

        /// <summary>
        /// Update value of that AAS element
        /// </summary>
        public class DiaryEntryUpdateValue : DiaryEntryBase
        {
        }

        /// <summary>
        /// Takes that diary information and correctly translate this to transaction of the AAS and its elements
        /// </summary>
        public void AddDiaryEntry(Aas.IReferable rf, DiaryEntryBase de,
            DiaryReference diaryReference = null,
            bool allChildrenAffected = false,
            Aas.IReferable explicitParent = null)
        {
            // trivial
            if (de == null)
                return;

            // tainting
            TaintedDataDef.TaintIdentifiable(rf);

            // structure?
            if (de is DiaryEntryStructChange desc)
            {
                // create
                var dataStr = "";
                try
                {
                    // transforming a (null) IClass crashes Transformer
                    if (rf == null)
                        dataStr = "null";
                    else
                        dataStr = Jsonization.Serialize.ToJsonObject(rf)
                            .ToJsonString(new System.Text.Json.JsonSerializerOptions());
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }

                var evi = new AasPayloadStructuralChangeItem(
                    DateTime.UtcNow, desc.Reason,
                    path: (rf as Aas.IReferable)?.GetReference()?.Keys,
                    createAtIndex: desc.CreateAtIndex,
                    // Assumption: models will be serialized correctly
                    // dead-csharp off
                    // data: JsonConvert.SerializeObject(rf));
                    // dead-csharp on
                    data: dataStr);

                if (diaryReference?.OriginalPath != null)
                    evi.Path = diaryReference.OriginalPath;

                // attach where?
                var attachRf = rf;
                if (rf != null && rf.Parent is Aas.IReferable parRf)
                    attachRf = parRf;
                if (explicitParent != null)
                    attachRf = explicitParent;

                // add 
                DiaryDataDef.AddAndSetTimestamps(attachRf, evi,
                    isCreate: desc.Reason == StructuralChangeReason.Create);
            }

            // update value?
            if (rf != null && de is DiaryEntryUpdateValue && rf is Aas.ISubmodelElement sme)
            {
                // create
                var evi = new AasPayloadUpdateValueItem(
                    path: (rf as Aas.IReferable)?.GetReference()?.Keys,
                    value: sme.ValueAsText());

                // TODO (MIHO, 2021-08-17): check if more SME types to serialize

                if (sme is Aas.Property p)
                    evi.ValueId = p.ValueId;

                if (sme is Aas.MultiLanguageProperty mlp)
                {
                    evi.Value = mlp.Value;
                    evi.ValueId = mlp.ValueId;
                }

                if (sme is Aas.Range rng)
                    evi.Value = new[] { rng.Min, rng.Max };

                // add 
                DiaryDataDef.AddAndSetTimestamps(rf, evi, isCreate: false);
            }

        }
    }
}
