/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Extensions
{
    public class LocatedReference
    {
        public IIdentifiable Identifiable;
        public IReference Reference;

        public LocatedReference() { }
        public LocatedReference(IIdentifiable identifiable, IReference reference)
        {
            Identifiable = identifiable;
            Reference = reference;
        }
    }

    /// <summary>
    /// This comparer takes a shortcut and does ONLY compare the object references, but
    /// NOT the complicated AAS references. In a <c>LocatedReference</c> they are assumed
    /// to be equivalent to each other!!
    /// </summary>
    public class LocatedReferenceComparer : IEqualityComparer<LocatedReference>
    {
        bool IEqualityComparer<LocatedReference>.Equals(LocatedReference x, LocatedReference y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return (x.Identifiable == y.Identifiable);
        }

        int IEqualityComparer<LocatedReference>.GetHashCode(LocatedReference obj)
        {
            return obj?.Identifiable?.GetHashCode() ?? 0;
        }
    }

    public static class LocatedReferenceExtensions
    {
        public static void AddIfNew(this IList<LocatedReference> refs, LocatedReference newLR)
        {
            var same = refs?.FirstOrDefault((lr) => lr?.Reference?.Matches(newLR?.Reference, MatchMode.Relaxed) == true);
            if (same == null)
                refs.Add(newLR);
        }
    }
}
