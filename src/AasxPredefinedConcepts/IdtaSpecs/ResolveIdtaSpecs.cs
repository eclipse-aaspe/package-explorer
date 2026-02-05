using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPredefinedConcepts.IdtaSpecs
{
    public enum Concept
    {
        AssetInformation,
        Resource,
        AssetKind,
        SpecificAssetId,
        Submodel,
        SubmodelElement,
        AdministrativeInformation,
        HasDataSpecification,
        HasExtensions,
        HasKind,
        HasSemantics,
        Identifiable,
        Qualifiable,
        Qualifier,
        Referable,
        CD,
        DataSpecs,
    };

    public enum Version
    {
        V3_1_2
    }

    public enum Part
    {
        Part1
    };

    public class IdtaSpecRecord
    {
        public Concept Concept;
        public Part Part;
        public Version Version;
        public string Uri;

        public IdtaSpecRecord(Part part, Version version, Concept concept, params string[] uris)
        {
            Concept = concept;
            Part = part;
            Version = version;
            Uri = string.Join("", uris);
        }
    }

    public class IdtaSpecRecordList : List<IdtaSpecRecord>
    {
        public Part GeneralPart;
        public Version GeneralVersion;

        public IdtaSpecRecordList() { }

        public IdtaSpecRecordList(IEnumerable<IdtaSpecRecord> items) : base(items) { }

        public IdtaSpecRecordList(Part part, Version version) 
        {
            GeneralPart = part;
            GeneralVersion = version;
        }

        public void Add(Part part, Version version, Concept concept, params string[] uris)
        {
            Add(new IdtaSpecRecord(part, version, concept, uris));
        }

        public void Add(Concept concept, params string[] uris)
        {
            Add(new IdtaSpecRecord(GeneralPart, GeneralVersion, concept, uris));
        }
    }

    /// <summary>
    /// This class keeps all URIs to IDTA technical specs together
    /// </summary>
    public class ResolveIdtaSpecs
    {
        public static ResolveIdtaSpecs Static = new ResolveIdtaSpecs();

        public IdtaSpecRecordList Records = new IdtaSpecRecordList();

        public ResolveIdtaSpecs()
        {
            // Part 1 - v3.1.2
            var list = new IdtaSpecRecordList(Part.Part1, Version.V3_1_2);
            var prefix = "https://industrialdigitaltwin.io/aas-specifications/IDTA-01001/v3.1.2/spec-metamodel/";
            
            Records.Add(Concept.AssetInformation,               prefix, "core.html#asset-information-attributes");
            Records.Add(Concept.Resource,                       prefix, "core.html#resource-attributes");
            Records.Add(Concept.AssetKind,                      prefix, "core.html#asset-kind-attributes");
            Records.Add(Concept.SpecificAssetId,                prefix, "core.html#specific-asset-id-attributes");
            Records.Add(Concept.Submodel,                       prefix, "core.html#submodel-attributes");
            Records.Add(Concept.SubmodelElement,                prefix, "core.html#submodel-element-attributes");
            Records.Add(Concept.AdministrativeInformation,      prefix, "common.html#administrative-information-attributes");
            Records.Add(Concept.HasDataSpecification,           prefix, "common.html#has-data-specification-attributes");
            Records.Add(Concept.HasExtensions,                  prefix, "common.html#_has_extensions_attributes");
            Records.Add(Concept.HasKind,                        prefix, "common.html#_has_kind_attributes");
            Records.Add(Concept.HasSemantics,                   prefix, "common.html#has-semantics-attributes");
            Records.Add(Concept.Identifiable,                   prefix, "common.html#identifiable-attributes");
            Records.Add(Concept.Qualifiable,                    prefix, "common.html#qualifiable-attributes");
            Records.Add(Concept.Qualifier,                      prefix, "common.html#qualifier-attributes");
            Records.Add(Concept.Referable,                      prefix, "common.html#referable-attributes");
            Records.Add(Concept.CD,                             prefix, "concept-description.html");
            Records.Add(Concept.DataSpecs,                      
                "https://industrialdigitaltwin.io/aas-specifications/IDTA-01001/v3.1.2/data-specifications.html");
        }

        public string Resolve(Concept concept, Part part, Version? version)
        {
            IdtaSpecRecord found = null;
            foreach (var rec in Records)
                if (rec.Concept == concept && rec.Part == part)
                {
                    // easy
                    if (version.HasValue && rec.Version == version)
                        return rec.Uri;

                    // maximum
                    if (found == null || (int)rec.Version > (int)found.Version)
                        found = rec;
                }
            return found?.Uri;
        }

    }
}
