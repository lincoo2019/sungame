using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class MagnetCoinComponent : CharacterComponentRuntime
{
	public delegate void CoinGetEventHandler(TowerDefenseCoinBase coin);

	public float timer;

	private readonly HashSet<int> _acceptedCoinObjectIds = new HashSet<int>();

	private int[] _acceptedCoinObjectIdArray = System.Array.Empty<int>();

	private NodePath _posMarkerPath = new NodePath();

	private bool _configured;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncTimerKey = new StringName("timer");

	private readonly Dictionary _syncPayload = new Dictionary();

	private bool _syncPayloadInitialized;

	private float _syncPayloadTimer;

	public Marker2D posMarker { get; private set; }

	public float magnetTime { get; set; } = 5f;

	public int magnetNum { get; set; } = -1;

	public bool useRegistryCoinTypes { get; set; } = true;

	public int[] objectList { get; private set; } = new int[6] { 0, 1, 2, 4, 5, 6 };

	public float pullSpeed { get; set; } = 5f;

	public float collectDistance { get; set; } = 10f;

	public Vector2 releaseVelocityMin { get; set; } = new Vector2(-50f, -200f);

	public Vector2 releaseVelocityMax { get; set; } = new Vector2(50f, -200f);

	public float releaseGravity { get; set; } = 980f;

	public string coinGroupName { get; set; } = "Coin";

	public string goldMagnetGroupName { get; set; } = "GoldMagnet";

	public TowerDefenseCharacter parent { get; private set; }

	public List<TowerDefenseCoinBase> coinList { get; } = new List<TowerDefenseCoinBase>();

	public List<int> _registryCoinObjectIds { get; } = new List<int>();

	private MagnetCoinComponentDefinition Definition => base.ComponentDefinition as MagnetCoinComponentDefinition;

	internal override bool WantsPhysicsProcess => true;

	public event CoinGetEventHandler OnCoinGet;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveSceneReferences();
		RefreshConfiguration();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ReleaseCapturedCoins();
		ClearSyncPayload();
		posMarker = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		ReleaseCapturedCoins();
		this.OnCoinGet = null;
		coinList.Clear();
		_registryCoinObjectIds.Clear();
		_acceptedCoinObjectIds.Clear();
		_acceptedCoinObjectIdArray = System.Array.Empty<int>();
		objectList = System.Array.Empty<int>();
		ClearSyncPayload();
		posMarker = null;
		parent = null;
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			MagnetCoinComponentDefinition definition = Definition;
			_posMarkerPath = definition?.posMarkerPath ?? new NodePath();
			magnetTime = definition?.magnetTime ?? 5f;
			magnetNum = definition?.magnetNum ?? (-1);
			useRegistryCoinTypes = definition?.useRegistryCoinTypes ?? true;
			int[] array = definition?.objectList;
			objectList = ((array != null) ? ((int[])array.Clone()) : new int[6] { 0, 1, 2, 4, 5, 6 });
			pullSpeed = definition?.pullSpeed ?? 5f;
			collectDistance = definition?.collectDistance ?? 10f;
			releaseVelocityMin = definition?.releaseVelocityMin ?? new Vector2(-50f, -200f);
			releaseVelocityMax = definition?.releaseVelocityMax ?? new Vector2(50f, -200f);
			releaseGravity = definition?.releaseGravity ?? 980f;
			coinGroupName = (string.IsNullOrWhiteSpace(definition?.coinGroupName) ? "Coin" : definition.coinGroupName);
			goldMagnetGroupName = (string.IsNullOrWhiteSpace(definition?.goldMagnetGroupName) ? "GoldMagnet" : definition.goldMagnetGroupName);
			_configured = true;
		}
	}

	private void ResolveSceneReferences()
	{
		posMarker = ((GodotObject.IsInstanceValid(parent) && !_posMarkerPath.IsEmpty) ? parent.GetNodeOrNull<Marker2D>(_posMarkerPath) : null);
	}

	public void RefreshConfiguration()
	{
		if (useRegistryCoinTypes)
		{
			RefreshRegistryCoinTypes();
		}
		RebuildAcceptedCoinIds();
	}

	public void RefreshRegistryCoinTypes()
	{
		_registryCoinObjectIds.Clear();
		foreach (DropItemConfig item in DropItemRegistry.GetByCategory(TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN))
		{
			if (item != null && item.CoinObjectId >= 0 && !_registryCoinObjectIds.Contains(item.CoinObjectId))
			{
				_registryCoinObjectIds.Add(item.CoinObjectId);
			}
		}
	}

	public void _RefreshRegistryCoinTypes()
	{
		RefreshRegistryCoinTypes();
		RebuildAcceptedCoinIds();
	}

	private void RebuildAcceptedCoinIds()
	{
		_acceptedCoinObjectIds.Clear();
		if (useRegistryCoinTypes && _registryCoinObjectIds.Count > 0)
		{
			for (int i = 0; i < _registryCoinObjectIds.Count; i++)
			{
				_acceptedCoinObjectIds.Add(_registryCoinObjectIds[i]);
			}
		}
		else
		{
			for (int j = 0; j < objectList.Length; j++)
			{
				_acceptedCoinObjectIds.Add(objectList[j]);
			}
		}
		_acceptedCoinObjectIdArray = new int[_acceptedCoinObjectIds.Count];
		_acceptedCoinObjectIds.CopyTo(_acceptedCoinObjectIdArray);
	}

	public int[] GetCoinObjectIds()
	{
		return _acceptedCoinObjectIdArray;
	}

	public int[] _GetCoinObjectIds()
	{
		return GetCoinObjectIds();
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (!IsOperational() || IsRemoteSyncedClient())
		{
			return;
		}
		float num = Math.Max(0f, (float)delta);
		timer = Mathf.Min(Math.Max(0f, magnetTime), timer + num);
		if (!GodotObject.IsInstanceValid(posMarker))
		{
			return;
		}
		float weight = Mathf.Clamp(pullSpeed * num, 0f, 1f);
		float num2 = Math.Max(0f, collectDistance);
		float num3 = num2 * num2;
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition(posMarker);
		for (int num4 = coinList.Count - 1; num4 >= 0; num4--)
		{
			TowerDefenseCoinBase towerDefenseCoinBase = coinList[num4];
			if (!GodotObject.IsInstanceValid(towerDefenseCoinBase))
			{
				coinList.RemoveAt(num4);
			}
			else
			{
				towerDefenseCoinBase.GlobalPosition = towerDefenseCoinBase.GlobalPosition.Lerp(logicalGlobalPosition, weight);
				if (!(towerDefenseCoinBase.GlobalPosition.DistanceSquaredTo(logicalGlobalPosition) > num3))
				{
					if (!string.IsNullOrEmpty(towerDefenseCoinBase.pickAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
					{
						AudioManager.Instance.AudioPlay(towerDefenseCoinBase.pickAudio);
					}
					towerDefenseCoinBase.isCollect = false;
					this.OnCoinGet?.Invoke(towerDefenseCoinBase);
					if (GodotObject.IsInstanceValid(towerDefenseCoinBase) && !towerDefenseCoinBase.isCollect)
					{
						towerDefenseCoinBase.Destroy();
					}
					coinList.RemoveAt(num4);
				}
			}
		}
	}

	private bool IsEligibleCoin(TowerDefenseCoinBase coin)
	{
		if (GodotObject.IsInstanceValid(coin) && _acceptedCoinObjectIds.Contains(coin.coinObjectId) && !coin.isCollect)
		{
			return coin.canMagnet;
		}
		return false;
	}

	public bool CanCoinDraw()
	{
		if (!IsOperational() || IsRemoteSyncedClient() || timer < Math.Max(0f, magnetTime))
		{
			return false;
		}
		foreach (Node item in parent.GetTree().GetNodesInGroup(coinGroupName))
		{
			if (item is TowerDefenseCoinBase coin && IsEligibleCoin(coin))
			{
				return true;
			}
		}
		return false;
	}

	public void CoinDraw()
	{
		if (!IsOperational() || IsRemoteSyncedClient())
		{
			return;
		}
		timer = 0f;
		int num = magnetNum;
		foreach (Node item in parent.GetTree().GetNodesInGroup(coinGroupName))
		{
			if (num == 0)
			{
				break;
			}
			if (item is TowerDefenseCoinBase towerDefenseCoinBase && IsEligibleCoin(towerDefenseCoinBase) && !coinList.Contains(towerDefenseCoinBase))
			{
				towerDefenseCoinBase.isCollect = true;
				towerDefenseCoinBase.RemoveFromGroup(coinGroupName);
				coinList.Add(towerDefenseCoinBase);
				if (num > 0)
				{
					num--;
				}
			}
		}
	}

	private void ReleaseCapturedCoins()
	{
		if (coinList.Count == 0)
		{
			return;
		}
		bool flag = GodotObject.IsInstanceValid(GameSaveManager.Instance) && GameSaveManager.Instance.GetFeatureValue("CoinCollect") > 0 && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && parent.GetTree().GetNodeCountInGroup(goldMagnetGroupName) <= 0;
		for (int num = coinList.Count - 1; num >= 0; num--)
		{
			TowerDefenseCoinBase towerDefenseCoinBase = coinList[num];
			if (GodotObject.IsInstanceValid(towerDefenseCoinBase))
			{
				towerDefenseCoinBase.isCollect = false;
				towerDefenseCoinBase.AddToGroup(coinGroupName);
				if (flag)
				{
					towerDefenseCoinBase.Collection();
				}
				else
				{
					towerDefenseCoinBase.over = false;
					if (GodotObject.IsInstanceValid(towerDefenseCoinBase.spriteNode))
					{
						towerDefenseCoinBase.spriteNode.Position = Vector2.Zero;
					}
					towerDefenseCoinBase.height = 0.0;
					if (GodotObject.IsInstanceValid(towerDefenseCoinBase.moveComponent))
					{
						towerDefenseCoinBase.moveComponent.SetVelocity(new Vector2((float)GD.RandRange(releaseVelocityMin.X, releaseVelocityMax.X), (float)GD.RandRange(releaseVelocityMin.Y, releaseVelocityMax.Y)));
						towerDefenseCoinBase.moveComponent.SetGravity(releaseGravity);
					}
				}
			}
		}
		coinList.Clear();
	}

	private bool IsOperational()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			return parent.IsInsideTree();
		}
		return false;
	}

	private bool IsRemoteSyncedClient()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(parent))
		{
			return parent.syncId >= 0;
		}
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { { "timer", timer } };
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		timer = Mathf.Clamp(data.GetValueOrDefault("timer", 0f).AsSingle(), 0f, Math.Max(0f, magnetTime));
	}

	public override Dictionary SyncSerialize()
	{
		float num = timer;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 2)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 1 && _syncPayloadTimer == num)
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		_syncPayload[SyncTimerKey] = num;
		_syncPayloadTimer = num;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		_syncPayload.Clear();
		_syncPayloadInitialized = false;
		_syncPayloadTimer = 0f;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		timer = Mathf.Clamp(data.GetValueOrDefault("timer", timer).AsSingle(), 0f, Math.Max(0f, magnetTime));
	}
}
