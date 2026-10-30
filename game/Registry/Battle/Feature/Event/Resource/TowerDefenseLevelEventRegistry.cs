using System;
using System.Collections.Generic;
using Godot;

public static class TowerDefenseLevelEventRegistry
{
	private static readonly Dictionary<string, TowerDefenseLevelEventDefinition> DefinitionsById = new Dictionary<string, TowerDefenseLevelEventDefinition>(StringComparer.Ordinal);

	private static readonly Dictionary<Type, TowerDefenseLevelEventDefinition> DefinitionsByType = new Dictionary<Type, TowerDefenseLevelEventDefinition>();

	private static readonly List<TowerDefenseLevelEventDefinition> EditorDefinitions = new List<TowerDefenseLevelEventDefinition>();

	private static readonly IReadOnlyList<TowerDefenseLevelEventDefinition> ReadOnlyEditorDefinitions = EditorDefinitions.AsReadOnly();

	public static IReadOnlyList<TowerDefenseLevelEventDefinition> Definitions => ReadOnlyEditorDefinitions;

	static TowerDefenseLevelEventRegistry()
	{
		RegisterBuiltIn<TowerDefenseLevelEventConditionNpcTalkFinish>("LEVLE_EVENT_CONDITION_NPC_TALK_FINISH", "ConditionNpcTalkFinish");
		RegisterBuiltIn<TowerDefenseLevelEventGravestoneCreateRandom>("LEVLE_EVENT_GRAVESTONE_CREATE_RANDOM", "GravestoneCreateRandom");
		RegisterBuiltIn<TowerDefenseLevelEventGravestoneSpawnZombie>("LEVLE_EVENT_GRAVESTONE_SPAWN_ZOMBIE", "GravestoneSpawnZombie");
		RegisterBuiltIn<TowerDefenseLevelEventGridSpawnZombie>("LEVLE_EVENT_GRID_SPAWN_ZOMBIE", "GridSpawnZombie");
		RegisterBuiltIn<TowerDefenseLevelEventCoralTrioSpawn>("LEVLE_EVENT_CORAL_TRIO_SPAWN", "CoralTrioSpawn");
		RegisterBuiltIn<TowerDefenseLevelEventBungiTrioSpawn>("LEVLE_EVENT_BUNGI_TRIO_SPAWN", "BungiTrioSpawn");
		RegisterBuiltIn<TowerDefenseLevelEventCurrentMapFunctionExecute>("LEVLE_EVENT_CURRENTMAP_FUNCTION_EXECUTE", "CurrentMapFunctionExecute");
		RegisterBuiltIn<TowerDefenseLevelEventCurrentMapUseStripe>("LEVLE_EVENT_CURRENTMAP_USE_STRIPE", "CurrentMapUseStripe");
		RegisterBuiltIn<TowerDefenseLevelEventCurrentMapUseWarningLine>("LEVLE_EVENT_CURRENTMAP_USE_WARNINGLINE", "CurrentMapUseWarningLine");
		RegisterBuiltIn<TowerDefenseLevelEventCurrentMapCharacterClear>("LEVLE_EVENT_CURRENTMAP_CHARACTER_CLEAR", "CurrentMapCharacterClear");
		RegisterBuiltIn<TowerDefenseLevelEventMapChange>("LEVLE_EVENT_MAP_CHANGE", "MapChange");
		RegisterBuiltIn<TowerDefenseLevelEventAddPacket>("LEVLE_EVENT_ADD_PACKET", "AddPacket");
		RegisterBuiltIn<TowerDefenseLevelEventTipsPlay>("LEVLE_EVENT_TIPS_PLAY", "TipsPlay");
		RegisterBuiltIn<TowerDefenseLevelEventSetGameMode>("LEVLE_EVENT_SET_GAME_MODE", "SetGameMode");
		RegisterBuiltIn<TowerDefenseLevelEventCreateProtal>("LEVLE_EVENT_CREATE_PROTAL", "CreateProtal");
		RegisterBuiltIn<TowerDefenseLevelEventChangeProtalPos>("LEVLE_EVENT_CHAGE_PROTAL_POS", "ChangeProtalPos");
		RegisterBuiltIn<TowerDefenseLevelEventBungiSpawnZombie>("LEVLE_EVENT_BUNGI_SPAWN_ZOMBIE", "BungiSpawnZombie");
		RegisterBuiltIn<TowerDefenseLevelEventRuneStorm>("LEVLE_EVENT_RUNE_STORM", "RuneStorm");
	}

	public static TowerDefenseLevelEventBase Create(string id)
	{
		if (!TryGetDefinition(id, out var definition))
		{
			return null;
		}
		try
		{
			return definition.Factory();
		}
		catch (Exception value)
		{
			GD.PushError($"[LevelEventRegistry] Failed to create '{definition.Id}': {value}");
			return null;
		}
	}

	public static bool TryGetDefinition(string id, out TowerDefenseLevelEventDefinition definition)
	{
		definition = null;
		if (string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		return DefinitionsById.TryGetValue(id, out definition);
	}

	public static bool TryGetDefinition(TowerDefenseLevelEventBase levelEvent, out TowerDefenseLevelEventDefinition definition)
	{
		definition = null;
		if (GodotObject.IsInstanceValid(levelEvent))
		{
			return DefinitionsByType.TryGetValue(levelEvent.GetType(), out definition);
		}
		return false;
	}

	public static bool Register<TEvent>(string displayKey, string eventId, Func<TEvent> factory = null, bool replace = false) where TEvent : TowerDefenseLevelEventBase, new()
	{
		Func<TowerDefenseLevelEventBase> factory2 = ((factory == null) ? ((Func<TowerDefenseLevelEventBase>)(() => new TEvent())) : ((Func<TowerDefenseLevelEventBase>)(() => factory())));
		return Register(displayKey, eventId, typeof(TEvent), factory2, replace);
	}

	private static bool Register(string displayKey, string eventId, Type eventType, Func<TowerDefenseLevelEventBase> factory, bool replace)
	{
		if (string.IsNullOrWhiteSpace(displayKey) || string.IsNullOrWhiteSpace(eventId) || eventType == null || factory == null || !typeof(TowerDefenseLevelEventBase).IsAssignableFrom(eventType))
		{
			return false;
		}
		DefinitionsById.TryGetValue(eventId, out var value);
		if (!replace && value != null)
		{
			return false;
		}
		if (DefinitionsByType.TryGetValue(eventType, out var value2) && value2.Id != eventId)
		{
			return false;
		}
		TowerDefenseLevelEventDefinition towerDefenseLevelEventDefinition = new TowerDefenseLevelEventDefinition(eventId, displayKey, eventType, factory);
		if (value != null)
		{
			DefinitionsByType.Remove(value.EventType);
			int num = EditorDefinitions.IndexOf(value);
			if (num >= 0)
			{
				EditorDefinitions[num] = towerDefenseLevelEventDefinition;
			}
		}
		else
		{
			EditorDefinitions.Add(towerDefenseLevelEventDefinition);
		}
		DefinitionsById[eventId] = towerDefenseLevelEventDefinition;
		DefinitionsByType[eventType] = towerDefenseLevelEventDefinition;
		return true;
	}

	private static void RegisterBuiltIn<TEvent>(string displayKey, string eventId) where TEvent : TowerDefenseLevelEventBase, new()
	{
		Register<TEvent>(displayKey, eventId);
	}
}
