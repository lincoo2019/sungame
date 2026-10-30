using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantReCactus : TowerDefensePlant
{
	private FireComponent fireComponent;

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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireNum = value;
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			fireComponent.OnFireReady += FireReady;
			base.OnLand += Landed;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		base.OnLand -= Landed;
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.OnFireReady -= FireReady;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			shadowSprite.Scale = transformPoint.Scale - Vector2.One * (float)((base.z - base.groundHeight) / 100.0);
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
		if (clip == "Jump" && isGround)
		{
			base.sprite.SetAnimation(idleAnimeClip, loop: true, 0.2);
		}
	}

	private void Landed()
	{
		if (base.sprite?.clip == "Jump")
		{
			base.sprite.SetAnimation(idleAnimeClip, loop: true, 0.2);
		}
	}

	public void FireReady()
	{
		if (fireComponent.runningCheckId == 0 && ySpeed >= 0.0)
		{
			ySpeed = -200.0;
			base.sprite.SetAnimation("Jump", loop: true, 0.2f * (float)((fireInterval + 4.5) / 6.0));
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "Spike").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
