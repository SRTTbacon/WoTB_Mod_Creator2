using AngleSharp.Io;
using System.Security.Cryptography;
using System.Text;
using WoTB_Mod_Creator2.All_Page;

namespace WoTB_Mod_Creator2.Class
{
    public class WMS_Save
    {
        enum Format
        {
            WMSLoad,
            FilePath
        }

        class SaveFormat(Format format, string filePath, long streamPosition = 0)
        {
            public Format format = format;

            public string filePath = filePath;

            public long streamPosition = streamPosition;

            public byte[] md5 = [];
        }

        public const string WMS_HEADER = "WMS_Format";
        public const byte WMS_VERSION = 0x00;

        List<OtherModPage> pages = [];
        WMS_Load? wmsLoad;

        bool bDefaultSoundMod = false;

        public void Add_Data(List<OtherModPage> pages, WMS_Load wmsLoad, bool bDefaultSoundMod)
        {
            this.pages = pages;
            this.wmsLoad = wmsLoad;
            this.bDefaultSoundMod = bDefaultSoundMod;
        }

        public void Create(string toFile, string projectName, int saveRange = -1, bool bIncludeSoundData = false, bool bUnloadWMSLoad = true)
        {
            //サウンドが存在する階層を保存 (ファイルサイズ削減のため)
            List<string> dirNames = [];

            List<SaveFormat> saveFormats = [];

            if (File.Exists(toFile + ".tmp"))
                File.Delete(toFile + ".tmp");

            BinaryWriter bw = new(File.OpenWrite(toFile + ".tmp"));
            //ヘッダー
            bw.Write(Encoding.ASCII.GetBytes(WMS_HEADER));
            //謎の4バイト
            bw.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            //WMSファイルのバージョン
            bw.Write(WMS_VERSION);
            //プロジェクト名のバイト配列を保存
            byte[] projectNameBytes = Encoding.UTF8.GetBytes(projectName);
            bw.Write((byte)projectNameBytes.Length);
            bw.Write(projectNameBytes);
            //セーブ範囲 (-1は全部、またはそのページの内容のみ)
            bw.Write((sbyte)saveRange);
            //砲撃音Modでサウンドが存在しない場合デフォルトのサウンドを挿入するかどうか (じゃ..入れるね.//)
            bw.Write(bDefaultSoundMod);
            //サウンドデータが含まれるかどうか
            bw.Write(bIncludeSoundData);

            //親フォルダを事前に保存
            for (int i = 0; i < pages.Count; i++)
            {
                if (saveRange != -1 && saveRange != i)
                    continue;
                foreach (OtherModType type in pages[i].Types)
                {
                    foreach (OtherModSound sound in type.Sounds)
                    {
                        string? parentDir = Path.GetDirectoryName(sound.FilePath);
                        if (parentDir != null && !dirNames.Contains(parentDir))
                            dirNames.Add(parentDir);
                    }
                }
            }
            bw.Write((ushort)dirNames.Count);
            foreach(string dirName in dirNames)
            {
                byte[] dirNameBytes = Encoding.UTF8.GetBytes(dirName);
                bw.Write((byte)dirNameBytes.Length);
                bw.Write(dirNameBytes);
            }

            //ページ数
            bw.Write((byte)pages.Count);
            for (int i = 0; i < pages.Count; i++)
            {
                if (saveRange != -1 && saveRange != i)
                    continue;
                OtherModPage page = pages[i];
                //ページ名
                byte[] pageNameBytes = Encoding.UTF8.GetBytes(page.ModPageName);
                bw.Write((byte)pageNameBytes.Length);
                bw.Write(pageNameBytes);
                //Wwiseのプロジェクト名 (ページによって異なる)
                byte[] wwiseProjectNameBytes = Encoding.UTF8.GetBytes(page.WwiseProjectName);
                bw.Write((byte)wwiseProjectNameBytes.Length);
                bw.Write(wwiseProjectNameBytes);
                bw.Write(page.modPageID);           //ページID
                bw.Write((byte)page.Types.Count);   //タイプ数
                foreach (OtherModType type in page.Types)
                {
                    //タイプ名
                    byte[] typeNameBytes = Encoding.UTF8.GetBytes(type.ModTypeName);
                    bw.Write((byte)typeNameBytes.Length);
                    bw.Write(typeNameBytes);
                    bw.Write(type.ModTypeID);   //タイプID
                    bw.Write(type.ContainerID); //サウンドの配置場所
                    bw.Write((byte)type.SoundCount);
                    for (int j = 0; j < type.SoundCount; j++)
                    {
                        OtherModSound sound = type.Sounds[j];
                        //ファイルパスを保存
                        //親フォルダが存在するインデックスを取得 (親フォルダが存在しない場合-1)
                        string? parentDir = Path.GetDirectoryName(sound.FilePath);
                        int dirIndex = -1;
                        if (parentDir != null)
                        {
                            if (dirNames.Contains(parentDir))
                                dirIndex = dirNames.IndexOf(parentDir);
                            else
                            {
                                dirNames.Add(parentDir);
                                dirIndex = dirNames.Count - 1;
                            }
                        }
                        bw.Write((short)dirIndex);
                        //ファイル名を保存
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(sound.FilePath));
                        bw.Write((byte)fileNameBytes.Length);
                        bw.Write(fileNameBytes);
                        if (bIncludeSoundData)
                        {
                            //既に同じサウンドが設定されていればそのインデックスを取得。なければCount
                            //貫通と致命弾は同じサウンドを使用する可能性があるため容量削減のため同じサウンドの場合はまとめる
                            int number = saveFormats.Count;
                            bool bExist = false;
                            byte[] soundBytes;
                            if (sound.IsBinarySound && wmsLoad != null)
                                soundBytes = wmsLoad.Load_Sound(sound.StreamPosition);
                            else
                                soundBytes = File.ReadAllBytes(sound.FilePath);
                            for (int k = 0; k < saveFormats.Count; k++)
                            {
                                if (Sub_Code.CompareBytes(saveFormats[k].md5, soundBytes))
                                {
                                    number = i;
                                    bExist = true;
                                    break;
                                }
                            }
                            if (!bExist)
                            {
                                if (sound.IsBinarySound && wmsLoad != null)
                                    saveFormats.Add(new(Format.WMSLoad, "", sound.StreamPosition));
                                else
                                    saveFormats.Add(new(Format.FilePath, sound.FilePath));
                                saveFormats[^1].md5 = MD5.HashData(soundBytes);
                            }
                            bw.Write((ushort)number);       //サウンドデータが入っているインデックス
                        }
                        //横長になるため3個イベントが進むたびに改行
                        if (j % 5 == 3)
                            bw.Write((byte)0x0a);
                    }
                }
                bw.Write((byte)0x0a);
            }

            if (bIncludeSoundData)
            {
                bw.Write((ushort)saveFormats.Count);     //サウンド数
                foreach (SaveFormat saveFormat in saveFormats)
                {
                    byte[] bytes;
                    if (saveFormat.format == Format.WMSLoad && wmsLoad != null)
                        bytes = wmsLoad.Load_Sound(saveFormat.streamPosition);
                    else
                        bytes = File.ReadAllBytes(saveFormat.filePath);

                    bw.Write(bytes.Length);     //サウンドデータのサイズ(65535バイト超えるデータがあったらやばいからint型)
                    bw.Write(bytes);            //サウンドデータ
                }
            }

            bw.Close();

            if (bUnloadWMSLoad)
            {
                //WVS_Loadを使用してセーブファイルを作成している場合アンロードする
                wmsLoad?.Dispose();
            }
            if (File.Exists(toFile))
                File.Delete(toFile);
            File.Move(toFile + ".tmp", toFile);
        }
    }

    public class WMS_Load
    {
        //ロード結果
        public enum WMS_Result
        {
            OK,             //正常にロード
            Wrong_Version,  //処理できないほど古いバージョン
            Wrong_Header,   //ヘッダーが異なる (そもそも.wmsファイルではない)
            No_Exist_File,  //ファイルが存在しない
            Wrong_Data,     //ロードできるデータではない
        }

        BinaryReader? br = null;

        public WMS_Result WMS_Load_File(string filePath, List<OtherModPage> pages, out string projectName)
        {
            projectName = "";
            //ファイルが存在しない場合終了
            if (!File.Exists(filePath))
                return WMS_Result.No_Exist_File;

            //既に何かロードされていれば解放
            Dispose();

            //サウンドが存在する階層
            List<string> dirNames = [];

            //.wvsファイルをバイナリデータとして読み取る
            br = new(File.OpenRead(filePath));

            try
            {
                //ヘッダーが異なれば終了
                if (Encoding.ASCII.GetString(br.ReadBytes(br.ReadByte())) != WMS_Save.WMS_HEADER)
                {
                    br.Close();
                    return WMS_Result.Wrong_Header;
                }

                //サウンドがどの場所に保存されているかを一時的に管理 (CVoiceSoundSettingを取得するときにはまだ分からないため)
                List<List<List<ushort>>> soundBytesTemp = [];

                //4バイトスキップ
                br.ReadBytes(4);
                //セーブファイルのバージョン
                _ = br.ReadUInt16();

                projectName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                int saveRange = br.ReadSByte();
                if (saveRange != -1)
                {
                    br.Close();
                    return WMS_Result.Wrong_Data;
                }

                foreach (OtherModPage page in pages)
                    foreach (OtherModType type in page.Types)
                        type.Sounds.Clear();

                _ = br.ReadBoolean();
                bool bIncludeSoundData = br.ReadBoolean();

                ushort dirs = br.ReadUInt16();
                for (int i = 0; i < dirs; i++)
                    dirNames.Add(Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte())));

                byte pageCount = br.ReadByte();

                for (int i = 0; i < pageCount; i++)
                {
                    soundBytesTemp.Add([]);

                    string pageName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    string wwiseProjectName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    uint pageID = br.ReadUInt32();

                    OtherModPage page = new(pageName, wwiseProjectName)
                    {
                        modPageID = pageID
                    };

                    int pageIndex = -1;
                    for (int nowPage = 0; nowPage < pages.Count; nowPage++)
                    {
                        if (pages[nowPage].modPageID == pageID)
                        {
                            pageIndex = nowPage;
                            break;
                        }
                    }

                    byte typeCount = br.ReadByte();

                    for (int j = 0; j < typeCount; j++)
                    {
                        soundBytesTemp[^1].Add([]);

                        string typeName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                        uint typeID = br.ReadUInt32();
                        uint containerID = br.ReadUInt32();

                        OtherModType type = new(typeName, containerID)
                        {
                            ModTypeID = typeID
                        };

                        if (pageIndex == -1)
                            page.Types.Add(type);
                        else
                        {
                            for (int nowType = 0; nowType < pages[pageIndex].Types.Count; nowType++)
                            {
                                if (pages[pageIndex].Types[nowType].ModTypeID == typeID)
                                {
                                    page.Types.Add(type);
                                    break;
                                }
                            }
                        }

                        byte soundCount = br.ReadByte();

                        for (int k = 0; k < soundCount; k++)
                        {
                            short dirIndex = br.ReadInt16();
                            string fileName = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                            if (dirIndex != -1)
                                fileName = dirNames[dirIndex] + "/" + fileName;

                            OtherModSound sound = new(fileName);

                            type.Sounds.Add(sound);

                            if (bIncludeSoundData)
                            {
                                ushort soundIndex = br.ReadUInt16();
                                soundBytesTemp[^1][^1].Add(soundIndex);
                            }

                            if (k % 5 == 3)
                                _ = br.ReadByte();
                        }
                    }
                    _ = br.ReadByte();

                    if (pageIndex == -1)
                        pages.Add(page);
                    else
                        pages[pageIndex] = page;
                }

                if (bIncludeSoundData)
                {
                    //サウンドの位置を取得
                    List<long> soundDataList = [];
                    ushort soundDataCount = br.ReadUInt16();
                    for (int i = 0; i < soundDataCount; i++)
                    {
                        soundDataList.Add(br.BaseStream.Position);
                        int soundLength = br.ReadInt32();
                        br.BaseStream.Seek(soundLength, SeekOrigin.Current);
                    }

                    //取得したサウンドの位置をOtherModSoundに適応
                    for (int i = 0; i < soundBytesTemp.Count; i++)
                    {
                        for (int j = 0; j < soundBytesTemp[i].Count; j++)
                        {
                            for (int k = 0; k < soundBytesTemp[i][j].Count; k++)
                            {
                                OtherModSound sound = pages[i].Types[j].Sounds[k];
                                sound.StreamPosition = soundDataList[soundBytesTemp[i][j][k]];
                            }
                        }
                    }
                }

                return WMS_Result.OK;
            }
            catch (Exception e)
            {
                pages.Clear();
                Sub_Code.ErrorLogWrite(e.Message);
                return WMS_Result.Wrong_Data;
            }
        }

        //.wmsファイル内のサウンドデータを取得
        public byte[] Load_Sound(long startPosition)
        {
            if (br == null)
                return [];
            //サウンドのバイト数を取得し、その長さぶん読み取る
            br.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            int soundLength = br.ReadInt32();
            return br.ReadBytes(soundLength);
        }

        //解放
        public void Dispose()
        {
            if (br != null)
            {
                br.Close();
                br = null;
            }
        }
    }
}
