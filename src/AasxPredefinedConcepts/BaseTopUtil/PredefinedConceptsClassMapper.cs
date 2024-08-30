/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasCore.Aas3_0;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Extensions;
using Namotion.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using Aas = AasCore.Aas3_0;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// AAS cardinality for predefined concepts
    /// </summary>
    public enum AasxPredefinedCardinality { ZeroToOne = 0, One, ZeroToMany, OneToMany };

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class AasConceptAttribute : Attribute
    {
        public string Cd { get; set; }
        public string SupplSemId { get; set; }
        
        public AasxPredefinedCardinality Card { get; set; }

        public AasConceptAttribute()
        {
        }
    }

    /// <summary>
    /// This class is used in auto-generated files by <c>AasxPredefinedConcepts.PredefinedConceptsClassMapper</c>
    /// It holds min/max information with particular type information.
    /// </summary>
    public class AasClassMapperRange<T>
    {
        public T Min;
        public T Max;
    }

    /// <summary>
    /// This class is used in auto-generated files by <c>AasxPredefinedConcepts.PredefinedConceptsClassMapper</c>
    /// It "replicates" the important information.
    /// </summary>
    public class AasClassMapperBlob
    {
        public string Value = null;
        public string ContentType = "";
    }

    /// <summary>
    /// This class is used in auto-generated files by <c>AasxPredefinedConcepts.PredefinedConceptsClassMapper</c>
    /// It "replicates" the important information.
    /// </summary>
    public class AasClassMapperFile
    {
        public string Value = null;
        public string ContentType = "";
    }

    /// <summary>
    /// This class is used in auto-generated files by <c>AasxPredefinedConcepts.PredefinedConceptsClassMapper</c>
    /// </summary>
    public class AasClassMapperHintedReference
    {
        /// <summary>
        /// The original reference.
        /// </summary>
        public Aas.IReference Value;

        /// <summary>
        /// If possible, links directly to the target of the reference.
        /// </summary>
        public Aas.IReferable ValueHint;

        /// <summary>
        /// This allows accessing the full AAS element.
        /// </summary>
        public AasClassMapperInfo __Info__ = null;
    }

    /// <summary>
    /// This class is used in auto-generated files by <c>AasxPredefinedConcepts.PredefinedConceptsClassMapper</c>
    /// </summary>
    public class AasClassMapperHintedRelation
    {
        /// <summary>
        /// The original reference of the first reference.
        /// </summary>
        public Aas.IReference First;

        /// <summary>
        /// If possible, links directly to the target of the first reference.
        /// </summary>
        public Aas.IReferable FirstHint;

        /// <summary>
        /// The original reference of the first reference.
        /// </summary>
        public Aas.IReference Second;

        /// <summary>
        /// If possible, links directly to the target of the second reference.
        /// </summary>
        public Aas.IReferable SecondHint;

        /// <summary>
        /// This allows accessing the full AAS element.
        /// </summary>
        public AasClassMapperInfo __Info__ = null;
    }

    /// <summary>
    /// If this class is present in an mapped class, then (source) information will be added to the
    /// data objects.
    /// </summary>
    public class AasClassMapperInfo
    {
        public Aas.IReferable Referable;

        public AasClassMapperInfo (Aas.IReferable rf = null)
        {
            Referable = rf;
        }
    }

    /// <summary>
    /// This class provides methods to derive a set of C# class definitions from a SMT definition and
    /// to create runtime instances from it based on a concrete SM.
    /// </summary>
    public class PredefinedConceptsClassMapper
	{
        //
        // Export C# classes
        //

        private static string CSharpTypeFrom(Aas.DataTypeDefXsd valueType)
        {
            switch (valueType)
            {
                case Aas.DataTypeDefXsd.Boolean:
                    return "bool";

                case Aas.DataTypeDefXsd.Byte:
                    return "byte";

                case Aas.DataTypeDefXsd.Date:
                case Aas.DataTypeDefXsd.DateTime:
                case Aas.DataTypeDefXsd.Time:
                    return "DateTime";

                case Aas.DataTypeDefXsd.Float:
                    return "float";

                case Aas.DataTypeDefXsd.Double:
                    return "double";

                case Aas.DataTypeDefXsd.Int:
                case Aas.DataTypeDefXsd.Integer:
                    return "int";

                case Aas.DataTypeDefXsd.Long:
                    return "long";

                case Aas.DataTypeDefXsd.NegativeInteger:
                case Aas.DataTypeDefXsd.NonPositiveInteger:
                    return "int";

                case Aas.DataTypeDefXsd.NonNegativeInteger:
                case Aas.DataTypeDefXsd.PositiveInteger:
                    return "unsigned int";

                case Aas.DataTypeDefXsd.Short:
                    return "short";

                case Aas.DataTypeDefXsd.UnsignedByte:
                    return "unsigned byte";

                case Aas.DataTypeDefXsd.UnsignedInt:
                    return "unsigned int";

                case Aas.DataTypeDefXsd.UnsignedLong:
                    return "usingned long";

                case Aas.DataTypeDefXsd.UnsignedShort:
                    return "unsigned short";
            }
            
            return "string";
        }

		private static void ExportCSharpMapperSingleItems(
            string indent, Aas.IEnvironment env, Aas.IReferable rf, System.IO.StreamWriter snippets,
            bool noEmptyLineFirst = false)
        {
			// access
			if (snippets == null || env == null || rf == null)
				return;

            //
            // require CD
            //

            Aas.IConceptDescription cd = null;
            if (rf is Aas.IHasSemantics ihs)
			    cd = env.FindConceptDescriptionByReference(ihs.SemanticId);

            var cdff = AdminShellUtil.FilterFriendlyName(cd?.IdShort, pascalCase: true);

			var cdRef = cd?.GetCdReference()?.ToStringExtended(format: 2);

            //
            // pretty idShort
            //

			var idsff = AdminShellUtil.FilterFriendlyName(rf.IdShort, pascalCase: true);
            if (idsff.HasContent() != true)
                return;

            if (idsff.Contains("ChangeTitle")) { ; }

            //
            // check Qualifiers/ Extensions
            //

            FormMultiplicity card = FormMultiplicity.One;
            if (rf is Aas.IQualifiable iqf)
            {
                var tst = AasFormUtils.GetCardinality(iqf.Qualifiers);
                if (tst.HasValue)
                    card = tst.Value;
            }

            var cardSt = card.ToString();

            //
            // lambda for attribute declaration
            //

            Action<string, bool, string> declareLambda = (dt, isScalar, instance) =>
            {
                // empty line ahead
                if (!noEmptyLineFirst)
                    snippets.WriteLine();

                // write attribute's attribute                
                if (cdRef?.HasContent() == true)
                    snippets.WriteLine($"{indent}[AasConcept(Cd = \"{cdRef}\", " +
                        $"Card = AasxPredefinedCardinality.{cardSt})]");

                // write attribute itself
                if (isScalar)
                {
                    var nullOp = (dt == "string") ? "" : "?";

                    if (card == FormMultiplicity.ZeroToOne)
                        snippets.WriteLine($"{indent}public {dt}{nullOp} {instance};");
                    else
                    if (card == FormMultiplicity.One)
                        snippets.WriteLine($"{indent}public {dt} {instance};");
                    else
                        snippets.WriteLine($"{indent}public List<{dt}> {instance} = new List<{dt}>();");
                }
                else
                {
                    if (card == FormMultiplicity.ZeroToOne)
                        snippets.WriteLine($"{indent}public {dt} {instance} = null;");
                    else
                    if (card == FormMultiplicity.One)
                        snippets.WriteLine($"{indent}public {dt} {instance} = new {dt}();");
                    else
                        snippets.WriteLine($"{indent}public List<{dt}> {instance} = new List<{dt}>();");
                }
            };

            //
            // Property
            //

            if (rf is Aas.IProperty prop)
            {
                var dt = CSharpTypeFrom(prop.ValueType);
                declareLambda(dt, true, idsff);
            }

            //
            // Range
            //

            if (rf is Aas.IRange rng)
            {
                var dt = CSharpTypeFrom(rng.ValueType);
                declareLambda($"AasClassMapperRange<{dt}>", false, idsff);
            }

            //
            // File
            //

            if (rf is Aas.IFile fl)
            {
                declareLambda($"AasClassMapperFile", false, idsff);
            }

            //
            // MultiLanguageProperty
            //

            if (rf is Aas.IMultiLanguageProperty mlp)
            {
                declareLambda("List<ILangStringTextType>", false, idsff);
            }

            //
            // Reference
            //

            if (rf is Aas.IReferenceElement rfe)
            {
                declareLambda("AasClassMapperHintedReference", false, idsff);
            }

            //
            // Relation
            //

            if (rf is Aas.IRelationshipElement rle)
            {
                declareLambda("AasClassMapperHintedRelation", false, idsff);
            }

            //
            // SMC, SML ..
            //

            if ((  rf is Aas.Submodel
                || rf is Aas.SubmodelElementCollection
                || rf is Aas.SubmodelElementList)
                && cdRef?.HasContent() == true)
            {
                // in sequence: class first
#if do_not_do
                snippets.WriteLine($"{indent}public class {cdff} {{");

                foreach (var x in rf.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                        ExportCSharpMapper("" + indent + "    ", env, sme, snippets);

				snippets.WriteLine($"{indent}}}");
#endif

                declareLambda($"CD_{cdff}", false, idsff);
			}
		}

        /// <summary>
        /// The contents of this class are based on one or multiple SMCs (SML..), however
        /// the class itself is associated with the associated CD of the SMC (SML..), therefore
        /// it is intended to aggregate member definitions.
        /// Duplicate members are avoided. Members are found to be duplicate, if IdShort and
        /// SemanticId are the same.
        /// </summary>
        private class ExportCSharpClassDef
        {
            /// <summary>
            /// The respective SM, SMC, SML ..
            /// </summary>
            public Aas.IReferable Rf = null;

            /// <summary>
            /// The associated CD.
            /// </summary>
            public Aas.IConceptDescription Cd = null;

            /// <summary>
            /// Superset of representative memebers.
            /// </summary>
            public List<Aas.ISubmodelElement> Members = new List<Aas.ISubmodelElement>();

			public ExportCSharpClassDef(Aas.IEnvironment env, Aas.IReferable rf)
			{
				Rf = rf;
                if (rf is Aas.IHasSemantics ihs)
                    Cd = env?.FindConceptDescriptionByReference(ihs.SemanticId);

                if (rf == null)
                    return;

                foreach (var x in rf.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                        Members.Add(sme);
			}

            public void EnrichMembersFrom(ExportCSharpClassDef cld)
            {
                if (cld?.Members == null)
                    return;

				foreach (var x in cld.Members)
					if (x is Aas.ISubmodelElement sme)
                    {
                        // check if member with same name and CD is already present
                        var found = false;
                        foreach (var em in Members)
                            if (em?.IdShort?.HasContent() == true
                                && em.IdShort == sme?.IdShort
                                && (em.SemanticId?.IsValid() != true 
                                    || em.SemanticId?.Matches(sme?.SemanticId, MatchMode.Relaxed) == true))
                                found = true;
                        if (!found)
                            Members.Add(sme);
                    }
			}
		}

		private static void ExportCSharpMapperOnlyClasses(
	        string indent, Aas.IEnvironment env, Aas.ISubmodel sm, System.IO.StreamWriter snippets,
            bool addInfoObj = false)
		{
            // list of class definitions (not merged, yet)
            var elems = new List<ExportCSharpClassDef>();
            foreach (var sme in sm.SubmodelElements?.FindDeep<Aas.ISubmodelElement>((sme) => sme.IsStructured()))
				elems.Add(new ExportCSharpClassDef(env, sme));

			// list of merged class defs
			var distElems = new List<ExportCSharpClassDef>();
            foreach (var x in elems.GroupBy((cld) => cld.Cd))
            {
                var l = x.ToList();
                for (int i = 1; i < l.Count; i++)
                    l[0].EnrichMembersFrom(l[i]);
                distElems.Add(l[0]);
            }

			// add Submodel at last, to be sure it is distinct
			distElems.Add(new ExportCSharpClassDef(env, sm));
            // distElems.Reverse();

            // try to output classed, do not recurse by itself
			foreach (var cld in distElems)
			{
				// gather infos
				var cdff = AdminShellUtil.FilterFriendlyName(cld.Cd?.IdShort, pascalCase: true);
                var cdRef = cld?.Cd?.GetCdReference()?.ToStringExtended(format: 2);

                // no empty class
                if (cdff?.HasContent() != true)
                    continue;

                // write out class
                snippets.WriteLine();

                if (cdRef?.HasContent() == true)
                    snippets.WriteLine($"{indent}[AasConcept(Cd = \"{cdRef}\")]");

                snippets.WriteLine($"{indent}public class CD_{cdff}");
                snippets.WriteLine($"{indent}{{");

                if (cdff == "MappingSourceSinkRelations")
                {
                    ;
                }

                if (cld.Members != null)
                {
                    var noEmptyLineFirst = true;
                    foreach (var x in cld.Members)
                        if (x is Aas.ISubmodelElement sme)
                        {
                            ExportCSharpMapperSingleItems("" + indent + "    ", env, sme, snippets,
                                noEmptyLineFirst: noEmptyLineFirst);
                            noEmptyLineFirst = false;
                        }
                }

                if (addInfoObj)
                {
                    snippets.WriteLine($"");
                    snippets.WriteLine($"{indent}    // auto-generated informations");
                    snippets.WriteLine($"{indent}    public AasClassMapperInfo __Info__ = null;");
                }

                snippets.WriteLine($"{indent}}}");
			}
		}

		public static void ExportCSharpClassDefs(Aas.IEnvironment env, Aas.ISubmodel sm, System.IO.StreamWriter snippets)
        {
            // access
            if (snippets == null || env == null || sm == null)
                return;

            var head = AdminShellUtil.CleanHereStringWithNewlines(
				@"
                /*
                Copyright (c) 2018-2023 Festo SE & Co. KG
                <https://www.festo.com/net/de_de/Forms/web/contact_international>
                Author: Michael Hoffmeister

                This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

                This source code may use other Open Source software components (see LICENSE.txt).

                This source code was auto-generated by the AASX Package Explorer.
                */

                using AasxIntegrationBase;
                using AdminShellNS;
                using Extensions;
                using System;
                using System.Collections.Generic;
                using Aas = AasCore.Aas3_0;");
            snippets.WriteLine(head);
            snippets.WriteLine("");

			snippets.WriteLine($"namespace AasxPredefinedConcepts.{AdminShellUtil.FilterFriendlyName(sm?.IdShort)} {{");

			ExportCSharpMapperOnlyClasses("    ", env, sm, snippets,
                addInfoObj: true);
			
            // ExportCSharpMapperSingleItems("    ", env, sm, snippets);

            snippets.WriteLine($"}}");
		}

        //
        // Parse AASX structures
        //

        private class ElemAttrInfo
        {
            public object Obj;
            public FieldInfo Fi;
            public AasConceptAttribute Attr;
        }

        private static object CreateRangeObjectSpecific(Type genericType0, Aas.ISubmodelElement sme)
        {
            // access
            if (genericType0 == null || sme == null || !(sme is Aas.IRange rng))
                return null;

            // create generic instance
            // see: https://stackoverflow.com/questions/4194033/adding-items-to-listt-using-reflection
            var objTyp = genericType0;
            var IListRef = typeof(AasClassMapperRange<>);
            Type[] IListParam = { objTyp };
            object rngObj = Activator.CreateInstance(IListRef.MakeGenericType(IListParam));

            // set
            var rngType = rngObj.GetType();
            AdminShellUtil.SetFieldLazyValue(rngType.GetField("Min"), rngObj, "" + rng.Min);
            AdminShellUtil.SetFieldLazyValue(rngType.GetField("Max"), rngObj, "" + rng.Max);

            // ok
            return rngObj;
        }

        // TODO (MIHO, 2024-01-04): Move to AdminShellUtil ..
        private static void SetFieldLazyFromSme(FieldInfo f, object obj, Aas.ISubmodelElement sme,
            Func<Aas.IReference, Aas.IReferable> lambdaLookupReference = null)
        {
            // access
            if (f == null || obj == null || sme == null)
                return;

            // identify type
            var t = AdminShellUtil.GetTypeOrUnderlyingType(f.FieldType);

            //
            // Range
            //

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(AasClassMapperRange<>)
                && t.GenericTypeArguments.Length >= 1
                && sme is Aas.IRange rng)
            {
                // create generic instance
                var rngObj = CreateRangeObjectSpecific(t.GenericTypeArguments[0], sme);

                // set it
                f.SetValue(obj, rngObj);

                // done
                return;
            }

            //
            // Blob
            //

            if (t == typeof(AasClassMapperBlob)
                && sme is Aas.IBlob blb)
            {
                // create generic instance
                var flObj = new AasClassMapperFile()
                {
                    Value = System.Text.Encoding.Default.GetString(blb.Value),
                    ContentType = blb.ContentType
                };

                // set it
                f.SetValue(obj, flObj);

                // done
                return;
            }

            //
            // File
            //

            if (t == typeof(AasClassMapperFile)
                && sme is Aas.IFile fl)
            {
                // create generic instance
                var flObj = new AasClassMapperFile()
                {
                    Value = fl.Value,
                    ContentType = fl.ContentType
                };

                // set it
                f.SetValue(obj, flObj);

                // done
                return;
            }

            //
            // List<ILangStringTextType>
            // 

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(List<>)
                && t.GenericTypeArguments.Count() > 0
                && t.GenericTypeArguments[0].IsAssignableTo(typeof(IAbstractLangString))
                && sme is Aas.IMultiLanguageProperty mlp
                && mlp.Value != null)
            {
                // create usable values
                var v = mlp.Value.Copy();

                // set it
                f.SetValue(obj, v);

                // done
                return;
            }

            //
            // Reference
            //

            if (t == typeof(AasClassMapperHintedReference)
                && sme is Aas.ReferenceElement rfe)
            {
                // create instance
                var rfeObj = new AasClassMapperHintedReference()
                {
                    Value = rfe.Value,
                    ValueHint = lambdaLookupReference?.Invoke(rfe.Value),
                    __Info__ = new AasClassMapperInfo(sme)
                };

                // set it
                f.SetValue(obj, rfeObj);

                // done
                return;
            }

            //
            // Relation
            //

            if (t == typeof(AasClassMapperHintedRelation)
                && sme is Aas.RelationshipElement rle)
            {
                // create instance
                var rleObj = new AasClassMapperHintedRelation()
                {
                    First = rle.First,
                    FirstHint = lambdaLookupReference?.Invoke(rle.First),
                    Second = rle.Second,
                    SecondHint = lambdaLookupReference?.Invoke(rle.Second),
                    __Info__ = new AasClassMapperInfo(sme)
                };

                // set it
                f.SetValue(obj, rleObj);

                // done
                return;
            }

            //
            // Default
            //

            {
                AdminShellUtil.SetFieldLazyValue(f, obj, sme.ValueAsText());
            }
        }

        public static void AddToListLazySme(FieldInfo f, object obj, Aas.ISubmodelElement sme,
            Func<Aas.IReference, Aas.IReferable> lambdaLookupReference = null)
        {
            // access
            if (f == null || obj == null || sme == null)
                return;

            // identify type
            var t = AdminShellUtil.GetTypeOrUnderlyingType(f.FieldType);
            var tGen = AdminShellUtil.GetTypeOrUnderlyingType(f.FieldType, resolveGeneric: true);

            //
            // Range
            //

            /* TODO (MIHO, 2024-02-29): I am pretty sure that this "if" needs to be
             * reworked according to the file section below */

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(AasClassMapperRange<>)
                && sme is Aas.IRange rng)
            {
                // create generic instance
                var rngObj = CreateRangeObjectSpecific(t.GenericTypeArguments[0], sme);

                // add it
                var listObj = f.GetValue(obj);
                listObj.GetType().GetMethod("Add").Invoke(listObj, new[] { rngObj });

                // ok
                return;
            }

            //
            // Blob
            //

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(List<>)
                && t.GenericTypeArguments.Count() > 0
                && t.GenericTypeArguments[0].IsAssignableTo(typeof(AasClassMapperFile))
                && sme is Aas.IBlob blb)
            {
                // create generic instance
                var flObj = new AasClassMapperFile()
                {
                    Value = System.Text.Encoding.Default.GetString(blb.Value),
                    ContentType = blb.ContentType
                };

                // add it
                var listObj = f.GetValue(obj);
                listObj.GetType().GetMethod("Add").Invoke(listObj, new[] { flObj });

                // ok
                return;
            }

            //
            // File
            //

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(List<>)
                && t.GenericTypeArguments.Count() > 0
                && t.GenericTypeArguments[0].IsAssignableTo(typeof(AasClassMapperFile))
                && sme is Aas.IFile fl)
            {
                // create generic instance
                var flObj = new AasClassMapperFile()
                {
                    Value = fl.Value,
                    ContentType = fl.ContentType
                };

                // add it
                var listObj = f.GetValue(obj);
                listObj.GetType().GetMethod("Add").Invoke(listObj, new[] { flObj });

                // ok
                return;
            }

            //
            // List<ILangStringTextType>
            // 

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(List<>)
                && t.GenericTypeArguments.Count() > 0
                && t.GenericTypeArguments[0].IsAssignableTo(typeof(IAbstractLangString))
                && sme is Aas.IMultiLanguageProperty mlp)
            {
                /* TODO (MIHO, 2024-02-28): check if this case could happen:
                   Having a List<List<IAbstractLangString>> */
                throw new NotImplementedException(
                    "PredefinedConceptClassMapper to add List<ILangStringTextType>");
            }

            //
            // Reference
            //

            if (tGen == typeof(AasClassMapperHintedReference)
                && sme is Aas.ReferenceElement rfe)
            {
                // create instance
                var rfeObj = new AasClassMapperHintedReference()
                {
                    Value = rfe.Value,
                    ValueHint = lambdaLookupReference?.Invoke(rfe.Value),
                    __Info__ = new AasClassMapperInfo(sme)
                };

                // add it
                var listObj = f.GetValue(obj);
                listObj.GetType().GetMethod("Add").Invoke(listObj, new[] { rfeObj });

                // done
                return;
            }

            //
            // Relation
            //

            if (tGen == typeof(AasClassMapperHintedRelation)
                && sme is Aas.RelationshipElement rle)
            {
                // create instance
                var rleObj = new AasClassMapperHintedRelation()
                {
                    First = rle.First,
                    FirstHint = lambdaLookupReference?.Invoke(rle.First),
                    Second = rle.Second,
                    SecondHint = lambdaLookupReference?.Invoke(rle.Second),
                    __Info__ = new AasClassMapperInfo(sme)
                };

                // add it
                var listObj = f.GetValue(obj);
                listObj.GetType().GetMethod("Add").Invoke(listObj, new[] { rleObj });

                // done
                return;
            }

            //
            // Default
            //

            {
                /* TODO (MIHO, 2024-02-29): check if it is OK to *NOT* check, if the                
                 * list is NULL */
                var listObj = f.GetValue(obj);
                AdminShellUtil.AddToListLazyValue(listObj, sme.ValueAsText());
            }
        }

        private static void ParseAasElemFillData(ElemAttrInfo eai, Aas.ISubmodelElement sme,
            Func<Aas.IReference, Aas.IReferable> lambdaLookupReference = null)
        {
            // access
            if (eai?.Fi == null || eai.Attr == null || sme == null)
                return;

            if (sme?.IdShort == "ItemCategory") { ; }
           
            // straight?
            if (!sme.IsStructured())
            {
                if (eai.Attr.Card == AasxPredefinedCardinality.One)
                {
                    // scalar value
                    SetFieldLazyFromSme(eai.Fi, eai.Obj, sme, lambdaLookupReference);
                }
                else
                if (eai.Attr.Card == AasxPredefinedCardinality.ZeroToOne)
                {
                    // sure to have a nullable type
                    SetFieldLazyFromSme(eai.Fi, eai.Obj, sme, lambdaLookupReference);
                }
                else
                if ((eai.Attr.Card == AasxPredefinedCardinality.ZeroToMany
                    || eai.Attr.Card == AasxPredefinedCardinality.OneToMany)
                    // && eai.Obj.GetType().IsGenericType
                    // && eai.Obj.GetType().GetGenericTypeDefinition() == typeof(List<>))
                    && eai.Fi.FieldType.IsGenericType
                    && eai.Fi.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // sure to have a (instantiated) List<scalar>
                    AddToListLazySme(eai.Fi, eai.Obj, sme, lambdaLookupReference);
                }
            }
            else
            {
                if (eai.Attr.Card == AasxPredefinedCardinality.One)
                {
                    // assume a already existing object
                    var childObj = eai.Fi.GetValue(eai.Obj);

                    // recurse to fill in
                    ParseAasElemsToObject(sme, childObj, lambdaLookupReference);
                }
                else
                if (eai.Attr.Card == AasxPredefinedCardinality.ZeroToOne)
                {
                    // get value first, shall not be present
                    var childObj = eai.Fi.GetValue(eai.Obj);
                    if (childObj != null)
                        throw new Exception(
                            $"ParseAasElemFillData: [0..1] instance for {eai.Fi.FieldType.Name}> already present!");

                    // ok, make new, add
                    childObj = Activator.CreateInstance(eai.Fi.FieldType);
                    eai.Fi.SetValue(eai.Obj, childObj);

                    // recurse to fill in
                    ParseAasElemsToObject(sme, childObj, lambdaLookupReference);
                }
                else
                if ((eai.Attr.Card == AasxPredefinedCardinality.ZeroToMany
                    || eai.Attr.Card == AasxPredefinedCardinality.OneToMany)
                    && eai.Fi.FieldType.IsGenericType
                    && eai.Fi.FieldType.GetGenericTypeDefinition() == typeof(List<>)
                    && eai.Fi.FieldType.GenericTypeArguments.Length > 0
                    && eai.Fi.FieldType.GenericTypeArguments[0] != null)
                {
                    // create a new object instance
                    var childObj = Activator.CreateInstance(eai.Fi.FieldType.GenericTypeArguments[0]);

                    // add to list
                    var listObj = eai.Fi.GetValue(eai.Obj);
                    listObj.GetType().GetMethod("Add").Invoke(listObj, new [] { childObj });

                    // recurse to fill in
                    ParseAasElemsToObject(sme, childObj, lambdaLookupReference);
                }

                // recurse

            }
        }

        /// <summary>
        /// Parse information from the AAS elements (within) <c>root</c> to the 
        /// attributed class referenced by <c>obj</c>. Reflection dictates the
        /// recursion into sub-classes.
        /// </summary>
        public static void ParseAasElemsToObject(Aas.IReferable root, object obj,
            Func<Aas.IReference, Aas.IReferable> lambdaLookupReference = null)
        {
            // access
            if (root == null || obj == null)
                return;

            // collect information driven by reflection
            var eais = new List<ElemAttrInfo>();

            // find fields for this object
            var t = obj.GetType();
            var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in l)
            {
                // special case
                if (f.FieldType == typeof(AasClassMapperInfo))
                {
                    var info = new AasClassMapperInfo() { Referable = root };
                    f.SetValue(obj, info);
                    continue;
                }

                // store for a bit later processing
                var a = f.GetCustomAttribute<AasConceptAttribute>();
                if (a != null)
                {
                    eais.Add(new ElemAttrInfo() { Obj = obj, Fi = f, Attr = a });
                }
            }

            // now try to fill information
            foreach (var eai in eais)
            {
                // try find sme in Rf
                foreach (var x in root.DescendOnce())
                    if (x is Aas.ISubmodelElement sme)
                    {
                        var hit = sme?.SemanticId?.MatchesExactlyOneKey(
                            new Aas.Key(Aas.KeyTypes.GlobalReference, eai?.Attr?.Cd),
                            matchMode: MatchMode.Relaxed) == true;

                        if (hit && eai?.Attr?.SupplSemId?.HasContent() == true)
                            hit = hit && sme?.SupplementalSemanticIds?.MatchesAnyWithExactlyOneKey(
                                new Aas.Key(Aas.KeyTypes.GlobalReference, eai?.Attr?.SupplSemId),
                            matchMode: MatchMode.Relaxed) == true;

                        if (hit)
                            ParseAasElemFillData(eai, sme, lambdaLookupReference);
                    }

            }
        }

        public static Aas.ISubmodelElement SerializeToAasElem(object obj,
            AasConceptAttribute externalAttr = null,
            AasConceptAttribute listElemAttr = null,
            string externalFieldName = null,
            string listElemName = null)
        {
            // determine type of object
            if (obj == null)
                return null;
            var t = obj.GetType();

            var nameIdShort = "" + ((externalFieldName != null) ? externalFieldName : t.Name);

            if (t.Name == "AffectedPartNumbers") { ; }
            
            // if there is not semantic, there is no point to care about?
            var attrCd = (externalAttr != null) ? externalAttr 
                : t.GetCustomAttribute<AasConceptAttribute>();
            if (attrCd == null)
                return null;
            var semId = new Aas.Reference(ReferenceTypes.ExternalReference,
                        (new[] { new Aas.Key(KeyTypes.GlobalReference, attrCd.Cd) })
                        .Cast<Aas.IKey>().ToList());

            //
            // Scalar -> Property
            //

            if (!t.IsGenericType
                && (new[] { typeof(string), typeof(int), typeof(double) }).Contains(t))
            {
                // scalar -> property
                var prop = new Aas.Property(
                    idShort: nameIdShort,
                    semanticId: semId,
                    valueType: DataTypeDefXsd.String,
                    value: "" + System.Convert.ToString(obj, CultureInfo.InvariantCulture));
                return prop;
            }

            //
            // List<ILangStringTextType> -> MLP
            // 

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(List<>)
                && t.GenericTypeArguments.Count() > 0
                && t.GenericTypeArguments[0].IsAssignableTo(typeof(IAbstractLangString)))
            {
                var mlp = new Aas.MultiLanguageProperty(
                    idShort: nameIdShort,
                    semanticId: semId,
                    value: obj as List<Aas.ILangStringTextType>);
                return mlp;
            }

            //
            // List of other objects -> SML over SME
            //

            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(List<>)
                && t.GenericTypeArguments.Count() > 0)
            {
                // SML
                var sml = new Aas.SubmodelElementList(
                    idShort: nameIdShort,
                    semanticId: semId,
                    typeValueListElement: AasSubmodelElements.SubmodelElementCollection,
                    value: new List<ISubmodelElement>());

                // can cast to IEnumerable
                var ie = obj as IEnumerable;
                if (ie != null)
                    foreach (var io in ie)
                    {
                        // get an individual list element
                        // (this time: take the attribute/ name from the element itself!)
                        var sme = SerializeToAasElem(
                            io, 
                            externalAttr: listElemAttr, 
                            externalFieldName: "" /* listElemName */);
                        if (sme != null)
                            sml.Value.Add(sme);
                    }

                // ok
                return sml;
            }

            //
            // Class -> SMC
            //

            if (!t.IsGenericType
                && t.IsClass)
            {
                // do a reflection loop first in order to do some statistics
                int numMapInfo = 0, numOther = 0;
                var listCdFields = new List<FieldInfo>();
                var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in l)
                {
                    if (f.FieldType == typeof(AasClassMapperInfo))
                    {
                        numMapInfo++;
                        continue;
                    }

                    if (null != f.GetCustomAttribute<AasConceptAttribute>())
                    {
                        listCdFields.Add(f);
                        continue;
                    }

                    numOther++;
                }

                // Now can decide, if there is a special case: class with exactly one List inside.
                // In this case, no __sourrounding__ class is required
                var smlCase = false;
                if (numMapInfo == 1 && listCdFields.Count == 1 && numOther == 0)
                {
                    var t2 = listCdFields[0].FieldType;
                    if (t2.IsGenericType
                        && t2.GetGenericTypeDefinition() == typeof(List<>)
                        && t2.GenericTypeArguments.Count() > 0)
                    {
                        smlCase = true;
                    }
                }

                // now, finally
                if (smlCase)
                {
                    // can pass this single field on to get constructed as SML
                    // Note: the cd attribute / name comes from the superior field!
                    var f2 = listCdFields[0];
                    return SerializeToAasElem(
                        f2.GetValue(obj),
                        externalAttr: externalAttr,
                        listElemAttr: f2.GetCustomAttribute<AasConceptAttribute>(),                        
                        externalFieldName: externalFieldName,
                        listElemName: f2.Name);

                    // MIHO considers this already as a design flaw of the export of
                    // the mappings: the double classes (class + class with list) seem
                    // to be pointless.
                    // A rework would affect all the (manually adjusted) exported classes
                    // and the source code using the mapped classes and is therefore 
                    // desirable but time consuming
                    /* TODO (MIHO, 2024-03-09): do the above mentioned rework for exporting
                     * the mapping classes */
                }
                else
                {
                    // try construct a class == SMC
                    var smc = new Aas.SubmodelElementCollection(
                        idShort: nameIdShort,
                        semanticId: semId,
                        value: new List<ISubmodelElement>());

                    // identify fields of the obj
                    l = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var f in l)
                    {
                        // special case
                        if (f.FieldType == typeof(AasClassMapperInfo))
                        {
                            continue;
                        }

                        if (f.Name == "AffectedPartNumbers") { ; }

                        // try create an SME
                        var sme = SerializeToAasElem(f.GetValue(obj),
                            externalAttr: f.GetCustomAttribute<AasConceptAttribute>(),                            
                            externalFieldName: f.Name);
                        if (sme != null)
                            smc.Value.Add(sme);
                    }

                    return smc;
                }
            }

            return null;
        }
    }
}