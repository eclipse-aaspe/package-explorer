/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Newtonsoft.Json;
using Extensions;

namespace AasxPluginGenericForms
{
    [DisplayName("Record")]
    public class GenericFormsOptionsRecord
    {
        /// <summary>
        /// Shall always contain 3-4 digit tag label, individual for each template
        /// </summary>
        public string FormTag = "";

        /// <summary>
        /// Shall always contain 3-4 digit tag title, individual for each template
        /// </summary>
        public string FormTitle = "";

        /// <summary>
        /// Full (recursive) description for Submodel to be generated
        /// </summary>
        public FormDescSubmodel FormSubmodel = null;

        /// <summary>
        /// A list with required concept descriptions, if appropriate.
        /// </summary>
        public List<Aas.IConceptDescription> RequiredCD = null;

        //
        // Constructors
        //

        public GenericFormsOptionsRecord() { }

#if !DoNotUseAasxCompatibilityModels
        public GenericFormsOptionsRecord(
            AasxCompatibilityModels.AasxPluginGenericForms.GenericFormsOptionsRecordV20 src) : base()
        {
            FormTag = src.FormTag;
            FormTitle = src.FormTitle;
            if (src.FormSubmodel != null)
                FormSubmodel = new FormDescSubmodel(src.FormSubmodel);
            if (src.ConceptDescriptions != null)
            {
                RequiredCD = new List<Aas.IConceptDescription>();
                foreach (var ocd in src.ConceptDescriptions)
                {
                    RequiredCD.Add(
                        ExtendConceptDescription.ConvertFromV20(
                            new Aas.ConceptDescription(""), ocd));
                }
            }
        }
#endif

    }

    [DisplayName("Options")]
    public class GenericFormOptions : AasxIntegrationBase.AasxPluginOptionsBase
    {
        //
        // Constants
        //

        //
        // Option fields
        //

        public List<GenericFormsOptionsRecord> Records = new List<GenericFormsOptionsRecord>();

        //
        // Constructors
        //

        public GenericFormOptions() : base() { }

#if !DoNotUseAasxCompatibilityModels
        public GenericFormOptions(AasxCompatibilityModels.AasxPluginGenericForms.GenericFormOptionsV20 src)
            : base()
        {
            if (src.Records != null)
                foreach (var rec in src.Records)
                    Records.Add(new GenericFormsOptionsRecord(rec));
        }
#endif

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static GenericFormOptions CreateDefault()
        {
            var opt = new GenericFormOptions();

            var rec = new GenericFormsOptionsRecord()
            {
                FormTag = "SMP",
                FormTitle = "Sample declaration of a GenericTemplate"
            };
            opt.Records.Add(rec);

            rec.FormSubmodel = new FormDescSubmodel(
                "Submodel Root",
                new Aas.Key(Aas.KeyTypes.Submodel, "www.exmaple.com/sms/1112"),
                "Example",
                "Information string");

            rec.FormSubmodel.Add(new FormDescProperty(
                formText: "Sample Property",
                multiplicity: FormMultiplicity.OneToMany,
                smeSemanticId: new Aas.Key(Aas.KeyTypes.ConceptDescription, "www.example.com/cds/1113"),
                presetIdShort: "SampleProp{0:0001}",
                valueType: "string",
                presetValue: "123"));

            return opt;
        }

        public GenericFormsOptionsRecord MatchRecordsForSemanticId(Aas.IReference sem)
        {
            // check for a record in options, that matches Submodel
            GenericFormsOptionsRecord res = null;
            if (Records != null)
                foreach (var rec in Records)
                    if (rec?.FormSubmodel?.KeySemanticId != null)
                        if (sem != null && sem.MatchesExactlyOneKey(rec.FormSubmodel.KeySemanticId))
                        {
                            res = rec;
                            break;
                        }
            return res;
        }

        public override void Merge(AasxPluginOptionsBase options)
        {
            var mergeOptions = options as GenericFormOptions;
            if (mergeOptions == null || mergeOptions.Records == null)
                return;

            if (this.Records == null)
                this.Records = new List<GenericFormsOptionsRecord>();

            this.Records.AddRange(mergeOptions.Records);
        }

    }
}
