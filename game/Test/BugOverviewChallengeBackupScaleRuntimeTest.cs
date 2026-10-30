using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewChallengeBackupScaleRuntimeTest : Node
{
	private const string BackupScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Backup/Scene/TowerDefenseZombieBackup.tscn";

	private static readonly Vector2 AuthoredSpriteScale = new Vector2(0.8f, 0.8f);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		PackedScene scene = null;
		TowerDefenseZombieBackup backup = null;
		try
		{
			try
			{
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/Backup/Scene/TowerDefenseZombieBackup.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(scene), "The real Challenge Backup scene must load.");
				backup = scene?.Instantiate<TowerDefenseZombieBackup>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(backup), "The real Challenge Backup scene must instantiate its production script.");
				if (!GodotObject.IsInstanceValid(backup))
				{
					goto end_IL_003e;
				}
				backup.inGame = false;
				backup.editorPreviewMode = true;
				AddChild(backup, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				backup.ProcessMode = ProcessModeEnum.Disabled;
				Check(backup.IsNodeReady() && GodotObject.IsInstanceValid(backup.sprite), "The real Challenge Backup animation root must be ready.");
				if (!GodotObject.IsInstanceValid(backup.sprite))
				{
					goto end_IL_003e;
				}
				Vector2 scale = backup.sprite.Scale;
				Transform2D globalTransform = backup.sprite.GlobalTransform;
				Check(scale.IsEqualApprox(AuthoredSpriteScale), $"Challenge Backup must begin at the authored uniform scale; got {scale}.");
				backup.DanceEntered();
				backup.DanceExited();
				Vector2 scale2 = backup.sprite.Scale;
				Transform2D globalTransform2 = backup.sprite.GlobalTransform;
				Check(scale2.IsEqualApprox(scale), $"Dance exit must preserve the authored local scale; before={scale}, after={scale2}.");
				Check(Mathf.IsEqualApprox(globalTransform.X.Length(), globalTransform2.X.Length()) && Mathf.IsEqualApprox(globalTransform.Y.Length(), globalTransform2.Y.Length()), $"Dance exit must preserve both rendered basis lengths; before=({globalTransform.X.Length()}, {globalTransform.Y.Length()}), after=({globalTransform2.X.Length()}, {globalTransform2.Y.Length()}).");
				goto end_IL_0035;
				end_IL_003e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewChallengeBackupScaleRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0035;
			}
			return;
			end_IL_0035:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(backup) && !backup.IsQueuedForDeletion())
			{
				backup.QueueFree();
			}
			await WaitFrames(4);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			scene?.Dispose();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 6;
		GD.Print($"CHALLENGE_BACKUP_SCALE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
			GD.PushError("[BugOverviewChallengeBackupScaleRuntimeTest] " + message);
		}
	}
}
