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
    /// Definitions of Submodel Generic Frame for Technical Data for Industrial Equipment
    /// in Manufacturing (IDTA 02003-2-0) from Mar 2025
    /// </summary>
    public class IdtaTechnicalDataV20 : AasxDefinitionBase
    {
        public static IdtaTechnicalDataV20 Static = new IdtaTechnicalDataV20();

        public Aas.Submodel
            SM_TechnicalData;

        public Aas.ConceptDescription
            CD_GeneralInformation,
            CD_ManufacturerName,
            CD_CompanyLogo,
            CD_ManufacturerProductDesignation,
            CD_ManufacturerArticleNumber,
            CD_ManufacturerOrderCode,
            CD_ProductImages,
            CD_ProductImage,
            CD_ImageFile,
            CD_ImageNote,
            CD_ProductClassifications,
            CD_ProductClassification,
            CD_ClassificationSystem,
            CD_ClassificationSystemVersion,
            CD_ClassificationSystemUrl,
            CD_ProductClassId,
            CD_ProductClassCodedName,
            CD_ProductClassName,
            CD_ReferenceToTechnicalPropertyArea,
            CD_TechnicalPropertyAreas,
            CD_TechnicalPropertyArea,
            CD_FurtherInformation,
            CD_TextStatement,
            CD_ValidDate,
            CD_SpecificDescriptions,
            CD_SpecificDescription;

        public IdtaTechnicalDataV20()
        {
            // info
            this.DomainInfo = "Generic Frame for Technical Data for Industrial Equipment (IDTA) V2.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "IdtaTechnicalDataV20.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaTechnicalDataV20), useFieldNames: true);
        }
    }
}
