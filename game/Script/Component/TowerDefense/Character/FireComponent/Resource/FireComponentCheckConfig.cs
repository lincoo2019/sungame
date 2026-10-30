using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class FireComponentCheckConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public FireComponentProjectileResource projectile;

	private bool _useParentCollision = true;

	[Export(PropertyHint.None, "")]
	public int collisionFlags;

	[Export(PropertyHint.None, "")]
	public bool useParentCollision
	{
		get
		{
			return _useParentCollision;
		}
		set
		{
			if (_useParentCollision != value)
			{
				_useParentCollision = value;
				NotifyPropertyListChanged();
			}
		}
	}

	public bool CanFire(FireComponent fireComponent)
	{
		if (GodotObject.IsInstanceValid(projectile) && fireComponent != null && !fireComponent.IsReleased)
		{
			return projectile.CanFire(fireComponent, GetCollisionFlags());
		}
		return false;
	}

	public TowerDefenseProjectileCreateData GetProjectile()
	{
		if (!GodotObject.IsInstanceValid(projectile))
		{
			return null;
		}
		return projectile.GetProjectile();
	}

	public TowerDefenseProjectileCreateData GetProjetile()
	{
		return GetProjectile();
	}

	public int GetCollisionFlags()
	{
		if (!useParentCollision)
		{
			return collisionFlags;
		}
		return -1;
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
		if (!useParentCollision)
		{
			array.Add(new Dictionary
			{
				{ "name", "Flag/Collision" },
				{ "type", 2 },
				{ "hint", 6 },
				{
					"hint_string",
					string.Join(",", Enum.GetNames<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>())
				},
				{ "usage", num }
			});
		}
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		if ((string?)property == "Flag/Collision")
		{
			collisionFlags = value.AsInt32();
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if ((string?)property == "Flag/Collision")
		{
			return Variant.From(in collisionFlags);
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if ((string?)property == "Flag/Collision")
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if ((string?)property == "Flag/Collision")
		{
			return Variant.From<int>(0);
		}
		return default(Variant);
	}
}
