/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;
using System.Collections.Generic;

namespace Extensions
{
    // TODO (Jui, 2022-12-21): I do not know, if to put the List<> extension here or in a separate file
    public static class ExtendListOfEmbeddedDataSpecification
    {
        public static bool IsOneBlank(this List<IEmbeddedDataSpecification> list)
        {
            return
                list != null
                && list.Count == 1
                && list[0]?.DataSpecification?.IsOneBlank() == true
                && list[0].DataSpecificationContent is DataSpecificationBlank;
        }


        public static IEmbeddedDataSpecification FindFirstIEC61360Spec(this List<IEmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationIec61360
                    || eds?.DataSpecification?.MatchesExactlyOneKey(
                        ExtendIDataSpecificationContent.GetKeyForIec61360()) == true)
                    return eds;
            return null;
        }

        public static DataSpecificationIec61360 GetIEC61360Content(this List<IEmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationIec61360 dsiec)
                    return dsiec;
            return null;
        }

        //TODO (jtikekar, 0000-00-00): DataSpecificationPhysicalUnit
#if SupportDataSpecificationPhysicalUnit
        public static DataSpecificationPhysicalUnit GetPhysicalUnitContent(this List<EmbeddedDataSpecification> list)
        {
            foreach (var eds in list)
                if (eds?.DataSpecificationContent is DataSpecificationPhysicalUnit dspu)
                    return dspu;
            return null;
        } 
#endif
    }

    public static class ExtendEmbeddedDataSpecification
    {
        // see: https://github.com/eclipse-aaspe/aaspe/issues/196
        // assume case sensitivity, follow the spec 
        public const string UriDataSpecificationIEC61360 =
            "https://admin-shell.io/DataSpecificationTemplates/DataSpecificationIec61360/3/0";

        public static EmbeddedDataSpecification ConvertFromV20(this EmbeddedDataSpecification embeddedDataSpecification, AasxCompatibilityModels.AdminShellV20.EmbeddedDataSpecification sourceEmbeddedSpec)
        {
            if (sourceEmbeddedSpec != null)
            {
                if (sourceEmbeddedSpec.dataSpecification != null)
                {
                    embeddedDataSpecification.DataSpecification = ExtensionsUtil.ConvertReferenceFromV20(sourceEmbeddedSpec.dataSpecification, ReferenceTypes.ExternalReference);

                    // TODO (MIHO, 2022-19-12): check again, see questions
                    var oldid = new[] {
                    "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360/2/0",
					"http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360",
					"www.admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360"
				};
                var newid = UriDataSpecificationIEC61360;

                    // map all "usable" old ids to new one ..
                    foreach (var oi in oldid)
                        if (sourceEmbeddedSpec.dataSpecification?.Matches("", false, "IRI", oi,
                            AasxCompatibilityModels.AdminShellV20.Key.MatchMode.Identification) == true)
                        {
                            embeddedDataSpecification.DataSpecification.Keys[0].Value = newid;
                        } 
                }

                if (sourceEmbeddedSpec.dataSpecificationContent != null)
                {
                    if (sourceEmbeddedSpec.dataSpecificationContent?.dataSpecificationIEC61360 != null)
                    {
                        embeddedDataSpecification.DataSpecificationContent =
                            new DataSpecificationIec61360(null).ConvertFromV20(
                                sourceEmbeddedSpec.dataSpecificationContent.dataSpecificationIEC61360);
                    } 
                }
            }
            
            return embeddedDataSpecification;
        }

        public static EmbeddedDataSpecification CreateIec61360WithContent(DataSpecificationIec61360 content = null)
        {
            if (content == null)
                content = new DataSpecificationIec61360(
                    new List<ILangStringPreferredNameTypeIec61360>());

            var res = new EmbeddedDataSpecification(
                new Reference(ReferenceTypes.ExternalReference,
                    new List<IKey>(new[] { ExtendIDataSpecificationContent.GetKeyForIec61360() })),
                content);
            return res;
        }

        public static bool FixReferenceWrtContent(this IEmbeddedDataSpecification eds)
        {
            // does content tell something?
            var ctc = ExtendIDataSpecificationContent.GuessContentTypeFor(eds?.DataSpecificationContent);
            var ctr = ExtendIDataSpecificationContent.GuessContentTypeFor(eds?.DataSpecification);

            if (ctc == ExtendIDataSpecificationContent.ContentTypes.NoInfo)
                return false;

            if (ctr == ctc)
                return false;

            // ok, fix
            eds.DataSpecification = new Reference(ReferenceTypes.ExternalReference,
                new List<IKey> { ExtendIDataSpecificationContent.GetKeyFor(ctc) });
            return true;
        }
        
    }

    /// <summary>
    /// This class is intended to provide a "blank" DataSpecificationContent
    /// in order to satisfy the cardinality [1] of EmbeddedDataSpecification.
    /// It has no specific semantics or sense. It is purely existing.
    /// </summary>
    public class DataSpecificationBlank : IDataSpecificationContent
    {
        /// <summary>
        /// Iterate over all the class instances referenced from this instance
        /// without further recursion.
        /// </summary>
        public IEnumerable<IClass> DescendOnce()
        {
            yield break;
        }

        /// <summary>
        /// Iterate recursively over all the class instances referenced from this instance.
        /// </summary>
        public IEnumerable<IClass> Descend()
        {
            yield break;
        }

        /// <summary>
        /// Accept the <paramref name="visitor" /> to visit this instance
        /// for double dispatch.
        /// </summary>
        public void Accept(Visitation.IVisitor visitor)
        {
        }

        /// <summary>
        /// Accept the visitor to visit this instance for double dispatch
        /// with the <paramref name="context" />.
        /// </summary>
        public void Accept<TContext>(
            Visitation.IVisitorWithContext<TContext> visitor,
            TContext context)
        {
        }

        /// <summary>
        /// Accept the <paramref name="transformer" /> to transform this instance
        /// for double dispatch.
        /// </summary>
        public T Transform<T>(Visitation.ITransformer<T> transformer)
        {
            return default(T);
        }

        /// <summary>
        /// Accept the <paramref name="transformer" /> to visit this instance
        /// for double dispatch with the <paramref name="context" />.
        /// </summary>
        public T Transform<TContext, T>(
            Visitation.ITransformerWithContext<TContext, T> transformer,
            TContext context)
        {
            return default(T);
        }

        public DataSpecificationBlank()
        {
        }
    }
}
