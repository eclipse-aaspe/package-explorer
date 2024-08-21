﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AdminShellNS;
using AdminShellNS.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Extensions
{
    public static class ExtendAssetAdministrationShell
    {
        #region AasxPackageExplorer

        public static Tuple<string, string> ToCaptionInfo(this IAssetAdministrationShell assetAdministrationShell)
        {
            var caption = AdminShellUtil.EvalToNonNullString("\"{0}\" ", assetAdministrationShell.IdShort, "\"AAS\"");
            if (assetAdministrationShell.Administration != null)
                caption += "V" + assetAdministrationShell.Administration.Version + "." + assetAdministrationShell.Administration.Revision;

            var info = "";
            if (assetAdministrationShell.Id != null)
                info = $"[{assetAdministrationShell.Id}]";
            return Tuple.Create(caption, info);
        }

        public static IEnumerable<LocatedReference> FindAllReferences(this IAssetAdministrationShell assetAdministrationShell)
        {
            // dead-csharp off
            // Asset
            //TODO (jtikekar, 0000-00-00): support asset
            //if (assetAdministrationShell.AssetInformation != null)
            //    yield return new LocatedReference(assetAdministrationShell, assetAdministrationShell.AssetInformation);
            // dead-csharp on
            // Submodel references
            if (assetAdministrationShell.Submodels != null)
                foreach (var r in assetAdministrationShell.Submodels)
                    yield return new LocatedReference(assetAdministrationShell, r);

        }

        #endregion

        public static IReference FindSubmodelReference(this IAssetAdministrationShell aas, IReference smRef)
        {
            if (aas?.Submodels == null || smRef == null)
                return null;

            foreach (var smr in aas.Submodels)
                if (smr.Matches(smRef))
                    return smr;

            return null;
        }

        public static bool HasSubmodelReference(this IAssetAdministrationShell aas, Reference smRef)
        {
            return aas.FindSubmodelReference(smRef) != null;
        }

        /// <summary>
        /// Adds. Might create the list.
        /// </summary>
        public static void Add(this IAssetAdministrationShell aas, IReference newSmRef)
        {
            if (aas == null)
                return;
            if (aas.Submodels == null)
                aas.Submodels = new List<IReference>();

            aas.Submodels.Add(newSmRef);
        }

        /// <summary>
        /// Removes. Might set the list to <c>null</c> !!
        /// Note: <c>smRef</c> must be the exact object, not only match it!
        /// </summary>
        public static void Remove(this IAssetAdministrationShell aas, IReference smRef)
        {
            if (aas?.Submodels == null)
                return;
            if (aas.Submodels.Contains(smRef))
                aas.Submodels.Remove(smRef);
            if (aas.Submodels.Count < 1)
                aas.Submodels = null;
        }

        //TODO (jtikekar, 0000-00-00): Change the name, currently based on older implementation
        public static string GetFriendlyName(this IAssetAdministrationShell assetAdministrationShell)
        {
            if (string.IsNullOrEmpty(assetAdministrationShell.IdShort))
            {
                return null;
            }

            return Regex.Replace(assetAdministrationShell.IdShort, @"[^a-zA-Z0-9\-_]", "_");
        }

        public static AssetAdministrationShell ConvertFromV10(this AssetAdministrationShell assetAdministrationShell, AasxCompatibilityModels.AdminShellV10.AdministrationShell sourceAas)
        {
            if (sourceAas == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceAas.idShort))
            {
                assetAdministrationShell.IdShort = "";
            }
            else
            {
                assetAdministrationShell.IdShort = sourceAas.idShort;
            }

            if (sourceAas.description != null)
            {
                assetAdministrationShell.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceAas.description);
            }

            if (sourceAas.administration != null)
            {
                assetAdministrationShell.Administration = new AdministrativeInformation(version: sourceAas.administration.version, revision: sourceAas.administration.revision);
            }

            if (sourceAas.derivedFrom != null)
            {
                var newKeyList = new List<IKey>();

                foreach (var sourceKey in sourceAas.derivedFrom.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(sourceKey.type);
                    if (keyType != null)
                    {
                        newKeyList.Add(new Key((KeyTypes)keyType, sourceKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {sourceKey.type} not found.");
                    }
                }
                assetAdministrationShell.DerivedFrom = new Reference(ReferenceTypes.ExternalReference, newKeyList);
            }

            if (!sourceAas.submodelRefs.IsNullOrEmpty())
            {
                foreach (var submodelRef in sourceAas.submodelRefs)
                {
                    if (!submodelRef.IsEmpty)
                    {
                        var keyList = new List<IKey>();
                        foreach (var refKey in submodelRef.Keys)
                        {
                            var keyType = Stringification.KeyTypesFromString(refKey.type);
                            if (keyType != null)
                            {
                                keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                            }
                            else
                            {
                                Console.WriteLine($"KeyType value {refKey.type} not found.");
                            }
                        }
                        assetAdministrationShell.Submodels ??= new List<IReference>();
                        assetAdministrationShell.Submodels.Add(new Reference(ReferenceTypes.ModelReference, keyList)); 
                    }
                }
            }

            if (sourceAas.hasDataSpecification != null && sourceAas.hasDataSpecification.reference.Count > 0)
            {
                //TODO (jtikekar, 0000-00-00): EmbeddedDataSpecification?? (as per old implementation)
                assetAdministrationShell.EmbeddedDataSpecifications ??= new List<IEmbeddedDataSpecification>();
                foreach (var dataSpecification in sourceAas.hasDataSpecification.reference)
                {
                    if (!dataSpecification.IsEmpty)
                    {
                        assetAdministrationShell.EmbeddedDataSpecifications.Add(new EmbeddedDataSpecification(
                                        ExtensionsUtil.ConvertReferenceFromV10(dataSpecification, ReferenceTypes.ExternalReference),
                                        null));
                    }
                }
            }

            return assetAdministrationShell;
        }

        public static AssetAdministrationShell ConvertFromV20(this AssetAdministrationShell assetAdministrationShell, AasxCompatibilityModels.AdminShellV20.AdministrationShell sourceAas)
        {
            if (sourceAas == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceAas.idShort))
            {
                assetAdministrationShell.IdShort = "";
            }
            else
            {
                assetAdministrationShell.IdShort = sourceAas.idShort;
            }

            if (sourceAas.description != null)
            {
                assetAdministrationShell.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceAas.description);
            }

            if (sourceAas.administration != null)
            {
                assetAdministrationShell.Administration = new AdministrativeInformation(version: sourceAas.administration.version, revision: sourceAas.administration.revision);
            }

            if (sourceAas.derivedFrom != null)
            {
                var newKeyList = new List<IKey>();

                foreach (var sourceKey in sourceAas.derivedFrom.Keys)
                {
                    var keyType = Stringification.KeyTypesFromString(sourceKey.type);
                    if (keyType != null)
                    {
                        newKeyList.Add(new Key((KeyTypes)keyType, sourceKey.value));
                    }
                    else
                    {
                        Console.WriteLine($"KeyType value {sourceKey.type} not found.");
                    }
                }
                assetAdministrationShell.DerivedFrom = new Reference(ReferenceTypes.ExternalReference, newKeyList);
            }

            if (!sourceAas.submodelRefs.IsNullOrEmpty())
            {
                foreach (var submodelRef in sourceAas.submodelRefs)
                {
                    if (!submodelRef.IsEmpty)
                    {
                        var keyList = new List<IKey>();
                        foreach (var refKey in submodelRef.Keys)
                        {
                            var keyType = Stringification.KeyTypesFromString(refKey.type);
                            if (keyType != null)
                            {
                                keyList.Add(new Key((KeyTypes)keyType, refKey.value));
                            }
                            else
                            {
                                Console.WriteLine($"KeyType value {refKey.type} not found.");
                            }
                        }
                        assetAdministrationShell.Submodels ??= new List<IReference>();
                        assetAdministrationShell.Submodels.Add(new Reference(ReferenceTypes.ModelReference, keyList)); 
                    }
                }
            }

            if (sourceAas.hasDataSpecification != null && sourceAas.hasDataSpecification.Count > 0)
            {
                //TODO (jtikekar, 0000-00-00): EmbeddedDataSpecification?? (as per old implementation)
                if (assetAdministrationShell.EmbeddedDataSpecifications == null)
                {
                    assetAdministrationShell.EmbeddedDataSpecifications = new List<IEmbeddedDataSpecification>();
                }

                //TODO (jtikekar, 0000-00-00): DataSpecificationContent?? (as per old implementation)
                foreach (var sourceDataSpec in sourceAas.hasDataSpecification)
                {
                    if (sourceDataSpec.dataSpecification != null)
                    {
                        assetAdministrationShell.EmbeddedDataSpecifications.Add(
                            new EmbeddedDataSpecification(
                                ExtensionsUtil.ConvertReferenceFromV20(sourceDataSpec.dataSpecification, ReferenceTypes.ExternalReference),
                                null));
                    }
                }
            }

            return assetAdministrationShell;
        }
    }
}
