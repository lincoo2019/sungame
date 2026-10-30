using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureFog : TowerDefenseBattleFeature
{
	private const int FogRightOverlapColumns = 3;

	private const int FogBottomOverlapRows = 1;

	private static PackedScene _towerDefenseFog;

	public Node2D fogNode;

	public TowerDefenseFogBatch fogBatch;

	public TowerDefenseLevelFogManagerConfig config;

	public int beginColumn;

	public Vector2I mapGridNum;

	public Array<Timer> timerList = new Array<Timer>();

	public Array<Array<Tween>> tweenList = new Array<Array<Tween>>();

	public Array<Array<TowerDefenseFog>> fogLine = new Array<Array<TowerDefenseFog>>();

	public double fogNodeInitX;

	private const double MagicTickInterval = 0.25;

	private const double MagicColorFadeDuration = 1.5;

	private const double MagicChangingTipDuration = 2.0;

	private const string MagicChangingTipKey = "TOWERDEFENSE_TIPS_MAGICFOG_CHANGING";

	private double _magicElapsed;

	private double _magicTickTimer;

	private int _magicChangeIndex;

	private int _magicWarningIndex = -1;

	private double _magicFadeRemaining;

	private bool _restoreMagicFade;

	private TowerDefenseRuneMagicColor _magicColor;

	private readonly HashSet<Vector2I> _magicCoveredCells = new HashSet<Vector2I>();

	private readonly List<Tween> _magicColorTweens = new List<Tween>();

	private static PackedScene TOWER_DEFENSE_FOG => _towerDefenseFog ?? (_towerDefenseFog = GD.Load<PackedScene>("uid://cu8nc147admpx"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseLevelFogManagerConfig();
		config.Init(data);
		beginColumn = config.beginColumn;
		TowerDefenseBattleFeatureMap feature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
		if (!GodotObject.IsInstanceValid(feature) || !GodotObject.IsInstanceValid(feature.mapConfig))
		{
			throw new InvalidOperationException("Fog requires an initialized Map feature configuration.");
		}
		mapGridNum = feature.mapConfig.gridNum;
		fogNode = new Node2D();
		control.AddNode(fogNode, 1);
		fogBatch = new TowerDefenseFogBatch();
		fogNode.AddChild(fogBatch, forceReadableName: false, Node.InternalMode.Disabled);
		int num = Math.Max(2, mapGridNum.Y + 2);
		for (int i = 0; i < num; i++)
		{
			Timer timer = new Timer();
			timer.OneShot = true;
			timer.Autostart = false;
			timer.WaitTime = config.blowReturnDelay;
			int lineIdx = i;
			timer.Timeout += delegate
			{
				FogBack(lineIdx);
			};
			timerList.Add(timer);
			fogNode.AddChild(timer, forceReadableName: false, Node.InternalMode.Disabled);
		}
		for (int num2 = 0; num2 < num; num2++)
		{
			fogLine.Add(new Array<TowerDefenseFog>());
			tweenList.Add(new Array<Tween>());
		}
		BattleEventBus.Instance.OnBlowAllEffectEmit += BlowAllEffectEmit;
		BattleEventBus.Instance.OnBlowLineEffectEmit += BlowLineEffectEmit;
		BattleEventBus.Instance.OnGameVictory += HideFog;
	}

	public override void Destroy()
	{
		ClearMagicColorTweens();
		TowerDefenseRuneMagicUtil.ForEachZombie(ClearMagicZombieBuffs);
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnBlowAllEffectEmit -= BlowAllEffectEmit;
			BattleEventBus.Instance.OnBlowLineEffectEmit -= BlowLineEffectEmit;
			BattleEventBus.Instance.OnGameVictory -= HideFog;
		}
		foreach (Array<Tween> tween in tweenList)
		{
			foreach (Tween item in tween)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.Kill();
				}
			}
			tween.Clear();
		}
		timerList.Clear();
		tweenList.Clear();
		fogLine.Clear();
		if (GodotObject.IsInstanceValid(fogNode))
		{
			fogNode.QueueFree();
		}
		fogBatch = null;
		fogNode = null;
		base.Destroy();
	}

	public override Task GameInit()
	{
		for (int i = beginColumn; i < mapGridNum.X + config.extraColumns; i++)
		{
			for (int j = 1; j <= mapGridNum.Y + 1; j++)
			{
				Vector2 mapCellPosCenter = TowerDefenseManager.Instance.GetMapCellPosCenter(new Vector2I(i, j));
				TowerDefenseFog towerDefenseFog = TOWER_DEFENSE_FOG.Instantiate<TowerDefenseFog>(PackedScene.GenEditState.Disabled);
				towerDefenseFog.GlobalPosition = mapCellPosCenter;
				towerDefenseFog.gridPos = new Vector2(i, j);
				towerDefenseFog.beginColumn = beginColumn;
				towerDefenseFog.lightOverlapEnabled = i <= mapGridNum.X + 3 && j <= mapGridNum.Y + 1;
				fogLine[j].Add(towerDefenseFog);
				fogNode.AddChild(towerDefenseFog, forceReadableName: false, Node.InternalMode.Disabled);
				if (towerDefenseFog.lightOverlapEnabled)
				{
					fogBatch.Register(towerDefenseFog);
				}
				else
				{
					towerDefenseFog.area.QueueFree();
				}
			}
		}
		Vector2 mapCellPosCenter2 = TowerDefenseManager.Instance.GetMapCellPosCenter(new Vector2I(beginColumn, 0));
		fogNodeInitX = config.entryStartX - mapCellPosCenter2.X;
		fogNode.GlobalPosition = new Vector2((float)fogNodeInitX, fogNode.GlobalPosition.Y);
		if (config.magicOpen)
		{
			_magicColor = ResolveInitialMagicColor();
			ApplyMagicColorToFog(animated: false);
		}
		return Task.CompletedTask;
	}

	public override async Task GameInitFromProgress()
	{
		await GameInit();
	}

	public override Task GameEntry()
	{
		if (control.hasProgress)
		{
			return Task.CompletedTask;
		}
		for (int i = 1; i <= mapGridNum.Y + 1; i++)
		{
			foreach (Tween item in tweenList[i])
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.Kill();
				}
			}
			tweenList[i].Clear();
			if (GodotObject.IsInstanceValid(timerList[i]))
			{
				timerList[i].Stop();
			}
			foreach (TowerDefenseFog item2 in fogLine[i])
			{
				if (GodotObject.IsInstanceValid(item2))
				{
					item2.Position = item2.savePos;
					if (item2.gridPos.X > (float)(mapGridNum.X + 1))
					{
						item2.SetCanVisible(visible: false);
					}
					else
					{
						item2.SetCanVisible(visible: true);
					}
				}
			}
		}
		if (GodotObject.IsInstanceValid(fogNode))
		{
			fogNode.GlobalPosition = new Vector2((float)fogNodeInitX, fogNode.GlobalPosition.Y);
		}
		return Task.CompletedTask;
	}

	public override Task GameReady()
	{
		for (int i = 1; i <= mapGridNum.Y + 1; i++)
		{
			foreach (TowerDefenseFog item in fogLine[i])
			{
				if (GodotObject.IsInstanceValid(item) && item.gridPos.X > (float)(mapGridNum.X + 1))
				{
					item.SetCanVisible(visible: true);
				}
			}
		}
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		Tween tween = fogNode.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(fogNode, "global_position:x", 0f, config.entryDuration);
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		if (GodotObject.IsInstanceValid(fogNode))
		{
			fogNode.GlobalPosition = new Vector2(0f, fogNode.GlobalPosition.Y);
		}
		if (_restoreMagicFade)
		{
			ApplyMagicColorToFog(animated: true, _magicFadeRemaining);
			_restoreMagicFade = false;
		}
		if (config.magicOpen)
		{
			ProcessMagicColorChange();
		}
		return Task.CompletedTask;
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeRuntimeState();
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		ApplyRuntimeState(_data, animateColor: true);
	}

	public override void GameFail()
	{
		HideFog();
	}

	public void HideFog()
	{
		if (GodotObject.IsInstanceValid(fogNode))
		{
			fogNode.Visible = false;
		}
	}

	public bool IsLineBlown(int line)
	{
		if (IsValidLine(line) && GodotObject.IsInstanceValid(timerList[line]))
		{
			return !timerList[line].IsStopped();
		}
		return false;
	}

	public override void Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(config) || !config.magicOpen || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		double num = Mathf.Max(0.0, delta);
		if (!(num <= 0.0))
		{
			_magicElapsed += num;
			_magicFadeRemaining = Math.Max(0.0, _magicFadeRemaining - num);
			ProcessMagicColorChange();
			_magicTickTimer += num;
			if (!(_magicTickTimer < 0.25))
			{
				double magicTickTimer = _magicTickTimer;
				_magicTickTimer = 0.0;
				ApplyMagicFogEffects(magicTickTimer);
			}
		}
	}

	private void ProcessMagicColorChange()
	{
		while (config.magicChangeInterval > 0.0)
		{
			double num = ((double)_magicChangeIndex + 1.0) * config.magicChangeInterval - _magicElapsed;
			if (_magicWarningIndex != _magicChangeIndex && num <= 2.0)
			{
				_magicWarningIndex = _magicChangeIndex;
				RuneWeatherNotice.Show(control, "TOWERDEFENSE_TIPS_MAGICFOG_CHANGING", Math.Max(0.01, num));
			}
			if (!(num > 0.0))
			{
				_magicChangeIndex++;
				PickNewMagicColor(animated: true);
				continue;
			}
			break;
		}
	}

	private TowerDefenseRuneMagicColor ResolveInitialMagicColor()
	{
		if (TowerDefenseRuneMagicUtil.TryParse(config.magicColor, out var color) && color != TowerDefenseRuneMagicColor.Orange)
		{
			return color;
		}
		return TowerDefenseRuneMagicUtil.RandomFogColor();
	}

	private void PickNewMagicColor(bool animated)
	{
		_magicColor = TowerDefenseRuneMagicUtil.RandomFogColorExcept(_magicColor);
		ApplyMagicColorToFog(animated);
	}

	private void ApplyMagicColorToFog(bool animated, double fadeDuration = 1.5)
	{
		ClearMagicColorTweens();
		_magicFadeRemaining = (animated ? fadeDuration : 0.0);
		Color color = TowerDefenseRuneMagicUtil.ToFogTint(_magicColor);
		foreach (Array<TowerDefenseFog> item in fogLine)
		{
			foreach (TowerDefenseFog fog in item)
			{
				if (!GodotObject.IsInstanceValid(fog))
				{
					continue;
				}
				if (!animated)
				{
					fog.SetMagicColor(color);
					continue;
				}
				Tween tween = fog.CreateTween();
				_magicColorTweens.Add(tween);
				tween.TweenMethod(Callable.From(delegate(Color magicColor)
				{
					fog.SetMagicColor(magicColor);
				}), fog.GetMagicColor(), color, fadeDuration);
			}
		}
	}

	private void ApplyMagicFogEffects(double tickDelta)
	{
		_magicCoveredCells.Clear();
		double refreshTime = 0.75;
		for (int i = 1; i <= mapGridNum.Y; i++)
		{
			if (!GodotObject.IsInstanceValid(fogNode) || !fogNode.Visible || !IsValidLine(i) || IsLineBlown(i))
			{
				continue;
			}
			foreach (TowerDefenseFog item in fogLine[i])
			{
				if (GodotObject.IsInstanceValid(item) && item.canVisible && !item.IsLit)
				{
					Vector2I vector2I = new Vector2I(Mathf.RoundToInt(item.gridPos.X), Mathf.RoundToInt(item.gridPos.Y));
					if (GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(vector2I)))
					{
						_magicCoveredCells.Add(vector2I);
					}
				}
			}
		}
		TowerDefenseRuneMagicUtil.ForEachPlant(delegate(TowerDefenseCharacter character)
		{
			ApplyMagicCharacterEffect(character, tickDelta, refreshTime);
		});
		TowerDefenseRuneMagicUtil.ForEachZombie(delegate(TowerDefenseCharacter character)
		{
			ApplyMagicCharacterEffect(character, tickDelta, refreshTime);
		});
	}

	private void ApplyMagicCharacterEffect(TowerDefenseCharacter character, double tickDelta, double refreshTime)
	{
		bool num = _magicCoveredCells.Contains(character.gridPos);
		if (!num || _magicColor != TowerDefenseRuneMagicColor.Purple)
		{
			character.buff.DeleteBuff("RuneFogDizzyImmune");
		}
		if (!num || _magicColor != TowerDefenseRuneMagicColor.Blue)
		{
			character.buff.DeleteBuff("RuneFogHaste");
		}
		if (num)
		{
			TowerDefenseRuneMagicUtil.ApplyFogEffect(_magicColor, character, refreshTime, config.magicDamagePercentPerSecond, config.magicHealPerSecond, tickDelta);
		}
	}

	private static void ClearMagicZombieBuffs(TowerDefenseCharacter character)
	{
		character.buff.DeleteBuff("RuneFogDizzyImmune");
		character.buff.DeleteBuff("RuneFogHaste");
	}

	private void ClearMagicColorTweens()
	{
		foreach (Tween magicColorTween in _magicColorTweens)
		{
			if (GodotObject.IsInstanceValid(magicColorTween))
			{
				magicColorTween.Kill();
			}
		}
		_magicColorTweens.Clear();
	}

	public void FogBlow(int line)
	{
		if (!IsValidLine(line))
		{
			return;
		}
		foreach (Tween item in tweenList[line])
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.Kill();
			}
		}
		tweenList[line].Clear();
		foreach (TowerDefenseFog item2 in fogLine[line])
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				Tween tween = item2.CreateTween();
				tween.SetEase(Tween.EaseType.Out);
				tween.SetTrans(Tween.TransitionType.Cubic);
				tween.TweenProperty(item2, "global_position:x", item2.savePos.X + config.blowDistance, config.blowDuration);
				tweenList[line].Add(tween);
			}
		}
	}

	public void FogBack(int line)
	{
		if (!IsValidLine(line))
		{
			return;
		}
		foreach (Tween item in tweenList[line])
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.Kill();
			}
		}
		tweenList[line].Clear();
		foreach (TowerDefenseFog item2 in fogLine[line])
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				Tween tween = item2.CreateTween();
				tween.SetEase(Tween.EaseType.Out);
				tween.SetTrans(Tween.TransitionType.Cubic);
				tween.TweenProperty(item2, "global_position:x", item2.savePos.X, config.returnDuration);
				tweenList[line].Add(tween);
			}
		}
	}

	public void BlowAllEffectEmit()
	{
		for (int i = 1; i <= mapGridNum.Y + 1; i++)
		{
			if (GodotObject.IsInstanceValid(timerList[i]))
			{
				timerList[i].Start(config.blowReturnDelay);
			}
			FogBlow(i);
		}
	}

	public void BlowLineEffectEmit(int line)
	{
		if (IsValidLine(line))
		{
			if (GodotObject.IsInstanceValid(timerList[line]))
			{
				timerList[line].Start(config.blowReturnDelay);
			}
			FogBlow(line);
		}
	}

	public override Dictionary SaveFeature()
	{
		return SerializeRuntimeState();
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		ApplyRuntimeState(_data);
	}

	private Dictionary SerializeRuntimeState()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Timer timer in timerList)
		{
			if (GodotObject.IsInstanceValid(timer) && !timer.IsStopped())
			{
				array.Add(timer.TimeLeft);
			}
			else
			{
				array.Add(-1.0);
			}
		}
		double num = 0.0;
		if (GodotObject.IsInstanceValid(fogNode))
		{
			num = fogNode.GlobalPosition.X;
		}
		Color color = TowerDefenseRuneMagicUtil.ToColor(_magicColor);
		foreach (Array<TowerDefenseFog> item in fogLine)
		{
			if (item.Count > 0 && GodotObject.IsInstanceValid(item[0]))
			{
				color = TowerDefenseRuneMagicUtil.UntintFogColor(item[0].GetMagicColor());
				break;
			}
		}
		return new Dictionary
		{
			["timerRemains"] = array,
			["fogNodePosX"] = num,
			["fogNodeVisible"] = GodotObject.IsInstanceValid(fogNode) && fogNode.Visible,
			["magicColor"] = TowerDefenseRuneMagicUtil.ToKey(_magicColor),
			["magicElapsed"] = _magicElapsed,
			["magicTickTimer"] = _magicTickTimer,
			["magicChangeIndex"] = _magicChangeIndex,
			["magicFadeRemaining"] = _magicFadeRemaining,
			["magicVisualColor"] = new Godot.Collections.Array { color.R, color.G, color.B }
		};
	}

	private void ApplyRuntimeState(Dictionary state, bool animateColor = false)
	{
		if (config.magicOpen && state.ContainsKey("magicColor"))
		{
			if (TowerDefenseRuneMagicUtil.TryParse(state["magicColor"].AsString(), out var color) && color != TowerDefenseRuneMagicColor.Orange)
			{
				bool flag = _magicColor != color;
				_magicColor = color;
				if (animateColor && flag)
				{
					ApplyMagicColorToFog(animated: true);
				}
				else if (!animateColor)
				{
					ClearMagicColorTweens();
					Color magicColor = TowerDefenseRuneMagicUtil.ToFogTint(color);
					Godot.Collections.Array array = state.GetValueOrDefault("magicVisualColor", new Godot.Collections.Array()).AsGodotArray();
					if (array.Count >= 3)
					{
						magicColor = TowerDefenseRuneMagicUtil.ToFogTint(new Color((float)array[0].AsDouble(), (float)array[1].AsDouble(), (float)array[2].AsDouble()));
					}
					foreach (Array<TowerDefenseFog> item in fogLine)
					{
						foreach (TowerDefenseFog item2 in item)
						{
							if (GodotObject.IsInstanceValid(item2))
							{
								item2.SetMagicColor(magicColor);
							}
						}
					}
					_magicFadeRemaining = Math.Max(0.0, state.GetValueOrDefault("magicFadeRemaining", 0.0).AsDouble());
					_restoreMagicFade = _magicFadeRemaining > 0.0;
				}
			}
			_magicElapsed = Math.Max(0.0, state.GetValueOrDefault("magicElapsed", 0.0).AsDouble());
			_magicTickTimer = Mathf.Clamp(state.GetValueOrDefault("magicTickTimer", 0.0).AsDouble(), 0.0, 0.25);
			_magicChangeIndex = Math.Max(0, state.GetValueOrDefault("magicChangeIndex", 0).AsInt32());
			_magicWarningIndex = -1;
		}
		double num = state.GetValueOrDefault("fogNodePosX", 0.0).AsDouble();
		if (GodotObject.IsInstanceValid(fogNode))
		{
			fogNode.GlobalPosition = new Vector2((float)num, fogNode.GlobalPosition.Y);
			fogNode.Visible = state.GetValueOrDefault("fogNodeVisible", fogNode.Visible).AsBool();
		}
		Godot.Collections.Array array2 = state.GetValueOrDefault("timerRemains", new Godot.Collections.Array()).AsGodotArray();
		for (int i = 0; i < timerList.Count; i++)
		{
			double num2 = ((i < array2.Count) ? array2[i].AsDouble() : (-1.0));
			if (num2 >= 0.0 && GodotObject.IsInstanceValid(timerList[i]))
			{
				timerList[i].Start(Math.Max(0.001, num2));
				SetLineBlownState(i, blown: true);
			}
			else if (GodotObject.IsInstanceValid(timerList[i]))
			{
				timerList[i].Stop();
				SetLineBlownState(i, blown: false);
			}
		}
	}

	private void SetLineBlownState(int line, bool blown)
	{
		if (!IsValidLine(line))
		{
			return;
		}
		foreach (Tween item in tweenList[line])
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.Kill();
			}
		}
		tweenList[line].Clear();
		foreach (TowerDefenseFog item2 in fogLine[line])
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				item2.GlobalPosition = new Vector2(item2.savePos.X + (blown ? config.blowDistance : 0f), item2.GlobalPosition.Y);
			}
		}
	}

	private bool IsValidLine(int line)
	{
		if (line >= 0 && line < timerList.Count && line < tweenList.Count)
		{
			return line < fogLine.Count;
		}
		return false;
	}
}
