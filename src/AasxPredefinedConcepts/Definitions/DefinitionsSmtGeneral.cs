/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Aas = AasCore.Aas3_1;

// reSharper disable UnusedType.Global
// reSharper disable ClassNeverInstantiated.Global

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// This class holds definitions, which are preliminary, experimental, partial, not stabilized.
    /// The definitions aim as the definition and handling of Events.
    /// The should end up finally in a AASiD specification.
    /// </summary>
    public class SmtGeneral : AasxDefinitionBase
    {
        public static SmtGeneral Static = new SmtGeneral();

        public Aas.ConceptDescription
            CD_IntentionallyEmpty,
            CD_ArbitraryConcept;

        public SmtGeneral()
        {
            // info
            this.DomainInfo = "AAS Submodel templates - General information";

            // definitons
            CD_IntentionallyEmpty = CreateSparseConceptDescription("en", "IRI",
                "IntentionallyEmpty",
                "https://admin-shell.io/SMT/General/IntentionallyEmpty",
                "Reference is intentionally empty, as filling out might occur later, upon " +
                "availability of information.");

            CD_ArbitraryConcept = CreateSparseConceptDescription("en", "IRI",
				"Arbitrary",
				"https://admin-shell.io/SMT/General/Arbitrary",
				"Associated Submodel(Element) might refer to an arbitrary concept repository entry.");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
