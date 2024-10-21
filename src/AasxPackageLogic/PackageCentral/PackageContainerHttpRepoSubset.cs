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

        //
        // REPO
        //

        public static bool IsValidUriForRepoAllAAS(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/shells(|/|/?\?(.*))$",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
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

        public static bool IsValidUriForRepoSingleCD(string location)
        {
            var m = Regex.Match(location, @"^(http(|s))://(.*?)/conceptdescriptions/(.{1,99})$",
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

        public static bool IsValidUriForRegistryAasByAssetId(string location)
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
            return IsValidUriForRepoAllAAS(location)
                || IsValidUriForRepoSingleAAS(location)
                || IsValidUriForRepoAllSubmodel(location)
                || IsValidUriForRepoSingleSubmodel(location)
                || IsValidUriForRepoSingleCD(location)
                || IsValidUriForRepoQuery(location)
                || IsValidUriForRegistryAllAAS(location)
                || IsValidUriForRegistrySingleAAS(location)
                || IsValidUriForRegistryAasByAssetId(location);
        }

        public static Uri GetBaseUri(string location)
        {
            // try an explicit search for known parts of ressources
            // (preserves scheme, host and leading pathes)
            var m = Regex.Match(location, @"^(.*?)(/shells|/submodel|/conceptdescription|/lookup)");
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

            if (Uri.TryCreate(baseUri, relativeUri, out var res))
                return res;

            return null;
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

        public static Uri BuildUriForRepoSingleAAS(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"shells/{smidenc}");
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

        public static Uri BuildUriForRepoSingleSubmodel(Uri baseUri, string id, bool encryptIds = true)
        {
            // access
            if (id?.HasContent() != true)
                return null;

            // try combine
            var smidenc = encryptIds ? AdminShellUtil.Base64UrlEncode(id) : id;
            return CombineUri(baseUri, $"submodels/{smidenc}");
        }

        public static Uri BuildUriForRepoSingleSubmodel(Uri baseUri, Aas.IReference submodelRef)
        {
            // access
            if (baseUri == null || submodelRef?.IsValid() != true
                || submodelRef.Count() != 1 || submodelRef.Keys[0].Type != KeyTypes.Submodel)
                return null;

            // pass on
            return BuildUriForRepoSingleSubmodel(baseUri, submodelRef.Keys[0].Value);
        }

        public static Uri BuildUriForRepoSingleCD(Uri baseUri, string id, bool encryptIds = true)
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

        /// <summary>
        /// This utility is able to parallel download Identifiables and will call lambda upon.
        /// Insted of a list of location, it is taking a list of objects (entities) and a lambda
        /// to extract the location from.
        /// </summary>
        /// <typeparam name="T">Type of Identifiable</typeparam>
        /// <typeparam name="E">Type of entity element</typeparam>
        protected static async Task<int> DownloadListOfIdentifiables<T, E>(
            IEnumerable<E> entities,
            Func<E, Uri> lambdaGetLocation,
            Action<HttpStatusCode, T, string, E> lambdaDownloadDoneOrFail,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false,
            bool useParallel = false) where T : Aas.IIdentifiable
        {
            // access
            if (entities == null)
                return 0;

            // result
            int numRes = 0;

            // lambda for deserialize
            Func<JsonNode, T> lambdaDeserialize = (node) =>
            {
                if (typeof(T).IsAssignableFrom(typeof(Aas.IAssetAdministrationShell)))
                    return (T)((Aas.IIdentifiable)Jsonization.Deserialize.AssetAdministrationShellFrom(node));
                if (typeof(T).IsAssignableFrom(typeof(Aas.ISubmodel)))
                    return (T)((Aas.IIdentifiable)Jsonization.Deserialize.SubmodelFrom(node));
                if (typeof(T).IsAssignableFrom(typeof(Aas.IConceptDescription)))
                    return (T)((Aas.IIdentifiable)Jsonization.Deserialize.ConceptDescriptionFrom(node));
                return default(T);
            };

            // over all locations
            if (!useParallel)
            {
                foreach (var ent in entities)
                {
                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        sourceUri: lambdaGetLocation?.Invoke(ent),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            // not OK?
                            if (code != HttpStatusCode.OK)
                            {
                                lambdaDownloadDoneOrFail?.Invoke(code, default(T), null, ent);
                                return;
                            }
                            
                            // go on
                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                T idf = lambdaDeserialize(node);
                                lambdaDownloadDoneOrFail?.Invoke(code, idf, contentFn, ent);
                                if (code == HttpStatusCode.OK)
                                    numRes++;
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, $"Parsing downloaded {typeof(T).GetDisplayName()}");
                            }
                        });
                }
            } 
            else
            {
                await Parallel.ForEachAsync(entities,
                    new ParallelOptions() { MaxDegreeOfParallelism = Options.Curr.MaxParallelOps },
                    async (ent, token) =>
                    {
                        var thisEnt = ent;
                        await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                            sourceUri: lambdaGetLocation?.Invoke(ent),
                            allowFakeResponses: allowFakeResponses,
                            runtimeOptions: runtimeOptions,
                            lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                            {
                                // not OK?
                                if (code != HttpStatusCode.OK)
                                {
                                    lambdaDownloadDoneOrFail?.Invoke(code, default(T), null, ent);
                                    return;
                                }

                                // go on
                                try
                                {
                                    var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                    T idf = lambdaDeserialize(node);
                                    lambdaDownloadDoneOrFail?.Invoke(code, idf, contentFn, thisEnt);
                                    if (code == HttpStatusCode.OK)
                                        numRes++;
                                }
                                catch (Exception ex)
                                {
                                    runtimeOptions?.Log?.Error(ex, $"Parsing downloaded {typeof(T).GetDisplayName()}");
                                }
                            });
                    });

            }

            // ok
            return numRes;
        }

        protected async Task<T> DownloadIdentifiableToOK<T>(
            Uri location,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false) where T : Aas.IIdentifiable
        {
            T res = default(T);

            await DownloadListOfIdentifiables<T, Uri>(
                new[] { location },
                lambdaGetLocation: (loc) => loc,
                runtimeOptions: runtimeOptions,
                allowFakeResponses: allowFakeResponses,
                lambdaDownloadDoneOrFail: (code, idf, contentFn, ent) =>
                {
                    if (code == HttpStatusCode.OK)
                        res = idf;
                });

            return res;
        }

        /// <summary>
        /// Can download arbitrary dynamic entity.
        /// </summary>
        /// <returns>Either dynamic object or <c>null</c></returns>
        protected async Task<dynamic> DownloadEntityToDynamicObject(
            Uri uri,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false)
        {
            // prepare receing the descriptors
            dynamic resObj = null;

            // GET
            await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                sourceUri: uri,
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
                            resObj = serializer.Deserialize(jsonTextReader);
                        }
                    }
                    catch (Exception ex)
                    {
                        runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                    }
                });

            return resObj;
        }

        private async Task<bool> FromRegistryGetAasAndSubmodels(            
            OnDemandListIdentifiable<IAssetAdministrationShell> prepAas, 
            OnDemandListIdentifiable<ISubmodel> prepSM,
            ConnectExtendedRecord record, 
            PackCntRuntimeOptions runtimeOptions,
            bool allowFakeResponses,            
            dynamic aasDescriptor)
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
                var aas = await DownloadIdentifiableToOK<Aas.IAssetAdministrationShell>(
                    aasSi.Endpoint, runtimeOptions, allowFakeResponses);
                if (aas == null)
                {
                    runtimeOptions?.Log?.Error(
                        "Unable to download AAS via registry. Skipping! Location: {0}",
                        aasSi.Endpoint.ToString());
                    continue;
                }
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
                prepAas?.Add(aas, aasSi);

                // check if to add the Submodels
                if (!record.AutoLoadOnDemand)
                {
                    // be prepared to download them
                    var numRes = await DownloadListOfIdentifiables<Aas.ISubmodel, AasIdentifiableSideInfo>(
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
                                prepSM.Add(null, si);
                            }

                            // no, add with data
                            si.IsStub = false;
                            prepSM?.Add(sm, si);
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
                        prepSM.Add(null, si);
                    }
                }

                // a little debug
                runtimeOptions?.Log?.Info(StoredPrint.Color.Blue,
                    "Retrieved AAS (potentially with Submodels) from: {0}",
                    aasSi.Endpoint.ToString());
            }

            return true;
        }

        public override async Task LoadFromSourceAsync(
            string fullItemLocation,
            PackageContainerOptionsBase containerOptions = null,
            PackCntRuntimeOptions runtimeOptions = null)
        {
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

            // invalidate cursor data
            string cursor = null;

            // TODO: very long function, needs to be refactored

            //
            // REGISTRY
            //

            var operationFound = false;

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Registry)
            {
                // AAS descriptors?
                var isAllAas = IsValidUriForRegistryAllAAS(fullItemLocation);
                var isAasByAssetId = IsValidUriForRegistryAasByAssetId(fullItemLocation);
                if (isAllAas || isAasByAssetId)
                {
                    // ok
                    operationFound = true;

                    // prepare receing the descriptors
                    var resObj = await DownloadEntityToDynamicObject(
                        new Uri(fullItemLocation), runtimeOptions, allowFakeResponses);

                    // Note: the result format for GetAllAAS and GetAllAssetAdministrationShellIdsByAssetLink
                    // is diffeent!
                    var arrObj = resObj;
                    if (isAllAas)
                        arrObj = resObj?.result;

                    // have a list of descriptors?!
                    if (resObj == null)
                    {
                        runtimeOptions?.Log?.Error("Registry did not return any AAS descriptors! Aborting.");
                        return;
                    }

                    // Have  a list of ids. Decompose into single id.
                    // Note: Parallel makes no sense, ideally only 1 result (is per AssetId)!!
                    foreach (var res in resObj)
                    {
                        // in res, have only an id. Get the descriptor
                        var id = "" + res;
                        var singleDesc = await DownloadEntityToDynamicObject(
                                BuildUriForRegistrySingleAAS(baseUri, id, encryptIds: true), 
                                runtimeOptions, allowFakeResponses);
                        if (singleDesc == null || !HasProperty(singleDesc, "endpoints"))
                            continue;

                        // refer to dedicated function
                        await FromRegistryGetAasAndSubmodels(
                            prepAas, prepSM, record, runtimeOptions, allowFakeResponses, singleDesc);
                    }
                }

                // start with single AAS?
                if (IsValidUriForRegistrySingleAAS(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    // prepare receing the descriptors
                    dynamic aasDesc = null;

                    // GET
                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
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
                        return;
                    }

                    // refer to dedicated function
                    var res = await FromRegistryGetAasAndSubmodels(
                                prepAas, prepSM, record, runtimeOptions, allowFakeResponses, aasDesc);
                    if (!res)
                    {
                        runtimeOptions?.Log?.Error("Error retrieving AAS from registry! Aborting.");
                        return;
                    }
                }
            }

            //
            // REPO
            //

            if (record.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
            {
                // start with a list of AAS or Submodels (very similar)
                var isAllAAS = IsValidUriForRepoAllAAS(fullItemLocation);
                var isAllSM = IsValidUriForRepoAllSubmodel(fullItemLocation);
                if (isAllAAS || isAllSM)
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDone: (ms, contentFn) =>
                        {
                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                if (node["result"] is JsonArray resChilds
                                    && resChilds.Count > 0)
                                {
                                    int childsToSkip = Math.Max(0, record.PageSkip);
                                    bool firstNonSkipped = true;

                                    foreach (var n2 in resChilds)
                                        // second try to reduce side effects
                                        try
                                        {
                                            if (childsToSkip > 0)
                                            {
                                                childsToSkip--;
                                                continue;
                                            }

                                            // on last child, attach side info for fetch prev/ next cursor
                                            AasIdentifiableSideInfo si = null;
                                            if (firstNonSkipped && record.PageOffset > 0)
                                                si = new AasIdentifiableSideInfo()
                                                {
                                                    IsStub = false,
                                                    ShowCursorAbove = true
                                                };
                                            firstNonSkipped = false;

                                            if (n2 == resChilds.Last() && record.PageLimit > 0)
                                                si = new AasIdentifiableSideInfo()
                                                {
                                                    IsStub = false,
                                                    ShowCursorBelow = true
                                                };

                                            // add
                                            if (isAllAAS)
                                                prepAas.Add(
                                                    Jsonization.Deserialize.AssetAdministrationShellFrom(n2),
                                                    si);
                                            if (isAllSM)
                                                prepSM.Add(
                                                    Jsonization.Deserialize.SubmodelFrom(n2),
                                                    si);
                                        }
                                        catch (Exception ex)
                                        {
                                            runtimeOptions?.Log?.Error(ex, "Parsing single AAS of list of all AAS");
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
                                runtimeOptions?.Log?.Error(ex, "Parsing list of all AAS");
                            }
                        });
                }

                // start with single AAS?
                if (IsValidUriForRepoSingleAAS(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDone: (ms, contentFn) =>
                        {
                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                prepAas.Add(Jsonization.Deserialize.AssetAdministrationShellFrom(node), null);
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                            }
                        });
                }

                // start with Submodel?
                if (IsValidUriForRepoSingleSubmodel(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
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
                if (IsValidUriForRepoSingleCD(fullItemLocation))
                {
                    // ok
                    operationFound = true;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                        sourceUri: new Uri(fullItemLocation),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
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

                // start with a query?
                if (IsValidUriForRepoQuery(fullItemLocation))
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
                        return;
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
                        sourceUri: new Uri(quri.GetLeftPart(UriPartial.Path)),
                        requestBody: jsonQuery,
                        requestContentType: "application/json",
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDone: (ms, contentFn) =>
                        {
                            try
                            {
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
                        return;
                    }

                    // only makes sense, if query returns something
                    if (fetchItems.Count < 1)
                    {
                        Log.Singleton.Info(StoredPrint.Color.Blue, "Query resulted in zero elements, " +
                            "which could be fetched. Aborting!");
                        return;
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
                            await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                                sourceUri: loc,
                                allowFakeResponses: allowFakeResponses,
                                runtimeOptions: runtimeOptions,
                                lambdaDownloadDone: (ms, contentFn) =>
                                {
                                    try
                                    {
                                        var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                        if (fi.Type == FetchItemType.SmUrl || fi.Type == FetchItemType.SmId)
                                            prepSM.Add(Jsonization.Deserialize.SubmodelFrom(node), null);
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
                if (record?.AutoLoadSubmodels ?? false)
                {
                    var lrs = env.FindAllSubmodelReferences(onlyNotExisting: true).ToList();

                    await Parallel.ForEachAsync(lrs,
                        new ParallelOptions() { MaxDegreeOfParallelism = Options.Curr.MaxParallelOps },
                        async (lr, token) =>
                        {
                            if (record?.AutoLoadOnDemand ?? true)
                            {
                                // side info level 1
                                lock (prepSM)
                                {
                                    prepSM.Add(null, new AasIdentifiableSideInfo()
                                    {
                                        IsStub = true,
                                        StubLevel = AasIdentifiableSideInfoLevel.IdOnly,
                                        Id = lr.Reference.Keys[0].Value
                                    });
                                }
                            }
                            else
                            {
                                // no side info => full element
                                await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                                    sourceUri: BuildUriForRepoSingleSubmodel(baseUri, lr.Reference),
                                    allowFakeResponses: allowFakeResponses,
                                    runtimeOptions: runtimeOptions,
                                    lambdaDownloadDone: (ms, contentFn) =>
                                    {
                                        try
                                        {
                                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                            lock (prepSM)
                                            {
                                                prepSM.Add(Jsonization.Deserialize.SubmodelFrom(node), null);
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
                if (record?.AutoLoadThumbnails ?? false)
                    await Parallel.ForEachAsync(env.AllAssetAdministrationShells(),
                        new ParallelOptions() { MaxDegreeOfParallelism = Options.Curr.MaxParallelOps },
                        async (aas, token) =>
                        {
                            await PackageHttpDownloadUtil.HttpGetToMemoryStreamOLD(
                                sourceUri: BuildUriForRepoAasThumbnail(baseUri, aas.Id),
                                allowFakeResponses: allowFakeResponses,
                                runtimeOptions: runtimeOptions,
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
                        });

            }

            //
            // FINALIZE
            //

            // any operation found?
            if (!operationFound)
            {
                runtimeOptions?.Log?.Error("Did not found any matching operation in location to " +
                    "execute on Registry or Repository! Location was: {0}",
                    fullItemLocation);
                return;
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
            EnvDynPack?.SetContext(new PackageContainerHttpRepoSubsetFetchContext()
            {
                Record = record,
                Cursor = cursor
            });
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

            // public string BaseAddress = "https://cloudrepo.aas-voyager.com/";
            // public string BaseAddress = "https://eis-data.aas-voyager.com/";
            // public string BaseAddress = "http://smt-repo.admin-shell-io.com/";
            public string BaseAddress = "https://techday2-registry.admin-shell-io.com/";

            // public BaseTypeEnum BaseType = BaseTypeEnum.Repository;
            public BaseTypeEnum BaseType = BaseTypeEnum.Registry;

            public bool GetAllAas;

            public bool GetSingleAas;
            // public string AasId = "https://new.abb.com/products/de/2CSF204101R1400/aas";
            public string AasId = "";
            // public string AasId = "https://phoenixcontact.com/qr/2900542/1/aas/1B";

            public bool GetAasByAssetLink = true;
            public string AssetId = "https://pk.harting.com/?.20P=ZSN1";

            public bool GetAllSubmodel;

            public bool GetSingleSubmodel;
            // public string SmId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvc20vMjAxNV82MDIwXzMwMTJfMDU4NQ==";
            public string SmId = "";

            public bool GetSingleCD;
            public string CdId;

            public bool ExecuteQuery;
            public string QueryScript = "";
            // public string QueryScript = "{\r\n  searchSMs(\r\n    expression: \"\"\"$LOG\r\n     filter=\r\n      or(\r\n        str_contains(sm.IdShort, \"Technical\"),\r\n        str_contains(sm.IdShort, \"Nameplate\")\r\n      )\r\n   \"\"\"\r\n  )\r\n  {\r\n    url\r\n    smId\r\n  }\r\n}";
            // public string QueryScript = "{\r\n  searchSMs(\r\n    expression: \"\"\"$LOG$QL\r\n          ( contains(sm.idShort, \"Technical\") and\r\n          sme.value ge 100 and\r\n          sme.value le 200 )\r\n        or\r\n          ( contains(sm.idShort, \"Nameplate\") and\r\n          contains(sme.idShort,\"ManufacturerName\") and\r\n          not(contains(sme.value,\"Phoenix\")))\r\n    \"\"\"\r\n  )\r\n  {\r\n    url\r\n    smId\r\n  }\r\n}";

            public bool AutoLoadSubmodels = true;
            public bool AutoLoadCds = true;
            public bool AutoLoadThumbnails = true;
            public bool AutoLoadOnDemand = true;
            public bool EncryptIds = true;
            public bool StayConnected;

            /// <summary>
            /// Pagenation. Limit to <c>n</c> resulsts.
            /// </summary>
            public int PageLimit = 15;

            /// <summary>
            /// When fetching, skip first <c>n</c> elements of the results
            /// </summary>
            public int PageSkip = 0;

            /// <summary>
            /// This offset in elements is computed by this client by "counting". It does NOT come form
            /// the server!
            /// </summary>
            public int PageOffset;

            public void SetQueryChoices(int choice)
            {
                GetAllAas = (choice == 1);
                GetSingleAas = (choice == 2);
                GetAasByAssetLink = (choice == 3);
                GetAllSubmodel = (choice == 4);
                GetSingleSubmodel = (choice == 5);
                GetSingleCD = (choice == 6);
                ExecuteQuery = (choice == 7);
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
                    if (GetAllSubmodel) res = "GetAllSubmodels";
                    if (GetSingleSubmodel) res = "GetSubmodelById";
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
                LoadResident = baseOpt.LoadResident;
                StayConnected = baseOpt.StayConnected;
                UpdatePeriod = baseOpt.UpdatePeriod;

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
                    return uri.ToString();
                }

                // Single AAS?
                if (record.GetSingleAas)
                {
                    var uri = BuildUriForRepoSingleAAS(baseUri, record.AasId, encryptIds: record.EncryptIds);
                    return uri.ToString();
                }

                // All Submodels?
                if (record.GetAllSubmodel)
                {
                    // if a skip has been requested, these AAS need to be loaded, as well
                    var uri = BuildUriForRepoAllSubmodel(baseUri, record.PageLimit + record.PageSkip, cursor);
                    return uri.ToString();
                }

                // Single Submodel?
                if (record.GetSingleSubmodel)
                {
                    var uri = BuildUriForRepoSingleSubmodel(baseUri, record.SmId, encryptIds: record.EncryptIds);
                    return uri.ToString();
                }

                // Single CD?
                if (record.GetSingleCD)
                {
                    var uri = BuildUriForRepoSingleCD(baseUri, record.CdId, encryptIds: record.EncryptIds);
                    return uri.ToString();
                }

                // Query?
                if (record.ExecuteQuery)
                {
                    var uri = BuildUriForRepoQuery(baseUri, record.QueryScript);
                    return uri.ToString();
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
                    return uri.ToString();
                }

                // Single AAS?
                if (record.GetSingleAas)
                {
                    var uri = BuildUriForRegistrySingleAAS(baseUri, record.AasId, encryptIds: record.EncryptIds);
                    return uri.ToString();
                }

                // Single AAS?
                if (record.GetAasByAssetLink)
                {
                    var uri = BuildUriForRegistryAasByAssetLink(baseUri, record.AssetId, encryptIds: record.EncryptIds);
                    return uri.ToString();
                }
            }

            // 
            // END
            //

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
                                    padding: new AnyUiThickness(0, -1, 0, -3)),
                                minWidth: 200, maxWidth: 200),
                                (i) => { record.BaseType = (ConnectExtendedRecord.BaseTypeEnum)i; });

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 1,
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

                    // AAS(es) by asset link
                    AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 0,
                                    content: "AAS by AssetId",
                                    isChecked: record.GetAasByAssetLink,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(3);
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
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(4);
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
                            (o) => {
                                if ((bool)o)
                                    record.SetQueryChoices(5);
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
                                    record.SetQueryChoices(6);
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
                                    record.SetQueryChoices(7);
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

                    // give back
                    return g;
                });

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return false;

            // ok
            return true;
        }

        protected class UploadAssistantJobRecord
        {
            // public string BaseAddress = "https://cloudrepo.aas-voyager.com/";
            public string BaseAddress = "https://eis-data.aas-voyager.com/";
            // public string BaseAddress = "http://smt-repo.admin-shell-io.com/";
            // public string BaseAddress = "https://techday2-registry.admin-shell-io.com/";

            public ConnectExtendedRecord.BaseTypeEnum BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository;
            // public ConnectExtendedRecord.BaseTypeEnum BaseType = ConnectExtendedRecord.BaseTypeEnum.Registry;

            public bool IncludeSubmodels = true;
            public bool IncludeCDs = true;

            public bool OverwriteIfExist = true;
        }

        protected class UploadAssistantElement
        {
            public string TypeName = "";
            public string Id = "";
            public string IdShort = "";
            public string VersionServer = "";
            public string VersionLocal = "";

            public bool Upload = true;
        }

        protected class UploadAssistantElementsRecord
        {
            public List<UploadAssistantElement> Elements = new List<UploadAssistantElement>();
        }

        public static async Task<bool> PerformUploadAssistant(
            AasxMenuActionTicket ticket,
            AnyUiContextBase displayContext,
            string caption,
            AdminShellPackageEnvBase packEnv,
            IEnumerable<Aas.IIdentifiable> idfs,
            PackCntRuntimeOptions runtimeOptions = null)
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

            // Screen 1 : Data
            var recordJob = new UploadAssistantJobRecord();
            var uc = new AnyUiDialogueDataModalPanel(caption);
            uc.ActivateRenderPanel(recordJob,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                // extraButtons: new[] { "A", "B" },
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

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 1,
                                    text: $"{recordJob.BaseAddress}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { recordJob.BaseAddress = s; });

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

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return false;

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
                        // Type, IdShort, Id, Ver. Local, Exists?, Ver. Server
                        "" + idf?.GetSelfDescription()?.ElementAbbreviation,
                        "" + idf?.IdShort,
                        "" + idf?.Id, 
                        "" + (idf?.Administration?.ToStringExtended(1) ?? "-"),
                        "New?",
                        "-"
                    }).ToList()
                });

            // ask server
            if (recordJob.BaseType == ConnectExtendedRecord.BaseTypeEnum.Repository)
            {
                // for the Repo, there is simply no other chance than to ask for existence of
                // an Identifiable that to try downloading it ..

                var baseUri = new Uri(recordJob.BaseAddress);

                var numRes = await DownloadListOfIdentifiables<Aas.ISubmodel, AnyUiDialogueDataGridRow>(
                        rows,
                        lambdaGetLocation: (row) =>
                        {
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
                        runtimeOptions: runtimeOptions,
                        allowFakeResponses: false,
                        lambdaDownloadDoneOrFail: (code, sm, contentFn, si) =>
                        {
                            // error by HTTP?
                            if (code != HttpStatusCode.OK)
                            {
                                return;
                            }

                            // error by no data available?
                            if (false)
                            {

                            }
                        });
            }

            // show list of elements
            var uc2 = new AnyUiDialogueDataSelectFromDataGrid(
                        "Select element(s) to be uploaded ..",
                        maxWidth: 9999);

            uc2.ColumnDefs = AnyUiListOfGridLength.Parse(new[] { "50:", "1*", "3*", "70:", "70:", "70:" });
            uc2.ColumnHeaders = new[] { "Type", "IdShort", "Id", "V.Local", "Exist?", "V.Server" };
            uc2.Rows = rows.ToList();

            if (!(await displayContext.StartFlyoverModalAsync(uc2)))
                return false;

            // ok
            return true;
        }
    }

}
