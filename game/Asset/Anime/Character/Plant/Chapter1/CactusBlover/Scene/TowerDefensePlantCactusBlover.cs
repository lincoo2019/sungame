using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCactusBlover : TowerDefensePlant
{
	private BloverComponent _bloverComponent;

	private const int CactusBloverProjectileDamageFlags = 2;

	private const int CactusBloverProjectileCollisionFlags = 11;

	private const int CactusBloverProjectileFireMethodFlags = 5;

	private string _projectileName = "Spike";

	private string _skinName = "Default";

	public bool run;

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value ?? "Spike";
			ApplyProjectileData();
		}
	}

	[Export(PropertyHint.None, "")]
	public string skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			_skinName = value ?? "Default";
			ApplyProjectileData();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_bloverComponent = componentManager.GetRuntime<BloverComponent>();
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.OnBlowOver += BlowOver;
			}
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Knife";
			}
			ApplyProjectileData();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BloverComponent bloverComponent = _bloverComponent;
		if (bloverComponent != null && !bloverComponent.IsReleased)
		{
			_bloverComponent.OnBlowOver -= BlowOver;
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Knife";
		}
		else
		{
			skinName = "Default";
		}
	}

	private TowerDefenseProjectileCreateData CreateProjectileData()
	{
		return new TowerDefenseProjectileCreateData(new StringName(projectileName))
		{
			skinName = new StringName(skinName),
			damageFlags = 2,
			collisionFlags = 11,
			fireMethodFlags = 5,
			penetrateNum = -1,
			overridePenetrateNum = true
		};
	}

	private void ApplyProjectileData()
	{
		BloverComponent bloverComponent = _bloverComponent;
		if (bloverComponent != null && !bloverComponent.IsReleased)
		{
			_bloverComponent.projectileDataList = new Array<TowerDefenseProjectileCreateData> { CreateProjectileData() };
		}
	}

	public override void IdleEntered()
	{
		if (inGame && TowerDefenseManager.Instance.currentControl.isGameRunning)
		{
			Run();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
		if (inGame && TowerDefenseManager.Instance.currentControl.isGameRunning && !run)
		{
			Run();
		}
	}

	public void Run()
	{
		base.sprite.SetAnimation("Blow", loop: false, 0.2);
		base.sprite.AddAnimation("Loop", 0.0);
		instance.invincible = true;
		run = true;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "blow")
		{
			BloverComponent bloverComponent = _bloverComponent;
			if (bloverComponent != null && !bloverComponent.IsReleased)
			{
				_bloverComponent.Execult();
			}
		}
	}

	public void BlowOver()
	{
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["projectileName"] = projectileName,
			["skinName"] = skinName,
			["run"] = run
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Spike");
		skinName = (data.ContainsKey("skinName") ? data["skinName"].AsString() : "Default");
		run = data.ContainsKey("run") && data["run"].AsBool();
	}
}
