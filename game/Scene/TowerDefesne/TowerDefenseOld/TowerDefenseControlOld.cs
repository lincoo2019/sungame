using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseControlOld : TowerDefenseControl
{
	private AabbArea2D _zombieWonArea;

	private NinePatchButtonBase _shopButton;

	private readonly HashSet<TowerDefenseZombie> _zombieWonOverlaps = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _zombieWonScratch = new HashSet<TowerDefenseZombie>();

	public override void _Ready()
	{
		base._Ready();
		_zombieWonArea = GetNode<AabbArea2D>("%ZombieWonArea");
		_shopButton = GetNode<NinePatchButtonBase>("GUILayerFront/ShopButton");
		RefreshShopVisibility();
		_shopButton.OnPressed += ShopButtonPressed;
		if (GlobalFeatureManager.Instance != null)
		{
			GlobalFeatureManager.Instance.FeatureChanged += OnGlobalFeatureChanged;
		}
		GetNode<NinePatchButtonBase>("GUILayerFront/AlmanacButton").OnPressed += AlmanacButtonPressed;
		GetNode<SpriteBrightButton>("%OptionButton").OnPressed += base.OptionButtonPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		ProcessZombieWonArea();
	}

	public override void _ExitTree()
	{
		if (GlobalFeatureManager.Instance != null)
		{
			GlobalFeatureManager.Instance.FeatureChanged -= OnGlobalFeatureChanged;
		}
		base._ExitTree();
	}

	private void OnGlobalFeatureChanged(string featureId, int _oldValue, int _newValue)
	{
		if (featureId == "Shop")
		{
			RefreshShopVisibility();
		}
	}

	private void RefreshShopVisibility()
	{
		if (GodotObject.IsInstanceValid(_shopButton))
		{
			_shopButton.Visible = GlobalFeatureManager.Instance?.IsUnlocked("Shop") ?? false;
		}
	}

	private void ProcessZombieWonArea()
	{
		if (!TowerDefenseManager._IsGameRunning())
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(_zombieWonArea) || _zombieWonArea.ProcessMode == ProcessModeEnum.Disabled)
		{
			_zombieWonOverlaps.Clear();
		}
		else
		{
			if (TowerDefenseManager.Instance == null || TowerDefenseManager.Instance.characterRegistry == null)
			{
				return;
			}
			Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(_zombieWonArea);
			List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(checkRect);
			_zombieWonScratch.Clear();
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				if (charactersIntersectingRectList[i] is TowerDefenseZombie towerDefenseZombie)
				{
					_zombieWonScratch.Add(towerDefenseZombie);
					if (!_zombieWonOverlaps.Contains(towerDefenseZombie))
					{
						HandleZombieWon(towerDefenseZombie);
					}
				}
			}
			_zombieWonOverlaps.RemoveWhere((TowerDefenseZombie zombie) => !GodotObject.IsInstanceValid(zombie) || !_zombieWonScratch.Contains(zombie));
			foreach (TowerDefenseZombie item in _zombieWonScratch)
			{
				_zombieWonOverlaps.Add(item);
			}
		}
	}

	private void HandleZombieWon(TowerDefenseZombie zombie)
	{
		if (GodotObject.IsInstanceValid(zombie) && !zombie.instance.die && !zombie.instance.nearDie && !zombie.instance.hypnoses && !(zombie.Scale.X < 0f))
		{
			TowerDefenseZombieWon node = GetNode<TowerDefenseZombieWon>("%TowerDefenseZombieWon");
			if (GodotObject.IsInstanceValid(node))
			{
				node.LevelFail();
			}
		}
	}

	public void ShopButtonPressed()
	{
		GlobalFeatureManager instance = GlobalFeatureManager.Instance;
		if (instance != null && instance.IsUnlocked("Shop"))
		{
			DialogManager.Instance.DialogCreate("Shop");
		}
	}

	public void AlmanacButtonPressed()
	{
		DialogManager.Instance.DialogCreate("Almanac");
	}
}
