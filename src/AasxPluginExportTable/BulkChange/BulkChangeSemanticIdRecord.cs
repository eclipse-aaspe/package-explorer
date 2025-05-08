/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxIntegrationBase;
using AnyUi;

namespace AasxPluginExportTable.BulkChange
{
    public class BulkChangeSemanticIdPair
    {
        public string OldId;
        public string IntermediateId;
        public string NewId;
    }

    public class BulkChangeSemanticIdRecord
    {
        //
        // Types
        //

        public enum FormatEnum { Excel }
        public static string[] FormatNames = new string[] { "Excel" };

        //
        // Members
        //

        public FormatEnum Format = 0;

        [AasxMenuArgument(help: "Specifies the 1-based row index for the change pairs.")]
        [AnyUiEditField(uiHeader: "Row for pairs",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public int RowPairs = 1;
    }
}
