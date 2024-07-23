using System.Reflection;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

//サウンド情報
public class OtherModSound(string filePath, bool bDefaultSound = false, bool bAndroidResource = false)
{
    public string FilePath = filePath;

    public string NameText => Path.GetFileName(FilePath);

    public long StreamPosition = 0;

    public bool IsBinarySound => StreamPosition != 0;
    public bool IsDefaultSound = bDefaultSound;
    public bool IsAndroidResource = bAndroidResource;
}
//タイプ情報
public class OtherModType(string modTypeName, uint containerID)
{
    public string ModTypeName = modTypeName;                    //タイプ名
    public uint ModTypeID = WwiseHash.HashString(modTypeName);  //タイプID
    public uint ContainerID = containerID;                      //サウンドの配置場所

    public string NameText => ModTypeName + " | " + SoundCount + "個";
    public Color NameColor => SoundCount == 0 ? Color.FromArgb("#BFFF2C8C") : Colors.Aqua;

    public readonly List<OtherModSound> Sounds = [];
    public int SoundCount => Sounds.Count;

    public bool AddSound(string filePath, bool bDefaultSound = false, bool bAndroidResource = false)
    {
        if (Sounds.Count >= 255)
            return false;
        foreach (OtherModSound sound in Sounds)
            if (sound.FilePath == filePath && sound.IsDefaultSound == bDefaultSound)
                return false;
        Sounds.Add(new(filePath, bDefaultSound, bAndroidResource));
        return true;
    }
}
//ページ情報
public class OtherModPage(string modPageName, string wwiseProjectName)
{
    public string ModPageName = modPageName;
    public string WwiseProjectName = wwiseProjectName;
    public uint modPageID = WwiseHash.HashString(modPageName);

    public List<OtherModType> Types = [];
    public List<string> BuildList = [];
}

public partial class Other_Create : ContentPage
{
    readonly List<OtherModPage> modPages = [];

    OtherModPage NowModPage => modPages[Mod_Selection_Picker.SelectedIndex];    //現在選択されているページ
    SYNCPROC? soundEndFunc = null;                                              //再生終了検知

    readonly WMS_Load wmsLoad = new();           //セーブファイルにサウンドデータが含まれている場合使用
    GCHandle soundPtr = new();
    Build_Setting.Build_State state = Build_Setting.Build_State.None;

    string projectName = "";
    string maxTime = "00:00";           //再生中のサウンドの長さ

    int streamHandle = 0;               //再生中のサウンドのハンドル

    bool bShowing = false;              //ウィンドウ表示中
    bool bOtherPageOpened = false;      //前面に他のウィンドウを表示中
    bool bMessageShowing = false;       //下部メッセージ表示中
    bool bEnded = false;                //再生終了検知
    bool bPaused = false;               //停止中か
    bool bLocationChanging = false;     //シークバーをタッチしたらtrue
    bool bPlayingMouseDown = false;     //シークバーをタッチしたらtrue

    public Other_Create()
	{
		InitializeComponent();

        //スライダー設定
        All_Volume_S.ValueChanged += All_Volume_S_ValueChanged;
        PlayTime_S.DragStarted += PlayTime_S_DragStarted; ;
        PlayTime_S.DragCompleted += PlayTime_S_DragCompleted;
        PlayTime_S.ValueChanged += PlayTime_S_ValueChanged;

        //ボタン設定
        Add_Sound_B.Clicked += Add_Sound_B_Clicked;
        Delete_Sound_B.Clicked += Delete_Sound_B_Clicked;
        Pause_B.Clicked += Pause_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Minus_B.Clicked += Minus_B_Clicked;
        Plus_B.Clicked += Plus_B_Clicked;
        Save_B.Clicked += Save_B_Clicked;
        Load_B.Clicked += Load_B_Clicked;
        BuildSetting_B.Clicked += BuildSetting_B_Clicked;

        //ページ変更
        Mod_Selection_Picker.SelectedIndexChanged += Mod_Selection_Picker_SelectedIndexChanged;

        InitializePages();

        foreach (OtherModPage type in modPages)
            Mod_Selection_Picker.Items.Add(type.ModPageName);
    }

    void InitializePages()
    {
        //ページ初期化
        //デフォルトのサウンド入れるか迷ったけど、上級者以外はイベント名だけだとどんな音か分からないと思ったから20MBくらい容量増えるけど入れた！えらい！
        modPages.Clear();
        modPages.Add(new("戦闘開始前ロードBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("ロード1:America_lakville", 205170598));
        modPages[^1].Types[^1].AddSound("Music_Map_America_Lakville.ogg", true, true);
        modPages[^1].Types.Add(new("ロード2:America_overlord", 148841988));
        modPages[^1].Types[^1].AddSound("Music_Map_America_Overlord.ogg", true, true);
        modPages[^1].Types.Add(new("ロード3:Chinese", 1067185674));
        modPages[^1].Types[^1].AddSound("Music_Map_Chinese.ogg", true, true);
        modPages[^1].Types.Add(new("ロード4:Desert_airfield", 99202684));
        modPages[^1].Types[^1].AddSound("Music_Map_Desert_Airfield.ogg", true, true);
        modPages[^1].Types.Add(new("ロード5:Desert_sand_river", 493356780));
        modPages[^1].Types[^1].AddSound("Music_Map_Desert_Sand_River.ogg", true, true);
        modPages[^1].Types.Add(new("ロード6:Europe_himmelsdorf", 277287194));
        modPages[^1].Types[^1].AddSound("Music_Map_Europe_Himmelsdorf.ogg", true, true);
        modPages[^1].Types.Add(new("ロード7:Europe_mannerheim", 321403539));
        modPages[^1].Types[^1].AddSound("Music_Map_Europe_Mannerheim.ogg", true, true);
        modPages[^1].Types.Add(new("ロード8:Europe_ruinberg", 603412881));
        modPages[^1].Types[^1].AddSound("Music_Map_Europe_Ruinberg.ogg", true, true);
        modPages[^1].Types.Add(new("ロード9:Japan", 256533957));
        modPages[^1].Types[^1].AddSound("Music_Map_Japan.ogg", true, true);
        modPages[^1].Types.Add(new("ロード10:Russian_malinovka", 520751345));
        modPages[^1].Types[^1].AddSound("Music_Map_Russian_Malinovka.ogg", true, true);
        modPages[^1].Types.Add(new("ロード11:Russian_prokhorovka", 307041675));
        modPages[^1].Types[^1].AddSound("Music_Map_Russian_Prokhorovka.ogg", true, true);
        //ビルドリスト (*.bnkの名前部分)
        modPages[^1].BuildList.Add("music_maps_america_lakville");
        modPages[^1].BuildList.Add("music_maps_america_overlord");
        modPages[^1].BuildList.Add("music_maps_chinese");
        modPages[^1].BuildList.Add("music_maps_desert_airfield");
        modPages[^1].BuildList.Add("music_maps_desert_sand_river");
        modPages[^1].BuildList.Add("music_maps_europe_himmelsdorf");
        modPages[^1].BuildList.Add("music_maps_europe_mannerheim");
        modPages[^1].BuildList.Add("music_maps_europe_ruinberg");
        modPages[^1].BuildList.Add("music_maps_japan");
        modPages[^1].BuildList.Add("music_maps_russian_malinovka");
        modPages[^1].BuildList.Add("music_maps_russian_prokhorovka");

        modPages.Add(new("リザルトBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("リザルト:勝利-BGM", 960016609));
        modPages[^1].Types[^1].AddSound("Music_Result_Victory.ogg", true, true);
        modPages[^1].Types.Add(new("リザルト:勝利-音声", 737229060));
        modPages[^1].Types.Add(new("リザルト:引き分け-BGM", 404033224));
        modPages[^1].Types[^1].AddSound("Music_Result_Draw.ogg", true, true);
        modPages[^1].Types.Add(new("リザルト:引き分け-音声", 480862388));
        modPages[^1].Types.Add(new("リザルト:敗北-BGM", 797792182));
        modPages[^1].Types[^1].AddSound("Music_Result_Defeat.ogg", true, true);
        modPages[^1].Types.Add(new("リザルト:敗北-音声", 761638380));
        //ビルドリスト
        modPages[^1].BuildList.Add("music_result_screen");
        modPages[^1].BuildList.Add("music_result_screen_basic");

        modPages.Add(new("戦闘中の優勢BGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("優勢-味方", 434309394));
        modPages[^1].Types[^1].AddSound("Music_Battle_Will_Win.ogg", true, true);
        modPages[^1].Types.Add(new("優勢-味方-音声", 1057587120));
        modPages[^1].Types.Add(new("優勢-敵", 868083406));
        modPages[^1].Types[^1].AddSound("Music_Battle_Will_Defeat.ogg", true, true);
        modPages[^1].Types.Add(new("優勢-敵-音声", 611060765));
        modPages[^1].Types.Add(new("優勢-サイレン", 971715704));
        modPages[^1].Types[^1].AddSound("danger.ogg", true, true);
        //ビルドリスト
        modPages[^1].BuildList.Add("music_battle");

        modPages.Add(new("ガレージSE", "WoTB_UI_Button_Sound"));
        modPages[^1].Types.Add(new("売却-SE", 432722439));
        modPages[^1].Types[^1].AddSound("Sell_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("売却-音声", 26462958));
        modPages[^1].Types.Add(new("チェックボックス-SE", 278676259));
        modPages[^1].Types[^1].AddSound("CheckBox_SE_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("CheckBox_SE_02.ogg", true, true);
        modPages[^1].Types.Add(new("チェックボックス-音声", 1015843643));
        modPages[^1].Types.Add(new("小隊受信-SE", 123366428));
        modPages[^1].Types[^1].AddSound("Squad_Request_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("小隊受信-音声", 1034987615));
        modPages[^1].Types.Add(new("モジュールの切り替え-SE", 1001742020));
        modPages[^1].Types[^1].AddSound("Module_Install_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("モジュールの切り替え-音声", 537387720));
        modPages[^1].Types.Add(new("戦闘開始ボタン-SE", 251988040));
        modPages[^1].Types[^1].AddSound("Start_Battle_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("戦闘開始ボタン-音声", 56850118));
        modPages[^1].Types.Add(new("ニュース-SE", 530790297));
        modPages[^1].Types[^1].AddSound("News_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("ニュース-音声", 1036212148));
        modPages[^1].Types.Add(new("車両納車-SE", 660827574));
        modPages[^1].Types[^1].AddSound("Tank_Buy_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("車両納車-音声", 192152217));
        modPages[^1].Types.Add(new("何か購入-SE", 409835290));
        modPages[^1].Types[^1].AddSound("Buy_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("何か購入-音声", 282116325));
        //ビルドリスト
        modPages[^1].BuildList.Add("ui_buttons_tasks");

        modPages.Add(new("砲撃音", "WoTB_Gun_Sound"));
        modPages[^1].Types.Add(new("12～23mm:自車両-通常", 634610718));
        modPages[^1].Types[^1].AddSound("Gun_Outside_auto_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_auto_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_auto_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("12～23mm:自車両-ズーム時", 142135010));
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_05.ogg", true, true);
        modPages[^1].Types.Add(new("12～23mm:他車両", 611442385));
        modPages[^1].Types[^1].AddSound("Gun_NPC_auto_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_auto_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_auto_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("12～23mm:自車両-音声", 245911224));
        modPages[^1].Types.Add(new("12～23mm:他車両-音声", 403248155));
        modPages[^1].Types.Add(new("20～45mm:自車両-通常", 220137673));
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_05.ogg", true, true);
        modPages[^1].Types.Add(new("20～45mm:自車両-ズーム時", 891043773));
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_04.ogg", true, true);
        modPages[^1].Types.Add(new("20～45mm:他車両", 983327549));
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_05.ogg", true, true);
        modPages[^1].Types.Add(new("20～45mm:自車両-音声", 391540681));
        modPages[^1].Types.Add(new("20～45mm:他車両-音声", 388036628));
        modPages[^1].Types.Add(new("50～75mm:自車両-通常", 342549628));
        modPages[^1].Types[^1].AddSound("Gun_Outside_mid_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_mid_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_mid_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("50～75mm:自車両-ズーム時", 76784519));
        modPages[^1].Types[^1].AddSound("Gun_Inside_mid_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_mid_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_mid_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("50～75mm:他車両", 670420603));
        modPages[^1].Types[^1].AddSound("Gun_NPC_mid_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_mid_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_mid_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("50～75mm:自車両-音声", 309167407));
        modPages[^1].Types.Add(new("50～75mm:他車両-音声", 161542186));
        modPages[^1].Types.Add(new("85～107mm:自車両-通常", 488206709));
        modPages[^1].Types[^1].AddSound("Gun_Outside_main_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_main_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_main_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("85～107mm:自車両-ズーム時", 91221195));
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_05.ogg", true, true);
        modPages[^1].Types.Add(new("85～107mm:他車両", 1023399622));
        modPages[^1].Types[^1].AddSound("Gun_NPC_main_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_main_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_main_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("85～107mm:自車両-音声", 965612953));
        modPages[^1].Types.Add(new("85～107mm:他車両-音声", 513231767));
        modPages[^1].Types.Add(new("115～152mm:自車両-通常", 547631281));
        modPages[^1].Types[^1].AddSound("Gun_Outside_large_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_large_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_large_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("115～152mm:自車両-ズーム時", 61886891));
        modPages[^1].Types[^1].AddSound("Gun_Inside_large_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_large_1p_02.ogg", true, true);
        modPages[^1].Types.Add(new("115～152mm:他車両", 619459354));
        modPages[^1].Types[^1].AddSound("Gun_NPC_large_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_large_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_large_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("115～152mm:自車両-音声", 452295751));
        modPages[^1].Types.Add(new("115～152mm:他車両-音声", 1032101855));
        modPages[^1].Types.Add(new("152mm以上:自車両-通常", 890327147));
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm以上:自車両-ズーム時", 697334890));
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm以上:他車両", 950138696));
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm以上:自車両-音声", 809907207));
        modPages[^1].Types.Add(new("152mm以上:他車両-音声", 985913513));
        modPages[^1].Types.Add(new("152mm以上_Extra:自車両-通常", 361462963));
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_04.ogg", true, true);
        modPages[^1].Types.Add(new("152mm以上_Extra:自車両-ズーム時", 5188110));
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_extra_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_extra_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_extra_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm以上_Extra:他車両", 349435285));
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_04.ogg", true, true);
        modPages[^1].Types.Add(new("152mm以上_Extra:自車両-音声", 483942445));
        modPages[^1].Types.Add(new("152mm以上_Extra:他車両-音声", 632064110));
        modPages[^1].Types.Add(new("ロケット:自車両-通常", 580241268));
        modPages[^1].Types[^1].AddSound("Gun_Outside_Rocket_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_Rocket_1p_02.ogg", true, true);
        modPages[^1].Types.Add(new("ロケット:自車両-ズーム時", 787886846));
        modPages[^1].Types[^1].AddSound("Gun_Inside_Rocket_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_Rocket_1p_02.ogg", true, true);
        modPages[^1].Types.Add(new("ロケット:他車両", 872967597));
        modPages[^1].Types[^1].AddSound("Gun_NPC_Rocket_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_Rocket_3p_02.ogg", true, true);
        modPages[^1].Types.Add(new("ロケット:自車両-音声", 385813932));
        modPages[^1].Types.Add(new("ロケット:他車両-音声", 410506553));
        //ビルドリスト
        modPages[^1].BuildList.Add("weapon");
        modPages[^1].BuildList.Add("weapon_basic");
    }

    //ループ (主にシークバー用)
    private async void Loop()
    {
        double nextFrame = Environment.TickCount;
        float period = 1000f / 20f;
        while (bShowing)
        {
            //一定時間経過するまで待機
            double tickCount = Environment.TickCount;
            if (tickCount < nextFrame)
            {
                if (nextFrame - tickCount > 1)
                    await Task.Delay((int)(nextFrame - tickCount));
                continue;
            }

            //再生中かどうか
            bool bPlaying = Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING && !bLocationChanging;
            if (bPlaying)
            {
                //現在の再生時間を表示
                long position = Bass.BASS_ChannelGetPosition(streamHandle);
                PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streamHandle, position);
                PlayTime_T.Text = Sub_Code.Get_Time_String(PlayTime_S.Value) + " / " + maxTime;
            }

            //再生が終わったらシークバーを0に戻す
            if (bEnded)
            {
                Bass.BASS_ChannelStop(streamHandle);
                PlayTime_S.Value = 0;
                PlayTime_T.Text = "00:00 / " + maxTime;
                bEnded = false;
                bPaused = true;
            }

            if (Environment.TickCount >= nextFrame + (double)period)
            {
                nextFrame += period;
                continue;
            }
            nextFrame += period;
        }
    }

    //サウンドの終了検知
    private async void EndSync(int handle, int channel, int data, IntPtr user)
    {
        if (!bEnded)
        {
            await Task.Delay(500);
            bEnded = true;
        }
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
            if (Number >= 200)
                Message_T.Opacity -= 0.025;
            await Task.Delay(1000 / 60);
        }
        bMessageShowing = false;
        Message_T.Text = "";
        Message_T.Opacity = 1;
    }

    //ページ変更
    private void Mod_Selection_Picker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (Mod_Selection_Picker.SelectedIndex == -1)
            return;
        _ = Pause_Volume_Animation(true, 5.0f);
        PlayTime_S.Value = 0;
        PlayTime_S.Maximum = 0;
        PlayTime_T.Text = "00:00 / 00:00";
        maxTime = "00:00";
        Set_Item_Type();
    }

    //タイプリストをタップ
    private void Other_Type_Tapped(object sender, EventArgs e)
    {
        _ = Pause_Volume_Animation(true, 10.0f);
        Other_Sound_L.ItemsSource = null;
        Other_Sound_L.ItemsSource = ((OtherModType)Other_Type_L.SelectedItem).Sounds;
    }

    //サウンドをタップ
    private async void Other_Voice_Tapped(object sender, EventArgs e)
    {
        OtherModSound sound = (OtherModSound)Other_Sound_L.SelectedItem;

        //再生中のサウンドを停止
        await Pause_Volume_Animation(true, 10);
        Bass.BASS_StreamFree(streamHandle);

        //ファイルが存在しない
        if (!sound.IsAndroidResource && !sound.IsBinarySound && !File.Exists(sound.FilePath))
        {
            Message_Feed_Out("ファイルが存在しません。削除されたか、移動されている可能性があります。");
            return;
        }

        //バッファーサイズを500に設定
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);

        //サウンドをエンジンに読み込む
        int baseHandle = 0;
        if (soundPtr.IsAllocated)
            soundPtr.Free();

        if (sound.IsAndroidResource)
        {
            byte[] soundBytes = Sub_Code.ReadResourceData(sound.FilePath);
            soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);
            IntPtr pin = soundPtr.AddrOfPinnedObject();
            baseHandle = Bass.BASS_StreamCreateFile(pin, 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_LOOP);
        }
        else if (sound.IsBinarySound)
        {
            //サウンドが.wvsファイルに内包されている場合はバイト配列に読み込んでエンジンにポインタを渡す
            byte[]? soundBytes = wmsLoad.Load_Sound(sound.StreamPosition);
            if (soundBytes != null)
            {
                //soundBytesが勝手に破棄されないように固定させる
                soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);

                //拡張子が.flacの場合通常の読み込みでは不安定になるため専用の関数を呼ぶ
                if (Path.GetExtension(sound.FilePath) == ".flac")
                    baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                else
                    baseHandle = Bass.BASS_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            }
        }
        else
        {
            if (Path.GetExtension(sound.FilePath) == ".flac")
                baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            else
                baseHandle = Bass.BASS_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        }

        //FXを適応できる形に
        streamHandle = BassFx.BASS_FX_TempoCreate(baseHandle, BASSFlag.BASS_FX_FREESOURCE);

        //エフェクトを適応
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);

        //サウンドの長さを取得し、シークバーに反映
        PlayTime_S.Maximum = Bass.BASS_ChannelBytes2Seconds(streamHandle, Bass.BASS_ChannelGetLength(streamHandle, BASSMode.BASS_POS_BYTES));

        //終了検知
        soundEndFunc = new SYNCPROC(EndSync);
        _ = Bass.BASS_ChannelSetSync(streamHandle, BASSSync.BASS_SYNC_END | BASSSync.BASS_SYNC_MIXTIME, 0, soundEndFunc, IntPtr.Zero);

        bPaused = true;

        //サウンドの長さを表示
        maxTime = Sub_Code.Get_Time_String(PlayTime_S.Maximum);
        PlayTime_T.Text = "00:00 / " + maxTime;
    }

    //全体の音量を変更
    private void All_Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);
        All_Volume_T.Text = "全体音量:" + (int)e.NewValue;
    }

    //再生位置を変更
    void Music_Pos_Change(double Pos, bool IsBassPosChange)
    {
        if (IsBassPosChange)
            Bass.BASS_ChannelSetPosition(streamHandle, Pos);
        PlayTime_T.Text = Sub_Code.Get_Time_String(Pos) + " / " + maxTime;
    }

    //シークバーを動かしたら
    private void PlayTime_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        //タッチしていない場合は反応しない
        if (bLocationChanging)
            Music_Pos_Change(PlayTime_S.Value, false);
    }

    //シークバーをタッチしたら
    private void PlayTime_S_DragStarted(object? sender, EventArgs e)
    {
        bLocationChanging = true;
        if (Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING)
        {
            bPlayingMouseDown = true;
            _ = Pause_Volume_Animation(false, 10);
        }
    }

    //シークバーを離したら
    private void PlayTime_S_DragCompleted(object? sender, EventArgs e)
    {
        bLocationChanging = false;
        Bass.BASS_ChannelSetPosition(streamHandle, PlayTime_S.Value);
        if (bPlayingMouseDown)
        {
            bPaused = false;
            Play_Volume_Animation(10);
            bPlayingMouseDown = false;
        }
    }

    //リストの状態を更新
    private void Set_Item_Type()
    {
        OtherModType? selectedType = Other_Type_L.SelectedItem as OtherModType;
        Other_Type_L.SelectedItem = null;
        Other_Type_L.ItemsSource = null;
        Other_Type_L.ItemsSource = NowModPage.Types;
        Other_Sound_L.SelectedItem = null;
        Other_Sound_L.ItemsSource = null;

        //項目が選択された状態であればイベント内のサウンドファイルをリストに表示
        if (selectedType != null && NowModPage.Types.Contains(selectedType))
        {
            Other_Type_L.SelectedItem = selectedType;
            Other_Sound_L.ItemsSource = selectedType.Sounds;
        }
    }

    //サウンドを追加
    public void Add_Sound(List<string> files)
    {
        OtherModType typeList = (OtherModType)Other_Type_L.SelectedItem;
        int alreadyVoiceCount = 0;
        int addedVoiceCount = 0;
        foreach (string filePath in files)
        {
            bool bExist = false;
            //データが全く同じファイルが存在するか調べる
            foreach (OtherModSound sound in typeList.Sounds)
            {
                if (sound.FilePath == filePath)
                {
                    bExist = true;
                    alreadyVoiceCount++;
                    break;
                }
            }
            if (bExist)
                continue;

            //イベントにサウンドを追加
            typeList.Sounds.Add(new(filePath));
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

    //サウンドを追加
    private async void Add_Sound_B_Clicked(object? sender, EventArgs e)
    {
        //エラー回避
        if (Other_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("イベント名が選択されていません。");
            return;
        }

        if (bOtherPageOpened)
            return;

#if ANDROID
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("アクセス許可を行ってください。");
                return;
            }
#endif

        //ファイル閲覧の権限を持っているかつ、ホーム画面でオリジナルの選択画面を有効にした場合はその選択画面でファイルを選択
        if (Sub_Code.IsUseSelectPage)
        {
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //対応している拡張子
            Sub_Code.Select_Files_Window.Window_Show("Other_Create", "", extension);    //選択画面を初期化
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

    private async void Delete_Sound_B_Clicked(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        if (Other_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("イベント名が選択されていません。");
            return;
        }
        if (Other_Sound_L.SelectedItem == null)
        {
            Message_Feed_Out("削除したいサウンドを選択してください。");
            return;
        }
        OtherModType type = (OtherModType)Other_Type_L.SelectedItem;
        OtherModSound sound = (OtherModSound)Other_Sound_L.SelectedItem;

        bool result = await DisplayAlert("確認", "'" + sound.NameText + "'をリストから削除しますか?", "はい", "いいえ");
        if (!result)
            return;

        if (!type.Sounds.Contains(sound))
        {
            Message_Feed_Out("不明なエラーが発生しました。");
            return;
        }

        _ = Pause_Volume_Animation(true, 15.0f);

        type.Sounds.Remove(sound);
        Set_Item_Type();
    }

    //再生
    private void Play_B_Clicked(object? sender, EventArgs e)
    {
        Play_Volume_Animation(5);
    }

    //一時停止
    private void Pause_B_Clicked(object? sender, EventArgs e)
    {
        _ = Pause_Volume_Animation(false, 5);
    }

    //再生時間+5秒
    private void Plus_B_Clicked(object? sender, EventArgs e)
    {
        if (PlayTime_S.Value + 5.0 >= PlayTime_S.Maximum)
            PlayTime_S.Value = PlayTime_S.Maximum;
        else
            PlayTime_S.Value += 5.0;
        Music_Pos_Change(PlayTime_S.Value, true);
    }

    //再生時間-5秒
    private void Minus_B_Clicked(object? sender, EventArgs e)
    {
        if (PlayTime_S.Value <= 5.0)
            PlayTime_S.Value = 0;
        else
            PlayTime_S.Value -= 5.0;
        Music_Pos_Change(PlayTime_S.Value, true);
    }

    private async void Save_B_Clicked(object? sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("セーブ", "プロジェクト名を指定してください。", "決定", "キャンセル", null, -1, null, projectName);
        if (result != null)
        {
            if (!Sub_Code.IsSafePath(result, true))
            {
                Message_Feed_Out("エラー:使用できない文字が含まれています。");
                return;
            }
            else if (File.Exists(Sub_Code.ExDir + "/Saves/" + result + ".wms"))
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

    private async void Load_B_Clicked(object? sender, EventArgs e)
    {
        //他のウィンドウが開かれていたらスルー
        if (bOtherPageOpened)
            return;

        //セーブさいている.wvsファイルを列挙
        List<string> savedNames = [];
        if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
            _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");
        foreach (string Files in Directory.GetFiles(Sub_Code.ExDir + "/Saves", "*.wms", SearchOption.TopDirectoryOnly))
            savedNames.Add(Path.GetFileNameWithoutExtension(Files));

        //選択画面を表示
        if (savedNames.Count == 0)      //セーブファイルが1つも存在しない
            await DisplayActionSheet("プロジェクトを選択してください。", "キャンセル", null, ["!プロジェクトが存在しません!"]);
        else
        {
            string fileName = await DisplayActionSheet("プロジェクトを選択してください。", "キャンセル", null, [.. savedNames]);
            int index = savedNames.IndexOf(fileName);
            if (index != -1)
            {
                Other_Load_From_File(Sub_Code.ExDir + "/Saves/" + savedNames[index] + ".wms");
                Set_Item_Type();
            }
        }
        savedNames.Clear();
    }

    private void BuildSetting_B_Clicked(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        Sub_Code.BuildSettingWindow.InitializeWMS(modPages, Mod_Selection_Picker.SelectedIndex, wmsLoad);
        Navigation.PushAsync(Sub_Code.BuildSettingWindow);
        bOtherPageOpened = true;
    }

    void SaveProject(string projectName)
    {
        Task.Run(() =>
        {
            WMS_Save save = new();
            save.Add_Data(modPages, wmsLoad, 0.0, false);
            save.Create(Sub_Code.ExDir + "/Saves/" + projectName + ".wms", projectName);

            state = Build_Setting.Build_State.None;
        });
    }

    async void StateLoop(string projectName)
    {
        while (state == Build_Setting.Build_State.Building)
            await Task.Delay(100);

        Other_Load_From_File(Sub_Code.ExDir + "/Saves/" + projectName + ".wms");

        Message_Feed_Out("セーブしました。");
    }

    void Other_Load_From_File(string filePath)
    {
        try
        {
            //音声を配置
            InitializePages();
            WMS_Load.WMS_Result wmsResult = wmsLoad.WMS_Load_File(filePath, modPages, out projectName);
            if (wmsResult == WMS_Load.WMS_Result.No_Exist_File)
            {
                Message_Feed_Out("エラー:ファイルが存在しません。");
                return;
            }
            else if (wmsResult == WMS_Load.WMS_Result.Wrong_Header)
            {
                Message_Feed_Out("エラー:ヘッダーが異なります。");
                return;
            }
            else if (wmsResult == WMS_Load.WMS_Result.Wrong_Version)
            {
                Message_Feed_Out("エラー:セーブファイルのバージョンが古すぎます。");
                return;
            }
            else if (wmsResult == WMS_Load.WMS_Result.Wrong_Data)
            {
                Message_Feed_Out("エラー:ファイルが破損しています。");
                InitializePages();
                return;
            }
            else
                Message_Feed_Out(Path.GetFileNameWithoutExtension(filePath) + "をロードしました。");
            Set_Item_Type();
        }
        catch (Exception e)
        {
            Message_Feed_Out("エラー:" + e.Message);
            InitializePages();
            Set_Item_Type();
            wmsLoad.Dispose();
        }
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        Mod_Selection_Picker.SelectedIndex = 0;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bShowing = true;
        bOtherPageOpened = false;
        Loop();
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        _ = Pause_Volume_Animation(false);

        bShowing = false;
    }

    //徐々に一時停止
    private async Task Pause_Volume_Animation(bool bStopMode, float fadeTime = 30f)
    {
        bPaused = true;
        float volumeNow = 1f;
        Bass.BASS_ChannelGetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref volumeNow);
        float Volume_Minus = volumeNow / fadeTime;
        //音量を少しずつ下げる
        while (volumeNow > 0f && bPaused)
        {
            volumeNow -= Volume_Minus;
            if (volumeNow < 0f)
                volumeNow = 0f;
            Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, volumeNow);
            await Task.Delay(1000 / 60);
        }
        //音量が0になったら再生を止める
        if (volumeNow <= 0f)
        {
            if (bStopMode)
            {
                Bass.BASS_ChannelStop(streamHandle);
                Bass.BASS_StreamFree(streamHandle);
                PlayTime_S.Value = 0;
                PlayTime_S.Maximum = 0;
                PlayTime_T.Text = "00:00 / 00:00";
                maxTime = "00:00";
                if (soundPtr.IsAllocated)
                    soundPtr.Free();
            }
            else if (bPaused)
                Bass.BASS_ChannelPause(streamHandle);
        }
    }

    //徐々に再生
    private async void Play_Volume_Animation(float fadeTime = 30f)
    {
        bPaused = false;
        //Change_Effect();
        Bass.BASS_ChannelPlay(streamHandle, false);
        float volumeNow = 1f;
        Bass.BASS_ChannelGetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref volumeNow);

        //1フレームで下げる音量を設定
        float volumePlus = (float)(All_Volume_S.Value / 100) / fadeTime;

        while (volumeNow < (float)(All_Volume_S.Value / 100) && !bPaused)
        {
            volumeNow += volumePlus;
            if (volumeNow > 1f)
                volumeNow = 1f;
            Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, volumeNow);
            await Task.Delay(1000 / 60);
        }
    }

    //選択状態のリストの色を変更
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

    private void ContentPage_SizeChanged(object sender, EventArgs e)
    {
        Sub_Code.SetListViewHeight(Other_Type_Border, Height);
        Sub_Code.SetListViewHeight(Other_Sound_Border, Height);
    }
}
