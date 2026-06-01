using Android.App;
using Android.Content.PM;
using Android.OS;

namespace LocalAIAssistant;

// Surfaces this app in Health Connect's "Connected apps" list so users can manage permissions.
[IntentFilter(new[] { "android.intent.action.VIEW_PERMISSION_USAGE" }
            , Categories = new[] { "android.intent.category.HEALTH_PERMISSIONS" })]
[Activity(Theme = "@style/Maui.SplashTheme"
        , MainLauncher = true
        , LaunchMode = LaunchMode.SingleTop
        , ConfigurationChanges = ConfigChanges.ScreenSize
                               | ConfigChanges.Orientation
                               | ConfigChanges.UiMode
                               | ConfigChanges.ScreenLayout
                               | ConfigChanges.SmallestScreenSize
                               | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Match status bar and navigation bar to the app's dark theme.
        // Do NOT set DecorView.SystemUiVisibility here — MAUI .NET 9 owns those flags
        // (edge-to-edge layout via WindowCompat.SetDecorFitsSystemWindows).  Overwriting
        // them after base.OnCreate strips LAYOUT_FULLSCREEN / LAYOUT_HIDE_NAVIGATION,
        // which breaks MAUI's inset math and makes all touch targets misaligned.
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#000000"));
        Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#000000"));
    }

    // Health Connect permissions are requested lazily from the Health section in
    // SettingsPage when the user intentionally taps "Connect Health".
    // See HealthConnectManager.RequestPermissionsAsync() for the implementation.
}
