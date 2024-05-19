using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace WoTB_Mod_Creator2
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
    public static class AndroidClass
    {
        public static bool CheckExternalStoragePermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R && OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                bool result = Android.OS.Environment.IsExternalStorageManager;
                if (!result)
                {
                    const string manage = Android.Provider.Settings.ActionManageAppAllFilesAccessPermission;
                    Intent intent = new(manage);
                    Android.Net.Uri? uri = Android.Net.Uri.Parse("package:" + AppInfo.Current.PackageName);
                    intent.SetData(uri);
                    Platform.CurrentActivity?.StartActivity(intent);
                }
                return result;
            }
            return true;
        }
        public static string GetExDir()
        {
            Java.IO.File? dirPath = Android.App.Application.Context.GetExternalFilesDir(null);
            if (dirPath != null)
                return dirPath.AbsolutePath;
            return "";
        }
        public static void UpdateApplication(string aplFilePath)
        {
            var context = Android.App.Application.Context;
            Java.IO.File file = new(aplFilePath);

            using Intent install = new(Intent.ActionView);
            if (context.ApplicationContext != null)
            {
                Android.Net.Uri? apkURI = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.ApplicationContext.PackageName + ".fileProvider", file);
                install.SetDataAndType(apkURI, "application/vnd.android.package-archive");
                install.AddFlags(ActivityFlags.NewTask);
                install.AddFlags(ActivityFlags.GrantReadUriPermission);
                install.AddFlags(ActivityFlags.ClearTop);
                install.PutExtra(Intent.ExtraNotUnknownSource, true);
                Platform.CurrentActivity?.StartActivity(install);
            }
        }
    }
}
