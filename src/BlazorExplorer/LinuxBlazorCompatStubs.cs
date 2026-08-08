// Linux Blazor port compatibility layer for WPF shell types referenced by the
// upstream Blazor project.
using System;
using System.Collections.Generic;

namespace System.Windows.Controls
{
    public class Control { }
}

namespace System.Windows.Input
{
    public class CommandBindingCollection : List<object> { }
    public class InputBindingCollection : List<object> { }

    public class KeyEventArgs : EventArgs
    {
        public object Key { get; set; }
    }
}

namespace System.Windows.Media.Imaging
{
    public class BitmapSource { }

    public class BitmapImage : BitmapSource
    {
        public BitmapImage() { }
        public BitmapImage(Uri uri) { UriSource = uri; }
        public Uri UriSource { get; set; }
    }
}

namespace ExhaustiveMatching
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class ClosedAttribute : Attribute { }
}
