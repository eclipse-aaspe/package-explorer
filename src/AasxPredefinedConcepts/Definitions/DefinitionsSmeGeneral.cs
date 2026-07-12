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
    /// The definitions aim to be used as Qualifiers and Extension to IDTA specifications.
    /// </summary>
    public class SmeGeneral : AasxDefinitionBase
    {
        public static SmeGeneral Static = new SmeGeneral();

        public Aas.ConceptDescription
            CD_UnitOfMeasure;

        public SmeGeneral()
        {
            // info
            this.DomainInfo = "AAS Submodel elements - General information";

            // definitions
            CD_UnitOfMeasure = CreateSparseConceptDescription("en", "IRI",
                "SME_UnitOfMeasure",
                "https://admin-shell.io/SubmodelElements/UnitOfMeasure/1/0",
				"Used a template qualifier, defines a unit of meausure (symbol).");

            // reflect
            AddEntriesByReflection(this.GetType(), useAttributes: false, useFieldNames: true);
        }
    }
}
