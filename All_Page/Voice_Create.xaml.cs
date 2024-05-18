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
}

public partial class Voice_Create : ContentPage
{
    //�C�x���g�ݒ�
    readonly Voice_Create_Sound_Setting soundSettingWindow = new();

    //�T�E���h�C�x���g
    readonly List<CVoiceSoundList> voiceSounds = [];
    readonly List<List<CVoiceTypeList>> voiceTypes = [];

    ViewCell? lastVoiceTypeCell = null;
    ViewCell? lastVoiceSoundCell = null;

    readonly WVS_Load wvsFile = new();

    static readonly string[] audioExtension = ["audio/*"];
    string projectName = "";

    int nowTypePage = 0;
    int streamHandle = 0;

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
        Create_B.Clicked += OpenEventSetting_B_Click;
        Clear_B.Clicked += Clear_B_Click;
        Init_Voice_Type();
        Set_Item_Type();
        Voice_Type_L.ItemsSource = voiceTypes[0];
    }
    private void Init_Voice_Type()
    {
        voiceSounds.Clear();
        voiceTypes.Clear();
        for (int i = 0; i  < 3; i++)
            voiceTypes.Add([]);

        //1�y�[�W��
        List<CVoiceTypeList> voiceTypeOne = voiceTypes[0];
        voiceTypeOne.Add(new("�����Ƀ_���[�W", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�e���", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G�ւ̖����e", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G�ւ̊ђʒe", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G�ւ̒v���e", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G�ւ̒��e", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�Ԓ�����", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���c�蕉��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G����", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G���j", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G���W���j��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G���W����j", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�G���W������", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���ԗ��΍�", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���ԗ�����", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�R���^���N�j��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("��C�j��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("��C��j", voiceTypeOne.Count));
        voiceTypeOne.Add(new("��C����", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�C�蕉��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���U�蕉��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�ʐM�@�j��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�ʐM�蕉��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�퓬�J�n", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�ϑ����u�j��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�ϑ����u��j", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�ϑ����u����", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���єj��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���ё�j", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���ѕ���", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�C���j��", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�C����j", voiceTypeOne.Count));
        voiceTypeOne.Add(new("�C������", voiceTypeOne.Count));
        voiceTypeOne.Add(new("���ԗ���j", voiceTypeOne.Count));
    }

    //��ʉ����Ƀ��b�Z�[�W��\��
    private async void Message_Feed_Out(string Message)
    {
        //�e�L�X�g�������Ԍo������t�F�[�h�A�E�g
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

    //���X�g�̏�Ԃ��X�V
    private void Set_Item_Type(int nextPage = -1)
    {
        if (nextPage == -1)
            nextPage = nowTypePage;
        bool bChangePage = nowTypePage != nextPage;
        nowTypePage = nextPage;

        Voice_Type_L.SelectedItem = null;
        Voice_Type_L.ItemsSource = null;
        Voice_Type_L.ItemsSource = voiceTypes[nowTypePage];
        Voice_Type_Page_T.Text = "�C�x���g���X�g" + (nowTypePage + 1);
        Sound_File_L.SelectedItem = null;
        Sound_File_L.ItemsSource = null;

        //�y�[�W���O��ƕς���Ă��Ȃ����A���ڂ��I�����ꂽ��Ԃł���΃C�x���g���̃T�E���h�t�@�C�������X�g�ɕ\��
        if (bChangePage && Voice_Type_L.SelectedItem is CVoiceTypeList selectedType)
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
        if (lastVoiceTypeCell != null)
            lastVoiceTypeCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        if (viewCell.View != null && Voice_Type_L.SelectedItem != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastVoiceTypeCell = viewCell;
            CVoiceTypeList typeList = (CVoiceTypeList)Voice_Type_L.SelectedItem;
            voiceSounds.Clear();
            foreach (CVoiceSoundSetting soundSetting in typeList.TypeSetting.Sounds)
                voiceSounds.Add(new(soundSetting));
            if (voiceSounds.Count == 0)
                Sound_File_L.ItemsSource = null;
            else 
                Sound_File_L.ItemsSource = voiceSounds;
        }
    }
    //�T�E���h�t�@�C����I��
    private void Sound_List_Tapped(object sender, EventArgs e)
    {
        if (lastVoiceSoundCell != null)
            lastVoiceSoundCell.View.BackgroundColor = Colors.Transparent;
        ViewCell viewCell = (ViewCell)sender;

        if (viewCell.View != null && Sound_File_L.SelectedItem != null)
        {
            viewCell.View.BackgroundColor = Color.FromArgb("#82bfc8");
            lastVoiceSoundCell = viewCell;
        }

        if (Sound_File_L.SelectedItem == null)
            return;

        if (streamHandle != 0)
            Bass.BASS_StreamFree(streamHandle);

        CVoiceSoundSetting voiceSoundSetting = ((CVoiceSoundList)Sound_File_L.SelectedItem).VoiceSoundSetting;
        //�T�E���h��.wvs�t�@�C�����ɑ��݂��Ă���ꍇ
        if (voiceSoundSetting.IsBinarySound)
        {
            byte[]? soundBytes = wvsFile.Load_Sound(voiceSoundSetting.StreamPosition);
            IntPtr soundPtr = IntPtr.Parse(soundBytes);
            if (soundBytes != null)
            {
                int handle = Bass.BASS_StreamCreateFile(soundPtr, 0, 0, BASSFlag.BASS_STREAM_DECODE);
                streamHandle = Un4seen.Bass.AddOn.Fx.BassFx.BASS_FX_TempoCreate(handle, BASSFlag.BASS_STREAM_DECODE);
            }
        }
        else
        {
            int handle = Bass.BASS_StreamCreateFile(voiceSoundSetting.FilePath, 0, 0, BASSFlag.BASS_STREAM_DECODE);
            streamHandle = Un4seen.Bass.AddOn.Fx.BassFx.BASS_FX_TempoCreate(handle, BASSFlag.BASS_STREAM_DECODE);
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

        if (savedNames.Count == 0)      //�Z�[�u�t�@�C����1�����݂��Ȃ�
        {
            await DisplayActionSheet("�v���W�F�N�g��I�����Ă��������B", "�L�����Z��", null, ["!�v���W�F�N�g�����݂��܂���!"]);
            savedNames.Clear();
        }
        else                            //�Z�[�u�t�@�C��������ΑI���E�B���h�E��\��
        {
            bOtherPageOpened = true;
            string extention = ".wvs";
            Sub_Code.Select_Files_Window.Window_Show("Voice_Create_Load", Sub_Code.ExDir + "/Saves", extention, true, false);
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);
        }
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
            WVS_Save Save = new();
            Save.Add_Sound(voiceTypes, wvsFile);
            wvsFile.Dispose();
            Save.Create(Sub_Code.ExDir + "/Saves/" + result + ".wvs", result);
            Save.Dispose();
            Voice_Load_From_File(Sub_Code.ExDir + "/Saves/" + result + ".wvs");
            Message_Feed_Out("�Z�[�u���܂����B");
        }
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
                Sub_Code.Show_Message("�G���[:�t�@�C�������݂��܂���B");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Header)
            {
                Sub_Code.Show_Message("�G���[:�w�b�_�[���قȂ�܂��B");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.Wrong_Version)
            {
                Sub_Code.Show_Message("�G���[:�Z�[�u�t�@�C���̃o�[�W�������Â����܂��B");
                return;
            }
            else if (wvsResult == WVS_Load.WVS_Result.WoTMode)
            {
                Sub_Code.Show_Message("�G���[:WoT�p�̃Z�[�u�t�@�C���̂��߃��[�h�ł��܂���B");
                return;
            }
            else
            {
                wvsResult = wvsFile.WVS_Load_File(filePath, voiceTypes);
                if (wvsResult == WVS_Load.WVS_Result.OK)
                {
                    projectName = wvsFile.ProjectName;
                    Sub_Code.Show_Message(Path.GetFileName(filePath) + "�����[�h���܂����B");
                }
                else
                    throw new Exception(".wvs�t�@�C�����j�����Ă��܂��B");
            }
            Set_Item_Type();
        }
        catch (Exception e)
        {
            Sub_Code.Show_Message("�G���[:" + e.Message);
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
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("�A�N�Z�X�����s���Ă��������B");
                return;
            }
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //�Ή����Ă���g���q
            Sub_Code.Select_Files_Window.Window_Show("Voice_Create", "", extension);    //�I����ʂ�������
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);              //�I����ʂ��J��
        }
        else
        {
            FilePickerFileType customFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, audioExtension }
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
        CVoiceTypeList typeList = ((CVoiceTypeList)Voice_Type_L.SelectedItem);
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
            Sub_Code.Show_Message("�C�x���g�����I������Ă��܂���B");
            return;
        }
        if (Sound_File_L.SelectedItem == null)
        {
            Sub_Code.Show_Message("�폜�������T�E���h��I�����Ă��������B");
            return;
        }
        int typeIndex = ((CVoiceTypeList)Voice_Type_L.SelectedItem).Index;
        CVoiceSoundList Temp = (CVoiceSoundList)Sound_File_L.SelectedItem;
        int removeIndex = voiceSounds.IndexOf(Temp);
        if (removeIndex == -1)
        {
            Sub_Code.Show_Message("�s���ȃG���[���������܂����B");
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
        soundSettingWindow.Initialize(voiceSounds, wvsFile);
        Navigation.PushAsync(soundSettingWindow);
        bOtherPageOpened = true;
    }
}