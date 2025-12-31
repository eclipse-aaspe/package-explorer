using System.Diagnostics;
using MauiTestTree;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

public class VisualTreeDebugger
{
    public List<IView> Elements = new();

    public enum Attributes { 
        None    = 0x0000,
        Text    = 0x0001,
        Margin  = 0x0010,
        Padding = 0x0020,
        All     = 0xffff
    }

    public void Dump(IView view, Attributes attr, int indent = 0)
    {
        if (view == null)
            return;

        string pad = new string(' ', indent * 2);

        string name = view is VisualElement ve &&
                      !string.IsNullOrEmpty(ve.AutomationId)
            ? $" (AutomationId={ve.AutomationId})"
            : string.Empty;

        var attrSum = new List<string>();
        if ((attr & Attributes.Margin) > 0)
            attrSum.Add($"M={view.Margin.Left},{view.Margin.Top},{view.Margin.Right},{view.Margin.Bottom}");

        if ((attr & Attributes.Text) > 0)
        {
            if (view is Label l)
                attrSum.Add($"T='{l.Text}'");
            if (view is Button b)
                attrSum.Add($"T='{b.Text}'");
            if (view is Entry en)
                attrSum.Add($"T='{en.Text}'");
            if (view is Editor ed)
                attrSum.Add($"T='{ed.Text?.Replace("\r", "").Replace("\n","").Replace("\t","")}'");
            if (view is LabelledCheckBox cb)
                attrSum.Add($"T='{cb.Text}'");
        }

        if ((attr & Attributes.Padding) > 0)
        {
            if (view is Grid g)
                attrSum.Add($"P='{g.Padding.Left},{g.Padding.Top},{g.Padding.Right},{g.Padding.Bottom}'");
            if (view is StackLayout sl)
                attrSum.Add($"P='{sl.Padding.Left},{sl.Padding.Top},{sl.Padding.Right},{sl.Padding.Bottom}'");
            if (view is VerticalStackLayout vsl)
                attrSum.Add($"P='{vsl.Padding.Left},{vsl.Padding.Top},{vsl.Padding.Right},{vsl.Padding.Bottom}'");
            if (view is HorizontalStackLayout hsl)
                attrSum.Add($"P='{hsl.Padding.Left},{hsl.Padding.Top},{hsl.Padding.Right},{hsl.Padding.Bottom}'");
            if (view is FlexLayout fl)
                attrSum.Add($"P='{fl.Padding.Left},{fl.Padding.Top},{fl.Padding.Right},{fl.Padding.Bottom}'");
            if (view is AbsoluteLayout al)
                attrSum.Add($"P='{al.Padding.Left},{al.Padding.Top},{al.Padding.Right},{al.Padding.Bottom}'");
        }

        Trace.WriteLine($"{pad}[{Elements.Count:D3}]{view.GetType().Name}{name} {string.Join(' ', attrSum)}");
        Elements.Add(view);

        switch (view)
        {
            case Layout layout:
                foreach (var child in layout.Children)
                    Dump(child, attr, indent + 1);
                break;

            case ContentView contentView:
                Dump(contentView.Content, attr, indent + 1);
                break;

            case ScrollView scrollView:
                Dump(scrollView.Content, attr, indent + 1);
                break;
        }
    }
}
