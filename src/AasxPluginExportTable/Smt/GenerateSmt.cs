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
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using System.Collections;
using System.Drawing.Imaging;
using AasxPluginExportTable.Uml;
using AasxPluginExportTable.Table;
using System.Runtime.Intrinsics.X86;
using AnyUi;
using System.IO.Packaging;

namespace AasxPluginExportTable.Smt
{
    /// <summary>
    /// This class allows generating SMT contentd for various operations.
    /// </summary>
    public class GenerateSmt
    {
        protected static IEnumerable<Aas.ISubmodelElement> FindIntendedElements(Aas.IReferable rf)
        {
            if (rf is Aas.ISubmodel sm)
            {
                if (sm?.SubmodelElements != null)
                    foreach (var x in sm.SubmodelElements)
                        yield return x;
            }
            else
            if (rf is Aas.ISubmodelElementCollection
                || rf is Aas.ISubmodelElementList)
            {
                foreach (var x in rf.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                        yield return sme;
            }
            else
            if (rf is Aas.ISubmodelElement sme)
            {
                // yield itself
                yield return sme;
            }
        }

        protected static void PutToIntendedElement(
            Aas.IReferable rf, Aas.ISubmodelElement sme)
        {
            if (rf is Aas.ISubmodel rfSm)
            {
                rfSm.Add(sme);
            }
            else
            if (rf is Aas.ISubmodelElement rfSme)
            {
                rfSme.Add(sme);
            }
        }

        public static async Task SmtTemplateTemplateStart(
            ExportTableOptions options,
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext,
            ExportTableOptions pluginOptions)
        {
            // first
            await Task.Yield();

            // check for pre-conditions
            var env = ticket?.Env;
            if (env == null)
            {
                log?.Error("No AAS Environment information! Aborting.");
                return;
            }

            var sm = ticket?.Submodel;
            if (true != sm?.SemanticId?.Matches(
                    AasxPredefinedConcepts.SmtAdditions.Static.CD_SmtTemplateTemplate?.GetCdReference(),
                    MatchMode.Relaxed))
            {
                log?.Error("No Submodel selected or Submodel not qualified as SMT template template.");
                return;
            }

            // elements on top-level?
            if (sm.SubmodelElements == null)
            {
                log?.Error("No Submodel elements with actions for SMT template template.");
                return;
            }

            // iterate
            foreach (var sme in sm.SubmodelElements)
            {
                // semanticId
                if (sme?.SemanticId == null)
                    continue;

                // copy
                if (sme is Aas.IRelationshipElement rele
                    && sme.SemanticId.Matches(
                        AasxPredefinedConcepts.SmtAdditions.Static.CD_SmtCopyElements?.GetCdReference(),
                        MatchMode.Relaxed))
                {
                    // log
                    log?.Info("Copy SME from {0} to {1} ..",
                        "" + rele?.First?.ToStringExtended(1),
                        "" + rele?.Second?.ToStringExtended(1));

                    // all info
                    if (rele.First?.IsValid() != true
                        || rele.Second?.IsValid() != true)
                    {
                        log?.Error("Copy Relationship does not have first/ seconds source/ targets.");
                        continue;
                    }

                    // find targets
                    var firstRf = env.FindReferableByReference(rele.First);
                    var secondRf = env.FindReferableByReference(rele.Second);
                    if (rele.First?.IsValid() != true
                        || rele.Second?.IsValid() != true)
                    {
                        log?.Error("Copy Relationship first and/ or second Reference not found in AAS Environment.");
                        continue;
                    }

                    // arguments?
                    var q = sme.HasExtensionOfName("ExportSmt.Args");
                    var args = ExportSmtArguments.Parse(q?.Value);
                    if (args == null)
                        log?.Info("No ExportSmt.Args found?!");

                    // which elements?
                    var all = FindIntendedElements(firstRf)?.ToList();
                    var elems = all?.ToList();

                    if (args?.idShorts != null)
                    {
                        elems = new();
                        foreach(var ids in args.idShorts)
                        {
                            var sme2 = all?.FindFirstIdShort(ids);
                            if (sme2 == null)
                                log?.Error("Requested idShort {0} not found! Skipping.", ids);
                            else
                                elems.Add(sme2);
                        }
                    }

                    if (elems.Count < 1)
                    {
                        log?.Error("No elements to COPY found! Skipping.");
                        continue;
                    }

                    // cycle through the elements
                    foreach (var elem in elems)
                    {
                        // info
                        log?.Info("Copy element {0} ..", elem.IdShort);

                        // copy
                        var newElem = elem.Copy();

                        // qualifier


                        // put
                        PutToIntendedElement(secondRf, newElem);
                    }
                }
            }
        }
    }
}
