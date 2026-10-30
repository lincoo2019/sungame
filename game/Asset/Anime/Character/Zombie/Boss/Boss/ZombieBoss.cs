using Godot;
using Godot.Collections;

[Tool]
public partial class ZombieBoss : AdobeAnimateSpriteBase
{
	private static readonly Dictionary _EFFECT_FLAGS = new Dictionary
	{
		["ash"] = 1,
		["iceSpeedDown"] = 2,
		["cover"] = 4,
		["hypnoses"] = 8,
		["imitater"] = 16,
		["redHeat"] = 32,
		["puzzle"] = 64,
		["poisoning"] = 128,
		["blink"] = 256
	};

	private const string ZOMBIE_BOSS_FOOT = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_foot.png";

	private const string ZOMBIE_BOSS_FOOT_DAMAGE1 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_foot_damage1.png";

	private const string ZOMBIE_BOSS_FOOT_DAMAGE2 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_foot_damage2.png";

	private const string ZOMBIE_BOSS_HEAD_DAMAGE1 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_head_damage1.png";

	private const string ZOMBIE_BOSS_HEAD_DAMAGE2 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_head_damage2.png";

	private const string ZOMBIE_BOSS_JAW = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_jaw.png";

	private const string ZOMBIE_BOSS_JAW_DAMAGE1 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_jaw_damage1.png";

	private const string ZOMBIE_BOSS_JAW_DAMAGE2 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_jaw_damage2.png";

	private const string ZOMBIE_BOSS_OUTERARM_HAND_DAMAGE1 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_outerarm_hand_damage1.png";

	private const string ZOMBIE_BOSS_OUTERARM_HAND_DAMAGE2 = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_outerarm_hand_damage2.png";

	private const string ZOMBIE_BOSS_MOUTHGLOW_BLUE = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_mouthglow_blue.png";

	private const string ZOMBIE_BOSS_MOUTHGLOW_RED = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_mouthglow_red.png";

	private const string ZOMBIE_BOSS_EYEGLOW_BLUE = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_eyeglow_blue.png";

	private const string ZOMBIE_BOSS_EYEGLOW_RED = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_eyeglow_red.png";

	private AdobeAnimateSpriteBase _driver;

	private AdobeAnimateSpriteBase _arm;

	private Node2D _armNode;

	private AdobeAnimateSpriteBase _rv;

	private Node2D _rvNode;

	private AdobeAnimatePart _headSprite;

	private GpuParticles2D _headSmoke1;

	private GpuParticles2D _headSmoke2;

	private GpuParticles2D _headSmoke3;

	private Marker2D _spawnMarker;

	private Marker2D _ballSpawnMarker;

	public Tween armTween;

	public Tween headTween;

	private void SetEffectFlag(AdobeAnimateSpriteBase _sprite, string effect, bool enable)
	{
		if (GodotObject.IsInstanceValid(_sprite) && _EFFECT_FLAGS.ContainsKey(effect))
		{
			_sprite.Modulate = (enable ? new Color(1.35f, 1.35f, 1.35f, _sprite.Modulate.A) : Colors.White);
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_driver = GetNode<AdobeAnimateSpriteBase>("%Driver");
			_arm = GetNode<AdobeAnimateSpriteBase>("%Arm");
			_armNode = GetNode<Node2D>("%ArmNode");
			_rv = GetNode<AdobeAnimateSpriteBase>("%RV");
			_rvNode = GetNode<Node2D>("%RVNode");
			_headSprite = GetNode<AdobeAnimatePart>("%HeadSprite");
			_headSmoke1 = GetNode<GpuParticles2D>("%HeadSmoke1");
			_headSmoke2 = GetNode<GpuParticles2D>("%HeadSmoke2");
			_headSmoke3 = GetNode<GpuParticles2D>("%HeadSmoke3");
			_spawnMarker = GetNode<Marker2D>("%SpawnMarker");
			_ballSpawnMarker = GetNode<Marker2D>("%BallSpawnMarker");
			base.OnAnimeCompleted += AnimeCompleted;
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (GodotObject.IsInstanceValid(_arm))
		{
			_arm.frameIndex = frameIndex;
			_arm.elapsedTimer = elapsedTimer;
		}
		if (GodotObject.IsInstanceValid(_rv))
		{
			_rv.frameIndex = frameIndex;
			_rv.elapsedTimer = elapsedTimer;
		}
		if (GodotObject.IsInstanceValid(_driver))
		{
			_driver.pause = base.pause;
		}
	}

	public override void SetClip(string clipName)
	{
		base.SetClip(clipName);
		if (GodotObject.IsInstanceValid(_headSprite))
		{
			AdobeAnimateManagedSprite2D.SetLogicalVisible(_headSprite, clipName != "Death");
		}
		if (GodotObject.IsInstanceValid(_arm))
		{
			_arm.loop = loop;
			_arm.SetClip(clipName);
		}
		if (GodotObject.IsInstanceValid(_rv))
		{
			_rv.loop = loop;
			_rv.SetClip(clipName);
		}
	}

	public void SetSpawn(int line = 1, double time = 0.5)
	{
		double num = TowerDefenseManager.Instance.GetMapGridSize().Y * ((float)line - (Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) - 1f));
		SetArmOffset((float)num, time);
	}

	public void SetHeadAttack(int line = 4, double time = 1.0)
	{
		double num = TowerDefenseManager.Instance.GetMapGridSize().Y * ((float)line - (Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 2f));
		SetHeadOffset((float)num, time);
		SetSpawn(line - ((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 1), time);
	}

	public void SetRVPos(Vector2I gridPos)
	{
		Vector2 vector = TowerDefenseManager.Instance.GetMapGridSize() * (new Vector2(gridPos.X, gridPos.Y) - new Vector2(1f, 2f)) - new Vector2(0f, 100f);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		float y = 0f;
		if (GodotObject.IsInstanceValid(mapCell))
		{
			y = (float)mapCell.GetGroundHeight(0.0);
		}
		SetRVOffset(vector - new Vector2(0f, y));
	}

	public void SetHeadAttackBall(bool isFire)
	{
		if (isFire)
		{
			SetAtlasReplace("Zombie_boss_mouthglow_red.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_mouthglow_red.png");
			SetAtlasReplace("Zombie_boss_eyeglow_red.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_eyeglow_red.png");
		}
		else
		{
			SetAtlasReplace("Zombie_boss_mouthglow_red.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_mouthglow_blue.png");
			SetAtlasReplace("Zombie_boss_eyeglow_red.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_eyeglow_blue.png");
		}
	}

	public void DamagePointSet(string damagePointName)
	{
		switch (damagePointName)
		{
		case "Stage1":
			SetAtlasReplace("Zombie_boss_head.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_head_damage1.png");
			SetAtlasReplace("Zombie_boss_jaw.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_jaw_damage1.png");
			SetAtlasReplace("Zombie_boss_foot_inner.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_foot_damage1.png");
			if (GodotObject.IsInstanceValid(_arm))
			{
				_arm.SetAtlasReplace("Zombie_boss_outerarm_hand.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_outerarm_hand_damage1.png");
			}
			if (GodotObject.IsInstanceValid(_headSprite))
			{
				_headSprite.externalAtlasTexturePath = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_head_damage1.png";
			}
			break;
		case "Stage2":
			SetAtlasReplace("Zombie_boss_head.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_head_damage2.png");
			SetAtlasReplace("Zombie_boss_jaw.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_jaw_damage2.png");
			SetAtlasReplace("Zombie_boss_foot.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_foot_damage2.png");
			if (GodotObject.IsInstanceValid(_arm))
			{
				_arm.SetAtlasReplace("Zombie_boss_outerarm_hand.png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_outerarm_hand_damage2.png");
			}
			if (GodotObject.IsInstanceValid(_headSprite))
			{
				_headSprite.externalAtlasTexturePath = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Zombie/Boss/Boss/DamagePoint/Zombie_boss_head_damage2.png";
			}
			if (GodotObject.IsInstanceValid(_headSmoke1))
			{
				_headSmoke1.Visible = true;
			}
			if (GodotObject.IsInstanceValid(_headSmoke2))
			{
				_headSmoke2.Visible = true;
			}
			break;
		case "Stage3":
			SetEffectFlag(this, "blink", enable: true);
			SetEffectFlag(_arm, "blink", enable: true);
			if (GodotObject.IsInstanceValid(_headSprite))
			{
				_headSprite.Modulate = new Color(1.35f, 1.35f, 1.35f, _headSprite.Modulate.A);
			}
			if (GodotObject.IsInstanceValid(_headSmoke3))
			{
				_headSmoke3.Visible = true;
			}
			break;
		case "Death":
			if (GodotObject.IsInstanceValid(_driver))
			{
				_driver.timeScale = 1.5;
				_driver.SetAnimation("Death", loop: false, 0.2);
				_driver.AddAnimation("Flag", 0.0, loop: false);
				_driver.AddAnimation("FlagLoop", 0.0);
			}
			SetEffectFlag(this, "blink", enable: false);
			SetEffectFlag(_arm, "blink", enable: false);
			if (GodotObject.IsInstanceValid(_headSprite))
			{
				_headSprite.Modulate = Colors.White;
			}
			if (GodotObject.IsInstanceValid(_headSmoke1))
			{
				_headSmoke1.Visible = false;
			}
			if (GodotObject.IsInstanceValid(_headSmoke2))
			{
				_headSmoke2.Visible = false;
			}
			if (GodotObject.IsInstanceValid(_headSmoke3))
			{
				_headSmoke3.Visible = false;
			}
			break;
		}
	}

	public void SetArmOffset(float offset, double time = 0.5)
	{
		if (GodotObject.IsInstanceValid(armTween) && armTween.IsRunning())
		{
			armTween.Kill();
		}
		armTween = CreateTween();
		armTween.SetEase(Tween.EaseType.Out);
		armTween.SetTrans(Tween.TransitionType.Linear);
		armTween.TweenProperty(_armNode, "position:y", offset, time);
	}

	public void SetHeadOffset(float offset, double time = 1.0)
	{
		if (GodotObject.IsInstanceValid(headTween) && headTween.IsRunning())
		{
			headTween.Kill();
		}
		Vector2 vector = new Vector2(base.offset.X, -300f + offset);
		headTween = CreateTween();
		headTween.SetEase(Tween.EaseType.Out);
		headTween.SetTrans(Tween.TransitionType.Linear);
		headTween.TweenProperty(this, "offset", vector, time);
	}

	public void SetRVOffset(Vector2 offset, double time = 0.2)
	{
		if (GodotObject.IsInstanceValid(headTween) && headTween.IsRunning())
		{
			headTween.Kill();
		}
		headTween = CreateTween();
		headTween.SetEase(Tween.EaseType.Out);
		headTween.SetTrans(Tween.TransitionType.Linear);
		headTween.TweenProperty(_rvNode, "position", offset, time);
	}

	public void SetRVVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(_rvNode))
		{
			_rvNode.Visible = visible;
		}
		SetFliters((Array?)new Array<string> { "Boss_RV", "Boss_RV_wheel1", "Boss_RV_wheel2" }, !visible);
	}

	public Vector2 GetSpawnMarkerGlobalPos(TowerDefenseCharacter owner)
	{
		if (!GodotObject.IsInstanceValid(_spawnMarker) || !GodotObject.IsInstanceValid(owner))
		{
			return Vector2.Zero;
		}
		return owner.GetLogicalGlobalPosition(_spawnMarker);
	}

	public Vector2 GetBallSpawnMarkerGlobalPos(TowerDefenseCharacter owner)
	{
		if (!GodotObject.IsInstanceValid(_ballSpawnMarker) || !GodotObject.IsInstanceValid(owner))
		{
			return Vector2.Zero;
		}
		UpdateChild();
		return owner.GetLogicalGlobalPosition(_ballSpawnMarker);
	}

	public void AnimeCompleted(string _clip)
	{
		if (!(_clip == "Spawn1"))
		{
			if (_clip == "RV")
			{
				SetRVVisible(visible: false);
				if (GodotObject.IsInstanceValid(_rvNode))
				{
					_rvNode.Position = Vector2.Zero;
				}
			}
		}
		else
		{
			SetSpawn();
		}
	}
}
