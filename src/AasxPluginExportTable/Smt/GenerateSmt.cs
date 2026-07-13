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
using AasCore.Aas3_0;

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

        public static async Task<bool> SmtTemplateTemplateStart(
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
                return false;
            }

            var sm = ticket?.Submodel;
            if (true != sm?.SemanticId?.Matches(
                    AasxPredefinedConcepts.SmtAdditions.Static.CD_SmtTemplateTemplate?.GetCdReference(),
                    MatchMode.Relaxed))
            {
                log?.Error("No Submodel selected or Submodel not qualified as SMT template template.");
                return false;
            }

            // elements on top-level?
            if (sm.SubmodelElements == null)
            {
                log?.Error("No Submodel elements with actions for SMT template template.");
                return false;
            }

            // iterate
            foreach (var outerSme in sm.SubmodelElements)
            {
                // semanticId
                if (outerSme?.SemanticId == null)
                    continue;

                // copy
                if (outerSme is Aas.IReferenceElement refel
                    && outerSme.SemanticId.Matches(
                        AasxPredefinedConcepts.SmtAdditions.Static.CD_SmtRemoveAutoGen?.GetCdReference(),
                        MatchMode.Relaxed))
                {
                    // log
                    log?.Info("Remove auto-generated SME from {0} ..",
                        "" + refel.Value?.ToStringExtended(1));

                    // all info
                    if (refel.Value?.IsValid() != true)
                    {
                        log?.Error("Remove auto-generated element does not have value target.");
                        continue;
                    }

                    // find target
                    var refelRf = env.FindReferableByReference(refel.Value);
                    if (refelRf == null)
                    {
                        log?.Error("Remove auto-generated value Reference not found in AAS Environment.");
                        continue;
                    }

                    // Need to find target Submodel. Should be at top of the list
                    Aas.ISubmodel topSm = null;
                    if (refel.Value.Count() >= 1
                        && refel.Value.Keys.First().Type == Aas.KeyTypes.Submodel)
                    {
                        topSm = env.FindSubmodelById(refel.Value.Keys.First().Value);
                        log?.Info("Fount target Submodel to be: {0}",
                            (topSm == null) ? "<Not found>" : "" + topSm.IdShort);
                    }

                    if (topSm == null)
                    {
                        log?.Error("Target Submodel of Remove auto-generated elements not found in AAS Environment.");
                        continue;
                    }

                    // make a deletion list (a bit tedious)
                    var toDel = new List<Tuple<Aas.IReferable, Aas.ISubmodelElement>>();
                    if (refelRf is Aas.ISubmodel refelSm)
                    {
                        topSm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                        {
                            if (null != sme.FindQualifierOfType(
                                AasxPredefinedConcepts.SmtAdditions.Static.Qual_SmtAutoGenerated.Type))
                            {
                                toDel.Add(new Tuple<Aas.IReferable, Aas.ISubmodelElement>(
                                    (parents.Count > 0) ? parents.Last() : topSm, sme));
                            }
                            return true;
                        });
                    }
                    else if (refelRf is Aas.ISubmodelElement refelSme)
                    {
                        refelSme.RecurseOnReferables(null, (o, parents, rf) =>
                        {
                            if (rf is not Aas.ISubmodelElement sme)
                                return true;
                            if (null != sme.FindQualifierOfType(
                                AasxPredefinedConcepts.SmtAdditions.Static.Qual_SmtAutoGenerated.Type))
                            {
                                toDel.Add(new Tuple<Aas.IReferable, Aas.ISubmodelElement>(
                                    (parents.Count > 0) ? parents.Last() : topSm, sme));
                            }
                            return true;
                        });
                    }

                    // info
                    log?.Info("Found {0} to delete ..", toDel.Count);

                    // Just take the rather over-simplified approach to delete all elements from
                    // its respective parents. A bit tedious, again.
                    // From back to front, to tackle the deep structures first.
                    toDel.Reverse();
                    foreach (var del in toDel)
                    {
                        var parent = del.Item1;
                        var sme = del.Item2;
                        log?.Info("Removing {0} from {1} ..", sme?.IdShort, parent?.IdShort);
                        parent.Remove(sme);
                    }

                    // done
                    log?.Info("Done.");
                }

                // copy
                if (outerSme is Aas.IRelationshipElement rele
                    && outerSme.SemanticId.Matches(
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
                    if (firstRf == null || secondRf == null)
                    {
                        log?.Error("Copy Relationship first and/ or second Reference not found in AAS Environment.");
                        continue;
                    }

                    // arguments?
                    var q = outerSme.HasExtensionOfName("ExportSmt.Args");
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
                            var sme = all?.FindFirstIdShort(ids);
                            if (sme == null)
                                log?.Error("Requested idShort {0} not found! Skipping.", ids);
                            else
                                elems.Add(sme);
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
                        var q2 = AasxPredefinedConcepts.SmtAdditions.Static.Qual_SmtAutoGenerated.Copy();
                        newElem.Add(q2);

                        // put
                        PutToIntendedElement(secondRf, newElem);
                    }

                    // done
                    log?.Info("Done.");
                }
            }

            return true;
        }
    }
}
