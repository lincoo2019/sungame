using System;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentPogoShadowGroundRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Scene/TowerDefenseZombiePogo.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		bool previousUseMultiMesh = ShadowComponent.UseMultiMesh;
		TowerDefenseZombiePogo pogo = null;
		PackedScene scene = null;
		try
		{
			try
			{
				ShadowComponent.UseMultiMesh = false;
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Scene/TowerDefenseZombiePogo.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(scene), "The real Pogo zombie scene must load.");
				pogo = scene?.Instantiate<TowerDefenseZombiePogo>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(pogo), "The real Pogo zombie scene must instantiate its production script.");
				if (!GodotObject.IsInstanceValid(pogo))
				{
					goto end_IL_0049;
				}
				pogo.editorPreviewMode = true;
				pogo.inGame = false;
				AddChild(pogo, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				BugDepartmentPogoShadowGroundRuntimeTest bugDepartmentPogoShadowGroundRuntimeTest = this;
				int condition;
				if (pogo.IsNodeReady())
				{
					ShadowComponent shadowComponent = pogo.shadowComponent;
					condition = ((shadowComponent != null && shadowComponent.Lifecycle == ComponentRuntimeLifecycle.Active) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugDepartmentPogoShadowGroundRuntimeTest.Check((byte)condition != 0, "The real Pogo zombie ShadowComponent must be active.");
				Check(GodotObject.IsInstanceValid(pogo.shadowSprite) && GodotObject.IsInstanceValid(pogo.spriteGroup) && GodotObject.IsInstanceValid(pogo.transformPoint), "The real Pogo zombie must bind its shadow and airborne visual hierarchy.");
				if (!GodotObject.IsInstanceValid(pogo.shadowSprite) || !GodotObject.IsInstanceValid(pogo.spriteGroup) || !GodotObject.IsInstanceValid(pogo.transformPoint) || (pogo.shadowComponent?.IsReleased ?? true))
				{
					goto end_IL_0049;
				}
				BugDepartmentPogoShadowGroundRuntimeTest bugDepartmentPogoShadowGroundRuntimeTest2 = this;
				AdobeAnimateSprite sprite = pogo.sprite;
				bugDepartmentPogoShadowGroundRuntimeTest2.Check(sprite != null && sprite.layerVisible.Count > 0 && !sprite.layerVisible[0], "The Pogo animation's embedded _ground layer must stay hidden; the root ShadowSprite owns the ground shadow.");
				Check(pogo.shadowSprite.GetParent() == pogo, "The shadow must be a direct Pogo root child, outside the airborne SpriteGroup.");
				Check(pogo.shadowSprite.ZAsRelative && pogo.shadowSprite.ZIndex == -1, "The ground shadow must retain its authored relative Z layer below the zombie.");
				pogo.ProcessMode = ProcessModeEnum.Disabled;
				pogo.GlobalPosition = new Vector2(640f, 300f);
				pogo.groundHeight = 0.0;
				pogo.z = 0.0;
				pogo.isGround = true;
				pogo.gravityUse = true;
				pogo.gravity = 490.0;
				pogo.gravityScale = 1.5;
				pogo.ySpeed = -300.0;
				pogo.shadowComponent.MarkDirty();
				pogo.shadowComponent.BatchUpdate();
				float y = pogo.shadowSprite.GlobalPosition.Y;
				int zIndex = pogo.shadowSprite.ZIndex;
				Node parent = pogo.shadowSprite.GetParent();
				float num = 0f;
				float num2 = 0f;
				bool flag = true;
				bool flag2 = true;
				for (int i = 0; i < 60; i++)
				{
					pogo.PhysiceUpdate(1f / 60f);
					pogo.shadowComponent.BatchUpdate();
					num = Mathf.Max(num, (float)pogo.z);
					num2 = Mathf.Max(num2, pogo.GlobalPosition.Y - pogo.spriteGroup.GlobalPosition.Y);
					flag &= Mathf.Abs(pogo.shadowSprite.GlobalPosition.Y - y) <= 0.05f;
					flag2 &= pogo.shadowSprite.GetParent() == parent && pogo.shadowSprite.ZIndex == zIndex;
				}
				Check(num >= 50f && num2 >= 50f, $"The production Pogo arc must visibly leave the ground; height={num}, visualLift={num2}.");
				Check(flag, $"The root shadow world Y must remain on the ground throughout the real Pogo arc; expected={y}, actual={pogo.shadowSprite.GlobalPosition.Y}.");
				Check(flag2, "The root shadow parent and relative Z layer must not follow the airborne visual point.");
				goto end_IL_0040;
				end_IL_0049:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentPogoShadowGroundRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0040;
			}
			return;
			end_IL_0040:;
		}
		finally
		{
			ShadowComponent.UseMultiMesh = previousUseMultiMesh;
			if (GodotObject.IsInstanceValid(pogo) && !pogo.IsQueuedForDeletion())
			{
				pogo.QueueFree();
			}
			await WaitFrames(6);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			scene?.Dispose();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag3 = _failures == 0 && _checks == 10;
		GD.Print($"POGO_SHADOW_GROUND_RESULT version=1 passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
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
			GD.PushError("[BugDepartmentPogoShadowGroundRuntimeTest] " + message);
		}
	}
}
