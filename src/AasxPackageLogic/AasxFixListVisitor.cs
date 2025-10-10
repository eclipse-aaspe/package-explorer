/*
Copyright (c) 2022 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2022 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using AdminShellNS.Extensions;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Aas = AasCore.Aas3_1;

namespace AasxPackageLogic
{
    public class AasxFixes
    {
        /// <summary>
        /// This function performs fixes to the AAS, where the AAS core framework crashes instead of working through the elements.
        /// </summary>
        /// <param name="env"></param>
        public static void PerformPreFixes(Aas.IEnvironment env)
        {
            // access
            if (env == null)
                return;
            try
            {
                foreach (var cd in env.AllConceptDescriptions())
                {
                    if (cd == null)
                        continue;

                    foreach (var eds in cd.EmbeddedDataSpecifications.ForEachSafe())
                    {
                        if (eds == null)
                            continue;

                        if (eds.DataSpecification?.IsValid() != true)
                        {
                            Log.Singleton.Info($"In CD {cd.IdShort}, fixing an embedded data specification reference.");
                            eds.FixReferenceWrtContent();
                        }

                        // THE FUCK VISITOR PATTERN IS NOT WORKING AND AFTER HOURS AND HOURS I WILL JUST DO IT FOR NORMAL PEOPLE!!!!!!
                        if (eds.DataSpecificationContent is DataSpecificationIec61360 iec61360)
                        {
                            if (iec61360.PreferredName == null)
                                iec61360.PreferredName = new List<ILangStringPreferredNameTypeIec61360>(new[] { new LangStringPreferredNameTypeIec61360("en", "EMPTY") });

                            if (iec61360.PreferredName?.IsValid() != true)
                                iec61360.PreferredName = new List<ILangStringPreferredNameTypeIec61360>(new[] { new LangStringPreferredNameTypeIec61360("en", "EMPTY") });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "when performing pre fixes to AASX.");
            }

            ;
        }
    }

    internal class AasxFixListVisitor : Visitation.AbstractTransformer<IClass>
    {
        public override IClass TransformAdministrativeInformation(IAdministrativeInformation that)
        {
            if (that != null)
            {
                if(string.IsNullOrWhiteSpace(that.Version))
                {
                    that.Version = null;
                }
                
                if(string.IsNullOrWhiteSpace(that.Revision))
                {
                    that.Revision = null;
                }

                if (that.Creator != null)
                {
                    that.Creator = (IReference)Transform(that.Creator);
                }

                if (string.IsNullOrWhiteSpace(that.TemplateId))
                {
                    that.TemplateId = null;
                }

                if (that.Version == null 
                    && that.Revision == null 
                    && string.IsNullOrEmpty(that.TemplateId) 
                    && that.Creator == null)
                {
                    that = null;
                }
            }

            return that;
        }

        public override IClass TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }
                
                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if(that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                //As first and second are mandatory parameters, empty keys are added.
                //This handling is subject to change based on https://github.com/admin-shell-io/aas-specs/issues/412
                if (that.First != null)
                {
                    if(that.First.Keys.IsNullOrEmpty())
                    {
                        var emptyKey = new Key(KeyTypes.GlobalReference, "EMPTY");
                        that.First.Keys.Add(emptyKey);
                    }
                }
                
                if(that.Second != null)
                {
                    if(that.Second.Keys.IsNullOrEmpty())
                    {
                        var emptyKey = new Key(KeyTypes.GlobalReference, "EMPTY");
                        that.Second.Keys.Add(emptyKey);
                    }
                }

                if (that.Annotations.IsNullOrEmpty())
                {
                    that.Annotations = null;
                }
                else
                {
                    List<IDataElement> newAnnotations = null;
                    foreach (var annotation in that.Annotations)
                    {
                        IDataElement newAnnotation = (IDataElement)Transform(annotation);
                        if (newAnnotation != null)
                        {
                            newAnnotations ??= new List<IDataElement>();
                            newAnnotations.Add(newAnnotation);
                        }
                    }
                    that.Annotations = newAnnotations;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.First == null
                    && that.Second == null
                    && that.Annotations == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformAssetAdministrationShell(IAssetAdministrationShell that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }
                
                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);
                
                that.Description = TransformDescription(that.Description);

                if (that.Administration != null)
                {
                    that.Administration = (IAdministrativeInformation)Transform(that.Administration);
                }

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if(that.DerivedFrom != null)
                {
                    that.DerivedFrom = (IReference)Transform(that.DerivedFrom);
                }

                if(that.AssetInformation != null)
                {
                    that.AssetInformation = (IAssetInformation)Transform(that.AssetInformation);
                }

                that.Submodels = TransformReferenceList(that.Submodels);

                if(that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.Administration == null
                    && that.Id == null
                    && that.EmbeddedDataSpecifications == null
                    && that.DerivedFrom == null
                    && that.AssetInformation == null
                    && that.Submodels == null)
                {
                    that = null;
                }
            }

            return that;
        }

        public override IClass TransformAssetInformation(IAssetInformation that)
        {
            if (that != null)
            {
                if (string.IsNullOrWhiteSpace(that.GlobalAssetId))
                {
                    that.GlobalAssetId = null;
                }

                that.SpecificAssetIds = TransformSpecificAssetIds(that.SpecificAssetIds);

                if (string.IsNullOrWhiteSpace(that.AssetType))
                {
                    that.AssetType = null;
                }

                if (that.DefaultThumbnail != null)
                {
                    that.DefaultThumbnail = (IResource)Transform(that.DefaultThumbnail);
                }
            }
            return that;
        }

        public override IClass TransformBasicEventElement(IBasicEventElement that)
        {
            if(that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                //As observed is a mandatory parameters, empty keys are added.
                if (that.Observed != null)
                {
                    if (that.Observed.Keys.IsNullOrEmpty())
                    {
                        var emptyKey = new Key(KeyTypes.GlobalReference, "EMPTY");
                        that.Observed.Keys.Add(emptyKey);
                    }
                }

                if (string.IsNullOrWhiteSpace(that.MessageTopic))
                {
                    that.MessageTopic = null;
                }

                if (that.MessageBroker != null)
                {
                    that.MessageBroker = (IReference)Transform(that.MessageBroker);
                }

                if (string.IsNullOrWhiteSpace(that.LastUpdate))
                {
                    that.LastUpdate = null;
                }

                if (string.IsNullOrWhiteSpace(that.MinInterval))
                {
                    that.MinInterval = null;
                }
                
                if (string.IsNullOrWhiteSpace(that.MaxInterval))
                {
                    that.MaxInterval = null;
                }

                if(that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Observed == null
                    && that.MessageTopic == null
                    && that.MessageBroker == null
                    && that.LastUpdate == null
                    && that.MinInterval == null
                    && that.MaxInterval == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformBlob(IBlob that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if(that.Value.IsNullOrEmpty())
                {
                    that.Value = null;
                }

                //ContentType is not mandatory anymore. So no need for default value.
                if (string.IsNullOrWhiteSpace(that.ContentType))
                {
                    that.ContentType = null;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Value == null
                    && that.ContentType == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformCapability(ICapability that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if(that.Extensions == null 
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformConceptDescription(IConceptDescription that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if(that.Administration != null)
                {
                    that.Administration = (IAdministrativeInformation)Transform(that.Administration);
                }

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if(that.IsCaseOf != null)
                {
                    that.IsCaseOf = TransformReferenceList(that.IsCaseOf);
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.Administration == null
                    && that.Id == null
                    && that.EmbeddedDataSpecifications == null
                    && that.IsCaseOf == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformDataSpecificationIec61360(IDataSpecificationIec61360 that)
        {
            if(that != null)
            {
                // add noise ratio!
                if (that.PreferredName == null)
                {
                    that.PreferredName = new List<ILangStringPreferredNameTypeIec61360>(new[] { new LangStringPreferredNameTypeIec61360("en", "EMPTY") });
                }
                else
                if (that.PreferredName.IsNullOrEmpty())
                {
                    that.PreferredName = new List<ILangStringPreferredNameTypeIec61360>(new[] { new LangStringPreferredNameTypeIec61360("en", "EMPTY") });
                }
                else
                {
                    List<ILangStringPreferredNameTypeIec61360> newPreferredNames = null;
                    foreach (var prefName in that.PreferredName)
                    {
                        ILangStringPreferredNameTypeIec61360 newPrefName = (ILangStringPreferredNameTypeIec61360)Transform(prefName);
                        if (newPrefName != null)
                        {
                            newPreferredNames ??= new List<ILangStringPreferredNameTypeIec61360>();
                            newPreferredNames.Add(newPrefName);
                        }
                    }
                    that.PreferredName = newPreferredNames;
                }
                
                if (that.ShortName.IsNullOrEmpty())
                {
                    that.ShortName = null;
                }
                else
                {
                    List<ILangStringShortNameTypeIec61360> newShortNames = null;
                    foreach (var name in that.ShortName)
                    {
                        ILangStringShortNameTypeIec61360 newShortName = (ILangStringShortNameTypeIec61360)Transform(name);
                        if (newShortName != null)
                        {
                            newShortNames ??= new List<ILangStringShortNameTypeIec61360>();
                            newShortNames.Add(newShortName);
                        }
                    }
                    that.ShortName = newShortNames;
                }

                if (string.IsNullOrWhiteSpace(that.Unit))
                {
                    that.Unit = null;
                }

                if (that.UnitId != null)
                {
                    that.UnitId = (IReference)Transform(that.UnitId);
                }

                if (string.IsNullOrWhiteSpace(that.SourceOfDefinition))
                {
                    that.SourceOfDefinition = null;
                }

                if (string.IsNullOrWhiteSpace(that.Symbol))
                {
                    that.Symbol = null;
                }

                if (that.Definition.IsNullOrEmpty())
                {
                    that.Definition = null;
                }
                else
                {
                    List<ILangStringDefinitionTypeIec61360> newDefinitions = null;
                    foreach (var definition in that.Definition)
                    {
                        ILangStringDefinitionTypeIec61360 newDefinition = (ILangStringDefinitionTypeIec61360)Transform(definition);
                        if (newDefinition != null)
                        {
                            newDefinitions ??= new List<ILangStringDefinitionTypeIec61360>();
                            newDefinitions.Add(newDefinition);
                        }
                    }
                    that.Definition = newDefinitions;
                }

                if (string.IsNullOrWhiteSpace(that.ValueFormat))
                {
                    that.ValueFormat = null;
                }

                if (that.ValueList != null)
                {
                    that.ValueList = (IValueList)Transform(that.ValueList);
                }

                if (string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                if (that.PreferredName == null 
                    && that.ShortName == null 
                    && that.Unit == null
                    && that.UnitId == null 
                    && that.SourceOfDefinition == null
                    && that.Definition == null
                    && that.ValueList == null 
                    && that.Symbol == null
                    && that.DataType == null
                    && that.SourceOfDefinition == null
                    && that.ValueFormat == null
                    && that.Value == null
                    && that.LevelType == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformEmbeddedDataSpecification(IEmbeddedDataSpecification that)
        {
            if(that != null)
            {
                if(that.DataSpecification != null)
                {
                    that.DataSpecification = (IReference)Transform(that.DataSpecification);
                }

                if(that.DataSpecificationContent != null)
                {
                    that.DataSpecificationContent = (IDataSpecificationContent)Transform(that.DataSpecificationContent);
                }


                //This is a workaround to support the interoperability,
                //in case of old AASX Files (V1, V2 or intermediatary V3 versions),
                //where DSContent exists, but not DS
                if(that.DataSpecification == null 
                    && that.DataSpecificationContent != null)
                {
                    that.DataSpecification = new Reference(ReferenceTypes.ExternalReference, new List<IKey>() 
                    { new Key(KeyTypes.GlobalReference, "EMPTY") });
                }
                
                if(that.DataSpecification == null 
                    && that.DataSpecificationContent == null)
                {
                    that = null; 
                }
            }
            
            return that;
        }

        public override IClass TransformEntity(IEntity that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (that.Statements.IsNullOrEmpty())
                {
                    that.Statements = null;
                }
                else
                {
                    List<ISubmodelElement> newStatements = null;
                    foreach (var statement in that.Statements)
                    {
                        ISubmodelElement newStatement = (ISubmodelElement)Transform(statement);
                        if (newStatement != null)
                        {
                            newStatements ??= new List<ISubmodelElement>();
                            newStatements.Add(newStatement);
                        }
                    }
                    that.Statements = newStatements;
                }

                if (string.IsNullOrWhiteSpace(that.GlobalAssetId))
                {
                    that.GlobalAssetId = null;
                }

                that.SpecificAssetIds = TransformSpecificAssetIds(that.SpecificAssetIds);

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Statements == null
                    && that.EntityType == null
                    && that.GlobalAssetId == null
                    && that.SpecificAssetIds == null)
                {
                    that = null;
                }

            }
            return that;
        }

        public override IClass TransformEnvironment(IEnvironment that)
        {
            if(that != null)
            {
                if(that.AssetAdministrationShells.IsNullOrEmpty())
                {
                    that.AssetAdministrationShells = null;
                }
                else
                {
                    List<IAssetAdministrationShell> newAasList = null;
                    foreach(var aas in that.AssetAdministrationShells)
                    {
                        IAssetAdministrationShell newAas = (IAssetAdministrationShell)Transform(aas);
                        if(newAas != null)
                        {
                            newAasList ??= new List<IAssetAdministrationShell>();
                            newAasList.Add(newAas);
                        }
                    }

                    that.AssetAdministrationShells = newAasList;
                }
                
                if(that.Submodels.IsNullOrEmpty())
                {
                    that.Submodels = null;
                }
                else
                {
                    List<ISubmodel> newSubmodelList = null;
                    foreach(var submodel in that.Submodels)
                    {
                        ISubmodel newSubmodel = (ISubmodel)Transform(submodel);
                        if(newSubmodel != null)
                        {
                            newSubmodelList ??= new List<ISubmodel>();
                            newSubmodelList.Add(newSubmodel);
                        }
                    }

                    that.Submodels = newSubmodelList;
                }
                
                if(that.ConceptDescriptions.IsNullOrEmpty())
                {
                    that.ConceptDescriptions = null;
                }
                else
                {
                    List<IConceptDescription> newCDList = null;
                    foreach(var cd in that.ConceptDescriptions)
                    {
                        IConceptDescription newCd = (IConceptDescription)Transform(cd);
                        if(newCd != null)
                        {
                            newCDList ??= new List<IConceptDescription>();
                            newCDList.Add(newCd);
                        }
                    }

                    that.ConceptDescriptions = newCDList;
                }
            }
            return that;
        }

        public override IClass TransformEventPayload(IEventPayload that)
        {
            //TODO (jtikekar, 2024-05-29): Implement
            return that;
        }

        public override IClass TransformExtension(IExtension that)
        {
            if(that != null)
            {
                if(string.IsNullOrWhiteSpace(that.Name))
                {
                    that.Name = null;
                }

                if(that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                if (string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                if (that.RefersTo.IsNullOrEmpty())
                {
                    that.RefersTo = null;
                }
                else
                {
                    List<IReference> newRefersTo = null;
                    foreach (var reference in that.RefersTo)
                    {
                        IReference newReference = (IReference)Transform(reference);
                        if (newReference != null)
                        {
                            newRefersTo ??= new List<IReference>();
                            newRefersTo.Add(newReference);
                        }
                    }

                    that.RefersTo = newRefersTo;
                }

                if(that.Name == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.ValueType == null
                    && that.Value == null
                    && that.RefersTo == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformFile(IFile that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                //ContentType is now allowed to be null, so no default value is set.
                if (string.IsNullOrWhiteSpace(that.ContentType))
                {
                    that.ContentType = null;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.ContentType == null
                    && that.Value == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformKey(IKey that)
        {
            if(that != null)
            {
                if(string.IsNullOrEmpty(that.Value))
                {
                    return null;
                }
            }
            return that;
        }

        public override IClass TransformLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that)
        {
            if (that != null)
            {
                if (!string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that.Text = "EMPTY";
                }

                if (string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
                }

                // MIHO
                if (that != null)
                {
                    var str = that.Language;
                    if (!AdminShellUtil.FixIso6391LangCode(ref str, noneResult: "en"))
                        that.Language = str;
                }
            }
            return that;
        }

        public override IClass TransformLangStringNameType(ILangStringNameType that)
        {
            if(that != null)
            {
                if (!string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that.Text = "EMPTY";
                }

                if (string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
                }

                // MIHO
                if (that != null)
                {
                    var str = that.Language;
                    if (!AdminShellUtil.FixIso6391LangCode(ref str, noneResult: "en"))
                        that.Language = str;
                }
            }
            return that;
        }

        public override IClass TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that)
        {
            if (that != null)
            {
                if (!string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that.Text = "EMPTY";
                }

                if (string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
                }

                // MIHO
                if (that != null)
                {
                    var str = that.Language;
                    if (!AdminShellUtil.FixIso6391LangCode(ref str, noneResult: "en"))
                        that.Language = str;
                }
            }
            return that;
        }

        public override IClass TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that)
        {
            if (that != null)
            {
                if (!string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that.Text = "EMPTY";
                }

                if (string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
                }

                // MIHO
                if (that != null)
                {
                    var str = that.Language;
                    if (!AdminShellUtil.FixIso6391LangCode(ref str, noneResult: "en"))
                        that.Language = str;
                }
            }
            return that;
        }

        public override IClass TransformLangStringTextType(ILangStringTextType that)
        {
            if (that != null)
            {
                if (!string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that.Text = "EMPTY";
                }

                if (string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
                }

                // MIHO
                if (that != null)
                {
                    var str = that.Language;
                    if (AdminShellUtil.FixIso6391LangCode(ref str, noneResult: "en"))
                        that.Language = str;
                }
            }
            return that;
        }

        public override IClass TransformLevelType(ILevelType that)
        {
            return that;
        }

        public override IClass TransformMultiLanguageProperty(IMultiLanguageProperty that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (that.Value.IsNullOrEmpty())
                {
                    that.Value = null;
                }
                else
                {
                    List<ILangStringTextType> newValueList = null;
                    foreach (var value in that.Value)
                    {
                        ILangStringTextType newValue = (ILangStringTextType)Transform(value);
                        if (newValue != null)
                        {
                            newValueList ??= new List<ILangStringTextType>();
                            newValueList.Add(newValue);
                        }
                    }

                    that.Value = newValueList;
                }

                if (that.ValueId != null)
                {
                    that.ValueId = (IReference)Transform(that.ValueId);
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Value == null
                    && that.ValueId == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformOperation(IOperation that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (that.InputVariables.IsNullOrEmpty())
                {
                    that.InputVariables = null;
                }
                else
                {
                    List<IOperationVariable> newInputVariables = null;
                    foreach (var inOpVariable in that.InputVariables)
                    {
                        IOperationVariable newInOpVariable = (IOperationVariable)Transform(inOpVariable);
                        if (newInOpVariable != null)
                        {
                            newInputVariables ??= new List<IOperationVariable>();
                            newInputVariables.Add(newInOpVariable);
                        }
                    }

                    that.InputVariables = newInputVariables;
                }
                
                if (that.OutputVariables.IsNullOrEmpty())
                {
                    that.OutputVariables = null;
                }
                else
                {
                    List<IOperationVariable> newOutputVariables = null;
                    foreach (var outOpVariable in that.OutputVariables)
                    {
                        IOperationVariable newOutOpVariable = (IOperationVariable)Transform(outOpVariable);
                        if (newOutOpVariable != null)
                        {
                            newOutputVariables ??= new List<IOperationVariable>();
                            newOutputVariables.Add(newOutOpVariable);
                        }
                    }

                    that.OutputVariables = newOutputVariables;
                }
                
                if (that.InoutputVariables.IsNullOrEmpty())
                {
                    that.InoutputVariables = null;
                }
                else
                {
                    List<IOperationVariable> newInOutVariables = null;
                    foreach (var inOutOpVariable in that.InoutputVariables)
                    {
                        IOperationVariable newInOutOpVariable = (IOperationVariable)Transform(inOutOpVariable);
                        if (newInOutOpVariable != null)
                        {
                            newInOutVariables ??= new List<IOperationVariable>();
                            newInOutVariables.Add(newInOutOpVariable);
                        }
                    }

                    that.InoutputVariables = newInOutVariables;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.InputVariables == null
                    && that.OutputVariables == null
                    && that.InoutputVariables == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformOperationVariable(IOperationVariable that)
        {
            if (that != null)
            {
                that.Value = (ISubmodelElement)Transform(that.Value);
            }
            return that;
        }

        public override IClass TransformProperty(IProperty that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                if (that.ValueId != null)
                {
                    that.ValueId = (IReference)Transform(that.ValueId);
                }

                if (that.Value != null 
                    && (that.ValueType == DataTypeDefXsd.Double 
                        || that.ValueType == DataTypeDefXsd.Float
                        || that.ValueType == DataTypeDefXsd.Decimal))
                {
                    var str = that.Value;
                    if (AdminShellUtil.FixFloatingPointString(ref str, noneResult: "0.0"))
                        that.Value = str;
                }

                if (that.Value != null
                    && (that.ValueType == DataTypeDefXsd.Integer 
                        || that.ValueType == DataTypeDefXsd.Long
                        || that.ValueType == DataTypeDefXsd.Short
                        || that.ValueType == DataTypeDefXsd.NegativeInteger
                        || that.ValueType == DataTypeDefXsd.NonNegativeInteger
                        || that.ValueType == DataTypeDefXsd.NonPositiveInteger
                        || that.ValueType == DataTypeDefXsd.PositiveInteger
                        || that.ValueType == DataTypeDefXsd.UnsignedByte
                        || that.ValueType == DataTypeDefXsd.UnsignedInt
                        || that.ValueType == DataTypeDefXsd.UnsignedLong
                        || that.ValueType == DataTypeDefXsd.UnsignedShort))
                {
                    var str = that.Value;
                    if (AdminShellUtil.FixIntegerString(ref str, noneResult: "0.0"))
                        that.Value = str;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Value == null
                    && that.ValueId == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformQualifier(IQualifier that)
        {
            if(that != null)
            {
                if(that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }
                  
                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                if (string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                if (that.ValueId != null)
                {
                    that.ValueId = (IReference)Transform(that.ValueId);
                }

                if(string.IsNullOrWhiteSpace(that.Type))
                {
                    that.Type = null;
                }
                
                if(string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                if(that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Type == null
                    && that.ValueType == null
                    && that.Kind == null
                    && that.Value == null
                    && that.ValueId == null)
                { 
                    that = null; 
                }

            }
            return that;
        }

        public override IClass TransformRange(IRange that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (string.IsNullOrWhiteSpace(that.Min))
                {
                    that.Min = null;
                }
                
                if (string.IsNullOrWhiteSpace(that.Max))
                {
                    that.Max = null;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.ValueType == null
                    && that.Min == null
                    && that.Max == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformReference(IReference that)
        {
            if(that != null)
            {
                if(that.Keys.IsNullOrEmpty())
                {
                    that = null;
                }
                else
                {
                    List<IKey> newKeys = null;
                    foreach (var key in that.Keys)
                    {
                        IKey newKey = (IKey)Transform(key);
                        if (newKey != null)
                        {
                            newKeys ??= new List<IKey>();
                            newKeys.Add(newKey);
                        }
                    }

                    if (!newKeys.IsNullOrEmpty())
                    {
                        that.Keys = newKeys; 
                    }
                    else
                    {
                        that = null;
                    }

                    // MIHO .. add more
                    if (that?.Keys != null && that.Keys.Count > 0)
                    {
                        var fk = that.Keys.First();

                        var ext = fk.Type == KeyTypes.GlobalReference;

                        if (ext && that.Type == ReferenceTypes.ModelReference)
                        {
                            that.Type = ReferenceTypes.ExternalReference;
                        }

                        if (!ext && that.Type == ReferenceTypes.ExternalReference)
                        {
                            that.Type = ReferenceTypes.ModelReference;
                        }
                    }
                }
            }
            return that;
        }

        public override IClass TransformReferenceElement(IReferenceElement that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (that.Value != null)
                {
                    that.Value = (IReference)Transform(that.Value);
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Value == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformRelationshipElement(IRelationshipElement that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                //first and second are no more mandatory parameters
                if (that.First != null)
                {
                    that.First = (IReference)Transform(that.First);
                }

                if (that.Second != null)
                {
                    that.Second = (IReference)Transform(that.Second);
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.First == null
                    && that.Second == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformResource(IResource that)
        {
            if(that != null)
            {
                if (string.IsNullOrWhiteSpace(that.Path))
                {
                    that.Path = null;
                }
                
                if (string.IsNullOrWhiteSpace(that.ContentType))
                {
                    that.ContentType = null;
                }

                if(that.Path == null
                    && that.ContentType == null)
                {
                    that = null;
                }
            }
            
            return that;
        }

        public override IClass TransformSpecificAssetId(ISpecificAssetId that)
        {
            if(that != null)
            {
                if(that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                if (that.ExternalSubjectId != null)
                {
                    that.ExternalSubjectId = (IReference)Transform(that.ExternalSubjectId);
                }

                if (string.IsNullOrWhiteSpace(that.Name))
                {
                    that.Name = null;
                }

                if (string.IsNullOrWhiteSpace(that.Value))
                {
                    that.Value = null;
                }

                if(that.Name == null
                    && that.Value == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.ExternalSubjectId == null)
                { 
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformSubmodel(ISubmodel that)
        {
            if(that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if(string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }
                
                if(string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }
                
                if(string.IsNullOrWhiteSpace(that.Id))
                {
                    that.Id = null;
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.Administration != null)
                {
                    that.Administration = (IAdministrativeInformation)Transform(that.Administration);
                }

                if(that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (that.SubmodelElements.IsNullOrEmpty())
                {
                    that.SubmodelElements = null;
                }
                else
                {
                    List<ISubmodelElement> newSubmodelElements = null;
                    foreach (var submodelElement in that.SubmodelElements)
                    {
                        ISubmodelElement newSubmodelElement = (ISubmodelElement)Transform(submodelElement);
                        if (newSubmodelElement != null)
                        {
                            newSubmodelElements ??= new List<ISubmodelElement>();
                            newSubmodelElements.Add(newSubmodelElement);
                        }
                    }

                    that.SubmodelElements = newSubmodelElements;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Id == null
                    && that.Administration == null
                    && that.Kind == null
                    && that.SubmodelElements == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformSubmodelElementCollection(ISubmodelElementCollection that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if (that.Value.IsNullOrEmpty())
                {
                    that.Value = null;
                }
                else
                {
                    List<ISubmodelElement> newSubmodelElements = null;
                    foreach (var submodelElement in that.Value)
                    {
                        ISubmodelElement newSubmodelElement = (ISubmodelElement)Transform(submodelElement);
                        if (newSubmodelElement != null)
                        {
                            newSubmodelElements ??= new List<ISubmodelElement>();
                            newSubmodelElements.Add(newSubmodelElement);
                        }
                    }

                    that.Value = newSubmodelElements;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.Value == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformSubmodelElementList(ISubmodelElementList that)
        {
            if (that != null)
            {
                that.Extensions = TransformExtensions(that.Extensions);

                if (string.IsNullOrWhiteSpace(that.Category))
                {
                    that.Category = null;
                }

                if (string.IsNullOrWhiteSpace(that.IdShort))
                {
                    that.IdShort = "EMPTY";
                }

                that.DisplayName = TransformDisplayName(that.DisplayName);

                that.Description = TransformDescription(that.Description);

                if (that.SemanticId != null)
                {
                    that.SemanticId = (IReference)Transform(that.SemanticId);
                }

                that.SupplementalSemanticIds = TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

                that.Qualifiers = TransformQualifiers(that.Qualifiers);

                that.EmbeddedDataSpecifications = TransformEmbeddedDataSpecifications(that.EmbeddedDataSpecifications);

                if(that.SemanticIdListElement != null)
                {
                    that.SemanticIdListElement = (IReference)Transform(that.SemanticIdListElement);
                }

                if (that.Value.IsNullOrEmpty())
                {
                    that.Value = null;
                }
                else
                {
                    List<ISubmodelElement> newSubmodelElements = null;
                    foreach (var submodelElement in that.Value)
                    {
                        ISubmodelElement newSubmodelElement = (ISubmodelElement)Transform(submodelElement);
                        if (newSubmodelElement != null)
                        {
                            newSubmodelElements ??= new List<ISubmodelElement>();
                            newSubmodelElements.Add(newSubmodelElement);
                        }
                    }

                    that.Value = newSubmodelElements;
                }

                if (that.Extensions == null
                    && that.Category == null
                    && that.IdShort == null
                    && that.DisplayName == null
                    && that.Description == null
                    && that.SemanticId == null
                    && that.SupplementalSemanticIds == null
                    && that.Qualifiers == null
                    && that.EmbeddedDataSpecifications == null
                    && that.TypeValueListElement == null
                    && that.OrderRelevant == null
                    && that.SemanticIdListElement == null
                    && that.ValueTypeListElement == null
                    && that.Value == null)
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformValueList(IValueList that)
        {
            if(that != null)
            {
                if (that.ValueReferencePairs.IsNullOrEmpty())
                {
                    that.ValueReferencePairs = null;
                }
                else
                {
                    List<IValueReferencePair> newValueRefPair = null;
                    foreach (var valRef in that.ValueReferencePairs)
                    {
                        IValueReferencePair newValRef = (IValueReferencePair)Transform(valRef);
                        if (newValRef != null)
                        {
                            newValueRefPair ??= new List<IValueReferencePair>();
                            newValueRefPair.Add(newValRef);
                        }
                    }

                    that.ValueReferencePairs = newValueRefPair;
                }
            }
            return that;
        }

        public override IClass TransformValueReferencePair(IValueReferencePair that)
        {
            if(that != null)
            {
                that.ValueId = (IReference)Transform(that.ValueId);
            }
            return that;
        }

        #region Private Methods

        private List<IReference> TransformSupplimentalSemanticIds(List<IReference> that)
        {
            if (that.IsNullOrEmpty())
            {
                return null;
            }
            else
            {
                List<IReference> newSupplSemIds = null;
                foreach (var suppSemId in that)
                {
                    IReference newSuppSemId = (IReference)Transform(suppSemId);
                    if (newSuppSemId != null)
                    {
                        newSupplSemIds ??= new List<IReference>();
                        newSupplSemIds.Add(newSuppSemId);
                    }
                }

                return newSupplSemIds;
            }
        }

        private List<IExtension> TransformExtensions(List<IExtension> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<IExtension> newExtensions = null;
                foreach (var ext in that)
                {
                    IExtension newExtension = (IExtension)Transform(ext);
                    if (newExtension != null)
                    {
                        newExtensions ??= new List<IExtension>();
                        newExtensions.Add(newExtension);
                    }
                }
                that = newExtensions;
            }

            return that;
        }

        private List<ILangStringNameType> TransformDisplayName(List<ILangStringNameType> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<ILangStringNameType> newDisplayName = null;
                foreach (var name in that)
                {
                    ILangStringNameType newName = (ILangStringNameType)Transform(name);
                    if (newName != null)
                    {
                        newDisplayName ??= new List<ILangStringNameType>();
                        newDisplayName.Add(newName);
                    }
                }
                that = newDisplayName;
            }

            return that;
        }

        private List<ILangStringTextType> TransformDescription(List<ILangStringTextType> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<ILangStringTextType> newDescription = null;
                foreach (var desc in that)
                {
                    ILangStringTextType newDesc = (ILangStringTextType)Transform(desc);
                    if (newDesc != null)
                    {
                        newDescription ??= new List<ILangStringTextType>();
                        newDescription.Add(newDesc);
                    }
                }
                that = newDescription;
            }

            return that;
        }
        
        private List<IQualifier> TransformQualifiers(List<IQualifier> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<IQualifier> newQualifiers = null;
                foreach (var qualifier in that)
                {
                    IQualifier newQualifier = (IQualifier)Transform(qualifier);
                    if (newQualifier != null)
                    {
                        newQualifiers ??= new List<IQualifier>();
                        newQualifiers.Add(newQualifier);
                    }
                }
                that = newQualifiers;
            }

            return that;
        }

        private List<IEmbeddedDataSpecification> TransformEmbeddedDataSpecifications(List<IEmbeddedDataSpecification> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<IEmbeddedDataSpecification> newEmbeddedDataSpecs = null;
                foreach (var embDataSpec in that)
                {
                    IEmbeddedDataSpecification newEmbDataSpec = (IEmbeddedDataSpecification)Transform(embDataSpec);
                    if (newEmbDataSpec != null)
                    {
                        newEmbeddedDataSpecs ??= new List<IEmbeddedDataSpecification>();
                        newEmbeddedDataSpecs.Add(newEmbDataSpec);
                    }
                }
                that = newEmbeddedDataSpecs;
            }

            return that;
        }

        private List<IReference> TransformReferenceList(List<IReference> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<IReference> newReferenceList = null;
                foreach (var reference in that)
                {
                    IReference newReference = (IReference)Transform(reference);
                    if (newReference != null)
                    {
                        newReferenceList ??= new List<IReference>();
                        newReferenceList.Add(newReference);
                    }
                }
                that = newReferenceList;
            }

            return that;
        }

        private List<ISpecificAssetId> TransformSpecificAssetIds(List<ISpecificAssetId> that)
        {
            if (that.IsNullOrEmpty())
            {
                that = null;
            }
            else
            {
                List<ISpecificAssetId> newSpecificAssetIds = null;
                foreach (var specAssetId in that)
                {
                    ISpecificAssetId newSpecAssetId = (ISpecificAssetId)Transform(specAssetId);
                    if (newSpecAssetId != null)
                    {
                        newSpecificAssetIds ??= new List<ISpecificAssetId>();
                        newSpecificAssetIds.Add(newSpecAssetId);
                    }
                }
                that = newSpecificAssetIds;
            }

            return that;
        }

        #endregion

    }
}