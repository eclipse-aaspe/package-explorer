/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// This class tries to join the distinct classes FieldInfo or PropertyInfo in
    /// some relevant functionality.
    /// </summary>
    public class FieldPropertyInfo
    {
        // very stupid union
        public FieldInfo Fi = null;
        public PropertyInfo Pi = null;

        // constructors
        
        public FieldPropertyInfo (FieldInfo fi) {  Fi = fi; }
        public FieldPropertyInfo (PropertyInfo pi) {  Pi = pi; }

        // union accessors

        public Type FiPiType { 
            get {
                if (Fi != null)
                    return Fi.FieldType;
                if (Pi != null)
                    return Pi.PropertyType;
                return null;
            }
        }

        public object GetValue(object obj)
        {
            if (Fi != null)
                return Fi.GetValue(obj);
            if (Pi != null)
                return Pi.GetValue(obj);
            return null;
        }

        public void SetValue(object obj, object value)
        {
            if (Fi != null)
                Fi.SetValue(obj, value);
            if (Pi != null)
                Pi.SetValue(obj, value);
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            if (Fi != null)
                return Fi.GetCustomAttribute<T>();
            if (Pi != null)
                return Pi.GetCustomAttribute<T>();
            return null;
        }

        // static

        public static List<FieldPropertyInfo> GetFieldProperties(Type t, BindingFlags flags)
        {
            var res = new List<FieldPropertyInfo>();
            foreach (var fi in t.GetFields(flags))
                res.Add(new FieldPropertyInfo(fi));
            foreach (var pi in t.GetProperties(flags))
                res.Add(new FieldPropertyInfo(pi));
            return res;
        }

        // Set Lazy
        // Note: this doubles with code in AdminShellUtil :-/

        /// <summary>
        /// Tries parsing the <c>value</c>, supposedly a string, to a field value
        /// for reflection of type specific data.
        /// Works for most scalars, dateTime, string.
        /// </summary>
        public void SetFieldLazyValue(object obj, object value)
        {
            // access
            if (obj == null)
                return;

            // 2024-01-04: make function more suitable for <DateTime?>
            switch (Type.GetTypeCode(AdminShellUtil.GetTypeOrUnderlyingType(FiPiType)))
            {
                case TypeCode.String:
                    SetValue(obj, "" + value);
                    break;

                case TypeCode.DateTime:
                    if (DateTime.TryParse("" + value, out var dt))
                        SetValue(obj, dt);
                    break;

                case TypeCode.Byte:
                    if (Byte.TryParse("" + value, out var ui8))
                        SetValue(obj, ui8);
                    break;

                case TypeCode.SByte:
                    if (SByte.TryParse("" + value, out var i8))
                        SetValue(obj, i8);
                    break;

                case TypeCode.Int16:
                    if (Int16.TryParse("" + value, out var i16))
                        SetValue(obj, i16);
                    break;

                case TypeCode.Int32:
                    if (Int32.TryParse("" + value, out var i32))
                        SetValue(obj, i32);
                    break;

                case TypeCode.Int64:
                    if (Int64.TryParse("" + value, out var i64))
                        SetValue(obj, i64);
                    break;

                case TypeCode.UInt16:
                    if (UInt16.TryParse("" + value, out var ui16))
                        SetValue(obj, ui16);
                    break;

                case TypeCode.UInt32:
                    if (UInt32.TryParse("" + value, out var ui32))
                        SetValue(obj, ui32);
                    break;

                case TypeCode.UInt64:
                    if (UInt64.TryParse("" + value, out var ui64))
                        SetValue(obj, ui64);
                    break;

                case TypeCode.Single:
                    if (value is double vd)
                        SetValue(obj, vd);
                    else
                    if (value is float vf)
                        SetValue(obj, vf);
                    else
                    if (Single.TryParse("" + value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var sgl))
                        SetValue(obj, sgl);
                    break;

                case TypeCode.Double:
                    if (value is double vd2)
                        SetValue(obj, vd2);
                    else
                    if (value is float vf2)
                        SetValue(obj, vf2);
                    else
                    if (Double.TryParse("" + value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var dbl))
                        SetValue(obj, dbl);
                    break;

                case TypeCode.Boolean:
                    var isFalse = value == null
                        || (value is int vi && vi == 0)
                        || (value is string vs && (vs == "" || vs == "false"))
                        || (value is bool vb && !vb);
                    SetValue(obj, !isFalse);
                    break;
            }
        }
    }
}
