using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSnowBlover : TowerDefensePlant
{
	public BloverComponent bloverComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	private string _projectileName = "SnowPea";

	private string _skinName = "Default";

	public bool run;

	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			if (_projectileName != value)
			{
				_projectileName = value;
				BloverComponent obj = bloverComponent;
				if (obj != null && !obj.IsReleased)
				{
					TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(new StringName(projectileName));
					towerDefenseProjectileCreateData.skinName = new StringName(skinName);
					bloverComponent.projectileDataList = new Array<TowerDefenseProjectileCreateData> { towerDefenseProjectileCreateData };
				}
			}
		}
	}

	public string skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			_skinName = value;
			BloverComponent obj = bloverComponent;
			if (obj != null && !obj.IsReleased && bloverComponent.projectileDataList.Count > 0)
			{
				bloverComponent.projectileDataList[0].skinName = new StringName(value);
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			bloverComponent = componentManager.GetRuntime<BloverComponent>();
			BloverComponent obj = bloverComponent;
			if (obj != null && !obj.IsReleased)
			{
				bloverComponent.OnBlowOver += BlowOver;
			}
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Piece";
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BloverComponent obj = bloverComponent;
		if (obj != null && !obj.IsReleased)
		{
			bloverComponent.OnBlowOver -= BlowOver;
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Piece";
		}
		else
		{
			skinName = "Default";
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

	public virtual void Run()
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
			TowerDefenseCharacter.CreateColdVisualEffect(base.gridPos);
			bloverComponent.Execult();
		}
	}

	public virtual void BlowOver()
	{
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "projectileName", projectileName },
			{ "skinName", skinName },
			{ "run", run }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		projectileName = data.GetValueOrDefault("projectileName", "SnowPea").AsString();
		skinName = data.GetValueOrDefault("skinName", "Default").AsString();
		run = data.GetValueOrDefault("run", false).AsBool();
	}
}
