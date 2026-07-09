using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity.Result;
using AndroidX.Health.Connect.Client;

namespace LocalAIAssistant;

// Surfaces this app in Health Connect's "Connected apps" list so users can manage permissions.
[IntentFilter(new[] { "android.intent.action.VIEW_PERMISSION_USAGE" }
            , Categories = new[] { "android.intent.category.HEALTH_PERMISSIONS" })]
[Activity(Theme = "@style/Maui.SplashTheme"
        , MainLauncher = true
        , LaunchMode = LaunchMode.SingleTop
        , WindowSoftInputMode = Android.Views.SoftInput.AdjustResize
        , ConfigurationChanges = ConfigChanges.ScreenSize
                               | ConfigChanges.Orientation
                               | ConfigChanges.UiMode
                               | ConfigChanges.ScreenLayout
                               | ConfigChanges.SmallestScreenSize
                               | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Registered in OnCreate so it is available before the user reaches the Health settings.
    // HealthConnectManager.RequestPermissionsAsync() calls Launch() on this.
    // Must be registered here (not on demand) per ComponentActivity.registerForActivityResult
    // lifecycle rules: the launcher must be registered before the Activity reaches STARTED.
    internal static ActivityResultLauncher? HealthPermissionLauncher { get; private set; }

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

        // Use the HC-specific contract so the Health Connect permission screen is shown
        // on all supported API levels (26+), not the standard runtime-permission dialog
        // which cannot reach HC's own permission store.
        HealthPermissionLauncher = RegisterForActivityResult(
            PermissionController.CreateRequestPermissionResultContract()
          , new NoOpPermissionCallback());
    }

    // The granted-permission set returned by the HC dialog is ignored here.
    // SettingsViewModel re-checks status via CheckPermissionsAsync() on OnAppearing.
    private sealed class NoOpPermissionCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object result) { }
    }
}
