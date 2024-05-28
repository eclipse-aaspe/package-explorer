/*
Copyright (c) 2022 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2022 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS.Extensions;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AasxPackageLogic
{
    internal class EmptyListVisitor : Visitation.AbstractTransformer<IClass>
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

                if (that.Version == null && that.Revision == null && string.IsNullOrEmpty(that.TemplateId) && that.Creator == null)
                    that = null;
            }

            return that;
        }

        public override IClass TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            throw new System.NotImplementedException();
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
                    that.IdShort = null;
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

                //TODO (jtikekar, 2024-05-28): Refactor, single method for list if references
                if (that.Submodels.IsNullOrEmpty())
                {
                    that.Submodels = null;
                }
                else
                {
                    List<IReference> newSubmodelRefs = null;
                    foreach (var submodelRef in that.Submodels)
                    {
                        IReference newSubmodelRef = (IReference)Transform(submodelRef);
                        if (newSubmodelRef != null)
                        {
                            newSubmodelRefs ??= new List<IReference>();
                            newSubmodelRefs.Add(newSubmodelRef);
                        }
                    }
                    that.Submodels = newSubmodelRefs;
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

                if (that.SpecificAssetIds.IsNullOrEmpty())
                {
                    that.SpecificAssetIds = null;
                }
                else
                {
                    List<ISpecificAssetId> newSpecificAssetIds = null;
                    foreach (var specAssetId in that.SpecificAssetIds)
                    {
                        ISpecificAssetId newSpecAssetId = (ISpecificAssetId)Transform(specAssetId);
                        if (newSpecAssetId != null)
                        {
                            newSpecificAssetIds ??= new List<ISpecificAssetId>();
                            newSpecificAssetIds.Add(newSpecAssetId);
                        }
                    }
                    that.SpecificAssetIds = newSpecificAssetIds;
                }

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
            throw new System.NotImplementedException();
        }

        public override IClass TransformBlob(IBlob that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformCapability(ICapability that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformConceptDescription(IConceptDescription that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformDataSpecificationIec61360(IDataSpecificationIec61360 that)
        {
            if(that != null)
            {
                if (that.PreferredName.IsNullOrEmpty())
                {
                    that.PreferredName = null;
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

                if (that.PreferredName == null && that.ShortName == null 
                    && that.UnitId == null && that.Definition == null
                    && that.ValueList == null && string.IsNullOrEmpty(that.Unit)
                    && string.IsNullOrEmpty(that.SourceOfDefinition)
                    && string.IsNullOrEmpty(that.Symbol)
                    && string.IsNullOrEmpty(that.ValueFormat)
                    && string.IsNullOrEmpty(that.Value))
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
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }

        public override IClass TransformExtension(IExtension that)
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
            }
            return that;
        }

        public override IClass TransformFile(IFile that)
        {
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringNameType(ILangStringNameType that)
        {
            if(that != null)
            {
                if(string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
                }
            }
            return that;
        }

        public override IClass TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringTextType(ILangStringTextType that)
        {
            if (that != null)
            {
                if (string.IsNullOrEmpty(that.Language) && string.IsNullOrEmpty(that.Text))
                {
                    that = null;
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
            throw new System.NotImplementedException();
        }

        public override IClass TransformOperation(IOperation that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformOperationVariable(IOperationVariable that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformProperty(IProperty that)
        {
            throw new System.NotImplementedException();
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
            }
            return that;
        }

        public override IClass TransformRange(IRange that)
        {
            throw new System.NotImplementedException();
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
                }
            }
            return that;
        }

        public override IClass TransformReferenceElement(IReferenceElement that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformRelationshipElement(IRelationshipElement that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformResource(IResource that)
        {
            if(that != null)
            {
                if (string.IsNullOrWhiteSpace(that.ContentType))
                {
                    that.ContentType = null;
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
                    that.IdShort = null;
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
            }
            return that;
        }

        public override IClass TransformSubmodelElementCollection(ISubmodelElementCollection that)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformSubmodelElementList(ISubmodelElementList that)
        {
            throw new System.NotImplementedException();
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

        #endregion

    }
}