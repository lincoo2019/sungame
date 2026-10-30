using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ConveyorBeltManager : Control
{
	private static Texture2D _conveyorBeltSun;

	private static Texture2D _conveyorBeltSunBackdrop;

	public HBoxContainer pcConveyorBeltContainer;

	public VBoxContainer mobileConveyorBeltContainer;

	public Control pcControl;

	public Control mobileControl;

	public Control packetContainer;

	public TextureRect belt;

	public Sprite2D mobileBelt1;

	public Control mobilePacketContainer1;

	public Sprite2D mobileBelt2;

	public Control mobilePacketContainer2;

	public NinePatchRect conveyorBeltRectPC;

	public NinePatchRect conveyorBeltRectMobile1;

	public NinePatchRect conveyorBeltRectMobile2;

	public TextureRect mobileConveyorBeltSunBarTexture;

	public Label mobileConveyorBeltSunLabel;

	public TextureRect pcConveyorBeltSunBarTexture;

	public Label pcConveyorBeltSunLabel;

	public double beltTime;

	public bool isMobileUI;

	public Container conveyorBeltContainer;

	public bool isSunType;

	public long sunNumShow;

	private string _configuredType = "Default";

	private bool _hudSingleColumn;

	private Control _pcCardClip;

	private Control _mobileCardClip1;

	private (TowerDefenseCardScroll Scroll, Control Content, Control Clip, Control Packets, bool Vertical)[] _cardScrolls;

	private EconomyAccountId _sunAccountId;

	private static Texture2D ConveyorBeltSun => _conveyorBeltSun ?? (_conveyorBeltSun = GD.Load<Texture2D>("uid://i6rwl358wldw"));

	private static Texture2D ConveyorBeltSunBackdrop => _conveyorBeltSunBackdrop ?? (_conveyorBeltSunBackdrop = GD.Load<Texture2D>("uid://6h0njlvpp86"));

	public EconomyAccountId SunAccountId => _sunAccountId;

	public bool HasSunAccount => _sunAccountId.IsValid;

	public TowerDefenseCardScroll GetCardScroll()
	{
		return _cardScrolls?[isMobileUI ? 1u : 0u].Scroll;
	}

	private (TowerDefenseCardScroll, Control, Control, Control, bool) InstallCardScroll(Control packets, Control clip, bool vertical)
	{
		Node parent = packets.GetParent();
		TowerDefenseCardScroll towerDefenseCardScroll = new TowerDefenseCardScroll
		{
			Name = "CardsScroll",
			MouseFilter = MouseFilterEnum.Pass,
			ClipContents = true,
			ScrollDeadzone = 16
		};
		Control control = new Control
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		parent.AddChild(towerDefenseCardScroll, forceReadableName: false, InternalMode.Disabled);
		towerDefenseCardScroll.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		packets.Reparent(control);
		return (towerDefenseCardScroll, control, clip, packets, vertical);
	}

	private void RefreshCardScrolls()
	{
		if (_cardScrolls != null)
		{
			(TowerDefenseCardScroll, Control, Control, Control, bool)[] cardScrolls = _cardScrolls;
			for (int i = 0; i < cardScrolls.Length; i++)
			{
				(TowerDefenseCardScroll, Control, Control, Control, bool) tuple = cardScrolls[i];
				Vector2 size = tuple.Item3.Size;
				float num = tuple.Item4.GetChildCount() * (tuple.Item5 ? 61 : 53) + 7;
				bool enabled = num > (tuple.Item5 ? size.Y : size.X);
				tuple.Item1.ConfigureScrolling(enabled, tuple.Item5);
				Vector2 vector = (tuple.Item5 ? new Vector2(size.X, Math.Max(size.Y, num)) : new Vector2(Math.Max(size.X, num), size.Y));
				tuple.Item2.CustomMinimumSize = vector;
				tuple.Item2.Size = vector;
				tuple.Item1.Size = size;
			}
		}
	}

	public void SetHudWidth(float width)
	{
		pcControl.CustomMinimumSize = new Vector2(width, 0f);
		conveyorBeltRectPC.CustomMinimumSize = new Vector2(Mathf.Min(518f, width), 86f);
		conveyorBeltRectPC.GetParent<Control>().Size = new Vector2(width, 86f);
		_pcCardClip.Size = new Vector2(width - 14f, 74f);
		pcConveyorBeltContainer.Size = new Vector2(width + (float)(pcConveyorBeltSunBarTexture.Visible ? 80 : 0), 86f);
	}

	public void SetHudSingleColumn(bool enabled)
	{
		if (_hudSingleColumn != enabled && isMobileUI)
		{
			Array<Node> packetChildren = GetPacketChildren(mobilePreset: true);
			_hudSingleColumn = enabled;
			for (int i = 0; i < packetChildren.Count; i++)
			{
				Control control = (Control)packetChildren[i];
				Control control2 = ((enabled || i % 2 == 0) ? mobilePacketContainer1 : mobilePacketContainer2);
				if (control.GetParent() != control2)
				{
					control.Reparent(control2);
				}
				control2.MoveChild(control, -1);
				control.Position = new Vector2(0f, 16 + 61 * (enabled ? i : (i / 2)));
			}
		}
		_hudSingleColumn = enabled;
		float num = 600f;
		mobileControl.CustomMinimumSize = new Vector2(enabled ? 110 : 220, num);
		conveyorBeltRectMobile2.GetParent().GetParent<Control>().Visible = !enabled;
		conveyorBeltRectMobile1.GetParent<Control>().Size = new Vector2(110f, num);
		_mobileCardClip1.Size = new Vector2(96f, num - 12f);
	}

	public bool TryBindSunAccount(EconomyAccountId accountId)
	{
		if (!accountId.IsValid)
		{
			return false;
		}
		if (_sunAccountId.IsValid)
		{
			return _sunAccountId == accountId;
		}
		_sunAccountId = accountId;
		return true;
	}

	private long GetBoundSun()
	{
		if (!HasSunAccount)
		{
			return TowerDefenseManager.Instance.GetSun();
		}
		return TowerDefenseManager.Instance.GetSun(_sunAccountId);
	}

	public override void _Ready()
	{
		pcConveyorBeltContainer = GetNode<HBoxContainer>("%PCConveyorBeltContainer");
		mobileConveyorBeltContainer = GetNode<VBoxContainer>("%MobileConveyorBeltContainer");
		pcControl = GetNode<Control>("%PCControl");
		mobileControl = GetNode<Control>("%MobileControl");
		packetContainer = GetNode<Control>("%PacketContainer");
		belt = GetNode<TextureRect>("%Belt");
		mobileBelt1 = GetNode<Sprite2D>("%MobileBelt1");
		mobilePacketContainer1 = GetNode<Control>("%MobilePacketContainer1");
		mobileBelt2 = GetNode<Sprite2D>("%MobileBelt2");
		mobilePacketContainer2 = GetNode<Control>("%MobilePacketContainer2");
		conveyorBeltRectPC = GetNode<NinePatchRect>("%ConveyorBeltRectPC");
		conveyorBeltRectMobile1 = GetNode<NinePatchRect>("%ConveyorBeltRectMobile1");
		conveyorBeltRectMobile2 = GetNode<NinePatchRect>("%ConveyorBeltRectMobile2");
		mobileConveyorBeltSunBarTexture = GetNode<TextureRect>("%MobileConveyorBeltSunBarTexture");
		mobileConveyorBeltSunLabel = GetNode<Label>("%MobileConveyorBeltSunLabel");
		pcConveyorBeltSunBarTexture = GetNode<TextureRect>("%PCConveyorBeltSunBarTexture");
		pcConveyorBeltSunLabel = GetNode<Label>("%PCConveyorBeltSunLabel");
		_pcCardClip = packetContainer.GetParent().GetParent<Control>();
		_mobileCardClip1 = mobilePacketContainer1.GetParent().GetParent<Control>();
		Control parent = mobilePacketContainer2.GetParent().GetParent<Control>();
		_cardScrolls = new(TowerDefenseCardScroll, Control, Control, Control, bool)[3]
		{
			InstallCardScroll(packetContainer, _pcCardClip, vertical: false),
			InstallCardScroll(mobilePacketContainer1, _mobileCardClip1, vertical: true),
			InstallCardScroll(mobilePacketContainer2, parent, vertical: true)
		};
		RefreshCardScrolls();
		BattleEventBus.Instance.OnUiSwitched += ApplyMode;
		isMobileUI = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		conveyorBeltContainer = (isMobileUI ? ((BoxContainer)mobileConveyorBeltContainer) : ((BoxContainer)pcConveyorBeltContainer));
		ApplyMode(isMobileUI);
		ApplyConfiguredType();
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
		if (mobilePreset)
		{
			conveyorBeltContainer = mobileConveyorBeltContainer;
			pcConveyorBeltContainer.Visible = false;
			base.CustomMinimumSize = new Vector2(0f, base.CustomMinimumSize.Y);
		}
		else
		{
			conveyorBeltContainer = pcConveyorBeltContainer;
			mobileConveyorBeltContainer.Visible = false;
			base.CustomMinimumSize = new Vector2(781 + (isSunType ? 80 : 0), base.CustomMinimumSize.Y);
		}
		isMobileUI = mobilePreset;
		conveyorBeltContainer.Visible = true;
		SyncConveyorBelt(isMobileUI);
		if (isSunType)
		{
			if (GodotObject.IsInstanceValid(pcConveyorBeltSunBarTexture))
			{
				pcConveyorBeltSunBarTexture.Visible = !mobilePreset;
			}
			if (GodotObject.IsInstanceValid(pcConveyorBeltSunLabel))
			{
				pcConveyorBeltSunLabel.Visible = !mobilePreset;
			}
			ShowMobileSunBar(mobilePreset);
		}
	}

	public void SyncConveyorBelt(bool mobilePreset)
	{
		Array<Node> packetChildren = GetPacketChildren(!mobilePreset);
		int num = 0;
		foreach (Node item in packetChildren)
		{
			if (!(item is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow))
			{
				continue;
			}
			if (mobilePreset)
			{
				if (_hudSingleColumn || num % 2 == 0)
				{
					towerDefenseInGamePacketShow.Reparent(mobilePacketContainer1);
					towerDefenseInGamePacketShow.Position = new Vector2(0f, 16f + 61f * (_hudSingleColumn ? ((float)num) : Mathf.Floor((float)num / 2f)));
				}
				else
				{
					towerDefenseInGamePacketShow.Reparent(mobilePacketContainer2);
					towerDefenseInGamePacketShow.Position = new Vector2(0f, 16f + 61f * Mathf.Floor((float)num / 2f));
				}
			}
			else
			{
				towerDefenseInGamePacketShow.Reparent(packetContainer);
				towerDefenseInGamePacketShow.Position = new Vector2(13f + 53f * (float)num, 0f);
			}
			num++;
		}
	}

	public void Init(string type)
	{
		_configuredType = (string.IsNullOrWhiteSpace(type) ? "Default" : type);
		if (IsNodeReady())
		{
			ApplyConfiguredType();
		}
	}

	private void ApplyConfiguredType()
	{
		if (_configuredType == "Sun")
		{
			isSunType = true;
			if (GodotObject.IsInstanceValid(conveyorBeltRectPC))
			{
				conveyorBeltRectPC.Texture = ConveyorBeltSunBackdrop;
			}
			if (GodotObject.IsInstanceValid(belt))
			{
				belt.Texture = ConveyorBeltSun;
			}
			if (GodotObject.IsInstanceValid(conveyorBeltRectMobile1))
			{
				conveyorBeltRectMobile1.Texture = ConveyorBeltSunBackdrop;
			}
			if (GodotObject.IsInstanceValid(conveyorBeltRectMobile2))
			{
				conveyorBeltRectMobile2.Texture = ConveyorBeltSunBackdrop;
			}
			if (GodotObject.IsInstanceValid(mobileBelt1))
			{
				mobileBelt1.Texture = ConveyorBeltSun;
			}
			if (GodotObject.IsInstanceValid(mobileBelt2))
			{
				mobileBelt2.Texture = ConveyorBeltSun;
			}
			if (GodotObject.IsInstanceValid(pcConveyorBeltSunBarTexture))
			{
				pcConveyorBeltSunBarTexture.Visible = !isMobileUI;
			}
			if (GodotObject.IsInstanceValid(pcConveyorBeltSunLabel))
			{
				pcConveyorBeltSunLabel.Visible = !isMobileUI;
			}
			ShowMobileSunBar(isMobileUI);
			if (!isMobileUI)
			{
				base.CustomMinimumSize = new Vector2(861f, base.CustomMinimumSize.Y);
			}
			long boundSun = GetBoundSun();
			if (boundSun >= 0)
			{
				sunNumShow = boundSun;
				if (GodotObject.IsInstanceValid(pcConveyorBeltSunLabel))
				{
					pcConveyorBeltSunLabel.Text = sunNumShow.ToString();
				}
				if (GodotObject.IsInstanceValid(mobileConveyorBeltSunLabel))
				{
					mobileConveyorBeltSunLabel.Text = sunNumShow.ToString();
				}
			}
		}
		else
		{
			isSunType = false;
		}
	}

	public void UpdateBeltAnimation(double delta)
	{
		RefreshCardScrolls();
		if (!base.Visible)
		{
			return;
		}
		if (TowerDefenseManager.Instance.currentControl.isGameRunning)
		{
			beltTime += delta;
		}
		if (!isMobileUI)
		{
			if (GodotObject.IsInstanceValid(belt) && GodotObject.IsInstanceValid(belt.Material))
			{
				((ShaderMaterial)belt.Material).SetShaderParameter("time", beltTime);
			}
			int childCount = packetContainer.GetChildCount();
			for (int i = 0; i < childCount; i++)
			{
				TowerDefenseInGamePacketShow childOrNull = packetContainer.GetChildOrNull<TowerDefenseInGamePacketShow>(i);
				if (childOrNull != null)
				{
					Vector2 packetPos = GetPacketPos(i);
					if (childOrNull.Position.X > packetPos.X)
					{
						childOrNull.Position = new Vector2(childOrNull.Position.X - (float)delta * 50f, childOrNull.Position.Y);
					}
					else
					{
						childOrNull.Position = new Vector2(packetPos.X, childOrNull.Position.Y);
					}
				}
			}
			return;
		}
		if (GodotObject.IsInstanceValid(mobileBelt1) && GodotObject.IsInstanceValid(mobileBelt1.Material))
		{
			((ShaderMaterial)mobileBelt1.Material).SetShaderParameter("time", beltTime);
		}
		if (GodotObject.IsInstanceValid(mobileBelt2) && GodotObject.IsInstanceValid(mobileBelt2.Material))
		{
			((ShaderMaterial)mobileBelt2.Material).SetShaderParameter("time", beltTime);
		}
		for (int j = 0; j < mobilePacketContainer1.GetChildCount(); j++)
		{
			TowerDefenseInGamePacketShow childOrNull2 = mobilePacketContainer1.GetChildOrNull<TowerDefenseInGamePacketShow>(j);
			if (childOrNull2 != null)
			{
				Vector2 vector = mobilePacketContainer1.Position + new Vector2(0f, 61 * j);
				if (childOrNull2.Position.Y > vector.Y)
				{
					childOrNull2.Position = new Vector2(childOrNull2.Position.X, childOrNull2.Position.Y - (float)delta * 50f);
				}
				else
				{
					childOrNull2.Position = new Vector2(childOrNull2.Position.X, vector.Y);
				}
			}
		}
		for (int k = 0; k < mobilePacketContainer2.GetChildCount(); k++)
		{
			TowerDefenseInGamePacketShow childOrNull3 = mobilePacketContainer2.GetChildOrNull<TowerDefenseInGamePacketShow>(k);
			if (childOrNull3 != null)
			{
				Vector2 vector2 = mobilePacketContainer2.Position + new Vector2(0f, 61 * k);
				if (childOrNull3.Position.Y > vector2.Y)
				{
					childOrNull3.Position = new Vector2(childOrNull3.Position.X, childOrNull3.Position.Y - (float)delta * 50f);
				}
				else
				{
					childOrNull3.Position = new Vector2(childOrNull3.Position.X, vector2.Y);
				}
			}
		}
	}

	public Vector2 GetPacketPos(int id)
	{
		return packetContainer.Position + new Vector2(53f * (float)id, 0f);
	}

	public Array<Node> GetPacketChildren(bool mobilePreset)
	{
		Array<Node> array = new Array<Node>();
		if (mobilePreset)
		{
			int num = (GodotObject.IsInstanceValid(mobilePacketContainer1) ? mobilePacketContainer1.GetChildCount() : 0);
			int num2 = (GodotObject.IsInstanceValid(mobilePacketContainer2) ? mobilePacketContainer2.GetChildCount() : 0);
			for (int i = 0; i < Math.Max(num, num2); i++)
			{
				if (i < num)
				{
					array.Add(mobilePacketContainer1.GetChild(i));
				}
				if (i < num2)
				{
					array.Add(mobilePacketContainer2.GetChild(i));
				}
			}
		}
		else if (GodotObject.IsInstanceValid(packetContainer))
		{
			array.AddRange(packetContainer.GetChildren());
		}
		return array;
	}

	public int GetPacketCount()
	{
		if (!isMobileUI)
		{
			return packetContainer.GetChildCount();
		}
		return mobilePacketContainer1.GetChildCount() + mobilePacketContainer2.GetChildCount();
	}

	public void ResetPacketPositions()
	{
		if (!isMobileUI)
		{
			int num = 0;
			{
				foreach (Node child in packetContainer.GetChildren())
				{
					if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow)
					{
						towerDefenseInGamePacketShow.Position = new Vector2(13f + 53f * (float)num, 0f);
						num++;
					}
				}
				return;
			}
		}
		int num2 = 0;
		foreach (Node child2 in mobilePacketContainer1.GetChildren())
		{
			if (child2 is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow2)
			{
				towerDefenseInGamePacketShow2.Position = new Vector2(0f, 16f + 61f * (float)num2);
				num2++;
			}
		}
		int num3 = 0;
		foreach (Node child3 in mobilePacketContainer2.GetChildren())
		{
			if (child3 is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow3)
			{
				towerDefenseInGamePacketShow3.Position = new Vector2(0f, 16f + 61f * (float)num3);
				num3++;
			}
		}
	}

	public void AddPacketToUI(TowerDefenseInGamePacketShow packet)
	{
		if (isMobileUI)
		{
			packet.Position = new Vector2(0f, 680f);
			if (_hudSingleColumn || mobilePacketContainer1.GetChildCount() <= mobilePacketContainer2.GetChildCount())
			{
				if (_hudSingleColumn)
				{
					packet.Position = new Vector2(0f, Mathf.Max(680, 61 * GetPacketCount() + 80));
				}
				mobilePacketContainer1.AddChild(packet, forceReadableName: false, InternalMode.Disabled);
			}
			else
			{
				mobilePacketContainer2.AddChild(packet, forceReadableName: false, InternalMode.Disabled);
			}
		}
		else
		{
			packet.Position = new Vector2(868f, 0f);
			packetContainer.AddChild(packet, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void ShowMobileSunBar(bool visible)
	{
		if (GodotObject.IsInstanceValid(mobileConveyorBeltSunBarTexture))
		{
			mobileConveyorBeltSunBarTexture.Visible = visible;
		}
	}

	public Label GetMobileSunLabel()
	{
		return mobileConveyorBeltSunLabel;
	}

	public Label GetPCSunLabel()
	{
		return pcConveyorBeltSunLabel;
	}

	public void UpdateSunDisplay()
	{
		if (!isSunType)
		{
			return;
		}
		long boundSun = GetBoundSun();
		if (sunNumShow != boundSun)
		{
			sunNumShow = boundSun;
			if (GodotObject.IsInstanceValid(pcConveyorBeltSunLabel))
			{
				pcConveyorBeltSunLabel.Text = sunNumShow.ToString();
			}
			if (GodotObject.IsInstanceValid(mobileConveyorBeltSunLabel))
			{
				mobileConveyorBeltSunLabel.Text = sunNumShow.ToString();
			}
		}
	}
}
