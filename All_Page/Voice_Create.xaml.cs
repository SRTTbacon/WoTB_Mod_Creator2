using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

//サウンド
public class CVoiceSoundList(CVoiceSoundSetting voiceSetting)
{
    public CVoiceSoundSetting VoiceSoundSetting { get; set; } = voiceSetting;
    public string Name_Text => Path.GetFileName(VoiceSoundSetting.FilePath);
    public string Name_Probability => Path.GetFileName(VoiceSoundSetting.FilePath) + " | 優先度:" + (int)VoiceSoundSetting.Probability;
}

//サウンドイベントクラス
public class CVoiceTypeList(string eventName, int index)
{
    public string Name { get; set; } = eventName;
    public string Name_Text => Name + " : " + Count + "個";
    public int Count => TypeSetting.Sounds.Count;
    public int Index { get; private set; } = index;
    public Color Name_Color => Count == 0 ? Color.FromArgb("#BFFF2C8C") : Colors.Aqua;
    public CVoiceTypeSetting TypeSetting = new();
}

public partial class Voice_Create : ContentPage
{
    //イベント設定
    readonly Voice_Create_Sound_Setting soundSettingWindow = new();

    //サウンドイベント
    readonly List<CVoiceSoundList> voiceSounds = [];
    readonly List<List<CVoiceTypeList>> voiceTypes = [];

    ViewCell? lastVoiceTypeCell = null;
    ViewCell? lastVoiceSoundCell = null;

    readonly WVS_Load wvsFile = new();

    static readonly string[] audioExtension = ["audio/*"];
    string projectName = "";

    int nowTypePage = 0;
    int streamHandle = 0;

    bool bOtherPageOpened = false;
    bool bMessageShowing = false;


    public Voice_Create()
	{
		InitializeComponent();
        Load_B.Clicked += Load_B_Click;
        Save_B.Clicked += Save_B_Click;
        Voice_Type_Back_B.Clicked += Voice_Type_Back_B_Click;
        Voice_Type_Next_B.Clicked += Voice_Type_Next_B_Click;
        Sound_Add_B.Clicked += Sound_Add_B_Click;
        Sound_Delete_B.Clicked += Sound_Delete_B_Click;
        Create_B.Clicked += OpenEventSetting_B_Click;
        Clear_B.Clicked += Clear_B_Click;
        Init_Voice_Type();
        Set_Item_Type();
        Voice_Type_L.ItemsSource = voiceTypes[0];
    }
    private void Init_Voice_Type()
    {
        voiceSounds.Clear();
        voiceTypes.Clear();
        for (int i = 0; i  < 3; i++)
            voiceTypes.Add([]);

        //1ページ目
        List<CVoiceTypeList> voiceTypeOne = voiceTypes[0];
        voiceTypeOne.Add(new("味方にダメージ", voiceTypeOne.Count));
        voiceTypeOne.Add(new("弾薬庫", voiceTypeOne.Count));
        voiceTypeOne.Add(new("敵への無効弾", voiceTypeOne.Count));
        voiceTypeOne.Add(new("敵への貫通弾", voiceTypeOne.Count));
        voiceTypeOne.Add(new("敵への致命弾", voiceTypeOne.Count));
        voiceTypeOne.Add(new("敵への跳弾", voiceTypeOne.Count));
        voiceTypeOne.Add(new("車長負傷", voiceTypeOne.Count));
        voiceTypeOne.Add(new("操縦手負傷", voiceTypeOne.Count));
        voiceTypeOne.Add(new("敵炎上", voiceTypeOne.Count));
        voiceTypeOne.Add(new("敵撃破", voiceTypeOne.Count));
        voiceTypeOne.Add(new("エンジン破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("エンジン大破", voiceTypeOne.Count));
        voiceTypeOne.Add(new("エンジン復旧", voiceTypeOne.Count));
        voiceTypeOne.Add(new("自車両火災", voiceTypeOne.Count));
        voiceTypeOne.Add(new("自車両消火", voiceTypeOne.Count));
        voiceTypeOne.Add(new("燃料タンク破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("主砲破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("主砲大破", voiceTypeOne.Count));
        voiceTypeOne.Add(new("主砲復旧", voiceTypeOne.Count));
        voiceTypeOne.Add(new("砲手負傷", voiceTypeOne.Count));
        voiceTypeOne.Add(new("装填手負傷", voiceTypeOne.Count));
        voiceTypeOne.Add(new("通信機破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("通信手負傷", voiceTypeOne.Count));
        voiceTypeOne.Add(new("戦闘開始", voiceTypeOne.Count));
        voiceTypeOne.Add(new("観測装置破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("観測装置大破", voiceTypeOne.Count));
        voiceTypeOne.Add(new("観測装置復旧", voiceTypeOne.Count));
        voiceTypeOne.Add(new("履帯破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("履帯大破", voiceTypeOne.Count));
        voiceTypeOne.Add(new("履帯復旧", voiceTypeOne.Count));
        voiceTypeOne.Add(new("砲塔破損", voiceTypeOne.Count));
        voiceTypeOne.Add(new("砲塔大破", voiceTypeOne.Count));
        voiceTypeOne.Add(new("砲塔復旧", voiceTypeOne.Count));
        voiceTypeOne.Add(new("自車両大破", voiceTypeOne.Count));
    }

    //画面下部にメッセージを表示
    private async void Message_Feed_Out(string Message)
    {
        //テキストが一定期間経ったらフェードアウト
        if (bMessageShowing)
        {
            bMessageShowing = false;
            await Task.Delay(1000 / 59);
        }
        Message_T.Text = Message;
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

    //リストの状態を更新
    private void Set_Item_Type(int nextPage = -1)
    {
        if (nextPage == -1)
            nextPage = nowTypePage;
        bool bChangePage = nowTypePage != nextPage;
        nowTypePage = nextPage;

        Voice_Type_L.SelectedItem = null;
        Voice_Type_L.ItemsSource = null;
        Voice_Type_L.ItemsSource = voiceTypes[nowTypePage];
        Voice_Type_Page_T.Text = "イベントリスト" + (nowTypePage + 1);
        Sound_File_L.SelectedItem = null;
        Sound_File_L.ItemsSource = null;

        //ページが前回と変わっていないかつ、項目が選択された状態であればイベント内のサウンドファイルをリストに表示
        if (bChangePage && Voice_Type_L.SelectedItem is CVoiceTypeList selectedType)
        {
            Voice_Type_L.SelectedItem = selectedType;

            voiceSounds.Clear();
            foreach (CVoiceSoundSetting voiceSetting in selectedType.TypeSetting.Sounds)
            {
                voiceSounds.Add(new(voiceSetting));
            }

            Sound_File_L.ItemsSource = voiceSounds;
        }
    }

    //サウンドイベントを選択
    private void VoiceTypeViewCell_Tapped(object sender, EventArgs e)
    {
        if (lastVoiceTypeCell != null)
            lastVoiceTypeCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        if (viewCell.View != null && Voice_Type_L.SelectedItem != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastVoiceTypeCell = viewCell;
            CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
            voiceSounds.Clear();
            foreach (CVoiceSoundSetting soundSetting in typeList.TypeSetting.Sounds)
                voiceSounds.Add(new(soundSetting));
            if (voiceSounds.Count == 0)
                Sound_File_L.ItemsSource = null;
            else 
                Sound_File_L.ItemsSource = voiceSounds;
        }
    }
    //サウンドファイルを選択
    private void Sound_List_Tapped(object sender, EventArgs e)
    {
        if (lastVoiceSoundCell != null)
            lastVoiceSoundCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        if (viewCell.View != null && Sound_File_L.SelectedItem != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastVoiceSoundCell = viewCell;
        }

        if (Sound_File_L.SelectedItem == null)
            return;

        if (streamHandle != 0)
            Bass.BASS_StreamFree(streamHandle);

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Sound_File_L.SelectedItem).VoiceSoundSetting;
        //サウンドが.wvsファイル内に存在している場合
        if (voiceSoundSetting.IsBinarySound)
        {
            byte[]? soundBytes = wvsFile.Load_Sound(voiceSoundSetting.StreamPosition);
            IntPtr soundPtr = IntPtr.Parse(soundBytes);
            if (soundBytes != null)
            {
                int handle = Bass.BASS_StreamCreateFile(soundPtr, 0, 0, BASSFlag.BASS_STREAM_DECODE);
                streamHandle = Un4seen.Bass.AddOn.Fx.BassFx.BASS_FX_TempoCreate(handle, BASSFlag.BASS_STREAM_DECODE);
            }
        }
        else
        {
            int handle = Bass.BASS_StreamCreateFile(voiceSoundSetting.FilePath, 0, 0, BASSFlag.BASS_STREAM_DECODE);
            streamHandle = Un4seen.Bass.AddOn.Fx.BassFx.BASS_FX_TempoCreate(handle, BASSFlag.BASS_STREAM_DECODE);
        }
    }

    //.wvsファイルをロード
    private async void Load_B_Click(object? sender, EventArgs e)
    {
        //他のウィンドウが開かれていたらスルー
        if (bOtherPageOpened)
            return;

        //セーブさいている.wvsファイルを列挙
        List<string> savedNames = [];
        if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
            _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");
        foreach (string Files in Directory.GetFiles(Sub_Code.ExDir + "/Saves", "*.wvs", SearchOption.TopDirectoryOnly))
            savedNames.Add(Path.GetFileNameWithoutExtension(Files));

        if (savedNames.Count == 0)      //セーブファイルが1つも存在しない
        {
            await DisplayActionSheet("プロジェクトを選択してください。", "キャンセル", null, ["!プロジェクトが存在しません!"]);
            savedNames.Clear();
        }
        else                            //セーブファイルがあれば選択ウィンドウを表示
        {
            bOtherPageOpened = true;
            string extention = ".wvs";
            Sub_Code.Select_Files_Window.Window_Show("Voice_Create_Load", Sub_Code.ExDir + "/Saves", extention, true, false);
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);
        }
    }

    //プロジェクトを保存
    private async void Save_B_Click(object? sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("セーブ", "プロジェクト名を指定してください。", "決定", "キャンセル", null, -1, null, projectName);
        if (result != null)
        {
            if (!Sub_Code.IsSafePath(result, true))
            {
                Message_Feed_Out("エラー:使用できない文字が含まれています。");
                return;
            }
            else if (File.Exists(Sub_Code.ExDir + "/Saves/" + result + ".wvs"))
            {
                bool result_01 = await DisplayAlert("セーブデータを上書きしますか?", null, "はい", "いいえ");
                if (!result_01)
                    return;
            }
            if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
                _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");
            WVS_Save Save = new();
            Save.Add_Sound(voiceTypes, wvsFile);
            wvsFile.Dispose();
            Save.Create(Sub_Code.ExDir + "/Saves/" + result + ".wvs", result);
            Save.Dispose();
            Voice_Load_From_File(Sub_Code.ExDir + "/Saves/" + result + ".wvs");
            Message_Feed_Out("セーブしました。");
        }
    }
    //プロジェクトをロード
    private void Voice_Load_From_File(string filePath)
    {
        try
        {
            //音声を配置
            Init_Voice_Type();
            WVS_Load.WVS_Result wvsResult = WVS_Load.IsBlitzWVSFile(filePath);
            if (wvsResult == WVS_Load.WVS_Result.No_Exist_File)
            {
                Sub_Code.Show_Message("エラー:ファイルが存在しません。");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Header)
            {
                Sub_Code.Show_Message("エラー:ヘッダーが異なります。");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Version)
            {
                Sub_Code.Show_Message("エラー:セーブファイルのバージョンが古すぎます。");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.WoTMode)
            {
                Sub_Code.Show_Message("エラー:WoT用のセーブファイルのためロードできません。");
                return;
            }
            else
            {
                wvsResult = wvsFile.WVS_Load_File(filePath, voiceTypes);
                if (wvsResult == WVS_Load.WVS_Result.OK)
                {
                    projectName = wvsFile.ProjectName;
                    Sub_Code.Show_Message(Path.GetFileName(filePath) + "をロードしました。");
                }
                else
                    throw new Exception(".wvsファイルが破損しています。");
            }
            Set_Item_Type();
        }
        catch (Exception e)
        {
            Sub_Code.Show_Message("エラー:" + e.Message);
            Init_Voice_Type();
            Set_Item_Type();
            wvsFile.Dispose();
        }
    }

    //前のページへ
    private void Voice_Type_Back_B_Click(object? sender, EventArgs e)
    {
        if (nowTypePage > 0)
        {
            Set_Item_Type(nowTypePage - 1);
        }
    }
    //次のページへ
    private void Voice_Type_Next_B_Click(object? sender, EventArgs e)
    {
        if (nowTypePage < 2)
        {
            Set_Item_Type(nowTypePage + 1);
        }
    }

    //サウンドを追加
    private async void Sound_Add_B_Click(object? sender, EventArgs e)
    {
        //エラー回避
        if (Voice_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("イベント名が選択されていません。");
            return;
        }

        if (bOtherPageOpened)
            return;

        //ファイル閲覧の権限を持っているかつ、ホーム画面でオリジナルの選択画面を有効にした場合はその選択画面でファイルを選択
        if (Sub_Code.IsUseSelectPage)
        {
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("アクセス許可を行ってください。");
                return;
            }
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //対応している拡張子
            Sub_Code.Select_Files_Window.Window_Show("Voice_Create", "", extension);    //選択画面を初期化
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);              //選択画面を開く
        }
        else
        {
            FilePickerFileType customFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, audioExtension }
            });
            PickOptions options = new()
            {
                FileTypes = customFileType,
                PickerTitle = "サウンドファイルを選択してください。"
            };
            IEnumerable<FileResult> result = await FilePicker.PickMultipleAsync(options);
            if (result != null)
            {
                List<string> files = [];
                foreach (FileResult fileResult in result)
                    files.Add(fileResult.FullPath);
                Add_Sound(files);
            }
        }
    }
    //サウンドを追加
    public void Add_Sound(List<string> files)
    {
        CVoiceTypeList typeList = ((CVoiceTypeList)Voice_Type_L.SelectedItem);
        int alreadyVoiceCount = 0;
        int addedVoiceCount = 0;
        foreach (string filePath in files)
        {
            bool bExist = false;
            //データが全く同じファイルが存在するか調べる
            foreach (CVoiceSoundSetting voiceSetting in typeList.TypeSetting.Sounds)
            {
                if (voiceSetting.FilePath != null && voiceSetting.FilePath == filePath)
                {
                    bExist = true;
                    alreadyVoiceCount++;
                    break;
                }
            }
            if (bExist)
                continue;

            //イベントにサウンドを追加
            typeList.TypeSetting.Sounds.Add(new(filePath));
            addedVoiceCount++;
        }
        //リストUIを更新
        Set_Item_Type();

        if (alreadyVoiceCount > 0 && addedVoiceCount == 0)
            Message_Feed_Out("既に追加されているため" + alreadyVoiceCount + "個のファイルをスキップしました。");
        else if (alreadyVoiceCount > 0 && addedVoiceCount > 0)
            Message_Feed_Out("既に追加されているため" + alreadyVoiceCount + "個のファイルをスキップし、" + addedVoiceCount + "個のファイルを追加しました。");
        else
            Message_Feed_Out(addedVoiceCount + "個のファイルをイベントに追加しました。");
    }

    //フォーカスがこの画面に移ったら
    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bOtherPageOpened = false;
    }

    //サウンドの削除ボタン
    private void Sound_Delete_B_Click(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        if (Voice_Type_L.SelectedItem == null)
        {
            Sub_Code.Show_Message("イベント名が選択されていません。");
            return;
        }
        if (Sound_File_L.SelectedItem == null)
        {
            Sub_Code.Show_Message("削除したいサウンドを選択してください。");
            return;
        }
        int typeIndex = ((CVoiceTypeList)Voice_Type_L.SelectedItem).Index;
        CVoiceSoundList Temp = (CVoiceSoundList)Sound_File_L.SelectedItem;
        int removeIndex = voiceSounds.IndexOf(Temp);
        if (removeIndex == -1)
        {
            Sub_Code.Show_Message("不明なエラーが発生しました。");
            return;
        }
        voiceTypes[nowTypePage][typeIndex].TypeSetting.Sounds.RemoveAt(removeIndex);
        Set_Item_Type();
    }

    //クリアボタン
    private async void Clear_B_Click(object? sender, EventArgs e)
    {
        bool Result = await DisplayAlert("確認", "内容をクリアしますか?", "はい", "いいえ");
        if (Result)
        {
            Init_Voice_Type();
            Set_Item_Type();
            wvsFile.Dispose();
            projectName = "";
            Message_Feed_Out("クリアしました。");
        }
    }

    //イベント設定画面を開く
    private void OpenEventSetting_B_Click(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;
        soundSettingWindow.Initialize(voiceSounds, wvsFile);
        Navigation.PushAsync(soundSettingWindow);
        bOtherPageOpened = true;
    }
}