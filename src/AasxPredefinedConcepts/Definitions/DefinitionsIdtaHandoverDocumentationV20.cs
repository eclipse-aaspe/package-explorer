/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Stefan Erler

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
    /// Definitions of Submodel Handover documentation (orig. ZVEI, now IDTA)
    /// </summary>
    public class IdtaHandoverDocumentationV20 : AasxDefinitionBase
    {
        public static IdtaHandoverDocumentationV20 Static = new IdtaHandoverDocumentationV20();

        public Aas.Submodel
            SM_HandoverDocumentation;

        public Aas.ConceptDescription
            CD_Documents,
            CD_Document,
            CD_DocumentIds,
            CD_DocumentId,
            CD_DocumentDomainId,
            CD_DocumentIdentifier,
            CD_DocumentIsPrimary,
            CD_DocumentClassifications,
            CD_DocumentClassification,
            CD_ClassId,
            CD_ClassName,
            CD_ClassificationSystem,
            CD_DocumentVersions,
            CD_DocumentVersion,
            CD_Language,
            CD_Version,
            CD_Title,
            CD_Subtitle,
            CD_Description,
            CD_KeyWords,
            CD_StatusSetDate,
            CD_StatusValue,
            CD_OrganizationShortName,
            CD_OrganizationOfficialName,
            CD_RefersToEntities,
            CD_BasedOn,
            CD_TranslationOfEntities,
            CD_DigitalFile,
            CD_PreviewFile,
            CD_DocumentedEntities,
            CD_DocumentedEntity,
            CD_Entities,
            CD_Entity;

        public IdtaHandoverDocumentationV20()
        {
            // info
            this.DomainInfo = "Handover Documentation (IDTA) V2.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(),
                "AasxPredefinedConcepts.Resources." + "IdtaHandoverDocumentationV20.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaHandoverDocumentationV20), useFieldNames: true);
        }
    }
}
