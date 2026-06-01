using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using LocalAIAssistant.Platforms.Android.Health;

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
    // Request code for Health Connect permissions result callback.
    private const int HealthPermissionsRequestCode = 1001;

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

    protected override void OnResume()
    {
        base.OnResume();
        RequestHealthPermissionsIfNeeded();
    }

    private void RequestHealthPermissionsIfNeeded()
    {
        // Health Connect requires Android 9+ (API 28).
        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
            return;

        // On Android 13+ (API 33) health permissions are standard runtime permissions and
        // the system routes them through the Health Connect permission UI automatically.
        //
        // TODO: On Android 9–12 (API 28–32) the permission dialog must be launched via
        //       IPermissionController.CreateRequestPermissionResultContract() registered in
        //       OnCreate() as an ActivityResultLauncher. This requires HealthConnect SDK to
        //       be resolvable (see HealthConnectManager.cs Blocker 1-3) and a
        //       KotlinContinuationBridge to first check which permissions are still missing.
        ActivityCompat.RequestPermissions(
            this
          , HealthConnectManager.RequiredPermissions
          , HealthPermissionsRequestCode);
    }
}
