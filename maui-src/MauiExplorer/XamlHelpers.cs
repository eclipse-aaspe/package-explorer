using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiTestTree
{
    public static class XamlHelpers
    {
        public static T GetDynamicRessource<T>(string name, T defValue)
        {
            if (Application.Current?.Resources.TryGetValue(name, out var res) == true
                && res is T val)
                return val;
            return defValue;
        }
    }
}
