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
    public partial class AnyUiDisplayContextMaui : AnyUiContextPlusDialogs
    {

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
            /// Height of a normal Entry field on the specific platform
            /// </summary>
            public double ControlSizeMauiStandard = 46;

            /// <summary>
            /// Height of an Entry field with height-limited Border
            /// </summary>
            public double ControlSizeBordered = 36;

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
                // measure out Label
                var lab = new Label();
                FontSizeNormal = lab.FontSize;
            }
        }

        private class RenderRec
        {
            public Type CntlType;
            public Func<RenderWidgetToolSet, AnyUiUIElement, Type> GetMauiType;
            [JsonIgnore]
            public Action<AnyUiUIElement, VisualElement, AnyUiRenderMode, RenderDefaults?>? InitLambda;
            [JsonIgnore]
            public Action<AnyUiUIElement, VisualElement, bool>? HighlightLambda;

            public Func<AnyUiUIElement, int>? CheckSuitability;

            public RenderRec(Type cntlType, Func<RenderWidgetToolSet, AnyUiUIElement, Type> getMauiType,
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

        [JsonIgnore]
        private CancellationTokenSource? _toolTipCancelTokSource;
        private DateTime? _toolTipLongPressTriggered = null;

        private void InitRenderRecs()
        {
            RenderRecs.Clear();
            RenderRecs.AddRange(new[]
            {
                new RenderRec(typeof(AnyUiUIElement), (wts, cntl) => typeof(VisualElement), null, (a, b, mode, rd) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiUIElement cntl && b is VisualElement maui
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiFrameworkElement), (wts, cntl) => typeof(View), null, (a, b, mode, rd) =>
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
                                    var evdata = new AnyUiEventData(AnyUiEventMask.LeftDown, cntl, 2, p);
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
                new RenderRec(typeof(AnyUiControl), (wts, cntl) => typeof(View), null, (a, b, mode, rd) =>
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
                new RenderRec(typeof(AnyUiContentControl), (wts, cntl) => typeof(View), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiContentControl && b is View
                       && mode == AnyUiRenderMode.All)
                   {
                   }
                }),

                // Do the  render record (basetype initialization) for AnyUiControl, even if there is no
                // directly equivalent on MAUI side
                new RenderRec(typeof(AnyUiDecorator), (wts, cntl) => typeof(ContentView), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiDecorator cntl && b is ContentView maui
                        && mode == AnyUiRenderMode.All)
                    {
                        // child
                        maui.Content = GetOrCreateMauiElement(cntl.Child, allowReUse: false, renderDefaults: rd) as View;
                    }
                }),

                new RenderRec(typeof(AnyUiViewbox), (wts, cntl) => typeof(Viewbox), null, (a, b, mode, rd) =>
                {
                   if (a is AnyUiViewbox cntl && b is Viewbox maui
                       && mode == AnyUiRenderMode.All)
                   {
#if TODO_IMPORTANT
                        wpf.Stretch = (Stretch)(int) cntl.Stretch;
#endif
                   }
                }),

                new RenderRec(typeof(AnyUiPanel), (wts, cntl) => typeof(Layout), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiGrid), (wts, cntl) => typeof(Grid), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiStackPanel), (wts, cntl) => typeof(VerticalStackLayout),
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

                new RenderRec(typeof(AnyUiStackPanel), (wts, cntl) => typeof(HorizontalStackLayout),
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

                new RenderRec(typeof(AnyUiWrapPanel), (wts, cntl) => typeof(FlexLayout), null, (a, b, mode, rd) =>
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

                       // Enable wrap
                       maui.Wrap = FlexWrap.Wrap;
                   }
                }),

                new RenderRec(typeof(AnyUiShape), (wts, cntl) => typeof(Shape), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiRectangle), (wts, cntl) => typeof(Rectangle), null, (a, b, mode, rd) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiRectangle cntl && b is Rectangle maui
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiEllipse), (wts, cntl) => typeof(Ellipse), null, (a, b, mode, rd) =>
                {
                    // ReSharper disable UnusedVariable
                    if (a is AnyUiEllipse cntl && b is Ellipse maui
                        && mode == AnyUiRenderMode.All)
                    {
                    }
                    // ReSharper enable UnusedVariable
                }),

                new RenderRec(typeof(AnyUiPolygon), (wts, cntl) => typeof(Polygon), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiCanvas), (wts, cntl) => typeof(AbsoluteLayout), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiScrollViewer), (wts, cntl) => typeof(ScrollView), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiBorder), (wts, cntl) => typeof(Border), null, (a, b, mode, rd) =>
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
                                    var evdata = new AnyUiEventData(AnyUiEventMask.LeftDown, cntl, 2, p);
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

                new RenderRec(typeof(AnyUiLabel), (wts, cntl) => typeof(Label), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiTextBlock), (wts, cntl) => typeof(Label), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiSelectableTextBlock), (wts, cntl) => typeof(Label), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiHintBubble), (wts, cntl) => typeof(Label), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiImage), (wts, cntl) => typeof(Image), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiCountryFlag), (wts, cntl) => typeof(Image), null, (a, b, mode, rd) =>
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
                (wts, cntl) => {
                    if (wts == RenderWidgetToolSet.Transparent)
                        return typeof(TransparentEntry);

                    if (cntl is AnyUiTextBox tb && tb.BorderRadius.HasValue)
                        return typeof(AbsoluteLayout);
                    else
                        return typeof(Entry /* Border */);
                },
                (anyElem) => (anyElem is AnyUiTextBox tb && tb.MultiLine == false) ? 1 : 0,
                (a, b, mode, rd) =>
                {
                    // TODO: Border in outside control!!

                    if (a is AnyUiTextBox cntl1 && b is Border maui1)
                        RenderRecInit_AnyUiTextBox_MauiBorder(cntl1, maui1, mode, rd);

                    if (a is AnyUiTextBox cntl2 && b is Entry maui2)
                        RenderRecInit_AnyUiTextBox_MauiEntry(cntl2, maui2, mode, rd);
                        
                    if (a is AnyUiTextBox cntl3 && b is AbsoluteLayout maui3)
                            RenderRecInit_AnyUiTextBox_MauiAbsoluteBorder(cntl3, maui3, mode, rd);

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
                new RenderRec(typeof(AnyUiTextBox), (wts, cntl) => typeof(Editor),
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
                (wts, cntl) => {
                    if (wts == RenderWidgetToolSet.Transparent)
                        return typeof(TransparentPicker);

                    if (cntl is AnyUiComboBox cb && cb.BorderRadius.HasValue)
                        return typeof(AbsoluteLayout);
                    else
                        return typeof(Picker /* Border */);
                },
                null, (a, b, mode, rd) =>
                {
                    // members
                    if (a is AnyUiComboBox cntl4 && b is AbsoluteLayout maui4)
                        RenderRecInit_AnyUiComboBox_MauiAbsoluteBorder(cntl4, maui4, mode, rd);

                    if (a is AnyUiComboBox cntl1 && b is Border maui1) 
                        RenderRecInit_AnyUiComboBox_MauiBorder(cntl1, maui1, mode, rd);

                    if (a is AnyUiComboBox cntl2 && b is Picker maui2) 
                        RenderRecInit_AnyUiComboBox_MauiPicker(cntl2, maui2, mode, rd);
                    
                    if (a is AnyUiComboBox cntl3 && b is TransparentPicker maui3) 
                        RenderRecInit_AnyUiComboBox_MauiTransparentPicker(cntl3, maui3, mode, rd);
                   
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

                new RenderRec(typeof(AnyUiCheckBox), (wts, cntl) => typeof(LabelledCheckBox), null, (a, b, mode, rd) =>
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

                new RenderRec(typeof(AnyUiButton), (wts, cntl) => typeof(/* Button */ Border), null, (a, b, mode, rd) =>
                {
                    if (a is AnyUiButton cntl && b is /* Button */ Border maui
                        && mode == AnyUiRenderMode.All)
                    {
                        // sort preference
                        var pref = AnyUiButtonPreference.Both;
                        if (cntl.Preference != AnyUiButtonPreference.None)
                            pref = cntl.Preference;

                        // some
                        if (cntl.Padding != null)
                            maui.Padding = GetMauiTickness(cntl.Padding);

#if __wrong__aproach
                        // members of Button
                        if (cntl.Background != null)
                            maui.Background = GetMauiBrush(cntl.Background);
                        if (rd?.ForegroundControl != null)
                            maui.TextColor = GetMauiColor(rd.ForegroundControl?.Color);
                        if (cntl.Foreground != null)
                            maui.TextColor = GetMauiColor(cntl.Foreground?.Color);
                        if (cntl.BorderColor != null)
                            maui.BorderColor = GetMauiColor(cntl.BorderColor.Color);
                        if (cntl.BorderWidth != null)
                            maui.BorderWidth = cntl.BorderWidth.Value;

                        maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                        if (cntl.FontMono)
                            maui.FontFamily = "Consolas";

                        if (cntl.FontWeight.HasValue)
                            maui.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                        // set contents
                        if (pref != AnyUiButtonPreference.Image)
                        {
                            maui.Text = cntl.Content;
                        }

                        if (pref != AnyUiButtonPreference.Text && cntl.ImageSource != null)
                        {
                            if (cntl.ImageSource is AnyUiImageSourceFont isf)
                            {
                                var reso = LambdaResolveImageSourceFont?.Invoke(this, isf);
                                if (reso != null)
                                    maui.ImageSource = new FontImageSource() {
                                        FontFamily = reso.FontAlias,
                                        Glyph = isf.IconGlyph,
                                        Size = isf.FontSize ?? 20,
                                        Color = GetMauiColor(reso.IconColor)
                                };
                            }
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

#else
                        // mimick the button
                        var hsl = new HorizontalStackLayout
                        {
                            Spacing = 6,
                            VerticalOptions = LayoutOptions.Center
                        };

                        if (cntl.HorizontalAlignment != null)
                            hsl.HorizontalOptions = GetLayoutOptions(cntl.HorizontalAlignment.Value);

                        maui.Content = hsl;

                        // maui.MinimumWidthRequest = 100;
                        maui.StrokeShape = new RoundRectangle() { CornerRadius = cntl.BorderRadius ?? 4 };

                        if (cntl.BorderColor != null)
                            maui.Stroke = GetMauiColor(cntl.BorderColor.Color);
                        if (cntl.BorderWidth != null)
                            maui.StrokeThickness = cntl.BorderWidth.Value;

                        // not clear, which order
                        IView? feText = null, feImage = null;

                        if (pref != AnyUiButtonPreference.Text && cntl.ImageSource != null)
                        {
                            if (cntl.ImageSource is AnyUiImageSourceFont isf)
                            {
                                var reso = LambdaResolveImageSourceFont?.Invoke(this, isf);
                                if (reso != null)
                                        feText = new Label
                                        {
                                            // Background = Brush.LightBlue,
                                            FontFamily = reso.FontAlias,
                                            Text = isf.IconGlyph,
                                            FontSize = isf.FontSize ?? 20,
                                            TextColor = GetMauiColor(reso.IconColor),
                                            VerticalTextAlignment = TextAlignment.Center
                                        };
                            }
                        }

                        if (pref != AnyUiButtonPreference.Image && cntl.Content?.HasContent() == true)
                        {
                            var lab = new Label
                            {
                                Text = cntl.Content,
                                // Background = Brush.MistyRose,
                                VerticalTextAlignment = TextAlignment.Center,
                                HorizontalTextAlignment = TextAlignment.Center,
                                HorizontalOptions = LayoutOptions.Center
                            };

                            lab.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

                            if (cntl.FontMono)
                                lab.FontFamily = "Consolas";

                            if (cntl.FontWeight.HasValue)
                                lab.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                            feImage = lab;
                        }

                        if (cntl.ImagePosition != AnyUiHorizontalAlignment.Right)
                        {
                            hsl.Children.Add(feText);
                            hsl.Children.Add(feImage);
                        }
                        else
                        {
                            hsl.Children.Add(feImage);
                            hsl.Children.Add(feText);
                        }

                        // "normal" callbacks
                        var myCntl = cntl;
                        var tap = new TapGestureRecognizer();
                        maui.GestureRecognizers.Add(tap);
                        tap.Tapped += async (sender, e) =>
                        {
                            // not, if a tool tip long press is pending
                            if (_toolTipLongPressTriggered != null
                                && ((DateTime.UtcNow - _toolTipLongPressTriggered.Value).TotalMilliseconds < 2500))
                                return;

                            _toolTipLongPressTriggered = null;

                            // normal procedure
                            if (myCntl.setValueAsyncLambda != null)
                                EmitOutsideAction(await myCntl.setValueAsyncLambda.Invoke(myCntl));

                            // special case
                            if (myCntl.SpecialAction is AnyUiSpecialActionContextMenu cntlcm
                                && cntlcm.MenuItemHeaders != null
                                && sender is View mauiSender
                                && (myCntl?.DisplayData is AnyUiDisplayDataMaui ddmaui) && ddmaui.Context != null)
                            {
                                var res = await ddmaui.Context.MauiShowContextMenuForControlWrapper(mauiSender, cntlcm);

                                if (res.HasValue)
                                {
                                    var action2 = cntlcm.MenuItemLambda?.Invoke(res.Value);
                                    if (action2 == null && cntlcm.MenuItemLambdaAsync != null)
                                        action2 = await cntlcm.MenuItemLambdaAsync(res.Value);
                                    EmitOutsideAction(action2);
                                }
                            }
                        };

                        // for touch devices -> click and long press for tool tip
#if WINDOWS
                        if (cntl.ToolTip != null)
                        {
                            var thisToolTip = "" + cntl.ToolTip;
                            var thisMaui = maui;
                            thisMaui.HandlerChanged += (s,e) =>
                            {
                                if (thisMaui.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement fe)
                                {
                                    Microsoft.UI.Xaml.Controls.ToolTip toolTip = new Microsoft.UI.Xaml.Controls.ToolTip();
                                    toolTip.Content = thisToolTip;
                                    Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(fe, toolTip);
                                }
                            };
                        }
#endif

#if ANDROID || MACCATALYST
                        if (cntl.ToolTip?.HasContent() == true)
                        {
                            var bh = new CommunityToolkit.Maui.Behaviors.TouchBehavior()
                            {
                                LongPressDuration = 600
                            };

                            var myToolTip = cntl.ToolTip;
                            bh.LongPressCommand = new Command(async () => {
                                _toolTipLongPressTriggered = DateTime.UtcNow;

                                await CommunityToolkit.Maui.Alerts.Toast
                                        .Make(myToolTip)
                                        .Show();
                            });

                            maui.Behaviors.Add(bh);
                        }
#endif

                        // Visual states
                        // also: timer for tool tip emulation
                        var bgColor = AnyUiColors.Transparent;
                        if (cntl.Background != null)
                            bgColor = cntl.Background?.Color;

                        var light = Application.Current?.RequestedTheme == AppTheme.Light;

                        var pointerOverColor = AnyUiColor.Overlay(bgColor, new AnyUiColor( light ? 0x08000000u : 0x08ffffffu));
                        var pressedColor = AnyUiColor.Overlay(bgColor, new AnyUiColor( light ? 0x15000000u : 0x15ffffffu));

                        var pointer = new PointerGestureRecognizer();

                        pointer.PointerEnteredCommand = new Command(() =>
                            VisualStateManager.GoToState(maui, "PointerOver"));

                        pointer.PointerExitedCommand = new Command(() =>
                            VisualStateManager.GoToState(maui, "Normal"));

                        pointer.PointerPressedCommand = new Command(() => {
                            // ALWAYS marshal to UI thread
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                VisualStateManager.GoToState(maui, "Pressed");
                            });

                            //// tool tip
                            //_toolTipLongPressTriggered = null;
                            //_toolTipCancelTokSource = new CancellationTokenSource();

                            //_ = Task.Run(async () =>
                            //{
                            //    try
                            //    {
                            //        await Task.Delay(600, _toolTipCancelTokSource.Token);

                            //        _toolTipLongPressTriggered = DateTime.UtcNow;

                            //        await MainThread.InvokeOnMainThreadAsync(async () =>
                            //        {
                            //            await CommunityToolkit.Maui.Alerts.Toast
                            //                .Make("Paste above")
                            //                .Show();
                            //        });
                            //    }
                            //    catch (TaskCanceledException)
                            //    {
                            //        // expected
                            //    }
                            //});
                        });

                        pointer.PointerReleasedCommand = new Command(() => {

                            //// re-aim
                            //if (_toolTipLongPressTriggered != null)
                            //    _toolTipLongPressTriggered = DateTime.UtcNow;

                            //_toolTipCancelTokSource?.Cancel();

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                VisualStateManager.GoToState(maui, "Normal");
                            });
                        });

                        maui.GestureRecognizers.Add(pointer);

                        VisualStateManager.SetVisualStateGroups(maui, new VisualStateGroupList
                        {
                            new VisualStateGroup
                            {
                                Name = "CommonStates",
                                States =
                                {
                                    new VisualState
                                    {
                                        Name = "Normal",
                                        Setters =
                                        {
                                            new Setter { Property = Border.BackgroundColorProperty, Value = GetMauiColor(bgColor) }
                                        }
                                    },
                                    new VisualState
                                    {
                                        Name = "PointerOver",
                                        Setters =
                                        {
                                            new Setter { Property = Border.BackgroundColorProperty, Value = GetMauiColor(pointerOverColor) }
                                        }
                                    },
                                    new VisualState
                                    {
                                        Name = "Pressed",
                                        Setters =
                                        {
                                            new Setter { Property = Border.BackgroundColorProperty, Value = GetMauiColor(pressedColor) }
                                        }
                                    }
                                }
                            }
                        });


#endif

                        if (cntl.ModalDialogStyle)
                        {
#if TODO_IMPORTANT
                            wpf.SetResourceReference(Control.StyleProperty, "TranspRoundCorner");
#endif
                        }

                    }
                })
            });
        }

        //
        // Utilities for Render Rec functions
        //

        #region RenderRec utils

        protected void SetPointerOverEffect(
            View maui,
            Setter normal,
            Setter pointerOver)
        {
            var pointer = new PointerGestureRecognizer();

            pointer.PointerEnteredCommand = new Command(() =>
                VisualStateManager.GoToState(maui, "PointerOver"));

            pointer.PointerExitedCommand = new Command(() =>
                VisualStateManager.GoToState(maui, "Normal"));

            pointer.PointerPressedCommand = new Command(() => {
                VisualStateManager.GoToState(maui, "Pressed");
            });

            pointer.PointerReleasedCommand = new Command(() => {
                VisualStateManager.GoToState(maui, "Normal");
            });

            maui.GestureRecognizers.Add(pointer);


            VisualStateManager.SetVisualStateGroups(maui, new VisualStateGroupList
                {
                    new VisualStateGroup
                    {
                        Name = "CommonStates",
                        States =
                        {
                            new VisualState
                            {
                                Name = "Normal",
                                Setters =
                                {
                                    normal
                                }
                            },
                            new VisualState
                            {
                                Name = "PointerOver",
                                Setters =
                                {
                                    pointerOver
                                }
                            }
                        }
                    }
                });
        }

        #endregion


        //
        // Indiviudual Render Rec functions
        //

        #region TextBox ..

        /// <summary>
        /// TextBox -> AbsoluteLayout
        /// </summary>
        protected void RenderRecInit_AnyUiTextBox_MauiAbsoluteBorder(
            AnyUiTextBox cntl,
            Microsoft.Maui.Controls.AbsoluteLayout maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
        {
            if (mode == AnyUiRenderMode.All)
            {
                // allow clear names
                var absLayout = maui;
                var border = new Border();
                var entry = new Entry();                
                absLayout.Add(border);                
                border.Content = entry;

                // set absolute layout
                absLayout.HeightRequest = rd?.ControlSizeBordered ?? -1;
                absLayout.HorizontalOptions = LayoutOptions.Fill;
                // absLayout.Background = Brush.LightBlue;

                // ok, border is the wrapping control
                absLayout.SetLayoutBounds(border, new Rect(0, 0, 1, 1));
                absLayout.SetLayoutFlags(border, AbsoluteLayoutFlags.All);
                border.HorizontalOptions = LayoutOptions.Fill;
                border.Padding = GetMauiTickness(cntl.BorderPadding);
                border.StrokeShape = new RoundRectangle() { CornerRadius = cntl.BorderRadius ?? 0 };
                //if (cntl.BorderColor != null)
                //    border.Stroke = GetMauiColor(cntl.BorderColor.Color);
                if (cntl.BorderWidth != null)
                    border.StrokeThickness = cntl.BorderWidth.Value;
                if (cntl.Background != null)
                    border.Background = GetMauiBrush(cntl.Background);
                if (cntl.BorderPadding != null)
                    border.Padding = GetMauiTickness(cntl.BorderPadding);

                // for the entry, set many attributes to visually neutral
                absLayout.SetLayoutBounds(entry, new Rect(0, 0, 1, 1));
                absLayout.SetLayoutFlags(entry, AbsoluteLayoutFlags.All);
                entry.BackgroundColor = Colors.Transparent;
                entry.HeightRequest = -1; // lets the parent control sizing
                entry.HorizontalOptions = LayoutOptions.Fill;
                entry.VerticalTextAlignment = TextAlignment.Center;
                if (cntl.Padding != null)
                    entry.Margin = GetMauiTickness(cntl.Padding);
                if (cntl.VerticalContentAlignment.HasValue)
                    entry.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                if (cntl.HorizontalContentAlignment.HasValue)
                    entry.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);
                entry.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);
                if (rd?.ForegroundControl != null)
                    entry.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                if (cntl.Foreground != null)
                    entry.TextColor = GetMauiColor(cntl.Foreground?.Color);
                if (cntl.IsReadOnly)
                    entry.IsReadOnly = cntl.IsReadOnly;
                if (cntl.FontMono)
                    entry.FontFamily = "Consolas";
                if (cntl.FontWeight.HasValue)
                    entry.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);
                entry.Text = cntl.Text;

                // for the entry, set many attributes to visually neutral
                Label? plateLabel = null;
                if (rd != null && cntl.PlateLabel?.Text?.HasContent() == true)
                {
                    plateLabel = new();
                    absLayout.Add(plateLabel);
                    absLayout.SetLayoutBounds(plateLabel, new Rect(0, 0, 1, 1));
                    absLayout.SetLayoutFlags(plateLabel, AbsoluteLayoutFlags.All);
                    plateLabel.HeightRequest = -1; // lets the parent control sizing
                    plateLabel.HorizontalOptions = LayoutOptions.Start;
                    plateLabel.VerticalOptions = LayoutOptions.Start;
                    plateLabel.VerticalTextAlignment = TextAlignment.Start;
                    plateLabel.Margin = GetMauiTickness(cntl.PlateLabel.Margin);
                    plateLabel.Padding = GetMauiTickness(cntl.PlateLabel.Padding);
                    plateLabel.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize, cntl.PlateLabel.FontSizeRel);
                    plateLabel.Text = cntl.PlateLabel.Text;
                    if (cntl.PlateLabel.Foreground != null)
                        plateLabel.TextColor = GetMauiColor(cntl.PlateLabel.Foreground?.Color);
                    if (cntl.PlateLabel.Background != null)
                        plateLabel.BackgroundColor = GetMauiColor(cntl.PlateLabel.Background?.Color);
                }

                // callbacks
                cntl.originalValue = "" + cntl.Text;
                entry.TextChanged += async (sender, e) => {
                    // state
                    cntl.Text = entry.Text;

                    // the value event
                    if (cntl.setValueAsyncLambda != null)
                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(entry.Text));

                    // other events
                    EmitOutsideAction(new AnyUiLambdaActionContentsChanged());
                };
                entry.Completed += (sender, e) =>
                {
                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                    EmitOutsideAction(cntl.takeOverLambda);
                };

                // visual focus
                var normalStrokeColor = cntl.BorderColor?.Color ?? new AnyUiColor(0xffd8d8d8);
                SetPointerOverEffect(border,
                    new Setter {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(normalStrokeColor)
                    },
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(AnyUiColor.Overlay(normalStrokeColor, new AnyUiColor(0x40000000)))
                    });
            }

            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
            {
                // TODO !!
                // entry.Text = cntl.Text;
            }
        }

        /// <summary>
        /// TextBox -> Border (around entry) -> idea to limit screen real estate
        /// </summary>
        protected void RenderRecInit_AnyUiTextBox_MauiBorder(
            AnyUiTextBox cntl,
            Microsoft.Maui.Controls.Border maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
        {
            if (mode == AnyUiRenderMode.All)
            {
                // allow clear names
                var border = maui;
                var entry = new Entry();
                border.Content = entry;

                // for the entry, set many attributes to visually neutral
                entry.BackgroundColor = Colors.Transparent;
                entry.HeightRequest = -1; // lets the parent control sizing
                entry.HorizontalOptions = LayoutOptions.Fill;
                entry.VerticalTextAlignment = TextAlignment.Center;
                if (cntl.VerticalContentAlignment.HasValue)
                    entry.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                if (cntl.HorizontalContentAlignment.HasValue)
                    entry.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);
                entry.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);
                if (rd?.ForegroundControl != null)
                    entry.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                if (cntl.Foreground != null)
                    entry.TextColor = GetMauiColor(cntl.Foreground?.Color);
                if (cntl.IsReadOnly)
                    entry.IsReadOnly = cntl.IsReadOnly;
                if (cntl.FontMono)
                    entry.FontFamily = "Consolas";
                if (cntl.FontWeight.HasValue)
                    entry.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);
                entry.Text = cntl.Text;

                // ok, border is the wrapping control
                border.Padding = new Thickness(0, -4, 0, 0);
                border.HeightRequest = rd?.ControlSizeBordered ?? -1;
                border.StrokeShape = new RoundRectangle() { CornerRadius = 16 };

                //if (cntl.BorderColor != null)
                //    border.Stroke = GetMauiColor(cntl.BorderColor.Color);
                if (cntl.BorderWidth != null)
                    border.StrokeThickness = cntl.BorderWidth.Value;
                if (cntl.Background != null)
                    border.Background = GetMauiBrush(cntl.Background);
                if (cntl.Padding != null)
                    border.Padding = GetMauiTickness(cntl.Padding);

                // callbacks
                cntl.originalValue = "" + cntl.Text;
                entry.TextChanged += async (sender, e) => {
                    // state
                    cntl.Text = entry.Text;

                    // the value event
                    if (cntl.setValueAsyncLambda != null)
                        EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke(entry.Text));

                    // other events
                    EmitOutsideAction(new AnyUiLambdaActionContentsChanged());
                };
                entry.Completed += (sender, e) =>
                {
                    EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());
                    EmitOutsideAction(cntl.takeOverLambda);
                };

                // visual focus
                var normalStrokeColor = cntl.BorderColor?.Color ?? new AnyUiColor(0xffd8d8d8);
                SetPointerOverEffect(maui,
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(normalStrokeColor)
                    },
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(AnyUiColor.Overlay(normalStrokeColor, new AnyUiColor(0x40000000)))
                    });
            }

            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
            {
                // TODO !!
                // entry.Text = cntl.Text;
            }
        }

        /// <summary>
        /// TextBox -> Entry .. straight forward, may be not used?!
        /// </summary>
        protected void RenderRecInit_AnyUiTextBox_MauiEntry(
            AnyUiTextBox cntl,
            Microsoft.Maui.Controls.Entry maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
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

        /// <summary>
        /// TextBox -> TransparentEntry -> used in modal dialogs .. plenty of screen real estate
        /// </summary>
        protected void RenderRecInit_AnyUiTextBox_MauiTransparentEntry(
            AnyUiTextBox cntl,
            TransparentEntry maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
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

        #endregion

        #region Picker

        /// <summary>
        /// ComboBox -> Border with Picker inside
        /// </summary>
        protected void RenderRecInit_AnyUiComboBox_MauiAbsoluteBorder(
            AnyUiComboBox cntl,
            AbsoluteLayout maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
        {
            if (mode == AnyUiRenderMode.All)
            {
                // allow clear names
                var absLayout = maui;
                var border = new Border();
                var picker = new Picker();
                absLayout.Add(border);
                border.Content = picker;

                // set absolute layout
                absLayout.HeightRequest = rd?.ControlSizeBordered ?? -1;
                absLayout.HorizontalOptions = LayoutOptions.Start;
                // absLayout.Background = Brush.LightBlue;

                // ok, border is the wrapping control
                absLayout.SetLayoutBounds(border, new Rect(0, 0, 1, 1));
                absLayout.SetLayoutFlags(border, AbsoluteLayoutFlags.All);
                border.HorizontalOptions = LayoutOptions.Fill;
                border.VerticalOptions = LayoutOptions.Center;
                border.Margin = new Thickness(0);
                border.Padding = GetMauiTickness(cntl.BorderPadding);
                border.StrokeShape = new RoundRectangle() { CornerRadius = cntl.BorderRadius ?? 0 };
                border.HeightRequest = rd?.ControlSizeBordered ?? -1;
                if (cntl.BorderColor != null)
                    border.Stroke = GetMauiColor(cntl.BorderColor.Color);
                if (cntl.BorderWidth != null)
                    border.StrokeThickness = cntl.BorderWidth.Value;
                if (cntl.Background != null)
                    border.Background = GetMauiBrush(cntl.Background);

                // for the picker, set many attributes to visually neutral
                picker.BackgroundColor = Colors.Transparent;
                picker.HeightRequest = -1; // lets the parent control sizing
                picker.HorizontalOptions = LayoutOptions.Fill;
                picker.VerticalOptions = LayoutOptions.Center;
                picker.VerticalTextAlignment = TextAlignment.Center;
                if (cntl.VerticalContentAlignment.HasValue)
                    picker.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                if (cntl.HorizontalContentAlignment.HasValue)
                    picker.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);
                picker.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);
                if (rd?.ForegroundControl != null)
                    picker.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                if (cntl.Foreground != null)
                    picker.TextColor = GetMauiColor(cntl.Foreground?.Color);
                if (cntl.FontMono)
                    picker.FontFamily = "Consolas";
                if (cntl.FontWeight.HasValue)
                    picker.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                // TODO
                // picker.Text = cntl.Text;

#if TODO_IMPORTANT
                if (cntl.IsEditable.HasValue)
                    maui.IsEditable = cntl.IsEditable.Value;
#endif

                if (cntl.Items != null)
                {
                    foreach (var i in cntl.Items)
                        picker.Items.Add(i?.ToString());
                }

#if TODO_IMPORTANT
                maui.Text = cntl.Text;
#endif

                if (cntl.Text != null && cntl.Text.Length > 0 && !cntl.SelectedIndex.HasValue
                    && cntl.Items != null)
                {
                    // use the existing text to set the combo box value via SelectedIndex
                    int ndx = -1;
                    for (int i = 0; i < cntl.Items.Count; i++)
                        if (cntl.Text.Trim().Equals(cntl.Items[i].ToString()?.Trim(),
                            StringComparison.InvariantCultureIgnoreCase))
                            ndx = i;
                    if (ndx >= 0)
                        cntl.SelectedIndex = ndx;
                }

                if (cntl.SelectedIndex.HasValue)
                    picker.SelectedIndex = cntl.SelectedIndex.Value;

                // for the entry, set many attributes to visually neutral
                Label? plateLabel = null;
                if (rd != null && cntl.PlateLabel?.Text?.HasContent() == true)
                {
                    plateLabel = new();
                    absLayout.Add(plateLabel);
                    absLayout.SetLayoutBounds(plateLabel, new Rect(0, 0, 1, 1));
                    absLayout.SetLayoutFlags(plateLabel, AbsoluteLayoutFlags.All);
                    plateLabel.HeightRequest = -1; // lets the parent control sizing
                    plateLabel.HorizontalOptions = LayoutOptions.Start;
                    plateLabel.VerticalOptions = LayoutOptions.Start;
                    plateLabel.VerticalTextAlignment = TextAlignment.Start;
                    plateLabel.Margin = GetMauiTickness(cntl.PlateLabel.Margin);
                    plateLabel.Padding = GetMauiTickness(cntl.PlateLabel.Padding);
                    plateLabel.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize, cntl.PlateLabel.FontSizeRel);
                    plateLabel.Text = cntl.PlateLabel.Text;
                    if (cntl.PlateLabel.Foreground != null)
                        plateLabel.TextColor = GetMauiColor(cntl.PlateLabel.Foreground?.Color);
                    if (cntl.PlateLabel.Background != null)
                        plateLabel.BackgroundColor = GetMauiColor(cntl.PlateLabel.Background?.Color);
                }

                // callbacks
                cntl.originalValue = "" + cntl.Text;
                // TODO!!
                if (true || cntl.IsEditable != true)
                {
                    // we need this event
                    picker.SelectedIndexChanged += async (s, e) =>
                    {
                        // state
                        cntl.SelectedIndex = picker.SelectedIndex;
                        cntl.Text = picker.SelectedItem as string;

                        // the value event
                        if (cntl.setValueAsyncLambda != null)
                            EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke((string)picker.SelectedItem));

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

                // visual focus
                var normalStrokeColor = cntl.BorderColor?.Color ?? new AnyUiColor(0xffd8d8d8);
                SetPointerOverEffect(maui,
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(normalStrokeColor)
                    },
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(AnyUiColor.Overlay(normalStrokeColor, new AnyUiColor(0x40000000)))
                    });

            }

            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
            {
            }
        }

        /// <summary>
        /// ComboBox -> Border with Picker inside
        /// </summary>
        protected void RenderRecInit_AnyUiComboBox_MauiBorder(
            AnyUiComboBox cntl,
            Border maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
        {
            if (mode == AnyUiRenderMode.All)
            {
                // allow clear names
                var border = maui;
                var picker = new Picker();
                border.Content = picker;

                // for the picker, set many attributes to visually neutral
                picker.BackgroundColor = Colors.Transparent;
                picker.HeightRequest = -1; // lets the parent control sizing
                picker.HorizontalOptions = LayoutOptions.Fill;
                picker.VerticalOptions = LayoutOptions.Center;
                picker.VerticalTextAlignment = TextAlignment.Center;
                if (cntl.VerticalContentAlignment.HasValue)
                    picker.VerticalTextAlignment = GetTextAlignment(cntl.VerticalContentAlignment.Value);
                if (cntl.HorizontalContentAlignment.HasValue)
                    picker.HorizontalTextAlignment = GetTextAlignment(cntl.HorizontalContentAlignment.Value);
                picker.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);
                if (rd?.ForegroundControl != null)
                    picker.TextColor = GetMauiColor(rd.ForegroundControl.Color);
                if (cntl.Foreground != null)
                    picker.TextColor = GetMauiColor(cntl.Foreground?.Color);
                if (cntl.FontMono)
                    picker.FontFamily = "Consolas";
                if (cntl.FontWeight.HasValue)
                    picker.FontAttributes = GetFontAttributesFrom(cntl.FontWeight.Value);

                // TODO
                // picker.Text = cntl.Text;

                if (cntl.Text == "en")
                    ;

                // ok, border is the wrapping control
                border.VerticalOptions = LayoutOptions.Center;
                border.Margin = new Thickness(0);
                border.Padding = new Thickness(0, 0, 0, 0);
                border.HeightRequest = rd?.ControlSizeBordered ?? -1;
                border.StrokeShape = new RoundRectangle() { CornerRadius = cntl.BorderRadius ?? 0 };

                if (cntl.BorderColor != null)
                    border.Stroke = GetMauiColor(cntl.BorderColor.Color);
                if (cntl.BorderWidth != null)
                    border.StrokeThickness = cntl.BorderWidth.Value;
                if (cntl.Background != null)
                    border.Background = GetMauiBrush(cntl.Background);
                //if (cntl.Padding != null)
                //    border.Padding = GetMauiTickness(cntl.Padding);

                if (cntl.Background != null)
                    maui.Background = GetMauiBrush(cntl.Background);

#if TODO_IMPORTANT
                if (cntl.Padding != null)
                    maui.Padding = GetMauiTickness(cntl.Padding);
                if (cntl.IsEditable.HasValue)
                    maui.IsEditable = cntl.IsEditable.Value;
#endif

                if (cntl.Items != null)
                {
                    foreach (var i in cntl.Items)
                        picker.Items.Add(i?.ToString());
                }

#if TODO_IMPORTANT
                maui.Text = cntl.Text;
#endif

                if (cntl.Text != null && cntl.Text.Length > 0 && !cntl.SelectedIndex.HasValue
                    && cntl.Items != null)
                {
                    // use the existing text to set the combo box value via SelectedIndex
                    int ndx = -1;
                    for (int i = 0; i < cntl.Items.Count; i++)
                        if (cntl.Text.Trim().Equals(cntl.Items[i].ToString()?.Trim(),
                            StringComparison.InvariantCultureIgnoreCase))
                            ndx = i;
                    if (ndx >= 0)
                        cntl.SelectedIndex = ndx;
                }

                if (cntl.SelectedIndex.HasValue)
                    picker.SelectedIndex = cntl.SelectedIndex.Value;

                // callbacks
                cntl.originalValue = "" + cntl.Text;
                // TODO!!
                if (true || cntl.IsEditable != true)
                {
                    // we need this event
                    picker.SelectedIndexChanged += async (s, e) =>
                    {
                        // state
                        cntl.SelectedIndex = picker.SelectedIndex;
                        cntl.Text = picker.SelectedItem as string;

                        // the value event
                        if (cntl.setValueAsyncLambda != null)
                            EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke((string)picker.SelectedItem));

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

                // visual focus
                var normalStrokeColor = cntl.BorderColor?.Color ?? new AnyUiColor(0xffd8d8d8);
                SetPointerOverEffect(maui,
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(normalStrokeColor)
                    },
                    new Setter
                    {
                        Property = Border.StrokeProperty,
                        Value = GetMauiColor(AnyUiColor.Overlay(normalStrokeColor, new AnyUiColor(0x40000000)))
                    });

            }

            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
            {
            }
        }

        /// <summary>
        /// ComboBox -> just Picker .. conventional
        /// </summary>
        protected void RenderRecInit_AnyUiComboBox_MauiPicker(
            AnyUiComboBox cntl,
            Picker maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
        {
            if (mode == AnyUiRenderMode.All)
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
                    for (int i = 0; i < cntl.Items.Count; i++)
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
                            EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke((string)maui.SelectedItem));

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
            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
            {
            }
        }
 
        /// <summary>
        /// ComboBox -> just Picker .. conventional
        /// </summary>
        protected void RenderRecInit_AnyUiComboBox_MauiTransparentPicker(
            AnyUiComboBox cntl,
            TransparentPicker maui,
            AnyUiRenderMode mode,
            RenderDefaults? rd)
        {
            if (mode == AnyUiRenderMode.All)
            {
                maui.FontSize = GetFontSizeFromRelative(rd, cntl.FontSize);

#if TODO_IMPORTANT
                if (cntl.Padding != null)
                    maui.Padding = GetMauiTickness(cntl.Padding);
                if (cntl.IsEditable.HasValue)
                    maui.IsEditable = cntl.IsEditable.Value;
#endif

                if (cntl.Items != null)
                {
                    maui.ItemsSource = cntl.Items;
                }

#if TODO_IMPORTANT
                maui.Text = cntl.Text;
#endif

                if (cntl.Text != null && cntl.Text.Length > 0 && !cntl.SelectedIndex.HasValue
                    && cntl.Items != null)
                {
                    // use the existing text to set the combo box value via SelectedIndex
                    int ndx = -1;
                    for (int i = 0; i < cntl.Items.Count; i++)
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
                if (cntl.IsEditable != true)
                {
                    // we need this event
                    maui.SelectedIndexChanged += async (s, e) =>
                    {
                        // state
                        cntl.SelectedIndex = maui.SelectedIndex;
                        cntl.Text = maui.SelectedItem as string;

                        // the value event
                        if (cntl.setValueAsyncLambda != null)
                            EmitOutsideAction(await cntl.setValueAsyncLambda.Invoke((string)maui.SelectedItem));

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
            if (mode == AnyUiRenderMode.All || mode == AnyUiRenderMode.StatusToUi)
            {
            }
        }

        #endregion
    }
}