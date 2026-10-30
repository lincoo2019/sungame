using System;
using Godot;

public partial class ShootingMowerLayerOrderRuntimeTest : Node2D
{
	private static readonly Vector2I SourceGridPosition = new Vector2I(2, 4);

	private static readonly Vector2I FirstTargetGridPosition = new Vector2I(2, 2);

	private static readonly Vector2I SecondTargetGridPosition = new Vector2I(2, 5);

	private static readonly Vector2I FinalTargetGridPosition = new Vector2I(2, 1);

	private static readonly Vector2 TestGridBeginPosition = Vector2.Zero;

	private static readonly Vector2 TestGridSize = new Vector2(100f, 100f);

	private static readonly Vector2I TestGridCount = new Vector2I(9, 5);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		Vector2 previousGridBeginPosition = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridCount = manager?.gridNum ?? Vector2I.Zero;
		ShootingMowerLayerOrderCharacterStub flowerPot = null;
		ShootingMowerLayerOrderCharacterStub plant = null;
		ShootingMowerLayerOrderCharacterStub pumpkin = null;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				return;
			}
			manager.gridBeginPos = TestGridBeginPosition;
			manager.gridSize = TestGridSize;
			manager.gridNum = TestGridCount;
			Vector2 cellVisualPosition = GetCellVisualPosition(SourceGridPosition);
			Vector2 firstTargetPosition = GetCellVisualPosition(FirstTargetGridPosition);
			Vector2 secondTargetPosition = GetCellVisualPosition(SecondTargetGridPosition);
			Vector2 finalTargetPosition = GetCellVisualPosition(FinalTargetGridPosition);
			flowerPot = CreateCharacter("FlowerPot", TowerDefenseEnum.LAYER_GROUNDITEM.PLANT_UNDER, cellVisualPosition);
			plant = CreateCharacter("Plant", TowerDefenseEnum.LAYER_GROUNDITEM.PLANT, cellVisualPosition);
			pumpkin = CreateCharacter("Pumpkin", TowerDefenseEnum.LAYER_GROUNDITEM.PLANT_FRONT, cellVisualPosition);
			for (int frame = 0; frame < 5; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, "warmup");
			StartStackMove(flowerPot, plant, pumpkin, FirstTargetGridPosition, firstTargetPosition);
			CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, "first-move-start");
			for (int frame = 0; frame < 2; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, $"first-move-frame-{frame}");
			}
			StartStackMove(flowerPot, plant, pumpkin, SecondTargetGridPosition, secondTargetPosition);
			CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, "second-move-start");
			for (int frame = 0; frame < 2; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, $"second-move-frame-{frame}");
			}
			StartStackMove(flowerPot, plant, pumpkin, FinalTargetGridPosition, finalTargetPosition);
			CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, "final-move-start");
			for (int frame = 0; frame < 40; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				CheckLayerOrderAtVisualPosition(flowerPot, plant, pumpkin, $"final-move-frame-{frame}");
			}
			Check(flowerPot.GetLogicalGlobalPosition().IsEqualApprox(finalTargetPosition) && plant.GetLogicalGlobalPosition().IsEqualApprox(finalTargetPosition) && pumpkin.GetLogicalGlobalPosition().IsEqualApprox(finalTargetPosition), "The whole shooting-mode stack must finish at the latest requested target position.");
			Check(flowerPot.gridPos == FinalTargetGridPosition && plant.gridPos == FinalTargetGridPosition && pumpkin.gridPos == FinalTargetGridPosition, "Logical grid authority must switch to the latest target before the visual tween finishes.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ShootingMowerLayerOrderRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			FreeCharacter(flowerPot);
			FreeCharacter(plant);
			FreeCharacter(pumpkin);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.gridBeginPos = previousGridBeginPosition;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridCount;
			}
		}
		bool flag = _failures == 0;
		GD.Print($"SHOOTING_MOWER_LAYER_ORDER_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private ShootingMowerLayerOrderCharacterStub CreateCharacter(string characterName, TowerDefenseEnum.LAYER_GROUNDITEM layer, Vector2 position)
	{
		ShootingMowerLayerOrderCharacterStub shootingMowerLayerOrderCharacterStub = new ShootingMowerLayerOrderCharacterStub
		{
			Name = characterName,
			itemLayer = layer,
			gridPos = SourceGridPosition,
			Position = position
		};
		AddChild(shootingMowerLayerOrderCharacterStub, forceReadableName: false, InternalMode.Disabled);
		shootingMowerLayerOrderCharacterStub.SetPhysicsProcess(enable: false);
		string text = ((characterName == "FlowerPot") ? "Pot/Pot" : ((!(characterName == "Plant")) ? "Pumpkin/Pumpkin" : "PeaShooter/PeaShooter"));
		string text2 = text;
		shootingMowerLayerOrderCharacterStub.sprite = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/" + text2 + ".tscn").Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		shootingMowerLayerOrderCharacterStub.AddChild(shootingMowerLayerOrderCharacterStub.sprite, forceReadableName: false, InternalMode.Disabled);
		return shootingMowerLayerOrderCharacterStub;
	}

	private static void StartStackMove(ShootingMowerLayerOrderCharacterStub flowerPot, ShootingMowerLayerOrderCharacterStub plant, ShootingMowerLayerOrderCharacterStub pumpkin, Vector2I targetGridPosition, Vector2 targetPosition)
	{
		StartMove(flowerPot, targetGridPosition, targetPosition);
		StartMove(plant, targetGridPosition, targetPosition);
		StartMove(pumpkin, targetGridPosition, targetPosition);
	}

	private static void StartMove(ShootingMowerLayerOrderCharacterStub character, Vector2I targetGridPosition, Vector2 targetPosition)
	{
		character.gridPos = targetGridPosition;
		TowerDefenseCellInstance.CreateCharacterCellMoveTween(character, targetPosition);
	}

	private void CheckLayerOrderAtVisualPosition(ShootingMowerLayerOrderCharacterStub flowerPot, ShootingMowerLayerOrderCharacterStub plant, ShootingMowerLayerOrderCharacterStub pumpkin, string phase)
	{
		int y = TowerDefenseManager.Instance.GetMapGridPos(plant.GetLogicalGlobalPosition()).Y;
		int num = y * 15;
		bool flag = flowerPot.ZIndex == num + 2 && plant.ZIndex == num + 4 && pumpkin.ZIndex == num + 5;
		bool flag2 = flowerPot.ZIndex < plant.ZIndex && plant.ZIndex < pumpkin.ZIndex;
		Check(flag && flag2, $"The flower pot, plant, and pumpkin must follow visual row {y} without reordering during {phase}; z={flowerPot.ZIndex},{plant.ZIndex},{pumpkin.ZIndex}.");
		CheckGpuOrder(flowerPot, phase);
		CheckGpuOrder(plant, phase);
		CheckGpuOrder(pumpkin, phase);
	}

	private void CheckGpuOrder(ShootingMowerLayerOrderCharacterStub character, string phase)
	{
		AdobeAnimateCrowdRenderState state;
		AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = character.sprite.TryBuildCrowdRenderState(out state);
		int num = character.ZIndex + character.sprite.ZIndex;
		Check(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph && state.EffectiveZIndex == num, $"GPU_ORDER {character.Name} phase={phase} result={adobeAnimateCrowdRenderStateResult} mode={state?.Mode} gpuZ={state?.EffectiveZIndex} expectedZ={num}");
	}

	private static Vector2 GetCellVisualPosition(Vector2I gridPosition)
	{
		return TestGridBeginPosition + new Vector2(((float)gridPosition.X - 0.5f) * TestGridSize.X, ((float)gridPosition.Y - 0.5f) * TestGridSize.Y);
	}

	private static void FreeCharacter(ShootingMowerLayerOrderCharacterStub character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.CancelCellMoveTween();
			character.Free();
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ShootingMowerLayerOrderRuntimeTest] " + message);
		}
	}
}
