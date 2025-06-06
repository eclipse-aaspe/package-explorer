/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// AasxFileRepository, which is held synchronized with a AAS REST repository interface. 
    /// Just a deriative from <c>PackageContainerListBase</c>. Only small additions.
    /// </summary>
    public class PackageContainerListHttpRestRepository : PackageContainerListHttpRestBase
    {
        //
        // Member
        //

        private PackageConnectorHttpRest _connector;

        [JsonIgnore]
        public string ServerStatus { get; private set; } = "Status unknown!";

        /// <summary>
        /// REST endpoint of the AAS repository, that is, without <c>/shells</c> etc. but
        /// with e.g. <c>/api/v3.0/</c>
        /// </summary>
        public Uri Endpoint;

        /// <summary>
        /// HTTP header attributes to be fed into the different HTTP get/ put/ post ..
        /// functions of the registry/ repository calls.
        /// Note: To be set by "HttpHeaderAttributes" (serialization).
        /// </summary>
        [JsonIgnore] 
        public HttpHeaderData HttpHeaderData = null;

        /// <summary>
        /// HTTP header attributes to be fed into the different HTTP get/ put/ post ..
        /// functions of the registry/ repository calls.
        /// </summary>
        public string HttpHeaderAttributes
        {
            set
            {
                HttpHeaderData = new HttpHeaderData(value);
            }
        }

        //
        // Constructor
        //

        public PackageContainerListHttpRestRepository(string location)
        {
            // infos
            this.Header = "AAS API Repository";
            this.IsStaticList = false;

            // always have a location
            Endpoint = (location == null) ? null : new Uri(location);

            // directly set endpoint
            // Note: later
            // _connector = new PackageConnectorHttpRest(null, Endpoint);
        }

        //
        // Outer funcs
        //

        /// <summary>
        /// This functions asks the AAS REST repository on given location, which AasIds would be availble.
        /// Using the AasIds, details are retrieved for each inidivudal AAS and synchronized with the 
        /// repository.
        /// Note: for the time being, the list of file items is recreated, but not synchronized
        /// Note: due to the nature of long-lasting actions, this is by design async!
        /// </summary>
        /// <returns>If a successfull retrieval could be made</returns>
        public async Task<bool> SyncronizeFromServerAsync()
        {
            if (true != _connector?.IsValid())
                return false;

            await Task.Yield();

#if old_implementation
            // try get a list of items from the connector
            var items = await _connector.GenerateRepositoryFromEndpointAsync();
            // just re-set
            FileMap.Clear();
            foreach (var fi in items)
                if (fi != null)
                {
                    FileMap.Add(fi);
                    fi.ContainerList = this;
                }
#endif

            // ok
            return true;
        }

        override public async Task<bool> PrepareStatus()
        {
            // for exception handling, mark as potential flaw!
            ServerStatus = "Error retrieving /description !";

            try
            {
                var resObj = await PackageHttpDownloadUtil.DownloadEntityToDynamicObject(
                        PackageContainerHttpRepoSubset.BuildUriForDescription(Endpoint),
                        runtimeOptions: null,
                        doNotLogExceptions: true);

                if (resObj == null || !PackageContainerHttpRepoSubset.HasProperty(resObj, "profiles"))
                    return false;

                // carefully access
                var abbrevs = new List<string>();
                foreach (var pr in resObj.profiles)
                {
                    // carefully access
                    string profile = ("" + pr).Trim();
                    if (profile?.HasContent() != true)
                        continue;

                    // find abbrevs
                    var pd = PackageContainerHttpRepoSubset.FindProfileDescription(profile);
                    if (pd != null)
                        abbrevs.Add(pd.Abbreviation);
                }

                if (abbrevs.Count > 0)
                    ServerStatus = "Profiles: " + string.Join(", ", abbrevs);
                else
                    ServerStatus = "No profiles described!";
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return false;
        }

        override public string GetMultiLineStatusInfo()
        {
            return "REPOSITORY at base " + Endpoint + "\n" +
                "" + ServerStatus;
        }

        /// <summary>
        /// Retrieve the full location specification of the item w.r.t. to persistency container 
        /// (filesystem, HTTP, ..)
        /// </summary>
        /// <returns></returns>
        public override string GetFullItemLocation(string location)
        {
            // access
            if (location == null)
                return null;

            // there is a good chance, that fi.Location is already absolute
            var ll = location.Trim().ToLower();
            if (ll.StartsWith("http://") || ll.StartsWith("https://"))
                return location;

            // TODO (MIHO, 2021-01-08): check, how to make absolute
            throw new NotImplementedException("AasxFileRepoHttpRestRepository.GetFullItemLocation()");
        }

        public override async Task<PackageContainerRepoItem> FindByAssetId(string aid)
        {
            if (!ToBeQueried)
                return null;

            try
            {
                // try to do this natively
                var fil = PackageContainerHttpRepoSubset.BuildUriForRegistryAasByAssetLink(Endpoint, aid);

                // prepare receiving the descriptors/ ids
                var idsObj = await PackageHttpDownloadUtil.DownloadEntityToDynamicObject(
                    fil, runtimeOptions: null);

                // Note: GetAllAssetAdministrationShellIdsByAssetLink only returns a list of ids
                if (idsObj == null)
                    return null;

                // Have a list of ids. Decompose into single id.
                // Note: Parallel makes no sense, ideally only 1 result (is per AssetId)!!
                foreach (var id in idsObj) {
                    var ri = new PackageContainerRepoItem()
                    {
                        Location = PackageContainerHttpRepoSubset.BuildUriForRepoSingleAAS(Endpoint, "" + id)?.ToString()
                    };
                    return ri;
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
            return null;
        }

    }
}
