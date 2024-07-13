using System.Reflection;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public class OtherModSound(string filePath)
{
    public string FilePath = filePath;

    public string NameText => Path.GetFileName(FilePath);

    public long StreamPosition = 0;

    public bool IsBinarySound => StreamPosition != 0;
}
public class OtherModType(string modTypeName, uint containerID)
{
    public string ModTypeName = modTypeName;                    //タイプ名
    public uint ModTypeID = WwiseHash.HashString(modTypeName);  //タイプID
    public uint ContainerID = containerID;                      //サウンドの配置場所

    public string NameText => ModTypeName + " | " + SoundCount + "個";
    public Color NameColor => SoundCount == 0 ? Color.FromArgb("#BFFF2C8C") : Colors.Aqua;

    public readonly List<OtherModSound> Sounds = [];
    public int SoundCount => Sounds.Count;

    public bool AddSound(string filePath)
    {
        foreach (OtherModSound sound in Sounds)
            if (sound.FilePath == filePath)
                return false;
        Sounds.Add(new(filePath));
        return true;
    }
}
public class OtherModPage(string modPageName, string wwiseProjectName)
{
    public string ModPageName = modPageName;
    public string WwiseProjectName = wwiseProjectName;
    public uint modPageID = WwiseHash.HashString(modPageName);

    public List<OtherModType> Types = [];

    public OtherModType? GetModType(string modTypeName)
    {
        foreach (OtherModType type in Types)
            if (type.ModTypeName == modTypeName)
                return type;
        return null;
    }
    public OtherModType? GetModType(uint modTypeID)
    {
        foreach (OtherModType type in Types)
            if (type.ModTypeID == modTypeID)
                return type;
        return null;
    }
}

public partial class Other_Create : ContentPage
{
    readonly List<OtherModPage> modPages = [];

    OtherModPage NowModPage => modPages[Mod_Selection_Picker.SelectedIndex];
    SYNCPROC? soundEndFunc = null;

    WMS_Load wmsLoad = new();

    string maxTime = "00:00";

    int streamHandle = 0;

    bool bShowing = false;
    bool bMessageShowing = false;
    bool bEnded = false;
    bool bPaused = false;
    bool bLocationChanging = false;
    bool bPlayingMouseDown = false;

    public Other_Create()
	{
		InitializeComponent();

        //スライダー設定
        All_Volume_S.ValueChanged += All_Volume_S_ValueChanged;
        PlayTime_S.DragStarted += PlayTime_S_DragStarted; ;
        PlayTime_S.DragCompleted += PlayTime_S_DragCompleted;
        PlayTime_S.ValueChanged += PlayTime_S_ValueChanged;

        //ボタン設定
        Pause_B.Clicked += Pause_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Minus_B.Clicked += Minus_B_Clicked;
        Plus_B.Clicked += Plus_B_Clicked;

        Mod_Selection_Picker.SelectedIndexChanged += Mod_Selection_Picker_SelectedIndexChanged;

        modPages.Add(new("戦闘開始前ロードBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("ロード1:America_lakville", 205170598));
        modPages[^1].Types.Add(new("ロード2:America_overlord", 148841988));
        modPages[^1].Types.Add(new("ロード3:Chinese", 1067185674));
        modPages[^1].Types.Add(new("ロード4:Desert_airfield", 99202684));
        modPages[^1].Types.Add(new("ロード5:Desert_sand_river", 493356780));
        modPages[^1].Types.Add(new("ロード6:Europe_himmelsdorf", 277287194));
        modPages[^1].Types.Add(new("ロード7:Europe_mannerheim", 321403539));
        modPages[^1].Types.Add(new("ロード8:Europe_ruinberg", 603412881));
        modPages[^1].Types.Add(new("ロード9:Japan", 256533957));
        modPages[^1].Types.Add(new("ロード10:Russian_malinovka", 520751345));
        modPages[^1].Types.Add(new("ロード11:Russian_prokhorovka", 307041675));
        modPages.Add(new("リザルトBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("リザルト:勝利-BGM", 960016609));
        modPages[^1].Types.Add(new("リザルト:勝利-音声", 737229060));
        modPages[^1].Types.Add(new("リザルト:引き分け-BGM", 404033224));
        modPages[^1].Types.Add(new("リザルト:引き分け-音声", 480862388));
        modPages[^1].Types.Add(new("リザルト:引き分け-BGM", 797792182));
        modPages[^1].Types.Add(new("リザルト:引き分け-音声", 761638380));
        modPages.Add(new("戦闘中の優勢BGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("優勢-味方", 434309394));
        modPages[^1].Types.Add(new("優勢-敵", 868083406));
        modPages.Add(new("ガレージSE", "WoTB_UI_Button_Sound"));
        modPages[^1].Types.Add(new("売却-SE", 432722439));
        modPages[^1].Types.Add(new("売却-音声", 26462958));
        modPages[^1].Types.Add(new("チェックボックス-SE", 278676259));
        modPages[^1].Types.Add(new("チェックボックス-音声", 1015843643));
        modPages[^1].Types.Add(new("小隊受信-SE", 123366428));
        modPages[^1].Types.Add(new("小隊受信-音声", 1034987615));
        modPages[^1].Types.Add(new("モジュールの切り替え-SE", 1001742020));
        modPages[^1].Types.Add(new("モジュールの切り替え-音声", 537387720));
        modPages[^1].Types.Add(new("戦闘開始ボタン-SE", 251988040));
        modPages[^1].Types.Add(new("戦闘開始ボタン-音声", 56850118));
        modPages[^1].Types.Add(new("ニュース-SE", 530790297));
        modPages[^1].Types.Add(new("ニュース-音声", 1036212148));
        modPages[^1].Types.Add(new("車両納車-SE", 660827574));
        modPages[^1].Types.Add(new("車両納車-音声", 192152217));
        modPages[^1].Types.Add(new("何か購入-SE", 409835290));
        modPages[^1].Types.Add(new("何か購入-音声", 282116325));
        modPages.Add(new("砲撃音", "WoTB_Gun_Sound_New"));

        foreach (OtherModPage type in modPages)
            Mod_Selection_Picker.Items.Add(type.ModPageName);
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

            //再生が終わったらシークバーを0に戻す
            if (bEnded)
            {
                Bass.BASS_ChannelStop(streamHandle);
                PlayTime_S.Value = 0;
                PlayTime_T.Text = "00:00 / " + maxTime;
                bEnded = false;
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
            if (Number >= 120)
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
        Other_Sound_L.ItemsSource = null;
        Other_Type_L.ItemsSource = null;
        if (Mod_Selection_Picker.SelectedIndex == -1)
            return;
        Other_Type_L.ItemsSource = NowModPage.Types;
    }

    private void Other_Type_Tapped(object sender, EventArgs e)
    {
        Other_Sound_L.ItemsSource = null;
        Other_Sound_L.ItemsSource = ((OtherModType)Other_Type_L.SelectedItem).Sounds;
    }

    private async void Other_Voice_Tapped(object sender, EventArgs e)
    {
        OtherModSound sound = (OtherModSound)Other_Sound_L.SelectedItem;

        //再生中のサウンドを停止
        await Pause_Volume_Animation(true, 10);
        Bass.BASS_StreamFree(streamHandle);

        if (!File.Exists(sound.FilePath))
        {
            Message_Feed_Out("ファイルが存在しません。削除されたか、移動されている可能性があります。");
            return;
        }

        //バッファーサイズを500に設定
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);

        //サウンドをエンジンに読み込む
        int baseHandle = 0;

        if (Path.GetExtension(sound.FilePath) == ".flac")
            baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        else
            baseHandle = Bass.BASS_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);

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

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        Mod_Selection_Picker.SelectedIndex = 0;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bShowing = true;
        Loop();
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
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
}
