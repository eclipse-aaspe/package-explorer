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
    public class HttpHeaderDataItem
    {
        public string Key;
        public string Value;

        public HttpHeaderDataItem() { }
        public HttpHeaderDataItem(string key, string value) {
            Key = key;
            Value = value;
        }
    }

    public class HttpHeaderData
    {
        public List<HttpHeaderDataItem> Headers = new();

        public HttpHeaderData() { }
        public HttpHeaderData(string jsonOrMime) {
            Parse(jsonOrMime);
        }

        public void AddForUnique(HttpHeaderDataItem item)
        {
            // do not duplicate!
            foreach (var y in this.Headers)
                if (item.Key == y.Key)
                {
                    // update
                    y.Value = item.Value;
                    return;
                }

            // no, add
            Headers.Add(item);
        }

        public void Add(string key, string value)
        {
            this.AddForUnique(new HttpHeaderDataItem(key, value));
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
                    Headers.Add(new HttpHeaderDataItem(key, val));
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
                // add
                res.AddForUnique(x);
            }

            // return new
            return res;
        }
    }
}
