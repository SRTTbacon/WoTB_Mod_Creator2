using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;

#if ANDROID
using Octokit;
#endif

namespace WoTB_Mod_Creator2.All_Page
{
    public partial class MainPage : ContentPage
    {
        readonly Voice_Create voiceCreate_Page = new();

#if ANDROID
        const string APK_FILEPATH = Sub_Code.ANDROID_ROOT + "/Download/Update_Mod_Creator2.apk";
        string apkURL = "";
        bool bDownloading = false;
        bool bUpdating = false;
        bool bCanUpdate = false;
        bool bAPKDeleted = false;
#endif

        bool bPageOpened = false;
        bool bMessageShowing = false;

        public MainPage()
        {
            InitializeComponent();

            //サウンドエンジンを初期化
            Bass.BASS_Init(-1, 48000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            Sub_Code.Initialize();

            Sub_Code.Select_Files_Window.Selected += delegate (string pageName)
            {
                if (pageName == "Voice_Create")
                    voiceCreate_Page.Add_Sound(Sub_Code.Select_Files_Window.Get_Select_Files());
                if (pageName == "SE_Setting" && voiceCreate_Page.SESettingWindow != null)
                    voiceCreate_Page.SESettingWindow.Add_Sound(Sub_Code.Select_Files_Window.Get_Select_Files());
                Sub_Code.Select_Files_Window.Dispose();
            };

            //ボタン
            Voice_Create_B.Clicked += Voice_Create_B_Clicked;
            Other_Sound_B.Clicked += Other_Sound_B_Clicked;
            Music_Player_B.Clicked += Music_Player_B_Clicked;
            Tools_B.Clicked += Tools_B_Clicked;
            UseSelectPage_C.CheckedChanged += UseSelectPage_C_CheckedChanged;

            Sub_Code.IsUseSelectPage = UseSelectPage_C.IsChecked;

            Develop_T.Text = "Developed by SRTTbacon - V" + Sub_Code.APP_VERSION;

            //アップデート関連のUIはあらかじめ非表示に
            Update_T.IsVisible = false;
            Update_B.IsVisible = false;

#if ANDROID
            Update_B.Clicked += Update_B_Clicked;

            //インターネットにアクセスできる環境であればアップデートがあるか確認する
            NetworkAccess accessType = Connectivity.Current.NetworkAccess;
            if (accessType == NetworkAccess.Internet)
            {
                CheckUpdate();
            }
#endif
        }

        private void Tools_B_Clicked(object? sender, EventArgs e)
        {
            Message_Feed_Out("現在のバージョンではこの機能は利用できません。");
        }

        private void Music_Player_B_Clicked(object? sender, EventArgs e)
        {
            Message_Feed_Out("現在のバージョンではこの機能は利用できません。");
        }

        private void Other_Sound_B_Clicked(object? sender, EventArgs e)
        {
            Message_Feed_Out("現在のバージョンではこの機能は利用できません。");
        }

        //画面下部にメッセージを表示
        private async void Message_Feed_Out(string message)
        {
            //テキストが一定期間経ったらフェードアウト
            if (bMessageShowing)
            {
                bMessageShowing = false;
                await Task.Delay(1000 / 59);
            }
            Message_T.Text = message;
            bMessageShowing = true;
            Message_T.Opacity = 1;
            int Number = 0;
            while (Message_T.Opacity > 0 && bMessageShowing)
            {
                Number++;
                if (Number >= 120)
                    Message_T.Opacity -= 0.025;
                await Task.Delay(1000 / 60);
            }
            bMessageShowing = false;
            Message_T.Text = "";
            Message_T.Opacity = 1;
        }

        private void Voice_Create_B_Clicked(object? sender, EventArgs e)
        {
#if ANDROID
            if (bUpdating)
            {
                Message_Feed_Out("アップデート中に他の操作を行うことはできません。");
                return;
            }
#endif
            if (bPageOpened)
                return;
            Navigation.PushAsync(voiceCreate_Page);
            bPageOpened = true;
        }

        private void ContentPage_Appearing(object sender, EventArgs e)
        {
            bPageOpened = false;
        }

        private void UseSelectPage_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            Sub_Code.IsUseSelectPage = e.Value;
        }

        private void Button_Pressed(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            button.BorderColor = Colors.Red;
        }

        private void Button_Released(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            button.BorderColor = Colors.Transparent;
        }

#if ANDROID
        //アップデートをチェック
        private async void CheckUpdate()
        {
            //GithubのAPKを通して最新のリリースバージョンと現在のバージョンを比較する
            GitHubClient client = new(new ProductHeaderValue("WoTB_Mod_Creator2"));
            Release releases = await client.Repository.Release.GetLatest("SRTTbacon", "WoTB_Mod_Creator2");
            string lastVersion = releases.Name;
            //現在より上のバージョンが存在していればフラグを立てる
            if (Sub_Code.Get_Version_To_Double(lastVersion) > Sub_Code.Get_Version_To_Double(Sub_Code.APP_VERSION))
            {
                foreach (ReleaseAsset release in releases.Assets)
                {
                    if (Path.GetExtension(release.BrowserDownloadUrl) == ".apk")
                    {
                        bCanUpdate = true;
                        apkURL = release.BrowserDownloadUrl;
                        break;
                    }
                }
            }

            //アップデートがあればUIを表示
            if (bCanUpdate)
            {
                Update_T.Text = "アプリのアップデートがあります。";
                Update_T.IsVisible = true;
                Update_B.IsVisible = true;
            }
        }
        //ダウンロードが終了したらインストール画面を出す
        private async void Loop()
        {
            double nextFrame = Environment.TickCount;
            float period = 1000f / 30f;
            while (bUpdating)
            {
                double tickCount = Environment.TickCount;
                if (tickCount < nextFrame)
                {
                    if (nextFrame - tickCount > 1)
                        await Task.Delay((int)(nextFrame - tickCount));
                    continue;
                }

                if (!bDownloading)
                {
                    Update_T.Text = "apkファイルをインストールできます。";
                    bUpdating = false;
                }

                if (Environment.TickCount >= nextFrame + (double)period)
                {
                    nextFrame += period;
                    continue;
                }
                nextFrame += period;
            }
        }

        //アップデートボタン
        private async void Update_B_Clicked(object? sender, EventArgs e)
        {
            if (bUpdating)
                return;

            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("アクセス許可を行ってください。");
                return;
            }

            if (!bAPKDeleted)
            {
                if (File.Exists(APK_FILEPATH))
                    File.Delete(APK_FILEPATH);
                bAPKDeleted = true;
            }

            bUpdating = true;
            bDownloading = true;
            Loop();

            if (!File.Exists(APK_FILEPATH))
            {
                bool bDownloadMode = await DisplayAlert("確認", "最新バージョンのapkファイルをダウンロードしますか?", "はい", "いいえ");
                if (!bDownloadMode)
                {
                    bDownloading = false;
                    bUpdating = false;
                    return;
                }
                Update_T.Text = "apkファイルをダウンロードしています...";
                DownloadAPK();
            }
            else
            {
                bool bDownloadMode = await DisplayAlert("確認", "apkファイルは既にダウンロードされています。更新しますか?", "はい", "いいえ");
                bDownloading = false;
                bUpdating = false;

                if (!bDownloadMode)
                    return;

                try
                {
                    AndroidClass.UpdateApplication(APK_FILEPATH);
                }
                catch (Exception e1)
                {
                    Sub_Code.ErrorLogWrite(e1.Message);
                    Message_Feed_Out("エラーが発生しました。" + e1.Message);
                }
            }
        }
        private async void DownloadAPK()
        {
            await DownloadManager.DownloadAsync(APK_FILEPATH, apkURL);
            bDownloading = false;
        }
#endif
    }
}
