using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.CognitivePlatform.Rendering;

public class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate UserTemplate      { get; set; }
    public DataTemplate AssistantTemplate { get; set; }
    public DataTemplate SystemTemplate    { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is not Message msg)
            return SystemTemplate;

        if (msg.IsTaskList)
            return TaskListTemplate;  // new template

        return msg.Sender?.ToLower() switch
        {
                "user"      => UserTemplate,
                "assistant" => AssistantTemplate,
                "system"    => SystemTemplate,
                _           => SystemTemplate
        };
    }

    public DataTemplate TaskListTemplate { get; set; }

}