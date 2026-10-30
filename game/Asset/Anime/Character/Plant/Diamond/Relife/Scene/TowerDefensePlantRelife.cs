using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantRelife : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			if (!inGame && !editorMapPreviewMode)
			{
				((RelifeSprite)base.sprite).back.ZIndex = 0;
			}
			if (base.currentCustom.Contains("Custom0"))
			{
				((RelifeSprite)base.sprite).back.SetFliter("Pumpkin_back", open: false);
				((RelifeSprite)base.sprite).back.SetFliter("skin1_4", open: true);
			}
			base.sprite.OnAnimeStarted += AnimeStarted;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			((RelifeSprite)base.sprite).back.SetFliter("Pumpkin_back", open: false);
			((RelifeSprite)base.sprite).back.SetFliter("skin1_4", open: true);
		}
		else
		{
			((RelifeSprite)base.sprite).back.SetFliter("Pumpkin_back", open: true);
			((RelifeSprite)base.sprite).back.SetFliter("skin1_4", open: false);
		}
	}

	public void AnimeStarted(string clip)
	{
		if (!(clip == _explodeComponent.explodeAnimeClips) || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character is TowerDefensePlant && !(character is TowerDefensePlantBowlingBase) && !character.instance.hologram && character.config.name != base.config.name && character.instance.hypnoses == instance.hypnoses)
			{
				character.instance.invincible = true;
				character.componentAlive = false;
				character.HitBoxDestroy();
				Tween tween = CreateTween();
				tween.SetEase(Tween.EaseType.InOut);
				tween.SetTrans(Tween.TransitionType.Sine);
				tween.TweenProperty(character.transformPoint, "scale", Vector2.Zero, 0.5);
			}
		}
	}

	public void Explode()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(cell.characterList))
		{
			if (item is TowerDefensePlant && !(item is TowerDefensePlantBowlingBase) && !item.instance.hologram && item.config.name != base.config.name && item.instance.hypnoses == instance.hypnoses)
			{
				item.Recycle(1.0);
			}
		}
	}
}
