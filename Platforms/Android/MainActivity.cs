using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity.Result;
using AndroidX.Core.View;
using AndroidX.Health.Connect.Client;

namespace LocalAIAssistant;

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

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        ApplySystemBarTheming();

        // Use the HC-specific contract so the Health Connect permission screen is shown
        // on all supported API levels (26+), not the standard runtime-permission dialog
        // which cannot reach HC's own permission store.
        HealthPermissionLauncher = RegisterForActivityResult(
            PermissionController.CreateRequestPermissionResultContract()
          , new NoOpPermissionCallback());
    }

    private void ApplySystemBarTheming()
    {
        if (Window is null)
        {
            return;
        }

        var isDarkTheme = (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;

        var barColor = isDarkTheme
            ? Android.Graphics.Color.ParseColor("#121212")
            : Android.Graphics.Color.ParseColor("#FFFFFF");

        Window.SetStatusBarColor(barColor);
        Window.SetNavigationBarColor(barColor);

        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is not null)
        {
            controller.AppearanceLightStatusBars = !isDarkTheme;
            controller.AppearanceLightNavigationBars = !isDarkTheme;
        }
    }

    // The granted-permission set returned by the HC dialog is ignored here.
    // SettingsViewModel re-checks status via CheckPermissionsAsync() on OnAppearing.
    private sealed class NoOpPermissionCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object result) { }
    }
}
