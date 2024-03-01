/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;
using AasxPredefinedConcepts;
using System.Reflection.PortableExecutable;

using PDPCN = AasxPredefinedConcepts.ProductChangeNotifications;
using System.Windows.Documents;
using System.Linq;
using System.Globalization;
using System.Windows.Controls;
using AasxIntegrationBaseGdi;

namespace AasxPluginProductChangeNotifications
{
    public class PcnAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private PcnOptions _options = null;
        private PluginEventStack _eventStack = null;
        private PluginSessionBase _session = null;
        private AnyUiStackPanel _panel = null;
        private AasxPluginBase _plugin = null;

        private PDPCN.CD_ProductChangeNotifications _pcnData = new PDPCN.CD_ProductChangeNotifications();

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        #endregion

        #region Members to be kept for state/ update
        //=============

        protected double _lastScrollPosition = 0.0;

        protected int _selectedLangIndex = 0;
        protected string _selectedLangStr = null;

        protected int _pcnIndex = 0;

        #endregion

        #region Constructors
        //=============

        public PcnAnyUiControl()
        {
        }

        public void Dispose()
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            PcnOptions theOptions,
            PluginEventStack eventStack,
            PluginSessionBase session,
            AnyUiStackPanel panel,
            AasxPluginBase plugin)
        {
            // internal members
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _session = session;
            _panel = panel;
            _plugin = plugin;

            // fill given panel
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        public static PcnAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            PcnOptions options,
            PluginEventStack eventStack,
            PluginSessionBase session,
            object opanel,
            AasxPluginBase plugin)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as Aas.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // factory this object
            var aidCntl = new PcnAnyUiControl();
            aidCntl.Start(log, package, sm, options, eventStack, session, panel, plugin);

            // return shelf
            return aidCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullView(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            AdminShellPackageEnv package,
            Aas.Submodel sm)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel
            var foundRecs = new List<PcnOptionsRecord>();
            foreach (var rec in _options.LookupAllIndexKey<PcnOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRecs.Add(rec);

            // try decode
            _pcnData = new PDPCN.CD_ProductChangeNotifications();
            PredefinedConceptsClassMapper.ParseAasElemsToObject(
                sm, _pcnData, 
                lambdaLookupReference: (rf) => package?.AasEnv?.FindReferableByReference(rf));

            // render
            RenderPanelOutside(view, uitk, foundRecs, package, sm, _pcnData);
        }

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            IEnumerable<PcnOptionsRecord> foundRecs,
            AdminShellPackageEnv package,
            Aas.Submodel sm,
            PDPCN.CD_ProductChangeNotifications data)
        {
            // make an outer grid, very simple grid of three rows: header, list, details
            var outer = view.Add(uitk.AddSmallGrid(rows: 7, cols: 1, colWidths: new[] { "*" }));

            //
            // Bluebar
            //

            var bluebar = uitk.AddSmallGridTo(outer, 0, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

            bluebar.Margin = new AnyUiThickness(0);
            bluebar.Background = AnyUiBrushes.LightBlue;

            uitk.AddSmallBasicLabelTo(bluebar, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"PCN #{_pcnIndex:D3}");

            Func<int, AnyUiLambdaActionBase> lambdaButtonClick = (i) => {
                // mode change
                if (data?.Records?.Record != null)
                    switch (i)
                    {
                        case 0:
                            _pcnIndex = 0;
                            break;
                        case 1:
                            _pcnIndex = Math.Max(0, _pcnIndex - 1);
                            break;
                        case 2:
                            _pcnIndex = Math.Min(data.Records.Record.Count - 1, _pcnIndex + 1);
                            break;
                        case 3:
                            _pcnIndex = data.Records.Record.Count - 1;
                            break;
                    }

                //redisplay
                PushUpdateEvent();
                return new AnyUiLambdaActionNone();
            };

            for (int i = 0; i < 4; i++)
            {
                var thisI = i;
                AnyUiUIElement.RegisterControl(
                    uitk.AddSmallButtonTo(bluebar, 0, 1 + i,
                        margin: new AnyUiThickness(2), setHeight: 21,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        content: (new[] { "\u2759\u25c0", "\u25c0", "\u25b6", "\u25b6\u2759" })[i]),
                        (o) => lambdaButtonClick(thisI));
            }

            //
            // Scroll area (list)
            //

            // small spacer
            outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[2] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 2, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    flattenForTarget: AnyUiTargetPlatform.Browser, initialScrollPosition: _lastScrollPosition),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                });

            // content of the scroll viewer
            // need a stack panel to add inside
            var inner = new AnyUiStackPanel()
            {
                Orientation = AnyUiOrientation.Vertical,
                Margin = new AnyUiThickness(2, 2, 8, 2)
            };
            scroll.Content = inner;

            if (foundRecs != null)
                foreach (var rec in foundRecs)
                    if (data?.Records?.Record != null
                        && _pcnIndex >= 0 && _pcnIndex < data.Records.Record.Count)
                        RenderPanelInner(inner, uitk, rec, package, sm, data.Records.Record[_pcnIndex]);
        }

        #endregion

        #region Inner
        //=============

        protected AnyUiLambdaActionBase TriggerUpdate(bool full = true)
        {
            // trigger a complete redraw, as the regions might emit 
            // events or not, depending on this flag
            return new AnyUiLambdaActionPluginUpdateAnyUi()
            {
                PluginName = _plugin?.GetPluginName(),
                UpdateMode = AnyUiRenderMode.All,
                UseInnerGrid = true
            };
        }

        protected int _innerDocumentCols = 6;

        protected int InnerDocGetNewRow(AnyUiGrid grid)
        {
            if (grid == null)
                return 0;
            int row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new AnyUiRowDefinition());
            return row;
        }

        protected void InnerDocAddHeadline(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col,
            string heading, int level,
            int assignRow = -1)
        {
            // access and add row
            if (grid == null)
                return;
            int row = (assignRow >= 0) ? assignRow : InnerDocGetNewRow(grid);

            var bg = AnyUi.AnyUiBrushes.DarkBlue;
            var fg = AnyUi.AnyUiBrushes.White;
            var bold = true;

            if (level == 2)
            {
                bg = AnyUi.AnyUiBrushes.LightBlue;
                fg = AnyUi.AnyUiBrushes.Black;
                bold = true;
            }

            uitk.Set(
                uitk.AddSmallBasicLabelTo(grid, row, col,
                    colSpan: _innerDocumentCols - col,
                    background: bg,
                    foreground: fg,
                    content: heading,
                    setBold: bold),
                margin: new AnyUiThickness(0, 14, 0, 6));
        }

        protected void InnerDocAddText(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col, int keyCols,
            string key,
            string text)
        {
            // access and add row
            if (grid == null)
                return;
            int row = InnerDocGetNewRow(grid);

            // key
            uitk.AddSmallBasicLabelTo(grid, row, col,
                colSpan: _innerDocumentCols - col,
                content: key,
                setBold: true);

            // text
            uitk.AddSmallBasicLabelTo(grid, row, col + keyCols,
                colSpan: _innerDocumentCols - col - keyCols,
                content: text,
                setBold: false);
        }

        protected void InnerDocAddLifeCycleMilestones(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col, 
            IList<PDPCN.CD_LifeCycleMilestone> milestones)
        {
            // access and add row
            if (grid == null || milestones == null || milestones.Count < 1)
                return;
            int row = InnerDocGetNewRow(grid);

            // make inner grid
            var inner = uitk.Set(
                uitk.AddSmallGridTo(grid, row, col, 
                    rows: 1, cols: milestones.Count + 1,
                    colWidths: "#".Times(milestones.Count).Add("*").ToArray()),
                colSpan: _innerDocumentCols - col);

            // have milestone name to color mapping
            var x = new Dictionary<string, AnyUiBrush>();
            x.Add("SOP",  new AnyUiBrush("#0128CB"));
            x.Add("NRND", new AnyUiBrush("#F9F871"));
            x.Add("PCN",  new AnyUiBrush("#00B9CD"));
            x.Add("PDN",  new AnyUiBrush("#FFBA44"));
            x.Add("EOS",  new AnyUiBrush("#FF724F"));
            x.Add("EOP",  new AnyUiBrush("#FF0076"));
            x.Add("LTD",  new AnyUiBrush("#B10000"));
            x.Add("EOSR", new AnyUiBrush("#C400A6"));

            // single "boxes"
            for (int ci=0; ci<milestones.Count; ci++) 
            {
                // make the border
                var brd = uitk.AddSmallBorderTo(inner, 0, ci,
                                margin: (ci == 0) ? new AnyUiThickness(0, -1, 0, 0)
                                                  : new AnyUiThickness(-1, -1, 0, 0),
                                borderThickness: new AnyUiThickness(1.0), 
                                borderBrush: AnyUiBrushes.Black);

                // find a nice color coding
                var bg = AnyUiBrushes.Transparent;
                var lookup = ("" + milestones[ci].MilestoneClassification).ToUpper().Trim();
                if (x.ContainsKey(lookup))
                    bg = x[lookup];
                var fg = AnyUiBrushes.Black;
                if (bg.Color.Blackness() > 0.5)
                    fg = AnyUiBrushes.White;

                // provide a nice date
                var vd = "" + milestones[ci].DateOfValidity;
                if (DateTime.TryParseExact(vd,
                    "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal, out DateTime dt))
                {
                    var delta = dt - DateTime.UtcNow;
                    var sign = (delta > TimeSpan.Zero) ? "in" : "ago";
                    var days = Convert.ToInt32(Math.Abs(delta.TotalDays));
                    var years = 0;
                    if (days > 365)
                    {
                        years = days / 365;
                        days = days % 365;
                    }
                    vd = $"{sign} {(years > 0 ? $"{years} yrs " : "")} {days} days";
                }

                // make the 2 row content grid
                var mg = uitk.AddSmallGrid(2, 1, new[] { "110:" }, new[] { "40:", "#" },
                background: bg,
                margin: new AnyUiThickness(2));
                brd.Child = mg;

                uitk.AddSmallBasicLabelTo(mg, 0, 0,   
                    foreground: fg,
                    fontSize: 1.5f,
                    setBold: true,
                    horizontalAlignment: AnyUiHorizontalAlignment.Center,
                    horizontalContentAlignment: AnyUiHorizontalAlignment.Center,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    content: "" + milestones[ci].MilestoneClassification);

                uitk.AddSmallBasicLabelTo(mg, 1, 0,
                    foreground: fg,
                    fontSize: 0.8f,
                    setBold: false,
                    horizontalAlignment: AnyUiHorizontalAlignment.Center,
                    horizontalContentAlignment: AnyUiHorizontalAlignment.Center,
                    content: vd,
                    margin: new AnyUiThickness(0, 0, 0, 4));
            }
        }

        protected void RenderPanelInner(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            PcnOptionsRecord rec,
            AdminShellPackageEnv package,
            Aas.Submodel sm,
            PDPCN.CD_Record data)
        {
            // access
            if (view == null || uitk == null || sm == null || rec == null)
                return;

            // 
            // Document approach via Grid.
            // Basically one large grid, which helps synchronizing multiple identation levels.
            //

            var grid = view.Add(uitk.AddSmallGrid(rows: 5, cols: _innerDocumentCols, 
                colWidths: new[] { "70:", "70:", "70:", "70:", "70:", "*" }));

            // Manufacturer
            if (data.Manufacturer != null)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Manufacturer", 1);
                
                InnerDocAddText(uitk, grid, 0, 2, "ManufacturerName", 
                    data.Manufacturer.ManufacturerName?.GetDefaultString(_selectedLangStr));

                InnerDocAddText(uitk, grid, 0, 2, "AdressInformation",
                    "<TBD>");

                InnerDocAddText(uitk, grid, 0, 2, "ManufacturerChangeID",
                    "" + data.ManufacturerChangeID);
            }

            // Life cylce mile stones
            if (data.LifeCycleData?.Milestone != null && data.LifeCycleData.Milestone.Count > 0)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Life cycle mile stones", 1);

                InnerDocAddLifeCycleMilestones(uitk, grid, 0, data.LifeCycleData.Milestone);
            }

            // asset, partnumbers, items
            if (data.ItemOfChange != null)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Identfication of changed item", 1);

                InnerDocAddText(uitk, grid, 0, 2, "ManufacturerAssetID",
                    "" + data.ItemOfChange.ManufacturerAssetID?.Value?.ToStringExtended(1));

                // make a grid for two columns
                int row = InnerDocGetNewRow(grid);
                var twoColGrid =
                    uitk.Set(
                        uitk.AddSmallGridTo(grid, row, 0, rows: 1, cols: 2, new[] { "1*", "2*" }),
                        colSpan: _innerDocumentCols);

                // access asset information
                if (data.ItemOfChange.ManufacturerAssetID?.ValueHint is Aas.IAssetAdministrationShell aas
                    && aas.AssetInformation is Aas.IAssetInformation ai
                    && ai.DefaultThumbnail?.Path != null)
                {
                    var img = AnyUiGdiHelper.LoadBitmapInfoFromPackage(package, ai.DefaultThumbnail.Path);

                    uitk.Set(
                        uitk.AddSmallImageTo(twoColGrid, 0, 0,
                            margin: new AnyUiThickness(2, 8, 2, 2),
                            stretch: AnyUiStretch.Uniform,
                            bitmap: img),
                    maxHeight: 400, maxWidth: 400,
                    rowSpan:2,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);
                }

                // identification info
                if (true)
                {
                    var colTwoGrid =
                    uitk.Set(
                        uitk.AddSmallGridTo(twoColGrid, 0, 1,
                            rows: 1, cols: 3, new[] { "70:", "70:", "*" }));

                    InnerDocAddHeadline(uitk, colTwoGrid, 0, "ItemOfChange", 1,
                        assignRow: 0);

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "Mfg.Prod.Family",
                        data.ItemOfChange.ManufacturerProductFamily?.GetDefaultString(_selectedLangStr));

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "Mfg.Prod.Deign.",
                        data.ItemOfChange.ManufacturerProductDesignation?.GetDefaultString(_selectedLangStr));

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "Order Code Mfg.",
                        data.ItemOfChange.OrderCodeOfManufacturer?.GetDefaultString(_selectedLangStr));
                }
            }

        }

        #endregion

        #region Event handling
        //=============

        private Action<AasxPluginEventReturnBase> _menuSubscribeForNextEventReturn = null;

        protected void PushUpdateEvent(AnyUiRenderMode mode = AnyUiRenderMode.All)
        {
            // bring it to the panel by redrawing the plugin
            _eventStack?.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
            {
                // get the always currentplugin name
                PluginName = _plugin?.GetPluginName(),
                Session = _session,
                Mode = mode,
                UseInnerGrid = true
            });
        }

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            // demands from shelf
            if (_menuSubscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = _menuSubscribeForNextEventReturn;
                _menuSubscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);

                // finish
                return;
            }
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 1
                || !(args[0] is AnyUiStackPanel newPanel))
                return;

            // ok, re-assign panel and re-display
            _panel = newPanel;
            _panel.Children.Clear();

            // the default: the full shelf
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        #endregion

        #region STUFF
        //=====================
        
        #endregion

        #region Callbacks
        //===============


        #endregion

        #region Utilities
        //===============


        #endregion
    }
}
