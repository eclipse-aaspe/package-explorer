/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_0;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Digital Nameplate 
    /// </summary>
    public class HierarchStructV11 : AasxDefinitionBase
    {
        public static HierarchStructV11 Static = new HierarchStructV11();

        public Aas.Submodel
            SM_HierarchicalStructures;

        public Aas.ConceptDescription
            CD_EntryNode,
            CD_Node,
            CD_SameAs,
            CD_IsPartOf,
            CD_HasPart,
            CD_BulkCount,
            CD_ArcheType;

        public HierarchStructV11()
        {
            // info
            this.DomainInfo = "Hierarchical Structures enabling Bills of Material (IDTA) V1.1";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources."
                + "IdtaHierarchicalStructV11.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(HierarchStructV11), useFieldNames: true);
        }
    }
}
