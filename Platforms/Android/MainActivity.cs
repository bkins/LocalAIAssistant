using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace LocalAIAssistant;

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
        
        Window?.SetSoftInputMode(SoftInput.AdjustResize);
        
        // Match status bar to app dark theme
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#000000"));
        Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#000000"));
        
        // Ensure status bar icons are light (important!)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            var decorView = Window.DecorView;
            decorView.SystemUiVisibility =
                    (StatusBarVisibility)(SystemUiFlags.LayoutStable);

        }
        else
        {
            Window.DecorView.SystemUiVisibility =
                    (StatusBarVisibility)SystemUiFlags.Visible;
        }
    }
}


