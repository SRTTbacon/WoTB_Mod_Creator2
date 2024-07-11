using System.Text;
using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Music_Player_Setting_Page : ContentPage
{
    const string CONFIG_HEADER = "WMC_Header";
    const byte CONFIG_VERSION = 0x00;

    public delegate void CheckBoxEventHandler<T>(T args);
    public event CheckBoxEventHandler<bool>? ChangeLPFEnable = null;
    public event CheckBoxEventHandler<bool>? ChangeHPFEnable = null;
    public event CheckBoxEventHandler<bool>? ChangeECHOEnable = null;
    public bool bLPFChanged = false;
    public bool bHPFChanged = false;
    public bool bECHOChanged = false;
    public bool bLPFEnable = false;
    public bool bHPFEnable = false;
    public bool bECHOEnable = false;
    public double lpfValue = 0;
    public double hpfValue = 0;
    public double echoDelayValue = 0;
    public double echoPowerOriginalValue = 0;
    public double echoPowerECHOValue = 0;
    public double echoLengthValue = 0;
    public bool bLoaded = false;
    bool bShowing = false;

    public Music_Player_Setting_Page()
	{
		InitializeComponent();
        LPF_S.ValueChanged += LPF_S_ValueChanged;
        HPF_S.ValueChanged += HPF_S_ValueChanged;
        ECHO_Delay_S.ValueChanged += ECHO_Delay_S_ValueChanged;
        ECHO_Power_Original_S.ValueChanged += ECHO_Power_Original_S_ValueChanged;
        ECHO_Power_ECHO_S.ValueChanged += ECHO_Power_ECHO_S_ValueChanged;
        ECHO_Length_S.ValueChanged += ECHO_Length_S_ValueChanged;
        LPF_Enable_C.CheckedChanged += LPF_Enable_C_ChanckedChanged;
        HPF_Enable_C.CheckedChanged += HPF_Enable_C_ChanckedChanged;
        ECHO_Enable_C.CheckedChanged += ECHO_Enable_C_ChackedChanged;
        Back_B.Clicked += Back_B_Clicked;
    }

    private void LPF_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        bLPFChanged = true;
        lpfValue = e.NewValue;
        LPF_T.Text = "Low Pass Filter:" + (int)LPF_S.Value;
    }

    private void HPF_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        bHPFChanged = true;
        hpfValue = e.NewValue;
        HPF_T.Text = "High Pass Filter:" + (int)HPF_S.Value;
    }

    private void ECHO_Delay_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        bECHOChanged = true;
        echoDelayValue = e.NewValue;
        ECHO_Delay_T.Text = "エコー(遅延):" + Math.Round(ECHO_Delay_S.Value, 1, MidpointRounding.AwayFromZero) + "秒";
    }

    private void ECHO_Power_Original_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        bECHOChanged = true;
        echoPowerOriginalValue = e.NewValue;
        ECHO_Power_Original_T.Text = "エコー(元音量):" + (int)ECHO_Power_Original_S.Value;
    }

    private void ECHO_Power_ECHO_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        bECHOChanged = true;
        echoPowerECHOValue = e.NewValue;
        ECHO_Power_ECHO_T.Text = "エコー音量:" + (int)ECHO_Power_ECHO_S.Value;
    }

    private void ECHO_Length_S_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        bECHOChanged = true;
        echoLengthValue = e.NewValue;
        ECHO_Length_T.Text = "エコー(長さ):" + (int)ECHO_Length_S.Value;
    }

    private void LPF_Enable_C_ChanckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        bLPFEnable = e.Value;
        ChangeLPFEnable?.Invoke(e.Value);
    }

    private void HPF_Enable_C_ChanckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        bHPFEnable = e.Value;
        ChangeHPFEnable?.Invoke(e.Value);
    }

    private void ECHO_Enable_C_ChackedChanged(object? sender, CheckedChangedEventArgs e)
    {
        bECHOEnable = e.Value;
        ChangeECHOEnable?.Invoke(e.Value);
    }

    public void Configs_Load()
    {
        if (IsLoaded)
            return;

        if (File.Exists(Sub_Code.ExDir + "/Configs/Music_Player_Setting.conf"))
        {
            BinaryReader? br = null;
            try
            {
                br = new(File.OpenRead(Sub_Code.ExDir + "/Configs/Music_Player_Setting.conf"));
                _ = br.ReadBytes(br.ReadByte());
                _ = br.ReadByte();
                LPF_S.Value = br.ReadDouble();
                HPF_S.Value = br.ReadDouble();
                ECHO_Delay_S.Value = br.ReadDouble();
                ECHO_Power_Original_S.Value = br.ReadDouble();
                ECHO_Power_ECHO_S.Value = br.ReadDouble();
                ECHO_Length_S.Value = br.ReadDouble();
                ECHO_Enable_C.IsChecked = br.ReadBoolean();
                br.Close();
            }
            catch (Exception e1)
            {
                br?.Close();
                Sub_Code.ErrorLogWrite(e1.Message);
            }
        }
        if (!IsLoaded)
        {
            ECHO_Delay_S.Value = 0.3;
            ECHO_Power_Original_S.Value = 85;
            ECHO_Power_ECHO_S.Value = 30;
            ECHO_Length_S.Value = 45;
            LPF_Enable_C.IsChecked = false;
            HPF_Enable_C.IsChecked = false;
        }
        bLoaded = true;
    }

    private void Configs_Save()
    {
        try
        {
            if (File.Exists(Sub_Code.ExDir + "/Configs/Music_Player_Setting.conf"))
                File.Delete(Sub_Code.ExDir + "/Configs/Music_Player_Setting.conf");
            BinaryWriter bw = new(File.OpenWrite(Sub_Code.ExDir + "/Configs/Music_Player_Setting.conf"));
            byte[] header = Encoding.ASCII.GetBytes(CONFIG_HEADER);
            bw.Write((byte)header.Length);
            bw.Write(header);
            bw.Write(CONFIG_VERSION);
            bw.Write(LPF_S.Value);
            bw.Write(HPF_S.Value);
            bw.Write(ECHO_Delay_S.Value);
            bw.Write(ECHO_Power_Original_S.Value);
            bw.Write(ECHO_Power_ECHO_S.Value);
            bw.Write(ECHO_Length_S.Value);
            bw.Write(ECHO_Enable_C.IsChecked);
            bw.Close();
        }
        catch (Exception e)
        {
            Sub_Code.ErrorLogWrite(e.Message);
        }
    }

    private void ContentPage_Disappearing(object? sender, EventArgs e)
    {
        Configs_Save();
    }

    private void Back_B_Clicked(object? sender, EventArgs e)
    {
        if (!bShowing)
            return;
        bShowing = false;
        Navigation.PopModalAsync();
    }

    private void ContentPage_Disappearing_1(object sender, EventArgs e)
    {
        bShowing = false;
    }

    private void ContentPage_Appearing(object sender, EventArgs e)
    {
        bShowing = true;
    }
}