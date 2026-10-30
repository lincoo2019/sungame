using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureLookStar : TowerDefenseBattleFeature
{
	public TowerDefenseLevelLookStarManagerConfig config;

	public CanvasLayer characterLayer;

	public Dictionary checkDictionary = new Dictionary();

	public bool isfinish;

	private Node2D _lookStarNode;

	private double _checkTimer;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseLevelLookStarManagerConfig();
		config.Init(data);
		characterLayer = new CanvasLayer();
		characterLayer.FollowViewportEnabled = true;
		characterLayer.Layer = config.previewLayer;
		_lookStarNode = new Node2D();
		control.AddNode(_lookStarNode, 2);
		_lookStarNode.AddChild(characterLayer, forceReadableName: false, Node.InternalMode.Disabled);
	}

	public override Task GameInit()
	{
		InitializeChecks(createPreviews: true);
		FreshGround();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		InitializeChecks(createPreviews: false);
		return Task.CompletedTask;
	}

	public override void Process(double _delta)
	{
		if (config.open && TowerDefenseManager.Instance.IsGameRunning() && !isfinish)
		{
			_checkTimer += _delta;
			if (!(_checkTimer < config.checkInterval))
			{
				_checkTimer %= config.checkInterval;
				FreshGround();
			}
		}
	}

	public void FreshGround()
	{
		if (config.checkList.Count == 0)
		{
			return;
		}
		bool flag = true;
		bool flag2 = false;
		foreach (TowerDefenseLevelLookStarCheckConfig check in config.checkList)
		{
			if (check == null)
			{
				continue;
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(check.gridPos);
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				continue;
			}
			flag2 = true;
			bool flag3 = mapCell.HasCharacter(check.packetName);
			if (checkDictionary.TryGetValue(check, out var value))
			{
				AdobeAnimateSprite adobeAnimateSprite = value.As<AdobeAnimateSprite>();
				if (GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					adobeAnimateSprite.Visible = !flag3;
				}
				if (!flag3)
				{
					flag = false;
				}
			}
		}
		if (!(flag2 && flag))
		{
			return;
		}
		TowerDefenseControlNew towerDefenseControlNew = control;
		if (towerDefenseControlNew != null && towerDefenseControlNew.process?.TryFinish() == true)
		{
			TowerDefenseBattleFeatureWave towerDefenseBattleFeatureWave = GetFeature("Wave") as TowerDefenseBattleFeatureWave;
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave))
			{
				towerDefenseBattleFeatureWave.waveFinal = true;
				towerDefenseBattleFeatureWave.spawnOver = true;
			}
			isfinish = true;
		}
	}

	public bool IsOpen()
	{
		return config.open;
	}

	public bool OnWaveReachFinal(TowerDefenseBattleFeatureWave waveFeature)
	{
		if (!config.open || isfinish)
		{
			return false;
		}
		int num = Mathf.Max(1, waveFeature.config.flagWaveInterval);
		int currentWave = Mathf.Max(0, (waveFeature.config.wave.Count - 1) / num * num);
		waveFeature.waveFinal = false;
		waveFeature.currentWave = currentWave;
		waveFeature.spawnOver = false;
		waveFeature.nextWaveTime = waveFeature.config.spawnColStart;
		return true;
	}

	public override Dictionary SaveFeature()
	{
		return new Dictionary
		{
			["isfinish"] = isfinish,
			["checkTimer"] = _checkTimer
		};
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary { ["isfinish"] = isfinish };
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		if (_data.ContainsKey("isfinish"))
		{
			isfinish = _data["isfinish"].AsBool();
			if (isfinish)
			{
				SetAllPreviewsVisible(visible: false);
			}
			else
			{
				FreshGround();
			}
		}
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		isfinish = _data.GetValueOrDefault("isfinish", false).AsBool();
		_checkTimer = Math.Max(0.0, _data.GetValueOrDefault("checkTimer", 0.0).AsDouble());
		if (isfinish)
		{
			SetAllPreviewsVisible(visible: false);
			return;
		}
		InitializeChecks(createPreviews: true);
		FreshGround();
	}

	private void InitializeChecks(bool createPreviews)
	{
		foreach (TowerDefenseLevelLookStarCheckConfig check in config.checkList)
		{
			if (check != null && !string.IsNullOrWhiteSpace(check.packetName))
			{
				if (!checkDictionary.ContainsKey(check))
				{
					checkDictionary[check] = Variant.From<GodotObject>((GodotObject)null);
				}
				if (createPreviews)
				{
					EnsureCheckPreview(check);
				}
			}
		}
	}

	private void EnsureCheckPreview(TowerDefenseLevelLookStarCheckConfig check)
	{
		if (!GodotObject.IsInstanceValid(characterLayer) || (checkDictionary.TryGetValue(check, out var value) && GodotObject.IsInstanceValid(value.AsGodotObject())) || !GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketConfig(check.packetName)))
		{
			return;
		}
		AdobeAnimateSprite characterSprite = TowerDefenseManager.GetCharacterSprite(check.packetName);
		if (GodotObject.IsInstanceValid(characterSprite))
		{
			characterLayer.AddChild(characterSprite, forceReadableName: false, Node.InternalMode.Disabled);
			characterSprite.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(check.gridPos);
			Color meshColor = characterSprite.meshColor;
			meshColor.A = config.previewOpacity;
			characterSprite.meshColor = meshColor;
			characterSprite.pause = true;
			checkDictionary[check] = characterSprite;
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(check.gridPos);
			if (GodotObject.IsInstanceValid(mapCell))
			{
				characterSprite.GlobalPosition = new Vector2(characterSprite.GlobalPosition.X, (float)((double)characterSprite.GlobalPosition.Y - mapCell.GetGroundHeight()));
			}
		}
	}

	private void SetAllPreviewsVisible(bool visible)
	{
		foreach (Variant value in checkDictionary.Values)
		{
			AdobeAnimateSprite adobeAnimateSprite = value.As<AdobeAnimateSprite>();
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.Visible = visible;
			}
		}
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(_lookStarNode))
		{
			_lookStarNode.QueueFree();
		}
		_lookStarNode = null;
		characterLayer = null;
		checkDictionary.Clear();
		config = null;
		base.Destroy();
	}
}
