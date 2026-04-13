using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Maui.Controls.Shapes;

namespace LocalAIAssistant.Views.Controls;

public class NativeMarkdownView : VerticalStackLayout
{
    public static readonly BindableProperty MarkdownProperty = BindableProperty.Create(nameof(Markdown)
                                                                                     , typeof(string)
                                                                                     , typeof(NativeMarkdownView)
                                                                                     , ""
                                                                                     , propertyChanged: OnMarkdownChanged);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor)
                                                                                      , typeof(Color)
                                                                                      , typeof(NativeMarkdownView)
                                                                                      , Colors.White
                                                                                      , propertyChanged: OnMarkdownChanged);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions()
                                                                                     .Build();

    private static void OnMarkdownChanged( BindableObject bindable
                                         , object         oldValue
                                         , object         newValue )
    {
        if (bindable is NativeMarkdownView control)
        {
            control.Render(newValue as string);
        }
    }

    private void Render( string? markdown )
    {
        Children.Clear();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            // Explicitly re-measure even when clearing to empty so the bubble
            // collapses rather than retaining the height of the previous content.
            InvalidateMeasure();
            return;
        }

        var document = Markdig.Markdown.Parse(markdown
                                            , Pipeline);

        foreach (var uiElement in document.Select(block => CreateUiElementForBlock(block)).OfType<View>())
        {
            Children.Add(uiElement);
        }

        // UX-03: MAUI's CollectionView caches item heights after the first measure.
        // Explicit invalidation here propagates up through the visual tree so that
        // a shorter response shrinks the bubble rather than leaving it at the
        // maximum height reached during the thinking animation.
        InvalidateMeasure();
    }

    private View? CreateUiElementForBlock( Block block )
    {
        return block switch
        {
                ParagraphBlock paragraph  => CreateLabelForParagraph(paragraph)
              , FencedCodeBlock codeBlock => CreateCodeBlock(codeBlock)
              , ListBlock listBlock       => CreateList(listBlock)
              , HeadingBlock heading      => CreateHeading(heading)
              , QuoteBlock quoteBlock     => CreateQuoteBlock(quoteBlock)
              , _                         => null
        };
    }

    private Label CreateLabelForParagraph( LeafBlock block )
    {
        var rawText = ExtractRawText(block);
        
        // 🚨 Detect "complex" content (JSON, stack traces, etc.)
        if (LooksLikeRawText(rawText))
        {
            return new Label
                   {
                           Text                    = rawText
                         , TextColor               = TextColor
                         , FontSize                = 16
                         , LineBreakMode           = LineBreakMode.WordWrap
                         , HorizontalOptions       = LayoutOptions.Fill
                         , HorizontalTextAlignment = TextAlignment.Start
                   };
        }

        var formatted = new FormattedString();
        if (block.Inline != null)
        {
            foreach (var inline in block.Inline)
            {
                AddSpans(formatted.Spans
                       , inline);
            }
        }

        Label label = new Label();
        
        label.FormattedText = formatted;
        
        return label;
    }

    private static bool LooksLikeRawText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains("{")
            && text.Contains("}")
            && text.Contains(":");
    }
    
    private static string ExtractRawText(LeafBlock block)
    {
        if (block.Inline == null)
            return string.Empty;

        var parts = new List<string>();

        foreach (var inline in block.Inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    parts.Add(literal.Content.ToString());
                    break;

                case CodeInline code:
                    parts.Add(code.Content);
                    break;

                case LineBreakInline:
                    parts.Add("\n");
                    break;

                case EmphasisInline emphasis:
                    foreach (var child in emphasis)
                    {
                        if (child is LiteralInline lit)
                            parts.Add(lit.Content.ToString());
                    }
                    break;

                case LinkInline link:
                    parts.Add(link.Url ?? "");
                    break;

                default:
                    // Fallback: try ToString for unknown inline types
                    parts.Add(inline.ToString());
                    break;
            }
        }

        return string.Concat(parts);
    }

    
    private void AddSpans( IList<Span> spans
                         , Inline      inline )
    {
        switch (inline)
        {
            case LiteralInline literal:
                spans.Add(new Span { Text = literal.Content.ToString() });
            
                break;
            
            case EmphasisInline emphasis:
                var style = emphasis.DelimiterCount == 2
                                    ? FontAttributes.Bold
                                    : FontAttributes.Italic;
                
                foreach (var child in emphasis)
                {
                    var span = new Span { FontAttributes = style };
                    
                    if (child is LiteralInline lit) span.Text = lit.Content.ToString();
                    
                    spans.Add(span);
                }

                break;
            case CodeInline code:
                spans.Add(new Span
                          {
                                  Text            = code.Content
                                , FontFamily      = "Consolas"
                                , BackgroundColor = Color.FromArgb("#33000000")
                                , TextColor       = Color.FromArgb("#D0B3FF")
                          });
            
                break;
            
            case LineBreakInline _:
                spans.Add(new Span { Text = "\n" });
                
                break;
            
            case LinkInline link:
                var url = link.Url ?? "";

                spans.Add(new Span
                          {
                                  Text            = url
                                , TextColor       = Color.FromArgb("#4EA1FF")
                                , TextDecorations = TextDecorations.Underline
                          });

                break;

        }
    }

    private View CreateCodeBlock(FencedCodeBlock block)
    {
        var lines = new List<string>();

        foreach (var line in block.Lines.Lines)
        {
            if (line.Slice.Text == null)
                continue;

            var text = line.Slice.Text
                           .Substring(line.Slice.Start, line.Slice.Length);

            lines.Add(text);
        }

        var code = string.Join("\n", lines);

        var codeLabel = new Label
                        {
                                Text          = code.Trim()
                              , FontFamily    = "Consolas"
                              , TextColor     = Color.FromArgb("#CCCCCC")
                              , FontSize      = 14
                              , LineBreakMode = LineBreakMode.WordWrap
                        };

        return new Border
               {
                       BackgroundColor = Color.FromArgb("#1e1e1e")
                     , Padding         = 10
                     , Margin          = new Thickness(0, 8)
                     , StrokeShape     = new RoundRectangle { CornerRadius = 6 }
                     , Content         = codeLabel
               };

        // return new Border
        //        {
        //                BackgroundColor = Color.FromArgb("#1e1e1e")
        //              , Padding         = 10
        //              , Margin          = new Thickness(0, 8)
        //              , StrokeShape     = new RoundRectangle { CornerRadius = 6 }
        //              , Content = new ScrollView
        //                          {
        //                                  Orientation = ScrollOrientation.Horizontal
        //                                , Content     = codeLabel
        //                          }
        //        };
    }

    private View CreateQuoteBlock(QuoteBlock block)
    {
        var stack = new VerticalStackLayout
                    {
                            Padding         = new Thickness(10, 4),
                            BackgroundColor = Color.FromArgb("#22000000"),
                            Margin          = new Thickness(0, 6),
                    };

        foreach (var sub in block)
        {
            var view = CreateUiElementForBlock(sub);
            if (view != null)
                stack.Add(view);
        }

        return stack;
    }

    private View CreateCodeBlock_old_delete( FencedCodeBlock block )
    {
        var code = string.Join(Environment.NewLine
                             , block.Lines.Lines.Take(block.Lines.Count));

        var codeLabel = new Label
                        {
                                Text          = code.Trim()
                              , FontFamily    = "Consolas"
                              , TextColor     = Color.FromArgb("#CCCCCC")
                              , FontSize      = 14
                              , LineBreakMode = LineBreakMode.NoWrap // Don't wrap code, let it scroll
                        };
        codeLabel.HorizontalOptions = LayoutOptions.Start;

        return new Border
               {
                       BackgroundColor = Color.FromArgb("#1e1e1e")
                     , Padding         = 10
                     , Margin          = new Thickness(0, 8)
                     , StrokeShape     = new RoundRectangle { CornerRadius = 6 }
                     , Content = new ScrollView // Wrap the label in a Horizontal ScrollView
                                 {
                                         Orientation = ScrollOrientation.Horizontal
                                       , Content     = codeLabel
                                 }
               };
    }

    private View CreateList(ListBlock block)
    {
        var mainStack = new VerticalStackLayout 
                        { 
                                Margin = new Thickness(10, 0, 10, 0) 
                        };

        foreach (var item in block)
        {
            if (item is not ListItemBlock listItem) continue;
        
            // Use a Grid with a 'Star' column to force the text to wrap
            var grid = new Grid 
                       { 
                               ColumnDefinitions = 
                               { 
                                       new ColumnDefinition { Width = GridLength.Auto }, // Bullet
                                       new ColumnDefinition { Width = GridLength.Star }  // Content
                               },
                               Margin = new Thickness(0, 2)
                       };

            // Bullet point
            grid.Add(new Label 
                     { 
                             Text      = "•", 
                             TextColor = this.TextColor, 
                             Margin    = new Thickness(0, 5, 8, 0) 
                     }, 0);

            // Content
            var contentStack = new VerticalStackLayout();
            foreach (var ui in listItem.Select(subBlock => CreateUiElementForBlock(subBlock)).OfType<View>())
            {
                contentStack.Children.Add(ui);
            }
        
            grid.Add(contentStack, 1);
            mainStack.Add(grid);
        }

        return mainStack;
    }
    private View CreateHeading( HeadingBlock block )
    {
        var label = CreateLabelForParagraph(block);
        label.FontSize       = 20 - (block.Level * 2);
        label.FontAttributes = FontAttributes.Bold;
        label.TextColor      = Color.FromArgb("#D0B3FF");
        label.LineBreakMode  = LineBreakMode.WordWrap;
        
        return label;
    }
}