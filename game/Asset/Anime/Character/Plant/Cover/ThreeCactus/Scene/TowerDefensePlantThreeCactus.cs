using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantThreeCactus : TowerDefensePlant
{
	private BloverComponent _bloverComponent;

	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

	private string _projectileName = "Spike";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 1)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[1].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_bloverComponent = componentManager.GetRuntime<BloverComponent>();
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.OnFireReady += FireReady;
			((ThreeCactusSprite)base.sprite).head.OnAnimeEvent += AnimeEvent;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnFireReady -= FireReady;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			shadowSprite.Scale = transformPoint.Scale - Vector2.One * (float)(base.z - base.groundHeight) / 100f;
			if (base.sprite.clip == "Jump")
			{
				base.sprite.timeScale = timeScale * 2.0;
			}
			else
			{
				base.sprite.timeScale = timeScale * 1.0;
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Jump")
		{
			base.sprite.SetAnimation(idleAnimeClip, loop: true, 0.2);
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "blow")
		{
			_bloverComponent.ExecuteLine(base.gridPos.Y);
		}
	}

	public void FireReady()
	{
		if (_fireComponent.runningCheckId == 0 && ySpeed >= 0.0)
		{
			ySpeed = -200.0;
			base.sprite.SetAnimation("Jump", loop: true, 0.2 * (double)(float)(fireInterval + 4.5) / 6.0);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (data.ContainsKey("fireNum") ? data["fireNum"].AsInt32() : 2);
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Spike");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
	}
}
