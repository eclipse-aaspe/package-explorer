/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;
using System.Collections.Generic;
using System.Linq;
    
namespace Extensions
{
    public static class ExtendILangStringNameType
    {
        public static string GetDefaultString(this List<ILangStringNameType> langStringSet, string defaultLang = null)
        {
            return ExtendLangString.GetDefaultStringGen(langStringSet, defaultLang);
        }

        public static string ToStringExtended(this ILangStringNameType ls, int fmt)
        {
            if (fmt == 2)
                return String.Format("{0}@{1}", ls.Text, ls.Language);
            return String.Format("[{0},{1}]", ls.Language, ls.Text);
        }

        public static string ToStringExtended(this List<ILangStringNameType> elems,
            int format = 1, string delimiter = ",")
        {
            return string.Join(delimiter, elems.Select((k) => k.ToStringExtended(format)));
        }

        public static List<ILangStringNameType> CreateFrom(string text, string lang = "en")
        {
            if (text == null)
                return null;

            var res = new List<ILangStringNameType>();
            res.Add(new LangStringNameType(lang, text));
            return res;
        }

        public static bool IsEmpty(this List<ILangStringNameType> langStringSet)
        {
            if (langStringSet == null || langStringSet.Count == 0)
            {
                return true;
            }

            return false;
        }

        public static bool IsValid(this List<ILangStringNameType> elems)
        {
            if (elems == null || elems.Count < 1)
                return false;
            foreach (var ls in elems)
                if (ls?.Language == null || ls.Language.Trim().Length < 1
                    || ls.Text == null || ls.Text.Trim().Length < 1)
                    return false;
            return true;
        }

        public static bool Contains(this List<ILangStringNameType> elems, String value, StringComparison comparisonType)
        {
            if (elems == null || elems.Count < 1)
                return false;
            var res = false;
            foreach (var ls in elems)
                res = res || ls?.Text?.Contains(value, comparisonType) == true;
            return res;
        }
    }
}
