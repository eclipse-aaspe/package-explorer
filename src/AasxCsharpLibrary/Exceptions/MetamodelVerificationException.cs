/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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

/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System;
using System.Collections.Generic;
using static AasCore.Aas3_0.Reporting;

namespace AdminShellNS.Exceptions
{
    public class MetamodelVerificationException : Exception
    {
        public List<Error> ErrorList { get; }

        public MetamodelVerificationException(List<Error> errorList) : base($"The request body not conformant with the metamodel. Found {errorList.Count} errors !!")
        {
            ErrorList = errorList;
        }


    }
}
