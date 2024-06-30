using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;
using System.Text;
using System.Runtime.InteropServices;

namespace WoTB_Mod_Creator2.All_Page;

public class SE_Sound(string filePath, uint shortID, bool bAndroidResource = false, bool bDefaultSound = false)
{
    //�t�@�C���p�X
    public string FilePath = filePath;
    public string SoundName => FilePath == null ? "" : Path.GetFileName(FilePath);

    //Actor Audio Mixer���̂ǂ̃R���e�i�ɔz�u���邩
    public uint ShortID = shortID;

    //���ߍ��݂̃T�E���h���ǂ��� (�f�t�H���g�̏ꍇ���ߍ��݃��\�[�X�̒�����f�[�^���Q�Ƃ��邽�ߍĐ������鎞�̏������قȂ�)
    public bool IsAndroidResource = bAndroidResource;
    //WoTB�̃f�t�H���g�̃T�E���h���ǂ��� (SE_Type��IsEnable��false�̎��ɍĐ������)
    public bool IsDefaultSound = bDefaultSound;
}

public class SE_Type(string typeName, uint defaultShortID, double gain)
{
    //�T�E���h�z��
    public List<SE_Sound> Sounds = [];
    //���X�g�ɕ\������T�E���h�z��
    public List<SE_Sound> SoundList = [];

    //���ʖ�(���X�g�ɕ\������閼�O)
    public string TypeName => typeName + " - " + SoundList.Count + "��";
    public string ConstTypeName => typeName;

    //����ID
    public uint TypeID = WwiseHash.HashString(typeName);
    //�f�t�H���g�̔z�u�ꏊ
    public uint DefaultShortID = defaultShortID;

    //���ʂ̑��� (�P��:db)
    public double Gain = gain;

    //�L�����ǂ���
    public bool IsEnable = true;
    //�I���ς݂��ǂ���
    public bool IsSelected = false;

    //�I�����ɔw�i�F��ύX����
    public Color BackColor => IsSelected ? Color.FromArgb("#82bfc8") : Colors.Transparent;
    //�������ɕ����̐F�𔖂�����
    public Color NameColor => !IsEnable ? Color.FromArgb("#BFFF2C8C") : Colors.Aqua;

    //�z�u����R���e�i���f�t�H���g�̃R���e�i�ɂ���ꍇshortID��0�ɂ���
    public bool AddSound(string filePath, uint shortID = 0, bool bAndroidResource = false, bool bDefaultSound = false)
    {
        if (Sounds.Count >= 255)
            return false;
        foreach (SE_Sound sound in Sounds)
            if (sound.FilePath == filePath)
                return false;
        if (shortID == 0)
            shortID = DefaultShortID;
        Sounds.Add(new(filePath, shortID, bAndroidResource, bDefaultSound));
        if (!bDefaultSound)
            SoundList.Add(Sounds[^1]);
        return true;
    }
    //SE���폜
    public void DeleteSound(SE_Sound seSound)
    {
        SoundList.Remove(seSound);
        Sounds.Remove(seSound);
    }
    //�T�E���h�������_���Ŏ擾
    public SE_Sound? GetRandomSESound()
    {
        if (SoundList.Count <= 0)
            return null;
        return SoundList[Sub_Code.RandomValue.Next(0, SoundList.Count)];
    }
}

public class SE_Preset(string presetName)
{
    //SE�̎�� (�ђʎ����j���Ȃ�)
    public List<SE_Type> Types = [];

    //�v���Z�b�g��
    public string PresetName = presetName;

    //SE������SE_Type���擾
    public SE_Type? GetSEType(string typeName)
    {
        uint nameID = WwiseHash.HashString(typeName);
        foreach (SE_Type seType in Types)
            if (seType.TypeID == nameID)
                return seType;
        return null;
    }

    public SE_Preset Clone()
    {
        SE_Preset clone = (SE_Preset)MemberwiseClone();
        clone.Types = [];
        foreach (SE_Type type in Types)
        {
            SE_Type newType = new(type.ConstTypeName, type.DefaultShortID, type.Gain)
            {
                TypeID = type.TypeID,
                IsEnable = type.IsEnable
            };
            foreach (SE_Sound sound in type.Sounds)
                newType.AddSound(sound.FilePath, sound.ShortID, sound.IsAndroidResource, sound.IsDefaultSound);
            clone.Types.Add(newType);
        }
        return clone;
    }
}

public partial class SE_Setting : ContentPage
{
    const string HEADER = "WSSFormat";
    const byte VERSION = 0x00;

    //���ݑI������Ă���v���Z�b�g
    public SE_Preset NowPreset;

    //�f�t�H���g�̃v���Z�b�g (������Ԃł�NowPreset�ɂ��ꂪ�������)
    readonly SE_Preset defaultPreset;

    readonly List<SE_Preset> presets = [];

    readonly string presetPath = Sub_Code.ExDir + "/Save/SE_Presets.wss";

    //���X�g��I�������Ƃ��̐F�ύX�p
    ViewCell? lastSoundCell = null;

    GCHandle soundIntPtr = new();

    //�Đ��p�n���h��
    int streamHandle = 0;
    int nowPresetIndex = -1;

    bool bOtherPageOpened = false;
    bool bMessageShowing = false;

    public SE_Setting()
	{
		InitializeComponent();

        //�f�t�H���g�v���Z�b�g���쐬
        defaultPreset = new("�W��");
        InitDefaultPreset();
        NowPreset = defaultPreset.Clone();

        Preset_Load();

        UpdateList();

        //�{�^������
        All_Disable_B.Clicked += All_Disable_B_Clicked;
        All_Enable_B.Clicked += All_Enable_B_Clicked;
        Disable_B.Clicked += Disable_B_Clicked;
        Enable_B.Clicked += Enable_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Pause_B.Clicked += Pause_B_Clicked;
        Add_Sound_B.Clicked += Add_Sound_B_Clicked;
        Delete_Sound_B.Clicked += Delete_Sound_B_Clicked;
        Preset_Load_B.Clicked += Preset_Load_B_Clicked;
        Preset_Save_B.Clicked += Preset_Save_B_Clicked;
    }

    private void InitDefaultPreset()
    {
        //�f�t�H���g��SE���Z�b�g
        //Types[^1]��Types[defaultPreset.Types.Count - 1]�Ɠ����Ӗ�
        //�t�@�C����Resources/Raw���ɓ����Ă��܂�
        defaultPreset.Types.Clear();
        defaultPreset.Types.Add(new("���Ԑ؂�&��̃|�C���gMax", 206640353, 0));
        defaultPreset.Types[^1].AddSound("Capture_End_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Capture_End_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Capture_Finish_SE.mp3", 0, true, true);
        defaultPreset.Types.Add(new("�N�C�b�N�R�}���h", 405861605, 0));
        defaultPreset.Types[^1].AddSound("QuickCommand_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("Command_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("quick_commands_positive.mp3", 851389356, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_negative.mp3", 747137713, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_help_me.mp3", 990119123, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_reloading.mp3", 560124813, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_attack.mp3", 1039956691, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_attack_target.mp3", 1041861596, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_capture_base.mp3", 284419845, true, true);
        defaultPreset.Types[^1].AddSound("quick_commands_defend_base.mp3", 93467631, true, true);
        defaultPreset.Types.Add(new("�e��ɔj��", 370075103, -2));
        defaultPreset.Types[^1].AddSound("Ammo_Damaged_01.mp3", 0, true);
        defaultPreset.Types.Add(new("���ԗ���j", 667880140, 0));
        defaultPreset.Types[^1].AddSound("Destroy_01.mp3", 0, true);
        defaultPreset.Types.Add(new("�G���j", 582349497, -2));
        defaultPreset.Types[^1].AddSound("EnemyDestory_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("EnemyDestory_02.wav", 0, true);
        defaultPreset.Types.Add(new("�ђ�", 862763776, -6));
        defaultPreset.Types[^1].AddSound("Piercing_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Piercing_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Piercing_03.mp3", 0, true);
        defaultPreset.Types.Add(new("�G���W���[���j��", 876186554, -1));
        defaultPreset.Types[^1].AddSound("Piercing_Special_01.mp3", 0, true);
        defaultPreset.Types.Add(new("�G����", 52837378, 0));
        defaultPreset.Types.Add(new("�����@�j��", 948692451, 0));
        defaultPreset.Types[^1].AddSound("RadioDamaged_01.mp3", 0, true);
        defaultPreset.Types.Add(new("�R���^���N�j��", 602706971, 0));
        defaultPreset.Types[^1].AddSound("FuelTankDamaged_01.mp3", 0, true);
        defaultPreset.Types.Add(new("��ђ�-�����e", 298293840, -1));
        defaultPreset.Types[^1].AddSound("Not_Piercing_01.mp3", 0, true);
        defaultPreset.Types.Add(new("��ђ�-���e", 568110765, -1));
        defaultPreset.Types[^1].AddSound("Not_Piercing_01.mp3", 0, true);
        defaultPreset.Types.Add(new("���U����", 769579073, 0));
        defaultPreset.Types[^1].AddSound("howitzer_load_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("howitzer_load_02.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("howitzer_load_03.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("howitzer_load_04.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("Reload_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Reload_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Reload_03.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Reload_04.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Reload_05.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Reload_06.mp3", 0, true);
        defaultPreset.Types.Add(new("��Z��", 917399664, 0));
        defaultPreset.Types[^1].AddSound("Lamp_SE_01.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Sixth_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Sixth_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Sixth_03.mp3", 0, true);
        defaultPreset.Types.Add(new("�G����", 479275647, 0));
        defaultPreset.Types[^1].AddSound("EnemySpoted_01.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Spot_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Spot_02.wav", 0, true);
        defaultPreset.Types.Add(new("�퓬�J�n�O�^�C�}�[", 816581364, 0));
        defaultPreset.Types[^1].AddSound("Timer_SE.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Timer_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("Timer_02.wav", 0, true);
        defaultPreset.Types.Add(new("���b�N�I��", 391999685, 0));
        defaultPreset.Types[^1].AddSound("target_on_SE_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("Lock_01.mp3", 0, true);
        defaultPreset.Types.Add(new("�A�����b�N", 166694669, 0));
        defaultPreset.Types[^1].AddSound("target_off_SE_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("Unlock_01.mp3", 0, true);
        defaultPreset.Types.Add(new("�m�C�Y��", 921545948, 0));
        defaultPreset.Types[^1].AddSound("Noise_01.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_02.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_03.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_04.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_05.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_06.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_07.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_08.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_09.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_10.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Noise_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_03.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_04.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_05.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_06.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_07.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_08.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_09.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Noise_10.mp3", 0, true);
        defaultPreset.Types.Add(new("���������", 554749525, 0));
        defaultPreset.Types.Add(new("���W���[���j��", 7268757, 0));
        defaultPreset.Types[^1].AddSound("ModuleBreak_01.wav", 0, true);
        defaultPreset.Types.Add(new("���W���[����j", 138494260, 0));
        defaultPreset.Types[^1].AddSound("ModuleBreak_01.wav", 0, true);
        defaultPreset.Types.Add(new("���W���[������", 554749525, 0));
        defaultPreset.Types.Add(new("�퓬�J�n", 267487625, 0));
        defaultPreset.Types[^1].AddSound("BattleStart_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("BattleStart_02.wav", 0, true);
        defaultPreset.Types[^1].AddSound("BattleStart_03.wav", 0, true);
        defaultPreset.Types[^1].AddSound("BattleStart_04.wav", 0, true);
        defaultPreset.Types.Add(new("�}�b�v�N���b�N", 951031474, 0));
        defaultPreset.Types[^1].AddSound("Map_Click_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("MapClicked_01.wav", 0, true);
        defaultPreset.Types.Add(new("�ړ���", 394210856, 0));
        defaultPreset.Types[^1].AddSound("Moving_01.wav", 0, true);
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

    //���X�g�̏�Ԃ��X�V
    private void UpdateList()
    {
        SE_Type? selectedType = SE_Type_L.SelectedItem as SE_Type;

        SE_Type_L.ItemsSource = null;
        SE_Type_L.ItemsSource = NowPreset.Types;
        SE_Sound_L.SelectedItem = null;

        if (selectedType != null)
        {
            SE_Type_L.SelectedItem = selectedType;
        }
    }

    //���ׂĂ�SE��L����
    private void All_Enable_B_Clicked(object? sender, EventArgs e)
    {
        foreach (SE_Type seType in NowPreset.Types)
            seType.IsEnable = true;
        UpdateList();
    }

    //���ׂĂ�SE�𖳌���
    private void All_Disable_B_Clicked(object? sender, EventArgs e)
    {
        foreach (SE_Type seType in NowPreset.Types)
            seType.IsEnable = false;
        UpdateList();
    }

    //�w�肵��SE��L����
    private void Enable_B_Clicked(object? sender, EventArgs e)
    {
        if (SE_Type_L.SelectedItem == null)
            return;

        SE_Type seType = (SE_Type)SE_Type_L.SelectedItem;
        seType.IsEnable = true;

        UpdateList();
    }

    //�w�肵��SE�𖳌���
    private void Disable_B_Clicked(object? sender, EventArgs e)
    {
        if (SE_Type_L.SelectedItem == null)
            return;

        SE_Type seType = (SE_Type)SE_Type_L.SelectedItem;
        seType.IsEnable = false;

        UpdateList();
    }

    private void SE_Sound_L_Tapped(object sender, EventArgs e)
    {
        if (lastSoundCell != null)
            lastSoundCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        if (viewCell.View != null && SE_Sound_L.SelectedItem != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastSoundCell = viewCell;
        }

        if (SE_Sound_L.SelectedItem == null)
            return;
    }

    private void Play_B_Clicked(object? sender, EventArgs e)
    {
        if (SE_Type_L.SelectedItem != null && SE_Sound_L.SelectedItem != null)
        {
            SE_Sound sound = (SE_Sound)SE_Sound_L.SelectedItem;

            if (sound.FilePath == null)
                return;

            Bass.BASS_ChannelStop(streamHandle);
            Bass.BASS_StreamFree(streamHandle);

            if (soundIntPtr.IsAllocated)
                soundIntPtr.Free();

            if (sound.IsAndroidResource)
            {
                byte[] soundBytes = Sub_Code.ReadResourceData(sound.FilePath);
                soundIntPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);
                IntPtr pin = soundIntPtr.AddrOfPinnedObject();
                int cacheHandle = Bass.BASS_StreamCreateFile(pin, 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_LOOP);
                streamHandle = BassFx.BASS_FX_TempoCreate(cacheHandle, BASSFlag.BASS_FX_FREESOURCE);
            }
            else if (!File.Exists(sound.FilePath))
            {
                Message_Feed_Out("�T�E���h�t�@�C����������܂���ł����B");
                Sub_Code.ErrorLogWrite("�T�E���h�t�@�C����������܂���ł����B\n�ꏊ:" + sound.FilePath);
                return;
            }
            else
            {
                int cacheHandle;
                if (Path.GetExtension(sound.FilePath) == ".flac")
                    cacheHandle = BassFlac.BASS_FLAC_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_LOOP);
                else
                    cacheHandle = Bass.BASS_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_STREAM_DECODE);
                streamHandle = BassFx.BASS_FX_TempoCreate(cacheHandle, BASSFlag.BASS_FX_FREESOURCE);
            }
            Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, 1.0f);
            Bass.BASS_ChannelPlay(streamHandle, false);
        }
    }

    private void Pause_B_Clicked(object? sender, EventArgs e)
    {
        Bass.BASS_ChannelStop(streamHandle);
        Bass.BASS_StreamFree(streamHandle);
    }

    private async void Add_Sound_B_Clicked(object? sender, EventArgs e)
    {
        //�G���[���
        if (SE_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("SE���I������Ă��܂���B");
            return;
        }

        if (bOtherPageOpened)
            return;

        //�t�@�C���{���̌����������Ă��邩�A�z�[����ʂŃI���W�i���̑I����ʂ�L���ɂ����ꍇ�͂��̑I����ʂŃt�@�C����I��
        if (Sub_Code.IsUseSelectPage)
        {
#if ANDROID
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("�A�N�Z�X�����s���Ă��������B");
                return;
            }
#endif
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //�Ή����Ă���g���q
            Sub_Code.Select_Files_Window.Window_Show("SE_Setting", "", extension);      //�I����ʂ�������
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);              //�I����ʂ��J��
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
                PickerTitle = "�T�E���h�t�@�C����I�����Ă��������B"
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
        if (SE_Type_L.SelectedItem == null || SE_Sound_L.SelectedItem == null)
        {
            Message_Feed_Out("�T�E���h���I������Ă��܂���B");
            return;
        }

        SE_Sound seSound = (SE_Sound)SE_Sound_L.SelectedItem;

        bool result_01 = await DisplayAlert(seSound.SoundName + "���폜���܂���?", null, "�͂�", "������");
        if (!result_01)
            return;

        SE_Type seType = (SE_Type)SE_Type_L.SelectedItem;

        seType.DeleteSound(seSound);

        UpdateList();

        SE_Sound_L.ItemsSource = null;

        if (seType.SoundList.Count > 0)
            SE_Sound_L.ItemsSource = seType.SoundList;
    }

    //�T�E���h��ǉ�

    public void Add_Sound(List<string> files)
    {
        SE_Type seType = (SE_Type)SE_Type_L.SelectedItem;

        int alreadyVoiceCount = 0;
        int addedVoiceCount = 0;

        foreach (string filePath in files)
        {
            if (seType.AddSound(filePath))
                addedVoiceCount++;
            else
                alreadyVoiceCount++;
        }

        //���X�gUI���X�V
        UpdateList();
        SE_Sound_L.ItemsSource = null;
        SE_Sound_L.ItemsSource = seType.SoundList;

        if (alreadyVoiceCount > 0 && addedVoiceCount == 0)
            Message_Feed_Out("���ɒǉ�����Ă��邽��" + alreadyVoiceCount + "�̃t�@�C�����X�L�b�v���܂����B");
        else if (alreadyVoiceCount > 0 && addedVoiceCount > 0)
            Message_Feed_Out("���ɒǉ�����Ă��邽��" + alreadyVoiceCount + "�̃t�@�C�����X�L�b�v���A" + addedVoiceCount + "�̃t�@�C����ǉ����܂����B");
        else
            Message_Feed_Out(addedVoiceCount + "�̃t�@�C�����C�x���g�ɒǉ����܂����B");
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bOtherPageOpened = false;
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        Bass.BASS_ChannelStop(streamHandle);
        Bass.BASS_StreamFree(streamHandle);
    }

    private void Preset_Save()
    {
        try
        {
            //���ɃZ�[�u�t�@�C�������݂�����폜 (�ǋL����Ă��܂��ꍇ�����邩��)
            if (File.Exists(presetPath))
                File.Delete(presetPath);

            //�t�@�C���T�C�Y�����������邽�߃t�H���_���͂܂Ƃ߂�
            List<string> dirs = [];
            foreach (SE_Preset preset in presets)
            {
                foreach (SE_Type type in preset.Types)
                {
                    foreach (SE_Sound sound in type.Sounds)
                    {
                        string? dir = Path.GetDirectoryName(sound.FilePath);
                        if (dir != null && !dirs.Contains(dir))
                            dirs.Add(dir);
                    }
                }
            }
            BinaryWriter bw = new(File.OpenWrite(presetPath));
            byte[] header = Encoding.ASCII.GetBytes(HEADER);
            bw.Write((byte)header.Length);                      //�w�b�_�[�̃T�C�Y
            bw.Write(header);                                   //�w�b�_�[
            bw.Write(VERSION);                                  //�o�[�W����(1�o�C�g0�`255)
            bw.Write((byte)nowPresetIndex);                     //�I������Ă���v���Z�b�g�̃C���f�b�N�X(�W����-1)
            bw.Write((byte)dirs.Count);                         //�t�H���_�̐�
            foreach (string dir in dirs)
            {
                //�t�H���_�̃p�X
                byte[] dirBytes = Encoding.UTF8.GetBytes(dir);
                bw.Write((byte)dirBytes.Length);
                bw.Write(dirBytes);
            }
            bw.Write((byte)presets.Count);                      //�v���Z�b�g��
            bw.Write((byte)0x0a);                               //���s
            foreach (SE_Preset preset in presets)
            {
                byte[] presetName = Encoding.UTF8.GetBytes(preset.PresetName);
                bw.Write((byte)presetName.Length);
                bw.Write(presetName);                                               //�v���Z�b�g��
                bw.Write((byte)preset.Types.Count);                                 //SE�̎�ނ̐� (�o�[�W�����ɂ���đ�������\�������邽��)
                foreach (SE_Type type in preset.Types)
                {
                    byte[] typeName = Encoding.UTF8.GetBytes(type.ConstTypeName);
                    bw.Write((byte)typeName.Length);
                    bw.Write(typeName);                                             //SE��
                    bw.Write(type.TypeID);                                          //����ID
                    bw.Write(type.DefaultShortID);                                  //�z�u����R���e�i��ID
                    bw.Write((sbyte)type.Gain);                                     //���ʂ̑��� (�P��:db)
                    bw.Write(type.IsEnable);                                        //�L�����ǂ���
                    bw.Write((byte)type.Sounds.Count);                              //�T�E���h��
                    foreach (SE_Sound sound in type.Sounds)
                    {
                        string? dir = Path.GetDirectoryName(sound.FilePath);
                        //�t�H���_����Dirs����Q�Ƃ��A���݂���΂��̃C���f�b�N�X��ۑ�����B�Ȃ����-1
                        if (dir != null && dirs.Contains(dir))
                            bw.Write((sbyte)dirs.IndexOf(dir));
                        else
                            bw.Write((sbyte)-1);
                        byte[] soundName = Encoding.UTF8.GetBytes(sound.SoundName);
                        bw.Write((byte)soundName.Length);
                        bw.Write(soundName);                                        //�t�@�C����
                        bw.Write(sound.ShortID);                                    //�z�u����R���e�i��ID
                        bw.Write(sound.IsAndroidResource);                          //�����œ����Ă���T�E���h���ǂ���
                        bw.Write(sound.IsDefaultSound);                             //WoTB�p�̃f�t�H���g�̃T�E���h���ǂ���
                    }
                }
            }
        }
        catch (Exception e)
        {
            Sub_Code.ErrorLogWrite(e.Message);
            Message_Feed_Out("�G���[���������܂����B\n" + e.Message);
        }
    }

    private void Preset_Load()
    {
        if (!File.Exists(presetPath))
            return;

        presets.Clear();

        BinaryReader br = new(File.OpenRead(presetPath));
        _ = br.ReadBytes(br.ReadByte());                                        //�w�b�_�[
        _ = br.ReadByte();                                                      //�o�[�W����
        byte dirCount = br.ReadByte();                                          //�t�H���_��
        List<string> dirs = [];
        for (int i = 0; i < dirCount; i++)
            dirs.Add(Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte())));     //�t�H���_�p�X
        nowPresetIndex = br.ReadByte();                                         //�I������Ă���v���Z�b�g�̃C���f�b�N�X
        byte presetCount = br.ReadByte();                                       //�v���Z�b�g��
        _ = br.ReadByte();                                                      //���s
        for (int i = 0; i < presetCount; i++)
        {
            string presetName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));       //�v���Z�b�g��
            SE_Preset preset = new(presetName);
            byte typeCount = br.ReadByte();                                                 //SE�̎�ނ̐�
            for (int j = 0; j < typeCount; j++)
            {
                string typeName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));     //SE��
                uint typeID = br.ReadUInt32();                                              //����ID
                uint defaultShortID = br.ReadUInt32();                                      //�z�u����R���e�i��ID
                sbyte gain = br.ReadSByte();                                                //���ʂ̑��� (�P��:db)
                bool bEnable = br.ReadBoolean();                                            //�L�����ǂ���
                SE_Type seType = new(typeName, defaultShortID, gain)
                {
                    TypeID = typeID,
                    IsEnable = bEnable
                };
                byte soundCount = br.ReadByte();                                            //�T�E���h��
                for (int k = 0; k < soundCount; k++)
                {
                    sbyte dirIndex = br.ReadSByte();                                        //�t�H���_�p�X�̃C���f�b�N�X
                    string dir = "";
                    //-1�łȂ���΃t�H���_�p�X����t�H���_�����擾
                    if (dirIndex != -1)
                        dir = dirs[dirIndex] + "/";
                    string soundName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    string filePath = dir + soundName;
                    uint shortID = br.ReadUInt32();                                         //�z�u����R���e�i��ID
                    bool bAndroidResource = br.ReadBoolean();                               //�����œ����Ă���T�E���h���ǂ���
                    bool bDefaultSound = br.ReadBoolean();                                  //WoTB�p�̃f�t�H���g�̃T�E���h���ǂ���
                    seType.AddSound(filePath, shortID, bAndroidResource, bDefaultSound);    //���X�g�ɒǉ�
                }
                preset.Types.Add(seType);                                       //���X�g�ɒǉ�
            }
            presets.Add(preset);                                //�v���Z�b�g�����X�g�ɒǉ�
        }

        //�O��I�����Ă����v���Z�b�g�����[�h
        if (nowPresetIndex != -1)
            NowPreset = presets[nowPresetIndex];
    }

    private async void Preset_Load_B_Clicked(object? sender, EventArgs e)
    {
        List<string> presetNames = [];
        presetNames.Add("�W��");
        foreach (SE_Preset preset in presets)
            presetNames.Add(preset.PresetName);
        string selectedPreset = await DisplayActionSheet("�v���Z�b�g��I�����Ă��������B", "�L�����Z��", null, [.. presetNames]);
        int index = presetNames.IndexOf(selectedPreset);
        if (index != -1)
        {
            if (selectedPreset == "�W��")
            {
                nowPresetIndex = -1;
                NowPreset = defaultPreset.Clone();
            }
            else
            {
                nowPresetIndex = index - 1;
                NowPreset = presets[index].Clone();
            }
            UpdateList();
            Message_Feed_Out("'" + selectedPreset + "'�����[�h���܂����B");
        }
    }

    private async void Preset_Save_B_Clicked(object? sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("�Z�[�u", "�v���Z�b�g�����w�肵�Ă��������B", "����", "�L�����Z��", null, -1, null, NowPreset.PresetName);
        if (result != null)
        {
            if (!Sub_Code.IsSafePath(result, true))
            {
                Message_Feed_Out("�G���[:�g�p�ł��Ȃ��������܂܂�Ă��܂��B");
                return;
            }
            if (result == "�W��")
            {
                Message_Feed_Out("�W���v���Z�b�g�͏㏑���ł��܂���B");
                return;
            }
            int index = -1;
            for (int i = 0; i < index; i++)
            {
                if (presets[i].PresetName == result)
                {
                    bool result_01 = await DisplayAlert("�v���Z�b�g���㏑�����܂���?", null, "�͂�", "������");
                    if (!result_01)
                        return;
                    index = i;
                }
            }
            if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
                _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");
            if (index != -1)
                presets[index] = NowPreset.Clone();
            else
                presets.Add(NowPreset.Clone());
            Preset_Save();
            Message_Feed_Out("�v���Z�b�g���Z�[�u���܂����B");
        }
    }

    private void SE_Type_L_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        //���ɑI���ς݂̏ꍇ�X�L�b�v
        if (e.SelectedItemIndex != -1)
            if (NowPreset.Types[e.SelectedItemIndex].IsSelected)
                return;

        //���I����Ԃɂ���
        foreach (SE_Type seType in NowPreset.Types)
            seType.IsSelected = false;

        //���̍��ڂ�SE_Type��I���ς݂�
        if (e.SelectedItemIndex != -1)
        {
            NowPreset.Types[e.SelectedItemIndex].IsSelected = true;
        }

        //SE���X�g���X�V
        SE_Type? typeList = e.SelectedItemIndex == -1 ? null : NowPreset.Types[e.SelectedItemIndex];
        SE_Sound_L.ItemsSource = null;
        if (typeList != null && typeList.SoundList.Count > 0)
        {
            SE_Sound_L.ItemsSource = typeList.SoundList;
        }

        UpdateList();
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
