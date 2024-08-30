/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Extensions
{
    public static class ExtendExtension
    {

        public static bool IsValid(this List<IExtension> elems)
        {
            if (elems == null || elems.Count < 1)
                return false;
            foreach (var q in elems)
                if (q?.Name == null || q.Name.Trim().Length < 1)
                    return false;
            return true;
        }

        public static bool IsOneBlank(this List<IExtension> elems)
        {
            if (elems == null || elems.Count != 1)
                return false;
            return elems[0].Value?.HasContent() != true;
        }
    }
}
