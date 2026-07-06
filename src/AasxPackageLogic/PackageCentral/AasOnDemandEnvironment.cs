/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxOpenIdClient;
using AdminShellNS;
using AdminShellNS.DiaryData;
using Aas = AasCore.Aas3_1;
using Extensions;
using IdentityModel.Client;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Lucene.Net.Util;

namespace AasxPackageLogic.PackageCentral
{
    public enum AasIdentifiableSideInfoLevel { None, IdOnly, IdAndMore, IdWithEndpoint };

    /// <summary>
    /// This side information helps managing Identifiables, which are not already loaded.
    /// </summary>
    public class AasIdentifiableSideInfo : OnDemandSideInfoBase
    {
        public bool IsStub = false;
        public AasIdentifiableSideInfoLevel StubLevel = AasIdentifiableSideInfoLevel.None;

        public string IdShort = "";

        /// <summary>
        /// Endpoint, as being used for query of Repo/ Registry. Used for re-iterating
        /// queries/ fetches.
        /// </summary>
        public Uri QueriedEndpoint;

        /// <summary>
        /// Endpoint as given by Repo (built via Id) or Registry (by descriptor)
        /// </summary>
        public Uri DesignatedEndpoint;

        /// <summary>
        /// In the tree of virtual elements, place a visual element below this entity
        /// </summary>
        public bool ShowCursorAbove = false;

        /// <summary>
        /// In the tree of virtual elements, place a visual element below this entity
        /// </summary>
        public bool ShowCursorBelow = false;
    }

    /// <summary>
    /// This class provides some service functions to manage on demand list of Identifiables.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OnDemandListIdentifiable<T> : OnDemandList<T, AasIdentifiableSideInfo>, IClass 
        where T : Aas.IIdentifiable
    {
        public OnDemandListIdentifiable() { }

        public OnDemandListIdentifiable(IEnumerable<T> items)
        {
            this.AddRange(items);
        }

        public int FindSideInfoIndexFromId(string id)
        {
            if (id?.HasContent() != true)
                return -1;

            for (int i=0; i < this.Count(); i++)
            {
                var si = this.GetSideInfo(i);
                if (si?.Id != null && si.Id.Trim() == id.Trim())
                    return i;
            }

            return -1;
        }

        public AasIdentifiableSideInfo FindSideInfoFromId(string id)
        {
            var ndx = FindSideInfoIndexFromId(id);
            if (ndx >= 0)
                return GetSideInfo(ndx);
            return null;
        }

        public static AasIdentifiableSideInfo FindSideInfoInListOfIdentifiables(
            object listIdf,
            Aas.IReference rf)
        {
            if (rf?.IsValid() == true
                && listIdf is OnDemandListIdentifiable<T> smsi)
            {
                var ndx = smsi.FindSideInfoIndexFromId(rf.GetAsIdentifier());
                if (ndx >= 0)
                    return smsi.GetSideInfo(ndx);
            }
            return null;
        }        

        //
        // make this class suitable for IClass as well
        //

        IEnumerable<IClass> IClass.DescendOnce()
        {
            foreach (var it in this)
                yield return it;
        }

        IEnumerable<IClass> IClass.Descend()
        {
            foreach (var it in this)
            {
                yield return it;
                foreach (var i2 in it.Descend())
                    yield return i2;
            }
        }

        void IClass.Accept(Visitation.IVisitor visitor)
        {
        }

        void IClass.Accept<TContext>(Visitation.IVisitorWithContext<TContext> visitor, TContext context)
        {
        }

        T1 IClass.Transform<T1>(Visitation.ITransformer<T1> transformer)
        {
            return default(T1);
        }

        T1 IClass.Transform<TContext, T1>(Visitation.ITransformerWithContext<TContext, T1> transformer, TContext context)
        {
            return default(T1);
        }
    }

    /// <summary>
    /// This class creates a package env, which can handle dynamic loading of elements
    /// </summary>
    public class AasOnDemandEnvironment : Aas.IEnvironment
    {
        /// <summary>
        /// Asset administration shell
        /// </summary>
        public IList<IAssetAdministrationShell> AssetAdministrationShells { get; set; }

        /// <summary>
        /// Submodel
        /// </summary>
        public IList<ISubmodel> Submodels { get; set; }

        /// <summary>
        /// Concept description
        /// </summary>
        public IList<IConceptDescription> ConceptDescriptions { get; set; }

        /// <summary>
        /// Iterate over AssetAdministrationShells, if set, and otherwise return an empty enumerable.
        /// </summary>
        public IEnumerable<IAssetAdministrationShell> OverAssetAdministrationShellsOrEmpty()
        {
            return AssetAdministrationShells
                ?? System.Linq.Enumerable.Empty<IAssetAdministrationShell>();
        }

        [JsonIgnore]
        private DiaryDataDef _diaryData = new DiaryDataDef();

        [JsonIgnore]
        public DiaryDataDef DiaryData { get { return _diaryData; } }

        /// <summary>
        /// Iterate over Submodels, if set, and otherwise return an empty enumerable.
        /// </summary>
        public IEnumerable<ISubmodel> OverSubmodelsOrEmpty()
        {
            return Submodels
                ?? System.Linq.Enumerable.Empty<ISubmodel>();
        }

        /// <summary>
        /// Iterate over ConceptDescriptions, if set, and otherwise return an empty enumerable.
        /// </summary>
        public IEnumerable<IConceptDescription> OverConceptDescriptionsOrEmpty()
        {
            return ConceptDescriptions
                ?? System.Linq.Enumerable.Empty<IConceptDescription>();
        }

        /// <summary>
        /// Iterate over all the class instances referenced from this instance
        /// without further recursion.
        /// </summary>
        public IEnumerable<IClass> DescendOnce()
        {
            if (AssetAdministrationShells != null)
            {
                foreach (var anItem in AssetAdministrationShells)
                {
                    yield return anItem;
                }
            }

            if (Submodels != null)
            {
                foreach (var anItem in Submodels)
                {
                    yield return anItem;
                }
            }

            if (ConceptDescriptions != null)
            {
                foreach (var anItem in ConceptDescriptions)
                {
                    yield return anItem;
                }
            }
        }

        /// <summary>
        /// Iterate recursively over all the class instances referenced from this instance.
        /// </summary>
        public IEnumerable<IClass> Descend()
        {
            if (AssetAdministrationShells != null)
            {
                foreach (var anItem in AssetAdministrationShells)
                {
                    yield return anItem;

                    // Recurse
                    foreach (var anotherItem in anItem.Descend())
                    {
                        yield return anotherItem;
                    }
                }
            }

            if (Submodels != null)
            {
                foreach (var anItem in Submodels)
                {
                    yield return anItem;

                    // Recurse
                    foreach (var anotherItem in anItem.Descend())
                    {
                        yield return anotherItem;
                    }
                }
            }

            if (ConceptDescriptions != null)
            {
                foreach (var anItem in ConceptDescriptions)
                {
                    yield return anItem;

                    // Recurse
                    foreach (var anotherItem in anItem.Descend())
                    {
                        yield return anotherItem;
                    }
                }
            }
        }

        /// <summary>
        /// Accept the <paramref name="visitor" /> to visit this instance
        /// for double dispatch.
        /// </summary>
        public void Accept(Visitation.IVisitor visitor)
        {
            visitor.VisitEnvironment(this);
        }

        /// <summary>
        /// Accept the visitor to visit this instance for double dispatch
        /// with the <paramref name="context" />.
        /// </summary>
        public void Accept<TContext>(
            Visitation.IVisitorWithContext<TContext> visitor,
            TContext context)
        {
            visitor.VisitEnvironment(this, context);
        }

        /// <summary>
        /// Accept the <paramref name="transformer" /> to transform this instance
        /// for double dispatch.
        /// </summary>
        public T Transform<T>(Visitation.ITransformer<T> transformer)
        {
            return transformer.TransformEnvironment(this);
        }

        /// <summary>
        /// Accept the <paramref name="transformer" /> to visit this instance
        /// for double dispatch with the <paramref name="context" />.
        /// </summary>
        public T Transform<TContext, T>(
            Visitation.ITransformerWithContext<TContext, T> transformer,
            TContext context)
        {
            return transformer.TransformEnvironment(this, context);
        }

        public AasOnDemandEnvironment(
            IList<IAssetAdministrationShell> assetAdministrationShells = null,
            IList<ISubmodel> submodels = null,
            IList<IConceptDescription> conceptDescriptions = null)
        {
            AssetAdministrationShells = assetAdministrationShells;
            Submodels = submodels;
            ConceptDescriptions = conceptDescriptions;
        }

    }

}
