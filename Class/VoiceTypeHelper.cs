using WoTB_Mod_Creator2.All_Page;

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
        public SE_Type? SEType { get; set; } = null;
        public string DefaultVoiceName = "";
        public double Volume { get; set; }
        public double Delay { get; set; }
        public uint EventShortID { get; set; }
        public uint VoiceShortID { get; set; }
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

        }
        public CVoiceTypeSetting(uint eventShortID, uint voiceShortID, SE_Type? seType, string defaultVoiceName)
        {
            Init(eventShortID, voiceShortID, seType, defaultVoiceName);
        }

        public void Init(uint eventShortID, uint voiceShortID, SE_Type? seType, string defaultVoiceName)
        {
            EventShortID = eventShortID;
            VoiceShortID = voiceShortID;
            SEType = seType;
            DefaultVoiceName = defaultVoiceName;
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
    }
}
