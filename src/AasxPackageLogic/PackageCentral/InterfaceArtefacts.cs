/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxOpenIdClient;
using AdminShellNS;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_1;
using Extensions;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Basic stub of an AAS endpoint descriptor; sufficient enough to do some
    /// upload/ download actions.
    /// </summary>
    public class InterfaceArtefacts
    {
        public partial class ProtocolInformation
        {
            /// <summary>
            /// Gets or Sets Href
            /// </summary>
            [JsonProperty(PropertyName = "href")]
            public string Href { get; set; }

            /// <summary>
            /// Gets or Sets EndpointProtocol
            /// </summary>
            [JsonProperty(PropertyName = "endpointProtocol")]
            public string EndpointProtocol { get; set; }

            /// <summary>
            /// Gets or Sets EndpointProtocolVersion
            /// </summary>
            [JsonProperty(PropertyName = "endpointProtocolVersion")]
            public List<string> EndpointProtocolVersion { get; set; }

            /// <summary>
            /// Gets or Sets Subprotocol
            /// </summary>
            [JsonProperty(PropertyName = "subprotocol")]
            public string Subprotocol { get; set; }

            /// <summary>
            /// Gets or Sets SubprotocolBody
            /// </summary>
            [JsonProperty(PropertyName = "subprotocolBody")]
            public string SubprotocolBody { get; set; }

            /// <summary>
            /// Gets or Sets SubprotocolBodyEncoding
            /// </summary>
            [JsonProperty(PropertyName = "subprotocolBodyEncoding")]
            public string SubprotocolBodyEncoding { get; set; }

            /// <summary>
            /// Gets or Sets SecurityAttributes
            /// </summary>
            //[JsonProperty(PropertyName = "securityAttributes")]
            //public List<ProtocolInformationSecurityAttributes>? SecurityAttributes { get; set; }
        }

        public partial class Endpoint
        {
            /// <summary>
            /// Gets or Sets _Interface
            /// </summary>
            [JsonProperty(PropertyName = "interface")]
            public string Interface { get; set; }

            /// <summary>
            /// Gets or Sets ProtocolInformation
            /// </summary>
            [JsonProperty(PropertyName = "protocolInformation")]
            public ProtocolInformation ProtocolInformation { get; set; }
        }

        public class Descriptor
        {
            /// <summary>
            /// Gets or Sets Description
            /// </summary>
            [JsonProperty(PropertyName = "description")]
            public List<Aas.ILangStringTextType> Description { get; set; }

            /// <summary>
            /// Gets or Sets DisplayName
            /// </summary>
            [JsonProperty(PropertyName = "displayName")]
            public List<Aas.ILangStringNameType> DisplayName { get; set; }

            /// <summary>
            /// Gets or Sets Extensions
            /// </summary>
            [JsonProperty(PropertyName = "extensions")]
            public List<Aas.IExtension> Extensions { get; set; }
        }

        public partial class SubmodelDescriptor : Descriptor
        {
            /// <summary>
            /// Gets or Sets Administration
            /// </summary>
            [JsonProperty(PropertyName = "administration")]
            public Aas.IAdministrativeInformation Administration { get; set; }

            /// <summary>
            /// Gets or Sets Endpoints
            /// </summary>
            [JsonProperty(PropertyName = "endpoints")]
            public List<Endpoint> Endpoints { get; set; }

            /// <summary>
            /// Gets or Sets IdShort
            /// </summary>
            [JsonProperty(PropertyName = "idShort")]
            public string IdShort { get; set; }

            /// <summary>
            /// Gets or Sets Id
            /// </summary>
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            /// <summary>
            /// Gets or Sets SemanticId
            /// </summary>
            [JsonProperty(PropertyName = "semanticId")]
            public Aas.IReference SemanticId { get; set; }

            /// <summary>
            /// Gets or Sets SupplementalSemanticId
            /// </summary>
            [JsonProperty(PropertyName = "supplementalSemanticId")]
            public List<Aas.IReference> SupplementalSemanticId { get; set; }
        }

        public partial class AssetAdministrationShellDescriptor : Descriptor
        {
            /// <summary>
            /// Gets or Sets Administration
            /// </summary>
            [JsonProperty(PropertyName = "administration")]
            public Aas.IAdministrativeInformation Administration { get; set; }

            /// <summary>
            /// Gets or Sets AssetKind
            /// </summary>
            [JsonProperty(PropertyName = "assetKind")]
            public Aas.AssetKind AssetKind { get; set; }

            /// <summary>
            /// Gets or Sets AssetType
            /// </summary>
            [JsonProperty(PropertyName = "assetType")]
            public string AssetType { get; set; }

            /// <summary>
            /// Gets or Sets Endpoints
            /// </summary>
            [JsonProperty(PropertyName = "endpoints")]
            public List<Endpoint> Endpoints { get; set; }

            /// <summary>
            /// Gets or Sets GlobalAssetId
            /// </summary>
            [JsonProperty(PropertyName = "globalAssetId")]
            public string GlobalAssetId { get; set; }

            /// <summary>
            /// Gets or Sets IdShort
            /// </summary>
            [JsonProperty(PropertyName = "idShort")]
            public string IdShort { get; set; }

            /// <summary>
            /// Gets or Sets Id
            /// </summary>
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            /// <summary>
            /// Gets or Sets SpecificAssetIds
            /// </summary>
            [JsonProperty(PropertyName = "specificAssetIds")]
            public List<Aas.ISpecificAssetId> SpecificAssetIds { get; set; }

            /// <summary>
            /// Gets or Sets SubmodelDescriptors
            /// </summary>
            [JsonProperty(PropertyName = "submodelDescriptors")]
            public List<SubmodelDescriptor> SubmodelDescriptors { get; set; }
        }

        public static AssetAdministrationShellDescriptor BuildAasDescriptor(
            BaseUriDict baseUris,
            Aas.IAssetAdministrationShell aas,
            IEnumerable<Aas.ISubmodel> submodels)
        {
            // access
            if (baseUris == null || aas == null)
                return null;

            // copy "simple" information
            var res = new AssetAdministrationShellDescriptor()
            {
                Administration = aas.Administration?.Copy(),
                Description = aas.Description?.Copy(),
                DisplayName = aas.DisplayName?.Copy(),
                IdShort = aas.IdShort,
                Id = aas.Id
            };

            if (aas.AssetInformation != null)
            {
                res.AssetKind = aas.AssetInformation.AssetKind;
                res.AssetType = aas.AssetInformation.AssetType;
                res.GlobalAssetId = aas.AssetInformation.GlobalAssetId;

                if (aas.AssetInformation.SpecificAssetIds != null)
                {
                    res.SpecificAssetIds = aas.AssetInformation.SpecificAssetIds.Copy();
                }
            }

            // build AAS endpoint
            res.Endpoints = new();
            var resEp = new Endpoint()
            {
                Interface = "AAS-3.0",
                ProtocolInformation = new ProtocolInformation()
                {
                    Href = PackageContainerHttpRepoSubset.BuildUriForRepoSingleAAS(
                        baseUris.GetBaseUriForAasRepo(), aas.Id, encryptIds: true)?.ToString(),
                    EndpointProtocol = "http"
                }
            };
            res.Endpoints.Add(resEp);

            // add Submodel information
            if (submodels != null && submodels.Count() > 0)
            {
                res.SubmodelDescriptors = new();
                foreach (var sm in submodels)
                {
                    var resSm = new SubmodelDescriptor()
                    {
                        Administration = sm.Administration?.Copy(),
                        Description = sm.Description?.Copy(),
                        DisplayName = sm.DisplayName?.Copy(),
                        IdShort = sm.IdShort,
                        Id = sm.Id,
                        SemanticId = sm.SemanticId?.Copy(),
                        SupplementalSemanticId = sm.SupplementalSemanticIds?.Copy()
                    };
                }
            }

            // ok
            return res;
        }
    }
}
