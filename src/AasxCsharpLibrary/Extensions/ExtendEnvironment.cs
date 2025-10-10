/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AdminShellNS;
using AdminShellNS.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Extensions
{
    public static class ExtendEnvironment
    {
        #region AasxPackageExplorer

        public static void RecurseOnReferables(this AasCore.Aas3_1.IEnvironment environment,
                object state, Func<object, List<IReferable>, IReferable, bool> lambda, bool includeThis = false)
        {
            // includeThis does not make sense, as no Referable
            // just use the others
            foreach (var idf in environment.FindAllReferable(onlyIdentifiables: true))
                idf?.RecurseOnReferables(state, lambda, includeThis);
        }

        #endregion

        /// <summary>
        /// Deprecated? Not compatible with AAS core?
        /// </summary>
        public static AasValidationRecordList ValidateAll(this AasCore.Aas3_1.IEnvironment environment)
        {
            // collect results
            var results = new AasValidationRecordList();

            // all entities
            foreach (var rf in environment.FindAllReferable())
                rf.Validate(results);

            // give back
            return results;
        }

        /// <summary>
        /// Deprecated? Not compatible with AAS core?
        /// </summary>
        public static int AutoFix(this AasCore.Aas3_1.IEnvironment environment, IEnumerable<AasValidationRecord> records)
        {
            // access
            if (records == null)
                return -1;

            // collect Referables (expensive safety measure)
            var allowedReferables = environment.FindAllReferable().ToList();

            // go thru records
            int res = 0;
            foreach (var rec in records)
            {
                // access 
                if (rec == null || rec.Fix == null || rec.Source == null)
                    continue;

                // minimal safety measure
                if (!allowedReferables.Contains(rec.Source))
                    continue;

                // apply fix
                res++;
                try
                {
                    rec.Fix.Invoke();
                }
                catch
                {
                    res--;
                }
            }

            // return number of applied fixes
            return res;
        }

        /// <summary>
        /// This function tries to silently fix some issues preventing the environment
        /// are parts of it to be properly serilaized.
        /// </summary>
        /// <returns>Number of fixes taken</returns>
        public static int SilentFix30(this AasCore.Aas3_1.IEnvironment env)
        {
            // access
            int res = 0;
            if (env == null)
                return res;

            // AAS core crashes without AssetInformation
            foreach (var aas in env.AllAssetAdministrationShells())
                if (aas.AssetInformation == null)
                {
                    aas.AssetInformation = new AssetInformation(assetKind: AssetKind.NotApplicable);
                    res++;
                }

            // AAS core crashes without EmbeddedDataSpecification.DataSpecificationContent
            // AAS core crashes without EmbeddedDataSpecification.DataSpecificationContent.PreferredName
            foreach (var rf in env.FindAllReferable())
                if (rf is IHasDataSpecification hds)
                    if (hds.EmbeddedDataSpecifications != null)
                        foreach (var eds in hds.EmbeddedDataSpecifications)
                        {
                            if (eds.DataSpecificationContent == null)
                                eds.DataSpecificationContent =
                                    new DataSpecificationIec61360(
                                        new List<ILangStringPreferredNameTypeIec61360>());
                        }

            // ok
            return res;
        }

        public static IEnumerable<IReferable> FindAllReferable(this AasCore.Aas3_1.IEnvironment environment, bool onlyIdentifiables = false)
        {
            foreach (var aas in environment.AllAssetAdministrationShells())
                if (aas != null)
                {
                    // AAS itself
                    yield return aas;
                }

            foreach (var sm in environment.AllSubmodels())
                if (sm != null)
                {
                    yield return sm;

                    if (!onlyIdentifiables)
                    {
                        // TODO (MIHO, 2020-08-26): not very elegant, yet. Avoid temporary collection
                        var allsme = new List<ISubmodelElement>();
                        sm.RecurseOnSubmodelElements(null, (state, parents, sme) =>
                        {
                            allsme.Add(sme); return true;
                        });
                        foreach (var sme in allsme)
                            yield return sme;
                    }
                }

            foreach (var cd in environment.AllConceptDescriptions())
                if (cd != null)
                    yield return cd;
        }

        public static IEnvironment ConvertFromV30(this IEnvironment environment, AasCore.Aas3_0.IEnvironment sourceEnvironment)
        {
            // access
            if (sourceEnvironment == null)
                return environment;
            // create new environment
            if (environment == null)
                environment = new AasCore.Aas3_1.Environment();

            // As there are only few metamodel changes, that too backward compatible, following approach should be sufficient.
            var env30_json = AasCore.Aas3_0.Jsonization.Serialize.ToJsonObject(sourceEnvironment);
            environment = AasCore.Aas3_1.Jsonization.Deserialize.EnvironmentFrom(env30_json);

            // ok
            return environment;
        }


#if !DoNotUseAasxCompatibilityModels

        public static AasCore.Aas3_1.IEnvironment ConvertFromV10(this AasCore.Aas3_1.IEnvironment environment, AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv sourceEnvironement)
        {
            // Convert Administration Shells
            foreach (var sourceAas in sourceEnvironement.AdministrationShells.ForEachSafe())
            {
                var newAssetInformation = new AssetInformation(AssetKind.Instance);
                var newAas = new AssetAdministrationShell(
                    id: sourceAas.identification.id, newAssetInformation);
                environment.Add(newAas);

                var sourceAsset = sourceEnvironement?.FindAsset(sourceAas.assetRef);
                if (sourceAsset != null)
                {
                    newAssetInformation = newAssetInformation.ConvertFromV10(sourceAsset);
                    newAas.AssetInformation = newAssetInformation;
                }
            }

            // Convert Submodels
            foreach (var sourceSubmodel in sourceEnvironement.Submodels.ForEachSafe())
            {
                var newSubmodel = new Submodel(sourceSubmodel.identification.id);
                newSubmodel = newSubmodel.ConvertFromV10(sourceSubmodel);
                environment.Add(newSubmodel);
            }

            // Convert CDs
            foreach (var sourceConceptDescription in sourceEnvironement.ConceptDescriptions.ForEachSafe())
            {
                var newConceptDescription = new ConceptDescription(sourceConceptDescription.identification.id);
                newConceptDescription = newConceptDescription.ConvertFromV10(sourceConceptDescription);
                environment.Add(newConceptDescription);
            }

            return environment;
        }


        public static AasCore.Aas3_1.IEnvironment ConvertFromV20(this AasCore.Aas3_1.IEnvironment environment, AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv sourceEnvironement)
        {
            // Convert Administration Shells
            foreach (var sourceAas in sourceEnvironement.AdministrationShells.ForEachSafe())
            {
                // first make the AAS
                var newAas = new AssetAdministrationShell(id: sourceAas.identification.id, null);
                newAas = newAas.ConvertFromV20(sourceAas);
                environment.Add(newAas);

                var sourceAsset = sourceEnvironement?.FindAsset(sourceAas.assetRef);
                if (sourceAsset != null)
                {
                    var newAssetInformation = new AssetInformation(AssetKind.Instance);
                    newAssetInformation = newAssetInformation.ConvertFromV20(sourceAsset);
                    newAas.AssetInformation = newAssetInformation;
                }

            }

            // Convert Submodels
            foreach (var sourceSubmodel in sourceEnvironement.Submodels.ForEachSafe())
            {
                var newSubmodel = new Submodel(sourceSubmodel.identification.id);
                newSubmodel = newSubmodel.ConvertFromV20(sourceSubmodel);
                environment.Add(newSubmodel);
            }

            // Convert CDs
            foreach (var sourceConceptDescription in sourceEnvironement.ConceptDescriptions.ForEachSafe())
            {
                var newConceptDescription = new ConceptDescription(sourceConceptDescription.identification.id);
                newConceptDescription = newConceptDescription.ConvertFromV20(sourceConceptDescription);
                environment.Add(newConceptDescription);
            }

            return environment;
        }

#endif

        //TODO (jtikekar, 0000-00-00): to test
        public static AasCore.Aas3_1.IEnvironment CreateFromExistingEnvironment(this AasCore.Aas3_1.IEnvironment environment,
            AasCore.Aas3_1.IEnvironment sourceEnvironment, List<IAssetAdministrationShell> filterForAas = null, List<AssetInformation> filterForAssets = null, List<ISubmodel> filterForSubmodel = null,
            List<IConceptDescription> filterForConceptDescriptions = null)
        {
            if (filterForAas == null)
            {
                filterForAas = new List<IAssetAdministrationShell>();
            }

            if (filterForAssets == null)
            {
                filterForAssets = new List<AssetInformation>();
            }

            if (filterForSubmodel == null)
            {
                filterForSubmodel = new List<ISubmodel>();
            }

            if (filterForConceptDescriptions == null)
            {
                filterForConceptDescriptions = new List<IConceptDescription>();
            }

            //Copy AssetAdministrationShells
            foreach (var aas in sourceEnvironment.AllAssetAdministrationShells())
            {
                if (filterForAas.Contains(aas))
                {
                    environment.Add(aas);

                    foreach (var submodelReference in aas.AllSubmodels())
                    {
                        var submodel = sourceEnvironment.FindSubmodel(submodelReference);
                        if (submodel != null)
                        {
                            filterForSubmodel.Add(submodel);
                        }
                    }
                }
            }

            //Copy Submodel
            foreach (var submodel in sourceEnvironment.AllSubmodels())
            {
                if (filterForSubmodel.Contains(submodel))
                {
                    environment.Add(submodel);

                    //Find Used CDs
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, submodel.SubmodelElements, ref filterForConceptDescriptions);
                }
            }

            //Copy ConceptDescription
            foreach (var conceptDescription in sourceEnvironment.AllConceptDescriptions())
            {
                if (filterForConceptDescriptions.Contains(conceptDescription))
                {
                    environment.Add(conceptDescription);
                }
            }

            return environment;

        }

        public static void CreateFromExistingEnvRecurseForCDs(this AasCore.Aas3_1.IEnvironment environment, AasCore.Aas3_1.IEnvironment sourceEnvironment,
            List<ISubmodelElement> submodelElements, ref List<IConceptDescription> filterForConceptDescription)
        {
            if (submodelElements == null || submodelElements.Count == 0 || filterForConceptDescription == null || filterForConceptDescription.Count == 0)
            {
                return;
            }

            foreach (var submodelElement in submodelElements)
            {
                if (submodelElement == null)
                {
                    return;
                }

                if (submodelElement.SemanticId != null)
                {
                    var conceptDescription = sourceEnvironment.FindConceptDescriptionByReference(submodelElement.SemanticId);
                    if (conceptDescription != null)
                    {
                        filterForConceptDescription.Add(conceptDescription);
                    }
                }

                if (submodelElement is SubmodelElementCollection smeColl)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, smeColl.Value, ref filterForConceptDescription);
                }

                if (submodelElement is SubmodelElementList smeList)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, smeList.Value, ref filterForConceptDescription);
                }

                if (submodelElement is Entity entity)
                {
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, entity.Statements, ref filterForConceptDescription);
                }

                if (submodelElement is AnnotatedRelationshipElement annotatedRelationshipElement)
                {
                    var annotedELements = new List<ISubmodelElement>();
                    foreach (var annotation in annotatedRelationshipElement.Annotations)
                    {
                        annotedELements.Add(annotation);
                    }
                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, annotedELements, ref filterForConceptDescription);
                }

                if (submodelElement is Operation operation)
                {
                    var operationELements = new List<ISubmodelElement>();
                    foreach (var inputVariable in operation.InputVariables)
                    {
                        operationELements.Add(inputVariable.Value);
                    }

                    foreach (var outputVariable in operation.OutputVariables)
                    {
                        operationELements.Add(outputVariable.Value);
                    }

                    foreach (var inOutVariable in operation.InoutputVariables)
                    {
                        operationELements.Add(inOutVariable.Value);
                    }

                    environment.CreateFromExistingEnvRecurseForCDs(sourceEnvironment, operationELements, ref filterForConceptDescription);

                }
            }
        }

        /// <summary>
        /// Enumerates any AssetAdministrationShells in the Environment. Will not return <c>null</c>.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static IEnumerable<IAssetAdministrationShell> AllAssetAdministrationShells(
            this AasCore.Aas3_1.IEnvironment env)
        {
            if (env?.AssetAdministrationShells != null)
                foreach (var aas in env.AssetAdministrationShells)
                    if (aas != null)
                        yield return aas;
        }

        /// <summary>
        /// Enumerates any Submodels in the Environment. Will not return <c>null</c>.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static IEnumerable<ISubmodel> AllSubmodels(this AasCore.Aas3_1.IEnvironment env)
        {
            if (env?.Submodels != null)
                foreach (var sm in env.Submodels)
                    if (sm != null)
                        yield return sm;
        }

        /// <summary>
        /// Enumerates any ConceptDescriptions in the Environment. Will not return <c>null</c>.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static IEnumerable<IConceptDescription> AllConceptDescriptions(this AasCore.Aas3_1.IEnvironment env)
        {
            if (env?.ConceptDescriptions != null)
                foreach (var cd in env.ConceptDescriptions)
                    if (cd != null)
                        yield return cd;
        }

        /// <summary>
        /// Enumerates any Identifiables in the Environment. Will not return <c>null</c>.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static IEnumerable<IIdentifiable> AllIdentifiables(this AasCore.Aas3_1.IEnvironment env)
        {
            foreach (var aas in env.AllAssetAdministrationShells())
                yield return aas;
            foreach (var sm in env.AllSubmodels())
                yield return sm;
            foreach (var cd in env.AllConceptDescriptions())
                yield return cd;    
        }

        /// <summary>
        /// Returns the number of AssetAdministrationShells.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static int AssetAdministrationShellCount(this AasCore.Aas3_1.IEnvironment env)
        {
            if (env?.AssetAdministrationShells != null)
                return env.AssetAdministrationShells.Count;
            return 0;
        }

        /// <summary>
        /// Returns the number of Submodels.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static int SubmodelCount(this AasCore.Aas3_1.IEnvironment env)
        {
            if (env?.Submodels != null)
                return env.Submodels.Count;
            return 0;
        }

        /// <summary>
        /// Returns the number of ConceptDescriptions.
        /// Is tolerant, if the list is <c>null</c>.
        /// </summary>
        public static int ConceptDescriptionCount(this AasCore.Aas3_1.IEnvironment env)
        {
            if (env?.ConceptDescriptions != null)
                return env.ConceptDescriptions.Count;
            return 0;
        }

        /// <summary>
        /// Returns the <c>index</c>-th Submodel, if exists. Returns <c>null</c> in any other case.
        /// </summary>
        public static ISubmodel SubmodelByIndex(this AasCore.Aas3_1.IEnvironment env, int index)
        {
            if (env?.Submodels == null || index < 0 || index >= env.Submodels.Count)
                return null;
            return env.Submodels[index];
        }

        /// <summary>
        /// Adds the ConceptDescription. If env.ConceptDescriptions are <c>null</c>, then
        /// the list will be created.
        /// </summary>
        public static IConceptDescription Add(this AasCore.Aas3_1.IEnvironment env, IConceptDescription cd)
        {
            if (cd == null)
                return null;
            if (env.ConceptDescriptions == null)
                env.ConceptDescriptions = new List<IConceptDescription>();
            env.ConceptDescriptions.Add(cd);
            return cd;
        }

        public static IConceptDescription AddConceptDescriptionOrReturnExisting(
            this AasCore.Aas3_1.IEnvironment env, IConceptDescription cd)
        {
            if (cd == null)
            {
                return null;
            }
            if (env.ConceptDescriptions != null)
            {
                var existingCd = env.ConceptDescriptions.Where(c => c.Id == cd.Id).FirstOrDefault();
                if (existingCd != null)
                {
                    return existingCd;
                }
                else
                {
                    env.ConceptDescriptions.Add(cd);
                }
            }

            return cd;
        }

        /// <summary>
        /// Adds the Submodel. If env.Submodels are <c>null</c>, then
        /// the list will be created.
        /// </summary>
        public static ISubmodel Add(this AasCore.Aas3_1.IEnvironment env, ISubmodel sm)
        {
            if (sm == null)
                return null;
            if (env.Submodels == null)
                env.Submodels = new List<ISubmodel>();
            env.Submodels.Add(sm);
            return sm;
        }

        /// <summary>
        /// Adds the AssetAdministrationShell. If env.AssetAdministrationShells are <c>null</c>, then
        /// the list will be created.
        /// </summary>
        public static IAssetAdministrationShell Add(this AasCore.Aas3_1.IEnvironment env, IAssetAdministrationShell aas)
        {
            if (aas == null)
                return null;
            if (env.AssetAdministrationShells == null)
                env.AssetAdministrationShells = new List<IAssetAdministrationShell>();
            env.AssetAdministrationShells.Add(aas);
            return aas;
        }

        /// <summary>
        /// Removes the ConceptDescription. If the env.ConceptDescriptions are subsequently empty,
        /// sets the env.ConceptDescriptions to <c>null</c> !!
        /// If the ConceptDescription is not found, simply returns.
        /// </summary>
        public static void Remove(this AasCore.Aas3_1.IEnvironment env, IConceptDescription cd)
        {
            if (cd == null || env.ConceptDescriptions == null || !env.ConceptDescriptions.Contains(cd))
                return;
            env.ConceptDescriptions.Remove(cd);
            if (env.ConceptDescriptions.Count < 1)
                env.ConceptDescriptions = null;
        }

        /// <summary>
        /// Removes the Submodel. If the env.Submodels are subsequently empty,
        /// sets the env.Submodels to <c>null</c> !!
        /// If the Submodel is not found, simply returns.
        /// </summary>
        public static void Remove(this AasCore.Aas3_1.IEnvironment env, ISubmodel sm)
        {
            if (sm == null || env.Submodels == null || !env.Submodels.Contains(sm))
                return;
            env.Submodels.Remove(sm);
            if (env.Submodels.Count < 1)
                env.Submodels = null;
        }

        /// <summary>
        /// Removes the AssetAdministrationShell. If the env.AssetAdministrationShells are subsequently empty,
        /// sets the env.AssetAdministrationShells to <c>null</c> !!
        /// If the AssetAdministrationShell is not found, simply returns.
        /// </summary>
        public static void Remove(this IEnvironment env, IAssetAdministrationShell aas)
        {
            if (aas == null || env.AssetAdministrationShells == null 
                || !env.AssetAdministrationShells.Contains(aas))
                return;
            env.AssetAdministrationShells.Remove(aas);
            if (env.AssetAdministrationShells.Count < 1)
                env.AssetAdministrationShells = null;
        }

        /// <summary>
        /// Remove References within the environment in dedicated areas.
        /// </summary>
        public static void RemoveReferences(this IEnvironment env, IReference rf,
            bool inAas = false)
        {
            // access
            if (env == null)
                return;

            // AAS?
            if (inAas)
                foreach (var aas in env.AllAssetAdministrationShells())
                {
                    var foundRf = aas.FindSubmodelReference(rf);
                    if (foundRf != null)
                        aas.Remove(foundRf);
                }
        }

        public static JsonWriter SerialiazeJsonToStream(this AasCore.Aas3_1.IEnvironment environment, StreamWriter streamWriter, bool leaveJsonWriterOpen = false)
        {
            streamWriter.AutoFlush = true;

            JsonSerializer serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            JsonWriter writer = new JsonTextWriter(streamWriter);
            serializer.Serialize(writer, environment);
            if (leaveJsonWriterOpen)
                return writer;
            writer.Close();
            return null;
        }

        #region Submodel Queries

        public static IEnumerable<ISubmodel> FindAllSubmodelGroupedByAAS(this AasCore.Aas3_1.IEnvironment environment, Func<IAssetAdministrationShell, ISubmodel, bool> p = null)
        {
            if (environment?.AssetAdministrationShells == null || environment?.Submodels == null)
                yield break;
            foreach (var aas in environment.AllAssetAdministrationShells())
            {
                foreach (var smref in aas.AllSubmodels())
                {
                    var sm = environment.FindSubmodel(smref);
                    if (sm != null && (p == null || p(aas, sm)))
                        yield return sm;
                }
            }
        }

        public static ISubmodel FindSubmodel(this AasCore.Aas3_1.IEnvironment environment, IReference submodelReference)
        {
            if (environment?.Submodels == null || submodelReference?.Keys == null)
            {
                return null;
            }

            if (submodelReference.Keys.Count != 1) // Can have only one reference key
            {
                return null;
            }

            var key = submodelReference.Keys[0];
            if (key.Type != KeyTypes.Submodel)
            {
                return null;
            }

            var submodels = environment.AllSubmodels()
                .Where(s => s.Id.Equals(key.Value, StringComparison.OrdinalIgnoreCase));
            if (submodels.Any())
            {
                return submodels.First();
            }

            return null;
        }

        public static ISubmodel FindSubmodelById(this AasCore.Aas3_1.IEnvironment environment, string submodelId)
        {
            if (environment?.Submodels == null || string.IsNullOrEmpty(submodelId))
            {
                return null;
            }

            var submodels = environment.AllSubmodels().Where(s => s.Id.Equals(submodelId));
            if (submodels.Any())
            {
                return submodels.First();
            }

            return null;
        }

        public static IEnumerable<ISubmodel> FindAllSubmodelBySemanticId(this AasCore.Aas3_1.IEnvironment environment, string semanticId)
        {
            if (semanticId == null)
                yield break;

            foreach (var submodel in environment.AllSubmodels())
                if (true == submodel.SemanticId?.Matches(semanticId))
                    yield return submodel;
        }

        #endregion

        #region AssetAdministrationShell Queries
        public static IAssetAdministrationShell FindAasWithSubmodelId(this AasCore.Aas3_1.IEnvironment environment, string submodelId)
        {
            if (submodelId == null)
            {
                return null;
            }

            var aas = environment.AllAssetAdministrationShells()
                .Where(a => (a.Submodels?.Where(s => s.Matches(submodelId)).FirstOrDefault()) != null)
                .FirstOrDefault();

            return aas;
        }

        public static IAssetAdministrationShell FindAasById(this AasCore.Aas3_1.IEnvironment environment, string aasId)
        {
            if (string.IsNullOrEmpty(aasId))
            {
                return null;
            }

            var aas = environment.AllAssetAdministrationShells()
                .Where(a => a.Id.Equals(aasId)).FirstOrDefault();

            return aas;
        }

        #endregion

        #region ConceptDescription Queries

        public static IConceptDescription FindConceptDescriptionById(
            this AasCore.Aas3_1.IEnvironment env, string cdId)
        {
            if (string.IsNullOrEmpty(cdId))
                return null;

            var conceptDescription = env.AllConceptDescriptions()
                .Where(c => c.Id.Equals(cdId)).FirstOrDefault();
            return conceptDescription;
        }

        public static IConceptDescription FindConceptDescriptionByReference(
            this AasCore.Aas3_1.IEnvironment env, IReference rf)
        {
            if (rf == null)
                return null;

            return env.FindConceptDescriptionById(rf.GetAsIdentifier());
        }

        #endregion

        #region Referable Queries

        /// <summary>
        /// Result of FindReferable in Environment
        /// </summary>
        public class ReferableRootInfo
        {
            public AssetAdministrationShell AAS = null;
            public AssetInformation Asset = null;
            public Submodel Submodel = null;
            public ConceptDescription CD = null;

            public int NrOfRootKeys = 0;

            public bool IsValid
            {
                get
                {
                    return NrOfRootKeys > 0 && (AAS != null || Submodel != null || Asset != null);
                }
            }
        }

        //TODO (jtikekar, 0000-00-00): Need to test
        //Micha added check for sourceOfSubElems to check if index is in SML
        public static IReferable FindReferableByReference(
            this AasCore.Aas3_1.IEnvironment environment,
            IReference reference,
            int keyIndex = 0,
            IReferable sourceOfSubElems = null,
            IEnumerable<ISubmodelElement> submodelElems = null,
            ReferableRootInfo rootInfo = null)
        {
            // access
            var keyList = reference?.Keys;
            if (keyList == null || keyList.Count == 0 || keyIndex >= keyList.Count)
                return null;

            // shortcuts
            var firstKeyType = keyList[keyIndex].Type;
            var firstKeyId = keyList[keyIndex].Value;

            // different pathes
            switch (firstKeyType)
            {
                case KeyTypes.AssetAdministrationShell:
                    {
                        var aas = environment.FindAasById(firstKeyId);

                        // side info?
                        if (rootInfo != null)
                        {
                            rootInfo.AAS = aas as AssetAdministrationShell;
                            rootInfo.NrOfRootKeys = 1 + keyIndex;
                        }

                        //Not found or already at the end of our search
                        if (aas == null || keyIndex >= keyList.Count - 1)
                        {
                            return aas;
                        }

                        return environment.FindReferableByReference(reference, ++keyIndex);
                    }
                // dead-csharp off
                // TODO (MIHO, 2023-01-01): stupid generalization :-(
                case KeyTypes.GlobalReference:
                case KeyTypes.ConceptDescription:
                    {
                        // In meta model V3, multiple important things might by identified
                        // by a flat GlobalReference :-(

                        // find an Asset by that id?

                        var keyedAas = environment.FindAasWithAssetInformation(firstKeyId);
                        if (keyedAas?.AssetInformation != null)
                        {
                            // found an Asset

                            // side info?
                            if (rootInfo != null)
                            {
                                rootInfo.AAS = keyedAas as AssetAdministrationShell;
                                rootInfo.Asset = (AssetInformation)(keyedAas?.AssetInformation);
                                rootInfo.NrOfRootKeys = 1 + keyIndex;
                            }

                            // test if to go further for Submodel
                            if (keyIndex < keyList.Count - 1
                                && keyList[keyIndex + 1].Type == KeyTypes.Submodel)
                            {
                                var foundSm = environment.FindAllSubmodelGroupedByAAS((aas, sm) 
                                            => aas == keyedAas && sm.Id?.Trim() == keyList[keyIndex + 1].Value?.Trim())
                                    .FirstOrDefault();

                                if (foundSm != null)
                                {
                                    keyIndex += 2;

                                    var foundSme = environment.FindReferableByReference(reference, keyIndex,
                                        foundSm, foundSm.SubmodelElements);

                                    if (foundSme != null)
                                    {
                                        return foundSme;
                                    }
                                    else
                                    {
                                        return foundSm;
                                    }
                                }
                            }

                            // nope, give back the AAS
                            return keyedAas;
                        }

                        // Concept?Description
                        var keyedCd = environment.FindConceptDescriptionById(firstKeyId);
                        if (keyedCd != null)
                        {
                            // side info?
                            if (rootInfo != null)
                            {
                                rootInfo.CD = keyedCd as ConceptDescription;
                                rootInfo.NrOfRootKeys = 1 + keyIndex;
                            }

                            // give back the CD
                            return keyedCd;
                        }

                        // Nope
                        return null;
                    }
                // dead-csharp on
                case KeyTypes.Submodel:
                    {
                        var submodel = environment.FindSubmodelById(firstKeyId);
                        // No?
                        if (submodel == null)
                            return null;

                        // notice in side info
                        if (rootInfo != null)
                        {
                            rootInfo.Submodel = submodel as Submodel;
                            rootInfo.NrOfRootKeys = 1 + keyIndex;

                            // add even more info
                            if (rootInfo.AAS == null)
                            {
                                foreach (var aas2 in environment.AllAssetAdministrationShells())
                                {
                                    var smref2 = environment.FindSubmodelById(submodel.Id);
                                    if (smref2 != null)
                                    {
                                        rootInfo.AAS = (AssetAdministrationShell)aas2;
                                        break;
                                    }
                                }
                            }
                        }

                        // at the end of the journey?
                        if (keyIndex >= keyList.Count - 1)
                            return submodel;

                        return environment.FindReferableByReference(reference, ++keyIndex, 
                            submodel, submodel.SubmodelElements);
                    }
            }            

            if (firstKeyType.IsSME() && submodelElems != null)
            {
                ISubmodelElement submodelElement;
                //check if key.value is index 
                var index = 0;
                bool isIndex = (sourceOfSubElems is ISubmodelElementList) && int.TryParse(firstKeyId, out index);
                if (isIndex)
                {
                    var smeList = submodelElems.ToList();
                    submodelElement = smeList[index];
                }
                else
                {
                    submodelElement = submodelElems.Where(
                    sme => sme.IdShort.Equals(keyList[keyIndex].Value,
                        StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                }

                if (submodelElement != null)
                {
                    //This is required element
                    if (keyIndex + 1 >= keyList.Count)
                    {
                        return submodelElement;
                    }

                    //Recurse again
                    if (submodelElement?.EnumeratesChildren() == true)
                        return environment.FindReferableByReference(reference, ++keyIndex, 
                            submodelElement, submodelElement.EnumerateChildren());
                }
            }

            //Nothing in this environment
            return null;
        }

        #endregion

        #region AasxPackageExplorer
        
        public static IEnumerable<T> FindAllSubmodelElements<T>(this AasCore.Aas3_1.IEnvironment environment,
                Predicate<T> match = null, AssetAdministrationShell onlyForAAS = null) where T : ISubmodelElement
        {
            // more or less two different schemes
            if (onlyForAAS != null)
            {
                foreach (var smr in onlyForAAS.AllSubmodels())
                {
                    var sm = environment.FindSubmodel(smr);
                    if (sm?.SubmodelElements != null)
                        foreach (var x in sm.SubmodelElements.FindDeep<T>(match))
                            yield return x;
                }
            }
            else
            {
                foreach (var sm in environment.AllSubmodels())
                    if (sm?.SubmodelElements != null)
                        foreach (var x in sm.SubmodelElements.FindDeep<T>(match))
                            yield return x;
            }
        }

        public static IEnumerable<LocatedReference> FindAllReferences(this AasCore.Aas3_1.IEnvironment environment)
        {
            foreach (var aas in environment.AllAssetAdministrationShells())
                if (aas != null)
                    foreach (var r in aas.FindAllReferences())
                        yield return r;

            foreach (var sm in environment.AllSubmodels())
                if (sm != null)
                    foreach (var r in sm.FindAllReferences())
                        yield return r;

            foreach (var cd in environment.AllConceptDescriptions())
                if (cd != null)
                    foreach (var r in cd.FindAllReferences())
                        yield return new LocatedReference(cd, r);
        }

        // TODO: Integrate into above function
        public static IEnumerable<LocatedReference> FindAllSubmodelReferences(
            this AasCore.Aas3_1.IEnvironment environment,
            bool onlyNotExisting = false)
        {
            // unique set of references
            var refs = new List<LocatedReference>();
            foreach (var aas in environment.AllAssetAdministrationShells())
                foreach (var smr in aas?.AllSubmodels())
                    refs.AddIfNew(new LocatedReference() { Identifiable = aas, Reference = smr});

            // only existing
            foreach (var lr in refs)
                if (!onlyNotExisting
                    || null == environment.FindSubmodel(lr?.Reference))
                    yield return lr;
        }

        /// <summary>
        /// Warning: very inefficient!
        /// </summary>
        public static IEnumerable<IConceptDescription> FindAllReferencedCdsForSubmodel(
            this AasCore.Aas3_1.IEnvironment env,
            ISubmodel sm)
        {
            // unique set of references
            var refs = new List<IConceptDescription>();
            sm?.RecurseOnSubmodelElements(null, (state, parents, sme) =>
            {
                if (sme.SemanticId != null)
                {
                    var cd = env?.FindConceptDescriptionByReference(sme.SemanticId);
                    if (cd != null)
                        refs.Add(cd);
                }

                // recurse
                return true;
            });

            return refs.Distinct();
        }

        /// <summary>
        /// Warning: very inefficient!
        /// </summary>
        public static IEnumerable<LocatedReference> FindAllSemanticIdsForSubmodel(
            this AasCore.Aas3_1.IEnvironment environment,
            ISubmodel sm)
        {
            // unique set of references
            var refs = new List<LocatedReference>();
            sm?.RecurseOnSubmodelElements(null, (state, parents, sme) =>
            {
                if (sme.SemanticId != null)
                    refs.AddIfNew(new LocatedReference() { Identifiable = sm, Reference = sme.SemanticId });

                // recurse
                return true;
            });

            return refs;
        }

        /// <summary>
        /// Warning: very inefficient!
        /// </summary>
        public static IEnumerable<LocatedReference> FindAllSemanticIdsForAas(
            this AasCore.Aas3_1.IEnvironment environment,
            IAssetAdministrationShell aas)
        {
            // unique set of references
            var refs = new List<LocatedReference>();
            foreach (var smr in aas?.AllSubmodels())
            {
                var sm = environment.FindSubmodel(smr);
                refs.AddRange(environment.FindAllSemanticIdsForSubmodel(sm));
            }

            return refs;
        }

        /// <summary>
        /// Warning: very inefficient!
        /// </summary>
        public static IEnumerable<LocatedReference> FindAllReferencedSemanticIds(
            this AasCore.Aas3_1.IEnvironment env)
        {
            // unique set of references
            var refs = new List<LocatedReference>();
            
            foreach (var aas in env.AllAssetAdministrationShells())
                refs.AddRange(env.FindAllSemanticIdsForAas(aas));

            return refs;
        }

        /// <summary>
        /// Warning: very inefficient!
        /// </summary>
        public static IEnumerable<IIdentifiable> FindAllReferencedIdentifiablesForAas(
            this AasCore.Aas3_1.IEnvironment environment,
            IAssetAdministrationShell aas)
        {
            // unique set of references
            var refs = new List<IIdentifiable>();
            foreach (var smr in aas?.AllSubmodels())
            {
                var sm = environment.FindSubmodel(smr);
                if (sm == null)
                    continue;

                refs.Add(sm);
                refs.AddRange(environment.FindAllReferencedCdsForSubmodel(sm));
            }

            return refs;
        }

        public static IEnumerable<LocatedReference> FindAllSubmodelReferencesForAAS(
            this AasCore.Aas3_1.IEnvironment environment,
            IAssetAdministrationShell aas)
        {
            // set of references
            var refs = new List<LocatedReference>();
            foreach (var smr in aas?.AllSubmodels())
            {
                var sm = environment.FindSubmodel(smr);
                if (sm != null)
                    refs.AddIfNew(new LocatedReference() { Identifiable = sm, Reference = smr });
            }
            return refs;
        }

        /// <summary>
        /// Warning: very inefficient!
        /// </summary>
        public static IEnumerable<IIdentifiable> FindAllReferencedIdentifiablesFor(
            this AasCore.Aas3_1.IEnvironment env,
            IIdentifiable idf,
            bool makeDistint = true)
        {
            // set of references
            var refs = new List<IIdentifiable>();

            if (idf is IAssetAdministrationShell aas)
            {
                refs.Add(aas);
                refs.AddRange(env.FindAllReferencedIdentifiablesForAas(aas));
            }

            if (idf is ISubmodel sm)
            {
                refs.Add(sm);
                refs.AddRange(env.FindAllReferencedCdsForSubmodel(sm));
            }

            if (idf is IConceptDescription cd)
            {
                refs.Add(cd);
            }

            // more distinct?
            if (makeDistint)
            {
                var refs2 = refs.Distinct();
                return refs2;
            }
            else
                return refs;
        }

        /// <summary>
        /// Tries renaming an Identifiable, specifically: the identification of an Identifiable and
        /// all references to it.
        /// Currently supported: ConceptDescriptions
        /// Returns a list of Referables, which were changed or <c>null</c> in case of error
        /// </summary>
        public static List<IReferable> RenameIdentifiable<T>(this AasCore.Aas3_1.IEnvironment environment, string oldId, string newId)
            where T : IClass
        {
            // access
            if (oldId == null || newId == null || oldId.Equals(newId))
                return null;

            var res = new List<IReferable>();

            if (typeof(T) == typeof(ConceptDescription))
            {
                // check, if exist or not exist
                var cdOld = environment.FindConceptDescriptionById(oldId);
                if (cdOld == null || environment.FindConceptDescriptionById(newId) != null)
                    return null;

                // rename old cd
                cdOld.Id = newId;
                res.Add(cdOld);

                // search all SMEs referring to this CD
                foreach (var sme in environment.FindAllSubmodelElements<ISubmodelElement>(match: (s) =>
                {
                    return (s != null && s.SemanticId != null && s.SemanticId.Matches(oldId));
                }))
                {
                    sme.SemanticId.Keys[0].Value = newId;
                    res.Add(sme);
                }

                // seems fine
                return res;
            }
            else
            if (typeof(T) == typeof(Submodel))
            {
                // check, if exist or not exist
                var smOld = environment.FindSubmodelById(oldId);
                if (smOld == null || environment.FindSubmodelById(newId) != null)
                    return null;

                // recurse all possible Referenes in the aas env
                foreach (var lr in environment.FindAllReferences())
                {
                    var r = lr?.Reference;
                    if (r != null)
                        for (int i = 0; i < r.Keys.Count; i++)
                            if (r.Keys[i].Matches(KeyTypes.Submodel, oldId, MatchMode.Relaxed))
                            {
                                // directly replace
                                r.Keys[i].Value = newId;
                                if (!res.Contains(lr.Identifiable))
                                    res.Add(lr.Identifiable);
                            }
                }

                // rename old Submodel
                smOld.Id = newId;

                // seems fine
                return res;
            }
            else
            if (typeof(T) == typeof(AssetAdministrationShell))
            {
                // check, if exist or not exist
                var aasOld = environment.FindAasById(oldId);
                if (aasOld == null || environment.FindAasById(newId) != null)
                    return null;

                // recurse? -> no?

                // rename old Asset
                aasOld.Id = newId;

                // seems fine
                return res;
            }
            else
            //TODO (jtikekar, 0000-00-00): support asset
            if (typeof(T) == typeof(AssetInformation))
            {
                // check, if exist or not exist
                var assetOld = environment.FindAasWithAssetInformation(oldId);
                if (assetOld == null || environment.FindAasWithAssetInformation(newId) != null)
                    return null;

                // recurse all possible Referenes in the aas env
                foreach (var lr in environment.FindAllReferences())
                {
                    var r = lr?.Reference;
                    if (r != null)
                        for (int i = 0; i < r.Keys.Count; i++)
                            if (r.Keys[i].Matches(KeyTypes.GlobalReference, oldId))
                            {
                                // directly replace
                                r.Keys[i].Value = newId;
                                if (res.Contains(lr.Identifiable))
                                    res.Add(lr.Identifiable);
                            }
                }

                // rename old Asset
                assetOld.AssetInformation.GlobalAssetId = newId;

                // seems fine
                return res;
            }

            // no result is false, as well
            return null;
        }

        public static IAssetAdministrationShell FindAasWithAssetInformation(this AasCore.Aas3_1.IEnvironment environment, string globalAssetId)
        {
            if (string.IsNullOrEmpty(globalAssetId))
            {
                return null;
            }

            foreach (var aas in environment.AllAssetAdministrationShells())
            {
                if (aas.AssetInformation?.GlobalAssetId?.Equals(globalAssetId) == true)
                {
                    return aas;
                }
            }

            return null;
        }

        public static ComparerIndexed CreateIndexedComparerCdsForSmUsage(this AasCore.Aas3_1.IEnvironment environment)
        {
            var cmp = new ComparerIndexed();
            int nr = 0;
            foreach (var sm in environment.FindAllSubmodelGroupedByAAS())
                foreach (var sme in sm.FindDeep<ISubmodelElement>())
                {
                    if (sme.SemanticId == null)
                        continue;
                    var cd = environment.FindConceptDescriptionByReference(sme.SemanticId);
                    if (cd == null)
                        continue;
                    if (cmp.Index.ContainsKey(cd))
                        continue;
                    cmp.Index[cd] = nr++;
                }
            return cmp;
        }

        public static ISubmodelElement CopySubmodelElementAndCD(this AasCore.Aas3_1.IEnvironment environment,
                AasCore.Aas3_1.IEnvironment srcEnv, ISubmodelElement srcElem, bool copyCD = false, bool shallowCopy = false)
        {
            // access
            if (srcEnv == null || srcElem == null)
                return null;

            // 1st result pretty easy (calling function will add this to the appropriate Submodel)
            var res = srcElem.Copy();

            // copy the CDs..
            if (copyCD)
                environment.CopyConceptDescriptionsFrom(srcEnv, srcElem, shallowCopy);

            // give back
            return res;
        }

        public static IReference CopySubmodelRefAndCD(this AasCore.Aas3_1.IEnvironment environment,
                AasCore.Aas3_1.IEnvironment srcEnv, IReference srcSubRef, bool copySubmodel = false, bool copyCD = false,
                bool shallowCopy = false)
        {
            // access
            if (srcEnv == null || srcSubRef == null)
                return null;

            // need to have the source Submodel
            var srcSub = srcEnv.FindSubmodel(srcSubRef);
            if (srcSub == null)
                return null;

            // 1st result pretty easy (calling function will add this to the appropriate AAS)
            var dstSubRef = srcSubRef.Copy();

            // get the destination and shall src != dst
            var dstSub = environment.FindSubmodel(dstSubRef);
            if (srcSub == dstSub)
                return null;

            // maybe we need the Submodel in our environment, as well
            if (dstSub == null && copySubmodel)
            {
                dstSub = srcSub.Copy();
                environment.Submodels ??= new List<ISubmodel>();
                environment.Submodels.Add(dstSub);
            }
            else
            if (dstSub != null)
            {
                // there is already an submodel, just add members
                if (!shallowCopy && srcSub.SubmodelElements != null)
                {
                    if (dstSub.SubmodelElements == null)
                        dstSub.SubmodelElements = new List<ISubmodelElement>();
                    foreach (var smw in srcSub.SubmodelElements)
                        dstSub.SubmodelElements.Add(
                            smw.Copy());
                }
            }

            // copy the CDs..
            if (copyCD && srcSub.SubmodelElements != null)
                foreach (var smw in srcSub.SubmodelElements)
                    environment.CopyConceptDescriptionsFrom(srcEnv, smw, shallowCopy);

            // give back
            return dstSubRef;
        }

        private static void CopyConceptDescriptionsFrom(this AasCore.Aas3_1.IEnvironment environment,
                AasCore.Aas3_1.IEnvironment srcEnv, ISubmodelElement src, bool shallowCopy = false)
        {
            // access
            if (srcEnv == null || src == null || src.SemanticId == null)
                return;

            // check for this SubmodelElement in Source
            var cdSrc = srcEnv.FindConceptDescriptionByReference(src.SemanticId);
            if (cdSrc == null)
                return;

            // check for this SubmodelElement in Destnation (this!)
            var cdDest = environment.FindConceptDescriptionByReference(src.SemanticId);
            if (cdDest == null)
            {
                // copy new
                environment.ConceptDescriptions ??= new List<IConceptDescription>();
                environment.ConceptDescriptions.Add(cdSrc.Copy());
            }

            // recurse?
            if (!shallowCopy)
                foreach (var m in src.EnumerateChildren())
                    environment.CopyConceptDescriptionsFrom(srcEnv, m, shallowCopy: false);

        }
        #endregion

    }



}
