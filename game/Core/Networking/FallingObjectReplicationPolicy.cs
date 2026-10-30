using System;
using Godot;

public static class FallingObjectReplicationPolicy
{
	private sealed class NoopDisposable : IDisposable
	{
		public static readonly NoopDisposable Instance = new NoopDisposable();

		public void Dispose()
		{
		}
	}

	public static bool IsCapturedSource(TowerDefenseCharacter character)
	{
		if (!(character is TowerDefenseZombieYetiDigger) && !(character is TowerDefenseZombieYetiFootball))
		{
			return character is TowerDefensePlantPanGoldBean;
		}
		return true;
	}

	public static bool ShouldSuppressRemoteDestroySet(TowerDefenseCharacter character)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			if (!(character is TowerDefenseZombieYetiDigger))
			{
				return character is TowerDefenseZombieYetiFootball;
			}
			return true;
		}
		return false;
	}

	public static IDisposable BeginCapture(TowerDefenseCharacter character)
	{
		if (!IsCapturedSource(character) || !Global.IsMultiplayerMode || !MultiPlayerManager.IsHost || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return NoopDisposable.Instance;
		}
		return TowerDefenseManager.Instance.BeginFallingObjectReplicationCapture();
	}
}
