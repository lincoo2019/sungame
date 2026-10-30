using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class RandomTransformationComponent : CharacterComponentRuntime
{
	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncPacketListKey = new StringName("packetList");

	public TowerDefenseCharacter parent;

	public Array<RandomTransformationComponentPacketBankConfig> includePacketBankList = new Array<RandomTransformationComponentPacketBankConfig>();

	public Array<RandomTransformationComponentPacketBankConfig> excludePacketBankList = new Array<RandomTransformationComponentPacketBankConfig>();

	public Array<string> includePacketNameList = new Array<string>();

	public Array<string> excludePacketNameList = new Array<string>();

	public readonly List<string> packetList = new List<string>();

	private readonly HashSet<string> _packetSet = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _excludeSet = new HashSet<string>(StringComparer.Ordinal);

	private readonly List<string> _configPacketBuffer = new List<string>();

	private readonly Dictionary _syncPayload = new Dictionary();

	private readonly Array<string> _syncPacketNames = new Array<string>();

	private bool _dirty = true;

	private bool _configured;

	private bool _syncPayloadInitialized;

	private ulong _candidateRevision;

	private ulong _syncCandidateRevision;

	private int _syncPacketCount;

	private RandomTransformationComponentDefinition Definition => base.ComponentDefinition as RandomTransformationComponentDefinition;

	private static bool IsRemoteClient
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		if (!IsRemoteClient && _dirty)
		{
			Refresh();
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnReleased()
	{
		parent = null;
		packetList.Clear();
		_packetSet.Clear();
		_excludeSet.Clear();
		_configPacketBuffer.Clear();
		includePacketBankList.Clear();
		excludePacketBankList.Clear();
		includePacketNameList.Clear();
		excludePacketNameList.Clear();
		_candidateRevision++;
		ClearSyncPayload();
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			RandomTransformationComponentDefinition definition = Definition;
			if (definition != null)
			{
				Copy(definition.includePacketBankList, includePacketBankList);
				Copy(definition.excludePacketBankList, excludePacketBankList);
				Copy(definition.includePacketNameList, includePacketNameList);
				Copy(definition.excludePacketNameList, excludePacketNameList);
				_dirty = true;
				_configured = true;
			}
		}
	}

	public void Refresh()
	{
		packetList.Clear();
		_packetSet.Clear();
		_excludeSet.Clear();
		for (int i = 0; i < excludePacketNameList.Count; i++)
		{
			AddName(_excludeSet, excludePacketNameList[i]);
		}
		CollectConfigNames(excludePacketBankList, _excludeSet);
		for (int j = 0; j < includePacketNameList.Count; j++)
		{
			AddCandidate(includePacketNameList[j]);
		}
		for (int k = 0; k < includePacketBankList.Count; k++)
		{
			RandomTransformationComponentPacketBankConfig randomTransformationComponentPacketBankConfig = includePacketBankList[k];
			if (GodotObject.IsInstanceValid(randomTransformationComponentPacketBankConfig))
			{
				_configPacketBuffer.Clear();
				randomTransformationComponentPacketBankConfig.FillPacketList(_configPacketBuffer);
				for (int l = 0; l < _configPacketBuffer.Count; l++)
				{
					AddCandidate(_configPacketBuffer[l]);
				}
			}
		}
		_dirty = false;
		_candidateRevision++;
	}

	private void CollectConfigNames(Array<RandomTransformationComponentPacketBankConfig> configs, HashSet<string> output)
	{
		if (configs == null)
		{
			return;
		}
		for (int i = 0; i < configs.Count; i++)
		{
			RandomTransformationComponentPacketBankConfig randomTransformationComponentPacketBankConfig = configs[i];
			if (GodotObject.IsInstanceValid(randomTransformationComponentPacketBankConfig))
			{
				_configPacketBuffer.Clear();
				randomTransformationComponentPacketBankConfig.FillPacketList(_configPacketBuffer);
				for (int j = 0; j < _configPacketBuffer.Count; j++)
				{
					AddName(output, _configPacketBuffer[j]);
				}
			}
		}
	}

	private static void AddName(HashSet<string> targetSet, string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			targetSet.Add(name);
		}
	}

	private void AddCandidate(string name)
	{
		if (!string.IsNullOrEmpty(name) && !_excludeSet.Contains(name) && _packetSet.Add(name))
		{
			packetList.Add(name);
		}
	}

	public string GetRandomPacketName()
	{
		if (_dirty)
		{
			if (IsRemoteClient)
			{
				return string.Empty;
			}
			Refresh();
		}
		if (packetList.Count != 0)
		{
			return packetList[(int)(GD.Randi() % (uint)packetList.Count)];
		}
		return string.Empty;
	}

	public string GetRandPacketName()
	{
		return GetRandomPacketName();
	}

	public override Dictionary ExportComponentSave()
	{
		return SerializeCandidates();
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		DeserializeCandidates(data);
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeCandidatesForSync();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		DeserializeCandidates(data);
	}

	private Dictionary SerializeCandidates()
	{
		if (_dirty && !IsRemoteClient)
		{
			Refresh();
		}
		Array<string> array = new Array<string>();
		for (int i = 0; i < packetList.Count; i++)
		{
			array.Add(packetList[i]);
		}
		return new Dictionary { { "packetList", array } };
	}

	private Dictionary SerializeCandidatesForSync()
	{
		if (_dirty && !IsRemoteClient)
		{
			Refresh();
		}
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 2)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 1 && _syncCandidateRevision == _candidateRevision && _syncPacketCount == packetList.Count)
			{
				return _syncPayload;
			}
		}
		_syncPacketNames.Clear();
		for (int i = 0; i < packetList.Count; i++)
		{
			string item = packetList[i];
			_syncPacketNames.Add(item);
		}
		_syncPayload.Clear();
		_syncPayload[SyncPacketListKey] = _syncPacketNames;
		_syncCandidateRevision = _candidateRevision;
		_syncPacketCount = packetList.Count;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		_syncPayload.Clear();
		_syncPacketNames.Clear();
		_syncCandidateRevision = 0uL;
		_syncPacketCount = 0;
		_syncPayloadInitialized = false;
	}

	private void DeserializeCandidates(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		packetList.Clear();
		_packetSet.Clear();
		Array<string> array = data.GetValueOrDefault("packetList", new Array<string>()).AsGodotArray<string>();
		for (int i = 0; i < array.Count; i++)
		{
			if (!string.IsNullOrEmpty(array[i]) && _packetSet.Add(array[i]))
			{
				packetList.Add(array[i]);
			}
		}
		_dirty = false;
		_candidateRevision++;
	}

	private static void Copy(Array<RandomTransformationComponentPacketBankConfig> source, Array<RandomTransformationComponentPacketBankConfig> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}

	private static void Copy(Array<string> source, Array<string> target)
	{
		target.Clear();
		if (source != null)
		{
			for (int i = 0; i < source.Count; i++)
			{
				target.Add(source[i]);
			}
		}
	}
}
