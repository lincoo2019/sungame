using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePumpLantern : TowerDefensePlant
{
	public const double SHIELD_HP = 500.0;

	public const double SHIELD_INTERVAL = 50.0;

	public static readonly StringName SHIELD_TYPE = new StringName("CommonShield");

	public static readonly Vector2I[] SURROUND_OFFSETS = new Vector2I[8]
	{
		new Vector2I(-1, -1),
		new Vector2I(0, -1),
		new Vector2I(1, -1),
		new Vector2I(-1, 0),
		new Vector2I(1, 0),
		new Vector2I(-1, 1),
		new Vector2I(0, 1),
		new Vector2I(1, 1)
	};

	private PointLight2D _light;

	private Timer _shieldTimer;

	private bool _restoredFromProgress;

	public override void PrepareForProgressRestore()
	{
		base.PrepareForProgressRestore();
		_restoredFromProgress = true;
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNodeOrNull<PointLight2D>("%Light");
			AudioManager.Instance.AudioPlay("Plantern");
			if (!_restoredFromProgress)
			{
				_GenerateShields();
			}
			_ShieldStartTimer();
			BuffComponent buffComponent = buff;
			if (buffComponent != null && !buffComponent.IsReleased)
			{
				buff.OnBuffDelete += OnBuffDelete;
			}
		}
	}

	public override void _ExitTree()
	{
		BuffComponent buffComponent = buff;
		if (buffComponent != null && !buffComponent.IsReleased)
		{
			buff.OnBuffDelete -= OnBuffDelete;
		}
		base._ExitTree();
		if (GodotObject.IsInstanceValid(_shieldTimer))
		{
			_shieldTimer.QueueFree();
		}
	}

	private void OnBuffDelete(string key)
	{
		if (key == "Hypnoses")
		{
			_SyncShieldHypnoses();
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (GodotObject.IsInstanceValid(_light))
			{
				_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
		}
	}

	private void _ShieldStartTimer()
	{
		_shieldTimer = new Timer();
		_shieldTimer.WaitTime = 50.0;
		_shieldTimer.Autostart = false;
		_shieldTimer.OneShot = false;
		_shieldTimer.Timeout += _GenerateShields;
		AddChild(_shieldTimer, forceReadableName: false, InternalMode.Disabled);
		_shieldTimer.Start();
	}

	private void _GenerateShields()
	{
		Vector2I[] sURROUND_OFFSETS = SURROUND_OFFSETS;
		foreach (Vector2I vector2I in sURROUND_OFFSETS)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos + vector2I);
			if (GodotObject.IsInstanceValid(mapCell))
			{
				TowerDefenseItemSheild.CreateOnCellWithHP(mapCell, SHIELD_TYPE, 500.0);
			}
		}
		if (instance.hypnoses)
		{
			Callable.From(_SyncShieldHypnoses).CallDeferred();
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		_SyncShieldHypnoses();
	}

	private void _SyncShieldHypnoses()
	{
		bool hypnoses = instance.hypnoses;
		Vector2I[] sURROUND_OFFSETS = SURROUND_OFFSETS;
		foreach (Vector2I vector2I in sURROUND_OFFSETS)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos + vector2I);
			if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell.itemShield))
			{
				TowerDefenseItemSheild itemShield = mapCell.itemShield;
				if (itemShield.instance.hypnoses != hypnoses)
				{
					itemShield.Hypnoses();
				}
			}
		}
	}
}
