using LocalAIAssistant.Presentation;
using Microsoft.Maui.Controls.Shapes;

namespace LocalAIAssistant.Views.Controls;

/// <summary>
/// Renders supported typed presentation models using native MAUI controls.
/// </summary>
public partial class PresentationHost : ContentView
{
    public static readonly BindableProperty ModelProperty = BindableProperty.Create(
        nameof(Model),
        typeof(PresentationModel),
        typeof(PresentationHost),
        propertyChanged: OnModelChanged);

    public PresentationModel? Model
    {
        get => (PresentationModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public PresentationHost()
    {
        InitializeComponent();
    }

    private static void OnModelChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        ((PresentationHost)bindable).Render(newValue as PresentationModel);
    }

    private void Render(PresentationModel? model)
    {
        RootLayout.Children.Clear();

        if (model is null || model.Style != PresentationStyle.Card)
            return;

        RootLayout.Add(new Label
        {
            Text = model.Title,
            FontSize = 16,
            LineBreakMode = LineBreakMode.WordWrap
        });

        if (!string.IsNullOrWhiteSpace(model.Summary))
        {
            var summary = new Label
            {
                Text = model.Summary,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            };
            summary.SetDynamicResource(Label.TextColorProperty, "SecondaryTextColor");
            RootLayout.Add(summary);
        }

        var badges = new HorizontalStackLayout { Spacing = 6 };
        foreach (var badge in model.Badges)
        {
            var badgeLabel = new Label
            {
                Text = badge.Value,
                FontSize = 11
            };
            badgeLabel.SetDynamicResource(Label.TextColorProperty, "PrimaryTextColor");
            SemanticProperties.SetDescription(badgeLabel, badge.Label);

            var badgeBorder = new Border
            {
                Padding = new Thickness(4, 2),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Content = badgeLabel
            };
            badgeBorder.SetDynamicResource(BackgroundColorProperty, "SurfaceAltColor");
            badgeBorder.SetDynamicResource(Border.StrokeProperty, "PrimaryBrush");
            badges.Add(badgeBorder);
        }

        RootLayout.Add(badges);

        if (model.LastModifiedAt is { } lastModifiedAt)
        {
            var updatedAt = new Label
            {
                Text = $"Updated {lastModifiedAt.ToLocalTime():g}",
                FontSize = 11
            };
            updatedAt.SetDynamicResource(Label.TextColorProperty, "SecondaryTextColor");
            RootLayout.Add(updatedAt);
        }
    }
}
