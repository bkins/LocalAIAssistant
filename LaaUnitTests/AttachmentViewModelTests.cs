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

    [Fact]
    public async Task DeleteCommand_Confirms_With_File_Name_Before_Deleting()
    {
        var attachment = new MediaAttachmentDto
        {
            Id = Guid.NewGuid(),
            FileName = "receipt.png",
            ContentType = "image/png"
        };
        string? confirmedFileName = null;
        Guid? deletedId = null;
        var model = new AttachmentViewModel(
            attachment,
            "http://localhost:5274",
            id =>
            {
                deletedId = id;
                return Task.CompletedTask;
            },
            fileName =>
            {
                confirmedFileName = fileName;
                return Task.FromResult(true);
            });

        await model.DeleteCommand.ExecuteAsync(null);

        Assert.Equal("receipt.png", confirmedFileName);
        Assert.Equal(attachment.Id, deletedId);
        Assert.Equal("Remove attachment receipt.png", model.RemoveAccessibilityText);
    }

    [Fact]
    public async Task DeleteCommand_Does_Not_Delete_When_Confirmation_Is_Cancelled()
    {
        var attachment = new MediaAttachmentDto
        {
            Id = Guid.NewGuid(),
            FileName = "keep.png",
            ContentType = "image/png"
        };
        var deleteCount = 0;
        var model = new AttachmentViewModel(
            attachment,
            "http://localhost:5274",
            _ =>
            {
                deleteCount++;
                return Task.CompletedTask;
            },
            _ => Task.FromResult(false));

        await model.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(0, deleteCount);
    }
}
