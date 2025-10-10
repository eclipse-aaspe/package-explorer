/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Extensions;
using Aas = AasCore.Aas3_1;
using System;
using AdminShellNS;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Digital Nameplate 
    /// </summary>
    public static class InfoAccessDigitalNameplateV20
    {
        /// <summary>
        /// Add all contact info together as list of string
        /// </summary>
        /// <returns>May return <c>null</c> in case of no results!</returns>
        public static List<string> ContactInfoToStrings(Aas.IReferable rootOfContactAdrInfo,
            string defaultLang = null)
        {
            var childs = rootOfContactAdrInfo?.EnumerateChildrenFor(SMC: true, SML: true)?.ToList();
            if (childs == null || childs.Count() < 1)
                return null;

            // access data
            var res = new List<string>();
            var defs = AasxPredefinedConcepts.DigitalNameplateV20.Static;
            var mm = MatchMode.Relaxed;

            Action<List<Aas.ISubmodelElement>, string, Aas.IKey> tryAdd = (coll, header, key) =>
            {
                var st = coll?
                    .FindFirstSemanticIdAs<Aas.IMultiLanguageProperty>(key, mm)?
                    .Value?.GetDefaultString(defaultLang);
                if (st?.HasContent() == true)
                    res.Add(("" + header) + st);
            };

            tryAdd(childs, null, defs.CD_ZipCodeOfPOBox?.GetSingleKey());
            tryAdd(childs, null, defs.CD_POBox?.GetSingleKey());
            tryAdd(childs, null, defs.CD_Street?.GetSingleKey());
            tryAdd(childs, null, defs.CD_CityTown?.GetSingleKey());
            tryAdd(childs, null, defs.CD_StateCounty?.GetSingleKey());
            tryAdd(childs, null, defs.CD_NationalCode?.GetSingleKey());
            tryAdd(childs, null, defs.CD_AddressOfAdditionalLink?.GetSingleKey());

            // Phone

            var smc2 = childs?
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Phone?.GetSingleKey(), mm);
            if (smc2 != null)
                tryAdd(smc2.Value, "\u260e", defs.CD_TelephoneNumber?.GetSingleKey());

            // Fax

            smc2 = childs?
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Fax?.GetSingleKey(), mm);
            if (smc2 != null)
                tryAdd(smc2.Value, "\U0001f5b7", defs.CD_FaxNumber?.GetSingleKey());

            // Email

            smc2 = childs?
                .FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(defs.CD_Email?.GetSingleKey(), mm);
            if (smc2 != null)
                tryAdd(smc2.Value, "\U0001f4e7", defs.CD_EmailAddress?.GetSingleKey());

            // OK
            return res;
        }
    }
}
