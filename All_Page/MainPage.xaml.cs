using Octokit;
using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page
{
    public partial class MainPage : ContentPage
    {
        readonly Voice_Create voiceCreate_Page = new();

        bool bPageOpened = false;

        public MainPage()
        {
            InitializeComponent();

            //サウンドエンジンを初期化
            Bass.BASS_Init(-1, 48000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            Sub_Code.Initialize();

            //ボタン
            Voice_Create_B.Clicked += Voice_Create_B_Clicked;
            UseSelectPage_C.CheckedChanged += UseSelectPage_C_CheckedChanged;

            Sub_Code.Select_Files_Window.Selected += delegate (string pageName)
            {
                if (pageName == "Voice_Create")
                    voiceCreate_Page.Add_Sound(Sub_Code.Select_Files_Window.Get_Select_Files());
                Sub_Code.Select_Files_Window.Dispose();
            };

            Sub_Code.IsUseSelectPage = UseSelectPage_C.IsChecked;

        }

        private void Voice_Create_B_Clicked(object? sender, EventArgs e)
        {
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
    }
}
