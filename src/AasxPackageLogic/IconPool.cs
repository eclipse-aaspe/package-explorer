using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyUi;

namespace AasxPackageLogic
{
    public static class IconPool
    {
        public static AnyUiImageSourceFont Delete = new("{uc}", "\u2702", AnyUiIconColor.Delete);
        
        public static AnyUiImageSourceFont MoveUp = new("{uc}", "\u25b2", AnyUiIconColor.Normal);
        public static AnyUiImageSourceFont MoveDown = new("{uc}", "\u25bc", AnyUiIconColor.Normal);
    }
}
