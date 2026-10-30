using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantJalaVase : TowerDefensePlant
{
	private const int MAX_JALA_COUNT = 4;

	private const string JALA_VASE_BACK5 = "uid://pnu58qk7jitu";

	private const string JALA_VASE_BODY2 = "uid://3jm2ghlcepiq";

	private const string JALA_VASE_BODY3 = "uid://co3xrnatu6j8a";

	private const string JALA_VASE_BODY4 = "uid://bap8un78l21il";

	private const string JALA_VASE_BODY5 = "uid://j6cc4gemmsje";

	private const string JALA_VASE_JALA4 = "uid://buvtla2up6tvu";

	private const string JALA_VASE_JALA5 = "uid://b57i3wamhxyrb";

	private static PackedScene _JALA_VASE_CHUNKS2;

	private static PackedScene _JALA_VASE_CHUNKS;

	private AdobeAnimateSpriteBase _hammer;

	private MousePressComponent _mousePressComponent;

	private StateHandle _attackState;

	private bool _roleStateSignalsConnected;

	public bool isMoseIn;

	public bool pressed;

	public bool over;

	public Godot.Collections.Array jalaNameList = new Godot.Collections.Array
	{
		"PlantJalapeno", "PlantJalaNut", "PlantJalaNutBowling", "PlantJalaCherryBomb", "PlantJalaSunShroom", "PlantJalaPurify", "PlantJalaTorch", "PlantJalaJoker", "PlantJalapenopepe", "PlantGarlicJalapeno",
		"PlantJalaGhost"
	};

	public PackedScene chunksEffect;

	public Array<TowerDefensePacketConfig> jalaList = new Array<TowerDefensePacketConfig>();

	public Array<double> timerList = new Array<double> { 0.0, 0.0, 0.0, 0.0 };

	public double timeNeed = 50.0;

	public bool pressAwait;

	private static PackedScene JALA_VASE_CHUNKS2 => _JALA_VASE_CHUNKS2 ?? (_JALA_VASE_CHUNKS2 = GD.Load<PackedScene>("uid://btoja37o43wvj"));

	private static PackedScene JALA_VASE_CHUNKS => _JALA_VASE_CHUNKS ?? (_JALA_VASE_CHUNKS = GD.Load<PackedScene>("uid://tw8msyx3iwy"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_hammer = GetNode<AdobeAnimateSpriteBase>("%Hammer");
			_hammer.OnAnimeCompleted += HammerAnimeCompleted;
			_mousePressComponent = componentManager?.GetRuntime<MousePressComponent>();
			MousePressComponent mousePressComponent = _mousePressComponent;
			if (mousePressComponent != null && !mousePressComponent.IsReleased)
			{
				_mousePressComponent.OnPressed += HandleVasePressed;
			}
			chunksEffect = JALA_VASE_CHUNKS;
			_attackState = base.StateMachine?.GetStateById("plant.jala_vase.attack");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		MousePressComponent mousePressComponent = _mousePressComponent;
		if (mousePressComponent != null && !mousePressComponent.IsReleased)
		{
			_mousePressComponent.OnPressed -= HandleVasePressed;
		}
		_mousePressComponent = null;
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (!_roleStateSignalsConnected)
		{
			StateHandle attackState = _attackState;
			if (attackState != null && attackState.IsValid)
			{
				_attackState.Entered += AttackEntered;
				_attackState.Exited += AttackExited;
				_attackState.PhysicsProcessing += AttackProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_attackState.Entered -= AttackEntered;
			_attackState.Exited -= AttackExited;
			_attackState.PhysicsProcessing -= AttackProcessing;
			_attackState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base.BatchUpdate(delta);
		isMoseIn = _mousePressComponent?.IsMouseInside ?? false;
		if ((Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage") || !TowerDefenseManager.Instance.IsGameRunning())
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < jalaList.Count; i++)
		{
			if (timerList[i] < timeNeed)
			{
				timerList[i] += delta;
				continue;
			}
			timerList[i] = 0.0;
			ExecuteJala(jalaList[i]);
			flag = true;
		}
		if (flag)
		{
			SendStateEvent("ToAttack");
		}
	}

	private void HandleVasePressed(Vector2 pointerPosition)
	{
		if (pressAwait || pressed)
		{
			return;
		}
		PacketPickControl packetPickControl = TowerDefenseManager.Instance.GetPacketPickControl();
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = (GodotObject.IsInstanceValid(packetPickControl) ? packetPickControl.packetPick : null);
		if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			if (!instance.hypnoses)
			{
				_hammer.Visible = true;
				AudioManager.Instance.AudioPlay("Swing");
				_hammer.SetAnimation("OpenPot", loop: false);
				pressed = true;
			}
		}
		else if (CanAddJala(towerDefenseInGamePacketShow.config))
		{
			TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
			List<TowerDefenseInGamePacketShow.ColumnPlacement> placements = ((GodotObject.IsInstanceValid(mapFeature) && mapFeature.isPlantColumn) ? packetPickControl.CollectColumnPlacements(base.gridPos.X) : new List<TowerDefenseInGamePacketShow.ColumnPlacement>
			{
				new TowerDefenseInGamePacketShow.ColumnPlacement(base.gridPos, this, "jala_vase")
			});
			if (packetPickControl.ProcessColumnPlacements(placements))
			{
				if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
				{
					LevelEditorMapEditor.instance.levelConfig.canExport = false;
				}
				else
				{
					packetPickControl.Release();
				}
			}
		}
		pressAwait = true;
		CallDeferred("_ResetPressAwait");
	}

	private async void _ResetPressAwait()
	{
		await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		pressAwait = false;
	}

	public virtual void AttackEntered()
	{
		base.sprite.SetAnimation("Fire", loop: false, 0.1);
	}

	public virtual void AttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void AttackExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Fire"))
		{
			if (clip == "Load")
			{
				Idle();
			}
		}
		else
		{
			Idle();
		}
	}

	public override async void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 globalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
		AudioManager.Instance.AudioPlay("VaseBreaking");
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(chunksEffect, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = globalPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		foreach (TowerDefensePacketConfig jala in jalaList)
		{
			if (instance.hypnoses)
			{
				jala.overrideHypnoses = true;
			}
			TowerDefenseManager.Instance.SpawnPacket(jala, logicalGlobalPosition + new Vector2(0f, 0f - (float)base.groundHeight), 15.0, isFall: false);
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		foreach (TowerDefensePacketConfig jala in jalaList)
		{
			jala.overrideHypnoses = instance.hypnoses;
		}
	}

	public void HammerAnimeCompleted(string clip)
	{
		if (clip == "OpenPot")
		{
			_hammer.Visible = false;
			Destroy();
		}
	}

	public bool CanAddJala(TowerDefensePacketConfig packetConfig)
	{
		if (!isDestroy && !die && !nearDie && !pressed && GodotObject.IsInstanceValid(instance) && !instance.hologram && !instance.invincible && instance.canBeCollection && jalaList.Count < 4 && GodotObject.IsInstanceValid(packetConfig) && packetConfig.characterConfig is TowerDefensePlantConfig && (packetConfig.characterConfig.physiqueTypeFlags & 0x80) != 0)
		{
			return instance.hypnoses == packetConfig.GetHypnoses();
		}
		return false;
	}

	public void AddJala(TowerDefensePacketConfig packetConfig)
	{
		TryAddJala(packetConfig);
	}

	public bool TryAddJala(TowerDefensePacketConfig packetConfig)
	{
		if (!CanAddJala(packetConfig))
		{
			return false;
		}
		if (packetConfig.saveKey == "PlantPresentBox")
		{
			packetConfig = TowerDefenseManager.GetPacketConfig(jalaNameList.PickRandom().AsString());
		}
		else if (packetConfig.saveKey == "PlantPresentBoxGreen")
		{
			packetConfig = TowerDefenseManager.GetPacketConfig(jalaNameList.PickRandom().AsString());
			EconomyAccountId accountId = (base.HasEconomyOwner ? base.EconomyOwnerAccountId : EconomyAccountId.Local);
			long num = (GodotObject.IsInstanceValid(packetConfig) ? packetConfig.characterConfig.cost : 0);
			SunSpendReceipt receipt = null;
			if (num > 0 && TowerDefenseManager.Instance.TryBeginSunSpend(accountId, num, out receipt))
			{
				receipt?.TryCommit();
			}
			else if (num != 0L)
			{
				long sun = TowerDefenseManager.Instance.GetSun(accountId);
				if (sun >= 0)
				{
					TowerDefenseManager.Instance.SetSun(accountId, sun - num, SunTransactionReason.Cost);
				}
			}
		}
		if (instance.hypnoses)
		{
			packetConfig.overrideHypnoses = true;
		}
		packetConfig.ColdDownDecreaseAdd(this, "JalaVase", 0.25);
		base.sprite.SetAnimation("Load", loop: false, 0.1);
		jalaList.Add(packetConfig);
		ApplyJalaPresentation();
		ExecuteJala(packetConfig);
		return true;
	}

	public void ExecuteJala(TowerDefensePacketConfig packetConfig)
	{
		string saveKey = packetConfig.saveKey;
		if (saveKey == null)
		{
			return;
		}
		switch (saveKey.Length)
		{
		case 19:
			switch (saveKey[9])
			{
			default:
				return;
			case 'N':
				break;
			case 'C':
				if (saveKey == "PlantJalaCherryBomb")
				{
					if (base.gridPos.Y > 1)
					{
						TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos - new Vector2I(0, 1), 450.0);
					}
					TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 450.0);
					if (base.gridPos.Y < TowerDefenseManager.Instance.GetMapGridNum().Y)
					{
						TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos + new Vector2I(0, 1), 450.0);
					}
				}
				return;
			case 'i':
				if (saveKey == "PlantGarlicJalapeno")
				{
					TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 125.0);
				}
				return;
			}
			if (!(saveKey == "PlantJalaNutBowling"))
			{
				break;
			}
			goto IL_0125;
		case 14:
			switch (saveKey[9])
			{
			default:
				return;
			case 'T':
				break;
			case 'J':
				if (saveKey == "PlantJalaJoker")
				{
					TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 450.0);
					TowerDefenseCharacter.CreateJalapenoFireColumn(base.camp, base.gridPos, 450.0);
				}
				return;
			case 'G':
				if (saveKey == "PlantJalaGhost")
				{
					TowerDefenseCharacter.CreateJalapenoFireSlash(base.camp, base.gridPos, 750.0);
				}
				return;
			}
			if (!(saveKey == "PlantJalaTorch"))
			{
				break;
			}
			goto IL_0149;
		case 13:
			if (!(saveKey == "PlantJalapeno"))
			{
				break;
			}
			goto IL_0125;
		case 12:
			if (!(saveKey == "PlantJalaNut"))
			{
				break;
			}
			goto IL_0125;
		case 17:
			if (!(saveKey == "PlantJalapenopepe"))
			{
				break;
			}
			goto IL_0149;
		case 18:
			if (saveKey == "PlantJalaSunShroom")
			{
				Bright(0.0, 0.0, 0.5, 1.5, 0.5);
				if (JalapenoSunCreate(GetLogicalGlobalPosition(), 15L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f)) is TowerDefenseSunJalapeno towerDefenseSunJalapeno && instance.hypnoses)
				{
					towerDefenseSunJalapeno.zombieCamp = true;
				}
			}
			break;
		case 15:
			if (saveKey == "PlantJalaPurify")
			{
				TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 450.0);
			}
			break;
		case 16:
			break;
			IL_0125:
			TowerDefenseCharacter.CreateJalapenoFire(base.camp, base.gridPos, 450.0);
			break;
			IL_0149:
			TowerDefenseCharacter.CreateJalapenoFireColumn(base.camp, base.gridPos, 450.0);
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		Array<double> array2 = new Array<double>();
		for (int i = 0; i < jalaList.Count && i < 4; i++)
		{
			TowerDefensePacketConfig towerDefensePacketConfig = jalaList[i];
			if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				Dictionary dictionary = new Dictionary
				{
					["saveKey"] = towerDefensePacketConfig.saveKey,
					["overrideHypnoses"] = towerDefensePacketConfig.overrideHypnoses,
					["overrideCostRise"] = towerDefensePacketConfig.overrideCostRise,
					["overrideCost"] = towerDefensePacketConfig.overrideCost,
					["overridePacketCooldown"] = towerDefensePacketConfig.overridePacketCooldown,
					["overrideStartingCooldown"] = towerDefensePacketConfig.overrideStartingCooldown,
					["overrideWeight"] = towerDefensePacketConfig.overrideWeight,
					["overrideWavePointCost"] = towerDefensePacketConfig.overrideWavePointCost
				};
				TowerDefensePacketRuntimeState.Write(towerDefensePacketConfig, dictionary, "overrideSave", "canChangeCost", "changeCostList");
				array.Add(dictionary);
				array2.Add((i < timerList.Count) ? timerList[i] : 0.0);
			}
		}
		return new Dictionary
		{
			{ "isMoseIn", isMoseIn },
			{ "pressed", pressed },
			{ "over", over },
			{ "timeNeed", timeNeed },
			{ "pressAwait", pressAwait },
			{ "jalaList", array },
			{ "timerList", array2 }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isMoseIn = data.GetValueOrDefault("isMoseIn", Variant.From<bool>(false)).AsBool();
		pressed = data.GetValueOrDefault("pressed", Variant.From<bool>(false)).AsBool();
		over = data.GetValueOrDefault("over", Variant.From<bool>(false)).AsBool();
		timeNeed = data.GetValueOrDefault("timeNeed", Variant.From<double>(50.0)).AsDouble();
		pressAwait = data.GetValueOrDefault("pressAwait", Variant.From<bool>(false)).AsBool();
		jalaList.Clear();
		timerList = new Array<double> { 0.0, 0.0, 0.0, 0.0 };
		Godot.Collections.Array array = data.GetValueOrDefault("jalaList", new Godot.Collections.Array()).AsGodotArray();
		Godot.Collections.Array array2 = data.GetValueOrDefault("timerList", new Godot.Collections.Array()).AsGodotArray();
		int num = Math.Min(array.Count, 4);
		for (int i = 0; i < num; i++)
		{
			Dictionary packetData = array[i].AsGodotDictionary();
			TowerDefensePacketConfig towerDefensePacketConfig = RestoreJalaPacket(packetData);
			if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				towerDefensePacketConfig.ColdDownDecreaseAdd(this, "JalaVase", 0.25);
				jalaList.Add(towerDefensePacketConfig);
				int index = jalaList.Count - 1;
				timerList[index] = ((i < array2.Count) ? Math.Max(0.0, array2[i].AsDouble()) : 0.0);
			}
		}
		ApplyJalaPresentation();
	}

	private TowerDefensePacketConfig RestoreJalaPacket(Dictionary packetData)
	{
		string text = packetData.GetValueOrDefault("saveKey", "").AsString();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (!TowerDefensePacketRuntimeState.TryCreate(text, packetData, "overrideSave", "canChangeCost", "changeCostList", out var packetConfig))
		{
			return null;
		}
		packetConfig.overrideHypnoses = packetData.GetValueOrDefault("overrideHypnoses", packetConfig.overrideHypnoses).AsBool();
		packetConfig.overrideCostRise = packetData.GetValueOrDefault("overrideCostRise", packetConfig.overrideCostRise).AsInt32();
		packetConfig.overrideCost = packetData.GetValueOrDefault("overrideCost", packetConfig.overrideCost).AsInt32();
		packetConfig.overridePacketCooldown = packetData.GetValueOrDefault("overridePacketCooldown", packetConfig.overridePacketCooldown).AsDouble();
		packetConfig.overrideStartingCooldown = packetData.GetValueOrDefault("overrideStartingCooldown", packetConfig.overrideStartingCooldown).AsDouble();
		packetConfig.overrideWeight = packetData.GetValueOrDefault("overrideWeight", packetConfig.overrideWeight).AsInt32();
		packetConfig.overrideWavePointCost = packetData.GetValueOrDefault("overrideWavePointCost", packetConfig.overrideWavePointCost).AsInt32();
		if (instance.hypnoses)
		{
			packetConfig.overrideHypnoses = true;
		}
		return packetConfig;
	}

	private void ApplyJalaPresentation()
	{
		chunksEffect = JALA_VASE_CHUNKS;
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			switch (jalaList.Count)
			{
			case 1:
				base.sprite.SetAtlasReplace("JalaVase_body.png", "uid://3jm2ghlcepiq");
				break;
			case 2:
				base.sprite.SetAtlasReplace("JalaVase_body.png", "uid://co3xrnatu6j8a");
				break;
			case 3:
				base.sprite.SetAtlasReplace("JalaVase_body.png", "uid://bap8un78l21il");
				base.sprite.SetAtlasReplace("JalaVase_jala.png", "uid://buvtla2up6tvu");
				break;
			case 4:
				base.sprite.SetAtlasReplace("JalaVase_body.png", "uid://j6cc4gemmsje");
				base.sprite.SetAtlasReplace("JalaVase_jala.png", "uid://b57i3wamhxyrb");
				base.sprite.SetAtlasReplace("JalaVase_back.png", "uid://pnu58qk7jitu");
				chunksEffect = JALA_VASE_CHUNKS2;
				break;
			}
		}
	}
}
