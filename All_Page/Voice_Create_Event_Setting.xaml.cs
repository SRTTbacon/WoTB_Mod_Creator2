using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Voice_Create_Event_Setting : ContentPage
{
	CVoiceTypeSetting? eventSetting = null;

    //LPF、HPF、Gainの設定クラス
    readonly BASS_BFX_BQF soundEventLPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_LOWPASS, 12000f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_BQF soundVoiceLPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_LOWPASS, 12000f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_BQF soundEventHPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_HIGHPASS, 0f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_BQF soundVoiceHPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_HIGHPASS, 0f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_VOLUME soundEventGainSetting = new(1.0f);
    readonly BASS_BFX_VOLUME soundVoiceGainSetting = new(1.0f);
    readonly BASS_BFX_VOLUME soundSEGainSetting = new(1.0f);

    WVS_Load? wvsFile = null;
    SYNCPROC? musicEndFunc = null;
    GCHandle soundPtr = new();
    GCHandle seIntPtr = new();

    string maxTime = "";

    byte[]? soundBytes;

    readonly float eventFreq = 44100.0f;      //サウンドの周波数
    float voiceFreq = 44100.0f;      //サウンドの周波数

    //サウンドのハンドル
    readonly List<int> streams = [];
    int streamHandle;            //メインハンドル(これにサウンドデータが入っている)
    int streamEventLPFHandle;    //LPFハンドル(LPFの設定用)
    int streamVoiceLPFHandle;    //LPFハンドル(LPFの設定用)
    int streamEventHPFHandle;    //HPFハンドル(HPFの設定用)
    int streamVoiceHPFHandle;    //HPFハンドル(HPFの設定用)
    int streamEventGainHandle;   //Gainハンドル(Gainの設定用)
    int streamVoiceGainHandle;   //Gainハンドル(Gainの設定用)
    int streamSEGainHandle;      //Gainハンドル(Gainの設定用)

    int maxTimeStream;
    int selectedVoiceIndex = -1;

    int beforeLPFValue;
    int beforeHPFValue;

    bool bEnded;                //再生が終了したらtrue
    bool bMessageShowing = false;
    bool bLocationChanging = false;
    bool bPlayingMouseDown = false;
    bool bPaused = false;
    bool bShowing = false;

    public Voice_Create_Event_Setting()
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

        Change_Range_Mode();
    }

	public void Initialize(WVS_Load wvsFile, CVoiceTypeSetting eventSetting)
	{
        this.wvsFile = wvsFile;
		this.eventSetting = eventSetting;
        bShowing = true;
        Position_Change();
    }

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

                if (eventSetting == null)
                    return;

                double startTime = eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start;
                double endTime = eventSetting.Sounds[selectedVoiceIndex].PlayTime.End;
                if (endTime != 0 && PlayTime_S.Value + eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start >= endTime)
                {
                    if (maxTimeStream == 1)
                    {
                        Bass.BASS_ChannelStop(streamHandle);
                        Bass.BASS_ChannelSetPosition(streamHandle, 0);
                        Bass.BASS_ChannelSetPosition(streams[0], 0);
                        Bass.BASS_ChannelSetPosition(streams[1], eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start);
                        Bass.BASS_ChannelSetAttribute(streams[1], BASSAttribute.BASS_ATTRIB_VOL, 1f);
                    }
                    else
                        Bass.BASS_ChannelStop(streams[1]);
                }
                long position = Bass.BASS_ChannelGetPosition(streams[maxTimeStream]);
                if (maxTimeStream == 1)
                    PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streams[maxTimeStream], position) - startTime;
                else
                    PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streams[maxTimeStream], position);
                if (endTime != 0 && eventSetting.Sounds[selectedVoiceIndex].IsFadeOut && endTime - startTime > 0.5 && endTime - startTime - PlayTime_S.Value <= 0.6)
                    _ = Pause_Volume_Animation(false, 25f, streams[1]);
                PlayTime_T.Text = Sub_Code.Get_Time_String(PlayTime_S.Value) + " / " + maxTime;
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

    //画面右下部にメッセージを表示
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
        if (eventSetting == null)
            return;
        double value = Math.Round(e.NewValue, 2, MidpointRounding.AwayFromZero);
        eventSetting.Delay = value;
        Delay_T.Text = "遅延:" + value + "秒";
    }

    //HPFの値(範囲)を変更
    private void HPF_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        HPF_End_T.Text = "～:" + (int)e.NewValue;
        if (eventSetting == null)
            return;
        eventSetting.HPFRange.End = (int)e.NewValue;
    }

    //HPFの値を変更
    private void HPF_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        HPF_Start_T.Text = "HPF:" + (int)e.NewValue;
        int NewValue = (int)e.NewValue == 0 ? 1 : (int)e.NewValue;

        if (eventSetting == null)
            return;

        if (beforeHPFValue < NewValue)
            beforeHPFValue = NewValue;

        if (beforeHPFValue != NewValue)
            return;

        //範囲指定でなければ即反映
        if (!eventSetting.IsHPFRange)
        {
            soundEventHPFSetting.fCenter = Get_HPF_Value(NewValue);
            eventSetting.HighPassFilter = (int)e.NewValue;
            Bass.BASS_FXSetParameters(streamEventHPFHandle, soundEventHPFSetting);
        }
        else
            eventSetting.HPFRange.End = (int)e.NewValue;
        beforeHPFValue = 0;
    }

    //LPFの値(範囲)を変更
    private void LPF_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        LPF_End_T.Text = "～:" + (int)e.NewValue;
        if (eventSetting == null)
            return;
        eventSetting.LPFRange.End = (int)e.NewValue;
    }

    //LPFの値を変更
    private void LPF_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        LPF_Start_T.Text = "LPF:" + (int)e.NewValue;
        int NewValue = (int)e.NewValue == 0 ? 1 : (int)e.NewValue;

        if (eventSetting == null)
            return;

        if (beforeLPFValue < NewValue)
            beforeLPFValue = NewValue;

        if (beforeLPFValue != NewValue)
            return;

        //範囲指定でなければ即反映
        if (!eventSetting.IsLPFRange)
        {
            soundEventLPFSetting.fCenter = Get_LPF_Value(NewValue);
            eventSetting.LowPassFilter = (int)e.NewValue;
            Bass.BASS_FXSetParameters(streamEventLPFHandle, soundEventLPFSetting);
        }
        else
            eventSetting.LPFRange.Start = (int)e.NewValue;
        beforeLPFValue = 0;
    }

    //ピッチ(再生速度)の値(範囲)を変更
    private void Pitch_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_End_T.Text = "～:" + (int)e.NewValue;
        if (eventSetting != null)
        {
            eventSetting.PitchRange.End = (int)e.NewValue;
        }
    }

    //ピッチ(再生速度)の値を変更
    private void Pitch_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_Start_T.Text = "ピッチ:" + (int)e.NewValue;
        if (eventSetting != null)
        {
            if (!eventSetting.IsPitchRange)
            {
                eventSetting.Pitch = (int)e.NewValue;
                SetPitch(streamHandle, eventSetting.Pitch);
            }
            else
                eventSetting.PitchRange.Start = (int)Pitch_Start_S.Value;
        }
    }

    //音量(ゲイン)の値(範囲)を変更
    private void Gain_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Gain_End_T.Text = "～:" + (int)e.NewValue;
        if (eventSetting != null)
        {
            eventSetting.VolumeRange.End = e.NewValue;
        }
    }

    //音量(ゲイン)の値を変更
    private void Gain_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Gain_Start_T.Text = "ゲイン(db):" + (int)e.NewValue;
        if (eventSetting != null)
        {
            if (!eventSetting.IsVolumeRange)
            {
                eventSetting.Volume = e.NewValue;
                soundEventGainSetting.fVolume = (float)Math.Pow(10.0, e.NewValue / 20.0);
                Bass.BASS_FXSetParameters(streamEventGainHandle, soundEventGainSetting);
            }
            else
                eventSetting.VolumeRange.Start = e.NewValue;
        }
    }

    //全体の音量を変更
    private void All_Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);
        All_Volume_T.Text = "全体音量:" + (int)e.NewValue;
    }

    //ピッチのスライダーから値を取得
    private void SetPitch(int stream, int pitch)
    {
        //pitchが0の場合はデフォルトの再生速度にする
        if (pitch == 0)
        {
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, eventFreq);
            return;
        }
        int key = 0;
        int index = 0;

        //スライダーの値からWwiseのピッチの仕様に一番近い値を取得
        List<int> keyInt = [.. Sub_Code.PitchValues.Keys];
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
        Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, eventFreq * (float)(1 + (Sub_Code.PitchValues[key] + plusFreq) / 100.0f));
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

    //徐々に一時停止
    async Task Pause_Volume_Animation(bool IsStop, float Fade_Time = 30f, int handle = -1)
    {
        if (bPaused)
            return;
        int Before_Handle = handle;
        if (handle == -1)
        {
            handle = streamHandle;
            bPaused = true;
        }
        float Volume_Now = 1f;
        Bass.BASS_ChannelGetAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, ref Volume_Now);
        float Volume_Minus = Volume_Now / Fade_Time;
        while (Volume_Now > 0f && bPaused)
        {
            Volume_Now -= Volume_Minus;
            if (Volume_Now < 0f)
                Volume_Now = 0f;
            Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, Volume_Now);
            await Task.Delay(1000 / 60);
        }
        if (Volume_Now <= 0f)
        {
            if (IsStop)
            {
                Bass.BASS_ChannelStop(handle);
                Bass.BASS_StreamFree(handle);
                PlayTime_S.Value = 0;
                PlayTime_S.Maximum = 0;
                PlayTime_T.Text = "00:00 / 00:00";
                maxTime = "00:00";
                if (soundPtr.IsAllocated)
                    soundPtr.Free();
                soundBytes = null;
            }
            else if (bPaused || Before_Handle != -1)
                Bass.BASS_ChannelPause(handle);
        }
    }
    //徐々に再生
    async void Play_Volume_Animation(float Feed_Time = 30f, int Handle = -1, float Max_Volume = -1)
    {
        if (Handle == -1)
            Handle = streamHandle;
        if (Max_Volume == -1)
            Max_Volume = (float)(All_Volume_S.Value / 100);
        bPaused = false;
        Bass.BASS_ChannelPlay(streamHandle, false);
        float Volume_Now = 1f;
        Bass.BASS_ChannelGetAttribute(Handle, BASSAttribute.BASS_ATTRIB_VOL, ref Volume_Now);
        float Volume_Plus = Feed_Time <= 0.0f ? 1.0f : Max_Volume / Feed_Time;
        while (Volume_Now < Max_Volume && !bPaused)
        {
            Volume_Now += Volume_Plus;
            if (Volume_Now > 1f)
                Volume_Now = 1f;
            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_VOL, Volume_Now);
            await Task.Delay(1000 / 60);
        }
        if (!bPaused && selectedVoiceIndex != -1)
            Bass.BASS_ChannelSetAttribute(streams[1], BASSAttribute.BASS_ATTRIB_VOL, 1f);
    }

    //再生
    private void Play_B_Clicked(object? sender, EventArgs e)
    {
        if (eventSetting == null)
        {
            Message_Feed_Out("エラーが発生しました。");
            return;
        }

        Sound_Dispose();

        List<double> soundTimes = [];
        string playSEName = "なし";
        streams.Add(0);
        soundTimes.Add(0);
        if (eventSetting.SEType != null)
        {
            SE_Type tempType = eventSetting.SEType;
            if (tempType.SoundList.Count > 0)
            {
                SE_Sound? randomSound = tempType.GetRandomSESound();
                if (randomSound != null)
                {
                    int seHandle = 0;
                    if (randomSound.IsAndroidResource)
                    {
                        byte[] soundBytes = Sub_Code.ReadResourceData(randomSound.FilePath);
                        seIntPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);
                        IntPtr pin = seIntPtr.AddrOfPinnedObject();
                        seHandle = Bass.BASS_StreamCreateFile(pin, 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_LOOP);
                    }
                    else if (File.Exists(randomSound.FilePath))
                    {
                        if (Path.GetExtension(randomSound.FilePath) == ".flac")
                            seHandle = BassFlac.BASS_FLAC_StreamCreateFile(randomSound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                        else
                            seHandle = Bass.BASS_StreamCreateFile(randomSound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                    }
                    soundTimes[0] = Bass.BASS_ChannelBytes2Seconds(seHandle, Bass.BASS_ChannelGetLength(seHandle, BASSMode.BASS_POS_BYTES));

                    streams[0] = BassFx.BASS_FX_TempoCreate(seHandle, BASSFlag.BASS_FX_FREESOURCE | BASSFlag.BASS_STREAM_DECODE);

                    streamSEGainHandle = Bass.BASS_ChannelSetFX(streams[0], BASSFXType.BASS_FX_BFX_VOLUME, 0);

                    soundSEGainSetting.fVolume = (float)Math.Pow(10d, tempType.Gain / 20.0);
                    Bass.BASS_FXSetParameters(streamSEGainHandle, soundSEGainSetting);

                    Console.Out.WriteLine("SEVolume = " + Math.Pow(10d, tempType.Gain / 20.0));

                    playSEName = Path.GetFileName(randomSound.FilePath);
                }
            }
        }
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 300);
        string playVoiceName = "なし";
        streams.Add(0);
        soundTimes.Add(0);
        selectedVoiceIndex = -1;
        if (eventSetting.Sounds.Count > 0)
        {
            int maxProbability = 0;
            foreach (CVoiceSoundSetting sound in eventSetting.Sounds)
                maxProbability += (int)sound.Probability;
            int randomProbability = Sub_Code.RandomValue.Next(0, maxProbability + 1);
            int nowProbability = 0;
            if (maxProbability > 0)
            {
                for (int i = 0; i < eventSetting.Sounds.Count; i++)
                {
                    if (nowProbability + eventSetting.Sounds[i].Probability >= randomProbability)
                    {
                        int voiceSoundHandle = 0;
                        if (eventSetting.Sounds[i].FilePath.Contains('\\') || eventSetting.Sounds[i].FilePath.Contains('/'))
                        {
                            if (Path.GetExtension(eventSetting.Sounds[i].FilePath) == ".flac")
                                voiceSoundHandle = BassFlac.BASS_FLAC_StreamCreateFile(eventSetting.Sounds[i].FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                            else
                                voiceSoundHandle = Bass.BASS_StreamCreateFile(eventSetting.Sounds[i].FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                        }
                        else if (wvsFile != null)
                        {
                            soundBytes = wvsFile.Load_Sound(eventSetting.Sounds[i].StreamPosition);
                            if (soundPtr.IsAllocated)
                                soundPtr.Free();
                            if (soundBytes != null)
                            {
                                soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);
                                IntPtr pin = soundPtr.AddrOfPinnedObject();
                                if (Path.GetExtension(eventSetting.Sounds[i].FilePath) == ".flac")
                                    voiceSoundHandle = BassFlac.BASS_FLAC_StreamCreateFile(pin, 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                                else
                                    voiceSoundHandle = Bass.BASS_StreamCreateFile(pin, 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                            }
                        }
                        playVoiceName = Path.GetFileName(eventSetting.Sounds[i].FilePath);
                        streams[1] = BassFx.BASS_FX_TempoCreate(voiceSoundHandle, BASSFlag.BASS_FX_FREESOURCE | BASSFlag.BASS_STREAM_DECODE);
                        selectedVoiceIndex = i;
                        break;
                    }
                    nowProbability += (int)eventSetting.Sounds[i].Probability;
                }
            }
        }
        int streamMixHandle = Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamCreate((int)eventFreq, 2, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        if (selectedVoiceIndex != -1)
        {
            Bass.BASS_ChannelGetAttribute(streams[1], BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, ref voiceFreq);
            streamVoiceLPFHandle = Bass.BASS_ChannelSetFX(streams[1], BASSFXType.BASS_FX_BFX_BQF, 2);
            streamVoiceHPFHandle = Bass.BASS_ChannelSetFX(streams[1], BASSFXType.BASS_FX_BFX_BQF, 1);
            streamVoiceGainHandle = Bass.BASS_ChannelSetFX(streams[1], BASSFXType.BASS_FX_BFX_VOLUME, 3);

            Change_Voice_Effect(streams[1], eventSetting.Sounds[selectedVoiceIndex]);
            if (eventSetting.Sounds[selectedVoiceIndex].PlayTime.End == 0)
                soundTimes[1] = Bass.BASS_ChannelBytes2Seconds(streams[1], Bass.BASS_ChannelGetLength(streams[1], BASSMode.BASS_POS_BYTES)) - eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start;
            else
                soundTimes[1] = eventSetting.Sounds[selectedVoiceIndex].PlayTime.End - eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start;
            long startPos = Bass.BASS_ChannelSeconds2Bytes(streams[1], eventSetting.Delay + eventSetting.Sounds[selectedVoiceIndex].Delay);
            Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannelEx(streamMixHandle, streams[1], BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE, startPos, 0);
        }
        Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(streamMixHandle, streams[0], BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        streamHandle = BassFx.BASS_FX_TempoCreate(streamMixHandle, BASSFlag.BASS_FX_FREESOURCE);
        streamEventLPFHandle = Bass.BASS_ChannelSetFX(streamHandle, BASSFXType.BASS_FX_BFX_BQF, 2);
        streamEventHPFHandle = Bass.BASS_ChannelSetFX(streamHandle, BASSFXType.BASS_FX_BFX_BQF, 1);
        streamEventGainHandle = Bass.BASS_ChannelSetFX(streamHandle, BASSFXType.BASS_FX_BFX_VOLUME, 3);

        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);
        musicEndFunc = new SYNCPROC(EndSync);
        Change_Event_Effect();
        maxTimeStream = -1;
        if (soundTimes.Count > 0)
        {
            PlayTime_S.Maximum = soundTimes.Max();
            maxTimeStream = soundTimes.IndexOf(PlayTime_S.Maximum);
            maxTime = Sub_Code.Get_Time_String(PlayTime_S.Maximum);
            PlayTime_T.Text = "00:00 / " + maxTime;
        }
        else
        {
            PlayTime_S.Maximum = 0;
            maxTime = "00:00";
            PlayTime_T.Text = "00:00 / 00:00";
        }
        About_T.Text = "SE -> " + playSEName + "\n音声 -> " + playVoiceName;

        _ = Bass.BASS_ChannelSetSync(streams[maxTimeStream], BASSSync.BASS_SYNC_END | BASSSync.BASS_SYNC_MIXTIME, 0, musicEndFunc, IntPtr.Zero);
        /*if (selectedVoiceIndex != -1)
            Bass.BASS_ChannelSetPosition(streams[1], eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start);
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100f);
        soundTimes.Clear();
        if (selectedVoiceIndex != -1 && eventSetting.Sounds[selectedVoiceIndex].IsFadeIn)
        {
            Bass.BASS_ChannelSetAttribute(streams[1], BASSAttribute.BASS_ATTRIB_VOL, 0f);
            double playTime = 0;
            if (eventSetting.Sounds[selectedVoiceIndex].PlayTime.End != 0)
                playTime = eventSetting.Sounds[selectedVoiceIndex].PlayTime.End - eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start;
            else
                playTime = eventSetting.Sounds[selectedVoiceIndex].PlayTime.Max - eventSetting.Sounds[selectedVoiceIndex].PlayTime.Start;
            if (playTime > 0.5)
                Play_Volume_Animation(30f, streams[1], 1f);
        }*/

        Play_Volume_Animation(0);
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

    //エフェクト更新ボタン
    private void Effect_Update_B_Clicked(object? sender, EventArgs e)
    {
        Change_Event_Effect();
    }

    //エフェクトを更新
    //mode = -1 : すべてのエフェクトを更新
    //mode = 0  : 音量(ゲイン)のみ更新
    //mode = 1  : ピッチのみ更新
    //mode = 2  : Low Pass Filterのみ更新
    //mode = 3  : High Pass Filterのみ更新
    void Change_Event_Effect(int mode = -1)
    {
        if (eventSetting == null)
            return;

        //音量(ゲイン)の更新
        if (mode == -1 || mode == 0)
        {
            double volume;
            //音量の範囲が有効の場合
            if (eventSetting.IsVolumeRange)
            {
                if (eventSetting.VolumeRange.End >= eventSetting.VolumeRange.Start)
                    volume = Sub_Code.Get_Random_Double(eventSetting.VolumeRange.Start, eventSetting.VolumeRange.End);
                else
                    volume = Sub_Code.Get_Random_Double(eventSetting.VolumeRange.End, eventSetting.VolumeRange.Start);
            }
            else
                volume = eventSetting.Volume;
            soundEventGainSetting.fVolume = (float)Math.Pow(10d, volume / 20.0);
            Bass.BASS_FXSetParameters(streamEventGainHandle, soundEventGainSetting);
        }
        //ピッチを更新
        if (mode == -1 || mode == 1)
        {
            int pitch;
            //ピッチの範囲が有効の場合
            if (eventSetting.IsPitchRange)
            {
                if (eventSetting.PitchRange.End >= eventSetting.PitchRange.Start)
                    pitch = Sub_Code.RandomValue.Next(eventSetting.PitchRange.Start, eventSetting.PitchRange.End + 1);
                else
                    pitch = Sub_Code.RandomValue.Next(eventSetting.PitchRange.End, eventSetting.PitchRange.Start);
            }
            else
                pitch = eventSetting.Pitch;
            SetPitch(streamHandle, pitch);
        }
        //Low Pass Filterを更新
        if (mode == -1 || mode == 2)
        {
            int lpf;
            //LPFの範囲が有効の場合
            if (eventSetting.IsLPFRange)
            {
                if (eventSetting.LPFRange.End >= eventSetting.LPFRange.Start)
                    lpf = Sub_Code.RandomValue.Next(eventSetting.LPFRange.Start, eventSetting.LPFRange.End + 1);
                else
                    lpf = Sub_Code.RandomValue.Next(eventSetting.LPFRange.End, eventSetting.LPFRange.Start);
            }
            else
                lpf = eventSetting.LowPassFilter;
            soundEventLPFSetting.fCenter = Get_LPF_Value(lpf);
            Bass.BASS_FXSetParameters(streamEventLPFHandle, soundEventLPFSetting);
        }
        //High Pass Filterを更新
        if (mode == -1 || mode == 3)
        {
            int hpf;
            //HPF範囲が有効の場合
            if (eventSetting.IsHPFRange)
            {
                if (eventSetting.HPFRange.End >= eventSetting.HPFRange.Start)
                    hpf = Sub_Code.RandomValue.Next(eventSetting.HPFRange.Start, eventSetting.HPFRange.End + 1);
                else
                    hpf = Sub_Code.RandomValue.Next(eventSetting.HPFRange.End, eventSetting.HPFRange.Start);
            }
            else
                hpf = eventSetting.HighPassFilter;
            soundEventHPFSetting.fCenter = Get_HPF_Value(hpf);
            Bass.BASS_FXSetParameters(streamEventHPFHandle, soundEventHPFSetting);
        }
    }
    //エフェクトを更新
    //mode = -1 : すべてのエフェクトを更新
    //mode = 0  : 音量(ゲイン)のみ更新
    //mode = 1  : ピッチのみ更新
    //mode = 2  : Low Pass Filterのみ更新
    //mode = 3  : High Pass Filterのみ更新
    void Change_Voice_Effect(int stream, CVoiceSoundSetting soundSetting)
    {
        if (eventSetting == null)
            return;

        //音量(ゲイン)の更新
        double volume;

        //音量の範囲が有効の場合
        if (soundSetting.IsVolumeRange)
        {
            if (soundSetting.VolumeRange.End >= soundSetting.VolumeRange.Start)
                volume = Sub_Code.Get_Random_Double(soundSetting.VolumeRange.Start, soundSetting.VolumeRange.End);
            else
                volume = Sub_Code.Get_Random_Double(soundSetting.VolumeRange.End, soundSetting.VolumeRange.Start);
        }
        else
            volume = soundSetting.Volume;
        soundVoiceGainSetting.fVolume = (float)Math.Pow(10d, volume / 20.0);
        Bass.BASS_FXSetParameters(streamVoiceGainHandle, soundVoiceGainSetting);
        Console.Out.WriteLine("VoiceVolume = " + volume);

        //ピッチを更新
        int pitch;

        //ピッチの範囲が有効の場合
        if (soundSetting.IsPitchRange)
        {
            if (soundSetting.PitchRange.End >= soundSetting.PitchRange.Start)
                pitch = Sub_Code.RandomValue.Next(soundSetting.PitchRange.Start, soundSetting.PitchRange.End + 1);
            else
                pitch = Sub_Code.RandomValue.Next(soundSetting.PitchRange.End, soundSetting.PitchRange.Start);
        }
        else
            pitch = soundSetting.Pitch;
        SetPitch(stream, pitch);

        //Low Pass Filterを更新
        int lpf;

        //LPFの範囲が有効の場合
        if (soundSetting.IsLPFRange)
        {
            if (soundSetting.LPFRange.End >= soundSetting.LPFRange.Start)
                lpf = Sub_Code.RandomValue.Next(soundSetting.LPFRange.Start, soundSetting.LPFRange.End + 1);
            else
                lpf = Sub_Code.RandomValue.Next(soundSetting.LPFRange.End, soundSetting.LPFRange.Start);
        }
        else
            lpf = soundSetting.LowPassFilter;
        soundVoiceLPFSetting.fCenter = Get_LPF_Value(lpf);
        Bass.BASS_FXSetParameters(streamVoiceLPFHandle, soundVoiceLPFSetting);

        //High Pass Filterを更新
        int hpf;

        //HPF範囲が有効の場合
        if (soundSetting.IsHPFRange)
        {
            if (soundSetting.HPFRange.End >= soundSetting.HPFRange.Start)
                hpf = Sub_Code.RandomValue.Next(soundSetting.HPFRange.Start, soundSetting.HPFRange.End + 1);
            else
                hpf = Sub_Code.RandomValue.Next(soundSetting.HPFRange.End, soundSetting.HPFRange.Start);
        }
        else
            hpf = soundSetting.HighPassFilter;
        soundVoiceHPFSetting.fCenter = Get_HPF_Value(hpf);
        Bass.BASS_FXSetParameters(streamVoiceHPFHandle, soundVoiceHPFSetting);
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
        if (eventSetting == null)
            return;

        eventSetting.IsVolumeRange = e.Value;

        Change_Range_Mode();
    }
    //速度の範囲指定の有無
    private void Pitch_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (eventSetting == null)
            return;

        eventSetting.IsPitchRange = e.Value;

        Change_Range_Mode();
    }
    //LPFの範囲指定の有無
    private void LPF_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (eventSetting == null)
            return;

        eventSetting.IsLPFRange = e.Value;

        Change_Range_Mode();
    }
    //HPFの範囲指定の有無
    private void HPF_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (eventSetting == null)
            return;

        eventSetting.IsHPFRange = e.Value;

        Change_Range_Mode();
    }

    //範囲のシークバーの表示・非表示
    private void Change_Range_Mode()
    {
        Effect_Update_B.IsVisible = false;
        if (eventSetting != null)
        {
            Gain_Range_C.IsChecked = eventSetting.IsVolumeRange;
            Gain_End_T.IsVisible = eventSetting.IsVolumeRange;
            Gain_End_S.IsVisible = eventSetting.IsVolumeRange;
            Pitch_Range_C.IsChecked = eventSetting.IsPitchRange;
            Pitch_End_T.IsVisible = eventSetting.IsPitchRange;
            Pitch_End_S.IsVisible = eventSetting.IsPitchRange;
            LPF_Range_C.IsChecked = eventSetting.IsLPFRange;
            LPF_End_T.IsVisible = eventSetting.IsLPFRange;
            LPF_End_S.IsVisible = eventSetting.IsLPFRange;
            HPF_Range_C.IsChecked = eventSetting.IsHPFRange;
            HPF_End_T.IsVisible = eventSetting.IsHPFRange;
            HPF_End_S.IsVisible = eventSetting.IsHPFRange;
            if (eventSetting.IsVolumeRange || eventSetting.IsPitchRange || eventSetting.IsLPFRange || eventSetting.IsHPFRange)
                Effect_Update_B.IsVisible = true;
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


    //サウンドを解放
    void Sound_Dispose()
    {
        maxTimeStream = -1;
        selectedVoiceIndex = -1;
        Bass.BASS_ChannelRemoveFX(streamHandle, streamEventLPFHandle);
        Bass.BASS_ChannelRemoveFX(streamHandle, streamEventHPFHandle);
        if (streams.Count >= 2)
        {
            Bass.BASS_ChannelRemoveFX(streams[1], streamVoiceLPFHandle);
            Bass.BASS_ChannelRemoveFX(streams[1], streamVoiceHPFHandle);
        }
        Bass.BASS_FXReset(streamEventLPFHandle);
        Bass.BASS_FXReset(streamEventHPFHandle);
        Bass.BASS_FXReset(streamVoiceLPFHandle);
        Bass.BASS_FXReset(streamVoiceHPFHandle);
        Bass.BASS_ChannelStop(streamHandle);
        Bass.BASS_StreamFree(streamHandle);
        if (streams.Count >= 2)
        {
            Bass.BASS_StreamFree(streams[0]);
            Bass.BASS_StreamFree(streams[1]);
        }
        if (soundPtr.IsAllocated)
            soundPtr.Free();
        if (seIntPtr.IsAllocated)
            seIntPtr.Free();
        soundBytes = null;
        streams.Clear();
        PlayTime_T.Text = "00:00 / 00:00";
        PlayTime_S.Value = 0;
        PlayTime_S.Maximum = 0;
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        bShowing = false;
        Sound_Dispose();
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