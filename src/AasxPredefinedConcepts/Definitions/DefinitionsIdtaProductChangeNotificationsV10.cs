/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Stefan Erler

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxPredefinedConcepts.ProductChangeNotifications;
using System.Reflection;
using Aas = AasCore.Aas3_1;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel ContactInformation (orig. ZVEI, now IDTA still V1.0)
    /// </summary>
    public class IdtaProductChangeNotificationsV10 : AasxDefinitionBase
    {
        public static IdtaProductChangeNotificationsV10 Static = new IdtaProductChangeNotificationsV10();

        public Aas.Submodel
            SM_ProductChangeNotifications;

        public Aas.ConceptDescription
            CD_PcnEventsOutgoing,
			CD_RecordsOfPcn,
			CD_Record,
			CD_Manufacturer,
			CD_ManufacturerName,
			CD_Address,
			CD_ManufacturerChangeID,
            CD_PcnType,
            CD_LifeCycleData,
			CD_LifeCycleMilestone,
			CD_MilestoneClassification,
			CD_DateOfValidityOfPcn,
			CD_ReasonsOfChange,
			CD_ReasonOfChange,
			CD_ClassificationOfPcnReason,
			CD_VersionOfClassificationSystem,
			CD_ReasonId,
			CD_ItemCategories,
			CD_ItemCategory,
			CD_ItemClassificationSystem,
			CD_AffectedPartNumbers,
			CD_AffectedPartNumber,
			CD_PcnReasonComment,
			CD_PcnChangeInformation,
			CD_ChangeTitle,
			CD_ChangeDetail,
			CD_AdditionalInformations,
			CD_AdditionalInformation,
			CD_DateOfRecordOfPcn,
			CD_ItemOfChange,
			CD_ManufacturerProductFamily,
			CD_ManufacturerProductDesignation,
			CD_ManufacturerAssetID,
			CD_ProductClassifications,
			CD_ProductClassification,
			CD_ClassificationSystem,
			CD_ProductClassId,
			CD_HardwareVersion,
			CD_StockInStorage,
			CD_TechnicalData_Changes,
			CD_NewValueOfChange,
			CD_Origin_of_change,
			CD_SingleChange,
			CD_TechnicalData_CurrentState,
			CD_RecommendedItems,
			CD_RecommendedItem,
			CD_TechnicalData_Fit,
			CD_TargetEstimate,
			CD_TechnicalData_Form,
			CD_TechnicalData_Function,
			CD_TechnicalData_Other,
			CD_Incotermcode,
			CD_DeliveryTimeClassOtherRegion,
			CD_DeliveryTimeClassSameRegion,
			CD_ConformityDeclarations;

        public IdtaProductChangeNotificationsV10()
        {
            // info
            this.DomainInfo = "Product Change Notifications (IDTA) Draft V1.0";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(),
                "AasxPredefinedConcepts.Resources." + "IdtaProductChangeNotificationsV10.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaProductChangeNotificationsV10), 
				useFieldNames: true);
        }
    }
}
