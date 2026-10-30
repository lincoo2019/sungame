using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingMagnetnut : TowerDefensePlantBowlingBase
{
	private MagnetComponent magnetComponent;

	public bool hitArmor;

	public bool isDraw;

	private bool _isProcessing;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		magnetComponent = componentManager.GetRuntime<MagnetComponent>();
		if (magnetComponent == null)
		{
			GD.PushError("Bowling Magnet-nut is missing its Magnet resource runtime.");
		}
		bowlingComponent.OnBowling += Bowling;
		if (base.config.customData != null)
		{
			string text = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantMagnetnut").GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
				.GetValueOrDefault("Custom", "")
				.AsString();
			if (text != "")
			{
				base.currentCustom = new Array<string> { text };
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.OnBowling -= Bowling;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (GodotObject.IsInstanceValid(magnetComponent.magnet))
			{
				TowerDefenseCharacterEventBowlingHurt towerDefenseCharacterEventBowlingHurt = (TowerDefenseCharacterEventBowlingHurt)bowlingComponent.hitEvent[0].Duplicate();
				towerDefenseCharacterEventBowlingHurt.num = 1800.0 + magnetComponent.breakDownArmor.hitPoints * 4.5;
				bowlingComponent.hitEvent[0] = towerDefenseCharacterEventBowlingHurt;
			}
			else if (!isDraw && !_isProcessing)
			{
				_isProcessing = true;
				ProcessArmorDrawAsync();
			}
		}
	}

	private async void ProcessArmorDrawAsync()
	{
		try
		{
			bool flag = await magnetComponent.CanArmorDraw();
			if (GodotObject.IsInstanceValid(this))
			{
				if (flag)
				{
					isDraw = true;
					magnetComponent.ArmorDrawNear();
					isDraw = false;
				}
				else
				{
					TowerDefenseCharacterEventBowlingHurt towerDefenseCharacterEventBowlingHurt = (TowerDefenseCharacterEventBowlingHurt)bowlingComponent.hitEvent[0].Duplicate();
					towerDefenseCharacterEventBowlingHurt.num = 1800.0;
					bowlingComponent.hitEvent[0] = towerDefenseCharacterEventBowlingHurt;
				}
			}
		}
		finally
		{
			_isProcessing = false;
		}
	}

	public void Bowling(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(magnetComponent.breakDownArmor))
		{
			magnetComponent.BreakDownOver();
		}
	}

	public override void DestroySet()
	{
		magnetComponent.Destroy();
		base.DestroySet();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "hitArmor", hitArmor },
			{ "isDraw", isDraw }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hitArmor = data.GetValueOrDefault("hitArmor", false).AsBool();
		isDraw = data.GetValueOrDefault("isDraw", false).AsBool();
	}
}
