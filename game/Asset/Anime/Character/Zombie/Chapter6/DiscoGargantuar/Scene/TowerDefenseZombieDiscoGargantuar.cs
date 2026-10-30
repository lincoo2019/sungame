using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieDiscoGargantuar : TowerDefenseZombieGargantuarBase, IJackson, INetworkDancerOwner
{
	private const double PendingDancerResolveIntervalSeconds = 0.25;

	private const string ZOMBIE_BACKUP_GARGANTUAR_BODY12 = "uid://c53wvxn6c14gs";

	private const string ZOMBIE_BACKUP_GARGANTUAR_BODY13 = "uid://0f6e278qdym4";

	private DancingComponent _dancingComponent;

	private readonly int[] _pendingDancerSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingDancerNodeNames = new string[4] { "", "", "", "" };

	private double _pendingDancerResolveRemaining;

	private bool _hasPendingDancerRelations;

	private string _dancerPacketName = "";

	private bool HasDancingRuntime
	{
		get
		{
			DancingComponent dancingComponent = _dancingComponent;
			if (dancingComponent == null)
			{
				return false;
			}
			return !dancingComponent.IsReleased;
		}
	}

	private bool HasDancingComponent
	{
		get
		{
			if (HasDancingRuntime)
			{
				return _dancingComponent.Lifecycle == ComponentRuntimeLifecycle.Active;
			}
			return false;
		}
	}

	[Export(PropertyHint.None, "")]
	public string dancerPacketName
	{
		get
		{
			return _dancerPacketName;
		}
		set
		{
			_dancerPacketName = value;
			if (!string.IsNullOrEmpty(value) && HasDancingComponent)
			{
				_dancingComponent.dancerPacketName = value;
			}
		}
	}

	public void RemoveDancer(TowerDefenseCharacter dancer)
	{
		for (int i = 0; i < 4; i++)
		{
			bool flag = GodotObject.IsInstanceValid(dancer) && dancer.syncId >= 0 && _pendingDancerSyncIds[i] == dancer.syncId;
			if (IsSameDancer(GetDancer(i), dancer) || flag)
			{
				SetNetworkDancer(i, null);
				break;
			}
		}
	}

	private static bool IsSameDancer(TowerDefenseCharacter candidate, TowerDefenseCharacter dancer)
	{
		if (candidate != dancer)
		{
			if (GodotObject.IsInstanceValid(candidate) && GodotObject.IsInstanceValid(dancer) && candidate.syncId >= 0)
			{
				return candidate.syncId == dancer.syncId;
			}
			return false;
		}
		return true;
	}

	public void SetNetworkDancer(int slot, TowerDefenseCharacter dancer)
	{
		if (slot < 0 || slot >= 4)
		{
			return;
		}
		if (!HasDancingRuntime)
		{
			if (GodotObject.IsInstanceValid(dancer))
			{
				_pendingDancerSyncIds[slot] = dancer.syncId;
				_pendingDancerNodeNames[slot] = "";
			}
			else
			{
				_pendingDancerSyncIds[slot] = -1;
				_pendingDancerNodeNames[slot] = "";
			}
			RefreshPendingDancerBatchEligibility();
		}
		else
		{
			_dancingComponent.SetNetworkDancer(slot, dancer);
			_pendingDancerSyncIds[slot] = -1;
			_pendingDancerNodeNames[slot] = "";
			RefreshPendingDancerBatchEligibility();
		}
	}

	private TowerDefenseCharacter GetDancer(int slot)
	{
		if (!HasDancingRuntime)
		{
			return null;
		}
		return _dancingComponent.GetDancer(slot);
	}

	private void RefreshPendingDancerBatchEligibility()
	{
		bool flag = false;
		for (int i = 0; i < 4; i++)
		{
			if (_pendingDancerSyncIds[i] >= 0 || _pendingDancerNodeNames[i] != "")
			{
				flag = true;
				break;
			}
		}
		if (_hasPendingDancerRelations != flag)
		{
			_hasPendingDancerRelations = flag;
			_pendingDancerResolveRemaining = (flag ? 0.25 : 0.0);
		}
	}

	private void ResolvePendingDancerRelations()
	{
		if (!_hasPendingDancerRelations)
		{
			return;
		}
		if (!HasDancingRuntime)
		{
			_pendingDancerResolveRemaining = 0.25;
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		for (int i = 0; i < 4; i++)
		{
			int num = _pendingDancerSyncIds[i];
			if (num >= 0 && GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value) && GodotObject.IsInstanceValid(value))
			{
				SetNetworkDancer(i, value);
				continue;
			}
			string text = _pendingDancerNodeNames[i];
			if (!(text == "") && GodotObject.IsInstanceValid(node2D))
			{
				TowerDefenseCharacter nodeOrNull = node2D.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text));
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					SetNetworkDancer(i, nodeOrNull);
				}
			}
		}
		_pendingDancerResolveRemaining = (_hasPendingDancerRelations ? 0.25 : 0.0);
	}

	private void ReleaseDancerOwnership()
	{
		if (HasDancingRuntime)
		{
			for (int i = 0; i < 4; i++)
			{
				_dancingComponent.SetNetworkDancer(i, null);
			}
		}
		System.Array.Fill(_pendingDancerSyncIds, -1);
		System.Array.Fill(_pendingDancerNodeNames, "");
		RefreshPendingDancerBatchEligibility();
	}

	public override void _ExitTree()
	{
		if (IsQueuedForDeletion())
		{
			ReleaseDancerOwnership();
		}
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_dancingComponent = componentManager.GetRuntime<DancingComponent>();
			if (HasDancingComponent && !string.IsNullOrEmpty(dancerPacketName))
			{
				_dancingComponent.dancerPacketName = dancerPacketName;
			}
			RefreshPendingDancerBatchEligibility();
			if (_hasPendingDancerRelations)
			{
				ResolvePendingDancerRelations();
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_hasPendingDancerRelations)
		{
			_pendingDancerResolveRemaining -= delta;
			if (!(_pendingDancerResolveRemaining > 0.0))
			{
				ResolvePendingDancerRelations();
			}
		}
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		if (HasDancingComponent)
		{
			_dancingComponent.OnWalkEntered();
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (HasDancingComponent)
		{
			groundMoveComponent.SetAlive(_dancingComponent.CanWalk());
		}
		base.WalkProcessing(delta);
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		if (HasDancingComponent)
		{
			_dancingComponent.OnAttackProcessing(delta);
		}
	}

	public override void Walk()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else if (!HasDancingComponent || !_dancingComponent.OnWalk())
		{
			SendStateEvent("ToWalk");
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		if (HasDancingComponent)
		{
			_dancingComponent.OnDieProcessing();
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		ReleaseDancerOwnership();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Array<int> array = new Array<int>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			array.Add(GodotObject.IsInstanceValid(dancer) ? dancer.syncId : _pendingDancerSyncIds[i]);
		}
		return new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["dancerSyncIds"] = array
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (!data.ContainsKey("dancerSyncIds"))
		{
			return;
		}
		Godot.Collections.Array array = data["dancerSyncIds"].AsGodotArray();
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		for (int i = 0; i < 4; i++)
		{
			int num = ((i < array.Count) ? array[i].AsInt32() : (-1));
			if (num < 0)
			{
				SetNetworkDancer(i, null);
				continue;
			}
			if (GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value) && GodotObject.IsInstanceValid(value))
			{
				SetNetworkDancer(i, value);
				continue;
			}
			SetNetworkDancer(i, null);
			_pendingDancerSyncIds[i] = num;
			_pendingDancerNodeNames[i] = "";
		}
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override int GetNetworkSpecialStateRevision()
	{
		int num = 17;
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			int num2 = (GodotObject.IsInstanceValid(dancer) ? dancer.syncId : _pendingDancerSyncIds[i]);
			num = num * 31 + num2;
		}
		return num;
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		Array<string> array = new Array<string>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			array.Add(GodotObject.IsInstanceValid(dancer) ? dancer.Name.ToString() : _pendingDancerNodeNames[i]);
		}
		dictionary["dancerNodeNames"] = array;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		if (data.ContainsKey("dancerNodeNames"))
		{
			Godot.Collections.Array array = data["dancerNodeNames"].AsGodotArray();
			for (int i = 0; i < 4; i++)
			{
				string text = ((i < array.Count) ? array[i].AsString() : "");
				SetNetworkDancer(i, null);
				_pendingDancerNodeNames[i] = text;
			}
		}
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (!(damangePointName == "Arm"))
		{
			if (damangePointName == "Head")
			{
				base.sprite.SetAtlasReplace("Zombie_BackupGargantuar_body1.png", "uid://0f6e278qdym4");
			}
		}
		else
		{
			base.sprite.SetAtlasReplace("Zombie_BackupGargantuar_body1.png", "uid://c53wvxn6c14gs");
		}
	}
}
