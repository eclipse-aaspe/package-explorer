/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using AasxCompatibilityModels;
using AdminShellNS;
using System.Collections.Generic;

namespace Extensions
{
    //TODO (jtikekar, 0000-00-00): remove
    //MIHO, 2024-06-01: migrate LANG_DEFAULT to AdminShellUtil.GetDefaultLngIso639()
    public static class ExtendLangString
    {
        // new version
        public static string GetDefaultStringGen<T>(List<T> langStrings, string defaultLang = null)
            where T : IAbstractLangString
        {
            // start
            if (defaultLang == null)
                defaultLang = AdminShellUtil.GetDefaultLngIso639();
            defaultLang = defaultLang.Trim().ToLower();
            string res = null;

            // search
            foreach (var ls in langStrings)
            {
                if (ls != null)
                {
                    if (ls.Language.Trim().ToLower() == defaultLang)
                        res = ls.Text; 
                }
            }

            if (res == null && langStrings.Count > 0)
            {
                if (langStrings[0] != null)
                {
                    res = langStrings[0].Text;
                }
            }   

            // found?
            return res;
        }

        public static IAbstractLangString Create<T>(string language, string text) where T : IAbstractLangString
        {
            if (typeof(T).IsAssignableFrom(typeof(ILangStringTextType)))
            {
                return new LangStringTextType(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringNameType)))
            {
                return new LangStringNameType(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringPreferredNameTypeIec61360)))
            {
                return new LangStringPreferredNameTypeIec61360(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringShortNameTypeIec61360)))
            {
                return new LangStringShortNameTypeIec61360(language, text);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringDefinitionTypeIec61360)))
            {
                return new LangStringDefinitionTypeIec61360(language, text);
            }
            else
                return null;
        }
    }
}
