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
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.Smt
{
    /// <summary>
    /// Some user options for exporting package to a (AsciiDoc) SMT specification
    /// </summary>
    public class ExportSmtRecord
    {
        [AasxMenuArgument(help: "Numerical zero based index of table export preset to use.")]
        public int PresetTables = 0;

        [AasxMenuArgument(help: "If >=10, column limit to hard wrap AsciiDoc lines at.")]
        public int WrapLines = 0;

        [AasxMenuArgument(help: "If true, will include table data in main AsciiDoc file.")]
        public bool IncludeTables = true;

        [AasxMenuArgument(help: "If true, will do dedicated sub-folders for images and diagrams.")]
        public bool AntoraStyle = false;

        [AasxMenuArgument(help: "If true, will execute external program to produce HTML from AsciiDoc " +
            "using options 'SmtExportHtmlCmd' and 'SmtExportHtmlArgs' with placeholders %WD%, %ADOC%.")]
        public bool ExportHtml = false;

        [AasxMenuArgument(help: "If true, will execute external program to produce PDF from AsciiDoc " +
            "using options 'SmtExportPdfCmd' and 'SmtExportPdfArgs' with placeholders %WD%, %ADOC%.")]
        public bool ExportPdf = false;

        [AasxMenuArgument(help: "If true, will execute external program while in temporary directory " +
            "using options 'SmtExportViewCmd' and 'SmtExportViewArgs' with placeholders %WD%, %ADOC%, %HTML%, %PDF%.")]
        public bool ViewResult = false;
    }
}
