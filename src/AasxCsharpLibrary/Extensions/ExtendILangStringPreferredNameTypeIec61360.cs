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
    public static class ExtendILangStringPreferredNameTypeIec61360
    {
        public static string GetDefaultString(this List<ILangStringPreferredNameTypeIec61360> langStringSet, string defaultLang = null)
        {
            return ExtendLangString.GetDefaultStringGen(langStringSet, defaultLang);
        }

        public static List<ILangStringPreferredNameTypeIec61360> CreateFrom(string text, string lang = "en")
        {
            if (text == null)
                return null;

            var res = new List<ILangStringPreferredNameTypeIec61360>();
            res.Add(new LangStringPreferredNameTypeIec61360(lang, text));
            return res;
        }

        public static bool IsEmpty(this List<ILangStringPreferredNameTypeIec61360> langStringSet)
        {
            if (langStringSet == null || langStringSet.Count == 0)
            {
                return true;
            }

            return false;
        }

        public static bool IsValid(this List<ILangStringPreferredNameTypeIec61360> elems)
        {
            if (elems == null || elems.Count < 1)
                return false;
            foreach (var ls in elems)
                if (ls?.Language == null || ls.Language.Trim().Length < 1
                    || ls.Text == null || ls.Text.Trim().Length < 1)
                    return false;
            return true;
        }

        public static List<ILangStringPreferredNameTypeIec61360> ConvertFromV20(
            this List<ILangStringPreferredNameTypeIec61360> lss,
            AasxCompatibilityModels.AdminShellV20.LangStringSetIEC61360 src)
        {
            lss = new List<ILangStringPreferredNameTypeIec61360>();
            if (src != null && src.Count != 0)
            {
                foreach (var sourceLangString in src)
                {
                    //Remove ? in the end added by AdminShellV20, to avoid verification error
                    string lang = sourceLangString.lang;
                    if (!string.IsNullOrEmpty(sourceLangString.lang) && sourceLangString.lang.EndsWith("?"))
                    {
                        lang = sourceLangString.lang.Remove(sourceLangString.lang.Length - 1);
                    }
                    var langString = new LangStringPreferredNameTypeIec61360(lang, sourceLangString.str);
                    lss.Add(langString);
                }
            }
            else
            {
                //set default preferred name
                lss.Add(new LangStringPreferredNameTypeIec61360("en", ""));
            }
            return lss;
        }
    }
}
