/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using AasxIntegrationBase;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;
using WpfMtpControl;
using WpfMtpControl.DataSources;
using System.IO.Packaging;

namespace AasxPluginMtpViewer
{
    public partial class WpfMtpControlWrapper : UserControl
    {
        // internal members

        private LogInstance _log = null;
        private AdminShellPackageEnvBase _package = null;
        private Aas.IAssetAdministrationShell _aas = null;
        private Aas.ISubmodel _submodel = null;
        private AasxPluginMtpViewer.MtpViewerOptions _options = null;
        private PluginEventStack _eventStack = null;

        private AasxPredefinedConcepts.DefinitionsExperimental.InteropRelations _defsInterop = null;
        private DefinitionsMTP.ModuleTypePackage _defsMtp = null;

        private MtpDataSourceOpcUaPreLoadInfo _preLoadInfo = new MtpDataSourceOpcUaPreLoadInfo();

        private WpfMtpControl.MtpSymbolLib _theSymbolLib = null;
        private WpfMtpControl.MtpVisualObjectLib _activeVisualObjectLib = null;
        private WpfMtpControl.MtpData _activeMtpData = null;

        private Aas.ISubmodel _mtpTypeSm = null;
        private Aas.File _activeMtpFileElem = null;
        private string _activeMtpFileFn = null;

        public WpfMtpControl.MtpVisuOpcUaClient UaClient = new WpfMtpControl.MtpVisuOpcUaClient();

        private MtpDataSourceSubscriber _activeSubscriber = null;

        private MtpSymbolMapRecordList _hintsForConfigRecs = null;

        // window / plugin mechanics

        public WpfMtpControlWrapper()
        {
            InitializeComponent();

            // use pre-definitions
            this._defsInterop = new AasxPredefinedConcepts.DefinitionsExperimental.InteropRelations();
            this._defsMtp = new DefinitionsMTP.ModuleTypePackage();
        }

        public void Start(
            AdminShellPackageEnvBase thePackage,
            Aas.Submodel theSubmodel,
            AasxPluginMtpViewer.MtpViewerOptions theOptions,
            PluginEventStack eventStack,
            LogInstance log)
        {
            _package = thePackage;
            _submodel = theSubmodel;
            _aas = _package?.AasEnv?.FindAasWithSubmodelId(_submodel?.Id);
            _options = theOptions;
            _eventStack = eventStack;
            _log = log;
        }

        public static WpfMtpControlWrapper FillWithWpfControls(
            object opackage, object osm,
            AasxPluginMtpViewer.MtpViewerOptions options,
            PluginEventStack eventStack,
            LogInstance log,
            object masterDockPanel)
        {
            // access
            var package = opackage as AdminShellPackageEnvBase;
            var sm = osm as Aas.Submodel;
            var master = masterDockPanel as DockPanel;
            if (package == null || sm == null || master == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // create TOP control
            var wrapperCntl = new WpfMtpControlWrapper();
            wrapperCntl.Start(package, sm, options, eventStack, log);
            master.Children.Add(wrapperCntl);

            // return shelf
            return wrapperCntl;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize symbol library
            this._theSymbolLib = new MtpSymbolLib();

            var ISO10628 = new ResourceDictionary();
            ISO10628.Source = new Uri(
                "pack://application:,,,/WpfMtpControl;component/Resources/PNID_DIN_EN_ISO_10628.xaml");
            this._theSymbolLib.ImportResourceDicrectory("PNID_ISO10628", ISO10628);

            var FESTO = new ResourceDictionary();
            FESTO.Source = new Uri(
                "pack://application:,,,/WpfMtpControl;component/Resources/PNID_Festo.xaml");
            this._theSymbolLib.ImportResourceDicrectory("PNID_Festo", FESTO);

            // initialize visual object libraries
            _activeVisualObjectLib = new WpfMtpControl.MtpVisualObjectLib();
            _activeVisualObjectLib.LoadStatic(this._theSymbolLib);

            // gather infos
            var ok = GatherMtpInfos(_preLoadInfo);
            if (ok && _activeMtpFileFn != null && _mtpTypeSm != null)
            {
                // access file
                var inputFn = this._activeMtpFileFn;
                if (CheckIfPackageFile(inputFn))
                {
                    // build idShort Path
                    var idShortPath = "" + _activeMtpFileElem.CollectIdShortPathByParent(
                            separatorChar: '.', excludeIdentifiable: true);

                    // _mtpTypeSm might be in another AAS
                    var mtpTypeAas = _package?.AasEnv?.FindAasWithSubmodelId(_mtpTypeSm.Id);

                    // wrap async
                    var task = Task.Run(() => _package.MakePackageFileAvailableAsTempFileAsync(
                        packageUri: inputFn,
                        aasId: "" + mtpTypeAas?.Id,
                        smId: "" + _mtpTypeSm.Id,
                        idShortPath: idShortPath));
                    task.Wait();
                    inputFn = task.Result;
                }

                // load file
                LoadFile(inputFn);

                // fit it
                this.mtpVisu.ZoomToFitCanvas();

                // double click handler
                this.mtpVisu.MtpObjectDoubleClick += MtpVisu_MtpObjectDoubleClick;
            }

            // Timer for status
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            // ReSharper disable once RedundantDelegateCreation
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.UaClient == null)
                textBoxDataSourceStatus.Text = "(no OPC UA client enabled)";
            else
            {
                await UaClient.TickAsync(100);
                textBoxDataSourceStatus.Text = this.UaClient.GetStatus();
            }
        }

        // handle Submodel data

        private bool GatherMtpInfos(MtpDataSourceOpcUaPreLoadInfo preLoadInfo)
        {
            // access
            var env = _package?.AasEnv;
            if (_submodel?.SemanticId == null || _submodel.SubmodelElements == null
                || this._defsMtp == null
                || env?.AssetAdministrationShells == null
                || _package.AasEnv.Submodels == null)
                return false;

            // need to find the type Submodel
            _mtpTypeSm = null;

            // check, if the user pointed to the instance submodel
            if (_submodel.SemanticId.Matches(this._defsMtp.SEM_MtpInstanceSubmodel))
            {
                // Source list
                foreach (var srcLst in _submodel.SubmodelElements
                    .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        this._defsMtp.CD_SourceList?.GetReference(), MatchMode.Relaxed))
                {
                    // found a source list, might contain sources
                    if (srcLst?.Value == null)
                        continue;

                    // UA Server?
                    foreach (var src in srcLst.Value.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        this._defsMtp.CD_SourceOpcUaServer?.GetReference(), MatchMode.Relaxed))
                        if (src?.Value != null)
                        {
                            // UA server
                            var ep = src.Value.FindFirstSemanticIdAs<Aas.Property>(
                                this._defsMtp.CD_Endpoint.GetReference(), MatchMode.Relaxed)?.Value;

                            // add
                            if (preLoadInfo?.EndpointMapping != null)
                                preLoadInfo.EndpointMapping.Add(
                                    new MtpDataSourceOpcUaEndpointMapping(
                                        "" + ep, ForName: ("" + src.IdShort).Trim()));
                        }
                }

                // Identifier renaming?
                foreach (var ren in _submodel.SubmodelElements
                    .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                    this._defsMtp.CD_IdentifierRenaming?.GetReference(), MatchMode.Relaxed))
                    if (ren?.Value != null)
                    {
                        var oldtxt = ren.Value.FindFirstSemanticIdAs<Aas.Property>(
                            this._defsMtp.CD_RenamingOldText?.GetReference(), MatchMode.Relaxed)?.Value;
                        var newtxt = ren.Value.FindFirstSemanticIdAs<Aas.Property>(
                            this._defsMtp.CD_RenamingNewText?.GetReference(), MatchMode.Relaxed)?.Value;
                        if (oldtxt.HasContent() && newtxt.HasContent() &&
                            preLoadInfo?.IdentifierRenaming != null)
                            preLoadInfo.IdentifierRenaming.Add(new MtpDataSourceStringReplacement(oldtxt, newtxt));
                    }

                // Namespace renaming?
                foreach (var ren in _submodel.SubmodelElements
                    .FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                    this._defsMtp.CD_NamespaceRenaming?.GetReference(), MatchMode.Relaxed))
                    if (ren?.Value != null)
                    {
                        var oldtxt = ren?.Value.FindFirstSemanticIdAs<Aas.Property>(
                            this._defsMtp.CD_RenamingOldText?.GetReference(), MatchMode.Relaxed)?.Value;
                        var newtxt = ren?.Value.FindFirstSemanticIdAs<Aas.Property>(
                            this._defsMtp.CD_RenamingNewText?.GetReference(), MatchMode.Relaxed)?.Value;
                        if (oldtxt.HasContent() && newtxt.HasContent() &&
                            preLoadInfo?.NamespaceRenaming != null)
                            preLoadInfo.NamespaceRenaming.Add(new MtpDataSourceStringReplacement(oldtxt, newtxt));
                    }

                // according spec from Sten Gruener, the derivedFrom relationship shall be exploited.
                // How to get from subModel to AAS?
                var instanceAas = env.FindAasWithSubmodelId(_submodel.Id);
                var typeAas = env.FindReferableByReference(instanceAas?.DerivedFrom) as Aas.AssetAdministrationShell;
                if (instanceAas?.DerivedFrom != null && typeAas != null)
                    foreach (var msm in env.FindAllSubmodelGroupedByAAS((aas, sm) =>
                    {
                        return aas == typeAas && true == sm?.SemanticId?.Matches(this._defsMtp.SEM_MtpSubmodel);
                    }))
                    {
                        _mtpTypeSm = msm;
                        break;
                    }

                // another possibility: direct reference
                var dirLink = _submodel.SubmodelElements
                    .FindFirstSemanticIdAs<Aas.ReferenceElement>(
                        this._defsMtp.CD_MtpTypeSubmodel?.GetReference(), MatchMode.Relaxed);
                var dirLinkSm = env.FindReferableByReference(dirLink?.Value) as Aas.Submodel;
                if (_mtpTypeSm == null)
                    _mtpTypeSm = dirLinkSm;

            }

            // other (not intended) case: user points to type submodel directly
            if (_mtpTypeSm == null
                && _submodel.SemanticId.Matches(this._defsMtp.SEM_MtpSubmodel))
                _mtpTypeSm = _submodel;

            // ok, is there a type submodel?
            if (_mtpTypeSm == null)
                return false;

            // find file, remember Submodel element for it, find filename
            // (ConceptDescription)(no-local)[IRI]http://www.admin-shell.io/mtp/v1/MTPSUCLib/ModuleTypePackage
            this._activeMtpFileElem = _mtpTypeSm.SubmodelElements?
                .FindFirstSemanticIdAs<Aas.File>(this._defsMtp.CD_MtpFile.GetReference(),
                    MatchMode.Relaxed);
            var inputFn = this._activeMtpFileElem?.Value;
            if (inputFn == null)
                return false;
            this._activeMtpFileFn = inputFn;

            return true;
        }

        // MTP handlings

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        private void LoadFile(string fn)
        {
            // access
            if (fn?.HasContent() != true)
                return;
            if (!".aml .zip .mtp".Contains(System.IO.Path.GetExtension(fn.Trim().ToLower())))
                return;

            this.UaClient = new WpfMtpControl.MtpVisuOpcUaClient();
            this.UaClient.ItemChanged += Client_ItemChanged;
            this._activeSubscriber = new MtpDataSourceSubscriber();
            this._hintsForConfigRecs = new MtpSymbolMapRecordList();

            this._activeMtpData = new WpfMtpControl.MtpData();
            this._activeMtpData.LoadAmlOrMtp(_activeVisualObjectLib,
                this.UaClient, this._preLoadInfo, this._activeSubscriber, fn);

            if (this._activeMtpData.PictureCollection.Count > 0)
                mtpVisu.SetPicture(this._activeMtpData.PictureCollection.Values.ElementAt(0));
            mtpVisu.RedrawMtp();
        }

        private void Client_ItemChanged(WpfMtpControl.DataSources.IMtpDataSourceStatus dataSource,
            MtpVisuOpcUaClient.DetailItem itemRef, MtpVisuOpcUaClient.ItemChangeType changeType)
        {
            if (dataSource == null || itemRef == null || itemRef.MtpSourceItemId == null
                || this._activeSubscriber == null)
                return;

            if (changeType == MtpVisuOpcUaClient.ItemChangeType.Value)
                this._activeSubscriber.Invoke(itemRef.MtpSourceItemId, MtpDataSourceSubscriber.ChangeType.Value,
                    itemRef.Value);
        }

        private void MtpVisu_MtpObjectDoubleClick(MtpData.MtpBaseObject source)
        {
            // access
            var sme = _submodel?.SubmodelElements;
            var first = this._activeMtpFileElem.GetReference();
            if (source == null || this._activeMtpFileElem == null || sme == null || first == null)
                return;

            // for the active file, find a Reference for it

            foreach (var searchId in new[] { source.Name, source.RefID })
            {
                // access
                if (searchId == null)
                    continue;
                //
                // Search for FileToNavigateElement
                //

                var firstFtn = first.Add(new Aas.Key(Aas.KeyTypes.GlobalReference, searchId));
                _log?.Info($"DblClick MTP .. search reference: {firstFtn.ToStringExtended(1)}");

                foreach (var fileToNav in sme.FindAllSemanticIdAs<Aas.RelationshipElement>(
                this._defsInterop?.CD_FileToNavigateElement?.GetReference(), MatchMode.Relaxed))
                    if (fileToNav.First?.Matches(firstFtn, MatchMode.Relaxed) == true)
                    {
                        // try activate
                        var ev = new AasxIntegrationBase.AasxPluginResultEventNavigateToReference();
                        ev.targetReference = fileToNav.Second.Copy();
                        _eventStack?.PushEvent(ev);
                        return;
                    }

                //
                // Search for FileToEntity
                //

                var firstFte = first.Add(new Aas.Key(Aas.KeyTypes.GlobalReference, searchId));
                _log?.Info($"DblClick MTP .. search reference: {firstFte.ToStringExtended(1)}");

                foreach (var fileToEnt in sme.FindAllSemanticIdAs<Aas.RelationshipElement>(
                this._defsInterop?.CD_FileToEntity?.GetReference(), MatchMode.Relaxed))
                    if (fileToEnt.First?.Matches(firstFte, MatchMode.Relaxed) == true)
                    {
                        // debug
                        _log?.Info($"try find Entity {"" + fileToEnt.Second} ..");

                        // find Entity, check if self-contained
                        var foundRef = _package?.AasEnv?.FindReferableByReference(fileToEnt.Second);
                        if (foundRef is Aas.Entity foundEnt
                            && foundEnt.EntityType == Aas.EntityType.SelfManagedEntity
                            && foundEnt.GlobalAssetId != null)
                        {
                            // try activate
                            var ev = new AasxIntegrationBase.AasxPluginResultEventNavigateToReference();
                            ev.targetReference = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                                new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, foundEnt.GlobalAssetId) }
                                .ToList());
                            _eventStack?.PushEvent(ev);
                            return;
                        }
                    }
            }
        }

        // visual window handling

        private int overlayPanelMode = 0;

        private void SetOverlayPanelMode(int newMode)
        {
            this.overlayPanelMode = newMode;

            switch (this.overlayPanelMode)
            {
                case 2:
                    this.ScrollViewerDataSources.Visibility = Visibility.Visible;
                    DataGridDataSources.ItemsSource = this.UaClient.Items;
                    this.RichTextReport.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    this.ScrollViewerDataSources.Visibility = Visibility.Collapsed;
                    DataGridDataSources.ItemsSource = null;
                    this.RichTextReport.Visibility = Visibility.Visible;
                    ReportOnConfiguration(this.RichTextReport);
                    break;

                default:
                    this.ScrollViewerDataSources.Visibility = Visibility.Collapsed;
                    DataGridDataSources.ItemsSource = null;
                    this.RichTextReport.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == buttonDataSourceDetails)
            {
                if (this.overlayPanelMode != 2)
                    SetOverlayPanelMode(2);
                else
                    SetOverlayPanelMode(0);
            }

            if (sender == buttonConfig)
            {
                if (this.overlayPanelMode != 1)
                    SetOverlayPanelMode(1);
                else
                    SetOverlayPanelMode(0);
            }
        }

        private void AddToRichTextBox(RichTextBox rtb, string text, bool bold = false, double? fontSize = null,
            bool monoSpaced = false)
        {
            var p = new Paragraph();
            if (bold)
                p.FontWeight = FontWeights.Bold;
            if (fontSize.HasValue)
                p.FontSize = fontSize.Value;
            if (monoSpaced)
                p.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            p.Inlines.Add(new Run(text));
            rtb.Document.Blocks.Add(p);
        }

        private void ReportOnConfiguration(RichTextBox rtb)
        {
            // access
            if (rtb == null)
                return;

            rtb.Document.Blocks.Clear();

            //
            // Report on available library symbols
            //

            if (this._theSymbolLib != null)
            {

                AddToRichTextBox(rtb, "Library symbols", bold: true, fontSize: 18);

                AddToRichTextBox(rtb, "The following lists shows available symbol full names.");

                foreach (var x in this._theSymbolLib.Values)
                {
                    AddToRichTextBox(rtb, "" + x.FullName, monoSpaced: true);
                }

                AddToRichTextBox(rtb, "");
            }

            //
            // Hints for configurations
            //

            if (this._hintsForConfigRecs != null)
            {
                AddToRichTextBox(rtb, "Preformatted configuration records", bold: true, fontSize: 18);
                AddToRichTextBox(rtb,
                    "The following JSON elements could be pasted into the options file named " + "" +
                    "'AasxPluginMtpViewer.options.json'. " +
                    "Prior to pasting, an appropriate symbol full name needs to be chosen from above list. " +
                    "For the eClass strings, multiples choices can be delimited by ';'. " +
                    "For EClassVersions, 'null' disables version checking. " +
                    "Either EClassClasses or EClassIRDIs shall be different to 'null'.");

                foreach (var x in this._hintsForConfigRecs)
                {
                    var txt = JsonConvert.SerializeObject(x, Formatting.None);
                    AddToRichTextBox(rtb, "" + txt, monoSpaced: true);
                }

                AddToRichTextBox(rtb, "");
            }
        }
    }
}
