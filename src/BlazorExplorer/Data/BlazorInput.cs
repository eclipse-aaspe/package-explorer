/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// resharper disable all

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AasxPackageLogic;
using BlazorExplorer;

namespace BlazorUI.Data
{
    /// <summary>
    /// Basic features for input handling, e.g. keyboard and mouse.
    /// </summary>
    public static class BlazorInput
    {
        public enum KeyboardModifiers
        {
            None = 0,
            Shift = 1,
            Ctrl = 2
        }

        /// <summary>
        /// Prefer this for click/tap handlers. JS keyboard state only tracks keydown and can leave Ctrl
        /// stuck, so plain clicks looked like Ctrl+click.
        /// </summary>
        public static KeyboardModifiers FromMouseEvent(MouseEventArgs e)
        {
            if (e == null)
                return KeyboardModifiers.None;
            var m = KeyboardModifiers.None;
            if (e.ShiftKey)
                m |= KeyboardModifiers.Shift;
            if (e.CtrlKey || e.MetaKey)
                m |= KeyboardModifiers.Ctrl;
            return m;
        }
    }
}
