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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AAS = AasCore.Aas3_0_RC02;
using AasCore.Aas3_0_RC02;

namespace Extensions
{
    public static class ExtendReferenceElement
    {
        public static AAS.ReferenceElement Set(this AAS.ReferenceElement elem,
            Reference rf)
        {
            elem.Value = rf;
            return elem;
        }

        public static AAS.ReferenceElement UpdateFrom(
            this AAS.ReferenceElement elem, AAS.ISubmodelElement source)
        {
            if (source == null)
                return elem;

            ((AAS.ISubmodelElement)elem).UpdateFrom(source);

            if (source is AAS.RelationshipElement srcRel)
            {
                if (srcRel.First != null)
                    elem.Value = srcRel.First.Copy();
            }

            if (source is AAS.AnnotatedRelationshipElement srcRelA)
            {
                if (srcRelA.First != null)
                    elem.Value = srcRelA.First.Copy();
            }

            return elem;
        }
    }
}
