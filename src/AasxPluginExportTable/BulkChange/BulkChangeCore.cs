/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using System.Reflection;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML.Excel;
using AasxPluginExportTable.Table;

namespace AasxPluginExportTable.BulkChange
{
    /// <summary>
    /// This class allows bulk changing attributes of an Environment.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// functionality uses table input.
    /// </summary>
    public static class BulkChangeCore
    {
        /// <summary>
        /// Single change in Reference. 
        /// Will match on single key, only value.
        /// Will extend <c>to</c> from string to Keys.
        /// </summary>
        /// <returns>Either 0 or 1, if changed.</returns>
        public static int ChangeIdInReference(Aas.IReference rf, string fromId, string toId, Aas.KeyTypes toType)
        {
            // access
            if (rf?.IsValid() != true || fromId == null || toId == null)
                return 0;

            // match
            if (rf.Count() != 1 || rf.Keys[0].Value?.HasContent() != true || !rf.Keys[0].Value.Equals(fromId))
                return 0;

            // match!
            var k = new Aas.Key(toType, toId);
            rf.Keys.Clear();
            rf.Keys.Add(k);
            rf.Type = rf.GuessType();
            return 1;
        }

        /// <summary>
        /// Bulk change of CD.id, CD.isCaseOf, SM.(suppl)semanticId, SME.(suppl)semanticId.
        /// </summary>
        /// <returns>-1 in case of error, number of changes, else.</returns>
        public static int BulkChangeSemanticId(Aas.IEnvironment env, List<BulkChangeSemanticIdPair> pairs)
        {
            // access
            if (env == null || pairs == null) 
                return -1;
            int num = 0;

            // do two passes, all pairs
            for (int pass = 0; pass < 2; pass++)
                foreach (var pair in pairs)
                {
                    // make this very clear
                    if (pair == null)
                        continue;
                    var fromId = ((pass == 0) ? pair.OldId : pair.IntermediateId).Trim();
                    var toId = ((pass == 0) ? pair.IntermediateId : pair.NewId).Trim();

                    // iterate CDs
                    foreach (var cd in env.AllConceptDescriptions())
                    {
                        // CD.id
                        if (cd.Id?.Trim().Equals(fromId) == true)
                        {
                            cd.Id = toId;
                            num++;
                        }

                        // CD.isCaseOf
                        if (cd.IsCaseOf != null)
                            foreach (var ico in cd.IsCaseOf)
                                num += ChangeIdInReference(ico, fromId, toId, Aas.KeyTypes.GlobalReference);
                    }

                    // iterate Submodels (only changing semanticIds, therefore linking AAS <-> Submodels not relevant)
                    foreach (var sm in env.AllSubmodels())
                    {
                        // SM.semanticId
                        num += ChangeIdInReference(sm.SemanticId, fromId, toId, Aas.KeyTypes.Submodel);

                        // SM.supplSemanticId
                        if (sm.SupplementalSemanticIds != null)
                            foreach (var ssi in sm.SupplementalSemanticIds)
                                num += ChangeIdInReference(ssi, fromId, toId, Aas.KeyTypes.GlobalReference);

                        // now iterate all SME
                        sm.RecurseOnSubmodelElements(null, (o, parents, sme) => {
                            // access
                            if (sme == null)
                                return true;

                            // SME.semanticId
                            num += ChangeIdInReference(sme.SemanticId, fromId, toId, Aas.KeyTypes.GlobalReference);

                            // SME.supplSemanticId
                            if (sme.SupplementalSemanticIds != null)
                                foreach (var ssi in sme.SupplementalSemanticIds)
                                    num += ChangeIdInReference(ssi, fromId, toId, Aas.KeyTypes.GlobalReference);

                            // recurse on
                            return true;
                        });
                    }
                }

            // ok
            return num;
        }
    }
}
