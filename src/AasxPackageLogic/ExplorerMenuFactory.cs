/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using Extensions;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This partial class contains the dynamic definition of the main menu (only).
    /// </summary>
    public static class ExplorerMenuFactory
    {
        //// Note for UltraEdit:
        //// <MenuItem Header="([^"]+)"\s*(|InputGestureText="([^"]+)")\s*Command="{StaticResource (\w+)}"/>
        //// .AddWpf\(name: "\4", header: "\1", inputGesture: "\3"\)
        //// or
        //// <MenuItem Header="([^"]+)"\s+([^I]|InputGestureText="([^"]+)")(.*?)Command="{StaticResource (\w+)}"/>
        //// .AddWpf\(name: "\5", header: "\1", inputGesture: "\3", \4\)

        /// <summary>
        /// Dynamic construction of the main menu
        /// </summary>
        public static AasxMenu CreateMainMenu()
        {
            //
            // Start
            //

            var menu = new AasxMenu();

            //
            // File
            //

            menu.AddMenu(header: "File",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "New", header: "_New …", inputGesture: "Ctrl+N",
                    help: "Create new AASX package.")
                .AddWpfBlazor(name: "Open", header: "_Open (local) …", inputGesture: "Ctrl+O",
                    help: "Open (local) existing AASX package.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Source filename including a path and extension."))
                .AddWpfBlazor(name: "FileRepoQuery", header: "Query open repositories/ registries …", inputGesture: "F12",
                        help: "Selects and repository item (AASX) from the open AASX file repositories.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Index", "Zero-based integer index to the list of all open repos.")
                            .Add("AAS", "String with AAS-Id")
                            .Add("Asset", "String with Asset-Id."))
                .AddWpfBlazor(name: "Save", header: "_Save", inputGesture: "Ctrl+S",
                        help: "Save current file without asking for file name.")
                .AddWpfBlazor(name: "SaveAs", header: "_Save as …", 
                    help: "Saves current package to given file name and type.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Filename including path and extension."))
                .AddWpfBlazor(name: "FixAndFinalize", header: "Fix and Finalize ...",
                        help: "Identifies constraint validations of the meta model and applies automatic fixes. " +
                        "Saves the file with the same filename.")
                .AddWpfBlazor(name: "Verify", header: "Verify (deprecated) ...",
                        help: "Verifies some aspects of the AASX file.")
                .AddWpfBlazor(name: "Close", header: "_Close …",
                        help: "Closes the current file.")
                .AddWpfBlazor(name: "CheckAndFix", header: "Check, validate and fix …",
                        help: "Converts the current file to XML/ JSON and applies validation on these.")
                .AddMenu(header: "Security …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "Sign", header: "_Sign (Submodel, Package) …",
                        help: "Sign a Submodel or SubmodelElement.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("UseX509", "Use X509 (true) or Verifiable Credential (false)")
                            .Add("Source", "Source package (.aasx) file.")
                            .Add("Certificate", "Certificate (.cer) file.")
                            .Add("Target", "Target package (.aasx2) file."))
                    .AddWpfBlazor(name: "ValidateCertificate", header: "_Validate (Submodel, Package) …",
                        help: "Validate a already signed Submodel or SubmodelElement.")
                    .AddWpfBlazor(name: "Encrypt", header: "_Encrypt (Package) …",
                        help: "Encrypts a Submodel, SubmodelElement or Package. For the latter, the arguments " +
                              "are required.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Source", "Source package (.aasx) file.")
                            .Add("Certificate", "Certificate (.cer) file.")
                            .Add("Target", "Target package (.aasx2) file."))
                    .AddWpfBlazor(name: "Decrypt", header: "_Decrypt (Package) …",
                        help: "Decrypts a Package.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Source", "Source package (.aasx2) file.")
                            .Add("Certificate", "Certificate (.pfx) file.")
                            .Add("Target", "Target package (.aasx) file.")))
                .AddMenu(header: "Reports …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "AssessSmt", header: "_Assess Submodel template …",
                        help: "Checks for a set of defined features for a Submodel template " +
                            "and reports the results. ",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Target", "Target report file (*.txt, *.xlsx)."))
                    .AddWpfBlazor(name: "CompareSmt", header: "_Compare Submodel template in main and auxiliary …",
                        help: "Compares Submodel templates given in main and auxiliary packages.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Target", "Target report file (*.txt, *.xlsx).")))
                .AddSeparator()
                .AddWpfBlazor(name: "OpenAux", header: "Open Au_xiliary AAS …", inputGesture: "Ctrl+Shift+X",
                    help: "Open existing AASX package to the auxiliary buffer (non visible in the tree).",
                    args: new AasxMenuListOfArgDefs()
                        .Add("File", "Source filename including a path and extension."))
                .AddWpfBlazor(name: "CloseAux", header: "Close Auxiliary AAS",
                        help: "Closes auxiliary buffer.")
                .AddSeparator()
                .AddMenu(header: "Further connect options …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "ConnectSecure", header: "Secure Connect (deprecated) …",
                        help: "Monolithic secure connect to a HTTP repository.")
                    .AddWpfBlazor(name: "ConnectOpcUa", header: "Connect via OPC-UA (deprecated) …",
                        help: "In future versions, connect to an AAS on a OPC UA server.")
                    .AddWpfBlazor(name: "ConnectRest", header: "Connect via REST (deprecated) …",
                        help: "Very old, very deprecated access to REST server.")
                    .AddWpfBlazor(name: "ConnectIntegrated", header: "Connect integrated (deprecated) …",
                        help: "Monolithic, but stepwise connection with a lot of information steps for demonstration.")
                    .AddWpfBlazor(name: "FileRepoConnectRepository", header: "Connect HTTP/REST repository … (deprecated, for Event demo)",
                        help: "Connects to an online repository via HTTP/REST.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Endpoint", "Endpoint of repo (without \"/server/listaas\").")))
                .AddSeparator()
                .AddMenu(header: "API for Registry and Repository …", childs: (new AasxMenu())
                    .AddWpf(name: "AddBaseAddress", header: "Add preset for base address …",
                        help: "Add a base address of a Registry or Repository in order to quickly access it in drop down menues.")
                    .AddWpf(name: "ClearBaseCredentials", header: "Clear all stored credentials …",
                        help: "Clear all attached information to different base addresses in order to enter new credentials.")
                    .AddWpfBlazor(name: "ConnectExtended", header: "Connect (extended) …",
                        help: "Establish a connection to Registry/ Repository and download AAS entities in order to edit them.",
                        args: new AasxMenuListOfArgDefs()
                            .AddFromReflection(new PackageContainerHttpRepoSubset.ConnectExtendedRecord()))
                    .AddWpfBlazor(name: "ApiUploadAssistant", header: "Upload assistant …",
                        help: "Upload selected AAS entities in a piece-by-piece approach to a Repository " +
                              "in order to prevent data overwrite.",
                        args: new AasxMenuListOfArgDefs()
                            .AddFromReflection(new PackageContainerHttpRepoSubset.UploadAssistantJobRecord()))
                    .AddWpfBlazor(name: "ApiUploadFiles", header: "Upload files …",
                        help: "Select a list of AASX files and upload them to a Repository, preventing needless data overwrite.",
                        args: new AasxMenuListOfArgDefs()
                            .AddFromReflection(new PackageContainerHttpRepoSubset.UploadFilesJobRecord()))
                    .AddWpf(name: "CreateRepoFromApi", header: "Create (local) file repository from API base …",
                        help: "Take downloaded entities and convert them to a local file repository (JSON list of addresses).",
                        args: new AasxMenuListOfArgDefs()
                            .AddFromReflection(new PackageContainerHttpRepoSubset.ConnectExtendedRecord()))
                )
                .AddSeparator()
                .AddMenu(header: "AASX File Repository …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "FileRepoNew", header: "New (local) repository …",
                        help: "Create new (empty) file repository.")
                    .AddWpfBlazor(name: "FileRepoOpen", header: "Open (local) repository …",
                        help: "Opens an existing AASX file repository and adds it to the list of open repos.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Path and filename of existing AASX file repository."))
                    .AddSeparator()
                    .AddWpfBlazor(name: "FileRepoCreateLRU", header: "Create last recently used list …",
                        help: "If present in application data directory, the LRU will give access to last recently used files."))
                .AddSeparator()
                .AddMenu(header: "Import …", attachPoint: "import", childs: (new AasxMenu())
					.AddWpfBlazor(name: "ImportAASX", header: "Import further AASX file into AASX …",
						help: "Import AASX file(s) with entities to overall AAS environment.",
						args: new AasxMenuListOfArgDefs()
							.Add("Files", "One or multiple AASX file(s) with AAS entities data."))
					.AddWpfBlazor(name: "ImportAML", header: "Import AutomationML into AASX …",
                        help: "Import AML file with AAS entities to overall AAS environment.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "AML file with AAS entities data."))
                    .AddWpfBlazor(name: "SubmodelRead", header: "Import Submodel from JSON …",
                        help: "Read Submodel from JSON and add/ replace existing to current AAS.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON file with Submodel data."))
                    .AddWpfBlazor(name: "SubmodelGet", header: "GET Submodel from URL …",
                        help: "Get Submodel from REST server.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("URL", "URL to get Submodel data from."))
                    .AddWpfBlazor(name: "ImportDictSubmodel", header: "Import Submodel from Dictionary …",
                        help: "UI assisted import from dictionaries such as ECLASS and IEC CDD to a Submodel.")
                    .AddWpfBlazor(name: "ImportDictSubmodelElements", header: "Import Submodel Elements from Dictionary …",
                        help: "UI assisted import from dictionaries such as ECLASS and IEC CDD to SubmodelElement.")
                    .AddWpfBlazor(name: "BMEcatImport", header: "Import BMEcat-file into SubModel …",
                        help: "Import BMEcat data into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "BMEcat file with data."))
                    .AddWpfBlazor(name: "SubmodelTDImport", header: "Import Thing Description JSON LD document into SubModel …",
                        help: "Import Thing Description (TD) file in JSON LD format into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON LD file with TD data."))
					.AddWpfBlazor(name: "SammAspectImport", header: "Import SAMM aspect into ConceptDescriptions …",
						help: "Import SAMM (Semantic Aspect Meta Model) aspect data into dedicated ConceptDescriptions.",
						args: new AasxMenuListOfArgDefs()
							.Add("File", "SAMM file (*.ttl, ..) with aspect model."))
					.AddWpfBlazor(name: "CSVImport", header: "Import CSV-file into SubModel …",
                        help: "Import comma separated values (CSV) into an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "CSV file with data."))
                    .AddWpfBlazor(name: "OPCUAi4aasImport", header: "Import AAS from i4aas-nodeset (deprecated) …",
                        help: "Imports an well-formatted OPC UA nodeset and converts it to an AAS following the meta model " +
                        "V2.0 mapping of AAS to OPC UA.")
                    .AddWpfBlazor(name: "OpcUaImportNodeSet", header: "Import OPC UA nodeset.xml as Submodel (deprecated) …",
                        help: "Import OPC UA nodeset.xml into an existing Submodel as a list of Properties. " +
                        "Note: Non-working, was V2.0 meta model.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset file."))
                    .AddWpfBlazor(name: "OPCRead", header: "Read OPC values into SubModel …",
                        help: "Use Qualifiers attributed in a Submodel to read actual OPC UA values.")
                    .AddWpfBlazor(name: "RDFRead", header: "Import BAMM RDF into AASX …",
                        help: "Import BAMM RDF into AASX.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "BAMM file with RDF data.")))
                .AddMenu(header: "Export …", attachPoint: "Export", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "ExportAML", header: "Export AutomationML …",
                        help: "Export AML file with AAS entities from AAS environment.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "AML file with AAS entities data.")
                            .Add("Location", "Location selection", hidden: true)
                            .Add("FilterIndex", "Set FilterIndex=2 for compact AML format."))
                    .AddWpfBlazor(name: "SubmodelWrite", header: "Export Submodel to JSON …",
                        help: "Write Submodel to JSON.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON file to write Submodel data to.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "SubmodelPut", header: "PUT Submodel to URL …",
                        help: "Put Submodel to REST server.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("URL", "URL to put Submodel data to."))
                    .AddWpfBlazor(name: "ExportCst", header: "Export to TeamCenter CST …",
                        help: "Export data to SIEMENS TeamCenter containing list of properties.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Head-part of filenames to write data to.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "ExportJsonSchema", header: "Export JSON schema for Submodel Templates …",
                        help: "Export data in JSON schema format to describe AAS Submodel Templates.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON schema file to write data to.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "OPCUAi4aasExport", header: "Export AAS as i4aas-nodeset …",
                        help: "Export OPC UA Nodeset2.xml format as i4aas-nodeset.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "OPC UA Nodeset2.xml file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "CopyClipboardElementJson",
                        header: "Copy selected element JSON to clipboard", 
                        help: "Serializes element as JSON to copies it to the system clip board.",
                        inputGesture: "Shift+Ctrl+C")
                    .AddWpfBlazor(name: "ExportGenericForms",
                        header: "Export Submodel as options for GenericForms …",
                        help: "With GenericForms, users can fill out Submodels in a form-like manner. " +
                              "Take a Submodel as template definition and export a JSON, which can be placed " +
                              "into the plug-ins folder to enable this function.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Export file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "ExportPredefineConcepts",
                        header: "Export Submodel as snippet for PredefinedConcepts …",
                        help: "Takes a Submodel a exports a text file with various C# snippets to have Submodel definitions " +
                              "available in sourc code.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Text file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpfBlazor(name: "SubmodelTDExport", header: "Export Submodel as Thing Description JSON LD document",
                        help: "Export Thing Description (TD) file in JSON LD format from an existing Submodel.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "JSON LD file with TD data.")
                            .Add("Location", "Location selection", hidden: true))
					.AddWpfBlazor(name: "SammAspectExport", header: "Export SAMM aspect model by selected CD",
						help: "Export SAMM aspect model in Turtle (.ttl) format from an selected ConceptDescription.",
						args: new AasxMenuListOfArgDefs()
							.Add("File", "Turtle file with SAMM data.")
							.Add("Location", "Location selection", hidden: true))
					.AddWpfBlazor(name: "PrintAsset", header: "Print Asset as code sheet …",
                        help: "Prints a sheet with 2D codes for the selected asset.")
                    .AddWpfBlazor(name: "ExportSMD", header: "Export TeDZ Simulation Model Description (SMD) …",
                        help: "Export TeDZ Simulation Model Description (SMD).",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Machine", "Designation of the machine/ equipment.")
                            .Add("Model", "Model type, either 'Physical' or 'Signal'.")))
                .AddSeparator(filter: AasxMenuFilter.NotBlazor)
                .AddMenu(header: "Server …", filter: AasxMenuFilter.NotBlazor,
                         attachPoint: "Server", childs: (new AasxMenu())
                    .AddWpf(name: "ServerRest", header: "Serve AAS as REST (deprecated) …", 
                         help: "Start an internal web server a server loaded AAS contents as REST Repository.")
                    .AddWpf(name: "MQTTPub", header: "Publish AAS via MQTT …",
                         help: "Connect to a MQTT broker and publish one-time and changes via MQTT topic structure.")
                    .AddSeparator()
                    .AddWpf(name: "ServerPluginEmptySample", header: "Plugin: Empty Sample …",
                         help: "Start empty plugin-in (as development example).")
                    .AddWpf(name: "ServerPluginMQTT", header: "Plugin: MQTT …",
                         help: "Connect to a MQTT broker and publish one-time and changes via MQTT topic structure."))
                .AddSeparator(filter: AasxMenuFilter.NotBlazor)
                .AddMenu(header: "System …", filter: AasxMenuFilter.NotBlazor,
                         attachPoint: "System", childs: (new AasxMenu())
                    .AddWpf(name: "AttachFileAssoc", header: "Attach .aasx file associations",
                        help: "Generate a short registry editor file for Windows to attach the " +
                              "AASX Package explorer to file association .aasx.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Windows RegEdit file to write.")
                            .Add("Location", "Location selection", hidden: true))
                    .AddWpf(name: "RemoveFileAssoc", header: "Remove .aasx file associations",
                            help: "Generate a short registry editor file for Windows to remove above registrations.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("File", "Windows RegEdit file to write.")
                            .Add("Location", "Location selection", hidden: true)))
                .AddWpfBlazor(name: "Exit", header: "_Exit", inputGesture: "Alt+F4"));

            //
            // Workspace
            //

            menu.AddMenu(header: "Workspace",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "EditMenu", header: "_Edit", inputGesture: "Ctrl+E",
                    onlyDisplay: true, isCheckable: true,
                    help: "Allows all attributes of the meta model to be edited, added, deleted.",
                    args: new AasxMenuListOfArgDefs()
                            .Add("Mode", "'True' to activate edit mode, 'False' to turn off."))
                .AddWpfBlazor(name: "HintsMenu", header: "_Hints", inputGesture: "Ctrl+H",
                    onlyDisplay: true, isCheckable: true, isChecked: true,
                    help: "Checks all attributes on helpful hints and violation of design rules of the AAS.",
                    args: new AasxMenuListOfArgDefs()
                        .Add("Mode", "'True' to activate hints mode, 'False' to turn off."))
                .AddWpfBlazor(name: "Test", header: "Test")
                .AddSeparator(filter: AasxMenuFilter.Wpf)
                .AddWpf(name: "ToolsFindText", header: "Find …", inputGesture: "Ctrl+F",
                    help: "Find individual text occurences in the attributes of the AAS elements.",
                    args: new AasxMenuListOfArgDefs()
                        .AddFromReflection(new AasxSearchUtil.SearchOptions()))
                .AddWpf(name: "ToolsReplaceText", header: "Replace …", inputGesture: "Ctrl+Shift+H",
                    help: "Find and replace individual text occurences in the attributes of the AAS elements.",
                    args: new AasxMenuListOfArgDefs()
                        .AddFromReflection(new AasxSearchUtil.SearchOptions())
                        .Add("Do", "Either do 'stay', 'forward' or 'all'."))
                .AddWpf(name: "ToolsFindForward", header: "Find Forward", inputGesture: "F3", hidden: true,
                    help: "Advance to next find position in the attributes of the AAS elements.")
                .AddWpf(name: "ToolsFindBackward", header: "Find Backward", inputGesture: "Shift+F3", hidden: true,
                    help: "Go back to previous find position in the attributes of the AAS elements.")
                .AddWpf(name: "ToolsReplaceStay", header: "Replace and stay", hidden: true,
                    help: "Replace the text in the currently found attibute of an AAS element and stay. Do not advance.")
                .AddWpf(name: "ToolsReplaceForward", header: "Replace and stay", hidden: true,
                    help: "Replace the text in the currently found attibute of an AAS element and advance to to next " +
                          "find position in the attributes of the AAS elements.")
                .AddWpf(name: "ToolsReplaceAll", header: "Replace all", hidden: true,
                    help: "Replace the text in all find position in the attributes of the AAS elements.")
                .AddSeparator()
                .AddMenu(header: "Navigation …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "NavigateBack", header: "Back", inputGesture: "Ctrl+Shift+Left",
                        help: "Go back to the last active AAS element before taking e.g. a jump command.")
                    .AddWpfBlazor(name: "NavigateHome", header: "Home", inputGesture: "Ctrl+Shift+Home",
                        help: "Go to the element of the AAS elements, which is identified (by extension) to be the home position."))
                .AddSeparator()
                .AddMenu(header: "Editing locations …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "LocationPush", header: "Push location", inputGesture: "Ctrl+Shift+P",
                        help: "Store the current AAS element position in the stack of editing locations.")
                    .AddWpfBlazor(name: "LocationPop", header: "Pop location", inputGesture: "Ctrl+Shift+O",
                        help: "Remove the latest position from the stack of editing locations and go there."))
                .AddSeparator()
                .AddMenu(header: "Create …", attachPoint: "Create", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "NewSubmodelFromPlugin", header: "New Submodel from plugin", inputGesture: "Ctrl+Shift+M",
                            help: "Creates a new Submodel based on defintions provided by plugin.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("Name", "Name of the Submodel (partially)")
                                .Add("Record", "Record data", hidden: true)
                                .Add("SmRef", "Return: Submodel generated", hidden: true))
                    .AddWpfBlazor(name: "NewSubmodelFromKnown", header: "New Submodel from pool of known",
                            help: "Creates a new Submodel based on defintions provided by a pool of known definitions.",
                            args: new AasxMenuListOfArgDefs()
                                .Add("Domain", "Domain of knowledge/ name of the Submodel (partially)")
                                .Add("SmRef", "Return: Submodel generated", hidden: true))
					.AddWpfBlazor(name: "MissingCdsFromKnown", header: "Missing ConceptDescriptions from pool of known",
							help: "For the selected element: checks which SME refer to missing " +
                                  "ConceptDescriptions, which can be created from pool of known definitions.")
                    .AddSeparator()
                    .AddWpfBlazor(name: "SmtExtensionFromQualifiers", header: "SMT extensions from single SMT qualifiers",
                            help: "Converts particular SMT qualifiers to SMT extension for selected element.")
                    .AddWpfBlazor(name: "SmtOrganizesFromSubmodel", header: "SMT organizes from Submodel",
                            help: "Take over Submodel's element relationships to associated concepts.")
                    .AddWpfBlazor(name: "SubmodelInstanceFromSammAspect", 
                        header: "New Submodel instance from selected SAMM aspect",
						help: "Creates a new Submodel instance from an selected ConceptDescription with a SAMM Aspect element.")
                    .AddWpfBlazor(name: "SubmodelInstanceFromSmtConcepts",
                        header: "New Submodel from SMT/ SAMM ConceptDescription",
                        help: "Creates a new Submodel instance from an selected root given by accessible ConceptDescriptions.")
                    )
                .AddMenu(header: "Change …", attachPoint: "Change")
                .AddMenu(header: "Visualize …", attachPoint: "Visualize")
                .AddMenu(header: "Plugins …", attachPoint: "Plugins")
                .AddSeparator()
                .AddWpfBlazor(name: "ConvertElement", header: "Convert via plugin …",
                        help: "Asks plugins if these could make offers to convert the current elements and " +
                            "subsequently converts the element.",
                        args: new AasxMenuListOfArgDefs()
                            .Add("Name", "Name of the potential offer (partially)")
                            .Add("Record", "Record data", hidden: true))
                .AddSeparator()
                .AddMenu(header: "Buffer …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "BufferClear", header: "Clear internal paste buffer",
                        help: "Clear internal paste buffer."))
                .AddMenu(header: "Log …", childs: (new AasxMenu())
                    .AddWpfBlazor(name: "StatusClear", header: "Clear status line and errors",
                        help: "Clear status line and error indications. Log line will remain untouched.")
                    .AddWpfBlazor(name: "LogShow", header: "Show log",
                        help: "Show an information panel with all log lines since start of appilcation."))
                .AddSeparator(filter: AasxMenuFilter.NotBlazor)
                .AddMenu(header: "Events …", childs: (new AasxMenu())
                    .AddWpf(name: "EventsShowLogMenu", header: "_Event log", inputGesture: "Ctrl+L",
                        onlyDisplay: true, isCheckable: true,
                        help: "Shows an information panel for received and emitted AAS events in the user interface.")
                    .AddWpf(name: "EventsResetLocks", header: "Reset interlocking"))
                .AddMenu(header: "Scripts …", filter: AasxMenuFilter.WpfBlazor, childs: (new AasxMenu())
                    .AddWpfBlazor(name: "ScriptEditLaunch", header: "Edit & launch …", inputGesture: "Ctrl+Shift+L",
                        help: "Show an panel to editor a script of AAS enabled C# commands and allow to execute " +
                              "this script or presets.")));

            //
            // Options
            //

            menu.AddMenu(header: "Option",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "ShowIriMenu", header: "Show id as IRI", inputGesture: "Ctrl+I", isCheckable: true,
                    help: "Format identifiers to be active IRIs/ URIs instead of plain text.")
                .AddWpfBlazor(name: "VerboseConnect", header: "Verbose connect", isCheckable: true,
                    help: "Enable more verbose connect dialog (deprecated).")
                .AddWpfBlazor(name: "FileRepoLoadWoPrompt", header: "Load without prompt", isCheckable: true,
                    help: "Load new AASX files in Repositories without prompting for allowance.")
                .AddWpfBlazor(name: "AnimateElements", header: "Animate elements", isCheckable: true,
                    help: "Show updates and updated values when element values are changed by incoming events.")
                .AddWpfBlazor(name: "ObserveEvents", header: "ObserveEvents", isCheckable: true,
                    help: "Activate observation of observable elements in order to generate and emit change events.")
                .AddWpfBlazor(name: "CompressEvents", header: "Compress events", isCheckable: true,
                    help: "Compress multiple value changes (e.g. character by character) to one event.")
				.AddWpfBlazor(name: "CheckSmtElements", header: "Check SMT elements (slow!)", isCheckable: true,
                    help: "Check for semanticIds for being known SMT elements and show further options."));

            //
            // Help
            //

            menu.AddMenu(header: "Help",
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "About", header: "About …",
                    help: "Show an about panel, if available.")
                .AddWpfBlazor(name: "HelpGithub", header: "Help on Github …",
                    help: "Show Github root page.")
                .AddWpfBlazor(name: "FaqGithub", header: "FAQ on Github …",
                    help: "Show faq entries on Github page.")
                .AddWpfBlazor(name: "HelpIssues", header: "Issues on Github …",
                    help: "Show issues index on Github page.")
                .AddWpfBlazor(name: "HelpOptionsInfo", header: "Available options …",
                    help: "Show an information panel with all available menu commands, key commands and script commands for the " +
                          "current AAS element."));

            //
            // Hotkeys
            //

            menu.AddHotkey(name: "EditKey", gesture: "Ctrl+E")
                .AddHotkey(name: "HintsKey", gesture: "Ctrl+H")
                .AddHotkey(name: "ShowIriKey", gesture: "Ctrl+I")
                .AddHotkey(name: "EventsShowLogKey", gesture: "Ctrl+L");

            for (int i = 0; i < 9; i++)
                menu.AddHotkey(name: $"LaunchScript{i}", gesture: $"Ctrl+Shift+{i}");

            //
            // invisible commands
            //

            menu.AddMenu(header: "", hidden: true,
                childs: (new AasxMenu())
                .AddWpfBlazor(name: "WinMaximize", header: "", hidden: true));

            //
            // Try attach plugins
            //

            foreach (var mi in menu.FindAll<AasxMenuItem>((test) => test.AttachPoint?.HasContent() == true))
            {
                // this is worth a search in the plugins
                foreach (var pi in Plugins.LoadedPlugins.Values)
                {
                    // menu items?
                    if (pi.MenuItems == null)
                        continue;

                    // search here
                    foreach (var pmi in pi.MenuItems)
                        if (pmi.AttachPoint.Equals(mi.AttachPoint,
                            System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            // double the data
                            var newMi = pmi.MenuItem.Copy();

                            // say, that it goes to a plugin
                            newMi.PluginToAction = pi.name;

                            // yes! can attach!
                            mi.Add(newMi);
                        }
                }
            }

            //
            // End
            //

            return menu;
        }
    }
}
