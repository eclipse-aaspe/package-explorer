/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdminShellNS;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Maintains a dictionary of URIs serving as a basis for building
    /// different URI pathes. In the context of AAS, URIs for different
    /// AAS/SM-Repo/Registries are differentiated.
    /// </summary>
    public class BaseUriDict : Dictionary<string, string>
    {
        /// <summary>
        /// Key for everything else.
        /// </summary>
        public const string Default = "*";

        public BaseUriDict() { }

        public BaseUriDict(string input) 
        { 
            Parse(input);
        }

        /// <summary>
        /// Checks for validity. Minimum of one URI with the default key shall exist.
        /// </summary>
        public bool IsValid()
        {
            return this.Count >=1 && this.ContainsKey(Default);
        }

        /// <summary>
        /// Parses a string containing the information on one or multiple base URIs.
        /// Variable sections are demarked by '{{' and '}}'. Inside, the choices are
        /// formatted as JSON, e.g. "SM-REG" : "/smregi/v33/", separated by comma.
        /// </summary>
        /// <param name="input">Input with complex syntax.</param>
        /// <returns>True, if no explicit error.</returns>
        public bool Parse(string input)
        {
            // test if any varieties
            var matchMulti = Regex.Match(input, @"(\{\{(.*?)\}\})", 
                    RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace 
                    | RegexOptions.Compiled);
            if (!matchMulti.Success)
            {
                this[Default] = input;
                return true;
            }

            // yes, try to split
            var pairs = AdminShellUtil.StringSplitUnquoted(matchMulti.Groups[2].ToString(), ',', 
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (pairs.Count < 1)
            {
                return false;
            }

            // take these pairs
            // for future, may see here:
            // see: https://stackoverflow.com/questions/6005609/replace-only-some-groups-with-regex
            foreach (var pair in pairs)
            {
                var matchPair = Regex.Match(pair, @"""([^""]*)""\s*:\s*""([^""]*)""",
                        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace
                        | RegexOptions.Compiled);
                if (!matchPair.Success)
                    continue;
                this[matchPair.Groups[1].ToString()] = 
                    input.Substring(0, matchMulti.Groups[1].Index) 
                    + matchPair.Groups[2].ToString()
                    + input.Substring(matchMulti.Groups[1].Index + matchMulti.Groups[1].Length) ;
            }

            // ok
            return true;
        }

        protected Uri GetBaseUri(params string[] keys)
        {
            // access
            if (keys == null || keys.Length == 0)
                return null;
            // find any
            foreach (var k in keys)
                if (this.ContainsKey(k))
                    return new Uri(this[k]);
            //no?
            return null;
        }

        public Uri GetBaseUriForDefault()
        {
            return GetBaseUri(Default);
        }

        public Uri GetBaseUriForAasRepo()
        {
            return GetBaseUri("AAS-REPO", "AAS-ENV", Default);
        }

        public Uri GetBaseUriForSmRepo()
        {
            return GetBaseUri("SM-REPO", "AAS-ENV", Default);
        }

        public Uri GetBaseUriForCdRepo()
        {
            return GetBaseUri("CD-REPO", "AAS-ENV", Default);
        }

        public Uri GetBaseUriForBasicDiscovery()
        {
            return GetBaseUri("DIS", Default);
        }

        public Uri GetBaseUriForQuery()
        {
            return GetBaseUri("QRY", Default);
        }

        public Uri GetBaseUriForAasReg()
        {
            return GetBaseUri("AAS-REG", "AAS-ENV", Default);
        }

        public Uri GetBaseUriForSmReg()
        {
            return GetBaseUri("SM-REG", "AAS-ENV", Default);
        }
    }
}
