/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using DocumentFormat.OpenXml.Bibliography;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Aas = AasCore.Aas3_0;

namespace AasxPluginBomStructure.Table
{
    /// <summary>
    /// This class create a context to a BOM Submodel, which shall be incrementally popoulated by
    /// table contents.
    /// </summary>
    public class ImportBomPopulateByTable
    {
        //
        // Context management
        //

        protected LogInstance _log;
        protected ImportExportBomRecord _job;
        protected Aas.ISubmodel _sm;
        protected Aas.IEnvironment _env;
        protected BomStructureOptions _options;

        protected Dictionary<string, Aas.IEntity> _keyToEnt = new Dictionary<string, Aas.IEntity>();

        public ImportBomPopulateByTable(
            LogInstance log,
            ImportExportBomRecord job,
            Aas.ISubmodel sm,
            Aas.IEnvironment env,
            BomStructureOptions options)
        {
            // context
            _log = log;
            _job = job;
            _sm = sm;
            _env = env;
            _options = options;

            // prepare Submodel
            if (sm == null || _job == null)
                return;
            if (sm.SubmodelElements == null)
                sm.SubmodelElements = new List<Aas.ISubmodelElement>();
        }

        public bool IsValid
        {
            get
            {
                return true == _job?.IsValid()
                    && _sm?.SubmodelElements != null
                    && _env != null;
            }
        }

        //
        // Population
        //

        /// <summary>
        /// Nicer to print cell text
        /// </summary>
        public string BeautifyAndShortenCell(string cellText)
        {
            if (cellText == null)
                return "";
            cellText = cellText.Replace("\r", "<CR>");
            cellText = cellText.Replace("\n", "<NL>");
            cellText = AdminShellUtil.ShortenWithEllipses(cellText, 30);
            return cellText;
        }

        /// <summary>
        /// As debugging import is difficult, identifying table is 
        /// important
        /// </summary>
        public string PrepareAbstract(IImportTableProvider table)
        {
            var cells = new List<string>();
            for (int ri=0; ri<table.MaxRows; ri++)
                for (int ci=0; ci<table.MaxCols; ci++)
                {
                    if (cells.Count >= 5)
                        break;
                    var ct = table.Cell(ri, ci);
                    if (ct?.HasContent() == true)
                        cells.Add(BeautifyAndShortenCell(ct));
                }
            return "[" + string.Join('|', cells) + "]";
        }

        public void PopulateBy(IImportTableProvider table)
        {
            // access
            if (!IsValid || table == null || table.MaxRows < 1 || table.MaxCols < 1
                || _job.RowStart < 1 || _job.ColIdShort < 1)
                return;

            _log?.Info("{0}", "Starting populating BOM items from NEW TABLE. Table has " +
                table.MaxRows + " rows and " + table.MaxCols + " columns (at max) and " +
                "starts with cells: " + PrepareAbstract(table));

            Aas.IEntity rootEnt = null;

            var addedEnts = new List<Aas.IEntity>();

            for (int ri = _job.RowStart - 1; ri < table.MaxRows; ++ri)
            {
                // get idShort
                var idShort = AdminShellUtil.FilterFriendlyName(
                                "" + table.Cell(ri, _job.ColIdShort - 1),
                                pascalCase: true,
                                fixMoreBlanks: true);
                if (idShort?.HasContent() != true)
                    continue;

                // get DisplayName
                string displayName = null;
                if (_job.ColDispName >= 1)
                    displayName = table.Cell(ri, _job.ColDispName - 1);

                // start with a entity
                var ent = new Aas.Entity(Aas.EntityType.CoManagedEntity,
                            idShort: idShort,
                            semanticId: AasxPredefinedConcepts.HierarchStructV11.Static.CD_Node?.GetCdReference(),
                            displayName: (displayName == null) ? null
                                         : new List<Aas.ILangStringNameType>(new[] { new Aas.LangStringNameType("en", displayName) }));

                // lambda to add some properties
                Action<string, Aas.IKey, Aas.DataTypeDefXsd, string> lambdaAddIfProp = (idShort, semId, valueType, value) =>
                {
                    if (value?.HasContent() != true)
                        return;
                    var prop = new Aas.Property(
                        valueType: valueType,
                        idShort: idShort,
                        semanticId: (semId == null) ? null
                                    : new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                                        new List<Aas.IKey>(new[] { semId })),
                        value: value);
                    ent.Add(prop);
                };

                if (_job.ColBulkCount >= 1)
                {
                    var stBC = table.Cell(ri, _job.ColBulkCount - 1);

                    // nneds to be a number and exclude the trivial case
                    if (int.TryParse(stBC, out var bc)
                        && bc > 1)
                    {
                        lambdaAddIfProp(
                            "BulkCount",
                            AasxPredefinedConcepts.HierarchStructV11.Static.CD_BulkCount?.GetSingleKey(),
                            Aas.DataTypeDefXsd.UnsignedLong,
                            stBC);
                    }
                }

                if (_job.ColRefDesignation >= 1)
                    lambdaAddIfProp(
                        "ReferenceDesignation",
                        null,
                        Aas.DataTypeDefXsd.AnyUri,
                        table.Cell(ri, _job.ColRefDesignation - 1));

                if (_job.ColManufacturer >= 1)
                    lambdaAddIfProp(
                        "ManufacturerName",
                        AasxPredefinedConcepts.DigitalNameplateV20.Static.CD_ManufacturerName?.GetSingleKey(),
                        Aas.DataTypeDefXsd.AnyUri,
                        table.Cell(ri, _job.ColRefDesignation - 1));

                if (_job.ColPartName >= 1)
                    lambdaAddIfProp(
                        "PartDesignation",
                        AasxPredefinedConcepts.DefinitionsExperimental.BomExtensions.Static
                            .CD_PartDesignation?.GetSingleKey(),
                        Aas.DataTypeDefXsd.AnyUri,
                        table.Cell(ri, _job.ColPartName - 1));

                if (_job.ColPartOrderCode >= 1)
                    lambdaAddIfProp(
                        "PartOrderCode",
                        AasxPredefinedConcepts.DefinitionsExperimental.BomExtensions.Static
                            .CD_PartOrderCode?.GetSingleKey(),
                        Aas.DataTypeDefXsd.AnyUri,
                        table.Cell(ri, _job.ColPartOrderCode - 1));

                if (_job.ColPartUrl >= 1)
                    lambdaAddIfProp(
                        "PartUrl",
                        AasxPredefinedConcepts.DefinitionsExperimental.BomExtensions.Static
                            .CD_PartUrl?.GetSingleKey(),
                        Aas.DataTypeDefXsd.AnyUri,
                        table.Cell(ri, _job.ColPartUrl - 1));

                // any hierarchy
                var hier = new List<string>();
                if (_job.ColHierarchy != null)
                    foreach (var ci in _job.ColHierarchy)
                        if (ci >= 1)
                        {
                            var st = table.Cell(ri, ci - 1);
                            if (st == null || !int.TryParse(st, out var i))
                                continue;
                            hier.Add(i.ToString());
                        }

                // remove double zeros
                while (hier.Count >= 2 && hier[^1] == "0" && hier[^2] == "0")
                    hier.RemoveAt(hier.Count - 1);

                // debug
                _log?.Info($"Found BOM entity idShort={idShort} with hierarchy: " + string.Join("/", hier));

                // any hierarchy info
                Aas.IEntity parent = null;
                if (hier.Count == 1)
                {
                    if (rootEnt == null)
                        // add as root
                        rootEnt = ent;
                    else
                        // use the first added root
                        parent = rootEnt;
                }
                else if (hier.Count > 1)
                {
                    // add deeper

                    // construct search key (all except last)
                    var keyL1 = string.Join("/", hier.SkipLast(1));

                    // find?
                    if (_keyToEnt?.ContainsKey(keyL1) != true)
                    {
                        // check if there is a hierarchy above?
                        if (hier.Count > 2)
                        {
                            var keyL2 = string.Join("/", hier.SkipLast(2));
                            if (_keyToEnt?.ContainsKey(keyL2) == true)
                            {
                                parent = _keyToEnt[keyL2];
                            }
                        }

                        // add current entity to remember
                        _keyToEnt?.Add(keyL1, ent);

                        // special rule: if exactly 2 deep (that is right '0' and left one level!)
                        // add to root
                        if (hier.Count == 2)
                            parent = rootEnt;
                    }
                    else
                    {
                        // L1 found!
                        parent = _keyToEnt[keyL1];
                    }
                }

                // now, how to add?
                if (parent == null)
                {
                    // add as root, now isPartOf
                    _sm?.Add(ent);
                }
                else
                {
                    // add to parent, hasPart
                    parent.Add(ent);

                    // build relationship
                    var klFirst = _sm.BuildKeysToTop(parent as Aas.ISubmodelElement);
                    var klSecond = _sm.BuildKeysToTop(ent as Aas.ISubmodelElement);

                    var relHasPart = new Aas.RelationshipElement(
                        idShort: "HasPart_" + ent.IdShort,
                        semanticId: AasxPredefinedConcepts.HierarchStructV11.Static.CD_HasPart.GetCdReference(),
                        first: new Aas.Reference(Aas.ReferenceTypes.ModelReference, klFirst),
                        second: new Aas.Reference(Aas.ReferenceTypes.ModelReference, klSecond));

                    if (_job.RelBelowEntity)
                    {
                        parent.Add(relHasPart);
                    }
                    else
                    {
                        // can even go one up?
                        var par = parent;
                        if (par.Parent is Aas.IEntity)
                            par = par.Parent as Aas.IEntity;
                        par.Add(relHasPart);
                    }
                }

                // remember 
                addedEnts.Add(ent);
            }

            _log?.Info($"End of available table rows {table.MaxRows} reached. Stopping.");

            // sort
            if (_job.SortEntities)
            {
                foreach (var ent in addedEnts)
                    if (ent?.Statements != null)
                        ent.Statements.Sort( (e1, e2) => {
                            var kt1 = e1?.GetSelfDescription()?.KeyType;
                            var kt2 = e2?.GetSelfDescription()?.KeyType;
                            if (kt1 == null || kt2 == null)
                                return 0;
                            if (kt1.Value < kt2.Value)
                                return -1;
                            return +1;
                        });
                _log?.Info($"Entities sorted for SME type");
            }
        }
    }
}
