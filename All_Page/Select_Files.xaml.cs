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

    //�t�@�C���I���E�B���h�E��������
    public void Window_Show(string pageName, string initDir = "", string searchOptions = "", bool bNobackMode = false, bool bCanMultiSelect = true, string bottomDir = "")
    {
        //�������Ă��Ȃ���Ή��
        Dispose();

        //�ݒ�����[�h
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

        //����̊g���q�̃t�@�C���̂ݕ\��
        if (searchOptions != "")
        {
            string[] options = searchOptions.Split('|');
            foreach (string option in options)
                this.searchOptions.Add(option);
        }

        //�t�H���_��߂�Ȃ��悤�� (�{�^�����\����)
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

    //��ʉ����Ƀ��b�Z�[�W��\��
    private async void Message_Feed_Out(string message)
    {
        //�e�L�X�g�������Ԍo������t�F�[�h�A�E�g
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

    //�I���ς݂̃t�@�C�����
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

    //���[�U�[�ɂ���ăy�[�W�������I�ɕ���ꂽ�ꍇ�������
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

    //���X�g���̍��ڂ�I��
    private void Files_L_ItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItemIndex == -1)
            return;

        Select_File_List selectFile = fileDirList[e.SelectedItemIndex];

        //�I���������ڂ��t�@�C���Ȃ�`�F�b�N��؂�ւ���
        if (!selectFile.IsDirectory)
        {
            bool bChecked = selectFile.IsChecked;
            //�P�I���݂̂ł���΁A���ɑI������Ă��鍀�ڂ���������
            if (!bCanMultiSelect)
                foreach (Select_File_List selectFileList in fileDirList)
                    selectFileList.IsChecked = false;
            //�`�F�b�N�̃I���I�t
            selectFile.IsChecked = !bChecked;
            Files_L.SelectedItem = null;
            OnPropertyChanged("SelectedItem");

            //���ڂ̔w�i�𓧖���
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
            //�t�H���_��I�������ꍇ�A���̃t�H���_�Ɉړ�
            backDirs.Add(dir);
            Change_Dir(dir + "/" + selectFile.Name);
        }
    }

    //�t�H���_���X�V
    private void Change_Dir(string nextDir)
    {
        //�����ȃt�H���_�̏ꍇRoot�Ɉړ�
        if (!string.IsNullOrWhiteSpace(nextDir) && Directory.Exists(nextDir))
            dir = nextDir;
        else
            dir = Sub_Code.ANDROID_ROOT;
        Files_L.SelectedItem = null;
        Files_L.ItemsSource = null;
        fileDirList.Clear();

        try
        {
            //�t�H���_���̃t�H���_���擾
            string[] dirs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            foreach (string nowDir in dirs)
                fileDirList.Add(new Select_File_List(Path.GetFileName(nowDir), true));
            //�t�H���_���̃t�@�C�����擾
            IEnumerable<string> files;
            if (searchOptions.Count > 0)
                files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly).Where(file => searchOptions.Any(pattern => file.ToLower().EndsWith(pattern)));
            else
                files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
                fileDirList.Add(new Select_File_List(Path.GetFileName(file), false));

            //���O���Ƀ\�[�g
            fileDirList = [.. fileDirList.OrderBy(h => !h.IsDirectory).ThenBy(h => h.Name)];
            Files_L.ItemsSource = fileDirList;
        }
        catch
        {
            Files_L.SelectedItem = null;
            Files_L.ItemsSource = null;
            fileDirList.Clear();
            Message_Feed_Out("���̃t�H���_�̓A�N�Z�X�ł��܂���B");
        }

        Dir_T.Text = (dir + "/").Replace(Sub_Code.ANDROID_ROOT, "");
    }

    //�t�H���_��1�߂�
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
            Message_Feed_Out("�����艺�̊K�w�͉{���ł��܂���B");
    }

    //�L�����Z���{�^��
    //���������E�B���h�E�����
    private void Cancel_B_Clicked(object? sender, EventArgs e)
    {
        Save_Page_Dir();
        Dispose();
        Navigation.PopModalAsync();
    }
    //����{�^��
    //�C�x���g�n���h���[�Œʒm
    private void OK_B_Clicked(object? sender, EventArgs e)
    {
        Save_Page_Dir();
        Selected?.Invoke(pageName);
        Navigation.PopModalAsync();
    }
    //���
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