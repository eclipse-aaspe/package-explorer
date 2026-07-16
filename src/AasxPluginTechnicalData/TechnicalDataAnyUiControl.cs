/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable NegativeEqualityExpression

namespace AasxPluginTechnicalData
{
    public class TechnicalDataAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnvBase _package = null;
        private Aas.IAssetAdministrationShell _aas = null;
        private Aas.ISubmodel _submodel = null;
        private TechnicalDataOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;
        private AasxPluginBase _plugin = null;
        private ISecurityAccessHandler _secureAccess = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        protected AnyUiGdiHelper.DelayedFileContentLoader _bitmapLoader = null;

        #endregion

        #region Members to be kept for state/ update
        //=============

        protected double _lastScrollPosition = 0.0;

        protected int _selectedLangIndex = 0;
        protected string _selectedLangStr = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        // ReSharper disable EmptyConstructor
        public TechnicalDataAnyUiControl()
        {
            // the loader
            if (OperatingSystem.IsWindows())
            {
                _bitmapLoader =
                        new AnyUiGdiHelper.DelayedFileContentLoader();
            }

            // start a timer
            if (OperatingSystem.IsWindowsVersionAtLeast(7, 0, 0))
            {
                AnyUiTimerHelper.CreatePluginTimerAsync(300, lambdaTick: async () => await DispatcherTimer_Tick(null, null));
            }
        }
        // ReSharper enable EmptyConstructor

        public void Start(
            LogInstance log,
            AdminShellPackageEnvBase thePackage,
            Aas.ISubmodel theSubmodel,
            TechnicalDataOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel,
            AasxPluginBase plugin,
            ISecurityAccessHandler secureAccess)
        {
            // typecast
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _aas = _package?.AasEnv?.FindAasWithSubmodelId(_submodel?.Id);
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;
            _plugin = plugin;
            _secureAccess = secureAccess;

            // fill given panel
            RenderFullView(_panel, _uitk, _package, _submodel, defaultLang: null);
        }

        public static TechnicalDataAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            TechnicalDataOptions options,
            PluginEventStack eventStack,
            object opanel,
            AasxPluginBase plugin,
            ISecurityAccessHandler secureAccess)
        {
            // access
            var package = opackage as AdminShellPackageEnvBase;
            var sm = osm as Aas.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // factory this object
            var techCntl = new TechnicalDataAnyUiControl();
            techCntl.Start(log, package, sm, options, eventStack, panel, plugin, secureAccess);

            // return shelf
            return techCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        public static bool CheckSuppleSemId (Aas.ISubmodel sm, TechnicalDataOptions options)
        {
            if (sm.SupplementalSemanticIds != null
                    && options.AllowSupplementalSemanticId != null)
                foreach (var k in options.AllowSupplementalSemanticId)
                    foreach (var ssid in sm.SupplementalSemanticIds)
                        if (ssid?.Matches(k.Value) == true)
                            return true;
            return false;
        }

        private void RenderFullView(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            AdminShellPackageEnvBase package,
            Aas.ISubmodel sm, string defaultLang = null)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel
            TechnicalDataOptionsRecord foundRec = null;
            foreach (var rec in _options.LookupAllIndexKey<TechnicalDataOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRec = rec;

            // extra rule
            if (CheckSuppleSemId(sm, _options))
                foundRec = new TechnicalDataOptionsRecord();

            if (foundRec == null)
                return;

            // retrieve the Definitions
            var theDefs = new ConceptModelZveiTechnicalData(sm);

            // render
            RenderPanelOutside(view, uitk, theDefs, package, sm, defaultLang);
        }

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnvBase package,
            Aas.ISubmodel sm, string defaultLang = null)
        {
            // make an outer grid, very simple grid of two rows: header & body
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
                content: $"Technical Data");

            // languages
            var langs = new List<string>();
            foreach (var dc in AasxLanguageHelper.Languages.GetAllLanguages())
                langs.Add("Lang - " + dc);

            if (_selectedLangStr == null)
                _selectedLangStr = defaultLang;

            AnyUiComboBox cbLang = null;
            cbLang = AnyUiUIElement.RegisterControl(
                uitk.Set(
                    uitk.AddSmallComboBoxTo(bluebar, 0, 1,
                        margin: new AnyUiThickness(2), minWidth: 120,
                        padding: new AnyUiThickness(2, 0, 2, 0),
                        items: langs.ToArray(),
                        selectedIndex: _selectedLangIndex),
                    maxHeight: 21),
                (o) =>
                {
                    if (!(cbLang?.SelectedIndex.HasValue == true))
                        return new AnyUiLambdaActionNone();
                    _selectedLangIndex = cbLang.SelectedIndex.Value;
                    _selectedLangStr = AasxLanguageHelper.Languages.GetAllLanguages(nullForAny: true)
                        .ElementAtOrDefault(_selectedLangIndex);
                    return new AnyUiLambdaActionPluginUpdateAnyUi()
                    {
                        PluginName = _plugin?.GetPluginName(),
                        UseInnerGrid = true
                    };
                });

            //
            // Header area
            //

            // small spacer
            outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // header
            var header = uitk.AddSmallStackPanelTo(outer, 2, 0, setVertical: true);
            RenderPanelHeader(header, uitk, theDefs, package, sm, _selectedLangStr);

            //
            // Scroll area
            //

            // small spacer
            outer.RowDefinitions[3] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 3, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[4] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 4, 0,
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
                Margin = new AnyUiThickness(2)
            };
            scroll.Content = inner;
            RenderPanelInner(inner, uitk, theDefs, package, sm, _selectedLangStr);

            //
            // Footer area
            //

            // small spacer
            outer.RowDefinitions[5] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 5, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // header
            var footer = uitk.AddSmallStackPanelTo(outer, 6, 0, setVertical: true);
            RenderPanelFooter(footer, uitk, theDefs, package, sm, _selectedLangStr);
        }

        #endregion

        #region Header
        //=============

        protected class ClassificationRecord
        {
            public string System { get; set; }
            public string Version { get; set; }
            public string ClassTxt { get; set; }
            public string ClassName { get; set; }
        }

        protected void RenderPanelHeader(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnvBase package,
            Aas.ISubmodel sm, string defaultLang = null)
        {
            // access
            if (view == null || uitk == null || sm == null)
                return;

            // make an grid with two columns, first a bit wider 
            var outer = view.Add(uitk.AddSmallGrid(rows: 4, cols: 2, colWidths: new[] { "7*", "3*" }));
            outer.ColumnDefinitions[1].MaxWidth = 200;

            //
            // General header
            //           

            // section General
            var smcGeneral = sm.SubmodelElements.FindFirstSemanticIdAs<Aas.SubmodelElementCollection>(
                theDefs.CD_GeneralInformation?.GetSingleKey(), MatchMode.Relaxed);
            if (smcGeneral != null)
            {
                // gather information

                var prodDesig = "" +
                    smcGeneral.Value.FindFirstSemanticId(
                        theDefs.CD_ManufacturerProductDesignation?.GetSingleKey(),
                        allowedTypes: ExtendISubmodelElement.PROP_MLP, MatchMode.Relaxed)?
                            .ValueAsText(defaultLang);

                var prodCode = "" +
                    smcGeneral.Value.FindFirstSemanticIdAs<Aas.Property>(
                        theDefs.CD_ManufacturerOrderCode?.GetSingleKey(), MatchMode.Relaxed)?.Value;

                var partNumber = "" +
                    smcGeneral.Value.FindFirstSemanticIdAs<Aas.Property>(
                        theDefs.CD_ManufacturerPartNumber?.GetSingleKey(), MatchMode.Relaxed)?.Value;

                var artNumber = "" +
                    smcGeneral.Value.FindFirstSemanticIdAs<Aas.Property>(
                        theDefs.CD_ManufacturerArticleNumber?.GetSingleKey(), MatchMode.Relaxed)?.Value;
                if (!partNumber.HasContent())
                    partNumber = artNumber;

                var manuName = "" +
                    smcGeneral.Value.FindFirstSemanticIdAs<Aas.Property>(
                        theDefs.CD_ManufacturerName?.GetSingleKey(), MatchMode.Relaxed)?.Value;

                // render information

                uitk.AddSmallBasicLabelTo(outer, 0, 0, margin: new AnyUiThickness(1),
                    fontSize: 1.2f,
                    textWrapping: AnyUiTextWrapping.Wrap,
                    setBold: true,
                    content: prodDesig);

                uitk.AddSmallBasicLabelTo(outer, 1, 0, margin: new AnyUiThickness(1),
                    fontSize: 1.6f,
                    setBold: true,
                    content: prodCode);

                uitk.AddSmallBasicLabelTo(outer, 2, 0, margin: new AnyUiThickness(1),
                    fontSize: 1.0f,
                    setBold: false,
                    content: partNumber);

                uitk.AddSmallBasicLabelTo(outer, 0, 1, margin: new AnyUiThickness(1),
                    horizontalAlignment: AnyUiHorizontalAlignment.Right,
                    horizontalContentAlignment: AnyUiHorizontalAlignment.Right,
                    fontSize: 1.0f,
                    setBold: false,
                    content: manuName);

                AnyUiBitmapInfo biManuLogo = null;
                AnyUiImage imageManuLogo = uitk.Set(
                    uitk.AddSmallImageTo(outer, 1, 1,
                        margin: new AnyUiThickness(2),
                        stretch: AnyUiStretch.Uniform,
                        bitmap: biManuLogo),
                    rowSpan: 2,
                    maxHeight: 100, maxWidth: 200,
                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                    verticalAlignment: AnyUiVerticalAlignment.Stretch);

                // subscribe to file information
                if (package != null)
                {
#if USE_WPF
                    var bi = AasxWpfBaseUtils.LoadBitmapImageFromPackage(
                        package,
                        smcGeneral.value.FindFirstSemanticIdAs<AdminShell.File>(
                            theDefs.CD_ManufacturerLogo.GetSingleKey())?.value
                        );
                    imageManuLogo = AnyUiHelper.CreateAnyUiBitmapInfo(bi);
#else
                    {
                        // File
                        var fe = smcGeneral.Value.FindFirstSemanticIdAs<Aas.File>(
                            theDefs.CD_ManufacturerLogo?.GetSingleKey(), MatchMode.Relaxed);

                        if (OperatingSystem.IsWindowsVersionAtLeast(7, 0, 0))
                        {
                            // for now
                            biManuLogo = AnyUiGdiHelper.LoadBitmapInfoFromPackage(
                                package, fe?.Value,
                                secureAccess: _secureAccess);

                            // for later
                            _bitmapLoader.Add(package, _aas, _submodel, fe, (fcl, bi) =>
                            {
                                imageManuLogo.BitmapInfo = bi;
                            });
                        }
                    }
#endif
                }

                //
                // Product Images
                //

                // list of file elements
                var fePIs = new List<Aas.IFile>();

                // old style
                fePIs.AddRange(
                    smcGeneral.Value.FindAllSemanticIdAs<Aas.File>(
                        theDefs.CD_ProductImage?.GetSingleKey(), MatchMode.Relaxed).ToList());

                // new style
                if (theDefs.ActiveVersion >= ConceptModelZveiTechnicalData.Version.V2_0)
                {
                    var smlPI = smcGeneral.Value.FindFirstSemanticIdAs<Aas.ISubmodelElementList>(
                        theDefs.CD_ProductImages?.GetSingleKey(), MatchMode.Relaxed);
                    foreach (var smcPI in smlPI?.Value?.FindAllSemanticIdAs<Aas.ISubmodelElementCollection>(
                        theDefs.CD_ProductImage?.GetSingleKey(), MatchMode.Relaxed))
                    {
                        var fPI = smcPI?.Value?.FindFirstSemanticIdAs<Aas.IFile>(
                            theDefs.CD_ProductImageFile?.GetSingleKey(), MatchMode.Relaxed);
                        if (fPI != null)
                            fePIs.Add(fPI);
                    }
                }

                // make an outer grid, very simple grid of two rows: header & body
                var pilGrid = uitk.AddSmallGridTo(outer, 3, 0, rows: 1, cols: fePIs.Count);

                int col = 0;
                foreach (var fePI in fePIs)
                {
                    var imageElem = uitk.Set(
                        uitk.AddSmallImageTo(pilGrid, 0, col++,
                            margin: new AnyUiThickness(2),
                            stretch: AnyUiStretch.Uniform),
                        maxHeight: 100, maxWidth: 200,
                        horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                        verticalAlignment: AnyUiVerticalAlignment.Stretch);

#if USE_WPF
                    var bi = AasxWpfBaseUtils.LoadBitmapImageFromPackage(package, pi.value);
                    var imgInfo = AnyUiHelper.CreateAnyUiBitmapInfo(bi);
                    if (imgInfo != null)
                        thisImage.BitmapInfo = bi;
#else
                    // for now
                    if (OperatingSystem.IsWindowsVersionAtLeast(7, 0, 0))
                    {
                        imageElem.BitmapInfo = AnyUiGdiHelper.LoadBitmapInfoFromPackage(package, fePI.Value);

                        // for later
                        _bitmapLoader.Add(package, _aas, _submodel, fePI, (fcl, bi) =>
                        {
                            imageElem.BitmapInfo = bi;
                        });
                    }
#endif
                }
                
            }

            //
            // also Section: Product Classifications
            //

            foreach (var childProdClass in sm.SubmodelElements.GetChildListsFromAllSemanticId(
                theDefs.CD_ProductClassifications?.GetSingleKey(), MatchMode.Relaxed))
            {
                // gather

                var clr = new List<ClassificationRecord>();
                foreach (var smc in
                    childProdClass.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        theDefs.CD_ProductClassificationItem?.GetSingleKey(),
                        MatchMode.Relaxed))
                {
                    var sys = (
                        "" +
                        smc.Value.FindFirstSemanticIdAs<Aas.Property>(
                            theDefs.CD_ProductClassificationSystem?.GetSingleKey(), MatchMode.Relaxed)?.Value)?.Trim();
                    var ver = (
                        "" +
                        smc.Value.FindFirstSemanticIdAs<Aas.Property>(
                            theDefs.CD_ClassificationSystemVersion?.GetSingleKey(), MatchMode.Relaxed)?.Value)?.Trim();
                    var cls = (
                        "" +
                        smc.Value.FindFirstSemanticIdAs<Aas.Property>(
                            (theDefs.ActiveVersion >= ConceptModelZveiTechnicalData.Version.V2_0
                             ? theDefs.CD_ProductClassCodedName?.GetSingleKey()
                             : theDefs.CD_ProductClassId?.GetSingleKey()
                            ), MatchMode.Relaxed)?.Value)?.Trim();

                    var name = "";
                    if (theDefs.ActiveVersion >= ConceptModelZveiTechnicalData.Version.V2_0)
                        name = "" + smc.Value.FindFirstSemanticIdAs<Aas.MultiLanguageProperty>(
                            theDefs.CD_ProductClassName?.GetSingleKey(), MatchMode.Relaxed)?.ValueAsText(defaultLang);

                    if (sys != "" && cls != "")
                        clr.Add(new ClassificationRecord() { System = sys, Version = ver, ClassTxt = cls, ClassName = name });
                }

                // render
                if (clr.Count > 0)
                {
                    // make an outer grid, very simple grid of two rows: header & body
                    var clrGrid = uitk.AddSmallGridTo(outer, 3, 1, rows: clr.Count, cols: 1, colWidths: new[] { "*" });

                    for (int i = 0; i < clr.Count; i++)
                    {
                        // instances are again a grid inside a border
                        var clrbrd = uitk.AddSmallBorderTo(clrGrid, 0 + i, 0,
                                        borderThickness: new AnyUiThickness(1.5), borderBrush: AnyUiBrushes.DarkBlue,
                                        margin: new AnyUiThickness(2),
                                        cornerRadius: 2.0);
                        var clrgi = uitk.AddSmallGrid(rows: 3, cols: 2, colWidths: new[] { "*", "#" });
                        clrbrd.Child = clrgi;

                        // labels
                        uitk.AddSmallBasicLabelTo(clrgi, 0, 0, margin: new AnyUiThickness(1),
                            fontSize: 1.1f,
                            setBold: false,
                            content: clr[i].System);

                        uitk.AddSmallBasicLabelTo(clrgi, 0, 1, margin: new AnyUiThickness(1),
                            horizontalAlignment: AnyUiHorizontalAlignment.Right,
                            horizontalContentAlignment: AnyUiHorizontalAlignment.Right,
                            textWrapping: AnyUiTextWrapping.NoWrap,
                            fontSize: 1.1f,
                            setBold: false,
                            content: clr[i].Version);

                        // note: adopt to the length of class id
                        uitk.AddSmallBasicLabelTo(clrgi, 1, 0, margin: new AnyUiThickness(1),
                            horizontalAlignment: AnyUiHorizontalAlignment.Center,
                            horizontalContentAlignment: AnyUiHorizontalAlignment.Center,
                            fontSize: (clr[i].ClassTxt.Length <= 12) ? 1.4f : 1.0f,
                            setBold: true,
                            colSpan: 2,
                            content: clr[i].ClassTxt);

                        // note: adopt to the length of class name
                        uitk.AddSmallBasicLabelTo(clrgi, 2, 0, margin: new AnyUiThickness(1),
                            horizontalAlignment: AnyUiHorizontalAlignment.Center,
                            horizontalContentAlignment: AnyUiHorizontalAlignment.Center,
                            fontSize: (clr[i].ClassName.Length <= 24) ? 0.8f : 0.6f,
                            setBold: true,
                            colSpan: 2,
                            content: clr[i].ClassName);
                    }
                }

            }
        }

        #endregion

        #region Inner
        //=============

        protected class TripleRowData
        {
            public string Heading = null;
            public AnyUiThickness HeadMargin = null;

            public bool Footer = false;
            public double FooterHeight = 5;

            public double? FontSize;
            public AnyUiFontWeight FontWeight;
            public string Name = "", Semantics = "", Value = "";
        }

        protected void RenderTripleRowData(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            TripleRowData[] rows)
        {
            // access
            if (rows == null)
                return;

            // ok
            var grid = view.Add(uitk.AddSmallGrid(rows: rows.Length, cols: 3, colWidths: new[] { "*", "*", "*" }));
            for (int ri = 0; ri < rows.Length; ri++)
            {
                // access
                var row = rows[ri];
                if (row == null)
                    continue;

                if (row.Heading.HasContent())
                {
                    // heading
                    var hlb = uitk.AddSmallBasicLabelTo(grid, ri, 0, margin: row.HeadMargin,
                        colSpan: 3,
                        content: row.Heading);
                    hlb.FontSize = row.FontSize;
                    hlb.FontWeight = row.FontWeight;
                }
                else if (row.Footer)
                {
                    uitk.AddVerticalSpaceTo(grid, ri, row.FooterHeight);
                }
                else
                {
                    // normal row, 3 bordered cells
                    var cols = new[] { row.Name, row.Semantics, row.Value };
                    for (int ci = 0; ci < 3; ci++)
                    {
                        var brd = uitk.AddSmallBorderTo(grid, ri, ci,
                            margin: (ci == 0) ? new AnyUiThickness(0, -1, 0, 0) : new AnyUiThickness(-1, -1, 0, 0),
                            borderThickness: new AnyUiThickness(1.0), borderBrush: AnyUiBrushes.DarkGray);
                        brd.Child = new AnyUiSelectableTextBlock()
                        {
                            Text = cols[ci],
                            Padding = new AnyUiThickness(1),
                            FontSize = row.FontSize,
                            FontWeight = row.FontWeight,
                        };
                    }
                }
            }
        }

        protected void TableAddPropertyRows_Recurse(
            ConceptModelZveiTechnicalData theDefs, string defaultLang, AdminShellPackageEnvBase package,
            List<TripleRowData> rows, List<Aas.ISubmodelElement> smec, int depth = 0)
        {
            // access
            if (rows == null || smec == null)
                return;

            // go element by element
            foreach (var sme in smec)
            {
                // access
                if (sme == null)
                    continue;

                // prepare information about displayName, semantics unit
                var semantics = "-";
                var unit = "";
                // make up property name (prio 4)
                var dispName = "" + sme.IdShort;
                var dispNameWithCD = dispName;

                // make up semantics
                if (sme.SemanticId != null)
                {
                    if (sme.SemanticId.MatchesExactlyOneKey(
                        theDefs.CD_SemanticIdNotAvailable?.GetSingleKey(), MatchMode.Relaxed))
                        semantics = "(not available)";
                    else
                    {
                        // the semantics display
                        semantics = "" + sme.SemanticId?.ToStringExtended(2);

                        // find better property name (prio 2 and prio 3)
                        var cd = package?.AasEnv?.FindConceptDescriptionByReference(sme.SemanticId,
                                    _options.GetCdMatchMode());
                        if (cd != null)
                        {
                            // unit?
                            unit = "" + cd.GetIEC61360()?.Unit;

                            // names
                            var dpn = cd.GetDefaultPreferredName(defaultLang);
                            if (dpn != "")
                                dispNameWithCD = dpn;

                            var dsn = cd.GetDefaultShortName(defaultLang);
                            if (dsn != "")
                                dispNameWithCD = dsn;
                        }
                    }
                }

                // may be add unit ad-hoc
                if (!unit.HasContent())
                {
                    var q = sme.Qualifiers?.FindQualifierOfType("SME/UnitOfMeasure");
                    if (q?.Value?.HasContent() == true)
                        unit = q.Value;
                }

                // make up even better better property name (prio 1b)
                var descDef = "" + sme.Description?.GetDefaultString(defaultLang);
                if (descDef.HasContent())
                {
                    dispName = descDef;
                    dispNameWithCD = dispName;
                }

                // make up even better better display name (prio 1a), this is the 
                // highest priority
                var dNameSme = "" + sme.DisplayName?.GetDefaultString(defaultLang);
                if (dNameSme.HasContent())
                {
                    dispName = dNameSme;
                    dispNameWithCD = dNameSme;
                }

                // special function?
                if (sme is Aas.SubmodelElementCollection &&
                        true == sme.SemanticId?.MatchesExactlyOneKey(
                            theDefs.CD_MainSection?.GetSingleKey(), MatchMode.Relaxed))
                {
                    // Main Section
                    rows.Add(new TripleRowData()
                    {
                        Heading = "" + dispName,
                        HeadMargin = new AnyUiThickness(-2 + 4 * depth, 6, 0, 4),
                        FontSize = 1.4f,
                        FontWeight = AnyUiFontWeight.Bold
                    });

                    // recurse into that (again, new group)
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, rows,
                        (sme as Aas.SubmodelElementCollection).Value, depth + 1);
                }
                else
                if (sme is Aas.SubmodelElementCollection &&
                    true == sme.SemanticId?.MatchesExactlyOneKey(
                        theDefs.CD_SubSection?.GetSingleKey(), MatchMode.Relaxed))
                {
                    // Sub Section
                    rows.Add(new TripleRowData()
                    {
                        Heading = "" + dispName,
                        HeadMargin = new AnyUiThickness(-2 + 4 * depth, 4, 0, 2),
                        FontSize = 1.2f,
                        FontWeight = AnyUiFontWeight.Bold
                    });

                    // recurse into that
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, rows,
                        (sme as Aas.SubmodelElementCollection).Value, depth + 1);
                }
                else
                if (sme is Aas.Property || sme is Aas.MultiLanguageProperty || sme is Aas.Range)
                {
                    rows.Add(new TripleRowData()
                    {
                        Name = dispNameWithCD,
                        Semantics = semantics,
                        Value = "" + sme.ValueAsText(defaultLang) + " " + unit
                    });
                }
                else
                if (sme is Aas.SubmodelElementCollection smc)
                {
                    // SMC which is not dedicated main/ sub-section
                    // decide automatically about main/ sub section

                    if (depth == 0)
                        // Main Section
                        rows.Add(new TripleRowData()
                        {
                            Heading = "" + dispName,
                            HeadMargin = new AnyUiThickness(-2 + 4 * depth, 6, 0, 4),
                            FontSize = 1.4f,
                            FontWeight = AnyUiFontWeight.Bold
                        });
                    else
                        rows.Add(new TripleRowData()
                        {
                            Heading = "" + dispName,
                            HeadMargin = new AnyUiThickness(-2 + 4 * depth, 4, 0, 2),
                            FontSize = 1.2f,
                            FontWeight = AnyUiFontWeight.Bold
                        });

                    // recurse into that
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, rows, smc.Value, depth + 1);

                    // small footer
                    rows.Add(new TripleRowData() { Footer = true, FooterHeight = (depth == 0) ? 5 : 3 });
                }
                else
                if (sme is Aas.SubmodelElementList sml)
                {
                    // SML which is not dedicated main/ sub-section
                    // decide automatically about main/ sub section

                    if (depth == 0)
                        // Main Section
                        rows.Add(new TripleRowData()
                        {
                            Heading = "" + dispName,
                            HeadMargin = new AnyUiThickness(-2 + 4 * depth, 6, 0, 4),
                            FontSize = 1.4f,
                            FontWeight = AnyUiFontWeight.Bold
                        });
                    else
                        rows.Add(new TripleRowData()
                        {
                            Heading = "" + dispName,
                            HeadMargin = new AnyUiThickness(-2 + 4 * depth, 4, 0, 2),
                            FontSize = 1.2f,
                            FontWeight = AnyUiFontWeight.Bold
                        });

                    // recurse into that
                    TableAddPropertyRows_Recurse(
                        theDefs, defaultLang, package, rows, sml.Value, depth + 1);

                    // small footer
                    rows.Add(new TripleRowData() { Footer = true, FooterHeight = (depth == 0) ? 5 : 3 });
                }
            }
        }

        protected void RenderPanelInner(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnvBase package,
            Aas.ISubmodel sm, string defaultLang = null)
        {
            // access
            if (view == null || uitk == null || sm == null)
                return;

            // rows and header
            var rows = new List<TripleRowData>();
            rows.Add(new TripleRowData()
            {
                Name = "Property",
                Semantics = "Semantics",
                Value = "Value",
                FontWeight = AnyUiFontWeight.Bold
            });

            // collect starting points
            var startPoints = new List<Aas.ISubmodelElementCollection>();

            // section Properties
            var smcProps =
                sm.SubmodelElements.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                    theDefs.CD_TechnicalProperties?.GetSingleKey(), MatchMode.Relaxed);
            if (smcProps != null)
                startPoints.Add(smcProps);

            if (theDefs.ActiveVersion >= ConceptModelZveiTechnicalData.Version.V2_0)
            {
                // technical property areas
                var smlTPA = sm.SubmodelElements.FindFirstSemanticIdAs<Aas.ISubmodelElementList>(
                        theDefs.CD_TechnicalPropertyAreas?.GetSingleKey(), MatchMode.Relaxed);
                if (smlTPA?.Value != null)
                    foreach (var smc in smlTPA.Value.FindAllSemanticIdAs<Aas.ISubmodelElementCollection>(
                            theDefs.CD_TechnicalPropertyArea?.GetSingleKey(), MatchMode.Relaxed).ForEachSafe())
                        startPoints.Add(smc);

                // specific descriptions
                var smlSPD = sm.SubmodelElements.FindFirstSemanticIdAs<Aas.ISubmodelElementList>(
                        theDefs.CD_SpecificDescriptions?.GetSingleKey(), MatchMode.Relaxed);
                if (smlSPD?.Value != null)
                    foreach (var smc in (smlSPD.Value.FindAllSemanticIdAs<Aas.ISubmodelElementCollection>(
                            theDefs.CD_SpecificDescription?.GetSingleKey(), MatchMode.Relaxed))?.ForEachSafe())
                        startPoints.Add(smc);
            }

            // something
            if (startPoints.Count < 1)
                return;

            // recurse
            foreach (var smc in startPoints)
                TableAddPropertyRows_Recurse(theDefs, defaultLang, package, rows, smc?.Value);

            // render
            RenderTripleRowData(view, uitk, rows.ToArray());
        }

        #endregion

        #region Footer
        //=============

        protected void RenderPanelFooter(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            ConceptModelZveiTechnicalData theDefs,
            AdminShellPackageEnvBase package,
            Aas.ISubmodel sm, string defaultLang = null)
        {
            // access
            if (view == null || uitk == null || sm == null)
                return;

            // gather data for footer
            var validDate = "";
            var tsl = new List<string>();

            var smcFurther = sm.SubmodelElements.FindFirstSemanticIdAs<Aas.SubmodelElementCollection>(
                theDefs.CD_FurtherInformation.GetSingleKey(), MatchMode.Relaxed);
            if (smcFurther != null)
            {
                // single items
                validDate = "" + smcFurther.Value.FindFirstSemanticIdAs<Aas.Property>(
                    theDefs.CD_ValidDate.GetSingleKey(), MatchMode.Relaxed)?.Value;

                // Lines
                foreach (var sme in
                    smcFurther.Value.FindAllSemanticId(
                        theDefs.CD_TextStatement.GetSingleKey(),
                        allowedTypes: ExtendISubmodelElement.PROP_MLP, MatchMode.Relaxed))
                    tsl.Add("" + sme?.ValueAsText(defaultLang));
            }

            // make an grid with two columns, first a bit wider 
            var outer = view.Add(uitk.AddSmallGrid(rows: 4, cols: 2,
                            colWidths: new[] { "*", "#" }, background: AnyUiBrushes.White));
            outer.ColumnDefinitions[1].MaxWidth = 200;

            // fill
            uitk.AddSmallBasicLabelTo(outer, 0, 1,
                textWrapping: AnyUiTextWrapping.NoWrap,
                fontSize: 1.0f,
                content: validDate);

            for (int i = 0; i < tsl.Count; i++)
                uitk.AddSmallBasicLabelTo(outer, 0 + i, 0,
                fontSize: 1.0f,
                content: tsl[i]);
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
            RenderFullView(_panel, _uitk, _package, _submodel, defaultLang: null);
        }

        #endregion

        #region Callbacks
        //===============

        protected int _dispatcherTimerNumberOfExceptions = 0;

        private async Task DispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                await Task.Yield();
                if (OperatingSystem.IsWindowsVersionAtLeast(7, 0, 0))
                {
                    if ((await _bitmapLoader?.TickToLoad(_secureAccess)) == true)
                    {
                        _eventStack?.PushEvent(new AasxPluginEventReturnUpdateAnyUi()
                        {
                            PluginName = null,
                            Mode = AnyUiRenderMode.StatusToUi,
                            UseInnerGrid = true
                        });
                    }
                }
            } catch (Exception ex)
            {
                if (_dispatcherTimerNumberOfExceptions++ < 3)
                    _log?.Error(ex, "cyclic display of content of plugin for technical data");
            }
        }


        #endregion

        #region Utilities
        //===============


        #endregion
    }
}
