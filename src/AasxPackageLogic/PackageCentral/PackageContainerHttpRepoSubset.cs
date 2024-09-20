/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxOpenIdClient;
using AdminShellNS;
using Aas = AasCore.Aas3_0;
using Extensions;
using IdentityModel.Client;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AasxPackageExplorer;
using AnyUi;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This container represents a subset of AAS elements retrieved from a HTTP / networked repository.
    /// </summary>
    [DisplayName("HttpRepoSubset")]
    public class PackageContainerHttpRepoSubset : PackageContainerRepoItem
    {
        /// <summary>
        /// Location of the Container in a certain storage container, e.g. a local or network based
        /// repository. In this implementation, the Location refers to a HTTP network ressource.
        /// </summary>
        public override string Location
        {
            get { return _location; }
            set { SetNewLocation(value); OnPropertyChanged("InfoLocation"); }
        }
        //
        // Constructors
        //

        public PackageContainerHttpRepoSubset()
        {
            Init();
        }

        public PackageContainerHttpRepoSubset(
            PackageCentral packageCentral,
            string sourceLocation, PackageContainerOptionsBase containerOptions = null)
            : base(packageCentral)
        {
            Init();
            SetNewLocation(sourceLocation);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }

        public PackageContainerHttpRepoSubset(CopyMode mode, PackageContainerBase other,
            PackageCentral packageCentral = null,
            string sourceLocation = null, PackageContainerOptionsBase containerOptions = null)
            : base(mode, other, packageCentral)
        {
            if ((mode & CopyMode.Serialized) > 0 && other != null)
            {
            }
            if ((mode & CopyMode.BusinessData) > 0 && other is PackageContainerNetworkHttpFile o)
            {
                sourceLocation = o.Location;
            }
            if (sourceLocation != null)
                SetNewLocation(sourceLocation);
            if (containerOptions != null)
                ContainerOptions = containerOptions;
        }

        public static async Task<PackageContainerHttpRepoSubset> CreateAndLoadAsync(
            PackageCentral packageCentral,
            string location,
            string fullItemLocation,
            bool overrideLoadResident,
            PackageContainerBase takeOver = null,
            PackageContainerListBase containerList = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            var res = new PackageContainerHttpRepoSubset(CopyMode.Serialized, takeOver,
                packageCentral, location, containerOptions);

            res.ContainerList = containerList;

            if (overrideLoadResident || true == res.ContainerOptions?.LoadResident)
                await res.LoadFromSourceAsync(fullItemLocation, containerOptions, runtimeOptions);

            return res;
        }

        //
        // Mechanics
        //

        private void Init()
        {
        }

        private void SetNewLocation(string sourceUri)
        {
            _location = sourceUri;
            IsFormat = Format.AASX;
        }

        public override string ToString()
        {
            return "HTTP Repository element: " + Location;
        }

        public static bool IsValidUriForAllAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shells(|/)$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForSingleAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shells/(.{1,99})$", 
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForSingleSubmodel(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/submodels/(.{1,99})$", 
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            if (m.Success)
                return true;

            // TODO: Add AAS based Submodel
            return false;

        }

        public static bool IsValidUriForSingleCD(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/conceptdescriptions/(.{1,99})$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForQuery(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/aaspe-query/(.{1,9999})$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriAnyMatch(string location)
        {
            return IsValidUriForAllAAS(location)
                || IsValidUriForSingleAAS(location)
                || IsValidUriForSingleSubmodel(location)
                || IsValidUriForSingleCD(location)
                || IsValidUriForQuery(location);
        }

        public static Uri GetBaseUri(string location)
        {
            // try an explicit search for known parts of ressources
            // (preserves scheme, host and leading pathes)
            var m = Regex.Match(location, @"^(.*?)(/shells|/submodel|/conceptdescription)");
            if (m.Success)
                return new Uri(m.Groups[1].ToString() + "/");

            // just go to the first slash
            var p0 = location.IndexOf("//");
            if (p0 > 0)
            {
                var p = location.IndexOf('/', p0 + 2);
                if (p > 0)
                {
                    return new Uri(location.Substring(0, p) + "/");
                }
            }

            // go to error
            return null;
        }

        //public static string CombineUri (string uri1, string uri2)
        //{
        //    var res = "" + uri1;
        //    if (uri2?.HasContent() == true)
        //    {
        //        if (!res.EndsWith("/"))
        //            res += "/";
        //        res += uri2;
        //    }
        //    return res;
        //}

        public static Uri CombineUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || relativeUri?.HasContent() != true)
                return null;

            if (Uri.TryCreate(baseUri, relativeUri, out var res))
                return res;

            return null;
        }

        public static Uri BuildUriForAAS(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"shells/{smidenc}");
        }

        public static Uri BuildUriForAasThumbnail(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"shells/{smidenc}/asset-information/thumbnail");
        }

        public static Uri BuildUriForSubmodel(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"submodels/{smidenc}");
        }

        public static Uri BuildUriForCD(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"concept-descriptions/{smidenc}");
        }

        /// <summary>
        /// Note: this is an AASPE specific, proprietary extension.
        /// This REST ressource does not exist in the official specification!
        /// </summary>
        public static Uri BuildUriForQuery(Uri baseUri, string query)
        {
            // access
            if (query?.HasContent() != true)
                return null;

            // try combine
            var queryEnc = AdminShellUtil.Base64Encode(query);
            return CombineUri(baseUri, $"aaspe-query/{queryEnc}");
        }

        public static Uri BuildUriForSubmodel(Uri baseUri, Aas.IReference submodelRef)
        {
            // access
            if (baseUri == null || submodelRef?.IsValid() != true
                || submodelRef.Count() != 1 || submodelRef.Keys[0].Type != KeyTypes.Submodel)
                return null;

            // pass on
            return BuildUriForSubmodel(baseUri, submodelRef.Keys[0].Value);
        }

        public override async Task LoadFromSourceAsync(
            string fullItemLocation,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            //PackageHttpDownloadUtil.TryLoadFakeRequests(Assembly.GetExecutingAssembly(),
            //    "AasxPackageLogic.Resources.PackageContainerFakeAnswers.json");

            var allowFakeResponses = runtimeOptions?.AllowFakeResponses ?? false;

            var baseUri = GetBaseUri(fullItemLocation);

            // for the time being, make sure, we have the correct list implementations
            var prepAas = new OnDemandListIdentifiable<Aas.IAssetAdministrationShell>();
            var prepSM = new OnDemandListIdentifiable<Aas.ISubmodel>();
            var prepCD = new OnDemandListIdentifiable<Aas.IConceptDescription>();

            // integrate in a fresh environment
            // TODO: new kind of environment
            var env = (Aas.IEnvironment) new AasOnDemandEnvironment();

            // already set structure to use some convenience functions
            env.AssetAdministrationShells = prepAas;
            env.Submodels = prepSM;
            env.ConceptDescriptions = prepCD;

            // also the package "around"
            var dynPack = new AdminShellPackageDynamicFetchEnv(runtimeOptions, baseUri);

            // get the record data
            var record = (containerOptions as PackageContainerHttpRepoSubsetOptions)?.Record;

            // start with AAS?
            if (IsValidUriForSingleAAS(fullItemLocation))
            {
                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                    sourceUri: new Uri(fullItemLocation),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms, contentFn) =>
                    {
                        try
                        {
                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                            prepAas.Add(Jsonization.Deserialize.AssetAdministrationShellFrom(node), null);
                        } catch (Exception ex)
                        {
                            runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                        }
                    });
            }

            // start with Submodel?
            if (IsValidUriForSingleSubmodel(fullItemLocation))
            {
                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                    sourceUri: new Uri(fullItemLocation),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms, contentFn) =>
                    {
                        try
                        {
                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                            prepSM.Add(Jsonization.Deserialize.SubmodelFrom(node), null);
                        }
                        catch (Exception ex)
                        {
                            runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded Submodel");
                        }
                    });
            }

            // start with CD?
            if (IsValidUriForSingleCD(fullItemLocation))
            {
                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                    sourceUri: new Uri(fullItemLocation),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms, contentFn) =>
                    {
                        try
                        {
                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                            prepCD.Add(Jsonization.Deserialize.ConceptDescriptionFrom(node), null);
                        }
                        catch (Exception ex)
                        {
                            runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded ConceptDescription");
                        }
                    });
            }

            // start auto-load missing Submodels?
            if (record?.AutoLoadSubmodels ?? false)
                foreach (var lr in env.FindAllSubmodelReferences(onlyNotExisting: true))
                {
                    if (record?.AutoLoadOnDemand ?? true)
                    {
                        // side info level 1
                        prepSM.Add(null, new AasIdentifiableSideInfo() { 
                            Level = AasIdentifiableSideInfoLevel.IdOnly,
                            Id = lr.Reference.Keys[0].Value
                        });
                    }
                    else
                    {
                        // no side info => full element
                        await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        sourceUri: BuildUriForSubmodel(baseUri, lr.Reference),
                        allowFakeResponses: allowFakeResponses,
                        lambdaDownloadDone: (ms, contentFn) =>
                        {
                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                env.Add(Jsonization.Deserialize.SubmodelFrom(node));
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing auto-loaded Submodel");
                            }
                        });
                    }
                }

            // start auto-load missing thumbnails?
            if (true)
                foreach (var aas in env.AllAssetAdministrationShells())
                {
                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        sourceUri: BuildUriForAasThumbnail(baseUri, aas.Id),
                        allowFakeResponses: allowFakeResponses,
                        lambdaDownloadDone: (ms, contentFn) =>
                        {
                            try
                            {
                                dynPack.AddThumbnail(aas.Id, ms.ToArray());
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Managing auto-loaded tumbnail");
                            }
                        });
                }

            // remove, what is not need
            if (env.AssetAdministrationShellCount() < 1)
                env.AssetAdministrationShells = null;
            if (env.SubmodelCount() < 1)
                env.Submodels = null;
            if (env.ConceptDescriptionCount() < 1)
                env.ConceptDescriptions = null;

            // commit
            Env = dynPack;
            Env.SetEnvironment(env);
        }

        public override async Task<bool> SaveLocalCopyAsync(
            string targetFilename,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            return false;
        }

        private async Task UploadToServerAsync(string copyFn, Uri serverUri,
            PackCntRuntimeOptions runtimeOptions = null)
        {
        }

        public override async Task SaveToSourceAsync(string saveAsNewFileName = null,
            AdminShellPackageFileBasedEnv.SerializationFormat prefFmt = AdminShellPackageFileBasedEnv.SerializationFormat.None,
            PackCntRuntimeOptions runtimeOptions = null,
            bool doNotRememberLocation = false)
        {
            // access
            if (!(Env is AdminShellPackageDynamicFetchEnv dynPack)
                || dynPack.IsOpen != true)
            {
                runtimeOptions.Log?.Error("Cannot save environment as pre-conditions are not met.");
                return;
            }

            // refer to dynPack
            await dynPack.TrySaveAllTaintedIdentifiables();
        }

        //
        // UI
        //

        public class ConnectExtendedRecord
        {
            public string BaseAddress = "https://eis-data.aas-voyager.com/";

            public bool GetAllAas;

            public bool GetSingleAas = true;
            public string AasId = "https://new.abb.com/products/de/2CSF204101R1400/aas";

            public bool GetSingleSubmodel;
            public string SmId;

            public bool GetSingleCD;
            public string CdId;

            public bool ExecuteQuery;
            public string QueryScript;
            
            public bool AutoLoadSubmodels = true;
            public bool AutoLoadCds = true;
            public bool AutoLoadThumbnails = true;
            public bool AutoLoadOnDemand = true;
            public bool EncryptIds = true;
            public bool StayConnected;

            public void SetQueryChoices(int choice)
            {
                GetAllAas = (choice == 1);
                GetSingleAas = (choice == 2);
                GetSingleSubmodel = (choice == 3);
                GetSingleCD = (choice == 4);
                ExecuteQuery = (choice == 5);
            }
        }

        public class PackageContainerHttpRepoSubsetOptions : PackageContainerOptionsBase
        {
            public PackageContainerHttpRepoSubsetOptions(
                PackageContainerOptionsBase baseOpt,
                ConnectExtendedRecord record)
            {
                LoadResident = baseOpt.LoadResident;
                StayConnected = baseOpt.StayConnected;
                UpdatePeriod = baseOpt.UpdatePeriod;
                Record = record;
            }

            public ConnectExtendedRecord Record;
        }

        public static string BuildLocationFrom(ConnectExtendedRecord record)
        {
            // access
            if (record == null || record.BaseAddress?.HasContent() != true)
                return null;

            var baseUri = new Uri(record.BaseAddress);

            // All AAS?
            if (record.GetAllAas)
            {

            }

            // Single AAS?
            if (record.GetSingleAas)
            {
                var uri = BuildUriForAAS(baseUri, record.AasId, encryptIds: record.EncryptIds);
                return uri.ToString();
            }

            // Single Submodel?
            if (record.GetSingleSubmodel)
            {
                var uri = BuildUriForSubmodel(baseUri, record.SmId, encryptIds: record.EncryptIds);
                return uri.ToString();
            }

            // Single CD?
            if (record.GetSingleCD)
            {
                var uri = BuildUriForCD(baseUri, record.CdId, encryptIds: record.EncryptIds);
                return uri.ToString();
            }

            // Query?
            if (record.ExecuteQuery)
            {
                var uri = BuildUriForQuery(baseUri, record.QueryScript);
                return uri.ToString();
            }

            // nope
            return null;
        }

        public static async Task<bool> PerformConnectExtendedDialogue(
            AasxMenuActionTicket ticket,
            AnyUiContextBase displayContext,
            string caption,
            ConnectExtendedRecord record)
        {
            // access
            if (displayContext == null || caption?.HasContent() != true || record == null)
                return false ;

            // if a target file is given, a headless operation occurs
#if __later
            if (ticket != null && ticket["Target"] is string targetFn)
            {
                var exportFmt = -1;
                var targExt = System.IO.Path.GetExtension(targetFn).ToLower();
                if (targExt == ".txt")
                    exportFmt = 0;
                if (targExt == ".xlsx")
                    exportFmt = 1;
                if (exportFmt < 0)
                {
                    MainWindowLogic.LogErrorToTicketStatic(ticket, null,
                        $"For operation '{caption}', the target format could not be " +
                        $"determined by filename '{targetFn}'. Aborting.");
                    return;
                }

                try
                {
                    WriteTargetFile(exportFmt, targetFn);
                }
                catch (Exception ex)
                {
                    MainWindowLogic.LogErrorToTicketStatic(ticket, ex,
                        $"While performing '{caption}'");
                    return;
                }

                // ok
                Log.Singleton.Info("Performed '{0}' and writing report to '{1}'.",
                    caption, targetFn);
                return;
            }
#endif

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // reserve some states for the inner viewing routine
            bool wrap = false;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(caption);
            uc.ActivateRenderPanel(record,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                extraButtons: new[] { "A", "B" },
                renderPanel: (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 2, new[] { "120:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5),
                                margin: new AnyUiThickness(10, 0, 30, 0));

                    panel.Add(g);

                    // dynamic rows
                    int row = 0;

                    // Base address
                    helper.AddSmallLabelTo(g, row, 0, content: "Base address:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row, 1,
                                    text: $"{record.BaseAddress}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { record.BaseAddress = s; });
                    row++;

                    // All AASes
                    AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 0,
                                    content: "Get all AAS",
                                    isChecked: record.GetAllAas,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (o) => {
                                if ((bool) o)
                                    record.SetQueryChoices(1);
                                else
                                    record.GetAllAas = false;
                                return new AnyUiLambdaActionModalPanelReRender(uc);
                            });
                    row++;

                    // Single AAS
                    AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 0,
                                    content: "Get single AAS",
                                    isChecked: record.GetSingleAas,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(2);
                                else
                                    record.GetSingleAas = false;
                                return new AnyUiLambdaActionModalPanelReRender(uc);
                            });

                    helper.AddSmallLabelTo(g, row + 1, 0, content: "AAS.Id:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row + 1, 1,
                                    text: $"{record.AasId}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { record.AasId = s; });

                    row += 2;

                    // Single Submodel
                    AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 0,
                                    content: "Get single Submodel",
                                    isChecked: record.GetSingleSubmodel,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(3);
                                else
                                    record.GetSingleSubmodel = false;
                                return new AnyUiLambdaActionModalPanelReRender(uc);
                            });

                    helper.AddSmallLabelTo(g, row + 1, 0, content: "Submodel.Id:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row + 1, 1,
                                    text: $"{record.SmId}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { record.SmId = s; });

                    row += 2;

                    // Single CD
                    AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 0,
                                    content: "Get single ConceptDescription (CD)",
                                    isChecked: record.GetSingleCD,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(4);
                                else
                                    record.GetSingleCD = false;
                                return new AnyUiLambdaActionModalPanelReRender(uc);
                            });

                    helper.AddSmallLabelTo(g, row + 1, 0, content: "CD.Id:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row + 1, 1,
                                    text: $"{record.CdId}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { record.CdId = s; });

                    row += 2;

                    // Query
                    AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 0,
                                    content: "Get by query definition",
                                    isChecked: record.ExecuteQuery,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(5);
                                else
                                    record.ExecuteQuery = false;
                                return new AnyUiLambdaActionModalPanelReRender(uc);
                            });

                    helper.AddSmallLabelTo(g, row + 1, 0, content: "Query:",
                            verticalAlignment: AnyUiVerticalAlignment.Top,
                            verticalContentAlignment: AnyUiVerticalAlignment.Top);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row + 1, 1,
                                    text: $"{record.QueryScript}",
                                    verticalAlignment: AnyUiVerticalAlignment.Stretch,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Top,
                                    textWrap: AnyUiTextWrapping.Wrap,
                                    fontSize: 0.7,
                                    multiLine: true),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch,
                                minHeight: 120),
                            (s) => { record.QueryScript = s; });

                    row += 2;

                    // Auto load Submodels

                    helper.AddSmallLabelTo(g, row, 0, content: "For above:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Auto-load Submodels",
                                    isChecked: record.AutoLoadSubmodels,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.AutoLoadSubmodels = b; });

                    row++;

                    // Auto load Submodels

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Auto-load ConceptDescriptions",
                                    isChecked: record.AutoLoadCds,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.AutoLoadCds = b; });

                    row++;

                    // Auto load Thumbnails

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Auto-load thumbnail for every AAS",
                                    isChecked: record.AutoLoadThumbnails,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.AutoLoadThumbnails = b; });

                    row++;

                    // Auto load on demand

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Mark auto-loaded elements for on-demand loading",
                                    isChecked: record.AutoLoadOnDemand,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.AutoLoadOnDemand = b; });

                    row++;

                    // Encrypt IDs

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Encrypt Ids (needs to be checked, unless encrypted Ids are provided)",
                                    isChecked: record.EncryptIds,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.EncryptIds = b; });

                    row++;                    

                    // Stay connected

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Stay connected (will receive events)",
                                    isChecked: record.StayConnected,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.StayConnected = b; });

                    row++;

                    // give back
                    return g;
                });

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return false;

            // ok
            return true;
        }
    }

}
