using Microsoft.Maui.Controls.Shapes;

namespace LocalAIAssistant.Views.Controls;

public partial class TagRepeater : ContentView
{
    public static readonly BindableProperty ItemsProperty =
            BindableProperty.Create(nameof(Items), typeof(IEnumerable<string>), typeof(TagRepeater),
                                    propertyChanged: OnItemsChanged);

    public IEnumerable<string> Items
    {
        get => (IEnumerable<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private static void OnItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TagRepeater repeater && newValue is IEnumerable<string> items)
            repeater.Render(items);
    }

    private void Render(IEnumerable<string> items)
    {
        TagContainer.Children.Clear();

        foreach (var tag in items)
        {
            TagContainer.Children.Add(CreateTag(tag));
        }
    }

    private View CreateTag(string tag)
    {
        return new Border
               {
                       StrokeShape     = new RoundRectangle { CornerRadius = 8 },
                       BackgroundColor = Color.FromArgb("#E2E8F0"),
                       Padding         = new Thickness(6, 2),
                       Margin          = new Thickness(3, 2),
                       Content = new Label
                                 {
                                         Text      = tag,
                                         TextColor = Color.FromArgb("#475569"),
                                         FontSize  = 12
                                 }
               };
    }
}