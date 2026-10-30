using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantHypnoShroomLight : TowerDefensePlant
{
	private const string IdleClip = "Idle";

	private const string ShootingClip = "Shooting";

	private const string AttackEvent = "attack";

	private const string CrystalLayerPrefix = "light";

	public const int MaxFlashNum = 3;

	[Export(PropertyHint.None, "")]
	public string flashProjectileName = "MagicFlashBall";

	[Export(PropertyHint.None, "")]
	public double flashSpeed = 300.0;

	[Export(PropertyHint.None, "")]
	public int flashNum;

	private int _crystalOffNum;

	private bool _shotPending;

	private bool _placedAnnounced;

	private bool _eaten;

	private CharacterAabbAreaComponent _fogArea;

	private PointLight2D _light;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_fogArea = componentManager.GetRuntime<CharacterAabbAreaComponent>("character.aabb_area.fog");
			_light = GetNodeOrNull<PointLight2D>("%Light");
			RefreshCrystalLights();
			if (inGame)
			{
				Callable.From(AnnouncePlanted).CallDeferred();
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_fogArea = null;
		_light = null;
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && inGame)
		{
			bool flag = !IsSleep() && !die && !isDestroy;
			CharacterAabbAreaComponent fogArea = _fogArea;
			if (fogArea != null && !fogArea.IsReleased)
			{
				_fogArea.SetEnabled(flag);
			}
			if (GodotObject.IsInstanceValid(_light))
			{
				_light.Visible = flag && TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
		}
	}

	private void AnnouncePlanted()
	{
		if (_placedAnnounced || !GodotObject.IsInstanceValid(this) || isDestroy || !IsInsideTree())
		{
			return;
		}
		_placedAnnounced = true;
		if (!GodotObject.IsInstanceValid(base.config))
		{
			return;
		}
		TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP = base.camp;
		Array<Node> nodesInGroup = GetTree().GetNodesInGroup(base.config.name);
		for (int i = 0; i < nodesInGroup.Count; i++)
		{
			if (nodesInGroup[i] is TowerDefensePlantHypnoShroomLight towerDefensePlantHypnoShroomLight && GodotObject.IsInstanceValid(towerDefensePlantHypnoShroomLight) && !towerDefensePlantHypnoShroomLight.isDestroy && !towerDefensePlantHypnoShroomLight.die && towerDefensePlantHypnoShroomLight.camp == cHARACTER_CAMP)
			{
				towerDefensePlantHypnoShroomLight.TryFireFlash();
			}
		}
	}

	public void TryFireFlash()
	{
		if (CanFireFlash())
		{
			flashNum++;
			if (!(_shotPending = GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("Shooting")))
			{
				FireFlashBall();
				HideNextCrystal();
			}
			else
			{
				base.sprite.SetAnimation("Shooting", loop: false);
			}
		}
	}

	private bool CanFireFlash()
	{
		if (!Engine.IsEditorHint() && inGame && !die && !isDestroy && !IsSleep())
		{
			return flashNum < 3;
		}
		return false;
	}

	private void FireFlashBall()
	{
		Vector2 vector = ((base.Scale.X < 0f) ? Vector2.Left : Vector2.Right);
		Vector2 pos = GetLogicalGlobalPosition() + new Vector2(vector.X * 20f, -30f);
		FireComponent.CreateProjectilePositionByName(this, null, 0.0, pos, vector * (float)flashSpeed, flashProjectileName, -1, base.camp);
	}

	private void HideNextCrystal()
	{
		if (_crystalOffNum < 3 && _crystalOffNum < flashNum)
		{
			_crystalOffNum++;
			SetCrystalLayerVisible(_crystalOffNum, visible: false);
		}
	}

	private void SetCrystalLayerVisible(int index, bool visible)
	{
		if (index < 1 || index > 3 || !GodotObject.IsInstanceValid(base.sprite))
		{
			return;
		}
		string text = "light" + index;
		base.sprite.Set("Animation/LayerVisible/" + text, visible);
		AdobeAnimateData flashAnimeData = base.sprite.flashAnimeData;
		if (flashAnimeData?.layerDictionary != null && flashAnimeData.layerDictionary.ContainsKey(text))
		{
			int num = (int)flashAnimeData.layerDictionary[text];
			Array<bool> layerVisibleForInternalRead = base.sprite.GetLayerVisibleForInternalRead();
			if (layerVisibleForInternalRead != null && num >= 0 && num < layerVisibleForInternalRead.Count && layerVisibleForInternalRead[num] != visible)
			{
				layerVisibleForInternalRead[num] = visible;
			}
		}
		base.sprite.QueueRedraw();
	}

	private void RefreshCrystalLights()
	{
		for (int i = 1; i <= 3; i++)
		{
			SetCrystalLayerVisible(i, i > _crystalOffNum);
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!string.IsNullOrEmpty(command) && string.Equals(command, "attack", StringComparison.OrdinalIgnoreCase))
		{
			HideNextCrystal();
			FlushPendingFlash();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (string.IsNullOrEmpty(clip) || !(clip != "Shooting"))
		{
			HideNextCrystal();
			FlushPendingFlash();
			if (clip == "Shooting")
			{
				RestoreIdleAnime();
			}
		}
	}

	private void RestoreIdleAnime()
	{
		if (!Engine.IsEditorHint() && inGame && !die && !isDestroy && !IsSleep() && GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("Idle"))
		{
			base.sprite.timeScale = timeScale;
			base.sprite.SetAnimation("Idle", loop: true, 0.2);
		}
	}

	private void FlushPendingFlash()
	{
		if (_shotPending)
		{
			_shotPending = false;
			FireFlashBall();
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (!instance.sleep && !_eaten && GodotObject.IsInstanceValid(character))
		{
			if ((character.instance.unUseBuffFlags & 8) != 0)
			{
				SkipInvincibleHurt(num);
			}
			else if (!(type != "Eat"))
			{
				_eaten = true;
				character.Hypnoses();
				character.BuffAdd(new TowerDefenseCharacterBuffRadiance());
				Destroy();
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["flashNum"] = flashNum,
			["crystalOffNum"] = _crystalOffNum,
			["placedAnnounced"] = _placedAnnounced,
			["eaten"] = _eaten
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		flashNum = (data.ContainsKey("flashNum") ? data["flashNum"].AsInt32() : 0);
		_crystalOffNum = (data.ContainsKey("crystalOffNum") ? data["crystalOffNum"].AsInt32() : 0);
		_placedAnnounced = data.ContainsKey("placedAnnounced") && data["placedAnnounced"].AsBool();
		_eaten = data.ContainsKey("eaten") && data["eaten"].AsBool();
		RefreshCrystalLights();
	}
}
