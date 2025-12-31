namespace MauiTestTree;

public class Viewbox : ContentView
{
	public Viewbox()
	{
	}

    // TODO: Stretch property

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        if (Content == null)
            return base.MeasureOverride(widthConstraint, heightConstraint);

        // Measure child at its desired size
        Content.Measure(double.PositiveInfinity, double.PositiveInfinity);
        var desired = Content.DesiredSize;

        if (desired.Width == 0 || desired.Height == 0)
            return base.MeasureOverride(widthConstraint, heightConstraint);

        var scaleX = widthConstraint / desired.Width;
        var scaleY = heightConstraint / desired.Height;
        var scale = Math.Min(scaleX, scaleY);

        Content.Scale = scale;

        return new Size(
            desired.Width * scale,
            desired.Height * scale);
    }
}