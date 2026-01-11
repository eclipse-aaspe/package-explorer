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
using Windows.ApplicationModel.Appointments.AppointmentsProvider;

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
        public string FontAlias = "";
        public int FontSize;
#if WINDOWS
        public Microsoft.UI.Xaml.Media.FontFamily? FontFamily;
#endif
    }

    public class AnyUiDisplayContextMaui : AnyUiContextPlusDialogs
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

        public void TryRegisterIconFont(string shortId, string fontAlias, int fontSize)
        {
            var n = new AnyUiIconFont()
            {
                Short = shortId,
                FontAlias = fontAlias,
                FontSize = fontSize
            };

#if WINDOWS
            n.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(fontAlias);
#endif

            IconFonts.Add(n);
        }

        public AnyUiIconFont? FindIconFont(string shortId)
        {
            foreach (var fo in IconFonts)
                if (fo.Short == shortId)
                    return fo;
            return null;
        }

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

        /// <summary>
        /// Different tool set, e.g. with transparent backgrounds, can be used
        /// </summary>
        public enum RenderWidgetToolSet { Normal, Transparent }

        /// <summary>
        /// This class holds information how rendered elements are initailized
        /// by default, when not other specified. Such as text color ..
        /// </summary>
        public class RenderDefaults
        {
            /// <summary>
            /// This foreground is for widgets, which do not have an 'own' background,
            /// such as labels.
            /// </summary>
            public AnyUiBrush? ForegroundSelfStand;

            /// <summary>
            /// This forground is for widgets, which have an own background given by the
            /// control theme, such as buttons and combo boxes.
            /// </summary>
            public AnyUiBrush? ForegroundControl;

            /// <summary>
            /// Supposed to be the 'normal' font size of a Label on the specific platform
            /// </summary>
            public double FontSizeNormal = 14.0;

            /// <summary>
            /// Relative font size (to make fonts relatively larger/ smaller) to the 
            /// default control theme. Multiplied by default font size and widgets own
            /// (relative) font size.
            /// </summary>
            public double? FontSizeRel = 1.0;

            /// <summary>
            /// For e.g. modal pages, a secondary toolset with partly transparent frames
            /// can beu used.
            /// </summary>
            public RenderWidgetToolSet WidgetToolSet = RenderWidgetToolSet.Normal;

            //
            // Constructor
            //

            public RenderDefaults()
            {
                var lab = new Label();
                FontSizeNormal = lab.FontSize;
            }
        }

        private class RenderRec
        {
            public Type CntlType;
            public Func<RenderWidgetToolSet, Type> GetMauiType;
            [JsonIgnore]
            public Action<AnyUiUIElement, VisualElement, AnyUiRenderMode, RenderDefaults?>? InitLambda;
            [JsonIgnore]
            public Action<AnyUiUIElement, VisualElement, bool>? HighlightLambda;

            public Func<AnyUiUIElement, int>? CheckSuitability;

            public RenderRec(Type cntlType, Func<RenderWidgetToolSet, Type> getMauiType,
                Func<AnyUiUIElement, int>? checkSuitability = null,
                Action<AnyUiUIElement, VisualElement, AnyUiRenderMode, RenderDefaults?>? initLambda = null,
                Action<AnyUiUIElement, VisualElement, bool>? highlightLambda = null)
            {
                CntlType = cntlType;
                GetMauiType = getMauiType;
                CheckSuitability = checkSuitability;
                InitLambda = initLambda;
                HighlightLambda = highlightLambda;
            }
        }

        private class ListOfRenderRec : List<RenderRec>
        {
            public RenderRec? FindAnyUiCntl(Type? searchType, AnyUiUIElement? anyUiElem)
            {
                if (searchType == null)
                    return null;
                foreach (var rr in this)
                    if (rr?.CntlType == searchType
                        && (rr?.CheckSuitability == null || anyUiElem == null
                            || rr.CheckSuitability(anyUiElem) >= 1))
                        return rr;
                return null;
            }
        }

        [JsonIgnore]
        private ListOfRenderRec RenderRecs = new ListOfRenderRec();

        [JsonIgnore]
        private Point _dragStartPoint = new Point(0, 0);

        private void InitRenderRecs()
        {
            RenderRecs.Clear();
            RenderRecs.AddRange(new[]
            {
                new RenderRec(typeof(AnyUiUIElement), (wts) => typeof(VisualElement), null, (a, b, mode, rd) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiUIElement cntl && b is VisualElement maui
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiFrameworkElement), (wts) => typeof(View), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiFrameworkElement cntl && b is View maui
                        && mode == AnyUiRenderMode.All)
                    {
                        if (a is AnyUiButton)
                            ;

                        if (cntl.Margin != null)
                            maui.Margin = GetMauiTickness(cntl.Margin);
                        if (cntl.VerticalAlignment.HasValue)
                            maui.VerticalOptions = GetLayoutOptions(cntl.VerticalAlignment.Value);
                        if (cntl.HorizontalAlignment.HasValue)
                            maui.HorizontalOptions = GetLayoutOptions(cntl.HorizontalAlignment.Value);
                        if (cntl.MinHeight.HasValue)
                            maui.MinimumHeightRequest = GetLengthFromRelative(rd, cntl.MinHeight.Value);
                        if (cntl.MinWidth.HasValue)
                            maui.MinimumWidthRequest = GetLengthFromRelative(rd, cntl.MinWidth.Value);
                        if (cntl.MaxHeight.HasValue)
                            maui.MaximumHeightRequest = GetLengthFromRelative(rd, cntl.MaxHeight.Value);
                        if (cntl.MaxWidth.HasValue)
                            maui.MaximumWidthRequest = GetLengthFromRelative(rd, cntl.MaxWidth.Value);
                        maui.BindingContext = cntl.Tag;

                        if (cntl.DisplayData is AnyUiDisplayDataMaui ddmaui
                            && ddmaui.EventsAdded == false)
                        {
                            // add events only once!
                            ddmaui.EventsAdded = true;

                            if ( (cntl.EmitEvent & AnyUiEventMask.LeftDown) > 0)
                            {
                                // single click
                                var tap = new TapGestureRecognizer() { NumberOfTapsRequired = 1 };
                                maui.GestureRecognizers.Add(tap);
                                tap.Tapped += async (s5, e5) =>
                                {
                                    // emit only value event
                                    // get the current coordinates relative to the framework element
                                    // (only this could be sensible information to an any ui business logic)
                                    AnyUiPoint? p = null;
                                    var evdata = new AnyUiEventData(AnyUiEventMask.LeftDown, cntl, 1, p);
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(evdata));
                                };
                            }

                            if ( (cntl.EmitEvent & AnyUiEventMask.LeftDouble) > 0)
                            {
                                // double click
                                var tap = new TapGestureRecognizer() { NumberOfTapsRequired = 2 };
                                maui.GestureRecognizers.Add(tap);
                                tap.Tapped += async (s5, e5) =>
                                {
                                    // emit only value event
                                    // get the current coordinates relative to the framework element
                                    // (only this could be sensible information to an any ui business logic)
                                    AnyUiPoint? p = null;
                                    var evdata = new AnyUiEventData(AnyUiEventMask.LeftDouble, cntl, 2, p);
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(evdata));
                                };
                            }
#if TODO_IMPORTANT
                            if ((cntl.EmitEvent & AnyUiEventMask.DragStart) > 0)
                            {
                                maui.MouseLeftButtonDown +=(s6, e6) =>
                                {
                                    _dragStartPoint = e6.GetPosition(null);
                                };

                                maui.PreviewMouseMove += (s7, e7) =>
                                {
                                    if (e7.LeftButton == MouseButtonState.Pressed)
                                    {
                                        Point position = e7.GetPosition(null);
                                        if (Math.Abs(position.X - _dragStartPoint.X)
                                                > SystemParameters.MinimumHorizontalDragDistance
                                            || Math.Abs(position.Y - _dragStartPoint.Y)
                                                > SystemParameters.MinimumVerticalDragDistance)
                                        {
                                            cntl.setValueLambda?.Invoke(
                                                new AnyUiEventData(AnyUiEventMask.DragStart, cntl));
                                        }
                                    }
                                };
                            }
#endif
                        }
                    }
                }),

                // Do the  render record (basetype initialization) for AnyUiControl, even if there is no
                // directly equivalent on MAUI side
                new RenderRec(typeof(AnyUiControl), (wts) => typeof(View), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiControl cntl && b is View wpf
                       && mode == AnyUiRenderMode.All)
                   {
#if TODO_IMPORTANT
                       if (cntl.VerticalContentAlignment.HasValue)
                           wpf.VerticalContentAlignment =
                            (VerticalAlignment)((int) cntl.VerticalContentAlignment.Value);
                       if (cntl.HorizontalContentAlignment.HasValue)
                           wpf.HorizontalContentAlignment =
                            (HorizontalAlignment)((int) cntl.HorizontalContentAlignment.Value);
#endif
                       // the font properties are not part of this (non-existing) base type,
                       // but are properties of the individual sub-ordinate types
                   }
                }),

                // Do the  render record (basetype initialization) for AnyUiControl, even if there is no
                // directly equivalent on MAUI side
                new RenderRec(typeof(AnyUiContentControl), (wts) => typeof(View), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiContentControl && b is View
                       && mode == AnyUiRenderMode.All)
                   {
                   }
                }),

                // Do the  render record (basetype initialization) for AnyUiControl, even if there is no
                // directly equivalent on MAUI side
                new RenderRec(typeof(AnyUiDecorator), (wts) => typeof(ContentView), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiDecorator cntl && b is ContentView maui
                        && mode == AnyUiRenderMode.All)
                    {
                        // child
                        maui.Content = GetOrCreateMauiElement(cntl.Child, allowReUse: false, renderDefaults: rd) as View;
                    }
                }),

                new RenderRec(typeof(AnyUiViewbox), (wts) => typeof(Viewbox), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiViewbox cntl && b is Viewbox maui
                       && mode == AnyUiRenderMode.All)
                   {
#if TODO_IMPORTANT
                        wpf.Stretch = (Stretch)(int) cntl.Stretch;
#endif
                   }
                }),

                new RenderRec(typeof(AnyUiPanel), (wts) => typeof(Layout), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiPanel cntl && b is Layout maui)
                   {
                        // figure out, when to redraw
                        var redraw = (mode == AnyUiRenderMode.All)
                            || (mode == AnyUiRenderMode.StatusToUi && a is AnyUiCanvas);

                        if (redraw)
                        {
                            // normal members
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);

                            // children
                            maui.Children.Clear();
                            if (cntl.Children != null)
                                foreach (var ce in cntl.Children)
                                {
                                    var chw = GetOrCreateMauiElement(ce, allowReUse: false, renderDefaults: rd);
                                    if (cntl.Padding != null)
                                    {
                                        // project the Panel padding to each indivuidual child
                                        if (chw is View few)
                                        {
                                            few.Margin = new Thickness(
                                                few.Margin.Left + cntl.Padding.Left,
                                                few.Margin.Top + cntl.Padding.Top,
                                                few.Margin.Right + cntl.Padding.Right,
                                                few.Margin.Bottom + cntl.Padding.Bottom);
                                        }
                                    }
                                    if (chw != null)
                                        maui.Children.Add(chw);
                                }
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiGrid), (wts) => typeof(Grid), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiGrid cntl && b is Grid maui
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.RowDefinitions != null)
                           foreach (var rds in cntl.RowDefinitions)
                               maui.RowDefinitions.Add(GetMauiRowDefinition(rds, rd));

                       if (cntl.ColumnDefinitions != null)
                           foreach (var cd in cntl.ColumnDefinitions)
                               maui.ColumnDefinitions.Add(GetMauiColumnDefinition(cd, rd));

                       // make sure to target only already realized children
                       foreach (var cel in cntl.Children)
                       {
                           var celMaui = GetOrCreateMauiElement(cel, allowCreate: false, renderDefaults: rd);
                           if (maui.Children.Contains(celMaui))
                           {
                                if (cel.GridRow.HasValue)
                                    Grid.SetRow(celMaui, cel.GridRow.Value);
                                if (cel.GridRowSpan.HasValue)
                                    Grid.SetRowSpan(celMaui, cel.GridRowSpan.Value);
                                if (cel.GridColumn.HasValue)
                                    Grid.SetColumn(celMaui, cel.GridColumn.Value);
                                if (cel.GridColumnSpan.HasValue)
                                    Grid.SetColumnSpan(celMaui, cel.GridColumnSpan.Value);

                                // new in this (MAUI) implementation: the principle is in MAUI care
                                // the childs for the size contrains, therefore try to adopt their properties
                                // TODO: What to do for spans?
                                if (celMaui is View && cel is AnyUiFrameworkElement celFe)
                                {
                                    var row = cel.GridRow ?? 0;
                                    var col = cel.GridColumn ?? 0;

                                    if (cntl.ColumnDefinitions != null && col >= 0 && col < cntl.ColumnDefinitions.Count())
                                    {
                                        if (cntl.ColumnDefinitions[col].MinWidth.HasValue && !celFe.MinWidth.HasValue)
                                            celMaui.MinimumWidthRequest = GetLengthFromRelative(rd, cntl.ColumnDefinitions[col].MinWidth!.Value);
                                        if (cntl.ColumnDefinitions[col].MaxWidth.HasValue && !celFe.MaxWidth.HasValue)
                                            celMaui.MaximumWidthRequest = GetLengthFromRelative(rd, cntl.ColumnDefinitions[col].MaxWidth!.Value);
                                    }

                                    if (cntl.RowDefinitions != null && row >= 0 && row < cntl.RowDefinitions.Count())
                                    {
                                        if (cntl.RowDefinitions[row].MinHeight.HasValue && !celFe.MinHeight.HasValue)
                                            celMaui.MinimumHeightRequest = GetLengthFromRelative(rd, cntl.RowDefinitions[row].MinHeight!.Value);
                                    }
                                }
                           }
                       }
                   }
                }),

                new RenderRec(typeof(AnyUiStackPanel), (wts) => typeof(VerticalStackLayout),
                (anyelem) => (anyelem is AnyUiStackPanel cntl 
                              && (cntl.Orientation == null || cntl.Orientation == AnyUiOrientation.Vertical)) ? 1 : 0,
                (a, b, mode, rd) =>
                {
                   if (a is AnyUiStackPanel cntl && b is VerticalStackLayout maui
                       && mode == AnyUiRenderMode.All)
                   {
#if TODO_IMPORTANT
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = (Orientation)((int) cntl.Orientation.Value);
#endif
                   }
                }),

                new RenderRec(typeof(AnyUiStackPanel), (wts) => typeof(HorizontalStackLayout),
                (anyelem) => (anyelem is AnyUiStackPanel cntl && cntl.Orientation == AnyUiOrientation.Horizontal) ? 1 : 0,
                (a, b, mode, rd) =>
                {
                   if (a is AnyUiStackPanel cntl && b is HorizontalStackLayout maui
                       && mode == AnyUiRenderMode.All)
                   {
#if TODO_IMPORTANT
                       if (cntl.Orientation.HasValue)
                           wpf.Orientation = (Orientation)((int) cntl.Orientation.Value);
#endif
                   }
                }),

                new RenderRec(typeof(AnyUiWrapPanel), (wts) => typeof(FlexLayout), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiWrapPanel cntl && b is FlexLayout maui
                       && mode == AnyUiRenderMode.All)
                   {
                       if (cntl.Orientation.HasValue)
                        {
                            if (cntl.Orientation == AnyUiOrientation.Horizontal)
                                maui.Direction = FlexDirection.Row;
                            if (cntl.Orientation == AnyUiOrientation.Vertical)
                                maui.Direction = FlexDirection.Column;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiShape), (wts) => typeof(Shape), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiShape cntl && b is Shape maui
                        && mode == AnyUiRenderMode.All)
                    {
                        if (cntl.Fill != null)
                            maui.Fill = GetMauiBrush(cntl.Fill);
                        if (cntl.Stroke != null)
                            maui.Stroke = GetMauiBrush(cntl.Stroke);
                        if (cntl.StrokeThickness.HasValue)
                            maui.StrokeThickness = cntl.StrokeThickness.Value;
                        maui.BindingContext = cntl.Tag;
                    }
                }),

                new RenderRec(typeof(AnyUiRectangle), (wts) => typeof(Rectangle), null, (a, b, mode, rd) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiRectangle cntl && b is Rectangle maui
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiEllipse), (wts) => typeof(Ellipse), null, (a, b, mode, rd) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiEllipse cntl && b is Ellipse maui
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiPolygon), (wts) => typeof(Polygon), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiPolygon cntl && b is Polygon maui
                        && (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi))
                    {
                        // points
                        if (cntl.Points != null)
                            foreach (var p in cntl.Points)
                                maui.Points.Add(GetMauiPoint(p));
                    }
                }),

                new RenderRec(typeof(AnyUiCanvas), (wts) => typeof(AbsoluteLayout), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiCanvas cntl && b is AbsoluteLayout maui)
                   {
                        // Children are added but deserve some post processing
                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            if (cntl.Children.Count == maui.Children.Count)
                                for (int i=0; i < cntl.Children.Count; i++)
                                {
                                    var cc = cntl.Children[i] as AnyUiFrameworkElement;
                                    var cw = maui.Children[i] as View;
                                    if (cc != null && cw != null)
                                    {
                                        AbsoluteLayout.SetLayoutBounds(cw, new Rect(cc.X, cc.Y, cc.Width, cc.Height));
                                    }
                                }
                        }

                        // need to subscribe for events?
                        if (mode == AnyUiRenderMode.All)
                        {
#if TODO_IMPORTANT
                            if ((cntl.EmitEvent & AnyUiEventMask.MouseAll) > 0)
                            {
                                maui.MouseDown += (s,e) =>
                                {
                                    // get the current coordinates relative to the framework element
                                    // (onlythis could be sensible information to an any ui business logic)
                                    var p = GetAnyUiPoint(Mouse.GetPosition(maui));

                                    // try to find AnyUI element emitting the event
                                    object auiSource = null;
                                    foreach (var ch in cntl.Children)
                                        if ((ch?.DisplayData as AnyUiDisplayDataWpf)?.WpfElement == e.Source)
                                            auiSource = ch;
                                
                                    // send event and emit return
                                    if (e.ChangedButton == MouseButton.Left)
                                    {
                                        EmitOutsideAction(
                                            cntl.setValueLambda?.Invoke(new AnyUiEventData(
                                                    AnyUiEventMask.LeftDown, auiSource, e.ClickCount, p)));
                                    }
                                };
                            }
#endif
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiScrollViewer), (wts) => typeof(ScrollView), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiScrollViewer cntl && b is ScrollView maui
                       && mode == AnyUiRenderMode.All)
                   {
                        // attributes
                        if (cntl.HorizontalScrollBarVisibility.HasValue)
                            maui.HorizontalScrollBarVisibility =
                                GetScrollBarVisibility(cntl.HorizontalScrollBarVisibility.Value);
                        if (cntl.VerticalScrollBarVisibility.HasValue)
                            maui.VerticalScrollBarVisibility =
                                GetScrollBarVisibility(cntl.VerticalScrollBarVisibility.Value);

                        // initial position (before attaching callback)
                        if (cntl.InitialScrollPosition.HasValue)
                        {
#if TODO_IMPORTANT
                            maui.ScrollToVerticalOffset(cntl.InitialScrollPosition.Value);
#endif
                            // TODO: Check for lambda vars
                            maui.Loaded += (_, __) =>
                            {
                                maui.ScrollToAsync(0, cntl.InitialScrollPosition.Value, animated: false);
                            };
                        }

                        // callbacks
                        maui.Scrolled += async (s,e) =>
                        {
                            if (cntl.setValueAsyncLambda != null)
                                await cntl.setValueAsyncLambda.Invoke(
                                    new Tuple<double, double>(e.ScrollX, e.ScrollY));
                        };
                   }
                }),

                new RenderRec(typeof(AnyUiBorder), (wts) => typeof(Border), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiBorder cntl && b is Border maui)
                    {
                        if (mode == AnyUiRenderMode.All)
                        {
                            // members
                            if (cntl.BorderThickness != null)
                                maui.StrokeThickness = GetDoubleTickness(cntl.BorderThickness);
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);
                            if (cntl.CornerRadius != null)
                                maui.StrokeShape = new RoundRectangle
                                {
                                    CornerRadius = new CornerRadius(cntl.CornerRadius.Value)
                                };
                        
                            // callbacks
                            if (cntl.IsDropBox)
                            {
#if TODO_IMPORTANT
                                wpf.AllowDrop = true;
                                wpf.DragEnter += (object sender2, DragEventArgs e2) =>
                                {
                                    e2.Effects = DragDropEffects.Copy;
                                };
                                wpf.PreviewDragOver += (object sender3, DragEventArgs e3) =>
                                {
                                    e3.Handled = true;
                                };
                                wpf.Drop += (object sender4, DragEventArgs e4) =>
                                {
                                    if (e4.Data.GetDataPresent(DataFormats.FileDrop, true))
                                    {
                                        // Note that you can have more than one file.
                                        string[] files = (string[])e4.Data.GetData(DataFormats.FileDrop);

                                        // Assuming you have one file that you care about, pass it off to whatever
                                        // handling code you have defined.
                                        if (files != null && files.Length > 0
                                            && sender4 is FrameworkElement)
                                        {
                                            // update UI
                                            if (wpf.Child is TextBlock tb2)
                                                tb2.Text = "" + files[0];

                                            // value changed
                                            cntl.setValueLambda?.Invoke(files[0]);

                                            // contents changed
                                            WishForOutsideAction.Add(new AnyUiLambdaActionContentsChanged());
                                        }
                                    }

                                    e4.Handled = true;
                                };
#endif
                            }

                            // single click
                            if ( (cntl.EmitEvent & AnyUiEventMask.LeftDown) > 0)
                            {
                                var tap = new TapGestureRecognizer() { NumberOfTapsRequired = 1 };
                                maui.GestureRecognizers.Add(tap);
                                tap.Tapped += async (s5, e5) =>
                                {
                                    await Task.Yield();
                                    // TODO
                                    // get the current coordinates relative to the framework element
                                    // (only this could be sensible information to an any ui business logic)
                                    AnyUiPoint? p = null;
                                    var evdata = new AnyUiEventData(AnyUiEventMask.LeftDown, cntl, 1, p);
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(evdata));
                                };
                            }

                            // double click
                            if ( (cntl.EmitEvent & AnyUiEventMask.LeftDouble) > 0)
                            {
                                var tap = new TapGestureRecognizer() { NumberOfTapsRequired = 2 };
                                maui.GestureRecognizers.Add(tap);
                                tap.Tapped += async (s5, e5) =>
                                {
                                    await Task.Yield();
                                    // TODO
                                    // get the current coordinates relative to the framework element
                                    // (only this could be sensible information to an any ui business logic)
                                    AnyUiPoint? p = null;
                                    var evdata = new AnyUiEventData(AnyUiEventMask.LeftDouble, cntl, 2, p);
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(evdata));
                                };
                            }
                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);
                            if (cntl.BorderBrush != null)
                                maui.Stroke = GetMauiBrush(cntl.BorderBrush);
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiLabel), (wts) => typeof(Label), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiLabel cntl && b is Label maui)
                    {
                        if (mode == AnyUiRenderMode.All)
                        {
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);
                            if (rd?.ForegroundSelfStand != null)
                                maui.TextColor = GetMauiColor(rd.ForegroundSelfStand?.Color);
                            if (cntl.Foreground != null)
                                maui.TextColor = GetMauiColor(cntl.Foreground?.Color);
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);

                            maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.VerticalContentAlignment.HasValue)
                                maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                            if (cntl.HorizontalContentAlignment.HasValue)
                                maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                            if (cntl.FontMono)
                                maui.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            maui.Text = cntl.Content;
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiTextBlock), (wts) => typeof(Label), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiTextBlock cntl && b is Label maui)
                   {
                        if (mode == AnyUiRenderMode.All)
                        {
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);
                            if (rd?.ForegroundSelfStand != null)
                                maui.TextColor = GetMauiColor(rd.ForegroundSelfStand?.Color);
                            if (cntl.Foreground != null)
                                maui.TextColor = GetMauiColor(cntl.Foreground?.Color);
                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);
                            if (cntl.TextWrapping.HasValue)
                                maui.LineBreakMode = GetLineBreakMode(cntl.TextWrapping.Value);

                            maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.VerticalContentAlignment.HasValue)
                                maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                            if (cntl.HorizontalContentAlignment.HasValue)
                                maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                            if (cntl.FontMono)
                                maui.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            maui.Text = cntl.Text;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiSelectableTextBlock), (wts) => typeof(Label), null, (a, b, mode, rd) =>
                {
                   // TODO IMPORTANT: For now, only NON-SELECTABLE LABEL, change!!
                   if (a is AnyUiTextBlock cntl && b is Label maui)
                   {
                        if (mode == AnyUiRenderMode.All)
                        {
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);
                            if (rd?.ForegroundSelfStand != null)
                                maui.TextColor = GetMauiColor(rd.ForegroundSelfStand?.Color);
                            if (cntl.Foreground != null)
                                maui.TextColor = GetMauiColor(cntl.Foreground?.Color);
                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);
                            if (cntl.TextWrapping.HasValue)
                                maui.LineBreakMode = GetLineBreakMode(cntl.TextWrapping.Value);

                            maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.VerticalContentAlignment.HasValue)
                                maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                            if (cntl.HorizontalContentAlignment.HasValue)
                                maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                            if (cntl.FontMono)
                                maui.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            maui.Text = cntl.Text;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiHintBubble), (wts) => typeof(Label), null, (a, b, mode, rd) =>
                {
                   // TODO IMPORTANT: For now, only LABEL, change!!
                   if (a is AnyUiHintBubble cntl && b is Label maui)
                   {
                        if (mode == AnyUiRenderMode.All)
                        {
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);
                            if (rd?.ForegroundSelfStand != null)
                                maui.TextColor = GetMauiColor(rd.ForegroundSelfStand?.Color);
                            if (cntl.Foreground != null)
                                maui.TextColor = GetMauiColor(cntl.Foreground?.Color);

                            maui.LineBreakMode = LineBreakMode.WordWrap;

                            maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.VerticalContentAlignment.HasValue)
                                maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                            if (cntl.HorizontalContentAlignment.HasValue)
                                maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                            if (cntl.FontMono)
                                maui.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            maui.Text = cntl.Text;
                        }
                   }
                }),

                new RenderRec(typeof(AnyUiImage), (wts) => typeof(Image), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiImage cntl && b is Image maui)
                   {
#if TODO_IMPORTANT
                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            BitmapSource sourceBi = null;
                            if (cntl.BitmapInfo?.ImageSource is BitmapSource bs)
                               sourceBi = bs;
                            else if (cntl.BitmapInfo?.PngData != null)
                            {
                                using (MemoryStream memory = new MemoryStream())
                                {
                                    memory.Write(cntl.BitmapInfo.PngData, 0, cntl.BitmapInfo.PngData.Length);
                                    memory.Position = 0;

                                    BitmapImage bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.StreamSource = memory;
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.EndInit();

                                    sourceBi = bi;
                                }
                            }

                            // found something?
                            if (sourceBi != null)
                            {
                                // additionally convert?
                                if (cntl.BitmapInfo?.ConvertTo96dpi == true)
                                {
                                    // prepare
                                    double dpi = 96;
                                    int width = sourceBi.PixelWidth;
                                    int height = sourceBi.PixelHeight;

                                    // execute
                                    int stride = width * sourceBi.Format.BitsPerPixel;
                                    byte[] pixelData = new byte[stride * height];
                                    sourceBi.CopyPixels(pixelData, stride, 0);
                                    var destBi = BitmapSource.Create(
                                        width, height, dpi, dpi, sourceBi.Format, null, pixelData, stride);
                                    destBi.Freeze();

                                    // remember
                                    sourceBi = destBi;
                                }

                                // finally set
                                maui.Source = sourceBi;
                            }

                            maui.Stretch = (Stretch)(int) cntl.Stretch;
                            maui.StretchDirection = StretchDirection.Both;
                        }
#endif
                   }
                }),

                new RenderRec(typeof(AnyUiCountryFlag), (wts) => typeof(Image), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiCountryFlag cntl && b is Image maui)
                   {
#if TODO_IMPORTANT
                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {

                            maui.Source = AasxIntegrationBaseWpf.CountryFlagWpf.GetCroppedFlag(cntl.ISO3166Code);

                            maui.Stretch = Stretch.Fill;
                            maui.StretchDirection = StretchDirection.Both;
                        }
#endif
                   }
                }),

#if old_diabled
                /*new RenderRec(typeof(AnyUiCountryFlag), typeof(CountryFlag.Wpf.CountryFlag), (a, b, mode, rd) =>
                {
                   if (a is AnyUiCountryFlag cntl && b is CountryFlag.Wpf.CountryFlag wpf
                       && mode == AnyUiRenderMode.All)
                   {
                        // dead-csharp off
                        // need to translate two enums -> seems to be old version
                        //foreach (var ev in (CountryCode[])Enum.GetValues(typeof(CountryFlag.CountryCode)))
                        //    if (Enum.GetName(typeof(CountryCode), ev)?.Trim().ToUpper() == cntl.ISO3166Code)
                        //        wpf.Code = ev;
                        // dead-csharp on
                        wpf.CountryCode = cntl.ISO3166Code;
                   }
                }),*/
#endif

                // TextBox -> Entry for SINGLE LINE
                new RenderRec(typeof(AnyUiTextBox),
                (wts) => (wts == RenderWidgetToolSet.Transparent) ? typeof(TransparentEntry) : typeof(Entry),
                (anyElem) => (anyElem is AnyUiTextBox tb && tb.MultiLine == false) ? 1 : 0,
                (a, b, mode, rd) =>
                {
                    // TODO: Border in outside control!!
                    {
                        // protect names
                        if (a is AnyUiTextBox cntl && b is Entry maui)
                        {
                            if (mode == AnyUiRenderMode.All)
                            {
                                // members  
                                if (cntl.Background != null)
                                    maui.Background = GetMauiBrush(cntl.Background);

                                if (rd?.ForegroundControl != null)
                                    maui.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                                if (cntl.Foreground != null)
                                    maui.TextColor = GetMauiColor(cntl.Foreground?.Color);

    #if TODO_IMPORTANT
                                if (cntl.Padding != null)
                                    maui.Padding = GetMauiTickness(cntl.Padding);
    #endif
                                if (cntl.IsReadOnly)
                                    maui.IsReadOnly = cntl.IsReadOnly;

                                maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                                if (cntl.VerticalContentAlignment.HasValue)
                                    maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                                if (cntl.HorizontalContentAlignment.HasValue)
                                    maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                                if (cntl.FontMono)
                                    maui.FontFamily = "Consolas";

                                if (cntl.FontWeight.HasValue)
                                    maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                                maui.Text = cntl.Text;
                        
                                // callbacks
                                cntl.originalValue = "" + cntl.Text;
                                maui.TextChanged += async (sender, e) => {
                                    // state
                                    cntl.Text = maui.Text;

                                    // the value event
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(maui.Text));

                                    // other events
                                    EmitOutsideAction(new AnyUiLambdaActionContentsChanged());
                                };
                                maui.Completed += (sender, e) =>
                                {
                                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                    EmitOutsideAction(cntl.takeOverLambda);
                                };
                            }

                            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                            {
                                maui.Text = cntl.Text;
                            }
                        }
                    }

                    {
                        // protect names
                        if (a is AnyUiTextBox cntl && b is TransparentEntry maui)
                        {
                            if (mode == AnyUiRenderMode.All)
                            {
                                // members  
                                if (cntl.Background != null)
                                    maui.Background = GetMauiBrush(cntl.Background);

                                if (rd?.ForegroundControl != null)
                                    maui.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                                if (cntl.Foreground != null)
                                    maui.TextColor = GetMauiColor(cntl.Foreground?.Color);

    #if TODO_IMPORTANT
                                if (cntl.Padding != null)
                                    maui.Padding = GetMauiTickness(cntl.Padding);
    #endif
                                if (cntl.IsReadOnly)
                                    maui.IsReadOnly = cntl.IsReadOnly;

                                maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                                if (cntl.VerticalContentAlignment.HasValue)
                                    maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                                if (cntl.HorizontalContentAlignment.HasValue)
                                    maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                                if (cntl.FontMono)
                                    maui.FontFamily = "Consolas";

                                if (cntl.FontWeight.HasValue)
                                    maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                                maui.Text = cntl.Text;
                        
                                // callbacks
                                cntl.originalValue = "" + cntl.Text;
                                maui.TextChanged += async (sender, e) => {
                                    // state
                                    cntl.Text = maui.Text;

                                    // the value event
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(maui.Text));

                                    // other events
                                    EmitOutsideAction(new AnyUiLambdaActionContentsChanged());
                                };
                                maui.Completed += (sender, e) =>
                                {
                                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                    EmitOutsideAction(cntl.takeOverLambda);
                                };
                            }

                            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                            {
                                maui.Text = cntl.Text;
                            }
                        }
                    }
                }, highlightLambda: (a,b,highlighted) => {
                    // TODO: Border in outside control!!
#if TODO_IMPORTANT
                    if (a is AnyUiTextBox && b is Entry tb)
                    {
                        if (highlighted)
                        {
                            tb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            tb.BorderThickness = new Thickness(3);
                            tb.Focus();
                            tb.SelectAll();
                        }
                        else
                        {
                            tb.BorderBrush = SystemColors.ControlDarkBrush;
                            tb.BorderThickness = new Thickness(1);
                        }
                    }
#endif
                }),

                // TextBox -> Editor for MULTI LINE
                new RenderRec(typeof(AnyUiTextBox), (wts) => typeof(Editor),
                (anyElem) => (anyElem is AnyUiTextBox tb && tb.MultiLine == true) ? 1 : 0,
                (a, b, mode, rd) =>
                {
                    // TODO: Border in outside control!!
                    if (a is AnyUiTextBox cntl && b is Editor maui)
                    {
                        if (mode == AnyUiRenderMode.All)
                        {
                            // members  
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);

                            if (rd?.ForegroundControl != null)
                                maui.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                            if (cntl.Foreground != null)
                                maui.TextColor = GetMauiColor(cntl.Foreground?.Color);

#if TODO_IMPORTANT
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);
#endif

                            if (cntl.IsReadOnly)
                                maui.IsReadOnly = cntl.IsReadOnly;

                            maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.VerticalContentAlignment.HasValue)
                                maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                            if (cntl.HorizontalContentAlignment.HasValue)
                                maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                            if (cntl.FontMono)
                                maui.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                            maui.Text = cntl.Text;
                        
                            // callbacks (only text changed, NO Completed/ Keys)
                            cntl.originalValue = "" + cntl.Text;
                            maui.TextChanged += async (sender, e) => {
                                // state
                                cntl.Text = maui.Text;

                                // the value event
                                if (cntl.setValueAsyncLambda != null)
                                    EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(maui.Text));

                                // other events
                                EmitOutsideAction(new AnyUiLambdaActionContentsChanged());
                            };
                        }

                        if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
                        {
                            maui.Text = cntl.Text;
                        }
                    }
                }, highlightLambda: (a,b,highlighted) => {
                    // TODO: Border in outside control!!
#if TODO_IMPORTANT
                    if (a is AnyUiTextBox && b is TextBox tb)
                    {
                        if (highlighted)
                        {
                            tb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            tb.BorderThickness = new Thickness(3);
                            tb.Focus();
                            tb.SelectAll();
                        }
                        else
                        {
                            tb.BorderBrush = SystemColors.ControlDarkBrush;
                            tb.BorderThickness = new Thickness(1);
                        }
                    }
#endif
                }),

                new RenderRec(typeof(AnyUiComboBox), 
                (wts) => (wts == RenderWidgetToolSet.Transparent) ? typeof(TransparentPicker) : typeof(Picker), 
                null, (a, b, mode, rd) =>
                {
                    // members
                    if (a is AnyUiComboBox cntl && mode == AnyUiRenderMode.All)
                    {
                        if (b is Picker maui)
                        {
                            if (cntl.Background != null)
                                maui.Background = GetMauiBrush(cntl.Background);
                            if (rd?.ForegroundControl != null)
                                maui.TextColor = GetMauiColor(rd.ForegroundControl?.Color);
                            if (cntl.Foreground != null)
                                maui.TextColor = GetMauiColor(cntl.Foreground?.Color);

    #if TODO_IMPORTANT
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);
                            if (cntl.IsEditable.HasValue)
                                maui.IsEditable = cntl.IsEditable.Value;
    #endif

                            maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.VerticalContentAlignment.HasValue)
                                maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                            if (cntl.HorizontalContentAlignment.HasValue)
                                maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                            if (cntl.FontMono)
                                maui.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                            if (cntl.Items != null)
                            {
                                foreach (var i in cntl.Items)
                                    maui.Items.Add(i?.ToString());
                            }

    #if TODO_IMPORTANT
                            maui.Text = cntl.Text;
    #endif
                        
                            if (cntl.Text != null && cntl.Text.Length > 0 && !cntl.SelectedIndex.HasValue
                                && cntl.Items != null)
                            {
                                // use the existing text to set the combo box value via SelectedIndex
                                int ndx = -1;
                                for (int i=0; i<cntl.Items.Count; i++)
                                    if (cntl.Text.Trim().Equals(cntl.Items[i].ToString()?.Trim(),
                                        StringComparison.InvariantCultureIgnoreCase))
                                        ndx = i;
                                if (ndx >= 0)
                                    cntl.SelectedIndex = ndx;
                            }

                            if (cntl.SelectedIndex.HasValue)
                                maui.SelectedIndex = cntl.SelectedIndex.Value;

                            // callbacks
                            cntl.originalValue = "" + cntl.Text;
                            // TODO!!
                            if (true || cntl.IsEditable != true)
                            {
                                // we need this event
                                maui.SelectedIndexChanged += async (s, e) =>
                                {
                                    // state
                                    cntl.SelectedIndex = maui.SelectedIndex;
                                    cntl.Text = maui.SelectedItem as string;

                                    // the value event
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke((string) maui.SelectedItem));

                                    // other events
                                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                    // Note for MIHO: this was the dangerous outside event loop!
                                    EmitOutsideAction(cntl.takeOverLambda);
                                };
                            }
                            else
                            {
    #if TODO_IMPORTANT
                                // if editable, add this for comfort
                                maui.KeyUp += (sender, e) =>
                                {
                                    if (e.Key == Key.Enter)
                                    {
                                        e.Handled = true;
                                        EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                        EmitOutsideAction(cntl.takeOverLambda);
                                    }
                                };
    #endif
                            }
                        }

                        if (b is TransparentPicker mauiTP)
                        {

                            mauiTP.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

    #if TODO_IMPORTANT
                            if (cntl.Padding != null)
                                maui.Padding = GetMauiTickness(cntl.Padding);
                            if (cntl.IsEditable.HasValue)
                                maui.IsEditable = cntl.IsEditable.Value;
    #endif

                            if (cntl.Items != null)
                            {
                                mauiTP.ItemsSource = cntl.Items;
                            }

    #if TODO_IMPORTANT
                            maui.Text = cntl.Text;
    #endif
                        
                            if (cntl.Text != null && cntl.Text.Length > 0 && !cntl.SelectedIndex.HasValue
                                && cntl.Items != null)
                            {
                                // use the existing text to set the combo box value via SelectedIndex
                                int ndx = -1;
                                for (int i=0; i<cntl.Items.Count; i++)
                                    if (cntl.Text.Trim().Equals(cntl.Items[i].ToString()?.Trim(),
                                        StringComparison.InvariantCultureIgnoreCase))
                                        ndx = i;
                                if (ndx >= 0)
                                    cntl.SelectedIndex = ndx;
                            }

                            if (cntl.SelectedIndex.HasValue)
                                mauiTP.SelectedIndex = cntl.SelectedIndex.Value;

                            // callbacks
                            cntl.originalValue = "" + cntl.Text;
                            if (cntl.IsEditable != true)
                            {
                                // we need this event
                                mauiTP.SelectedIndexChanged += async (s, e) =>
                                {
                                    // state
                                    cntl.SelectedIndex = mauiTP.SelectedIndex;
                                    cntl.Text = mauiTP.SelectedItem as string;

                                    // the value event
                                    if (cntl.setValueAsyncLambda != null)
                                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke((string) mauiTP.SelectedItem));

                                    // other events
                                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                    // Note for MIHO: this was the dangerous outside event loop!
                                    EmitOutsideAction(cntl.takeOverLambda);
                                };
                            }
                            else
                            {
    #if TODO_IMPORTANT
                                // if editable, add this for comfort
                                maui.KeyUp += (sender, e) =>
                                {
                                    if (e.Key == Key.Enter)
                                    {
                                        e.Handled = true;
                                        EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                                        EmitOutsideAction(cntl.takeOverLambda);
                                    }
                                };
    #endif
                            }
                        }
                    }
                }, highlightLambda: (a,b,highlighted) => {
                    if (a is AnyUiComboBox && b is Picker cb)
                    {
                        if (highlighted)
                        {
                            // TODO: Color ..
#if TODO_IMPORTANT
                            cb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            cb.BorderThickness = new Thickness(3);

                            try
                            {
                                // see: https://stackoverflow.com/questions/37006596/borderbrush-to-combobox
                                // see also: https://stackoverflow.com/questions/2285491/
                                // wpf-findname-returns-null-when-it-should-not
                                cb.ApplyTemplate();
                                var cbTemp = cb.Template;
                                if (cbTemp != null)
                                {
                                    var toggleButton = cbTemp.FindName(
                                        "toggleButton", cb) as System.Windows.Controls.Primitives.ToggleButton;
                                    toggleButton?.ApplyTemplate();
                                    var tgbTemp = toggleButton?.Template;
                                    if (tgbTemp != null)
                                    {
                                        var border = tgbTemp.FindName("templateRoot", toggleButton) as Border;
                                        if (border != null)
                                            border.BorderBrush = new SolidColorBrush(
                                                Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }
#endif
                            cb.Focus();
                        }
                        else
                        {
                            // TODO .. Color
#if TODO_IMPORTANT
                            cb.BorderBrush = Colors.DarkGray;
                            cb.BorderThickness = new Thickness(1);
#endif
                        }
                    }
                }),

                new RenderRec(typeof(AnyUiCheckBox), (wts) => typeof(LabelledCheckBox), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiCheckBox cntl && b is LabelledCheckBox maui
                        && mode == AnyUiRenderMode.All)
                    {
                        // members
                        if (cntl.Background != null)
                            maui.Background = GetMauiColor(cntl.Background?.Color);
                        if (rd?.ForegroundSelfStand != null)
                            maui.Foreground = GetMauiColor(rd.ForegroundSelfStand?.Color);
                        if (cntl.Foreground != null)
                            maui.Foreground = GetMauiColor(cntl.Foreground?.Color);
                        if (cntl.IsChecked.HasValue)
                            maui.IsChecked = cntl.IsChecked.Value;
#if TODO
                        if (cntl.Padding != null)
                            maui.Padding = GetMauiTickness(cntl.Padding);
#endif

                        maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                        if (cntl.VerticalContentAlignment.HasValue)
                            maui.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                        if (cntl.HorizontalContentAlignment.HasValue)
                            maui.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);

                        if (cntl.FontMono)
                            maui.FontFamily = "Consolas";

                        if (cntl.FontWeight.HasValue)
                            maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        maui.Text = cntl.Content;
                        // callbacks
                        cntl.originalValue = cntl.IsChecked;
                        maui.Checked += async (s1, e1) =>
                        {
                            // state
                            cntl.IsChecked = maui.IsChecked == true;

                            // the value events
                            if (cntl.setValueAsyncLambda != null)
                                EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(maui.IsChecked == true));

                            // other events
                            EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                            EmitOutsideAction(cntl.takeOverLambda);
                        };
                        maui.Unchecked += async (s1, e1) =>
                        {
                            // state
                            cntl.IsChecked = maui.IsChecked == true;

                            // the value events
                            if (cntl.setValueAsyncLambda != null)
                                EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(maui.IsChecked == true));

                            // other events
                            EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                            EmitOutsideAction(cntl.takeOverLambda);
                        };
                    }
                }),

                new RenderRec(typeof(AnyUiButton), (wts) => typeof(Button), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiButton cntl && b is Button maui
                        && mode == AnyUiRenderMode.All)
                    {
                        // members
                        if (cntl.Background != null)
                            maui.Background = GetMauiBrush(cntl.Background);
                        if (rd?.ForegroundControl != null)
                            maui.TextColor = GetMauiColor(rd.ForegroundControl?.Color);
                        if (cntl.Foreground != null)
                            maui.TextColor = GetMauiColor(cntl.Foreground?.Color);
                        if (cntl.Padding != null)
                            maui.Padding = GetMauiTickness(cntl.Padding);

                        maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                        if (cntl.FontMono)
                            maui.FontFamily = "Consolas";

                        if (cntl.FontWeight.HasValue)
                            maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        maui.Text = cntl.Content;

                        if (cntl.ToolTip != null)
                        {
                            var thisToolTip = "" + cntl.ToolTip;
                            var thisMaui = maui;
                            thisMaui.HandlerChanged += (s,e) =>
                            {
#if WINDOWS
                                if (thisMaui.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button btn)
                                {
                                    Microsoft.UI.Xaml.Controls.ToolTip toolTip = new Microsoft.UI.Xaml.Controls.ToolTip();
                                    toolTip.Content = thisToolTip;
                                    Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(btn, toolTip);
                                }
#endif
                            };
                        }

                        if (cntl.ModalDialogStyle)
                        {
#if TODO_IMPORTANT
                            wpf.SetResourceReference(Control.StyleProperty, "TranspRoundCorner");
#endif
                        }

                        // callbacks
                        maui.Clicked += async (sender, e) =>
                        {
                            // normal procedure
                            if (cntl.setValueAsyncLambda != null)
                                EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(cntl));

                            // special case
                            if (cntl.SpecialAction is AnyUiSpecialActionContextMenu cntlcm
                                && cntlcm.MenuItemHeaders != null
                                && sender is View mauiSender
                                && (cntl?.DisplayData is AnyUiDisplayDataMaui ddmaui) && ddmaui.Context != null)
                            {
                                var res = await ddmaui.Context.MauiShowContextMenuForControlWrapper(mauiSender, cntlcm);

                                if (res.HasValue)
                                {
                                    var action2 = cntlcm.MenuItemLambda?.Invoke(res.Value);
                                    if (action2 == null && cntlcm.MenuItemLambdaAsync != null)
                                        action2 = await cntlcm.MenuItemLambdaAsync(res.Value);
                                    EmitOutsideAction(action2);
                                }

#if TODO_IMPORTANT
                                var nmi = cntlcm.MenuItemHeaders.Length / 2;
                                var cm = new ContextMenu();
                                for (int i = 0; i < nmi; i++)
                                {
                                    // menu item itself
                                    var mi = new MenuItem();
                                    mi.Icon = "" + cntlcm.MenuItemHeaders[2 * i + 0];
                                    mi.Header = "" + cntlcm.MenuItemHeaders[2 * i + 1];
                                    mi.Tag = i;
                                    cm.Items.Add(mi);

                                    // directly attached
                                    var bufferedI = i;
                                    mi.Click += async (sender2, e2) =>
                                    {
                                        var action2 = cntlcm.MenuItemLambda?.Invoke(bufferedI);
                                        if (action2 == null && cntlcm.MenuItemLambdaAsync != null)
                                            action2 = await cntlcm.MenuItemLambdaAsync(bufferedI);
                                        EmitOutsideAction(action2);
                                    };
                                }
                                cm.PlacementTarget = maui;
                                cm.IsOpen = true;
#endif
                            }
                        };
                    }
                })
            });
        }

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
            var mauiType = foundRR.GetMauiType?.Invoke(renderDefaults?.WidgetToolSet == null ? RenderWidgetToolSet.Normal : renderDefaults.WidgetToolSet);
            if (mauiType == null)
                return null;
            if (topClass)
                dd.MauiElement = (VisualElement?)Activator.CreateInstance(mauiType);
            if (dd.MauiElement == null)
                return null;

            // recurse (first) in the base types ..
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
                fo = dc.FindIconFont(fontFamily);
                glyph = iconText;
            }
            else
            {
                fo = dc.FindIconFont("uc");
                glyph = iconText;
            }
                
            if (fo?.FontFamily != null && glyph != null)
            {
                return new Microsoft.UI.Xaml.Controls.FontIcon
                {
                    Glyph = glyph,
                    FontFamily = fo.FontFamily,
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

            await Shell.Current.ShowPopupAsync(uc);
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

            if (mauiCntl is not Button mauiButton)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }


            var handler = mauiButton.Handler;
            if (handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button winButton)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            var flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();

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
                    Icon = ContextMenu_CreateIcon(dc, mi.IconGlyph, mi.IconFontAlias),
                    Tag = i
                };

                var thisI = i;
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

            flyout.ShowAt(winButton);

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
            return null;
        
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

            if (dialogueData is AnyUiDialogueDataSelectFromList ddsl)
            {
                var uc = new SelectFromListFlyout();
                uc.DiaData = ddsl;
                res = uc;
            }

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

    public class AnyUiColorToWpfBrushConverter : IValueConverter
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