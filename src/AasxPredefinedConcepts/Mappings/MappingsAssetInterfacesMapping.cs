﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using Extensions;
using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0;

// These classes were serialized by "export predefined concepts"
// and shall allow to automatically de-serialize AAS elements structures
// into C# classes.

namespace AasxPredefinedConcepts.AssetInterfacesMappingConfiguration
{
    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingConfigurations")]
    public class CD_MappingConfigurations
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingConfiguration", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_MappingConfiguration> MappingConfigs = new List<CD_MappingConfiguration>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingConfiguration")]
    public class CD_MappingConfiguration
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/InterfaceReference", Card = AasxPredefinedCardinality.One)]
        public AasClassMapperHintedReference InterfaceReference = new AasClassMapperHintedReference();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingSourceSinkRelations", Card = AasxPredefinedCardinality.One)]
        public CD_MappingSourceSinkRelations MappingSourceSinkRelations = new CD_MappingSourceSinkRelations();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingSourceSinkRelations")]
    public class CD_MappingSourceSinkRelations
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingSourceSinkRelation", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasClassMapperHintedRelation> MapRels = new List<AasClassMapperHintedRelation>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/Submodel")]
    public class CD_AssetInterfacesMappingConfiguration
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesMappingConfiguration/1/0/MappingConfigurations", Card = AasxPredefinedCardinality.One)]
        public CD_MappingConfigurations MappingConfigurations = new CD_MappingConfigurations();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }
}

