/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AasxIntegrationBase
{
    /// <summary>
    /// This class lists valid combinations of  ISO 639 (2 digit language codes)
    /// and ISO 3166 (2 digit country codes). Only 1:1 relations are modelled.
    /// </summary>
    public class AasxLanguageTuple
    {
        /// <summary>
        /// According ISO 639-2, 2 digit language code
        /// </summary>
        public string LangCode;

        /// <summary>
        /// According ISO 3166, 2 digit country code
        /// </summary>
        public string CountryCode;

        /// <summary>
        /// Marks the wildcard element
        /// </summary>
        public bool IsAny()
        {
            return LangCode == "" && CountryCode == ""
                || LangCode == "All" && CountryCode == "All";
        }

        public static AasxLanguageTuple GetAny()
        {
            return new AasxLanguageTuple() { LangCode = "All", CountryCode = "All" };
        }
    }

    /// <summary>
    /// Maintains a list of language tuples.
    /// Note: Is always defaulted with a list of language tuples. 
    /// Could be cleard/ added.
    /// </summary>
    public class AasxLanguageTupleSet : MultiValueDictionary<string, AasxLanguageTuple>
    {
        public void Add(string lang, string country, bool correctInput = true)
        {
            if (correctInput)
            {
                lang = lang?.ToLower().Trim();
                country = country?.ToUpper().Trim();
            }

            if (lang != null && lang != "" 
                && country != null && country != "")
            {
                this.Add(lang, new AasxLanguageTuple() { LangCode = lang, CountryCode = country });
            }
        }

        public AasxLanguageTupleSet()
        {
            Init();
        }

        /// <summary>
        /// Rationale for default languages/ countries: member in IDTA or IEC TC65 WG24
        /// </summary>
        public void Init()
        {
            this.Clear();
            Add("All", "All", correctInput: false); 
            Add("en", "GB");
            Add("en", "US");
            Add("de", "DE");
            Add("de", "CH");
            Add("de", "AT");
            Add("es", "ES");
            Add("fi", "FI");
            Add("fr", "FR");
            Add("it", "IT");
            Add("ja", "JP"); 
            Add("ko", "KR");
            Add("nl", "NL");
            Add("no", "NO");
            Add("pt", "PT");
            Add("sv", "SE");
            Add("zh", "CN");
        }

        public IEnumerable<AasxLanguageTuple> FindByLang(string lang)
        {
            lang = lang?.ToLower().Trim();
            if (lang == null || lang == "" || this.ContainsKey(lang) == false)
                yield break;
            foreach (var x in this[lang])
                yield return x;
        }

        public IEnumerable<AasxLanguageTuple> FindByCountry(string country)
        {
            country = country?.ToUpper().Trim();
            if (country == null || country == "")
                yield break;
            foreach (var tp in this.Values)
                if (country == tp.CountryCode.ToUpper())
                    yield return tp;
        }

        public IEnumerable<string> GetAllLanguages(bool nullForAny = false)
        {
            var temp = new List<string>();
            foreach (var tp in this.Values)
                if (!tp.IsAny())
                    temp.Add(tp.LangCode);
                else
                {
                    if (nullForAny)
                        temp.Add(null);
                    else
                        temp.Add(tp.LangCode);
                }

            if (temp.Count < 1)
                yield break;

            temp = temp.Distinct().ToList();
            var first = temp[0];
            temp.RemoveAt(0);
            temp.Sort();
            temp.Insert(0, first);

            foreach (var t in temp)
                yield return t;
        }

        public IEnumerable<string> GetAllCountries()
        {
            var temp = new List<string>();
            foreach (var tp in this.Values)
                temp.Add(tp.CountryCode);

            if (temp.Count < 1)
                yield break;

            temp = temp.Distinct().ToList();
            var first = temp[0];
            temp.RemoveAt(0);
            temp.Sort();
            temp.Insert(0, first);

            foreach (var t in temp)
                yield return t;
        }

        public void InitByCustomString(string input)
        {
            this.Clear();
            Add("All", "All", correctInput: false);
            var pairs = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var pair in pairs)
            {
                var m = Regex.Match(pair, @"(\w+)\s*-\s*(\w+)");
                if (m.Success)
                    Add(m.Groups[1].ToString(), m.Groups[2].ToString());
            }
        }
    }

    public static class AasxLanguageHelper
    {
        public static AasxLanguageTupleSet Languages = new AasxLanguageTupleSet();

#if __old
        public enum LangEnum { Any = 0, EN, DE, ZH, JA, KO, FR, ES };

        public static string[] LangEnumToISO639String = {
                "All", "en", "de", "zh", "ja", "ko", "fr", "es" }; // ISO 639 -> List of languages

        public static string[] LangEnumToISO3166String = {
                "All", "GB", "DE", "CN", "JP", "KR", "FR", "ES" }; // ISO 3166 -> List of countries

        public static string GetLangCodeFromEnum(LangEnum le, bool nullForDefault = false)
        {
            if (nullForDefault && le == LangEnum.Any)
                return null;
            return "" + LangEnumToISO639String[(int)le];
        }

        public static string GetCountryCodeFromEnum(LangEnum le)
        {
            return "" + LangEnumToISO3166String[(int)le];
        }

        public static LangEnum FindLangEnumFromLangCode(string candidate)
        {
            if (candidate == null)
                return LangEnum.Any;
            candidate = candidate.ToLower().Trim();
            foreach (var ev in (LangEnum[])Enum.GetValues(typeof(LangEnum)))
                if (candidate == LangEnumToISO639String[(int)ev]?.ToLower())
                    return ev;
            return LangEnum.Any;
        }

        public static LangEnum FindLangEnumFromCountryCode(string candidate)
        {
            if (candidate == null)
                return LangEnum.Any;
            candidate = candidate.ToUpper().Trim();
            foreach (var ev in (LangEnum[])Enum.GetValues(typeof(LangEnum)))
                if (candidate == LangEnumToISO3166String[(int)ev]?.ToUpper())
                    return ev;
            return LangEnum.Any;
        }

        public static IEnumerable<string> GetLangCodes()
        {
            for (int i = 1; i < LangEnumToISO639String.Length; i++)
                yield return LangEnumToISO639String[i];
        }
#endif

        public static string GetFirstLangCode(string codes)
        {
            if (codes == null)
                return null;
            var lst = codes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return lst.FirstOrDefault();
        }
    }
}
