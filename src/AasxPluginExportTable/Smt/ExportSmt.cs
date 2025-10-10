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
    /// This class allows exporting a Submodel to an AsciiDoc specification.
    /// The general approach is to identify several dedicated SME (mostly BLOBs) and
    /// to chunk together their AsciiDoc contents.
    /// </summary>
    public class ExportSmt
    {
        protected LogInstance _log = null;
        protected AdminShellPackageEnvBase _package = null;
        protected Aas.ISubmodel _srcSm = null;
        protected ExportTableOptions _optionsAll = null;
        protected ExportSmtRecord _optionsSmt = null;
        protected string _tempDir = "";
        protected StringBuilder _adoc = new StringBuilder();
        protected bool _singleFile = true;

        protected string _locationPages = "";
        protected string _locationImages = "";
        protected string _locationDiagrams = "";

        protected void ProcessTextBlob(string header, Aas.IBlob blob)
        {
            // any content
            if (blob?.Value == null || blob.Value.Length < 1)
                return;

            // may wrap
            var text = System.Text.Encoding.UTF8.GetString(blob.Value);
            if (_optionsSmt.WrapLines >= 10)
            {
                text = AdminShellUtil.WrapLinesAtColumn(text, _optionsSmt.WrapLines);
            }

            // simply add
            if (header?.HasContent() == true)
                _adoc.AppendLine("");
            _adoc.AppendLine(header + text);
        }

        protected static string EscapeAdText(string input)
        {
            if (input == null)
                return null;
            input = input.Replace(@"""", @"\""");
            return input;
        }

        protected string EvalLinkArguments(ExportSmtArguments args, Aas.ISubmodelElement sme)
        {
            var astr = "";

            if (args?.width != null)
                astr += $",width=\"{AdminShellUtil.FromDouble(args.width ?? 0.0, "{0:0.0}")}%\"";

            var titStr = sme?.Description?.GetDefaultString();
            if (titStr?.HasContent() == true)
                astr += $",title=\"{EscapeAdText(titStr)}\"";

            astr = astr.Trim(',');

            return astr;
        }

        protected async Task ProcessImageLink(Aas.ISubmodelElement sme,
            string aasId = null,
            string smId = null,
            string idShortPath = null)
        {
            // first get to the data
            byte[] data = null;
            string dataExt = ".bin";
            if (sme is Aas.IFile smeFile)
            {
                // old: data = _package?.GetBytesFromPackageOrExternal(smeFile.Value);
                data = await _package?.GetBytesFromPackageOrExternalAsync(
                    smeFile.Value, aasId: aasId, smId: smId, idShortPath: idShortPath);

                dataExt = Path.GetExtension(smeFile.Value);
            }

            if (sme is Aas.IBlob smeBlop && smeBlop.Value != null)
            {
                // the BLOB may contain direct binary data or 
                // intends to transport text data only
                var convertStrBase64 = AdminShellUtil.CheckIfAsciiOnly(smeBlop.Value);

                var isTxtFmt = AdminShellUtil.CheckForTextContentType(smeBlop.ContentType);
                if (isTxtFmt)
                    convertStrBase64 = false;

                if (convertStrBase64)
                {
                    // assume base64 coded string
                    string strData = System.Text.Encoding.UTF8.GetString(smeBlop.Value);
                    data = System.Convert.FromBase64String(strData);
                }
                else
                    data = smeBlop.Value;

                // assume png
                dataExt = AdminShellUtil.GuessImageTypeExtension(data) ?? ".png";
                if (isTxtFmt)
                    dataExt = ".txt";
            }

            if (data == null)
            {
                _log?.Error("No image data found in AAS element {0}",
                    sme?.GetReference()?.ToStringExtended(1));
                return;
            }

            if (!dataExt.HasContent())
            {
                _log?.Error("No data format extension found in AAS element {0}",
                    sme?.GetReference()?.ToStringExtended(1));
                return;
            }

            // check if to link in text?
            var doLink = true;
            var q = sme.HasExtensionOfName("ExportSmt.Args");
            var args = ExportSmtArguments.Parse(q?.Value);
            if (args?.noLink == true)
                doLink = false;

            // determine (automatic) target file name
            var targetName = "image_" + Path.GetRandomFileName().Replace(".", "_");

            // define by idShort (as default)
            if (sme.IdShort.HasContent())
            {
                // filter a little more relaxed (allow "-")
                targetName = AdminShellUtil.FilterFriendlyName(sme.IdShort, 
                    regexForFilter: @"[^a-zA-Z0-9_-]");

                int p = targetName.ToLower().LastIndexOf("_dot_");
                if (p >= 0)
                {
                    dataExt = "." + targetName.Substring(p + "_dot_".Length);
                    targetName = targetName.Substring(0, p);
                }
            }
            var fn = targetName + dataExt;

            // may be overruled?
            if (args?.fileName?.HasContent() == true)
                fn = args.fileName;

            // save absolute
            var absFn = Path.Combine(_locationImages, fn);
            File.WriteAllBytes(absFn, data);
            _log?.Info("Image data with {0} bytes writen to {1}.", data.Length, absFn);

            // create link arguments
            var imgId = AdminShellUtil.FilterFriendlyName(sme.IdShort);
            var astr = EvalLinkArguments(args, sme);

            // create link text
            if (doLink)
            {
                _adoc.AppendLine("");
                _adoc.AppendLine($"image::{fn}[id=\"{imgId}\", {astr}]");
                _adoc.AppendLine("");
            }
        }

        protected void ProcessUml(Aas.IReferenceElement refel)
        {
            // access
            if (_package?.AasEnv == null || refel == null)
                return;

            // try find target of reference
            var target = _package?.AasEnv.FindReferableByReference(refel.Value);
            if (target == null)
            {
                _log?.Error("ExportSMT: No target reference for UML found in {0}",
                    refel.GetReference()?.ToStringExtended(1));
                return;
            }

            // check arguments
            var q = refel.HasExtensionOfName("ExportSmt.Args");
            var args = ExportSmtArguments.Parse(q?.Value);
            var processDepth = args?.depth ?? int.MaxValue;

            // determine (automatic) target file name
            var pumlName = "uml_" + Path.GetRandomFileName().Replace(".", "_");
            if (refel.IdShort.HasContent())
                pumlName = AdminShellUtil.FilterFriendlyName(refel.IdShort);
            var pumlFn = pumlName + ".puml";
            var absPumlFn = Path.Combine(_locationDiagrams, pumlFn);
            var extraAntoraPath = _optionsSmt.AntoraStyle ? "partial$diagrams/" : "";

            // make options
            var umlOptions = new ExportUmlRecord();
            if (args?.uml != null)
                umlOptions = args.uml;

            // make writer
            var writer = new PlantUmlWriter();
            writer.StartDoc(umlOptions, _package.AasEnv);
            writer.ProcessTopElement(target, processDepth);
            writer.ProcessPost();
            _log?.Info("ExportSMT: writing PlantUML to {0} ..", absPumlFn);
            writer.SaveDoc(absPumlFn);

            // create link arguments
            var astr = EvalLinkArguments(args, refel);

            // include file into AsciiDoc
            _adoc.AppendLine("");
            _adoc.AppendLine($"[plantuml, {pumlName}, svg, id=\"{pumlName}\", {astr}]");
            _adoc.AppendLine("----");
            _adoc.AppendLine("include::" + extraAntoraPath + pumlFn + "[]");
            _adoc.AppendLine("----");
            _adoc.AppendLine("");
        }

        protected void ProcessTables(Aas.IReferenceElement refel)
        {
            // access
            if (_package?.AasEnv == null || refel == null)
                return;

            // try find target of reference
            var target = _package?.AasEnv.FindReferableByReference(refel.Value);
            if (target == null)
            {
                _log?.Error("ExportSMT: No target reference for Tables found in {0}",
                    refel.GetReference()?.ToStringExtended(1));
                return;
            }

            // find options for tables
            if (_optionsAll?.Presets == null || _optionsSmt == null
                || _optionsSmt.PresetTables < 0
                || _optionsSmt.PresetTables > _optionsAll.Presets.Count)
            {
                _log?.Error("ExportSMT: Error accessing selected table presets for conversion.");
                return;
            }
            var optionsTable = _optionsAll.Presets[_optionsSmt.PresetTables];

            // check arguments
            var q = refel.HasExtensionOfName("ExportSmt.Args");
            var args = ExportSmtArguments.Parse(q?.Value);
            var processDepth = int.MaxValue;
            if (args?.depth != null)
            {
                processDepth = (int)args.depth;
                optionsTable.NoHeadings = true;
            }

            // determine (automatic) target file name
            var tableFn = "table_" + Path.GetRandomFileName().Replace(".", "_");
            if (refel.IdShort.HasContent())
                tableFn = AdminShellUtil.FilterFriendlyName(refel.IdShort);
            tableFn += ".adoc";
            var absTableFn = Path.Combine(_tempDir, tableFn);

            // may change, if to include
            if (_optionsSmt.IncludeTables)
            {
                absTableFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".adoc");
            }

            // start export
            _log?.Info("ExportSMT: Starting table export for element {0} ..",
                refel.GetReference()?.ToStringExtended(1));

            var ticket = new AasxMenuActionTicket();

            AnyUiDialogueTable.Export(
                _optionsAll, optionsTable, absTableFn,
                target, _package?.AasEnv, ticket, _log, maxDepth: processDepth,
                idOfElem: refel.IdShort,
                titleOfTable: EscapeAdText(refel.Description?.GetDefaultString()));

            // include file into AsciiDoc
            if (_optionsSmt.IncludeTables)
            {
                // read file, append
                var lines = File.ReadAllLines(absTableFn);

                _adoc.AppendLine("");
                _adoc.AppendLine("// Table generated from " + refel.GetReference()?.ToStringExtended(1));
                _adoc.AppendLine("");

                foreach (var ln in lines)
                    _adoc.AppendLine(ln);

                _adoc.AppendLine("");
            }
            else
            {
                // include file
                _adoc.AppendLine("");
                _adoc.AppendLine("include::" + tableFn + "[]");
                _adoc.AppendLine("");
            }
        }

        protected class SmeLinearItem
        {
            public Aas.IReference SemId;
            public Aas.ISubmodelElement Sme;
            public List<Aas.IReferable> Parents;
        }

        public async Task ExportSmtToFile(
            LogInstance log,
            AnyUiContextPlusDialogs displayContext,
            AdminShellPackageEnvBase package,
            Aas.IAssetAdministrationShell aas,
            Aas.ISubmodel submodel,
            ExportTableOptions optionsAll,
            ExportSmtRecord optionsSmt,
            string fn)
        {
            // access
            if (optionsSmt == null || submodel == null || optionsSmt == null || !fn.HasContent())
                return;
            _log = log;
            _package = package;
            _srcSm = submodel;
            _optionsAll = optionsAll;
            _optionsSmt = optionsSmt;

            // decide to write singleFile?
            _singleFile = fn.ToLower().EndsWith(".adoc");

            // create temp directory
            _tempDir = AdminShellUtil.GetTemporaryDirectory();
            log?.Info("ExportSmt: using temp directory {0} ..", _tempDir);

            _locationPages = _tempDir;
            _locationImages = _tempDir;
            _locationDiagrams = _tempDir;

            // sub-folders?
            if (optionsSmt.AntoraStyle)
            {
                try
                {
                    // create a lot of directories
                    var docRoot = Path.Combine(_tempDir, "documentation");
                    Directory.CreateDirectory(docRoot);

                    var modules = Path.Combine(docRoot, "modules");
                    Directory.CreateDirectory(modules);

                    var root = Path.Combine(modules, "ROOT");
                    Directory.CreateDirectory(root);

                    _locationPages = Path.Combine(root, "pages");
                    Directory.CreateDirectory(_locationPages);
                    
                    _locationImages = Path.Combine(root, "images");
                    Directory.CreateDirectory(_locationImages);
                    
                    _locationDiagrams = Path.Combine(Path.Combine(root, "partials"), "diagrams");
                    Directory.CreateDirectory(Path.Combine(_tempDir, "partials"));
                    Directory.CreateDirectory(_locationDiagrams);

                    _log?.Info(StoredPrint.Color.Black,
                        "Created dedicated sub-folders for documentation, modules, root, pages, images, partials/diagrams.");

                    // create some boiler plate 
                    var antoraYamlTxt = AdminShellUtil.CleanHereStringWithNewlines(
                        @"name: IDTA-00000
                        title: 'TODO'
                        version: 'v1.0'
                        start_page: ROOT:index.adoc");

                    System.IO.File.WriteAllText(Path.Combine(docRoot, "antora.yml"), antoraYamlTxt);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "Creating sub-folders within " + _tempDir);
                }
            }

            // predefined semantic ids
            var defs = AasxPredefinedConcepts.AsciiDoc.Static;
            var mm = MatchMode.Relaxed;

            // walk the Submodel (need to linearize because of async)
            var linearSmes = new List<SmeLinearItem>();
            _srcSm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
            {
                // semantic id
                var semId = sme?.SemanticId;
                if (semId?.IsValid() != true)
                    return true;

                // add
                linearSmes.Add(new SmeLinearItem()
                {
                    SemId = semId,
                    Sme = sme,
                    Parents = parents.ToList()
                });
                return true;
            });

            // now go this linear list
            foreach (var item in linearSmes)
            {
                var semId = item.SemId;
                var sme = item.Sme;

                // check for special semantic ids and process elements
                if (sme is Aas.IBlob blob)
                {
                    if (semId.Matches(defs.CD_TextBlock.GetCdReference(), mm))
                        ProcessTextBlob("", blob);
                    if (semId.Matches(defs.CD_CoverPage.GetCdReference(), mm))
                        ProcessTextBlob("", blob);
                    if (semId.Matches(defs.CD_Heading1.GetCdReference(), mm))
                        ProcessTextBlob("== ", blob);
                    if (semId.Matches(defs.CD_Heading2.GetCdReference(), mm))
                        ProcessTextBlob("=== ", blob);
                    if (semId.Matches(defs.CD_Heading3.GetCdReference(), mm))
                        ProcessTextBlob("==== ", blob);
                    if (semId.Matches(defs.CD_Heading4.GetCdReference(), mm))
                        ProcessTextBlob("===== ", blob);
                }

                if (sme is Aas.IFile || sme is Aas.IBlob)
                {
                    if (semId.Matches(defs.CD_ImageFile.GetCdReference(), mm))
                        await ProcessImageLink(sme,
                            aasId: aas?.Id, smId: submodel?.Id, 
                            idShortPath: ExtendISubmodelElement.CollectIdShortPathBySmeAndParents(
                                sme, item.Parents, separatorChar: '.', excludeIdentifiable: true));
                }

                if (sme is Aas.IReferenceElement refel)
                {
                    if (semId.Matches(defs.CD_GenerateUml.GetCdReference(), mm))
                        ProcessUml(refel);
                    if (semId.Matches(defs.CD_GenerateTables.GetCdReference(), mm))
                        ProcessTables(refel);
                }
            }

            // ok, build raw Adoc
            var adocText = _adoc.ToString();

            // build adoc file
            var title = (!optionsSmt.AntoraStyle && (_srcSm.IdShort?.HasContent() == true))
                    ? AdminShellUtil.FilterFriendlyName(_srcSm.IdShort)
                    : "index";
            var adocFn = title + ".adoc";
            var absAdocFn = Path.Combine(_locationPages, adocFn);

            // write it
            File.WriteAllText(absAdocFn, adocText);
            log?.Info("ExportSmt: written {0} bytes to temp file {1}.", adocText.Length, absAdocFn);

            // prepare ignore error patterns
            string[] ignoreError = null;
            if (_optionsAll?.SmtExportIgnoreError != null && _optionsAll.SmtExportIgnoreError.Count > 0)
                ignoreError = _optionsAll.SmtExportIgnoreError.ToArray();

            // start outside commands?
            if (_optionsSmt.ExportHtml)
            {
                var cmd = _optionsAll.SmtExportHtmlCmd;
                var args = _optionsAll.SmtExportHtmlArgs
                    .Replace("%WD%", "" + _tempDir)
                    .Replace("%ADOC%", "" + adocFn);

                displayContext?.MenuExecuteSystemCommand("Exporting HTML", _tempDir, cmd, args, ignoreError: ignoreError);
            }

            if (_optionsSmt.ExportPdf)
            {
                var cmd = _optionsAll.SmtExportPdfCmd;
                var args = _optionsAll.SmtExportPdfArgs
                    .Replace("%WD%", "" + _tempDir)
                    .Replace("%ADOC%", "" + adocFn);

                displayContext?.MenuExecuteSystemCommand("Exporting PDF", _tempDir, cmd, args, ignoreError: ignoreError);
            }

            // now, how to handle files?
            if (_singleFile)
            {
                // simply copy
                File.Copy(absAdocFn, fn, overwrite: true);
                log?.Info("ExportSmt: copied temp file to {0}", fn);
            }
            else
            {
                // create zip package
#if __old_
                var first = true;
                foreach (var infn in Directory.EnumerateFiles(_tempDir, "*"))
                {
                    AdminShellUtil.AddFileToZip(
                        fn, infn,
                        fileMode: first ? FileMode.Create : FileMode.OpenOrCreate);
                    first = false;
                }
#else
                try
                {
                    using (Package zip = System.IO.Packaging.Package.Open(fn, FileMode.Create))
                    {
                        AdminShellUtil.RecursiveAddDirToZip(
                            zip,
                            _tempDir);
                    }
                } catch (Exception ex)
                {
                    log?.Error(ex, $"ExportSmt: Error creating zip file {fn}");
                    throw;
                }
#endif
                log?.Info("ExportSmt: packed all files to {0}", fn);
            }

            // now, view?
            if (_optionsSmt.ViewResult)
            {
                var cmd = _optionsAll.SmtExportViewCmd;
                var args = _optionsAll.SmtExportViewArgs
                    .Replace("%WD%", "" + _tempDir)
                    .Replace("%ADOC%", "" + adocFn)
                    .Replace("%HTML%", "" + adocFn.Replace(".adoc", ".html"))
                    .Replace("%PDF%", "" + adocFn.Replace(".adoc", ".pdf"));

                displayContext?.MenuExecuteSystemCommand("Viewing results", _tempDir, cmd, args, ignoreError: ignoreError);
            }

            // remove temp directory
            Directory.Delete(_tempDir, recursive: true);
            log?.Info("ExportSmt: deleted temp directory.");
        }
    }
}
