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
                if (that.Creator != null)
                {
                    that.Creator = (IReference)Transform(that.Creator);
                }

                if(that.Version == null && that.Revision == null && string.IsNullOrEmpty(that.TemplateId) && that.Creator == null)
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
                if (that.Extensions.IsNullOrEmpty())
                {
                    that.Extensions = null;
                }
                else
                {
                    List<IExtension> newExtensions = null;
                    foreach (var ext in that.Extensions)
                    {
                        IExtension newExtension = (IExtension)Transform(ext);
                        if (newExtension != null)
                        {
                            newExtensions ??= new List<IExtension>();
                            newExtensions.Add(newExtension);
                        }
                    }
                    that.Extensions = newExtensions;
                }
                
                if (that.DisplayName.IsNullOrEmpty())
                {
                    that.DisplayName = null;
                }
                else
                {
                    List<ILangStringNameType> newDisplayName = null;
                    foreach (var name in that.DisplayName)
                    {
                        ILangStringNameType newName = (ILangStringNameType)Transform(name);
                        if (newName != null)
                        {
                            newDisplayName ??= new List<ILangStringNameType>();
                            newDisplayName.Add(newName);
                        }
                    }
                    that.DisplayName = newDisplayName;
                }
                
                if (that.Description.IsNullOrEmpty())
                {
                    that.Description = null;
                }
                else
                {
                    List<ILangStringTextType> newDescription = null;
                    foreach (var desc in that.Description)
                    {
                        ILangStringTextType newDesc = (ILangStringTextType)Transform(desc);
                        if (newDesc != null)
                        {
                            newDescription ??= new List<ILangStringTextType>();
                            newDescription.Add(newDesc);
                        }
                    }
                    that.Description = newDescription;
                }

                if (that.Administration != null)
                {
                    that.Administration = (IAdministrativeInformation)Transform(that.Administration);
                }

                if(that.EmbeddedDataSpecifications.IsNullOrEmpty())
                {
                    that.EmbeddedDataSpecifications = null;
                }
                else
                {
                    List<IEmbeddedDataSpecification> newEmbeddedDataSpecs = null;
                    foreach (var embDataSpec in that.EmbeddedDataSpecifications)
                    {
                        IEmbeddedDataSpecification newEmbDataSpec = (IEmbeddedDataSpecification)Transform(embDataSpec);
                        if (newEmbDataSpec != null)
                        {
                            newEmbeddedDataSpecs ??= new List<IEmbeddedDataSpecification>();
                            newEmbeddedDataSpecs.Add(newEmbDataSpec);
                        }
                    }
                    that.EmbeddedDataSpecifications = newEmbeddedDataSpecs;
                }

                if(that.DerivedFrom != null)
                {
                    that.DerivedFrom = (IReference)Transform(that.DerivedFrom);
                }

                if(that.AssetInformation != null)
                {
                    that.AssetInformation = (IAssetInformation)Transform(that.AssetInformation);
                }

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

                if (that.UnitId != null)
                {
                    that.UnitId = (IReference)Transform(that.UnitId);
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

                if(that.ValueList != null)
                {
                    that.ValueList = (IValueList)Transform(that.ValueList);
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
                    //TODO (jtikekar, 2024-05-24): Implement
                    that.DataSpecificationContent = (IDataSpecificationContent)Transform(that.DataSpecificationContent);
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

                TransformSupplimentalSemanticIds(that.SupplementalSemanticIds);

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
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
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

        #endregion

    }
}