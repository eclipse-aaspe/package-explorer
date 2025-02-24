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

namespace AasxPredefinedConcepts.ProductChangeNotifications
{
    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/Record/List/1/0")]
    public class CD_RecordsOfPcn
    {
        [AasConcept(Cd = "0173-10029#01-XFB002#001", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_Record> Record = new List<CD_Record>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB002#001")]
    public class CD_Record
    {
        [AasConcept(Cd = "0173-10029#01-XFB003#001", Card = AasxPredefinedCardinality.One)]
        public CD_Manufacturer Manufacturer = new CD_Manufacturer();

        [AasConcept(Cd = "0173-10029#02-ABC507#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string ManufacturerChangeID;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnType/1/0", Card = AasxPredefinedCardinality.One)]
        public string PcnType;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_LifeCycleData LifeCycleData = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ReasonOfChange/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_ReasonsOfChange ReasonsOfChange = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_ItemCategories ItemCategories = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AffectedPartNumber/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_AffectedPartNumbers AffectedPartNumbers = null;

        [AasConcept(Cd = "0173-1#02-ABF814#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<ILangStringTextType> PcnReasonComment = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/1/0", Card = AasxPredefinedCardinality.One)]
        public CD_PcnChangeInformation PcnChangeInformation = new CD_PcnChangeInformation();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AdditionalInformation/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_AdditionalInformations AdditionalInformations = null;

        [AasConcept(Cd = "0173-1#02-ABF816#002", Card = AasxPredefinedCardinality.One)]
        public string DateOfRecord;

        [AasConcept(Cd = "0173-10029#01-XFB006#001", Card = AasxPredefinedCardinality.One)]
        public CD_ItemOfChange ItemOfChange = new CD_ItemOfChange();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_RecommendedItems RecommendedItems = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB003#001")]
    public class CD_Manufacturer
    {
        [AasConcept(Cd = "0173-1#02-AAO677#003", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerName = new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAQ832#005", Card = AasxPredefinedCardinality.One)]
        public CD_Address AdressInformation = new CD_Address();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-1#02-AAQ832#005")]
    public class CD_Address
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/List/1/0")]
    public class CD_LifeCycleData
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/Milestone/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_LifeCycleMilestone> Milestone = new List<CD_LifeCycleMilestone>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/LifeCycleData/Milestone/1/0")]
    public class CD_LifeCycleMilestone
    {
        [AasConcept(Cd = "0173-10029#02-ABC548#001", Card = AasxPredefinedCardinality.One)]
        public string MilestoneClassification;

        [AasConcept(Cd = " 0173-1#02-ABF815#002", Card = AasxPredefinedCardinality.One)]
        public string DateOfValidity;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ReasonOfChange/List/1/0")]
    public class CD_ReasonsOfChange
    {
        [AasConcept(Cd = "0173-10029#01-XFB005#001", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_ReasonOfChange> ReasonOfChange = new List<CD_ReasonOfChange>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB005#001")]
    public class CD_ReasonOfChange
    {
        [AasConcept(Cd = "0173-1#02-ABF813#002", Card = AasxPredefinedCardinality.One)]
        public string ReasonClassificationSystem;

        [AasConcept(Cd = "0173-1#02-AAR710#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string VersionOfClassificationSystem;

        [AasConcept(Cd = "0173-10029#02-ABC727#001", Card = AasxPredefinedCardinality.One)]
        public string ReasonId;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/List/1/0")]
    public class CD_ItemCategories
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_ItemCategory> ItemCategory = new List<CD_ItemCategory>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/1/0")]
    public class CD_ItemCategory
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/ItemClassificationSystem/1/0", Card = AasxPredefinedCardinality.One)]
        public string ItemClassificationSystem;

        [AasConcept(Cd = "0173-1#02-AAR710#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string VersionOfClassificationSystem;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ItemCategory/ItemCategory/1/0", Card = AasxPredefinedCardinality.One)]
        public string ItemCategory;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AffectedPartNumber/List/1/0")]
    public class CD_AffectedPartNumbers
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AffectedPartNumber/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<string> AffectedPartNumber = new List<string>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/1/0")]
    public class CD_PcnChangeInformation
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/ChangeTitle/1/0", 
            Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ChangeTitle = new List<ILangStringTextType>();

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/PcnChangeInformation/ChangeDetail/1/0", 
            Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ChangeDetail = new List<ILangStringTextType>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/AdditionalInformation/List/1/0")]
    public class CD_AdditionalInformations
    {
        [AasConcept(Cd = "0173-1#01-ADN356#009", 
            Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasClassMapperFile> AdditionalInformation = new List<AasClassMapperFile>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB006#001")]
    public class CD_ItemOfChange
    {
        [AasConcept(Cd = "0173-1#02-AAU731#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductFamily = new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAW338#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductDesignation = new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAO227#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<ILangStringTextType> OrderCodeOfManufacturer = null;

        [AasConcept(Cd = "0173-10029#02-ABF978#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperHintedReference ManufacturerAssetID = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ProductClassification/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_ProductClassifications ProductClassifications = null;

        [AasConcept(Cd = "0173-1#02-AAN270#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string HardwareVersion;

        [AasConcept(Cd = "0173-1#02-BAF551#003", Card = AasxPredefinedCardinality.ZeroToOne)]
        public uint? RemainingAmountAvailable;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_TechnicalData_Changes TechnicalData_Changes = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_CurrentState/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_TechnicalData_CurrentState TechnicalData_CurrentState = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ProductClassification/List/1/0")]
    public class CD_ProductClassifications
    {
        [AasConcept(Cd = "0173-10029#01-XFB007#001", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_ProductClassification> ProductClassification = new List<CD_ProductClassification>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB007#001")]
    public class CD_ProductClassification
    {
        [AasConcept(Cd = "0173-1#02-AAR709#002", Card = AasxPredefinedCardinality.One)]
        public string ClassificationSystem;

        [AasConcept(Cd = "0173-1#02-AAR710#002", Card = AasxPredefinedCardinality.One)]
        public string VersionOfClassificationSystem;

        [AasConcept(Cd = "0173-10029#02-ABF979#001", Card = AasxPredefinedCardinality.One)]
        public string ProductClassId;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/List/1/0")]
    public class CD_TechnicalData_Changes
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/Change/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_SingleChange> SingleChange = new List<CD_SingleChange>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/Change/1/0")]
    public class CD_SingleChange
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_Changes/Origin_of_Change/1/0", Card = AasxPredefinedCardinality.One)]
        public AasClassMapperHintedReference Origin_of_change = new AasClassMapperHintedReference();

        [AasConcept(Cd = "0173-10029#02-ABC727#001", Card = AasxPredefinedCardinality.One)]
        public string ReasonId;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/TechnicalData_CurrentState/List/1/0")]
    public class CD_TechnicalData_CurrentState
    {
        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/List/1/0")]
    public class CD_RecommendedItems
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/1/0", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_RecommendedItem> RecommendedItem = new List<CD_RecommendedItem>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/RecommendedItem/1/0")]
    public class CD_RecommendedItem
    {
        [AasConcept(Cd = "0173-1#02-AAU731#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductFamily = new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAW338#001", Card = AasxPredefinedCardinality.One)]
        public List<ILangStringTextType> ManufacturerProductDesignation = new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-1#02-AAO227#002", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<ILangStringTextType> OrderCodeOfManufacturer = new List<ILangStringTextType>();

        [AasConcept(Cd = "0173-10029#02-ABF978#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperHintedReference ManufacturerAssetID = null;

        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/ProductClassification/List/1/0", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_ProductClassifications ProductClassifications = null;

        [AasConcept(Cd = "0173-10029#01-XFB008#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_TechnicalData_Fit TechnicalData_Fit = null;

        [AasConcept(Cd = "0173-10029#01-XFB009#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_TechnicalData_Form TechnicalData_Form = null;

        [AasConcept(Cd = "0173-10029#01-XFB010#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_TechnicalData_Function TechnicalData_Function = null;

        [AasConcept(Cd = "0173-10029#01-XFB011#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_TechnicalData_Other TechnicalData_Other = null;

        [AasConcept(Cd = "0173-1#02-AAO280#003", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Incotermcode;

        [AasConcept(Cd = "0173-10029#02-ABF982#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public int? DeliveryTimeClassOtherRegion;

        [AasConcept(Cd = "0173-10029#02-ABF981#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public int? DeliveryTimeClassSameRegion;

        [AasConcept(Cd = "0173-10029#01-XFB012#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_ConformityDeclarations ConformityDeclarations = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB008#001")]   
    public class CD_TechnicalData_Fit
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB009#001")]
    public class CD_TechnicalData_Form
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB010#001")]
    public class CD_TechnicalData_Function
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB011#001")]
    public class CD_TechnicalData_Other
    {
        [AasConcept(Cd = "0173-10029#02-ABF980#001", Card = AasxPredefinedCardinality.ZeroToOne)]
        public float? TargetEstimate;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "0173-10029#01-XFB012#001")]
    public class CD_ConformityDeclarations
    {
        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Submodel")]
    public class CD_ProductChangeNotifications
    {
        [AasConcept(Cd = "http://admin-shell.io/VDMA/Fluidics/ProductChangeNotification/Record/List/1/0",
            Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_RecordsOfPcn Records = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    //
    // Reasons
    //

    public class PcnReasonDescription
    {
        public string Id;
        public string Name;
        public string Description;

        public static List<PcnReasonDescription> GetAll()
        {
            var res = new List<PcnReasonDescription>();
            Action<string, string, string> lambda = (id, name, desc) =>
                res.Add(new PcnReasonDescription() { Id = id, Name = name, Description = desc });

            lambda("PDN", "Discontinuation", "Unit is no longer produced by the original manufacturer according to original specification.");
            lambda("MANAQ", "Acquisition", "Transfer of a unit, portfolio or production from one production from one manufacturer to another");
            lambda("ALERT", "Alarm", "The manufacturer warns of changes and restrictions that he has detected in a product. For example, functional limitations on the units themselves, but also descriptions of unexpected behavior under certain conditions and also temporary interruptions in the production of the units.");
            lambda("SOFTW", "Change of the software", "Change of the software");
            lambda("LABEL", "Labeling", "Change the labeling of the unit and or packing");
            lambda("CHARA", "Characteristics", "Characteristics such as attribute values of the unit are omitted, are added or changed. They can be electrical, mechanical, thermal or other characteristics kind");
            lambda("DOCUM", "Documentation", "General summary of changes made to the changes made. It does not change characteristics of the units are changed.");
            lambda("NRND", "Restriction of the Recommendation for use", "Official recommendation to no longer use the unit for new developments");
            lambda("FIT", "Fit", "Describes a change in the units of fit and fit with respect to other units connected in the units connected in the product.");
            lambda("FORM", "Shape and Appearance", "Describes a change in the outward appearance of the units. This concerns the spatial dimensions and form, but also colors and surface textures.");
            lambda("FUNCT", "Function", "Changes or effects from operation and performance");
            lambda("INSOL", "Insolvency", "insolvency of the manufacturer");
            lambda("CORR", "Correction", "Correction of documentation without change to the unit");
            lambda("SHIP", "Delivery", "Change of delivery. e.g. container sizes etc. or delivery routes and times");
            lambda("MATER", "Material", "Change of the material or substances in the Material declaration");
            lambda("PRODS", "Production start-up", "The production of this unit is officially started");
            lambda("PPROC", "Production process", "Production process is changed.");
            lambda("PSITE", "Production site", "The production site is changed.");
            lambda("CANCN", "Undo PCN", "A certain previous PCN will be undone reversed");
            lambda("CANDN", "Withdrawal PDN", "Production of the unit is resumed. PDN loses validity.");
            lambda("RECA", "Recall", "The manufacturer recalls the units from the market and explains the reasons and effects on the units themselves. The reasons can be manifold, from technical malfunctions to patent infringements");
            lambda("TESTP", "Test process", "Modification of test processes before, during and after production, before delivery");
            lambda("TESTS", "Test location", "Change of the location where the tests are performed are performed");
            lambda("ORCOD", "Type codes", "Accompanying numbers next to the identifying number of the unit are changed - not the identifying number itself.");

            return res;
        }

        public static Dictionary<string, PcnReasonDescription> BuildDict()
        {
            return GetAll().ToDictionary(x => x.Id, x => x);
        }
    }

    public class PcnItemDescription
    {
        public string Id;
        public string Name;
        public string Description;

        public static List<PcnItemDescription> GetAll()
        {
            var res = new List<PcnItemDescription>();
            Action<string, string, string> lambda = (id, name, desc) =>
                res.Add(new PcnItemDescription() { Id = id, Name = name, Description = desc });

            lambda("ACEL", "Active electronics", "Units with active electronics: semiconductors, electronic assemblies");
            lambda("DACE", "Data / Certificate", "Data media and digital certificates (such as parameter sets, setting values, databases, security certificates, cryptography keys)");
            lambda("SERV", "Service", "Services of all kinds (such as logistics, monitoring, cleaning, maintenance)");
            lambda("DOCU", "Documentation", "Documentation (such as data sheets, descriptions, instructions) DOCU");
            lambda("ELME", "Electromechanics", "Units with electromechanical function (like relays, contactors, switches)");
            lambda("FLUI", "Fluid", "Fluids of all kinds (such as oils, fuels, hydraulic oil, gases)");
            lambda("AUXM", "Auxiliary material", "Auxiliary materials of all kinds (such as chemical substances, operating materials, cleaning agents)");
            lambda("HYDR", "Hydraulics", "Units with hydraulic function (such as hoses, pumps, cylinders)");
            lambda("MECH", "Mechanics", "Units with a purely mechanical function (such as shafts, gears, screws)");
            lambda("MULT", "Several categories", "Not to be assigned to a specific category, affects more than one category, type to be used for the PCN/PDN as a whole when type assignment is specific within the block Item numbers.");
            lambda("PAEL", "Passive electrics / electronics", "Units with passive electrics/electronics, assemblies that do not receive active components.");
            lambda("PNEU", "Pneumatics", "Units with pneumatic function (such as hoses, pumps, valves, cylinders)");
            lambda("RAWM", "Raw material", "Raw materials of all types (such as chemical materials, plastic granules, metals, textiles)");
            lambda("SWFW", "Software / Firmware", "Software including firmware");
            lambda("OTHR", "Other", "Other");
            lambda("CCBL", "Connectors / Cables", "Connectors and cables of all kinds, passive connectivity");
            lambda("ASSY", "Assembly", "Assemblies");

            return res;
        }

        public static Dictionary<string, PcnItemDescription> BuildDict()
        {
            return GetAll().ToDictionary(x => x.Id, x => x);
        }
    }
}

