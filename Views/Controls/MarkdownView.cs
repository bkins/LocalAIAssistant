using Markdig;

namespace LocalAIAssistant.Views.Controls;

public class MarkdownView : WebView
{
    public static readonly BindableProperty MarkdownProperty =
        BindableProperty.Create(nameof(Markdown)
                              , typeof(string)
                              , typeof(MarkdownView)
                              , ""
                              , propertyChanged: OnMarkdownChanged);

    public static readonly BindableProperty BubbleColorProperty =
        BindableProperty.Create(nameof(BubbleColor)
                              , typeof(Color)
                              , typeof(MarkdownView)
                              , Color.FromArgb("#7C5CE6")
                              , propertyChanged: OnMarkdownChanged);

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public Color BubbleColor
    {
        get => (Color)GetValue(BubbleColorProperty);
        set => SetValue(BubbleColorProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions()
                                                                                     .DisableHtml()
                                                                                     .Build();

    public MarkdownView()
    {
        BackgroundColor = Colors.Transparent;

        // WebView must have an initial HeightRequest > 0 or it renders
        // as zero-height.  We start at a sensible minimum and resize to
        // content after each load via JavaScript.
        HeightRequest = 44;

        Navigated += OnNavigated;
    }

    private static void OnMarkdownChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not MarkdownView control) return;

        var markdown = control.Markdown ?? "";
        var hexColor = control.BubbleColor.ToHex()[..7];
        var htmlBody = Markdig.Markdown.ToHtml(markdown, Pipeline);

        var html = $$"""
                     <html>
                     <head>
                         <meta name="viewport" content="width=device-width, initial-scale=1.0">
                         <style>
                             html, body {
                                 font-family: Segoe UI, sans-serif;
                                 color: white;
                                 background-color: {{hexColor}};
                                 margin: 0;
                                 padding: 8px;
                                 line-height: 1.5;
                             }

                             h1, h2, h3 { color: #d0b3ff; }

                             p { margin: 8px 0; }

                             a { color: #7aa2ff; }

                             code, pre {
                                 font-family: Consolas, monospace;
                             }

                             code {
                                 background: #1e1e1e;
                                 padding: 3px 6px;
                                 border-radius: 4px;
                                 font-size: 0.95em;
                             }

                             pre {
                                 background: #1e1e1e;
                                 padding: 10px;
                                 border-radius: 6px;
                                 overflow-x: auto;
                             }

                             pre code {
                                 background: none;
                                 padding: 0;
                             }

                             ul { padding-left: 20px; }

                             table { border-collapse: collapse; }

                             th, td {
                                 border: 1px solid #444;
                                 padding: 6px 10px;
                             }
                         </style>
                     </head>
                     <body>
                     {{htmlBody}}
                     </body>
                     </html>
                     """;

        control.Source = new HtmlWebViewSource { Html = html };
    }

    // After each navigation (i.e. after each content update) ask the page
    // how tall it actually is and resize the WebView to match.
    private async void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success) return;

        try
        {
            var result = await EvaluateJavaScriptAsync("document.body.scrollHeight.toString()");

            if (double.TryParse(result, out var contentHeight) && contentHeight > 0)
            {
                // Add a small buffer so the bottom of the content isn't clipped.
                MainThread.BeginInvokeOnMainThread(() => HeightRequest = contentHeight + 16);
            }
        }
        catch
        {
            // If JS evaluation fails for any reason, leave the current HeightRequest as-is.
        }
    }
}