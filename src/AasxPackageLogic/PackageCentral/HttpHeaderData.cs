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
using System.Threading.Tasks;
using AdminShellNS;
using Newtonsoft.Json.Linq;

namespace AasxPackageLogic.PackageCentral
{
    public class HttpHeaderData
    {
        public List<Tuple<string, string>> Headers = new();

        public HttpHeaderData() { }
        public HttpHeaderData(string jsonOrMime) {
            Parse(jsonOrMime);
        }

        /// <summary>
        /// Parses a JSON-like object with header key/ value information.
        /// </summary>
        /// <returns>True, if empty or success.</returns>
        public bool Parse(string json)
        {
            // access
            if (json?.HasContent() != true)
                return true;

            // json could still be mime encoded
            if (AdminShellUtil.CheckIfBase64Only(json))
            {
                try
                { 
                    json = AdminShellUtil.Base64UrlDecode(json);
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                    return false;
                }
            }

            // convert
            try
            {
                var jarr = JObject.Parse(json);
                foreach (var ch in jarr.Children<JProperty>())
                {
                    string key = ch.Name;
                    string val = ch.Value?.ToString();
                    Headers.Add(new Tuple<string, string>(key, val));
                }
                return true;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
                return false;
            }
        }

        public static HttpHeaderData Merge(HttpHeaderData primary, HttpHeaderData subsidary)
        {
            // trivial cases
            if (primary == null && subsidary == null)
                return null;
            if (primary != null && subsidary == null)
                return primary;
            if (primary == null && subsidary != null)
                return subsidary;

            // non trivial
            var res = primary;
            foreach (var x in subsidary.Headers)
            {
                // do not duplicate!
                bool found = false;
                foreach (var y in res.Headers)
                    if (x.Item1 == y.Item1)
                        found = true;
                if (found)
                    continue;

                // add
                res.Headers.Add(x);
            }

            // return new
            return res;
        }
    }
}
