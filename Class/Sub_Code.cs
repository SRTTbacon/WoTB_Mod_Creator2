using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WoTB_Mod_Creator2.All_Page;

namespace WoTB_Mod_Creator2.Class
{
    public partial class Sub_Code
    {
        [GeneratedRegex(@"(^|\\|/)(CON|PRN|AUX|NUL|CLOCK\$|COM[0-9]|LPT[0-9])(\.|\\|/|$)", RegexOptions.IgnoreCase, "ja-JP")]
        private static partial Regex MyRegex();

        //読み取り専用のファイル選択ウィンドウ
        public static readonly Select_Files Select_Files_Window = new();

        //エフェクトの値一覧
        public static readonly Dictionary<int, int> LPFValues = [];
        public static readonly Dictionary<int, int> HPFValues = [];
        public static readonly Dictionary<int, int> PitchValues = [];

        public const string APP_VERSION = "0.1";
        public const string ANDROID_ROOT = "/storage/emulated/0";

        public static readonly Random RandomValue = new();

        //アプリ固有のディレクトリを参照
        public static string ExDir
        {
            get
            {
#if ANDROID
                return AndroidClass.GetExDir();
#else
                return "";
#endif
            }
        }

        //オリジナルのファイル選択画面を使用するかどうか
        public static bool IsUseSelectPage { get; set; } = false;

        //エフェクトの値を初期化
        public static void Initialize()
        {
            LPFValues.Clear();
            HPFValues.Clear();
            PitchValues.Clear();
            LPFValues.Add(12000, 1500);
            LPFValues.Add(10500, 2500);
            LPFValues.Add(8500, 2000);
            LPFValues.Add(6500, 3200);
            LPFValues.Add(3300, 2000);
            LPFValues.Add(1300, 700);
            LPFValues.Add(600, 400);
            LPFValues.Add(200, 0);
            HPFValues.Add(0, 45);
            HPFValues.Add(45, 25);
            HPFValues.Add(70, 45);
            HPFValues.Add(115, 25);
            HPFValues.Add(140, 60);
            HPFValues.Add(200, 130);
            HPFValues.Add(330, 70);
            HPFValues.Add(400, 150);
            HPFValues.Add(550, 350);
            HPFValues.Add(900, 400);
            HPFValues.Add(1300, 475);
            HPFValues.Add(1775, 1225);
            HPFValues.Add(3000, 1500);
            HPFValues.Add(4500, 1500);
            HPFValues.Add(6500, 0);
            PitchValues.Add(1200, 100);
            PitchValues.Add(1100, 90);
            PitchValues.Add(1000, 80);
            PitchValues.Add(900, 70);
            PitchValues.Add(800, 60);
            PitchValues.Add(700, 50);
            PitchValues.Add(575, 40);
            PitchValues.Add(450, 30);
            PitchValues.Add(300, 20);
            PitchValues.Add(175, 10);
            PitchValues.Add(0, 0);
            PitchValues.Add(-175, -10);
            PitchValues.Add(-400, -20);
            PitchValues.Add(-600, -30);
            PitchValues.Add(-900, -40);
            PitchValues.Add(-1200, -50);
        }

        //ファイルとして扱えるパスかどうか調べる
        public static bool IsSafePath(string path, bool bFileName)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            char[] invalidChars = bFileName ? Path.GetInvalidFileNameChars() : Path.GetInvalidPathChars();
            return path.IndexOfAny(invalidChars) < 0 && !MyRegex().IsMatch(path);
        }

        //エラーをファイルに出力
        public static void ErrorLogWrite(string text)
        {
            DateTime dt = DateTime.Now;
            string time = Get_Time_Now(dt, ".", 1, 6);
            if (text.EndsWith('\n'))
                File.AppendAllText(ExDir + "/Error_Log.txt", time + ":" + text);
            else
                File.AppendAllText(ExDir + "/Error_Log.txt", time + ":" + text + "\n");
        }

        public static string Get_Time_String(double Position)
        {
            TimeSpan Time = TimeSpan.FromSeconds(Position);
            string Minutes = Time.Minutes.ToString();
            string Seconds = Time.Seconds.ToString();
            if (Time.Minutes < 10)
                Minutes = "0" + Time.Minutes;
            if (Time.Seconds < 10)
                Seconds = "0" + Time.Seconds;
            return Minutes + ":" + Seconds;
        }

        public static string Get_Time_Now(DateTime dt, string between, int first, int end)
        {
            if (first > end)
                return "";
            if (first == end)
                return Get_Time_Index(dt, first);
            string Temp = "";
            for (int i = first; i <= end; i++)
            {
                if (i != end)
                    Temp += Get_Time_Index(dt, i) + between;
                else
                    Temp += Get_Time_Index(dt, i);
            }
            return Temp;
        }
        private static string Get_Time_Index(DateTime dt, int Index)
        {
            if (Index > 0 && Index < 7)
            {
                if (Index == 1)
                    return dt.Year.ToString();
                else if (Index == 2)
                    return dt.Month.ToString();
                else if (Index == 3)
                    return dt.Day.ToString();
                else if (Index == 4)
                    return dt.Hour.ToString();
                else if (Index == 5)
                    return dt.Minute.ToString();
                else if (Index == 6)
                    return dt.Second.ToString();
            }
            return "";
        }

        public static bool File_Equal(string path1, string path2)
        {
            if (path1 == path2)
                return true;
            FileStream? fs1 = null;
            FileStream? fs2 = null;
            try
            {
                fs1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs2 = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs1.Length != fs2.Length)
                    return false;
                int file1byte;
                int file2byte;
                long End_Length = fs1.Length / 50;
                long Now_Length = 0;
                do
                {
                    file1byte = fs1.ReadByte();
                    file2byte = fs2.ReadByte();
                    Now_Length++;
                }
                while ((file1byte == file2byte) && (file1byte != -1) && End_Length >= Now_Length);
                return (file1byte - file2byte) == 0;
            }
            finally
            {
                using (fs1)
                { }
                using (fs2)
                { }
            }
        }

        public static double Get_Decimal(double value)
        {
            string text = value.ToString();
            if (!text.Contains('.'))
                return 0;
            string Decim = text[(text.IndexOf('.') + 1)..];
            return double.Parse("0." + Decim);
        }

        public static double Get_Random_Double(double Minimum, double Maximum)
        {
            return RandomValue.NextDouble() * (Maximum - Minimum) + Minimum;
        }

        public static double Get_Version_To_Double(string version)
        {
            string onePoint = "";
            foreach (char c in version)
            {
                if (onePoint == "")
                    onePoint += c + ".";
                else if (c != '.')
                    onePoint += c;
            }
            try
            {
                return double.Parse(onePoint, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0.0;
            }
        }
    }
    public partial class WwiseHash
    {
        //GUIDからShortIDを生成
        public static uint HashGUID(string ID)
        {
            Regex alphanum = MyRegex();
            string filtered = alphanum.Replace(ID, "");
            List<byte> guidBytes = [];
            int[] byteOrder = [3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15];
            for (int i = 0; i < byteOrder.Length; i++)
                guidBytes.Add(byte.Parse(filtered.AsSpan(byteOrder[i] * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            return FnvHash([.. guidBytes], false);
        }

        public static uint HashString(string Name)
        {
            return FnvHash(Encoding.ASCII.GetBytes(Name.ToLowerInvariant()), true);
        }

        static uint FnvHash(byte[] input, bool use32bits)
        {
            uint prime = 16777619;
            uint offset = 2166136261;
            uint mask = 1073741823;
            uint hash = offset;
            for (int i = 0; i < input.Length; i++)
            {
                hash *= prime;
                hash ^= input[i];
            }
            if (use32bits)
                return hash;
            else
                return (hash >> 30) ^ (hash & mask);
        }

        [GeneratedRegex("[^0-9A-Za-z]")]
        private static partial Regex MyRegex();
    }
}
