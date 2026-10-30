using System;
using System.Collections.Generic;
using Godot;

public partial class TowerDefenseCoexistHud : Control
{
	private sealed class BankView
	{
		public TowerDefenseCardScroll Scroll;

		public Control Content;

		public bool Scrollable;

		public bool Vertical;
	}

	public TowerDefenseControlNew Battle;

	private readonly Dictionary<Control, BankView> _views = new Dictionary<Control, BankView>();

	private readonly Dictionary<Control, (Vector2 Minimum, Vector2 Size)> _seedGeometry = new Dictionary<Control, (Vector2, Vector2)>();

	private readonly List<(EconomyAccountId Account, TextureRect Bar, Label Text)> _balances = new List<(EconomyAccountId, TextureRect, Label)>();

	private NinePatchRect _seedFrame;

	private int _signature;

	private bool _hasSignature;

	private bool _managed;

	private TowerDefenseInGameSeedBank _seed;

	private ConveyorBeltManager _conveyor;

	private RainManager _rain;

	private SlotMachineControl _slot;

	public bool IsCoexistLayoutActive { get; private set; }

	public Vector2 ReservedSpace { get; private set; }

	private int SeedSlots
	{
		get
		{
			if (_seed != null)
			{
				return Math.Max(TowerDefenseManager.Instance.seedbankPacketMax, _seed.packetNum);
			}
			return 0;
		}
	}

	public override void _Ready()
	{
		base.Name = "CoexistHud";
		base.MouseFilter = MouseFilterEnum.Ignore;
		_seedFrame = new NinePatchRect
		{
			Name = "SeedBankFrame",
			MouseFilter = MouseFilterEnum.Ignore,
			LightMask = 0,
			Visible = false,
			PatchMarginLeft = 79,
			PatchMarginRight = 11,
			PatchMarginTop = 9,
			PatchMarginBottom = 9
		};
		AddChild(_seedFrame, forceReadableName: false, InternalMode.Disabled);
	}

	public void RegisterBank(Node node)
	{
		Control control = node as Control;
		if (control == null)
		{
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		TowerDefenseCardScroll towerDefenseCardScroll = new TowerDefenseCardScroll
		{
			MouseFilter = MouseFilterEnum.Ignore,
			ClipContents = false,
			ScrollDeadzone = 16
		};
		Control control2 = new Control
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		_views.Add(control, new BankView
		{
			Scroll = towerDefenseCardScroll,
			Content = control2
		});
		towerDefenseCardScroll.ScrollStarted += delegate
		{
			if (_managed && control == _seed)
			{
				TowerDefenseManager.Instance.GetPacketPickControl()?.Release();
			}
		};
		AddChild(towerDefenseCardScroll, forceReadableName: false, InternalMode.Disabled);
		towerDefenseCardScroll.AddChild(control2, forceReadableName: false, InternalMode.Disabled);
		control2.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		_hasSignature = false;
	}

	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(Battle) || Battle.IsQueuedForDeletion())
		{
			return;
		}
		TowerDefenseInGameSeedBank towerDefenseInGameSeedBank = (Battle.GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank)?.seedBank;
		ConveyorBeltManager conveyorBeltManager = (Battle.GetFeature("ConveyorBelt") as TowerDefenseBattleFeatureConveyorBelt)?.conveyorBeltManager;
		RainManager rainManager = (Battle.GetFeature("RainMode") as TowerDefenseBattleFeatureRainMode)?.rainManager;
		SlotMachineControl slotMachineControl = (Battle.GetFeature("SlotMachine") as TowerDefenseBattleFeatureSlotMachine)?.slotMachineControl;
		if (!Usable(towerDefenseInGameSeedBank))
		{
			towerDefenseInGameSeedBank = null;
		}
		if (!Usable(conveyorBeltManager))
		{
			conveyorBeltManager = null;
		}
		if (!Usable(rainManager) || !rainManager.isSunType)
		{
			rainManager = null;
		}
		if (!Usable(slotMachineControl))
		{
			slotMachineControl = null;
		}
		bool flag = ((towerDefenseInGameSeedBank != null) ? 1 : 0) + ((conveyorBeltManager != null) ? 1 : 0) + ((rainManager != null) ? 1 : 0) + ((slotMachineControl != null) ? 1 : 0) > 1 && Battle.isGameRunning;
		bool flag2 = Battle.isGameRunning && (flag || (towerDefenseInGameSeedBank != null && towerDefenseInGameSeedBank.packetNum > 16));
		bool flag3 = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		Vector2 size = GetViewportRect().Size;
		HashCode hashCode = default(HashCode);
		hashCode.Add(towerDefenseInGameSeedBank);
		hashCode.Add(conveyorBeltManager);
		hashCode.Add(rainManager);
		hashCode.Add(slotMachineControl);
		hashCode.Add(flag2);
		hashCode.Add(flag3);
		hashCode.Add(size);
		hashCode.Add(towerDefenseInGameSeedBank?.packetNum ?? 0);
		hashCode.Add(TowerDefenseManager.Instance.seedbankPacketMax);
		hashCode.Add(Battle.uITopPropContainer.GetCombinedMinimumSize());
		foreach (KeyValuePair<Control, BankView> view in _views)
		{
			if (GodotObject.IsInstanceValid(view.Key))
			{
				hashCode.Add(view.Key.GetCombinedMinimumSize());
				hashCode.Add(view.Key.Visible);
			}
		}
		int num = hashCode.ToHashCode();
		if (!_hasSignature || num != _signature)
		{
			if (_seed != towerDefenseInGameSeedBank || _conveyor != conveyorBeltManager || _rain != rainManager || _slot != slotMachineControl || !flag2)
			{
				RestoreLegacyLayout();
			}
			_seed = towerDefenseInGameSeedBank;
			_conveyor = conveyorBeltManager;
			_rain = rainManager;
			_slot = slotMachineControl;
			_managed = flag2;
			IsCoexistLayoutActive = flag;
			if (flag2)
			{
				Arrange(flag3, size);
			}
			else
			{
				ArrangeLegacy();
			}
			_signature = num;
			_hasSignature = true;
		}
		if (!_managed)
		{
			return;
		}
		foreach (var balance in _balances)
		{
			balance.Text.Text = TowerDefenseManager.Instance.GetSun(balance.Account).ToString();
		}
	}

	private static bool Usable(Control node)
	{
		if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion() && node.Visible)
		{
			return node.IsNodeReady();
		}
		return false;
	}

	private void Arrange(bool mobile, Vector2 screen)
	{
		base.CustomMinimumSize = Vector2.Zero;
		foreach (BankView value in _views.Values)
		{
			value.Scroll.Visible = false;
		}
		BuildBalances(mobile);
		_seedFrame.Visible = !mobile && _seed != null;
		if (_seed != null)
		{
			_seed.pcSeedBankTexture.Visible = false;
		}
		if (_conveyor != null)
		{
			_conveyor.SetHudSingleColumn(mobile);
			_conveyor.pcConveyorBeltSunBarTexture.Visible = false;
			_conveyor.mobileConveyorBeltSunBarTexture.Visible = false;
		}
		if (_rain != null)
		{
			TextureRect pcSunBarTexture = _rain.pcSunBarTexture;
			bool visible = (_rain.mobileSunBarTexture.Visible = false);
			pcSunBarTexture.Visible = visible;
			Mount(_rain, Vector2.Zero, Vector2.One, Vector2.One);
		}
		if (mobile)
		{
			int num = ((_conveyor != null) ? 1 : ((SeedSlots <= 8) ? 1 : 2));
			float num2 = ((_seed != null) ? ((num == 2) ? 194 : 110) : 0);
			float num3 = ((_conveyor != null) ? (num2 + 110f) : 0f);
			for (int i = 0; i < _balances.Count; i++)
			{
				Place(_balances[i].Bar, new Vector2(num3 + (float)(i * 136), 0f), new Vector2(131f, 55f));
			}
			if (_seed != null)
			{
				ConfigureMobileSeed(num);
				float num4 = ((_conveyor == null) ? 58 : 0);
				float num5 = (float)Math.Ceiling((double)SeedSlots / (double)num) * 62f + 4f;
				float num6 = screen.Y - num4;
				bool flag2 = num5 > num6;
				if (flag2)
				{
					num6 = HalfCardExtent(num6, 62f, 30f, 4f);
				}
				Mount(_seed, new Vector2(0f, num4), new Vector2(num2, num6), new Vector2(num2, num5), new Vector2(0f, -55f), flag2, vertical: true);
			}
			if (_conveyor != null)
			{
				Mount(_conveyor, new Vector2(num2, 0f), new Vector2(110f, screen.Y), new Vector2(110f, screen.Y));
			}
			if (_slot != null)
			{
				float x = Math.Max(num2, num3 + (float)(_balances.Count * 136)) + 8f;
				Mount(_slot, new Vector2(x, 0f), new Vector2(340f, 108f), new Vector2(340f, 108f));
			}
			ReservedSpace = new Vector2(num2 + (float)((_conveyor != null) ? 110 : 0), (_slot != null) ? 108 : 87);
		}
		else
		{
			float num7 = _balances.Count * 80;
			for (int j = 0; j < _balances.Count; j++)
			{
				Place(_balances[j].Bar, new Vector2(j * 80, 0f), new Vector2(80f, 87f));
			}
			float x2 = Battle.uITopPropContainer.GetCombinedMinimumSize().X;
			float num8 = ((_slot != null) ? 344 : 0);
			int num9 = ((_seed != null) ? 1 : 0) + ((_conveyor != null) ? 1 : 0);
			float num10 = Math.Max(100f, screen.X - 112f - x2 - num7 - num8 - (float)(num9 * 4));
			float num11 = ((num9 == 2) ? (num10 / 2f) : num10);
			float num12 = ((_seed != null) ? (SeedSlots * 51 - 1) : 0);
			if (_seed != null && num12 + 12f > num11)
			{
				num11 = HalfCardExtent(num11 - 12f, 51f, 25f) + 12f;
			}
			if (_seed != null)
			{
				float num13 = Math.Min(num11, num12 + 12f);
				float num14 = num13 - 12f;
				bool flag3 = (float)(_seed.packetNum * 51 - 1) > num14;
				_seedFrame.Texture = _seed.pcSeedBankTexture.Texture;
				Place(_seedFrame, new Vector2(num7 - 78f, 0f), new Vector2(num13 + 78f, 87f));
				Mount(_seed, new Vector2(num7, 6f), new Vector2(num14, 70f), new Vector2(Math.Max(num14, flag3 ? num12 : num14), 70f), new Vector2(-78f, -6f), flag3);
				num7 += ((num9 == 2) ? num11 : num13) + 4f;
			}
			if (_conveyor != null)
			{
				float num15 = Math.Min(num11, 781f);
				_conveyor.SetHudWidth(num15);
				Mount(_conveyor, new Vector2(num7, 0f), new Vector2(num15, 87f), new Vector2(num15, 87f));
				num7 += num15 + 4f;
			}
			if (_slot != null)
			{
				Mount(_slot, new Vector2(num7, 0f), new Vector2(340f, 108f), new Vector2(340f, 108f));
			}
			ReservedSpace = new Vector2(0f, (_slot != null) ? 108 : 87);
		}
		if (Battle.uITopPropContainer.GetChildCount() > 0)
		{
			Battle.uITopPropContainer.TopLevel = true;
			Battle.uITopPropContainer.Position = new Vector2(screen.X - 112f - Battle.uITopPropContainer.GetCombinedMinimumSize().X, 0f);
		}
	}

	private static float HalfCardExtent(float available, float pitch, float half, float inset = 0f)
	{
		return inset + Math.Max(0f, Mathf.Floor((available - inset - half) / pitch)) * pitch + half;
	}

	private void BuildBalances(bool mobile)
	{
		foreach (var balance in _balances)
		{
			RemoveChild(balance.Bar);
			balance.Bar.QueueFree();
		}
		_balances.Clear();
		List<EconomyAccountId> list = new List<EconomyAccountId>();
		if (_seed != null)
		{
			list.Add(_seed.HasSunAccount ? _seed.SunAccountId : EconomyAccountId.Local);
		}
		ConveyorBeltManager conveyor = _conveyor;
		if (conveyor != null && conveyor.isSunType)
		{
			EconomyAccountId item = (_conveyor.HasSunAccount ? _conveyor.SunAccountId : EconomyAccountId.Local);
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		if ((_rain != null || _slot != null) && !list.Contains(EconomyAccountId.Local))
		{
			list.Add(EconomyAccountId.Local);
		}
		foreach (EconomyAccountId item2 in list)
		{
			Texture2D texture = (mobile ? (_seed?.mobileSunBarTexture.Texture ?? GD.Load<Texture2D>("res://Asset/Texture/TowerDefense/SeedBank/SunBankMobile.png")) : GD.Load<Texture2D>("res://Asset/Texture/TowerDefense/SeedBank/SunBank.png"));
			TextureRect textureRect = new TextureRect
			{
				Texture = texture,
				MouseFilter = MouseFilterEnum.Ignore
			};
			Label label = new Label
			{
				Position = (mobile ? new Vector2(54f, 18f) : new Vector2(9f, 60f)),
				Size = new Vector2(59f, 23f),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore
			};
			label.AddThemeColorOverride("font_color", Colors.Black);
			label.AddThemeFontOverride("font", GD.Load<FontFile>("res://Asset/Font/fzcq.ttf"));
			label.AddThemeFontSizeOverride("font_size", 16);
			textureRect.AddChild(label, forceReadableName: false, InternalMode.Disabled);
			AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
			_balances.Add((item2, textureRect, label));
		}
	}

	private void ConfigureMobileSeed(int columns)
	{
		Control[] array = new Control[4]
		{
			_seed.mobileISeedContain,
			(Control)_seed.mobilePacketContainer,
			(Control)_seed.mobilePacketSlotContainer,
			_seed.mobileItemContainer
		};
		foreach (Control control in array)
		{
			if (!_seedGeometry.ContainsKey(control))
			{
				_seedGeometry.Add(control, (control.CustomMinimumSize, control.Size));
			}
		}
		float num = (float)Math.Ceiling((double)SeedSlots / (double)columns) * 62f;
		float num2 = ((columns == 2) ? 194 : 96);
		_seed.mobileISeedContain.CustomMinimumSize = new Vector2(num2, 0f);
		array = new Control[2]
		{
			(Control)_seed.mobilePacketContainer,
			(Control)_seed.mobilePacketSlotContainer
		};
		foreach (Control obj in array)
		{
			obj.CustomMinimumSize = new Vector2(0f, num);
			obj.Size = new Vector2(num2, num);
		}
		_seed.mobileItemContainer.Size = new Vector2(Math.Max(131f, num2), num + 59f);
	}

	private BankView Mount(Control control, Vector2 position, Vector2 size, Vector2 extent, Vector2 offset = default(Vector2), bool scrollable = false, bool vertical = false)
	{
		BankView bankView = _views[control];
		bankView.Vertical = vertical;
		bankView.Scrollable = scrollable;
		bankView.Scroll.Visible = true;
		bankView.Scroll.ClipContents = true;
		bankView.Scroll.MouseFilter = MouseFilterEnum.Pass;
		bankView.Scroll.ConfigureScrolling(scrollable, vertical);
		bankView.Content.CustomMinimumSize = extent;
		bankView.Content.Size = extent;
		Place(bankView.Scroll, position, size);
		control.Position = offset;
		return bankView;
	}

	private void RestoreLegacyLayout()
	{
		if (GodotObject.IsInstanceValid(_seed))
		{
			_seed.pcSeedBankTexture.Visible = true;
		}
		foreach (KeyValuePair<Control, (Vector2, Vector2)> item in _seedGeometry)
		{
			if (GodotObject.IsInstanceValid(item.Key))
			{
				item.Key.CustomMinimumSize = item.Value.Item1;
				item.Key.Size = item.Value.Item2;
			}
		}
		_seedGeometry.Clear();
		if (_managed)
		{
			bool mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
			if (GodotObject.IsInstanceValid(_conveyor))
			{
				_conveyor.SetHudSingleColumn(enabled: false);
				_conveyor.SetHudWidth(781f);
				_conveyor.ApplyMode(mobilePreset);
			}
			if (GodotObject.IsInstanceValid(_rain))
			{
				_rain.ApplyMode(mobilePreset);
			}
		}
		_managed = false;
		ReservedSpace = Vector2.Zero;
	}

	private void ArrangeLegacy()
	{
		_seedFrame.Visible = false;
		foreach (var balance in _balances)
		{
			balance.Bar.Visible = false;
		}
		Battle.uITopPropContainer.TopLevel = false;
		float num = 0f;
		float num2 = 80f;
		foreach (KeyValuePair<Control, BankView> view in _views)
		{
			if (Usable(view.Key))
			{
				num2 = Math.Max(num2, view.Key.GetCombinedMinimumSize().Y);
			}
		}
		foreach (KeyValuePair<Control, BankView> view2 in _views)
		{
			BankView value = view2.Value;
			value.Scrollable = false;
			value.Scroll.Visible = Usable(view2.Key);
			if (value.Scroll.Visible)
			{
				value.Scroll.ClipContents = false;
				value.Scroll.MouseFilter = MouseFilterEnum.Ignore;
				value.Scroll.ConfigureScrolling(enabled: false, vertical: false);
				TowerDefenseCardScroll scroll = value.Scroll;
				int scrollHorizontal = (value.Scroll.ScrollVertical = 0);
				scroll.ScrollHorizontal = scrollHorizontal;
				Vector2 vector = new Vector2(view2.Key.GetCombinedMinimumSize().X, num2);
				value.Content.CustomMinimumSize = vector;
				value.Content.Size = vector;
				Place(value.Scroll, new Vector2(num, 0f), vector);
				Place(view2.Key, Vector2.Zero, vector);
				num += vector.X;
			}
		}
		base.CustomMinimumSize = new Vector2(num, num2);
	}

	private static void Place(Control control, Vector2 position, Vector2 size)
	{
		control.Position = position;
		control.Size = size;
	}
}
