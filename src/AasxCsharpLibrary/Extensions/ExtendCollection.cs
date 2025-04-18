﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;

namespace AdminShellNS.Extensions
{
    public static class ExtendCollection
    {
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            if (list != null && list.Count != 0)
            {
                return false;
            }

            return true;
        }
    }
}
