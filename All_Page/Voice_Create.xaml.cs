using System.Reflection;
using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

//�T�E���h
public class CVoiceSoundList(CVoiceSoundSetting voiceSetting)
{
    public CVoiceSoundSetting VoiceSoundSetting { get; set; } = voiceSetting;
    public string Name_Text => Path.GetFileName(VoiceSoundSetting.FilePath);
    public string Name_Probability => Path.GetFileName(VoiceSoundSetting.FilePath) + " | �D��x:" + (int)VoiceSoundSetting.Probability;
}

//�T�E���h�C�x���g�N���X
public class CVoiceTypeList(string eventName, int index)
{
    public string Name { get; set; } = eventName;
    public string Name_Text => Name + " : " + Count + "��";
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
    //SE�ݒ�
    public SE_Setting SESettingWindow;

    //�C�x���g�ݒ�
    Voice_Create_Sound_Setting? soundSettingWindow;
    Build_Setting? buildSettingWindow;

    //�T�E���h�C�x���g
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

        //1�y�[�W��
        List<CVoiceTypeList> voiceTypeOne = voiceTypes[0];
        voiceTypeOne.Add(new("�����Ƀ_���[�W", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(341425709, 170029050, sePreset.GetSEType("�ђ�"), "ally_killed_by_player");
        voiceTypeOne.Add(new("�e��ɔj��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(908426860, 95559763, sePreset.GetSEType("�e��ɔj��"), "ammo_bay_damaged");
        voiceTypeOne.Add(new("�G�ւ̖����e", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(280189980, 766083947, sePreset.GetSEType("��ђ�-�����e"), "armor_not_pierced_by_player");
        voiceTypeOne.Add(new("�G�ւ̊ђʒe", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(815358870, 569784404, sePreset.GetSEType("�ђ�"), "armor_pierced_by_player");
        voiceTypeOne.Add(new("�G�ւ̒v���e", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(49295125, 266422868, sePreset.GetSEType("�G���W���[���j��"), "armor_pierced_crit_by_player");
        voiceTypeOne.Add(new("�G�ւ̒��e", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(733342682, 1052258113, sePreset.GetSEType("��ђ�-���e"), "armor_ricochet_by_player");
        voiceTypeOne.Add(new("�Ԓ�����", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(331196727, 242302464, sePreset.GetSEType("���������"), "commander_killed");
        voiceTypeOne.Add(new("���c�蕉��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(619058694, 334837201, sePreset.GetSEType("���������"), "driver_killed");
        voiceTypeOne.Add(new("�G����", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(794420468, 381780774, sePreset.GetSEType("�G����"), "enemy_fire_started_by_player");
        voiceTypeOne.Add(new("�G���j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(109598189, 489572734, sePreset.GetSEType("�G���j"), "enemy_killed_by_player");
        voiceTypeOne.Add(new("�G���W���j��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(244621664, 210078142, sePreset.GetSEType("���W���[���j��"), "engine_damaged");
        voiceTypeOne.Add(new("�G���W����j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(73205091, 249535989, sePreset.GetSEType("���W���[����j"), "engine_destroyed");
        voiceTypeOne.Add(new("�G���W������", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(466111031, 908710042, sePreset.GetSEType("���W���[������"), "engine_functional");
        voiceTypeOne.Add(new("���ԗ��΍�", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(471196930, 1057023960, null, "fire_started");
        voiceTypeOne.Add(new("���ԗ�����", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(337626756, 953778289, null, "fire_stopped");
        voiceTypeOne.Add(new("�R���^���N�j��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(930519512, 121897540, sePreset.GetSEType("�R���^���N�j��"), "fuel_tank_damaged");
        voiceTypeOne.Add(new("��C�j��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(1063632502, 127877647, sePreset.GetSEType("���W���[���j��"), "gun_damaged");
        voiceTypeOne.Add(new("��C��j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(175994480, 462397017, sePreset.GetSEType("���W���[����j"), "gun_destroyed");
        voiceTypeOne.Add(new("��C����", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(546476029, 651656679, sePreset.GetSEType("���W���[������"), "gun_functional");
        voiceTypeOne.Add(new("�C�蕉��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(337748775, 739086111, sePreset.GetSEType("���������"), "gunner_killed");
        voiceTypeOne.Add(new("���U�蕉��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(302644322, 363753108, sePreset.GetSEType("���������"), "loader_killed");
        voiceTypeOne.Add(new("�ʐM�@�j��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(356562073, 91697210, sePreset.GetSEType("�����@�j��"), "radio_damaged");
        voiceTypeOne.Add(new("�ʐM�蕉��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(156782042, 987172940, sePreset.GetSEType("���������"), "radioman_killed");
        voiceTypeOne.Add(new("�퓬�J�n", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(769815093, 518589126, sePreset.GetSEType("�퓬�J�n"), "start_battle");
        voiceTypeOne.Add(new("�ϑ����u�j��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(236686366, 330491031, sePreset.GetSEType("���W���[���j��"), "surveying_devices_damaged");
        voiceTypeOne.Add(new("�ϑ����u��j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(559710262, 792301846, sePreset.GetSEType("���W���[����j"), "surveying_devices_destroyed");
        voiceTypeOne.Add(new("�ϑ����u����", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(47321344, 539730785, sePreset.GetSEType("���W���[������"), "surveying_devices_functional");
        voiceTypeOne.Add(new("���єj��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(978556760, 38261315, sePreset.GetSEType("���W���[���j��"), "track_damaged");
        voiceTypeOne.Add(new("���ё�j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(878993268, 37535832, sePreset.GetSEType("���W���[����j"), "track_destroyed");
        voiceTypeOne.Add(new("���ѕ���", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(581830963, 558576963, sePreset.GetSEType("���W���[������"), "track_functional");
        voiceTypeOne.Add(new("�C���j��", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(984973529, 1014565012, sePreset.GetSEType("���W���[���j��"), "turret_rotator_damaged");
        voiceTypeOne.Add(new("�C����j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(381112709, 135817430, sePreset.GetSEType("���W���[����j"), "turret_rotator_destroyed");
        voiceTypeOne.Add(new("�C������", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(33436524, 985679417, sePreset.GetSEType("���W���[������"), "turret_rotator_functional");
        voiceTypeOne.Add(new("���ԗ���j", voiceTypeOne.Count));
        voiceTypeOne[^1].InitTypeSetting(116097397, 164671745, sePreset.GetSEType("���ԗ���j"), "vehicle_destroyed");

        //2�y�[�W��
        List<CVoiceTypeList> voiceTypeTwo = voiceTypes[1];
        voiceTypeTwo.Add(new("�G����", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(308272618, 447063394, sePreset.GetSEType("�G����"));
        voiceTypeTwo.Add(new("��Z��", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(767278023, 154835998, sePreset.GetSEType("��Z��"));
        voiceTypeTwo.Add(new("����", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(230904672, 607694618, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("����", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(390478464, 391276124, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("�~���𐿂�", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(17969037, 840378218, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("�U������I", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(900922817, 549968154, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("�U����", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(727518878, 1015337424, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("�w�n���̂���I", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(101252368, 271044645, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("�w�n��h�q����I ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(576711003, 310153012, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("�Ŏ点��I ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(470859110, 379548034, sePreset.GetSEType("�N�C�b�N�R�}���h"));
        voiceTypeTwo.Add(new("���b�N�I�� ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(502585189, 839607605, sePreset.GetSEType("���b�N�I��"));
        voiceTypeTwo.Add(new("�A�����b�N ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(769354725, 233444430, sePreset.GetSEType("�A�����b�N"));
        voiceTypeTwo.Add(new("���U���� ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(402727222, 299739777, sePreset.GetSEType("���U����"));
        voiceTypeTwo.Add(new("�}�b�v�N���b�N�� ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(670169971, 120795627, sePreset.GetSEType("�}�b�v�N���b�N"));
        voiceTypeTwo.Add(new("�퓬�I����(���Ԑ؂���̎��̂�) ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(204685755, 924876614, sePreset.GetSEType("���Ԑ؂�&��̃|�C���gMax"));
        voiceTypeTwo.Add(new("�퓬BGM ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(1065169508, 891902653, null);
        voiceTypeTwo.Add(new("�ړ��� ", voiceTypeTwo.Count));
        voiceTypeTwo[^1].InitTypeSetting(198183306, 52813795, sePreset.GetSEType("�ړ���"));

        //3�y�[�W��
        List<CVoiceTypeList> voiceTypeThree = voiceTypes[2];
        voiceTypeThree.Add(new("�`���b�g:����-���M", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(440862849, 491691546, null);
        voiceTypeThree.Add(new("�`���b�g:����-��M", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(750405, 417768496, null);
        voiceTypeThree.Add(new("�`���b�g:�S��-���M", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(228164469, 46472417, null);
        voiceTypeThree.Add(new("�`���b�g:�S��-��M", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(253510370, 681331945, null);
        voiceTypeThree.Add(new("�`���b�g:����-���M", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(639029638, 190711689, null);
        voiceTypeThree.Add(new("�`���b�g:����-��M", voiceTypeThree.Count));
        voiceTypeTwo[^1].InitTypeSetting(523678232, 918836720, null);
    }

    //��ʉ����Ƀ��b�Z�[�W��\��
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

    async void StateLoop(string projectName)
    {
        while (state == Build_Setting.Build_State.Building)
            await Task.Delay(100);

        Voice_Load_From_File(Sub_Code.ExDir + "/Saves/" + projectName + ".wvs");

        Message_Feed_Out("�Z�[�u���܂����B");
    }

    //���X�g�̏�Ԃ��X�V
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
        Voice_Type_Page_T.Text = "�C�x���g���X�g" + (nowTypePage + 1);
        Sound_File_L.SelectedItem = null;
        Sound_File_L.ItemsSource = null;

        //�y�[�W���O��ƕς���Ă��Ȃ����A���ڂ��I�����ꂽ��Ԃł���΃C�x���g���̃T�E���h�t�@�C�������X�g�ɕ\��
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

    //�T�E���h�C�x���g��I��
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

    //.wvs�t�@�C�������[�h
    private async void Load_B_Click(object? sender, EventArgs e)
    {
        //���̃E�B���h�E���J����Ă�����X���[
        if (bOtherPageOpened)
            return;

        //�Z�[�u�����Ă���.wvs�t�@�C�����
        List<string> savedNames = [];
        if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
            _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");
        foreach (string Files in Directory.GetFiles(Sub_Code.ExDir + "/Saves", "*.wvs", SearchOption.TopDirectoryOnly))
            savedNames.Add(Path.GetFileNameWithoutExtension(Files));

        //�I����ʂ�\��
        if (savedNames.Count == 0)      //�Z�[�u�t�@�C����1�����݂��Ȃ�
            await DisplayActionSheet("�v���W�F�N�g��I�����Ă��������B", "�L�����Z��", null, ["!�v���W�F�N�g�����݂��܂���!"]);
        else
        {
            string fileName = await DisplayActionSheet("�v���W�F�N�g��I�����Ă��������B", "�L�����Z��", null, [..savedNames]);
            int index = savedNames.IndexOf(fileName);
            if (index != -1)
            {
                Voice_Load_From_File(Sub_Code.ExDir + "/Saves/" + savedNames[index] + ".wvs");
                Set_Item_Type();
            }
        }
        savedNames.Clear();
    }

    //�v���W�F�N�g��ۑ�
    private async void Save_B_Click(object? sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("�Z�[�u", "�v���W�F�N�g�����w�肵�Ă��������B", "����", "�L�����Z��", null, -1, null, projectName);
        if (result != null)
        {
            if (!Sub_Code.IsSafePath(result, true))
            {
                Message_Feed_Out("�G���[:�g�p�ł��Ȃ��������܂܂�Ă��܂��B");
                return;
            }
            else if (File.Exists(Sub_Code.ExDir + "/Saves/" + result + ".wvs"))
            {
                bool result_01 = await DisplayAlert("�Z�[�u�f�[�^���㏑�����܂���?", null, "�͂�", "������");
                if (!result_01)
                    return;
            }
            if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
                _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");

            bMessageShowing = false;
            Message_T.Text = "�Z�[�u���Ă��܂�...";
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

    //�v���W�F�N�g�����[�h
    private void Voice_Load_From_File(string filePath)
    {
        try
        {
            //������z�u
            Init_Voice_Type();
            WVS_Load.WVS_Result wvsResult = WVS_Load.IsBlitzWVSFile(filePath);
            if (wvsResult == WVS_Load.WVS_Result.No_Exist_File)
            {
                Message_Feed_Out("�G���[:�t�@�C�������݂��܂���B");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Header)
            {
                Message_Feed_Out("�G���[:�w�b�_�[���قȂ�܂��B");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Version)
            {
                Message_Feed_Out("�G���[:�Z�[�u�t�@�C���̃o�[�W�������Â����܂��B");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.WoTMode)
            {
                Message_Feed_Out("�G���[:WoT�p�̃Z�[�u�t�@�C���̂��߃��[�h�ł��܂���B");
                return;
            }
            else
            {
                wvsResult = wvsFile.WVS_Load_File(filePath, voiceTypes);
                if (wvsResult == WVS_Load.WVS_Result.OK)
                {
                    projectName = wvsFile.ProjectName;
                    Message_Feed_Out(Path.GetFileNameWithoutExtension(filePath) + "�����[�h���܂����B");
                }
                else
                    throw new Exception(".wvs�t�@�C�����j�����Ă��܂��B");
            }
            Set_Item_Type();
        }
        catch (Exception e)
        {
            Message_Feed_Out("�G���[:" + e.Message);
            Init_Voice_Type();
            Set_Item_Type();
            wvsFile.Dispose();
        }
    }

    //�O�̃y�[�W��
    private void Voice_Type_Back_B_Click(object? sender, EventArgs e)
    {
        if (nowTypePage > 0)
        {
            Set_Item_Type(nowTypePage - 1);
        }
    }
    //���̃y�[�W��
    private void Voice_Type_Next_B_Click(object? sender, EventArgs e)
    {
        if (nowTypePage < 2)
        {
            Set_Item_Type(nowTypePage + 1);
        }
    }

    //�T�E���h��ǉ�
    private async void Sound_Add_B_Click(object? sender, EventArgs e)
    {
        //�G���[���
        if (Voice_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("�C�x���g�����I������Ă��܂���B");
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
            Sub_Code.Select_Files_Window.Window_Show("Voice_Create", "", extension);    //�I����ʂ�������
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
    //�T�E���h��ǉ�
    public void Add_Sound(List<string> files)
    {
        CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
        int alreadyVoiceCount = 0;
        int addedVoiceCount = 0;
        foreach (string filePath in files)
        {
            bool bExist = false;
            //�f�[�^���S�������t�@�C�������݂��邩���ׂ�
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

            //�C�x���g�ɃT�E���h��ǉ�
            typeList.TypeSetting.Sounds.Add(new(filePath));
            addedVoiceCount++;
        }
        //���X�gUI���X�V
        Set_Item_Type();

        if (alreadyVoiceCount > 0 && addedVoiceCount == 0)
            Message_Feed_Out("���ɒǉ�����Ă��邽��" + alreadyVoiceCount + "�̃t�@�C�����X�L�b�v���܂����B");
        else if (alreadyVoiceCount > 0 && addedVoiceCount > 0)
            Message_Feed_Out("���ɒǉ�����Ă��邽��" + alreadyVoiceCount + "�̃t�@�C�����X�L�b�v���A" + addedVoiceCount + "�̃t�@�C����ǉ����܂����B");
        else
            Message_Feed_Out(addedVoiceCount + "�̃t�@�C�����C�x���g�ɒǉ����܂����B");
    }

    //�t�H�[�J�X�����̉�ʂɈڂ�����
    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bOtherPageOpened = false;
    }

    //�T�E���h�̍폜�{�^��
    private void Sound_Delete_B_Click(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        if (Voice_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("�C�x���g�����I������Ă��܂���B");
            return;
        }
        if (Sound_File_L.SelectedItem == null)
        {
            Message_Feed_Out("�폜�������T�E���h��I�����Ă��������B");
            return;
        }
        int typeIndex = ((CVoiceTypeList)Voice_Type_L.SelectedItem).Index;
        CVoiceSoundList Temp = (CVoiceSoundList)Sound_File_L.SelectedItem;
        int removeIndex = voiceSounds.IndexOf(Temp);
        if (removeIndex == -1)
        {
            Message_Feed_Out("�s���ȃG���[���������܂����B");
            return;
        }
        voiceTypes[nowTypePage][typeIndex].TypeSetting.Sounds.RemoveAt(removeIndex);
        Set_Item_Type();
    }

    //�N���A�{�^��
    private async void Clear_B_Click(object? sender, EventArgs e)
    {
        bool Result = await DisplayAlert("�m�F", "���e���N���A���܂���?", "�͂�", "������");
        if (Result)
        {
            Init_Voice_Type();
            Set_Item_Type();
            wvsFile.Dispose();
            projectName = "";
            Message_Feed_Out("�N���A���܂����B");
        }
    }

    //�C�x���g�ݒ��ʂ��J��
    private void OpenEventSetting_B_Click(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
        if (typeList == null)
            return;
        if (typeList.Count <= 0)
        {
            Message_Feed_Out("�C�x���g���ɃT�E���h���܂܂�Ă��Ȃ����ߐݒ��ʂ��J�����Ƃ��ł��܂���B");
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