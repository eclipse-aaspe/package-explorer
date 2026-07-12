/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_1;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Digital Nameplate 
    /// </summary>
    public class DigitalNameplateV301 : AasxDefinitionBase
    {
        public static DigitalNameplateV301 Static = new DigitalNameplateV301();

        public Aas.Submodel
            SM_Nameplate;

        public Aas.ConceptDescription
            CD_URIOfTheProduct,
            CD_ManufacturerName,
            CD_ManufacturerProductDesignation,
            CD_ManufacturerProductRoot,
            CD_ManufacturerProductFamily,
            CD_ManufacturerProductType,
            CD_OrderCodeOfManufacturer,
            CD_ProductArticleNumberOfManufacturer,
            CD_SerialNumber,
            CD_YearOfConstruction,
            CD_DateOfManufacture,
            CD_HardwareVersion,
            CD_FirmwareVersion,
            CD_SoftwareVersion,
            CD_CountryOfOrigin,
            CD_UniqueFacilityIdentifier,
            CD_CompanyLogo,
            CD_Markings,
            CD_Marking,
            CD_MarkingName,
            CD_DesignationOfCertificateOrApproval,
            CD_IssueDate,
            CD_ExpiryDate,
            CD_MarkingFile,
            CD_MarkingAdditionalText,
            CD_AssetSpecificProperties,
            CD_GuidelineSpecificProperties,
            CD_GuidelineForConformityDeclaration;

        public DigitalNameplateV301()
        {
            // info
            this.DomainInfo = "Digital Nameplate (IDTA) V3.0.1";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "DigitalNameplateV301.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(DigitalNameplateV301), useFieldNames: true);
        }
    }
}
