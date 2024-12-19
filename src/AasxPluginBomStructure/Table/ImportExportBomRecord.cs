﻿/*
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
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginBomStructure.Table
{
    /// <summary>
    /// Configurable options for the import / export of BOM items w.r.t. a Submodel.
    /// </summary>
    public class ImportExportBomRecord
    {
        //
        // Types
        //

        public enum FormatEnum { Excel = 0 }
        public static string[] FormatNames = new string[] {
            "Excel"
        };

        //
        // Members
        //

        public string Name = "";

        public int Format = 0;

        /// <summary>
        /// 1-based row, at which the data starts.
        /// </summary>
        public int RowStart = 2;

        /// <summary>
        /// 1-based column indices for all columns which specify the hierarchical position
        /// of the BOM items. Input is a comma-separated list of ints.
        /// </summary>
        public List<int> ColHierarchy = null;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column bulk count",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// If positive, 1-based column for potential bulk count information of the 
        /// BOM item.
        /// </summary>
        public int ColBulkCount = -1;

        [AasxMenuArgument(help: "1-based column, mandatory, will be filtered")]
        [AnyUiEditField(uiHeader: "Column IdShort",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// If positive, 1-based column for IdShort of the BOM item.
        /// Note: all invalid characters will be filtered out.
        /// </summary>
        public int ColIdShort = 1;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column DisplayName",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// If positive, 1-based column for DisplayName (EN) of the BOM item.
        /// </summary>
        public int ColDispName = -1;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column ref.desig.",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// positive, 1-based column for reference designation of the BOM item.
        /// </summary>
        public int ColRefDesignation = -1;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column manufacturer",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// positive, 1-based column for name of manufacturer of the BOM item.
        /// </summary>
        public int ColManufacturer = -1;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column part name",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// positive, 1-based column for name of manufactured part name of the BOM item.
        /// </summary>
        public int ColPartName = -1;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column part code",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// positive, 1-based column for name of manufactured part order code of the BOM item.
        /// </summary>
        public int ColPartOrderCode = -1;

        [AasxMenuArgument(help: "1-based column, optional, -1 will disable")]
        [AnyUiEditField(uiHeader: "Column part URL",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// positive, 1-based column for name of manufactured part URL of the BOM item.
        /// </summary>
        public int ColPartUrl = -1;

        [AasxMenuArgument(help: "optional, -1 means all worksheets")]
        [AnyUiEditField(uiHeader: "# worksheets",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// If >= 1, number of work sheets in table to process, before stopping
        /// </summary>
        public int NumOfWorksheets = -1;

        [AasxMenuArgument(help: "place the relation below entity")]
        [AnyUiEditField(uiHeader: "Rel. below Entity",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// If <c>true</c>, will place the relation below entity, if <c>false</c>,
        /// will place on the same level as entity.
        /// </summary>
        public bool RelBelowEntity = true;

        [AasxMenuArgument(help: "sort entities for SME types")]
        [AnyUiEditField(uiHeader: "Sort entities",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        /// <summary>
        /// If <c>true</c>, will sort entities' childs for SME type.
        /// Note: This will separate Entity elements from Relationship elements for clarity.
        /// </summary>
        public bool SortEntities = false;

        public bool IsValid()
        {
            return RowStart >= 1
                && ColIdShort >= 1;
        }

        //
        // Constructurs
        //

        public ImportExportBomRecord() { }

        public void SaveToFile(string fn)
        {
            using (StreamWriter file = File.CreateText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public static ImportExportBomRecord LoadFromFile(string fn)
        {
            using (StreamReader file = File.OpenText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                var res = (ImportExportBomRecord)serializer.Deserialize(file, typeof(ImportExportBomRecord));
                return res;
            }
        }

    }
}