/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AnyUi;
using BlazorExplorer;
using BlazorExplorer.Shared;
using BlazorUI.Utils;
using Microsoft.JSInterop;
using AasCore.Aas3_1;
using ConnectExtendedRecord = AasxPackageLogic.PackageCentral.PackageContainerHttpRepoSubset.ConnectExtendedRecord;

namespace BlazorUI.Data
{
    /// <summary>
    /// This partial class holds the parts which are similar to the MainWindow of
    /// PAckage Explorer.
    /// </summary>
    public partial class BlazorSession : IDisposable, IMainWindow
    {
        /// <summary>
        /// Check for menu switch and flush events, if required.
        /// </summary>
        public void CheckIfToFlushEvents()
        {
            if (MainMenu?.IsChecked("CompressEvents") == true)
            {
                var evs = _eventCompressor?.Flush();
                if (evs != null)
                    foreach (var ev in evs)
                        PackageCentral?.PushEvent(ev);
            }
        }

        /// <summary>
        /// Clears the status line and pending errors.
        /// </summary>
        public void StatusLineClear()
        {
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, SessionId,
                    newLambdaAction: new AnyUiLambdaActionStatusLineClear()));
        }

        /// <summary>
        /// Show log in a window / list perceivable for the user.
        /// </summary>
        public async void LogShow()
        {
            var uc = new AnyUiDialogueDataTextEditor(
                caption: "Log display",
                mimeType: "text/plain");

            var sb = new StringBuilder();
            foreach (var sp in Log.Singleton.GetStoredLongTermPrints())
            {
                var line = "";
                if (sp.color == StoredPrint.Color.Blue || sp.color == StoredPrint.Color.Yellow)
                    line += "!";
                if (sp.color == StoredPrint.Color.Red)
                    line += "Error: ";
                line += sp.msg;
                if (sp.linkTxt != null)
                    line += $" ({sp.linkTxt} -> {sp.linkUri})";
                sb.AppendLine(line);
            }
            uc.Text = sb.ToString();
            uc.ReadOnly = true;

            await DisplayContext.StartFlyoverModalAsync(uc);
        }

        /// <summary>
        /// Take a screenshot and save to file
        /// </summary>
        public void SaveScreenshot(string filename = "noname")
        {
            // no such capability
        }

        public async Task CommandExecution_RedrawAllAsync()
        {
            await Task.Yield();

            // redraw everything
            await RedrawAllAasxElementsAsync();
            await RedrawElementViewAsync();
        }

        private PackCntRuntimeOptions UiBuildRuntimeOptionsForMainAppLoad()
        {
            var ro = new PackCntRuntimeOptions()
            {
                Log = Log.Singleton,
                ProgressChanged = (state, tfs, tbd, msg) =>
                {
                    ;
                },
                ShowMesssageBox = (content, text, title, buttons) =>
                {
                    return AnyUiMessageBoxResult.Cancel;
                }
            };
            return ro;
        }

        public async Task UiLoadPackageWithNew(
            PackageCentralItem packItem,
            AdminShellPackageEnvBase takeOverEnv = null,
            string loadLocalFilename = null,
            string info = null,
            bool onlyAuxiliary = false,
            bool doNotNavigateAfterLoaded = false,
            PackageContainerBase takeOverContainer = null,
            string storeFnToLRU = null,
            bool indexItems = false,
            bool preserveEditMode = false,
            bool? nextEditMode = null, 
            bool autoFocusFirstRelevant = false)
        {
            await Task.Yield();

            // access
            if (packItem == null)
                return;

            if (loadLocalFilename != null)
            {
                if (info == null)
                    info = loadLocalFilename;
                Log.Singleton.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);
                if (!packItem.Load(PackageCentral, loadLocalFilename, loadLocalFilename,
                    overrideLoadResident: true,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr)))
                {
                    Log.Singleton.Error($"Loading local-file {info} as auxiliary {onlyAuxiliary} did not " +
                        $"return any result!");
                    return;
                }
            }
            else
            if (takeOverEnv != null)
            {
                Log.Singleton.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);
                packItem.TakeOver(takeOverEnv);
            }
            else
            if (takeOverContainer != null)
            {
                Log.Singleton.Info("Loading new AASX from container: {0} as auxiliary {1} ..",
                    "" + takeOverContainer.ToString(), onlyAuxiliary);
                packItem.TakeOver(takeOverContainer);
            }
            else
            {
                Log.Singleton.Error("UiLoadPackageWithNew(): no information what to load!");
                return;
            }

            // displaying
            try
            {
                RestartUIafterNewPackage(onlyAuxiliary);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When displaying element tree of {info}, an error occurred");
                return;
            }

            // further actions
            try
            {
                // dead-csharp off
                // TODO (MIHO, 2020-12-31): check for ANYUI MIHO
                //if (!doNotNavigateAfterLoaded)
                //    UiCheckIfActivateLoadedNavTo();
                // dead-csharp off

                if (indexItems && packItem?.Container?.Env?.AasEnv != null)
                    packItem.Container.SignificantElements
                        = new IndexOfSignificantAasElements(packItem.Container.Env.AasEnv);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When performing actions after load of {info}, an error occurred");
                return;
            }

            // record in LRU?
            try
            {
                var lru = PackageCentral?.Repositories?.FindLRU();
                if (lru != null && storeFnToLRU.HasContent())
                    lru.Push(packItem?.Container as PackageContainerRepoItem, storeFnToLRU);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When managing LRU files");
                return;
            }

            // done
            Log.Singleton.Info("AASX {0} loaded.", info);
        }

        public bool CheckIsAnyTaintedIdentifiableInMain()
        {
            return DisplayElements.IsAnyTaintedIdentifiable();
        }        

        public async Task RestartUIafterNewPackage(bool onlyAuxiliary = false, bool? nextEditMode = null)
        {
            if (onlyAuxiliary)
            {
                // reduced, in the background
                await RedrawAllAasxElementsAsync();
            }
            else
            {
                // dead-csharp off
                // visually a new content
                // switch off edit mode -> will will cause the browser to show the AAS as selected element
                // and -> this will update the left side of the screen correctly!
                // _mainMenu?.SetChecked("EditMenu", false);
                // ClearAllViews();
                await RedrawAllAasxElementsAsync();
                await RedrawElementViewAsync();
                // ShowContentBrowser(Options.Curr.ContentHome, silent: true);
                // _eventHandling.Reset();
                // dead-csharp on
            }
        }

        /// <summary>
        /// Redraw window title, AAS info?, entity view (right), element tree (middle)
        /// </summary>
        /// <param name="keepFocus">Try remember which element was focussed and focus it after redrawing.</param>
        /// <param name="nextFocusMdo">Focus a new main data object attached to an tree element.</param>
        /// <param name="wishExpanded">If focussing, expand this item.</param>
        public async Task RedrawAllAasxElementsAsync(
            bool keepFocus = false,
            object nextFocusMdo = null,
            bool wishExpanded = true)
        {
            // focus info
            var focusMdo = DisplayElements.SelectedItem?.GetDereferencedMainDataObject();
            // dead-csharp off
            // TODO (??, 0000-00-00): Can we set title of the browser tab?
            //var t = "AASX Package Explorer V3RC02";  
            //TODO (jtikekar, 0000-00-00): remove V3RC02
            //if (PackageCentral.MainAvailable)
            //    t += " - " + PackageCentral.MainItem.ToString();
            //if (PackageCentral.AuxAvailable)
            //    t += " (auxiliary AASX: " + PackageCentral.AuxItem.ToString() + ")";            
            // this.Title = t;
            // dead-csharp on
#if _log_times
            Log.Singleton.Info("Time 10 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif
            // dead-csharp off
            // clear the right section, first (might be rebuild by callback from below)
            // DispEditEntityPanel.ClearDisplayDefautlStack();
            // ContentTakeOver.IsEnabled = false;

            // rebuild middle section
            DisplayElements.RebuildAasxElements(
                PackageCentral, PackageCentral.Selector.Main, this.EditMode,
                lazyLoadingFirst: true);
            // dead-csharp on
            // ok .. try re-focus!!
            if (keepFocus || nextFocusMdo != null)
            {
                // make sure that Submodel is expanded
                this.DisplayElements.ExpandAllItems();

                // still proceed?
                var veFound = this.DisplayElements.SearchVisualElementOnMainDataObject(
                    (nextFocusMdo != null) ? nextFocusMdo : focusMdo,
                    alsoDereferenceObjects: true);

                // select?
                if (veFound != null)
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: wishExpanded);
            }

            // Info box ..
            await RedrawElementViewAsync();

            // display again
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, this.SessionId));
            DisplayElements.Refresh();

            if (PackageCentral?.MainAvailable != true)
                BlazorFileDropHandler?.ClearUploadBanner();

#if _log_times
            Log.Singleton.Info("Time 90 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif
        }

        /// <summary>
        /// Based on save information, will redraw the AAS entity (element) view (right).
        /// </summary>
        /// <param name="hightlightField">Highlight field (for find/ replace)</param>
        public async Task RedrawElementViewAsync(DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            await Task.Yield();

            if (DisplayElements == null)
                return;

            // no cached plugin
            DisposeLoadedPlugin();

            // the AAS will cause some more visual effects
            if (DisplayElements.SelectedItem is VisualElementAdminShell veaas)
                InfoBox.SetInfos(veaas.theAas, veaas.thePackage);
            else
                InfoBox.SetInfos(null, null);
        }

        /// <summary>
        /// Clear AAS info, tree section, browser window
        /// </summary>
        public void ClearAllViews()
        {
            // left info card (same empty copy as SetInfos(null))
            InfoBox.AasId = "# No information available";
            InfoBox.HtmlImageData = "";
            InfoBox.AssetId = "";

            // middle side
            DisplayElements.Clear();
        }

        /// <inheritdoc />
        public void ClearTransientOpenUiState()
        {
            BlazorFileDropHandler?.ClearUploadBanner();
        }

        public async Task DisplayElements_SelectedItemChanged(object sender, EventArgs e)
        {
            // access
            if (DisplayElements == null || sender != DisplayElements)
                return;

            // Mirror WPF MainWindow.DisplayElements_MouseDoubleClick: tree nodes "Fetch previous" / "Fetch next"
            // trigger pagination. (WPF uses double-click; Blazor has no tree double-click — run on selection.)
            var si = DisplayElements.SelectedItem;
            if (si is VisualElementEnvironmentItem siei
                && (siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev
                    || siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext))
            {
                if (siei.thePackage is AdminShellPackageDynamicFetchEnv dynPack
                    && dynPack.GetContext() is PackageContainerHttpRepoSubsetFetchContext fetchContext
                    && fetchContext.Record != null)
                {
                    var goPrev = siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev;
                    var goNext = siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext;
                    var goNextFake = false;
                    if (goNext && fetchContext.Cursor?.HasContent() != true)
                    {
                        Log.Singleton.Info(StoredPrint.Color.Blue,
                            "No cursor for fetch operation available " +
                            "(at the end of the selected subset of elements or no server support).");
                        goNext = false;
                        goNextFake = true;
                    }

                    await DispEditHelperEntities.ExecuteUiForFetchOfElements(
                        PackageCentral, DisplayContext, new AasxMenuActionTicket(), this,
                        fetchContext,
                        preserveEditMode: true,
                        doEditNewRecord: false,
                        doCheckTainted: true,
                        doFetchGoPrev: goPrev,
                        doFetchGoNext: goNext,
                        doFakeGoNext: goNextFake,
                        doFetchExec: true);
                }
                else
                {
                    Log.Singleton.Error("Fetch next within dynamic environment: " +
                        "Not enough data to provide dynamic fetch operations.");
                }

                CheckIfToFlushEvents();
                return;
            }

            // try identify the business object
            if (DisplayElements.SelectedItem != null)
            {
                Logic?.LocationHistory?.Push(DisplayElements.SelectedItem);
            }

            // may be flush events
            CheckIfToFlushEvents();

            // redraw view
            await RedrawElementViewAsync();
        }

        /// <summary>
        /// Make sure the file repo is visible
        /// </summary>
        public void UiShowRepositories(bool visible)
        {
            // for Blazor: nothing
            ;
        }

        /// <summary>
        /// Give a signal to redraw the repositories (because something has changed)
        /// </summary>
        public void RedrawRepositories()
        {
            // Blazor: simply redraw all
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, SessionId));
        }

        // REFACTOR: for later refactoring
        /// <summary>
        /// Signal a redrawing and execute focussing afterwards.
        /// </summary>
        public void RedrawAllElementsAndFocus(object nextFocus = null, bool isExpanded = true)
        {
            // Blazor: refer 
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, SessionId,
                    new AnyUiLambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: isExpanded)));
        }

        /// <summary>
        /// Gets the interface to the components which manages the AAS tree elements (middle section)
        /// </summary>
        public IDisplayElements GetDisplayElements() => DisplayElements;

        /// <summary>
        /// Allows an other class to inject a lambda action.
        /// This will be perceived by the main window, most likely.
        /// </summary>
        public void AddWishForToplevelAction(AnyUiLambdaActionBase action)
        {
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, SessionId,
                    action));
        }

        //
        // Subject to refactor
        //

        protected class LoadFromFileRepositoryInfo
        {
            public Aas.IReferable Referable;
            public object BusinessObject;
        }

        protected async Task<LoadFromFileRepositoryInfo> LoadFromFileRepository(PackageContainerRepoItem fi,
            Aas.IReference requireReferable = null)
        {
            // access single file repo
            var fileRepo = PackageCentral.Repositories.FindRepository(fi);
            if (fileRepo == null)
                return null;

            // which file?
            var location = fileRepo.GetFullItemLocation(fi?.Location);
            if (location == null)
                return null;

            // try load (in the background/ RAM) first..
            PackageContainerBase container = null;
            try
            {
                Log.Singleton.Info($"Auto-load file from repository {location} into container");
                container = await PackageContainerFactory.GuessAndCreateForAsync(
                    PackageCentral,
                    location,
                    location,
                    overrideLoadResident: true,
                    autoAuthenticate: Options.Curr.AutoAuthenticateUris,
                    null, null,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    runtimeOptions: PackageCentral.CentralRuntimeOptions);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"When auto-loading {location}");
            }

            // if successfull ..
            if (container != null)
            {
                // .. try find business object!
                LoadFromFileRepositoryInfo res = new LoadFromFileRepositoryInfo();
                if (requireReferable != null)
                {
                    var rri = new ExtendEnvironment.ReferableRootInfo();
                    res.Referable = container.Env?.AasEnv.FindReferableByReference(requireReferable, rootInfo: rri);
                    res.BusinessObject = res.Referable;

                    // do some special decoding because auf Meta Model V3
                    if (rri.Asset != null)
                        res.BusinessObject = rri.Asset;
                }

                // only proceed, if business object was found .. else: close directly
                if (requireReferable != null && res.Referable == null)
                    container.Close();
                else
                {
                    // make sure the user wants to change
                    if (MainMenu?.IsChecked("FileRepoLoadWoPrompt") != true)
                    {
                        // ask double question
                        if (AnyUiMessageBoxResult.OK != await DisplayContext.MessageBoxFlyoutShowAsync(
                                "Load file from AASX file repository?",
                                "AASX File Repository",
                                AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                            return null;
                    }

                    // start animation
                    fileRepo.StartAnimation(fi, PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                    // activate
                    UiLoadPackageWithNew(PackageCentral.MainItem,
                        takeOverContainer: container, onlyAuxiliary: false);

                    Log.Singleton.Info($"Successfully loaded AASX {location}");
                }

                // return bo to focus
                return res;
            }

            return null;
        }

        /// <summary>
        /// Same logic as WPF <see cref="AasxPackageExplorer.MainWindow.UiSearchRepoAndExtendEnvironmentAsync"/>:
        /// resolve a reference against a connected HTTP repo / dynamic-fetch environment.
        /// </summary>
        public async Task<Aas.IIdentifiable> UiSearchRepoAndExtendEnvironmentAsync(
            AdminShellPackageEnvBase packEnv,
            Aas.IReference workRef = null,
            string fullItemLocation = null,
            bool trySelect = false)
        {
            await Task.Yield();

            if (packEnv == null || (workRef?.IsValid() != true && fullItemLocation?.HasContent() != true))
                return null;

            if (packEnv is not AdminShellPackageDynamicFetchEnv dynPack)
                return null;

            var context = dynPack.GetContext() as PackageContainerHttpRepoSubsetFetchContext;
            var record = context?.Record?.Copy();
            if (record == null)
                return null;

            if (record.BaseAddress?.HasContent() != true)
                return null;

            BaseUriDict baseUris = null;
            var searches = new List<Tuple<ConnectExtendedRecord, BaseUriDict, string>>();
            if (workRef?.IsValid() == true)
            {
                if (workRef.Count() >= 1 && workRef.Keys[0].Type == Aas.KeyTypes.AssetAdministrationShell)
                {
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleAas);
                    record.AasId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }

                if (workRef.Count() >= 1 && workRef.Keys[0].Type == Aas.KeyTypes.GlobalReference)
                {
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.AasByAssetLink);
                    record.AssetId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }

                if (workRef.Count() >= 1 && (workRef.Keys[0].Type == Aas.KeyTypes.GlobalReference
                                             || workRef.Keys[0].Type == Aas.KeyTypes.Submodel))
                {
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleSM);
                    record.SmId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }

                if (workRef.Count() >= 1 && (workRef.Keys[0].Type == Aas.KeyTypes.GlobalReference
                                             || workRef.Keys[0].Type == Aas.KeyTypes.ConceptDescription))
                {
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleCD);
                    record.CdId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }
            }

            if (searches.Count < 1)
                return null;

            var foundIdfs = new List<Aas.IIdentifiable>();
            foreach (var search in searches)
            {
                var newIdfs = new List<Aas.IIdentifiable>();
                var loadedIdfs = new List<Aas.IIdentifiable>();

                var loadRes = await PackageContainerHttpRepoSubset.LoadFromSourceToTargetAsync(
                    fullItemLocation: search.Item3,
                    targetEnv: packEnv,
                    loadNew: false,
                    trackNewIdentifiables: newIdfs,
                    trackLoadedIdentifiables: loadedIdfs,
                    containerOptions: new PackageContainerHttpRepoSubset.PackageContainerHttpRepoSubsetOptions(
                        PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        search.Item1)
                    {
                        BaseUris = search.Item2
                    },
                    runtimeOptions: PackageCentral.CentralRuntimeOptions);

                if (loadRes != null && newIdfs.Count >= 1)
                {
                    foundIdfs.AddRange(newIdfs);
                    break;
                }
            }

            if (foundIdfs.Count < 1)
                return null;

            DisplayElements.RebuildAasxElements(
                PackageCentral, PackageCentral.Selector.Main, EditMode,
                lazyLoadingFirst: true);

            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.RebuildTreeKeepOpen, SessionId));

            var newIdf = foundIdfs.FirstOrDefault();

            if (trySelect && newIdf != null)
            {
                var veFound = DisplayElements.SearchVisualElementOnMainDataObject(newIdf, alsoDereferenceObjects: true);
                if (veFound != null)
                {
                    DisplayElements.ExpandAllItems();
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                    Logic?.LocationHistory?.Push(veFound);
                    await RedrawElementViewAsync();
                    DisplayElements.Refresh();
                }
            }

            return newIdf;
        }

        public async Task UiHandleNavigateTo(
            Aas.IReference targetReference,
            bool alsoDereferenceObjects = true)
        {
            // access
            if (targetReference == null || targetReference.Keys.Count < 1)
                return;

            // make a copy of the Reference for searching
            VisualElementGeneric veFound = null;
            var work = targetReference.Copy();

            try
            {
                // remember some further supplementary search information
                var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(work);
                work = sri.CleanReference;

                // for later search in visual elements, expand them all in order to be absolutely 
                // sure to find business object
                this.DisplayElements.ExpandAllItems();

                // incrementally make it unprecise (same order as WPF MainWindow.UiHandleNavigateTo)
                var firstTime = true;
                while (work.Keys.Count > 0)
                {
                    object bo = null;
                    if (PackageCentral.MainAvailable && PackageCentral.Main.AasEnv != null)
                        bo = PackageCentral.Main.AasEnv.FindReferableByReference(work);

                    if (firstTime && bo == null)
                    {
                        bo = await UiSearchRepoAndExtendEnvironmentAsync(PackageCentral.Main, work);
                        firstTime = false;
                    }

                    if (bo == null && PackageCentral.Aux != null && PackageCentral.Aux.AasEnv != null)
                        bo = PackageCentral.Aux.AasEnv.FindReferableByReference(work);

                    if (bo == null && PackageCentral.Repositories != null)
                    {
                        PackageContainerRepoItem fi = null;
                        if (work.Keys[0].Type == Aas.KeyTypes.GlobalReference)
                            fi = await PackageCentral.Repositories.FindByAssetId(work.Keys[0].Value.Trim());
                        if (work.Keys[0].Type == Aas.KeyTypes.AssetAdministrationShell)
                            fi = await PackageCentral.Repositories.FindByAasId(work.Keys[0].Value.Trim());

                        var boInfo = await LoadFromFileRepository(fi, work);
                        bo = boInfo?.BusinessObject;
                    }

                    if (bo != null && DisplayElements != null)
                    {
                        DisplayElements.ExpandAllItems();
                        var ve = DisplayElements.SearchVisualElementOnMainDataObject(bo,
                            alsoDereferenceObjects: alsoDereferenceObjects, sri: sri);
                        if (ve != null)
                        {
                            veFound = ve;
                            break;
                        }
                    }

                    work.Keys.RemoveAt(work.Keys.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While retrieving element requested for navigate to");
            }

            // if successful, try to display it
            try
            {
                if (veFound != null)
                {
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                    Logic?.LocationHistory?.Push(veFound);
                    await RedrawElementViewAsync();
                    DisplayElements.Refresh();
                    Program.signalNewData(
                        new Program.NewDataAvailableArgs(
                            Program.DataRedrawMode.RebuildTreeKeepOpen, SessionId));
                }
                else
                {
                    // everything is in default state, push adequate button history
                    var veTop = DisplayElements.GetDefaultVisualElement();
                    // ButtonHistory.Push(veTop);
                    // dead-csharp off
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying element requested for navigate to");
            }
        }

        /// <summary>
        /// WPF: <c>MainWindow.ShowContent_Click</c> for a <see cref="VisualElementSubmodelElement"/> (tree double-click).
        /// Opens file/blob content in the browser or a text editor flyout when in edit mode.
        /// </summary>
        public async Task TryShowSubmodelElementContentAsync(VisualElementSubmodelElement veSme)
        {
            if (veSme == null || !PackageCentral.MainAvailable)
                return;

            await Task.Yield();

            if (veSme.theWrapper is Aas.IBlob blb
                && EditMode
                && AdminShellUtil.CheckForTextContentType(blb.ContentType))
            {
                try
                {
                    var uc = new AnyUiDialogueDataTextEditor(
                        caption: $"Edit Blob '{"" + blb.IdShort}'",
                        mimeType: blb.ContentType,
                        text: Encoding.Default.GetString(blb.Value ?? Array.Empty<byte>()));
                    if (await DisplayContext.StartFlyoverModalAsync(uc))
                    {
                        blb.Value = Encoding.Default.GetBytes(uc.Text);
                        await RedrawElementViewAsync();
                        Program.signalNewData(
                            new Program.NewDataAvailableArgs(
                                Program.DataRedrawMode.ValueChanged, SessionId));
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When editing blob content from {blb.IdShort}");
                }

                return;
            }

            if (veSme.theWrapper is Aas.IFile file
                && EditMode
                && AdminShellUtil.CheckForTextContentType(file.ContentType))
            {
                await DispEditHelperModules.DisplayOrEditEntityFileResource_EditTextFileAsync(
                    DisplayContext, PackageCentral.Main, file.ContentType, file.Value);
                return;
            }

            Tuple<object, string> contentFound = null;
            if (veSme.theWrapper is Aas.IFile scFile)
                contentFound = new Tuple<object, string>(scFile.Value, scFile.ContentType);
            if (veSme.theWrapper is Aas.IBlob scBlob && !EditMode)
                contentFound = new Tuple<object, string>(scBlob.Value, scBlob.ContentType);

            if (contentFound == null || renderJsRuntime == null)
                return;

            try
            {
                if (contentFound.Item1 is string contentUri)
                {
                    if (!contentUri.ToLower().Trim().StartsWith("http://")
                        && !contentUri.ToLower().Trim().StartsWith("https://"))
                    {
                        var x = veSme.FindAasSubmodelIdShortPath();
                        contentUri = await PackageCentral.Main.MakePackageFileAvailableAsTempFileAsync(contentUri,
                            aasId: x?.Item1?.Id,
                            smId: x?.Item2?.Id,
                            idShortPath: x?.Item3,
                            secureAccess: _securityAccessHandler);
                    }

                    await BlazorUtils.DisplayOrDownloadFile(renderJsRuntime, contentUri, contentFound.Item2);
                }
                else if (contentFound.Item1 is byte[] ba)
                {
                    var tempext = AdminShellUtil.GuessExtension(
                        contentType: contentFound.Item2,
                        contents: ba);
                    var temppath = System.IO.Path.GetTempFileName().Replace(".tmp", tempext);
                    System.IO.File.WriteAllBytes(temppath, ba);
                    await BlazorUtils.DisplayOrDownloadFile(renderJsRuntime, temppath, contentFound.Item2);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "When displaying submodel element file/blob content");
            }
        }

        /// <summary>
        /// If a button is provided to take over just edited fields, enable/ disable it.
        /// </summary>
        public void TakeOverContentEnable(bool enabled)
        {
            // nothing to do for Blazor version
        }

        /// <summary>
        /// Triggers update of display
        /// </summary>
        public void UpdateDisplay()
        {
            Program.signalNewData(
                new Program.NewDataAvailableArgs(
                    Program.DataRedrawMode.None, SessionId));
        }
    }

}
