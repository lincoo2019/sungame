using System;
using Godot;

public partial class FireballSlowImmunityRuntimeTest : Node
{
	private const string FireballScenePath = "res://Asset/Anime/Character/Item/ItemFireball/Scene/TowerDefenseItemFireball.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		_ = 1;
		try
		{
			TowerDefenseItemFireball fireball = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Item/ItemFireball/Scene/TowerDefenseItemFireball.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseItemFireball>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(fireball), "ItemFireball scene must instantiate.");
			if (GodotObject.IsInstanceValid(fireball))
			{
				fireball.inGame = false;
				fireball.editorPreviewMode = true;
				AddChild(fireball, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				int num = 1;
				int num2 = 2;
				Check((fireball.instance.unUseBuffFlags & num) != 0, "ItemFireball must reject IceSpeedDown.");
				Check((fireball.instance.unUseBuffFlags & num2) == 0, "The narrow fix must not add Frozen immunity; maskFlags already rejects Frozen independently.");
				fireball.timeScale = 1.0;
				fireball.buff.AddBuff(new TowerDefenseCharacterBuffIceSpeedDown
				{
					time = 15.0
				});
				Check(!fireball.buff.BuffHas("IceSpeedDown"), "Rejected IceSpeedDown must not remain in the fireball Buff dictionary.");
				fireball.buff.BuffUpdate(1.0);
				Check(Math.Abs(fireball.timeScale - 1.0) < 0.0001, $"Rejected IceSpeedDown must not halve fireball timeScale; got {fireball.timeScale}.");
				fireball.QueueFree();
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[FireballSlowImmunityRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"FIREBALL_SLOW_IMMUNITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FireballSlowImmunityRuntimeTest] " + message);
		}
	}
}
