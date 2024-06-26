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
    /// BlobCertificate
    /// </summary>
    [DataContract]
    public partial class BlobCertificate : IEquatable<BlobCertificate>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobCertificate" /> class.
        /// </summary>
        /// <param name="blobCertificate">blobCertificate.</param>
        /// <param name="containedExtension">containedExtension.</param>
        /// <param name="lastCertificate">lastCertificate.</param>
        public BlobCertificate(Blob blobCertificate = default(Blob), List<Reference> containedExtension = default(List<Reference>), bool? lastCertificate = default(bool?))
        {
            this._BlobCertificate = blobCertificate;
            this.ContainedExtension = containedExtension;
            this.LastCertificate = lastCertificate;
        }

        /// <summary>
        /// Gets or Sets _BlobCertificate
        /// </summary>
        [DataMember(Name = "blobCertificate", EmitDefaultValue = false)]
        public Blob _BlobCertificate { get; set; }

        /// <summary>
        /// Gets or Sets ContainedExtension
        /// </summary>
        [DataMember(Name = "containedExtension", EmitDefaultValue = false)]
        public List<Reference> ContainedExtension { get; set; }

        /// <summary>
        /// Gets or Sets LastCertificate
        /// </summary>
        [DataMember(Name = "lastCertificate", EmitDefaultValue = false)]
        public bool? LastCertificate { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BlobCertificate {\n");
            sb.Append("  _BlobCertificate: ").Append(_BlobCertificate).Append("\n");
            sb.Append("  ContainedExtension: ").Append(ContainedExtension).Append("\n");
            sb.Append("  LastCertificate: ").Append(LastCertificate).Append("\n");
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
            return this.Equals(input as BlobCertificate);
        }

        /// <summary>
        /// Returns true if BlobCertificate instances are equal
        /// </summary>
        /// <param name="input">Instance of BlobCertificate to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(BlobCertificate input)
        {
            if (input == null)
                return false;

            return
                (
                    this._BlobCertificate == input._BlobCertificate ||
                    (this._BlobCertificate != null &&
                    this._BlobCertificate.Equals(input._BlobCertificate))
                ) &&
                (
                    this.ContainedExtension == input.ContainedExtension ||
                    this.ContainedExtension != null &&
                    input.ContainedExtension != null &&
                    this.ContainedExtension.SequenceEqual(input.ContainedExtension)
                ) &&
                (
                    this.LastCertificate == input.LastCertificate ||
                    (this.LastCertificate != null &&
                    this.LastCertificate.Equals(input.LastCertificate))
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
                if (this._BlobCertificate != null)
                    hashCode = hashCode * 59 + this._BlobCertificate.GetHashCode();
                if (this.ContainedExtension != null)
                    hashCode = hashCode * 59 + this.ContainedExtension.GetHashCode();
                if (this.LastCertificate != null)
                    hashCode = hashCode * 59 + this.LastCertificate.GetHashCode();
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
