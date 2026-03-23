/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdminShellNS;

namespace AdminShellNS
{
    /// <summary>
    /// This interface allows accessing a handler to get some access information (e.g. HTTP headers)
    /// suitable to access some AAS registries or repositories.
    /// </summary>
    public interface ISecurityAccessHandler
    {
        /// <summary>
        /// This will be called by the application in order to get an HTTP header data item helping
        /// to access restricted information from an AAS server.
        /// Note: May just lookup already existing data, does not interactively determine new data!
        /// </summary>
        HttpHeaderDataItem LookupAuthenticateHeader(string location);

        /// <summary>
        /// This will be called by the application in order to get an HTTP header data item helping
        /// to access restricted information from an AAS server.
        /// Note: This function is async and may require the GUI thread!
        /// </summary>
        Task<HttpHeaderDataItem> InteractiveDetermineAuthenticateHeader(string location, bool askForUnknown);

        void ClearAllCredentials();
    }
}
