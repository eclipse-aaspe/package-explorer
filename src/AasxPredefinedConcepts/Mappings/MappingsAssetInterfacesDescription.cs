/*
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
using static AasxPredefinedConcepts.ConceptModel.ConceptModelZveiTechnicalData;
using Aas = AasCore.Aas3_1;

// These classes were serialized by "export predefined concepts"
// and shall allow to automatically de-serialize AAS elements structures
// into C# classes.

namespace AasxPredefinedConcepts.AssetInterfacesDescription
{
    
    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface")]
    public class CD_GenericInterface
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasxPredefinedCardinality.One)]
        public string Title;

        [AasConcept(Cd = "http://purl.org/dc/terms/created", Card = AasxPredefinedCardinality.ZeroToOne)]
        public DateTime? Created;

        [AasConcept(Cd = "http://purl.org/dc/terms/modified", Card = AasxPredefinedCardinality.ZeroToOne)]
        public DateTime? Modified;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#supportContact", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Support;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/EndpointMetadata", Card = AasxPredefinedCardinality.One)]
        public CD_EndpointMetadata EndpointMetadata = new CD_EndpointMetadata();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/InteractionMetadata", Card = AasxPredefinedCardinality.One)]
        public CD_InteractionMetadata InteractionMetadata = new CD_InteractionMetadata();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/ExternalDescriptor", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_ExternalDescriptor ExternalDescriptor = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/EndpointMetadata")]
    public class CD_EndpointMetadata
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#baseURI", Card = AasxPredefinedCardinality.One)]
        public string Base;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/hypermedia#forContentType", Card = AasxPredefinedCardinality.One)]
        public string ContentType;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasMostSignificantByte", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_mostSignificantByte = "";

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasMostSignificantWord", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_mostSignificantWord = "";

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasSecurityConfiguration", Card = AasxPredefinedCardinality.One)]
        public CD_Security Security = new CD_Security();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasxPredefinedCardinality.One)]
        public CD_SecurityDefinitions SecurityDefinitions = new CD_SecurityDefinitions();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasSecurityConfiguration")]
    public class CD_Security
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasClassMapperHintedReference> SecurityRef = new List<AasClassMapperHintedReference>();
        
        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme")]
    public class CD_SecurityDefinitions
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#NoSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Nosec_sc Nosec_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#AutoSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Auto_sc Auto_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#BasicSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Basic_sc Basic_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#ComboSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Combo_sc Combo_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#APIKeySecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Apikey_sc Apikey_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#PSKSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Psk_sc Psk_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#DigestSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Digest_sc Digest_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#BearerSecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Bearer_sc Bearer_sc = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#OAuth2SecurityScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Oauth2_sc Oauth2_sc = null;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/OPCUASecurityChannelScheme", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Opcua_channel_sc Opcua_channel_sc = null;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/OPCUASecurityAuthenticationScheme ", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Opcua_authentication_sc Opcua_authentication_sc = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#NoSecurityScheme")]
    public class CD_Nosec_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#AutoSecurityScheme")]
    public class CD_Auto_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#BasicSecurityScheme")]
    public class CD_Basic_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string In;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#ComboSecurityScheme")]
    public class CD_Combo_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#oneOf", Card = AasxPredefinedCardinality.One)]
        public CD_OneOf OneOf = new CD_OneOf();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#allOf", Card = AasxPredefinedCardinality.One)]
        public CD_AllOf AllOf = new CD_AllOf();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#oneOf")]
    public class CD_OneOf
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasClassMapperHintedReference> SecurityRef = new List<AasClassMapperHintedReference>();
        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#allOf")]
    public class CD_AllOf
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#definesSecurityScheme", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<AasClassMapperHintedReference> SecurityRef = new List<AasClassMapperHintedReference>();
        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#APIKeySecurityScheme")]
    public class CD_Apikey_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string In;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#PSKSecurityScheme")]
    public class CD_Psk_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#identity", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Identity;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#DigestSecurityScheme")]
    public class CD_Digest_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string In;

        public string Qop;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#BearerSecurityScheme")]
    public class CD_Bearer_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#name", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Name;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#in", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string In;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#authorization", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Authorization;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#alg", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Alg;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#format", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Format;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/security#OAuth2SecurityScheme")]
    public class CD_Oauth2_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#token", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Token;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#refresh", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Refresh;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#authorization", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Authorization;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#scopes", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Scopes;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#flow", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Flow;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/OPCUASecurityChannelScheme")]
    public class CD_Opcua_channel_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/securityMode ", Card = AasxPredefinedCardinality.One)]
        public string Uav_securityMode;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/securityPolicy", Card = AasxPredefinedCardinality.One)]
        public string Uav_securityPolicy;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/OPCUASecurityAuthenticationScheme")]
    public class CD_Opcua_authentication_sc
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#SecurityScheme", Card = AasxPredefinedCardinality.One)]
        public string Scheme;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/userIdentityToken", Card = AasxPredefinedCardinality.One)]
        public string Uav_userIdentityToken;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding/issueToken", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperHintedReference Uav_issueToken = new AasClassMapperHintedReference();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/security#proxy", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Proxy;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/InteractionMetadata")]
    public class CD_InteractionMetadata
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#PropertyAffordance", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_PropertiesAffordance Properties = null;
        // public CD_Properties Properties = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#ActionAffordance", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Actions Actions = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#EventAffordance", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Events Events = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/td#PropertyAffordance")]
    public class CD_PropertiesAffordance
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_PropertyName> Property = new List<CD_PropertyName>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfaceDescription/1/0/PropertyDefinition")]
    public class CD_PropertyName
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/key", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Key;

        [AasConcept(Cd = "https://www.w3.org/1999/02/22-rdf-syntax-ns#type", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Type;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Title;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#isObservable", Card = AasxPredefinedCardinality.ZeroToOne)]
        public bool? Observable;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#const", Card = AasxPredefinedCardinality.ZeroToOne)]
        public int? Const;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#enum", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Enum Enum = new CD_Enum();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#default", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Default;

        [AasConcept(Cd = "https://schema.org/unitCode", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Unit;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperRange<string> Min_max = null;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperRange<string> LengthRange = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#items", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Items Items = null;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/itemsRange", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperRange<string> ItemsRange = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#properties", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Properties Properties = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasUriTemplateSchema", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Properties UriVariables = null;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/valueSemantics", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperHintedReference ValueSemanticas = new AasClassMapperHintedReference();

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasForm", Card = AasxPredefinedCardinality.One)]
        public CD_Forms Forms = new CD_Forms();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    public class CD_Enum
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#items")]
    public class CD_Items
    {
        [AasConcept(Cd = "https://www.w3.org/1999/02/22-rdf-syntax-ns#type", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Type;

        [AasConcept(Cd = "https://schema.org/unitCode", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Unit;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#default", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Default;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#const", Card = AasxPredefinedCardinality.ZeroToOne)]
        public int? Const;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#isObservable", Card = AasxPredefinedCardinality.ZeroToOne)]
        public bool? Observable;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#title", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Title;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/minMaxRange", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperRange<string> Min_max = null;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/lengthRange", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperRange<string> LengthRange = null;

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/valueSemantics", Card = AasxPredefinedCardinality.ZeroToOne)]
        public AasClassMapperHintedReference ValueSemanticas = new AasClassMapperHintedReference();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#properties")]
    public class CD_Properties
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/json-schema#propertyName", Card = AasxPredefinedCardinality.ZeroToMany)]
        public List<CD_PropertyName> Property = new List<CD_PropertyName>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasForm")]
    public class CD_Forms
    {
        [AasConcept(Cd = "https://www.w3.org/2019/wot/hypermedia#hasTarget", Card = AasxPredefinedCardinality.One)]
        public string Href;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/hypermedia#forContentType", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string ContentType;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/td#hasSecurityConfiguration", Card = AasxPredefinedCardinality.One)]
        public CD_Security Security = new CD_Security();

        [AasConcept(Cd = "https://www.w3.org/2011/http#methodName", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Htv_methodName;

        [AasConcept(Cd = "https://www.w3.org/2011/http#headers", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Htv_headers Htv_headers = null;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/http#pollingTime", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Htv_pollingTime;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/http#timeout", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Htv_timeout;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasFunction", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_function;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasEntity", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_entity;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasZeroBasedAddressingFlag", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_zeroBasedAddressing;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasPollingTime", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_pollingTime;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasTimeout", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_timeout;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasPayloadDataType", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_type;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasMostSignificantByte", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_mostSignificantByte = "";

        [AasConcept(Cd = "https://www.w3.org/2019/wot/modbus#hasMostSignificantWord", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Modv_mostSignificantWord = "";

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#hasRetainFlag", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Mqv_retain;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#ControlPacket", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Mqv_controlPacket;

        [AasConcept(Cd = "https://www.w3.org/2019/wot/mqtt#hasQoSFlag", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Mqv_qos;

        [AasConcept(Cd = "http://opcfoundation.org/UA/WoT-Binding#browsePath", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Uav_browsePath;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#usesService", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string Bacv_useService;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasDataType", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Bacv_hasDataType Bacv_hasDataType = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2011/http#headers")]
    public class CD_Htv_headers
    {
        [AasConcept(Cd = "https://www.w3.org/2011/http#headers", Card = AasxPredefinedCardinality.OneToMany)]
        public List<CD_Htv_headers> Htv_headers = new List<CD_Htv_headers>();

        [AasConcept(Cd = "https://www.w3.org/2011/http#fieldName", Card = AasxPredefinedCardinality.One)]
        public string Htv_fieldName;

        [AasConcept(Cd = "https://www.w3.org/2011/http#fieldValue", Card = AasxPredefinedCardinality.One)]
        public string Htv_fieldValue;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasDataType")]
    public class CD_Bacv_hasDataType
    {
        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#isIso8601", Card = AasxPredefinedCardinality.ZeroToOne)]
        public bool? bacv_isISO8601;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasBinaryRepresentation", Card = AasxPredefinedCardinality.ZeroToOne)]
        public string bacv_hasBinaryRepresentation;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasMember", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Bacv_hasDataType bacv_hasMember = null;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasNamedMember", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<CD_Bacv_hasNamedMember> bacv_hasNamedMember = null;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasValueMap", Card = AasxPredefinedCardinality.ZeroToOne)]
        public List<CD_Bacv_hasValueMap> bacv_hasValueMap = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://www.w3.org/2022/bacnet#NamedMember")]
    public class CD_Bacv_hasNamedMember
    {
        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasfieldName", Card = AasxPredefinedCardinality.One)]
        public string bacv_hasFieldName;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasContextTag", Card = AasxPredefinedCardinality.ZeroToOne)]
        public bool? bacv_hasContextTag;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasDataType", Card = AasxPredefinedCardinality.ZeroToOne)]
        public CD_Bacv_hasDataType bacv_hasDataType = null;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasMapEntry")]
    public class CD_Bacv_hasValueMap
    {

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasLogicalVal", Card = AasxPredefinedCardinality.One)]
        public string bacv_hasLogicalVal;

        [AasConcept(Cd = "http://www.w3.org/2022/bacnet#hasProtocolVal", Card = AasxPredefinedCardinality.One)]
        public int bacv_hasProtocolVal;

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/td#ActionAffordance")]
    public class CD_Actions
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://www.w3.org/2019/wot/td#EventAffordance")]
    public class CD_Events
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }

    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/ExternalDescriptor")]
    public class CD_ExternalDescriptor
    {

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }
    
    [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Submodel")]
    public class CD_AssetInterfacesDescription    
    {
        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasxPredefinedCardinality.ZeroToMany,
            SupplSemId = "http://www.w3.org/2011/http")]
        public List<CD_GenericInterface> InterfaceHTTP = new List<CD_GenericInterface>();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasxPredefinedCardinality.ZeroToMany,
            SupplSemId = "http://www.w3.org/2011/modbus")]
        public List<CD_GenericInterface> InterfaceMODBUS = new List<CD_GenericInterface>();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasxPredefinedCardinality.ZeroToMany,
            SupplSemId = "http://www.w3.org/2011/mqtt")]
        public List<CD_GenericInterface> InterfaceMQTT = new List<CD_GenericInterface>();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasxPredefinedCardinality.ZeroToMany,
            SupplSemId = "http://opcfoundation.org/UA/WoT-Binding")]
        public List<CD_GenericInterface> InterfaceOPCUA = new List<CD_GenericInterface>();

        [AasConcept(Cd = "https://admin-shell.io/idta/AssetInterfacesDescription/1/0/Interface", Card = AasxPredefinedCardinality.ZeroToMany,
            SupplSemId = "http://www.w3.org/2022/bacnet")]
        public List<CD_GenericInterface> InterfaceBACNET = new List<CD_GenericInterface>();

        // auto-generated informations
        public AasClassMapperInfo __Info__ = null;
    }
}