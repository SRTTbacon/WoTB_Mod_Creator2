namespace WoTB_Mod_Creator2.Class
{
    public struct SVector2<T>(T start, T end)
    {
        public T Start { get; set; } = start;
        public T End { get; set; } = end;
    }
    public struct SVector3<T>(T start, T end, T max)
    {
        public T Start { get; set; } = start;
        public T End { get; set; } = end;
        public T Max { get; set; } = max;
    }

    public class CVoiceSoundSetting
    {
        public string FilePath { get; set; }
        public long StreamPosition { get; set; }
        public double Probability { get; set; }
        public double Volume { get; set; }
        public double Delay { get; set; }
        public int Pitch { get; set; }
        public int LowPassFilter { get; set; }
        public int HighPassFilter { get; set; }
        public int TypeIndex { get; set; }
        public int VoiceIndex { get; set; }
        public int FileIndex { get; set; }
        public SVector2<double> VolumeRange = new(0.0, 0.0);
        public SVector3<double> PlayTime = new(0.0, 0.0, 0.0);
        public SVector2<int> PitchRange = new(0, 0);
        public SVector2<int> LPFRange = new(0, 0);
        public SVector2<int> HPFRange = new(0, 0);
        public bool IsVolumeRange = false;
        public bool IsPitchRange = false;
        public bool IsLPFRange = false;
        public bool IsHPFRange = false;
        public bool IsFadeIn = false;
        public bool IsFadeOut = false;
        public bool IsNormalMode = true;
        public bool IsBinarySound => StreamPosition != 0;
        public CVoiceSoundSetting(string filePath = "")
        {
            FilePath = filePath;
            Init();
        }
        public CVoiceSoundSetting Clone()
        {
            CVoiceSoundSetting Info = (CVoiceSoundSetting)MemberwiseClone();
            return Info;
        }
        private void Init()
        {
            StreamPosition = 0;
            Probability = 50;
            Pitch = 0;
            LowPassFilter = 0;
            HighPassFilter = 0;
            Volume = 0;
            Delay = 0;
            TypeIndex = -1;
            VoiceIndex = -1;
            FileIndex = -1;
            VolumeRange = new(0.0, 0.0);
            PlayTime = new(0.0, 0.0, 0.0);
            PitchRange = new(0, 0);
            LPFRange = new(0, 0);
            HPFRange = new(0, 0);
        }
    }

    //サウンドの親クラス
    public class CVoiceTypeSetting
    {
        //SE_Volumeは±表記なので、初期値の0は増減なしとなります。
        public List<CVoiceSoundSetting> Sounds { get; private set; } = [];
        public string EventName { get; set; } = string.Empty;
        public double Volume { get; set; }
        public double SEVolume { get; set; }
        public double Delay { get; set; }
        public uint EventShortID { get; private set; }
        public uint VoiceShortID { get; private set; }
        public uint SEShortID { get; private set; }
        public int SEIndex { get; private set; }
        public int Pitch { get; set; }
        public int LowPassFilter { get; set; }
        public int HighPassFilter { get; set; }
        public int LimitSoundInstance { get; set; }
        public int WhenLimitReached { get; set; }
        public int WhenPriorityEqual { get; set; }
        public SVector2<double> VolumeRange = new(0.0, 0.0);
        public SVector2<int> PitchRange = new(0, 0);
        public SVector2<int> LPFRange = new(0, 0);
        public SVector2<int> HPFRange = new(0, 0);
        public bool IsVolumeRange = false;
        public bool IsPitchRange = false;
        public bool IsLPFRange = false;
        public bool IsHPFRange = false;
        public bool IsLoadMode = false;
        public CVoiceTypeSetting()
        {
            SEIndex = -1;
            SEVolume = 0;
            Init(0, 0);
        }
        public CVoiceTypeSetting(uint eventShortID, uint voiceShortID, uint seShortID = 0, int seIndex = -1, double seVolume = 0)
        {
            SEIndex = seIndex;
            SEVolume = seVolume;
            Init(eventShortID, voiceShortID, seShortID);
        }
        public void Set_Param(uint eventShortID, uint voiceShortID, uint seShortID = 0, int seIndex = -1, double seVolume = 0)
        {
            EventShortID = eventShortID;
            VoiceShortID = voiceShortID;
            SEShortID = seShortID;
            SEIndex = seIndex;
            SEVolume = seVolume;
        }
        public void Set_Param(string eventName, uint voiceShortID)
        {
            EventName = eventName;
            VoiceShortID = voiceShortID;
        }
        public void Init(uint eventShortID, uint voiceShortID, uint seShortID = 0)
        {
            EventShortID = eventShortID;
            VoiceShortID = voiceShortID;
            SEShortID = seShortID;
            EventName = string.Empty;
            Pitch = 0;
            LowPassFilter = 0;
            HighPassFilter = 0;
            Volume = 0.0;
            Delay = 0.0;
            LimitSoundInstance = 50;
            WhenLimitReached = 0;
            WhenPriorityEqual = 0;
            VolumeRange = new(0.0, 0.0);
            PitchRange = new(0, 0);
            LPFRange = new(0, 0);
            HPFRange = new(0, 0);
            IsLoadMode = false;
        }
        public CVoiceTypeSetting Clone(bool bIsIncludeSounds)
        {
            CVoiceTypeSetting clone = (CVoiceTypeSetting)MemberwiseClone();
            clone.Sounds = [];
            if (bIsIncludeSounds)
                foreach (CVoiceSoundSetting soundInfo in Sounds)
                    clone.Sounds.Add(soundInfo.Clone());
            return clone;
        }

        public static void Set_Event_ShortID(List<List<All_Page.CVoiceTypeList>> eventSettings)
        {
            //イベントID, 音声コンテナID, SEコンテナID, SE_Index, 音量
            eventSettings[0][0].TypeSetting.Set_Param(341425709, 170029050, 366092539, 5, -6);
            eventSettings[0][1].TypeSetting.Set_Param(908426860, 95559763, 370075103, 3, -2);
            eventSettings[0][2].TypeSetting.Set_Param(280189980, 766083947, 298293840, 9, -1);
            eventSettings[0][3].TypeSetting.Set_Param(815358870, 569784404, 862763776, 5, -6);
            eventSettings[0][4].TypeSetting.Set_Param(49295125, 266422868, 876186554, 6, -1);
            eventSettings[0][5].TypeSetting.Set_Param(733342682, 1052258113, 568110765, 10, -1);
            eventSettings[0][6].TypeSetting.Set_Param(331196727, 242302464, 66753859, 18, -1);
            eventSettings[0][7].TypeSetting.Set_Param(619058694, 334837201, 162440597, 18, -1);
            eventSettings[0][8].TypeSetting.Set_Param(794420468, 381780774, 52837378, 23);
            eventSettings[0][9].TypeSetting.Set_Param(109598189, 489572734, 582349497, 5, -1);
            eventSettings[0][10].TypeSetting.Set_Param(244621664, 210078142, 750651777, 19, -1);
            eventSettings[0][11].TypeSetting.Set_Param(73205091, 249535989, 1042937732, 20, -1);
            eventSettings[0][12].TypeSetting.Set_Param(466111031, 908710042, 125367048, 21, -1);
            eventSettings[0][13].TypeSetting.Set_Param(471196930, 1057023960);
            eventSettings[0][14].TypeSetting.Set_Param(337626756, 953778289);
            eventSettings[0][15].TypeSetting.Set_Param(930519512, 121897540, 602706971, 8, -1);
            eventSettings[0][16].TypeSetting.Set_Param(1063632502, 127877647, 953241595, 19, -1);
            eventSettings[0][17].TypeSetting.Set_Param(175994480, 462397017, 734855314, 20, -1);
            eventSettings[0][18].TypeSetting.Set_Param(546476029, 651656679, 265156722, 21, -1);
            eventSettings[0][19].TypeSetting.Set_Param(337748775, 739086111, 738480888, 18);
            eventSettings[0][20].TypeSetting.Set_Param(302644322, 363753108, 97368200, 18);
            eventSettings[0][21].TypeSetting.Set_Param(356562073, 91697210, 948692451, 7);
            eventSettings[0][22].TypeSetting.Set_Param(156782042, 987172940, 87851485, 18);
            eventSettings[0][23].TypeSetting.Set_Param(769815093, 518589126, 267487625, 22);
            eventSettings[0][24].TypeSetting.Set_Param(236686366, 330491031, 904204732, 19, -1);
            eventSettings[0][25].TypeSetting.Set_Param(559710262, 792301846, 42606663, 20, -1);
            eventSettings[0][26].TypeSetting.Set_Param(47321344, 539730785, 308135346, 21, -1);
            eventSettings[0][27].TypeSetting.Set_Param(978556760, 38261315, 480932913, 19, -1);
            eventSettings[0][28].TypeSetting.Set_Param(878993268, 37535832, 887022, 20, -1);
            eventSettings[0][29].TypeSetting.Set_Param(581830963, 558576963, 783375460, 21, -1);
            eventSettings[0][30].TypeSetting.Set_Param(984973529, 1014565012, 124621166, 19, -1);
            eventSettings[0][31].TypeSetting.Set_Param(381112709, 135817430, 634991721, 20, -1);
            eventSettings[0][32].TypeSetting.Set_Param(33436524, 985679417, 940515369, 21, -1);
            eventSettings[0][33].TypeSetting.Set_Param(116097397, 164671745, 667880140, 4);
            eventSettings[1][0].TypeSetting.Set_Param(308272618, 447063394, 479275647, 13);
            eventSettings[1][1].TypeSetting.Set_Param(767278023, 154835998, 917399664, 12);
            eventSettings[1][2].TypeSetting.Set_Param(230904672, 607694618, 904269149, 2);
            eventSettings[1][3].TypeSetting.Set_Param(390478464, 391276124, 747137713, 2);
            eventSettings[1][4].TypeSetting.Set_Param(17969037, 840378218, 990119123, 2);
            eventSettings[1][5].TypeSetting.Set_Param(900922817, 549968154, 1039956691, 2);
            eventSettings[1][6].TypeSetting.Set_Param(727518878, 1015337424, 1041861596, 2);
            eventSettings[1][7].TypeSetting.Set_Param(101252368, 271044645, 284419845, 2);
            eventSettings[1][8].TypeSetting.Set_Param(576711003, 496552975, 93467631, 2);
            eventSettings[1][9].TypeSetting.Set_Param(470859110, 430377111, 236153639, 2);
            eventSettings[1][10].TypeSetting.Set_Param(502585189, 839607605, 391999685, 15);
            eventSettings[1][11].TypeSetting.Set_Param(769354725, 233444430, 166694669, 16);
            eventSettings[1][12].TypeSetting.Set_Param(402727222, 299739777, 769579073, 11);
            eventSettings[1][13].TypeSetting.Set_Param(670169971, 120795627, 951031474, 24);
            eventSettings[1][14].TypeSetting.Set_Param(204685755, 820440351, 206640353, 1);
            eventSettings[1][15].TypeSetting.Set_Param(1065169508, 891902653);
            eventSettings[1][16].TypeSetting.Set_Param(198183306, 52813795, 394210856, 25);
            if (!eventSettings[1][15].TypeSetting.IsLoadMode)
                eventSettings[1][15].TypeSetting.Volume = -11;
            eventSettings[2][0].TypeSetting.Set_Param(420002792, 491691546);
            eventSettings[2][1].TypeSetting.Set_Param(420002792, 417768496);
            eventSettings[2][2].TypeSetting.Set_Param(420002792, 46472417);
            eventSettings[2][3].TypeSetting.Set_Param(420002792, 681331945);
            eventSettings[2][4].TypeSetting.Set_Param(420002792, 190711689);
            eventSettings[2][5].TypeSetting.Set_Param(420002792, 918836720);
        }
    }
}
