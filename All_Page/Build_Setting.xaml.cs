using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Build_Setting : ContentPage
{
    public enum Build_State
    {
        None,
        Building,
        Uploading,
        Uploaded,
        Failed
    }
    readonly List<List<CVoiceTypeList>> voiceTypes;
    readonly WVS_Load wvsData;
    readonly SE_Preset sePreset;

    readonly Google_Drive drive = new();

    readonly string uploadIDFile = Sub_Code.ExDir + "/Temp0101.dat";

    Build_State state;

    string commandText = "";
    string fileID = "";

    bool bMessageShowing = false;
    bool bUploaded = false;

    public Build_Setting(List<List<CVoiceTypeList>> voiceTypes, WVS_Load wvsData, SE_Preset sePreset)
	{
		InitializeComponent();

        this.voiceTypes = voiceTypes;
        this.wvsData = wvsData;
        this.sePreset = sePreset;

        //�{�^������
        Server_Build_B.Clicked += Server_Build_B_Clicked;
        Command_Copy_B.Clicked += Command_Copy_B_Clicked;

        Volume_S.ValueChanged += Volume_S_ValueChanged;

        Command_T.Text = "�A�b�v���[�h�҂�...";

        ShowVolumeSlider();
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
            if (Number >= 250)
                Message_T.Opacity -= 0.025;
            await Task.Delay(1000 / 60);
        }
        bMessageShowing = false;
        Message_T.Text = "";
        Message_T.Opacity = 1;
    }

    async void Loop()
    {
        Build_State nowState = state;
        while (true)
        {
            if (state == Build_State.Uploaded)
                break;
            if (nowState == Build_State.Building && state == Build_State.Uploading)
                Message_T.Text = ".wvs�t�@�C�����쐬���܂����B\n�A�b�v���[�h���Ă��܂�...";
            nowState = state;

            await Task.Delay(1000 / 10);
        }

        if (state == Build_State.Uploaded)
            Message_T.Text = "�v���W�F�N�g�t�@�C�����A�b�v���[�h���܂����B�\������Ă���R�}���h��Discord��Bot(SRTT_Yuna)�ɑ��M���Ă��������B";
        else if (state == Build_State.Failed)
            Message_Feed_Out("�A�b�v���[�h�Ɏ��s���܂����B���Ԃ�u���čēx�r���h���Ă��������B");

        BinaryWriter bw = new(File.OpenWrite(uploadIDFile));
        byte[] fileIDBytes = System.Text.Encoding.UTF8.GetBytes(fileID);
        bw.Write((byte)fileIDBytes.Length);
        bw.Write(fileIDBytes);
        bw.Close();

        Command_T.Text = commandText;

        state = Build_State.None;
    }

    //.wvs���쐬���h���C�u�ɃA�b�v���[�h
    private async void Server_Build_B_Clicked(object? sender, EventArgs e)
    {
        if (state != Build_State.None)
            return;

        if (voiceTypes == null || wvsData == null || sePreset == null)
            return;

        if (string.IsNullOrWhiteSpace(Project_Name_T.Text))
        {
            Message_Feed_Out("�G���[:�ۑ��������͂���Ă��Ȃ����A�󔒂̉\��������܂��B");
            return;
        }
        else if (Project_Name_T.Text.Contains('|'))
        {
            Message_Feed_Out("�ۑ�����'|'���g�p���邱�Ƃ͂ł��܂���B");
            return;
        }
        else if (Project_Name_T.Text.Contains('\\') || Project_Name_T.Text.Contains('/'))
        {
            Message_Feed_Out("�ۑ����Ɏg�p�s�ȕ������܂܂�Ă��܂��B");
            return;
        }
        else if (!Sub_Code.IsSafePath(Project_Name_T.Text, true))
        {
            Message_Feed_Out("�ۑ����Ɏg�p�s�ȕ������܂܂�Ă��܂��B");
            return;
        }

        if (bUploaded || File.Exists(uploadIDFile))
        {
            string message = "'" + Project_Name_T.Text + "'���r���h���܂���?";
            string message2 = "�ȑO�r���h�����t�@�C���ƃR�}���h�͍폜����܂��B";
            bool result_01 = await DisplayAlert(message, message2, "�͂�", "������");
            if (!result_01)
                return;
        }

        state = Build_State.Building;

        Command_T.Text = "�A�b�v���[�h�҂�...";

        //���ɃA�b�v���[�h����wvs�t�@�C�����h���C�u����폜
        if (File.Exists(uploadIDFile))
        {
            BinaryReader br = new(File.OpenRead(uploadIDFile));
            try
            {
                string id = System.Text.Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                drive.DeleteFile(id);
            }
            catch
            {
            }
            br.Close();
            File.Delete(uploadIDFile);
        }

        //.wvs�t�@�C�����쐬
        bMessageShowing = false;
        Message_T.Text = ".wvs�t�@�C�����쐬���Ă��܂�...";
        Message_T.Opacity = 1.0;

        Loop();
        BuildAndUpload();
    }

    private async void Command_Copy_B_Clicked(object? sender, EventArgs e)
    {
        if (state != Build_State.None)
            return;
        try
        {
            if (Command_T.Text == "�A�b�v���[�h�҂�...")
            {
                Message_Feed_Out("���'�쐬'�{�^���������ĉ������B");
                return;
            }
            await Clipboard.SetTextAsync(Command_T.Text);
            Message_Feed_Out("�N���b�v�{�[�h�ɃR�s�[���܂����B");
        }
        catch
        {
            Message_Feed_Out("�R�s�[�Ɏ��s���܂����B");
        }
    }

    void BuildAndUpload()
    {
        string saveFilePath = Sub_Code.ExDir + "/Generated_Project.wvs";
        Task.Run(() =>
        {
            //SE���܂߂��v���W�F�N�g���쐬
            WVS_Save save = new();
            if (Volume_Set_C.IsChecked)
                save.Add_Sound(voiceTypes, wvsData, Math.Round(Volume_S.Value, 1), Default_Voice_Mode_C.IsChecked);
            else
                save.Add_Sound(voiceTypes, wvsData, 0.0, Default_Voice_Mode_C.IsChecked);
            save.Create(saveFilePath, Project_Name_T.Text, true, sePreset, false);

            state = Build_State.Uploading;

            //�h���C�u�ɃA�b�v���[�h
            int uploadID = Sub_Code.RandomValue.Next(1000000, int.MaxValue);
            if (drive.UploadFile(saveFilePath, out string fileID, uploadID + ".wvs"))
            {
                commandText = ".swvs-id " + uploadID;
                this.fileID = fileID;
                state = Build_State.Uploaded;
                bUploaded = true;
            }
            else
            {
                state = Build_State.Failed;
            }
        });
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

    private void Volume_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        double volume = Math.Round(Volume_S.Value, 1);
        Volume_T.Text = volume + "db";
    }

    private void Volume_Set_C_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Volume_Set_T.Text = "���ʂ��ψ�ɂ���";
        if (e.Value)
            Volume_Set_T.Text += "(�f�t�H���g:99db)";
        ShowVolumeSlider();
    }

    void ShowVolumeSlider()
    {
        Volume_S.IsVisible = Volume_Set_C.IsChecked;
        Volume_T.IsVisible = Volume_Set_C.IsChecked;
    }
}