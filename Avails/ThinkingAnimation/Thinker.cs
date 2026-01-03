using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Avails.ThinkingAnimation;

public sealed class Thinker
{
    private readonly Message                  _message;
    private          CancellationTokenSource? _cts;

    public Thinker(Message message)
    {
        _message = message;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _    = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        var index = 0;

        try
        {
            while (!token.IsCancellationRequested)
            {
                _message.Content = ThinkingAnimator.Frames[index];
                index            = (index + 1) % ThinkingAnimator.Frames.Length;
                await Task.Delay(120, token);
            }
        }
        catch (TaskCanceledException)
        {
            // expected
        }
    }
}