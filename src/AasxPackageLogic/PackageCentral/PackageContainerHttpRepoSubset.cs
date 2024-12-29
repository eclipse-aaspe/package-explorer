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
using Microsoft.Win32;
using Namotion.Reflection;
using System.Text.Json.Nodes;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Lucene.Net.Util.Automaton;
using RestSharp;
using static Lucene.Net.Search.FieldCache;
using System.Reflection.Metadata;
using AngleSharp.Io;
using System.Globalization;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Context data to fetched packages to remember record, cursor and more
    /// </summary>
    public class PackageContainerHttpRepoSubsetFetchContext : AdminShellPackageDynamicFetchContextBase
    {
        public PackageContainerHttpRepoSubset.ConnectExtendedRecord Record;

        /// <summary>
        /// Cursor, as provided by the server.
        /// </summary>
        public string Cursor;

    }

    /// <summary>
    /// This container represents a subset of AAS elements retrieved from a HTTP / networked repository.
    /// </summary>
    [AasxIntegrationBase.DisplayName("HttpRepoSubset")]
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

        [JsonIgnore]
        public AdminShellPackageDynamicFetchEnv EnvDynPack { get => Env as AdminShellPackageDynamicFetchEnv; }

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
                if (!await res.LoadFromSourceAsync(fullItemLocation, containerOptions, runtimeOptions))
                    return null;

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

        //
        // REPO
        //

        public static bool IsValidUriForRepoAllAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shells(|/|/?\?(.*))$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            // prevent the false alarm for looking for single assetIds
            if (m.Success && location.Contains("assetId"))
                return false;

            // ok?
            return m.Success;
        }

        public static bool IsValidUriForRepoSingleAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shells/([^?]{1,99})", 
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForRepoAllSubmodel(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/submodels(|/|/?\?(.*))$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForRepoSingleSubmodel(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/submodels/(.{1,99})$", 
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            if (m.Success)
                return true;

            // TODO: Add AAS based Submodel
            return false;
        }

        public static bool IsValidUriForRepoAllCD(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/concept-descriptions(|/|/?\?(.*))$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForRepoSingleCD(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/concept-descriptions/(.{1,99})$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForRepoQuery(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/graphql(|/|/?\?(.*))$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        //
        // REGISTRY
        //

        public static bool IsValidUriForRegistryAllAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shell-descriptors(|/|/?\?(.*))$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForRegistrySingleAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shell-descriptors/([^?]{1,99})",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        public static bool IsValidUriForRepoRegistryAasByAssetId(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/lookup/shells/{0,1}\?(.*)assetId=([-A-Za-z0-9_]{1,99})",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return m.Success;
        }

        //
        // ALL
        //

        public static bool IsValidUriAnyMatch(string location)
        {
            if (location?.HasContent() != true)
                return false;

            return IsValidUriForRepoAllAAS(location)
                || IsValidUriForRepoSingleAAS(location)
                || IsValidUriForRepoAllSubmodel(location)
                || IsValidUriForRepoSingleSubmodel(location)
                || IsValidUriForRepoAllCD(location)
                || IsValidUriForRepoSingleCD(location)
                || IsValidUriForRepoQuery(location)
                || IsValidUriForRegistryAllAAS(location)
                || IsValidUriForRegistrySingleAAS(location)
                || IsValidUriForRepoRegistryAasByAssetId(location);
        }

        public static Uri GetBaseUri(string location)
        {
            // access
            if (location?.HasContent() != true)
                return null;

            // try an explicit search for known parts of ressources
            // (preserves scheme, host and leading pathes)
            var m = Regex.Match(location, @"^(.*?)(/shells|/submodel|/concept-description|/lookup)");
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

        public static Uri CombineUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || relativeUri?.HasContent() != true)
                return null;

            // for problems see:
            // https://stackoverflow.com/questions/372865/path-combine-for-urls
            //if (Uri.TryCreate(baseUri, relativeUri, out var res))
            //    return res;
            //return null;

            var bu = baseUri.ToString().TrimEnd('/');
            bu += "/" + relativeUri.TrimStart('/');

            return new Uri(bu);
        }

        //
        // build query string
        //

        public class LittleQueryString : List<string>
        {
            public LittleQueryString Add(string key, string value)
            {
                Add(key + "=" + value);
                return this;
            }

            public LittleQueryString IfAdd(bool condition, string key, string value)
            {
                if (condition)
                    Add(key, value);
                return this;
            }

            public string ToQueryString()
            {
                if (Count < 1)
                    return "";
                return "?" + string.Join('&', this);
            }
        }

        //
        // GENERAL
        //

        public static Uri BuildUriForDescription(Uri baseUri)
        {
            // try combine
            return CombineUri(baseUri, $"description");
        }

        //
        // REPO
        //

        public static Uri BuildUriForRepoAllAAS(Uri baseUri, int pageLimit = 100, string cursor = null)
        {
            // access
            if (baseUri == null)
                return null;

            // try combine
            // see: https://code-maze.com/how-to-create-a-url-query-string/
            // (not an simple & obvious approach even with Uri/ UriBuilder)
            var uri = new UriBuilder(CombineUri(baseUri, $"shells"));
            if (pageLimit > 0)
                uri.Query = $"Limit={pageLimit:D}";
            if (cursor != null)
                uri.Query += $"&Cursor={cursor}";

            // Note: cursor comes from the internet (server?) and is used unmodified, simply
            // trusted to be continous string and/ or BASE64 encoded. This is not checked, so
            // theoretically, a MITM attack could modify the cursor to modify this query!!

            return uri.Uri;
        }

        public static Uri BuildUriForRepoSingleAAS(
            Uri baseUri, string id, 
            bool encryptIds = true,
            bool usePost = false)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            if (!usePost)
            {
                var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
                return CombineUri(baseUri, $"shells/{smidenc}");
            }
            else
            {
                return CombineUri(baseUri, "shells");
            }
        }

        public static Uri BuildUriForRepoAasThumbnail(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"shells/{smidenc}/asset-information/thumbnail");
        }

        public static Uri BuildUriForRepoAllSubmodel(Uri baseUri, int pageLimit = 100, string cursor = null)
        {
            // for more info: see BuildUriForRepoAllAAS
            // access
            if (baseUri == null)
                return null;

            var uri = new UriBuilder(CombineUri(baseUri, $"submodels"));
            if (pageLimit > 0)
                uri.Query = $"Limit={pageLimit:D}";
            if (cursor != null)
                uri.Query += $"&Cursor={cursor}";

            return uri.Uri;
        }

        public static Uri BuildUriForRepoSingleSubmodel(
            Uri baseUri, string id, 
            bool encryptIds = true,
            bool usePost = false,
            bool addAasId = false,
            string aasId = null,
            bool levelDeep = true,
            bool extentWith = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // start query sttring
            var qs = new LittleQueryString()
                .IfAdd(levelDeep, "level", "deep")
                .IfAdd(extentWith, "extent", "withBlobValue");

            // query string for aasId?
            if (usePost && addAasId && aasId?.HasContent() == true)
            {
                var aasidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(aasId) : aasId;
                qs.Add("aasIdentifier", aasidenc);
            }

            // try combine
            if (!usePost)
            {
                var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
                return CombineUri(baseUri, $"submodels/{smidenc}" + qs.ToQueryString());
            }
            else
            {
                return CombineUri(baseUri, "submodels" + qs.ToQueryString());
            }
        }

        public static Uri BuildUriForRepoSingleSubmodelAttachment(
            Uri baseUri, string smId,
            string idShortPath,
            bool encryptIds = true,
            string aasId = null)
        {
            // access
            if (smId?.HasContent() != true || idShortPath?.HasContent() != true)
                return null;

            // smId
            var smIdEnc = encryptIds ? AdminShellUtil.Base64UrlEncode(smId) : smId;

            // aasId present?
            if (aasId?.HasContent() == true)
            {
                var aasIdEnc = encryptIds ? AdminShellUtil.Base64UrlEncode(aasId) : aasId;
                return CombineUri(baseUri, $"/shells/{aasIdEnc}/submodels/{smIdEnc}/submodel-elements/{idShortPath}/attachment");
            }
            else
            {
                return CombineUri(baseUri, $"/submodels/{smIdEnc}/submodel-elements/{idShortPath}/attachment");
            }
        }

        public static Uri BuildUriForRepoSingleSubmodel(
            Uri baseUri, Aas.IReference submodelRef,
            bool encryptIds = true,
            bool usePost = false)
        {
            // access
            if (baseUri == null || submodelRef?.IsValid() != true
                || submodelRef.Count() != 1 || submodelRef.Keys[0].Type != KeyTypes.Submodel)
                return null;

            // pass on
            return BuildUriForRepoSingleSubmodel(baseUri, submodelRef.Keys[0].Value,
                encryptIds: encryptIds, usePost: usePost);
        }

        public static Uri BuildUriForRepoAllCD(Uri baseUri, int pageLimit = 100, string cursor = null)
        {
            // for more info: see BuildUriForRepoAllAAS
            // access
            if (baseUri == null)
                return null;

            var uri = new UriBuilder(CombineUri(baseUri, $"concept-descriptions"));
            if (pageLimit > 0)
                uri.Query = $"Limit={pageLimit:D}";
            if (cursor != null)
                uri.Query += $"&Cursor={cursor}";

            return uri.Uri;
        }

        public static Uri BuildUriForRepoSingleCD(
            Uri baseUri, string id, 
            bool encryptIds = true,
            bool usePost = false)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            if (!usePost)
            {
                var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
                return CombineUri(baseUri, $"concept-descriptions/{smidenc}");
            }
            else
            {
                return CombineUri(baseUri, "concept-descriptions");
            }
        }

        /// <summary>
        /// Note: this is an AASPE specific, proprietary extension.
        /// This REST ressource does not exist in the official specification!
        /// </summary>
        public static Uri BuildUriForRepoQuery(Uri baseUri, string query)
        {
            // access
            if (query?.HasContent() != true)
                return null;

            // For the time being, only POST is possible, therefore only
            // endpoint name is required for the real call. 
            // However, lets store the query as BASE64 query parameter
            var queryEnc = AdminShellUtil.Base64UrlEncode(query);            
            var uri = new UriBuilder(CombineUri(baseUri, $"graphql"));
            uri.Query = $"query={queryEnc}";
            return uri.Uri;
        }

        //
        // REGISTRY
        //

        public static Uri BuildUriForRegistryAllAAS(Uri baseUri, int pageLimit = 100, string cursor = null)
        {
            // for more info: see BuildUriForRepoAllAAS
            // access
            if (baseUri == null)
                return null;

            var uri = new UriBuilder(CombineUri(baseUri, $"shell-descriptors"));
            if (pageLimit > 0)
                uri.Query = $"Limit={pageLimit:D}";
            if (cursor != null)
                uri.Query += $"&Cursor={cursor}";

            return uri.Uri;
        }

        public static Uri BuildUriForRegistrySingleAAS(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"shell-descriptors/{smidenc}");
        }

        public static Uri BuildUriForRegistryAasByAssetLink(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var assenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"lookup/shells?assetId={assenc}");
        }

        //
        // translate PROFILES
        //

        public class ProfileDescription
        {
            public string Id;
            public string Name;
            public string Abbreviation;

            public ProfileDescription() { }

            public ProfileDescription(string id, string name, string abbreviation)
            {
                Id = id;
                Name = name;
                Abbreviation = abbreviation;
            }
        }

        public static ProfileDescription[] ProfileDescriptions =
        {
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-001", "AAS Full Profile ", "All/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AssetAdministrationShellServiceSpecification/SSP-002", "AAS Read Profile", "All/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001", "Submodel Full Profile", "SM/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-002", "Submodel Read Profile", "SM/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-003", "Submodel Value Profile", "SM/Value"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AasxFileServerServiceSpecification/SSP-001", "AASX File Server Full Profile", "AASX/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-001 ", "Asset Administration Shell Registry Full Profile", "AAS-REG/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRegistryServiceSpecification/SSP-002", "AAS Registry Read Profile", "AAS-REG/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-001 ", "Submodel Registry Full Profile", "SM-REG/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelRegistryServiceSpecification/SSP-002 ", "Submodel Registry Read Profile", "SM-REG/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/DiscoveryServiceSpecification/SSP-001", "Discovery Service Full Profile", "DISC/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-001", "AAS Repository Full Profile", "AAS-REPO/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/AssetAdministrationShellRepositoryServiceSpecification/SSP-002", "AAS Repository Read Profile", "AAS-REPO/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001", "Submodel Repository Full Profile", "SM-REPO/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-001", "Submodel Repository Read Profile", "SM-REPO/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-003", "Submodel Repository Template Profile", "SMT-REPO/Full"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/SubmodelServiceSpecification/SSP-004", "Submodel Repository Template Read Profile", "SMT-REPO/Read"),
            new ProfileDescription("https://admin-shell.io/aas/API/3/0/ConceptDescriptionRepositoryServiceSpecification/SSP-001", "Concept Description Repository Full Profile", "CD-REPO/Full")
        };

        public static ProfileDescription FindProfileDescription(string input)
        {
            ProfileDescription pdFound = null;
            foreach (var pd in ProfileDescriptions)
                if (pd.Id.Equals(input))
                    return pd;
            return null;
        }        

        //
        // ALL
        //

        protected enum FetchItemType { SmUrl, SmId }
        protected class FetchItem
        {
            public FetchItemType Type;
            public string Value;
        }

        /// see: https://stackoverflow.com/questions/9956648/how-do-i-check-if-a-property-exists-on-a-dynamic-anonymous-type-in-c
        /// see: https://stackoverflow.com/questions/63972270/newtonsoft-json-check-if-property-and-its-value-exists
        public static bool HasProperty(dynamic obj, string name)
        {
            Type objType = obj.GetType();

            if (obj is Newtonsoft.Json.Linq.JObject jo)
            {
                return jo.ContainsKey(name);
            }

            if (objType == typeof(ExpandoObject))
            {
                return ((IDictionary<string, object>)obj).ContainsKey(name);
            }

            return objType.GetProperty(name) != null;
        }
       
        private static async Task<bool> FromRegistryGetAasAndSubmodels(            
            OnDemandListIdentifiable<IAssetAdministrationShell> prepAas, 
            OnDemandListIdentifiable<ISubmodel> prepSM,
            ConnectExtendedRecord record,
            PackCntRuntimeOptions runtimeOptions,
            bool allowFakeResponses,            
            dynamic aasDescriptor,
            List<Aas.IIdentifiable> trackNewIdentifiables = null,
            List<Aas.IIdentifiable> trackLoadedIdentifiables = null,
            Action<int, int> lambdaReportProgress = null)
        {
            // access
            if (record == null)
                return false;

            foreach (var ep in aasDescriptor.endpoints)
            {
                // strictly check IFC
                var aasIfc = "" + ep["interface"];
                if (aasIfc != "AAS-1.0")
                    continue;

                // direct access HREF
                // var aasUri = new Uri("" + ep.protocolInformation.href);
                var aasSi = new AasIdentifiableSideInfo()
                {
                    IsStub = false,
                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                    Id = "" + aasDescriptor.id,
                    IdShort = "" + aasDescriptor.idShort,
                    Endpoint = new Uri("" + ep.protocolInformation.href)
                };

                // but in order to operate as registry, a list if Submodel endpoints
                // is required as well
                var smRegged = new List<AasIdentifiableSideInfo>();
                if (HasProperty(aasDescriptor, "submodelDescriptors"))
                    foreach (var smdesc in aasDescriptor.submodelDescriptors)
                    {
                        foreach (var smep in smdesc.endpoints)
                        {
                            // strictly check IFC
                            var smIfc = "" + smep["interface"];
                            if (smIfc != "SUBMODEL-1.0")
                                continue;

                            // ok
                            string href = smep.protocolInformation.href;
                            if (href.HasContent() == true)
                                smRegged.Add(new AasIdentifiableSideInfo()
                                {
                                    IsStub = true,
                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                    Id = smdesc.id,
                                    IdShort = smdesc.idShort,
                                    Endpoint = new Uri(href)
                                });
                        }
                    }

                // ok
                var aas = await PackageHttpDownloadUtil.DownloadIdentifiableToOK<Aas.IAssetAdministrationShell>(
                    aasSi.Endpoint, runtimeOptions, allowFakeResponses);
                if (aas == null)
                {
                    runtimeOptions?.Log?.Error(
                        "Unable to download AAS via registry. Skipping! Location: {0}",
                        aasSi.Endpoint.ToString());
                    continue;
                }

                lambdaReportProgress?.Invoke(1, 0);

                // possible culprit: Submodels are listed twice (or more) in an AAS
                // try filter (actually: false alarm, but leave in)
                var uniqSms = aas.Submodels?.Distinct(
                    new AdminShellComparers.PredicateEqualityComparer<Aas.IReference>(
                        (x, y) => x?.Matches(y, MatchMode.Relaxed) == true))
                    ?? new List<Aas.Reference>();

                // make sure the list of Submodel endpoints is the same (in numbers)
                // as the AAS expects
                if (smRegged.Count != uniqSms.Count())
                {
                    Log.Singleton.Info(StoredPrint.Color.Blue,
                        "For downloading AAS at {0}, the number of Submodels " +
                        "was different to the number of given Submodel endpoints.",
                        aasSi.Endpoint.ToString());

                    // cycle to next endpoint or next descriptor (more likely)
                    // continue;
                }

                // makes most sense to "recrate" the AAS.Submodels with the side infos
                // from the registry
                aas.Submodels = null;
                foreach (var smrr in smRegged)
                    aas.AddSubmodelReference(new Aas.Reference(
                        ReferenceTypes.ModelReference,
                        (new Aas.IKey[] { new Aas.Key(KeyTypes.Submodel, smrr.Id) }).ToList()));

                // add this AAS
                
                trackLoadedIdentifiables?.Add(aas);
                if (prepAas?.AddIfNew(aas, aasSi) == true)
                    trackNewIdentifiables?.Add(aas);

                // check if to add the Submodels
                if (!record.AutoLoadOnDemand)
                {
                    // be prepared to download them
                    var numRes = await PackageHttpDownloadUtil.DownloadListOfIdentifiables<Aas.ISubmodel, AasIdentifiableSideInfo>(
                        null,
                        smRegged,
                        lambdaGetLocation: (si) => si.Endpoint,
                        runtimeOptions: runtimeOptions,
                        allowFakeResponses: allowFakeResponses,
                        lambdaDownloadDoneOrFail: (code, sm, contentFn, si) =>
                        {
                            // error ?
                            if (code != HttpStatusCode.OK)
                            {
                                Log.Singleton.Error(
                                    "Could not download Submodel from endpoint given by registry: {0}",
                                    si.Endpoint.ToString());

                                // add as pure side info
                                si.IsStub = true;
                                prepSM?.AddIfNew(null, si);
                            }

                            // no, add with data
                            si.IsStub = false;
                            trackLoadedIdentifiables?.Add(sm);
                            if (prepSM?.AddIfNew(sm, si) == true)
                                trackNewIdentifiables?.Add(sm);
                            lambdaReportProgress?.Invoke(0, 1);
                        });
                }
                else
                {
                    foreach (var si in smRegged)
                    {
                        // valid Id is required
                        if (si?.Id?.HasContent() != true)
                            continue;

                        // for the Submodels add Identifiables with null content, but side infos
                        var siEx = prepSM.FindSideInfoFromId(si.Id);
                        if (siEx != null)
                            // already existing!
                            continue;

                        // need to do
                        si.IsStub = true;
                        prepSM?.AddIfNew(null, si);
                    }
                }

                // a little debug
                runtimeOptions?.Log?.Info(StoredPrint.Color.Blue,
                    "Retrieved AAS (potentially with Submodels) from: {0}",
                    aasSi.Endpoint.ToString());
            }

            return true;
        }

        public override async Task<bool> LoadFromSourceAsync(
            string fullItemLocation,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.StartOverall, message: "Start repo load");

            var newEnv = await LoadFromSourceInternalAsync(
                fullItemLocation,
                Env,
                containerOptions: containerOptions, runtimeOptions: runtimeOptions);

            if (newEnv == null)
            {
                runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.EndOverall, message: "Stopped repo load");
                return false;
            }

            // okay
            Env = newEnv;
            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.EndOverall, message: "Done repo load");

            return true;
        }

        public static async Task<AdminShellPackageEnvBase> LoadFromSourceToTargetAsync(
            string fullItemLocation,
            AdminShellPackageEnvBase targetEnv = null,
            bool loadNew = true,
            List<Aas.IIdentifiable> trackNewIdentifiables = null,
            List<Aas.IIdentifiable> trackLoadedIdentifiables = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.StartOverall, message: "Start repo load");

            var res = await LoadFromSourceInternalAsync(
                fullItemLocation: fullItemLocation,
                targetEnv: targetEnv,
                loadNew: loadNew,
                trackNewIdentifiables: trackNewIdentifiables,
                trackLoadedIdentifiables: trackLoadedIdentifiables,
                containerOptions: containerOptions, runtimeOptions: runtimeOptions);

            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.EndOverall, message: "Done repo load");

            return res;
        }

        protected static async Task<AdminShellPackageEnvBase> LoadFromSourceInternalAsync(
            string fullItemLocation,
            AdminShellPackageEnvBase targetEnv = null,
            bool loadNew = true,
            List<Aas.IIdentifiable> trackNewIdentifiables = null,
            List<Aas.IIdentifiable> trackLoadedIdentifiables = null,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            // lambda for overall progress
            int numAAS = 0;
            int numSM = 0;
            int numCD = 0;
            int numDiv = 0;
            
            Action<int, int, int, int> lambdaReportAll = (nAas, nSm, nCd, nDiv) =>
            {
                runtimeOptions?.ProgressChanged(PackCntRuntimeOptions.Progress.OverallMessage,
                    message: $"{nAas} / {nSm} / {nCd} / {nDiv}");
            };

            Action<int, int> lambdaReportAasSm = (iAAS, iSM) =>
            {
                numAAS += iAAS;
                numSM += iSM;
                lambdaReportAll(numAAS, numSM, numCD, numDiv);
            };

            runtimeOptions?.Log?.Info("Note: overall progress format is #AAS / #Submodel / #CD / #else");

            // start
            var allowFakeResponses = runtimeOptions?.AllowFakeResponses ?? false;
            PackageContainerListBase containerList = null;
            var baseUri = GetBaseUri(fullItemLocation);

            // use existing ?
            var env = (targetEnv as AdminShellPackageEnvBase)?.AasEnv;
            var dynPack = targetEnv as AdminShellPackageDynamicFetchEnv;

            var prepAas = env?.AssetAdministrationShells as OnDemandListIdentifiable<Aas.IAssetAdministrationShell>
                          ?? new OnDemandListIdentifiable<IAssetAdministrationShell>();
            var prepSM = env?.Submodels as OnDemandListIdentifiable<Aas.ISubmodel>
                         ?? new OnDemandListIdentifiable<ISubmodel>();
            var prepCD = env?.ConceptDescriptions as OnDemandListIdentifiable<Aas.IConceptDescription>
                         ?? new OnDemandListIdentifiable<IConceptDescription>();

            if (!loadNew && (env == null || dynPack == null))
                return null;

            // new
            if (loadNew)
            {
                // for the time being, make sure, we have the correct list implementations
                prepAas = new OnDemandListIdentifiable<Aas.IAssetAdministrationShell>();
                prepSM = new OnDemandListIdentifiable<Aas.ISubmodel>();
                prepCD = new OnDemandListIdentifiable<Aas.IConceptDescription>();

                // integrate in a fresh environment
                // TODO: new kind of environment
                env = (Aas.IEnvironment) new AasOnDemandEnvironment();

                // already set structure to use some convenience functions
                env.AssetAdministrationShells = prepAas;
                env.Submodels = prepSM;
                env.ConceptDescriptions = prepCD;

                // also the package "around"
                dynPack = new AdminShellPackageDynamicFetchEnv(runtimeOptions, baseUri);
            }

            // get the record data (as supplemental infos to the fullItemLocation)
            var record = (containerOptions as PackageContainerHttpRepoSubsetOptions)?.Record;

            // invalidate cursor data (as a new request is about to be started)
            string cursor = null;

            // TODO: very long function, needs to be refactored
            var operationFound = false;

            //
            // in REPO & REGISTRY
            //

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository
                || record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Registry)
            {
                // Asset Link
                if (IsValidUriForRepoRegistryAasByAssetId(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    // prepare receiving the descriptors/ ids
                    var resObj = await PackageHttpDownloadUtil.DownloadEntityToDynamicObject(
                        new Uri(fullItemLocation), runtimeOptions, allowFakeResponses);

                    lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                    // Note: GetAllAssetAdministrationShellIdsByAssetLink only returns a list of ids
                    if (resObj == null)
                    {
                        runtimeOptions?.Log?.Error("Repository/ Registry did not return any AAS descriptors! Aborting.");
                        return null;
                    }

                    // Have  a list of ids. Decompose into single id.
                    // Note: Parallel makes no sense, ideally only 1 result (is per AssetId)!!
                    var noRes = true;
                    foreach (var res in resObj)
                    {
                        noRes = false;

                        // in res, have only an id. Get the descriptor / the AAS itself
                        var id = "" + res;

                        if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Registry)
                        {
                            var singleDesc = await PackageHttpDownloadUtil.DownloadEntityToDynamicObject(
                                    BuildUriForRegistrySingleAAS(baseUri, id, encryptIds: true),
                                    runtimeOptions, allowFakeResponses);
                            if (singleDesc == null || !HasProperty(singleDesc, "endpoints"))
                                continue;

                            lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                            // refer to dedicated function
                            await FromRegistryGetAasAndSubmodels(
                                prepAas, prepSM, record, runtimeOptions, allowFakeResponses, singleDesc,
                                trackNewIdentifiables, trackLoadedIdentifiables,
                                lambdaReportProgress: lambdaReportAasSm);
                        }

                        if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
                        {
                            // get the AAS (new download approach)
                            var aas = await PackageHttpDownloadUtil.DownloadIdentifiableToOK<Aas.IAssetAdministrationShell>(
                                BuildUriForRepoSingleAAS(baseUri, id, encryptIds: true), 
                                runtimeOptions, allowFakeResponses);

                            // found?
                            if (aas != null)
                            {
                                // add
                                lambdaReportAll(++numAAS, numSM, numCD, numDiv);
                                trackLoadedIdentifiables?.Add(aas);
                                if (prepAas.AddIfNew(aas, new AasIdentifiableSideInfo()
                                {
                                    IsStub = false,
                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                    Id = aas.Id,
                                    IdShort = aas.IdShort,
                                    Endpoint = new Uri(fullItemLocation)
                                }))
                                {
                                    trackNewIdentifiables?.Add(aas);
                                }
                            }
                        }
                    }

                    // check again (count)
                    if (noRes)
                    {
                        runtimeOptions?.Log?.Error("Repository/ Registry did not return any AAS descriptors! Aborting.");
                        return null;
                    }
                }
            }

            //
            // REGISTRY
            //

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Registry)
            {
                // All AAS descriptors?
                if (!operationFound && IsValidUriForRegistryAllAAS(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    // prepare receiving the descriptors
                    var resObj = await PackageHttpDownloadUtil.DownloadEntityToDynamicObject(
                        new Uri(fullItemLocation), runtimeOptions, allowFakeResponses);

                    // have directly a list of descriptors?!
                    if (resObj?.result == null)
                    {
                        runtimeOptions?.Log?.Error("Registry did not return any AAS descriptors! Aborting.");
                        return null;
                    }

                    lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                    foreach (var res in resObj.result)
                    {
                        // refer to dedicated function
                        await FromRegistryGetAasAndSubmodels(
                            prepAas, prepSM, record, runtimeOptions, allowFakeResponses, res,
                            trackNewIdentifiables, trackLoadedIdentifiables,
                            lambdaReportProgress: lambdaReportAasSm);
                    }
                }

                // start with single AAS?
                if (!operationFound && IsValidUriForRegistrySingleAAS(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    // prepare receing the descriptors
                    dynamic aasDesc = null;

                    // GET
                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        null,
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            if (code != HttpStatusCode.OK)
                                return;

                            try
                            {
                                // try working with dynamic objects
                                using (StreamReader reader = new StreamReader(ms, System.Text.Encoding.UTF8, true))
                                using (var jsonTextReader = new JsonTextReader(reader))
                                {
                                    JsonSerializer serializer = new JsonSerializer();
                                    aasDesc = serializer.Deserialize(jsonTextReader);
                                }
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                            }
                        });

                    // have a descriptors with some members
                    if (aasDesc?.id == null)
                    {
                        runtimeOptions?.Log?.Error("Registry did not return a valid AAS descriptor! Aborting.");
                        return null;
                    }
                    lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                    // refer to dedicated function
                    var res = await FromRegistryGetAasAndSubmodels(
                                prepAas, prepSM, record, runtimeOptions, allowFakeResponses, aasDesc,
                                trackNewIdentifiables, trackLoadedIdentifiables,
                                lambdaReportProgress: lambdaReportAasSm);
                    if (!res)
                    {
                        runtimeOptions?.Log?.Error("Error retrieving AAS from registry! Aborting.");
                        return null;
                    }
                }
            }

            //
            // REPO
            //

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
            {
                // for all repo access, use the same client
                var client = PackageHttpDownloadUtil.CreateHttpClient(baseUri, runtimeOptions, containerList);

                // start with a list of AAS or Submodels (very similar, therefore unified)
                var isAllAAS = IsValidUriForRepoAllAAS(fullItemLocation);
                var isAllSM = IsValidUriForRepoAllSubmodel(fullItemLocation);
                var isAllCD = IsValidUriForRepoAllCD(fullItemLocation);
                var receivedAllAAS = 0;
                if (!operationFound && (isAllAAS || isAllSM || isAllCD))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        client,
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            if (code != HttpStatusCode.OK)
                                return;

                            lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                if (node["result"] is JsonArray resChilds
                                    && resChilds.Count > 0)
                                {
                                    int childsToSkip = Math.Max(0, record.PageSkip);
                                    int childsRead = 0;
                                    bool firstNonSkipped = true;

                                    foreach (var n2 in resChilds)
                                        // second try to reduce side effects
                                        try
                                        {
                                            // skip
                                            if (childsToSkip > 0)
                                            {
                                                childsToSkip--;
                                                continue;
                                            }                                            

                                            // get identifiable
                                            Aas.IIdentifiable idf = null;
                                            if (isAllAAS)
                                                idf = Jsonization.Deserialize.AssetAdministrationShellFrom(n2);
                                            if (isAllSM)
                                                idf = Jsonization.Deserialize.SubmodelFrom(n2);
                                            if (isAllCD)
                                                idf = Jsonization.Deserialize.ConceptDescriptionFrom(n2);
                                            if (idf == null)
                                                continue;

                                            // on last child, attach side info for fetch prev/ next cursor
                                            AasIdentifiableSideInfo si = new AasIdentifiableSideInfo()
                                            {
                                                IsStub = false,
                                                StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                                Id = idf.Id,
                                                IdShort = idf.IdShort,
                                                Endpoint = new Uri(fullItemLocation)
                                            };
                                            if (firstNonSkipped && record.PageOffset > 0)
                                                si.ShowCursorAbove = true;

                                            if (n2 == resChilds.Last() && record.PageLimit > 0)
                                                si.ShowCursorBelow = true;
                                            
                                            firstNonSkipped = false;

                                            // add
                                            var added = false;
                                            if (isAllAAS)
                                            {
                                                lambdaReportAll(++numAAS, numSM, numCD, numDiv);
                                                added = prepAas.AddIfNew(
                                                    idf as Aas.IAssetAdministrationShell,
                                                    si);
                                            }
                                            if (isAllSM)
                                            {
                                                lambdaReportAll(numAAS, ++numSM, numCD, numDiv);
                                                added = prepSM.AddIfNew(
                                                    idf as Aas.ISubmodel,
                                                    si);
                                            }
                                            if (isAllCD)
                                            {
                                                lambdaReportAll(numAAS, numSM, ++numCD, numDiv);
                                                added = prepCD.AddIfNew(
                                                    idf as Aas.IConceptDescription,
                                                    si);
                                            }
                                            receivedAllAAS++;
                                            trackLoadedIdentifiables?.Add(idf);
                                            if (added)
                                                trackNewIdentifiables?.Add(idf);

                                            // maintain page limit (may be server does not care..)
                                            childsRead++;
                                            if (record.PageLimit > 0 && childsRead >= record.PageLimit)
                                            {
                                                si.ShowCursorBelow = true;
                                                break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            runtimeOptions?.Log?.Error(ex, "Parsing single AAS/ Submodel/ CD of list of all AAS/ Submodel/ CD");
                                        }
                                }

                                // cursor data
                                if (node["paging_metadata"] is JsonNode nodePaging
                                    && nodePaging["cursor"] is JsonNode nodeCursor)
                                {
                                    cursor = nodeCursor.ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing list of all AAS / SM / CD");
                            }
                        });

                }

                // "heal" the GetAllAAS operation from above?
                // Means, modify query to lookup
                if (isAllAAS && (receivedAllAAS < 1)
                    && fullItemLocation.Contains("/shells"))
                {
                    // modify query string to provide a list of aas
                    var filNew = fullItemLocation.Replace("/shells", "/lookup/shells");

                    // prepare receiving the descriptors/ ids
                    var resObj = await PackageHttpDownloadUtil.DownloadEntityToDynamicObject(
                        new Uri(filNew), runtimeOptions, allowFakeResponses);

                    if (resObj == null)
                    {
                        runtimeOptions?.Log?.Error("Repository/ Registry did not return any AAS descriptors! Aborting.");
                        return null;
                    }

                    lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                    // Have  a list of ids. Decompose into single id.
                    // Note: Parallel makes no sense, ideally only 1 result (is per AssetId)!!
                    // TODO: not parallel!
                    var noRes = true;
                    foreach (var res in resObj)
                    {
                        noRes = false;

                        // in res, have only an id. Get the AAS itself
                        var aas = await PackageHttpDownloadUtil.DownloadIdentifiableToOK<Aas.IAssetAdministrationShell>(
                            BuildUriForRepoSingleAAS(baseUri, "" + res, encryptIds: true),
                            runtimeOptions, allowFakeResponses);

                        // found?
                        if (aas != null)
                        {
                            // add
                            lambdaReportAll(++numAAS, numSM, numCD, numDiv);
                            trackLoadedIdentifiables?.Add(aas);
                            if (prepAas.AddIfNew(aas, new AasIdentifiableSideInfo()
                            {
                                IsStub = false,
                                StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                Id = aas.Id,
                                IdShort = aas.IdShort,
                                Endpoint = new Uri(fullItemLocation)
                            }))
                            {
                                trackNewIdentifiables?.Add(aas);
                            }
                        }
                    }

                    // check again (count)
                    if (noRes)
                    {
                        runtimeOptions?.Log?.Error("Repository/ Registry did not return any AAS descriptors! Aborting.");
                        return null;
                    }
                }

                // start with single AAS?
                if (!operationFound && IsValidUriForRepoSingleAAS(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        client,
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            if (code != HttpStatusCode.OK)
                                return;

                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                var aas = Jsonization.Deserialize.AssetAdministrationShellFrom(node);
                                lambdaReportAll(++numAAS, numSM, numCD, numDiv);
                                trackLoadedIdentifiables?.Add(aas);
                                if (prepAas.AddIfNew(aas, new AasIdentifiableSideInfo()
                                {
                                    IsStub = false,
                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                    Id = aas.Id,
                                    IdShort = aas.IdShort,
                                    Endpoint = new Uri(fullItemLocation)
                                }))
                                {
                                    trackNewIdentifiables?.Add(aas);
                                }
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                            }
                        });
                }

                // start with Submodel?
                if (!operationFound && IsValidUriForRepoSingleSubmodel(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        client,
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            if (code != HttpStatusCode.OK)
                                return;

                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                var sm = Jsonization.Deserialize.SubmodelFrom(node);
                                lambdaReportAll(numAAS, ++numSM, numCD, numDiv);
                                trackLoadedIdentifiables?.Add(sm);
                                if (prepSM.AddIfNew(sm, new AasIdentifiableSideInfo()
                                {
                                    IsStub = false,
                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                    Id = sm.Id,
                                    IdShort = sm.IdShort,
                                    Endpoint = new Uri(fullItemLocation)
                                }))
                                {
                                    trackNewIdentifiables?.Add(sm);
                                }
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded Submodel");
                            }
                        });
                }

                // start with CD?
                if (!operationFound && IsValidUriForRepoSingleCD(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        client,
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            if (code != HttpStatusCode.OK)
                                return;

                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                var cd = Jsonization.Deserialize.ConceptDescriptionFrom(node);
                                lambdaReportAll(numAAS, numSM, ++numCD, numDiv);
                                trackLoadedIdentifiables?.Add(cd);
                                if (prepCD.AddIfNew(cd, new AasIdentifiableSideInfo()
                                {
                                    IsStub = false,
                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                    Id = cd.Id,
                                    IdShort = cd.IdShort,
                                    Endpoint = new Uri(fullItemLocation)
                                }))
                                {
                                    trackNewIdentifiables?.Add(cd);
                                }
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded ConceptDescription");
                            }
                        });
                }

                // start with a query?
                if (!operationFound && IsValidUriForRepoQuery(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    // try extract query from the location
                    var query = "";
                    var quri = new Uri(fullItemLocation);
                    if (quri.Query?.HasContent() == true)
                    {
                        var pc = HttpUtility.ParseQueryString(quri.Query);
                        foreach (var key in pc.AllKeys)
                            if (key == "query")
                                query = AdminShellUtil.Base64UrlDecode(pc[key]);
                    }

                    // if not, try to get from record
                    if (!query.HasContent() && record?.QueryScript?.HasContent() == true)
                        query = record.QueryScript;

                    // error
                    if (!query.HasContent())
                    {
                        runtimeOptions?.Log?.Error("Could not determine valid query script. Aborting!");
                        return null;
                    }

                    // but, the query needs to be reformatted as JSON
                    // query = "{ searchSMs(expression: \"\"\"$LOG  \"\"\") { url smId } }";
                    // query = "{ searchSMs(expression: \"\"\"$LOG filter=or(str_contains(sm.IdShort, \"Technical\"), str_contains(sm.IdShort, \"Nameplate\")) \"\"\") { url smId } }";
                    query = query.Replace("\\", "\\\\");
                    query = query.Replace("\"", "\\\"");
                    query = query.Replace("\r", " ");
                    query = query.Replace("\n", " ");
                    var jsonQuery = $"{{ \"query\" : \"{query}\" }} ";

                    // there are subsequent fetch operations necessary
                    var fetchItems = new List<FetchItem>();

                    // HTTP POST
                    var statCode = await PackageHttpDownloadUtil.HttpPostRequestToMemoryStream(
                        client,
                        sourceUri: new Uri(quri.GetLeftPart(UriPartial.Path)),
                        requestBody: jsonQuery,
                        requestContentType: "application/json",
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDone: (ms, contentFn) =>
                        {
                            try
                            {
                                lambdaReportAll(numAAS, numSM, numCD, ++numDiv);

                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                if (node["data"]?["searchSMs"] is JsonArray smdata
                                    && smdata.Count >= 1)
                                {
                                    foreach (var smrec in smdata)
                                    {
                                        var url = smrec["url"]?.ToString();
                                        var smId = smrec["smId"]?.ToString();
                                        if (smId?.HasContent() == true)
                                            fetchItems.Add(new FetchItem() { Type = FetchItemType.SmId, Value = smId });
                                        else
                                        if (url?.HasContent() == true)
                                            fetchItems.Add(new FetchItem() { Type = FetchItemType.SmUrl, Value = url });
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing graphql result set");
                            }
                        });

                    if (statCode != HttpStatusCode.OK)
                    {
                        Log.Singleton.Error("Could not fetch new dynamic elements by graphql. Aborting!");
                        Log.Singleton.Error("  POST request was: {0}", jsonQuery);
                        return null;
                    }

                    // only makes sense, if query returns something
                    if (fetchItems.Count < 1)
                    {
                        Log.Singleton.Info(StoredPrint.Color.Blue, "Query resulted in zero elements, " +
                            "which could be fetched. Aborting!");
                        return null;
                    }

                    // skip items?
                    if (record.PageSkip > 0)
                    {
                        fetchItems.RemoveRange(0, Math.Min(fetchItems.Count, record.PageSkip));
                    }
                    var numItem = 0;

                    // TODO: convert to parallel for each async
                    var dlErrors = 0;
                    foreach (var fi in fetchItems)
                    {
                        // reached end
                        numItem++;
                        if (record.PageLimit > 0 && numItem > record.PageLimit)
                            break;

                        // prepare download
                        Uri loc = null;
                        if (fi.Type == FetchItemType.SmUrl)
                            loc = new Uri(fi.Value);
                        if (fi.Type == FetchItemType.SmId)
                            loc = BuildUriForRepoSingleSubmodel(baseUri, fi.Value, encryptIds: true);

                        if (loc == null)
                            continue;

                        // download (and skip errors)
                        try
                        {
                            await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                                null,
                                sourceUri: loc,
                                allowFakeResponses: allowFakeResponses,
                                runtimeOptions: runtimeOptions,
                                lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                                {
                                    if (code != HttpStatusCode.OK)
                                        return;

                                    try
                                    {
                                        var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                        var sm = Jsonization.Deserialize.SubmodelFrom(node);
                                        lambdaReportAll(numAAS, ++numSM, numCD, numDiv);
                                        if (fi.Type == FetchItemType.SmUrl || fi.Type == FetchItemType.SmId)
                                        {
                                            trackLoadedIdentifiables?.Add(sm);
                                            if (prepSM.AddIfNew(sm, new AasIdentifiableSideInfo()
                                            {
                                                IsStub = false,
                                                StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                                Id = sm.Id,
                                                IdShort = sm.IdShort,
                                                Endpoint = new Uri(fullItemLocation)
                                            }))
                                            {
                                                trackNewIdentifiables?.Add(sm);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        dlErrors++;
                                        runtimeOptions?.Log?.Error(ex, "Parsing individual fetch element of query.");
                                    }
                                });
                        }
                        catch (Exception ex)
                        {
                            dlErrors++;
                            LogInternally.That.CompletelyIgnoredError(ex);
                        }
                    }

                    Log.Singleton.Info(StoredPrint.Color.Blue, "Executed GraphQL query. Receiving list of {0} elements, " +
                        "found {1} errors when individually downloading elements.", fetchItems.Count, dlErrors);
                }

                // start auto-load missing Submodels?
                if (operationFound && (record?.AutoLoadSubmodels ?? false))
                {
                    var lrs = env.FindAllSubmodelReferences(onlyNotExisting: true).ToList();

                    await Parallel.ForEachAsync(lrs,
                        new ParallelOptions() { 
                            MaxDegreeOfParallelism = record?.ParallelReads ?? Options.Curr.MaxParallelOps,
                            CancellationToken = runtimeOptions?.CancellationTokenSource?.Token ?? CancellationToken.None
                        },
                        async (lr, token) =>
                        {
                            if (record?.AutoLoadOnDemand ?? true)
                            {
                                // side info level 1
                                lock (prepSM)
                                {
                                    prepSM.AddIfNew(null, new AasIdentifiableSideInfo()
                                    {
                                        IsStub = true,
                                        StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                        Id = lr.Reference.Keys[0].Value,
                                        IdShort = "",
                                        Endpoint = BuildUriForRepoSingleSubmodel(baseUri, lr.Reference)
                                    });
                                }
                            }
                            else
                            {
                                // no side info => full element
                                var sourceUri = BuildUriForRepoSingleSubmodel(baseUri, lr.Reference);
                                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                                    null,
                                    sourceUri: sourceUri,
                                    allowFakeResponses: allowFakeResponses,
                                    runtimeOptions: runtimeOptions,
                                    lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                                    {
                                        if (code != HttpStatusCode.OK)
                                            return;

                                        try
                                        {
                                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                            var sm = Jsonization.Deserialize.SubmodelFrom(node);
                                            lock (prepSM)
                                            {
                                                lambdaReportAll(numAAS, ++numSM, numCD, numDiv);
                                                trackLoadedIdentifiables?.Add(sm);
                                                if (prepSM.AddIfNew(sm, new AasIdentifiableSideInfo()
                                                {
                                                    IsStub = false,
                                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                                    Id = sm.Id,
                                                    IdShort = sm.IdShort,
                                                    Endpoint = sourceUri
                                                }))
                                                {
                                                    trackNewIdentifiables?.Add(sm);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            runtimeOptions?.Log?.Error(ex, "Parsing auto-loaded Submodel");
                                        }
                                    });
                            }
                        });
                }

                // start auto-load missing thumbnails?
                if (operationFound && (record?.AutoLoadThumbnails ?? false))
                    await Parallel.ForEachAsync(env.AllAssetAdministrationShells(),
                        new ParallelOptions() {
                            MaxDegreeOfParallelism = record?.ParallelReads ?? Options.Curr.MaxParallelOps,
                            CancellationToken = runtimeOptions?.CancellationTokenSource?.Token ?? CancellationToken.None
                        },
                        async (aas, token) =>
                        {
                            await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                                client,
                                sourceUri: BuildUriForRepoAasThumbnail(baseUri, aas.Id),
                                allowFakeResponses: allowFakeResponses,
                                runtimeOptions: runtimeOptions,
                                lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                                {
                                    if (code != HttpStatusCode.OK)
                                        return;

                                    try
                                    {
                                        lambdaReportAll(numAAS, numSM, numCD, ++numDiv);
                                        dynPack.AddThumbnail(aas.Id, ms.ToArray());
                                    }
                                    catch (Exception ex)
                                    {
                                        runtimeOptions?.Log?.Error(ex, "Managing auto-loaded tumbnail");
                                    }
                                });
                        });

                // start auto-load missing CDs?
                if (operationFound && (record?.AutoLoadCds ?? false))
                {
                    var lrs = env.FindAllReferencedSemanticIds().ToList();

                    await Parallel.ForEachAsync(lrs,
                        new ParallelOptions() {
                            MaxDegreeOfParallelism = record?.ParallelReads ?? Options.Curr.MaxParallelOps,
                            CancellationToken = runtimeOptions?.CancellationTokenSource?.Token ?? CancellationToken.None
                        },
                        async (lr, token) =>
                        {
                            // cancelled?
                            token.ThrowIfCancellationRequested();

                            // valid?
                            var cdid = lr.Reference?.GetAsExactlyOneKey()?.Value;
                            if (cdid?.HasContent() != true)
                                return;

                            // only side info or full?
                            if (record?.AutoLoadOnDemand ?? true)
                            {
                                // side info level 1
                                lock (prepCD)
                                {
                                    prepCD.AddIfNew(null, new AasIdentifiableSideInfo()
                                    {
                                        IsStub = true,
                                        StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                        Id = lr.Reference.Keys[0].Value,
                                        IdShort = "",
                                        Endpoint = BuildUriForRepoSingleCD(baseUri, cdid)
                                    });
                                }
                            }
                            else
                            {
                                // no side info => full element
                                var sourceUri = BuildUriForRepoSingleCD(baseUri, cdid);
                                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                                    null,
                                    sourceUri: sourceUri,
                                    allowFakeResponses: allowFakeResponses,
                                    runtimeOptions: runtimeOptions,
                                    lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                                    {
                                        if (code != HttpStatusCode.OK)
                                            return;

                                        try
                                        {
                                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                            var cd = Jsonization.Deserialize.ConceptDescriptionFrom(node);
                                            lock (prepCD)
                                            {
                                                lambdaReportAll(numAAS, numSM, ++numCD, numDiv);
                                                trackLoadedIdentifiables?.Add(cd);
                                                if (prepCD.AddIfNew(cd, new AasIdentifiableSideInfo()
                                                {
                                                    IsStub = false,
                                                    StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                                                    Id = cd.Id,
                                                    IdShort = cd.IdShort,
                                                    Endpoint = sourceUri
                                                }))
                                                {
                                                    trackNewIdentifiables?.Add(cd);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            runtimeOptions?.Log?.Error(ex, "Parsing auto-loaded ConceptDescriptions");
                                        }
                                    });
                            }
                        });
                }
            }

            //
            // FINALIZE
            //

            // any operation found?
            if (!operationFound)
            {
                runtimeOptions?.Log?.Error("Did not find any matching operation in location to " +
                    "execute on Registry or Repository! Location was: {0}",
                    fullItemLocation);
                return null;
            }

            // before committing: shall this commit?
            runtimeOptions?.CancellationTokenSource?.Token.ThrowIfCancellationRequested();

            // how to commit?
            if (loadNew)
            {
                // commit new situation
                // bring back to env
                env.AssetAdministrationShells = prepAas;
                env.Submodels = prepSM;
                env.ConceptDescriptions = prepCD;

                // remove, what is not need
                if (env.AssetAdministrationShellCount() < 1)
                    env.AssetAdministrationShells = null;
                if (env.SubmodelCount() < 1)
                    env.Submodels = null;
                if (env.ConceptDescriptionCount() < 1)
                    env.ConceptDescriptions = null;

                // commit
                targetEnv = dynPack;
                targetEnv.SetEnvironment(env);
            }
            else
            {
                // for the "not new" option, treat the existing situation very carefully
                if (prepAas.Count() < 1)
                    prepAas = null;
                if (prepSM.Count() < 1)
                    prepSM = null;
                if (prepCD.Count() < 1)
                    prepCD = null;

                // adopt back
                if (env.AssetAdministrationShells == null && prepAas != null)
                    env.AssetAdministrationShells = prepAas;
                if (env.Submodels == null && prepSM != null)
                    env.Submodels = prepSM;
                if (env.ConceptDescriptions == null && prepCD != null)
                    env.ConceptDescriptions = prepCD;
            }

            // for the records
            (targetEnv as AdminShellPackageDynamicFetchEnv)?.SetContext(new PackageContainerHttpRepoSubsetFetchContext()
            {
                Record = record,
                Cursor = cursor
            });

            return targetEnv;
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
            public enum BaseTypeEnum { Repository, Registry }
            public static string[] BaseTypeEnumNames = new[] { "Repository", "Registry" };

            [AasxMenuArgument(help: "Specifies the part of the URI of the Repository/ Registry, which is " +
                "common to all operations.")]
            public string BaseAddress = "";
            // public string BaseAddress = "https://cloudrepo.aas-voyager.com/";
            // public string BaseAddress = "https://eis-data.aas-voyager.com/";
            // public string BaseAddress = "http://smt-repo.admin-shell-io.com/api/v3.0";
            // public string BaseAddress = "https://techday2-registry.admin-shell-io.com/";

            [AasxMenuArgument(help: "Either: Repository or Registry")]
            public BaseTypeEnum BaseType = BaseTypeEnum.Repository;
            // public BaseTypeEnum BaseType = BaseTypeEnum.Registry;

            [AasxMenuArgument(help: "Retrieve all AAS from Repository or Registry. " +
                "Note: Use of PageLimit is recommended.")]
            public bool GetAllAas = true;

            [AasxMenuArgument(help: "Get a single AAS, which is specified by AasId.")]
            public bool GetSingleAas;

            [AasxMenuArgument(help: "Specicies the Id of the AAS to be retrieved.")]
            // public string AasId = "https://new.abb.com/products/de/2CSF204101R1400/aas";
            public string AasId = "";
            // public string AasId = "https://phoenixcontact.com/qr/2900542/1/aas/1B";

            [AasxMenuArgument(help: "Get a single AAS, which is specified by a asset link/ asset id.")]
            public bool GetAasByAssetLink;

            [AasxMenuArgument(help: "Specicies the Id of the asset to be retrieved.")]
            public string AssetId = "";
            // public string AssetId = "https://pk.harting.com/?.20P=ZSN1";

            [AasxMenuArgument(help: "Retrieve all Submodels from Repository or Registry. " +
                "Note: Use of PageLimit is recommended.")]
            public bool GetAllSubmodel;

            [AasxMenuArgument(help: "Get a single AAS, which is specified by SmId.")]
            public bool GetSingleSubmodel;

            [AasxMenuArgument(help: "Specicies the Id of the Submodel to be retrieved.")]
            // public string SmId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvc20vMjAxNV82MDIwXzMwMTJfMDU4NQ==";
            public string SmId = "";

            [AasxMenuArgument(help: "Retrieve all ConceptDescriptions from Repository or Registry. " +
                "Note: Use of PageLimit is recommended.")]
            public bool GetAllCD;

            [AasxMenuArgument(help: "Get a single ConceptDescription, which is specified by CdId.")]
            public bool GetSingleCD;

            [AasxMenuArgument(help: "Specicies the Id of the ConceptDescription to be retrieved.")]
            public string CdId;

            [AasxMenuArgument(help: "Executes a GraphQL query on the Repository/ Registry. ")]
            public bool ExecuteQuery;

            [AasxMenuArgument(help: "Specicies the contents of the query script to be executed. " +
                "Note: Complex syntax and quoting needs to be applied!")]
            public string QueryScript = "";
            // public string QueryScript = "{\r\n  searchSMs(\r\n    expression: \"\"\"$LOG\r\n     filter=\r\n      or(\r\n        str_contains(sm.IdShort, \"Technical\"),\r\n        str_contains(sm.IdShort, \"Nameplate\")\r\n      )\r\n   \"\"\"\r\n  )\r\n  {\r\n    url\r\n    smId\r\n  }\r\n}";
            // public string QueryScript = "{\r\n  searchSMs(\r\n    expression: \"\"\"$LOG$QL\r\n          ( contains(sm.idShort, \"Technical\") and\r\n          sme.value ge 100 and\r\n          sme.value le 200 )\r\n        or\r\n          ( contains(sm.idShort, \"Nameplate\") and\r\n          contains(sme.idShort,\"ManufacturerName\") and\r\n          not(contains(sme.value,\"Phoenix\")))\r\n    \"\"\"\r\n  )\r\n  {\r\n    url\r\n    smId\r\n  }\r\n}";

            [AasxMenuArgument(help: "When a AAS is retrieved, try to retrieve Submodels as well. " +
                "Note: For this retrieveal, AutoLoadOnDemand may apply.")]
            public bool AutoLoadSubmodels = true;

            [AasxMenuArgument(help: "When a Submodel is retrieved, try to retrieve ConceptDescriptions " +
                "identified by semanticIds as well. " +
                "Note: For this retrieveal, AutoLoadOnDemand may apply. " +
                "Note: This might significantly increase the number of retrievals.")]
            public bool AutoLoadCds = false;

            [AasxMenuArgument(help: "When a AAS is retrieved, try to retrieve the associated thumbnail as well.")]
            public bool AutoLoadThumbnails = true;
            
            [AasxMenuArgument(help: "When a Submodel/ ConceptDescription is auto-loaded, either load the element " +
                "directly (true) or just create a side-information for later fetch.")]
            public bool AutoLoadOnDemand = true;

            [AasxMenuArgument(help: "Encrypt given Ids.")]
            public bool EncryptIds = true;

            [AasxMenuArgument(help: "Number of paralle read-operations at the same time. 1 is linear.")]
            public int ParallelReads = Options.Curr.MaxParallelOps;

            [AasxMenuArgument(help: "Stay connected with Repository/ Registry and eventually subscribe to " +
                "AAS events.")]
            public bool StayConnected;

            /// <summary>
            /// Pagenation. Limit to <c>n</c> results.
            /// </summary>
            [AasxMenuArgument(help: "Pagenation. Limit to n results.")]
            public int PageLimit = 15;

            /// <summary>
            /// When fetching, skip first <c>n</c> elements of the results.
            /// </summary>
            [AasxMenuArgument(help: "When fetching, skip first n elements of the results.")] 
            public int PageSkip = 0;

            /// <summary>
            /// This offset in elements is computed by this client by "counting". It does NOT come form
            /// the server!
            /// </summary>
            [JsonIgnore]
            public int PageOffset;

            public enum QueryChoice { 
                None, 
                AllAas, 
                SingleAas, 
                AasByAssetLink,
                AllSM,
                SingleSM,
                AllCD,
                SingleCD,
                Query
            }

            public void SetQueryChoices(QueryChoice choice)
            {
                GetAllAas = (choice == QueryChoice.AllAas);
                GetSingleAas = (choice == QueryChoice.SingleAas);
                GetAasByAssetLink = (choice == QueryChoice.AasByAssetLink);
                GetAllSubmodel = (choice == QueryChoice.AllSM);
                GetSingleSubmodel = (choice == QueryChoice.SingleSM);
                GetAllCD = (choice == QueryChoice.AllCD);
                GetSingleCD = (choice == QueryChoice.SingleCD);
                ExecuteQuery = (choice == QueryChoice.Query);
            }

            public string GetBaseTypStr()
            {
                return AdminShellUtil.MapIntToStringArray((int)BaseType,
                        "Unknown", ConnectExtendedRecord.BaseTypeEnumNames);
            }

            public string GetFetchOperationStr()
            {
                var res = "Unknown";

                if (BaseType == BaseTypeEnum.Registry)
                {
                    if (GetAllAas) res = "GetAllAssetAdministrationShellDescriptors";
                    if (GetSingleAas) res = "GetAssetAdministrationShellDescriptorById";
                    if (GetAasByAssetLink) res = "GetAllAssetAdministrationShellIdsByAssetLink";
                    if (GetAllSubmodel) res = "GetAllSubmodelDescriptors";
                    if (GetSingleSubmodel) res = "GetSubmodelDescriptorById";
                }

                if (BaseType == BaseTypeEnum.Repository)
                {
                    if (GetAllAas) res = "GetAllAssetAdministrationShells";
                    if (GetSingleAas) res = "GetAssetAdministrationShellById";
                    if (GetAasByAssetLink) res = "GetAllAssetAdministrationShellIdsByAssetLink";
                    if (GetAllSubmodel) res = "GetAllSubmodels";
                    if (GetSingleSubmodel) res = "GetSubmodelById";
                    if (GetAllCD) res = "GetAllConceptDescriptions";
                    if (GetSingleCD) res = "GetConceptDescriptionById";
                    if (ExecuteQuery) res = "ExecuteQuery";
                }

                return res;
            }
        }

        public class PackageContainerHttpRepoSubsetOptions : PackageContainerOptionsBase
        {
            public PackageContainerHttpRepoSubsetOptions(
                PackageContainerOptionsBase baseOpt,
                ConnectExtendedRecord record)
            {
                if (baseOpt != null)
                {
                    LoadResident = baseOpt.LoadResident;
                    StayConnected = baseOpt.StayConnected;
                    UpdatePeriod = baseOpt.UpdatePeriod;
                }

                if (baseOpt is PackageContainerHttpRepoSubsetOptions fullOpt)
                    Record = fullOpt.Record?.Copy();
                else
                    Record = record;
            }

            public ConnectExtendedRecord Record;
        }

        public static string BuildLocationFrom(
            ConnectExtendedRecord record,
            string cursor = null)
        {
            // access
            if (record == null || record.BaseAddress?.HasContent() != true)
                return null;

            var baseUri = new Uri(record.BaseAddress);

            //
            // REPO
            //

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
            {
                // All AAS?
                if (record.GetAllAas)
                {
                    // if a skip has been requested, these AAS need to be loaded, as well
                    var uri = BuildUriForRepoAllAAS(baseUri, record.PageLimit + record.PageSkip, cursor);
                    return uri?.ToString();
                }

                // Single AAS?
                if (record.GetSingleAas)
                {
                    var uri = BuildUriForRepoSingleAAS(baseUri, record.AasId, encryptIds: record.EncryptIds);
                    return uri?.ToString();
                }

                // Single AAS by AssetLink?
                if (record.GetAasByAssetLink)
                {
                    var uri = BuildUriForRegistryAasByAssetLink(baseUri, record.AssetId, encryptIds: record.EncryptIds);
                    return uri?.ToString();
                }

                // All Submodels?
                if (record.GetAllSubmodel)
                {
                    // if a skip has been requested, these AAS need to be loaded, as well
                    var uri = BuildUriForRepoAllSubmodel(baseUri, record.PageLimit + record.PageSkip, cursor);
                    return uri?.ToString();
                }

                // Single Submodel?
                if (record.GetSingleSubmodel)
                {
                    var uri = BuildUriForRepoSingleSubmodel(baseUri, record.SmId, encryptIds: record.EncryptIds);
                    return uri?.ToString();
                }

                // All Submodels?
                if (record.GetAllCD)
                {
                    var uri = BuildUriForRepoAllCD(baseUri, record.PageLimit + record.PageSkip, cursor);
                    return uri?.ToString();
                }

                // Single CD?
                if (record.GetSingleCD)
                {
                    var uri = BuildUriForRepoSingleCD(baseUri, record.CdId, encryptIds: record.EncryptIds);
                    return uri?.ToString();
                }

                // Query?
                if (record.ExecuteQuery)
                {
                    var uri = BuildUriForRepoQuery(baseUri, record.QueryScript);
                    return uri?.ToString();
                }

            }

            //
            // REGISTRY
            //

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Registry)
            {
                // All AAS?
                if (record.GetAllAas)
                {
                    // if a skip has been requested, these AAS need to be loaded, as well
                    var uri = BuildUriForRegistryAllAAS(baseUri, record.PageLimit + record.PageSkip, cursor);
                    return uri?.ToString();
                }

                // Single AAS?
                if (record.GetSingleAas)
                {
                    var uri = BuildUriForRegistrySingleAAS(baseUri, record.AasId, encryptIds: record.EncryptIds);
                    return uri?.ToString();
                }

                // Single AAS by AssetLink?
                if (record.GetAasByAssetLink)
                {
                    var uri = BuildUriForRegistryAasByAssetLink(baseUri, record.AssetId, encryptIds: record.EncryptIds);
                    return uri?.ToString();
                }
            }

            // 
            // END
            //

            // nope
            return null;
        }

        public enum ConnectExtendedScope { 
            All = 0xffff, 
            BaseInfo = 0x0001,
            IdfTypes = 0x0002,
            Query = 0x004,
            GetOptions = 0x0008,
            StayConnected = 0x0010,
            Pagination = 0x0020,
        }

        public static async Task<bool> PerformConnectExtendedDialogue(
            AasxMenuActionTicket ticket,
            AnyUiContextBase displayContext,
            string caption,
            ConnectExtendedRecord record,
            ConnectExtendedScope scope = ConnectExtendedScope.All)
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

            if (ticket?.ScriptMode == true)
                return true;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(caption);
            uc.ActivateRenderPanel(record,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                // extraButtons: new[] { "A", "B" },
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

                    if ((scope & ConnectExtendedScope.BaseInfo) > 0)
                    {
                        // Base address + Type
                        helper.AddSmallLabelTo(g, row, 0, content: "Base address:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        var g2 = helper.AddSmallGridTo(g, row, 1, 1, 2, new[] { "#", "*" });

                        AnyUiUIElement.SetIntFromControl(
                                helper.Set(
                                    helper.AddSmallComboBoxTo(g2, 0, 0,
                                        items: ConnectExtendedRecord.BaseTypeEnumNames,
                                        selectedIndex: (int)record.BaseType,
                                        margin: new AnyUiThickness(0, 0, 5, 0),
                                        padding: new AnyUiThickness(0, 0, 0, 0)),
                                    minWidth: 200, maxWidth: 200),
                                    (i) => { record.BaseType = (ConnectExtendedRecord.BaseTypeEnum)i; });

                        if (displayContext is AnyUiContextPlusDialogs cpd
                                && cpd.HasCapability(AnyUiContextCapability.WPF))
                        {
                            AnyUiUIElement.SetStringFromControl(
                                helper.Set(
                                    helper.AddSmallComboBoxTo(g2, 0, 1,
                                        isEditable: true,
                                        items: Options.Curr.BaseAddresses?.ToArray(),
                                        text: "" + record.BaseAddress,
                                        margin: new AnyUiThickness(0, 0, 0, 0),
                                        padding: new AnyUiThickness(0, 0, 0, 0),
                                        horizontalAlignment: AnyUiHorizontalAlignment.Stretch)),
                                    (s) => { record.BaseAddress = s; });
                        }
                        else
                        {
                            AnyUiUIElement.SetStringFromControl(
                                    helper.Set(
                                        helper.AddSmallTextBoxTo(g2, 0, 1,
                                            text: $"{record.BaseAddress}",
                                            verticalAlignment: AnyUiVerticalAlignment.Center,
                                            verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                        horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                    (s) => { record.BaseAddress = s; });
                        }

                        row++;
                    }

                    if ((scope & ConnectExtendedScope.IdfTypes) > 0)
                    {
                        // All AASes
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "Get all AAS",
                                        isChecked: record.GetAllAas,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.AllAas);
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
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleAas);
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

                        // AAS(es) by asset link
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "AAS by AssetId",
                                        isChecked: record.GetAasByAssetLink,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.AasByAssetLink);
                                    else
                                        record.GetAasByAssetLink = false;
                                    return new AnyUiLambdaActionModalPanelReRender(uc);
                                });

                        helper.AddSmallLabelTo(g, row + 1, 0, content: "AssetId:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetStringFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g, row + 1, 1,
                                        text: $"{record.AssetId}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                (s) => { record.AssetId = s; });

                        row += 2;

                        // All Submodels
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "Get all Submodels",
                                        isChecked: record.GetAllSubmodel,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.AllSM);
                                    else
                                        record.GetAllSubmodel = false;
                                    return new AnyUiLambdaActionModalPanelReRender(uc);
                                });
                        row++;

                        // Single Submodel
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "Get single Submodel",
                                        isChecked: record.GetSingleSubmodel,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleSM);
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

                        // All CD
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "Get all CDs",
                                        isChecked: record.GetAllCD,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.AllCD);
                                    else
                                        record.GetAllCD = false;
                                    return new AnyUiLambdaActionModalPanelReRender(uc);
                                });
                        row++;

                        // Single CD
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "Get single ConceptDescription (CD)",
                                        isChecked: record.GetSingleCD,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleCD);
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
                    }

                    if ((scope & ConnectExtendedScope.Query) > 0)
                    {
                        // Query
                        AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallCheckBoxTo(g, row, 0,
                                        content: "Get by query definition",
                                        isChecked: record.ExecuteQuery,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    colSpan: 2),
                                (o) =>
                                {
                                    if ((bool)o)
                                        record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.Query);
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
                    }

                    if ((scope & ConnectExtendedScope.GetOptions) > 0)
                    {
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
                    }

                    // Parallel execution
                    if ((scope & ConnectExtendedScope.GetOptions) > 0)
                    {
                        // Pagination
                        helper.AddSmallLabelTo(g, row, 0, content: "Parallel fetch:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        var g3 = helper.AddSmallGridTo(g, row, 1, 1, 2, new[] { "#", "*" });

                        AnyUiUIElement.SetIntFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g3, 0, 0,
                                        margin: new AnyUiThickness(0, 0, 0, 0),
                                        text: $"{record.ParallelReads:D}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                        minWidth: 80, maxWidth: 80,
                                        horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                        (i) => { record.ParallelReads = i; });

                        helper.AddSmallLabelTo(g3, 0, 1, content: "(concurrent reads, 1 = sequential)",
                            margin: new AnyUiThickness(10, 0, 0, 0),
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        row++;
                    }

                    if ((scope & ConnectExtendedScope.StayConnected) > 0)
                    {
                        // Stay connected
                        AnyUiUIElement.SetBoolFromControl(
                                    helper.Set(
                                        helper.AddSmallCheckBoxTo(g, row, 1,
                                            content: "Stay connected (will receive events)",
                                            isChecked: record.StayConnected,
                                            verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                                    (b) => { record.StayConnected = b; });

                        row++;
                    }

                    if ((scope & ConnectExtendedScope.Pagination) > 0)
                    { 
                        // Pagination
                        helper.AddSmallLabelTo(g, row, 0, content: "Pagination:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        var g3 = helper.AddSmallGridTo(g, row, 1, 1, 4, new[] { "#", "*", "#", "*" });

                        helper.AddSmallLabelTo(g3, 0, 0, content: "Limit results:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g3, 0, 1,
                                        margin: new AnyUiThickness(10, 0, 0, 0),
                                        text: $"{record.PageLimit:D}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                        minWidth: 80, maxWidth: 80,
                                        horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                        (i) => { record.PageLimit = i; });

                        helper.AddSmallLabelTo(g3, 0, 2, content: "Skip results:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g3, 0, 3,
                                        margin: new AnyUiThickness(10, 0, 0, 0),
                                        text: $"{record.PageSkip:D}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                        minWidth: 80, maxWidth: 80,
                                        horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                        (i) => { record.PageSkip = i; });

                        row++;
                    }

                    // give back
                    return g;
                });

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return false;

            // ok
            return true;
        }

        public class FileElementRecord
        {
            public List<Aas.IReferable> Parents;
            public Aas.IFile FileSme;
            public string IdShortPath;
        }

        public static List<FileElementRecord> FindAllUsedFileElements(
            Aas.ISubmodel submodel,
            Action<string> lambdaReportIdShortPathError = null,
            char seperatorChar = '.')
        {
            // access
            var res = new List<FileElementRecord>();
            if (submodel?.SubmodelElements == null)
                return res;

            // recurse and add
            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
            {
                // File?
                if (sme is Aas.IFile file)
                {
                    // check, if there is file content available
                    if (file?.Value?.HasContent() != true)
                    {
                        // do not count as severe failure; just skip
                        return true;
                    }

                    // build idShort list
                    var rfs = new List<Aas.IReferable>();
                    if (parents != null)
                        rfs.AddRange(parents);
                    rfs.Add(sme);
                    var idss = rfs.Select((rf) => "" + rf?.IdShort);

                    // check if the elements are fully complying
                    var comply = true;
                    foreach (var ids in idss)
                    {
                        var test = Regex.Replace(ids, @"[^a-zA-Z0-9_]", "_");
                        if (test != ids)
                            comply = false;
                    }

                    // join and report!
                    var idsp = string.Join(seperatorChar, idss);
                    if (!comply)
                    {
                        // may report
                        lambdaReportIdShortPathError?.Invoke(idsp);
                        // continue with next SME
                        return true;
                    }

                    // now add
                    res.Add(new FileElementRecord() { 
                        Parents = parents.Copy(),
                        FileSme = file,
                        IdShortPath = idsp
                    });
                }

                // always go deeper
                return true;
            });

            return res;
        }

        public class UploadAssistantJobRecord
        {
            [AasxMenuArgument(help: "Specifies the part of the URI of the Repository/ Registry, which is " +
                "common to all operations.")]
            public string BaseAddress = "";
            // public string BaseAddress = "https://cloudrepo.aas-voyager.com/";
            // public string BaseAddress = "https://eis-data.aas-voyager.com/";
            // public string BaseAddress = "http://smt-repo.admin-shell-io.com/api/v3.0";
            // public string BaseAddress = "https://techday2-registry.admin-shell-io.com/";

            [AasxMenuArgument(help: "Either: Repository or Registry")]
            public ConnectExtendedRecord.BaseTypeEnum BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository;
            // public ConnectExtendedRecord.BaseTypeEnum BaseType = ConnectExtendedRecord.BaseTypeEnum.Registry;

            [AasxMenuArgument(help: "Includes Submodels of the particular AAS into the upload.")]
            public bool IncludeSubmodels = false;

            [AasxMenuArgument(help: "Includes ConceptDescriptions referrred by semanticIds of the Submodels and " +
                "SubmodelElements into the upload.")]
            public bool IncludeCDs = false;

            [AasxMenuArgument(help: "Includes supplementary files of FileElements of the Submodels into the upload.")]
            public bool IncludeSupplFiles = false;

            [AasxMenuArgument(help: "If Identifiables exist on particular Repository/ Registry, still flag to " +
                "upload them anyway, overwriting the existing contents.")]
            public bool OverwriteIfExist = false;
        }

        public static async Task<bool> PerformUploadAssistant(
            AasxMenuActionTicket ticket,
            AnyUiContextBase displayContext,
            string caption,
            AdminShellPackageEnvBase packEnv,
            IEnumerable<Aas.IIdentifiable> idfs,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // access
            if (displayContext == null || caption?.HasContent() != true || packEnv == null || idfs == null)
                return false;

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

            // build statistics
            var numAas = idfs.Where((idf) => idf is Aas.IAssetAdministrationShell).Count();
            var numSm = idfs.Where((idf) => idf is Aas.ISubmodel).Count();
            var numCD = idfs.Where((idf) => idf is Aas.IConceptDescription).Count();

            //
            // Screen 1 : Job attributes
            //
            
            var recordJob = new UploadAssistantJobRecord();
            ticket?.ArgValue?.PopulateObjectFromArgs(recordJob);

            var ucJob = new AnyUiDialogueDataModalPanel(caption);
            ucJob.ActivateRenderPanel(recordJob,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                renderPanel: (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 2, new[] { "200:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5),
                                margin: new AnyUiThickness(10, 0, 30, 0));

                    panel.Add(g);

                    // dynamic rows
                    int row = 0;

                    // Statistics
                    helper.AddSmallLabelTo(g, row, 0, content: "Available for upload:");

                    helper.AddSmallLabelTo(g, row, 1, content: "# of AAS: " + numAas);
                    helper.AddSmallLabelTo(g, row + 1, 1, content: "# of Submodel: " + numSm);
                    helper.AddSmallLabelTo(g, row + 2, 1, content: "# of ConceptDescription: " + numCD);

                    row += 3;

                    // separation
                    helper.AddSmallBorderTo(g, row, 0, 
                        borderThickness: new AnyUiThickness(0.5), borderBrush: AnyUiBrushes.White, 
                        colSpan: 2,
                        margin: new AnyUiThickness(0, 0, 0, 20));
                    row++;

                    // Base address + Type
                    helper.AddSmallLabelTo(g, row, 0, content: "Base address:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    var g2 = helper.AddSmallGridTo(g, row, 1, 1, 2, new[] { "#", "*" });

                    AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g2, 0, 0,
                                    items: ConnectExtendedRecord.BaseTypeEnumNames,
                                    selectedIndex: (int)recordJob.BaseType,
                                    margin: new AnyUiThickness(0, 0, 5, 0),
                                    padding: new AnyUiThickness(0, -1, 0, -3)),
                                minWidth: 200, maxWidth: 200),
                                (i) => { recordJob.BaseType = (ConnectExtendedRecord.BaseTypeEnum)i; });

                    if (displayContext is AnyUiContextPlusDialogs cpd
                            && cpd.HasCapability(AnyUiContextCapability.WPF))
                    {
                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g2, 0, 1,
                                    isEditable: true,
                                    items: Options.Curr.BaseAddresses?.ToArray(),
                                    text: "" + recordJob.BaseAddress,
                                    margin: new AnyUiThickness(0, 0, 0, 0),
                                    padding: new AnyUiThickness(0, 0, 0, 0),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch)),
                                (s) => { recordJob.BaseAddress = s; });
                    }
                    else
                    {
                        AnyUiUIElement.SetStringFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g2, 0, 1,
                                        text: $"{recordJob.BaseAddress}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                (s) => { recordJob.BaseAddress = s; });
                    }

                    row++;

                    // Include Submodels
                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Include Submodels (in upload)",
                                    isChecked: recordJob.IncludeSubmodels,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { recordJob.IncludeSubmodels = b; });

                    row++;

                    // Include CDs
                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Include ConceptDescriptions (in upload)",
                                    isChecked: recordJob.IncludeCDs,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { recordJob.IncludeCDs = b; });

                    row++;

                    // Include supplementary files
                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Include supplementary files for File elements",
                                    isChecked: recordJob.IncludeSupplFiles,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { recordJob.IncludeSupplFiles = b; });

                    row++;

                    // Overwrite if exists?
                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Upload to overwrite on repository, if exists",
                                    isChecked: recordJob.OverwriteIfExist,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { recordJob.OverwriteIfExist = b; });

                    row++;

                    // give back
                    return g;
                });

            if (ticket?.ScriptMode != true)
            {
                // if not in script mode, perform dialogue
                if (!(await displayContext.StartFlyoverModalAsync(ucJob)))
                    return false;
            }

            // some obvious checks
            if (recordJob.BaseAddress?.HasContent() != true)
            {
                Log.Singleton.Error("No BaseAddress given. Aborting!");
                return false;
            }

            // sort Identifiables to look nice
            var sorted = idfs.ToList();
            Func<Aas.IIdentifiable, int> rankLambda = (idf) =>
            {
                if (idf is Aas.IAssetAdministrationShell)
                    return 1;
                if (idf is Aas.ISubmodel)
                    return 2;
                if (idf is Aas.IConceptDescription)
                    return 3;
                return 4;
            };
            sorted.Sort((i1, i2) => {
                if (rankLambda(i1) < rankLambda(i2))
                    return -1;
                if (rankLambda(i1) > rankLambda(i2))
                    return +1;
                return i1?.IdShort?.CompareTo(i2?.IdShort) ?? 0;
            });
            
            if (sorted.Count < 1)
                return false;

            // build list
            var rows = sorted
                .Where((idf) => (
                       (!(idf is Aas.ISubmodel) || recordJob.IncludeSubmodels)
                    && (!(idf is Aas.IConceptDescription) || recordJob.IncludeCDs)
                ))
                .Select((idf) => new AnyUiDialogueDataGridRow() { 
                    Tag = idf,
                    Cells = (new[] { 
                        // Type, IdShort, Id, Ver. Local, Method, Ver. Server
                        "" + idf?.GetSelfDescription()?.ElementAbbreviation,
                        "" + idf?.IdShort,
                        "" + idf?.Id, 
                        "" + (idf?.Administration?.ToStringExtended(1) ?? "-"),
                        "POST?",
                        "-"
                    }).ToList()
                })
                .ToList();

            // in order to re-use sockets
            Uri baseUri = null;
            HttpClient client = null;
            if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
            {
                // Note: it seems to be also possible to create an HttpClient with "" as BaseAddress and pass Host via URL!!
                baseUri = new Uri(recordJob.BaseAddress);
                client = PackageHttpDownloadUtil.CreateHttpClient(baseUri, runtimeOptions, containerList);
            }

            //
            // Screen 2: make a progress on the check of existence
            //
            var ucProgExist = new AnyUiDialogueDataProgress(
                "Check existence of Identifiables",
                info: "Preparing ...", symbol: AnyUiMessageBoxImage.Information);
            ucProgExist.Progress = 0.0;

            var numTotal = rows.Count;
            var numOK = 0;
            var numNOK = 0;

            // setup worker
            var workerCheck = new BackgroundWorker();
            workerCheck.DoWork += async (sender, e) =>
            {
                // ask server
                if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
                {
                    var numRes = await PackageHttpDownloadUtil.DownloadListOfIdentifiables<Aas.IIdentifiable, AnyUiDialogueDataGridRow>(
                        client,
                        rows,
                        lambdaGetLocation: (row) =>
                        {
                            // return new Uri("https://eis-data.aas-voyager.com/shells/aHR0cHM6Ly9uZXcuYWJiLmNvbS9wcm9kdWN0cy9kZS8yQ1NSMjU1MTYzUjExNjUvYWFz");
                            if (!(row.Tag is Aas.IIdentifiable idf))
                                return null;
                            if (idf is Aas.IAssetAdministrationShell)
                                return BuildUriForRepoSingleAAS(baseUri, idf?.Id, encryptIds: true);
                            if (idf is Aas.ISubmodel)
                                return BuildUriForRepoSingleSubmodel(baseUri, idf?.Id, encryptIds: true);
                            if (idf is Aas.IConceptDescription)
                                return BuildUriForRepoSingleCD(baseUri, idf?.Id, encryptIds: true);
                            return null;
                        },
                        lambdaGetTypeToSerialize: (row) => row.Tag?.GetType(),
                        runtimeOptions: runtimeOptions,
                        allowFakeResponses: false,
                        useParallel: Options.Curr.MaxParallelOps > 1,
                        lambdaDownloadDoneOrFail: (code, idf, contentFn, row) =>
                        {
                            // can change row?
                            if (row?.Cells == null || row.Cells.Count < 6)
                                return;

                            Action<bool> lambdaStat = (found) =>
                            {
                                if (found) { numOK++; } else { numNOK++; };
                                ucProgExist.Info = $"{numOK} entities found, {numNOK} entities missed of {numTotal}\n" +
                                    $"Id: {idf?.Id}";
                                ucProgExist.Progress = 100.0 * (1.0 * (numOK + numNOK) / Math.Max(1, numTotal));
                            };

                            // error by HTTP?
                            if (code == HttpStatusCode.NotFound)
                            {
                                row.Cells[4] = "POST(404)";
                                lambdaStat(false);
                                return;
                            }

                            if (code != HttpStatusCode.OK || idf == null)
                            {
                                row.Cells[4] = $"POST({(int)code})";
                                lambdaStat(false);
                                return;
                            }

                            if (row?.Cells != null && row.Cells.Count >= 6)
                            {
                                // status
                                row.Cells[4] = "-";
                                if (recordJob.OverwriteIfExist)
                                    row.Cells[4] = "PUT";
                                row.Cells[5] = (idf.Administration?.ToStringExtended(1)) ?? "-";
                                lambdaStat(true);
                            }
                        });

                }

                ucProgExist.DialogShallClose = true;

            };
            workerCheck.RunWorkerAsync();

            // close again
            await displayContext.StartFlyoverModalAsync(ucProgExist);

            //
            // Screen 3: show list of elements
            //
            var ucSelect = new AnyUiDialogueDataSelectFromDataGrid(
                        "Select element(s) to be uploaded ..",
                        maxWidth: 9999);

            ucSelect.ColumnDefs = AnyUiListOfGridLength.Parse(new[] { "50:", "1*", "3*", "70:", "70:", "70:" });
            ucSelect.ColumnHeaders = new[] { "Type", "IdShort", "Id", "V.Local", "Action", "V.Server" };
            ucSelect.Rows = rows.ToList();
            ucSelect.EmptySelectOk = true;

            if (ticket?.ScriptMode != true)
            {
                // if not in script mode, perform dialogue
                if (!(await displayContext.StartFlyoverModalAsync(ucSelect)))
                    return false;
            }

            // translate result items
            var rowsToUpload = ucSelect.ResultItems;
            if (rowsToUpload == null || rowsToUpload.Count() < 1)
                // nothings means: everything!
                rowsToUpload = rows;

            //
            // Screen 4: make a progress on the upload of Identifiables
            //
            var ucProgUpload = new AnyUiDialogueDataProgress(
                "Upload of Identifiables",
                info: "Preparing ...", symbol: AnyUiMessageBoxImage.Information);
            ucProgUpload.Progress = 0.0;

            numTotal = rowsToUpload
                .Where((row) => row?.Cells != null && row?.Cells.Count >= 6 &&
                    row.Cells[4].StartsWith("P"))
                .Count();
            numOK = 0;
            numNOK = 0;
            var numAttOK = 0;
            var numAttNOK = 0;

            // setup worker
            var workerUpload = new BackgroundWorker();
            workerUpload.DoWork += async (sender, e) =>
            {
                // ask server
                if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
                {
                    Func<AnyUiDialogueDataGridRow, Task> lambdaRow = async (row) =>
                    {
                        // idf?
                        if (!(row?.Tag is Aas.IIdentifiable idf))
                            return;

                        // put / post
                        var usePut = row.Cells != null && row.Cells.Count >= 6 &&
                                row.Cells[4].StartsWith("PUT");
                        var usePost = row.Cells != null && row.Cells.Count >= 6 &&
                                row.Cells[4].StartsWith("POST");

                        if (!usePut && !usePost)
                            return;

                        // workaround: API for POST Submodel seems to require Aas.Id
                        // TODO (MIHO, 2024-11-01): follow up, if this is really required!
                        string aasId = null;
                        if (idf is Aas.ISubmodel)
                        {
                            var aas = packEnv?.AasEnv?.FindAasWithSubmodelId(idf.Id);
                            aasId = aas?.Id;
                        }

                        //
                        // Identifiable
                        //

                        // location
                        Uri location = null;
                        if (idf is Aas.IAssetAdministrationShell)
                            location = BuildUriForRepoSingleAAS(baseUri, idf?.Id, encryptIds: true, usePost: usePost);
                        if (idf is Aas.ISubmodel)
                            location = BuildUriForRepoSingleSubmodel(baseUri, idf?.Id, encryptIds: true, usePost: usePost,
                                            addAasId: true, aasId: aasId);
                        if (idf is Aas.IConceptDescription)
                            location = BuildUriForRepoSingleCD(baseUri, idf?.Id, encryptIds: true, usePost: usePost);
                        if (location == null)
                            return;

                        // put Identifiable
                        var res2 = await PackageHttpDownloadUtil.HttpPutPostIdentifiable(
                            client,
                            idf,
                            destUri: location,
                            usePost: usePost,
                            runtimeOptions: runtimeOptions,
                            containerList: containerList);

                        //
                        // attachments (for Submodels)
                        //

                        if (idf is Aas.ISubmodel submodel && submodel.SubmodelElements != null)
                        {
                            // Note: the Part 2 PDF says '/', the swagger says '.'
                            var filEls = FindAllUsedFileElements(submodel,
                                seperatorChar: '.',
                                lambdaReportIdShortPathError: (idsp) =>
                                {
                                    Log.Singleton.Error("When uploading Submodel {0}, idShort path for File " +
                                            "element contains invalid characters and prevents uploading file " +
                                            "attchment: {1}", submodel.IdShort, idsp);
                                    lock (rowsToUpload)
                                    {
                                        numAttNOK++;
                                    }
                                });

                            foreach (var filEl in filEls)
                            {
                                // access
                                if (filEl?.FileSme?.Value?.HasContent() != true)
                                    continue;

                                // try read the bytes (has NO try/catch in it)
                                byte[] ba = null;
                                try
                                {
                                    ba = await packEnv.GetBytesFromPackageOrExternalAsync(filEl.FileSme.Value);
                                } 
                                catch (Exception ex)
                                {
                                    LogInternally.That.SilentlyIgnoredError(ex);
                                    ba = null;
                                }
                                if (ba == null || ba.Length < 1)
                                {
                                    Log.Singleton.Error("Centralize file: cannot read file: {0}", filEl.FileSme.Value);
                                    lock (rowsToUpload)
                                    {
                                        numAttNOK++;
                                    }
                                    continue;
                                }

                                // try PUT
                                try
                                {
                                    // serialize to memory stream
                                    var attLoc = BuildUriForRepoSingleSubmodelAttachment(
                                        baseUri, submodel.Id,
                                        idShortPath: filEl.IdShortPath,
                                        encryptIds: true,
                                        aasId: aasId);
                                    using (var ms = new MemoryStream(ba))
                                    {
                                        // write
#if _not_now
                                        using (var hc = new HttpClient())
                                        {
                                            // hc.BaseAddress = new Uri("http://localhost:5001");
                                            // hc.BaseAddress = new Uri("https://echo.free.beeceptor.com");
                                            hc.DefaultRequestHeaders.Clear();
                                            // hc.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                                            hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                                            var url = "https://echo.free.beeceptor.com";
                                            // var url = "http://localhost:5001/api/v3.0/shells/d3d3LmV4YW1wbGUuY29tL2lkcy9hYXMvODI4MV8yMTUyXzIxNDJfODE2Ng/submodels/d3d3LmV4YW1wbGUuY29tL2lkcy9zbS83NDg1XzAxMjFfMjA0Ml82NzAw/submodel-elements/Fixed_content.idta_smt_badge/attachment";
                                            // var url = "/";
                                            // var content = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
                                            // content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                                            // var content = new StringContent("{1.2345}", Encoding.UTF8, "application/json");

                                            using (var content = new MultipartFormDataContent())
                                            {
                                                content.Headers.ContentType.MediaType = "multipart/form-data";
                                                var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ \"a\": 3 }"));
                                                stream.Flush();
                                                stream.Seek(0, SeekOrigin.Begin);
                                                var fc = new StreamContent(stream); 
                                                fc.Headers.Add("Content-Disposition", "form-data; name=\"file\"");
                                                fc.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                                                content.Add(fc, "file", "upload.png");

                                                // content.Add(fc, "\"textdatei\"", "upload.txt");

                                                var resp = await hc.PutAsync(url, content);
                                                if (true || resp.IsSuccessStatusCode)
                                                {
                                                    // TODO: give back?
                                                    var c2 = await resp.Content.ReadAsStringAsync();
                                                    ;
                                                }
                                            }
                                        } 
#endif

#if __by_chatgpt
                                        using (var hc = new HttpClient())
                                        {
                                            // Base address if needed
                                            // hc.BaseAddress = new Uri("http://localhost:5001");
                                            // hc.BaseAddress = new Uri("https://echo.free.beeceptor.com");

                                            hc.DefaultRequestHeaders.Clear();
                                            hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                                            // var url = "https://echo.free.beeceptor.com"; // Replace with your actual URL
                                            var url = "http://localhost:5001/api/v3.0/shells/d3d3LmV4YW1wbGUuY29tL2lkcy9hYXMvODI4MV8yMTUyXzIxNDJfODE2Ng/submodels/d3d3LmV4YW1wbGUuY29tL2lkcy9zbS83NDg1XzAxMjFfMjA0Ml82NzAw/submodel-elements/Fixed_content.idta_smt_badge/attachment"; // Replace with your actual URL

                                            using (var content = new MultipartFormDataContent())
                                            {
                                                // Creating a stream with file data
                                                var filePath = @"C:\Users\Micha\Desktop\t3\new2\idta_smt_badge.png"; // Replace with your file path
                                                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                                                var fileContent = new StreamContent(fileStream);
                                                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                                                // Adding the file as form-data
                                                content.Add(fileContent, "file", "upload.png");

                                                // You can add other form data if needed
                                                // var jsonContent = new StringContent("{ \"a\": 3 }", Encoding.UTF8, "application/json");
                                                // content.Add(jsonContent, "data");

                                                var resp = await hc.PutAsync(url, content);
                                                var responseBody = await resp.Content.ReadAsStringAsync();
                                            }
                                        }
#endif

                                        // the multi-part content needs very specific information to work
                                        var mpFn = Path.GetFileName(filEl.FileSme.Value);
                                        var mpCt = filEl.FileSme.ContentType?.Trim();
                                        if (mpCt?.HasContent() != true)
                                            mpCt = "application/octet-stream";

                                        // do the PUT with multi-part content
                                        var res3 = await PackageHttpDownloadUtil.HttpPutPostFromMemoryStream(
                                            null, // do NOT re-use client, as headers are re-defined!
                                            ms,
                                            destUri: attLoc,
                                            runtimeOptions: runtimeOptions,
                                            containerList: containerList,
                                            usePost: usePost,
                                            useMultiPart: true,
                                            mpParamName: "file",
                                            mpFileName: mpFn,
                                            mpContentType: mpCt);

                                        lock (rowsToUpload)
                                        {
                                            if (res3.Item1 != HttpStatusCode.OK && res3.Item1 != HttpStatusCode.NoContent)
                                            {
                                                Log.Singleton.Error("Error uploading attachment of {0} bytes to: {1}. HTTP code {2} with content: {3}",
                                                    ba.Length, attLoc.ToString(), res3.Item1, res3.Item2);
                                                numAttNOK++;
                                            }
                                            else
                                            {
                                                numAttOK++;
                                            }
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(ex, 
                                        $"when PUTting attachment with {ba.Length} bytes to File element {filEl.IdShortPath}");
                                    numAttNOK++;
                                }
                            }
                        }

                        //
                        // mutex (after all async calls!)
                        //

                        lock (rowsToUpload)
                        {
                            Action<bool> lambdaStat = (ok) =>
                            {
                                if (ok) { numOK++; } else { numNOK++; };
                                ucProgUpload.Info = $"{numOK} entities OK, {numNOK} entities NOT OK of {numTotal}\n" +
                                        $"{numAttOK} attachments OK, {numAttNOK} attachments NOT OK\n" +
                                        $"Id: {idf?.Id}";
                                ucProgUpload.Progress = 100.0 * (1.0 * (numOK + numNOK) / Math.Max(1, numTotal));
                            };

                            if (res2 != null
                                && (res2.Item1 == HttpStatusCode.Created || res2.Item1 == HttpStatusCode.NoContent))
                            {
                                lambdaStat(true);
                            }
                            else
                            {
                                runtimeOptions?.Log?.Error("Put/Post of modified Identifiable returned error {0} for id={1} at {2}",
                                    "" + ((res2 != null) ? (int)res2.Item1 : -1),
                                    idf.Id,
                                    location.ToString());
                                lambdaStat(false);
                            }
                        }
                    };

                    // simple or parallel?
                    // TODO: currently suspecting aasx server to be not thread safe (error 500?)
                    if (true || Options.Curr.MaxParallelOps <= 1)
                    {
                        // simple to debug
                        foreach (var row in rowsToUpload)
                            await lambdaRow(row);
                    }
                    else
                    {
                        await Parallel.ForEachAsync(rowsToUpload,
                            new ParallelOptions() { MaxDegreeOfParallelism = Options.Curr.MaxParallelOps },
                            async (row, token) =>
                            {
                                await lambdaRow(row);
                            });
                    }
                }

                ucProgUpload.DialogShallClose = true;

            };
            workerUpload.RunWorkerAsync();

            // close again
            await displayContext.StartFlyoverModalAsync(ucProgUpload);

            // make stat on Log
            if (numNOK > 0)
            {
                runtimeOptions?.Log?.Error("When Put/ Push to Registry/ Repository, {0} element(s) were not uploaded " +
                    "while {1} uploaded ok. Location: {2}",
                    numNOK, numOK, recordJob.BaseAddress);
            }
            else if (numOK < 1)
            {
                runtimeOptions?.Log?.Info(StoredPrint.Color.Blue, "No need of Put/ Push element(s) " +
                    "to Registry/ Repository. Location {0}", recordJob.BaseAddress);
            }
            else
            {
                runtimeOptions?.Log?.Info(StoredPrint.Color.Blue, "Successful Put/ Push of {0} element(s) " +
                    "to Registry/ Repository. Location {1}",
                    numOK, recordJob.BaseAddress);
            }

            // ok
            return true;
        }

        public class DeleteAssistantJobRecord
        {
            public string BaseAddress = "";
            public ConnectExtendedRecord.BaseTypeEnum BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository;
        }

        /// <summary>
        /// Deletes a range of Identifiables from an Repo / Registry.
        /// </summary>
        /// <param name="idfIds">Each key to be an individual Identifiable!</param>
        public static async Task<bool> AssistantDeleteIdfsInRepo(
            AasxMenuActionTicket ticket,
            AnyUiContextBase displayContext,
            string caption,
            string elemKindName,
            IEnumerable<Aas.IKey> idfIds,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null,
            DeleteAssistantJobRecord presetRecord = null)
        {
            // access
            if (displayContext == null || caption?.HasContent() != true || idfIds == null || idfIds.Count() < 1)
                return false;

            var idfIdsDis = idfIds.Distinct().ToList();

            //
            // Screen 1 : ask for job / Repo
            //

            var recordJob = presetRecord ?? new DeleteAssistantJobRecord();
            var ucJob = new AnyUiDialogueDataModalPanel(caption);
            ucJob.ActivateRenderPanel(recordJob,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                renderPanel: (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 2, new[] { "200:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5),
                                margin: new AnyUiThickness(10, 0, 30, 0));

                    panel.Add(g);

                    // dynamic rows
                    int row = 0;

                    // Statistics
                    helper.AddSmallLabelTo(g, row, 0, content: "Requested to delete:");
                    helper.AddSmallLabelTo(g, row + 0, 1, 
                        content: $"# of {elemKindName}: " + idfIds.Count());

                    row += 1;

                    // separation
                    helper.AddSmallBorderTo(g, row, 0,
                        borderThickness: new AnyUiThickness(0.5), borderBrush: AnyUiBrushes.White,
                        colSpan: 2,
                        margin: new AnyUiThickness(0, 0, 0, 20));
                    row++;

                    // Base address + Type
                    helper.AddSmallLabelTo(g, row, 0, content: "Base address:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    var g2 = helper.AddSmallGridTo(g, row, 1, 1, 2, new[] { "#", "*" });

                    AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g2, 0, 0,
                                    items: ConnectExtendedRecord.BaseTypeEnumNames,
                                    selectedIndex: (int)recordJob.BaseType,
                                    margin: new AnyUiThickness(0, 0, 5, 0),
                                    padding: new AnyUiThickness(0, -1, 0, -3)),
                                minWidth: 200, maxWidth: 200),
                                (i) => { recordJob.BaseType = (ConnectExtendedRecord.BaseTypeEnum)i; });

                    if (displayContext is AnyUiContextPlusDialogs cpd
                        && cpd.HasCapability(AnyUiContextCapability.WPF))
                    {
                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g2, 0, 1,
                                    isEditable: true,
                                    items: Options.Curr.BaseAddresses?.ToArray(),
                                    text: "" + recordJob.BaseAddress,
                                    margin: new AnyUiThickness(0, 0, 0, 0),
                                    padding: new AnyUiThickness(0, 0, 0, 0),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch)),
                                (s) => { recordJob.BaseAddress = s; });
                    }
                    else
                    {
                        AnyUiUIElement.SetStringFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g2, 0, 1,
                                        text: $"{recordJob.BaseAddress}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                (s) => { recordJob.BaseAddress = s; });
                    }

                    row++;

                    // give back
                    return g;
                });

            if (!(await displayContext.StartFlyoverModalAsync(ucJob)))
                return false;

            //
            // Screen 2: make a progress on checking, which CDs are existing
            //

            var ucProgTest = new AnyUiDialogueDataProgress(
                $"Checking individual {elemKindName}s to exist",
                info: "Preparing ...", symbol: AnyUiMessageBoxImage.Information);
            ucProgTest.Progress = 0.0;

            var numTotal = idfIdsDis.Count;
            var idfExist = new List<Aas.IIdentifiable>();
            var numOK = 0;
            var numWrongType = 0;
            var numNOK = 0;

            // in order to re-use sockets
            Uri baseUri = null;
            HttpClient client = null;
            if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
            {
                baseUri = new Uri(recordJob.BaseAddress);
                client = PackageHttpDownloadUtil.CreateHttpClient(baseUri, runtimeOptions, containerList);
            }

            // setup worker
            var workerTest = new BackgroundWorker();
            workerTest.DoWork += async (sender, e) =>
            {
                // ask server
                if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
                {
                    // first, test which ids exist as CD in repo
                    await PackageHttpDownloadUtil.DownloadListOfIdentifiables<Aas.IIdentifiable, Aas.IKey>(
                        client,
                        idfIdsDis,
                        lambdaGetLocation: (key) =>
                        {
                            Uri location = null;
                            if (key?.Type == KeyTypes.AssetAdministrationShell)
                                location = BuildUriForRepoSingleAAS(baseUri, key.Value, encryptIds: true);
                            if (key?.Type == KeyTypes.Submodel)
                                location = BuildUriForRepoSingleSubmodel(baseUri, key.Value, encryptIds: true);
                            if (key?.Type == KeyTypes.ConceptDescription)
                                location = BuildUriForRepoSingleCD(baseUri, key.Value, encryptIds: true);
                            return location;
                        },
                        lambdaGetTypeToSerialize: (key) =>
                        {
                            Type type = null;
                            if (key?.Type == KeyTypes.AssetAdministrationShell)
                                type = typeof(Aas.IAssetAdministrationShell);
                            if (key?.Type == KeyTypes.Submodel)
                                type = typeof(Aas.ISubmodel);
                            if (key?.Type == KeyTypes.ConceptDescription)
                                type = typeof(Aas.IConceptDescription);
                            return type;
                        },
                        runtimeOptions: runtimeOptions,
                        useParallel: Options.Curr.MaxParallelOps > 1,
                        lambdaDownloadDoneOrFail: (code, idf, contentFn, key) =>
                        {
                            // need mutex
                            lock (idfExist)
                            {
                                // stat
                                Action<bool> lambdaStat = (ok) =>
                                {
                                    if (ok) { numOK++; } else { numNOK++; };
                                    ucProgTest.Info = $"{numOK} entities exist, {numNOK} entities NOT found in {numTotal}\n"
                                        + $"{numWrongType} were of wrong type,\n"
                                        + $"Id: {idf?.Id}";
                                    ucProgTest.Progress = 100.0 * (1.0 * (numOK + numNOK) / Math.Max(1, numTotal));
                                };

                                // match the types
                                var matchType = (idf != null && key != null) && (
                                    (idf is Aas.IAssetAdministrationShell && key.Type == KeyTypes.AssetAdministrationShell)
                                    || (idf is Aas.ISubmodel && key.Type == KeyTypes.Submodel)
                                    || (idf is Aas.IConceptDescription && key.Type == KeyTypes.ConceptDescription)
                                );
                                if (idf != null && !matchType)
                                    numWrongType++;

                                // any error?
                                if (code != HttpStatusCode.OK || idf == null || !matchType)
                                {
                                    lambdaStat(false);
                                    return;
                                }

                                // remember for later
                                idfExist.Add(idf);
                                lambdaStat(true);
                            }
                        });
                }

                ucProgTest.DialogShallClose = true;

            };
            workerTest.RunWorkerAsync();

            // show and close again
            await displayContext.StartFlyoverModalAsync(ucProgTest);

            // test
            if (idfExist.Count < 1)
            {
                runtimeOptions?.Log.Info(StoredPrint.Color.Blue, 
                    $"No {elemKindName} to delete found. Finalizing! Location: {0}",
                    recordJob.BaseAddress);
                return false;
            }
            
            // ask to proceed?
            if (AnyUiMessageBoxResult.Yes != displayContext.MessageBoxFlyoutShow(
                $"After checking individual ids, {idfExist.Count} {elemKindName}s seem to " +
                $"exist on the server. Proceed with deleting these?",
                $"Delete {elemKindName}",
                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
            {
                runtimeOptions?.Log.Info("Aborted.");
                return false;
            }

            //
            // Screen 4: make a progress on deleting
            //

            var ucProgDel = new AnyUiDialogueDataProgress(
                $"Deleting individual {elemKindName} to exist",
                info: "Preparing ...", symbol: AnyUiMessageBoxImage.Information);
            ucProgTest.Progress = 0.0;

            numTotal = idfExist.Count;
            numOK = 0;
            numNOK = 0;

            // setup worker
            var workerDel = new BackgroundWorker();
            workerDel.DoWork += async (sender, e) =>
            {
                // ask server
                if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
                {
                    var baseUri = new Uri(recordJob.BaseAddress);

                    // second, send deletes
                    await PackageHttpDownloadUtil.DeleteListOfEntities<Aas.IIdentifiable>(
                        client,
                        idfExist,
                        lambdaGetLocation: (idf) =>
                        {
                            Uri location = null;
                            if (idf is Aas.IAssetAdministrationShell)
                                location = BuildUriForRepoSingleAAS(baseUri, idf?.Id, encryptIds: true);
                            if (idf is Aas.ISubmodel)
                                location = BuildUriForRepoSingleSubmodel(baseUri, idf?.Id, encryptIds: true);
                            if (idf is Aas.IConceptDescription)
                                location = BuildUriForRepoSingleCD(baseUri, idf?.Id, encryptIds: true);
                            return location;
                        },
                        runtimeOptions: runtimeOptions,
                        useParallel: Options.Curr.MaxParallelOps > 1,
                        lambdaDeleteDoneOrFail: (code, content, idf) =>
                        {
                            // need mutex
                            lock (idfExist)
                            {
                                // stat
                                Action<bool> lambdaStat = (ok) =>
                                {
                                    if (ok) { numOK++; } else { numNOK++; };
                                    ucProgDel.Info = $"{numOK} entities exist, {numNOK} entities NOT found in {numTotal}\n" +
                                            $"Id: {idf?.Id}";
                                    ucProgDel.Progress = 100.0 * (1.0 * (numOK + numNOK) / Math.Max(1, numTotal));
                                };

                                // any error?
                                if (code != HttpStatusCode.OK && code != HttpStatusCode.NoContent)
                                {
                                    lambdaStat(false);
                                    return;
                                }

                                // ok
                                lambdaStat(true);
                            }
                        });
                }

                ucProgDel.DialogShallClose = true;

            };
            workerDel.RunWorkerAsync();

            // show and close again
            await displayContext.StartFlyoverModalAsync(ucProgDel);

            // okay?
            runtimeOptions?.Log.Info(StoredPrint.Color.Blue,
                "{0} {1}s deleted successfully, {2} NOK. Location: {3}",
                numOK, elemKindName, numNOK, recordJob.BaseAddress);

            return true;
        }
    }

}
