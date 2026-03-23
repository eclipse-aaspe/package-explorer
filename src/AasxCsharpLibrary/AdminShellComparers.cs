/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AdminShellNS
{
    public static class AdminShellComparers
    {

        /// see: https://www.codeproject.com/Articles/72470/LINQ-Enhancing-Distinct-With-The-PredicateEquality
        public class PredicateEqualityComparer<T> : EqualityComparer<T>
        {
            private Func<T, T, bool> predicate;

            public PredicateEqualityComparer(Func<T, T, bool> predicate)
                : base()
            {
                this.predicate = predicate;
            }

            public override bool Equals(T x, T y)
            {
                if (x != null)
                {
                    return ((y != null) && this.predicate(x, y));
                }

                if (y != null)
                {
                    return false;
                }

                return true;
            }

            public override int GetHashCode(T obj)
            {
                // Always return the same value to force the call to IEqualityComparer<T>.Equals
                return 0;
            }
        }
    }
}
