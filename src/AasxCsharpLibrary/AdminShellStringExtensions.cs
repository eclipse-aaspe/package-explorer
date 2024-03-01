/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxCompatibilityModels;
using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace AdminShellNS
{
    public static class AdminShellStringExtensions
    {
        /// <summary>
        /// Check if real content is in a string
        /// </summary>
        public static bool HasContent(this string str)
        {
            return str != null && str.Trim() != "";
        }

        /// <summary>
        /// Multiple a string into a iterable list
        /// </summary>
        public static IEnumerable<string> Times(this string str, int num = 1)
        {
            for (int i= 0; i < num; i++)
                yield return str;
        }

        /// <summary>
        /// Adds an items to a sequence of strings.
        /// </summary>
        public static IEnumerable<string> Add(this IEnumerable<string> seq, string str)
        {
            foreach (var s in seq)
                yield return s;
            yield return str;
        }

        public static string SubstringMax(this string str, int pos, int len)
        {
            if (!str.HasContent() || str.Length <= pos)
                return "";
            return str.Substring(pos, Math.Min(len, str.Length));
        }

        public static void SetIfNoContent(ref string s, string input)
        {
            if (!input.HasContent())
                return;
            if (!s.HasContent())
                s = input;
        }

        public static string AddWithDelimiter(this string str, string content, string delimter = "")
        {
            var res = str;
            if (res == null)
                return null;

            if (res.HasContent())
                res += "" + delimter;

            res += content;

            return res;
        }

    }
}
