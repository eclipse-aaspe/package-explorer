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
    /// DataSpecificationPhysicalUnitContent
    /// </summary>
    [DataContract]
    public partial class DataSpecificationPhysicalUnitContent : IEquatable<DataSpecificationPhysicalUnitContent>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSpecificationPhysicalUnitContent" /> class.
        /// </summary>
        /// <param name="conversionFactor">conversionFactor.</param>
        /// <param name="definition">definition (required).</param>
        /// <param name="dinNotation">dinNotation.</param>
        /// <param name="eceCode">eceCode.</param>
        /// <param name="eceName">eceName.</param>
        /// <param name="nistName">nistName.</param>
        /// <param name="registrationAuthorityId">registrationAuthorityId.</param>
        /// <param name="siName">siName.</param>
        /// <param name="siNotation">siNotation.</param>
        /// <param name="sourceOfDefinition">sourceOfDefinition.</param>
        /// <param name="supplier">supplier.</param>
        /// <param name="unitName">unitName (required).</param>
        /// <param name="unitSymbol">unitSymbol (required).</param>
        public DataSpecificationPhysicalUnitContent(string conversionFactor = default(string), List<LangString> definition = default(List<LangString>), string dinNotation = default(string), string eceCode = default(string), string eceName = default(string), string nistName = default(string), string registrationAuthorityId = default(string), string siName = default(string), string siNotation = default(string), string sourceOfDefinition = default(string), string supplier = default(string), string unitName = default(string), string unitSymbol = default(string))
        {
            // to ensure "definition" is required (not null)
            if (definition == null)
            {
                throw new InvalidDataException("definition is a required property for DataSpecificationPhysicalUnitContent and cannot be null");
            }
            else
            {
                this.Definition = definition;
            }
            // to ensure "unitName" is required (not null)
            if (unitName == null)
            {
                throw new InvalidDataException("unitName is a required property for DataSpecificationPhysicalUnitContent and cannot be null");
            }
            else
            {
                this.UnitName = unitName;
            }
            // to ensure "unitSymbol" is required (not null)
            if (unitSymbol == null)
            {
                throw new InvalidDataException("unitSymbol is a required property for DataSpecificationPhysicalUnitContent and cannot be null");
            }
            else
            {
                this.UnitSymbol = unitSymbol;
            }
            this.ConversionFactor = conversionFactor;
            this.DinNotation = dinNotation;
            this.EceCode = eceCode;
            this.EceName = eceName;
            this.NistName = nistName;
            this.RegistrationAuthorityId = registrationAuthorityId;
            this.SiName = siName;
            this.SiNotation = siNotation;
            this.SourceOfDefinition = sourceOfDefinition;
            this.Supplier = supplier;
        }

        /// <summary>
        /// Gets or Sets ConversionFactor
        /// </summary>
        [DataMember(Name = "conversionFactor", EmitDefaultValue = false)]
        public string ConversionFactor { get; set; }

        /// <summary>
        /// Gets or Sets Definition
        /// </summary>
        [DataMember(Name = "definition", EmitDefaultValue = false)]
        public List<LangString> Definition { get; set; }

        /// <summary>
        /// Gets or Sets DinNotation
        /// </summary>
        [DataMember(Name = "dinNotation", EmitDefaultValue = false)]
        public string DinNotation { get; set; }

        /// <summary>
        /// Gets or Sets EceCode
        /// </summary>
        [DataMember(Name = "eceCode", EmitDefaultValue = false)]
        public string EceCode { get; set; }

        /// <summary>
        /// Gets or Sets EceName
        /// </summary>
        [DataMember(Name = "eceName", EmitDefaultValue = false)]
        public string EceName { get; set; }

        /// <summary>
        /// Gets or Sets NistName
        /// </summary>
        [DataMember(Name = "nistName", EmitDefaultValue = false)]
        public string NistName { get; set; }

        /// <summary>
        /// Gets or Sets RegistrationAuthorityId
        /// </summary>
        [DataMember(Name = "registrationAuthorityId", EmitDefaultValue = false)]
        public string RegistrationAuthorityId { get; set; }

        /// <summary>
        /// Gets or Sets SiName
        /// </summary>
        [DataMember(Name = "siName", EmitDefaultValue = false)]
        public string SiName { get; set; }

        /// <summary>
        /// Gets or Sets SiNotation
        /// </summary>
        [DataMember(Name = "siNotation", EmitDefaultValue = false)]
        public string SiNotation { get; set; }

        /// <summary>
        /// Gets or Sets SourceOfDefinition
        /// </summary>
        [DataMember(Name = "sourceOfDefinition", EmitDefaultValue = false)]
        public string SourceOfDefinition { get; set; }

        /// <summary>
        /// Gets or Sets Supplier
        /// </summary>
        [DataMember(Name = "supplier", EmitDefaultValue = false)]
        public string Supplier { get; set; }

        /// <summary>
        /// Gets or Sets UnitName
        /// </summary>
        [DataMember(Name = "unitName", EmitDefaultValue = false)]
        public string UnitName { get; set; }

        /// <summary>
        /// Gets or Sets UnitSymbol
        /// </summary>
        [DataMember(Name = "unitSymbol", EmitDefaultValue = false)]
        public string UnitSymbol { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DataSpecificationPhysicalUnitContent {\n");
            sb.Append("  ConversionFactor: ").Append(ConversionFactor).Append("\n");
            sb.Append("  Definition: ").Append(Definition).Append("\n");
            sb.Append("  DinNotation: ").Append(DinNotation).Append("\n");
            sb.Append("  EceCode: ").Append(EceCode).Append("\n");
            sb.Append("  EceName: ").Append(EceName).Append("\n");
            sb.Append("  NistName: ").Append(NistName).Append("\n");
            sb.Append("  RegistrationAuthorityId: ").Append(RegistrationAuthorityId).Append("\n");
            sb.Append("  SiName: ").Append(SiName).Append("\n");
            sb.Append("  SiNotation: ").Append(SiNotation).Append("\n");
            sb.Append("  SourceOfDefinition: ").Append(SourceOfDefinition).Append("\n");
            sb.Append("  Supplier: ").Append(Supplier).Append("\n");
            sb.Append("  UnitName: ").Append(UnitName).Append("\n");
            sb.Append("  UnitSymbol: ").Append(UnitSymbol).Append("\n");
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
            return this.Equals(input as DataSpecificationPhysicalUnitContent);
        }

        /// <summary>
        /// Returns true if DataSpecificationPhysicalUnitContent instances are equal
        /// </summary>
        /// <param name="input">Instance of DataSpecificationPhysicalUnitContent to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DataSpecificationPhysicalUnitContent input)
        {
            if (input == null)
                return false;

            return
                (
                    this.ConversionFactor == input.ConversionFactor ||
                    (this.ConversionFactor != null &&
                    this.ConversionFactor.Equals(input.ConversionFactor))
                ) &&
                (
                    this.Definition == input.Definition ||
                    this.Definition != null &&
                    input.Definition != null &&
                    this.Definition.SequenceEqual(input.Definition)
                ) &&
                (
                    this.DinNotation == input.DinNotation ||
                    (this.DinNotation != null &&
                    this.DinNotation.Equals(input.DinNotation))
                ) &&
                (
                    this.EceCode == input.EceCode ||
                    (this.EceCode != null &&
                    this.EceCode.Equals(input.EceCode))
                ) &&
                (
                    this.EceName == input.EceName ||
                    (this.EceName != null &&
                    this.EceName.Equals(input.EceName))
                ) &&
                (
                    this.NistName == input.NistName ||
                    (this.NistName != null &&
                    this.NistName.Equals(input.NistName))
                ) &&
                (
                    this.RegistrationAuthorityId == input.RegistrationAuthorityId ||
                    (this.RegistrationAuthorityId != null &&
                    this.RegistrationAuthorityId.Equals(input.RegistrationAuthorityId))
                ) &&
                (
                    this.SiName == input.SiName ||
                    (this.SiName != null &&
                    this.SiName.Equals(input.SiName))
                ) &&
                (
                    this.SiNotation == input.SiNotation ||
                    (this.SiNotation != null &&
                    this.SiNotation.Equals(input.SiNotation))
                ) &&
                (
                    this.SourceOfDefinition == input.SourceOfDefinition ||
                    (this.SourceOfDefinition != null &&
                    this.SourceOfDefinition.Equals(input.SourceOfDefinition))
                ) &&
                (
                    this.Supplier == input.Supplier ||
                    (this.Supplier != null &&
                    this.Supplier.Equals(input.Supplier))
                ) &&
                (
                    this.UnitName == input.UnitName ||
                    (this.UnitName != null &&
                    this.UnitName.Equals(input.UnitName))
                ) &&
                (
                    this.UnitSymbol == input.UnitSymbol ||
                    (this.UnitSymbol != null &&
                    this.UnitSymbol.Equals(input.UnitSymbol))
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
                if (this.ConversionFactor != null)
                    hashCode = hashCode * 59 + this.ConversionFactor.GetHashCode();
                if (this.Definition != null)
                    hashCode = hashCode * 59 + this.Definition.GetHashCode();
                if (this.DinNotation != null)
                    hashCode = hashCode * 59 + this.DinNotation.GetHashCode();
                if (this.EceCode != null)
                    hashCode = hashCode * 59 + this.EceCode.GetHashCode();
                if (this.EceName != null)
                    hashCode = hashCode * 59 + this.EceName.GetHashCode();
                if (this.NistName != null)
                    hashCode = hashCode * 59 + this.NistName.GetHashCode();
                if (this.RegistrationAuthorityId != null)
                    hashCode = hashCode * 59 + this.RegistrationAuthorityId.GetHashCode();
                if (this.SiName != null)
                    hashCode = hashCode * 59 + this.SiName.GetHashCode();
                if (this.SiNotation != null)
                    hashCode = hashCode * 59 + this.SiNotation.GetHashCode();
                if (this.SourceOfDefinition != null)
                    hashCode = hashCode * 59 + this.SourceOfDefinition.GetHashCode();
                if (this.Supplier != null)
                    hashCode = hashCode * 59 + this.Supplier.GetHashCode();
                if (this.UnitName != null)
                    hashCode = hashCode * 59 + this.UnitName.GetHashCode();
                if (this.UnitSymbol != null)
                    hashCode = hashCode * 59 + this.UnitSymbol.GetHashCode();
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
