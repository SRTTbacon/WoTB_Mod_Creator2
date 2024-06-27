using System.Text;
using WoTB_Mod_Creator2.All_Page;

namespace WoTB_Mod_Creator2.Class
{
    //Mod Creator専用のセーブファイルをバイナリデータとして作成
    //このセーブファイルは、サウンドファイルも一緒に書き込まれるため、別のPC、Android端末でロードしても正常に再生やModの作成が行えます。
    public class WVS_Save
    {
        //ヘッダ情報(WVSFormatの9バイトとバージョン情報の2バイトは確定)
        public const string WVSHeader = "WVSFormat";
        public const ushort WVSVersion = 5;

        List<List<CVoiceTypeList>> blitzEvents = [];
        readonly List<CVoiceSoundSetting> soundFiles = [];
        readonly List<int> soundIndexes = [];
        readonly List<ushort> listIndexes = [];
        readonly List<byte[]> soundBinaryes = [];

        //WVS_Fileは、.wvsファイルがロードされていない場合はnullを指定します。
        //ここでWVS_Fileを指定かつ、セーブファイルを上書きする場合はCreate()を実行する前に必ずWVS_File.Dispose()を実行する必要があります。
        public void Add_Sound(List<List<CVoiceTypeList>> blitzEvents, WVS_Load wvsFile)
        {
            this.blitzEvents = blitzEvents;
            for (int i = 0; i < blitzEvents.Count; i++)
            {
                for (int j = 0; j < blitzEvents[i].Count; j++)
                {
                    for (int k = 0; k < blitzEvents[i][j].TypeSetting.Sounds.Count; k++)
                    {
                        //サウンド設定のみ抽出
                        soundFiles.Add(blitzEvents[i][j].TypeSetting.Sounds[k]);
                        soundIndexes.Add(j);
                        listIndexes.Add((ushort)i);
                        //ロード済みの.wvsファイル内に同じサウンドがあれば配列にbyte[]を追加
                        if (wvsFile != null && wvsFile.IsLoaded && (!blitzEvents[i][j].TypeSetting.Sounds[k].FilePath.Contains('\\') || !blitzEvents[i][j].TypeSetting.Sounds[k].FilePath.Contains('/')))
                        {
                            byte[]? soundData = wvsFile.Load_Sound(blitzEvents[i][j].TypeSetting.Sounds[k].StreamPosition);
                            if (soundData != null)
                                soundBinaryes.Add(soundData);
                            else
                                soundBinaryes.Add([]);
                        }
                        else
                            soundBinaryes.Add([]);
                    }
                }
            }
        }

        //.wvsファイルを生成
        public void Create(string toFile, string projectName, bool bIncludeSE, SE_Preset? sePreset = null)
        {
            //同名のファイルが存在する場合、削除する
            if (File.Exists(toFile))
                File.Delete(toFile);

            //サウンドが存在する階層を保存 (ファイルサイズ削減のため)
            List<string> dirNames = [];

            BinaryWriter bw = new(File.OpenWrite(toFile));
            //ヘッダー
            bw.Write(Encoding.ASCII.GetBytes(WVSHeader));
            //謎の4バイト
            bw.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            //WVSファイルのバージョン
            bw.Write(WVSVersion);
            //WoT用のセーブファイルかどうか (Android版はWoTB用のため常にfalse)
            bw.Write(false);
            //サウンド数
            bw.Write((ushort)soundFiles.Count);
            //プロジェクト名のバイト配列を保存
            byte[] Project_Name_Byte = Encoding.UTF8.GetBytes(projectName);
            bw.Write((byte)Project_Name_Byte.Length);
            bw.Write(Project_Name_Byte);
            //命名変更が可能かどうか (Android版は常にtrue)
            bw.Write(true);
            //サウンドファイルを内包するかどうか (Android版は常にtrue)
            bw.Write(true);
            //改行1バイト
            bw.Write((byte)0x0a);
            //音声のイベント数
            bw.Write((byte)blitzEvents.Count);
            for (int typeIndex = 0; typeIndex < blitzEvents.Count; typeIndex++)
            {
                //イベント内のサウンド数
                bw.Write((byte)blitzEvents[typeIndex].Count);
                for (int eventIndex = 0; eventIndex < blitzEvents[typeIndex].Count; eventIndex++)
                {
                    CVoiceTypeSetting eventInfo = blitzEvents[typeIndex][eventIndex].TypeSetting;
                    //音量、ピッチ、エフェクトなどの情報を保存
                    bw.Write(eventInfo.IsVolumeRange);
                    bw.Write(eventInfo.IsPitchRange);
                    bw.Write(eventInfo.IsLPFRange);
                    bw.Write(eventInfo.IsHPFRange);
                    bw.Write(eventInfo.Volume);
                    bw.Write((sbyte)eventInfo.Pitch);
                    bw.Write((sbyte)eventInfo.LowPassFilter);
                    bw.Write((sbyte)eventInfo.HighPassFilter);
                    bw.Write(eventInfo.VolumeRange.Start);
                    bw.Write(eventInfo.VolumeRange.End);
                    bw.Write((sbyte)eventInfo.PitchRange.Start);
                    bw.Write((sbyte)eventInfo.PitchRange.End);
                    bw.Write((sbyte)eventInfo.LPFRange.Start);
                    bw.Write((sbyte)eventInfo.LPFRange.End);
                    bw.Write((sbyte)eventInfo.HPFRange.Start);
                    bw.Write((sbyte)eventInfo.HPFRange.End);
                    bw.Write(eventInfo.Delay);
                    bw.Write((byte)eventInfo.LimitSoundInstance);
                    bw.Write((byte)eventInfo.WhenLimitReached);
                    bw.Write((byte)eventInfo.WhenPriorityEqual);
                    if (eventIndex == 17)
                        bw.Write((byte)0x0a);
                }
                bw.Write((byte)0x0a);
            }
            //各サウンドの情報を保存
            for (int i = 0; i < soundFiles.Count; i++)
            {
                bw.Write((byte)listIndexes[i]);
                bw.Write((byte)soundIndexes[i]);
                byte[] fileNameByte = Encoding.UTF8.GetBytes(Path.GetFileName(soundFiles[i].FilePath));
                bw.Write((byte)fileNameByte.Length);
                bw.Write(fileNameByte);
                bw.Write((byte)0x0a);
                CVoiceSoundSetting soundInfo = soundFiles[i];
                //音量、ピッチ、エフェクトなどの情報を保存
                bw.Write(soundInfo.Probability);
                bw.Write(soundInfo.PlayTime.Start);
                bw.Write(soundInfo.PlayTime.End);
                bw.Write(soundInfo.IsVolumeRange);
                bw.Write(soundInfo.IsPitchRange);
                bw.Write(soundInfo.IsLPFRange);
                bw.Write(soundInfo.IsHPFRange);
                bw.Write(soundInfo.Volume);
                bw.Write((sbyte)soundInfo.Pitch);
                bw.Write((sbyte)soundInfo.LowPassFilter);
                bw.Write((sbyte)soundInfo.HighPassFilter);
                bw.Write(soundInfo.VolumeRange.Start);
                bw.Write(soundInfo.VolumeRange.End);
                bw.Write((sbyte)soundInfo.PitchRange.Start);
                bw.Write((sbyte)soundInfo.PitchRange.End);
                bw.Write((sbyte)soundInfo.LPFRange.Start);
                bw.Write((sbyte)soundInfo.LPFRange.End);
                bw.Write((sbyte)soundInfo.HPFRange.Start);
                bw.Write((sbyte)soundInfo.HPFRange.End);
                bw.Write(soundInfo.Delay);
                bw.Write(soundInfo.IsFadeIn);
                bw.Write(soundInfo.IsFadeOut);

                //サウンドのバイト数
                if (soundBinaryes[i].Length > 0)
                {
                    bw.Write(soundBinaryes[i].Length);
                    bw.Write(soundBinaryes[i]);
                }
                else
                {
                    byte[] Sound_File_Bytes = File.ReadAllBytes(soundFiles[i].FilePath);
                    bw.Write(Sound_File_Bytes.Length);
                    bw.Write(Sound_File_Bytes);
                }
            }
            //SE情報も保存 (Android版かつビルド時のみ)
            bw.Write(bIncludeSE && sePreset != null);
            if (bIncludeSE && sePreset != null)
            {
                //どのイベントがどのSEを使うか (SEを使用しないイベントは0を入力)
                foreach (List<CVoiceTypeList> types in blitzEvents)
                {
                    foreach (CVoiceTypeList type in types)
                    {
                        if (type.TypeSetting.SEType != null)
                            bw.Write(type.TypeSetting.SEType.TypeID);
                        else
                            bw.Write((uint)0);
                    }
                }

                //SEを保存 (サウンド含む)
                bw.Write((byte)sePreset.Types.Count);
                foreach (SE_Type seType in sePreset.Types)
                {
                    bw.Write(seType.TypeID);                //識別ID
                    bw.Write((sbyte)seType.Gain);           //音量の増減 (0がデフォルト)
                    bw.Write(seType.IsEnable);              //プリセットに指定されたサウンドを使用するか(falseの場合WoTBデフォルトのサウンドが入る)
                    bw.Write((byte)seType.Sounds.Count);    //サウンド数
                    foreach (SE_Sound seSound in seType.Sounds)
                    {
                        bw.Write(seSound.ShortID);          //どのコンテナに配置するか
                        bw.Write(seSound.IsDefaultSound);   //WoTBデフォルトのサウンドかどうか
                        if (seSound.IsAndroidResource)          //アプリに埋め込まれているサウンドの場合
                        {
                            bw.Write(true);
                            byte[] soundBytes = Sub_Code.ReadResourceData(seSound.FilePath);    //埋め込みリソースからサウンドを取得
                            bw.Write(soundBytes.Length);
                            bw.Write(soundBytes);
                        }
                        else if (File.Exists(seSound.FilePath)) //ユーザーが選択したサウンドファイルの場合
                        {
                            bw.Write(true);
                            byte[] soundBytes = File.ReadAllBytes(seSound.FilePath);
                            bw.Write(soundBytes.Length);
                            bw.Write(soundBytes);
                        }
                        else
                            bw.Write(false);        //ファイルが存在しない場合
                    }
                }
            }
            bw.Close();
            dirNames.Clear();
        }

        //解放
        public void Dispose()
        {
            soundFiles.Clear();
            soundIndexes.Clear();
            listIndexes.Clear();
            soundBinaryes.Clear();
        }
    }

    //.wvsファイルをロード
    public class WVS_Load
    {
        //ロード結果
        public enum WVS_Result
        {
            OK,             //正常にロード
            Wrong_Version,  //処理できないほど古いバージョン
            Wrong_Header,   //ヘッダーが異なる (そもそも.wvsファイルではない)
            No_Exist_File,  //ファイルが存在しない
            WoTMode,        //WoT用のセーブファイル
            BlitzMode       //WoTB用のセーブファイル
        }

        public string ProjectName { get; private set; } = "";
        public string WVSFile { get; private set; } = "";
        public bool IsNotChangeNameMode { get; private set; }
        public bool IsIncludedSound = true;
        public bool IsLoaded = false;

        private const byte MAX_VERSION = 5;         //この値はPC版Mod Creatorと同じにする必要あり
        private BinaryReader? bin = null;

        //WoTB用のセーブファイルか確認 (そうであればtrue)
        public static WVS_Result IsBlitzWVSFile(string filePath)
        {
            //ファイルが存在するか
            if (!File.Exists(filePath))
                return WVS_Result.No_Exist_File;

            //バイナリデータとして読み取る
            BinaryReader bin = new(File.OpenRead(filePath));
            //ヘッダー
            string header = Encoding.ASCII.GetString(bin.ReadBytes(9));
            bin.ReadBytes(4);
            //バージョン
            ushort version = bin.ReadUInt16();
            //WoT用かどうか
            bool bWoTMode = false;
            if (version >= 4)
                bWoTMode = bin.ReadBoolean();
            bin.Close();

            //ロード結果
            if (version > MAX_VERSION)
                return WVS_Result.Wrong_Version;
            if (header != "WVSFormat")
                return WVS_Result.Wrong_Header;
            if (bWoTMode)
                return WVS_Result.WoTMode;
            return WVS_Result.OK;
        }
        //WVSファイルをロードします
        //この時点ではサウンドを読み込まないため、サウンドを取得するときはGet_Sound_Bytes()を実行する必要があります。(メモリ使用率を抑えるため)
        public WVS_Result WVS_Load_File(string filePath, List<List<CVoiceTypeList>> voiceTypes)
        {
            //ファイルが存在しない場合終了
            if (!File.Exists(filePath))
                return WVS_Result.No_Exist_File;

            //既に何かロードされていれば解放
            Dispose();

            //サウンド情報をクリア
            for (int i = 0; i < voiceTypes.Count; i++)
                for (int j = 0; j < voiceTypes[i].Count; j++)
                    voiceTypes[i][j].TypeSetting.Sounds.Clear();

            //サウンドが存在する階層
            List<string> dirNames = [];

            //.wvsファイルをバイナリデータとして読み取る
            bin = new BinaryReader(File.OpenRead(filePath));

            //ヘッダーが異なれば終了
            if (Encoding.ASCII.GetString(bin.ReadBytes(9)) != "WVSFormat")
            {
                bin.Close();
                return WVS_Result.Wrong_Header;
            }

            //4バイトスキップ
            bin.ReadBytes(4);
            //セーブファイルのバージョン
            ushort version = bin.ReadUInt16();
            if (version >= 4)
            {
                bool bWoTMode = bin.ReadBoolean();
                if (bWoTMode)
                {
                    bin.Close();
                    return WVS_Result.WoTMode;
                }
            }
            if (version < 4)
            {
                bin.Close();
                return WVS_Result.Wrong_Version;
            }
            //ファイル内のサウンド数
            ushort soundCount = bin.ReadUInt16();
            //プロジェクト名のバイト数
            int projectNameBytes = version < 3 ? bin.ReadInt32() : bin.ReadByte();
            ProjectName = Encoding.UTF8.GetString(bin.ReadBytes(projectNameBytes));
            //プロジェクト名の変更が可能かどうかを取得
            IsNotChangeNameMode = bin.ReadBoolean();
            IsIncludedSound = bin.ReadBoolean();
            bin.ReadByte();
            if (!IsIncludedSound && version >= 3)
            {
                ushort dirCount = bin.ReadUInt16();
                for (int i = 0; i < dirCount; i++)
                    dirNames.Add(Encoding.UTF8.GetString(bin.ReadBytes(bin.ReadByte())));
                bin.ReadByte();
            }
            try
            {
                byte typeCount = bin.ReadByte();
                for (int typeIndex = 0; typeIndex < typeCount; typeIndex++)
                {
                    //イベント数繰り返す
                    byte eventCount = bin.ReadByte();
                    for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
                    {
                        //CVoiceTypeSettingにサウンド情報をロード
                        CVoiceTypeSetting eventInfo = voiceTypes[typeIndex][eventIndex].TypeSetting;
                        eventInfo.IsLoadMode = true;
                        eventInfo.IsVolumeRange = bin.ReadBoolean();
                        eventInfo.IsPitchRange = bin.ReadBoolean();
                        eventInfo.IsLPFRange = bin.ReadBoolean();
                        eventInfo.IsHPFRange = bin.ReadBoolean();
                        eventInfo.Volume = bin.ReadDouble();
                        eventInfo.Pitch = bin.ReadSByte();
                        eventInfo.LowPassFilter = bin.ReadSByte();
                        eventInfo.HighPassFilter = bin.ReadSByte();
                        eventInfo.VolumeRange.Start = bin.ReadDouble();
                        eventInfo.VolumeRange.End = bin.ReadDouble();
                        eventInfo.PitchRange.Start = bin.ReadSByte();
                        eventInfo.PitchRange.End = bin.ReadSByte();
                        eventInfo.LPFRange.Start = bin.ReadSByte();
                        eventInfo.LPFRange.End = bin.ReadSByte();
                        eventInfo.HPFRange.Start = bin.ReadSByte();
                        eventInfo.HPFRange.End = bin.ReadSByte();
                        eventInfo.Delay = bin.ReadDouble();
                        eventInfo.LimitSoundInstance = bin.ReadByte();
                        eventInfo.WhenLimitReached = bin.ReadByte();
                        eventInfo.WhenPriorityEqual = bin.ReadByte();
                        if (eventIndex == 17)
                            _ = bin.ReadByte();
                        voiceTypes[typeIndex][eventIndex].TypeSetting = eventInfo;
                    }
                    _ = bin.ReadByte();
                }
                for (ushort i = 0; i < soundCount; i++)
                {
                    //イベントのタイプ(のインデックス)
                    ushort listIndex;
                    //サウンドのインデックス
                    int soundIndex;
                    //ファイル名の長さ
                    int fileNameCount;
                    //ファイル名
                    string fileName = "";
                    if (version < 3)
                    {
                        listIndex = bin.ReadUInt16();
                        soundIndex = bin.ReadInt32();
                        fileNameCount = bin.ReadInt32();
                    }
                    else
                    {
                        listIndex = bin.ReadByte();
                        soundIndex = bin.ReadByte();
                        if (!IsIncludedSound)
                            fileName = dirNames[bin.ReadUInt16()] + "\\";
                        fileNameCount = bin.ReadByte();
                    }
                    fileName += Encoding.UTF8.GetString(bin.ReadBytes(fileNameCount));
                    //謎の1バイト
                    bin.ReadByte();
                    CVoiceSoundSetting soundInfo = new();
                    //WVSファイルのバージョンが2以降であればサウンド設定を取得
                    if (version >= 2)
                    {
                        soundInfo.FilePath = fileName;
                        soundInfo.Probability = bin.ReadDouble();
                        soundInfo.PlayTime.Start = bin.ReadDouble();
                        soundInfo.PlayTime.End = bin.ReadDouble();
                        soundInfo.IsVolumeRange = bin.ReadBoolean();
                        soundInfo.IsPitchRange = bin.ReadBoolean();
                        soundInfo.IsLPFRange = bin.ReadBoolean();
                        soundInfo.IsHPFRange = bin.ReadBoolean();
                        soundInfo.Volume = bin.ReadDouble();
                        soundInfo.Pitch = bin.ReadSByte();
                        soundInfo.LowPassFilter = bin.ReadSByte();
                        soundInfo.HighPassFilter = bin.ReadSByte();
                        soundInfo.VolumeRange.Start = bin.ReadDouble();
                        soundInfo.VolumeRange.End = bin.ReadDouble();
                        soundInfo.PitchRange.Start = bin.ReadSByte();
                        soundInfo.PitchRange.End = bin.ReadSByte();
                        soundInfo.LPFRange.Start = bin.ReadSByte();
                        soundInfo.LPFRange.End = bin.ReadSByte();
                        soundInfo.HPFRange.Start = bin.ReadSByte();
                        soundInfo.HPFRange.End = bin.ReadSByte();
                        soundInfo.Delay = bin.ReadDouble();
                        soundInfo.IsFadeIn = bin.ReadBoolean();
                        soundInfo.IsFadeOut = bin.ReadBoolean();
                        if (IsIncludedSound)
                        {
                            //サウンドが開始される地点を保存しておく(Get_Sound_Bytes()でその地点のサウンドをbyte[]形式で読み取れます)
                            soundInfo.StreamPosition = bin.BaseStream.Position;
                            int soundLength = bin.ReadInt32();
                            bin.BaseStream.Seek(soundLength, SeekOrigin.Current);
                            soundInfo.FilePath = Path.GetFileName(fileName);
                        }
                    }
                    else if (IsIncludedSound)
                    {
                        //サウンドが開始される地点を保存しておく(Get_Sound_Bytes()でその地点のサウンドをbyte[]形式で読み取れます)
                        soundInfo.StreamPosition = bin.BaseStream.Position;
                        //サウンドの長さを取得し、次のサウンドの位置までスキップ
                        int soundLength = bin.ReadInt32();
                        bin.BaseStream.Seek(soundLength, SeekOrigin.Current);
                        soundInfo.FilePath = Path.GetFileName(fileName);
                    }
                    voiceTypes[listIndex][soundIndex].TypeSetting.Sounds.Add(soundInfo);
                }
                WVSFile = filePath;
                IsLoaded = true;
                return WVS_Result.OK;
            }
            catch
            {
                WVSFile = "";
                IsLoaded = false;
                bin.Close();
                return WVS_Result.Wrong_Version;
            }
        }

        //.wvsファイル内のサウンドデータを取得
        public byte[]? Load_Sound(long startPosition)
        {
            if (bin == null)
                return null;
            //サウンドのバイト数を取得し、その長さぶん読み取る
            bin.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            int soundLength = bin.ReadInt32();
            return bin.ReadBytes(soundLength);
        }

        //.wvsファイル内のサウンドデータをファイルに保存
        public bool Sound_To_File(CVoiceSoundSetting setting, string toFile)
        {
            if (IsLoaded && setting.StreamPosition > 0)
            {
                try
                {
                    byte[]? soundBytes = Load_Sound(setting.StreamPosition);
                    if (soundBytes != null)
                        File.WriteAllBytes(toFile, soundBytes);
                    return true;
                }
                catch (Exception e)
                {
                    Sub_Code.ErrorLogWrite(e.Message);
                }
            }
            return false;
        }

        //解放
        public void Dispose()
        {
            if (bin != null)
            {
                bin.Close();
                bin = null;
            }
            WVSFile = "";
            IsLoaded = false;
        }
    }
}
