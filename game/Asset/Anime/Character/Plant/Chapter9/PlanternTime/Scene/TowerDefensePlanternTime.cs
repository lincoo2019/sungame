using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlanternTime : TowerDefensePlant
{
	private PointLight2D _light;

	private GpuParticles2D _planternFog;

	private const double CooldownPercentage = 0.25;

	private static readonly Vector2I[] SURROUND_OFFSETS = new Vector2I[9]
	{
		new Vector2I(-1, -1),
		new Vector2I(0, -1),
		new Vector2I(1, -1),
		new Vector2I(-1, 0),
		new Vector2I(0, 0),
		new Vector2I(1, 0),
		new Vector2I(-1, 1),
		new Vector2I(0, 1),
		new Vector2I(1, 1)
	};

	private readonly HashSet<string> _accelerateKeys = new HashSet<string>();

	private readonly HashSet<string> _decelerateKeys = new HashSet<string>();

	private bool _paused;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNode<PointLight2D>("%Light");
			_planternFog = GetNodeOrNull<GpuParticles2D>("%PlanternFog");
			AudioManager.Instance.AudioPlay("Plantern");
		}
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint())
		{
			ClearAllCooldownEffect();
		}
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base.BatchUpdate(delta);
		if (GodotObject.IsInstanceValid(_light))
		{
			_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
		if (GodotObject.IsInstanceValid(_planternFog) && !_planternFog.Visible)
		{
			_planternFog.Visible = true;
		}
		if (inGame && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && TowerDefenseManager.Instance.IsGameRunning())
		{
			if (die)
			{
				ClearAllCooldownEffect();
			}
			else
			{
				UpdateCooldownEffect();
			}
		}
	}

	private void UpdateCooldownEffect()
	{
		if (TowerDefenseManager.Instance.backPacket || TowerDefenseManager.Instance.pausePacket)
		{
			if (!_paused)
			{
				ClearAllCooldownEffect();
				_paused = true;
			}
			return;
		}
		_paused = false;
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		Vector2I[] sURROUND_OFFSETS = SURROUND_OFFSETS;
		foreach (Vector2I vector2I in sURROUND_OFFSETS)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos + vector2I);
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				continue;
			}
			foreach (TowerDefenseCharacter character in mapCell.GetCharacterList())
			{
				if (!(character is TowerDefensePlant towerDefensePlant) || character is TowerDefensePlantBowlingBase || towerDefensePlant == this || towerDefensePlant.die)
				{
					continue;
				}
				TowerDefensePacketConfig towerDefensePacketConfig = towerDefensePlant.packet;
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
				{
					continue;
				}
				TowerDefenseEnum.PACKET_TYPE pACKET_TYPE = towerDefensePacketConfig._GetType();
				if ((pACKET_TYPE == TowerDefenseEnum.PACKET_TYPE.WHITE || pACKET_TYPE == TowerDefenseEnum.PACKET_TYPE.ORIGINAL) ? true : false)
				{
					string saveKey = towerDefensePacketConfig.saveKey;
					if (towerDefensePacketConfig.GetHypnoses() == instance.hypnoses)
					{
						hashSet.Add(saveKey);
					}
					else
					{
						hashSet2.Add(saveKey);
					}
				}
			}
		}
		UpdateCooldownSet(_accelerateKeys, hashSet, 0.25, "_PTAcc");
		UpdateCooldownSet(_decelerateKeys, hashSet2, -0.25, "_PTDec");
	}

	private void UpdateCooldownSet(HashSet<string> activeKeys, HashSet<string> currentKeys, double percentage, string keySuffix)
	{
		foreach (string currentKey in currentKeys)
		{
			if (activeKeys.Add(currentKey))
			{
				ApplyCooldownToPacketShow(currentKey, percentage, keySuffix);
			}
		}
		activeKeys.RemoveWhere(delegate(string saveKey)
		{
			if (!currentKeys.Contains(saveKey))
			{
				ApplyCooldownToPacketShow(saveKey, percentage, keySuffix, remove: true);
				return true;
			}
			return false;
		});
	}

	private void ApplyCooldownToPacketShow(string saveKey, double percentage, string keySuffix, bool remove = false)
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		foreach (TowerDefenseInGamePacketShow seedBank in TowerDefenseManager.Instance.GetSeedBankList())
		{
			if (!GodotObject.IsInstanceValid(seedBank?.config) || seedBank.config.saveKey != saveKey)
			{
				continue;
			}
			TowerDefensePacketConfig towerDefensePacketConfig = seedBank.config;
			string key = saveKey + keySuffix;
			if (remove)
			{
				towerDefensePacketConfig.ColdDownDecreaseDelete(this, key);
			}
			else
			{
				towerDefensePacketConfig.ColdDownDecreaseAdd(this, key, percentage);
			}
			if (!seedBank.coldDownOpen)
			{
				break;
			}
			double coldDown = seedBank.coldDown;
			double packetCooldown = towerDefensePacketConfig.GetPacketCooldown();
			if (coldDown > 0.0 && packetCooldown > 0.0)
			{
				double num = packetCooldown / coldDown;
				seedBank.coldDownTimer = Math.Clamp(seedBank.coldDownTimer * num, 0.0, packetCooldown);
				seedBank.coldDown = packetCooldown;
				if (GodotObject.IsInstanceValid(seedBank.coldDownProgressBar))
				{
					seedBank.coldDownProgressBar.MaxValue = packetCooldown;
					seedBank.coldDownProgressBar.Value = seedBank.coldDownTimer;
				}
			}
			break;
		}
	}

	private void ClearAllCooldownEffect()
	{
		foreach (string accelerateKey in _accelerateKeys)
		{
			ApplyCooldownToPacketShow(accelerateKey, 0.25, "_PTAcc", remove: true);
		}
		_accelerateKeys.Clear();
		foreach (string decelerateKey in _decelerateKeys)
		{
			ApplyCooldownToPacketShow(decelerateKey, -0.25, "_PTDec", remove: true);
		}
		_decelerateKeys.Clear();
	}
}
