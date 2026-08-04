#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;

namespace LocalAIAssistant.Platforms.Android.Health;

[Activity(Name = "com.snikpoh.localaiassistant.PermissionsRationaleActivity"
        , Exported = true
        , Permission = "android.permission.START_VIEW_PERMISSION_USAGE")]
[IntentFilter(new[] { "android.intent.action.VIEW_PERMISSION_USAGE" }
            , Categories = new[] { "android.intent.category.HEALTH_PERMISSIONS" })]
[IntentFilter(new[] { "androidx.health.ACTION_SHOW_PERMISSIONS_RATIONALE" })]
public class PermissionsRationaleActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Redirect to MainActivity to handle app foregrounding/resuming.
        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        StartActivity(intent);
        Finish();
    }
}
#endif
