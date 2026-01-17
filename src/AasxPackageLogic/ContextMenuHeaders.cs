/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_1;

namespace AasxPackageLogic
{
    public enum ContextMenuIconFont { 
        /// <summary>
        /// Regular unicode font, such as OpenSansRegular
        /// </summary>
        Normal,
        
        /// <summary>
        /// Font Awesome
        /// see: https://fontawesome.com/icons
        /// </summary>
        Awesome,
        
        /// <summary>
        /// Google Material font
        /// see: https://fonts.google.com/icons
        /// </summary>
        Material
    }

    /// <summary>
    /// Represents any context menu header, which is shared between the editing functions
    /// of DispEditXXX classes.
    /// </summary>
    public class ContextMenuHeaderBase
    {
        /// <summary>
        /// Identification index, in order to identify the header item in callback
        /// via an int value.
        /// Disable value: -1
        /// </summary>
        public int Id { get; set; } = -1;

        /// <summary>
        /// Which font shall be used to render the <c>IconGlyph</c>
        /// </summary>
        public ContextMenuIconFont IconFont { get; set; } = ContextMenuIconFont.Normal;

        /// <summary>
        /// Which icon shall be shown; see also <c>IconFont</c>
        /// </summary>
        public string IconGlyph { get; set; } = null;

        /// <summary>
        /// The textual header to present
        /// </summary>
        public string Header { get; set; } = "";
    }

    /// <summary>
    /// 'Normal' class to have icon glyph and headers
    /// </summary>
    public class ContextMenuHeader : ContextMenuHeaderBase
    {
        public ContextMenuHeader() { }
        
        public ContextMenuHeader(
            int id,
            string iconGlyph,
            string header,
            ContextMenuIconFont? iconFont = null)
        {
            Id = id;
            IconGlyph = iconGlyph;
            Header = header;
            if (iconFont.HasValue)
                IconFont = iconFont.Value;
        }        
    }

    public class ContextMenuHeaders : List<ContextMenuHeaderBase>
    {
        /// <summary>
        /// Compatibility constructor, to be initialized by an array of strings,
        /// each having icon glyph and header text one after each other.
        /// </summary>
        public ContextMenuHeaders(string[] glyphAndHeaders,
            ContextMenuIconFont iconFont = ContextMenuIconFont.Normal)
        {
            // access
            if (glyphAndHeaders == null || glyphAndHeaders.Length < 2)
                return;

            // each
            for (int i=0; i<glyphAndHeaders.Length/2; i++)
            {
                Add(new ContextMenuHeader(
                    i,
                    glyphAndHeaders[2 * i + 0],
                    glyphAndHeaders[2 * i + 1],
                    iconFont));
            }
        }
    }
}
