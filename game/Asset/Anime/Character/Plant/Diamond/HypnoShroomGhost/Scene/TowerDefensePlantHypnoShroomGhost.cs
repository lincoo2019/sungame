using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantHypnoShroomGhost : TowerDefensePlant
{
	private const int CostDecreasePerHypnosis = 25;

	private const string PacketName = "PlantHypnoShroomGhost";

	private ExplodeComponent _explodeComponent;

	private bool _initialized;

	private static readonly HashSet<TowerDefenseCharacter> _explodingZombies = new HashSet<TowerDefenseCharacter>();

	private static readonly Dictionary<TowerDefensePacketConfig, int> _baseCosts = new Dictionary<TowerDefensePacketConfig, int>();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			_initialized = true;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
		_explodeComponent = null;
		_initialized = false;
	}

	internal static void ResetBattleState()
	{
		_explodingZombies.Clear();
		_baseCosts.Clear();
	}

	internal static void OnCharacterHypnotized(TowerDefenseCharacter character)
	{
		if (_explodingZombies.Contains(character))
		{
			return;
		}
		List<TowerDefenseInGamePacketShow> hypnoShroomPacketShows = GetHypnoShroomPacketShows();
		TowerDefensePacketConfig towerDefensePacketConfig = null;
		foreach (TowerDefenseInGamePacketShow item in hypnoShroomPacketShows)
		{
			if (GodotObject.IsInstanceValid(item) && GodotObject.IsInstanceValid(item.config) && ((item.originalSaveKey != "") ? item.originalSaveKey : item.config.saveKey) == "PlantHypnoShroomGhost")
			{
				towerDefensePacketConfig = item.config;
				break;
			}
		}
		if (GodotObject.IsInstanceValid(towerDefensePacketConfig) && (towerDefensePacketConfig.GetHypnoses() ? (character is TowerDefensePlant) : (character is TowerDefenseZombie)))
		{
			DecreaseCost();
		}
	}

	private static List<TowerDefenseInGamePacketShow> GetHypnoShroomPacketShows()
	{
		List<TowerDefenseInGamePacketShow> list = new List<TowerDefenseInGamePacketShow>();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return list;
		}
		list.AddRange(TowerDefenseManager.Instance.GetSeedBankList());
		TowerDefenseBattleFeatureConveyorBelt conveyorBeltFeature = TowerDefenseManager.Instance.GetConveyorBeltFeature();
		if (GodotObject.IsInstanceValid(conveyorBeltFeature))
		{
			foreach (Node packetChild in conveyorBeltFeature.GetPacketChildren())
			{
				if (packetChild is TowerDefenseInGamePacketShow item)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private static void EnsureBaseCostInitialized(TowerDefensePacketConfig config)
	{
		if (!_baseCosts.ContainsKey(config))
		{
			_baseCosts[config] = config.GetCost();
		}
	}

	private static void SetPacketCost(TowerDefensePacketConfig config, int cost)
	{
		if (GodotObject.IsInstanceValid(config._override) && _baseCosts.ContainsKey(config) && _baseCosts[config] != config.characterConfig.cost)
		{
			config._override.cost = cost;
		}
		else if (GodotObject.IsInstanceValid(config._override) && config._override.cost != -1)
		{
			config._override.cost = cost;
		}
		else
		{
			config.overrideCost = cost;
		}
	}

	private static void ResetPacketCost(TowerDefensePacketConfig config)
	{
		if (_baseCosts.TryGetValue(config, out var value))
		{
			SetPacketCost(config, value);
			return;
		}
		config.overrideCost = -1;
		if (GodotObject.IsInstanceValid(config._override))
		{
			config._override.cost = -1;
		}
	}

	public void Explode()
	{
		_explodingZombies.Clear();
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieFootballGargantuarBlack");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			Callable.From(delegate
			{
				_explodingZombies.Clear();
			}).CallDeferred();
			return;
		}
		for (int num = 1; num <= mapGridNum.Y; num++)
		{
			if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				Destroy();
				Callable.From(delegate
				{
					_explodingZombies.Clear();
				}).CallDeferred();
				return;
			}
			try
			{
				Vector2I vector2I = new Vector2I((!instance.hypnoses) ? 1 : mapGridNum.X, num);
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
				TowerDefenseCharacter zombie = packetConfig.Create(mapCellPlantPos, vector2I, base.groundHeight);
				if (!GodotObject.IsInstanceValid(zombie))
				{
					continue;
				}
				TowerDefenseGroundItemBase.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				if (!instance.hypnoses)
				{
					_explodingZombies.Add(zombie);
					zombie.Hypnoses();
				}
				IStateMachineController stateMachine = zombie.StateMachine;
				if (stateMachine != null && stateMachine.IsInitialized)
				{
					zombie.SetMainStateMachineDispatchEnabled(enabled: false);
				}
				Tween tween = zombie.CreateTween();
				tween.SetEase(Tween.EaseType.Out);
				tween.SetTrans(Tween.TransitionType.Back);
				tween.TweenProperty(zombie.transformPoint, "scale", Vector2.One, 0.5).From(Vector2.One * 0.5f);
				tween.Finished += delegate
				{
					if (GodotObject.IsInstanceValid(zombie))
					{
						((TowerDefenseZombie)zombie).Walk();
						IStateMachineController stateMachine2 = zombie.StateMachine;
						if (stateMachine2 != null && stateMachine2.IsInitialized)
						{
							zombie.SetMainStateMachineDispatchEnabled(enabled: true);
						}
					}
				};
				if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
				{
					TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
					if (GodotObject.IsInstanceValid(currentControl))
					{
						int nextSyncId = currentControl.GetNextSyncId();
						currentControl.RegisterSyncCharacter(nextSyncId, zombie);
						MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieFootballGargantuarBlack", vector2I.X, vector2I.Y, nextSyncId, 1.0, 1.0, !instance.hypnoses, 0.0, useCreate: true, mapCellPlantPos.X, mapCellPlantPos.Y, walkAfterSpawn: true, base.groundHeight);
					}
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"HypnoShroomGhost Explode error at row {num}: {ex.Message}");
			}
		}
		foreach (TowerDefenseInGamePacketShow hypnoShroomPacketShow in GetHypnoShroomPacketShows())
		{
			if (GodotObject.IsInstanceValid(hypnoShroomPacketShow) && GodotObject.IsInstanceValid(hypnoShroomPacketShow.config) && !(((hypnoShroomPacketShow.originalSaveKey != "") ? hypnoShroomPacketShow.originalSaveKey : hypnoShroomPacketShow.config.saveKey) != "PlantHypnoShroomGhost"))
			{
				EnsureBaseCostInitialized(hypnoShroomPacketShow.config);
				ResetPacketCost(hypnoShroomPacketShow.config);
			}
		}
		Callable.From(delegate
		{
			_explodingZombies.Clear();
		}).CallDeferred();
	}

	private static void DecreaseCost()
	{
		foreach (TowerDefenseInGamePacketShow hypnoShroomPacketShow in GetHypnoShroomPacketShows())
		{
			if (!GodotObject.IsInstanceValid(hypnoShroomPacketShow) || !GodotObject.IsInstanceValid(hypnoShroomPacketShow.config) || ((hypnoShroomPacketShow.originalSaveKey != "") ? hypnoShroomPacketShow.originalSaveKey : hypnoShroomPacketShow.config.saveKey) != "PlantHypnoShroomGhost")
			{
				continue;
			}
			EnsureBaseCostInitialized(hypnoShroomPacketShow.config);
			int num = hypnoShroomPacketShow.config.GetCost();
			if (num >= 0)
			{
				int num2 = num - 25;
				if (num2 < 0)
				{
					num2 = 0;
				}
				SetPacketCost(hypnoShroomPacketShow.config, num2);
			}
		}
	}
}
