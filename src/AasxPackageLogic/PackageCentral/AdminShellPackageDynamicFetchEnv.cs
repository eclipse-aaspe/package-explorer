/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using AdminShellNS.DiaryData;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_1;

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

        public enum IndicateFetchPrevType { None = 0x0, AllAas = 0x01, AllSm = 0x02, AllCd = 0x04 };
        public IndicateFetchPrevType IndicateFetchPrev = IndicateFetchPrevType.None; 

        public AdminShellPackageDynamicFetchEnv(
            PackCntRuntimeOptions runtimeOptions = null,
            Uri baseUri = null) : base()
        {
            _runtimeOptions = runtimeOptions;
            _defaultRepoBaseUri = baseUri;
        }

        public PackCntRuntimeOptions RuntimeOptions { get { return _runtimeOptions; } }

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
                    var loc = (si.StubLevel >= AasIdentifiableSideInfoLevel.IdWithEndpoint && si.QueriedEndpoint != null)
                        ? si.QueriedEndpoint
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
                    ids, new ParallelOptions() { MaxDegreeOfParallelism = Options.Curr.MaxParallelReadOps },
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
            Func<Uri, Aas.IIdentifiable, Task<Uri>> lambdaGetBaseUriForNewIdentifiables = null,
            bool clearTaintedFlags = true) where T : Aas.IIdentifiable
        {
            var list = listOfIdf as OnDemandListIdentifiable<T>;
            if (list == null)
                return 0;

            var count = 0;
            for (int listNdx = 0; listNdx < list.Count(); listNdx++)
            {
                // access
                var idf = list[listNdx];
                if (idf == null)
                    // surprising :-/
                    continue;

                // Only concern on tainted data
                var tidf = idf as ITaintedData;
                if (tidf?.TaintedData != null && tidf.TaintedData.Tainted == null)
                    continue;

                // Prepare building URI for REST ressource
                Uri uri = null;
                Uri futureEP = null;
                bool usePost = false;
                bool buildSideInfo = false;

                // Get the side info. 
                var si = list.GetSideInfo(listNdx);
                if (si == null)
                {
                    // no side info, so this might be a new Identifiable
                    // Investigate further, need to get a "positive info" from tghe user (lambda)
                    Uri workBaseUri = null;
                    if (lambdaGetBaseUriForNewIdentifiables != null) 
                        workBaseUri = await lambdaGetBaseUriForNewIdentifiables.Invoke(_defaultRepoBaseUri, idf);
                    if (workBaseUri == null || !workBaseUri.IsAbsoluteUri)
                    {
                        // skip Identifiable
                        continue;
                    }
                    else
                    {
                        // build the uri
                        // assume no collision, so use POST
                        usePost = true;
                        uri = PackageContainerHttpRepoSubset.BuildUriForRepoSingleIdentifiable<T>(
                                workBaseUri, idf.Id, usePost: true, encryptIds: true);
                        buildSideInfo = true;
                        futureEP = PackageContainerHttpRepoSubset.BuildUriForRepoSingleIdentifiable<T>(
                                workBaseUri, idf.Id, usePost: false, encryptIds: true);

                        if (uri == null)
                        {
                            // skip Identifiable
                            continue;
                        }
                    }
                }

                if (si != null && si.IsStub)
                {
                    // Continue (for the time being), if side info present (was dynamically fetched) but
                    // is a stub to a registry
                    continue;
                }

                if (uri == null)
                {
                    // try use the existing endpoint
                    if (si.StubLevel >= AasIdentifiableSideInfoLevel.IdWithEndpoint && si.DesignatedEndpoint != null)
                        uri = si.DesignatedEndpoint;
                    else
                    if (si.StubLevel >= AasIdentifiableSideInfoLevel.IdWithEndpoint && si.QueriedEndpoint != null)
                        uri = si.QueriedEndpoint;
                    else
                        uri = PackageContainerHttpRepoSubset
                                .BuildUriForRepoSingleIdentifiable<T>(_defaultRepoBaseUri, idf.Id, encryptIds: true);
                }

                if (uri == null)
                {
                    _runtimeOptions?.Log?.Error("Could not determine URI for saving Identifiable {0} with id {1}",
                        typeof(T).Name, idf.Id);
                    continue;
                }

                // serialize to memory stream
                var res2 = await PackageHttpDownloadUtil.HttpPutPostIdentifiable(
                    reUseClient: null,
                    usePost: usePost,
                    idf: idf,
                    destUri: uri);
                
                if (res2 == null || !((int)res2.Item1 >= 200 && (int)res2.Item1 <= 299))
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

                    // may be new, build side info
                    if (buildSideInfo && futureEP != null)
                    {
                        // build side info
                        si = new AasIdentifiableSideInfo()
                        {
                            IsStub = false,
                            StubLevel = AasIdentifiableSideInfoLevel.IdWithEndpoint,
                            Id = idf.Id,
                            IdShort = idf.IdShort,
                            QueriedEndpoint = futureEP,
                            DesignatedEndpoint = futureEP
                        };

                        // add to list
                        list.SetSideInfo(listNdx, si);
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// As the requirement is, that a list of Identifiables is null, if empty and most of the
        /// functionality (AasxCSharpLibrary and beyond) is not aware, that the list of Identifiables
        /// should be <code>OnDemandListIdentifiable<T></code>, these functionality might create an
        /// ordinary list of Identifiables, not fullfilling the requirements.
        /// This function tries to fix it.
        /// </summary>
        public void FixIdfListsToBeOnDemandLists()
        {
            if (_aasEnv.AssetAdministrationShells is not null
                && _aasEnv.AssetAdministrationShells is not OnDemandListIdentifiable<Aas.IAssetAdministrationShell>)
            {
                // fix
                _aasEnv.AssetAdministrationShells = new OnDemandListIdentifiable<Aas.IAssetAdministrationShell>(
                    _aasEnv.AssetAdministrationShells);
            }

            if (_aasEnv.Submodels is not null
                && _aasEnv.Submodels is not OnDemandListIdentifiable<Aas.ISubmodel>)
            {
                // fix
                _aasEnv.Submodels = new OnDemandListIdentifiable<Aas.ISubmodel>(
                    _aasEnv.Submodels);
            }

            if (_aasEnv.ConceptDescriptions is not null
                && _aasEnv.ConceptDescriptions is not OnDemandListIdentifiable<Aas.IConceptDescription>)
            {
                // fix
                _aasEnv.ConceptDescriptions = new OnDemandListIdentifiable<Aas.IConceptDescription>(
                    _aasEnv.ConceptDescriptions);
            }
        }

        public async Task<int> TrySaveAllTaintedIdentifiables(
            bool allAas = true,
            bool allSubmodels = true,
            bool allCDs = true,
            Func<Uri, Aas.IIdentifiable, Task<Uri>> lambdaGetBaseUriForNewIdentifiables = null)
        {
            var count = 0;

            FixIdfListsToBeOnDemandLists();

            if (allAas) count += await TrySaveAllTaintedIdentifiablesOf<Aas.IAssetAdministrationShell>(
                _aasEnv?.AssetAdministrationShells,
                lambdaGetBaseUriForNewIdentifiables: lambdaGetBaseUriForNewIdentifiables);

            if (allSubmodels) count += await TrySaveAllTaintedIdentifiablesOf<Aas.ISubmodel>(
                _aasEnv?.Submodels,
                lambdaGetBaseUriForNewIdentifiables: lambdaGetBaseUriForNewIdentifiables);

            if (allCDs) count += await TrySaveAllTaintedIdentifiablesOf<Aas.IConceptDescription>(
                _aasEnv?.ConceptDescriptions,
                lambdaGetBaseUriForNewIdentifiables: lambdaGetBaseUriForNewIdentifiables);

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

        public void RenameThumbnailData(string oldId, string newId)
        {
            // access
            if (oldId?.HasContent() != true || newId?.HasContent() != true)
                return;
            // rename
            lock (_thumbStreamPerAasId)
            {
                if (_thumbStreamPerAasId.ContainsKey(oldId))
                {
                    _thumbStreamPerAasId[newId] = _thumbStreamPerAasId[oldId];
                    _thumbStreamPerAasId.Remove(oldId);
                }
            }
        }

        public override async Task<byte[]> GetBytesFromPackageOrExternalAsync(
            string uriString,
            string aasId = null,
            string smId = null,
            ISecurityAccessHandler secureAccess = null,
            string idShortPath = null)
        {
            // access
            if (uriString?.HasContent() != true)
                return null;

            // check if it is indeed subject to HTTP stream/ external file or is an attachment
            if (!AdminShellUtil.CheckIfUriIsAttachment(uriString))
            {
                // should be HTTP stream/ external file!
                var absBytes = await base.GetBytesFromPackageOrExternalAsync(uriString, secureAccess: secureAccess);
                if (absBytes != null)
                    return absBytes;

                return null;
            }

            // ok, try to load as attachment from the server
            if (aasId?.HasContent() != true || smId?.HasContent() != true || idShortPath?.HasContent() != true
                || _defaultRepoBaseUri == null)
                return null;

            // try contact repo
            var attLoc = PackageContainerHttpRepoSubset.BuildUriForRepoSingleSubmodelAttachment(
                _defaultRepoBaseUri, 
                aasId: null /* aasId */,        // BaSyx seems to expect ONLY at Submodel interface
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
