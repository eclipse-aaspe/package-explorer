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

using IDisposable = System.IDisposable;

namespace AdminShellNS.Tests
{
    class TemporaryDirectory : IDisposable
    {
        public readonly string Path;

        public TemporaryDirectory()
        {
            this.Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName());

            System.IO.Directory.CreateDirectory(this.Path);
        }

        public void Dispose()
        {
            System.IO.Directory.Delete(this.Path, true);
        }
    }
}
