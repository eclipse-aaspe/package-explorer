/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Aas3_0;
using AasxIntegrationBase;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using AdminShellNS;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Aas = AasCore.Aas3_0;

// These classes were serialized by "export predefined concepts"
// and shall allow to automatically de-serialize AAS elements structures
// into C# classes.

namespace AasxPredefinedConcepts.ProductChangeNotifications.V_1_0_pre
{
    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/Record/List/1/0")]
    public class CD_RecordsOfPcn : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecordsOfPcn
    {
        [AasConcept(Cd = "0173-10029#01-XFB002#001", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Record> Record { get; set; } =
            (new List<CD_Record>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Record>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB002#001")]
    public class CD_Record : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Record
    {
        [AasConcept(Cd = "0173-10029#01-XFB003#001", Card = AasxPredefinedCardinality.One)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Manufacturer Manufacturer { get; set; } =
            new CD_Manufacturer();

        [AasConcept(Cd = "0173-10029#02-ABC507#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string ManufacturerChangeID { get; set; }

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnType/1/0", Card = AasxPredefinedCardinality.One)]
        public string PcnType { get; set; }

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_LifeCycleData LifeCycleData { get; set; } = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ReasonOfChange/List/1/0", Card = AasxPredefinedCardinality.One)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ReasonsOfChange ReasonsOfChange { get; set; } =
            new CD_ReasonsOfChange();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/List/1/0", Card = AasxPredefinedCardinality.One)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemCategories ItemCategories { get; set; } =
            new CD_ItemCategories();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AffectedPartNumber/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_AffectedPartNumbers AffectedPartNumbers { get; set; } = null;

        [AasConcept(Cd = "0173-1#02-ABF814#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<ILangStringTextType> PcnReasonComment { get; set; } = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/1/0", Card = AasxPredefinedCardinality.One)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_PcnChangeInformation PcnChangeInformation { get; set; } =
            new CD_PcnChangeInformation();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AdditionalInformation/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_AdditionalInformations AdditionalInformation { get; set; } = null;

        [AasConcept(Cd = "0173-1#02-ABF816#002", Card = AasxPredefinedCardinality.One)]
        public string DateOfRecord { get; set; }

        [AasConcept(Cd = "0173-10029#01-XFB006#001", Card = AasxPredefinedCardinality.One)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemOfChange ItemOfChange { get; set; } =
            new CD_ItemOfChange();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecommendedItems RecommendedItems { get; set; } = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB003#001")]
    public class CD_Manufacturer : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Manufacturer
    {
        [AasConcept(Cd = "0173-1#02-AAO677#003", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerName { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAQ832#005", Card = AasxPredefinedCardinality.One)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Address AdressInformation { get; set; } =
            new CD_Address();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-1#02-AAQ832#005")]
    public class CD_Address : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_Address
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/List/1/0")]
    public class CD_LifeCycleData : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_LifeCycleData
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/Milestone/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_LifeCycleMilestone> Milestone { get; set; } =
            (new List<CD_LifeCycleMilestone>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_LifeCycleMilestone>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/Milestone/1/0")]
    public class CD_LifeCycleMilestone : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_LifeCycleMilestone
    {
        [AasConcept(Cd = "0173-10029#02-ABC548#001", Card = AasxPredefinedCardinality.One)]
        public string MilestoneClassification { get; set; }

        [AasConcept(Cd = "0173-1#02-ABF815#002", Card = AasxPredefinedCardinality.One)]
        public string DateOfValidity { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ReasonOfChange/List/1/0")]
    public class CD_ReasonsOfChange : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ReasonsOfChange
    {
        [AasConcept(Cd = "0173-10029#01-XFB005#001", Card = AasxPredefinedCardinality.OneToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ReasonOfChange> ReasonOfChange { get; set; } =
            (new List<CD_ReasonOfChange>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ReasonOfChange>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB005#001")]
    public class CD_ReasonOfChange : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ReasonOfChange
    {
        [AasConcept(Cd = "0173-1#02-ABF813#002", Card = AasxPredefinedCardinality.One)]
        public string ReasonClassificationSystem { get; set; }

        [AasConcept(Cd = "0173-1#02-AAR710#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string VersionOfClassificationSystem { get; set; }

        [AasConcept(Cd = "0173-10029#02-ABC727#001", Card = AasxPredefinedCardinality.One)]
        public string ReasonId { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/List/1/0")]
    public class CD_ItemCategories : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemCategories
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/1/0", Card = AasxPredefinedCardinality.OneToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemCategory> ItemCategory { get; set; } =
            (new List<CD_ItemCategory>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemCategory>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/1/0")]
    public class CD_ItemCategory : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemCategory
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/ItemClassificationSystem/1/0", Card = AasxPredefinedCardinality.One)]
        public string ItemClassificationSystem { get; set; }

        [AasConcept(Cd = "0173-1#02-AAR710#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string VersionOfClassificationSystem { get; set; }

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/ItemCategory/1/0", Card = AasxPredefinedCardinality.One)]
        public string ItemCategory { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AffectedPartNumber/List/1/0")]
    public class CD_AffectedPartNumbers : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_AffectedPartNumbers
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AffectedPartNumber/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<string> AffectedPartNumber { get; set; } =
            (new List<string>());

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/1/0")]
    public class CD_PcnChangeInformation : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_PcnChangeInformation
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/ChangeTitle/1/0", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ChangeTitle { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/ChangeDetail/1/0", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ChangeDetail { get; set; } =
            new List<ILangStringTextType>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AdditionalInformation/List/1/0")]
    public class CD_AdditionalInformations : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_AdditionalInformations
    {
        [AasConcept(Cd = "0173-1#01-ADN356#009", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasClassMapperFile> AdditionalInformation { get; set; } =
            (new List<AasClassMapperFile>());

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB006#001")]
    public class CD_ItemOfChange : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ItemOfChange
    {
        [AasConcept(Cd = "0173-1#02-AAU731#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductFamily { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAW338#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductDesignation { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAO227#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<ILangStringTextType> OrderCodeOfManufacturer { get; set; } = null;

        [AasConcept(Cd = "0173-10029#02-ABF978#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperHintedReference ManufacturerAssetID { get; set; } = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ProductClassification/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductClassifications ProductClassifications { get; set; } = null;

        [AasConcept(Cd = "0173-1#02-AAN270#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string HardwareVersion { get; set; }

        [AasConcept(Cd = "0173-1#02-BAF551#003", Card = AasxPredefinedCardinality.ZeroToOne)]
        public UInt64? RemainingAmountAvailable { get; set; }

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Changes TechnicalData_Changes { get; set; } = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_CurrentState/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_CurrentState TechnicalData_CurrentState { get; set; } = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ProductClassification/List/1/0")]
    public class CD_ProductClassifications : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductClassifications
    {
        [AasConcept(Cd = "0173-10029#01-XFB007#001", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductClassification> ProductClassification { get; set; } =
            (new List<CD_ProductClassification>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductClassification>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB007#001")]
    public class CD_ProductClassification : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductClassification
    {
        [AasConcept(Cd = "0173-1#02-AAR709#002", Card = AasxPredefinedCardinality.One)]
        public string ClassificationSystem { get; set; }

        [AasConcept(Cd = "0173-1#02-AAR710#002", Card = AasxPredefinedCardinality.One)]
        public string VersionOfClassificationSystem { get; set; }

        [AasConcept(Cd = "0173-10029#02-ABF979#001", Card = AasxPredefinedCardinality.One)]
        public string ProductClassId { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/List/1/0")]
    public class CD_TechnicalData_Changes : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Changes
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/Change/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_SingleChange> SingleChange { get; set; } =
            (new List<CD_SingleChange>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_SingleChange>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/Change/1/0")]
    public class CD_SingleChange : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_SingleChange
    {
        public string Arbitrary { get; set; }

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/OriginOfChange/1/0", Card = AasxPredefinedCardinality.One)]
        public AasClassMapperHintedReference OriginOfChange { get; set; } =
            new AasClassMapperHintedReference();

        [AasConcept(Cd = "0173-10029#02-ABC727#001", Card = AasxPredefinedCardinality.One)]
        public string ReasonId { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_CurrentState/List/1/0")]
    public class CD_TechnicalData_CurrentState : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_CurrentState
    {
        public List<string> Arbitrary { get; set; } =
            (new List<string>());

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/List/1/0")]
    public class CD_RecommendedItems : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecommendedItems
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecommendedItem> RecommendedItem { get; set; } =
            (new List<CD_RecommendedItem>()).Cast<AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecommendedItem>().ToList();

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/1/0")]
    public class CD_RecommendedItem : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecommendedItem
    {
        [AasConcept(Cd = "0173-1#02-AAU731#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductFamily { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAW338#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductDesignation { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAO227#002", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> OrderCodeOfManufacturer { get; set; } =
            new List<ILangStringTextType>();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ProductClassification/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductClassifications ProductClassifications { get; set; } = null;

        [AasConcept(Cd = "0173-10029#01-XFB008#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Fit TechnicalData_Fit { get; set; } = null;

        [AasConcept(Cd = "0173-10029#01-XFB009#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Form TechnicalData_Form { get; set; } = null;

        [AasConcept(Cd = "0173-10029#01-XFB010#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Function TechnicalData_Function { get; set; } = null;

        [AasConcept(Cd = "0173-10029#01-XFB011#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Other TechnicalData_Other { get; set; } = null;

        [AasConcept(Cd = "0173-1#02-AAO280#003", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string IncotermCode { get; set; }

        [AasConcept(Cd = "0173-10029#02-ABF982#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public Int32? DeliveryTimeClassOtherRegion { get; set; }

        [AasConcept(Cd = "0173-10029#02-ABF981#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public Int32? DeliveryTimeClassSameRegion { get; set; }

        [AasConcept(Cd = "0173-10029#01-XFB012#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ConformityDeclarations ConformityDeclarations { get; set; } = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB008#001")]
    public class CD_TechnicalData_Fit : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Fit
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate { get; set; }

        public string Arbitrary { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB009#001")]
    public class CD_TechnicalData_Form : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Form
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate { get; set; }

        public string Arbitrary { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB010#001")]
    public class CD_TechnicalData_Function : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Function
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate { get; set; }

        public string Arbitrary { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB011#001")]
    public class CD_TechnicalData_Other : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_TechnicalData_Other
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate { get; set; }

        public string Arbitrary { get; set; }

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB012#001")]
    public class CD_ConformityDeclarations : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ConformityDeclarations
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB001#001")]
    public class CD_ProductChangeNotifications : AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_ProductChangeNotifications
    {

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/Record/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasxPredefinedConcepts.ProductChangeNotifications.Base.ICD_RecordsOfPcn Records { get; set; } = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ { get; set; } = null;
    }

}

