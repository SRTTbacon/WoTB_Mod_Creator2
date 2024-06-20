using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public class SE_Sound(string filePath, uint shortID, bool bAndroidResource = false)
{
    //ファイルパス
    public string? FilePath = filePath;
    public string SoundName => FilePath == null ? "" : Path.GetFileName(FilePath);

    //Actor Audio Mixer内のどのコンテナに配置するか
    public uint ShortID = shortID;

    //デフォルトのサウンドかどうか (デフォルトの場合埋め込みリソースの中からデータを参照するため再生させる時の処理が異なる)
    public bool IsAndroidResource = bAndroidResource;
}

public class SE_Type(string typeName)
{
    //サウンド配列
    public List<SE_Sound> Sounds = [];

    //識別名
    public string TypeName = typeName;
    //識別ID
    public uint TypeID = WwiseHash.HashString(typeName);

    //有効かどうか
    public bool IsEnable = true;
}

public class SE_Preset(string presetName)
{
    //SEの種類 (貫通時や大破時など)
    public List<SE_Type> Types = [];

    //プリセット名
    public string PresetName = presetName;
}

public partial class SE_Setting : ContentPage
{
    //現在選択されているプリセット
    public SE_Preset NowPreset;

    //デフォルトのプリセット (初期状態ではNowPresetにこれが入れられる)
    readonly SE_Preset defaultPreset;

    //デフォルトのSEのフォルダパス
    readonly string defaultPresetPath = Sub_Code.ExDir + "/SE";

    public SE_Setting()
	{
		InitializeComponent();

        //デフォルトプリセットを作成
        defaultPreset = new("デフォルト");
        NowPreset = defaultPreset;
        InitDefaultPreset();

        //ボタン処理
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
