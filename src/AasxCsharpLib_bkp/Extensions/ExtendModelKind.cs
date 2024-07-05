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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;

namespace Extensions
{
    public static class ExtendModelKind
    {
        public static void Validate(this ModelingKind modelingKind, AasValidationRecordList results, IReferable container)
        {
            // access
            if (results == null || container == null)
                return;

            // check
            if (modelingKind != ModelingKind.Template && modelingKind != ModelingKind.Instance)
            {
                // violation case
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SchemaViolation, container,
                    $"ModelingKind: enumeration value neither Template nor Instance",
                    () =>
                    {
                        modelingKind = ModelingKind.Instance;
                    }));
            }
        }
    }
}
