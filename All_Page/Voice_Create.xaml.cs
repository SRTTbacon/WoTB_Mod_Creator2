using System.Reflection;
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

    public void InitTypeSetting(uint eventShortID, uint voiceShortID, SE_Type? seType, string defaultVoiceName = "")
    {
        TypeSetting.Init(eventShortID, voiceShortID, seType, defaultVoiceName);
    }
}

public partial class Voice_Create : ContentPage
{
    //SE設定
    public SE_Setting SESettingWindow;

    //イベント設定
    Voice_Create_Sound_Setting? soundSettingWindow;
    Build_Setting? buildSettingWindow;

    //サウンドイベント
    readonly List<CVoiceSoundList> voiceSounds = [];
    readonly List<List<CVoiceTypeList>> voiceTypes = [];

    readonly WVS_Load wvsFile = new();

    Build_Setting.Build_State state = Build_Setting.Build_State.None;

    string projectName = "";

    int nowTypePage = 0;

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
        EventSetting_B.Clicked += OpenEventSetting_B_Click;
        Clear_B.Clicked += Clear_B_Click;
        SE_Setting_B.Clicked += SE_Setting_B_Clicked;
        BuildSetting_B.Clicked += BuildSetting_B_Clicked;

        SESettingWindow = new();
        Init_Voice_Type();
        Set_Item_Type();
    }

    private void Init_Voice_Type()
    {
        voiceSounds.Clear();
        voiceTypes.Clear();
        for (int i = 0; i < 3; i++)
            voiceTypes.Add([]);


        SE_Preset sePreset = SESettingWindow.NowPreset;

        //1ページ目
        List<CVoiceTypeList> voiceTypeOne = voiceTypes[0];
        voiceTypeOne.Add(new("味方にダメージ", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(341425709, 170029050, sePreset.GetSEType("貫通"), "ally_killed_by_player");
        voiceTypeOne.Add(new("弾薬庫破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(908426860, 95559763, sePreset.GetSEType("弾薬庫破損"), "ammo_bay_damaged");
        voiceTypeOne.Add(new("敵への無効弾", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(280189980, 766083947, sePreset.GetSEType("非貫通-無効弾"), "armor_not_pierced_by_player");
        voiceTypeOne.Add(new("敵への貫通弾", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(815358870, 569784404, sePreset.GetSEType("貫通"), "armor_pierced_by_player");
        voiceTypeOne.Add(new("敵への致命弾", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(49295125, 266422868, sePreset.GetSEType("敵モジュール破損"), "armor_pierced_crit_by_player");
        voiceTypeOne.Add(new("敵への跳弾", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(733342682, 1052258113, sePreset.GetSEType("非貫通-跳弾"), "armor_ricochet_by_player");
        voiceTypeOne.Add(new("車長負傷", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(331196727, 242302464, sePreset.GetSEType("搭乗員負傷"), "commander_killed");
        voiceTypeOne.Add(new("操縦手負傷", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(619058694, 334837201, sePreset.GetSEType("搭乗員負傷"), "driver_killed");
        voiceTypeOne.Add(new("敵炎上", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(794420468, 381780774, sePreset.GetSEType("敵炎上"), "enemy_fire_started_by_player");
        voiceTypeOne.Add(new("敵撃破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(109598189, 489572734, sePreset.GetSEType("敵撃破"), "enemy_killed_by_player");
        voiceTypeOne.Add(new("エンジン破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(244621664, 210078142, sePreset.GetSEType("モジュール破損"), "engine_damaged");
        voiceTypeOne.Add(new("エンジン大破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(73205091, 249535989, sePreset.GetSEType("モジュール大破"), "engine_destroyed");
        voiceTypeOne.Add(new("エンジン復旧", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(466111031, 908710042, sePreset.GetSEType("モジュール復旧"), "engine_functional");
        voiceTypeOne.Add(new("自車両火災", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(471196930, 1057023960, null, "fire_started");
        voiceTypeOne.Add(new("自車両消火", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(337626756, 953778289, null, "fire_stopped");
        voiceTypeOne.Add(new("燃料タンク破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(930519512, 121897540, sePreset.GetSEType("燃料タンク破損"), "fuel_tank_damaged");
        voiceTypeOne.Add(new("主砲破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(1063632502, 127877647, sePreset.GetSEType("モジュール破損"), "gun_damaged");
        voiceTypeOne.Add(new("主砲大破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(175994480, 462397017, sePreset.GetSEType("モジュール大破"), "gun_destroyed");
        voiceTypeOne.Add(new("主砲復旧", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(546476029, 651656679, sePreset.GetSEType("モジュール復旧"), "gun_functional");
        voiceTypeOne.Add(new("砲手負傷", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(337748775, 739086111, sePreset.GetSEType("搭乗員負傷"), "gunner_killed");
        voiceTypeOne.Add(new("装填手負傷", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(302644322, 363753108, sePreset.GetSEType("搭乗員負傷"), "loader_killed");
        voiceTypeOne.Add(new("通信機破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(356562073, 91697210, sePreset.GetSEType("無線機破損"), "radio_damaged");
        voiceTypeOne.Add(new("通信手負傷", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(156782042, 987172940, sePreset.GetSEType("搭乗員負傷"), "radioman_killed");
        voiceTypeOne.Add(new("戦闘開始", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(769815093, 518589126, sePreset.GetSEType("戦闘開始"), "start_battle");
        voiceTypeOne.Add(new("観測装置破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(236686366, 330491031, sePreset.GetSEType("モジュール破損"), "surveying_devices_damaged");
        voiceTypeOne.Add(new("観測装置大破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(559710262, 792301846, sePreset.GetSEType("モジュール大破"), "surveying_devices_destroyed");
        voiceTypeOne.Add(new("観測装置復旧", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(47321344, 539730785, sePreset.GetSEType("モジュール復旧"), "surveying_devices_functional");
        voiceTypeOne.Add(new("履帯破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(978556760, 38261315, sePreset.GetSEType("モジュール破損"), "track_damaged");
        voiceTypeOne.Add(new("履帯大破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(878993268, 37535832, sePreset.GetSEType("モジュール大破"), "track_destroyed");
        voiceTypeOne.Add(new("履帯復旧", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(581830963, 558576963, sePreset.GetSEType("モジュール復旧"), "track_functional");
        voiceTypeOne.Add(new("砲塔破損", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(984973529, 1014565012, sePreset.GetSEType("モジュール破損"), "turret_rotator_damaged");
        voiceTypeOne.Add(new("砲塔大破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(381112709, 135817430, sePreset.GetSEType("モジュール大破"), "turret_rotator_destroyed");
        voiceTypeOne.Add(new("砲塔復旧", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(33436524, 985679417, sePreset.GetSEType("モジュール復旧"), "turret_rotator_functional");
        voiceTypeOne.Add(new("自車両大破", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(116097397, 164671745, sePreset.GetSEType("自車両大破"), "vehicle_destroyed");

        //2ページ目
        List<CVoiceTypeList> voiceTypeTwo = voiceTypes[1];
        voiceTypeTwo.Add(new("敵発見", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(308272618, 447063394, sePreset.GetSEType("敵発見"));
        voiceTypeTwo.Add(new("第六感", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(767278023, 154835998, sePreset.GetSEType("第六感"));
        voiceTypeTwo.Add(new("了解", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(230904672, 607694618, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("拒否", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(390478464, 391276124, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("救援を請う", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(17969037, 840378218, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("攻撃せよ！", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(900922817, 549968154, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("攻撃中", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(727518878, 1015337424, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("陣地を占領せよ！", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(101252368, 271044645, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("陣地を防衛せよ！ ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(576711003, 310153012, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("固守せよ！ ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(470859110, 379548034, sePreset.GetSEType("クイックコマンド"));
        voiceTypeTwo.Add(new("ロックオン ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(502585189, 839607605, sePreset.GetSEType("ロックオン"));
        voiceTypeTwo.Add(new("アンロック ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(769354725, 233444430, sePreset.GetSEType("アンロック"));
        voiceTypeTwo.Add(new("装填完了 ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(402727222, 299739777, sePreset.GetSEType("装填完了"));
        voiceTypeTwo.Add(new("マップクリック時 ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(670169971, 120795627, sePreset.GetSEType("マップクリック"));
        voiceTypeTwo.Add(new("戦闘終了時(時間切れや占領時のみ) ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(204685755, 924876614, sePreset.GetSEType("時間切れ&占領ポイントMax"));
        voiceTypeTwo.Add(new("戦闘BGM ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(1065169508, 891902653, null);
        voiceTypeTwo.Add(new("移動中 ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(198183306, 52813795, sePreset.GetSEType("移動中"));

        //3ページ目
        List<CVoiceTypeList> voiceTypeThree = voiceTypes[2];
        voiceTypeThree.Add(new("チャット:味方-送信", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(440862849, 491691546, null);
        voiceTypeThree.Add(new("チャット:味方-受信", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(750405, 417768496, null);
        voiceTypeThree.Add(new("チャット:全体-送信", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(228164469, 46472417, null);
        voiceTypeThree.Add(new("チャット:全体-受信", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(253510370, 681331945, null);
        voiceTypeThree.Add(new("チャット:小隊-送信", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(639029638, 190711689, null);
        voiceTypeThree.Add(new("チャット:小隊-受信", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(523678232, 918836720, null);
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

    async void StateLoop(string projectName)
    {
        while (state == Build_Setting.Build_State.Building)
            await Task.Delay(100);

        Voice_Load_From_File(Sub_Code.ExDir + "/Saves/" + projectName + ".wvs");

        Message_Feed_Out("セーブしました。");
    }

    //リストの状態を更新
    private void Set_Item_Type(int nextPage = -1)
    {
        if (nextPage == -1)
            nextPage = nowTypePage;
        bool bChangePage = nowTypePage != nextPage;
        nowTypePage = nextPage;

        CVoiceTypeList? selectedType = Voice_Type_L.SelectedItem as CVoiceTypeList;
        Voice_Type_L.SelectedItem = null;
        Voice_Type_L.ItemsSource = null;
        Voice_Type_L.ItemsSource = voiceTypes[nowTypePage];
        Voice_Type_Page_T.Text = "イベントリスト" + (nowTypePage + 1);
        Sound_File_L.SelectedItem = null;
        Sound_File_L.ItemsSource = null;

        //ページが前回と変わっていないかつ、項目が選択された状態であればイベント内のサウンドファイルをリストに表示
        if (!bChangePage && selectedType != null)
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
        if (Voice_Type_L.SelectedItem != null)
        {
            CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
            voiceSounds.Clear();
            foreach (CVoiceSoundSetting soundSetting in typeList.TypeSetting.Sounds)
                voiceSounds.Add(new(soundSetting));
            Sound_File_L.ItemsSource = null;
            if (voiceSounds.Count > 0)
                Sound_File_L.ItemsSource = voiceSounds;
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

        //選択画面を表示
        if (savedNames.Count == 0)      //セーブファイルが1つも存在しない
            await DisplayActionSheet("プロジェクトを選択してください。", "キャンセル", null, ["!プロジェクトが存在しません!"]);
        else
        {
            string fileName = await DisplayActionSheet("プロジェクトを選択してください。", "キャンセル", null, [..savedNames]);
            int index = savedNames.IndexOf(fileName);
            if (index != -1)
            {
                Voice_Load_From_File(Sub_Code.ExDir + "/Saves/" + savedNames[index] + ".wvs");
                Set_Item_Type();
            }
        }
        savedNames.Clear();
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

            bMessageShowing = false;
            Message_T.Text = "セーブしています...";
            Message_T.Opacity = 1.0;

            state = Build_Setting.Build_State.Building;

            StateLoop(result);
            SaveProject(result);
        }
    }

    void SaveProject(string projectName)
    {
        Task.Run(() =>
        {
            WVS_Save save = new();
            save.Add_Sound(voiceTypes, wvsFile);
            save.Create(Sub_Code.ExDir + "/Saves/" + projectName + ".wvs", projectName, false);

            state = Build_Setting.Build_State.None;
        });
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
                Message_Feed_Out("エラー:ファイルが存在しません。");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Header)
            {
                Message_Feed_Out("エラー:ヘッダーが異なります。");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Version)
            {
                Message_Feed_Out("エラー:セーブファイルのバージョンが古すぎます。");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.WoTMode)
            {
                Message_Feed_Out("エラー:WoT用のセーブファイルのためロードできません。");
                return;
            }
            else
            {
                wvsResult = wvsFile.WVS_Load_File(filePath, voiceTypes);
                if (wvsResult == WVS_Load.WVS_Result.OK)
                {
                    projectName = wvsFile.ProjectName;
                    Message_Feed_Out(Path.GetFileNameWithoutExtension(filePath) + "をロードしました。");
                }
                else
                    throw new Exception(".wvsファイルが破損しています。");
            }
            Set_Item_Type();
        }
        catch (Exception e)
        {
            Message_Feed_Out("エラー:" + e.Message);
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
#if ANDROID
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("アクセス許可を行ってください。");
                return;
            }
#endif
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //対応している拡張子
            Sub_Code.Select_Files_Window.Window_Show("Voice_Create", "", extension);    //選択画面を初期化
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);              //選択画面を開く
        }
        else
        {
            FilePickerFileType customFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, Sub_Code.AudioExtension }
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
        CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
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
            Message_Feed_Out("イベント名が選択されていません。");
            return;
        }
        if (Sound_File_L.SelectedItem == null)
        {
            Message_Feed_Out("削除したいサウンドを選択してください。");
            return;
        }
        int typeIndex = ((CVoiceTypeList)Voice_Type_L.SelectedItem).Index;
        CVoiceSoundList Temp = (CVoiceSoundList)Sound_File_L.SelectedItem;
        int removeIndex = voiceSounds.IndexOf(Temp);
        if (removeIndex == -1)
        {
            Message_Feed_Out("不明なエラーが発生しました。");
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

        CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
        if (typeList == null)
            return;
        if (typeList.Count <= 0)
        {
            Message_Feed_Out("イベント内にサウンドが含まれていないため設定画面を開くことができません。");
            return;
        }

        soundSettingWindow ??= new();

        soundSettingWindow.Initialize(typeList.TypeSetting, voiceSounds, wvsFile);
        Navigation.PushAsync(soundSettingWindow);
        bOtherPageOpened = true;
    }

    private void SE_Setting_B_Clicked(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        Navigation.PushAsync(SESettingWindow);
        bOtherPageOpened = true;
    }

    private void BuildSetting_B_Clicked(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        buildSettingWindow ??= new(voiceTypes, wvsFile, SESettingWindow.NowPreset);

        Navigation.PushAsync(buildSettingWindow);
        bOtherPageOpened = true;
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

    private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        ListView listView = (ListView)sender;
        IEnumerable<PropertyInfo> pInfos = listView.GetType().GetRuntimeProperties();
        PropertyInfo? templatedItems = pInfos.FirstOrDefault(info => info.Name == "TemplatedItems");
        if (templatedItems != null)
        {
            object? cells = templatedItems.GetValue(listView);
            if (cells == null)
                return;
            int index = 0;
            foreach (ViewCell cell in ((ITemplatedItemsList<Cell>)cells).Cast<ViewCell>())
            {
                if (index == e.SelectedItemIndex)
                    cell.View.BackgroundColor = Color.FromArgb("#82bfc8");
                else
                    cell.View.BackgroundColor = Colors.Transparent;
                index++;
            }
        }
    }
}