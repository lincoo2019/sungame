using Godot;

[GlobalClass]
public partial class TowerDefenseSunJalapeno : TowerDefenseSunBase
{
	public bool zombieCamp;

	public override void _Ready()
	{
		base._Ready();
		base.dieDownTimer.Timeout += DieDown;
	}

	public override string GetGroupName()
	{
		return "JalapenoSun";
	}

	public override ObjectManagerConfig.OBJECT GetPoolKey()
	{
		return ObjectManagerConfig.OBJECT.SUN_JALAPENO;
	}

	public override void OnCollectStart()
	{
		if (gridPos.X < 0 || gridPos.Y < 0)
		{
			gridPos = TowerDefenseManager.Instance.GetMapGridPos(base.sprite.GlobalPosition);
		}
		Explode();
	}

	public override long GetCollectValue()
	{
		return sunNum;
	}

	public override bool ShouldAutoCollect()
	{
		autoCollect = false;
		return base.ShouldAutoCollect();
	}

	public override bool OnDieDown()
	{
		return false;
	}

	public override void DieDown()
	{
		if (!isCollect)
		{
			if (GameSaveManager.Instance.GetFeatureValue("SunCollect") != 0)
			{
				Collection();
			}
			else
			{
				base.DieDown();
			}
		}
	}

	public override void OnRefresh()
	{
		gridPos = new Vector2I(-1, -1);
		zombieCamp = false;
	}

	public void Explode()
	{
		TowerDefenseCharacter.CreateJalapenoFire(zombieCamp ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT, gridPos, (double)sunNum * 10.0);
	}
}
