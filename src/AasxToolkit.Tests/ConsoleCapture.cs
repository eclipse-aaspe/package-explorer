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

using Console = System.Console;
using IDisposable = System.IDisposable;
using StringWriter = System.IO.StringWriter;
using TextWriter = System.IO.TextWriter;

namespace AasxToolkit.Test
{
    public class ConsoleCapture : IDisposable
    {
        private readonly StringWriter _writerOut;
        private readonly StringWriter _writerError;
        private readonly TextWriter _originalOutput;
        private readonly TextWriter _originalError;

        public ConsoleCapture()
        {
            _writerOut = new StringWriter();
            _writerError = new StringWriter();

            _originalOutput = Console.Out;
            _originalError = Console.Error;

            Console.SetOut(_writerOut);
            Console.SetError(_writerError);
        }

        public string Output()
        {
            return _writerOut.ToString();
        }

        public string Error()
        {
            return _writerError.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            Console.SetError(_originalError);
            _writerOut.Dispose();
            _writerOut.Dispose();
        }
    }
}
