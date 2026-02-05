using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Layouts;
using Newtonsoft.Json;

#if IOS || MACCATALYST
using UIKit;
#endif

namespace MauiTestTree
{
    public class AnyUiDisplayDataMaui : AnyUiDisplayDataBase
    {
        [JsonIgnore]
        public AnyUiDisplayContextMaui Context;

        [JsonIgnore]
        public VisualElement? MauiElement;

        [JsonIgnore]
        public bool EventsAdded;

        public AnyUiDisplayDataMaui(AnyUiDisplayContextMaui Context)
        {
            this.Context = Context;
        }

        /// <summary>
        /// Initiates a drop operation with one ore more files given by filenames.
        /// </summary>
        public override void DoDragDropFiles(AnyUiUIElement elem, string[] files)
        {
            // access 
            if (files == null || files.Length < 1)
                return;
            var sc = new System.Collections.Specialized.StringCollection();
            sc.AddRange(files);

#if TODO
            // WPF element
            var dd = elem?.DisplayData as AnyUiDisplayDataWpf;

            // start
            DataObject data = new DataObject();
            data.SetFileDropList(sc);

            // Inititate the drag-and-drop operation.
            DragDrop.DoDragDrop(dd?.WpfElement, data, DragDropEffects.Copy | DragDropEffects.Move);
#endif
        }

    }

    public class AnyUiIconFont
    {
        public string Short = "";

        /// <summary>
        /// Font alias to be used for MAUI controls
        /// </summary>
        public string FontAlias = "";
        public int FontSize;

#if WINDOWS

        /// <summary>
        /// Complete path for font location to be used by WinUI
        /// </summary>
        public string? FontLocationWin = "";

        /// <summary>
        /// Font Family as be used by WinUI
        /// </summary>
        public Microsoft.UI.Xaml.Media.FontFamily? FontFamilyWin;
#endif
    }

    public partial class AnyUiDisplayContextMaui : AnyUiContextPlusDialogs
    {
        /// <summary>
        /// Object which is able to open, close flyover panels
        /// </summary>
        [JsonIgnore]
        public IFlyoutProvider FlyoutProvider;

        /// <summary>
        /// Object which carries a UI dispatched. Used to convert sync to async method invocations
        /// for UI handline.
        /// </summary>
        [JsonIgnore]
        public BindableObject DispatchObject;

        [JsonIgnore]
        public PackageCentral Packages;

        public static string SessionSingletonMaui = "session-maui";

        public override IEnumerable<AnyUiContextCapability> EnumCapablities()
        {
            yield return AnyUiContextCapability.MAUI;
            yield return AnyUiContextCapability.DialogWithoutFlyover;
        }

        public AnyUiDisplayContextMaui(
            IFlyoutProvider flyoutProvider, BindableObject dispatchObject, PackageCentral packages)
        {
            FlyoutProvider = flyoutProvider;
            DispatchObject = dispatchObject;
            Packages = packages;
            InitRenderRecs();
        }

        protected List<AnyUiIconFont> IconFonts = new();

        public void TryRegisterIconFont(
            string shortId, 
            string fontAlias, 
            string? fontLocationWin,
            int fontSize)
        {
            var n = new AnyUiIconFont()
            {
                Short = shortId,
                FontAlias = fontAlias,
                FontSize = fontSize
            };

#if WINDOWS
            n.FontLocationWin = fontLocationWin;
            n.FontFamilyWin = new Microsoft.UI.Xaml.Media.FontFamily(n.FontLocationWin ?? n.FontAlias);
#endif

            IconFonts.Add(n);
        }

        public AnyUiIconFont? FindIconFontByShort(string shortId)
        {
            foreach (var fo in IconFonts)
                if (fo.Short == shortId)
                    return fo;
            return null;
        }

        public AnyUiIconFont? FindIconFontByAlias(string alias)
        {
            foreach (var fo in IconFonts)
                if (fo.FontAlias == alias)
                    return fo;
            return null;
        }

        public class IconSourceResolveResult
        {
            public string FontAlias = "";
            public AnyUiColor IconColor = AnyUiColors.Transparent;
        }

        public Func<AnyUiDisplayContextMaui, AnyUiImageSourceFont, IconSourceResolveResult?>? LambdaResolveImageSourceFont = null;

        public static Color GetMauiColor(AnyUiColor? c)
        {
            if (c == null)
                return Colors.Transparent;
            return Color.FromRgba(c.R, c.G, c.B, c.A);
        }

        public static SolidColorBrush GetMauiBrush(AnyUiColor c)
        {
            if (c == null)
                return Brush.Transparent;
            return new SolidColorBrush(Color.FromRgba(c.R, c.G, c.B, c.A));
        }

        public static SolidColorBrush GetMauiBrush(AnyUiBrush br)
        {
            if (br == null)
                return Brush.Transparent;
            var c = br.Color;
            return new SolidColorBrush(Color.FromRgba(c.R, c.G, c.B, c.A));
        }

        public static AnyUiColor GetAnyUiColor(Color c)
        {
            return AnyUiColor.FromArgb(
                (byte)Math.Round(255.0 * c.Alpha),
                (byte)Math.Round(255.0 * c.Red),
                (byte)Math.Round(255.0 * c.Green),
                (byte)Math.Round(255.0 * c.Blue));
        }

        public static AnyUiColor GetAnyUiColor(SolidColorBrush br)
        {
            if (br == null)
                return AnyUiColors.Default;
            return GetAnyUiColor(br.Color);
        }

        public static AnyUiBrush GetAnyUiBrush(Color c)
        {
            if (c == null)
                return AnyUiBrushes.Transparent;
            return new AnyUiBrush(GetAnyUiColor(c));
        }

        public static AnyUiBrush GetAnyUiBrush(SolidColorBrush br)
        {
            if (br == null)
                return AnyUiBrushes.Transparent;
            return new AnyUiBrush(GetAnyUiColor(br.Color));
        }

        public GridUnitType GetGridUnitType(AnyUiGridUnitType gut)
        {
            switch(gut)
            {
                case AnyUiGridUnitType.Auto: return GridUnitType.Auto;
                case AnyUiGridUnitType.Pixel: return GridUnitType.Absolute;
                case AnyUiGridUnitType.Star: return GridUnitType.Star;
            }
            return GridUnitType.Auto;
        }

        public GridLength GetMauiGridLength(AnyUiGridLength gl, RenderDefaults? rd = null)
        {
            if (gl == null)
                return GridLength.Auto;
            return new GridLength(GetLengthFromRelative(rd, gl.Value), GetGridUnitType(gl.Type));
        }

        public ColumnDefinition GetMauiColumnDefinition(AnyUiColumnDefinition cd, RenderDefaults? rd = null)
        {
            var res = new ColumnDefinition();
            if (cd?.Width != null)
                res.Width = GetMauiGridLength(cd.Width, rd);
#if TODO_IMPORTANT
            if (cd?.MinWidth.HasValue == true)
                res.MinWidth = cd.MinWidth.Value;
            if (cd?.MaxWidth.HasValue == true)
                res.MaxWidth = cd.MaxWidth.Value;
#endif
            return res;
        }

        public RowDefinition GetMauiRowDefinition(AnyUiRowDefinition rwd, RenderDefaults? rd = null)
        {
            var res = new RowDefinition();
            if (rwd?.Height != null)
                res.Height = GetMauiGridLength(rwd.Height, rd);
#if TODO_IMPORTANT
            if (rd?.MinHeight.HasValue == true)
                res.MinHeight = rd.MinHeight.Value;
#endif
            return res;
        }

        public Thickness GetMauiTickness(AnyUiThickness tn)
        {
            if (tn == null)
                return new Thickness(0);
            return new Thickness(tn.Left, tn.Top, tn.Right, tn.Bottom);
        }

        public double GetDoubleTickness(AnyUiThickness tn)
        {
            if (tn == null)
                return 0.0;
            return (tn.Left + tn.Top + tn.Right + tn.Bottom) / 4.0;
        }

        public FontWeight GetFontWeight(AnyUiFontWeight wt)
        {
            FontWeight res = (FontWeight)FontWeights.Normal;
            if (wt == AnyUiFontWeight.Bold)
                res = (FontWeight)FontWeights.Bold;
            return res;
        }

        public Point GetMauiPoint(AnyUiPoint p)
        {
            return new Point(p.X, p.Y);
        }

        public AnyUiPoint GetAnyUiPoint(Point p)
        {
            return new AnyUiPoint(p.X, p.Y);
        }

        public LayoutOptions GetLayoutOptions(AnyUiVerticalAlignment va)
        {
            switch (va)
            {
                case AnyUiVerticalAlignment.Top: return LayoutOptions.Start;
                case AnyUiVerticalAlignment.Bottom: return LayoutOptions.End;
                case AnyUiVerticalAlignment.Center: return LayoutOptions.Center;
                case AnyUiVerticalAlignment.Stretch: return LayoutOptions.Fill;
            }
            return LayoutOptions.Start;
        }

        public LayoutOptions GetLayoutOptions(AnyUiHorizontalAlignment va)
        {
            switch (va)
            {
                case AnyUiHorizontalAlignment.Left: return LayoutOptions.Start;
                case AnyUiHorizontalAlignment.Right: return LayoutOptions.End;
                case AnyUiHorizontalAlignment.Center: return LayoutOptions.Center;
                case AnyUiHorizontalAlignment.Stretch: return LayoutOptions.Fill;
            }
            return LayoutOptions.Start;
        }

        public TextAlignment GetTextAlignment(AnyUiVerticalAlignment va)
        {
            switch (va)
            {
                case AnyUiVerticalAlignment.Top: return TextAlignment.Start;
                case AnyUiVerticalAlignment.Center: return TextAlignment.Center;
                case AnyUiVerticalAlignment.Bottom: return TextAlignment.End;
                case AnyUiVerticalAlignment.Stretch: return TextAlignment.Justify;
            }
            return TextAlignment.Start;
        }

        public TextAlignment GetTextAlignment(AnyUiHorizontalAlignment va)
        {
            switch (va)
            {
                case AnyUiHorizontalAlignment.Left: return TextAlignment.Start;
                case AnyUiHorizontalAlignment.Center: return TextAlignment.Center;
                case AnyUiHorizontalAlignment.Right: return TextAlignment.End;
                case AnyUiHorizontalAlignment.Stretch: return TextAlignment.Justify;
            }
            return TextAlignment.Start;
        }

        public ScrollBarVisibility GetScrollBarVisibility(AnyUiScrollBarVisibility vis)
        {
            switch (vis)
            {
                case AnyUiScrollBarVisibility.Disabled: return ScrollBarVisibility.Never;
                case AnyUiScrollBarVisibility.Hidden: return ScrollBarVisibility.Never;
                case AnyUiScrollBarVisibility.Visible: return ScrollBarVisibility.Always;
                case AnyUiScrollBarVisibility.Auto: return ScrollBarVisibility.Default;
            }
            return ScrollBarVisibility.Default;
        }

        public FontAttributes GetFontAttributesFrom(AnyUiFontWeight fw)
        {
            switch (fw)
            {
                case AnyUiFontWeight.Normal: return FontAttributes.None;
                case AnyUiFontWeight.Bold: return FontAttributes.Bold;
            }
            return FontAttributes.None;
        }

        public LineBreakMode GetLineBreakMode(AnyUiTextWrapping tw)
        {
            switch (tw)
            {
                case AnyUiTextWrapping.NoWrap: return LineBreakMode.NoWrap;
                case AnyUiTextWrapping.WrapWithOverflow: return LineBreakMode.WordWrap;
                case AnyUiTextWrapping.Wrap: return LineBreakMode.CharacterWrap;
            }
            return LineBreakMode.NoWrap;
        }

        public double GetFontSizeFromRelative(double rel)
        {
            // TODO
#pragma warning disable CS0612 // Typ oder Element ist veraltet
            var normalSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label));
#pragma warning restore CS0612 // Typ oder Element ist veraltet
            return normalSize * rel;
        }

        public double GetFontSizeFromRelative(
            RenderDefaults? rd, 
            params double?[] relFactor)
        {
            // access, start of chain
            if (rd == null)
                return -1;
            var res = rd.FontSizeNormal;

            if (rd?.FontSizeRel != null)
                res *= rd.FontSizeRel.Value;

            foreach (var rf in relFactor)
                if (rf.HasValue)
                    res *= rf.Value;

            return res;
        }

        public double GetLengthFromRelative(
            RenderDefaults? rd,
            double input)
        {
            // access
            if (rd?.FontSizeRel == null)
                return input;

            // ok
            return rd.FontSizeRel.Value * input;
        }
            

        //
        // Handling of outside actions
        //

        [JsonIgnore]
        public List<AnyUiLambdaActionBase> WishForOutsideAction = new List<AnyUiLambdaActionBase>();

        /// <summary>
        /// This function is called from multiple places inside this class to emit an labda action
        /// to the superior logic of the application
        /// </summary>
        /// <param name="action"></param>
        public override void EmitOutsideAction(AnyUiLambdaActionBase? action)
        {
            if (action == null || action is AnyUiLambdaActionNone)
                return;
            WishForOutsideAction.Add(action);
        }

        //
        // Render records: mapping AnyUi-Widgets to MAUI controls
        //
        // see: AnyUiMaui_RenderRecs.cs
        // 

        //
        // Creation of MAUI hierarchies
        //

        public VisualElement? GetOrCreateMauiElement(
            AnyUiUIElement? el,
            Type? superType = null,
            bool allowCreate = true,
            bool allowReUse = true,
            AnyUiRenderMode mode = AnyUiRenderMode.All,
            RenderDefaults? renderDefaults = null,
            Dictionary<AnyUiUIElement, bool>? updateElemsOnly = null)
        {
            // access
            if (el == null)
                return null;

            // have data attached to cntl
            if (el.DisplayData == null)
                el.DisplayData = new AnyUiDisplayDataMaui(this);
            var dd = el?.DisplayData as AnyUiDisplayDataMaui;
            if (dd == null)
                return null;

            // most specialized class or in recursion/ creation of base classes?
            var topClass = superType == null;

            // identify render rec
            var searchType = (superType != null) ? superType : el?.GetType();

            if (el?.GetType() == typeof(AnyUiTextBox))
                ;

            // TODO: Multiple possibilities with a check lambda .. vert|hor
            var foundRR = RenderRecs.FindAnyUiCntl(searchType, el);
            if (foundRR == null || foundRR.GetMauiType == null)
                return null;

            // special case: update status only
            if (mode == AnyUiRenderMode.StatusToUi
                && dd.MauiElement != null && allowReUse && topClass)
            {
                // itself
                if (el!.Touched)
                {
                    // perform a "minimal" render action
                    // recurse (first) in the base types ..
                    var bt2 = searchType!.BaseType;
                    if (bt2 != null)
                        GetOrCreateMauiElement(el, superType: bt2, allowReUse: true,
                            mode: AnyUiRenderMode.StatusToUi, renderDefaults: renderDefaults,
                            updateElemsOnly: updateElemsOnly);

                    if (updateElemsOnly == null || updateElemsOnly.ContainsKey(el))
                        foundRR.InitLambda?.Invoke(el, dd.MauiElement, AnyUiRenderMode.StatusToUi, renderDefaults);
                }
                el.Touched = false;

                // recurse into
                if (el is AnyUi.IEnumerateChildren ien)
                    foreach (var elch in ien.GetChildren())
                        GetOrCreateMauiElement(elch, allowCreate: false, allowReUse: true,
                            mode: AnyUiRenderMode.StatusToUi,
                            renderDefaults: renderDefaults,
                            updateElemsOnly: updateElemsOnly);

                // return (effectively TOP element)
                return dd.MauiElement;
            }

            // special case: return, if already created and not (still) in recursion/ creation of base classes
            if (dd.MauiElement != null && allowReUse && topClass)
                return dd.MauiElement;
            if (!allowCreate)
                return null;

            // create MauiElement accordingly?
            //// Note: excluded from condition: dd.WpfElement == null
            var mauiType = foundRR.GetMauiType?.Invoke(
                    renderDefaults?.WidgetToolSet == null ? RenderWidgetToolSet.Normal : renderDefaults.WidgetToolSet,
                    el);
            if (mauiType == null)
                return null;
            if (topClass)
                dd.MauiElement = (VisualElement?)Activator.CreateInstance(mauiType);
            if (dd.MauiElement == null)
                return null;

            // recurse (first) in the base types ..
            if (el is AnyUiBorder)
                ;

            var bt = searchType!.BaseType;
            if (bt != null)
                GetOrCreateMauiElement(el, superType: bt,
                    allowReUse: allowReUse, renderDefaults: renderDefaults,
                    updateElemsOnly: updateElemsOnly);

            // perform the render action (for this level of attributes, second)
            if (updateElemsOnly == null || updateElemsOnly.ContainsKey(el!))
                foundRR.InitLambda?.Invoke(el!, dd.MauiElement, AnyUiRenderMode.All, renderDefaults);

            // does the element need child elements?
            // do a special case handling here, unless a more generic handling is required

            {
                if (el is AnyUiScrollViewer cntl && dd.MauiElement is ScrollView maui
                    && cntl.Content != null)
                {
                    maui.Content = GetOrCreateMauiElement(cntl.Content,
                        allowReUse: allowReUse, renderDefaults: renderDefaults,
                        updateElemsOnly: updateElemsOnly) as View;
                }
            }

            // does the element need child elements?
            // do a special case handling here, unless a more generic handling is required

            // MIHO+OZ
            //// 
            ////    if (el is AnyUiBorder cntl && dd.WpfElement is Border wpf
            ////        && cntl.Child != null)
            ////    
            ////        wpf.Content = GetOrCreateWpfElement(cntl.Content, allowReUse: allowReUse)
            ////    
            //// 
            //

            // call action
            if (topClass)
            {
                UIElementWasRendered(el!, dd.MauiElement);
            }

            // result
            return dd.MauiElement;
        }

        //
        // Tag information
        //

        protected List<AnyUiUIElement> _namedElements = new List<AnyUiUIElement>();

        public int PrepareNameList(AnyUiUIElement root)
        {
            _namedElements = new List<AnyUiUIElement>();
            if (root == null)
                return 0;
            _namedElements = root.FindAllNamed().ToList();
            return _namedElements.Count;
        }

        public AnyUiUIElement? FindFirstNamedElement(string name)
        {
            if (_namedElements == null || name == null)
                return null;
            foreach (var el in _namedElements)
                if (el.Name?.Trim()?.ToLower() == name.Trim().ToLower())
                    return el;
            return null;
        }

        //
        // Show of context menues on MAUI (involves platform specific code)
        //

        /// <summary>
        /// This wrapper bundles the calling convention for the async / sync platform specific
        /// implementations of showing a context menu.
        /// Note: This overloaded variant is mostly for internal use when rendering controls.
        /// </summary>
        public async Task<int?> MauiShowContextMenuForControlWrapper(
            View? mauiCntl,
            AnyUiSpecialActionContextMenu cntlcm)
        {
            // make a independent view model for the context menu
            var vm = ContextMenuSubstituteViewModel.CreateNew(cntlcm.MenuItemHeaders, this, scaleFontSize: 1.2);

            var res = await ShowContextMenuForControlAsync(this, vm, mauiCntl);
            if (!res.HasValue)
                res = await ShowContextMenuForControlSync(this, vm, mauiCntl);

            return res;
        }

        /// <summary>
        /// This wrapper bundles the calling convention for the async / sync platform specific
        /// implementations of showing a context menu.
        /// Note: External use
        /// </summary>
        public async Task<int?> MauiShowContextMenuForControlWrapper(
            View? mauiCntl,
            ContextMenuSubstituteViewModel? vm)
        {
            // access
            if (vm == null || mauiCntl == null)
                return null;

            var res = await ShowContextMenuForControlAsync(this, vm, mauiCntl);
            if (!res.HasValue)
                res = await ShowContextMenuForControlSync(this, vm, mauiCntl);

            return res;
        }

#if WINDOWS
        protected static Microsoft.UI.Xaml.Controls.IconElement? ContextMenu_CreateIcon(
            AnyUiDisplayContextMaui dc, string? iconText, string? fontFamily = null)
        {
            // access
            if (iconText == null)
                return null;

            // recognize icon font
            AnyUiIconFont? fo = null;
            string? glyph = null;
            if (fontFamily != null)
            {
                fo = dc.FindIconFontByShort(fontFamily);
                glyph = iconText;
            }
            else
            {
                fo = dc.FindIconFontByShort("uc");
                glyph = iconText;
            }
                
            if (fo?.FontFamilyWin != null && glyph != null)
            {
                // ChatGPT: check, if a Label would be more reliable
                return new Microsoft.UI.Xaml.Controls.FontIcon
                {
                    Glyph = glyph,
                    FontFamily = fo.FontFamilyWin,
                    FontSize = fo.FontSize
                };
            }

            return null;
        }
#endif

        // Note: Some of the code needs async, some not ..

        protected static async Task<int?> ShowContextMenuForControlAsync(
            AnyUiDisplayContextMaui? dc,
            ContextMenuSubstituteViewModel vm,
            View? mauiCntl)
        {
            // access
            if (vm?.Items == null || vm.Items.Count < 1)
                return null;

            await Task.Yield();

            //
            // Android -> Custom dialogue
            //

#if ANDROID

            // generate modal page and start
            //var pickerPage = new ContextMenuSubstitute(
            //    ContextMenuSubstituteViewModel.GetFromPairsOfString(cntlcm.MenuItemHeaders, dc, scaleFontSize: 2.0));
            //await Application.Current!.Windows[0]!.Navigation.PushModalAsync(pickerPage);

            // var vm = ContextMenuSubstituteViewModel.GetFromPairsOfString(cntlcm.MenuItemHeaders, dc, scaleFontSize: 1.2);
            var uc = new ContextMenuPopup(vm);
            await Shell.Current.ShowPopupAsync(uc, new CommunityToolkit.Maui.PopupOptions() { 
                Shape = new RoundRectangle() { 
                    CornerRadius = 16,
                    Stroke = Colors.Transparent,
                    StrokeThickness = 0.0
                }
            });
            return uc.Result?.Index;

#endif

            return null;
        }

        protected static Task<int?> ShowContextMenuForControlSync(
            AnyUiDisplayContextMaui? dc,
            ContextMenuSubstituteViewModel vm,
            View? mauiCntl)
        {
            // awaitable task completion
            var tcs = new TaskCompletionSource<int?>();

            // access
            if (dc == null || mauiCntl == null)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

#if WINDOWS
            //
            // Windows
            //

            Microsoft.UI.Xaml.FrameworkElement? fe = null;
            // original
            if (mauiCntl is Button mauiButton && mauiButton.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button winButton)
                fe = winButton;
            // alternative
            if (mauiCntl is Border mauiBorder && mauiBorder.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement winFe)
                fe = winFe;


            if (fe == null)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            var flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();

            //var fontFamilies = System.Drawing.FontFamily.Families;
            //foreach (var family in fontFamilies)
            //{
            //    Trace.WriteLine(family.Name);
            //}

            for (int i=0; i<vm.Items.Count; i++)
            {
                // independent menu item
                var mi = vm.Items[i];
                if (mi == null)
                    continue;

                // menu item itself
                var menuItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
                {
                    Text = "" + mi.Header,
                    // Icon = ContextMenu_CreateIcon(dc, mi.IconGlyph, mi.IconFontAlias),
                    Tag = i
                };

                if (mi.IconGlyph != null && mi.IconFontAlias != null)
                {
                    var font = dc.FindIconFontByAlias(mi.IconFontAlias);
                    if (font?.FontFamilyWin != null)
                        // ChatGPT: replace with Label for better reliability?
                        menuItem.Icon = new Microsoft.UI.Xaml.Controls.FontIcon()
                        {
                            Glyph = mi.IconGlyph,
                            FontFamily = font.FontFamilyWin,
                            FontSize = mi.IconFontSize
                        };  
                }

                var thisI = mi.Index;
                menuItem.Click += (_, _) =>
                {
                    tcs.TrySetResult(thisI);
                };

                flyout.Items.Add(menuItem);
            }

            flyout.Closed += (_, _) =>
            {
                tcs.TrySetResult(null); // dismissed
            };

            flyout.ShowAt(fe /* winButton */, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions() { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft });

            return tcs.Task;
#endif

#if IOS || MACCATALYST

            var handler = myView.Handler;
            if (handler?.PlatformView is not UIView uiView)
                return;

            var actions = MyDataEntries.Select(entry =>
                UIAction.Create(
                    entry.Text,
                    entry.Image,   // optional UIImage
                    _ => HandleMenuClick(entry)
                )
            ).ToArray();

            var menu = UIMenu.Create(actions);

            uiView.ShowMenu(menu);
#endif

            // no?
            tcs.SetResult(null);
            return tcs.Task;

        }

        //
        // Shortcut handling
        //

        public class KeyShortcutRecord
        {
            public AnyUiUIElement? Element;
            public KeyboardAcceleratorModifiers Modifiers;
            public string? Key;
            public bool Preview = true;
            public string Info = "";

            public string GestureToString(int fmt)
            {
                if (fmt == 1)
                {
                    var res = "";
                    if (Modifiers.HasFlag(KeyboardAcceleratorModifiers.Shift))
                        res += "[Shift] ";
                    if (Modifiers.HasFlag(KeyboardAcceleratorModifiers.Ctrl))
                        res += "[Control] ";
                    if (Modifiers.HasFlag(KeyboardAcceleratorModifiers.Alt))
                        res += "[Alt] ";

                    res += "[" + Key.ToString() + "]";
                    return res;
                }
                else
                {
                    var l = new List<string>();
                    if (Modifiers.HasFlag(KeyboardAcceleratorModifiers.Shift))
                        l.Add("Shift");
                    if (Modifiers.HasFlag(KeyboardAcceleratorModifiers.Ctrl))
                        l.Add("Ctrl");
                    if (Modifiers.HasFlag(KeyboardAcceleratorModifiers.Alt))
                        l.Add("Alt");
                    l.Add(Key.ToString());
                    return String.Join("+", l);
                }
            }
        }

        private List<KeyShortcutRecord> _keyShortcuts = new List<KeyShortcutRecord>();

        public List<KeyShortcutRecord> KeyShortcuts { get { return _keyShortcuts; } }

        public bool RegisterKeyShortcut(
            string name,
            KeyboardAcceleratorModifiers modifiers,
            string key,
            string info)
        {
            var el = FindFirstNamedElement(name);
            if (el == null)
                return false;
            _keyShortcuts.Add(new KeyShortcutRecord()
            {
                Element = el,
                Modifiers = modifiers,
                Key = key,
                Info = info
            });
            return true;
        }

        // TODO
        public async Task<int> TriggerKeyShortcutAsync(
            string key,
            KeyboardAcceleratorModifiers modifiers,
            bool preview)
        {
            var res = 0;
            if (_keyShortcuts == null)
                return res;
            foreach (var sc in _keyShortcuts)
                if (key == sc.Key && modifiers == sc.Modifiers && preview == sc.Preview)
                {
                    // found, any lambdas appicable?
                    if (sc.Element is AnyUiButton btn && btn?.setValueAsyncLambda != null)
                    {
                        var action = await btn.setValueAsyncLambda.Invoke(btn);
                        EmitOutsideAction(action);
                        res++;
                    }
                }
            return res;
        }

        //
        // Utilities
        //

        /// <summary>
        /// Graphically highlights/ marks an element to be "selected", e.g for seacg/ replace
        /// operations.
        /// </summary>
        /// <param name="el">AnyUiElement</param>
        /// <param name="highlighted">True for highlighted, set for clear state</param>
        public override void HighlightElement(AnyUiFrameworkElement el, bool highlighted)
        {
#if TODO_IMPORTANT
            // access 
            if (el == null)
                return;
            var dd = el?.DisplayData as AnyUiDisplayDataWpf;
            if (dd?.WpfElement == null)
                return;

            // renderRec?
            var foundRR = RenderRecs.FindAnyUiCntl(el.GetType());
            if (foundRR?.HighlightLambda == null)
                return;

            // perform the render action (for this level of attributes, second)
            foundRR.HighlightLambda.Invoke(el, dd.WpfElement, highlighted);
#endif
        }

        public void UIElementWasRendered(AnyUiUIElement AnyUi, VisualElement el)
        {
        }

        /// <summary>
        /// Tries to revert changes in some controls.
        /// </summary>
        /// <returns>True, if changes were applied</returns>
        public override bool CallUndoChanges(AnyUiUIElement root)
        {
#if TODO_IMPORTANT
            var res = false;

            // recurse?
            if (root is AnyUiPanel panel)
                if (panel.Children != null)
                    foreach (var ch in panel.Children)
                        res = res || CallUndoChanges(ch);

            // can do something
            if (root is AnyUiTextBox cntl && cntl.DisplayData is AnyUiDisplayDataWpf dd
                && dd?.WpfElement is TextBox tb && cntl.originalValue != null)
            {
                tb.Text = cntl.originalValue as string;
                res = true;
            }

            // some changes
            return res;
#endif
            return false;
        }

        /// <summary>
        /// If supported by implementation technology, will set Clipboard (copy/ paste buffer)
        /// of the main application computer.
        /// </summary>
        public override async Task ClipboardSetAsync(AnyUiClipboardData cb)
        {
            // Note: Watermark not migrated to MAUI

            if (cb == null)
                return;

            if (cb.Text != null)
                await Clipboard.Default.SetTextAsync(cb.Text);
        }

        /// <summary>
        /// If supported by implementation technology, will get Clipboard (copy/ paste buffer)
        /// of the main application computer.
        /// </summary>
        public override async Task<AnyUiClipboardData> ClipboardGetAsync()
        {
            // Note: Watermark not migrated to MAUI

            var res = new AnyUiClipboardData();
            if (Clipboard.Default.HasText)
            {
                var text = await Clipboard.Default.GetTextAsync();
                if (text != null)
                {
                    res.Text = text;
                }
            }
            return res;
        }

        /// <summary>
        /// Returns the selected items in the tree, which are provided by the implementation technology
        /// (derived class of this).
        /// Note: these would be of type <c>VisualElementGeneric</c>, but is in other assembly.
        /// </summary>
        /// <returns></returns>
        public override List<IAnyUiSelectedItem>? GetSelectedItems()
        {
            return null;
        }

        /// <summary>
        /// Show MessageBoxFlyout with contents
        /// </summary>
        /// <param name="message">Message on the main screen</param>
        /// <param name="caption">Caption string (title)</param>
        /// <param name="buttons">Buttons according to WPF standard messagebox</param>
        /// <param name="image">Image according to WPF standard messagebox</param>
        /// <returns></returns>
        public override async Task<AnyUiMessageBoxResult> MessageBoxFlyoutShowAsync(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            await Task.Yield();
            if (FlyoutProvider == null)
                return AnyUiMessageBoxResult.Cancel;
            return await FlyoutProvider.MessageBoxFlyoutShowAsync(message, caption, buttons, image);
        }

        private VisualElement? DispatchFlyout(AnyUiDialogueDataBase dialogueData)
        {
            // access
            if (dialogueData == null)
                return null;
            
            VisualElement? res = null;

            // dispatch
            // TODO (MIHO, 2020-12-21): can be realized without tedious central dispatch?
            if (dialogueData is AnyUiDialogueDataEmpty ddem)
            {
                var uc = new EmptyFlyoutPage();
                uc.DiaData = ddem;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataModalPanel ddmp)
            {
                return new ModalPanelFlyoutPage(this, ddmp);
            }

            if (dialogueData is AnyUiDialogueDataOpenFile ddof)
            {
                // see below: PerformSpecialOps()
                var uc = new EmptyFlyoutPage();
                uc.DiaData = ddof;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSaveFile ddsf)
            {
                // see below: PerformSpecialOps()
                var uc = new EmptyFlyoutPage();
                uc.DiaData = ddsf;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataTextBox ddtb)
            {
                res = new TextBoxFlyoutPage(ddtb);
            }

#if TODO_IMPORTANT
            if (dialogueData is AnyUiDialogueDataChangeElementAttributes ddcea)
            {
                var uc = new ModalPanelFlyout(this);
                uc.DiaData = ChangeElementAttributesFlyoutAnyUiFlyout.CreateModelDialogue(ddcea);
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectEclassEntity ddec)
            {
                var uc = new SelectEclassEntityFlyout();
                uc.DiaData = ddec;
                res = uc;
            }
#endif

            if (dialogueData is AnyUiDialogueDataTextEditor ddte)
            {
                res = new TextEditorFlyoutPage(this, ddte);
            }

#if TODO_IMPORTANT
            if (dialogueData is AnyUiDialogueDataLogMessage ddsc)
            {
                var uc = new LogMessageFlyout(ddsc.Caption, "");
                uc.DiaData = ddsc;
                res = uc;
            }
#endif

            if (dialogueData is AnyUiDialogueDataSelectFromList ddsl)
            {
                res = new SelectFromListFlyoutPage(this, ddsl);
            }

#if TODO_IMPORTANT
            if (dialogueData is AnyUiDialogueDataSelectFromDataGrid ddsdg)
            {
                var uc = new SelectFromDataGridFlyout();
                uc.DiaData = ddsdg;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectAasEntity ddsa)
            {
                var uc = new SelectAasEntityFlyout(Packages);
                uc.DiaData = ddsa;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataSelectReferableFromPool ddrf)
            {
                var uc = new SelectFromReferablesPoolFlyout(AasxPredefinedConcepts.DefinitionsPool.Static);
                uc.DiaData = ddrf;
                res = uc;
            }
#endif

            if (dialogueData is AnyUiDialogueDataSelectFromRepository ddfr)
            {
                return new SelectFromRepositoryFlyoutPage(ddfr);
            }

#if TODO_IMPORTANT
            if (dialogueData is AnyUiDialogueDataSelectQualifierPreset ddsq)
            {
                var fullfn = System.IO.Path.GetFullPath(Options.Curr.QualifiersFile);
                var uc = new SelectQualifierPresetFlyout(fullfn);
                uc.DiaData = ddsq;
                res = uc;
            }

            if (dialogueData is AnyUiDialogueDataProgress ddpr)
            {
                var uc = new ProgressBarFlyout();
                uc.DiaData = ddpr;
                res = uc;
            }
#endif
            return res;
        }

        public static FilePickerFileType? GetMauiFilePickerFileTypeFromWpfFilter(string? filter)
        {
            // access
            if (filter == null)
                return null;

            // we shall have 4 plattform specific lists (nott kidding)
            var listWinUI = new List<string>();
            var listAndroid = new List<string>();
            var listIosMac = new List<string>();

            // we need some dictionaries
            var dictAndroid = new Dictionary<string, string[]>() {
                { "txt", new [] { "text/plain" }},
                { "text", new [] { "text/plain" }},
                { "json", new [] { "application/json" }},
                { "xml", new [] { "text/xml" }},
                { "png", new [] { "image/png" }},
                { "jpg", new [] { "image/jpeg" }},
                { "jpeg", new [] { "image/jpeg" }},
                { "bmp", new [] { "image/bmp" }},
                { "md", new [] { "text/markdown", "text/plain" }}
            };

            var dictIos = new Dictionary<string, string[]>() {
                { "txt", new [] { "public.plain-text" }},
                { "text", new [] { "public.plain-text" }},
                { "json", new [] { "public.json" }},
                { "xml", new [] { "public.xml" }},
                { "png", new [] { "public.image" }},
                { "jpg", new [] { "public.image" }},
                { "jpeg", new [] { "public.image" }},
                { "bmp", new [] { "public.image" }},
                { "md", new [] { "net.daringfireball.markdown", "public.plain-text" }}
            };

            // try decompose the filter into pairs of 2 strings
            var parts = filter.Split('|', StringSplitOptions.TrimEntries);
            for (int i=0; i<parts.Length / 2; i++)
            {
                // real filter?
                var p = parts[2 * i + 1];
                if (!p.StartsWith("*."))
                    continue;

                // ok, WinUI is simple, e.g. ".json", ".xml"
                var work = p.Substring(1);
                if (work != ".*")
                    listWinUI.Add(work);

                // ok, iOS is simple but weird, e.g. "public.json", "public.xml" or any: "public.data"
                // some for MAC
                // Note: Apple defines a large set of system UTTypes, and most of them live in the public namespace
                if (p.Length > 2)
                {
                    work = p.Substring(2).ToLowerInvariant();
                    
                    if (dictIos.ContainsKey(work))
                        foreach (var x in dictIos[work])
                            listIosMac.Add(x);
                    else
                        if (!listIosMac.Contains("public.data"))
                            listIosMac.Add("public.data");
                }

                // same principle for Android
                if (p.Length > 2)
                {
                    work = p.Substring(2).ToLowerInvariant();
                    
                    if (dictAndroid.ContainsKey(work))
                        foreach (var x in dictAndroid[work])
                            listAndroid.Add(x);
                    else
                        if (!listAndroid.Contains("*/*"))
                            listAndroid.Add("*/*");
                }
            }

            // now produce it
            return new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, listWinUI },
                { DevicePlatform.Android, listAndroid },
                { DevicePlatform.iOS, listIosMac.ToList() },
                { DevicePlatform.MacCatalyst, listIosMac.ToList() }
            });
        }

        private async Task PerformSpecialOpsAsync(bool modal, AnyUiDialogueDataBase dialogueData)
        {
            await Task.Yield();

            if (modal && dialogueData is AnyUiDialogueDataOpenFile ddof)
            {
                // try to derive a options
                var options = new PickOptions
                {
                    PickerTitle = ddof.Caption,
                    FileTypes = GetMauiFilePickerFileTypeFromWpfFilter(ddof.Filter)
                };

                if (!ddof.Multiselect)
                {
                    var result = await FilePicker.Default.PickAsync(options);

                    if (result != null)
                    {
                        ddof.Result = true;
                        ddof.ResultUserFile = false;
                        ddof.OriginalFileName = result.FullPath;
                        ddof.TargetFileName = result.FullPath;
                    }
                }
                else
                {
                    var result = await FilePicker.Default.PickMultipleAsync(options);

                    if (result != null && result.Count() > 0)
                    {
                        ddof.Result = true;
                        ddof.ResultUserFile = false;
                        ddof.Filenames = result.Select((r) => r.FullPath).ToList();
                    }
                }
            }

            if (modal && dialogueData is AnyUiDialogueDataSaveFile ddsf)
            {

                ;

                //var result = await CommunityToolkit.Maui.Storage.FileSaver.SaveAsync .Default.SaveAsync(
                //                "data.json",
                //                stream,
                //                new CancellationToken());

                //if (result.IsSuccessful)
                //{


                //    var options = new PickOptions
                //{
                //    PickerTitle = ddsf.Caption,
                //    FileTypes = GetMauiFilePickerFileTypeFromWpfFilter(ddsf.Filter)
                //};

                //var result = await FilePicker.Default. (options);

                //if (result != null)
                //{
                //    ddsf.Result = true;
                //    ddsf.Location = AnyUiDialogueDataSaveFile.LocationKind.Local;
                //    ddsf.TargetFileName = result.FullPath;
                //}

#if TODO_IMPORTANT
                var dlg = new Microsoft.Win32.SaveFileDialog();

                if (ddsf.Filter != null)
                    dlg.Filter = ddsf.Filter;
                if (ddsf.ProposeFileName != null)
                    dlg.FileName = ddsf.ProposeFileName;
                if (ddsf.TargetFileName != null)
                    dlg.FileName = ddsf.TargetFileName;

                var idir = System.IO.Path.GetDirectoryName(dlg.FileName);
                if (idir.HasContent())
                {
                    dlg.InitialDirectory = idir;
                    dlg.FileName = System.IO.Path.GetFileName(dlg.FileName);
                }

                var res = dlg.ShowDialog();
                if (res == true)
                {
                    ddsf.Result = true;
                    ddsf.Location = AnyUiDialogueDataSaveFile.LocationKind.Local;
                    ddsf.TargetFileName = dlg.FileName;
                }
#endif
            }
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Modal dialogue: this function will block, until user ends dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public override async Task<bool> StartFlyoverModalAsync(AnyUiDialogueDataBase dialogueData, Action? rerender = null)
        {
            // note: rerender not required in this UI platform
            // access
            if (dialogueData == null || FlyoutProvider == null)
                return false;

            // make sure to reset
            dialogueData.Result = false;

            // beware of exceptions
            try
            {
                var uc = DispatchFlyout(dialogueData);
                if (uc != null)
                {
                    if (dialogueData.HasModalSpecialOperation)
                        // start WITHOUT modal
                        await FlyoutProvider!.StartFlyoverAsync(uc);
                    else
                        await FlyoutProvider!.StartFlyoverModalAsync(uc);
                }

                // now, in case
                await PerformSpecialOpsAsync(modal: true, dialogueData: dialogueData);

                // may be close?
                if (dialogueData.HasModalSpecialOperation)
                    // start WITHOUT modal
                    await FlyoutProvider!.CloseFlyoverAsync();
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while showing modal AnyUI dialogue {dialogueData.GetType().ToString()}");
            }

            // result
            return dialogueData.Result;
        }

        /// <summary>
        /// Shows specified dialogue hardware-independent. The technology implementation will show the
        /// dialogue based on the type of provided <c>dialogueData</c>. 
        /// Non-modal: This function wil return immideately after initially displaying the dialogue.
        /// </summary>
        /// <param name="dialogueData"></param>
        /// <returns>If the dialogue was end with "OK" or similar success.</returns>
        public async override Task StartFlyoverAsync(AnyUiDialogueDataBase dialogueData)
        {
            await Task.Yield();

            throw new NotImplementedException("StartFlyover w/o Async not implemented, yet!");

#if TODO_IMPORTANT
            // access
            if (dialogueData == null || FlyoutProvider == null)
                return;

            // make sure to reset
            dialogueData.Result = false;

            // beware of exceptions
            try
            {
                var uc = DispatchFlyout(dialogueData);
                if (uc != null)
                    FlyoutProvider?.StartFlyover(uc);

            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while starting AnyUI dialogue {dialogueData.GetType().ToString()}");
            }
#endif
        }

        /// <summary>
        /// Closes started flyover dialogue-
        /// </summary>
        public override void CloseFlyover()
        {
#if TODO_IMPORTANT
            FlyoutProvider.CloseFlyover();
#endif
        }

        //
        // Special functions
        //

        /// <summary>
        /// Print a page with QR codes
        /// </summary>
        public override void PrintSingleAssetCodeSheet(
            string assetId, string description, string title = "Single asset code sheet")
        {
#if TODO_IMPORTANT
            AasxPrintFunctions.PrintSingleAssetCodeSheet(assetId, description, title);
#endif
        }

        //
        // Convenience for file dialogues
        //

        // REFACTOR: the SAME as for HTML!!

        /// <summary>
        /// Selects a filename to read either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public async override Task<AnyUiDialogueDataOpenFile> MenuSelectOpenFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            bool requireNoFlyout = false)
        {
            // filename
            var sourceFn = ticket?[argName] as string;

            // prepare query
            var uc = new AnyUiDialogueDataOpenFile(
                   caption: caption,
                   message: "Select filename by uploading it or from stored user files.",
                   filter: filter, proposeFn: proposeFn);
            uc.AllowUserFiles = PackageContainerUserFile.CheckForUserFilesPossible();

            // scripted success?
            if (ticket?.ScriptMode == true && sourceFn?.HasContent() == true)
            {
                uc.Result = true;
                uc.TargetFileName = sourceFn;
                return uc;
            }

            // do direct?
            if (sourceFn?.HasContent() != true && requireNoFlyout)
            {
                // do not perform further with show "new" (overlapping!) flyout ..
                await PerformSpecialOpsAsync(modal: true, dialogueData: uc);
                return uc;
            }

            // no, via modal dialog?
            if (sourceFn?.HasContent() != true)
            {
                if (await StartFlyoverModalAsync(uc))
                {
                    // house keeping
                    RememberForInitialDirectory(uc.TargetFileName);

                    // modify
                    if (uc.ResultUserFile)
                        uc.TargetFileName = PackageContainerUserFile.Scheme + uc.TargetFileName;

                    // ok
                    return uc;
                }
            }

            if (sourceFn?.HasContent() != true)
            {
                MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, msg);
                uc.Result = false;
                return uc;
            }

            return new AnyUiDialogueDataOpenFile()
            {
                OriginalFileName = sourceFn,
                TargetFileName = sourceFn
            };
        }

        /// <summary>
		/// If ticket does not contain the filename named by <c>argName</c>,
		/// read it by the user.
		/// </summary>
		public async override Task<bool> MenuSelectOpenFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            var uc = await MenuSelectOpenFilenameAsync(ticket, argName, caption, proposeFn, filter, msg);
            if (uc?.Result == true)
            {
                ticket[argName] = uc.TargetFileName;
                return true;
            }
            return false;
        }

        /// <summary>
		/// Selects a filename to write either from user or from ticket.
		/// </summary>
		/// <returns>The dialog data containing the filename or <c>null</c></returns>
		public async override Task<AnyUiDialogueDataSaveFile> MenuSelectSaveFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            bool requireNoFlyout = false,
            bool reworkSpecialFn = false)
        {
            // filename
            var targetFn = ticket?[argName] as string;

            // prepare query
            var uc = new AnyUiDialogueDataSaveFile(
                    caption: caption,
                    message: "Select filename and how to provide the file. " +
                    "It might be possible to store files " +
                    "as user file or on a local file system.",
                    filter: filter, proposeFn: proposeFn);

            uc.AllowUserFiles = PackageContainerUserFile.CheckForUserFilesPossible();
            uc.AllowLocalFiles = Options.Curr.AllowLocalFiles;

            // scripted success?
            if (ticket?.ScriptMode == true && targetFn?.HasContent() == true)
            {
                uc.Result = true;
                uc.TargetFileName = targetFn;
                uc.Location = AnyUiDialogueDataSaveFile.LocationKind.Local;
                return uc;
            }

            // do direct?
            if (targetFn?.HasContent() != true && requireNoFlyout)
            {
                // do not perform further with show "new" (overlapping!) flyout ..
                await PerformSpecialOpsAsync(modal: true, dialogueData: uc);
                if (!uc.Result)
                    return uc;

                // maybe rework?
                if (reworkSpecialFn)
                    MainWindowAnyUiDialogs.SaveFilenameReworkTargetFilename(uc);

                // ok
                return uc;
            }

            // no, via modal dialog?
            if (targetFn?.HasContent() != true)
            {
                if (await StartFlyoverModalAsync(uc))
                {
                    // house keeping
                    RememberForInitialDirectory(uc.TargetFileName);

                    // maybe rework?
                    if (reworkSpecialFn)
                        MainWindowAnyUiDialogs.SaveFilenameReworkTargetFilename(uc);

                    // ok
                    return uc;
                }
            }

            if (targetFn?.HasContent() != true)
            {
                MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, msg);
                uc.Result = false;
                return uc;
            }

            return new AnyUiDialogueDataSaveFile()
            {
                TargetFileName = targetFn
            };
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public async override Task<bool> MenuSelectSaveFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            string argFilterIndex = null,
            string argLocation = null,
            bool reworkSpecialFn = false)
        {
            var uc = await MenuSelectSaveFilenameAsync(
                ticket, argName, caption, proposeFn, filter, msg,
                reworkSpecialFn: reworkSpecialFn);

            if (uc.Result && uc.TargetFileName.HasContent())
            {
                ticket[argName] = uc.TargetFileName;
                if (argFilterIndex?.HasContent() == true)
                    ticket[argFilterIndex] = uc.FilterIndex;
                if (argLocation?.HasContent() == true)
                    ticket[argLocation] = uc.Location.ToString();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public async override Task<AnyUiDialogueDataTextBox?> MenuSelectTextAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            // filename
            var targetText = ticket?[argName] as string;

            if (targetText?.HasContent() != true)
            {
                var uc = new AnyUiDialogueDataTextBox(caption, symbol: AnyUiMessageBoxImage.Question);
                uc.Text = proposeText;
                await StartFlyoverModalAsync(uc);
                if (uc.Result)
                    targetText = uc.Text;
            }

            if (targetText?.HasContent() != true)
            {
                MainWindowLogic.LogErrorToTicketOrSilentStatic(ticket, msg);
                return null;
            }

            return new AnyUiDialogueDataTextBox()
            {
                Text = targetText
            };
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public async override Task<bool> MenuSelectTextToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            var uc = await MenuSelectTextAsync(ticket, argName, caption, proposeText, msg);
            if (uc.Result)
            {
                ticket[argName] = uc.Text;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public override async Task<AnyUiDialogueDataLogMessage> MenuExecuteSystemCommand(
            string caption,
            string workDir,
            string cmd,
            string args,
            string[]? ignoreError = null)
        {
            // create dialogue
            var uc = new AnyUiDialogueDataLogMessage(caption);

            // create logger
            Process? proc = null;
            var logError = false;
            var logBuffer = new List<StoredPrint>();

            // wrap to track errors
            try
            {
                // start
                lock (logBuffer)
                {
                    logBuffer.Add(new StoredPrint(StoredPrint.Color.Black,
                        "Starting in " + workDir + " : " + cmd + " " + args + " .."));
                }
                ;

                // start process??
                proc = new Process();
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = args;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.StartInfo.WorkingDirectory = workDir;

                // see: https://stackoverflow.com/questions/1390559/
                // how-to-get-the-output-of-a-system-diagnostics-process

                // see: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.beginoutputreadline?
                // view=net-7.0&redirectedfrom=MSDN#System_Diagnostics_Process_BeginOutputReadLine

                uc.CheckForLogAndEnd = () =>
                {
                    StoredPrint[]? msgs = null;
                    lock (logBuffer)
                    {
                        if (logBuffer.Count > 0)
                        {
                            foreach (var sp in logBuffer)
                                Log.Singleton.Append(sp);

                            msgs = logBuffer.ToArray();
                            logBuffer.Clear();
                        }
                    }
                    ;
                    return new Tuple<object[]?, bool>(msgs, !logError && proc != null && proc.HasExited);
                };

                proc.OutputDataReceived += (s1, e1) =>
                {
                    var msg = e1.Data;
                    if (msg?.HasContent() == true)
                        lock (logBuffer)
                        {
                            logBuffer.Add(new StoredPrint(StoredPrint.Color.Black, "" + msg));
                        }
                    ;
                };

                proc.ErrorDataReceived += (s2, e2) =>
                {
                    var msg = e2.Data;
                    if (msg?.HasContent() == true)
                        lock (logBuffer)
                        {
                            // check if to ignore
                            var ignore = false;
                            if (ignoreError != null)
                                foreach (var ign in ignoreError)
                                    if (msg.IndexOf(ign, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                        ignore = true;

                            // how to handle?
                            if (ignore)
                                logBuffer.Add(new StoredPrint(StoredPrint.Color.Yellow, "" + msg));
                            else
                            {
                                logError = true;
                                logBuffer.Add(new StoredPrint(StoredPrint.Color.Red, "" + msg));
                            }
                        }
                    ;
                };

                proc.Exited += (s3, e3) =>
                {
                    lock (logBuffer)
                    {
                        logBuffer.Add(new StoredPrint(StoredPrint.Color.Black, "Done."));
                    }
                    ;
                };

                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await StartFlyoverModalAsync(uc);
            }
            catch (Exception ex)
            {
                // mirror exception to inside and outside
                lock (logBuffer)
                {
                    logError = true;
                    logBuffer.Add(new StoredPrint(StoredPrint.Color.Red, "" + ex.Message));
                }
                Log.Singleton.Error(ex, "executing system command");
            }

            return uc;
        }

        
    }

    public class AnyUiColorToMauiBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiColor col)
                return AnyUiDisplayContextMaui.GetMauiBrush(col);
            return Brush.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush br)
            {
                return AnyUiDisplayContextMaui.GetAnyUiColor(br);
            }
            return AnyUiColors.Default;
        }
    }

    public class AnyUiColorToMauiColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiColor col)
                return AnyUiDisplayContextMaui.GetMauiColor(col);
            return Brush.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is Color col)
            {
                return AnyUiDisplayContextMaui.GetAnyUiColor(col);
            }
            return AnyUiColors.Default;
        }
    }

    public class AnyUiMessageBoxImageToFontImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiMessageBoxImage mbi)
            {
                string? glyph = null;

                if (mbi == AnyUiMessageBoxImage.Error) 
                    glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Error;
                if (mbi == AnyUiMessageBoxImage.Question) 
                    glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Help;
                if (mbi == AnyUiMessageBoxImage.Hand) 
                    glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Back_hand;
                if (mbi == AnyUiMessageBoxImage.Asterisk) 
                    glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Star;
                if (mbi == AnyUiMessageBoxImage.Exclamation) 
                    glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Report;
                if (mbi == AnyUiMessageBoxImage.Stop) 
                    glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Cancel;

                if (glyph != null)
                {
                    var fis = new FontImageSource()
                    {
                        Glyph = UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Error,
                        FontFamily = "MaterialOutlined",
                        Color = XamlHelpers.GetDynamicRessource("Backstage_Frame", Colors.LightGray),
                        Size = 128
                    };

                    return fis;
                }

                return null;
            }
            return Brush.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("AnyUiMessageBoxImageToFontImageSourceConverter:ConvertBack");
        }
    }

#if TODO_IMPORTANT

    public class AnyUiBrushToWpfBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiBrush br)
                return AnyUiDisplayContextWpf.GetMauiBrush(br);
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush br)
            {
                return AnyUiDisplayContextWpf.GetAnyUiBrush(br);
            }
            return AnyUiBrushes.Default;
        }
    }

    public class AnyUiVisibilityToWpfVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is AnyUiVisibility vis)
            {
                if (vis == AnyUiVisibility.Visible)
                    return System.Windows.Visibility.Visible;
                if (vis == AnyUiVisibility.Collapsed)
                    return System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is System.Windows.Visibility vis)
            {
                if (vis == Visibility.Visible)
                    return AnyUiVisibility.Visible;
                if (vis == Visibility.Collapsed)
                    return AnyUiVisibility.Collapsed;
            }
            return AnyUiVisibility.Hidden;
        }
    }
#endif
}