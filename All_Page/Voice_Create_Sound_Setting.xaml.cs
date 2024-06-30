using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Voice_Create_Sound_Setting : ContentPage
{
    //�C�x���g�ݒ���
    readonly Voice_Create_Event_Setting eventSettingWindow = new();

    WVS_Load? wvsFile = null;
    SYNCPROC? musicEndFunc = null;
    GCHandle soundPtr = new();

    //LPF�AHPF�AGain�̐ݒ�N���X
    readonly BASS_BFX_BQF soundLPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_LOWPASS, 12000f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_BQF soundHPFSetting = new(BASSBFXBQF.BASS_BFX_BQF_HIGHPASS, 0f, 0f, 0f, 1f, 0f, BASSFXChan.BASS_BFX_CHANALL);
    readonly BASS_BFX_VOLUME soundGainSetting = new(1.0f);

    List<CVoiceSoundList> sounds = [];

    ViewCell? lastVoiceSoundCell = null;

    string maxTime = "";

    float freq = 44100.0f;      //�T�E���h�̎��g��

    //�T�E���h�̃n���h��
    int streamHandle;       //���C���n���h��(����ɃT�E���h�f�[�^�������Ă���)
    int streamLPFHandle;    //LPF�n���h��(LPF�̐ݒ�p)
    int streamHPFHandle;    //HPF�n���h��(HPF�̐ݒ�p)
    int streamGainHandle;   //Gain�n���h��(Gain�̐ݒ�p)

    int beforeLPFValue;
    int beforeHPFValue;

    bool bPaused;               //�ꎞ��~��
    bool bLocationChanging;     //�V�[�N�o�[���^�b�`������true
    bool bPlayingMouseDown;     //�V�[�N�o�[���^�b�`������true
    bool bNoSelectMode;         //���X�g���ύX����Ă��T�E���h�����[�h���Ȃ�
    bool bMessageShowing;       //���b�Z�[�W�\����
    bool bEnded;                //�Đ����I��������true
    bool bShowing;              //���̉�ʂ�\�������ǂ���
    bool bOtherPageOpened;      //�ق��̉�ʂ��J����Ă��邩

    public Voice_Create_Sound_Setting()
	{
		InitializeComponent();

        //�X���C�_�[�̐ݒ�
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

        //�V�[�N�o�[�̐ݒ�
        PlayTime_S.DragStarted += PlayTime_S_DragStarted; ;
        PlayTime_S.DragCompleted += PlayTime_S_DragCompleted;
        PlayTime_S.ValueChanged += PlayTime_S_ValueChanged;

        //�{�^���̐ݒ�
        Pause_B.Clicked += Pause_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Minus_B.Clicked += Minus_B_Clicked;
        Plus_B.Clicked += Plus_B_Clicked;
        Effect_Update_B.Clicked += Effect_Update_B_Clicked;
        EventSetting_B.Clicked += EventSetting_B_Clicked;
        //�l���Z�b�g�{�^��
        Gain_Reset_B.Clicked += Gain_Reset_B_Clicked;
        Pitch_Reset_B.Clicked += Pitch_Reset_B_Clicked;
        LPF_Reset_B.Clicked += LPF_Reset_B_Clicked;
        HPF_Reset_B.Clicked += HPF_Reset_B_Clicked;

        //�͈͎w��`�F�b�N�{�b�N�X
        Gain_Range_C.CheckedChanged += Gain_Range_C_CheckedChanged;
        Pitch_Range_C.CheckedChanged += Pitch_Range_C_CheckedChanged;
        LPF_Range_C.CheckedChanged += LPF_Range_C_CheckedChanged;
        HPF_Range_C.CheckedChanged += HPF_Range_C_CheckedChanged;
    }

    //������(��ʂ�\��������x�ɌĂяo���K�v����)
    public void Initialize(CVoiceTypeSetting voiceEvent, List<CVoiceSoundList> voiceSoundList, WVS_Load wvsFile)
    {
        //�T�E���h���X�g�ɃT�E���h��������
        Voice_Sound_L.SelectedItem = null;
        this.wvsFile = wvsFile;
        sounds = voiceSoundList;
        Voice_Sound_L.SelectedItem = null;
        Voice_Sound_L.ItemsSource = null;
        Voice_Sound_L.ItemsSource = voiceSoundList;
        bShowing = true;
        //�C�x���g�ݒ��ʂ�������
        eventSettingWindow.Initialize(wvsFile, voiceEvent);
        //�V�[�N�o�[�p�̃��[�v�������J�n
        Position_Change();
        Change_Range_Mode();
    }

    //��ʉE�����Ƀ��b�Z�[�W��\��
    private async void Message_Feed_Out(string message)
    {
        //�e�L�X�g�������Ԍo������t�F�[�h�A�E�g
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

    //���[�v (��ɃV�[�N�o�[�p)
    private async void Position_Change()
    {
        double nextFrame = Environment.TickCount;
        float period = 1000f / 20f;
        while (bShowing)
        {
            //��莞�Ԍo�߂���܂őҋ@
            double tickCount = Environment.TickCount;
            if (tickCount < nextFrame)
            {
                if (nextFrame - tickCount > 1)
                    await Task.Delay((int)(nextFrame - tickCount));
                continue;
            }

            //�Đ������ǂ���
            bool bPlaying = Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING && !bLocationChanging;
            if (bPlaying)
            {
                //�o�b�t�@�[���X�V (�T�C�Y���傫���Ȃ�΂Ȃ�قǍ����ׂɂȂ邪�A�G�t�F�N�g�̐ݒ肪�����f�����悤�ɂȂ�)
                _ = Bass.BASS_ChannelUpdate(streamHandle, 400);

                if (Voice_Sound_L.SelectedItem != null)
                {
                    CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;

                    //�Đ����Ԃ𐧌����Ă����ꍇ�A���͈̔͂𒴂����烊�Z�b�g������ (Android�ł͍Đ����Ԃ̐ݒ�͂ł��Ȃ��d�l�̂��߈Ӗ��Ȃ�)
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

                    //���݂̍Đ����Ԃ�\��
                    long position = Bass.BASS_ChannelGetPosition(streamHandle);
                    PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streamHandle, position);
                    PlayTime_T.Text = Sub_Code.Get_Time_String(PlayTime_S.Value) + " / " + maxTime;
                }
            }

            //�Đ����I�������V�[�N�o�[��0�ɖ߂�
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

    //�T�E���h�̏I�����m
    private async void EndSync(int handle, int channel, int data, IntPtr user)
    {
        if (!bEnded)
        {
            await Task.Delay(500);
            bEnded = true;
        }
    }

    //�x�����Ԃ�ύX
    private void Delay_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;
        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        double value = Math.Round(e.NewValue, 2, MidpointRounding.AwayFromZero);
        voiceSoundSetting.Delay = value;
        Delay_T.Text = "�x��:" + value + "�b";
    }

    //�D��x�̃X���C�_�[�𗣂����烊�X�g�̃e�L�X�g�ɔ��f
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
    //�D��x��ύX
    private void Priority_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Priority_T.Text = "�D��x:" + (int)Priority_S.Value;
        if (Voice_Sound_L.SelectedItem != null)
        {
            bNoSelectMode = true;
            CVoiceSoundList voiceSoundList = (CVoiceSoundList)Voice_Sound_L.SelectedItem;
            voiceSoundList.VoiceSoundSetting.Probability = Priority_S.Value;
            bNoSelectMode = false;
        }
    }

    //HPF�̒l(�͈�)��ύX
    private void HPF_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        HPF_End_T.Text = "�`:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem == null)
            return;
        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.HPFRange.End = (int)e.NewValue;
    }

    //HPF�̒l��ύX
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

        //�͈͎w��łȂ���Α����f
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

    //LPF�̒l(�͈�)��ύX
    private void LPF_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        LPF_End_T.Text = "�`:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem == null)
            return;
        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.LPFRange.End = (int)e.NewValue;
    }

    //LPF�̒l��ύX
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

        //�͈͎w��łȂ���Α����f
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

    //�s�b�`(�Đ����x)�̒l(�͈�)��ύX
    private void Pitch_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_End_T.Text = "�`:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            voiceSoundSetting.PitchRange.End = (int)e.NewValue;
        }
    }

    //�s�b�`(�Đ����x)�̒l��ύX
    private void Pitch_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_Start_T.Text = "�s�b�`:" + (int)e.NewValue;
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

    //����(�Q�C��)�̒l(�͈�)��ύX
    private void Gain_End_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Gain_End_T.Text = "�`:" + (int)e.NewValue;
        if (Voice_Sound_L.SelectedItem != null)
        {
            CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
            voiceSoundSetting.VolumeRange.End = e.NewValue;
        }
    }

    //����(�Q�C��)�̒l��ύX
    private void Gain_Start_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Gain_Start_T.Text = "�Q�C��(db):" + (int)e.NewValue;
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

    //�S�̂̉��ʂ�ύX
    private void All_Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);
        All_Volume_T.Text = "�S�̉���:" + (int)e.NewValue;
    }

    //�T�E���h�t�@�C����I��
    private async void Sound_List_Tapped(object sender, EventArgs e)
    {
        //���X�g�̑I�����̔w�i�F���w��

        //1�O�ɑI�����Ă������ڂ̔w�i�͓�����
        if (lastVoiceSoundCell != null)
            lastVoiceSoundCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        //���I�������w�i���D�F��
        if (viewCell.View != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastVoiceSoundCell = viewCell;
        }

        //���I���̏ꍇ��~
        if (Voice_Sound_L.SelectedItem == null || bNoSelectMode)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;

        //�t�@�C�������݂��邩�m�F
        if (voiceSoundSetting.FilePath.Contains('\\') || voiceSoundSetting.FilePath.Contains('/'))
        {
            if (!File.Exists(voiceSoundSetting.FilePath))
            {
                Message_Feed_Out("�����t�@�C�������݂��܂���B�폜���ꂽ�\��������܂��B");
                return;
            }
        }

        //�Đ����̃T�E���h���~
        await Pause_Volume_Animation(true, 10);

        //�������Ă���G�t�F�N�g���폜
        Bass.BASS_ChannelRemoveFX(streamHandle, streamLPFHandle);
        Bass.BASS_ChannelRemoveFX(streamHandle, streamHPFHandle);
        Bass.BASS_ChannelRemoveFX(streamHandle, streamGainHandle);
        Bass.BASS_FXReset(streamLPFHandle);
        Bass.BASS_FXReset(streamHPFHandle);
        Bass.BASS_FXReset(streamGainHandle);
        Bass.BASS_StreamFree(streamHandle);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 200);

        //�T�E���h���G���W���ɓǂݍ���
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
            //�T�E���h��.wvs�t�@�C���ɓ����Ă���ꍇ�̓o�C�g�z��ɓǂݍ���ŃG���W���Ƀ|�C���^��n��
            byte[]? soundBytes = wvsFile.Load_Sound(voiceSoundSetting.StreamPosition);
            if (soundBytes != null)
            {
                if (soundPtr.IsAllocated)
                    soundPtr.Free();

                //soundBytes������ɔj������Ȃ��悤�ɌŒ肳����
                soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);

                //�g���q��.flac�̏ꍇ�ʏ�̓ǂݍ��݂ł͕s����ɂȂ邽�ߐ�p�̊֐����Ă�
                if (Path.GetExtension(voiceSoundSetting.FilePath) == ".flac")
                    baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                else
                    baseHandle = Bass.BASS_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            }
        }

        //FX��K���ł���`��
        streamHandle = BassFx.BASS_FX_TempoCreate(baseHandle, BASSFlag.BASS_FX_FREESOURCE);

        //�o�b�t�@�[�T�C�Y��500�ɐݒ�
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);

        //�T�E���h�̒������擾���A�V�[�N�o�[�ɔ��f
        PlayTime_S.Maximum = Bass.BASS_ChannelBytes2Seconds(streamHandle, Bass.BASS_ChannelGetLength(streamHandle, BASSMode.BASS_POS_BYTES));
        voiceSoundSetting.PlayTime.Max = PlayTime_S.Maximum;
        PlayTime_T.Text = "00:00 / " + maxTime;

        //�I�����m
        musicEndFunc = new SYNCPROC(EndSync);

        //�G�t�F�N�g��K��
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

        //�T�E���h�ݒ肩��G�t�F�N�g�̒l���w��
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

        //�G�t�F�N�g��K��
        SetPitch((int)Pitch_Start_S.Value);
        Change_Effect();
        bPaused = true;

        //�T�E���h�̒�����\��
        maxTime = Sub_Code.Get_Time_String(PlayTime_S.Maximum);
        Change_Range_Mode();
    }

    //���X�Ɉꎞ��~
    private async Task Pause_Volume_Animation(bool bStopMode, float fadeTime = 30f)
    {
        bPaused = true;
        float volumeNow = 1f;
        Bass.BASS_ChannelGetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref volumeNow);
        float Volume_Minus = volumeNow / fadeTime;
        //���ʂ�������������
        while (volumeNow > 0f && bPaused)
        {
            volumeNow -= Volume_Minus;
            if (volumeNow < 0f)
                volumeNow = 0f;
            Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, volumeNow);
            await Task.Delay(1000 / 60);
        }
        //���ʂ�0�ɂȂ�����Đ����~�߂�
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

    //���X�ɍĐ�
    private async void Play_Volume_Animation(float fadeTime = 30f)
    {
        bPaused = false;
        //Change_Effect();
        Bass.BASS_ChannelPlay(streamHandle, false);
        float volumeNow = 1f;
        Bass.BASS_ChannelGetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, ref volumeNow);

        //1�t���[���ŉ����鉹�ʂ�ݒ�
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

    //�Đ��ʒu��ύX
    void Music_Pos_Change(double Pos, bool IsBassPosChange)
    {
        if (IsBassPosChange)
            Bass.BASS_ChannelSetPosition(streamHandle, Pos);
        PlayTime_T.Text = Sub_Code.Get_Time_String(Pos) + " / " + maxTime;
    }

    //�V�[�N�o�[�𓮂�������
    private void PlayTime_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        //�^�b�`���Ă��Ȃ��ꍇ�͔������Ȃ�
        if (bLocationChanging)
            Music_Pos_Change(PlayTime_S.Value, false);
    }

    //�V�[�N�o�[���^�b�`������
    private void PlayTime_S_DragStarted(object? sender, EventArgs e)
    {
        bLocationChanging = true;
        if (Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING)
        {
            bPlayingMouseDown = true;
            _ = Pause_Volume_Animation(false, 10);
        }
    }

    //�V�[�N�o�[�𗣂�����
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

    //�Đ�
    private void Play_B_Clicked(object? sender, EventArgs e)
    {
        Play_Volume_Animation(5);
    }

    //�ꎞ��~
    private void Pause_B_Clicked(object? sender, EventArgs e)
    {
        _ = Pause_Volume_Animation(false, 5);
    }

    //�Đ�����+5�b
    private void Plus_B_Clicked(object? sender, EventArgs e)
    {
        if (PlayTime_S.Value + 5.0 >= PlayTime_S.Maximum)
            PlayTime_S.Value = PlayTime_S.Maximum;
        else
            PlayTime_S.Value += 5.0;
        Music_Pos_Change(PlayTime_S.Value, true);
    }

    //�Đ�����-5�b
    private void Minus_B_Clicked(object? sender, EventArgs e)
    {
        if (PlayTime_S.Value <= 5.0)
            PlayTime_S.Value = 0;
        else
            PlayTime_S.Value -= 5.0;
        Music_Pos_Change(PlayTime_S.Value, true);
    }

    //�͈͂̃V�[�N�o�[�̕\���E��\��
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

    //�s�b�`�̃X���C�_�[����l���擾
    private void SetPitch(int pitch)
    {
        //pitch��0�̏ꍇ�̓f�t�H���g�̍Đ����x�ɂ���
        if (pitch == 0)
        {
            Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, freq);
            return;
        }
        int key = 0;
        int index = 0;

        //�X���C�_�[�̒l����Wwise�̃s�b�`�̎d�l�Ɉ�ԋ߂��l���擾
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

    //LPF�̃X���C�_�[����l���擾
    private static int Get_LPF_Value(int value)
    {
        int newValue = value == 0 ? 1 : value;
        int index = (int)Math.Floor(newValue / 10.0);
        int key = Sub_Code.LPFValues.Keys.ElementAt(index);
        return key - (int)(Sub_Code.LPFValues[key] * Sub_Code.Get_Decimal(newValue / 10.0));
    }

    //HPF�̃X���C�_�[����l���擾
    private static int Get_HPF_Value(int value)
    {
        int newValue = value == 0 ? 1 : value;
        int index = (int)Math.Floor(newValue / 5.0);
        int key = Sub_Code.HPFValues.Keys.ElementAt(index);
        return key + (int)(Sub_Code.HPFValues[key] * Sub_Code.Get_Decimal(newValue / 5.0));
    }

    //�Q�C���̒l�����Z�b�g (Android�̃X���C�_�[�ł�0.0�ɍ��킹��̂��������)
    private void Gain_Reset_B_Clicked(object? sender, EventArgs e)
    {
        Gain_Start_S.Value = 0.0;
        Gain_End_S.Value = 0.0;
    }
    //���x�̒l�����Z�b�g
    private void Pitch_Reset_B_Clicked(object? sender, EventArgs e)
    {
        Pitch_Start_S.Value = 0.0;
        Pitch_End_S.Value = 0.0;
    }
    //LPF�̒l�����Z�b�g
    private void LPF_Reset_B_Clicked(object? sender, EventArgs e)
    {
        LPF_Start_S.Value = 0.0;
        LPF_End_S.Value = 0.0;
    }
    //HPF�̒l�����Z�b�g
    private void HPF_Reset_B_Clicked(object? sender, EventArgs e)
    {
        HPF_Start_S.Value = 0.0;
        HPF_End_S.Value = 0.0;
    }

    //�Q�C���͈͎̔w��̗L��
    private void Gain_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsVolumeRange = e.Value;

        Change_Range_Mode();
    }
    //���x�͈͎̔w��̗L��
    private void Pitch_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsPitchRange = e.Value;

        Change_Range_Mode();
    }
    //LPF�͈͎̔w��̗L��
    private void LPF_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsLPFRange = e.Value;

        Change_Range_Mode();
    }
    //HPF�͈͎̔w��̗L��
    private void HPF_Range_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;
        voiceSoundSetting.IsHPFRange = e.Value;

        Change_Range_Mode();
    }

    //�G�t�F�N�g���X�V
    //mode = -1 : ���ׂẴG�t�F�N�g���X�V
    //mode = 0  : ����(�Q�C��)�̂ݍX�V
    //mode = 1  : �s�b�`�̂ݍX�V
    //mode = 2  : Low Pass Filter�̂ݍX�V
    //mode = 3  : High Pass Filter�̂ݍX�V
    void Change_Effect(int mode = -1)
    {
        if (Voice_Sound_L.SelectedItem == null)
            return;

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Voice_Sound_L.SelectedItem).VoiceSoundSetting;

        //����(�Q�C��)�̍X�V
        if (mode == -1 || mode == 0)
        {
            double volume;
            //���ʂ͈̔͂��L���̏ꍇ
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
        //�s�b�`���X�V
        if (mode == -1 || mode == 1)
        {
            int pitch;
            //�s�b�`�͈̔͂��L���̏ꍇ
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
        //Low Pass Filter���X�V
        if (mode == -1 || mode == 2)
        {
            int lpf;
            //LPF�͈̔͂��L���̏ꍇ
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
        //High Pass Filter���X�V
        if (mode == -1 || mode == 3)
        {
            int hpf;
            //HPF�͈͂��L���̏ꍇ
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

    //�G�t�F�N�g�X�V�{�^��
    private void Effect_Update_B_Clicked(object? sender, EventArgs e)
    {
        Change_Effect();
    }

    //��ʂ������
    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        //���[�v�v���O�������~
        bShowing = false;
        _ = Pause_Volume_Animation(true, 5.0f);
    }

    //�C�x���g�ݒ��ʂ�
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