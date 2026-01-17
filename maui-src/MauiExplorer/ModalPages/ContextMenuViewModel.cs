using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasxIntegrationBase;
using System.Collections.ObjectModel;
using AnyUi;

namespace MauiTestTree
{
    /// <summary>
    /// This is used by multiple controls. A single menue item.
    /// </summary>
    public class ContextMenuSubstituteMenuItem
    {
        /// <summary>
        /// Numerical, zero-based index
        /// </summary>
        public int Index { get; set; } = 0;

        /// <summary>
        /// Text header for the context menu
        /// </summary>
        public string Header { get; set; } = "";

        /// <summary>
        /// Possible heklp text for the context menu
        /// </summary>
        public string HelpText { get; set; } = "";

        /// <summary>
        /// Checks if help text is present
        /// </summary>
        public bool ShowHelpText { get => HelpText?.HasContent() == true; }

        /// <summary>
        /// Glyph (e.g. unicode) for a possible icon
        /// </summary>
        public string IconGlyph { get; set; } = "";

        /// <summary>
        /// Font family for the icon
        /// </summary>
        public string IconFontAlias { get; set; } = "";

        /// <summary>
        /// Font size for the icon
        /// </summary>
        public int IconFontSize { get; set; } = 16;

        /// <summary>
        /// Checks if icon is present
        /// </summary>
        public bool ShowIcon { get => IconGlyph?.HasContent() == true && IconFontAlias?.HasContent() == true; }
    }

    /// <summary>
    /// This is used by multiple controls. Description of a context menu.
    /// </summary>
    public class ContextMenuSubstituteViewModel
    {
        /// <summary>
        /// If set, title for the dialog panel
        /// </summary>
        public string DialogHeader { get; set; } = "Select an context option";

        /// <summary>
        /// Items to be edited.
        /// </summary>
        public ObservableCollection<ContextMenuSubstituteMenuItem> Items { get; set; } = new();

        /// <summary>
        /// Creates and initializes a context menu from a list of pair of strings.
        /// First of pair is the icon (or empty), second is the textual header of the menu item.
        /// Format of icon could be e.g. "{awe}\uef50", then the AwesomeFont is being used.
        /// For these preset, the display context must be given.
        /// </summary>
        /// <param name="pairs">Pairs of strings</param>
        /// <param name="dc">MAUI display context with icon fonts</param>
        public static ContextMenuSubstituteViewModel CreateNew(
            IEnumerable<string> pairs,
            AnyUiDisplayContextMaui? dc = null,
            double? scaleFontSize = null)
        {
            // start
            var res = new ContextMenuSubstituteViewModel();
            var workPairs = pairs.ToArray();

            // loop
            var nmi = workPairs.Length / 2;
            for (int i = 0; i < nmi; i++)
            {
                // menu item itself
                var menuItem = new ContextMenuSubstituteMenuItem
                {
                    Index = i,
                    Header = "" + workPairs[2 * i + 1],
                };

                // try find icon font
                var input = workPairs[2 * i + 0];
                AnyUiIconFont? fo = null;
                string? glyph = null;
                if (input.StartsWith("{") && input.Contains("}"))
                {
                    var p = input.IndexOf('}');
                    fo = dc?.FindIconFont(input.Substring(1, p - 1));
                    glyph = input.Substring(p + 1);
                }
                else
                {
                    fo = dc?.FindIconFont("uc");
                    glyph = input;
                }

                // fill icon info?
                if (fo?.FontAlias != null)
                {
                    menuItem.IconGlyph = glyph;
                    menuItem.IconFontAlias = fo.FontAlias;
                    menuItem.IconFontSize = (!scaleFontSize.HasValue) ? fo.FontSize
                        : (int)(1.0 * scaleFontSize.Value * fo.FontSize);
                }

                // add
                res.Items.Add(menuItem);
            }

            // ok
            return res;
        }

        /// <summary>
        /// Creates and initializes a context menu from a list of pair of strings.
        /// First of pair is the icon (or empty), second is the textual header of the menu item.
        /// Format of icon could be e.g. "{awe}\uef50", then the AwesomeFont is being used.
        /// For these preset, the display context must be given.
        /// </summary>
        public static ContextMenuSubstituteViewModel CreateNew(
            AnyUiContextMenuHeaderList headers,
            AnyUiDisplayContextMaui? dc = null,
            double? scaleFontSize = null)
        {
            // start
            var res = new ContextMenuSubstituteViewModel();

            // loop
            foreach (var hdr in headers)
            {
                // menu item itself
                var menuItem = new ContextMenuSubstituteMenuItem
                {
                    Index = hdr.Id,
                    Header = hdr.Header,
                    IconGlyph = hdr.IconGlyph,
                };

                // try find icon font
                var input = AnyUiContextMenuHeaderBase.IconFontToTag(hdr.IconFont);
                AnyUiIconFont? fo = null;
                string? glyph = null;
                if (input.StartsWith("{") && input.Contains("}"))
                {
                    var p = input.IndexOf('}');
                    fo = dc?.FindIconFont(input.Substring(1, p - 1));
                }
                else
                {
                    fo = dc?.FindIconFont("uc");
                }

                // fill icon info?
                if (fo?.FontAlias != null)
                {
                    menuItem.IconFontAlias = fo.FontAlias;
                    menuItem.IconFontSize = (!scaleFontSize.HasValue) ? fo.FontSize
                        : (int)(1.0 * scaleFontSize.Value * fo.FontSize);
                }

                // add
                res.Items.Add(menuItem);
            }

            // ok
            return res;
        }

        /// <summary>
        /// Creates and initializes a context menu from a list of pair of strings.
        /// First of pair is the icon (or empty), second is the textual header of the menu item.
        /// Format of icon could be e.g. "{awe}\uef50", then the AwesomeFont is being used.
        /// For these preset, the display context must be given.
        /// </summary>
        /// <param name="pairs">Pairs of strings</param>
        /// <param name="dc">MAUI display context with icon fonts</param>
        public static ContextMenuSubstituteViewModel CreateNew(
            AasxMenu root,
            AnyUiDisplayContextMaui? dc = null,
            double? scaleFontSize = null)
        {
            // start
            var res = new ContextMenuSubstituteViewModel();

            // do it on one level
            for (int i=0; i<root.Count; i++)
            {
                var item = root[i];
                if (item == null)
                    continue;

                if (item is AasxMenuSeparator sep)
                {
                    // nothing yet
                }

                if (item is AasxMenuTextBox mitb)
                {
                    // nothing yet
                }

                if (item is AasxMenuItem mi)
                {
                    // basics
                    var menuItem = new ContextMenuSubstituteMenuItem
                    {
                        Index = i,
                        Header = "" + mi.Header
                    };

                    // icon
                    if (mi.Icon is string input)
                    {
                        // try find icon font
                        AnyUiIconFont? fo = null;
                        string? glyph = null;
                        if (input.StartsWith("{") && input.Contains("}"))
                        {
                            var p = input.IndexOf('}');
                            fo = dc?.FindIconFont(input.Substring(1, p - 1));
                            glyph = input.Substring(p + 1);
                        }
                        else
                        {
                            fo = dc?.FindIconFont("uc");
                            glyph = input;
                        }

                        // fill icon info?
                        if (fo?.FontAlias != null)
                        {
                            menuItem.IconGlyph = glyph;
                            menuItem.IconFontAlias = fo.FontAlias;
                            menuItem.IconFontSize = (!scaleFontSize.HasValue) ? fo.FontSize
                                : (int)(1.0 * scaleFontSize.Value * fo.FontSize);
                        }
                    }

                    // add
                    res.Items.Add(menuItem);
                }
            }

            // ok
            return res;
        }
    }
}
