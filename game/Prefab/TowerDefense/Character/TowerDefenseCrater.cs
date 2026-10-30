using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseCrater : TowerDefenseCharacter
{
	public WeatheringComponent weatheringComponent;

	public EnvironmentAnimeComponent environmentAnimeComponent;

	public double dieDownTimer;

	public int stage;

	public int stageMax;

	public double timer;

	public bool weatheringDisabled;

	public bool spawnHypnoses;

	private bool _halfCrater;

	private bool _isWater;

	private bool _isNight;

	public virtual bool HasPendingRevival => false;

	public TowerDefenseEnum.CHARACTER_CAMP RevivalCamp
	{
		get
		{
			if (!spawnHypnoses)
			{
				return TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
			}
			return TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
	}

	public bool halfCrater
	{
		get
		{
			return _halfCrater;
		}
		set
		{
			if (_halfCrater != value)
			{
				_halfCrater = value;
				if (value)
				{
					ApplyHalfCraterStage();
				}
			}
		}
	}

	public bool isWater
	{
		get
		{
			return _isWater;
		}
		set
		{
			if (_isWater != value)
			{
				_isWater = value;
				base.sprite.Position = new Vector2(base.sprite.Position.X, 0f);
				timer = 0.0;
				SetFrame();
			}
		}
	}

	public bool isNight
	{
		get
		{
			return _isNight;
		}
		set
		{
			if (_isNight != value)
			{
				_isNight = value;
				SetFrame();
			}
		}
	}

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		EnsureSpriteBound();
		base._Ready();
		if (editorPreviewMode)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(componentManager))
		{
			weatheringComponent = componentManager.GetRuntime<WeatheringComponent>();
			environmentAnimeComponent = componentManager.GetRuntime<EnvironmentAnimeComponent>();
		}
		HitBoxDestroy();
		AddToGroup("Crater", persistent: true);
		if (base.config is TowerDefenseCraterConfig towerDefenseCraterConfig)
		{
			stageMax = towerDefenseCraterConfig.dieDownFliters.Count;
			if (stageMax > 0)
			{
				stage = Mathf.Clamp(stage, 0, stageMax - 1);
				SetFliter(stage);
			}
		}
		if (_halfCrater)
		{
			ApplyHalfCraterStage();
		}
		if (GodotObject.IsInstanceValid(cell))
		{
			isWater = cell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER);
		}
	}

	private void EnsureSpriteBound()
	{
		if (!GodotObject.IsInstanceValid(base.sprite) && base.config is TowerDefenseCraterConfig towerDefenseCraterConfig && !string.IsNullOrEmpty(towerDefenseCraterConfig.name))
		{
			AdobeAnimateSprite nodeOrNull = GetNodeOrNull<AdobeAnimateSprite>("SpriteGroup/TransformPoint/" + towerDefenseCraterConfig.name);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				base.sprite = nodeOrNull;
			}
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		isNight = TowerDefenseManager.GetMapIsNight();
		if (isWater)
		{
			timer = ((double?)environmentAnimeComponent?.WaterBob((float)delta, (float)timeScale)) ?? timer;
		}
		if (!weatheringDisabled)
		{
			weatheringComponent?.Processing((float)delta);
		}
	}

	public void SetFliter(int _stage)
	{
		if (!(base.config is TowerDefenseCraterConfig towerDefenseCraterConfig))
		{
			return;
		}
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (string dieDownFliter in towerDefenseCraterConfig.dieDownFliters)
		{
			array.Add(dieDownFliter);
		}
		base.sprite.SetFliters(array, open: false);
		base.sprite.SetFliter(towerDefenseCraterConfig.dieDownFliters[_stage], open: true);
	}

	private void ApplyHalfCraterStage()
	{
		stage = 1;
		if (stageMax > 0 && GodotObject.IsInstanceValid(base.sprite))
		{
			SetFliter(stage);
		}
	}

	public void SetFrame()
	{
		environmentAnimeComponent?.SetFrame(isNight, isWater);
	}

	public virtual void DieDown()
	{
		Destroy();
	}

	protected void SpawnWeatheredZombie(string packetName)
	{
		if (isDestroy)
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			Destroy();
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseZombie towerDefenseZombie = packetConfig.Create(logicalGlobalPosition, base.gridPos, base.groundHeight) as TowerDefenseZombie;
		if (!GodotObject.IsInstanceValid(towerDefenseZombie))
		{
			Destroy();
			return;
		}
		towerDefenseZombie.Set("over", true);
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseZombie, forceReadableName: false, InternalMode.Disabled);
		if (spawnHypnoses)
		{
			towerDefenseZombie.Hypnoses();
		}
		towerDefenseZombie.CallDeferred("Walk");
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseZombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, spawnHypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true, base.groundHeight);
			}
		}
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "stage", stage },
			{ "dieDownTimer", dieDownTimer },
			{ "weatheringDisabled", weatheringDisabled },
			{ "spawnHypnoses", spawnHypnoses }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		int num = data.GetValueOrDefault("stage", stage).AsInt32();
		stage = ((stageMax > 0) ? Mathf.Clamp(num, 0, stageMax - 1) : Math.Max(0, num));
		dieDownTimer = Math.Max(0.0, data.GetValueOrDefault("dieDownTimer", dieDownTimer).AsDouble());
		weatheringDisabled = data.GetValueOrDefault("weatheringDisabled", weatheringDisabled).AsBool();
		spawnHypnoses = data.GetValueOrDefault("spawnHypnoses", false).AsBool();
		if (stageMax > 0 && GodotObject.IsInstanceValid(base.sprite))
		{
			SetFliter(stage);
		}
	}
}
