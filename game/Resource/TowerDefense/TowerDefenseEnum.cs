public class TowerDefenseEnum
{
	public enum GAMEMODE
	{
		TOWERDEFENSE,
		HAMMER
	}

	public enum LAYER_GROUNDITEM
	{
		DEFAULT = 0,
		GROUNDITEM = 1,
		PLANT_UNDER = 2,
		PLANT_BACK = 3,
		PLANT = 4,
		PLANT_FRONT = 5,
		PLANT_AIR = 6,
		ZOMBIE = 7,
		DAMAGEPART = 8,
		PROJECTILE = 9,
		EFFECT = 10,
		MAX = 15
	}

	public enum SUN_MOVING_METHOD
	{
		LAND,
		GRAVITY,
		MOVING
	}

	public enum DROP_ITEM_CATEGORY
	{
		NOONE = -1,
		SUN,
		COIN,
		SPECIAL
	}

	public enum PACKET_TYPE
	{
		NOONE = -1,
		WHITE,
		GOLD,
		DIAMOND,
		COLOUR,
		STAR,
		ORIGINAL,
		ZOMBIE,
		COVER,
		GRAY
	}

	public enum CHARACTER_CAMP
	{
		NOONE = -1,
		PLANT,
		ZOMBIE,
		ALL
	}

	public enum CHARACTER_HEIGHT
	{
		GROUND,
		LOW,
		NORMAL,
		TALL
	}

	public enum CHARACTER_COLLISION_FLAGS
	{
		GROUND_CHARACTRE = 1,
		OFF_GROUND_CHARACTRE = 2,
		DYING_CHARACTER = 4,
		GRIDITEM = 8,
		UNDER_GROUND = 0x10,
		UNDER_WATER = 0x20
	}

	public enum CHARACTER_BUFF_FLAGS
	{
		ICESPEEDDOWN = 1,
		FROZEN = 2,
		BUTTER = 4,
		HYPNOSES = 8,
		REDHEAT = 16,
		DIZZINESS = 32,
		POISONING = 64,
		GARLIC = 128,
		SLEEP = 256,
		BLOW = 512,
		CHERRY = 1024,
		SQUID = 2048,
		EMP = 4096,
		MAGICIMMOBILIZE = 8192,
		ALL = -1
	}

	public enum CHARACTER_PHYSIQUE_TYPE
	{
		NUT = 1,
		POT = 2,
		LILYPAD = 4,
		COFFEE = 8,
		SPIKE = 0x10,
		VASE = 0x20,
		LIGHT = 0x40,
		JALAPENO = 0x80,
		CAT = 0x100,
		NO_POT_REPLACE = 0x200,
		CANT_PENETRATE = 0x400,
		MACHINE = 0x800,
		MAGIC = 0x1000
	}

	public enum ZOMBIE_PHYSIQUE
	{
		NOONE,
		SMALL,
		NORMAL,
		MID,
		HUGE,
		CAR,
		BOSS
	}

	public enum ARMOR_METHOD_FLAGS
	{
		NOONE = 1,
		BODY = 2,
		SHIELD = 4,
		HELM = 8,
		METALLIC = 0x10,
		INVINCIBLE = 0x20,
		DAMAGEABLE = 0x40,
		DROPABLE = 0x80,
		PASSDAMAGE = 0x100,
		ABSORBOVERFLOW = 0x200,
		CANT_PENETRATE = 0x400,
		HEAD_COVER = 0x800,
		BURST_WHEEL = 0x1000,
		BLOCK_PROJECTILE_EFFECTS = 0x2000
	}

	public enum PLANTGRIDTYPE
	{
		ALL = -1,
		NOONE,
		SOIL,
		GROUND,
		WATER,
		AIR,
		LILYPAD,
		POT,
		SURROUND,
		GRAVESTONE,
		CRATER,
		BRICK,
		ICECAP,
		PLANT
	}

	public enum ELEMENT_SYSTEM
	{
		ICE = 1,
		FIRE = 2,
		DAY = 4,
		NIGHT = 8
	}

	public enum PROJECTILE_DAMAGE_FLAG
	{
		HITSHIELD = 1,
		HITBODY = 2,
		FIRE = 4,
		HITHEAD_COVER = 8,
		DIRECT_BODY = 0x10,
		SUPPRESS_DEATHRATTLES = 0x20,
		MAGIC = 0x40
	}

	public enum PROJECTILE_FIRE_METHOD_FLAG
	{
		SHOOTER = 1,
		CATAPULT = 2,
		PENETRATE = 4,
		BACK = 8,
		ROLLING = 0x10,
		TRACK = 0x20,
		IGNORE_SHIELD_ARMOR = 0x40
	}

	public enum EXPLOSION_DAMAGE_KIND : byte
	{
		OTHER,
		BOMB,
		JALA,
		MINE
	}

	public enum RANGE_TYPE
	{
		NOONE,
		AREA,
		ROW,
		ENEMY
	}

	public enum TARGET_NEAR_METHOD
	{
		DEFAULT,
		POSITION
	}

	public enum LEVEL_FINISH_METHOD
	{
		WAVE,
		VASE,
		IZM,
		QUIZ,
		IZM2,
		EMPTY
	}

	public enum LEVEL_REWARDTYPE
	{
		NOONE,
		PACKET,
		COLLECTABLE,
		COIN,
		TROPHY
	}

	public enum LEVEL_SEEDBANK_METHOD
	{
		NOONE,
		CHOOSE,
		PRESET,
		CONVEYOR,
		RAIN
	}

	public enum VASE_TYPE
	{
		NORMAL,
		PLANT,
		ZOMBIE
	}

	public const long CHARACTER_COLLISION_FLAGS_MAX = 4294967295L;
}
