using System.Reflection;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

//�T�E���h���
public class OtherModSound(string filePath, bool bDefaultSound = false, bool bAndroidResource = false)
{
    public string FilePath = filePath;

    public string NameText => Path.GetFileName(FilePath);

    public long StreamPosition = 0;

    public bool IsBinarySound => StreamPosition != 0;
    public bool IsDefaultSound = bDefaultSound;
    public bool IsAndroidResource = bAndroidResource;
}
//�^�C�v���
public class OtherModType(string modTypeName, uint containerID)
{
    public string ModTypeName = modTypeName;                    //�^�C�v��
    public uint ModTypeID = WwiseHash.HashString(modTypeName);  //�^�C�vID
    public uint ContainerID = containerID;                      //�T�E���h�̔z�u�ꏊ

    public string NameText => ModTypeName + " | " + SoundCount + "��";
    public Color NameColor => SoundCount == 0 ? Color.FromArgb("#BFFF2C8C") : Colors.Aqua;

    public readonly List<OtherModSound> Sounds = [];
    public int SoundCount => Sounds.Count;

    public bool AddSound(string filePath, bool bDefaultSound = false, bool bAndroidResource = false)
    {
        if (Sounds.Count >= 255)
            return false;
        foreach (OtherModSound sound in Sounds)
            if (sound.FilePath == filePath && sound.IsDefaultSound == bDefaultSound)
                return false;
        Sounds.Add(new(filePath, bDefaultSound, bAndroidResource));
        return true;
    }
}
//�y�[�W���
public class OtherModPage(string modPageName, string wwiseProjectName)
{
    public string ModPageName = modPageName;
    public string WwiseProjectName = wwiseProjectName;
    public uint modPageID = WwiseHash.HashString(modPageName);

    public List<OtherModType> Types = [];
    public List<string> BuildList = [];
}

public partial class Other_Create : ContentPage
{
    readonly List<OtherModPage> modPages = [];

    OtherModPage NowModPage => modPages[Mod_Selection_Picker.SelectedIndex];    //���ݑI������Ă���y�[�W
    SYNCPROC? soundEndFunc = null;                                              //�Đ��I�����m

    readonly WMS_Load wmsLoad = new();           //�Z�[�u�t�@�C���ɃT�E���h�f�[�^���܂܂�Ă���ꍇ�g�p
    GCHandle soundPtr = new();
    Build_Setting.Build_State state = Build_Setting.Build_State.None;

    string projectName = "";
    string maxTime = "00:00";           //�Đ����̃T�E���h�̒���

    int streamHandle = 0;               //�Đ����̃T�E���h�̃n���h��

    bool bShowing = false;              //�E�B���h�E�\����
    bool bOtherPageOpened = false;      //�O�ʂɑ��̃E�B���h�E��\����
    bool bMessageShowing = false;       //�������b�Z�[�W�\����
    bool bEnded = false;                //�Đ��I�����m
    bool bPaused = false;               //��~����
    bool bLocationChanging = false;     //�V�[�N�o�[���^�b�`������true
    bool bPlayingMouseDown = false;     //�V�[�N�o�[���^�b�`������true

    public Other_Create()
	{
		InitializeComponent();

        //�X���C�_�[�ݒ�
        All_Volume_S.ValueChanged += All_Volume_S_ValueChanged;
        PlayTime_S.DragStarted += PlayTime_S_DragStarted; ;
        PlayTime_S.DragCompleted += PlayTime_S_DragCompleted;
        PlayTime_S.ValueChanged += PlayTime_S_ValueChanged;

        //�{�^���ݒ�
        Add_Sound_B.Clicked += Add_Sound_B_Clicked;
        Delete_Sound_B.Clicked += Delete_Sound_B_Clicked;
        Pause_B.Clicked += Pause_B_Clicked;
        Play_B.Clicked += Play_B_Clicked;
        Minus_B.Clicked += Minus_B_Clicked;
        Plus_B.Clicked += Plus_B_Clicked;
        Save_B.Clicked += Save_B_Clicked;
        Load_B.Clicked += Load_B_Clicked;
        BuildSetting_B.Clicked += BuildSetting_B_Clicked;

        //�y�[�W�ύX
        Mod_Selection_Picker.SelectedIndexChanged += Mod_Selection_Picker_SelectedIndexChanged;

        InitializePages();

        foreach (OtherModPage type in modPages)
            Mod_Selection_Picker.Items.Add(type.ModPageName);
    }

    void InitializePages()
    {
        //�y�[�W������
        //�f�t�H���g�̃T�E���h����邩���������ǁA�㋉�҈ȊO�̓C�x���g���������Ƃǂ�ȉ���������Ȃ��Ǝv��������20MB���炢�e�ʑ����邯�Ǔ��ꂽ�I���炢�I
        modPages.Clear();
        modPages.Add(new("�퓬�J�n�O���[�hBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("���[�h1:America_lakville", 205170598));
        modPages[^1].Types[^1].AddSound("Music_Map_America_Lakville.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h2:America_overlord", 148841988));
        modPages[^1].Types[^1].AddSound("Music_Map_America_Overlord.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h3:Chinese", 1067185674));
        modPages[^1].Types[^1].AddSound("Music_Map_Chinese.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h4:Desert_airfield", 99202684));
        modPages[^1].Types[^1].AddSound("Music_Map_Desert_Airfield.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h5:Desert_sand_river", 493356780));
        modPages[^1].Types[^1].AddSound("Music_Map_Desert_Sand_River.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h6:Europe_himmelsdorf", 277287194));
        modPages[^1].Types[^1].AddSound("Music_Map_Europe_Himmelsdorf.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h7:Europe_mannerheim", 321403539));
        modPages[^1].Types[^1].AddSound("Music_Map_Europe_Mannerheim.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h8:Europe_ruinberg", 603412881));
        modPages[^1].Types[^1].AddSound("Music_Map_Europe_Ruinberg.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h9:Japan", 256533957));
        modPages[^1].Types[^1].AddSound("Music_Map_Japan.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h10:Russian_malinovka", 520751345));
        modPages[^1].Types[^1].AddSound("Music_Map_Russian_Malinovka.ogg", true, true);
        modPages[^1].Types.Add(new("���[�h11:Russian_prokhorovka", 307041675));
        modPages[^1].Types[^1].AddSound("Music_Map_Russian_Prokhorovka.ogg", true, true);
        //�r���h���X�g (*.bnk�̖��O����)
        modPages[^1].BuildList.Add("music_maps_america_lakville");
        modPages[^1].BuildList.Add("music_maps_america_overlord");
        modPages[^1].BuildList.Add("music_maps_chinese");
        modPages[^1].BuildList.Add("music_maps_desert_airfield");
        modPages[^1].BuildList.Add("music_maps_desert_sand_river");
        modPages[^1].BuildList.Add("music_maps_europe_himmelsdorf");
        modPages[^1].BuildList.Add("music_maps_europe_mannerheim");
        modPages[^1].BuildList.Add("music_maps_europe_ruinberg");
        modPages[^1].BuildList.Add("music_maps_japan");
        modPages[^1].BuildList.Add("music_maps_russian_malinovka");
        modPages[^1].BuildList.Add("music_maps_russian_prokhorovka");

        modPages.Add(new("���U���gBGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("���U���g:����-BGM", 960016609));
        modPages[^1].Types[^1].AddSound("Music_Result_Victory.ogg", true, true);
        modPages[^1].Types.Add(new("���U���g:����-����", 737229060));
        modPages[^1].Types.Add(new("���U���g:��������-BGM", 404033224));
        modPages[^1].Types[^1].AddSound("Music_Result_Draw.ogg", true, true);
        modPages[^1].Types.Add(new("���U���g:��������-����", 480862388));
        modPages[^1].Types.Add(new("���U���g:�s�k-BGM", 797792182));
        modPages[^1].Types[^1].AddSound("Music_Result_Defeat.ogg", true, true);
        modPages[^1].Types.Add(new("���U���g:�s�k-����", 761638380));
        //�r���h���X�g
        modPages[^1].BuildList.Add("music_result_screen");
        modPages[^1].BuildList.Add("music_result_screen_basic");

        modPages.Add(new("�퓬���̗D��BGM", "WoTB_Sound_Mod2"));
        modPages[^1].Types.Add(new("�D��-����", 434309394));
        modPages[^1].Types[^1].AddSound("Music_Battle_Will_Win.ogg", true, true);
        modPages[^1].Types.Add(new("�D��-����-����", 1057587120));
        modPages[^1].Types.Add(new("�D��-�G", 868083406));
        modPages[^1].Types[^1].AddSound("Music_Battle_Will_Defeat.ogg", true, true);
        modPages[^1].Types.Add(new("�D��-�G-����", 611060765));
        modPages[^1].Types.Add(new("�D��-�T�C����", 971715704));
        modPages[^1].Types[^1].AddSound("danger.ogg", true, true);
        //�r���h���X�g
        modPages[^1].BuildList.Add("music_battle");

        modPages.Add(new("�K���[�WSE", "WoTB_UI_Button_Sound"));
        modPages[^1].Types.Add(new("���p-SE", 432722439));
        modPages[^1].Types[^1].AddSound("Sell_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("���p-����", 26462958));
        modPages[^1].Types.Add(new("�`�F�b�N�{�b�N�X-SE", 278676259));
        modPages[^1].Types[^1].AddSound("CheckBox_SE_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("CheckBox_SE_02.ogg", true, true);
        modPages[^1].Types.Add(new("�`�F�b�N�{�b�N�X-����", 1015843643));
        modPages[^1].Types.Add(new("������M-SE", 123366428));
        modPages[^1].Types[^1].AddSound("Squad_Request_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("������M-����", 1034987615));
        modPages[^1].Types.Add(new("���W���[���̐؂�ւ�-SE", 1001742020));
        modPages[^1].Types[^1].AddSound("Module_Install_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("���W���[���̐؂�ւ�-����", 537387720));
        modPages[^1].Types.Add(new("�퓬�J�n�{�^��-SE", 251988040));
        modPages[^1].Types[^1].AddSound("Start_Battle_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("�퓬�J�n�{�^��-����", 56850118));
        modPages[^1].Types.Add(new("�j���[�X-SE", 530790297));
        modPages[^1].Types[^1].AddSound("News_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("�j���[�X-����", 1036212148));
        modPages[^1].Types.Add(new("�ԗ��[��-SE", 660827574));
        modPages[^1].Types[^1].AddSound("Tank_Buy_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("�ԗ��[��-����", 192152217));
        modPages[^1].Types.Add(new("�����w��-SE", 409835290));
        modPages[^1].Types[^1].AddSound("Buy_SE_01.ogg", true, true);
        modPages[^1].Types.Add(new("�����w��-����", 282116325));
        //�r���h���X�g
        modPages[^1].BuildList.Add("ui_buttons_tasks");

        modPages.Add(new("�C����", "WoTB_Gun_Sound"));
        modPages[^1].Types.Add(new("12�`23mm:���ԗ�-�ʏ�", 634610718));
        modPages[^1].Types[^1].AddSound("Gun_Outside_auto_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_auto_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_auto_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("12�`23mm:���ԗ�-�Y�[����", 142135010));
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_auto_1p_05.ogg", true, true);
        modPages[^1].Types.Add(new("12�`23mm:���ԗ�", 611442385));
        modPages[^1].Types[^1].AddSound("Gun_NPC_auto_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_auto_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_auto_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("12�`23mm:���ԗ�-����", 245911224));
        modPages[^1].Types.Add(new("12�`23mm:���ԗ�-����", 403248155));
        modPages[^1].Types.Add(new("20�`45mm:���ԗ�-�ʏ�", 220137673));
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_small_1p_05.ogg", true, true);
        modPages[^1].Types.Add(new("20�`45mm:���ԗ�-�Y�[����", 891043773));
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_small_1p_04.ogg", true, true);
        modPages[^1].Types.Add(new("20�`45mm:���ԗ�", 983327549));
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_small_3p_05.ogg", true, true);
        modPages[^1].Types.Add(new("20�`45mm:���ԗ�-����", 391540681));
        modPages[^1].Types.Add(new("20�`45mm:���ԗ�-����", 388036628));
        modPages[^1].Types.Add(new("50�`75mm:���ԗ�-�ʏ�", 342549628));
        modPages[^1].Types[^1].AddSound("Gun_Outside_mid_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_mid_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_mid_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("50�`75mm:���ԗ�-�Y�[����", 76784519));
        modPages[^1].Types[^1].AddSound("Gun_Inside_mid_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_mid_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_mid_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("50�`75mm:���ԗ�", 670420603));
        modPages[^1].Types[^1].AddSound("Gun_NPC_mid_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_mid_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_mid_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("50�`75mm:���ԗ�-����", 309167407));
        modPages[^1].Types.Add(new("50�`75mm:���ԗ�-����", 161542186));
        modPages[^1].Types.Add(new("85�`107mm:���ԗ�-�ʏ�", 488206709));
        modPages[^1].Types[^1].AddSound("Gun_Outside_main_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_main_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_main_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("85�`107mm:���ԗ�-�Y�[����", 91221195));
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_04.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_main_1p_05.ogg", true, true);
        modPages[^1].Types.Add(new("85�`107mm:���ԗ�", 1023399622));
        modPages[^1].Types[^1].AddSound("Gun_NPC_main_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_main_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_main_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("85�`107mm:���ԗ�-����", 965612953));
        modPages[^1].Types.Add(new("85�`107mm:���ԗ�-����", 513231767));
        modPages[^1].Types.Add(new("115�`152mm:���ԗ�-�ʏ�", 547631281));
        modPages[^1].Types[^1].AddSound("Gun_Outside_large_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_large_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_large_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("115�`152mm:���ԗ�-�Y�[����", 61886891));
        modPages[^1].Types[^1].AddSound("Gun_Inside_large_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_large_1p_02.ogg", true, true);
        modPages[^1].Types.Add(new("115�`152mm:���ԗ�", 619459354));
        modPages[^1].Types[^1].AddSound("Gun_NPC_large_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_large_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_large_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("115�`152mm:���ԗ�-����", 452295751));
        modPages[^1].Types.Add(new("115�`152mm:���ԗ�-����", 1032101855));
        modPages[^1].Types.Add(new("152mm�ȏ�:���ԗ�-�ʏ�", 890327147));
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm�ȏ�:���ԗ�-�Y�[����", 697334890));
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm�ȏ�:���ԗ�", 950138696));
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_3p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm�ȏ�:���ԗ�-����", 809907207));
        modPages[^1].Types.Add(new("152mm�ȏ�:���ԗ�-����", 985913513));
        modPages[^1].Types.Add(new("152mm�ȏ�_Extra:���ԗ�-�ʏ�", 361462963));
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_huge_extra_1p_04.ogg", true, true);
        modPages[^1].Types.Add(new("152mm�ȏ�_Extra:���ԗ�-�Y�[����", 5188110));
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_extra_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_extra_1p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_huge_extra_1p_03.ogg", true, true);
        modPages[^1].Types.Add(new("152mm�ȏ�_Extra:���ԗ�", 349435285));
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_02.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_03.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_huge_extra_3p_04.ogg", true, true);
        modPages[^1].Types.Add(new("152mm�ȏ�_Extra:���ԗ�-����", 483942445));
        modPages[^1].Types.Add(new("152mm�ȏ�_Extra:���ԗ�-����", 632064110));
        modPages[^1].Types.Add(new("���P�b�g:���ԗ�-�ʏ�", 580241268));
        modPages[^1].Types[^1].AddSound("Gun_Outside_Rocket_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Outside_Rocket_1p_02.ogg", true, true);
        modPages[^1].Types.Add(new("���P�b�g:���ԗ�-�Y�[����", 787886846));
        modPages[^1].Types[^1].AddSound("Gun_Inside_Rocket_1p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_Inside_Rocket_1p_02.ogg", true, true);
        modPages[^1].Types.Add(new("���P�b�g:���ԗ�", 872967597));
        modPages[^1].Types[^1].AddSound("Gun_NPC_Rocket_3p_01.ogg", true, true);
        modPages[^1].Types[^1].AddSound("Gun_NPC_Rocket_3p_02.ogg", true, true);
        modPages[^1].Types.Add(new("���P�b�g:���ԗ�-����", 385813932));
        modPages[^1].Types.Add(new("���P�b�g:���ԗ�-����", 410506553));
        //�r���h���X�g
        modPages[^1].BuildList.Add("weapon");
        modPages[^1].BuildList.Add("weapon_basic");
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

            //�Đ������ǂ���
            bool bPlaying = Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING && !bLocationChanging;
            if (bPlaying)
            {
                //���݂̍Đ����Ԃ�\��
                long position = Bass.BASS_ChannelGetPosition(streamHandle);
                PlayTime_S.Value = Bass.BASS_ChannelBytes2Seconds(streamHandle, position);
                PlayTime_T.Text = Sub_Code.Get_Time_String(PlayTime_S.Value) + " / " + maxTime;
            }

            //�Đ����I�������V�[�N�o�[��0�ɖ߂�
            if (bEnded)
            {
                Bass.BASS_ChannelStop(streamHandle);
                PlayTime_S.Value = 0;
                PlayTime_T.Text = "00:00 / " + maxTime;
                bEnded = false;
                bPaused = true;
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
            if (Number >= 200)
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
        if (Mod_Selection_Picker.SelectedIndex == -1)
            return;
        _ = Pause_Volume_Animation(true, 5.0f);
        PlayTime_S.Value = 0;
        PlayTime_S.Maximum = 0;
        PlayTime_T.Text = "00:00 / 00:00";
        maxTime = "00:00";
        Set_Item_Type();
    }

    //�^�C�v���X�g���^�b�v
    private void Other_Type_Tapped(object sender, EventArgs e)
    {
        _ = Pause_Volume_Animation(true, 10.0f);
        Other_Sound_L.ItemsSource = null;
        Other_Sound_L.ItemsSource = ((OtherModType)Other_Type_L.SelectedItem).Sounds;
    }

    //�T�E���h���^�b�v
    private async void Other_Voice_Tapped(object sender, EventArgs e)
    {
        OtherModSound sound = (OtherModSound)Other_Sound_L.SelectedItem;

        //�Đ����̃T�E���h���~
        await Pause_Volume_Animation(true, 10);
        Bass.BASS_StreamFree(streamHandle);

        //�t�@�C�������݂��Ȃ�
        if (!sound.IsAndroidResource && !sound.IsBinarySound && !File.Exists(sound.FilePath))
        {
            Message_Feed_Out("�t�@�C�������݂��܂���B�폜���ꂽ���A�ړ�����Ă���\��������܂��B");
            return;
        }

        //�o�b�t�@�[�T�C�Y��500�ɐݒ�
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);

        //�T�E���h���G���W���ɓǂݍ���
        int baseHandle = 0;
        if (soundPtr.IsAllocated)
            soundPtr.Free();

        if (sound.IsAndroidResource)
        {
            byte[] soundBytes = Sub_Code.ReadResourceData(sound.FilePath);
            soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);
            IntPtr pin = soundPtr.AddrOfPinnedObject();
            baseHandle = Bass.BASS_StreamCreateFile(pin, 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_LOOP);
        }
        else if (sound.IsBinarySound)
        {
            //�T�E���h��.wvs�t�@�C���ɓ����Ă���ꍇ�̓o�C�g�z��ɓǂݍ���ŃG���W���Ƀ|�C���^��n��
            byte[]? soundBytes = wmsLoad.Load_Sound(sound.StreamPosition);
            if (soundBytes != null)
            {
                //soundBytes������ɔj������Ȃ��悤�ɌŒ肳����
                soundPtr = GCHandle.Alloc(soundBytes, GCHandleType.Pinned);

                //�g���q��.flac�̏ꍇ�ʏ�̓ǂݍ��݂ł͕s����ɂȂ邽�ߐ�p�̊֐����Ă�
                if (Path.GetExtension(sound.FilePath) == ".flac")
                    baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
                else
                    baseHandle = Bass.BASS_StreamCreateFile(soundPtr.AddrOfPinnedObject(), 0L, soundBytes.Length, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            }
        }
        else
        {
            if (Path.GetExtension(sound.FilePath) == ".flac")
                baseHandle = BassFlac.BASS_FLAC_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
            else
                baseHandle = Bass.BASS_StreamCreateFile(sound.FilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE);
        }

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

    //���X�g�̏�Ԃ��X�V
    private void Set_Item_Type()
    {
        OtherModType? selectedType = Other_Type_L.SelectedItem as OtherModType;
        Other_Type_L.SelectedItem = null;
        Other_Type_L.ItemsSource = null;
        Other_Type_L.ItemsSource = NowModPage.Types;
        Other_Sound_L.SelectedItem = null;
        Other_Sound_L.ItemsSource = null;

        //���ڂ��I�����ꂽ��Ԃł���΃C�x���g���̃T�E���h�t�@�C�������X�g�ɕ\��
        if (selectedType != null && NowModPage.Types.Contains(selectedType))
        {
            Other_Type_L.SelectedItem = selectedType;
            Other_Sound_L.ItemsSource = selectedType.Sounds;
        }
    }

    //�T�E���h��ǉ�
    public void Add_Sound(List<string> files)
    {
        OtherModType typeList = (OtherModType)Other_Type_L.SelectedItem;
        int alreadyVoiceCount = 0;
        int addedVoiceCount = 0;
        foreach (string filePath in files)
        {
            bool bExist = false;
            //�f�[�^���S�������t�@�C�������݂��邩���ׂ�
            foreach (OtherModSound sound in typeList.Sounds)
            {
                if (sound.FilePath == filePath)
                {
                    bExist = true;
                    alreadyVoiceCount++;
                    break;
                }
            }
            if (bExist)
                continue;

            //�C�x���g�ɃT�E���h��ǉ�
            typeList.Sounds.Add(new(filePath));
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

    //�T�E���h��ǉ�
    private async void Add_Sound_B_Clicked(object? sender, EventArgs e)
    {
        //�G���[���
        if (Other_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("�C�x���g�����I������Ă��܂���B");
            return;
        }

        if (bOtherPageOpened)
            return;

#if ANDROID
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("�A�N�Z�X�����s���Ă��������B");
                return;
            }
#endif

        //�t�@�C���{���̌����������Ă��邩�A�z�[����ʂŃI���W�i���̑I����ʂ�L���ɂ����ꍇ�͂��̑I����ʂŃt�@�C����I��
        if (Sub_Code.IsUseSelectPage)
        {
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //�Ή����Ă���g���q
            Sub_Code.Select_Files_Window.Window_Show("Other_Create", "", extension);    //�I����ʂ�������
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
        if (bOtherPageOpened)
            return;

        if (Other_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("�C�x���g�����I������Ă��܂���B");
            return;
        }
        if (Other_Sound_L.SelectedItem == null)
        {
            Message_Feed_Out("�폜�������T�E���h��I�����Ă��������B");
            return;
        }
        OtherModType type = (OtherModType)Other_Type_L.SelectedItem;
        OtherModSound sound = (OtherModSound)Other_Sound_L.SelectedItem;

        bool result = await DisplayAlert("�m�F", "'" + sound.NameText + "'�����X�g����폜���܂���?", "�͂�", "������");
        if (!result)
            return;

        if (!type.Sounds.Contains(sound))
        {
            Message_Feed_Out("�s���ȃG���[���������܂����B");
            return;
        }

        _ = Pause_Volume_Animation(true, 15.0f);

        type.Sounds.Remove(sound);
        Set_Item_Type();
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

    private async void Save_B_Clicked(object? sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("�Z�[�u", "�v���W�F�N�g�����w�肵�Ă��������B", "����", "�L�����Z��", null, -1, null, projectName);
        if (result != null)
        {
            if (!Sub_Code.IsSafePath(result, true))
            {
                Message_Feed_Out("�G���[:�g�p�ł��Ȃ��������܂܂�Ă��܂��B");
                return;
            }
            else if (File.Exists(Sub_Code.ExDir + "/Saves/" + result + ".wms"))
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

    private async void Load_B_Clicked(object? sender, EventArgs e)
    {
        //���̃E�B���h�E���J����Ă�����X���[
        if (bOtherPageOpened)
            return;

        //�Z�[�u�����Ă���.wvs�t�@�C�����
        List<string> savedNames = [];
        if (!Directory.Exists(Sub_Code.ExDir + "/Saves"))
            _ = Directory.CreateDirectory(Sub_Code.ExDir + "/Saves");
        foreach (string Files in Directory.GetFiles(Sub_Code.ExDir + "/Saves", "*.wms", SearchOption.TopDirectoryOnly))
            savedNames.Add(Path.GetFileNameWithoutExtension(Files));

        //�I����ʂ�\��
        if (savedNames.Count == 0)      //�Z�[�u�t�@�C����1�����݂��Ȃ�
            await DisplayActionSheet("�v���W�F�N�g��I�����Ă��������B", "�L�����Z��", null, ["!�v���W�F�N�g�����݂��܂���!"]);
        else
        {
            string fileName = await DisplayActionSheet("�v���W�F�N�g��I�����Ă��������B", "�L�����Z��", null, [.. savedNames]);
            int index = savedNames.IndexOf(fileName);
            if (index != -1)
            {
                Other_Load_From_File(Sub_Code.ExDir + "/Saves/" + savedNames[index] + ".wms");
                Set_Item_Type();
            }
        }
        savedNames.Clear();
    }

    private void BuildSetting_B_Clicked(object? sender, EventArgs e)
    {
        if (bOtherPageOpened)
            return;

        Sub_Code.BuildSettingWindow.InitializeWMS(modPages, Mod_Selection_Picker.SelectedIndex, wmsLoad);
        Navigation.PushAsync(Sub_Code.BuildSettingWindow);
        bOtherPageOpened = true;
    }

    void SaveProject(string projectName)
    {
        Task.Run(() =>
        {
            WMS_Save save = new();
            save.Add_Data(modPages, wmsLoad, 0.0, false);
            save.Create(Sub_Code.ExDir + "/Saves/" + projectName + ".wms", projectName);

            state = Build_Setting.Build_State.None;
        });
    }

    async void StateLoop(string projectName)
    {
        while (state == Build_Setting.Build_State.Building)
            await Task.Delay(100);

        Other_Load_From_File(Sub_Code.ExDir + "/Saves/" + projectName + ".wms");

        Message_Feed_Out("�Z�[�u���܂����B");
    }

    void Other_Load_From_File(string filePath)
    {
        try
        {
            //������z�u
            InitializePages();
            WMS_Load.WMS_Result wmsResult = wmsLoad.WMS_Load_File(filePath, modPages, out projectName);
            if (wmsResult == WMS_Load.WMS_Result.No_Exist_File)
            {
                Message_Feed_Out("�G���[:�t�@�C�������݂��܂���B");
                return;
            }
            else if (wmsResult == WMS_Load.WMS_Result.Wrong_Header)
            {
                Message_Feed_Out("�G���[:�w�b�_�[���قȂ�܂��B");
                return;
            }
            else if (wmsResult == WMS_Load.WMS_Result.Wrong_Version)
            {
                Message_Feed_Out("�G���[:�Z�[�u�t�@�C���̃o�[�W�������Â����܂��B");
                return;
            }
            else if (wmsResult == WMS_Load.WMS_Result.Wrong_Data)
            {
                Message_Feed_Out("�G���[:�t�@�C�����j�����Ă��܂��B");
                InitializePages();
                return;
            }
            else
                Message_Feed_Out(Path.GetFileNameWithoutExtension(filePath) + "�����[�h���܂����B");
            Set_Item_Type();
        }
        catch (Exception e)
        {
            Message_Feed_Out("�G���[:" + e.Message);
            InitializePages();
            Set_Item_Type();
            wmsLoad.Dispose();
        }
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        Mod_Selection_Picker.SelectedIndex = 0;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bShowing = true;
        bOtherPageOpened = false;
        Loop();
    }

    private void ContentPage_Disappearing(object sender, EventArgs e)
    {
        _ = Pause_Volume_Animation(false);

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

    //�I����Ԃ̃��X�g�̐F��ύX
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

    private void ContentPage_SizeChanged(object sender, EventArgs e)
    {
        Sub_Code.SetListViewHeight(Other_Type_Border, Height);
        Sub_Code.SetListViewHeight(Other_Sound_Border, Height);
    }
}
