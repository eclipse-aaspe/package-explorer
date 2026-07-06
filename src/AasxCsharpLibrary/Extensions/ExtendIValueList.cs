/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using Extensions;
using System.Collections.Generic;
using System.Linq;
using AdminShellNS;
using Aas = AasCore.Aas3_1;

namespace Extensions
{
    public static class ExtendIValueList
    {
        public static bool IsEmpty(this IValueList list)
        {
            return list.ValueReferencePairs == null 
                || list.ValueReferencePairs.Count < 1;
        }

        public static bool IsValid(this IValueList list)
        {
            if (IsEmpty(list))
                return false;

            foreach (var vp in list.ValueReferencePairs)
                if (vp.Value?.HasContent() != true
                    || vp.ValueId?.IsValid() != true)
                    return false;

            return true;
        }
    }
}
