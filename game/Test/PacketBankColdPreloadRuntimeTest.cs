using System.Collections.Generic;
using Godot;

public partial class PacketBankColdPreloadRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		ResourceManager resourceManager = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(resourceManager))
		{
			Fail("ResourceManager is unavailable");
			Finish();
			return;
		}
		resourceManager.BeginLoad();
		for (int frame = 0; frame < 7200; frame++)
		{
			if (resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Ready)
			{
				break;
			}
			if (resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Failed)
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if (!resourceManager.AreFullGameplayResourcesReady)
		{
			Fail($"ResourceManager full readiness failed: state={resourceManager.CurrentGameplayResourceLoadState}, error={resourceManager.FullGameplayResourceLoadError}");
			Finish();
		}
		else
		{
			ValidateFullRegistryMemoryHits(resourceManager);
			Finish();
		}
	}

	private void ValidateFullRegistryMemoryHits(ResourceManager resourceManager)
	{
		int lateCharacterResourceLoadCount = resourceManager.LateCharacterResourceLoadCount;
		foreach (string key in resourceManager.TOWERDEFENSE_CHARCATERS.Keys)
		{
			Check(GodotObject.IsInstanceValid(resourceManager.GetCharacterScene(key)), "Character Scene is invalid: " + key);
		}
		foreach (string key2 in resourceManager.CHARCTAER_SPRITE.Keys)
		{
			Check(GodotObject.IsInstanceValid(resourceManager.GetCharacterSprite(key2)), "Character Sprite is invalid: " + key2);
		}
		foreach (string packetName in resourceManager.GetPacketNames())
		{
			Variant packet = resourceManager.GetPacket(packetName);
			Check(packet.VariantType == Variant.Type.Object && GodotObject.IsInstanceValid(packet.AsGodotObject()), "Packet config is invalid: " + packetName);
		}
		foreach (string requiredRuntimePacketBank in FullGameplayResourceManifest.RequiredRuntimePacketBanks)
		{
			TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(requiredRuntimePacketBank);
			Check(GodotObject.IsInstanceValid(packetBankData), "Required PacketBank is invalid: " + requiredRuntimePacketBank);
			if (!GodotObject.IsInstanceValid(packetBankData))
			{
				continue;
			}
			foreach (Variant packet2 in packetBankData.GetPacketList())
			{
				string text = packet2.AsString();
				Check(GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketConfigReadOnly(text)), "PacketBank candidate is not resident: bank=" + requiredRuntimePacketBank + ", packet=" + text);
			}
		}
		FullGameplayResourceLoadMetricsSnapshot fullGameplayResourceLoadMetrics = resourceManager.FullGameplayResourceLoadMetrics;
		Check(resourceManager.LateCharacterResourceLoadCount == lateCharacterResourceLoadCount, $"lateCharacterResourceLoadCount changed from {lateCharacterResourceLoadCount} to {resourceManager.LateCharacterResourceLoadCount}");
		Check(fullGameplayResourceLoadMetrics.FailureCount == 0, $"full resource failure count was {fullGameplayResourceLoadMetrics.FailureCount}");
		Check(fullGameplayResourceLoadMetrics.PacketBankMissingCount == 0, $"PacketBank missing count was {fullGameplayResourceLoadMetrics.PacketBankMissingCount}");
		Check(fullGameplayResourceLoadMetrics.FullWallMilliseconds <= 15000.0, $"process-cold full readiness took {fullGameplayResourceLoadMetrics.FullWallMilliseconds:F3} ms, expected at most 15000 ms");
		foreach (FullGameplayRootLoadMetric rootMetric in fullGameplayResourceLoadMetrics.RootMetrics)
		{
			Check(rootMetric.QueueWaitMilliseconds == 0.0, $"root queue wait was not zero: category={rootMetric.Category}, path={rootMetric.Path}, queueWaitMs={rootMetric.QueueWaitMilliseconds:F3}");
		}
		GD.Print($"FULL_GAMEPLAY_RESOURCE_READY_RESULT passed={_failures.Count == 0} characters={resourceManager.TOWERDEFENSE_CHARCATERS.Count} sprites={resourceManager.CHARCTAER_SPRITE.Count} packets={resourceManager.TOWERDEFENSE_PACKETS.Count} sceneRoots={fullGameplayResourceLoadMetrics.CategoryMetrics.GetValueOrDefault("CharacterScene")?.RootCount ?? 0} independentSpriteRoots={fullGameplayResourceLoadMetrics.CategoryMetrics.GetValueOrDefault("IndependentSprite")?.RootCount ?? 0} packetRoots={fullGameplayResourceLoadMetrics.CategoryMetrics.GetValueOrDefault("Packet")?.RootCount ?? 0} gameplayMs={fullGameplayResourceLoadMetrics.CharacterSceneMilliseconds + fullGameplayResourceLoadMetrics.SpriteMilliseconds + fullGameplayResourceLoadMetrics.PacketMilliseconds:F3} fullWallMs={fullGameplayResourceLoadMetrics.FullWallMilliseconds:F3} lateCharacterResourceLoadCount={resourceManager.LateCharacterResourceLoadCount} failures={_failures.Count}");
	}

	private void Check(bool condition, string message)
	{
		if (!condition)
		{
			Fail(message);
		}
	}

	private void Fail(string message)
	{
		_failures.Add(message);
		GD.PrintErr("FULL_GAMEPLAY_RESOURCE_READY_FAILURE " + message);
	}

	private void Finish()
	{
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
