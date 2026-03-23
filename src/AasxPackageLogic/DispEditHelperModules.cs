/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Samm2_2_0;
using AasxAmlImExport;
using AasxCompatibilityModels;
using AasxIntegrationBase;
using AdminShellNS;
using AdminShellNS.Extensions;
using AnyUi;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xaml;
using VDS.RDF.Parsing;
using VDS.RDF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Aas = AasCore.Aas3_1;
using Samm = AasCore.Samm2_2_0;
using System.Text.RegularExpressions;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Text;
using AasxPackageLogic.PackageCentral;
using System.Threading.Tasks;
using static AasxPackageLogic.PackageCentral.PackageContainerHttpRepoSubset;
using System.Security.Cryptography;
using System.Collections;

namespace AasxPackageLogic
{
    /// <summary>
    /// This class extends the basic helper functionalities of DispEditHelper by providing modules for display/
    /// editing disting modules of the GUI, such as the different (re-usable) Interfaces of the AAS entities
    /// </summary>
    public class DispEditHelperModules : DispEditHelperExtensions
    {
        //
        // Members
        //

        public class UploadAssistance
        {
            public string SourcePath = "";
            public string TargetPath = "/aasx/files";
        }
        public UploadAssistance uploadAssistance = new UploadAssistance();

        //
        // Inject a number of customised function in modules
        //

        public class DispEditInjectAction
        {
            public string[] auxTitles = null;
            public string[] auxToolTips = null;
            public Func<int, AnyUiLambdaActionBase> auxLambda = null;
            public Func<int, Task<AnyUiLambdaActionBase>> auxLambdaAsync = null;

            public DispEditInjectAction() { }

            public DispEditInjectAction(string[] auxTitles, 
                Func<int, AnyUiLambdaActionBase> auxLambda,
                Func<int, Task<AnyUiLambdaActionBase>> auxLambdaAsync)
            {
                this.auxTitles = auxTitles;
                this.auxLambda = auxLambda;
                this.auxLambdaAsync = auxLambdaAsync;
            }

            public DispEditInjectAction(string[] auxTitles, string[] auxToolTips,
                Func<int, AnyUiLambdaActionBase> auxActions)
            {
                this.auxTitles = auxTitles;
                this.auxToolTips = auxToolTips;
                this.auxLambda = auxActions;
            }

            public static string[] GetTitles(string[] fixTitles, DispEditInjectAction action)
            {
                var res = new List<string>();
                if (fixTitles != null)
                    res.AddRange(fixTitles);
                if (action?.auxTitles != null)
                    res.AddRange(action.auxTitles);
                if (res.Count < 1)
                    return null;
                return res.ToArray();
            }

            public static string[] GetToolTips(string[] fixToolTips, DispEditInjectAction action)
            {
                var res = new List<string>();
                if (fixToolTips != null)
                    res.AddRange(fixToolTips);
                if (action?.auxToolTips != null)
                    res.AddRange(action.auxToolTips);
                if (res.Count < 1)
                    return null;
                return res.ToArray();
            }
        }

        //
        // IReferable
        //

        public void DisplayOrEditEntityReferable(
			Aas.IEnvironment env, AnyUiStackPanel stack,
            Aas.IReferable parentContainer,
            Aas.IReferable referable,
            int indexPosition,
            DispEditInjectAction injectToIdShort = null,
            bool hideExtensions = false,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null || referable == null)
                return;

            // members
            this.AddGroup(stack, "Referable:", levelColors.SubSection);

            // special case SML ..
            if (parentContainer?.IsIndexed() == true)
            {
                AddKeyValue(stack, "index", $"#{indexPosition:D2}", repo: null);
            }

            // for clarity, have two kind of hints for SML and for other
            var isIndexed = parentContainer.IsIndexed() == true;
            if (!isIndexed)
            {
                // not SML
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck( () => !(referable is Aas.IIdentifiable) && !referable.IdShort.HasContent(),
                        "The idShort is mandatory for all Referables which are not Identifiable. " +
                        "It is a short, unique identifier that is unique just in its context, " +
                        "its name space. ", breakIfTrue: true),
                    new HintCheck(
                        () => {
                            if (referable.IdShort == null) return false;
                            return !Verification.MatchesIdShort(referable.IdShort);
                            //return !AdminShellUtil.ComplyIdShort(referable.IdShort);
                        },
                        "The idShort shall only feature letters, digits, underscore ('_'), hyphen ('-'); " +
                        "starting mandatory with a letter."),
                    new HintCheck(
                        () => {
                            return true == referable.IdShort?.Contains("---");
                        },
                        "The idShort contains 3 dashes. Probably, the entitiy was auto-named " +
                        "to keep it unique because of an operation such a copy/ paste.",
                        severityLevel: HintCheck.Severity.High)
                    });
            }
            else
            {
                // SML ..
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck( () => referable.IdShort.HasContent(),
                        "Constraint AASd-120: idShort of SubmodelElements being a direct child of a " +
                        "SubmodelElementList shall not be specified.")
                    });
            }
            AddKeyValueExRef(
                stack, "idShort", referable, referable.IdShort, null, repo,
                v =>
                {
                    referable.IdShort = v as string;
                    this.AddDiaryEntry(referable, new DiaryEntryStructChange(), new DiaryReference(referable));
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: DispEditInjectAction.GetTitles(new[] { "Fix" } , injectToIdShort),
                auxButtonToolTips: DispEditInjectAction.GetToolTips(
                    new[] { "Fix characters of idShort to be in the allowed ranges." }, 
                    injectToIdShort),
                auxButtonLambda: (i) =>
                {
                    if (i == 0)
                    {
                        referable.IdShort = AdminShellUtil.FilterFriendlyName(referable.IdShort, 
                            pascalCase: true, fixMoreBlanks: true);

                        if (!referable.IdShort.HasContent())
                            referable.IdShort = AdminShellUtil.GiveRandomIdShort(referable);

                        this.AddDiaryEntry(referable, new DiaryEntryStructChange(), new DiaryReference(referable));
                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: referable);
                    }
                    else
                        return injectToIdShort?.auxLambda(i-1);
                },
                takeOverLambdaAction: new AnyUiLambdaActionRedrawAllElements(nextFocus: referable)
                );

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => referable.DisplayName != null && referable.DisplayName.IsValid() != true,
                        "According to the specification, an existing list of elements shall contain " +
                        "at least one element and for each element all mandatory fields shall be " +
                        "not empty."),
                    new HintCheck(
                        () => referable.DisplayName?.IsValid() != true,
                        "The use of a display name is recommended to express a human readable name " +
                        "for the Referable in multiple languages.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return referable.DisplayName.Count < 2; },
                        "Consider having Display name in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.DisplayName, "displayName:", "Create w/ default!", v =>
            {
                referable.DisplayName = ExtendILangStringNameType.CreateFrom(
                    lang: AdminShellUtil.GetDefaultLngIso639(), text: "");
                this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddKeyListLangStr<ILangStringNameType>(
                    stack, "displayName", referable.DisplayName,
                    repo, relatedReferable: referable,
                    setNullList: () => referable.DisplayName = null);
            }

            // category deprecated
            this.AddHintBubble(
                stack, hintMode,
                new HintCheck(() => referable.Category?.HasContent() == true,
                "The use of category is deprecated, hence the field is ReadOnly. Do not plan to use this information in new developments.",
                severityLevel: HintCheck.Severity.Notice));

            if (referable.Category?.HasContent() == true)
            {
                AddKeyValueExRef(
                        stack, "category", referable, referable.Category, null, repo,
                        v =>
                        {
                            referable.Category = v as string;
                            this.AddDiaryEntry(referable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionNone();
                        }, isValueReadOnly: true);
                //},
                //comboBoxItems: new string[] { "CONSTANT", "PARAMETER", "VARIABLE" }, comboBoxIsEditable: true); 
            }

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => referable.Description != null && referable.Description.IsValid() != true,
                        "According to the specification, an existing list of elements shall contain " +
                        "at least one element and for each element all mandatory fields shall be " +
                        "not empty."),
                    new HintCheck(
                        () => {
                            return referable.Description == null || referable.Description == null ||
                                referable.Description.Count < 1;
                        },
                        "The use of an description is recommended to allow the consumer of an Referable " +
                            "to understand the nature of it.",
                        breakIfTrue: true,
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return referable.Description.Count < 2; },
                        "Consider having description in multiple langauges.",
                        severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(stack, repo, referable.Description, "description:", "Create w/ default!", v =>
            {
                referable.Description = ExtendILangStringTextType.CreateFrom(
                    lang: AdminShellUtil.GetDefaultLngIso639(), text: "");
                return new AnyUiLambdaActionRedrawEntity();
            }))
            {
                this.AddHintBubble(
                    stack, hintMode,
                    new HintCheck(
                        () =>
                        {
                            return referable.Description == null
                            || referable.Description.Count < 1;
                        },
                        "Please add some descriptions in your main languages here to help consumers " +
                            "of your Administration shell to understand your intentions.",
                        severityLevel: HintCheck.Severity.Notice));
                this.AddKeyListLangStr<ILangStringTextType>(
                    stack, "description", referable.Description,
                    repo, relatedReferable: referable,
                    setNullList: () => referable.Description = null);
            }

            if (!hideExtensions)
            {
				// before extension, some helpful records
				DisplayOrEditEntityExtensionRecords(
					env, stack, referable.Extensions,
					(v) => { referable.Extensions = v; },
					relatedReferable: referable);

				// Extensions (at the end to make them not so much impressive!)
				DisplayOrEditEntityListOfExtension(
                    stack: stack, extensions: referable.Extensions,
                    setOutput: (v) => { referable.Extensions = v; },
                    relatedReferable: referable, superMenu: superMenu);
            }
        }

        public void DisplayOrEditEntityReferableContinue(
            Aas.IEnvironment env, AnyUiStackPanel stack,
            Aas.IReferable parentContainer,
            Aas.IReferable referable,
            int indexPosition,
            DispEditInjectAction injectToIdShort = null,
            bool hideExtensions = false,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null || referable == null)
                return;

            // members
            this.AddGroup(stack, "Referable (continue):", levelColors.SubSection);

			// before extension, some helpful records
			DisplayOrEditEntityExtensionRecords(
				env, stack, referable.Extensions,
				(v) => { referable.Extensions = v; },
				relatedReferable: referable);

			// Extensions (at the end to make them not so much impressive!)
			DisplayOrEditEntityListOfExtension(
				stack: stack, extensions: referable.Extensions,
				setOutput: (v) => { referable.Extensions = v; },
				relatedReferable: referable, superMenu: superMenu);
		}

        public void DisplayOrEditEntitySideInfo(
            Aas.IEnvironment env, AnyUiStackPanel stack,
            Aas.IReferable referable,
            AasIdentifiableSideInfo si,
            string key,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null || si == null)
                return;

            this.AddGroup(stack, $"{key} is provided by an Endpoint of dynamic fetch environment",
                    this.levelColors.SubSection);

            AddKeyValue(stack, "StubLevel", "" + si.StubLevel.ToString(), repo: null);
            AddKeyValue(stack, "IdShort", "" + si.IdShort, repo: null);
            AddKeyValue(stack, "Id", "" + si.Id, repo: null);

            AddKeyValue(stack, "Id endpoint", "" + si.Id, repo: null,
                auxButtonTitle: "Copy",
                auxButtonLambda: (i) => {
                    this.context?.ClipboardSet(new AnyUiClipboardData(
                        text: si.Id)
                        { });
                    Log.Singleton.Info(StoredPrint.Color.Blue, "Id copied to clipboard.");
                    return new AnyUiLambdaActionNone();
                },
                auxButtonOverride: true);

            AddKeyValue(stack, "Queried endpoint", "" + si.QueriedEndpoint?.ToString(), repo: null,
                auxButtonTitle: "Copy",
                auxButtonLambda: (i) => {
                    if (si.QueriedEndpoint == null)
                    {
                        Log.Singleton.Error("No endpoint data available");
                    }
                    else
                    {
                        this.context?.ClipboardSet(new AnyUiClipboardData(
                            text: si.QueriedEndpoint.ToString())
                            { });
                        Log.Singleton.Info(StoredPrint.Color.Blue, "Queried endpoint copied to clipboard.");
                    }
                    return new AnyUiLambdaActionNone();
                },
                auxButtonOverride: true);

            AddKeyValue(stack, "Designated endpoint", "" + si.DesignatedEndpoint?.ToString(), repo: null,
                auxButtonTitle: "Copy",
                auxButtonLambda: (i) => {
                    if (si.DesignatedEndpoint == null)
                    {
                        Log.Singleton.Error("No endpoint data available");
                    }
                    else
                    {
                        this.context?.ClipboardSet(new AnyUiClipboardData(
                            text: si.DesignatedEndpoint.ToString())
                            { });
                        Log.Singleton.Info(StoredPrint.Color.Blue, "Designated endpoint copied to clipboard.");
                    }
                    return new AnyUiLambdaActionNone();
                },
                auxButtonOverride: true);
        }
        
        public void DisplayOrEditEntityMissingSideInfo(
            AnyUiStackPanel stack, 
            string key)
        {
            // access
            if (key == null)
                return;

            this.AddGroup(stack, $"{key} could be provided by an Endpoint of a dynamic fetch environment",
                    this.levelColors.SubSection);

            AddInfoText(stack, $"However, the {key} data is not present! Reasons could be a wrong or missing"
                + $" Reference or a missing access information.");
        }

        //
        // Extensions
        //

        public void DisplayOrEditEntityListOfExtension(AnyUiStackPanel stack,
            List<Aas.IExtension> extensions,
            Action<List<Aas.IExtension>> setOutput,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasExtension:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, extensions, "extensions:", "Create w/ default!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.IExtension>(new[] { new Aas.Extension("") }));
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.ExtensionHelper(
                    stack, repo, 
                    extensions, 
                    setOutput, 
                    relatedReferable: relatedReferable, superMenu: superMenu);
            }

        }

        //
        // Identifiable
        //

        public void DisplayOrEditEntityIdentifiable(AnyUiStackPanel stack,
            AdminShellPackageEnvBase packageEnv,
            Aas.IEnvironment env,
            Aas.IIdentifiable identifiable,
            string templateForIdString,
            DispEditInjectAction injectToId = null)
        {
            // access
            if (stack == null || identifiable == null)
                return;

            // special flags
            var isDynEnv = packageEnv is AdminShellPackageDynamicFetchEnv;
            var idReadOnly = isDynEnv && identifiable.Id?.HasContent() == true;

            // members
            this.AddGroup(stack, "Identifiable:", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.Id == null; },
                    "Providing a worldwide unique identification is mandatory.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return identifiable.Id == ""; },
                    "Identification id shall not be empty. You could use the 'Generate' button in order to " +
                        "generate a worldwide unique id. " +
                        "The template of this id could be set by commandline arguments." ),
                new HintCheck(
                    () => {
                        int count = 0;
                        foreach(var aas in env.AllAssetAdministrationShells())
                        {
                            if(aas.Id == identifiable.Id)
                                count++;
                        }
                        return (count >= 2?true:false);
                    },
                    "It is not allowed to have duplicate Ids in AAS of the same file. This will break functionality and we strongly encoure to make the Id unique!",
                    breakIfTrue: false)
            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.Id, "id:", "Create data element!",
                    v =>
                    {
                        identifiable.Id = "";
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                AddKeyValueExRef(
                    stack, "id", identifiable, identifiable.Id, null, 
                    (idReadOnly) ? null : repo,
                    v =>
                    {
                        var dr = new DiaryReference(identifiable);
                        string value = v as string;
                        identifiable.Id = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange(), diaryReference: dr);
                        return new AnyUiLambdaActionNone();
                    },
                    takeOverLambdaAction: new AnyUiLambdaActionRedrawAllElements(nextFocus: identifiable),
                    auxButtonOverride: true,
                    auxButtonTitles: DispEditInjectAction.GetTitles(new[] { "Generate" }, injectToId),
                    auxButtonLambdaAsync: async (i) =>
                    {
                        if (i == 0)
                        {
                            var dr = new DiaryReference(identifiable);
                            identifiable.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                templateForIdString);
                            this.AddDiaryEntry(identifiable, new DiaryEntryStructChange(), diaryReference: dr);
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: identifiable);
                        }
                        if (i >= 1)
                        {
                            AnyUiLambdaActionBase la = null;
                            if (injectToId?.auxLambda != null)
                                la = injectToId.auxLambda?.Invoke(i - 1);
                            if (injectToId?.auxLambdaAsync != null)
                                la = await injectToId.auxLambdaAsync?.Invoke(i - 1);
                            return la;
                        }
                        return new AnyUiLambdaActionNone();
                    });

                // further info?
                if (identifiable.Id.HasContent())
                {
                    this.AddKeyValue(
                        stack, "id (base64url)", AdminShellUtil.Base64UrlEncode(identifiable.Id),
                        repo: null);
                }

            }

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return identifiable.Administration == null; },
                    "Check if providing admistrative information on version/ revision would be useful. " +
                        "This allows for better life-cycle management.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () =>
                    {
                        return identifiable.Administration.Version?.HasContent() != true ||
                            identifiable.Administration.Revision?.HasContent() != true;
                    },
                    "Admistrative information fields should not be empty.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (this.SafeguardAccess(
                    stack, repo, identifiable.Administration, "administration:", "Create data element!",
                    v =>
                    {
                        identifiable.Administration = new Aas.AdministrativeInformation();
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                // Allow administrative information to be deleted again
                this.AddGroup(stack, "administration:", levelColors.SubSection,
                    requestAuxButton: repo != null,
                    auxContextHeader: new[] { "\u2702", "Delete" },
                    auxContextLambda: (o) =>
                    {
                        if (o is int i && i == 0)
                        {
                            identifiable.Administration = null;
                            this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueExRef(
                    stack, "version", identifiable.Administration, identifiable.Administration.Version,
                    null, repo,
                    v =>
                    {
                        identifiable.Administration.Version = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                AddKeyValueExRef(
                    stack, "revision", identifiable.Administration, identifiable.Administration.Revision,
                    null, repo,
                    v =>
                    {
                        identifiable.Administration.Revision = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });

                if (this.SafeguardAccess(
                    stack, repo, identifiable.Administration.Creator, "creator:", "Create data element!",
                    v =>
                    {
                        identifiable.Administration.Creator =
                            new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                {
                    this.AddKeyReference(
                        stack, "creator", 
                        identifiable.Administration.Creator, () => identifiable.Administration.Creator = null,
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", // no restriction
                        relatedReferable: identifiable,
                        showRefSemId: false,
                        auxContextHeader: new[] { "\u2702", "Delete" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                identifiable.Administration.Creator = null;
                                this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            };
                            return new AnyUiLambdaActionNone();
                        },
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }

                AddKeyValueExRef(
                    stack, "templateId", identifiable.Administration, identifiable.Administration.TemplateId,
                    null, repo,
                    v =>
                    {
                        identifiable.Administration.TemplateId = v as string;
                        this.AddDiaryEntry(identifiable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
            }
        }

        //Added this method only to support embeddedDS from ConceptDescriptions
        public void DisplayOrEditEntityHasDataSpecificationReferences(AnyUiStackPanel stack,
            List<Aas.IEmbeddedDataSpecification>? hasDataSpecification,
            Action<List<Aas.IEmbeddedDataSpecification>> setOutput,
            string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
            bool dataSpecRefsAreUsual = false,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (Reference):", levelColors.SubSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => dataSpecRefsAreUsual && (hasDataSpecification == null
                        ||  hasDataSpecification.Count < 1),
                    "Check if a data specification is appropriate here. " +
                    "A ConceptDescription typically goes along with a data specification, e.g. " +
                    "according IEC61360.",
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () => { return !dataSpecRefsAreUsual && hasDataSpecification != null
                        && hasDataSpecification.Count > 0; },
                    "Check if a data specification is appropriate here.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice) });
            if (this.SafeguardAccess(
                    stack, this.repo, hasDataSpecification, "DataSpecification:", "Create w/ default!",
                    v =>
                    {
                        setOutput?.Invoke(new List<Aas.IEmbeddedDataSpecification>(new[] { 
                            new Aas.EmbeddedDataSpecification(
                                dataSpecification: Options.Curr.GetDefaultEmptyReference(),
                                dataSpecificationContent: new DataSpecificationBlank()) 
                        }));
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, "Specifications:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-reference", "Add Reference",
                                "Adds a reference to a data specification.")
                            .AddAction("add-preset", "Add Preset",
                                "Adds a reference to a data specification given by preset file.")
                            .AddAction("delete-reference", "Delete last reference",
                                "Deletes the last reference in the list."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                                hasDataSpecification.Add(
                                    new Aas.EmbeddedDataSpecification(
                                        dataSpecification: Options.Curr.GetDefaultEmptyReference(),
                                        dataSpecificationContent: new DataSpecificationBlank()));

                            if (buttonNdx == 1)
                            {
                                var pfn = Options.Curr.DataSpecPresetFile;
                                if (pfn == null || !System.IO.File.Exists(pfn))
                                {
                                    Log.Singleton.Error(
                                        $"JSON file for data specifcation presets not defined nor existing ({pfn}).");
                                    return new AnyUiLambdaActionNone();
                                }
                                try
                                {
                                    // read file contents
                                    var init = System.IO.File.ReadAllText(pfn);
                                    var presets = JsonConvert.DeserializeObject<List<DataSpecPreset>>(init);

                                    // define dialogue and map presets into dialogue items
                                    var uc = new AnyUiDialogueDataSelectFromList();
                                    uc.ListOfItems = new AnyUiDialogueListItemList(presets.Select((pr)
                                            => new AnyUiDialogueListItem() { Text = pr.name, Tag = pr }));

                                    // perform dialogue
                                    this.context.StartFlyoverModal(uc);
                                    if (uc.Result && uc.ResultItem?.Tag is DataSpecPreset preset
                                        && preset.value != null)
                                    {
                                        // if hasDataSpecification is actually containing only one
                                        // "blank" reference, replace this
                                        if (hasDataSpecification.IsOneBlank())
                                            hasDataSpecification.RemoveAt(0);

                                        hasDataSpecification.Add(
                                            new Aas.EmbeddedDataSpecification(
                                                dataSpecification: new Aas.Reference(
                                                    Aas.ReferenceTypes.ExternalReference,
                                                    new Aas.IKey[] {
                                                        new Aas.Key(KeyTypes.GlobalReference, preset.value) }
                                                    .ToList()),
                                                dataSpecificationContent: new DataSpecificationBlank()));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"While show Qualifier presets ({pfn})");
                                }
                            }

                            if (buttonNdx == 2)
                            {
                                if (hasDataSpecification.Count > 0)
                                    hasDataSpecification.RemoveAt(hasDataSpecification.Count - 1);
                                if (hasDataSpecification.Count < 1)
                                    setOutput?.Invoke(null);
                            }

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (hasDataSpecification != null && hasDataSpecification.Count > 0)
                {
                    for (int i = 0; i < hasDataSpecification.Count; i++)
                    {
                        int currentI = i;
                        if (this.SafeguardAccess(
                            stack, this.repo, hasDataSpecification[i].DataSpecification,
                                "DataSpecification:", "Create (inner) data element!",
                            v =>
                            {
                                hasDataSpecification[currentI].DataSpecification =
                                    Options.Curr.GetDefaultEmptyReference();
                                if (hasDataSpecification[currentI].DataSpecificationContent == null)
                                    hasDataSpecification[currentI].DataSpecificationContent = 
                                        new DataSpecificationBlank();
                                return new AnyUiLambdaActionRedrawEntity();
                            }))
                        {
                            this.AddKeyReference(
                            stack, String.Format("dataSpec.[{0}]", i),
                            hasDataSpecification[i].DataSpecification,
                            () =>
                            {
                                if (currentI >= 0 && currentI <= hasDataSpecification.Count)
                                    hasDataSpecification.RemoveAt(currentI);
                                if (hasDataSpecification.Count < 1)
                                    setOutput?.Invoke(null);
                            },
                            repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                            addExistingEntities: null /* "All" */,
                            addPresetNames: addPresetNames, addPresetKeyLists: addPresetKeyLists,
                            relatedReferable: relatedReferable,
                            showRefSemId: false,
                            auxContextHeader: new[] { "\u2573", "Delete this dataSpec." },
                            auxContextLambda: (choice) =>
                            {
                                if (choice == 0)
                                {
                                    if (currentI >= 0 && currentI <= hasDataSpecification.Count)
                                        hasDataSpecification.RemoveAt(currentI);
                                    if (hasDataSpecification.Count < 1)
                                        setOutput?.Invoke(null);
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                                return new AnyUiLambdaActionNone();
                            });
                        }
                    }
                }
            }
        }

        public void DisplayOrEditEntityHasEmbeddedSpecification(
            Aas.IEnvironment env, AnyUiStackPanel stack,
            List<Aas.IEmbeddedDataSpecification> hasDataSpecification,
            Action<List<Aas.IEmbeddedDataSpecification>> setOutput,
            string[] addPresetNames = null, List<Aas.IKey>[] addPresetKeyLists = null,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null,
            bool suppressNoEdsWarning = false)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "HasDataSpecification (records of embedded data specification):", levelColors.MainSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return !suppressNoEdsWarning && (hasDataSpecification == null ||
                            hasDataSpecification.Count < 1); },
                        "There are no embedded data specification elements. " +
                        (hasDataSpecification == null ? "List is null! " : "List is empty! ") +
                        "For ConceptDescriptions, the main data carrier lies in the embedded data specification. " +
                        "In these elements, a Reference to a data specification is combined with content " +
                        "attributes, which are attached to the ConceptDescription. These attributes hold the " +
                        "descriptive information on a concept and thus allow for an off-line understanding of " +
                        "the meaning of a concept/ SubmodelElement. Multiple data specifications " +
                        "could be possible. The most used is the IEC61360, which is also used by ECLASS. " +
                        "Please create this data element."),
                });

            // Head control. Allow menu, even if list is null!
            if (editMode)
            {
                // let the user control the number of references
                this.AddActionPanel(
                    stack, "Spec. records:", repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("add-record", "Add record",
                            "Adds a record for data specification reference and content.")
                        .AddAction("add-iec61360", "Add IEC61360",
                            "Adds a record initialized for IEC 61360 content.")
                        .AddAction("auto-detect", "Auto detect content",
                            "Auto dectects known data specification contents and sets valid references.")
                        .AddAction("delete-last", "Delete last record",
                            "Deletes last record (data specification reference and content)."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            hasDataSpecification = hasDataSpecification ?? new List<IEmbeddedDataSpecification>();
                            hasDataSpecification.Add(
                                new Aas.EmbeddedDataSpecification(
                                    dataSpecification: Options.Curr.GetDefaultEmptyReference(),
                                    dataSpecificationContent: new DataSpecificationBlank()));
                            setOutput?.Invoke(hasDataSpecification);
                        }

                        if (buttonNdx == 1)
                        {
                            hasDataSpecification = hasDataSpecification ?? new List<IEmbeddedDataSpecification>();
                            hasDataSpecification.Add(
                                new Aas.EmbeddedDataSpecification(
                                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey> {
                                        ExtendIDataSpecificationContent.GetKeyForIec61360()
                                    }),
                                    new Aas.DataSpecificationIec61360(new List<Aas.ILangStringPreferredNameTypeIec61360>() {
                                        new Aas.LangStringPreferredNameTypeIec61360(
                                            AdminShellUtil.GetDefaultLngIso639(), "")
                                    })));
                            setOutput?.Invoke(hasDataSpecification);
                        }

                        if (buttonNdx == 2)
                        {
                            var fix = 0;
                            foreach (var eds in hasDataSpecification.ForEachSafe())
                                if (eds != null && eds.FixReferenceWrtContent())
                                    fix++;
                            Log.Singleton.Info($"Fixed {fix} records of embedded data specification.");
                        }

                        if (buttonNdx == 3)
                        {
                            if (hasDataSpecification != null && hasDataSpecification.Count > 0)
                                hasDataSpecification.RemoveAt(hasDataSpecification.Count - 1);
                            
                            if (hasDataSpecification != null && hasDataSpecification.Count < 1)
                                setOutput?.Invoke(null);
                        }

                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    });
            }

            // now use the normal mechanism to deal with editMode or not ..
            if (hasDataSpecification != null && hasDataSpecification.Count > 0)
            {
                for (int i = 0; i < hasDataSpecification.Count; i++)
                {
                    // indicate
                    this.AddGroup(stack, $"dataSpec.[{i}] / Reference:", levelColors.SubSection);

                    // Reference
                    int currentI = i;
                    if (SafeguardAccess(
                        stack, this.repo, hasDataSpecification[i].DataSpecification,
                            "DataSpecification:", "Create (inner) data element!",
                        v =>
                        {
                            hasDataSpecification[currentI].DataSpecification =
                                new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                    {
                        AddKeyReference(
                            stack, String.Format("dataSpec.[{0}]", i),
                            hasDataSpecification[i].DataSpecification,
                            () =>
                            {
                                if (currentI >= 0 && currentI <= hasDataSpecification.Count)
                                    hasDataSpecification.RemoveAt(currentI);
                            },
                            repo, packages, PackageCentral.PackageCentral.Selector.MainAux,
                            addExistingEntities: null /* "All" */,
                            addPresetNames: addPresetNames, addPresetKeyLists: addPresetKeyLists,
                            relatedReferable: relatedReferable,
                            showRefSemId: false);
                    }

                    // indicate new section
                    AddGroup(stack, $"dataSpec.[{i}] / Content:", levelColors.SubSection);

                    var cntByDs = ExtendIDataSpecificationContent.GuessContentTypeFor(
                                    hasDataSpecification[i].DataSpecificationContent);

                    AddHintBubble(
                        stack, hintMode, new[] {
                        new HintCheck(
                            () => cntByDs == ExtendIDataSpecificationContent.ContentTypes.NoInfo,
                            "No valid data specification Reference could be identified. Thus, no content " +
                            "attributes could be provided. Check the Reference.")
                        });

                    // edit content?
                    if (cntByDs != ExtendIDataSpecificationContent.ContentTypes.NoInfo)
                    {
                        var cntNone = hasDataSpecification[i].DataSpecificationContent == null;
                        var cntMismatch = ExtendIDataSpecificationContent.GuessContentTypeFor(
                                        hasDataSpecification[i].DataSpecificationContent) !=
                                            ExtendIDataSpecificationContent.ContentTypes.NoInfo
                                        && ExtendIDataSpecificationContent.GuessContentTypeFor(
                                        hasDataSpecification[i].DataSpecificationContent) != cntByDs;

                        this.AddHintBubble(
                            stack, hintMode,
                            new[] {
                            new HintCheck(
                                () => cntNone,
                                "No data specification content is available for this record. " +
                                "Create content in order to create this important descriptinve " +
                                "information.",
                                breakIfTrue: true),
                            new HintCheck(
                                () => cntMismatch,
                                "Mismatch between data specification Reference and stored content " +
                                "of data specification.")
                            });

                        if (SafeguardAccess(
                            stack, this.repo, (cntNone || cntMismatch) ? null : "NotNull",
                                "Content:", "Create (reset) content data element!",
                            v =>
                            {
                                hasDataSpecification[currentI].DataSpecificationContent =
                                    ExtendIDataSpecificationContent.ContentFactoryFor(cntByDs);

                                return new AnyUiLambdaActionRedrawEntity();
                            }))
                        {
                            if (cntByDs == ExtendIDataSpecificationContent.ContentTypes.Iec61360)
                                this.DisplayOrEditEntityDataSpecificationIec61360(
                                    env, stack,
                                    hasDataSpecification[i].DataSpecificationContent
                                        as Aas.DataSpecificationIec61360,
                                    relatedReferable: relatedReferable, superMenu: superMenu);

                            //TODO (jtikekar, 0000-00-00): support DataSpecificationPhysicalUnit
#if SupportDataSpecificationPhysicalUnit
                            if (cntByDs == ExtendIDataSpecificationContent.ContentTypes.PhysicalUnit)
                                this.DisplayOrEditEntityDataSpecificationPhysicalUnit(
                                    stack,
                                    hasDataSpecification[i].DataSpecificationContent
                                        as Aas.DataSpecificationPhysicalUnit,
                                    relatedReferable: relatedReferable); 
#endif
                        }
                    }
                }
            }            
        }
        
        //
        // List of References (used for isCaseOf..)
        //

        public void DisplayOrEditEntityListOfReferences(AnyUiStackPanel stack,
            List<Aas.IReference> references,
            Action<List<Aas.IReference>> setOutput,
            string entityName,
            string[] addPresetNames = null, Aas.Key[] addPresetKeys = null,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            if (this.SafeguardAccess(
                    stack, this.repo, references, $"{entityName}:", "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke((new Aas.IReference[] {
                            Options.Curr.GetDefaultEmptyReference() }).ToList());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, $"{entityName}:", levelColors.SubSection);

                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, $"{entityName}:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-reference", "Add Reference",
                                "Adds a reference to the list.")
                            .AddAction("delete-reference", "Delete last reference",
                                "Deletes the last reference in the list."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                                references.Add(Options.Curr.GetDefaultEmptyReference());

                            if (buttonNdx == 1 && references.Count > 0)
                            {
                                references.RemoveAt(references.Count - 1);
                                if (references.Count < 1)
                                    setOutput(null);
                            }

                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (references != null && references.Count > 0)
                {
                    for (int i = 0; i < references.Count; i++)
                    {
                        var localI = i;
                        this.AddKeyReference(
                            stack, String.Format("reference[{0}]", i),
                            references[i],
                            () =>
                            {
                                references.RemoveAt(localI);
                                if (references.Count < 1)
                                    setOutput?.Invoke(null);
                            },
                            repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            "All",
                            addEclassIrdi: true,
                            relatedReferable: relatedReferable,
                            showRefSemId: false);
                    }
                }
            }
        }

        //
        // Kind
        //

        public void DisplayOrEditEntityAssetKind(AnyUiStackPanel stack,
            Aas.AssetKind kind,
            Action<Aas.AssetKind> setOutput,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind (of AssetInformation):", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return kind != Aas.AssetKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice.",
                    severityLevel: HintCheck.Severity.Notice )
            });
            if (SafeguardAccess(
                stack, repo, kind, "kind:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(new Aas.AssetKind());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyValueExRef(
                    stack, "kind", kind, Aas.Stringification.ToString(kind), null, repo,
                    v =>
                    {
                        setOutput?.Invoke((Aas.AssetKind)Aas.Stringification.AssetKindFromString((string)v));
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(Aas.AssetKind)), comboBoxMinWidth: 105);
            }
        }

        public void DisplayOrEditEntityModelingKind(AnyUiStackPanel stack,
            Aas.ModellingKind? kind,
            Action<Aas.ModellingKind> setOutput,
            string instanceExceptionStatement = null,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Kind (of model):", levelColors.SubSection);

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return kind == null; },
                    "Providing kind information is mandatory. Typically you want to model instances. " +
                        "A manufacturer would define types of assets, as well.",
                    breakIfTrue: true),
                new HintCheck(
                    () => { return kind != Aas.ModellingKind.Instance; },
                    "Check for kind setting. 'Instance' is the usual choice." + instanceExceptionStatement,
                    severityLevel: HintCheck.Severity.Notice )
            });

            if (this.SafeguardAccess(
                stack, repo, kind, "kind:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(Aas.ModellingKind.Instance);
                    return new AnyUiLambdaActionRedrawEntity();
                }
                ))
            {
                AddKeyValueExRef(
                    stack, "kind", kind, Aas.Stringification.ToString(kind), null, repo,
                    v =>
                    {
                        setOutput?.Invoke((Aas.ModellingKind)Aas.Stringification.ModellingKindFromString((string)v));
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    Enum.GetNames(typeof(Aas.ModellingKind)), comboBoxMinWidth: 105);
            }
        }

        //
        // HasSemantic
        //

        public void DisplayOrEditEntitySemanticId(AnyUiStackPanel stack,
            Aas.IHasSemantics semElem,
            string statement = null,
            bool checkForCD = false,
            string addExistingEntities = null,
            CopyPasteBuffer cpb = null,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null || semElem == null)
                return;

            //
            // SemanticId
            //

            this.AddGroup(stack, "Semantic ID:", levelColors.SubSection);

            // hint
            this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return semElem.SemanticId == null
                                || semElem.SemanticId.IsEmpty(); },
                            "Check if you want to add a semantic reference to an external " +
                            "concept repository entry. " + statement,
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return semElem.SemanticId?.HasSuspicousWhiteSpace() == true; },
                            "There seems to be whitespace in this Reference. This could lead to " +
                            "matching problems. Try to resolve.",
                            severityLevel: HintCheck.Severity.High,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return checkForCD &&
                                semElem.SemanticId.Keys[0].Type != Aas.KeyTypes.GlobalReference; },
                            "The semanticId usually features a GlobalReference to a concept " +
                            "within a respective concept repository.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

            // add from Copy Buffer
            var bufferKeys = CopyPasteBuffer.PreparePresetsForListKeys(cpb);

            // add the keys
            if (this.SafeguardAccess(
                    stack, repo, semElem.SemanticId, "semanticId:", "Create w/ default!",
                    v =>
                    {
                        semElem.SemanticId = Options.Curr.GetDefaultEmptyReference();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyReference(
                    stack, "semanticId", 
                    semElem.SemanticId, 
                    () => semElem.SemanticId = null,
                    repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    showRefSemId: false,
                    addExistingEntities: addExistingEntities, addFromKnown: true,
                    addEclassIrdi: true,
                    addPresetNames: bufferKeys.Item1,
                    addPresetKeyLists: bufferKeys.Item2,
                    jumpLambda: (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)));
                    },
                    relatedReferable: relatedReferable,
                    auxContextHeader: new[] { "\u2573", "Delete semanticId" },
                    auxContextLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            semElem.SemanticId = null;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    }, 
                    addKnownSemanticId: true);

            //
            // Supplemenatal SemanticId
            //

            this.AddGroup(stack, "Supplemental Semantic IDs:", levelColors.SubSection);

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => semElem.SupplementalSemanticIds == null
                        || semElem.SupplementalSemanticIds.Count < 1,
                    "Check if a supplemental semanticId is appropriate here. This only make sense, when " +
                    "the primary semanticId does not semantically identifies all relevant aspects of the " +
                    "AAS element.",
                    breakIfTrue: true,
                    severityLevel: HintCheck.Severity.Notice),
                new HintCheck(
                    () => { return semElem.SupplementalSemanticIds?.HasSuspicousWhiteSpace() == true; },
                    "There seems to be whitespace in some of these References. This could lead to " +
                    "matching problems. Try to resolve",
                    severityLevel: HintCheck.Severity.High,
                    breakIfTrue: true),
            });
            if (this.SafeguardAccess(
                    stack, this.repo, semElem.SupplementalSemanticIds, "supplementalSem.Id:", "Create w/ default!",
                    action: v =>
                    {
                        semElem.SupplementalSemanticIds = new List<Aas.IReference>();
                        semElem.SupplementalSemanticIds.Add(Options.Curr.GetDefaultEmptyReference());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                if (editMode)
                {
                    // let the user control the number of references
                    this.AddActionPanel(
                        stack, "supplementalSem.Id:",
                        new[] { "Add", "Delete last" }, repo,
                        (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                                semElem.SupplementalSemanticIds.Add(Options.Curr.GetDefaultEmptyReference());

                            if (buttonNdx == 1)
                            {
                                if (semElem.SupplementalSemanticIds.Count > 0)
                                    semElem.SupplementalSemanticIds.RemoveAt(
                                        semElem.SupplementalSemanticIds.Count - 1);
                                
                                if (semElem.SupplementalSemanticIds.Count < 1)
                                    semElem.SupplementalSemanticIds = null;
                            }

                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                }

                // now use the normal mechanism to deal with editMode or not ..
                if (semElem.SupplementalSemanticIds != null
                    && semElem.SupplementalSemanticIds.Count > 0)
                {
                    for (int i = 0; i < semElem.SupplementalSemanticIds.Count; i++)
                    {
                        // lambda
                        var localI = i;

                        // edit field
                        AddKeyReference(
                            stack, String.Format("Suppl.Sem.Id[{0}]", i),
                            semElem.SupplementalSemanticIds[i], 
                            () =>
                            {
                                semElem.SupplementalSemanticIds.RemoveAt(localI);
                                if (semElem.SupplementalSemanticIds.Count < 1)
                                    semElem.SupplementalSemanticIds = null;
                            },
                            repo,
                            packages, PackageCentral.PackageCentral.Selector.MainAux,
                            showRefSemId: false,
                            addExistingEntities: addExistingEntities, addFromKnown: true,
                            addEclassIrdi: true,
                            addPresetNames: bufferKeys.Item1,
                            addPresetKeyLists: bufferKeys.Item2,
                            jumpLambda: (kl) =>
                            {
                                return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)));
                            },
                            relatedReferable: relatedReferable,
                            auxContextHeader: new[] { "\u2573", "Delete supplementalSemanticId" },
                            auxContextLambda: (i) =>
                            {
                                if (i == 0)
                                {
                                    semElem.SemanticId = null;
                                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                                return new AnyUiLambdaActionNone();
                            });

                        // small vertical space
                        if (i < semElem.SupplementalSemanticIds.Count - 1)
                            AddVerticalSpace(stack);
                    }
                }
            }
        }

        //
        // Qualifiable
        //

        public void DisplayOrEditEntityQualifierCollection(AnyUiStackPanel stack,
            List<Aas.IQualifier> qualifiers,
            Action<List<Aas.IQualifier>> setOutput,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, "Qualifiable:", levelColors.SubSection,
                requestAuxButton: repo != null,
                auxContextHeader: new[] { "\u27f4", "Migrate to Extensions" },
                auxContextLambda: (o) =>
                {
                    if (o is int i && i == 0 && relatedReferable != null)
                    {
                        if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                "Migrate particular Qualifiers (V2.0) to Extensions (V3.0) " +
                                "for this element and all child elements? " +
                                "This operation cannot be reverted!", "Qualifiers",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            return new AnyUiLambdaActionNone();

                        relatedReferable.RecurseOnReferables(null,
                            includeThis: true,
                            lambda: (o, parents, rf) =>
                            {
                                rf?.MigrateV20QualifiersToExtensions();
                                return true;
                            });

                        Log.Singleton.Info("Migration of particular Qualifiers (V2.0) to Extensions (V3.0).");
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });

            if (this.SafeguardAccess(
                stack, repo, qualifiers, "Qualifiers:", "Create w/ default!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.IQualifier>(new[] { new Aas.Qualifier("", DataTypeDefXsd.String) }));
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.QualifierHelper(
                    stack, repo, 
                    qualifiers, () => setOutput(null),
                    relatedReferable: relatedReferable, superMenu: superMenu);
            }

        }

        //
        // List of SpecificAssetId
        //

        public void DisplayOrEditEntityListOfSpecificAssetIds(AnyUiStackPanel stack,
            List<Aas.ISpecificAssetId> pairs,
            Action<List<Aas.ISpecificAssetId>> setOutput,
            string key = "IdentifierKeyValuePairs",
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddGroup(stack, $"{key}:", levelColors.SubSection);

            if (this.SafeguardAccess(
                stack, repo, pairs, $"{key}:", "Create data element!",
                v =>
                {
                    setOutput?.Invoke(new List<Aas.ISpecificAssetId>());
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.SpecificAssetIdHelper(stack, repo, pairs,
                    key: key,
                    relatedReferable: relatedReferable);
            }

        }
        // dead-csharp off
        //not anymore required?!
        //public void DisplayOrEditEntitySingleIdentifierKeyValuePair(AnyUiStackPanel stack,
        //    List<Aas.ISpecificAssetId> pair,
        //    Action<List<Aas.ISpecificAssetId>> setOutput,
        //    string key = "IdentifierKeyValuePair",
        //    Aas.IReferable relatedReferable = null,
        //    string[] auxContextHeader = null, Func<object, AnyUiLambdaActionBase> auxContextLambda = null)
        //{
        //    // access
        //    if (stack == null)
        //        return;

        //    // members
        //    this.AddGroup(stack, $"{key}:", levelColors.SubSection, 
        //        requestAuxButton: repo != null,
        //        auxContextHeader: auxContextHeader, auxContextLambda: auxContextLambda);

        //    if (this.SafeguardAccess(
        //        stack, repo, pair, $"{key}:", "Create data element!",
        //        v =>
        //        {
        //            setOutput?.Invoke(new List<ISpecificAssetId>());
        //            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
        //            return new AnyUiLambdaActionRedrawEntity();
        //        }))
        //    {
        //        //TODO (jtikekar, 0000-00-00): need to test
        //        foreach (var specificAssetId in pair)
        //        {
        //            this.IdentifierKeyValueSinglePairHelper(
        //                        stack, repo, specificAssetId,
        //                        relatedReferable: relatedReferable);
        //        }
        //    }
        //}
        // dead-csharp on

        //
        // DataSpecificationIEC61360
        //

        public void DisplayOrEditEntityDataSpecificationIec61360(
            Aas.IEnvironment env,
            AnyUiStackPanel stack,
            Aas.DataSpecificationIec61360 dsiec,
            Aas.IReferable relatedReferable = null,
            AasxMenu superMenu = null)
        {
            // access
            if (stack == null || dsiec == null)
                return;

            // members
            this.AddGroup(stack, "Data Specification Content IEC61360:", levelColors.SubSection);

            this.AddActionPanel(
                stack, "Actions:", repo: repo,
                superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("add-record", "Delete invalid (empty)",
                        "Delete element attrributes which are invalid because of being empty."),
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx == 0)
                    {
                        if (dsiec.ShortName != null && dsiec.ShortName.IsValid() != true)
                            dsiec.ShortName = null;

                        if (dsiec.Unit != null && dsiec.Unit.HasContent() != true)
                            dsiec.Unit = null;

                        if (dsiec.UnitId != null && dsiec.UnitId.IsValid() != true)
                            dsiec.UnitId = null;

                        if (dsiec.SourceOfDefinition != null && dsiec.SourceOfDefinition.HasContent() != true)
                            dsiec.SourceOfDefinition = null;

                        if (dsiec.Symbol != null && dsiec.Symbol.HasContent() != true)
                            dsiec.Symbol = null;

                        if (dsiec.Definition != null && dsiec.Definition.IsValid() != true)
                            dsiec.Definition = null;

                        if (dsiec.ValueFormat != null && dsiec.ValueFormat.HasContent() != true)
                            dsiec.ValueFormat = null;

                        if (dsiec.ValueList != null && dsiec.ValueList.IsValid() != true)
                            dsiec.ValueList = null;

                        if (dsiec.Value != null && dsiec.Value.HasContent() != true)
                            dsiec.Value = null;

                        if (dsiec.LevelType != null && dsiec.LevelType.IsEmpty() == true)
                            dsiec.LevelType = null;

                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });

            // PreferredName

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dsiec.PreferredName != null && dsiec.PreferredName.IsValid() != true,
                        "According to the specification, an existing list of elements shall contain " +
                        "at least one element and for each element all mandatory fields shall be " +
                        "not empty."),
                    new HintCheck(
                        () => { return dsiec.PreferredName == null || dsiec.PreferredName.Count < 1; },
                        "Please add a preferred name, which could be used on user interfaces " +
                            "to identify the concept to a human person.",
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return dsiec.PreferredName.Count <2; },
                        "Please add multiple languanges.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.PreferredName, "preferredName:", "Create data element!",
                    v =>
                    {
                        dsiec.PreferredName = ExtendILangStringPreferredNameTypeIec61360.CreateFrom(
                            lang: AdminShellUtil.GetDefaultLngIso639(), text: "");

                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyListLangStr<ILangStringPreferredNameTypeIec61360>(
                    stack, "preferredName", dsiec.PreferredName,
                    repo, relatedReferable: relatedReferable,
                    setNullList: () =>
                    {
                        dsiec.PreferredName = ExtendILangStringPreferredNameTypeIec61360.CreateFrom(
                            lang: AdminShellUtil.GetDefaultLngIso639(), text: "");
                    });

            // ShortName

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => dsiec.ShortName != null && dsiec.ShortName.IsValid() != true,
                            "According to the specification, an existing list of elements shall contain " +
                            "at least one element and for each element all mandatory fields shall be " +
                            "not empty."),
                        new HintCheck(
                            () => { return dsiec.ShortName == null || dsiec.ShortName.Count < 1; },
                            "Please check if you can add a short name, which is a reduced, even symbolic version of " +
                                "the preferred name. IEC 61360 defines some symbolic rules " +
                                "(e.g. greek characters) for this name.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.ShortName.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice),
                        new HintCheck(
                            () => { return dsiec.ShortName
                                .Select((ls) => ls.Text != null && ls.Text.Length > 18)
                                .Any((c) => c == true); },
                            "ShortNameTypeIEC61360 only allows 1..18 characters.")
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.ShortName, "shortName:", "Create data element!",
                    v =>
                    {
                        dsiec.ShortName = ExtendILangStringShortNameTypeIec61360.CreateFrom(
                            lang: AdminShellUtil.GetDefaultLngIso639(), text: "");
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                AddKeyListLangStr<ILangStringShortNameTypeIec61360>(
                    stack, "shortName", dsiec.ShortName,
                    repo, relatedReferable: relatedReferable,
                    setNullList: () => dsiec.ShortName = null);

            // Unit

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return (dsiec.UnitId == null || dsiec.UnitId.Keys.Count < 1) &&
                                ( dsiec.Unit == null || dsiec.Unit.Trim().Length < 1);
                        },
                        "Please check, if you can provide a unit or a unitId, " +
                            "in which the concept is being measured. " +
                            "Usage of SI-based units is encouraged.",
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return dsiec.Unit != null && dsiec.Unit.Trim() == ""; },
                        "According to the specification, empty values are not allowed. " +
                        "Please delete the data element or set the content.")
                        });
            if (SafeguardAccess(
                stack, repo, dsiec.Unit, "unit:", "Create data element!",
                v =>
                {
                    dsiec.Unit = "";
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyValueExRef(
                stack, "unit", dsiec, dsiec.Unit, null, repo,
                v =>
                {
                    dsiec.Unit = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Delete" },
                auxButtonToolTips: new[] { "Delete data element" },
                auxButtonLambda: (i) =>
                {
                    if (i == 0)
                    {
                        dsiec.Unit = null;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            // UnitId

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => {
                            return ( dsiec.Unit == null || dsiec.Unit.Trim().Length < 1) 
                                && ( dsiec.UnitId == null || dsiec.UnitId.Keys.Count < 1);
                        },
                        "Please check, if you can provide a unit or a unitId, " +
                            "in which the concept is being measured. " +
                            "Usage of SI-based units is encouraged.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                stack, repo, dsiec.UnitId, "unitId:", "Create data element!",
                v =>
                {
                    dsiec.UnitId = Options.Curr.GetDefaultEmptyReference();
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyReference(
                    stack, "unitId", 
                    dsiec.UnitId, () => dsiec.UnitId = null,
                    repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                    addExistingEntities: Aas.Stringification.ToString(Aas.KeyTypes.GlobalReference),
                    addEclassIrdi: true,
                    relatedReferable: relatedReferable);
            }

            // source of definition

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dsiec.SourceOfDefinition != null && dsiec.SourceOfDefinition.Trim() == ""; },
                        "According to the specification, empty values are not allowed. " +
                        "Please delete the data element or set the content.",
                        breakIfTrue: true),
                    new HintCheck(
                        () =>
                        {
                            return dsiec.SourceOfDefinition == null || dsiec.SourceOfDefinition.Length < 1;
                        },
                        "Please check, if you can provide a source of definition for the concepts. " +
                            "This could be an informal link to a document, glossary item etc.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                stack, repo, dsiec.SourceOfDefinition, "sourceOfDef.:", "Create data element!",
                v =>
                {
                    dsiec.SourceOfDefinition = "";
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyValueExRef(
                stack, "sourceOfDef.", dsiec, dsiec.SourceOfDefinition, null, repo,
                v =>
                {
                    dsiec.SourceOfDefinition = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Delete" },
                auxButtonToolTips: new[] { "Delete data element" },
                auxButtonLambda: (i) =>
                {
                    if (i == 0)
                    {
                        dsiec.SourceOfDefinition = null;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            // Symbol

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dsiec.Symbol == null || dsiec.Symbol.Trim().Length < 1; },
                        "Please check, if you can provide formulaic character for the concept.",
                        severityLevel: HintCheck.Severity.Notice),
                    new HintCheck(
                        () => { return dsiec.Symbol != null && dsiec.Symbol.Trim() == ""; },
                        "According to the specification, empty values are not allowed. " +
                        "Please delete the data element or set the content.")
                });
            if (SafeguardAccess(
                stack, repo, dsiec.Symbol, "symbol:", "Create data element!",
                v =>
                {
                    dsiec.Symbol = "";
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyValueExRef(
                stack, "symbol", dsiec, dsiec.Symbol, null, repo,
                v =>
                {
                    dsiec.Symbol = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Delete" },
                auxButtonToolTips: new[] { "Delete data element" },
                auxButtonLambda: (i) =>
                {
                    if (i == 0)
                    {
                        dsiec.Symbol = null;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            // DataType

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dsiec.DataType == null; },
                        "Please check if you can provide a data type for the concept. For regular IEC 61360 " +
                        "properties, this should be the cases. The data types are provided by the IEC 61360.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.DataType, "dataType:", "Create data element!",
                    v =>
                    {
                        dsiec.DataType = DataTypeIec61360.StringTranslatable;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                // TODO (MIHO, 2024-12-09): check if to allow further Iec data types such as "File":
                // comboBoxItems: (AdminShellUtil.GetEnumValues<Aas.DataTypeIec61360>()
                //      .Select((dt) => dt.ToString())).ToArray(),

                AddKeyValueExRef(
                    stack, "dataType", dsiec, Aas.Stringification.ToString(dsiec.DataType), null, repo,
                    v =>
                    {
                        dsiec.DataType = Aas.Stringification.DataTypeIec61360FromString(v as string);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    },
                    comboBoxIsEditable: false,
                    comboBoxMinWidth: 190,
                    comboBoxItems: Aas.Constants.DataTypeIec61360ForPropertyOrValue.Select(
                        (dt) => Aas.Stringification.ToString(dt)).ToArray(),
                    auxButtonTitles: new[] { "Delete" },
                    auxButtonToolTips: new[] { "Delete data element" },
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            dsiec.DataType = null;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    });
            }

            // Definition

            AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return dsiec.Definition == null || dsiec.Definition.Count < 1; },
                            "Please check, if you can add a definition, which could be used to describe exactly, " +
                                "how to establish a value/ measurement for the concept.",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return dsiec.Definition.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.Definition, "definition:", "Create data element!",
                    v =>
                    {
                        dsiec.Definition = ExtendILangStringDefinitionTypeIec61360.CreateFrom(
                            lang: AdminShellUtil.GetDefaultLngIso639(), text: "");

                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr<ILangStringDefinitionTypeIec61360>(
                    stack, "definition", dsiec.Definition,
                    repo, relatedReferable: relatedReferable,
                    setNullList: () => dsiec.Definition = null);

            // ValueFormat
            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dsiec.ValueFormat != null && dsiec.ValueFormat.Trim() == ""; },
                        "According to the specification, empty values are not allowed. " +
                        "Please delete the data element or set the content.")
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.ValueFormat, "valueFormat:", "Create data element!",
                    v =>
                    {
                        dsiec.ValueFormat = "";
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                AddKeyValueExRef(
                    stack, "valueFormat", dsiec, dsiec.ValueFormat, null, repo,
                    v =>
                    {
                        dsiec.ValueFormat = v as string;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: new[] { "Delete" },
                    auxButtonToolTips: new[] { "Delete data element" },
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            dsiec.ValueFormat = null;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    });
            }

            // ValueList

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dsiec.ValueList == null
                            || dsiec.ValueList.ValueReferencePairs == null
                            || dsiec.ValueList.ValueReferencePairs.Count < 1; },
                        "If the concept features multiple possible discrete values (enumeration), " +
                        "please check, if you can add pairs of name and References to concepts " +
                        "representing the single values.",
                        severityLevel: HintCheck.Severity.Notice,
                        breakIfTrue: true),
                    new HintCheck(
                        () => dsiec.ValueList.IsValid() != true,
                        "According to the specification, an existing list of elements shall contain " +
                        "at least one element and for each element all mandatory fields shall be " +
                        "not empty.",
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return dsiec.ValueList.ValueReferencePairs.Count < 2; },
                        "Please add multiple pairs of name and Reference.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.ValueList?.ValueReferencePairs, "valueList:", "Create data element!",
                    v =>
                    {
                        dsiec.ValueList ??= new Aas.ValueList(null);
                        dsiec.ValueList.ValueReferencePairs = new()
                        {
                            new Aas.ValueReferencePair(
                            "", Options.Curr.GetDefaultEmptyReference())
                        };
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(stack, "IEC61360 value list items", levelColors.SubSection);

                ValueListHelper(
                    env, stack, repo, "valueList",
                    dsiec.ValueList.ValueReferencePairs,
                    relatedReferable: relatedReferable, superMenu: superMenu,
                    setValueList: (val) => dsiec.ValueList = val);
            }

            // Value

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dsiec.Value == null; },
                        "If the concept stands for a single value of a value list, please provide " +
                        "the value. Not required for enumerations or properties.",
                        severityLevel: HintCheck.Severity.Notice,
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return dsiec.Value != null && dsiec.Value.Trim() == ""; },
                        "According to the specification, empty values are not allowed. " +
                        "Please delete the data element or set the content.")
                });
            if (SafeguardAccess(
                stack, repo, dsiec.Value, "value:", "Create data element!",
                v =>
                {
                    dsiec.Value = "";
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                AddKeyValueExRef(
                stack, "value", dsiec, dsiec.Value, null, repo,
                v =>
                {
                    dsiec.Value = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Delete" },
                auxButtonToolTips: new[] { "Delete data element" },
                auxButtonLambda: (i) =>
                {
                    if (i == 0)
                    {
                        dsiec.Value = null;
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            // LevelType

            AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dsiec.LevelType == null ||
                            !(dsiec.LevelType.Min || dsiec.LevelType.Max
                                || dsiec.LevelType.Nom || dsiec.LevelType.Typ),
                        "Consider specifying a IEC61360 level type attribute for the " +
                        "intended values.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (SafeguardAccess(
                    stack, repo, dsiec.LevelType, "levelType:", "Create data element!",
                    v =>
                    {
                        dsiec.LevelType = new Aas.LevelType(false, false, false, false);
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                var subg = AddSubGrid(stack, "levelType:",
                    1, 6, new[] { "#", "#", "#", "#", "*", "#" },
                    paddingCaption: new AnyUiThickness(5, 0, 0, 0),
                    marginGrid: new AnyUiThickness(4, 0, 0, 0),
                    minWidthFirstCol: GetWidth(FirstColumnWidth.Standard));

                Action<int, string, bool, Action<bool>> lambda = (col, name, value, setValue) =>
                {
                    AnyUiUIElement.RegisterControl(
                        AddSmallCheckBoxTo(subg, 0, col,
                            content: name,
                            isChecked: value,
                            margin: new AnyUiThickness(0, 0, 15, 0)),
                        (v) =>
                        {
                            setValue?.Invoke(!value);
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                };

                lambda(0, "min", dsiec.LevelType.Min, (sv) => { dsiec.LevelType.Min = sv; });
                lambda(1, "max", dsiec.LevelType.Max, (sv) => { dsiec.LevelType.Max = sv; });
                lambda(2, "nom", dsiec.LevelType.Nom, (sv) => { dsiec.LevelType.Nom = sv; });
                lambda(3, "typ", dsiec.LevelType.Typ, (sv) => { dsiec.LevelType.Typ = sv; });

                AnyUiUIElement.RegisterControl(
                    AddSmallButtonTo(subg, 0, 5,
                        margin: new AnyUiThickness(2, 2, 2, 2),
                        content: "Delete",
                        toolTip: "Delete data element"),
                        (v) =>
                        {
                            dsiec.LevelType = null;
                            this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        });
            }
        }

        //
        // DataSpecificationIEC61360
        //

        //TODO (jtikekar, 0000-00-00): support DataSpecificationPhysicalUnit
#if SupportDataSpecificationPhysicalUnit
        public void DisplayOrEditEntityDataSpecificationPhysicalUnit(
    AnyUiStackPanel stack,
    Aas.DataSpecificationPhysicalUnit dspu,
    Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null || dspu == null)
                return;

            // members
            AddGroup(
                stack, "Data Specification Content Physical Unit:", levelColors.SubSection);

            // UnitName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.UnitName.HasContent() != true,
                        "Please name the phyiscal unit. This is mandatory information.")
                });
            AddKeyValueExRef(
                stack, "unitName", dspu, dspu.UnitName, null, repo,
                v =>
                {
                    dspu.UnitName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // UnitSymbol

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.UnitSymbol.HasContent() != true,
                        "Please provide a symbol representation to the phyiscal unit. " +
                        "This is mandatory information, if available.")
                });
            AddKeyValueExRef(
                stack, "unitSymbol", dspu, dspu.UnitSymbol, null, repo,
                v =>
                {
                    dspu.UnitSymbol = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // Definition

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return dspu.Definition == null || dspu.Definition.Count < 1; },
                        "Please check, if you can add a definition, which could be used to describe exactly, " +
                            "how to the unit is defined or measured concept.",
                        severityLevel: HintCheck.Severity.Notice,
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return dspu.Definition.Count <2; },
                        "Please add multiple languanges for the definition.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            if (this.SafeguardAccess(
                    stack, repo, dspu.Definition, "definition:", "Create data element!",
                    v =>
                    {
                        dspu.Definition = new List<Aas.LangString>();
                        this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
                this.AddKeyListLangStr(stack, "definition", dspu.Definition,
                    repo, relatedReferable: relatedReferable);

            // SiNotation

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.SiNotation.HasContent() != true,
                        "Please check, if you can provide a notation according to SI.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "SI notation", dspu, dspu.SiNotation, null, repo,
                v =>
                {
                    dspu.SiNotation = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // SiName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.SiName.HasContent() != true,
                        "Please check, if you can provide a name according to SI.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "SI name", dspu, dspu.SiName, null, repo,
                v =>
                {
                    dspu.SiName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // DinNotation

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.DinNotation.HasContent() != true,
                        "Please check, if you can provide a notation according to DIN.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "DIN notation", dspu, dspu.DinNotation, null, repo,
                v =>
                {
                    dspu.DinNotation = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // EceName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.EceName.HasContent() != true,
                        "Please check, if you can provide a name according to ECE.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "ECE name", dspu, dspu.EceName, null, repo,
                v =>
                {
                    dspu.EceName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // EceCode

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.EceCode.HasContent() != true,
                        "Please check, if you can provide a code according to DIN.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "ECE code", dspu, dspu.EceCode, null, repo,
                v =>
                {
                    dspu.EceCode = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // NistName

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.NistName.HasContent() != true,
                        "Please check, if you can provide a name according to NIST.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "NIST name", dspu, dspu.NistName, null, repo,
                v =>
                {
                    dspu.EceName = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // source of definition

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.SourceOfDefinition.HasContent() != true,
                        "Please check, if you can provide a source of definition for the unit. " +
                        "This could be an informal link to a document, glossary item etc.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "sourceOfDef.", dspu, dspu.SourceOfDefinition, null, repo,
                v =>
                {
                    dspu.SourceOfDefinition = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // conversion factor

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.ConversionFactor.HasContent() != true,
                        "Please check, if you can provide a conversion factor. " +
                        "Example could be: 1.0/60 .",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "conversionFac.", dspu, dspu.ConversionFactor, null, repo,
                v =>
                {
                    dspu.ConversionFactor = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // registration authority id

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.RegistrationAuthorityId.HasContent() != true,
                        "Please check, if you can provide a registration authority id.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "regAuthId.", dspu, dspu.RegistrationAuthorityId, null, repo,
                v =>
                {
                    dspu.RegistrationAuthorityId = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });

            // Supplier

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => dspu.Supplier.HasContent() != true,
                        "Please check, if you can provide a supplier.",
                        severityLevel: HintCheck.Severity.Notice)
                });
            AddKeyValueExRef(
                stack, "supplier", dspu, dspu.Supplier, null, repo,
                v =>
                {
                    dspu.Supplier = v as string;
                    this.AddDiaryEntry(relatedReferable, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                });
        } 
#endif

        //
        // special Submodel References
        // 


        // TODO (MIHO, 2022-12-26): seems not to be used
#if OLD
        public void DisplayOrEditEntitySubmodelRef(AnyUiStackPanel stack,
            Aas.Reference smref,
            Action<Aas.Reference> setOutput,
            string entityName,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => { return smref == null; },
                    $"No {entityName}. Please consider adding a reference " +
                        "to an adequate Submodel."),
            });
            if (this.SafeguardAccess(
                    stack, repo, smref, $"{entityName}:",
                    "Create data element!",
                    v =>
                    {
                        setOutput?.Invoke(new Aas.Reference(Aas.ReferenceTypes.GlobalReference, new List<Aas.Key>()));
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                this.AddGroup(
                    stack, $"{entityName} - Aas.Reference to describing Submodel:",
                    levelColors.SubSection);
                this.AddKeyListKeys(
                    stack, $"{entityName}:", smref.Keys,
                    repo, packages, PackageCentral.PackageCentral.Selector.Main, "Submodel",
                    relatedReferable: relatedReferable);
            }
        }
#endif

        //
        // File / Resource attributes
        // 

        public class CentralizeFilesRecord
        {
            public string CentralStoreUri = "";
            public string UUID = "";
            public bool DeleteFileAfter = false;
        }

        /// <summary>
        /// Implements a crude, very short version of UUID, that is, base64 over hash over GUID.
        /// For alternatives, see: https://stackoverflow.com/questions/9278909/net-short-unique-identifier
        /// </summary>
        public string GetShortUUid()
        {
            var uuid = Guid.NewGuid().ToString();
            var hash = uuid.GetHashCode().ToString("X8");
            var base64 = AdminShellUtil.Base64UrlEncode(hash);
            return base64;
        }

        public CentralizeFilesRecord GenerateNewCentralizeFilesRecord()
        {
            return new CentralizeFilesRecord()
            {
                CentralStoreUri = "" + Options.Curr.CentralStores?.FirstOrDefault(),
                UUID = GetShortUUid(),
                DeleteFileAfter = false
            };
        }

        public static async Task<bool> PerformCentralizeFilesDialogue(
            AnyUiContextBase displayContext,
            string caption,
            CentralizeFilesRecord record,
            string info = null)
        {
            // access
            if (displayContext == null || caption?.HasContent() != true || record == null)
                return false;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(caption);
            uc.ActivateRenderPanel(record,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                renderPanel: (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 2, new[] { "180:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5),
                                margin: new AnyUiThickness(10, 0, 30, 0));

                    panel.Add(g);

                    // dynamic rows
                    int row = 0;

                    // info?
                    if (info?.HasContent() == true)
                    {
                        // Statistics
                        helper.Set(
                            helper.AddSmallLabelTo(g, row, 0, content: info),
                            colSpan: 2);
                        row++;

                        // separation
                        helper.AddSmallBorderTo(g, row, 0,
                            borderThickness: new AnyUiThickness(0.5), borderBrush: AnyUiBrushes.White,
                            colSpan: 2,
                            margin: new AnyUiThickness(0, 0, 0, 20));
                        row++;
                    }

                    // Central store URI
                    helper.AddSmallLabelTo(g, row, 0, content: "Central store URI:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    if (displayContext is AnyUiContextPlusDialogs cpd
                        && cpd.HasCapability(AnyUiContextCapability.WPF))
                    {
                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g, row, 1,
                                    isEditable: true,
                                    items: Options.Curr.CentralStores?.ToArray(),
                                    text: "" + record.CentralStoreUri,
                                    margin: new AnyUiThickness(0, 0, 0, 0),
                                    padding: new AnyUiThickness(0, 0, 0, 0),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch)),
                                (s) => { record.CentralStoreUri = s; });
                    }
                    else
                    {
                        AnyUiUIElement.SetStringFromControl(
                                helper.Set(
                                    helper.AddSmallTextBoxTo(g, row, 1,
                                        text: $"{record.CentralStoreUri}",
                                        verticalAlignment: AnyUiVerticalAlignment.Center,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                (s) => { record.CentralStoreUri = s; });
                    }

                    row++;

                    // UUID
                    helper.AddSmallLabelTo(g, row, 0, content: "UUID (header):",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row, 1,
                                    text: $"{record.UUID}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { record.UUID = s; });

                    row++;

                    // Delete
                    helper.AddSmallLabelTo(g, row, 0, content: "Post process:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, row, 1,
                                    content: "Delete supplementary file, if local",
                                    isChecked: record.DeleteFileAfter,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) => { record.DeleteFileAfter = b; });
                    row++;

                    // give back
                    return g;
                });

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return false;

            // ok
            return true;
        }

        public async Task<bool> PerformCentralizeFileExecution(
            AdminShellPackageEnvBase packEnv,
            CentralizeFilesRecord record,
            string filePath,
            Action<string> lambdaSetFilePath = null)
        {
            // access 
            if (packEnv == null 
                || record?.CentralStoreUri?.HasContent() != true
                || record?.UUID?.HasContent() != true
                || filePath?.HasContent() != true)
                return false;

            // try accessing it
            var ba = await packEnv.GetBytesFromPackageOrExternalAsync(filePath);
            if (ba == null || ba.Length < 1)
            {
                Log.Singleton.Error("Centralize file: cannot read file: {0}", filePath);
                return false;
            }

            // filePath shall only contain harmless chars and NO separators, but dots!
            var filterFilePath = AdminShellUtil.FilterFriendlyName(filePath,
                            regexForFilter: @"[^a-zA-Z0-9\-_.]",
                            fixMoreBlanks: true);

            // add the UUID in front
            var newFilePath = record.UUID.Trim() + "_" + filterFilePath;

            // build target path
            var centralStorePath = record.CentralStoreUri;
            if (centralStorePath.EndsWith('/') || centralStorePath.EndsWith('\\'))
                centralStorePath = centralStorePath.Substring(0, centralStorePath.Length - 1);
            var targetPath = Path.Combine(centralStorePath, newFilePath);                           

            // try to write there?
            // assuming file storage writable to computer
            var res = await packEnv.PutByteArrayToExternalUri(targetPath, ba);
            if (!res)
            {
                Log.Singleton.Error("Centralize file: error writing bytes to location: {0}", targetPath);
                return false;
            }

            // can set new filename
            lambdaSetFilePath?.Invoke(targetPath);

            // delete only possible for local files
            if (packEnv.IsLocalFile(filePath) && record.DeleteFileAfter)
            {
                var psfs = packEnv.GetListOfSupplementaryFiles();
                var psf = psfs?.FindByUri(filePath);
                if (psf == null)
                {
                    Log.Singleton.Error("Centralize file: unable to find existing file in package " +
                        "before deleting: {0}", targetPath);
                    return false;
                }

                packEnv.DeleteSupplementaryFile(psf);

                Log.Singleton.Info(StoredPrint.Color.Blue,
                    "Centralized file {0} bytes to new location and deleted original: {1}. " +
                    "A save operation is required for the package!",
                    ba.Length, newFilePath);

                return true;
            }

            // do the normal info
            Log.Singleton.Info(StoredPrint.Color.Blue,
                "Centralized file {0} bytes to new location (preserved original): {1}.",
                ba.Length, targetPath);

            return true;
        }

        public static bool DisplayOrEditEntityFileResource_EditTextFile(
            AnyUiContextBase context,
            AdminShellPackageEnvBase env,
            string valueContent,
            string valuePath)
        {
            // access
            if (env == null || context == null)
                return false;

            // try
            try
            {
                // try find ..
                var psfs = env.GetListOfSupplementaryFiles();
                var psf = psfs?.FindByUri(valuePath);
                if (psf == null)
                {
                    Log.Singleton.Error(
                        $"Not able to locate supplementary file {valuePath} for edit. " +
                        $"Aborting!");
                    return false;
                }

                // try read ..
                Log.Singleton.Info($"Reading text-file {valuePath} ..");
                var contents = AdminShellUtil.GetStringFromBytes(
                        env.GetBytesFromPackageOrExternal(valuePath));

                // test
                if (contents == null)
                {
                    Log.Singleton.Error(
                        $"Not able to read contents from  supplmentary file {valuePath} " +
                        $"for edit. Aborting!");
                    return false;
                }

                // edit
                var uc = new AnyUiDialogueDataTextEditor(
                            caption: $"Edit text-file '{valuePath}'",
                            mimeType: valueContent,
                            text: contents);
                if (!context.StartFlyoverModal(uc))
                    return false;

                // save
                byte[] bytes = Encoding.ASCII.GetBytes(uc.Text);
                try
                {
                    // TODO: add IdShortPath !!
                    env.PutBytesToPackageOrExternal(
                        valuePath, bytes);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "when storing contents to text-file: " + valuePath);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"Edit text-file {valuePath} in package.");
            }

            return true;
        }

        public void DisplayOrEditEntityFileResource(AnyUiStackPanel stack,
            AdminShellPackageEnvBase packEnv,
            Aas.IReferable containingObject,
            ModifyRepo repo, AasxMenu superMenu,
            string valuePath,
            string valueContent,
            Action<string, string> setOutput,
            Aas.IReferable relatedReferable = null)
        {
            // access
            if (stack == null)
                return;

            // members

            // Value

            AddKeyValueExRef(
                stack, "value", containingObject, valuePath, null, repo,
                v =>
                {
                    valuePath = v as string;
                    setOutput?.Invoke(valuePath, valueContent);
                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                auxButtonTitles: new[] { "Choose supplemental file", },
                auxButtonToolTips: new[] { "Select existing supplemental file" },
                auxButtonLambda: (bi) =>
                {
                    if (bi == 0)
                    {
                        // Select
                        var ve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.Main, "SupplementalFile");
                        if (ve != null)
                        {
                            var sf = (ve.GetMainDataObject()) as AdminShellPackageSupplementaryFile;
                            if (sf != null)
                            {
                                valuePath = sf.Uri.ToString();
                                setOutput?.Invoke(valuePath, valueContent);
                                this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                    }

                    return new AnyUiLambdaActionNone();
                });

            // ContentType

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () =>
                        {
                            return valueContent == null || valueContent.Trim().Length < 1 ||
                                valueContent.IndexOf('/') < 1 || valueContent.EndsWith("/");
                        },
                        "The content type of the file. Former known as MIME type. " +
                        "See RFC2046.", severityLevel: HintCheck.Severity.Notice)
                });

            AddKeyValueExRef(
                stack, "contentType", containingObject, valueContent, null, repo,
                v =>
                {
                    valueContent = v as string;
                    setOutput?.Invoke(valuePath, valueContent);
                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionNone();
                },
                comboBoxIsEditable: true, comboBoxMinWidth: 140,
                comboBoxItems: AdminShellUtil.GetPopularMimeTypes());

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                        new HintCheck(
                            () => { return valuePath == null || valuePath.Trim().Length < 1; },
                            "The path to an external file or a file relative the AASX package root('/'). " +
                                "Files are typically relative to '/aasx/' or sub-directories of it. " +
                                "External files typically comply to an URL, e.g. starting with 'https://..'.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return valuePath.IndexOf('\\') >= 0; },
                            "Backslashes ('\') are not allow. Please use '/' as path delimiter.",
                            severityLevel: HintCheck.Severity.Notice)
                });

            // Further actions

            if (editMode && uploadAssistance != null && packages.Main != null)
            {
                // Remove, create text, edit
                // More file actions
                this.AddActionPanel(
                    stack, "Action",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("remove-file", "Remove existing file",
                            "Removes the file from the AASX environment.")
                        .AddAction("create-text", "Create text file",
                            "Creates a text file and adds it to the AAS environment.")
                        .AddAction("edit-text", "Edit text file",
                            "Edits the associated text file and updates it to the AAS environment.")
                        .AddAction("centralize-file", "Centralize file",
                            "Rename file, copy it to central file storage and potentially delete supplemental file.")
                        ,
                    ticketActionAsync: async (buttonNdx, ticket) =>

                    {
                        if (buttonNdx == 0 && valuePath.HasContent())
                        {
                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                try
                                {
                                    // try find ..
                                    var psfs = packages.Main.GetListOfSupplementaryFiles();
                                    var psf = psfs?.FindByUri(valuePath);
                                    if (psf == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Not able to locate supplementary file {valuePath} for removal! " +
                                            $"Aborting!");
                                    }
                                    else
                                    {
                                        Log.Singleton.Info($"Removing file {valuePath} ..");
                                        packages.Main.DeleteSupplementaryFile(psf);
                                        Log.Singleton.Info(
                                            $"Added {valuePath} to pending package items to be deleted. " +
                                            "A save-operation might be required.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Removing file {valuePath} in package");
                                }

                                // clear value
                                valuePath = "";

                                // value event
                                setOutput?.Invoke(valuePath, valueContent);
                                this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());

                                // show empty
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }

                        if (buttonNdx == 1)
                        {
                            // ask for a name
                            var uc = new AnyUiDialogueDataTextBox(
                                "Name of text file to create",
                                symbol: AnyUiMessageBoxImage.Question,
                                maxWidth: 1400,
                                text: "Textfile_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
                            this.context?.StartFlyoverModal(uc);
                            if (!uc.Result)
                            {
                                return new AnyUiLambdaActionNone();
                            }

                            var ptd = "/aasx/";
                            var ptfn = uc.Text.Trim();
                            packages.Main.PrepareSupplementaryFileParameters(ref ptd, ref ptfn);

                            // make sure the name is not already existing
                            var psfs = packages.Main.GetListOfSupplementaryFiles();
                            var psf = psfs?.FindByUri(ptd + ptfn);
                            if (psf != null)
                            {
                                this.context?.MessageBoxFlyoutShow(
                                    $"The supplemental file {ptd + ptfn} is already existing in the " +
                                    "package. Please re-try with a different file name.", "Create text file",
                                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                                return new AnyUiLambdaActionNone();
                            }

                            // try execute
                            try
                            {
                                // make temp file
                                var tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".txt");
                                System.IO.File.WriteAllText(tempFn, "");

                                var mimeType = AdminShellPackageFileBasedEnv.GuessMimeType(ptfn);

                                var targetPath = packages.Main.AddSupplementaryFileToStore(
                                    tempFn, ptd, ptfn,
                                    embedAsThumb: false, useMimeType: mimeType);

                                if (targetPath == null)
                                {
                                    Log.Singleton.Error(
                                        $"Error creating text-file {ptd + ptfn} within package");
                                }
                                else
                                {
                                    Log.Singleton.Info(StoredPrint.Color.Blue,
                                        $"Added empty text-file {ptd + ptfn} to pending package items. " +
                                        $"A save-operation is required.");
                                    valueContent = mimeType;
                                    valuePath = targetPath;
                                    setOutput?.Invoke(valuePath, valueContent);

                                    // value + struct event
                                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                    this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"Creating text-file {ptd + ptfn} within package");
                            }
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: containingObject);
                        }

                        if (buttonNdx == 2)
                        {
                            if (DisplayOrEditEntityFileResource_EditTextFile(
                                context, packages.Main,
                                valueContent: valueContent,
                                valuePath: valuePath))
                                return new AnyUiLambdaActionRedrawEntity();
                            else
                                return new AnyUiLambdaActionNone();
                        }

                        if (buttonNdx == 3 && valuePath.HasContent())
                        {
                            var changed = false;
                            var record = GenerateNewCentralizeFilesRecord();

                            if (!await PerformCentralizeFilesDialogue(
                                    context, 
                                    "Centralize file",
                                    record,
                                    $"File: {valuePath}"))
                                return new AnyUiLambdaActionNone();

                            await PerformCentralizeFileExecution(
                                packEnv, record,
                                valuePath,
                                lambdaSetFilePath: (v) =>
                                {
                                    changed = true;
                                    valuePath = v;
                                    setOutput?.Invoke(valuePath, valueContent);
                                    this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                });

                            if (changed)
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: relatedReferable);
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // Further file assistance
                this.AddGroup(stack, "Supplemental file assistance", this.levelColors.SubSection);

                AddKeyValueExRef(
                    stack, "Target path", this.uploadAssistance, this.uploadAssistance.TargetPath, null, repo,
                    v =>
                    {
                        this.uploadAssistance.TargetPath = v as string;
                        return new AnyUiLambdaActionNone();
                    });

                this.AddKeyDropTarget(
                    stack, "Source file to add",
                    !(this.uploadAssistance.SourcePath.HasContent())
                        ? "(Please drop a file to set source file to add)"
                        : this.uploadAssistance.SourcePath,
                    null, repo,
                    v =>
                    {
                        this.uploadAssistance.SourcePath = v as string;
                        return new AnyUiLambdaActionRedrawEntity();
                    }, minHeight: 40);

                this.AddActionPanel(
                    stack, "Action",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("select-source", "Select source file",
                            "Select a filename to be added later.")
                        .AddAction("add-to-aasx", "Add or update to AASX",
                            "Add or update file given by selected filename to the AAS environment."),
                    ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var uc = new AnyUiDialogueDataOpenFile(
                                message: "Select a supplemental file to add..");
                                this.context?.StartFlyoverModal(uc);
                                if (uc.Result && uc.TargetFileName != null)
                                {
                                    this.uploadAssistance.SourcePath = uc.TargetFileName;
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }

                            if (buttonNdx == 1)
                            {
                                try
                                {
                                    var ptd = uploadAssistance.TargetPath.Trim();
                                    var ptfn = System.IO.Path.GetFileName(uploadAssistance.SourcePath);
                                    packages.Main.PrepareSupplementaryFileParameters(ref ptd, ref ptfn);

                                    var mimeType = AdminShellPackageFileBasedEnv.GuessMimeType(ptfn);

                                    var targetPath = packages.Main.AddSupplementaryFileToStore(
                                        uploadAssistance.SourcePath, ptd, ptfn,
                                        embedAsThumb: false, useMimeType: mimeType);

                                    if (targetPath == null)
                                    {
                                        Log.Singleton.Error(
                                            $"Error adding file {uploadAssistance.SourcePath} to package");
                                    }
                                    else
                                    {
                                        Log.Singleton.Info(StoredPrint.Color.Blue,
                                            $"Added {ptfn} to pending package items. A save-operation is required.");
                                        valueContent = mimeType;
                                        valuePath = targetPath;
                                        setOutput?.Invoke(valuePath, valueContent);

                                        // value + struct event
                                        this.AddDiaryEntry(containingObject, new DiaryEntryStructChange());
                                        this.AddDiaryEntry(containingObject, new DiaryEntryUpdateValue());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Adding file {uploadAssistance.SourcePath} to package");
                                }

                                // refresh dialogue
                                uploadAssistance.SourcePath = "";
                                return new AnyUiLambdaActionRedrawEntity();
                            }

                            return new AnyUiLambdaActionNone();
                        });
            }

        }

        //
        // Values checking
        //

        /// <summary>
        /// Infomration carrying from the functionbelow to the main application
        /// </summary>
        public class DisplayOrEditEntityCheckValueHandle
        {
            public Aas.IReferable Referable = null;

            public List<SmtAttributeCheckItem> CheckItems = new List<SmtAttributeCheckItem>();

			public AnyUiBorder Border = null;
            public AnyUiTextBlock TextBlock = null;
        }

        public List<SmtAttributeCheckItem> DisplayOrEditEntityCheckValueEvalItems(
            Aas.IReferable rf)
        {
            // access
            var checkItems = new List<SmtAttributeCheckItem>();
            if (rf == null)
                return checkItems;

            // try gain information from the given referable itself
            var rec = CheckReferableForExtensionRecords<SmtAttributeRecord>(rf).FirstOrDefault();
            if (rec == null)
            {
                // can analyze qualifiers?
                rec = AasSmtQualifiers.FindSmtQualifiers(rf, removeQualifers: false);
            }            

            // if not, can access semanticId -> ConcepTdescription?
            if (rec == null && rf is Aas.IHasSemantics rfsem
                && rfsem.SemanticId?.IsValid() == true
                && rfsem.SemanticId.Count() == 1
                && packages != null)
            {
                // try find
                foreach (var x in packages.QuickLookupAllIdent(rfsem.SemanticId.Keys[0].Value))
                    if (x.Item2 is Aas.IConceptDescription rfsemCd)
                    {
						var rec2 = CheckReferableForExtensionRecords<SmtAttributeRecord>(rfsemCd).FirstOrDefault();
                        if (rec2 == null)
                            rec2 = AasSmtQualifiers.FindSmtQualifiers(rf, removeQualifers: false);
                        if (rec2 != null)
                            rec = rec2;
					}
            }

			// some checks can be done on the static record function, as record entities might
			// be only on subordinate elements

			Func<Aas.IReferable, SmtAttributeRecord> lambdaLookupSmtRec = (rf2) =>
			{
				return CheckReferableForExtensionRecords<SmtAttributeRecord>(rf2).FirstOrDefault();
			};

			if (rf is Aas.ISubmodel sm)
				checkItems = SmtAttributeRecord.PerformAttributeCheck(
                    sm, sm.SubmodelElements, inList: checkItems,
					lambdaLookupSmtRec: lambdaLookupSmtRec);

			if (rf is Aas.ISubmodelElementCollection smc)
				checkItems = SmtAttributeRecord.PerformAttributeCheck(
                    smc, smc.Value, inList: checkItems,
					lambdaLookupSmtRec: lambdaLookupSmtRec);

			if (rf is Aas.ISubmodelElementList sml)
				checkItems = SmtAttributeRecord.PerformAttributeCheck(
					sml, sml.Value, inList: checkItems,
					lambdaLookupSmtRec: lambdaLookupSmtRec);

			// perform the check on factual record of this element
			if (rec != null)
            {                
                if (rf is Aas.IProperty prop)
					checkItems = rec.PerformAttributeCheck(rf.IdShort, prop.Value, checkItems);
                
                if (rf is Aas.IMultiLanguageProperty mlp)
					checkItems = rec.PerformAttributeCheck(mlp, checkItems);                
			}

            // okay
            return checkItems;

        }

        /// <summary>
        /// this handle is used to link edit value field and status fields together
        /// </summary>
        protected DisplayOrEditEntityCheckValueHandle _checkValueHandle = new DisplayOrEditEntityCheckValueHandle();

		public void DisplayOrEditEntityCheckValue(
            Aas.IEnvironment env, AnyUiStackPanel stack,
			DisplayOrEditEntityCheckValueHandle handle,
			Aas.IReferable rf,
			bool update = false)
        {
            // access
            if (stack == null || rf == null || handle == null)
                return;

            // evaluate
            bool? alarmState = null; 
            var evalText = "Idle (no SMT spec)";
            var indicatorBg = AnyUiBrushes.White;
            var indicatorFg = AnyUiBrushes.Black;

            // test
            handle.CheckItems = DisplayOrEditEntityCheckValueEvalItems(rf);
            if (handle.CheckItems != null)
            {
                // evaluate alarm
                alarmState = handle.CheckItems.Where((aci) => aci.Fail).Count() > 0;

                if (alarmState == true)
                {
                    var distinctMsg = handle.CheckItems.GroupBy((ci) => ci.ShortText).Select((gr) => gr.First().ShortText);
                    evalText = "Fail: " + string.Join(" ", distinctMsg);
                    indicatorBg = new AnyUiBrush(0xffFF4F0E);
                    indicatorFg = new AnyUiBrush(0xFF541805);
				}
                else
                {
                    evalText = "PASS!";
					indicatorBg = new AnyUiBrush(0xff00cd90);
					indicatorFg = new AnyUiBrush(0xff009064);
				}
			}

			// update
			if (update && handle.Referable == rf)
            {
                if (handle.Border != null)
                {
					handle.Border.Background = indicatorBg;
					handle.Border.BorderBrush = indicatorFg;
					handle.Border.Touch();
				}

                if (handle.TextBlock != null)
                {
					handle.TextBlock.Text = evalText;
					handle.Border.Touch();
				}

				// stop here
				return;
            }

            // NO update, rebuild
            handle.Referable = rf;

			// add grid
			var g = AddSubGrid(stack, "SMT value check:",
				rows: 1, cols: 2, new[] { "*", "#" },
				paddingCaption: new AnyUiThickness(6, 0, 0, 0),
				minWidthFirstCol: GetWidth(FirstColumnWidth.Standard));

            g.DebugTag = "TEST2";

			// indicator
			handle.Border = AddSmallBorderTo(g, 0, 0,
                margin: new AnyUiThickness(5, 2, 2, 2),
                background: indicatorBg,
                borderBrush: indicatorFg,
                borderThickness: new AnyUiThickness(1));
            handle.TextBlock = new AnyUiTextBlock()
            {
                Text = "" + evalText,
                HorizontalAlignment = AnyUiHorizontalAlignment.Center,
                VerticalAlignment = AnyUiVerticalAlignment.Center,
                Foreground = AnyUiBrushes.White,
                Background = AnyUi.AnyUiBrushes.Transparent,
                FontSize = 1.0,
                FontWeight = AnyUiFontWeight.Bold
            };
			handle.Border.Child = handle.TextBlock;

			AnyUiUIElement.RegisterControl(
				AddSmallButtonTo(g, 0, 1,
					margin: new AnyUiThickness(2, 2, 2, 2),
					padding: new AnyUiThickness(5, 0, 5, 0),
					content: "\u2261"),
					setValue: (v) =>
					{
                        // re-evaluate
                        Log.Singleton.Info("Starting check.");
						var ci2 = DisplayOrEditEntityCheckValueEvalItems(rf);
                        if (ci2 != null)
						    foreach (var aci in handle.CheckItems)
                                Log.Singleton.Info("" + aci.LongText);
                        Log.Singleton.Info(StoredPrint.Color.Blue,
                            "SMT value check: " + ((alarmState == true) ? "FAIL!" : "PASS / IDLE!") + " See log for details.");
						return new AnyUiLambdaActionNone();
					});            
		}

	}
}
