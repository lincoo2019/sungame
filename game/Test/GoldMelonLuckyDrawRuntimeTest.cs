using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class GoldMelonLuckyDrawRuntimeTest : Node
{
	private const string GoldMelonPath = "res://Registry/Projectile/Config/Melon/GoldMelon.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			TowerDefenseProjectileData towerDefenseProjectileData = ResourceLoader.Load<TowerDefenseProjectileData>("res://Registry/Projectile/Config/Melon/GoldMelon.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(towerDefenseProjectileData != null, "GoldMelon projectile data must load.");
			Check(towerDefenseProjectileData != null && towerDefenseProjectileData.hitTargetEventList.Count == 1, "GoldMelon must have one hit-target lottery event.");
			TowerDefenseCharacterEventLuckyDraw towerDefenseCharacterEventLuckyDraw = ((towerDefenseProjectileData != null && towerDefenseProjectileData.hitTargetEventList.Count > 0) ? (towerDefenseProjectileData.hitTargetEventList[0] as TowerDefenseCharacterEventLuckyDraw) : null);
			Check(towerDefenseCharacterEventLuckyDraw != null, "GoldMelon hit event must be the lucky-draw event.");
			Check(towerDefenseCharacterEventLuckyDraw != null && towerDefenseCharacterEventLuckyDraw.eventList.Count == 6, "GoldMelon design contains exactly six documented prize outcomes.");
			if (towerDefenseCharacterEventLuckyDraw != null && towerDefenseCharacterEventLuckyDraw.eventList.Count == 6)
			{
				double[] array = new double[6] { 50.0, 30.0, 10.0, 6.0, 3.0, 1.0 };
				for (int i = 0; i < array.Length; i++)
				{
					TowerDefenseCharacterEventLuckyDrawItem towerDefenseCharacterEventLuckyDrawItem = towerDefenseCharacterEventLuckyDraw.eventList[i];
					Check(towerDefenseCharacterEventLuckyDrawItem?._event != null, $"Prize outcome {i + 1} must have an executable event.");
					Check(towerDefenseCharacterEventLuckyDrawItem != null && Math.Abs(towerDefenseCharacterEventLuckyDrawItem.weight - array[i]) < 0.0001, $"Prize outcome {i + 1} weight must remain {array[i]}%.");
				}
				double num = towerDefenseCharacterEventLuckyDraw.eventList.Sum((TowerDefenseCharacterEventLuckyDrawItem item) => item.weight);
				Check(Math.Abs(num - 100.0) < 0.0001, $"GoldMelon prize weights must total 100%; got {num}.");
				GoldMelonLuckyDrawRuntimeTest goldMelonLuckyDrawRuntimeTest = this;
				Array<TowerDefenseCharacterEventLuckyDrawItem> eventList = towerDefenseCharacterEventLuckyDraw.eventList;
				goldMelonLuckyDrawRuntimeTest.Check(eventList[eventList.Count - 1]._event is TowerDefenseCharacterEventHypnoses, "The documented 1% first prize must remain the hypnosis outcome.");
				Array<TowerDefenseCharacterEventLuckyDrawItem> array2 = new Array<TowerDefenseCharacterEventLuckyDrawItem>();
				GoldMelonLuckyDrawProbeEvent[] array3 = new GoldMelonLuckyDrawProbeEvent[array.Length];
				for (int num2 = 0; num2 < array.Length; num2++)
				{
					array3[num2] = new GoldMelonLuckyDrawProbeEvent();
					array2.Add(new TowerDefenseCharacterEventLuckyDrawItem
					{
						_event = array3[num2],
						weight = towerDefenseCharacterEventLuckyDraw.eventList[num2].weight
					});
				}
				GoldMelonLuckyDrawProbeProjectile goldMelonLuckyDrawProbeProjectile = new GoldMelonLuckyDrawProbeProjectile();
				AddChild(goldMelonLuckyDrawProbeProjectile, forceReadableName: false, InternalMode.Disabled);
				try
				{
					GD.Seed(20260717uL);
					for (int num3 = 0; num3 < 20000; num3++)
					{
						TowerDefenseCharacterEventLuckyDraw.Run(Vector2.Zero, null, array2, goldMelonLuckyDrawProbeProjectile);
					}
					Check(array3.Sum((GoldMelonLuckyDrawProbeEvent probe) => probe.ProjectDispatchCount) == 20000, "Every draw must dispatch exactly one projectile event.");
					Check(array3.All((GoldMelonLuckyDrawProbeEvent probe) => probe.ProjectDispatchCount > 0), "Every configured prize, including the 1% final item, must be reachable.");
					Check(array3[^1].ProjectDispatchCount > 0, "The 1% first-prize item must execute through the projectile dispatch path.");
					Check(array3.All((GoldMelonLuckyDrawProbeEvent probe) => probe.PositionDispatchCount == 0), "A valid projectile must use ExecuteProject rather than the position-only path.");
				}
				finally
				{
					goldMelonLuckyDrawProbeProjectile.QueueFree();
				}
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[GoldMelonLuckyDrawRuntimeTest] Unexpected exception: {value}");
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool flag = _failures == 0;
		GD.Print($"GOLD_MELON_LUCKY_DRAW_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[GoldMelonLuckyDrawRuntimeTest] " + message);
		}
	}
}
