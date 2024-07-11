using System.Security.Cryptography;
using System.Text;
using WoTB_Mod_Creator2.All_Page;

namespace WoTB_Mod_Creator2.Class
{
    //Mod Creator専用のセーブファイルをバイナリデータとして作成
    //このセーブファイルはサウンドデータも一緒に書き込まれるため、別のPC、Android端末でロードしても正常にModの作成が行えます。(バージョン5以降対応していれば)
    public class WVS_Save
    {
        enum Format
        {
            AndroidResorce,
            WVSLoad,
            FilePath
        }

        class SaveFormat(Format format, string filePath, long streamPosition = 0)
        {
            public Format format = format;

            public string filePath = filePath;

            public long streamPosition = streamPosition;

            public byte[] md5 = [];
        }

        //ヘッダ情報(WVSFormatの9バイトとバージョン情報の2バイトは確定)
        public const string WVSHeader = "WVSFormat";
        public const ushort WVSVersion = 5;

        List<List<CVoiceTypeList>> blitzEvents = [];
        WVS_Load? wvsFile;

        double volume = 0.0;
        bool bDefaultVoiceMode = false;

        //wvsFileは、.wvsファイルがロードされていない場合はnullを指定します。
        //ここでwvsFileを指定かつ、セーブファイルを上書きする場合はCreate()を実行する前に必ずwvsFile.Dispose()を実行する必要があります。
        public void Add_Sound(List<List<CVoiceTypeList>> blitzEvents, WVS_Load wvsFile, double volume = 0.0, bool bDefaultVoiceMode = false)
        {
            this.blitzEvents = blitzEvents;
            this.wvsFile = wvsFile;
            this.volume = volume;
            this.bDefaultVoiceMode = bDefaultVoiceMode;
        }

        //サウンド同士を比較し、同じサウンドであればtrueを返す
        private static bool CompareBytes(byte[] lhs, byte[] rhs)
        {
            //MD5ハッシュを取る
            byte[] hash2 = MD5.HashData(rhs);

            //ハッシュを比較
            return lhs.SequenceEqual(hash2);
        }

        //.wvsファイルを生成
        public void Create(string toFile, string projectName, bool bIncludeSE, SE_Preset? sePreset = null, bool bUnloadWVSLoad = true)
        {
            //サウンドが存在する階層を保存 (ファイルサイズ削減のため)
            List<string> dirNames = [];

            List<SaveFormat> saveFormats = [];

            if (File.Exists(toFile + ".tmp"))
                File.Delete(toFile + ".tmp");

            BinaryWriter bw = new(File.OpenWrite(toFile + ".tmp"));
            //ヘッダー
            bw.Write(Encoding.ASCII.GetBytes(WVSHeader));
            //謎の4バイト
            bw.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            //WVSファイルのバージョン
            bw.Write(WVSVersion);
            //WoT用のセーブファイルかどうか (Android版はWoTB用のため常にfalse)
            bw.Write(false);
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

            bw.Write(volume);
            bw.Write(bDefaultVoiceMode);
            //SE情報を保存 (Android版かつビルド時のみ)
            bw.Write(bIncludeSE && sePreset != null);
            if (bIncludeSE && sePreset != null)
            {
                //SEを保存 (サウンド含む)
                bw.Write((byte)sePreset.Types.Count);
                foreach (SE_Type seType in sePreset.Types)
                {
                    bw.Write(seType.TypeID);                //識別ID
                    bw.Write((sbyte)seType.Gain);           //音量の増減 (0がデフォルト)
                    bw.Write(seType.IsEnable);              //プリセットに指定されたサウンドを使用するか(falseの場合WoTBデフォルトのサウンドが入る)
                    bw.Write(seType.IsLoop);                //戦闘開始タイマーなどループするかどうか
                    bw.Write((byte)seType.Sounds.Count);    //サウンド数
                    foreach (SE_Sound seSound in seType.Sounds)
                    {
                        byte[] seName = Encoding.UTF8.GetBytes(Path.GetFileName(seSound.FilePath));
                        bw.Write((byte)seName.Length);
                        bw.Write(seName);
                        bw.Write(seSound.ShortID);          //どのコンテナに配置するか
                        bw.Write(seSound.IsDefaultSound);   //WoTBデフォルトのサウンドかどうか

                        byte[] soundBytes = [];

                        if (seSound.IsAndroidResource)          //アプリに埋め込まれているサウンドの場合
                            soundBytes = Sub_Code.ReadResourceData(seSound.FilePath);    //埋め込みリソースからサウンドを取得
                        else if (File.Exists(seSound.FilePath)) //ユーザーが選択したサウンドファイルの場合
                            soundBytes = File.ReadAllBytes(seSound.FilePath);

                        int number = saveFormats.Count;
                        bool bExist = false;

                        for (int i = 0; i < saveFormats.Count; i++)
                        {
                            if (CompareBytes(saveFormats[i].md5, soundBytes))
                            {
                                number = i;
                                bExist = true;
                                break;
                            }
                        }
                        if (!bExist)
                        {
                            if (seSound.IsAndroidResource)
                                saveFormats.Add(new(Format.AndroidResorce, seSound.FilePath));
                            else
                                saveFormats.Add(new(Format.FilePath, seSound.FilePath));
                            saveFormats[^1].md5 = MD5.HashData(soundBytes);
                        }
                        bw.Write((ushort)number);       //サウンドデータが入っているインデックス
                    }
                }
            }

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
                    bw.Write(eventInfo.EventShortID);
                    bw.Write(eventInfo.VoiceShortID);
                    byte[] voiceName = Encoding.UTF8.GetBytes(eventInfo.DefaultVoiceName);
                    bw.Write((byte)voiceName.Length);
                    bw.Write(voiceName);
                    bool IsIncludeSEType = eventInfo.SEType != null;
                    bw.Write(IsIncludeSEType);
                    if (eventInfo.SEType != null)
                        bw.Write(eventInfo.SEType.TypeID);
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
                    //横長になるため3個イベントが進むたびに改行
                    if (eventIndex % 5 == 3)
                        bw.Write((byte)0x0a);
                    bw.Write((byte)eventInfo.Sounds.Count);
                    for (int soundIndex = 0; soundIndex < eventInfo.Sounds.Count; soundIndex++)
                    {
                        CVoiceSoundSetting soundInfo = eventInfo.Sounds[soundIndex];
                        //名前、音量、ピッチ、エフェクトなどの情報を保存
                        byte[] fileNameByte = Encoding.UTF8.GetBytes(Path.GetFileName(soundInfo.FilePath));
                        bw.Write((byte)fileNameByte.Length);
                        bw.Write(fileNameByte);
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

                        //既に同じサウンドが設定されていればそのインデックスを取得。なければCount
                        //貫通と致命弾は同じサウンドを使用する可能性があるため容量削減のため同じサウンドの場合はまとめる
                        int number = saveFormats.Count;
                        bool bExist = false;
                        byte[] soundBytes;
                        if (soundInfo.IsBinarySound && wvsFile != null)
                            soundBytes = wvsFile.Load_Sound(soundInfo.StreamPosition);
                        else
                            soundBytes = File.ReadAllBytes(soundInfo.FilePath);
                        for (int i = 0; i < saveFormats.Count; i++)
                        {
                            if (CompareBytes(saveFormats[i].md5, soundBytes))
                            {
                                number = i;
                                bExist = true;
                                break;
                            }
                        }
                        if (!bExist)
                        {
                            if (soundInfo.IsBinarySound && wvsFile != null)
                                saveFormats.Add(new(Format.WVSLoad, "", soundInfo.StreamPosition));
                            else
                                saveFormats.Add(new(Format.FilePath, soundInfo.FilePath));
                            saveFormats[^1].md5 = MD5.HashData(soundBytes);
                        }
                        bw.Write((ushort)number);       //サウンドデータが入っているインデックス

                        //横長になるため3個イベントが進むたびに改行
                        if (soundIndex % 5 == 3)
                            bw.Write((byte)0x0a);
                    }
                }
                bw.Write((byte)0x0a);
            }

            bw.Write((ushort)saveFormats.Count);     //サウンド数
            foreach (SaveFormat saveFormat in saveFormats)
            {
                byte[] bytes;
                if (saveFormat.format == Format.AndroidResorce)
                    bytes = Sub_Code.ReadResourceData(saveFormat.filePath);
                else if (saveFormat.format == Format.WVSLoad && wvsFile != null)
                    bytes = wvsFile.Load_Sound(saveFormat.streamPosition);
                else
                    bytes = File.ReadAllBytes(saveFormat.filePath);

                bw.Write(bytes.Length);     //サウンドデータのサイズ(65535バイト超えるデータがあったらやばいからint型)
                bw.Write(bytes);            //サウンドデータ
            }

            bw.Close();
            dirNames.Clear();

            if (bUnloadWVSLoad)
            {
                //WVS_Loadを使用してセーブファイルを作成している場合アンロードする
                wvsFile?.Dispose();
            }
            if (File.Exists(toFile))
                File.Delete(toFile);
            File.Move(toFile + ".tmp", toFile);
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
        public bool IsLoaded = false;

        private const byte MAX_VERSION = 5;         //2024/06/30現在はPC版はバージョン4、Android版は5
                                                    //PC版ではAndroid版で作成されたセーブファイルは読み込めないが、PC版で作成されたセーブファイルはAndroid版で読み込める
        private BinaryReader? br = null;

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
            br = new BinaryReader(File.OpenRead(filePath));

            //ヘッダーが異なれば終了
            if (Encoding.ASCII.GetString(br.ReadBytes(9)) != "WVSFormat")
            {
                br.Close();
                return WVS_Result.Wrong_Header;
            }

            //4バイトスキップ
            br.ReadBytes(4);
            //セーブファイルのバージョン
            ushort version = br.ReadUInt16();
            if (version >= 4)
            {
                bool bWoTMode = br.ReadBoolean();
                if (bWoTMode)
                {
                    br.Close();
                    return WVS_Result.WoTMode;
                }
            }
            if (version < 4)
            {
                br.Close();
                return WVS_Result.Wrong_Version;
            }
            ushort soundCount = 0;
            if (version < 5)
            {
                //ファイル内のサウンド数
                soundCount = br.ReadUInt16();
            }
            //プロジェクト名のバイト数
            int projectNameBytes = version < 3 ? br.ReadInt32() : br.ReadByte();
            ProjectName = Encoding.UTF8.GetString(br.ReadBytes(projectNameBytes));
            //プロジェクト名の変更が可能かどうかを取得
            _ = br.ReadBoolean();
            bool bIncludeSound = br.ReadBoolean();
            br.ReadByte();
            if (!bIncludeSound && version >= 3)
            {
                ushort dirCount = br.ReadUInt16();
                for (int i = 0; i < dirCount; i++)
                    dirNames.Add(Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte())));
                br.ReadByte();
            }
            try
            {
                //バージョン5以降は構造が大きく変わっているため処理を分ける
                if (version >= 5)
                {
                    List<List<List<ushort>>> soundBytesTemp = [];               //サウンドがどの場所に保存されているかを一時的に管理 (CVoiceSoundSettingを取得するときにはまだ分からないため)

                    _ = br.ReadDouble();        //音量を均一に(アプリ上では使用しない)
                    _ = br.ReadBoolean();       //サウンドが1つもない場合デフォルトの音声を再生させるか(アプリ上では使用しない)

                    //SE情報が含まれていたらスキップ (サーバー側ではSE情報も読み取る必要があるが、Android版Mod Creatorではプリセットをロードするため読み取る必要はない)
                    bool bIncludeSE = br.ReadBoolean();
                    if (bIncludeSE)
                    {
                        byte seTypeCount = br.ReadByte();
                        for (int index = 0; index < seTypeCount; index++)
                        {
                            _ = br.ReadUInt32();
                            _ = br.ReadSByte();
                            _ = br.ReadBoolean();
                            _ = br.ReadBoolean();
                            byte seCount = br.ReadByte();
                            for (int i = 0; i < seCount; i++)
                            {
                                _ = br.ReadBytes(br.ReadByte());
                                _ = br.ReadUInt32();
                                _ = br.ReadBoolean();
                                _ = br.ReadUInt16();
                            }
                        }
                    }

                    //ページ数ぶん繰り返す
                    byte typeCount = br.ReadByte();
                    for (int typeIndex = 0; typeIndex < typeCount; typeIndex++)
                    {
                        soundBytesTemp.Add([]);
                        //ページ内のイベント数ぶん繰り返す
                        byte eventCount = br.ReadByte();
                        for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
                        {
                            soundBytesTemp[^1].Add([]);
                            //CVoiceTypeSettingにイベント情報をロード
                            CVoiceTypeSetting eventInfo = voiceTypes[typeIndex][eventIndex].TypeSetting;
                            eventInfo.EventShortID = br.ReadUInt32();
                            eventInfo.VoiceShortID = br.ReadUInt32();
                            _ = br.ReadBytes(br.ReadByte());
                            bool bIncludeSEType = br.ReadBoolean();
                            if (bIncludeSEType)
                                _ = br.ReadUInt32();
                            eventInfo.IsLoadMode = true;
                            eventInfo.IsVolumeRange = br.ReadBoolean();
                            eventInfo.IsPitchRange = br.ReadBoolean();
                            eventInfo.IsLPFRange = br.ReadBoolean();
                            eventInfo.IsHPFRange = br.ReadBoolean();
                            eventInfo.Volume = br.ReadDouble();
                            eventInfo.Pitch = br.ReadSByte();
                            eventInfo.LowPassFilter = br.ReadSByte();
                            eventInfo.HighPassFilter = br.ReadSByte();
                            eventInfo.VolumeRange.Start = br.ReadDouble();
                            eventInfo.VolumeRange.End = br.ReadDouble();
                            eventInfo.PitchRange.Start = br.ReadSByte();
                            eventInfo.PitchRange.End = br.ReadSByte();
                            eventInfo.LPFRange.Start = br.ReadSByte();
                            eventInfo.LPFRange.End = br.ReadSByte();
                            eventInfo.HPFRange.Start = br.ReadSByte();
                            eventInfo.HPFRange.End = br.ReadSByte();
                            eventInfo.Delay = br.ReadDouble();
                            eventInfo.LimitSoundInstance = br.ReadByte();
                            eventInfo.WhenLimitReached = br.ReadByte();
                            eventInfo.WhenPriorityEqual = br.ReadByte();
                            //ファイルをテキストエディタで開いたとき横長になりすぎるため改行
                            if (eventIndex % 5 == 3)
                                _ = br.ReadByte();
                            //イベント内のサウンド数ぶん繰り返す
                            byte soundCount2 = br.ReadByte();
                            for (int soundIndex = 0; soundIndex < soundCount2; soundIndex++)
                            {
                                //CVoiceSoundSettingにサウンド情報をロード
                                CVoiceSoundSetting soundInfo = new()
                                {
                                    FilePath = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte())),
                                    Probability = br.ReadDouble()
                                };
                                soundInfo.PlayTime.Start = br.ReadDouble();
                                soundInfo.PlayTime.End = br.ReadDouble();
                                soundInfo.IsVolumeRange = br.ReadBoolean();
                                soundInfo.IsPitchRange = br.ReadBoolean();
                                soundInfo.IsLPFRange = br.ReadBoolean();
                                soundInfo.IsHPFRange = br.ReadBoolean();
                                soundInfo.Volume = br.ReadDouble();
                                soundInfo.Pitch = br.ReadSByte();
                                soundInfo.LowPassFilter = br.ReadSByte();
                                soundInfo.HighPassFilter = br.ReadSByte();
                                soundInfo.VolumeRange.Start = br.ReadDouble();
                                soundInfo.VolumeRange.End = br.ReadDouble();
                                soundInfo.PitchRange.Start = br.ReadSByte();
                                soundInfo.PitchRange.End = br.ReadSByte();
                                soundInfo.LPFRange.Start = br.ReadSByte();
                                soundInfo.LPFRange.End = br.ReadSByte();
                                soundInfo.HPFRange.Start = br.ReadSByte();
                                soundInfo.HPFRange.End = br.ReadSByte();
                                soundInfo.Delay = br.ReadDouble();
                                soundInfo.IsFadeIn = br.ReadBoolean();
                                soundInfo.IsFadeOut = br.ReadBoolean();
                                voiceTypes[typeIndex][eventIndex].TypeSetting.Sounds.Add(soundInfo);
                                ushort soundBytesIndex = br.ReadUInt16();
                                soundBytesTemp[^1][^1].Add(soundBytesIndex);        //サウンド本体がどの位置にあるかを一時的に保存
                                //ファイルをテキストエディタで開いたとき横長になりすぎるため改行
                                if (soundIndex % 5 == 3)
                                    _ = br.ReadByte();
                            }
                        }
                        _ = br.ReadByte();
                    }

                    //サウンドの位置を取得
                    List<long> soundDataList = [];
                    ushort soundDataCount = br.ReadUInt16();
                    for (int i = 0; i < soundDataCount; i++)
                    {
                        soundDataList.Add(br.BaseStream.Position);
                        int soundLength = br.ReadInt32();
                        br.BaseStream.Seek(soundLength, SeekOrigin.Current);
                    }

                    //取得したサウンドの位置をCVoiceSoundSettingに適応
                    for (int i = 0; i < soundBytesTemp.Count; i++)
                    {
                        for (int j = 0; j < soundBytesTemp[i].Count; j++)
                        {
                            for (int k = 0; k < soundBytesTemp[i][j].Count; k++)
                            {
                                CVoiceSoundSetting soundInfo = voiceTypes[i][j].TypeSetting.Sounds[k];
                                soundInfo.StreamPosition = soundDataList[soundBytesTemp[i][j][k]];
                            }
                        }
                    }
                }
                //バージョン5未満 (PC版のセーブデータを読み取る)
                else
                {
                    byte typeCount = br.ReadByte();
                    for (int typeIndex = 0; typeIndex < typeCount; typeIndex++)
                    {
                        //イベント数繰り返す
                        byte eventCount = br.ReadByte();
                        for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
                        {
                            //CVoiceTypeSettingにサウンド情報をロード
                            CVoiceTypeSetting eventInfo = voiceTypes[typeIndex][eventIndex].TypeSetting;
                            eventInfo.IsLoadMode = true;
                            eventInfo.IsVolumeRange = br.ReadBoolean();
                            eventInfo.IsPitchRange = br.ReadBoolean();
                            eventInfo.IsLPFRange = br.ReadBoolean();
                            eventInfo.IsHPFRange = br.ReadBoolean();
                            eventInfo.Volume = br.ReadDouble();
                            eventInfo.Pitch = br.ReadSByte();
                            eventInfo.LowPassFilter = br.ReadSByte();
                            eventInfo.HighPassFilter = br.ReadSByte();
                            eventInfo.VolumeRange.Start = br.ReadDouble();
                            eventInfo.VolumeRange.End = br.ReadDouble();
                            eventInfo.PitchRange.Start = br.ReadSByte();
                            eventInfo.PitchRange.End = br.ReadSByte();
                            eventInfo.LPFRange.Start = br.ReadSByte();
                            eventInfo.LPFRange.End = br.ReadSByte();
                            eventInfo.HPFRange.Start = br.ReadSByte();
                            eventInfo.HPFRange.End = br.ReadSByte();
                            eventInfo.Delay = br.ReadDouble();
                            eventInfo.LimitSoundInstance = br.ReadByte();
                            eventInfo.WhenLimitReached = br.ReadByte();
                            eventInfo.WhenPriorityEqual = br.ReadByte();
                            if (eventIndex == 17)
                                _ = br.ReadByte();
                            voiceTypes[typeIndex][eventIndex].TypeSetting = eventInfo;
                        }
                    }
                        _ = br.ReadByte();
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
                            listIndex = br.ReadUInt16();
                            soundIndex = br.ReadInt32();
                            fileNameCount = br.ReadInt32();
                        }
                        else
                        {
                            listIndex = br.ReadByte();
                            soundIndex = br.ReadByte();
                            if (!bIncludeSound)
                                fileName = dirNames[br.ReadUInt16()] + "\\";
                            fileNameCount = br.ReadByte();
                        }
                        fileName += Encoding.UTF8.GetString(br.ReadBytes(fileNameCount));
                        //謎の1バイト
                        br.ReadByte();
                        CVoiceSoundSetting soundInfo = new();
                        //WVSファイルのバージョンが2以降であればサウンド設定を取得
                        if (version >= 2)
                        {
                            soundInfo.FilePath = fileName;
                            soundInfo.Probability = br.ReadDouble();
                            soundInfo.PlayTime.Start = br.ReadDouble();
                            soundInfo.PlayTime.End = br.ReadDouble();
                            soundInfo.IsVolumeRange = br.ReadBoolean();
                            soundInfo.IsPitchRange = br.ReadBoolean();
                            soundInfo.IsLPFRange = br.ReadBoolean();
                            soundInfo.IsHPFRange = br.ReadBoolean();
                            soundInfo.Volume = br.ReadDouble();
                            soundInfo.Pitch = br.ReadSByte();
                            soundInfo.LowPassFilter = br.ReadSByte();
                            soundInfo.HighPassFilter = br.ReadSByte();
                            soundInfo.VolumeRange.Start = br.ReadDouble();
                            soundInfo.VolumeRange.End = br.ReadDouble();
                            soundInfo.PitchRange.Start = br.ReadSByte();
                            soundInfo.PitchRange.End = br.ReadSByte();
                            soundInfo.LPFRange.Start = br.ReadSByte();
                            soundInfo.LPFRange.End = br.ReadSByte();
                            soundInfo.HPFRange.Start = br.ReadSByte();
                            soundInfo.HPFRange.End = br.ReadSByte();
                            soundInfo.Delay = br.ReadDouble();
                            soundInfo.IsFadeIn = br.ReadBoolean();
                            soundInfo.IsFadeOut = br.ReadBoolean();
                            if (bIncludeSound)
                            {
                                //サウンドが開始される地点を保存しておく(Get_Sound_Bytes()でその地点のサウンドをbyte[]形式で読み取れます)
                                soundInfo.StreamPosition = br.BaseStream.Position;
                                int soundLength = br.ReadInt32();
                                br.BaseStream.Seek(soundLength, SeekOrigin.Current);
                                soundInfo.FilePath = Path.GetFileName(fileName);
                            }
                        }
                        else if (bIncludeSound)
                        {
                            //サウンドが開始される地点を保存しておく(Get_Sound_Bytes()でその地点のサウンドをbyte[]形式で読み取れます)
                            soundInfo.StreamPosition = br.BaseStream.Position;
                            //サウンドの長さを取得し、次のサウンドの位置までスキップ
                            int soundLength = br.ReadInt32();
                            br.BaseStream.Seek(soundLength, SeekOrigin.Current);
                            soundInfo.FilePath = Path.GetFileName(fileName);
                        }
                        voiceTypes[listIndex][soundIndex].TypeSetting.Sounds.Add(soundInfo);
                    }
                }
                WVSFile = filePath;
                IsLoaded = true;
                return WVS_Result.OK;
            }
            catch (Exception e)
            {
                WVSFile = "";
                IsLoaded = false;
                br.Close();
                Sub_Code.ErrorLogWrite(e.Message);
                return WVS_Result.Wrong_Version;
            }
        }

        //.wvsファイル内のサウンドデータを取得
        public byte[] Load_Sound(long startPosition)
        {
            if (br == null)
                return [];
            //サウンドのバイト数を取得し、その長さぶん読み取る
            br.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            int soundLength = br.ReadInt32();
            return br.ReadBytes(soundLength);
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
            if (br != null)
            {
                br.Close();
                br = null;
            }
            WVSFile = "";
            IsLoaded = false;
        }
    }
}
