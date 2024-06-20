using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public class SE_Sound(string filePath, uint shortID, bool bAndroidResource = false)
{
    //�t�@�C���p�X
    public string? FilePath = filePath;
    public string SoundName => FilePath == null ? "" : Path.GetFileName(FilePath);

    //Actor Audio Mixer���̂ǂ̃R���e�i�ɔz�u���邩
    public uint ShortID = shortID;

    //�f�t�H���g�̃T�E���h���ǂ��� (�f�t�H���g�̏ꍇ���ߍ��݃��\�[�X�̒�����f�[�^���Q�Ƃ��邽�ߍĐ������鎞�̏������قȂ�)
    public bool IsAndroidResource = bAndroidResource;
}

public class SE_Type(string typeName)
{
    //�T�E���h�z��
    public List<SE_Sound> Sounds = [];

    //���ʖ�
    public string TypeName = typeName;
    //����ID
    public uint TypeID = WwiseHash.HashString(typeName);

    //�L�����ǂ���
    public bool IsEnable = true;
}

public class SE_Preset(string presetName)
{
    //SE�̎�� (�ђʎ����j���Ȃ�)
    public List<SE_Type> Types = [];

    //�v���Z�b�g��
    public string PresetName = presetName;
}

public partial class SE_Setting : ContentPage
{
    //���ݑI������Ă���v���Z�b�g
    public SE_Preset NowPreset;

    //�f�t�H���g�̃v���Z�b�g (������Ԃł�NowPreset�ɂ��ꂪ�������)
    readonly SE_Preset defaultPreset;

    //�f�t�H���g��SE�̃t�H���_�p�X
    readonly string defaultPresetPath = Sub_Code.ExDir + "/SE";

    public SE_Setting()
	{
		InitializeComponent();

        //�f�t�H���g�v���Z�b�g���쐬
        defaultPreset = new("�f�t�H���g");
        NowPreset = defaultPreset;
        InitDefaultPreset();

        //�{�^������
        All_Disable_B.Clicked += All_Disable_B_Clicked;
        All_Enable_B.Clicked += All_Enable_B_Clicked;
    }

    void InitDefaultPreset()
    {
        if (!Directory.Exists(defaultPresetPath))
            return;

        defaultPreset.Types.Clear();
        defaultPreset.Types.Add(new("CaptureEnd", 0));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Capture_End_01.mp3"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Capture_End_02.mp3"));
        defaultPreset.Types.Add(new("QuickCommand"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/QuickCommand_01.wav"));
        defaultPreset.Types.Add(new("AmmoDestory"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Danyaku_SE_01.mp3"));
        defaultPreset.Types.Add(new("Destory"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Destroy_01.mp3"));
        defaultPreset.Types.Add(new("Piercing"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Enable_01.mp3"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Enable_02.mp3"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Enable_03.mp3"));
        defaultPreset.Types.Add(new("EnemyModuleDestory"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Enable_Special_01.mp3"));
        defaultPreset.Types.Add(new("RadioDestory"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Musenki_01.mp3"));
        defaultPreset.Types.Add(new("FuelTankDestory"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Nenryou_SE_01.mp3"));
        defaultPreset.Types.Add(new("NotPiercing"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Not_Enable_01.mp3"));
        defaultPreset.Types.Add(new("Ricochet"));
        defaultPreset.Types[defaultPreset.Types.Count - 1].Sounds.Add(new(defaultPresetPath + "/Not_Enable_01.mp3"));
        defaultPreset.Types.Add(new("Reloaded"));
        defaultPreset.Types.Add(new("Lamp"));
        defaultPreset.Types.Add(new("EnemySpoted"));
        defaultPreset.Types.Add(new("Timer"));
        defaultPreset.Types.Add(new("Lock"));
        defaultPreset.Types.Add(new("Unlock"));
        defaultPreset.Types.Add(new("Noise"));
        defaultPreset.Types.Add(new("CrewKilled"));
        defaultPreset.Types.Add(new("ModuleDamaged"));
        defaultPreset.Types.Add(new("ModuleDestroy"));
        defaultPreset.Types.Add(new("ModuleFunctional"));
        defaultPreset.Types.Add(new("StartBattle"));
        defaultPreset.Types.Add(new("EnemyFire"));
        defaultPreset.Types.Add(new("MapClicked"));
        defaultPreset.Types.Add(new("Moving"));
    }

    private void All_Enable_B_Clicked(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void All_Disable_B_Clicked(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SE_Type_L_Tapped(object sender, EventArgs e)
	{

	}

    private void SE_Sound_L_Tapped(object sender, EventArgs e)
    {

    }
}
