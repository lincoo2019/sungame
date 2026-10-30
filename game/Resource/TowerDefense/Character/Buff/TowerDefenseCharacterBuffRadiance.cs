using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffRadiance : TowerDefenseCharacterBuffConfig
{
	private static PackedScene _LIGHT_FOG;

	private static PackedScene _LIGHT_AREA;

	private const string FlashEffectSceneUid = "uid://cxghtqumcubh4";

	private const string FlashEffectClip = "Fire";

	private const int FlashRange = 1;

	private const double FlashDizzinessTime = 5.0;

	private static PackedScene _flashEffectScene;

	[Export(PropertyHint.None, "")]
	public bool permanent = true;

	[Export(PropertyHint.None, "")]
	public double time = 30.0;

	[Export(PropertyHint.None, "")]
	public bool flashOnDeath = true;

	[Export(PropertyHint.None, "")]
	public bool radianceRemoveOnCampFlip = true;

	[Export(PropertyHint.None, "")]
	public bool deathFlashRequireSameCamp = true;

	[Export(PropertyHint.None, "")]
	public int baselineCamp = -1;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public Node fog;

	public Node light;

	private bool _gameplayStateApplied;

	private bool _flashPlayed;

	private bool _visualActive;

	private bool _baselineCaptured;

	private bool _originalHadLightPhysique;

	private static PackedScene LIGHT_FOG => _LIGHT_FOG ?? (_LIGHT_FOG = GD.Load<PackedScene>("uid://dsc2tkcmoc8l1"));

	private static PackedScene LIGHT_AREA => _LIGHT_AREA ?? (_LIGHT_AREA = GD.Load<PackedScene>("uid://byee3s263f1rj"));

	private static PackedScene FlashEffectScene => _flashEffectScene ?? (_flashEffectScene = GD.Load<PackedScene>("uid://cxghtqumcubh4"));

	public override void _Init()
	{
		key = "Radiance";
	}

	public override void Enter()
	{
		ShowVisuals();
		ApplyGameplayState();
		_visualActive = true;
	}

	public override void EnterReadOnlyClient()
	{
		ShowVisuals();
		_visualActive = true;
	}

	public override bool Step(double delta)
	{
		CaptureBaselineCampOnce();
		UpdateRadianceState(applyGameplayState: true);
		if (permanent)
		{
			return false;
		}
		currentTime += delta;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		CaptureBaselineCampOnce();
		UpdateRadianceState(applyGameplayState: false);
		if (!permanent)
		{
			currentTime += delta;
		}
	}

	private void CaptureBaselineCampOnce()
	{
		if (!_baselineCaptured && GodotObject.IsInstanceValid(character))
		{
			baselineCamp = (int)character.camp;
			_baselineCaptured = true;
		}
	}

	public override void Exit()
	{
		RestoreGameplayState();
		HideVisuals();
		_visualActive = false;
	}

	public override void ExitReadOnlyClient()
	{
		HideVisuals();
		_visualActive = false;
	}

	public override void Cancel()
	{
		RestoreGameplayState();
		HideVisuals();
		_visualActive = false;
	}

	public override void Destroy()
	{
		PlayDeathFlash();
		RestoreGameplayState();
		HideVisuals();
		_visualActive = false;
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffRadiance towerDefenseCharacterBuffRadiance)
		{
			permanent = permanent || towerDefenseCharacterBuffRadiance.permanent;
			time = Mathf.Max(time, towerDefenseCharacterBuffRadiance.time + GD.RandRange(-0.2, 0.2));
			flashOnDeath = flashOnDeath || towerDefenseCharacterBuffRadiance.flashOnDeath;
		}
		currentTime = 0.0;
	}

	private void UpdateRadianceState(bool applyGameplayState)
	{
		bool flag = ShouldShowRadiance();
		if (flag == _visualActive)
		{
			return;
		}
		if (flag)
		{
			ShowVisuals();
			if (applyGameplayState)
			{
				ApplyGameplayState();
			}
		}
		else
		{
			if (applyGameplayState)
			{
				RestoreGameplayState();
			}
			HideVisuals();
		}
		_visualActive = flag;
	}

	private bool ShouldShowRadiance()
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.instance))
		{
			return false;
		}
		if (radianceRemoveOnCampFlip && IsCampFlipped())
		{
			return false;
		}
		if (character.nearDie || character.die || character.instance.die)
		{
			return false;
		}
		return true;
	}

	private bool IsCampFlipped()
	{
		if (GodotObject.IsInstanceValid(character))
		{
			return character.camp != (TowerDefenseEnum.CHARACTER_CAMP)baselineCamp;
		}
		return false;
	}

	private void ShowVisuals()
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.spriteGroup))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(fog))
		{
			PackedScene lIGHT_FOG = LIGHT_FOG;
			if (GodotObject.IsInstanceValid(lIGHT_FOG))
			{
				fog = lIGHT_FOG.Instantiate(PackedScene.GenEditState.Disabled);
				character.spriteGroup.AddChild(fog, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		if (!GodotObject.IsInstanceValid(light))
		{
			PackedScene lIGHT_AREA = LIGHT_AREA;
			if (GodotObject.IsInstanceValid(lIGHT_AREA))
			{
				light = lIGHT_AREA.Instantiate(PackedScene.GenEditState.Disabled);
				character.spriteGroup.AddChild(light, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
	}

	private void HideVisuals()
	{
		Node node = fog;
		fog = null;
		if (GodotObject.IsInstanceValid(node))
		{
			node.QueueFree();
		}
		Node node2 = light;
		light = null;
		if (GodotObject.IsInstanceValid(node2))
		{
			node2.QueueFree();
		}
	}

	private void ApplyGameplayState()
	{
		if (!_gameplayStateApplied && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			int num = 64;
			_originalHadLightPhysique = (character.instance.physiqueTypeFlags & num) != 0;
			_gameplayStateApplied = true;
			character.instance.physiqueTypeFlags |= num;
		}
	}

	private void RestoreGameplayState()
	{
		if (!_gameplayStateApplied)
		{
			return;
		}
		_gameplayStateApplied = false;
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			int num = 64;
			if (_originalHadLightPhysique)
			{
				character.instance.physiqueTypeFlags |= num;
			}
			else
			{
				character.instance.physiqueTypeFlags &= ~num;
			}
		}
	}

	private void PlayDeathFlash()
	{
		if (flashOnDeath && !_flashPlayed)
		{
			_flashPlayed = true;
			if (GodotObject.IsInstanceValid(character) && (!deathFlashRequireSameCamp || !IsCampFlipped()))
			{
				Vector2I vector2I = ResolveFlashGridPos();
				PlayFlashEffect(vector2I);
				ApplyFlashDizziness(vector2I);
			}
		}
	}

	private Vector2I ResolveFlashGridPos()
	{
		Vector2I gridPos = character.gridPos;
		if (gridPos.X >= 0 && gridPos.Y >= 0)
		{
			return gridPos;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return new Vector2I(-1, -1);
		}
		return instance.GetMapGridPos(character.GetLogicalGlobalPosition());
	}

	private void PlayFlashEffect(Vector2I gridPos)
	{
		PackedScene flashEffectScene = FlashEffectScene;
		if (!GodotObject.IsInstanceValid(flashEffectScene))
		{
			return;
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(flashEffectScene, gridPos, "Fire");
		if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
		{
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			if (!GodotObject.IsInstanceValid(characterNode))
			{
				towerDefenseEffectSpriteOnce.QueueFree();
				return;
			}
			characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = character.GetLogicalGlobalPosition();
		}
	}

	private void ApplyFlashDizziness(Vector2I center)
	{
		if (center.X < 0 || center.Y < 0 || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return;
		}
		Vector2 mapCellPosCenter = instance.GetMapCellPosCenter(center);
		Vector2 size = instance.GetMapGridSize() * 3f;
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		instance.characterRegistry.FillCharactersIntersectingRectListExcludingCamp(AabbShapeUtil.RectFromCenter(mapCellPosCenter, size), character.camp, list);
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = list[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter != character && towerDefenseCharacter.camp != character.camp && !towerDefenseCharacter.die && !towerDefenseCharacter.isDestroy)
			{
				towerDefenseCharacter.BuffAdd(new TowerDefenseCharacterBuffDizziness
				{
					time = 5.0
				});
			}
		}
	}
}
