using WoTB_Mod_Creator2.Class;

namespace WoTB_Mod_Creator2.All_Page;

public partial class Voice_Create_Event_Setting : ContentPage
{
	CVoiceTypeSetting? eventSetting = null;

	public Voice_Create_Event_Setting()
	{
		InitializeComponent();
	}

	public void Initialize(CVoiceTypeSetting eventSetting)
	{
		this.eventSetting = eventSetting;
    }
}