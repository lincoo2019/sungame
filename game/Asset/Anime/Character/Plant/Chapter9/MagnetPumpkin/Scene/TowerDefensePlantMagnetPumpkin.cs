using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantMagnetPumpkin : TowerDefensePlant
{
	private static readonly StringName ShieldTypeMG = new StringName("MG");

	private MagnetComponent _magnetComponent;

	private readonly HashSet<TowerDefenseCharacter> _subscribedMagnetPlants = new HashSet<TowerDefenseCharacter>();

	private float _refreshTimer;

	private const float REFRESH_INTERVAL = 1f;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			MagnetComponent magnetComponent = _magnetComponent;
			if (magnetComponent != null && !magnetComponent.IsReleased)
			{
				_magnetComponent.OnBreakDown += OnSelfIronBreakDown;
			}
		}
	}

	public override void _ExitTree()
	{
		MagnetComponent magnetComponent = _magnetComponent;
		if (magnetComponent != null && !magnetComponent.IsReleased)
		{
			_magnetComponent.OnBreakDown -= OnSelfIronBreakDown;
		}
		UnsubscribeAll();
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(instance))
		{
			_refreshTimer += (float)delta;
			if (_refreshTimer >= 1f)
			{
				_refreshTimer = 0f;
				RefreshNearbyMagnetSubscriptions();
			}
		}
	}

	private void RefreshNearbyMagnetSubscriptions()
	{
		if (!GodotObject.IsInstanceValid(cell) || !GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		HashSet<TowerDefenseCharacter> hashSet = new HashSet<TowerDefenseCharacter>();
		foreach (Variant item in TowerDefenseManager.Instance.GetCampFriendly(base.camp))
		{
			TowerDefenseCharacter towerDefenseCharacter = item.As<TowerDefenseCharacter>();
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter?.instance) || towerDefenseCharacter.instance == instance || towerDefenseCharacter.isDestroy || towerDefenseCharacter.die)
			{
				continue;
			}
			Vector2 vector = (towerDefenseCharacter.GetLogicalGlobalPosition() - logicalGlobalPosition).Abs();
			if (!(vector.X > mapGridSize.X) && !(vector.Y > mapGridSize.Y))
			{
				MagnetComponent magnetComponent = towerDefenseCharacter.componentManager?.GetRuntime<MagnetComponent>();
				if (magnetComponent != null && !magnetComponent.IsReleased)
				{
					hashSet.Add(towerDefenseCharacter);
				}
			}
		}
		foreach (TowerDefenseCharacter item2 in hashSet)
		{
			if (!_subscribedMagnetPlants.Contains(item2))
			{
				MagnetComponent runtime = item2.componentManager.GetRuntime<MagnetComponent>();
				if (runtime != null && !runtime.IsReleased)
				{
					runtime.OnBreakDown += OnNearbyIronBreakDown;
					_subscribedMagnetPlants.Add(item2);
				}
			}
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter subscribedMagnetPlant in _subscribedMagnetPlants)
		{
			if (!hashSet.Contains(subscribedMagnetPlant))
			{
				UnsubscribePlant(subscribedMagnetPlant);
				list.Add(subscribedMagnetPlant);
			}
		}
		foreach (TowerDefenseCharacter item3 in list)
		{
			_subscribedMagnetPlants.Remove(item3);
		}
	}

	private void UnsubscribePlant(TowerDefenseCharacter plant)
	{
		if (GodotObject.IsInstanceValid(plant))
		{
			MagnetComponent magnetComponent = plant.componentManager?.GetRuntime<MagnetComponent>();
			if (magnetComponent != null && !magnetComponent.IsReleased)
			{
				magnetComponent.OnBreakDown -= OnNearbyIronBreakDown;
			}
		}
	}

	private void UnsubscribeAll()
	{
		foreach (TowerDefenseCharacter subscribedMagnetPlant in _subscribedMagnetPlants)
		{
			UnsubscribePlant(subscribedMagnetPlant);
		}
		_subscribedMagnetPlants.Clear();
	}

	private void OnSelfIronBreakDown(TowerDefenseArmorInstance _armor)
	{
		GenerateShieldShield(1000.0);
	}

	private void OnNearbyIronBreakDown(TowerDefenseArmorInstance _armor)
	{
		GenerateShieldShield(500.0);
	}

	private void GenerateShieldShield(double hp)
	{
		if (GodotObject.IsInstanceValid(cell))
		{
			TowerDefenseItemSheild.CreateOnCellWithHP(cell, ShieldTypeMG, hp);
		}
	}
}
