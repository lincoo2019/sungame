using Godot;

[GlobalClass]
public partial class RainManager : Control
{
	public HBoxContainer pcRainContainer;

	public TextureRect pcSunBarTexture;

	public Label pcSunLabel;

	public TextureRect mobileSunBarTexture;

	public Label mobileSunLabel;

	public bool isMobileUI;

	public bool isSunType;

	public long sunNumShow;

	public override void _Ready()
	{
		pcRainContainer = GetNode<HBoxContainer>("%PCRainContainer");
		pcSunBarTexture = GetNode<TextureRect>("%PCSunBarTexture");
		pcSunLabel = GetNode<Label>("%PCSunLabel");
		mobileSunBarTexture = GetNode<TextureRect>("%MobileSunBarTexture");
		mobileSunLabel = GetNode<Label>("%MobileSunLabel");
		BattleEventBus.Instance.OnUiSwitched += ApplyMode;
		isMobileUI = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		ApplyMode(isMobileUI);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnUiSwitched -= ApplyMode;
		}
	}

	public void ApplyMode(bool mobilePreset)
	{
		isMobileUI = mobilePreset;
		if (mobilePreset)
		{
			pcRainContainer.Visible = false;
			mobileSunBarTexture.Visible = isSunType;
			base.CustomMinimumSize = new Vector2(0f, base.CustomMinimumSize.Y);
		}
		else
		{
			pcRainContainer.Visible = true;
			mobileSunBarTexture.Visible = false;
			pcSunBarTexture.Visible = isSunType;
			pcSunLabel.Visible = isSunType;
			base.CustomMinimumSize = new Vector2(isSunType ? 80 : 0, base.CustomMinimumSize.Y);
		}
	}

	public void Init(string type, long initialSun)
	{
		if (type == "Sun")
		{
			isSunType = true;
			SetSunDisplay(initialSun);
			if (!isMobileUI)
			{
				pcSunBarTexture.Visible = true;
				pcSunLabel.Visible = true;
				base.CustomMinimumSize = new Vector2(80f, base.CustomMinimumSize.Y);
			}
			else
			{
				mobileSunBarTexture.Visible = true;
			}
		}
	}

	public void SetSunDisplay(long sunNum)
	{
		if (isSunType && sunNumShow != sunNum)
		{
			sunNumShow = sunNum;
			if (GodotObject.IsInstanceValid(pcSunLabel))
			{
				pcSunLabel.Text = sunNumShow.ToString();
			}
			if (GodotObject.IsInstanceValid(mobileSunLabel))
			{
				mobileSunLabel.Text = sunNumShow.ToString();
			}
		}
	}

	public void ShowMobileSunBar(bool visible)
	{
		if (GodotObject.IsInstanceValid(mobileSunBarTexture))
		{
			mobileSunBarTexture.Visible = visible;
		}
	}

	public Label GetMobileSunLabel()
	{
		return mobileSunLabel;
	}

	public Label GetPCSunLabel()
	{
		return pcSunLabel;
	}
}
