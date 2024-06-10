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
    /// <summary>
    /// Holds some extensions which are very fundamental in order to
    /// shortcut some hanlding of basic datat types.
    /// </summary>
    public static class AdminShellBasicExtensions
    {
        /// <summary>
        /// Check if real content is in a string
        /// </summary>
        public static bool HasContent(this string str)
        {
            return str != null && str.Trim() != "";
        }

        public static IEnumerable<T> ForEachSafe<T>(this List<T> list)
        {
            if (list == null)
                yield break;
            foreach (var x in list)
                yield return x;
        }

        /// <summary>
        /// Multiple a string into a iterable list
        /// </summary>
        public static IEnumerable<T> Times<T>(this T value, int num = 1)
        {
            for (int i= 0; i < num; i++)
                yield return value;
        }

        /// <summary>
        /// Adds an item to a sequence of strings.
        /// </summary>
        public static IEnumerable<T> Add<T>(this IEnumerable<T> seq, T value)
        {
            foreach (var s in seq)
                yield return s;
            yield return value;
        }

        /// <summary>
        /// Adds items to a sequence of strings.
        /// </summary>
        public static IEnumerable<T> Add<T>(this IEnumerable<T> seq, IEnumerable<T> others)
        {
            foreach (var s in seq.AsNotNull())
                yield return s;
            foreach (var s in others.AsNotNull())
                yield return s;
        }

        //public static IEnumerable<T> Times<T>(T value, int num = 1) where T : class
        //{
        //    for(int i = 0; i < num; i++)
        //        yield return value;
        //}

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

        public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> original)
        {
            return original ?? Enumerable.Empty<T>();
        }
    }
}
