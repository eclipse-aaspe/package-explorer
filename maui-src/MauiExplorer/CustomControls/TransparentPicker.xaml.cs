using System.Collections;

namespace MauiTestTree;

public partial class TransparentPicker : ContentView
{
    public TransparentPicker()
    {
        InitializeComponent();
    }

    // ----------------------------
    // ItemsSource
    // ----------------------------
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(TransparentPicker));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    // ----------------------------
    // ItemDisplayBinding
    // ----------------------------
    public static readonly BindableProperty DisplayMemberPathProperty =
    BindableProperty.Create(
        nameof(DisplayMemberPath),
        typeof(string),
        typeof(TransparentPicker),
        default(string),
        propertyChanged: OnDisplayMemberPathChanged);

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    private static void OnDisplayMemberPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (TransparentPicker)bindable;
        if (control.PartPicker != null && newValue is string path)
        {
            control.PartPicker.ItemDisplayBinding = new Binding(path);
        }
    }

    // ----------------------------
    // SelectedIndex
    // ----------------------------
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(TransparentPicker),
            -1,
            BindingMode.TwoWay);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    // ----------------------------
    // SelectedItem
    // ----------------------------
    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(
            nameof(SelectedItem),
            typeof(object),
            typeof(TransparentPicker),
            null,
            BindingMode.TwoWay);

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    // ----------------------------
    // Events (Picker-compatible)
    // ----------------------------
    public event EventHandler? SelectedIndexChanged;

    // ----------------------------
    // Internal forwarding
    // ----------------------------
    private void OnInternalSelectedIndexChanged(object sender, EventArgs e)
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }
}
