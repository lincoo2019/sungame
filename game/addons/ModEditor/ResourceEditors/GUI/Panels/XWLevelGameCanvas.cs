using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWLevelGameCanvas : VBoxContainer
{
	[Signal]
	public delegate void LevelEditedEventHandler();

	private const string DefaultMapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private TowerDefenseLevelConfig _level;

	private LevelEditorMapEditor _runtimeMapEditor;

	private Label _mapLabel;

	private Label _statusLabel;

	private bool _initialized;

	private bool _committing;

	public override void _Ready()
	{
		_runtimeMapEditor = GetNode<LevelEditorMapEditor>("%RuntimeMapEditor");
		_mapLabel = GetNode<Label>("%MapLabel");
		_statusLabel = GetNode<Label>("%Status");
		GetNode<Button>("%ReloadButton").Connect(BaseButton.SignalName.Pressed, new Callable(this, "ReloadFromResource"));
		GetNode<Button>("%CommitButton").Connect(BaseButton.SignalName.Pressed, new Callable(this, "CommitMapEdits"));
		base.VisibilityChanged += HandleVisibilityChanged;
		RefreshVisibilityGatedProcessing();
		if (GodotObject.IsInstanceValid(_level))
		{
			ReloadFromResource();
		}
	}

	public override void _Process(double delta)
	{
		if (_initialized && !_committing && GodotObject.IsInstanceValid(_level) && !_level.canExport)
		{
			_level.canExport = true;
			_level.EmitChanged();
			UpdateStatus("画面已修改，等待写回布局或使用资源编辑器保存");
			EmitSignal(SignalName.LevelEdited);
		}
	}

	public override void _ExitTree()
	{
		base.VisibilityChanged -= HandleVisibilityChanged;
		SetProcess(enable: false);
		if (GodotObject.IsInstanceValid(_runtimeMapEditor))
		{
			_runtimeMapEditor.ProcessMode = ProcessModeEnum.Disabled;
		}
		if (_initialized && GodotObject.IsInstanceValid(_level) && GodotObject.IsInstanceValid(_runtimeMapEditor))
		{
			CommitMapEdits();
		}
		base._ExitTree();
	}

	public void EditLevel(TowerDefenseLevelConfig level)
	{
		_level = level;
		if (IsNodeReady())
		{
			ReloadFromResource();
		}
	}

	public void ReloadFromResource()
	{
		_initialized = false;
		RefreshVisibilityGatedProcessing();
		if (!GodotObject.IsInstanceValid(_level) || !GodotObject.IsInstanceValid(_runtimeMapEditor))
		{
			return;
		}
		_runtimeMapEditor.ClearCharacter();
		_runtimeMapEditor.Init(_level);
		TowerDefenseMapConfig towerDefenseMapConfig = ResolveMapConfig(_level.map);
		if (!GodotObject.IsInstanceValid(towerDefenseMapConfig))
		{
			_mapLabel.Text = "地图：" + DisplayValue(_level.map);
			_statusLabel.Text = "无法加载地图配置，请在关卡游戏画面的“地图”资源卡中选择有效地图。";
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = _runtimeMapEditor.mapFeature;
		mapFeature.editorPreviewMode = true;
		bool flag;
		if (GodotObject.IsInstanceValid(mapFeature.currentMap))
		{
			mapFeature.MapChange(towerDefenseMapConfig);
			flag = true;
		}
		else
		{
			flag = mapFeature.MapInit(towerDefenseMapConfig);
		}
		if (!flag)
		{
			_statusLabel.Text = "地图运行画面加载失败：" + DisplayValue(_level.map);
			return;
		}
		_runtimeMapEditor.ApplyMapPreviewConfig(towerDefenseMapConfig);
		_runtimeMapEditor.Save(isSave: false);
		_level.canExport = true;
		_initialized = true;
		RefreshVisibilityGatedProcessing();
		_mapLabel.Text = $"地图：{DisplayValue(_level.map)} · {towerDefenseMapConfig.gridNum.X}×{towerDefenseMapConfig.gridNum.Y}";
		UpdateStatus("已载入真实游戏地图；从卡牌库选择单位后直接放置到格子");
	}

	public void CommitMapEdits()
	{
		if (_initialized && !_committing && GodotObject.IsInstanceValid(_level) && GodotObject.IsInstanceValid(_runtimeMapEditor))
		{
			_committing = true;
			_runtimeMapEditor.Save(isSave: true);
			_level.canExport = true;
			_level.EmitChanged();
			UpdateStatus($"已从游戏画面写回 {_level.preSpawnList?.Count ?? 0} 个预生成单位");
			EmitSignal(SignalName.LevelEdited);
			_committing = false;
		}
	}

	private void UpdateStatus(string message)
	{
		if (GodotObject.IsInstanceValid(_statusLabel))
		{
			_statusLabel.Text = $"{message} · 当前 preSpawn {(_level?.preSpawnList?.Count).GetValueOrDefault()}";
		}
	}

	private void HandleVisibilityChanged()
	{
		RefreshVisibilityGatedProcessing();
	}

	private void RefreshVisibilityGatedProcessing()
	{
		bool flag = IsVisibleInTree();
		SetProcess(_initialized && flag);
		if (GodotObject.IsInstanceValid(_runtimeMapEditor))
		{
			_runtimeMapEditor.ProcessMode = (ProcessModeEnum)((_initialized && flag) ? 0 : 4);
		}
	}

	private static TowerDefenseMapConfig ResolveMapConfig(string mapValue)
	{
		if (!string.IsNullOrWhiteSpace(mapValue) && ResourceLoader.Exists(mapValue))
		{
			TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>(mapValue, null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(towerDefenseMapConfig))
			{
				return towerDefenseMapConfig;
			}
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseMapConfig mapConfig = TowerDefenseManager.Instance.GetMapConfig(mapValue);
			if (GodotObject.IsInstanceValid(mapConfig))
			{
				return mapConfig;
			}
		}
		return ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres", null, ResourceLoader.CacheMode.Reuse);
	}

	private static string DisplayValue(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "Frontlawn";
	}
}
