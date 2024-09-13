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
                await res.LoadFromSourceAsync(fullItemLocation, runtimeOptions);

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

        public static bool IsValidUriAnyMatch(string location)
        {
            return IsValidUriForAllAAS(location)
                || IsValidUriForSingleAAS(location)
                || IsValidUriForSingleSubmodel(location)
                || IsValidUriForSingleCD(location);
        }

        public override async Task LoadFromSourceAsync(
            string fullItemLocation,
            PackCntRuntimeOptions runtimeOptions = null)
        {
            //PackageHttpDownloadUtil.TryLoadFakeRequests(Assembly.GetExecutingAssembly(),
            //    "AasxPackageLogic.Resources.PackageContainerFakeAnswers.json");

            var allowFakeResponses = true;

            var baseUri = PackageHttpDownloadUtil.GetBaseUri(fullItemLocation);

            // integrate in a fresh environment
            // TODO: new kind of environment
            var env = (Aas.IEnvironment) new Aas.Environment();

            // start with AAS?
            if (IsValidUriForSingleAAS(fullItemLocation))
            {
                await PackageHttpDownloadUtil.DownloadToMemoryStream(
                    sourceUri: new Uri(fullItemLocation),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms) =>
                    {
                        try
                        {
                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                            env.Add(Jsonization.Deserialize.AssetAdministrationShellFrom(node));
                        } catch (Exception ex)
                        {
                            runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                        }
                    });
            }

            // start with Submodel?
            if (IsValidUriForSingleSubmodel(fullItemLocation))
            {
                await PackageHttpDownloadUtil.DownloadToMemoryStream(
                    sourceUri: new Uri(fullItemLocation),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms) =>
                    {
                        try
                        {
                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                            env.Add(Jsonization.Deserialize.SubmodelFrom(node));
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
                await PackageHttpDownloadUtil.DownloadToMemoryStream(
                    sourceUri: new Uri(fullItemLocation),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms) =>
                    {
                        try
                        {
                            var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                            env.Add(Jsonization.Deserialize.ConceptDescriptionFrom(node));
                        }
                        catch (Exception ex)
                        {
                            runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded ConceptDescription");
                        }
                    });
            }

            // start auto-load missing Submodels?
            if (true)
                foreach (var lr in env.FindAllSubmodelReferences(onlyNotExisting: true))
                    await PackageHttpDownloadUtil.DownloadToMemoryStream(
                    sourceUri: PackageHttpDownloadUtil.BuildUriForSubmodel(baseUri, lr.Reference),
                    allowFakeResponses: allowFakeResponses,
                    lambdaDownloadDone: (ms) =>
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

            // prototypic!!
            Env = new AdminShellPackageFileBasedEnv();
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
        }
    }

}
