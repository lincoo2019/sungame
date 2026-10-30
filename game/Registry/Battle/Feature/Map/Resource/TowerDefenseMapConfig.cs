using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseMapConfig : Resource
{
	public const int MaxGridDimension = 50;

	[Export(PropertyHint.None, "")]
	public string translate = "";

	[Export(PropertyHint.None, "")]
	public string dayNightSwitching = "";

	[Export(PropertyHint.None, "")]
	public string mapTexturePath = "";

	[Export(PropertyHint.None, "")]
	public string mapScenePath = "";

	private Texture2D _mapTexture;

	private Texture2D _mapThumbnail;

	private string _mapThumbnailKey = "";

	private PackedScene _mapScene;

	private bool _specialRuleRuntimeCacheValid;

	private double _cachedAttackDpsLifestealRatio;

	private readonly double[] _cachedZombieColumnSpeedMultipliers = new double[51];

	[Export(PropertyHint.None, "")]
	public Vector2 mapSize = new Vector2(1400f, 600f);

	[Export(PropertyHint.None, "")]
	public Vector2 mapOffset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public double plantOffset = 50.0;

	[Export(PropertyHint.None, "")]
	public Vector2I gridNum = new Vector2I(9, 5);

	[Export(PropertyHint.None, "")]
	public Vector2 gridBeginPos = new Vector2(256f, 45f);

	[Export(PropertyHint.None, "")]
	public Vector2 gridSize = new Vector2(80f, 98f);

	[Export(PropertyHint.None, "")]
	public Vector4 edge = new Vector4(200f, 0f, 1100f, 576f);

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCellConfig> cellConfig = new Array<TowerDefenseCellConfig>();

	[Export(PropertyHint.None, "")]
	public Array<int> lineUse = new Array<int>();

	[Export(PropertyHint.None, "")]
	public bool isNight;

	[Export(PropertyHint.None, "")]
	public bool useSunFall = true;

	[Export(PropertyHint.None, "")]
	public bool enableBattleZoom;

	[Export(PropertyHint.None, "")]
	public int maximumFps = -1;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseMapRuleConfig> specialRules = new Array<TowerDefenseMapRuleConfig>();

	public Texture2D mapTexture
	{
		get
		{
			return GetMapTexture();
		}
		set
		{
			_mapTexture = value;
			mapTexturePath = GetResourcePath(value);
		}
	}

	public PackedScene mapScene
	{
		get
		{
			return GetMapScene();
		}
		set
		{
			_mapScene = value;
			mapScenePath = GetResourcePath(value);
		}
	}

	public bool HasSpecialRule(StringName ruleId)
	{
		if (ruleId.IsEmpty || specialRules == null)
		{
			return false;
		}
		foreach (TowerDefenseMapRuleConfig specialRule in specialRules)
		{
			if (GodotObject.IsInstanceValid(specialRule) && specialRule.id == ruleId)
			{
				return true;
			}
		}
		return false;
	}

	public bool SpecialRulesPreventSleep()
	{
		if (specialRules == null)
		{
			return false;
		}
		foreach (TowerDefenseMapRuleConfig specialRule in specialRules)
		{
			if (GodotObject.IsInstanceValid(specialRule) && specialRule.preventsSleep)
			{
				return true;
			}
		}
		return false;
	}

	public double ApplyPacketCooldownRules(TowerDefenseEnum.PACKET_TYPE packetType, double multiplier)
	{
		if (!double.IsFinite(multiplier))
		{
			multiplier = 1.0;
		}
		if (specialRules == null)
		{
			return multiplier;
		}
		foreach (TowerDefenseMapRuleConfig specialRule in specialRules)
		{
			if (!GodotObject.IsInstanceValid(specialRule) || specialRule.packetRules == null)
			{
				continue;
			}
			foreach (TowerDefenseMapPacketRuleConfig packetRule in specialRule.packetRules)
			{
				if (GodotObject.IsInstanceValid(packetRule) && packetRule.Matches(packetType))
				{
					multiplier *= packetRule.cooldownMultiplier;
				}
			}
		}
		if (!double.IsFinite(multiplier))
		{
			return 0.0;
		}
		return Math.Max(0.0, multiplier);
	}

	public bool IgnoresDynamicPacketCostGrowth(TowerDefenseEnum.PACKET_TYPE packetType)
	{
		if (specialRules == null)
		{
			return false;
		}
		foreach (TowerDefenseMapRuleConfig specialRule in specialRules)
		{
			if (!GodotObject.IsInstanceValid(specialRule) || specialRule.packetRules == null)
			{
				continue;
			}
			foreach (TowerDefenseMapPacketRuleConfig packetRule in specialRule.packetRules)
			{
				if (GodotObject.IsInstanceValid(packetRule) && packetRule.Matches(packetType) && packetRule.ignoreDynamicCostGrowth)
				{
					return true;
				}
			}
		}
		return false;
	}

	public double GetAttackDpsLifestealRatio()
	{
		if (_specialRuleRuntimeCacheValid)
		{
			return _cachedAttackDpsLifestealRatio;
		}
		return RefreshSpecialRuleRuntimeCache();
	}

	public double RefreshSpecialRuleRuntimeCache()
	{
		double num = 0.0;
		System.Array.Fill(_cachedZombieColumnSpeedMultipliers, 1.0);
		int num2 = specialRules?.Count ?? 0;
		for (int i = 0; i < num2; i++)
		{
			TowerDefenseMapRuleConfig towerDefenseMapRuleConfig = specialRules[i];
			if (!GodotObject.IsInstanceValid(towerDefenseMapRuleConfig))
			{
				continue;
			}
			if (double.IsFinite(towerDefenseMapRuleConfig.attackDpsLifestealRatio))
			{
				num += Math.Max(0.0, towerDefenseMapRuleConfig.attackDpsLifestealRatio);
			}
			if (towerDefenseMapRuleConfig.zombieColumnSpeedMinCol > 0 && towerDefenseMapRuleConfig.zombieColumnSpeedMaxCol > 0 && towerDefenseMapRuleConfig.zombieColumnSpeedMinCol <= towerDefenseMapRuleConfig.zombieColumnSpeedMaxCol && double.IsFinite(towerDefenseMapRuleConfig.zombieColumnSpeedMultiplier) && !(towerDefenseMapRuleConfig.zombieColumnSpeedMultiplier < 0.0))
			{
				int num3 = Math.Max(1, towerDefenseMapRuleConfig.zombieColumnSpeedMinCol);
				int num4 = Math.Min(50, towerDefenseMapRuleConfig.zombieColumnSpeedMaxCol);
				for (int j = num3; j <= num4; j++)
				{
					_cachedZombieColumnSpeedMultipliers[j] *= towerDefenseMapRuleConfig.zombieColumnSpeedMultiplier;
				}
			}
		}
		for (int k = 1; k <= 50; k++)
		{
			if (!double.IsFinite(_cachedZombieColumnSpeedMultipliers[k]))
			{
				_cachedZombieColumnSpeedMultipliers[k] = 1.0;
			}
		}
		_cachedAttackDpsLifestealRatio = (double.IsFinite(num) ? num : 0.0);
		_specialRuleRuntimeCacheValid = true;
		return _cachedAttackDpsLifestealRatio;
	}

	public void InvalidateSpecialRuleRuntimeCache()
	{
		_specialRuleRuntimeCacheValid = false;
	}

	public double GetZombieColumnSpeedMultiplier(int gridX)
	{
		if (gridX <= 0 || gridX > 50)
		{
			return 1.0;
		}
		if (!_specialRuleRuntimeCacheValid)
		{
			RefreshSpecialRuleRuntimeCache();
		}
		return _cachedZombieColumnSpeedMultipliers[gridX];
	}

	public void ApplyCharacterRules(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || specialRules == null)
		{
			return;
		}
		foreach (TowerDefenseMapRuleConfig specialRule in specialRules)
		{
			if (!GodotObject.IsInstanceValid(specialRule) || specialRule.characterRules == null)
			{
				continue;
			}
			foreach (TowerDefenseMapCharacterRuleConfig characterRule in specialRule.characterRules)
			{
				if (GodotObject.IsInstanceValid(characterRule))
				{
					characterRule.Apply(character);
				}
			}
		}
	}

	public bool TryValidateRuntime(out string reason)
	{
		if (gridNum.X < 1 || gridNum.Y < 1)
		{
			reason = $"grid dimensions must be positive, got {gridNum.X}x{gridNum.Y}";
			return false;
		}
		if (gridNum.X > 50 || gridNum.Y > 50)
		{
			reason = $"grid dimensions exceed the supported {50}x{50} limit: {gridNum.X}x{gridNum.Y}";
			return false;
		}
		if (maximumFps == 0 || maximumFps < -1)
		{
			reason = $"maximum FPS must be -1 or positive, got {maximumFps}";
			return false;
		}
		if (!IsFinitePositive(gridSize))
		{
			reason = $"grid cell size must be finite and positive, got {gridSize}";
			return false;
		}
		if (!IsFinitePositive(mapSize))
		{
			reason = $"map size must be finite and positive, got {mapSize}";
			return false;
		}
		if (!IsFinite(gridBeginPos) || !IsFinite(mapOffset) || !double.IsFinite(plantOffset))
		{
			reason = "grid origin, map offset, and plant offset must be finite";
			return false;
		}
		if (!IsFinite(edge) || edge.Z <= edge.X || edge.W <= edge.Y)
		{
			reason = $"map edge must be finite with right > left and bottom > top, got {edge}";
			return false;
		}
		if (lineUse != null)
		{
			foreach (int item in lineUse)
			{
				if (item < 1 || item > gridNum.Y)
				{
					reason = $"default enabled line {item} is outside 1..{gridNum.Y}";
					return false;
				}
			}
		}
		if (specialRules != null)
		{
			HashSet<StringName> hashSet = new HashSet<StringName>();
			foreach (TowerDefenseMapRuleConfig specialRule in specialRules)
			{
				if (!GodotObject.IsInstanceValid(specialRule))
				{
					reason = "special rule list contains a null or freed rule";
					return false;
				}
				if (!specialRule.TryValidateRuntime(out reason))
				{
					return false;
				}
				if (!hashSet.Add(specialRule.id))
				{
					reason = $"special rule id '{specialRule.id}' is duplicated";
					return false;
				}
			}
		}
		if (cellConfig != null)
		{
			foreach (TowerDefenseCellConfig item2 in cellConfig)
			{
				if (!GodotObject.IsInstanceValid(item2))
				{
					reason = "cell topology contains a null or freed config";
					return false;
				}
				if (item2.pos.X < 1 || item2.pos.Y < 1 || item2.pos.Z < item2.pos.X || item2.pos.W < item2.pos.Y || item2.pos.Z > gridNum.X || item2.pos.W > gridNum.Y)
				{
					reason = $"cell range {item2.pos} is outside the {gridNum.X}x{gridNum.Y} grid";
					return false;
				}
				if (item2.gridType == null)
				{
					reason = $"cell range {item2.pos} has no grid-type array";
					return false;
				}
				if (GodotObject.IsInstanceValid(item2.groundHeightCurve) && !GodotObject.IsInstanceValid(item2.groundHeightCurve.Curve))
				{
					reason = $"cell range {item2.pos} has an invalid ground-height curve";
					return false;
				}
			}
		}
		reason = "";
		return true;
	}

	public bool IsRuntimeTopologyCompatibleWith(TowerDefenseMapConfig other, out string reason)
	{
		if (!GodotObject.IsInstanceValid(other))
		{
			reason = "target map config is null or freed";
			return false;
		}
		if (!TryValidateRuntime(out reason))
		{
			return false;
		}
		if (!other.TryValidateRuntime(out reason))
		{
			return false;
		}
		if (this == other)
		{
			reason = "";
			return true;
		}
		if (gridNum != other.gridNum)
		{
			reason = $"grid dimensions differ: {gridNum} versus {other.gridNum}";
			return false;
		}
		if (gridBeginPos != other.gridBeginPos || gridSize != other.gridSize || plantOffset != other.plantOffset || mapSize != other.mapSize || edge != other.edge)
		{
			reason = "grid origin, cell size, plant offset, map size, or edge differs";
			return false;
		}
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig effectiveCellConfig = GetEffectiveCellConfig(i, j);
				TowerDefenseCellConfig effectiveCellConfig2 = other.GetEffectiveCellConfig(i, j);
				if (!AreGridTypesEquivalent(effectiveCellConfig, effectiveCellConfig2) || !AreGroundCurvesEquivalent(effectiveCellConfig?.groundHeightCurve, effectiveCellConfig2?.groundHeightCurve))
				{
					reason = $"structural cell topology differs at ({i}, {j})";
					return false;
				}
			}
		}
		reason = "";
		return true;
	}

	internal TowerDefenseCellConfig GetEffectiveCellConfig(int x, int y)
	{
		TowerDefenseCellConfig result = null;
		if (cellConfig == null)
		{
			return result;
		}
		foreach (TowerDefenseCellConfig item in cellConfig)
		{
			if (GodotObject.IsInstanceValid(item) && x >= item.pos.X && x <= item.pos.Z && y >= item.pos.Y && y <= item.pos.W)
			{
				result = item;
			}
		}
		return result;
	}

	private static bool AreGridTypesEquivalent(TowerDefenseCellConfig left, TowerDefenseCellConfig right)
	{
		int num = (GodotObject.IsInstanceValid(left) ? left.gridType.Count : 2);
		int num2 = (GodotObject.IsInstanceValid(right) ? right.gridType.Count : 2);
		if (num != num2)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			int num3 = (GodotObject.IsInstanceValid(left) ? ((int)left.gridType[i]) : ((i == 0) ? 2 : 4));
			TowerDefenseEnum.PLANTGRIDTYPE pLANTGRIDTYPE = (GodotObject.IsInstanceValid(right) ? right.gridType[i] : ((i == 0) ? TowerDefenseEnum.PLANTGRIDTYPE.GROUND : TowerDefenseEnum.PLANTGRIDTYPE.AIR));
			if (num3 != (int)pLANTGRIDTYPE)
			{
				return false;
			}
		}
		return true;
	}

	private static bool AreGroundCurvesEquivalent(CurveTexture leftTexture, CurveTexture rightTexture)
	{
		Curve curve = (GodotObject.IsInstanceValid(leftTexture) ? leftTexture.Curve : null);
		Curve curve2 = (GodotObject.IsInstanceValid(rightTexture) ? rightTexture.Curve : null);
		if (!GodotObject.IsInstanceValid(curve) || !GodotObject.IsInstanceValid(curve2))
		{
			if (!GodotObject.IsInstanceValid(curve))
			{
				return !GodotObject.IsInstanceValid(curve2);
			}
			return false;
		}
		for (int i = 0; i <= 128; i++)
		{
			float offset = (float)i / 128f;
			if (!Mathf.IsEqualApprox(curve.Sample(offset), curve2.Sample(offset)))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsFinite(Vector2 value)
	{
		if (float.IsFinite(value.X))
		{
			return float.IsFinite(value.Y);
		}
		return false;
	}

	private static bool IsFinitePositive(Vector2 value)
	{
		if (IsFinite(value) && value.X > 0f)
		{
			return value.Y > 0f;
		}
		return false;
	}

	private static bool IsFinite(Vector4 value)
	{
		if (float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z))
		{
			return float.IsFinite(value.W);
		}
		return false;
	}

	public Texture2D GetMapTexture(bool cache = true)
	{
		if (cache && GodotObject.IsInstanceValid(_mapTexture))
		{
			return _mapTexture;
		}
		if (string.IsNullOrWhiteSpace(mapTexturePath))
		{
			return null;
		}
		Texture2D result = ResourceLoader.Load<Texture2D>(mapTexturePath, "", (ResourceLoader.CacheMode)(cache ? 1 : 0));
		if (cache)
		{
			_mapTexture = result;
		}
		return result;
	}

	public PackedScene GetMapScene(bool cache = true)
	{
		if (cache && GodotObject.IsInstanceValid(_mapScene))
		{
			return _mapScene;
		}
		if (string.IsNullOrWhiteSpace(mapScenePath))
		{
			return null;
		}
		PackedScene result = ResourceLoader.Load<PackedScene>(mapScenePath, "", (ResourceLoader.CacheMode)(cache ? 1 : 0));
		if (cache)
		{
			_mapScene = result;
		}
		return result;
	}

	public Texture2D GetMapThumbnail(int maxDimension = 384)
	{
		maxDimension = Math.Max(16, maxDimension);
		string mapThumbnailCacheKey = GetMapThumbnailCacheKey(maxDimension);
		if (GodotObject.IsInstanceValid(_mapThumbnail) && _mapThumbnailKey == mapThumbnailCacheKey)
		{
			return _mapThumbnail;
		}
		Texture2D texture2D = ThumbnailBinaryResourceCache.Load("map-thumbnail", mapThumbnailCacheKey);
		if (GodotObject.IsInstanceValid(texture2D))
		{
			_mapThumbnail = texture2D;
			_mapThumbnailKey = mapThumbnailCacheKey;
			return _mapThumbnail;
		}
		Texture2D texture2D2 = CreateMapThumbnail(maxDimension);
		if (GodotObject.IsInstanceValid(texture2D2))
		{
			ThumbnailBinaryResourceCache.Save("map-thumbnail", mapThumbnailCacheKey, texture2D2);
			_mapThumbnail = texture2D2;
			_mapThumbnailKey = mapThumbnailCacheKey;
		}
		return _mapThumbnail;
	}

	public void ClearLoadedMapResources(bool clearThumbnail = false)
	{
		_mapTexture = null;
		_mapScene = null;
		if (clearThumbnail)
		{
			_mapThumbnail = null;
			_mapThumbnailKey = "";
		}
	}

	public static void ApplyMapPreviewTexture(TextureRect textureRect, Texture2D texture)
	{
		if (GodotObject.IsInstanceValid(textureRect))
		{
			textureRect.Texture = texture;
			textureRect.Scale = Vector2.One;
			textureRect.Rotation = 0f;
			textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
		}
	}

	private static string GetResourcePath(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "";
		}
		return resource.ResourcePath ?? "";
	}

	private Texture2D CreateMapThumbnail(int maxDimension)
	{
		Image image = LoadMapSourceImage();
		if (!GodotObject.IsInstanceValid(image) || image.IsEmpty())
		{
			return null;
		}
		if (!PrepareMapThumbnailSourceImage(image))
		{
			return null;
		}
		int width = image.GetWidth();
		int height = image.GetHeight();
		int num = Math.Max(width, height);
		if (num > maxDimension)
		{
			float num2 = (float)maxDimension / (float)num;
			int width2 = Math.Max(1, Mathf.RoundToInt((float)width * num2));
			int height2 = Math.Max(1, Mathf.RoundToInt((float)height * num2));
			image.Resize(width2, height2, Image.Interpolation.Bilinear);
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static bool PrepareMapThumbnailSourceImage(Image image)
	{
		if (!GodotObject.IsInstanceValid(image) || image.IsEmpty())
		{
			return false;
		}
		if (!image.IsCompressed())
		{
			return true;
		}
		return image.Decompress() == Error.Ok;
	}

	private Image LoadMapSourceImage()
	{
		if (!string.IsNullOrWhiteSpace(mapTexturePath))
		{
			Texture2D texture2D = GetMapTexture(cache: false);
			Image image = (GodotObject.IsInstanceValid(texture2D) ? texture2D.GetImage() : null);
			if (GodotObject.IsInstanceValid(image) && !image.IsEmpty())
			{
				return image;
			}
			string text = ProjectSettings.GlobalizePath(mapTexturePath);
			if (!string.IsNullOrWhiteSpace(text))
			{
				Image image2 = Image.LoadFromFile(text);
				if (GodotObject.IsInstanceValid(image2) && !image2.IsEmpty())
				{
					return image2;
				}
			}
		}
		return null;
	}

	private string GetMapThumbnailCacheKey(int maxDimension)
	{
		string text = (string.IsNullOrWhiteSpace(mapTexturePath) ? base.ResourcePath : mapTexturePath);
		return $"{text}|{maxDimension}|{GetSourceModifiedStamp(text)}";
	}

	private static long GetSourceModifiedStamp(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return 0L;
		}
		try
		{
			string text = ProjectSettings.GlobalizePath(path);
			if (!string.IsNullOrWhiteSpace(text) && File.Exists(text))
			{
				return File.GetLastWriteTimeUtc(text).Ticks;
			}
		}
		catch
		{
		}
		try
		{
			if (Godot.FileAccess.FileExists(path))
			{
				return (long)Godot.FileAccess.GetModifiedTime(path);
			}
		}
		catch
		{
		}
		return 0L;
	}
}
