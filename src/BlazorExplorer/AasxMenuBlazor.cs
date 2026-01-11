/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BlazorUI
{
    /// <summary>
    /// This class "converts" the AasxMenu struncture into Blazor menues
    /// </summary>
    public class AasxMenuBlazor
    {
        //
        // Private
        //
        protected DoubleSidedDict<string, AasxMenuItemBase> _menuItems
            = new DoubleSidedDict<string, AasxMenuItemBase>();

        protected DoubleSidedDict<AasxMenuItemBase, object> _blazorItems
            = new DoubleSidedDict<AasxMenuItemBase, object>();

        public AasxMenu Menu { get => _menu; }
        private AasxMenu _menu = new AasxMenu();

        public void LoadAndRender(
            AasxMenu menuInfo)
        {
            _menu = menuInfo;
            _menuItems.Clear();
            _blazorItems.Clear();

            if (_menu == null)
                return;

            foreach (var mi in _menu.FindAll())
                if (mi.Name.HasContent() && !_menuItems.Contains1(mi.Name.Trim().ToLower()))
                    _menuItems.AddPair(mi.Name.Trim().ToLower(), mi);
        }

        public bool IsChecked(string name)
        {

            // directly look up menu item and return *internal* state
            var mi = _menuItems.Get2OrDefault(name?.Trim().ToLower());
            if (mi is AasxMenuItem mii && mii.IsCheckable)
                return mii.IsChecked;
            return false;

        }

        public void SetChecked(string name, bool state)
        {
            var mi = _menuItems.Get2OrDefault(name?.Trim().ToLower());
            //if (wpf != null)
            //    wpf.IsChecked = state;
            if (mi is AasxMenuItem mii && mii.IsCheckable)
                mii.IsChecked = state;
        }
    }
}
