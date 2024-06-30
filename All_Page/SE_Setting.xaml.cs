using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass;
using WoTB_Mod_Creator2.Class;
using System.Text;
using System.Runtime.InteropServices;

namespace WoTB_Mod_Creator2.All_Page;

public class SE_Sound(string filePath, uint shortID, bool bAndroidResource = false, bool bDefaultSound = false)
{
    //ファイルパス
    public string FilePath = filePath;
    public string SoundName => FilePath == null ? "" : Path.GetFileName(FilePath);

    //Actor Audio Mixer内のどのコンテナに配置するか
    public uint ShortID = shortID;

    //埋め込みのサウンドかどうか (デフォルトの場合埋め込みリソースの中からデータを参照するため再生させる時の処理が異なる)
    public bool IsAndroidResource = bAndroidResource;
    //WoTBのデフォルトのサウンドかどうか (SE_TypeのIsEnableがfalseの時に再生される)
    public bool IsDefaultSound = bDefaultSound;
}

public class SE_Type(string typeName, uint defaultShortID, double gain)
{
    //サウンド配列
    public List<SE_Sound> Sounds = [];
    //リストに表示するサウンド配列
    public List<SE_Sound> SoundList = [];

    //識別名(リストに表示される名前)
    public string TypeName => typeName + " - " + SoundList.Count + "個";
    public string ConstTypeName => typeName;

    //識別ID
    public uint TypeID = WwiseHash.HashString(typeName);
    //デフォルトの配置場所
    public uint DefaultShortID = defaultShortID;

    //音量の増減 (単位:db)
    public double Gain = gain;

    //有効かどうか
    public bool IsEnable = true;
    //選択済みかどうか
    public bool IsSelected = false;

    //選択時に背景色を変更する
    public Color BackColor => IsSelected ? Color.FromArgb("#82bfc8") : Colors.Transparent;
    //無効時に文字の色を薄くする
    public Color NameColor => !IsEnable ? Color.FromArgb("#BFFF2C8C") : Colors.Aqua;

    //配置するコンテナをデフォルトのコンテナにする場合shortIDは0にする
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
    //SEを削除
    public void DeleteSound(SE_Sound seSound)
    {
        SoundList.Remove(seSound);
        Sounds.Remove(seSound);
    }
    //サウンドをランダムで取得
    public SE_Sound? GetRandomSESound()
    {
        if (SoundList.Count <= 0)
            return null;
        return SoundList[Sub_Code.RandomValue.Next(0, SoundList.Count)];
    }
}

public class SE_Preset(string presetName)
{
    //SEの種類 (貫通時や大破時など)
    public List<SE_Type> Types = [];

    //プリセット名
    public string PresetName = presetName;

    //SE名からSE_Typeを取得
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

    //現在選択されているプリセット
    public SE_Preset NowPreset;

    //デフォルトのプリセット (初期状態ではNowPresetにこれが入れられる)
    readonly SE_Preset defaultPreset;

    readonly List<SE_Preset> presets = [];

    readonly string presetPath = Sub_Code.ExDir + "/Save/SE_Presets.wss";

    //リストを選択したときの色変更用
    ViewCell? lastSoundCell = null;

    GCHandle soundIntPtr = new();

    //再生用ハンドル
    int streamHandle = 0;
    int nowPresetIndex = -1;

    bool bOtherPageOpened = false;
    bool bMessageShowing = false;

    public SE_Setting()
	{
		InitializeComponent();

        //デフォルトプリセットを作成
        defaultPreset = new("標準");
        InitDefaultPreset();
        NowPreset = defaultPreset.Clone();

        Preset_Load();

        UpdateList();

        //ボタン処理
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
        //デフォルトのSEをセット
        //Types[^1]はTypes[defaultPreset.Types.Count - 1]と同じ意味
        //ファイルはResources/Raw内に入っています
        defaultPreset.Types.Clear();
        defaultPreset.Types.Add(new("時間切れ&占領ポイントMax", 206640353, 0));
        defaultPreset.Types[^1].AddSound("Capture_End_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Capture_End_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Capture_Finish_SE.mp3", 0, true, true);
        defaultPreset.Types.Add(new("クイックコマンド", 405861605, 0));
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
        defaultPreset.Types.Add(new("弾薬庫破損", 370075103, -2));
        defaultPreset.Types[^1].AddSound("Ammo_Damaged_01.mp3", 0, true);
        defaultPreset.Types.Add(new("自車両大破", 667880140, 0));
        defaultPreset.Types[^1].AddSound("Destroy_01.mp3", 0, true);
        defaultPreset.Types.Add(new("敵撃破", 582349497, -2));
        defaultPreset.Types[^1].AddSound("EnemyDestory_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("EnemyDestory_02.wav", 0, true);
        defaultPreset.Types.Add(new("貫通", 862763776, -6));
        defaultPreset.Types[^1].AddSound("Piercing_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Piercing_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Piercing_03.mp3", 0, true);
        defaultPreset.Types.Add(new("敵モジュール破損", 876186554, -1));
        defaultPreset.Types[^1].AddSound("Piercing_Special_01.mp3", 0, true);
        defaultPreset.Types.Add(new("敵炎上", 52837378, 0));
        defaultPreset.Types.Add(new("無線機破損", 948692451, 0));
        defaultPreset.Types[^1].AddSound("RadioDamaged_01.mp3", 0, true);
        defaultPreset.Types.Add(new("燃料タンク破損", 602706971, 0));
        defaultPreset.Types[^1].AddSound("FuelTankDamaged_01.mp3", 0, true);
        defaultPreset.Types.Add(new("非貫通-無効弾", 298293840, -1));
        defaultPreset.Types[^1].AddSound("Not_Piercing_01.mp3", 0, true);
        defaultPreset.Types.Add(new("非貫通-跳弾", 568110765, -1));
        defaultPreset.Types[^1].AddSound("Not_Piercing_01.mp3", 0, true);
        defaultPreset.Types.Add(new("装填完了", 769579073, 0));
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
        defaultPreset.Types.Add(new("第六感", 917399664, 0));
        defaultPreset.Types[^1].AddSound("Lamp_SE_01.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Sixth_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Sixth_02.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Sixth_03.mp3", 0, true);
        defaultPreset.Types.Add(new("敵発見", 479275647, 0));
        defaultPreset.Types[^1].AddSound("EnemySpoted_01.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Spot_01.mp3", 0, true);
        defaultPreset.Types[^1].AddSound("Spot_02.wav", 0, true);
        defaultPreset.Types.Add(new("戦闘開始前タイマー", 816581364, 0));
        defaultPreset.Types[^1].AddSound("Timer_SE.mp3", 0, true, true);
        defaultPreset.Types[^1].AddSound("Timer_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("Timer_02.wav", 0, true);
        defaultPreset.Types.Add(new("ロックオン", 391999685, 0));
        defaultPreset.Types[^1].AddSound("target_on_SE_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("Lock_01.mp3", 0, true);
        defaultPreset.Types.Add(new("アンロック", 166694669, 0));
        defaultPreset.Types[^1].AddSound("target_off_SE_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("Unlock_01.mp3", 0, true);
        defaultPreset.Types.Add(new("ノイズ音", 921545948, 0));
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
        defaultPreset.Types.Add(new("搭乗員負傷", 554749525, 0));
        defaultPreset.Types.Add(new("モジュール破損", 7268757, 0));
        defaultPreset.Types[^1].AddSound("ModuleBreak_01.wav", 0, true);
        defaultPreset.Types.Add(new("モジュール大破", 138494260, 0));
        defaultPreset.Types[^1].AddSound("ModuleBreak_01.wav", 0, true);
        defaultPreset.Types.Add(new("モジュール復旧", 554749525, 0));
        defaultPreset.Types.Add(new("戦闘開始", 267487625, 0));
        defaultPreset.Types[^1].AddSound("BattleStart_01.wav", 0, true);
        defaultPreset.Types[^1].AddSound("BattleStart_02.wav", 0, true);
        defaultPreset.Types[^1].AddSound("BattleStart_03.wav", 0, true);
        defaultPreset.Types[^1].AddSound("BattleStart_04.wav", 0, true);
        defaultPreset.Types.Add(new("マップクリック", 951031474, 0));
        defaultPreset.Types[^1].AddSound("Map_Click_01.wav", 0, true, true);
        defaultPreset.Types[^1].AddSound("MapClicked_01.wav", 0, true);
        defaultPreset.Types.Add(new("移動中", 394210856, 0));
        defaultPreset.Types[^1].AddSound("Moving_01.wav", 0, true);
    }

    //画面右下部にメッセージを表示
    private async void Message_Feed_Out(string message)
    {
        //テキストが一定期間経ったらフェードアウト
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

    //リストの状態を更新
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

    //すべてのSEを有効化
    private void All_Enable_B_Clicked(object? sender, EventArgs e)
    {
        foreach (SE_Type seType in NowPreset.Types)
            seType.IsEnable = true;
        UpdateList();
    }

    //すべてのSEを無効化
    private void All_Disable_B_Clicked(object? sender, EventArgs e)
    {
        foreach (SE_Type seType in NowPreset.Types)
            seType.IsEnable = false;
        UpdateList();
    }

    //指定したSEを有効化
    private void Enable_B_Clicked(object? sender, EventArgs e)
    {
        if (SE_Type_L.SelectedItem == null)
            return;

        SE_Type seType = (SE_Type)SE_Type_L.SelectedItem;
        seType.IsEnable = true;

        UpdateList();
    }

    //指定したSEを無効化
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
                Message_Feed_Out("サウンドファイルが見つかりませんでした。");
                Sub_Code.ErrorLogWrite("サウンドファイルが見つかりませんでした。\n場所:" + sound.FilePath);
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
        //エラー回避
        if (SE_Type_L.SelectedItem == null)
        {
            Message_Feed_Out("SEが選択されていません。");
            return;
        }

        if (bOtherPageOpened)
            return;

        //ファイル閲覧の権限を持っているかつ、ホーム画面でオリジナルの選択画面を有効にした場合はその選択画面でファイルを選択
        if (Sub_Code.IsUseSelectPage)
        {
#if ANDROID
            if (!AndroidClass.CheckExternalStoragePermission())
            {
                Message_Feed_Out("アクセス許可を行ってください。");
                return;
            }
#endif
            bOtherPageOpened = true;
            string extension = ".aac|.mp3|.wav|.ogg|.aiff|.flac|.m4a|.mp4";             //対応している拡張子
            Sub_Code.Select_Files_Window.Window_Show("SE_Setting", "", extension);      //選択画面を初期化
            await Navigation.PushModalAsync(Sub_Code.Select_Files_Window);              //選択画面を開く
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
                PickerTitle = "サウンドファイルを選択してください。"
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
            Message_Feed_Out("サウンドが選択されていません。");
            return;
        }

        SE_Sound seSound = (SE_Sound)SE_Sound_L.SelectedItem;

        bool result_01 = await DisplayAlert(seSound.SoundName + "を削除しますか?", null, "はい", "いいえ");
        if (!result_01)
            return;

        SE_Type seType = (SE_Type)SE_Type_L.SelectedItem;

        seType.DeleteSound(seSound);

        UpdateList();

        SE_Sound_L.ItemsSource = null;

        if (seType.SoundList.Count > 0)
            SE_Sound_L.ItemsSource = seType.SoundList;
    }

    //サウンドを追加

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

        //リストUIを更新
        UpdateList();
        SE_Sound_L.ItemsSource = null;
        SE_Sound_L.ItemsSource = seType.SoundList;

        if (alreadyVoiceCount > 0 && addedVoiceCount == 0)
            Message_Feed_Out("既に追加されているため" + alreadyVoiceCount + "個のファイルをスキップしました。");
        else if (alreadyVoiceCount > 0 && addedVoiceCount > 0)
            Message_Feed_Out("既に追加されているため" + alreadyVoiceCount + "個のファイルをスキップし、" + addedVoiceCount + "個のファイルを追加しました。");
        else
            Message_Feed_Out(addedVoiceCount + "個のファイルをイベントに追加しました。");
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
            //既にセーブファイルが存在したら削除 (追記されてしまう場合があるから)
            if (File.Exists(presetPath))
                File.Delete(presetPath);

            //ファイルサイズを小さくするためフォルダ情報はまとめる
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
            bw.Write((byte)header.Length);                      //ヘッダーのサイズ
            bw.Write(header);                                   //ヘッダー
            bw.Write(VERSION);                                  //バージョン(1バイト0～255)
            bw.Write((byte)nowPresetIndex);                     //選択されているプリセットのインデックス(標準は-1)
            bw.Write((byte)dirs.Count);                         //フォルダの数
            foreach (string dir in dirs)
            {
                //フォルダのパス
                byte[] dirBytes = Encoding.UTF8.GetBytes(dir);
                bw.Write((byte)dirBytes.Length);
                bw.Write(dirBytes);
            }
            bw.Write((byte)presets.Count);                      //プリセット数
            bw.Write((byte)0x0a);                               //改行
            foreach (SE_Preset preset in presets)
            {
                byte[] presetName = Encoding.UTF8.GetBytes(preset.PresetName);
                bw.Write((byte)presetName.Length);
                bw.Write(presetName);                                               //プリセット名
                bw.Write((byte)preset.Types.Count);                                 //SEの種類の数 (バージョンによって増減する可能性があるため)
                foreach (SE_Type type in preset.Types)
                {
                    byte[] typeName = Encoding.UTF8.GetBytes(type.ConstTypeName);
                    bw.Write((byte)typeName.Length);
                    bw.Write(typeName);                                             //SE名
                    bw.Write(type.TypeID);                                          //識別ID
                    bw.Write(type.DefaultShortID);                                  //配置するコンテナのID
                    bw.Write((sbyte)type.Gain);                                     //音量の増減 (単位:db)
                    bw.Write(type.IsEnable);                                        //有効かどうか
                    bw.Write((byte)type.Sounds.Count);                              //サウンド数
                    foreach (SE_Sound sound in type.Sounds)
                    {
                        string? dir = Path.GetDirectoryName(sound.FilePath);
                        //フォルダ情報をDirsから参照し、存在すればそのインデックスを保存する。なければ-1
                        if (dir != null && dirs.Contains(dir))
                            bw.Write((sbyte)dirs.IndexOf(dir));
                        else
                            bw.Write((sbyte)-1);
                        byte[] soundName = Encoding.UTF8.GetBytes(sound.SoundName);
                        bw.Write((byte)soundName.Length);
                        bw.Write(soundName);                                        //ファイル名
                        bw.Write(sound.ShortID);                                    //配置するコンテナのID
                        bw.Write(sound.IsAndroidResource);                          //初期で入っているサウンドかどうか
                        bw.Write(sound.IsDefaultSound);                             //WoTB用のデフォルトのサウンドかどうか
                    }
                }
            }
        }
        catch (Exception e)
        {
            Sub_Code.ErrorLogWrite(e.Message);
            Message_Feed_Out("エラーが発生しました。\n" + e.Message);
        }
    }

    private void Preset_Load()
    {
        if (!File.Exists(presetPath))
            return;

        presets.Clear();

        BinaryReader br = new(File.OpenRead(presetPath));
        _ = br.ReadBytes(br.ReadByte());                                        //ヘッダー
        _ = br.ReadByte();                                                      //バージョン
        byte dirCount = br.ReadByte();                                          //フォルダ数
        List<string> dirs = [];
        for (int i = 0; i < dirCount; i++)
            dirs.Add(Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte())));     //フォルダパス
        nowPresetIndex = br.ReadByte();                                         //選択されているプリセットのインデックス
        byte presetCount = br.ReadByte();                                       //プリセット数
        _ = br.ReadByte();                                                      //改行
        for (int i = 0; i < presetCount; i++)
        {
            string presetName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));       //プリセット名
            SE_Preset preset = new(presetName);
            byte typeCount = br.ReadByte();                                                 //SEの種類の数
            for (int j = 0; j < typeCount; j++)
            {
                string typeName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));     //SE名
                uint typeID = br.ReadUInt32();                                              //識別ID
                uint defaultShortID = br.ReadUInt32();                                      //配置するコンテナのID
                sbyte gain = br.ReadSByte();                                                //音量の増減 (単位:db)
                bool bEnable = br.ReadBoolean();                                            //有効かどうか
                SE_Type seType = new(typeName, defaultShortID, gain)
                {
                    TypeID = typeID,
                    IsEnable = bEnable
                };
                byte soundCount = br.ReadByte();                                            //サウンド数
                for (int k = 0; k < soundCount; k++)
                {
                    sbyte dirIndex = br.ReadSByte();                                        //フォルダパスのインデックス
                    string dir = "";
                    //-1でなければフォルダパスからフォルダ情報を取得
                    if (dirIndex != -1)
                        dir = dirs[dirIndex] + "/";
                    string soundName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    string filePath = dir + soundName;
                    uint shortID = br.ReadUInt32();                                         //配置するコンテナのID
                    bool bAndroidResource = br.ReadBoolean();                               //初期で入っているサウンドかどうか
                    bool bDefaultSound = br.ReadBoolean();                                  //WoTB用のデフォルトのサウンドかどうか
                    seType.AddSound(filePath, shortID, bAndroidResource, bDefaultSound);    //リストに追加
                }
                preset.Types.Add(seType);                                       //リストに追加
            }
            presets.Add(preset);                                //プリセットをリストに追加
        }

        //前回選択していたプリセットをロード
        if (nowPresetIndex != -1)
            NowPreset = presets[nowPresetIndex];
    }

    private async void Preset_Load_B_Clicked(object? sender, EventArgs e)
    {
        List<string> presetNames = [];
        presetNames.Add("標準");
        foreach (SE_Preset preset in presets)
            presetNames.Add(preset.PresetName);
        string selectedPreset = await DisplayActionSheet("プリセットを選択してください。", "キャンセル", null, [.. presetNames]);
        int index = presetNames.IndexOf(selectedPreset);
        if (index != -1)
        {
            if (selectedPreset == "標準")
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
            Message_Feed_Out("'" + selectedPreset + "'をロードしました。");
        }
    }

    private async void Preset_Save_B_Clicked(object? sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("セーブ", "プリセット名を指定してください。", "決定", "キャンセル", null, -1, null, NowPreset.PresetName);
        if (result != null)
        {
            if (!Sub_Code.IsSafePath(result, true))
            {
                Message_Feed_Out("エラー:使用できない文字が含まれています。");
                return;
            }
            if (result == "標準")
            {
                Message_Feed_Out("標準プリセットは上書きできません。");
                return;
            }
            int index = -1;
            for (int i = 0; i < index; i++)
            {
                if (presets[i].PresetName == result)
                {
                    bool result_01 = await DisplayAlert("プリセットを上書きしますか?", null, "はい", "いいえ");
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
            Message_Feed_Out("プリセットをセーブしました。");
        }
    }

    private void SE_Type_L_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        //既に選択済みの場合スキップ
        if (e.SelectedItemIndex != -1)
            if (NowPreset.Types[e.SelectedItemIndex].IsSelected)
                return;

        //未選択状態にする
        foreach (SE_Type seType in NowPreset.Types)
            seType.IsSelected = false;

        //その項目のSE_Typeを選択済みに
        if (e.SelectedItemIndex != -1)
        {
            NowPreset.Types[e.SelectedItemIndex].IsSelected = true;
        }

        //SEリストを更新
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
