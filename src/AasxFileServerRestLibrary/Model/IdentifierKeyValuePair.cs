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
    /// IdentifierKeyValuePair
    /// </summary>
    [DataContract]
    public partial class IdentifierKeyValuePair : HasSemantics, IEquatable<IdentifierKeyValuePair>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentifierKeyValuePair" /> class.
        /// </summary>
        /// <param name="key">key (required).</param>
        /// <param name="subjectId">subjectId (required).</param>
        /// <param name="value">value (required).</param>
        public IdentifierKeyValuePair(string key = default(string), Reference subjectId = default(Reference), string value = default(string), Reference semanticId = default(Reference)) : base(semanticId)
        {
            // to ensure "key" is required (not null)
            if (key == null)
            {
                throw new InvalidDataException("key is a required property for IdentifierKeyValuePair and cannot be null");
            }
            else
            {
                this.Key = key;
            }
            // to ensure "subjectId" is required (not null)
            if (subjectId == null)
            {
                throw new InvalidDataException("subjectId is a required property for IdentifierKeyValuePair and cannot be null");
            }
            else
            {
                this.SubjectId = subjectId;
            }
            // to ensure "value" is required (not null)
            if (value == null)
            {
                throw new InvalidDataException("value is a required property for IdentifierKeyValuePair and cannot be null");
            }
            else
            {
                this.Value = value;
            }
        }

        /// <summary>
        /// Gets or Sets Key
        /// </summary>
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets SubjectId
        /// </summary>
        [DataMember(Name = "subjectId", EmitDefaultValue = false)]
        public Reference SubjectId { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string Value { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class IdentifierKeyValuePair {\n");
            sb.Append("  ").Append(base.ToString().Replace("\n", "\n  ")).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  SubjectId: ").Append(SubjectId).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public override string ToJson()
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
            return this.Equals(input as IdentifierKeyValuePair);
        }

        /// <summary>
        /// Returns true if IdentifierKeyValuePair instances are equal
        /// </summary>
        /// <param name="input">Instance of IdentifierKeyValuePair to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(IdentifierKeyValuePair input)
        {
            if (input == null)
                return false;

            return base.Equals(input) &&
                (
                    this.Key == input.Key ||
                    (this.Key != null &&
                    this.Key.Equals(input.Key))
                ) && base.Equals(input) &&
                (
                    this.SubjectId == input.SubjectId ||
                    (this.SubjectId != null &&
                    this.SubjectId.Equals(input.SubjectId))
                ) && base.Equals(input) &&
                (
                    this.Value == input.Value ||
                    (this.Value != null &&
                    this.Value.Equals(input.Value))
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
                int hashCode = base.GetHashCode();
                if (this.Key != null)
                    hashCode = hashCode * 59 + this.Key.GetHashCode();
                if (this.SubjectId != null)
                    hashCode = hashCode * 59 + this.SubjectId.GetHashCode();
                if (this.Value != null)
                    hashCode = hashCode * 59 + this.Value.GetHashCode();
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
