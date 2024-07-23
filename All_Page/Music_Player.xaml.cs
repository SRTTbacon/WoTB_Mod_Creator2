using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;
using System.Reflection;
using System.Text;

namespace WoTB_Mod_Creator2.All_Page;

//���X�g���̃T�E���h���
public class Music_Type_List(string filePath, string? fileName = null)
{
    public string FilePath = filePath;
    public string? FileName = fileName;
    public string Name_Text => FileName ?? Path.GetFileName(FilePath);
    public bool IsPlayed = false;
    public Color Name_Color => IsPlayed ? Color.FromArgb("#BF6C6C6C") : Color.FromArgb("#FF00FFFF");

    public static implicit operator Music_Type_List(string filePath) => new(filePath);
}

public partial class Music_Player : ContentPage
{
    readonly List<List<Music_Type_List>> musicList = [];                //�T�E���h���X�g
    List<Music_Type_List> SelectedMusicList => musicList[musicPage];    //���݂̃v���Z�b�g
    Music_Type_List? playingMusicNameNow = null;                        //���ݍĐ����Ă���T�E���h1
    readonly List<Music_Type_List> alreadyPlayedPath = [];              //���ɍĐ��ς݂̃T�E���h
    readonly BASS_BFX_BQF lpfSetting = new(BASSBFXBQF.BASS_BFX_BQF_LOWPASS, 500f, 0f, 0.707f, 0f, 0f, BASSFXChan.BASS_BFX_CHANALL);     //Low Pass Filter�̐ݒ�
    readonly BASS_BFX_BQF hpfSetting = new(BASSBFXBQF.BASS_BFX_BQF_HIGHPASS, 1000f, 0f, 0.707f, 0f, 0f, BASSFXChan.BASS_BFX_CHANALL);   //High Pass Filter�̐ݒ�
    readonly BASS_BFX_ECHO4 echoSetting = new(0, 0, 0, 0, true, BASSFXChan.BASS_BFX_CHANALL);                                           //�G�R�[�̐ݒ�
    SYNCPROC? bMusicEnd = null;                 //�Đ��I�����m�̊֐�
    ViewCell? Music_List_LastCell = null;
    static readonly string[] value = ["audio/*"];
    const string CONFIG_HEADER = "WMC_Format";  //�Z�[�u�t�@�C���̃w�b�_�[
    const byte CONFIG_VERSION = 0x00;           //�Z�[�u�t�@�C���̃o�[�W����
    const byte MUSIC_VERSION = 0x00;            //����
    int stream = 0;                             //�Đ����̃T�E���h�̃n���h��
    int streamLPF = 0;                          //Low Pass Filter�̃n���h��
    int streamHPF = 0;                          //High Pass Filter�̃n���h��
    int streamECHO = 0;                         //�G�R�[�̃n���h��
    int musicPage = 0;                          //�v���Z�b�g�̃y�[�W
    double startTime = 0;                       //���[�v�Đ��̊J�n�ʒu(PC�ł̂�)
    double endTime = 0;                         //���[�v�Đ��̏I���ʒu(PC�ł̂�)
    float musicFrequency = 44100f;              //�Đ����̃T�E���h�̎��g��
    bool bNotMusicChange = false;               //true�̊Ԃ̓��X�g�̑I����Ԃ�ύX���Ă��T�E���h��ύX���Ȃ�
    bool bEnded = false;                        //�Đ����I��������true
    bool bSyncPitch_And_Speed = false;          //�Đ����x�ƃs�b�`�𓯊�
    bool bPaused = false;                       //��~�����ǂ���
    bool bLocationChanging = false;             //�V�[�N�o�[�𓮂����Ă��邩
    bool bPlayingMouseDown = false;             //����
    bool bAddMode = false;                      //�T�E���h��ǉ����̓��X�g��ύX���Ă��T�E���h��ύX���Ȃ�
    bool bMessageShowing = false;               //�����̃��b�Z�[�W���\����
    bool bPageOpen = false;                     //���ɃE�B���h�E���\����
    bool bIgnorePageChange = false;             //���̃E�B���h�E�Ɉړ�����ۃT�E���h���������Ȃ��悤��
    bool bShowing = false;                      //���y�v���C���[�̃E�B���h�E���\�������ǂ���

    readonly Music_Player_Setting_Page settingWindow = new();
    Youtube_Downloader? youtubeDownloaderWindow = null;

    public Music_Player()
	{
		InitializeComponent();

        for (int Number = 0; Number < 9; Number++)
            musicList.Add([]);

        Music_L.ItemTapped += delegate (object? sender, ItemTappedEventArgs e)
        {
            Music_L_SelectionChanged(e, true);
        };

        //�{�^����
        Music_Page_Back_B.Clicked += Music_Page_Back_B_Click;
        Music_Page_Next_B.Clicked += Music_Page_Next_B_Click;
        Music_Pause_B.Clicked += Music_Pause_B_Click;
        Music_Play_B.Clicked += Music_Play_B_Click;
        Music_Add_B.Clicked += Music_Add_B_Click;
        Music_Delete_B.Clicked += Music_Delete_B_Click;
        Music_Minus_B.Clicked += Music_Minus_B_Click;
        Music_Plus_B.Clicked += Music_Plus_B_Click;
        Reset_B.Clicked += Reset_B_Clicked;
        Setting_B.Clicked += Setting_B_Click;
        Clear_B.Clicked += Clear_B_Clicked;
        Youtube_B.Clicked += Youtube_B_Clicked;
        Loop_C.CheckedChanged += Loop_C_CheckedChanged;
        Random_C.CheckedChanged += Random_C_CheckedChanged;
        Mode_C.CheckedChanged += Mode_C_CheckedChanged;
        Volume_S.ValueChanged += Volume_S_ValueChanged;
        Volume_S.DragCompleted += delegate
        {
            Configs_Save();
        };
        Location_S.DragStarted += Location_S_DragStarted;
        Location_S.DragCompleted += Location_S_DragCompleted;
        Location_S.ValueChanged += Location_S_ValueChanged;
        Pitch_S.ValueChanged += Pitch_S_ValueChanged;
        Speed_S.ValueChanged += Speed_S_ValueChanged;
        Pitch_Speed_S.ValueChanged += Pitch_Speed_S_ValueChanged;
        Volume_S.Value = 50;
        Pitch_Speed_S.Value = 50;

        Configs_Load();

        //�����ݒ�𔽉f
        settingWindow.ChangeLPFEnable += delegate (bool IsEnable)
        {
            if (IsEnable)
            {
                lpfSetting.fCenter = 500f + 4000f * (1 - (float)settingWindow.lpfValue / 100f);
                Bass.BASS_FXSetParameters(streamLPF, lpfSetting);
            }
            else
            {
                Bass.BASS_ChannelRemoveFX(stream, streamLPF);
                streamLPF = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_BQF, 2);
            }
        };
        settingWindow.ChangeHPFEnable += delegate (bool IsEnable)
        {
            if (IsEnable)
            {
                hpfSetting.fCenter = 100f + 4000f * (float)settingWindow.hpfValue / 100f;
                Bass.BASS_FXSetParameters(streamHPF, hpfSetting);
            }
            else
            {
                Bass.BASS_ChannelRemoveFX(stream, streamHPF);
                streamHPF = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_BQF, 1);
            }
        };
        settingWindow.ChangeECHOEnable += delegate (bool IsEnable)
        {
            if (IsEnable)
            {
                streamECHO = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_ECHO4, 0);
                echoSetting.fDelay = (float)settingWindow.echoDelayValue;
                echoSetting.fDryMix = (float)settingWindow.echoPowerOriginalValue / 100f;
                echoSetting.fWetMix = (float)settingWindow.echoPowerECHOValue / 100f;
                echoSetting.fFeedback = (float)settingWindow.echoLengthValue / 100f;
                Bass.BASS_FXSetParameters(streamECHO, echoSetting);
            }
            else
                Bass.BASS_ChannelRemoveFX(stream, streamECHO);
        };

        settingWindow.Configs_Load();

        if (!Directory.Exists(Sub_Code.ExDir + "/Configs"))
            Directory.CreateDirectory(Sub_Code.ExDir + "/Configs");
    }

    //Youtuber_Downloader.cs����Ăяo�����
    public void Add_Youtube_Music(Music_Type_List? musicList)
    {
        if (musicList != null)
        {
            this.musicList[musicPage].Add(musicList);
            Music_List_Sort();
            Music_List_Save();
            Message_Feed_Out(musicList.Name_Text + "��ǉ����܂����B");
        }
    }

    //�E�B���h�E�\���������ƃ��[�v (�V�[�N�o�[�̈ʒu������T�E���h�̃u�c�u�c�y��)
    private async void Loop()
    {
        double nextFrame = Environment.TickCount;
        float period = 1000f / 20f;
        while (bShowing)
        {
            //FPS�������Ă�����X�L�b�v
            int tickCount = Environment.TickCount;
            if (tickCount < nextFrame)
            {
                if (nextFrame - tickCount > 1)
                    await Task.Delay((int)(nextFrame - tickCount));
                continue;
            }

            //�Đ������ǂ���
            bool IsPlaying = Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING;
            if (IsPlaying)
            {
                Bass.BASS_ChannelUpdate(stream, 300);
                if (startTime != -1 && Location_S.Value >= endTime)
                {
                    Music_Pos_Change(startTime, true);
                    Set_Position_Slider();
                }
                else if (startTime != -1 && Location_S.Value < startTime)
                {
                    Music_Pos_Change(startTime, true);
                    Set_Position_Slider();
                }
            }
            if (IsPlaying && !bLocationChanging)
            {
                Set_Position_Slider();
                TimeSpan Time = TimeSpan.FromSeconds(Location_S.Value);
                string Minutes = Time.Minutes.ToString();
                string Seconds = Time.Seconds.ToString();
                if (Time.Minutes < 10)
                    Minutes = "0" + Time.Minutes;
                if (Time.Seconds < 10)
                    Seconds = "0" + Time.Seconds;
                Location_T.Text = Minutes + ":" + Seconds;
            }
            if (streamLPF != 0 && settingWindow.IsVisible)
            {
                if (settingWindow.bLPFChanged && settingWindow.bLPFEnable)
                {
                    lpfSetting.fCenter = 500f + 4000f * (1 - (float)settingWindow.lpfValue / 100f);
                    Bass.BASS_FXSetParameters(streamLPF, lpfSetting);
                    settingWindow.bLPFChanged = false;
                }
                if (settingWindow.bHPFChanged && settingWindow.bHPFEnable)
                {
                    hpfSetting.fCenter = 100f + 4000f * (float)settingWindow.hpfValue / 100f;
                    Bass.BASS_FXSetParameters(streamHPF, hpfSetting);
                    settingWindow.bHPFChanged = false;
                }
                if (settingWindow.bECHOChanged && settingWindow.bECHOEnable)
                {
                    echoSetting.fDelay = (float)settingWindow.echoDelayValue;
                    echoSetting.fDryMix = (float)settingWindow.echoPowerOriginalValue / 100f;
                    echoSetting.fWetMix = (float)settingWindow.echoPowerECHOValue / 100f;
                    echoSetting.fFeedback = (float)settingWindow.echoLengthValue / 100f;
                    Bass.BASS_FXSetParameters(streamECHO, echoSetting);
                    settingWindow.bECHOChanged = false;
                }
            }

            //�Đ����I��������ŏ��ɖ߂��������_���ɓ���ւ��邩
            if (bEnded)
            {
                //���[�v�Đ�
                if (Loop_C.IsChecked)
                {
                    Bass.BASS_ChannelStop(stream);
                    Bass.BASS_ChannelPlay(stream, true);
                }
                //�����_���Đ�
                else if (Random_C.IsChecked)
                {
                    if (musicList[musicPage].Count == 1)
                        Bass.BASS_ChannelSetPosition(stream, 0);
                    else
                    {
                        //���ɑS�čĐ����Ă�����ŏ�����
                        if (alreadyPlayedPath.Count >= musicList[musicPage].Count)
                        {
                            alreadyPlayedPath.Clear();
                            foreach (Music_Type_List Info in musicList[musicPage])
                                Info.IsPlayed = false;
                            Music_List_Sort();
                        }
                        else
                            musicList[musicPage][Get_Select_Index()].IsPlayed = true;

                        //���̃T�E���h�������_���ɍĐ�
                        while (true)
                        {
                            int r2 = Sub_Code.RandomValue.Next(0, musicList[musicPage].Count);
                            if (!alreadyPlayedPath.Contains(musicList[musicPage][r2]))
                            {
                                Set_Music_Index(r2);
                                alreadyPlayedPath.Add(musicList[musicPage][r2]);
                                Music_L_SelectionChanged(new ItemTappedEventArgs(musicList[musicPage], musicList[musicPage][r2], r2));
                                break;
                            }
                        }
                    }
                }
                //���[�v�Đ��ł������_���Đ��ł��Ȃ��ꍇ�͒�~&���
                else
                {
                    Bass.BASS_ChannelStop(stream);
                    Bass.BASS_StreamFree(stream);
                    Music_L.SelectedItem = null;
                    Music_List_Sort();
                    playingMusicNameNow = null;
                }
            }
            bEnded = false;
            //���̃t���[�����Ԃ��v�Z
            if (Environment.TickCount >= nextFrame + period)
            {
                nextFrame += period;
                continue;
            }
            nextFrame += period;
        }
    }

    //�T�E���h�̍Đ��I�������m
    private async void EndSync(int handle, int channel, int data, IntPtr user)
    {
        if (!bEnded)
        {
            await Task.Delay(500);
            bEnded = true;
        }
    }

    //�e�L�X�g�������Ԍo������t�F�[�h�A�E�g
    private async void Message_Feed_Out(string Message)
    {
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
            if (Number >= 200)
                Message_T.Opacity -= 0.025;
            await Task.Delay(1000 / 60);
        }
        bMessageShowing = false;
        Message_T.Text = "";
        Message_T.Opacity = 1;
    }

    //���X�g���̃T�E���h���^�b�v
    private void Music_List_Tapped(object? sender, EventArgs e)
    {
        if (Music_List_LastCell != null)
            Music_List_LastCell.View.BackgroundColor = Color.FromArgb("#00000000");
        if (sender is ViewCell viewCell && viewCell.View != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            Music_List_LastCell = viewCell;
        }
    }

    //�Đ����̃T�E���h�̃C���f�b�N�X���擾
    private int Get_Select_Index()
    {
        if (Music_L.SelectedItem == null)
            return -1;
        Music_Type_List Temp = (Music_Type_List)Music_L.SelectedItem;
        for (int Number = 0; Number < musicList[musicPage].Count; Number++)
            if (musicList[musicPage][Number].Name_Text == Temp.Name_Text)
                return Number;
        return -1;
    }

    //�w�肵���C���f�b�N�X�̃T�E���h�ɕύX
    private void Set_Music_Index(int Index)
    {
        Music_L.ItemsSource = null;
        Music_L.ItemsSource = musicList[musicPage];
        Music_L.SelectedItem = musicList[musicPage][Index];
        OnPropertyChanged("SelectedItem");
        IEnumerable<PropertyInfo> pInfos = Music_L.GetType().GetRuntimeProperties();
        PropertyInfo? templatedItems = pInfos.FirstOrDefault(info => info.Name == "TemplatedItems");
        if (templatedItems != null)
        {
            object? cells = templatedItems.GetValue(Music_L);
            if (cells == null)
                return;
            int Count = 0;
            foreach (ViewCell cell in ((ITemplatedItemsList<Cell>)cells).Cast<ViewCell>())
            {
                cell.View.BackgroundColor = cell.BindingContext != null && Count == Index ? Color.FromArgb("#82bfc8") : Color.FromArgb("#00000000");
                Count++;
            }
        }
    }

    //���X�g�̑I����Ԃ��ύX���ꂽ��T�E���h��ύX
    private void Music_L_SelectionChanged(ItemTappedEventArgs e, bool bClear = false)
    {
        if (e.Item != null && !bNotMusicChange)
        {
            Music_Type_List temp = (Music_Type_List)Music_L.SelectedItem;
            if (!File.Exists(temp.FilePath))
            {
                Message_Feed_Out("�t�@�C�������݂��܂���B���X�g����폜����܂��B");
                Sub_Code.ErrorLogWrite("���̃t�@�C����������܂���ł����B->" + temp.FilePath);
                List_Remove_Index(e.ItemIndex);
                playingMusicNameNow = null;
                return;
            }

            if (playingMusicNameNow == musicList[musicPage][e.ItemIndex])
                return;

            if (bClear)
            {
                alreadyPlayedPath.Clear();
                Set_Music_Index(e.ItemIndex);
                foreach (Music_Type_List Info in musicList[musicPage])
                    Info.IsPlayed = false;
                alreadyPlayedPath.Add(musicList[musicPage][e.ItemIndex]);
            }

            //�Đ����̃T�E���h�����
            Bass.BASS_ChannelStop(stream);
            Bass.BASS_FXReset(streamLPF);
            Bass.BASS_FXReset(streamHPF);
            Bass.BASS_FXReset(streamECHO);
            Bass.BASS_StreamFree(stream);
            Location_S.Value = 0;
            playingMusicNameNow = musicList[musicPage][e.ItemIndex];
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 200);
            int StreamHandle = Bass.BASS_StreamCreateFile(musicList[musicPage][e.ItemIndex].FilePath, 0, 0,
                    BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_LOOP | BASSFlag.BASS_STREAM_PRESCAN);
            stream = BassFx.BASS_FX_TempoCreate(StreamHandle, BASSFlag.BASS_FX_FREESOURCE);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);
            bMusicEnd = new SYNCPROC(EndSync);
            Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, ref musicFrequency);
            _ = Bass.BASS_ChannelSetSync(stream, BASSSync.BASS_SYNC_END | BASSSync.BASS_SYNC_MIXTIME, 0, bMusicEnd, IntPtr.Zero);
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)Volume_S.Value / 100);
            streamLPF = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_BQF, 2);
            streamHPF = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_BQF, 1);
            hpfSetting.fCenter = 1000f;
            if (settingWindow.bLPFEnable)
            {
                lpfSetting.fCenter = 500 + 4000f * (1 - (float)settingWindow.lpfValue / 100.0f);
                Bass.BASS_FXSetParameters(streamLPF, lpfSetting);
            }
            if (settingWindow.bHPFEnable)
            {
                hpfSetting.fCenter = 1000 + 4000f * (float)settingWindow.hpfValue / 100.0f;
                Bass.BASS_FXSetParameters(streamHPF, hpfSetting);
            }
            echoSetting.fWetMix = 0;
            if (settingWindow.bECHOEnable)
            {
                streamECHO = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_ECHO4, 0);
                echoSetting.fDelay = (float)settingWindow.echoDelayValue;
                echoSetting.fDryMix = (float)settingWindow.echoPowerOriginalValue / 100f;
                echoSetting.fWetMix = (float)settingWindow.echoPowerECHOValue / 100f;
                echoSetting.fFeedback = (float)settingWindow.echoLengthValue / 100f;
                Bass.BASS_FXSetParameters(streamECHO, echoSetting);
            }
            if (bSyncPitch_And_Speed)
                Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, musicFrequency * (float)(Pitch_Speed_S.Value / 50));
            else
            {
                Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_PITCH, (float)Pitch_S.Value);
                Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, (float)Speed_S.Value);
            }
            Location_S.Maximum = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream, BASSMode.BASS_POS_BYTES));
            startTime = 0;
            endTime = Location_S.Maximum;
            bPaused = false;
            Bass.BASS_ChannelPlay(stream, true);
        }
        else if (!bAddMode)
        {
            Location_S.Value = 0;
            Location_S.Maximum = 1;
            Location_T.Text = "00:00";
        }
    }

    //�w�肵���C���f�b�N�X�̃T�E���h�����X�g����폜
    private void List_Remove_Index(int Index)
    {
        Music_L.SelectedItem = null;
        musicList[musicPage].RemoveAt(Index);
        Bass.BASS_ChannelStop(stream);
        Bass.BASS_StreamFree(stream);
        Music_List_Sort();
        alreadyPlayedPath.Clear();
        Music_List_Save();
    }

    //�T�E���h�𖼑O���Ƀ\�[�g
    private void Music_List_Sort()
    {
        bNotMusicChange = true;
        string List_Now = "";
        if (Music_L.SelectedItem != null)
            List_Now = ((Music_Type_List)Music_L.SelectedItem).FilePath;
        musicList[musicPage] = [.. musicList[musicPage].OrderBy(x => Path.GetExtension(x.FilePath)).ThenBy(x => x.Name_Text)];
        Music_L.ItemsSource = null;
        Music_L.SelectedItem = null;
        Music_L.ItemsSource = musicList[musicPage];
        if (List_Now != "")
        {
            int Index = -1;
            for (int Number = 0; Number < musicList[musicPage].Count; Number++)
            {
                if (musicList[musicPage][Number].FilePath == List_Now)
                {
                    Index = Number;
                    break;
                }
            }
            if (Index != -1)
                Set_Music_Index(Index);
        }
        bNotMusicChange = false;
    }

    //�T�E���h�̃��X�g���擾
    private void Music_List_Save()
    {
        if (File.Exists(Sub_Code.ExDir + "/Configs/Music_Player_List.conf"))
            File.Delete(Sub_Code.ExDir + "/Configs/Music_Player_List.conf");

        BinaryWriter bw = new(File.OpenWrite(Sub_Code.ExDir + "/Configs/Music_Player_List.conf"));
        byte[] header = Encoding.ASCII.GetBytes(CONFIG_HEADER);
        bw.Write((byte)header.Length);
        bw.Write(header);
        bw.Write(MUSIC_VERSION);
        bw.Write((byte)musicList.Count);
        foreach (List<Music_Type_List> typeList in musicList)
        {
            bw.Write((ushort)typeList.Count);
            foreach (Music_Type_List type in typeList)
            {
                byte[] filePathBytes = Encoding.UTF8.GetBytes(type.FilePath);
                bw.Write((byte)filePathBytes.Length);
                bw.Write(filePathBytes);
                bw.Write(type.FileName != null);
                if (type.FileName != null)
                {
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(type.FileName);
                    bw.Write((byte)fileNameBytes.Length);
                    bw.Write(fileNameBytes);
                }
            }
        }
        bw.Close();
    }

    //�ݒ��ۑ�
    private void Configs_Save()
    {
        try
        {
            int page = 0;
            if (musicList[musicPage].Count == 0)
            {
                for (int i = 0; i < musicList.Count; i++)
                    if (musicList[i].Count > 0)
                        page = i;
            }
            else
                page = musicPage;

            if (File.Exists(Sub_Code.ExDir + "/Configs/Music_Player.conf"))
                File.Delete(Sub_Code.ExDir + "/Configs/Music_Player.conf");

            BinaryWriter bw = new(File.OpenWrite(Sub_Code.ExDir + "/Configs/Music_Player.conf"));
            byte[] header = Encoding.ASCII.GetBytes(CONFIG_HEADER);
            bw.Write((byte)header.Length);
            bw.Write(header);
            bw.Write(CONFIG_VERSION);
            bw.Write(Loop_C.IsChecked);
            bw.Write(Random_C.IsChecked);
            bw.Write(Volume_S.Value);
            bw.Write(Mode_C.IsChecked);
            bw.Write((byte)page);
            bw.Close();
        }
        catch (Exception e)
        {
            Sub_Code.ErrorLogWrite(e.Message);
        }
    }

    //�ݒ��ǂݍ���
    private void Configs_Load()
    {
        if (File.Exists(Sub_Code.ExDir + "/Configs/Music_Player_List.conf") && Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_STOPPED)
        {
            BinaryReader? br = null;
            try
            { 
                br = new(File.OpenRead(Sub_Code.ExDir + "/Configs/Music_Player_List.conf"));
                musicList.Clear();
                _ = br.ReadBytes(br.ReadByte());
                _ = br.ReadByte();
                byte musicListCount = br.ReadByte();
                for (int i = 0; i < musicListCount; i++)
                {
                    musicList.Add([]);
                    ushort typeCount = br.ReadUInt16();
                    for (int j = 0; j < typeCount; j++)
                    {
                        string filePath = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                        musicList[^1].Add(filePath);
                        bool bExistName = br.ReadBoolean();
                        if (bExistName)
                            musicList[^1][^1].FileName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    }
                }
                br.Close();
                alreadyPlayedPath.Clear();
            }
            catch (Exception e1)
            {
                br?.Close();
                musicList.Clear();
                for (int i = 0; i < 9; i ++)
                    musicList.Add([]);
                Music_L.SelectedItem = null;
                Music_L.ItemsSource = null;
                Sub_Code.ErrorLogWrite(e1.Message);
            }
        }
        if (File.Exists(Sub_Code.ExDir + "/Configs/Music_Player.conf"))
        {
            try
            {
                BinaryReader br = new(File.OpenRead(Sub_Code.ExDir + "/Configs/Music_Player.conf"));

                _ = br.ReadBytes(br.ReadByte());
                _ = br.ReadByte();

                Loop_C.IsChecked = br.ReadBoolean();
                Random_C.IsChecked = br.ReadBoolean();
                Volume_S.Value = br.ReadDouble();
                Mode_C.IsChecked = br.ReadBoolean();

                Music_List_Change(br.ReadByte());

                br.Close();
            }
            catch (Exception e1)
            {
                File.Delete(Sub_Code.ExDir + "/Configs/Music_Player.conf");
                Loop_C.IsChecked = false;
                Random_C.IsChecked = false;
                Sub_Code.ErrorLogWrite(e1.Message);
                Message_Feed_Out("�ݒ�����[�h���ɃG���[���������܂����B\n" + e1.Message);
            }
        }
        else
        {
            Loop_C.IsChecked = true;
            Mode_C.IsChecked = true;
        }
    }

    //�Đ��ʒu��ύX
    //���� : double �Đ��ʒu(�b), bool �V�[�N�o�[��������Ȃ��T�E���h�̈ʒu���ύX���邩
    private void Music_Pos_Change(double Pos, bool IsBassPosChange)
    {
        if (IsBusy)
            return;
        if (IsBassPosChange)
            Bass.BASS_ChannelSetPosition(stream, Pos);
        TimeSpan Time = TimeSpan.FromSeconds(Pos);
        string Minutes = Time.Minutes.ToString();
        string Seconds = Time.Seconds.ToString();
        if (Time.Minutes < 10)
            Minutes = "0" + Time.Minutes;
        if (Time.Seconds < 10)
            Seconds = "0" + Time.Seconds;
        Location_T.Text = Minutes + ":" + Seconds;
    }

    //�V�[�N�o�[�̈ʒu�����݂̍Đ����ԂƓ���
    private void Set_Position_Slider()
    {
        long position = Bass.BASS_ChannelGetPosition(stream);
        Location_S.Value = Bass.BASS_ChannelBytes2Seconds(stream, position);
    }

    //�����ݒ��
    private void Setting_B_Click(object? sender, EventArgs e)
    {
        if (bPageOpen)
            return;
        bPageOpen = true;
        bIgnorePageChange = true;
        _ = Navigation.PushModalAsync(settingWindow);
    }

    //Youtube����T�E���h���擾
    private void Youtube_B_Clicked(object? sender, EventArgs e)
    {
        if (bPageOpen)
            return;
        bPageOpen = true;
        bIgnorePageChange = true;
        if (youtubeDownloaderWindow == null)
        {
            youtubeDownloaderWindow = new();
            youtubeDownloaderWindow.Init(this);
        }

        _ = Navigation.PushAsync(youtubeDownloaderWindow);
    }

    private void Loop_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
            Random_C.IsChecked = !e.Value;
        Configs_Save();
    }

    private void Random_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
            Loop_C.IsChecked = !e.Value;
        Configs_Save();
    }

    private void Mode_C_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        bSyncPitch_And_Speed = e.Value;
        if (e.Value)
        {
            Pitch_T.IsVisible = false;
            Pitch_S.IsVisible = false;
            Speed_T.IsVisible = false;
            Speed_S.IsVisible = false;
            Pitch_Speed_T.IsVisible = true;
            Pitch_Speed_S.IsVisible = true;
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, musicFrequency * (float)(Pitch_Speed_S.Value / 50));
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_PITCH, 0f);
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, 0f);
        }
        else
        {
            Pitch_T.IsVisible = true;
            Pitch_S.IsVisible = true;
            Speed_T.IsVisible = true;
            Speed_S.IsVisible = true;
            Pitch_Speed_T.IsVisible = false;
            Pitch_Speed_S.IsVisible = false;
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, musicFrequency);
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_PITCH, (float)Pitch_S.Value);
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, (float)Speed_S.Value);
        }
        Configs_Save();
    }

    //�T�E���h��ǉ�
    private async void Music_Add_B_Click(object? sender, EventArgs e)
    {
        if (bPageOpen)
            return;

#if ANDROID
        if (!AndroidClass.CheckExternalStoragePermission())
        {
            Message_Feed_Out("�A�N�Z�X�����s���Ă��������B");
            return;
        }
#endif

        if (Sub_Code.IsUseSelectPage)
        {
            bPageOpen = true;
            bIgnorePageChange = true;
            string Ex = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4|.webm";
            Sub_Code.Select_Files_Window.Window_Show("Music_Player", "", Ex);
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);
        }
        else
        {
            FilePickerFileType customFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, value }
                });
            PickOptions options = new()
            {
                FileTypes = customFileType,
                PickerTitle = "�T�E���h�t�@�C����I�����Ă��������B"
            };
            IEnumerable<FileResult> result = await FilePicker.PickMultipleAsync(options);
            if (result != null)
            {
                bAddMode = true;
                bool bExist = false;
                foreach (FileResult file_result in result)
                {
                    bool bCanAdd = true;
                    foreach (Music_Type_List musicType in SelectedMusicList)
                    {
                        if (musicType.FilePath == file_result.FullPath)
                        {
                            bExist = true;
                            bCanAdd = false;
                            break;
                        }
                    }
                    if (bCanAdd)
                        SelectedMusicList.Add(file_result.FileName);
                }
                if (bExist)
                    Message_Feed_Out("���ɒǉ�����Ă���t�@�C�������݂��܂��B");
                Music_List_Sort();
                Music_List_Save();
                bAddMode = false;
            }
        }
    }

    //�I�����Ă���T�E���h�����X�g����폜
    private async void Music_Delete_B_Click(object? sender, EventArgs e)
    {
        if (Music_L.SelectedItem == null)
            return;
        bool Result = await DisplayAlert("�I���������ڂ��폜���܂���?", null, "�͂�", "������");
        if (Result)
        {
            Music_Type_List temp = (Music_Type_List)Music_L.SelectedItem;
            try
            {
                if (temp.FilePath.Contains(Sub_Code.ExDir))
                    File.Delete(temp.FilePath);
            }
            catch (Exception e1)
            {
                Sub_Code.ErrorLogWrite(e1.Message);
            }
            musicList[musicPage].Remove(temp);
            Music_L.SelectedItem = null;
            Bass.BASS_ChannelStop(stream);
            Bass.BASS_StreamFree(stream);
            alreadyPlayedPath.Clear();
            foreach (Music_Type_List Info in musicList[musicPage])
                Info.IsPlayed = false;
            Music_List_Sort();
            Music_List_Save();
        }
    }

    private void Music_Minus_B_Click(object? sender, EventArgs e)
    {
        if (Location_S.Value <= 5)
            Location_S.Value = 0;
        else
            Location_S.Value -= 5;
        Music_Pos_Change(Location_S.Value, true);
    }
    private void Music_Plus_B_Click(object? sender, EventArgs e)
    {
        if (Location_S.Value + 5 >= Location_S.Maximum)
            Location_S.Value = Location_S.Maximum;
        else
            Location_S.Value += 5;
        Music_Pos_Change(Location_S.Value, true);
    }

    private void Music_Play_B_Click(object? sender, EventArgs e)
    {
        Play_Volume_Animation();
    }
    private void Music_Pause_B_Click(object? sender, EventArgs e)
    {
        Pause_Volume_Animation(false);
    }

    private void Music_Page_Back_B_Click(object? sender, EventArgs e)
    {
        if (musicPage > 0)
            Music_List_Change(musicPage - 1);
    }
    private void Music_Page_Next_B_Click(object? sender, EventArgs e)
    {
        if (musicPage < 8)
            Music_List_Change(musicPage + 1);
    }

    //�Đ����x�ƃs�b�`��������
    private void Reset_B_Clicked(object? sender, EventArgs e)
    {
        Speed_S.Value = 0;
        Pitch_S.Value = 0;
        Pitch_Speed_S.Value = 50;
    }

    //�t�F�[�h�C�����Ȃ���Đ��J�n
    async void Play_Volume_Animation(float Feed_Time = 30f)
    {
        bPaused = false;
        if (playingMusicNameNow == null)
            return;
        Bass.BASS_ChannelPlay(stream, false);
        float Volume_Now = 1f;
        Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref Volume_Now);
        float Volume_Plus = (float)(Volume_S.Value / 100) / Feed_Time;
        while (Volume_Now < (float)(Volume_S.Value / 100) && !bPaused)
        {
            Volume_Now += Volume_Plus;
            if (Volume_Now > 1f)
                Volume_Now = 1f;
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, Volume_Now);
            await Task.Delay(1000 / 60);
        }
    }

    //�t�F�[�h�A�E�g���Ȃ����~
    public async void Pause_Volume_Animation(bool IsStop, float Feed_Time = 30f)
    {
        bPaused = true;
        if (playingMusicNameNow == null)
            return;
        float Volume_Now = 1f;
        Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref Volume_Now);
        float Volume_Minus = Volume_Now / Feed_Time;
        while (Volume_Now > 0f && bPaused)
        {
            Volume_Now -= Volume_Minus;
            if (Volume_Now < 0f)
                Volume_Now = 0f;
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, Volume_Now);
            await Task.Delay(1000 / 60);
        }
        if (Volume_Now <= 0f)
        {
            if (IsStop)
            {
                Bass.BASS_ChannelStop(stream);
                Bass.BASS_StreamFree(stream);
                Location_S.Value = 0;
                Location_S.Maximum = 1;
                Location_T.Text = "00:00";
                Music_L.SelectedItem = null;
            }
            else if (bPaused)
                Bass.BASS_ChannelPause(stream);
        }
    }

    private void Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Volume_T.Text = "����:" + (int)e.NewValue;
        Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)Volume_S.Value / 100);
    }

    private void Speed_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Speed_T.Text = "���x:" + (Math.Floor(Speed_S.Value * 10) / 10).ToString();
        Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, (float)Speed_S.Value);
    }

    private void Pitch_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_T.Text = "����:" + (Math.Floor(Pitch_S.Value * 10) / 10).ToString();
        Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_PITCH, (float)Pitch_S.Value);
    }
    private void Pitch_Speed_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Pitch_Speed_T.Text = "�����Ƒ��x:" + (int)Pitch_Speed_S.Value;
        Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO_FREQ, musicFrequency * (float)(Pitch_Speed_S.Value / 50));
        Bass.BASS_ChannelUpdate(stream, 50);
    }

    private void Location_S_DragStarted(object? sender, EventArgs e)
    {
        bLocationChanging = true;
        if (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
        {
            bPlayingMouseDown = true;
            Pause_Volume_Animation(false, 10);
        }
    }
    private void Location_S_DragCompleted(object? sender, EventArgs e)
    {
        bLocationChanging = false;
        Bass.BASS_ChannelSetPosition(stream, Location_S.Value);
        if (bPlayingMouseDown)
        {
            bPaused = false;
            Play_Volume_Animation(10);
            bPlayingMouseDown = false;
        }
    }
    private void Location_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (bLocationChanging)
            Music_Pos_Change(Location_S.Value, false);
    }

    //�y�[�W��ύX
    private void Music_List_Change(int pageIndex)
    {
        if (musicPage == pageIndex)
            return;
        alreadyPlayedPath.Clear();
        musicPage = pageIndex;
        PageText.Text = "���y���X�g:" + (musicPage + 1);
        Music_L.SelectedItem = null;
        Pause_Volume_Animation(true, 10f);
        Music_List_Sort();
        Configs_Save();
        playingMusicNameNow = null;
    }

    //�y�[�W���̃��X�g�����ׂč폜
    private async void Clear_B_Clicked(object? sender, EventArgs e)
    {
        if (musicList[musicPage].Count > 0)
        {
            bool IsOK = await DisplayAlert("�y�[�W���̍��ڂ��폜���܂���?", null, "�͂�", "������");
            if (IsOK)
            {
                musicList[musicPage].Clear();
                Music_L.ItemsSource = null;
                Music_L.SelectedItem = null;
                Pause_Volume_Animation(true, 10f);
                playingMusicNameNow = null;
                Music_List_Save();
            }
        }
    }

    //�I���W�i���̃t�@�C���I����ʂ���I�����ꂽ�t�@�C����ǉ� (MainPage.cs����Ă΂��)
    public void Selected_Files(List<string> files)
    {
        bAddMode = true;
        bool bExist = false;
        int count = 0;
        foreach (string file_result in files)
        {
            bool bCanAdd = true;
            foreach (Music_Type_List musicType in SelectedMusicList)
            {
                if (musicType.FilePath == file_result)
                {
                    bExist = true;
                    bCanAdd = false;
                    break;
                }
            }
            if (bCanAdd)
            {
                SelectedMusicList.Add(file_result);
                count++;
            }
        }
        if (bExist)
            Message_Feed_Out("���ɒǉ�����Ă���t�@�C�������݂��܂��B");
        else
            Message_Feed_Out(count + "�̃t�@�C����ǉ����܂����B");
        Music_List_Sort();
        Music_List_Save();
        bAddMode = false;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bShowing = true;
        if (!bPageOpen)
            Loop();
        bPageOpen = false;
        bIgnorePageChange = false;
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        if (!bIgnorePageChange)
        {
            bShowing = false;
            Pause_Volume_Animation(true);
            alreadyPlayedPath.Clear();
            playingMusicNameNow = null;
        }
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        Music_List_Sort();
    }

    private void ContentPage_SizeChanged(object sender, EventArgs e)
    {
        Sub_Code.SetListViewHeight(Music_Border, Height);
    }
}
