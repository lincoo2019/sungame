using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorAnimationAtlasVisualWorkbenchRuntimeProbe : Node
{
	private const string BootstrapProfilePath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres";

	private readonly List<string> _failures = new List<string>();

	private string _temporaryRoot = "";

	public override async void _Ready()
	{
		try
		{
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "无法实例化 ModEditorManager。");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish();
				return;
			}
			await WaitFrames(2);
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			bool f3 = await WaitForInterface(900);
			Require(f3, "F3 未初始化真实 Mod 编辑器。");
			if (!f3)
			{
				Finish();
				return;
			}
			_temporaryRoot = ProjectSettings.GlobalizePath($"user://AnimationAtlasVisualWorkbench-{Guid.NewGuid():N}");
			Directory.CreateDirectory(_temporaryRoot);
			AdobeAnimateAtlasProfile builtIn = ResourceLoader.Load<AdobeAnimateAtlasProfile>("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres", "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(builtIn), "无法加载内置启动图集配置。");
			if (!GodotObject.IsInstanceValid(builtIn))
			{
				Finish();
				return;
			}
			AdobeAnimateAtlasProfile profile = builtIn.Duplicate(deep: true) as AdobeAnimateAtlasProfile;
			profile.ProfileId = "可视化图集探针";
			profile.StartupOnly = true;
			string profilePath = Path.Combine(_temporaryRoot, "可视化图集探针.tres");
			Error error = ResourceSaver.Save(profile, profilePath, ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"图集探针资源保存失败：{error}");
			profile = ResourceLoader.Load<AdobeAnimateAtlasProfile>(profilePath, "", ResourceLoader.CacheMode.Ignore);
			XWVisualEditorDescriptor descriptor = null;
			bool route = GodotObject.IsInstanceValid(profile) && XWResourceEditorRegistry.TryGetEditor(profile, profilePath, out descriptor) && descriptor.Category == "AnimationAtlas" && descriptor.DockKey == "animation_atlas_editor" && descriptor.ScenePath.EndsWith("XWAnimationAtlasEditorPanel.tscn", StringComparison.Ordinal);
			Require(route, "动画图集资源没有路由到专用工作台场景。");
			Node sentinel = new Node
			{
				Name = "AnimationAtlasInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(sentinel);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool inspectorBaseline = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == sentinel;
			Require(inspectorBaseline, "无法建立全局 Inspector 哨兵。");
			ulong openStartedAt = Time.GetTicksMsec();
			XWEditorInterface.Instance.EditResource(profile);
			XWEditorInterface.Instance.FocusPanel("animation_atlas_editor");
			XWAnimationAtlasVisualResourceEditor editor = await WaitForWorkbench(300);
			ulong openElapsedMs = Time.GetTicksMsec() - openStartedAt;
			bool specialized = GodotObject.IsInstanceValid(editor) && editor.GetType() == typeof(XWAnimationAtlasVisualResourceEditor);
			Require(specialized, "动画图集 Dock 没有实例化专用编辑器。");
			if (!specialized)
			{
				Finish();
				return;
			}
			XWDirectPropertySurface xWDirectPropertySurface = editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			bool direct = GodotObject.IsInstanceValid(xWDirectPropertySurface) && editor.DirectEditablePropertyCount >= 3 && editor.DirectMissingPropertyCount == 0 && xWDirectPropertySurface.EditablePropertyNames.Contains("ProfileId") && xWDirectPropertySurface.EditablePropertyNames.Contains("ManifestPath") && xWDirectPropertySurface.EditablePropertyNames.Contains("StartupOnly") && InspectorIsHidden(editor);
			Require(direct, "ProfileId、ManifestPath、StartupOnly 没有全部位于主画面直接属性区。");
			ItemList itemList = editor.FindChild("AtlasEntryList", recursive: true, owned: false) as ItemList;
			ItemList itemList2 = editor.FindChild("AtlasPageList", recursive: true, owned: false) as ItemList;
			TextureRect textureRect = editor.FindChild("AtlasPreviewTexture", recursive: true, owned: false) as TextureRect;
			Label label = editor.FindChild("AtlasManifestStatus", recursive: true, owned: false) as Label;
			GD.Print($"[MOD_EDITOR_ANIMATION_ATLAS_VISUAL_DIAGNOSTIC] loaded={editor.ManifestLoaded} sources={editor.ManifestSourceCount} pages={editor.ManifestPageCount} errors={editor.ManifestErrorCount} entries={itemList?.ItemCount ?? (-1)} pageRows={itemList2?.ItemCount ?? (-1)} texture={textureRect?.Texture?.GetType().Name ?? "null"} status={label?.Text ?? "null"}");
			bool manifest = editor.ManifestLoaded && GodotObject.IsInstanceValid(editor.ActiveManifest) && editor.ManifestSourceCount == 2 && editor.ManifestPageCount >= 2 && editor.ManifestErrorCount == 0;
			bool visual = manifest && GodotObject.IsInstanceValid(itemList) && itemList.ItemCount == 2 && GodotObject.IsInstanceValid(itemList2) && itemList2.ItemCount >= 2 && GodotObject.IsInstanceValid(textureRect) && GodotObject.IsInstanceValid(textureRect.Texture) && GodotObject.IsInstanceValid(label) && label.Text.Contains("清单已就绪", StringComparison.Ordinal);
			Require(manifest, "专用工作台没有加载或校验启动图集清单。");
			Require(visual, "图集页、动画条目或纹理预览没有显示。");
			string validManifestPath = profile.ManifestPath;
			profile.ManifestPath = "res://__missing_animation_atlas_manifest__.tres";
			editor.LoadResource(profile, profilePath, descriptor);
			await WaitFrames(8);
			Label label2 = editor.FindChild("AtlasManifestStatus", recursive: true, owned: false) as Label;
			bool errors = !editor.ManifestLoaded && editor.ManifestErrorCount > 0 && GodotObject.IsInstanceValid(label2) && label2.Text.Contains("错误", StringComparison.Ordinal) && GodotObject.IsInstanceValid(editor.FindChild("AtlasManifestErrors", recursive: true, owned: false));
			Require(errors, "无效清单路径没有显示可见错误状态。");
			profile.ManifestPath = validManifestPath;
			editor.LoadResource(profile, profilePath, descriptor);
			await WaitFrames(8);
			string category;
			string key;
			bool infer = ModLoader.InferRuntimeEntry("Resources/AnimationAtlasProfiles/可视化图集探针.tres", out category, out key) && category == "AnimationAtlas" && key == "可视化图集探针";
			Require(infer, "ModLoader 未推断动画图集配置运行时入口。");
			bool registry = XWModRuntimeRegistry.Register("animation-atlas-direct-registry-probe", "AnimationAtlas", "可视化图集直接注册", Variant.From(in profile)) && XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("可视化图集直接注册", out var profile2) && profile2 == profile && XWModRuntimeRegistry.UnregisterOwner("animation-atlas-direct-registry-probe") == 1 && !XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("可视化图集直接注册", out var profile3);
			AdobeAnimateAtlasProfile from = profile.Duplicate(deep: true) as AdobeAnimateAtlasProfile;
			AdobeAnimateAtlasProfile from2 = profile.Duplicate(deep: true) as AdobeAnimateAtlasProfile;
			from.ProfileId = "共享覆盖图集";
			from2.ProfileId = "共享覆盖图集";
			from.StartupOnly = false;
			from2.StartupOnly = false;
			AdobeAnimateAtlasProfile profile4;
			bool flag = XWModRuntimeRegistry.Register("animation-atlas-update-probe", "AnimationAtlas", "动画图集同所有者更新", Variant.From(in from)) && XWModRuntimeRegistry.Register("animation-atlas-update-probe", "AnimationAtlas", "动画图集同所有者更新", Variant.From(in from2)) && XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("动画图集同所有者更新", out profile4) && profile4 == from2 && XWModRuntimeRegistry.UnregisterOwner("animation-atlas-update-probe") == 1;
			string diagnostic;
			AdobeAnimateAtlasProfile profile5;
			AdobeAnimateAtlasProfile profile6;
			bool flag2 = XWModRuntimeRegistry.Register("animation-atlas-lower-layer-probe", "AnimationAtlas", "动画图集覆盖恢复", Variant.From(in from)) && XWModRuntimeRegistry.Register("animation-atlas-upper-layer-probe", "AnimationAtlas", "动画图集覆盖恢复", Variant.From(in from2), allowOverride: true, out diagnostic) && XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("动画图集覆盖恢复", out profile5) && profile5 == from2 && XWModRuntimeRegistry.UnregisterOwner("animation-atlas-upper-layer-probe") == 1 && XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("动画图集覆盖恢复", out profile6) && profile6 == from && XWModRuntimeRegistry.UnregisterOwner("animation-atlas-lower-layer-probe") == 1 && !XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("动画图集覆盖恢复", out profile3);
			registry = registry && flag && flag2;
			Require(registry, "动画图集配置没有完成注册、查询和卸载闭环。");
			string text = Path.Combine(_temporaryRoot, "PackageProject");
			string text2 = Path.Combine(text, "Resources", "AnimationAtlasProfiles");
			string text3 = Path.Combine(text, "Resources", "AnimationAtlasManifests");
			Directory.CreateDirectory(text2);
			Directory.CreateDirectory(text3);
			string path = Path.Combine(text3, "可视化图集包清单.tres");
			string path2 = Path.Combine(text3, "可视化图集包页.png");
			using (Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8))
			{
				image.Fill(new Color(0.22f, 0.72f, 0.94f));
				Require(image.SavePng(path2) == Error.Ok, "无法创建包内图集页。");
			}
			AdobeAnimateGlobalAtlasManifest obj = new AdobeAnimateGlobalAtlasManifest
			{
				ResourceName = "动画图集包自带清单",
				AtlasTextureArrayPath = "可视化图集包页.png",
				AtlasTextureArrayLayerSize = new Vector2(2f, 2f),
				AtlasTextureArrayLayerCount = 1,
				AtlasTextureArrayColumns = 1,
				SourceKeys = new Array<string> { "包内动画来源" },
				SourceStarts = new Array<int> { 0 },
				SourceCounts = new Array<int> { 0 },
				SourceSignatures = new Array<string> { "package-local" }
			};
			Error error2 = ResourceSaver.Save(obj, path, ResourceSaver.SaverFlags.None);
			string path3 = Path.Combine(text2, "可视化图集包.tres");
			AdobeAnimateAtlasProfile obj2 = builtIn.Duplicate(deep: true) as AdobeAnimateAtlasProfile;
			obj2.ProfileId = "可视化图集包";
			obj2.ManifestPath = "../AnimationAtlasManifests/可视化图集包清单.tres";
			obj2.StartupOnly = true;
			Error error3 = ResourceSaver.Save(obj2, path3, ResourceSaver.SaverFlags.None);
			XWModManifest xWModManifest = new XWModManifest
			{
				Id = "animation-atlas-package-probe",
				Name = "动画图集运行时包探针",
				Version = "1.0.0",
				Resources = new List<string> { "Resources/AnimationAtlasProfiles/可视化图集包.tres", "Resources/AnimationAtlasManifests/可视化图集包清单.tres", "Resources/AnimationAtlasManifests/可视化图集包页.png" }
			};
			xWModManifest.Save(Path.Combine(text, "mod.json"));
			string outputDir = Path.Combine(_temporaryRoot, "Packages");
			string text4 = ((error3 == Error.Ok && error2 == Error.Ok) ? ModExporter.ExportFromDirectory("动画图集运行时包探针", new ModExporter.ModInfo
			{
				Name = "动画图集运行时包探针",
				Version = "1.0.0",
				Author = "Mod 编辑器回归"
			}, text, xWModManifest.Resources, outputDir) : "");
			ModLoader.LoadedMod loadedMod = (File.Exists(text4) ? ModLoader.LoadMod(text4) : null);
			int num;
			if (loadedMod != null && ModLoader.ApplyMod(loadedMod) && loadedMod.AppliedResourceCount == 1 && XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("可视化图集包", out var profile7) && profile7.ProfileId == "可视化图集包" && profile7.ManifestPath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
			{
				AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = profile7.LoadManifest();
				if (adobeAnimateGlobalAtlasManifest != null && adobeAnimateGlobalAtlasManifest.ResourceName == "动画图集包自带清单" && adobeAnimateGlobalAtlasManifest.AtlasTextureArrayPath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
				{
					num = (File.Exists(ProjectSettings.GlobalizePath(adobeAnimateGlobalAtlasManifest.AtlasTextureArrayPath)) ? 1 : 0);
					goto IL_0f87;
				}
			}
			num = 0;
			goto IL_0f87;
			IL_10bd:
			int num2;
			bool flag3 = (byte)num2 != 0;
			bool flag4 = flag3 && ModLoader.UnloadMod("animation-atlas-package-probe") && !XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("可视化图集包", out profile3);
			bool flag5;
			bool package = error3 == Error.Ok && error2 == Error.Ok && flag5 && flag3 && flag4;
			Require(package, "动画图集配置没有通过真实 .pmod 加载、应用和卸载。");
			ulong before = Time.GetTicksMsec();
			await WaitFrames(6);
			bool flag6 = openElapsedMs < 1500 && Time.GetTicksMsec() - before < 1000;
			Require(flag6, "动画图集工作台阻塞了主线程。");
			bool flag7 = inspectorBaseline && GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == sentinel;
			Require(flag7, "动画图集工作台替换了全局 Inspector 对象。");
			GD.Print($"[MOD_EDITOR_ANIMATION_ATLAS_VISUAL_WORKBENCH_PROBE] f3={f3} route={route} specialized={specialized} direct={direct} manifest={manifest} visual={visual} errors={errors} infer={infer} registry={registry} package={package} responsive={flag6} openMs={openElapsedMs} inspectorUntouched={flag7} sources={editor.ManifestSourceCount} pages={editor.ManifestPageCount} failures={_failures.Count}");
			goto end_IL_0083;
			IL_0f87:
			flag5 = (byte)num != 0;
			obj.ResourceName = "动画图集包自带清单第二版";
			obj.SourceKeys[0] = "包内动画来源第二版";
			Error error4 = ResourceSaver.Save(obj, path, ResourceSaver.SaverFlags.None);
			xWModManifest.Version = "2.0.0";
			xWModManifest.Save(Path.Combine(text, "mod.json"));
			string text5 = ((flag5 && error4 == Error.Ok) ? ModExporter.ExportFromDirectory("动画图集运行时包探针", new ModExporter.ModInfo
			{
				Name = "动画图集运行时包探针",
				Version = "2.0.0",
				Author = "Mod 编辑器回归"
			}, text, xWModManifest.Resources, outputDir) : "");
			ModLoader.LoadedMod loadedMod2 = (File.Exists(text5) ? ModLoader.LoadMod(text5) : null);
			if (flag5 && loadedMod2 != null && ModLoader.ApplyMod(loadedMod2) && XWModRuntimeRegistry.TryGetRuntimeAnimationAtlasProfile("可视化图集包", out var profile8))
			{
				AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest2 = profile8.LoadManifest();
				if (adobeAnimateGlobalAtlasManifest2 != null && adobeAnimateGlobalAtlasManifest2.ResourceName == "动画图集包自带清单第二版" && adobeAnimateGlobalAtlasManifest2.SourceKeys.Count == 1 && adobeAnimateGlobalAtlasManifest2.SourceKeys[0] == "包内动画来源第二版")
				{
					num2 = (adobeAnimateGlobalAtlasManifest2.AtlasTextureArrayPath.StartsWith("user://", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
					goto IL_10bd;
				}
			}
			num2 = 0;
			goto IL_10bd;
			end_IL_0083:;
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static async Task<bool> WaitForInterface(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (GodotObject.IsInstanceValid(XWEditorInterface.Instance?.GetEditorPanel()) && GodotObject.IsInstanceValid(XWEditorInterface.Instance?.GetInspector()))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static async Task<XWAnimationAtlasVisualResourceEditor> WaitForWorkbench(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("animation_atlas_editor") is XWAnimationAtlasVisualResourceEditor xWAnimationAtlasVisualResourceEditor && GodotObject.IsInstanceValid(xWAnimationAtlasVisualResourceEditor.FindChild("AtlasManifestStatus", recursive: true, owned: false)) && GodotObject.IsInstanceValid(xWAnimationAtlasVisualResourceEditor.FindChild("DirectPropertySurface", recursive: true, owned: false)))
			{
				return xWAnimationAtlasVisualResourceEditor;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static bool InspectorIsHidden(Control editor)
	{
		Control control = editor?.FindChild("InspectorPanel", recursive: true, owned: false) as Control;
		Control control2 = editor?.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as Control;
		if (!GodotObject.IsInstanceValid(control) || !control.IsVisibleInTree())
		{
			if (GodotObject.IsInstanceValid(control2))
			{
				return !control2.IsVisibleInTree();
			}
			return true;
		}
		return false;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private static async Task WaitFrames(int frames)
	{
		SceneTree tree = Engine.GetMainLoop() as SceneTree;
		for (int index = 0; index < frames; index++)
		{
			await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError(failure);
		}
		if (!string.IsNullOrWhiteSpace(_temporaryRoot) && Directory.Exists(_temporaryRoot))
		{
			Directory.Delete(_temporaryRoot, recursive: true);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
