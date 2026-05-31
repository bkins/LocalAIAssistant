using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace LocalAIAssistant.Platforms.Android.Handlers;

// AutoSize="TextChanges" leaves the native EditText height at WrapContent with no
// content, which Android resolves to 0 on the first layout pass — the field is
// invisible to the touch system even though MAUI's virtual view reports
// MinimumHeightRequest. Calling SetMinimumHeight() directly after the handler
// connects ensures the touch target is the correct size before the user taps.
public class MinHeightEditorHandler : EditorHandler
{
    protected override void ConnectHandler(AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView is Editor { MinimumHeightRequest: > 0 } editor)
        {
            var density     = platformView.Context?.Resources?.DisplayMetrics?.Density ?? 1f;
            var minHeightPx = (int)(editor.MinimumHeightRequest * density);
            platformView.SetMinimumHeight(minHeightPx);
        }
    }
}
