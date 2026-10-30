using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseVaseSquashBlack : TowerDefenseVase
{
	public List<TowerDefenseCharacter> CharacterList = new List<TowerDefenseCharacter>();

	private readonly List<int> _pendingCaptureSyncIds = new List<int>();

	private bool _captureReleased;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			base._Ready();
			instance.invincibleSmash = true;
			_InitCharacters();
		}
	}

	private async void _InitCharacters()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		ApplyCapturedCharacters();
	}

	private void ApplyCapturedCharacters()
	{
		for (int num = CharacterList.Count - 1; num >= 0; num--)
		{
			TowerDefenseCharacter towerDefenseCharacter = CharacterList[num];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || towerDefenseCharacter == this)
			{
				CharacterList.RemoveAt(num);
			}
			else
			{
				ApplyCapturedCharacter(towerDefenseCharacter);
			}
		}
	}

	private void ApplyCapturedCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && character != this)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			Vector2 logicalGlobalPosition2 = character.GetLogicalGlobalPosition();
			character.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X, logicalGlobalPosition2.Y));
			TargetRegistrationComponent obj = character.targetRegistrationComponent;
			if (obj != null && !obj.IsReleased)
			{
				character.targetRegistrationComponent.canProjectileCheck = false;
			}
			character.ProcessMode = ProcessModeEnum.Disabled;
			character.Visible = false;
			character.shadowComponent?.SetShadowVisible(visible: false);
			if (character is TowerDefenseZombie)
			{
				character.RemoveFromGroup("Zombie");
			}
			if (character is TowerDefensePlant)
			{
				character.RemoveFromGroup("Plant");
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		ResolvePendingNetworkCaptures();
	}

	private void ResolvePendingNetworkCaptures()
	{
		if (_captureReleased || _pendingCaptureSyncIds.Count == 0)
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return;
		}
		for (int num = _pendingCaptureSyncIds.Count - 1; num >= 0; num--)
		{
			int key = _pendingCaptureSyncIds[num];
			if (currentControl._syncCharacters.TryGetValue(key, out var value) && GodotObject.IsInstanceValid(value) && value != this)
			{
				if (!CharacterList.Contains(value))
				{
					CharacterList.Add(value);
				}
				ApplyCapturedCharacter(value);
				_pendingCaptureSyncIds.RemoveAt(num);
			}
		}
	}

	private void ReleaseCapturedCharacters()
	{
		_captureReleased = true;
		_pendingCaptureSyncIds.Clear();
		foreach (TowerDefenseCharacter character in CharacterList)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				character.ProcessMode = ProcessModeEnum.Inherit;
				character.Visible = true;
				character.shadowComponent?.SetShadowVisible(!character.inWater);
				TargetRegistrationComponent obj = character.targetRegistrationComponent;
				if (obj != null && !obj.IsReleased)
				{
					character.targetRegistrationComponent.canProjectileCheck = true;
				}
				if (character is TowerDefenseZombie towerDefenseZombie)
				{
					character.AddToGroup("Zombie");
					towerDefenseZombie.WalkReady();
				}
				if (character is TowerDefensePlant)
				{
					character.AddToGroup("Plant");
				}
			}
		}
		CharacterList.Clear();
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			ReleaseCapturedCharacters();
			AudioManager.Instance.AudioPlay("VaseBreaking");
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(base.chunkParticles, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Array<string> array = new Array<string>();
		foreach (TowerDefenseCharacter character in CharacterList)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				array.Add(character.Name);
			}
		}
		return new Dictionary
		{
			["capturedCharacterNodeNames"] = array,
			["captureReleased"] = _captureReleased
		};
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		CharacterList.Clear();
		_captureReleased = data.GetValueOrDefault("captureReleased", false).AsBool();
		if (_captureReleased || !data.ContainsKey("capturedCharacterNodeNames"))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		foreach (Variant item in data["capturedCharacterNodeNames"].AsGodotArray())
		{
			string text = item.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				TowerDefenseCharacter nodeOrNull = node2D.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text));
				if (GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull != this && !CharacterList.Contains(nodeOrNull))
				{
					CharacterList.Add(nodeOrNull);
				}
			}
		}
		ApplyCapturedCharacters();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Array<int> array = new Array<int>();
		foreach (TowerDefenseCharacter character in CharacterList)
		{
			if (GodotObject.IsInstanceValid(character) && character.syncId >= 0)
			{
				array.Add(character.syncId);
			}
		}
		return new Dictionary
		{
			["capturedSyncIds"] = array,
			["captureReleased"] = _captureReleased
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (data.GetValueOrDefault("captureReleased", false).AsBool())
		{
			ReleaseCapturedCharacters();
			return;
		}
		_captureReleased = false;
		_pendingCaptureSyncIds.Clear();
		foreach (Variant item in data.GetValueOrDefault("capturedSyncIds", new Array<int>()).AsGodotArray())
		{
			int num = item.AsInt32();
			if (num >= 0 && !_pendingCaptureSyncIds.Contains(num))
			{
				_pendingCaptureSyncIds.Add(num);
			}
		}
		ResolvePendingNetworkCaptures();
	}
}
