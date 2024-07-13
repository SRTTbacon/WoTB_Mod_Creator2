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
    public string ModTypeName = modTypeName;                    //�^�C�v��
    public uint ModTypeID = WwiseHash.HashString(modTypeName);  //�^�C�vID
    public uint ContainerID = containerID;                      //�T�E���h�̔z�u�ꏊ

    public string NameText => ModTypeName + " | " + SoundCount + "��";
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

        //�X���C�_�[�ݒ�
        All_Volume_S.ValueChanged += All_Volume_S_ValueChanged;
        PlayTime_S.DragStarted += PlayTime_S_DragStarted; ;
        PlayTime_S.DragCompleted += PlayTime_S_DragCompleted;
        PlayTime_S.ValueChanged += PlayTime_S_ValueChanged;

        //�{�^���ݒ�
        Pause_B.Clicked += Pause_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Minus_B.Clicked += Minus_B_Clicked;
        Plus_B.Clicked += Plus_B_Clicked;

        Mod_Selection_Picker.SelectedIndexChanged += Mod_Selection_Picker_SelectedIndexChanged;

        modPages.Add(new("�퓬�J�n�O���[�hBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("���[�h1:America_lakville", 205170598));
        modPages[^1].Types.Add(new("���[�h2:America_overlord", 148841988));
        modPages[^1].Types.Add(new("���[�h3:Chinese", 1067185674));
        modPages[^1].Types.Add(new("���[�h4:Desert_airfield", 99202684));
        modPages[^1].Types.Add(new("���[�h5:Desert_sand_river", 493356780));
        modPages[^1].Types.Add(new("���[�h6:Europe_himmelsdorf", 277287194));
        modPages[^1].Types.Add(new("���[�h7:Europe_mannerheim", 321403539));
        modPages[^1].Types.Add(new("���[�h8:Europe_ruinberg", 603412881));
        modPages[^1].Types.Add(new("���[�h9:Japan", 256533957));
        modPages[^1].Types.Add(new("���[�h10:Russian_malinovka", 520751345));
        modPages[^1].Types.Add(new("���[�h11:Russian_prokhorovka", 307041675));
        modPages.Add(new("���U���gBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("���U���g:����-BGM", 960016609));
        modPages[^1].Types.Add(new("���U���g:����-����", 737229060));
        modPages[^1].Types.Add(new("���U���g:��������-BGM", 404033224));
        modPages[^1].Types.Add(new("���U���g:��������-����", 480862388));
        modPages[^1].Types.Add(new("���U���g:��������-BGM", 797792182));
        modPages[^1].Types.Add(new("���U���g:��������-����", 761638380));
        modPages.Add(new("�퓬���̗D��BGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("�D��-����", 434309394));
        modPages[^1].Types.Add(new("�D��-�G", 868083406));
        modPages.Add(new("�K���[�WSE", "WoTB_UI_Button_Sound"));
        modPages[^1].Types.Add(new("���p-SE", 432722439));
        modPages[^1].Types.Add(new("���p-����", 26462958));
        modPages[^1].Types.Add(new("�`�F�b�N�{�b�N�X-SE", 278676259));
        modPages[^1].Types.Add(new("�`�F�b�N�{�b�N�X-����", 1015843643));
        modPages[^1].Types.Add(new("������M-SE", 123366428));
        modPages[^1].Types.Add(new("������M-����", 1034987615));
        modPages[^1].Types.Add(new("���W���[���̐؂�ւ�-SE", 1001742020));
        modPages[^1].Types.Add(new("���W���[���̐؂�ւ�-����", 537387720));
        modPages[^1].Types.Add(new("�퓬�J�n�{�^��-SE", 251988040));
        modPages[^1].Types.Add(new("�퓬�J�n�{�^��-����", 56850118));
        modPages[^1].Types.Add(new("�j���[�X-SE", 530790297));
        modPages[^1].Types.Add(new("�j���[�X-����", 1036212148));
        modPages[^1].Types.Add(new("�ԗ��[��-SE", 660827574));
        modPages[^1].Types.Add(new("�ԗ��[��-����", 192152217));
        modPages[^1].Types.Add(new("�����w��-SE", 409835290));
        modPages[^1].Types.Add(new("�����w��-����", 282116325));
        modPages.Add(new("�C����", "WoTB_Gun_Sound_New"));

        foreach (OtherModPage type in modPages)
            Mod_Selection_Picker.Items.Add(type.ModPageName);
    }

    //���[�v (��ɃV�[�N�o�[�p)
    private async void Loop()
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

    //�y�[�W�ύX
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

        //�Đ����̃T�E���h���~
        await Pause_Volume_Animation(true, 10);
        Bass.BASS_StreamFree(streamHandle);

        if (!File.Exists(sound.FilePath))
        {
            Message_Feed_Out("�t�@�C�������݂��܂���B�폜���ꂽ���A�ړ�����Ă���\��������܂��B");
            return;
        }

        //�o�b�t�@�[�T�C�Y��500�ɐݒ�
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);

        //�T�E���h���G���W���ɓǂݍ���
        int baseHandle = 0;

        if (Path.GetExtension(sound.FilePath) == ".flac")
            baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        else
            baseHandle = Bass.BASS_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);

        //FX��K���ł���`��
        streamHandle = BassFx.BASS_FX_TempoCreate(baseHandle, BASSFlag.BASS_FX_FREESOURCE);
        //�G�t�F�N�g��K��
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);

        //�T�E���h�̒������擾���A�V�[�N�o�[�ɔ��f
        PlayTime_S.Maximum = Bass.BASS_ChannelBytes2Seconds(streamHandle, Bass.BASS_ChannelGetLength(streamHandle, BASSMode.BASS_POS_BYTES));

        //�I�����m
        soundEndFunc = new SYNCPROC(EndSync);
        _ = Bass.BASS_ChannelSetSync(streamHandle, BASSSync.BASS_SYNC_END | BASSSync.BASS_SYNC_MIXTIME, 0, soundEndFunc, IntPtr.Zero);

        bPaused = true;

        //�T�E���h�̒�����\��
        maxTime = Sub_Code.Get_Time_String(PlayTime_S.Maximum);
        PlayTime_T.Text = "00:00 / " + maxTime;
    }

    //�S�̂̉��ʂ�ύX
    private void All_Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, (float)All_Volume_S.Value / 100);
        All_Volume_T.Text = "�S�̉���:" + (int)e.NewValue;
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
