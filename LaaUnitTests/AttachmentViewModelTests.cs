using LocalAIAssistant.Core.Media;
using LocalAIAssistant.Knowledge.Journals.ViewModels;

namespace LaaUnitTests;

public class AttachmentViewModelTests
{
    [Fact]
    public void Preview_And_Open_Use_File_Endpoint_Not_Server_Storage_Path()
    {
        var id = Guid.NewGuid();
        var attachment = new MediaAttachmentDto { Id = id, FileName = "Entry1.jpg", ContentType = "image/jpeg", StoragePath = "" };

        var model = new AttachmentViewModel(attachment, "http://localhost:5274", _ => Task.CompletedTask);

        Assert.Equal($"http://localhost:5274/api/media/{id}/file", model.StoragePath);
        Assert.True(model.IsImage);
    }
}
