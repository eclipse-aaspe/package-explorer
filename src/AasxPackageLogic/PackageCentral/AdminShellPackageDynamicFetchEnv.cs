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
using System.Linq;
using AdminShellNS.DiaryData;
using System.Text.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Base class for storing further data which led to the creation of this dynamic
    /// set of elements
    /// </summary>
    public class AdminShellPackageDynamicFetchContextBase
    {
    }

    /// <summary>
    /// This class creates a package env, which can handle dynamic loading of elements
    /// </summary>
    public class AdminShellPackageDynamicFetchEnv : AdminShellPackageEnvBase
    {
        protected PackCntRuntimeOptions _runtimeOptions = null;

        protected Uri _defaultRepoBaseUri = null;
        public Uri GetBaseUri() => _defaultRepoBaseUri;

        protected AdminShellPackageDynamicFetchContextBase _context = null;
        public AdminShellPackageDynamicFetchContextBase GetContext() => _context;
        public void SetContext(AdminShellPackageDynamicFetchContextBase cursor) { _context = cursor; }

        protected Dictionary<string, byte[]> _thumbStreamPerAasId = new Dictionary<string, byte[]>();

        public AdminShellPackageDynamicFetchEnv(
            PackCntRuntimeOptions runtimeOptions = null,
            Uri baseUri = null) : base()
        {
            _runtimeOptions = runtimeOptions;
            _defaultRepoBaseUri = baseUri;
        }

        public override bool IsOpen
        {
            get
            {
                if (AasEnv == null)
                    return false;
                var someIdf = AasEnv.AssetAdministrationShellCount() > 0
                        || AasEnv.SubmodelCount() > 0
                        || AasEnv.ConceptDescriptionCount() > 0;
                return someIdf && _defaultRepoBaseUri != null;
            }
        }

        public async Task<bool> TryFetchThumbnail(Aas.IAssetAdministrationShell aas)
        {
            // access
            if (aas?.Id?.HasContent() != true)
                return false;

            // try
            try
            {
                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                    null,
                    sourceUri: PackageContainerHttpRepoSubset.BuildUriForRepoAasThumbnail(_defaultRepoBaseUri, aas.Id),
                    runtimeOptions: _runtimeOptions,
                    lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                    {
                        if (code != HttpStatusCode.OK)
                            return;
                        AddThumbnail(aas.Id, ms.ToArray());
                    });

                // happy
                return true;
            }
            catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }

            return false;
        }

        public async Task<Aas.IIdentifiable> FindOrFetchIdentifiable(string id)
        {
            // access
            if (id?.HasContent() != true
                || !(_aasEnv is AasOnDemandEnvironment odEnv))
                return null;

            // try
            try
            {
                // find existing?
                Aas.IIdentifiable idf = _aasEnv?.FindAasById(id);
                idf = idf ?? _aasEnv?.FindSubmodelById(id);
                idf = idf ?? _aasEnv?.FindConceptDescriptionById(id);
                if (idf != null)
                    return idf;

                // try locate id in Submodels?
                var sms = _aasEnv?.Submodels as OnDemandListIdentifiable<Aas.ISubmodel>;
                var smndx = sms?.FindSideInfoIndexFromId(id);
                if (smndx.HasValue && smndx.Value >= 0)
                {
                    // side info
                    var si = sms.GetSideInfo(smndx.Value);
                    
                    // directly use id to fetch Identifiable
                    Aas.IIdentifiable res = null;

                    // build the location
                    var loc = (si.StubLevel >= AasIdentifiableSideInfoLevel.IdWithEndpoint && si.Endpoint != null)
                        ? si.Endpoint
                        : PackageContainerHttpRepoSubset.BuildUriForRepoSingleSubmodel(_defaultRepoBaseUri, id);
                    if (loc == null)
                        return null;

                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        null,
                        sourceUri: loc,
                        allowFakeResponses: _runtimeOptions?.AllowFakeResponses ?? false,
                        runtimeOptions: _runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            // fail!!
                            if (code != HttpStatusCode.OK)
                            {
                                _runtimeOptions?.Log?.Error($"Error while downloading on-demand loaded Submodel. " +
                                    $"Endpoint was expected to be available! Status: {(int)code} {code}. " +
                                    $"Location: {loc.ToString()}");
                                return;
                            }

                            try
                            {
                                // load?
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                var sm = Jsonization.Deserialize.SubmodelFrom(node);

                                // replace, side info need to be preserved!
                                lock (_aasEnv.Submodels)
                                {
                                    // data
                                    sms[smndx.Value] = sm;

                                    // side info
                                    si.IsStub = false;
                                    sms.SetSideInfo(smndx.Value, si);
                                }

                                // ok
                                res = sm;
                            }
                            catch (Exception ex)
                            {
                                _runtimeOptions?.Log?.Error(ex, "Parsing on demand loaded Submodel");
                            }
                        });

                    return res;
                }
            } catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }

            // nope
            return null;
        }

        public async Task<bool> TryFetchSpecificIds(
            IEnumerable<string> ids,
            bool useParallel = true)
        {
            var someFetched = false;
            if (useParallel)
            {
                // parallel
                await Parallel.ForEachAsync(
                    ids, new ParallelOptions() { MaxDegreeOfParallelism = Options.Curr.MaxParallelOps },
                    async (id, token) =>
                    {
                        var idf = await FindOrFetchIdentifiable(id);
                        if (idf != null)
                            someFetched = true;
                    });
            }
            else
            {
                // not optimal
                foreach (var id in ids)
                {
                    var idf = await FindOrFetchIdentifiable(id);
                    if (idf != null)
                        someFetched = true;
                }
            }
            return someFetched;
        }

        protected async Task<bool> TryFetchAllMissingOf<T>(object listOfIdf) where T : Aas.IIdentifiable
        {
            var list = listOfIdf as OnDemandListIdentifiable<T>;
            if (list == null)
                return false;

            var idsToFetch = new List<string>();
            for (int i=0; i<list.Count(); i++)
            {
                var si = list.GetSideInfo(i);
                if (si != null)
                    // try fetch
                    idsToFetch.Add(si.Id);
            }

            return await TryFetchSpecificIds(idsToFetch);
        }

        public async Task<bool> TryFetchAllMissingIdentifiables(
            bool allAas = true,
            bool allSubmodels = true,
            bool allCDs = true)
        {
            var res = false;            

            if (allAas) res = res || await TryFetchAllMissingOf<Aas.IAssetAdministrationShell>(_aasEnv?.AssetAdministrationShells);
            if (allSubmodels) res = res || await TryFetchAllMissingOf<Aas.ISubmodel>(_aasEnv?.Submodels);
            if (allCDs) res = res || await TryFetchAllMissingOf<Aas.IConceptDescription>(_aasEnv?.ConceptDescriptions);

            return res;
        }

        protected async Task<int> TrySaveAllTaintedIdentifiablesOf<T>(
            object listOfIdf,
            Func<Uri, string, Uri> lambdaBuildRessourceForNoEndpoint,
            bool clearTaintedFlags = true) where T : Aas.IIdentifiable
        {
            var list = listOfIdf as OnDemandListIdentifiable<T>;
            if (list == null)
                return 0;

            var count = 0;
            for (int i = 0; i < list.Count(); i++)
            {
#if __old
                // Note: elements with side info are not relevant, as only fetched elements
                // need to be written back ..
                var si = list.GetSideInfo(i);
                if (si != null)
                    continue;
#else
                // Get the side info. Continue, if side info present (was dynamically fetched) but
                // is no stub
                var si = list.GetSideInfo(i);
                if (si == null || si.IsStub)
                    continue;
#endif

                // access
                var idf = list[i];
                if (idf == null)
                    // surprising :-/
                    continue;

                // tainted? (in doubt, yes)
                var tidf = idf as ITaintedData;
                if (tidf?.TaintedData != null && tidf.TaintedData.Tainted == null)
                    continue;

                // try save, need a REST ressource. Either use the existing endpoint
                // or build the uri.
                var uri = (si.StubLevel >= AasIdentifiableSideInfoLevel.IdWithEndpoint && si.Endpoint != null)
                        ? si.Endpoint
                        : lambdaBuildRessourceForNoEndpoint(_defaultRepoBaseUri, idf.Id);
                if (uri == null)
                    continue;

                // serialize to memory stream
                var res2 = await PackageHttpDownloadUtil.HttpPutPostIdentifiable(
                    reUseClient: null,
                    idf: idf,
                    destUri: uri);
                if (res2 == null || ((res2.Item1 != HttpStatusCode.OK) && (res2.Item1 != HttpStatusCode.NoContent)))
                {
                    _runtimeOptions?.Log?.Error("Save of modified Identifiable returned error {0} for id={1} at {2}",
                        "" + ((res2 != null) ? (int)res2.Item1 : -1),
                        idf.Id,
                        uri.ToString());
                }
                else
                {
                    // clear the tainted flag
                    if (clearTaintedFlags && tidf?.TaintedData != null)
                        tidf.TaintedData.Tainted = null;
                }
            }

            return count;
        }

        public async Task<int> TrySaveAllTaintedIdentifiables(
            bool allAas = true,
            bool allSubmodels = true,
            bool allCDs = true)
        {
            var count = 0;
            
            if (allAas) count += await TrySaveAllTaintedIdentifiablesOf<Aas.IAssetAdministrationShell>(
                _aasEnv?.AssetAdministrationShells,
                (defBase, id) => PackageContainerHttpRepoSubset.BuildUriForRepoSingleAAS(defBase, id));

            if (allSubmodels) count += await TrySaveAllTaintedIdentifiablesOf<Aas.ISubmodel>(
                _aasEnv?.Submodels,
                (defBase, id) => PackageContainerHttpRepoSubset.BuildUriForRepoSingleSubmodel(defBase, id));

            if (allCDs) count += await TrySaveAllTaintedIdentifiablesOf<Aas.IConceptDescription>(
                _aasEnv?.ConceptDescriptions,
                (defBase, id) => PackageContainerHttpRepoSubset.BuildUriForRepoSingleCD(defBase, id));

            return count;
        }

        public void AddThumbnail(string aasId, byte[] content)
        {
            if (aasId?.HasContent() != true)
                return;

            lock (_thumbStreamPerAasId)
            {
                if (_thumbStreamPerAasId.ContainsKey(aasId))
                    _thumbStreamPerAasId[aasId] = content;
                else
                    _thumbStreamPerAasId.Add(aasId, content);
            }
        }

        public override byte[] GetThumbnailBytesFromAasOrPackage(string aasId)
        {
            // can serve ourself?
            if (aasId?.HasContent() == true && _thumbStreamPerAasId.ContainsKey(aasId))
                return _thumbStreamPerAasId[aasId];

            // refer to base
            return base.GetThumbnailBytesFromAasOrPackage(aasId);
        }

        public override async Task<byte[]> GetBytesFromPackageOrExternalAsync(
            string uriString,
            string aasId = null,
            string smId = null,
            string idShortPath = null)
        {
            // IMPORTANT! First try to use the base implementation to get an stream to
            // HTTP or ABSOLUTE file
            var absBytes = await base.GetBytesFromPackageOrExternalAsync(uriString);
            if (absBytes != null)
                return absBytes;

            // ok, try to load from the server
            if (aasId?.HasContent() != true || smId?.HasContent() != true || idShortPath?.HasContent() != true
                || _defaultRepoBaseUri == null)
                return null;

            // try contact repo
            var attLoc = PackageContainerHttpRepoSubset.BuildUriForRepoSingleSubmodelAttachment(
                _defaultRepoBaseUri, 
                aasId: aasId,
                smId: smId,
                idShortPath: idShortPath,
                encryptIds: true);

            try
            {
                byte[] res = null;

                await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                    null,
                    sourceUri: attLoc,
                    runtimeOptions: _runtimeOptions,
                    lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                    {
                        // error
                        if (code != HttpStatusCode.OK && code != HttpStatusCode.NoContent)
                            return;

                        // store (this is stupid!)
                        res = ms.ToArray();
                    });            

                return res;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
                return null;
            }
        }
    }

}
