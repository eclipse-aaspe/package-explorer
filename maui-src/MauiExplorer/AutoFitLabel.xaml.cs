using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace MauiTestTree
{
    public partial class AutoFitLabel : ContentView
    {
        //
        // Constructor
        //

        public AutoFitLabel()
        {
            InitializeComponent();
            SizeChanged += (_, _) => RecalculateScale();
            InnerLabel.SizeChanged += (_, _) => RecalculateScale();
            Container.SizeChanged += (_, _) => UpdateClip();
        }

        /// <summary>
        /// Text to be displayed
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(
                nameof(Text),
                typeof(string),
                typeof(AutoFitLabel),
                string.Empty,
                propertyChanged: OnTextChanged);

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (AutoFitLabel)bindable;
            self.InnerLabel.Text = (string)newValue;
            self.RecalculateScale();
        }

        /// <summary>
        /// Text color
        /// </summary>
        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public static readonly BindableProperty TextColorProperty =
            BindableProperty.Create(
                nameof(TextColor),
                typeof(Color),
                typeof(AutoFitLabel),
                default(Color),
                propertyChanged: OnTextColorChanged);

        private static void OnTextColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (AutoFitLabel)bindable;
            self.InnerLabel.TextColor = self.TextColor;
        }

        //
        // Inner behavior
        //

        private void UpdateClip()
        {
            if (Container.Width <= 0 || Container.Height <= 0)
                return;

            Container.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, Container.Width, Container.Height)
            };
        }

        private void RecalculateScale()
        {
            if (Container.Width <= 0 || Container.Height <= 0)
                return;

            // Measure the label with infinite space (intrinsic size)
            var measured = InnerLabel.Measure(double.PositiveInfinity, double.PositiveInfinity);

            if (measured.Width <= 0 || measured.Height <= 0)
                return;

            var scale = Math.Min(
                Container.Width / measured.Width,
                Container.Height / measured.Height);

            InnerLabel.Scale = Math.Min(5.0, 0.99 * scale);

            if (Text == "Asset")
                Trace.WriteLine("Asset-SCALE:" + InnerLabel.Scale);
        }

    }
}