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

        protected static Dictionary<string, PDPCN.PcnReasonDescription> _dictIdToReason =
            PDPCN.PcnReasonDescription.BuildDict();

        protected static Dictionary<string, PDPCN.PcnItemDescription> _dictIdToItem =
            PDPCN.PcnItemDescription.BuildDict();

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
                    setBold: bold,
                    padding: new AnyUiThickness(2,0,0,0)),
                margin: new AnyUiThickness(0, 14, 0, 6));
        }

        protected void InnerDocAddText(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col, int keyCols,
            string[] texts,            
            int[] bold = null,
            int[] wrap = null)
        {
            // access and add row
            if (grid == null || texts == null)
                return;
            int row = InnerDocGetNewRow(grid);

            // key
            if (texts.Length > 0)
            {
                uitk.AddSmallBasicLabelTo(grid, row, col,
                    colSpan: _innerDocumentCols - col,
                    content: texts[0],
                    setBold: bold != null && bold.Contains(0),
                    textWrapping: (wrap != null && wrap.Contains(0)) 
                        ? AnyUiTextWrapping.Wrap : null);
            }

            // text(s)
            for (int i = 1; i < texts.Length; i++)
            {
                uitk.AddSmallBasicLabelTo(grid, row, col + keyCols + i - 1,
                    colSpan: (i != texts.Length - 1) 
                        ? 1 
                        : _innerDocumentCols - col - keyCols - (texts.Length - 2),
                    content: texts[i],
                    setBold: bold != null && bold.Contains(i),
                    textWrapping: (wrap != null && wrap.Contains(i))
                        ? AnyUiTextWrapping.Wrap : null);
            }
        }

        protected void InnerDocAddText(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col, int keyCols,
            string key,
            string text,
            AnyUiTextWrapping? wrapText = null)
        {
            InnerDocAddText(
                uitk, grid, col, keyCols,
                new[] { key, text },
                bold: new[] { 0 },
                wrap: (wrapText != AnyUiTextWrapping.Wrap) ? null : (new[] { 1 }));
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

        protected void InnerDocAddAffectedPartNumbers(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col,
            IList<string> partNumbers)
        {
            // access and add row
            if (grid == null || partNumbers == null || partNumbers.Count < 1)
                return;
            int row = InnerDocGetNewRow(grid);

            // set layout
            var numRows = 1 + partNumbers.Count / 3;

            // make inner grid
            var inner = uitk.Set(
                uitk.AddSmallGridTo(grid, row, col,
                    rows: numRows, cols: 3,
                    colWidths: "*".Times(3).ToArray()),
                colSpan: _innerDocumentCols - col);

            // single "boxes"
            for (int i = 0; i < partNumbers.Count; i++)
            {
                // row, col
                var ri = i % numRows;
                var ci = i / numRows;

                // make the border
                var brd = uitk.AddSmallBorderTo(inner, ri, ci,
                    margin: (ci == 0) ? new AnyUiThickness(0, -1, 0, 0)
                                        : new AnyUiThickness(-1, -1, 0, 0),
                    borderThickness: new AnyUiThickness(1.0),
                    borderBrush: AnyUiBrushes.Black);

                brd.Child = new AnyUiSelectableTextBlock()
                {
                    Text = "" + partNumbers[i],
                    Padding = new AnyUiThickness(1),
                    FontSize = 0.9f,
                    TextWrapping = AnyUiTextWrapping.Wrap,
                    HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                    HorizontalContentAlignment = AnyUiHorizontalAlignment.Center,
                    VerticalAlignment = AnyUiVerticalAlignment.Center,
                    VerticalContentAlignment = AnyUiVerticalAlignment.Center
                };
            }
        }

        protected void InnerDocDisplaySaveFile(
            string uri, string contentType, bool display, bool save)
        {
            // first check
            if (uri == null || uri.Trim().Length < 1
                || _eventStack == null)
                return;

            try
            {
                // temp input
                var inputFn = uri;
                try
                {
                    if (!inputFn.ToLower().Trim().StartsWith("http://")
                            && !inputFn.ToLower().Trim().StartsWith("https://"))
                        inputFn = _package?.MakePackageFileAvailableAsTempFile(inputFn);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "Making local file available");
                }

                // give over to event stack
                _eventStack?.PushEvent(new AasxPluginResultEventDisplayContentFile()
                {
                    SaveInsteadDisplay = save,
                    ProposeFn = System.IO.Path.GetFileName(uri),
                    Session = _session,
                    fn = inputFn,
                    mimeType = contentType
                });
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "when executing file action");
            }
        }

        protected void InnerDocAddAdditionalInfo(
            AnyUiSmallWidgetToolkit uitk,
            AnyUiGrid grid, int col,
            IList<AasClassMapperFile> addInfo)
        {
            // access and add row
            if (grid == null || addInfo == null || addInfo.Count < 1)
                return;
            int row = InnerDocGetNewRow(grid);

            // make inner grid
            var inner = uitk.Set(
                uitk.AddSmallGridTo(grid, row, col,
                    rows: addInfo.Count, cols: 3,
                    colWidths: new[] { "#", "*", "#" }),
                colSpan: _innerDocumentCols - col);

            // single "boxes"
            for (int i = 0; i < addInfo.Count; i++)
            {
                // handle good & bad path values as uri
                var uriStr = addInfo[i].Value;
                var contentType = addInfo[i].ContentType;
                var uriValid = AdminShellUtil.TryReFormatAsValidUri(ref uriStr);

                // border: symbol
                var brdSymbol = uitk.AddSmallBorderTo(inner, i, 0,
                    margin: (true) ? new AnyUiThickness(0, -1, 0, 0)
                                        : new AnyUiThickness(-1, -1, 0, 0),
                    borderThickness: new AnyUiThickness(1.0),
                    background: AnyUiBrushes.DarkGray,
                    borderBrush: AnyUiBrushes.DarkGray);

                // child: symbol
                brdSymbol.Child = new AnyUiSelectableTextBlock()
                {
                    Text = "\U0001F5CE",
                    Background = AnyUiBrushes.DarkGray,
                    Foreground = AnyUiBrushes.White,
                    Padding = new AnyUiThickness(4, 2, 4, 2),
                    FontSize = 1.0f,
                    FontWeight = AnyUiFontWeight.Bold,
                    HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                    HorizontalContentAlignment = AnyUiHorizontalAlignment.Center,
                    VerticalAlignment = AnyUiVerticalAlignment.Center,
                    VerticalContentAlignment = AnyUiVerticalAlignment.Center
                };

                // border: inner link
                var brdLink = uitk.AddSmallBorderTo(inner, i, 1,
                    margin: (false) ? new AnyUiThickness(0, -1, 0, 0)
                                        : new AnyUiThickness(-1, -1, 0, 0),
                    borderThickness: new AnyUiThickness(1.0),
                    borderBrush: AnyUiBrushes.DarkGray);

                // child: inner link
                brdLink.Child =
                    AnyUiUIElement.RegisterControl(
                        new AnyUiSelectableTextBlock()
                        {
                            Text = "" + uriStr,
                            Padding = new AnyUiThickness(4, 2, 4, 2),
                            FontSize = 1.0f,
                            TextWrapping = AnyUiTextWrapping.Wrap,
                            TextAsHyperlink = uriValid,
                            VerticalAlignment = AnyUiVerticalAlignment.Center,
                            VerticalContentAlignment = AnyUiVerticalAlignment.Center
                        },
                        setValue: (o) =>
                        {
                            InnerDocDisplaySaveFile(uriStr, contentType,
                                display: true, save: false);
                            return new AnyUiLambdaActionNone();
                        });

                // border: button
                var brdBtn = uitk.AddSmallBorderTo(inner, i, 2,
                    margin: (false) ? new AnyUiThickness(0, -1, 0, 0)
                                        : new AnyUiThickness(-1, -1, 0, 0),
                    borderThickness: new AnyUiThickness(1.0),
                    borderBrush: AnyUiBrushes.DarkGray);

                // child: button
                brdBtn.Child =
                    AnyUiUIElement.RegisterControl(
                        new AnyUiButton()
                        {
                            Content = "\U0001f80b",
                            Padding = new AnyUiThickness(2, -3, 2, -3),
                            Margin = new AnyUiThickness(2),
                            FontSize = 0.8f,
                            HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                            HorizontalContentAlignment = AnyUiHorizontalAlignment.Center
                        },
                        setValue: (o) =>
                        {
                            InnerDocDisplaySaveFile(uriStr, contentType,
                                display: false, save: true);
                            return new AnyUiLambdaActionNone();
                        });
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

            // Start
            InnerDocAddHeadline(uitk, grid, 0, "Record", 1);

            InnerDocAddText(uitk, grid, 0, 2, "DateOfRecord:",
                    "" + data.DateOfRecord);

            // Manufacturer
            if (data.Manufacturer != null)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Manufacturer", 2);
                
                InnerDocAddText(uitk, grid, 0, 2, "ManufacturerName:", 
                    data.Manufacturer.ManufacturerName?.GetDefaultString(_selectedLangStr));

                InnerDocAddText(uitk, grid, 0, 2, "AdressInformation:",
                    "<TBD>");

                InnerDocAddText(uitk, grid, 0, 2, "ManufacturerChangeID:",
                    "" + data.ManufacturerChangeID);
            }

            // Life cycle mile stones
            if (data.LifeCycleData?.Milestone != null && data.LifeCycleData.Milestone.Count > 0)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Life cycle mile stones changed by this notification", 2);

                InnerDocAddLifeCycleMilestones(uitk, grid, 0, data.LifeCycleData.Milestone);
            }

            // Reasons of change
            if (data.ReasonsOfChange?.ReasonOfChange != null && data.ReasonsOfChange.ReasonOfChange.Count > 0)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Reasons of change", 2);

                foreach (var roc in data.ReasonsOfChange.ReasonOfChange)
                {
                    var rid = ("" + roc.ReasonId).ToUpper().Trim();
                    if (_dictIdToReason.ContainsKey(rid))
                    {
                        var rd = _dictIdToReason[rid];
                        InnerDocAddText(uitk, grid, 0, 1, rd.Id,
                            rd.Description,
                            wrapText: AnyUiTextWrapping.Wrap);
                    }
                    else
                    {
                        InnerDocAddText(uitk, grid, 0, 1, "" + roc.ReasonId,
                            ("" + roc.ReasonClassificationSystem)
                            .AddWithDelimiter("" + roc.VersionOfClassificationSystem, delimter: ":"));
                    }
                }
            }

            // asset, partnumbers, items
            if (data.ItemOfChange != null)
            {
                // make a grid for two columns
                int row = InnerDocGetNewRow(grid);
                var twoColGrid =
                    uitk.Set(
                        uitk.AddSmallGridTo(grid, row, 0, rows: 1, cols: 2, new[] { "2*", "1*" }),
                        colSpan: _innerDocumentCols);

                // access asset information
                if (data.ItemOfChange.ManufacturerAssetID?.ValueHint is Aas.IAssetAdministrationShell aas
                    && aas.AssetInformation is Aas.IAssetInformation ai
                    && ai.DefaultThumbnail?.Path != null)
                {
                    var img = AnyUiGdiHelper.LoadBitmapInfoFromPackage(package, ai.DefaultThumbnail.Path);

                    uitk.Set(
                        uitk.AddSmallImageTo(twoColGrid, 0, 1,
                            margin: new AnyUiThickness(2, 8, 2, 2),
                            stretch: AnyUiStretch.Uniform,
                            bitmap: img),
                    maxHeight: 300, maxWidth: 300,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);
                }

                // identification info
                if (true)
                {
                    var colTwoGrid =
                    uitk.Set(
                        uitk.AddSmallGridTo(twoColGrid, 0, 0,
                            rows: 1, cols: 3, new[] { "70:", "70:", "*" }));

                    InnerDocAddHeadline(uitk, colTwoGrid, 0, "ItemOfChange (1)", 1,
                        assignRow: 0);

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "ManufacturerAssetID:",
                        "" + data.ItemOfChange.ManufacturerAssetID?.Value?.ToStringExtended(1),
                        wrapText: AnyUiTextWrapping.Wrap);

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "Mfg.Prod.Family:",
                        data.ItemOfChange.ManufacturerProductFamily?.GetDefaultString(_selectedLangStr));

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "Mfg.Prod.Deign.:",
                        data.ItemOfChange.ManufacturerProductDesignation?.GetDefaultString(_selectedLangStr));

                    InnerDocAddText(uitk, colTwoGrid, 0, 2, "Order Code Mfg.:",
                        data.ItemOfChange.OrderCodeOfManufacturer?.GetDefaultString(_selectedLangStr));

                    InnerDocAddText(uitk, grid, 0, 2, "HardwareVersion:",
                        "" + data.ItemOfChange.HardwareVersion);
                }
            }

            // part numbers
            if (data.AffectedPartNumbers?.AffectedPartNumber != null 
                && data.AffectedPartNumbers.AffectedPartNumber.Count >= 0)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Affected part numbers", 2);

                InnerDocAddAffectedPartNumbers(uitk, grid, 0,
                    data.AffectedPartNumbers.AffectedPartNumber);
            }

            // Item categories
            if (data.ItemCategories?.ItemCategory != null && data.ItemCategories.ItemCategory.Count > 0)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Item categories", 2);

                foreach (var ic in data.ItemCategories.ItemCategory)
                {
                    var cid = ("" + ic.ItemCategory).ToUpper().Trim();
                    if (_dictIdToItem.ContainsKey(cid))
                    {
                        var cd = _dictIdToItem[cid];
                        InnerDocAddText(uitk, grid, 0, 1, cd.Id,
                            cd.Description,
                            wrapText: AnyUiTextWrapping.Wrap);
                    }
                    else
                    {
                        InnerDocAddText(uitk, grid, 0, 1, "" + ic.ItemCategory,
                            ("" + ic.ItemClassificationSystem)
                            .AddWithDelimiter("" + ic.VersionOfClassificationSystem, delimter: ":"));
                    }
                }
            }

            // human readable infos
            if (true)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Human readable change information", 2);

                InnerDocAddText(uitk, grid, 0, 2, "PcnChg.Info/Title:",
                    "" + data.PcnChangeInformation?.ChangeTitle?.GetDefaultString(_selectedLangStr));

                InnerDocAddText(uitk, grid, 0, 2, "PcnChg.Info/Details:",
                    "" + data.PcnChangeInformation?.ChangeDetail?.GetDefaultString(_selectedLangStr),
                    wrapText: AnyUiTextWrapping.Wrap);

                InnerDocAddText(uitk, grid, 0, 2, "PcnReasonComment:",
                    "" + data.PcnReasonComment?.GetDefaultString(_selectedLangStr),
                    wrapText: AnyUiTextWrapping.Wrap);

            }

            // additional infos
            if (data.AdditionalInformations?.AdditionalInformation != null
                && data.AdditionalInformations.AdditionalInformation.Count >= 0)
            {
                InnerDocAddHeadline(uitk, grid, 0, "Additional information provided by documents", 2);

                InnerDocAddAdditionalInfo(uitk, grid, 0,
                    data.AdditionalInformations.AdditionalInformation);
            }

            // further of item of change
            if (data.ItemOfChange != null)
            {
                InnerDocAddHeadline(uitk, grid, 0, "ItemOfChange (2)", 1);

                InnerDocAddHeadline(uitk, grid, 0, "Remaining stock information", 2);

                InnerDocAddText(uitk, grid, 0, 2, "Rem.AmountAvail.:",
                    "" + data.ItemOfChange.RemainingAmountAvailable);
                
                // Reasons of change
                if (data.ItemOfChange.ProductClassifications?.ProductClassification != null 
                    && data.ItemOfChange.ProductClassifications.ProductClassification.Count > 0)
                {
                    InnerDocAddHeadline(uitk, grid, 0, "Product classification(s)", 2);

                    foreach (var pc in data.ItemOfChange.ProductClassifications.ProductClassification)
                    {
                        InnerDocAddText(uitk, grid, 0, 1, "" + pc.ProductClassId,
                            ("" + pc.ClassificationSystem)
                            .AddWithDelimiter("" + pc.VersionOfClassificationSystem, delimter: ":"));
                    }
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
