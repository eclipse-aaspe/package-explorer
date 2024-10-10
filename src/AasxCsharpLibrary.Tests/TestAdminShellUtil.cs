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

using NUnit.Framework;

namespace AdminShellNS.Tests
{
    // ReSharper disable UnusedType.Global
    public class Test_EvalToNonNullString
    {
        [Test]
        public void NonNull_Gives_Formatted()
        {
            var result = AdminShellNS.AdminShellUtil.EvalToNonNullString(
                "some message: {0}", 1984, "something else");

            Assert.That(result, Is.EqualTo("some message: 1984"));
        }

        [Test]
        public void Null_Gives_ElseString()
        {
            var result = AdminShellNS.AdminShellUtil.EvalToNonNullString(
                            "some message: {0}", null, "something else");

            Assert.That(result, Is.EqualTo("something else"));
        }
    }
    // ReSharper restore UnusedType.Global

}
