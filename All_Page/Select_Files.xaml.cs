using System.ComponentModel;
using System.Reflection;
using System.Text;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public class Select_File_List(string name, bool bIsDirectory, string fullPath = "") : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; private set; } = name;
    public string FullPath { get; private set; } = fullPath;
    public bool IsDirectory { get; private set; } = bIsDirectory;
    public bool IsVisible => !IsDirectory;
    private bool bChacked;
    public bool IsChecked
    {
        get { return bChacked; }
        set
        {
            if (bChacked != value)
            {
                bChacked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }
    public Color Name_Color => IsDirectory ? Colors.Purple : Colors.Aqua;
}
public partial class Select_Files : ContentPage
{
    public delegate void Select_File_EventHandler<T>(T args);
    public event Select_File_EventHandler<string>? Selected;
    private readonly List<string> searchOptions = [];
    private readonly List<string> backDirs = [];
    private readonly Dictionary<string, string> pageDirs = [];
    private List<Select_File_List> fileDirList = [];
    private string pageName = "";
    private string dir = "";
    private string bottomDir = "";
    private bool bLoaded = false;
    private bool bCanMultiSelect = true;
    private bool bMessageShowing = false;

    public Select_Files()
    {
        InitializeComponent();
        Dir_Back_B.Clicked += Dir_Back_B_Clicked;
        Cancel_B.Clicked += Cancel_B_Clicked;
        OK_B.Clicked += OK_B_Clicked;
        Files_L.ItemSelected += Files_L_ItemSelected;
    }

    //ファイル選択ウィンドウを初期化
    public void Window_Show(string pageName, string initDir = "", string searchOptions = "", bool bNobackMode = false, bool bCanMultiSelect = true, string bottomDir = "")
    {
        //解放されていなければ解放
        Dispose();

        //設定をロード
        if (!bLoaded)
        {
            bLoaded = true;
            if (File.Exists(Sub_Code.ExDir + "/Configs/Select_Files_Dir.dat"))
            {
                BinaryReader br = new(File.OpenRead(Sub_Code.ExDir + "/Configs/Select_Files_Dir.dat"));
                ushort count = br.ReadUInt16();
                for (int i = 0; i < count; i++)
                {
                    string key = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    string value = Encoding.UTF8.GetString(br.ReadBytes(br.ReadByte()));
                    pageDirs.Add(key, value);

                }
                br.Close();
            }
        }

        string selectDir = initDir;

        //特定の拡張子のファイルのみ表示
        if (searchOptions != "")
        {
            string[] options = searchOptions.Split('|');
            foreach (string option in options)
                this.searchOptions.Add(option);
        }

        //フォルダを戻れないように (ボタンを非表示に)
        if (bNobackMode)
            Dir_Back_B.IsVisible = false;
        else
            Dir_Back_B.IsVisible = true;
        this.bCanMultiSelect = bCanMultiSelect;
        this.pageName = pageName;
        this.bottomDir = bottomDir;

        if (initDir == "" && pageDirs.TryGetValue(pageName, out string? pageValue))
            selectDir = pageValue;

        Change_Dir(selectDir);
    }

    //画面下部にメッセージを表示
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

    //選択済みのファイルを列挙
    public List<string> Get_Select_Files()
    {
        List<string> temp = [];
        foreach (Select_File_List selectFile in fileDirList)
        {
            if (!selectFile.IsDirectory && selectFile.IsChecked)
            {
                temp.Add(dir + "/" + selectFile.Name);
            }
        }
        return temp;
    }

    //ユーザーによってページが強制的に閉じられた場合解放する
    protected override bool OnBackButtonPressed()
    {
        if (backDirs.Count == 0)
        {
            Save_Page_Dir();
            Dispose();
            return base.OnBackButtonPressed();
        }
        else
        {
            Change_Dir(backDirs[^1]);
            backDirs.RemoveAt(backDirs.Count - 1);
            return true;
        }
    }

    //リスト内の項目を選択
    private void Files_L_ItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItemIndex == -1)
            return;

        Select_File_List selectFile = fileDirList[e.SelectedItemIndex];

        //選択した項目がファイルならチェックを切り替える
        if (!selectFile.IsDirectory)
        {
            bool bChecked = selectFile.IsChecked;
            //単選択のみであれば、既に選択されている項目を解除する
            if (!bCanMultiSelect)
                foreach (Select_File_List selectFileList in fileDirList)
                    selectFileList.IsChecked = false;
            //チェックのオンオフ
            selectFile.IsChecked = !bChecked;
            Files_L.SelectedItem = null;
            OnPropertyChanged("SelectedItem");

            //項目の背景を透明化
            IEnumerable<PropertyInfo> pInfos = Files_L.GetType().GetRuntimeProperties();
            PropertyInfo? templatedItems = pInfos.FirstOrDefault(info => info.Name == "TemplatedItems");
            if (templatedItems != null)
            {
                object? cells = templatedItems.GetValue(Files_L);
                if (cells != null)
                    foreach (ViewCell? cell in ((ITemplatedItemsList<Cell?>)cells).Cast<ViewCell?>())
                        if (cell != null)
                            cell.View.BackgroundColor = Colors.Transparent;
            }
        }
        else
        {
            //フォルダを選択した場合、そのフォルダに移動
            backDirs.Add(dir);
            Change_Dir(dir + "/" + selectFile.Name);
        }
    }

    //フォルダを更新
    private void Change_Dir(string nextDir)
    {
        //無効なフォルダの場合Rootに移動
        if (!string.IsNullOrWhiteSpace(nextDir) && Directory.Exists(nextDir))
            dir = nextDir;
        else
            dir = Sub_Code.ANDROID_ROOT;
        Files_L.SelectedItem = null;
        Files_L.ItemsSource = null;
        fileDirList.Clear();

        try
        {
            //フォルダ内のフォルダを取得
            string[] dirs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            foreach (string nowDir in dirs)
                fileDirList.Add(new Select_File_List(Path.GetFileName(nowDir), true));
            //フォルダ内のファイルを取得
            IEnumerable<string> files;
            if (searchOptions.Count > 0)
                files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly).Where(file => searchOptions.Any(pattern => file.ToLower().EndsWith(pattern)));
            else
                files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
                fileDirList.Add(new Select_File_List(Path.GetFileName(file), false));

            //名前順にソート
            fileDirList = [.. fileDirList.OrderBy(h => !h.IsDirectory).ThenBy(h => h.Name)];
            Files_L.ItemsSource = fileDirList;
        }
        catch
        {
            Files_L.SelectedItem = null;
            Files_L.ItemsSource = null;
            fileDirList.Clear();
            Message_Feed_Out("このフォルダはアクセスできません。");
        }

        Dir_T.Text = (dir + "/").Replace(Sub_Code.ANDROID_ROOT, "");
    }

    //フォルダを1つ戻る
    private void Dir_Back_B_Clicked(object? sender, EventArgs e)
    {
        if (dir != Sub_Code.ANDROID_ROOT && dir != bottomDir)
        {
            DirectoryInfo? dirInfo = Directory.GetParent(dir);
            if (dirInfo != null)
            {
                string nextDir = dirInfo.FullName;
                Change_Dir(nextDir);
                backDirs.Add(nextDir);
            }
        }
        else
            Message_Feed_Out("これより下の階層は閲覧できません。");
    }

    //キャンセルボタン
    //何もせずウィンドウを閉じる
    private void Cancel_B_Clicked(object? sender, EventArgs e)
    {
        Save_Page_Dir();
        Dispose();
        Navigation.PopModalAsync();
    }
    //決定ボタン
    //イベントハンドラーで通知
    private void OK_B_Clicked(object? sender, EventArgs e)
    {
        Save_Page_Dir();
        Selected?.Invoke(pageName);
        Navigation.PopModalAsync();
    }
    //解放
    public void Dispose()
    {
        fileDirList.Clear();
        searchOptions.Clear();
        backDirs.Clear();
        Files_L.SelectedItem = null;
        Files_L.ItemsSource = null;
        dir = "";
    }
    private void Change_Page_Dir()
    {
        if (!pageDirs.TryAdd(pageName, dir))
            pageDirs[pageName] = dir;
    }
    private void Save_Page_Dir()
    {
        Change_Page_Dir();
        if (!Directory.Exists(Sub_Code.ExDir + "/Configs"))
            Directory.CreateDirectory(Sub_Code.ExDir + "/Configs");
        BinaryWriter bw = new(File.OpenWrite(Sub_Code.ExDir + "/Configs/Select_Files_Dir.dat"));
        bw.Write((ushort)pageDirs.Count);
        foreach (KeyValuePair<string, string> keyValue in pageDirs)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(keyValue.Key);
            bw.Write((byte)keyBytes.Length);
            bw.Write(keyBytes);
            byte[] valueBytes = Encoding.UTF8.GetBytes(keyValue.Value);
            bw.Write((byte)valueBytes.Length);
            bw.Write(valueBytes);
        }
        bw.Close();
    }
}