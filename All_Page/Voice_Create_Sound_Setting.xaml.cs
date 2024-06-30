using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Voice_Create_Sound_Setting : ContentPage
{
    //イベント設定画面
    readonly Voice_Create_Event_Setting eventSettingWindow = new();

    WVS_Load? wvsFile = null;
    SYNCPROC? musicEndFunc = null;
    GCHandle soundPtr = new();

    //LPF、HPF、Gainの設定クラス
    readonly BASS_BFX_BQF soundLPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_LOWPASS, 12000f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_BQF soundHPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_HIGHPASS, 0f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_VOLUME soundGainSetting = new(1.0f);

    List<CVoiceSoundList> sounds = [];

    ViewCell? lastVoiceSoundCell = null;

    string maxTime = "";

    float freq = 44100.0f;      //サウンドの周波数

    //サウンドのハンドル
    int streamHandle;       //メインハンドル(これにサウンドデータが入っている)
    int streamLPFHandle;    //LPFハンドル(LPFの設定用)
    int streamHPFHandle;    //HPFハンドル(HPFの設定用)
    int streamGainHandle;   //Gainハンドル(Gainの設定用)

    int beforeLPFValue;
    int beforeHPFValue;

    bool bPaused;               //一時停止中
    bool bLocationChanging;     //シークバーをタッチしたらtrue
    bool bPlayingMouseDown;     //シークバーをタッチしたらtrue
    bool bNoSelectMode;         //リストが変更されてもサウンドをロードしない
    bool bMessageShowing;       //メッセージ表示中
    bool bEnded;                //再生が終了したらtrue
    bool bShowing;              //この画面を表示中かどうか
    bool bOtherPageOpened;      //ほかの画面が開かれているか

    public Voice_Create_Sound_Setting()
	{
		InitializeComponent();

        //スライダーの設定
        All_Volume_S.ValueChanged += All_Volume_S_ValueChanged;
        Gain_Start_S.ValueChanged += Gain_Start_S_ValueChanged;
        Gain_End_S.ValueChanged += Gain_End_S_ValueChanged;
        Pitch_Start_S.ValueChanged += Pitch_Start_S_ValueChanged;
        Pitch_End_S.ValueChanged += Pitch_End_S_ValueChanged;
        LPF_Start_S.ValueChanged += LPF_Start_S_ValueChanged;
        LPF_End_S.ValueChanged += LPF_End_S_ValueChanged;
        HPF_Start_S.ValueChanged += HPF_Start_S_ValueChanged;
        HPF_End_S.ValueChanged += HPF_End_S_ValueChanged;
        Priority_S.ValueChanged += Priority_S_ValueChanged;
        Priority_S.DragCompleted += Priority_S_DragCompleted;
        Delay_S.ValueChanged += Delay_S_ValueChanged;

        //シークバーの設定
        PlayTime_S.DragStarted += PlayTime_S_DragStarted; ;
        PlayTime_S.DragCompleted += PlayTime_S_DragCompleted;
        PlayTime_S.ValueChanged += PlayTime_S_ValueChanged;

        //ボタンの設定
        Pause_B.Clicked += Pause_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Minus_B.Clicked += Minus_B_Clicked;
        Plus_B.Clicked += Plus_B_Clicked;
        Effect_Update_B.Clicked += Effect_Update_B_Clicked;
        EventSetting_B.Clicked += EventSetting_B_Clicked;
        //値リセットボタン
        Gain_Reset_B.Clicked += Gain_Reset_B_Clicked;
        Pitch_Reset_B.Clicked += Pitch_Reset_B_Clicked;
        LPF_Reset_B.Clicked += LPF_Reset_B_Clicked;
        HPF_Reset_B.Clicked += HPF_Reset_B_Clicked;

        //範囲指定チェックボックス
        Gain_Range_C.CheckedChanged += Gain_Range_C_CheckedChanged;
        Pitch_Range_C.CheckedChanged += Pitch_Range_C_CheckedChanged;
        LPF_Range_C.CheckedChanged += LPF_Range_C_CheckedChanged;
        HPF_Range_C.CheckedChanged += HPF_Range_C_CheckedChanged;
    }

    //初期化(画面を表示させる度に呼び出す必要あり)
    public void Initialize(CVoiceTypeSetting voiceEvent, List<CVoiceSoundList> voiceSoundList, WVS_Load wvsFile)
    {
        //サウンドリストにサウンド名を入れる
        Voice_Sound_L.SelectedItem = null;
        this.wvsFile = wvsFile;
        sounds = voiceSoundList;
        Voice_Sound_L.SelectedItem = null;
        Voice_Sound_L.ItemsSource = null;
        Voice_Sound_L.ItemsSource = voiceSoundList;
        bShowing = true;
        //イベント設定画面を初期化
        eventSettingWindow.Initialize(wvsFile, voiceEvent);
        //シークバー用のループ処理を開始
        Position_Change();
        Change_Range_Mode();
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

    //ループ (主にシークバー用)
    private async void Position_Change()
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
                //バッファーを更新 (サイズが大きくなればなるほど高負荷になるが、エフェクトの設定が即反映されるようになる)
                _ = Bass.BASS_ChannelUpdate(streamHandle, 400);

                if (Voice_Sound_L.SelectedItem != null)
                {
                    CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;

                    //再生時間を制限していた場合、その範囲を超えたらリセットさせる (Android版は再生時間の設定はできない仕様のため意味なし)
                    double endTime = voiceSoundSetting.PlayTime.End;
                    if (endTime != 0 && PlayTime_S.Value >= endTime)
                    {
                        Change_Effect();
                        Music_Pos_Change(voiceSoundSetting.PlayTime.Start, true);
                        long position2 = Bass.BASS_ChannelGetPosition(streamHandle);
                        PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streamHandle, position2);
                    }
                    else if (PlayTime_S.Value < voiceSoundSetting.PlayTime.Start)
                    {
                        Change_Effect();
                        Music_Pos_Change(voiceSoundSetting.PlayTime.Start, true);
                        long position2 = Bass.BASS_ChannelGetPosition(streamHandle);
                        PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streamHandle, position2);
                    }

                    //現在の再生時間を表示
                    long position = Bass.BASS_ChannelGetPosition(streamHandle);
                    PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streamHandle, position);
                    PlayTime_T.Text = Sub_Code.Get_Time_String(PlayTime_S.Value) + " / " + maxTime;
                }
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

    //遅延時間を変更
    private void Delay_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;
        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        double value = Math.Round(e.NewValue, 2, MidpointRounding.AwayFromZero);
        voiceSoundSetting.Delay = value;
        Delay_T.Text = "遅延:" + value + "秒";
    }

    //優先度のスライダーを離したらリストのテキストに反映
    private void Priority_S_DragCompleted(object? sender, EventArgs e)
    {
        if (Voice_Sound_L.SelectedItem != null)
        {
            bNoSelectMode = true;
            object voiceSoundList = Voice_Sound_L.SelectedItem;
            Voice_Sound_L.ItemsSource = null;
            Voice_Sound_L.ItemsSource = sounds;
            Voice_Sound_L.SelectedItem = voiceSoundList;
            bNoSelectMode = false;
        }
    }
    //優先度を変更
    private void Priority_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Priority_T.Text = "優先度:" + (int)Priority_S.Value;
        if (Voice_Sound_L.SelectedItem != null)
        {
            bNoSelectMode = true;
            CVoiceSoundList voiceSoundList = (CVoiceSoundList)Voice_Sound_L.SelectedItem;
            voiceSoundList.VoiceSoundSetting.Probability = Priority_S.Value;
            bNoSelectMode = false;
        }
    }

    //HPFの値(範囲)を変更
    private void HPF_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        HPF_End_T.Text = "～:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem == null)
            return;
        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.HPFRange.End = (int)e.NewValue;
    }

    //HPFの値を変更
    private void HPF_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        HPF_Start_T.Text = "HPF:" + (int)e.NewValue;
        int NewValue = (int)e.NewValue == 0 ? 1 : (int)e.NewValue;

        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        if (beforeHPFValue < NewValue)
            beforeHPFValue = NewValue;

        if (beforeHPFValue != NewValue)
            return;

        //範囲指定でなければ即反映
        if (!voiceSoundSetting.IsHPFRange)
        {
            soundHPFSetting.fCenter = Get_HPF_Value(NewValue);
            voiceSoundSetting.HighPassFilter = (int)e.NewValue;
            Bass.BASS_FXSetParameters(streamHPFHandle, soundHPFSetting);
        }
        else
            voiceSoundSetting.HPFRange.End = (int)e.NewValue;
        beforeHPFValue = 0;
    }

    //LPFの値(範囲)を変更
    private void LPF_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        LPF_End_T.Text = "～:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem == null)
            return;
        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.LPFRange.End = (int)e.NewValue;
    }

    //LPFの値を変更
    private void LPF_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        LPF_Start_T.Text = "LPF:" + (int)e.NewValue;
        int NewValue = (int)e.NewValue == 0 ? 1 : (int)e.NewValue;

        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        if (beforeLPFValue < NewValue)
            beforeLPFValue = NewValue;

        if (beforeLPFValue != NewValue)
            return;

        //範囲指定でなければ即反映
        if (!voiceSoundSetting.IsLPFRange)
        {
            soundLPFSetting.fCenter = Get_LPF_Value(NewValue);
            voiceSoundSetting.LowPassFilter = (int)e.NewValue;
            Bass.BASS_FXSetParameters(streamLPFHandle, soundLPFSetting);
        }
        else
            voiceSoundSetting.LPFRange.Start = (int)e.NewValue;
        beforeLPFValue = 0;
    }

    //ピッチ(再生速度)の値(範囲)を変更
    private void Pitch_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_End_T.Text = "～:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            voiceSoundSetting.PitchRange.End = (int)e.NewValue;
        }
    }

    //ピッチ(再生速度)の値を変更
    private void Pitch_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_Start_T.Text = "ピッチ:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            if (!voiceSoundSetting.IsPitchRange)
            {
                voiceSoundSetting.Pitch = (int)e.NewValue;
                SetPitch(voiceSoundSetting.Pitch);
            }
            else
                voiceSoundSetting.PitchRange.Start = (int)Pitch_Start_S.Value;
        }
    }

    //音量(ゲイン)の値(範囲)を変更
    private void Gain_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Gain_End_T.Text = "～:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            voiceSoundSetting.VolumeRange.End = e.NewValue;
        }
    }

    //音量(ゲイン)の値を変更
    private void Gain_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Gain_Start_T.Text = "ゲイン(db):" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            if (!voiceSoundSetting.IsVolumeRange)
            {
                voiceSoundSetting.Volume = e.NewValue;
                soundGainSetting.fVolume = (float)Math.Pow(10.0, e.NewValue / 20.0);
                Bass.BASS_FXSetParameters(streamGainHandle, soundGainSetting);
            }
            else
                voiceSoundSetting.VolumeRange.Start = e.NewValue;
        }
    }

    //全体の音量を変更
    private void All_Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);
        All_Volume_T.Text = "全体音量:" + (int)e.NewValue;
    }

    //サウンドファイルを選択
    private async void Sound_List_Tapped(object sender, EventArgs e)
    {
        //リストの選択個所の背景色を指定

        //1つ前に選択していた項目の背景は透明に
        if (lastVoiceSoundCell != null)
            lastVoiceSoundCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        //今選択した背景を灰色に
        if (viewCell.View != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastVoiceSoundCell = viewCell;
        }

        //未選択の場合停止
        if (Voice_Sound_L.SelectedItem == null || bNoSelectMode)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;

        //ファイルが存在するか確認
        if (voiceSoundSetting.FilePath.Contains('\\') || voiceSoundSetting.FilePath.Contains('/'))
        {
            if (!File.Exists(voiceSoundSetting.FilePath))
            {
                Message_Feed_Out("音声ファイルが存在しません。削除された可能性があります。");
                return;
            }
        }

        //再生中のサウンドを停止
        await Pause_Volume_Animation(true, 10);

        //かかっているエフェクトを削除
        Bass.BASS_ChannelRemoveFX(streamHandle, streamLPFHandle);
        Bass.BASS_ChannelRemoveFX(streamHandle, streamHPFHandle);
        Bass.BASS_ChannelRemoveFX(streamHandle, streamGainHandle);
        Bass.BASS_FXReset(streamLPFHandle);
        Bass.BASS_FXReset(streamHPFHandle);
        Bass.BASS_FXReset(streamGainHandle);
        Bass.BASS_StreamFree(streamHandle);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 200);

        //サウンドをエンジンに読み込む
        int baseHandle = 0;
        if (voiceSoundSetting.FilePath.Contains('\\') || voiceSoundSetting.FilePath.Contains('/'))
        {
            if (Path.GetExtension(voiceSoundSetting.FilePath) == ".flac")
                baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(voiceSoundSetting.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            else
                baseHandle = Bass.BASS_StreamCreateFile(voiceSoundSetting.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        }
        else if (wvsFile != null) 
        {
            //サウンドが.wvsファイルに内包されている場合はバイト配列に読み込んでエンジンにポインタを渡す
            byte[]? soundBytes = wvsFile.Load_Sound(voiceSoundSetting.StreamPosition);
            if (soundBytes != null)
            {
                if (soundPtr.IsAllocated)
                    soundPtr.Free();

                //soundBytesが勝手に破棄されないように固定させる
                soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);

                //拡張子が.flacの場合通常の読み込みでは不安定になるため専用の関数を呼ぶ
                if (Path.GetExtension(voiceSoundSetting.FilePath) == ".flac")
                    baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                else
                    baseHandle = Bass.BASS_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            }
        }

        //FXを適応できる形に
        streamHandle = BassFx.BASS_FX_TempoCreate(baseHandle, BASSFlag.BASS_FX_FREESOURCE);

        //バッファーサイズを500に設定
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);

        //サウンドの長さを取得し、シークバーに反映
        PlayTime_S.Maximum = Bass.BASS_ChannelBytes2Seconds(streamHandle, Bass.BASS_ChannelGetLength(streamHandle, BASSMode.BASS_POS_BYTES));
        voiceSoundSetting.PlayTime.Max = PlayTime_S.Maximum;
        PlayTime_T.Text = "00:00 / " + maxTime;

        //終了検知
        musicEndFunc = new SYNCPROC(EndSync);

        //エフェクトを適応
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);
        Bass.BASS_ChannelGetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, ref freq);
        _ = Bass.BASS_ChannelSetSync(streamHandle, BASSSync.BASS_SYNC_END | BASSSync.BASS_SYNC_MIXTIME, 0, musicEndFunc, IntPtr.Zero);
        streamLPFHandle = Bass.BASS_ChannelSetFX(streamHandle, BASSFXType.BASS_FX_BFX_BQF, 2);
        streamHPFHandle = Bass.BASS_ChannelSetFX(streamHandle, BASSFXType.BASS_FX_BFX_BQF, 1);
        streamGainHandle = Bass.BASS_ChannelSetFX(streamHandle, BASSFXType.BASS_FX_BFX_VOLUME, 3);
        Bass.BASS_FXSetParameters(streamLPFHandle, soundLPFSetting);
        Bass.BASS_FXSetParameters(streamHPFHandle, soundHPFSetting);
        Bass.BASS_FXSetParameters(streamGainHandle, soundGainSetting);
        Priority_S.Value = voiceSoundSetting.Probability;

        //サウンド設定からエフェクトの値を指定
        if (voiceSoundSetting.IsVolumeRange)
        {
            Gain_Start_S.Value = voiceSoundSetting.VolumeRange.Start;
            Gain_End_S.Value = voiceSoundSetting.VolumeRange.End;
        }
        else
            Gain_Start_S.Value = voiceSoundSetting.Volume;
        if (voiceSoundSetting.IsPitchRange)
        {
            Pitch_Start_S.Value = voiceSoundSetting.PitchRange.Start;
            Pitch_End_S.Value = voiceSoundSetting.PitchRange.End;
        }
        else
            Pitch_Start_S.Value = voiceSoundSetting.Pitch;
        if (voiceSoundSetting.IsLPFRange)
        {
            LPF_Start_S.Value = voiceSoundSetting.LPFRange.Start;
            LPF_End_S.Value = voiceSoundSetting.LPFRange.End;
        }
        else
            LPF_Start_S.Value = voiceSoundSetting.LowPassFilter;
        if (voiceSoundSetting.IsHPFRange)
        {
            HPF_Start_S.Value = voiceSoundSetting.HPFRange.Start;
            HPF_End_S.Value = voiceSoundSetting.HPFRange.End;
        }
        else
            HPF_Start_S.Value = voiceSoundSetting.HighPassFilter;
        Delay_S.Value = voiceSoundSetting.Delay;

        //エフェクトを適応
        SetPitch((int)Pitch_Start_S.Value);
        Change_Effect();
        bPaused = true;

        //サウンドの長さを表示
        maxTime = Sub_Code.Get_Time_String(PlayTime_S.Maximum);
        Change_Range_Mode();
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

    //範囲のシークバーの表示・非表示
    private void Change_Range_Mode()
    {
        Effect_Update_B.IsVisible = false;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            Gain_Range_C.IsChecked = voiceSoundSetting.IsVolumeRange;
            Gain_End_T.IsVisible = voiceSoundSetting.IsVolumeRange;
            Gain_End_S.IsVisible = voiceSoundSetting.IsVolumeRange;
            Pitch_Range_C.IsChecked = voiceSoundSetting.IsPitchRange;
            Pitch_End_T.IsVisible = voiceSoundSetting.IsPitchRange;
            Pitch_End_S.IsVisible = voiceSoundSetting.IsPitchRange;
            LPF_Range_C.IsChecked = voiceSoundSetting.IsLPFRange;
            LPF_End_T.IsVisible = voiceSoundSetting.IsLPFRange;
            LPF_End_S.IsVisible = voiceSoundSetting.IsLPFRange;
            HPF_Range_C.IsChecked = voiceSoundSetting.IsHPFRange;
            HPF_End_T.IsVisible = voiceSoundSetting.IsHPFRange;
            HPF_End_S.IsVisible = voiceSoundSetting.IsHPFRange;
            Gain_Start_S.Value = voiceSoundSetting.IsVolumeRange ? voiceSoundSetting.VolumeRange.Start : voiceSoundSetting.Volume;
            Pitch_Start_S.Value = voiceSoundSetting.IsPitchRange ? voiceSoundSetting.PitchRange.Start : voiceSoundSetting.Pitch;
            LPF_Start_S.Value = voiceSoundSetting.IsLPFRange ? voiceSoundSetting.LPFRange.Start : voiceSoundSetting.LowPassFilter;
            HPF_Start_S.Value = voiceSoundSetting.IsHPFRange ? voiceSoundSetting.HPFRange.Start : voiceSoundSetting.HighPassFilter;
            if (voiceSoundSetting.IsVolumeRange || voiceSoundSetting.IsPitchRange || voiceSoundSetting.IsLPFRange || voiceSoundSetting.IsHPFRange)
                Effect_Update_B.IsVisible = true;
            Change_Effect();
        }
        else
        {
            Gain_End_T.IsVisible = false;
            Gain_End_S.IsVisible = false;
            Pitch_End_T.IsVisible = false;
            Pitch_End_S.IsVisible = false;
            LPF_End_T.IsVisible = false;
            LPF_End_S.IsVisible = false;
            HPF_End_T.IsVisible = false;
            HPF_End_S.IsVisible = false;
            Effect_Update_B.IsVisible = false;
        }
    }

    //ピッチのスライダーから値を取得
    private void SetPitch(int pitch)
    {
        //pitchが0の場合はデフォルトの再生速度にする
        if (pitch == 0)
        {
            Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, freq);
            return;
        }
        int key = 0;
        int index = 0;

        //スライダーの値からWwiseのピッチの仕様に一番近い値を取得
        List<int> keyInt = [..Sub_Code.PitchValues.Keys];
        if (pitch > 0)
        {
            for (int i = 0; i < Sub_Code.PitchValues.Keys.Count; i++)
            {
                if (pitch >= keyInt[i])
                {
                    key = keyInt[i];
                    index = i;
                    break;
                }
            }
        }
        else if (pitch < 0)
        {
            for (int i = Sub_Code.PitchValues.Keys.Count - 1; i >= 0; i--)
            {
                if (pitch <= keyInt[i])
                {
                    key = keyInt[i];
                    index = i;
                    break;
                }
            }
        }
        double plusFreq = 0;
        if (pitch > 0 && pitch < 1200)
            plusFreq = 10.0 * (pitch - key) / (keyInt[index - 1] - key);
        else if (pitch < 0 && pitch > -1200)
            plusFreq = -10.0 * (pitch - key) / (keyInt[index + 1] - key);
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, freq * (float)(1 + (Sub_Code.PitchValues[key] + plusFreq) / 100.0));
    }

    //LPFのスライダーから値を取得
    private static int Get_LPF_Value(int value)
    {
        int newValue = value == 0 ? 1 : value;
        int index = (int)Math.Floor(newValue / 10.0);
        int key = Sub_Code.LPFValues.Keys.ElementAt(index);
        return key - (int)(Sub_Code.LPFValues[key] * Sub_Code.Get_Decimal(newValue / 10.0));
    }

    //HPFのスライダーから値を取得
    private static int Get_HPF_Value(int value)
    {
        int newValue = value == 0 ? 1 : value;
        int index = (int)Math.Floor(newValue / 5.0);
        int key = Sub_Code.HPFValues.Keys.ElementAt(index);
        return key + (int)(Sub_Code.HPFValues[key] * Sub_Code.Get_Decimal(newValue / 5.0));
    }

    //ゲインの値をリセット (Androidのスライダーでは0.0に合わせるのが難しいため)
    private void Gain_Reset_B_Clicked(object? sender, EventArgs e)
    {
        Gain_Start_S.Value = 0.0;
        Gain_End_S.Value = 0.0;
    }
    //速度の値をリセット
    private void Pitch_Reset_B_Clicked(object? sender, EventArgs e)
    {
        Pitch_Start_S.Value = 0.0;
        Pitch_End_S.Value = 0.0;
    }
    //LPFの値をリセット
    private void LPF_Reset_B_Clicked(object? sender, EventArgs e)
    {
        LPF_Start_S.Value = 0.0;
        LPF_End_S.Value = 0.0;
    }
    //HPFの値をリセット
    private void HPF_Reset_B_Clicked(object? sender, EventArgs e)
    {
        HPF_Start_S.Value = 0.0;
        HPF_End_S.Value = 0.0;
    }

    //ゲインの範囲指定の有無
    private void Gain_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsVolumeRange = e.Value;

        Change_Range_Mode();
    }
    //速度の範囲指定の有無
    private void Pitch_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsPitchRange = e.Value;

        Change_Range_Mode();
    }
    //LPFの範囲指定の有無
    private void LPF_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsLPFRange = e.Value;

        Change_Range_Mode();
    }
    //HPFの範囲指定の有無
    private void HPF_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsHPFRange = e.Value;

        Change_Range_Mode();
    }

    //エフェクトを更新
    //mode = -1 : すべてのエフェクトを更新
    //mode = 0  : 音量(ゲイン)のみ更新
    //mode = 1  : ピッチのみ更新
    //mode = 2  : Low Pass Filterのみ更新
    //mode = 3  : High Pass Filterのみ更新
    void Change_Effect(int mode = -1)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;

        //音量(ゲイン)の更新
        if (mode == -1 || mode == 0)
        {
            double volume;
            //音量の範囲が有効の場合
            if (voiceSoundSetting.IsVolumeRange)
            {
                if (voiceSoundSetting.VolumeRange.End >= voiceSoundSetting.VolumeRange.Start)
                    volume = Sub_Code.Get_Random_Double(voiceSoundSetting.VolumeRange.Start, voiceSoundSetting.VolumeRange.End);
                else
                    volume = Sub_Code.Get_Random_Double(voiceSoundSetting.VolumeRange.End, voiceSoundSetting.VolumeRange.Start);
            }
            else
                volume = voiceSoundSetting.Volume;
            soundGainSetting.fVolume = (float)Math.Pow(10d, volume / 20.0);
            Bass.BASS_FXSetParameters(streamGainHandle, soundGainSetting);
        }
        //ピッチを更新
        if (mode == -1 || mode == 1)
        {
            int pitch;
            //ピッチの範囲が有効の場合
            if (voiceSoundSetting.IsPitchRange)
            {
                if (voiceSoundSetting.PitchRange.End >= voiceSoundSetting.PitchRange.Start)
                    pitch = Sub_Code.RandomValue.Next(voiceSoundSetting.PitchRange.Start, voiceSoundSetting.PitchRange.End + 1);
                else
                    pitch = Sub_Code.RandomValue.Next(voiceSoundSetting.PitchRange.End, voiceSoundSetting.PitchRange.Start);
            }
            else
                pitch = voiceSoundSetting.Pitch;
            SetPitch(pitch);
        }
        //Low Pass Filterを更新
        if (mode == -1 || mode == 2)
        {
            int lpf;
            //LPFの範囲が有効の場合
            if (voiceSoundSetting.IsLPFRange)
            {
                if (voiceSoundSetting.LPFRange.End >= voiceSoundSetting.LPFRange.Start)
                    lpf = Sub_Code.RandomValue.Next(voiceSoundSetting.LPFRange.Start, voiceSoundSetting.LPFRange.End + 1);
                else
                    lpf = Sub_Code.RandomValue.Next(voiceSoundSetting.LPFRange.End, voiceSoundSetting.LPFRange.Start);
            }
            else
                lpf = voiceSoundSetting.LowPassFilter;
            soundLPFSetting.fCenter = Get_LPF_Value(lpf);
            Bass.BASS_FXSetParameters(streamLPFHandle, soundLPFSetting);
        }
        //High Pass Filterを更新
        if (mode == -1 || mode == 3)
        {
            int hpf;
            //HPF範囲が有効の場合
            if (voiceSoundSetting.IsHPFRange)
            {
                if (voiceSoundSetting.HPFRange.End >= voiceSoundSetting.HPFRange.Start)
                    hpf = Sub_Code.RandomValue.Next(voiceSoundSetting.HPFRange.Start, voiceSoundSetting.HPFRange.End + 1);
                else
                    hpf = Sub_Code.RandomValue.Next(voiceSoundSetting.HPFRange.End, voiceSoundSetting.HPFRange.Start);
            }
            else
                hpf = voiceSoundSetting.HighPassFilter;
            soundHPFSetting.fCenter = Get_HPF_Value(hpf);
            Bass.BASS_FXSetParameters(streamHPFHandle, soundHPFSetting);
        }
    }

    //エフェクト更新ボタン
    private void Effect_Update_B_Clicked(object? sender, EventArgs e)
    {
        Change_Effect();
    }

    //画面を閉じたら
    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        //ループプログラムを停止
        bShowing = false;
        _ = Pause_Volume_Animation(true, 5.0f);
    }

    //イベント設定画面へ
    private void EventSetting_B_Clicked(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;
        Navigation.PushAsync(eventSettingWindow);
        bOtherPageOpened = true;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bOtherPageOpened = false;
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