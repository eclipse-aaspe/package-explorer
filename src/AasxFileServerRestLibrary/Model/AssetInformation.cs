/* 
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// AssetInformation
    /// </summary>
    [DataContract]
    public partial class AssetInformation : IEquatable<AssetInformation>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetInformation" /> class.
        /// </summary>
        /// <param name="assetKind">assetKind (required).</param>
        /// <param name="billOfMaterial">billOfMaterial.</param>
        /// <param name="globalAssetId">globalAssetId.</param>
        /// <param name="specificAssetIds">specificAssetIds.</param>
        /// <param name="thumbnail">thumbnail.</param>
        public AssetInformation(AssetKind assetKind = default(AssetKind), List<Reference> billOfMaterial = default(List<Reference>), Reference globalAssetId = default(Reference), List<IdentifierKeyValuePair> specificAssetIds = default(List<IdentifierKeyValuePair>), System.IO.Stream thumbnail = default(System.IO.Stream))
        {
            // to ensure "assetKind" is required (not null)
            if (assetKind == null)
            {
                throw new InvalidDataException("assetKind is a required property for AssetInformation and cannot be null");
            }
            else
            {
                this.AssetKind = assetKind;
            }
            this.BillOfMaterial = billOfMaterial;
            this.GlobalAssetId = globalAssetId;
            this.SpecificAssetIds = specificAssetIds;
            this.Thumbnail = thumbnail;
        }

        /// <summary>
        /// Gets or Sets AssetKind
        /// </summary>
        [DataMember(Name = "assetKind", EmitDefaultValue = false)]
        public AssetKind AssetKind { get; set; }

        /// <summary>
        /// Gets or Sets BillOfMaterial
        /// </summary>
        [DataMember(Name = "billOfMaterial", EmitDefaultValue = false)]
        public List<Reference> BillOfMaterial { get; set; }

        /// <summary>
        /// Gets or Sets GlobalAssetId
        /// </summary>
        [DataMember(Name = "globalAssetId", EmitDefaultValue = false)]
        public Reference GlobalAssetId { get; set; }

        /// <summary>
        /// Gets or Sets SpecificAssetIds
        /// </summary>
        [DataMember(Name = "specificAssetIds", EmitDefaultValue = false)]
        public List<IdentifierKeyValuePair> SpecificAssetIds { get; set; }

        /// <summary>
        /// Gets or Sets Thumbnail
        /// </summary>
        [DataMember(Name = "thumbnail", EmitDefaultValue = false)]
        public System.IO.Stream Thumbnail { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AssetInformation {\n");
            sb.Append("  AssetKind: ").Append(AssetKind).Append("\n");
            sb.Append("  BillOfMaterial: ").Append(BillOfMaterial).Append("\n");
            sb.Append("  GlobalAssetId: ").Append(GlobalAssetId).Append("\n");
            sb.Append("  SpecificAssetIds: ").Append(SpecificAssetIds).Append("\n");
            sb.Append("  Thumbnail: ").Append(Thumbnail).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as AssetInformation);
        }

        /// <summary>
        /// Returns true if AssetInformation instances are equal
        /// </summary>
        /// <param name="input">Instance of AssetInformation to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AssetInformation input)
        {
            if (input == null)
                return false;

            return
                (
                    this.AssetKind == input.AssetKind ||
                    (this.AssetKind != null &&
                    this.AssetKind.Equals(input.AssetKind))
                ) &&
                (
                    this.BillOfMaterial == input.BillOfMaterial ||
                    this.BillOfMaterial != null &&
                    input.BillOfMaterial != null &&
                    this.BillOfMaterial.SequenceEqual(input.BillOfMaterial)
                ) &&
                (
                    this.GlobalAssetId == input.GlobalAssetId ||
                    (this.GlobalAssetId != null &&
                    this.GlobalAssetId.Equals(input.GlobalAssetId))
                ) &&
                (
                    this.SpecificAssetIds == input.SpecificAssetIds ||
                    this.SpecificAssetIds != null &&
                    input.SpecificAssetIds != null &&
                    this.SpecificAssetIds.SequenceEqual(input.SpecificAssetIds)
                ) &&
                (
                    this.Thumbnail == input.Thumbnail ||
                    (this.Thumbnail != null &&
                    this.Thumbnail.Equals(input.Thumbnail))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.AssetKind != null)
                    hashCode = hashCode * 59 + this.AssetKind.GetHashCode();
                if (this.BillOfMaterial != null)
                    hashCode = hashCode * 59 + this.BillOfMaterial.GetHashCode();
                if (this.GlobalAssetId != null)
                    hashCode = hashCode * 59 + this.GlobalAssetId.GetHashCode();
                if (this.SpecificAssetIds != null)
                    hashCode = hashCode * 59 + this.SpecificAssetIds.GetHashCode();
                if (this.Thumbnail != null)
                    hashCode = hashCode * 59 + this.Thumbnail.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
