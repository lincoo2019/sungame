using System.Collections.Generic;
using Godot;

public partial class TrioSeaweed : Node2D
{
	private TowerDefenseZombie _zombie;

	private readonly List<Node2D> _anchors = new List<Node2D>();

	private readonly List<Sprite2D> _blades = new List<Sprite2D>();

	private readonly string[] _layers = new string[3] { "anim_head1", "Zombie_outerarm_upper", "Zombie_duckytube" };

	private readonly Vector2[] _offsets = new Vector2[3]
	{
		new Vector2(30f, 20f),
		new Vector2(5f, 5f),
		new Vector2(77f, 20f)
	};

	private ShaderMaterial _material;

	private double _age;

	public static void Attach(TowerDefenseZombie zombie)
	{
		if (!zombie.HasNode("TrioSeaweed"))
		{
			zombie.AddChild(new TrioSeaweed
			{
				Name = "TrioSeaweed",
				_zombie = zombie
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override void _Ready()
	{
		base.ZIndex = 5;
		base.ProcessPhysicsPriority = 10002;
		_material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://Registry/Battle/Feature/Wave/TrioAmbush/TrioSeaweed.gdshader")
		};
		Texture2D texture = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Zombie/ZombieSeaweed.png");
		for (int i = 0; i < 3; i++)
		{
			Node2D node2D = new Node2D
			{
				Name = "Attachment" + i
			};
			AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			_anchors.Add(node2D);
			for (int j = 0; j < 2; j++)
			{
				Sprite2D sprite2D = new Sprite2D
				{
					Texture = texture,
					Hframes = 4,
					Frame = (i + j) % 4,
					Material = _material,
					Scale = Vector2.One * ((j == 0) ? 0.65f : 0.45f),
					Position = new Vector2(j * 12 - 6, j * 5),
					FlipH = ((i + j) % 2 == 0)
				};
				node2D.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
				_blades.Add(sprite2D);
			}
		}
		UpdateAttachments();
	}

	private string HeadLayer()
	{
		foreach (TowerDefenseArmorInstance armor in _zombie.instance.armorList)
		{
			if (!armor.isRemove)
			{
				if (armor.slotConfig.armorName == "Cone")
				{
					return "anim_cone";
				}
				if (armor.slotConfig.armorName == "Bucket")
				{
					return "anim_bucket";
				}
			}
		}
		return "anim_head1";
	}

	public override void _Process(double delta)
	{
		if (GodotObject.IsInstanceValid(_zombie) && _zombie.IsNodeReady())
		{
			_age += delta;
			UpdateAttachments();
		}
	}

	private void UpdateAttachments()
	{
		_layers[0] = HeadLayer();
		for (int i = 0; i < _anchors.Count; i++)
		{
			Node2D node2D = _anchors[i];
			int slotLayerId = _zombie.sprite.flashAnimeData.layerDictionary[_layers[i]].AsInt32();
			int mediaId;
			Transform2D transform;
			bool flag = _zombie.sprite.TryGetInterpolatedSlotPose(slotLayerId, out mediaId, out transform);
			node2D.Visible = flag && mediaId >= 0 && !_zombie.invisible && !_zombie.die && !_zombie.nearDie && (i != 1 || !(_zombie is TowerDefenseZombieNormal towerDefenseZombieNormal) || !towerDefenseZombieNormal.halfHp);
			if (flag)
			{
				transform = transform.Translated(_zombie.sprite.offset).TranslatedLocal(_offsets[i]);
				node2D.GlobalTransform = _zombie.GetLogicalGlobalTransform(_zombie.sprite) * transform;
			}
		}
		for (int j = 0; j < _blades.Count; j++)
		{
			_blades[j].Rotation = Mathf.Sin((float)_age * 2f + (float)j) * 0.05f;
		}
		_material.SetShaderParameter("surface_y", _zombie.inWater ? (_zombie.GlobalPosition.Y + 36f) : 100000f);
	}
}
