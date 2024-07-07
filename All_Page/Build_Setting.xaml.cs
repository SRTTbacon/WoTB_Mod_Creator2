using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Build_Setting : ContentPage
{
    public enum Build_State
    {
        None,
        Building,
        Uploading,
        Uploaded,
        Failed
    }
    readonly List<List<CVoiceTypeList>> voiceTypes;
    readonly WVS_Load wvsData;
    readonly SE_Preset sePreset;

    readonly Google_Drive drive = new();

    readonly string uploadIDFile = Sub_Code.ExDir + "/Temp0101.dat";

    Build_State state;

    string commandText = "";
    string fileID = "";

    bool bMessageShowing = false;
    bool bUploaded = false;

    public Build_Setting(List<List<CVoiceTypeList>> voiceTypes, WVS_Load wvsData, SE_Preset sePreset)
	{
		InitializeComponent();

        this.voiceTypes = voiceTypes;
        this.wvsData = wvsData;
        this.sePreset = sePreset;

        //ボタン処理
        Server_Build_B.Clicked += Server_Build_B_Clicked;
        Command_Copy_B.Clicked += Command_Copy_B_Clicked;

        Volume_S.ValueChanged += Volume_S_ValueChanged;

        Command_T.Text = "アップロード待ち...";

        ShowVolumeSlider();
    }

    //画面右下部にメッセージを表示
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
            if (Number >= 250)
                Message_T.Opacity -= 0.025;
            await Task.Delay(1000 / 60);
        }
        bMessageShowing = false;
        Message_T.Text = "";
        Message_T.Opacity = 1;
    }

    async void Loop()
    {
        Build_State nowState = state;
        while (true)
        {
            if (state == Build_State.Uploaded)
                break;
            if (nowState == Build_State.Building && state == Build_State.Uploading)
                Message_T.Text = ".wvsファイルを作成しました。\nアップロードしています...";
            nowState = state;

            await Task.Delay(1000 / 10);
        }

        if (state == Build_State.Uploaded)
            Message_T.Text = "プロジェクトファイルをアップロードしました。表示されているコマンドをDiscordのBot(SRTT_Yuna)に送信してください。";
        else if (state == Build_State.Failed)
            Message_Feed_Out("アップロードに失敗しました。時間を置いて再度ビルドしてください。");

        BinaryWriter bw = new(File.OpenWrite(uploadIDFile));
        byte[] fileIDBytes = System.Text.Encoding.UTF8.GetBytes(fileID);
        bw.Write((byte)fileIDBytes.Length);
        bw.Write(fileIDBytes);
        bw.Close();

        Command_T.Text = commandText;

        state = Build_State.None;
    }

    //.wvsを作成しドライブにアップロード
    private async void Server_Build_B_Clicked(object? sender, EventArgs e)
    {
        if (state != Build_State.None)
            return;

        if (voiceTypes == null || wvsData == null || sePreset == null)
            return;

        if (string.IsNullOrWhiteSpace(Project_Name_T.Text))
        {
            Message_Feed_Out("エラー:保存名が入力されていないか、空白の可能性があります。");
            return;
        }
        else if (Project_Name_T.Text.Contains('|'))
        {
            Message_Feed_Out("保存名に'|'を使用することはできません。");
            return;
        }
        else if (Project_Name_T.Text.Contains('\\') || Project_Name_T.Text.Contains('/'))
        {
            Message_Feed_Out("保存名に使用不可な文字が含まれています。");
            return;
        }
        else if (!Sub_Code.IsSafePath(Project_Name_T.Text, true))
        {
            Message_Feed_Out("保存名に使用不可な文字が含まれています。");
            return;
        }

        if (bUploaded || File.Exists(uploadIDFile))
        {
            string message = "'" + Project_Name_T.Text + "'をビルドしますか?";
            string message2 = "以前ビルドしたファイルとコマンドは削除されます。";
            bool result_01 = await DisplayAlert(message, message2, "はい", "いいえ");
            if (!result_01)
                return;
        }

        state = Build_State.Building;

        Command_T.Text = "アップロード待ち...";

        //既にアップロードしたwvsファイルをドライブから削除
        if (File.Exists(uploadIDFile))
        {
            BinaryReader br = new(File.OpenRead(uploadIDFile));
            try
            {
                string id = System.Text.Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                drive.DeleteFile(id);
            }
            catch
            {
            }
            br.Close();
            File.Delete(uploadIDFile);
        }

        //.wvsファイルを作成
        bMessageShowing = false;
        Message_T.Text = ".wvsファイルを作成しています...";
        Message_T.Opacity = 1.0;

        Loop();
        BuildAndUpload();
    }

    private async void Command_Copy_B_Clicked(object? sender, EventArgs e)
    {
        if (state != Build_State.None)
            return;
        try
        {
            if (Command_T.Text == "アップロード待ち...")
            {
                Message_Feed_Out("先に'作成'ボタンを押して下さい。");
                return;
            }
            await Clipboard.SetTextAsync(Command_T.Text);
            Message_Feed_Out("クリップボードにコピーしました。");
        }
        catch
        {
            Message_Feed_Out("コピーに失敗しました。");
        }
    }

    void BuildAndUpload()
    {
        string saveFilePath = Sub_Code.ExDir + "/Generated_Project.wvs";
        Task.Run(() =>
        {
            //SEを含めたプロジェクトを作成
            WVS_Save save = new();
            if (Volume_Set_C.IsChecked)
                save.Add_Sound(voiceTypes, wvsData, Math.Round(Volume_S.Value, 1), Default_Voice_Mode_C.IsChecked);
            else
                save.Add_Sound(voiceTypes, wvsData, 0.0, Default_Voice_Mode_C.IsChecked);
            save.Create(saveFilePath, Project_Name_T.Text, true, sePreset, false);

            state = Build_State.Uploading;

            //ドライブにアップロード
            int uploadID = Sub_Code.RandomValue.Next(1000000, int.MaxValue);
            if (drive.UploadFile(saveFilePath, out string fileID, uploadID + ".wvs"))
            {
                commandText = ".swvs-id " + uploadID;
                this.fileID = fileID;
                state = Build_State.Uploaded;
                bUploaded = true;
            }
            else
            {
                state = Build_State.Failed;
            }
        });
    }

    private void Button_Pressed(object sender, EventArgs e)
    {
        Button button = (Button)sender;
        button.BorderColor = Colors.White;
    }

    private void Button_Released(object sender, EventArgs e)
    {
        Button button = (Button)sender;
        button.BorderColor = Colors.Aqua;
    }

    private void Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        double volume = Math.Round(Volume_S.Value, 1);
        Volume_T.Text = volume + "db";
    }

    private void Volume_Set_C_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Volume_Set_T.Text = "音量を均一にする";
        if (e.Value)
            Volume_Set_T.Text += "(デフォルト:99db)";
        ShowVolumeSlider();
    }

    void ShowVolumeSlider()
    {
        Volume_S.IsVisible = Volume_Set_C.IsChecked;
        Volume_T.IsVisible = Volume_Set_C.IsChecked;
    }
}