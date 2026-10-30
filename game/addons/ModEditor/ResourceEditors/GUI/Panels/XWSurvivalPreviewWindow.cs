using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWSurvivalPreviewWindow : Window
{
	private TowerDefenseLevelSurvivalConfig _config;

	private int _round;

	private int _wave;

	private long _points;

	private Label _roundLabel;

	private Label _waveLabel;

	private Label _timeLabel;

	private Label _pointLabel;

	private ProgressBar _pointProgress;

	private FlowContainer _zombiePool;

	private FlowContainer _roundAdds;

	private Node2D _zombiePreviewRoot;

	private TextureRect _gameBackground;

	private readonly List<Node2D> _runtimeZombies = new List<Node2D>();

	private readonly Dictionary<ulong, ProcessModeEnum> _runtimeProcessModes = new Dictionary<ulong, ProcessModeEnum>();

	private SpinBox _roundSpin;

	private SpinBox _waveSpin;

	private CheckBox _bigWaveCheck;

	private bool _updatingControls;

	public override void _Ready()
	{
		SetProcess(enable: false);
		base.CloseRequested += base.Hide;
		base.VisibilityChanged += OnPreviewVisibilityChanged;
		BindInterface();
		ResetSimulation();
		RefreshPreviewProcessing();
	}

	public override void _ExitTree()
	{
		base.VisibilityChanged -= OnPreviewVisibilityChanged;
		SetProcess(enable: false);
		_runtimeZombies.Clear();
		_runtimeProcessModes.Clear();
		base._ExitTree();
	}

	public void Preview(TowerDefenseLevelSurvivalConfig config)
	{
		_config = config;
		if (IsNodeReady())
		{
			ResetSimulation();
			PopupCenteredClamped(new Vector2I(900, 620), 0.9f);
		}
	}

	public void RefreshConfig(TowerDefenseLevelSurvivalConfig config)
	{
		_config = config;
		if (IsNodeReady())
		{
			RecalculateFromControls();
		}
	}

	private void BindInterface()
	{
		_roundLabel = GetNode<Label>("PreviewScroll/Layout/Stage/Content/Counters/Round");
		_waveLabel = GetNode<Label>("PreviewScroll/Layout/Stage/Content/Counters/Wave");
		_timeLabel = GetNode<Label>("PreviewScroll/Layout/Stage/Content/Counters/Time");
		_pointLabel = GetNode<Label>("PreviewScroll/Layout/Stage/Content/PointLabel");
		_pointProgress = GetNode<ProgressBar>("PreviewScroll/Layout/Stage/Content/PointProgress");
		_roundSpin = GetNode<SpinBox>("PreviewScroll/Layout/Selector/RoundSpin");
		_waveSpin = GetNode<SpinBox>("PreviewScroll/Layout/Selector/WaveSpin");
		_bigWaveCheck = GetNode<CheckBox>("PreviewScroll/Layout/Selector/BigWaveCheck");
		_zombiePool = GetNode<FlowContainer>("PreviewScroll/Layout/ZombiePoolScroll/ZombiePool");
		_roundAdds = GetNode<FlowContainer>("PreviewScroll/Layout/RoundAddsScroll/RoundAdds");
		_zombiePreviewRoot = GetNode<Node2D>("%ZombieRoot");
		_gameBackground = GetNode<TextureRect>("%GameBackground");
		_roundSpin.ValueChanged += delegate
		{
			RecalculateFromControls();
		};
		_waveSpin.ValueChanged += delegate
		{
			RecalculateFromControls();
		};
		Button node = GetNode<Button>("PreviewScroll/Layout/Controls/ResetButton");
		Button node2 = GetNode<Button>("PreviewScroll/Layout/Controls/NextWaveButton");
		Button node3 = GetNode<Button>("PreviewScroll/Layout/Controls/NextRoundButton");
		node.Pressed += ResetSimulation;
		node2.Pressed += AdvanceWave;
		node3.Pressed += AdvanceRound;
	}

	private void ResetSimulation()
	{
		_round = 0;
		_wave = 0;
		_points = _config?.pointBegin ?? 0;
		UpdateControls();
		RenderSimulation();
	}

	private void AdvanceWave()
	{
		if (GodotObject.IsInstanceValid(_config))
		{
			_wave++;
			_points += (_bigWaveCheck.ButtonPressed ? _config.pointIncrementPerBigWave : _config.pointIncrementPerWave);
			_points = Math.Min(_points, _config.pointMax);
			_bigWaveCheck.ButtonPressed = false;
			UpdateControls();
			RenderSimulation();
		}
	}

	private void AdvanceRound()
	{
		if (GodotObject.IsInstanceValid(_config) && (_config.roundLimit < 0 || _round < _config.roundLimit))
		{
			_round++;
			_points += _config.pointIncrementPerRound;
			_points = Math.Min(_points, _config.pointMax);
			UpdateControls();
			RenderSimulation();
		}
	}

	private void RecalculateFromControls()
	{
		if (!_updatingControls && GodotObject.IsInstanceValid(_config))
		{
			_round = (int)_roundSpin.Value;
			_wave = (int)_waveSpin.Value;
			_points = _config.pointBegin + (long)_round * (long)_config.pointIncrementPerRound + (long)_wave * (long)_config.pointIncrementPerWave;
			_points = Math.Min(_points, _config.pointMax);
			RenderSimulation();
		}
	}

	private void UpdateControls()
	{
		_updatingControls = true;
		_roundSpin.Value = _round;
		_waveSpin.Value = _wave;
		SpinBox roundSpin = _roundSpin;
		TowerDefenseLevelSurvivalConfig config = _config;
		roundSpin.MaxValue = ((config != null && config.roundLimit >= 0) ? _config.roundLimit : 999);
		_updatingControls = false;
	}

	private void RenderSimulation()
	{
		foreach (Node child in _zombiePool.GetChildren())
		{
			child.QueueFree();
		}
		foreach (Node child2 in _roundAdds.GetChildren())
		{
			child2.QueueFree();
		}
		if (!GodotObject.IsInstanceValid(_config))
		{
			return;
		}
		_roundLabel.Text = ((_config.roundLimit < 0) ? $"轮次 {_round} / 无尽" : $"轮次 {_round} / {_config.roundLimit}");
		_waveLabel.Text = $"波次 {_wave}";
		_timeLabel.Text = ((!_config.roundDayNightChange || _round % 2 == 0) ? "白天" : "黑夜");
		_timeLabel.AddThemeColorOverride("font_color", (_timeLabel.Text == "白天") ? new Color(1f, 0.88f, 0.38f) : new Color(0.55f, 0.68f, 1f));
		_gameBackground.Modulate = ((_timeLabel.Text == "白天") ? Colors.White : new Color(0.36f, 0.48f, 0.72f));
		_pointLabel.Text = $"生成点数 {_points:N0} / {_config.pointMax:N0}    大波加成 ×{_config.pointBigWaveScale:0.##}";
		_pointProgress.MinValue = 0.0;
		_pointProgress.MaxValue = Math.Max(1, _config.pointMax);
		_pointProgress.Value = _points;
		List<string> list = new List<string>();
		foreach (Variant item in _config.zombiePoolBase)
		{
			list.Add(item.AsString());
		}
		foreach (TowerDefenseLevelSurvivalZombiePoolRoundAddConfig item2 in _config.zombiePoolRoundAdd)
		{
			if (!GodotObject.IsInstanceValid(item2))
			{
				continue;
			}
			bool flag = item2.round <= _round;
			_roundAdds.AddChild(CreateChip($"第 {item2.round} 轮: {item2.zombieList.Count} 种", flag), forceReadableName: false, InternalMode.Disabled);
			if (!flag)
			{
				continue;
			}
			foreach (Variant zombie in item2.zombieList)
			{
				list.Add(zombie.AsString());
			}
		}
		foreach (string item3 in list)
		{
			_zombiePool.AddChild(CreateChip(string.IsNullOrWhiteSpace(item3) ? "未设置" : item3, active: true), forceReadableName: false, InternalMode.Disabled);
		}
		if (list.Count == 0)
		{
			_zombiePool.AddChild(new Label
			{
				Text = "僵尸池为空"
			}, forceReadableName: false, InternalMode.Disabled);
		}
		RebuildRuntimeZombieWave(list);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float num = 34f + (float)Mathf.Min(_round, 20) * 2f;
		foreach (Node2D runtimeZombie in _runtimeZombies)
		{
			if (GodotObject.IsInstanceValid(runtimeZombie))
			{
				runtimeZombie.Position += Vector2.Left * num * (float)delta;
				if (runtimeZombie.Position.X < -80f)
				{
					runtimeZombie.Position = new Vector2(960f, runtimeZombie.Position.Y);
				}
			}
		}
	}

	private void RebuildRuntimeZombieWave(List<string> pool)
	{
		if (!GodotObject.IsInstanceValid(_zombiePreviewRoot))
		{
			RefreshPreviewProcessing();
			return;
		}
		foreach (Node child in _zombiePreviewRoot.GetChildren())
		{
			ForgetPreviewTreeModes(child);
			child.QueueFree();
		}
		_runtimeZombies.Clear();
		int num = Math.Min(pool?.Count ?? 0, 7);
		for (int i = 0; i < num; i++)
		{
			string characterName = pool[i];
			PackedScene packedScene = ResourceManager.Instance?.GetCharacterScene(characterName);
			if (packedScene == null)
			{
				packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			if (GodotObject.IsInstanceValid(packedScene))
			{
				Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				if (!(node is TowerDefenseCharacter towerDefenseCharacter))
				{
					node.Free();
					continue;
				}
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
				towerDefenseCharacter.Position = new Vector2(760f + (float)i * 115f, 145f + (float)(i % 3) * 45f);
				towerDefenseCharacter.Scale = Vector2.One * 0.78f;
				_zombiePreviewRoot.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
				_runtimeZombies.Add(towerDefenseCharacter);
			}
		}
		RefreshPreviewProcessing();
	}

	private void OnPreviewVisibilityChanged()
	{
		RefreshPreviewProcessing();
	}

	private void RefreshPreviewProcessing()
	{
		for (int num = _runtimeZombies.Count - 1; num >= 0; num--)
		{
			if (!GodotObject.IsInstanceValid(_runtimeZombies[num]))
			{
				_runtimeZombies.RemoveAt(num);
			}
		}
		bool flag = base.Visible && _runtimeZombies.Count > 0;
		SetProcess(flag);
		SetPreviewTreeProcessing(_zombiePreviewRoot, flag);
	}

	private void SetPreviewTreeProcessing(Node node, bool active)
	{
		if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion())
		{
			return;
		}
		ulong instanceId = node.GetInstanceId();
		if (active)
		{
			if (_runtimeProcessModes.TryGetValue(instanceId, out var value))
			{
				node.ProcessMode = value;
			}
		}
		else
		{
			_runtimeProcessModes.TryAdd(instanceId, node.ProcessMode);
			node.ProcessMode = ProcessModeEnum.Disabled;
		}
		foreach (Node child in node.GetChildren())
		{
			SetPreviewTreeProcessing(child, active);
		}
	}

	private void ForgetPreviewTreeModes(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		foreach (Node child in node.GetChildren())
		{
			ForgetPreviewTreeModes(child);
		}
		_runtimeProcessModes.Remove(node.GetInstanceId());
	}

	private static Control CreateChip(string text, bool active)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.CustomMinimumSize = new Vector2(135f, 34f);
		panelContainer.Modulate = (active ? Colors.White : new Color(1f, 1f, 1f, 0.4f));
		panelContainer.AddChild(new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}
}
