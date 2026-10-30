using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewHypnotizedCornpultMeleeRuntimeTest : Node
{
	private const string CornpultScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Cornpult/TowerDefenseZombieNormalCornpult.tscn";

	private const string CabbagepultScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Cabbagepult/TowerDefenseZombieNormalCabbagepult.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewHypnotizedCornpultMeleeControlStub control = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(manager.characterRegistry), "TowerDefenseManager and its real character registry must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(manager.characterRegistry))
				{
					goto end_IL_00d7;
				}
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				control = new BugOverviewHypnotizedCornpultMeleeControlStub
				{
					Name = "HypnotizedCornpultMeleeControl",
					isGameRunning = false,
					isInit = false
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				TowerDefenseZombieNormalCornpult cornpult = LoadCharacter<TowerDefenseZombieNormalCornpult>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Cornpult/TowerDefenseZombieNormalCornpult.tscn");
				TowerDefenseZombieNormalCabbagepult cabbagepult = LoadCharacter<TowerDefenseZombieNormalCabbagepult>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Cabbagepult/TowerDefenseZombieNormalCabbagepult.tscn");
				TowerDefenseZombie cornTarget = LoadCharacter<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				TowerDefenseZombie cabbageTarget = LoadCharacter<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(cornpult) && cornpult.config?.name == "ZombieNormalCornpult", "The fixture must use the real Cornpult Zombie scene.");
				Check(GodotObject.IsInstanceValid(cabbagepult) && cabbagepult.config?.name == "ZombieNormalCabbagepult", "The control fixture must use the real Cabbagepult Zombie scene.");
				Check(GodotObject.IsInstanceValid(cornTarget) && GodotObject.IsInstanceValid(cabbageTarget) && cornTarget.config?.name == "ZombieNormal" && cabbageTarget.config?.name == "ZombieNormal", "Both contact targets must be real normal zombies.");
				if (!GodotObject.IsInstanceValid(cornpult) || !GodotObject.IsInstanceValid(cabbagepult) || !GodotObject.IsInstanceValid(cornTarget) || !GodotObject.IsInstanceValid(cabbageTarget))
				{
					goto end_IL_00d7;
				}
				PrepareCharacter(cornpult, new Vector2I(3, 2), new Vector2(300f, 200f));
				PrepareCharacter(cornTarget, new Vector2I(3, 2), new Vector2(300f, 200f));
				PrepareCharacter(cabbagepult, new Vector2I(6, 2), new Vector2(600f, 200f));
				PrepareCharacter(cabbageTarget, new Vector2I(6, 2), new Vector2(600f, 200f));
				control.characterNode.AddChild(cornpult, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(cornTarget, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(cabbagepult, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(cabbageTarget, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				AttackComponent cornAttack = cornpult.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				FireComponent fireComponent = cornpult.componentManager?.GetRuntime<FireComponent>("character.fire");
				AttackComponent cabbageAttack = cabbagepult.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				Check(cornAttack != null && !cornAttack.IsReleased && fireComponent != null && !fireComponent.IsReleased, "The real Cornpult must expose both migrated melee Attack and ranged Fire runtimes.");
				Check(cabbageAttack != null && !cabbageAttack.IsReleased, "The real Cabbagepult control must inherit its melee Attack runtime.");
				Check(fireComponent != null && fireComponent.fireCheckList?.Count == 1 && fireComponent.fireCheckList[0]?.projectile != null, "Cornpult's ranged Fire definition must remain configured while melee is tested.");
				if ((cornAttack?.IsReleased ?? true) || (fireComponent?.IsReleased ?? true) || (cabbageAttack?.IsReleased ?? true))
				{
					goto end_IL_00d7;
				}
				cornpult.Hypnoses();
				cabbagepult.Hypnoses();
				await WaitFrames(3);
				Check(cornpult.instance.hypnoses && cabbagepult.instance.hypnoses && cornpult.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && cabbagepult.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, "Both real plant-zombies must be hypnotized into the Plant camp.");
				AttackComponentDefinition attackComponentDefinition = cornAttack.ComponentDefinition as AttackComponentDefinition;
				AttackComponentDefinition attackComponentDefinition2 = cabbageAttack.ComponentDefinition as AttackComponentDefinition;
				Check(attackComponentDefinition2 != null && attackComponentDefinition2.useParentHitBox && attackComponentDefinition2.checkLine, "Control: Cabbagepult must preserve the inherited zombie contact-bite geometry.");
				Check(attackComponentDefinition != null && attackComponentDefinition.useParentHitBox && attackComponentDefinition.checkLine && attackComponentDefinition.useCheckAreaGridColumn, "Cornpult's local Attack override must preserve parent-hitbox and same-line bite semantics while adding grid-column lookup.");
				Check(cabbageAttack.TryGetCheckAreaWorldRect(out var worldRect) && worldRect.HasArea(), "Control: Cabbagepult must expose live contact geometry from its parent hitbox.");
				Check(cornAttack.TryGetCheckAreaWorldRect(out var worldRect2) && worldRect2.HasArea(), "Cornpult must expose live contact geometry instead of an empty melee area.");
				cornpult.ProcessMode = ProcessModeEnum.Disabled;
				cabbagepult.ProcessMode = ProcessModeEnum.Disabled;
				cornTarget.ProcessMode = ProcessModeEnum.Disabled;
				cabbageTarget.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				cabbageAttack.target = cabbageTarget;
				Check(cabbageAttack.CanAttack() && cabbageAttack.target == cabbageTarget, "Control: a hypnotized Cabbagepult must recognize a touching zombie as biteable.");
				cornAttack.target = cornTarget;
				Check(cornAttack.CanAttack() && cornAttack.target == cornTarget, "A hypnotized Cornpult must recognize a touching zombie as biteable.");
				goto end_IL_00c5;
				end_IL_00d7:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewHypnotizedCornpultMeleeRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c5;
			}
			return;
			end_IL_00c5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"HYPNOTIZED_CORNPULT_MELEE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I gridPos, Vector2 position)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = gridPos;
		character.GlobalPosition = position;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewHypnotizedCornpultMeleeRuntimeTest] " + message);
		}
	}
}
