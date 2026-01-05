using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Namotion.Reflection;

namespace MauiTestTree
{
    /// <summary>
    /// Simply inverts a bool value
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    
    public class BoolToColorConverter : IValueConverter
    {
        public Microsoft.Maui.Graphics.Color TrueColor { get; set; } = Colors.Transparent;
        public Microsoft.Maui.Graphics.Color FalseColor { get; set; } = Colors.Transparent;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? TrueColor : FalseColor;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToColorXXXConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b || parameter is not string p)
                return Colors.Transparent;

            var parts = p.Split('|');
            if (parts.Length != 2)
                return Colors.Transparent;

            var trueColor = Color.FromArgb(parts[0]);
            var falseColor = Color.FromArgb(parts[1]);

            return b ? trueColor : falseColor;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EnumToIntParamterizedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Enum e || parameter is not string p)
                return 0;

            int i = System.Convert.ToInt32(e);

            var parts = p.Split('|');
            if (parts.Length < 1)
                return 0;

            i = Math.Min(parts.Length - 1, Math.Max(0, i));

            if (!int.TryParse(parts[i], out var res))
                return 0;

            return res;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolEnumToColorParamterizedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // access
            int? i = null;
            if (value is Enum e)
                i = System.Convert.ToInt32(e);
            if (value is bool b)
                i = System.Convert.ToInt32(b);

            if (!i.HasValue || parameter is not string paramStr || paramStr.Length < 1)
                return Colors.Transparent;

            // split paramStr in parts

            var paramParts = paramStr.Split('|');
            if (paramParts.Length < 1)
                return Colors.Transparent;

            // clamp index, access color param
            i = Math.Min(paramParts.Length - 1, Math.Max(0, i.Value));
            var colorStr = paramParts[i.Value];
            if (colorStr.Length < 1)
                return Colors.Transparent;

            // try lookup
            var foundColor = XamlHelpers.GetDynamicRessource<Color?>(colorStr, null);
            if (foundColor == null)
            {
                if (Color.TryParse(colorStr, out var c))
                    foundColor = c;
            }
            if (foundColor == null)
                return Colors.Transparent;

            // ok
            return foundColor;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
