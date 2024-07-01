/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using AasCore.Aas3_0_RC02;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Extensions
{
    public static class ExtendKeyTypes
    {
        public static bool IsSME(this KeyTypes keyType)
        {
            foreach (var kt in Constants.AasSubmodelElementsAsKeys)
                if (kt.HasValue && kt.Value == keyType)
                    return true;
            return false;
        }
    }
}
