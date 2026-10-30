using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Tools;

namespace PVZHE.ModEditor.PVZIntegration;

public partial class TresExtensionMethod : XWFileSystemExtensionMethod
{
	private static readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

	private const int _iconCacheLimit = 4096;

	private static string _lastAnimationMainOpenPath = "";

	private static ulong _lastAnimationMainOpenMsec;

	public override Texture2D GetIcon(XWFileSystemTreeItemData data)
	{
		if (data == null || data.Path == null)
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Object.svg");
		}
		string path = data.Path;
		if (_iconCache.TryGetValue(path, out var value))
		{
			return value;
		}
		Texture2D texture2D = ResolveIconByResourceType(path);
		if (_iconCache.Count >= 4096)
		{
			return texture2D;
		}
		_iconCache[path] = texture2D;
		return texture2D;
	}

	private static Texture2D ResolveIconByResourceType(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Object.svg");
		}
		string text = ReadResourceTypeWithoutLoading(path);
		if (string.IsNullOrWhiteSpace(text))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Object.svg");
		}
		return GetIconForResourceType(text);
	}

	private static Texture2D GetIconForResourceType(string resourceType)
	{
		if (ResourceTypeMatches(resourceType, "XWBPScript") || resourceType.Contains("XWBPScript"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/GraphEdit.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefenseLevelConfig") || resourceType.Contains("TowerDefenseLevel"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceLevel.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefenseMapConfig") || resourceType.Contains("TowerDefenseMap"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceMap.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefenseCharacterConfig") || resourceType.Contains("CharacterConfig"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceCharacter.svg");
		}
		if (ResourceTypeMatches(resourceType, "CharacterArmorData") || ResourceTypeMatches(resourceType, "CharacterCustomData") || ResourceTypeMatches(resourceType, "CharacterDamagePointData") || resourceType.Contains("CharacterArmorData") || resourceType.Contains("CharacterCustomData") || resourceType.Contains("CharacterDamagePointData"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceCharacter.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefensePacketConfig") || resourceType.Contains("PacketConfig"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceCard.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefensePacketBank") || resourceType.Contains("PacketBank"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourcePacketBank.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefenseProjectileChange") || resourceType.Contains("ProjectileChange"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceProjectileChange.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefenseProjectile") || resourceType.Contains("Projectile"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceProjectile.svg");
		}
		if (ResourceTypeMatches(resourceType, "CollectableConfig") || resourceType.Contains("Collectable"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceCollectable.svg");
		}
		if (ResourceTypeMatches(resourceType, "Mower") || resourceType.Contains("Mower"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceMower.svg");
		}
		if (ResourceTypeMatches(resourceType, "ShovelConfig") || resourceType.Contains("Shovel"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceShovel.svg");
		}
		if (ResourceTypeMatches(resourceType, "FallingObject") || resourceType.Contains("FallingObject"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceFallingObject.svg");
		}
		if (ResourceTypeMatches(resourceType, "AdobeAnimateData") || resourceType.Contains("AdobeAnimate"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceAnimation.svg");
		}
		if (ResourceTypeMatches(resourceType, "TowerDefenseBackgroundMusic") || resourceType.Contains("BackgroundMusic"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceBGM.svg");
		}
		if (ResourceTypeMatches(resourceType, "ShopConfig") || resourceType.Contains("ShopConfig"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceShop.svg");
		}
		if (ResourceTypeMatches(resourceType, "DialogBoxBase") || resourceType.Contains("Dialog"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceGUI.svg");
		}
		if (ResourceTypeMatches(resourceType, "Texture2D") || resourceType.Contains("Texture"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/FileThumbnail.svg");
		}
		if (ResourceTypeMatches(resourceType, "PackedScene"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/PackedScene.svg");
		}
		if (ResourceTypeMatches(resourceType, "AudioStream") || resourceType.StartsWith("AudioStream"))
		{
			return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/ResourceAudio.svg");
		}
		if (!ResourceTypeMatches(resourceType, "Font"))
		{
			resourceType.Contains("Font");
		}
		return XWFileSystemExtensionMethod.LoadIcon("res://addons/ModEditor/Icons/Object.svg");
	}

	private static string ReadResourceTypeWithoutLoading(string path)
	{
		using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return "";
		}
		for (int i = 0; i < 32; i++)
		{
			if (fileAccess.EofReached())
			{
				break;
			}
			string line = fileAccess.GetLine();
			if (line.Contains("script_class=\"XWBPScript\""))
			{
				return "XWBPScript";
			}
			if (line.StartsWith("[gd_resource"))
			{
				string text = ReadQuotedAttribute(line, "script_class");
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
				return ReadQuotedAttribute(line, "type");
			}
		}
		return "";
	}

	private static string ReadQuotedAttribute(string line, string attributeName)
	{
		string text = attributeName + "=\"";
		int num = line.IndexOf(text, StringComparison.Ordinal);
		if (num < 0)
		{
			return "";
		}
		num += text.Length;
		int num2 = line.IndexOf('"', num);
		if (num2 <= num)
		{
			return "";
		}
		return line.Substring(num, num2 - num);
	}

	private static bool ResourceTypeMatches(string resourceType, string expectedBaseType)
	{
		if (string.IsNullOrWhiteSpace(resourceType))
		{
			return false;
		}
		if (resourceType == expectedBaseType)
		{
			return true;
		}
		if (ClassDB.ClassExists(resourceType))
		{
			return ClassDB.IsParentClass(resourceType, expectedBaseType);
		}
		return false;
	}

	public override void Execute(XWFileSystemTreeItemData data)
	{
		if (!ResourceLoader.Exists(data.Path))
		{
			XWEditorInterface.Instance?.ShowToast("资源不存在: " + data.Path);
		}
		else
		{
			if (TryOpenAdobeAnimateResourceWithoutLoading(data.Path))
			{
				return;
			}
			Resource resource = ResourceLoader.Load<Resource>(data.Path, null, ResourceLoader.CacheMode.Reuse);
			if (resource == null)
			{
				XWEditorInterface.Instance?.ShowToast("无法加载资源: " + data.Path);
			}
			else if (resource is XWBPScript bpScript)
			{
				if (XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor)
				{
					xWBPEditor.Init(bpScript);
					XWEditorInterface.Instance?.FocusPanel("bp_editor");
				}
				else
				{
					XWEditorInterface.Instance?.ShowToast("蓝图编辑器尚未就绪，蓝图未打开。", 2);
				}
			}
			else
			{
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, data.Path, "resource_editor"));
			}
		}
	}

	public static bool TryCreateAdobeAnimatePreview(string resourcePath, out AdobeAnimateData animation)
	{
		animation = null;
		resourcePath = resourcePath?.Replace('\\', '/') ?? "";
		if (!XWFileSystemCompanionResourcePolicy.IsAdobeAnimateResource(resourcePath))
		{
			return false;
		}
		animation = new AdobeAnimateData
		{
			ResourceName = resourcePath.GetFile().GetBaseName()
		};
		if (XWFileSystemCompanionResourcePolicy.TryResolveAdobeAnimateDatPath(resourcePath, out var datPath))
		{
			animation.SetAnimeFileForModImport(datPath.GetFile());
			animation.LoadDatMetadataForModPreview(datPath);
			animation.SetEditorResolvedAnimeFilePath(datPath);
			XWAdobeAnimateTrace.Write($"TRES Adobe Animate quick open metadata state: resource={resourcePath}, dat={datPath}, frames={animation.frameMax}, clips={animation.clips?.Count ?? 0}, layers={animation.layerDictionary?.Count ?? 0}");
		}
		return true;
	}

	public static bool TryOpenAdobeAnimateResourceWithoutLoading(string resourcePath)
	{
		resourcePath = resourcePath?.Replace('\\', '/') ?? "";
		if (resourcePath == _lastAnimationMainOpenPath && Time.GetTicksMsec() - _lastAnimationMainOpenMsec < 750)
		{
			XWEditorInterface.Instance?.FocusPanel("animation_editor");
			return true;
		}
		if (!TryCreateAdobeAnimatePreview(resourcePath, out var animation))
		{
			return false;
		}
		XWEditorInterface.Instance?.EditResource(animation, XWResourceEditContext.ForRoot(animation, resourcePath, "animation_editor"));
		_lastAnimationMainOpenPath = resourcePath;
		_lastAnimationMainOpenMsec = Time.GetTicksMsec();
		return true;
	}
}
