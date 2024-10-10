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
using System.Runtime.Intrinsics.X86;

namespace Extensions
{
    public static class ExtendIHasSemantics
	{
        public static string GetConceptDescriptionId(this IHasSemantics ihs)
        {
            if (ihs?.SemanticId != null 
                && ihs.SemanticId.IsValid() == true
                && ihs.SemanticId.Count() == 1
				&& (ihs.SemanticId.Keys[0].Type == KeyTypes.ConceptDescription
                    || ihs.SemanticId.Keys[0].Type == KeyTypes.Submodel
					|| ihs.SemanticId.Keys[0].Type == KeyTypes.GlobalReference)
				&& ihs.SemanticId.Keys[0].Value != null
				&& ihs.SemanticId.Keys[0].Value.Trim().Length > 0)
            {
                return ihs.SemanticId.Keys[0].Value;
			}
            return null;
        }
    }
}
