using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorInlineTextCoverageRuntimeProbe : Node
{
	private sealed record DedicatedVisualRoute(string Category, string DockKey, string Surface, string ControlName, string RuntimeEvidence);

	private readonly List<string> _failures = new List<string>();

	private XWGenericVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	private static readonly Dictionary<string, DedicatedVisualRoute> DedicatedVisualRoutes = new Dictionary<string, DedicatedVisualRoute>(StringComparer.Ordinal)
	{
		["ExpressionGuard.expression"] = new DedicatedVisualRoute("StateGuard", "state_guard_editor", "state-guard-workbench", "ExpressionEdit", "MOD_EDITOR_STATE_GUARD_RUNTIME_PROBE"),
		["StateIsActiveGuard.state"] = new DedicatedVisualRoute("StateGuard", "state_guard_editor", "state-guard-workbench", "StatePathEdit", "MOD_EDITOR_STATE_GUARD_RUNTIME_PROBE"),
		["StateMachineGuardDefinition.ComparedProperty"] = new DedicatedVisualRoute("StateGuard", "state_guard_editor", "state-guard-workbench", "ComparedPropertyEdit", "MOD_EDITOR_STATE_GUARD_RUNTIME_PROBE")
	};

	public override async void _Ready()
	{
		bool undoRedo = false;
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
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
			bool f3 = await WaitForEditor(900);
			Require(f3, "F3 did not initialize the real resource editor.");
			if (!f3)
			{
				Finish();
				return;
			}
			Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://InlineTextCoverageProbe"));
			string resourcePath = "user://InlineTextCoverageProbe/InlineTextProbe.tres";
			XWInlineTextProbeResource resource = new XWInlineTextProbeResource();
			Require(ResourceSaver.Save(resource, resourcePath, ResourceSaver.SaverFlags.None) == Error.Ok, "Probe resource could not be saved initially.");
			XWInlineTextProbeResource resource2 = ResourceLoader.Load<XWInlineTextProbeResource>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(resource2), "Probe resource did not cache-ignore reload.");
			Node sentinel = new Node
			{
				Name = "InlineTextInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector seededInspector = await WaitForInspector(120);
			seededInspector?.EditObject(sentinel);
			await WaitFrames(2);
			Require(GodotObject.IsInstanceValid(seededInspector) && seededInspector.CurrentObject == sentinel, "Global Inspector sentinel could not be seeded before inline-text authoring.");
			XWEditorInterface.Instance.EditResource(resource2);
			XWEditorInterface.Instance.FocusPanel("resource_editor");
			await WaitFrames(10);
			XWInlineTextSurface xWInlineTextSurface = _editor.FindChild("InlineTextSurface", recursive: true, owned: false) as XWInlineTextSurface;
			bool inline = GodotObject.IsInstanceValid(xWInlineTextSurface) && xWInlineTextSurface.SemanticTextCount == 5 && xWInlineTextSurface.InlineCoveredCount == 5 && xWInlineTextSurface.MissingCount == 0 && xWInlineTextSurface.TechnicalTextCount == 4 && xWInlineTextSurface.TechnicalCoveredCount == 4 && xWInlineTextSurface.TechnicalMissingCount == 0 && xWInlineTextSurface.FindChild("GameTextStage", recursive: true, owned: false) != null && xWInlineTextSurface.FindChild("Pick_ResourceFile", recursive: true, owned: false) is Button;
			Require(inline, $"Inline surface failed representative coverage: semantic={xWInlineTextSurface?.SemanticTextCount ?? (-1)}; covered={xWInlineTextSurface?.InlineCoveredCount ?? (-1)}; technical={xWInlineTextSurface?.TechnicalTextCount ?? (-1)}; technicalCovered={xWInlineTextSurface?.TechnicalCoveredCount ?? (-1)}.");
			OptionButton optionButton = xWInlineTextSurface?.FindChild("InlineEnum_SleepMode", recursive: true, owned: false) as OptionButton;
			XWInspectorPropertyEditorNodePath xWInspectorPropertyEditorNodePath = xWInlineTextSurface?.FindChild("InlineNodePath_TargetPath", recursive: true, owned: false) as XWInspectorPropertyEditorNodePath;
			XWInlineTextProperty property = xWInlineTextSurface?.Properties.FirstOrDefault((XWInlineTextProperty xWInlineTextProperty) => xWInlineTextProperty.Property.ToString() == "ResourceFile");
			XWInlineTextProperty property2 = xWInlineTextSurface?.Properties.FirstOrDefault((XWInlineTextProperty xWInlineTextProperty) => xWInlineTextProperty.Property.ToString() == "SaveKey");
			string baseType;
			string baseType2;
			bool typedBindings = GodotObject.IsInstanceValid(optionButton) && optionButton.ItemCount == 3 && xWInlineTextSurface.FindChild("Inline_SleepMode", recursive: true, owned: false) == null && GodotObject.IsInstanceValid(xWInspectorPropertyEditorNodePath) && xWInspectorPropertyEditorNodePath.FindChild("SelectNodePathButton", recursive: true, owned: false) is Button && xWInlineTextSurface.FindChild("Pick_TargetPath", recursive: true, owned: false) == null && xWInlineTextSurface.FindChild("Pick_SaveKey", recursive: true, owned: false) == null && XWInlineTextCoverageContract.TryResolveBindingBaseType(property, out baseType) && baseType == "Resource" && XWInlineTextCoverageContract.ShouldStorePickedResourcePath(property) && !XWInlineTextCoverageContract.TryResolveBindingBaseType(property2, out baseType2);
			Require(typedBindings, "Enum/NodePath/resource binding fields did not receive their field-specific visual controls.");
			if (GodotObject.IsInstanceValid(optionButton))
			{
				_history.ClearHistory();
				optionButton.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
				await WaitFrames(3);
				bool enumApplied = resource2.SleepMode == "Night" && _history.HasUndo();
				bool enumUndone = _history.Undo();
				await WaitFrames(2);
				enumUndone &= resource2.SleepMode == "Never";
				bool enumRedone = _history.Redo();
				await WaitFrames(2);
				enumRedone &= resource2.SleepMode == "Night";
				typedBindings = typedBindings && enumApplied && enumUndone && enumRedone;
				Require(enumApplied && enumUndone && enumRedone, "Text enum did not round-trip through the shared Undo/Redo history.");
			}
			xWInlineTextSurface = _editor.FindChild("InlineTextSurface", recursive: true, owned: false) as XWInlineTextSurface;
			Require(GodotObject.IsInstanceValid(xWInlineTextSurface), "Inline text surface was not remounted after the enum edit refresh.");
			XWDirectPropertySurface xWDirectPropertySurface = _editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			bool deduplicated = GodotObject.IsInstanceValid(xWDirectPropertySurface) && xWDirectPropertySurface.SpecializedPropertyCount == xWInlineTextSurface.Properties.Count && xWDirectPropertySurface.MissingPropertyCount == 0 && xWDirectPropertySurface.MountedPropertyCount == xWDirectPropertySurface.EditablePropertyCount && xWDirectPropertySurface.DirectControlCount + xWDirectPropertySurface.SpecializedPropertyCount == xWDirectPropertySurface.EditablePropertyCount && xWDirectPropertySurface.FindChild("Direct_Title", recursive: true, owned: false) == null && xWDirectPropertySurface.FindChild("Direct_SaveKey", recursive: true, owned: false) == null && xWDirectPropertySurface.FindChild("Direct_SleepMode", recursive: true, owned: false) == null && xWDirectPropertySurface.FindChild("Direct_TargetPath", recursive: true, owned: false) == null && xWDirectPropertySurface.FindChild("Direct_ResourceFile", recursive: true, owned: false) == null;
			Require(deduplicated, $"Inline properties were duplicated by direct-property cards: specialized={xWDirectPropertySurface?.SpecializedPropertyCount ?? (-1)}; direct={xWDirectPropertySurface?.DirectControlCount ?? (-1)}; editable={xWDirectPropertySurface?.EditablePropertyCount ?? (-1)}.");
			bool condition = VerifyClassificationSamples();
			Require(condition, "Strong semantic tokens were shadowed by broad technical tokens.");
			bool condition2 = VerifyPromotedUsageClassifications();
			Require(condition2, "Audited template/canvas/player-facing text was not promoted to an inline semantic role.");
			LineEdit lineEdit = xWInlineTextSurface?.FindChild("Inline_Title", recursive: true, owned: false) as LineEdit;
			Require(GodotObject.IsInstanceValid(lineEdit), "Title was not editable at the game title position.");
			if (GodotObject.IsInstanceValid(lineEdit))
			{
				_history.ClearHistory();
				lineEdit.EmitSignal(Control.SignalName.FocusEntered);
				lineEdit.Text = "Edited on game card";
				lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, lineEdit.Text);
				lineEdit.EmitSignal(Control.SignalName.FocusExited);
				await WaitFrames(6);
				bool enumRedone = resource2.Title == "Edited on game card" && _history.HasUndo();
				bool enumUndone = _history.Undo();
				await WaitFrames(3);
				enumUndone &= resource2.Title == "Original title";
				bool enumApplied = _history.Redo();
				await WaitFrames(3);
				enumApplied &= resource2.Title == "Edited on game card";
				undoRedo = enumRedone && enumUndone && enumApplied;
				Require(undoRedo, "Inline title did not round-trip through shared Undo/Redo.");
			}
			Button button = FindButtonByText(_editor, "保存");
			Require(GodotObject.IsInstanceValid(button), "Resource toolbar Save button was missing.");
			button?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(8);
			XWInlineTextProbeResource xWInlineTextProbeResource = ResourceLoader.Load<XWInlineTextProbeResource>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
			bool savedReloaded = GodotObject.IsInstanceValid(xWInlineTextProbeResource) && xWInlineTextProbeResource.Title == "Edited on game card";
			Require(savedReloaded, "Inline edit did not survive explicit Save/cache-ignore reload.");
			(int, int, int, int, int, int, int, int, int, int, int) obj = await AuditAllAuthorResources();
			int item = obj.Item1;
			int item2 = obj.Item2;
			int item3 = obj.Item3;
			int item4 = obj.Item4;
			int item5 = obj.Item5;
			int item6 = obj.Item6;
			int item7 = obj.Item7;
			int item8 = obj.Rest.Item1;
			int item9 = obj.Rest.Item2;
			int item10 = obj.Rest.Item3;
			int item11 = obj.Rest.Item4;
			Require(item == 324, $"Expected 324 author resource types, got {item}.");
			int num = (from descriptor in XWResourceEditorRegistry.GetAllEditors()
				select descriptor.Category).Distinct<string>(StringComparer.Ordinal).Count();
			Require(item2 == num, $"Authoring category audit did not match the runtime registry: audited={item2}; registry={num}.");
			Require(item2 == 39, $"Expected the current 39-category authoring baseline, got {item2}.");
			Require(item3 == 52 && item4 == 52 && item8 == 0, $"Semantic coverage incomplete: semantic={item3}; covered={item4}; missing={item8}.");
			Require(item5 == 635 && item6 == 635, $"Technical binding coverage incomplete: technical={item5}; covered={item6}.");
			Require(item7 == 0, $"Technical scalar audit still has {item7} unclassified properties.");
			Require(item9 > 0 && item10 == item9, $"NodePath selector coverage incomplete: fields={item9}; selectors={item10}.");
			Require(item11 == 0, $"Dedicated visual routes duplicated {item11} technical fields.");
			PanelContainer panelContainer = _editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
			XWInspector xWInspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool flag = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null && (xWInspector == null || xWInspector.CurrentObject == sentinel);
			Require(flag, "Inline text authoring touched or exposed the Inspector.");
			GD.Print($"[MOD_EDITOR_INLINE_TEXT_COVERAGE_PROBE] f3={f3} inline={inline} typedBindings={typedBindings} deduplicated={deduplicated} authorTypes={item} categories={item2} semanticText={item3} inlineCovered={item4} technical={item5} technicalCovered={item6} unclassified={item7} missing={item8} nodePaths={item9} nodePathSelectors={item10} duplicateVisualFields={item11} undoRedo={undoRedo} savedReloaded={savedReloaded} inspectorUntouched={flag} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(int AuthorTypes, int Categories, int Semantic, int InlineCovered, int Technical, int TechnicalCovered, int Unclassified, int Missing, int NodePaths, int NodePathSelectors, int DuplicateVisualFields)> AuditAllAuthorResources()
	{
		List<Type> types = (from type2 in typeof(TowerDefenseBackgroundMusicConfig).Assembly.GetTypes()
			where !type2.IsAbstract && typeof(Resource).IsAssignableFrom(type2) && type2.GetCustomAttributes(typeof(GlobalClassAttribute), inherit: false).Length != 0 && IsAuthorableType(type2)
			select type2).OrderBy<Type, string>((Type type2) => type2.FullName, StringComparer.Ordinal).ToList();
		Require(!types.Contains(typeof(ModEditorStateMachineProbeDerivedDefinition)), "Non-GlobalClass derived state-machine probe unexpectedly changed the author resource baseline.");
		List<string> source = (from xWVisualEditorDescriptor in XWResourceEditorRegistry.GetAllEditors()
			select xWVisualEditorDescriptor.Category).Distinct<string>(StringComparer.Ordinal).ToList();
		HashSet<string> categoryNames = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, int> authorTypesByCategory = source.ToDictionary<string, string, int>((string result) => result, (string _) => 0, StringComparer.Ordinal);
		Dictionary<string, int> semanticCountByCategory = source.ToDictionary<string, string, int>((string result) => result, (string _) => 0, StringComparer.Ordinal);
		Dictionary<string, int> technicalCountByCategory = source.ToDictionary<string, string, int>((string result) => result, (string _) => 0, StringComparer.Ordinal);
		SortedDictionary<string, List<string>> technicalByCategory = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
		SortedDictionary<string, List<string>> semanticByCategory = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
		XWInlineTextSurface surface = new XWInlineTextSurface
		{
			Visible = false
		};
		AddChild(surface, forceReadableName: false, InternalMode.Disabled);
		int semantic = 0;
		int covered = 0;
		int technical = 0;
		int technicalCovered = 0;
		int unclassified = 0;
		int missing = 0;
		int nodePaths = 0;
		int nodePathSelectors = 0;
		List<XWInlineTextProperty> specializedTechnical = new List<XWInlineTextProperty>();
		string key;
		for (int index = 0; index < types.Count; index++)
		{
			Type type = types[index];
			Resource resource;
			try
			{
				resource = Activator.CreateInstance(type) as Resource;
			}
			catch (Exception ex)
			{
				missing++;
				Require(condition: false, $"{type.FullName} could not instantiate: {ex.GetType().Name}: {ex.Message}");
				continue;
			}
			if (!GodotObject.IsInstanceValid(resource) || !XWResourceEditorRegistry.TryGetEditor(resource, "", out var descriptor))
			{
				missing++;
				Require(condition: false, type.FullName + " had no authoring descriptor.");
				continue;
			}
			string category = descriptor.Category;
			categoryNames.Add(category);
			authorTypesByCategory[category]++;
			IReadOnlyList<XWInlineTextProperty> readOnlyList = XWInlineTextCoverageContract.Inspect(resource, category);
			VerifyTechnicalBaselineGrowth(type, readOnlyList);
			surface.BindResource(resource, category, _history);
			int num = readOnlyList.Count((XWInlineTextProperty property) => property.IsSemantic);
			int num2 = readOnlyList.Count((XWInlineTextProperty property) => property.IsTechnical);
			List<XWInlineTextProperty> list = readOnlyList.Where((XWInlineTextProperty property) => property.IsTechnical && HasDedicatedVisualRoute(property)).ToList();
			semantic += num;
			semanticCountByCategory[category] += num;
			covered += surface.InlineCoveredCount;
			technical += num2;
			unclassified += readOnlyList.Count((XWInlineTextProperty property) => property.ClassificationReason == "technical:unclassified-scalar" && !HasDedicatedVisualRoute(property));
			Dictionary<string, int> dictionary = technicalCountByCategory;
			key = category;
			dictionary[key] += num2;
			foreach (XWInlineTextProperty item3 in readOnlyList.Where((XWInlineTextProperty property) => property.IsNodePath))
			{
				nodePaths++;
				string pattern = "InlineNodePath_" + item3.Property.ToString().Replace('/', '_').Replace(':', '_')
					.Replace('.', '_')
					.Replace('@', '_');
				if (surface.FindChild(pattern, recursive: true, owned: false) is XWInspectorPropertyEditorNodePath)
				{
					nodePathSelectors++;
				}
				else
				{
					missing++;
				}
			}
			specializedTechnical.AddRange(list);
			technicalCovered += Math.Max(0, surface.TechnicalCoveredCount - list.Count);
			missing += surface.MissingCount + Math.Max(0, surface.TechnicalMissingCount - list.Count);
			if (category == "Animation" && type.Name == "AdobeAnimateData" && num != 0)
			{
				missing += num;
				Require(condition: false, "Animation skips the generic inline surface but " + type.Name + " declared semantic text.");
			}
			foreach (XWInlineTextProperty item4 in readOnlyList.Where((XWInlineTextProperty property) => property.IsSemantic))
			{
				if (!semanticByCategory.TryGetValue(category, out var value))
				{
					value = (semanticByCategory[category] = new List<string>());
				}
				value.Add($"{item4.CoverageKey}({item4.ClassificationReason},{item4.Role})");
			}
			foreach (XWInlineTextProperty item5 in readOnlyList.Where((XWInlineTextProperty property) => property.IsTechnical))
			{
				if (!technicalByCategory.TryGetValue(category, out var value2))
				{
					value2 = (technicalByCategory[category] = new List<string>());
				}
				string value3 = (HasDedicatedVisualRoute(item5) ? "specialized" : "binding-card");
				value2.Add($"{item5.CoverageKey}({item5.ClassificationReason},{value3})");
			}
			if ((index + 1) % 10 == 0)
			{
				await WaitFrames(1);
			}
		}
		AuditNativeCategoryRoute(new AudioStreamWav
		{
			MixRate = 22050
		}, "res://Assets/Audio/InlineTextProbe.wav", "Audio", categoryNames);
		AuditNativeCategoryRoute(new PackedScene(), "res://Resources/Dialogs/InlineTextProbe.tscn", "Dialog", categoryNames);
		AuditNativeCategoryRoute(new Gradient(), "res://Resources/Native/InlineTextProbe.tres", "General", categoryNames);
		(int, int, int) tuple = await VerifySpecializedTechnicalCoverage(specializedTechnical);
		int item = tuple.Item1;
		int item2 = tuple.Item2;
		int duplicateVisualFields = tuple.Item3;
		technicalCovered += item;
		missing += item2;
		foreach (string item6 in categoryNames.OrderBy<string, string>((string result) => result, StringComparer.Ordinal))
		{
			GD.Print($"[MOD_EDITOR_INLINE_TEXT_CATEGORY] category={item6} authorTypes={authorTypesByCategory.GetValueOrDefault(item6)} semantic={semanticCountByCategory.GetValueOrDefault(item6)} technical={technicalCountByCategory.GetValueOrDefault(item6)}");
		}
		List<string> value4;
		foreach (KeyValuePair<string, List<string>> item7 in semanticByCategory)
		{
			item7.Deconstruct(out key, out value4);
			string value5 = key;
			List<string> list4 = value4;
			list4.Sort(StringComparer.Ordinal);
			GD.Print($"[MOD_EDITOR_INLINE_TEXT_SEMANTIC] category={value5} count={list4.Count} properties={string.Join(',', list4)}");
		}
		foreach (KeyValuePair<string, List<string>> item8 in technicalByCategory)
		{
			item8.Deconstruct(out key, out value4);
			string value6 = key;
			List<string> list5 = value4;
			list5.Sort(StringComparer.Ordinal);
			GD.Print($"[MOD_EDITOR_INLINE_TEXT_TECHNICAL] category={value6} count={list5.Count} properties={string.Join(',', list5)}");
		}
		surface.QueueFree();
		await WaitFrames(2);
		return (AuthorTypes: types.Count, Categories: categoryNames.Count, Semantic: semantic, InlineCovered: covered, Technical: technical, TechnicalCovered: technicalCovered, Unclassified: unclassified, Missing: missing, NodePaths: nodePaths, NodePathSelectors: nodePathSelectors, DuplicateVisualFields: duplicateVisualFields);
	}

	private void VerifyTechnicalBaselineGrowth(Type type, IReadOnlyList<XWInlineTextProperty> declared)
	{
		HashSet<string> hashSet = (from property in declared
			where property.IsTechnical
			select property.CoverageKey).ToHashSet<string>(StringComparer.Ordinal);
		if (type == typeof(ModCharacterComponentDefinition))
		{
			string[] array = new string[5] { "ModCharacterComponentDefinition.ComponentTypeId", "ModCharacterComponentDefinition.DefinitionId", "ModCharacterComponentDefinition.InstanceId", "ModCharacterComponentDefinition.DefinitionTypeName", "ModCharacterComponentDefinition.RuntimeTypeName" };
			Require(hashSet.SetEquals(array), "Mod character component technical baseline changed: " + string.Join(",", hashSet.OrderBy<string, string>((string value) => value, StringComparer.Ordinal)) + ".");
		}
		else if (type == typeof(CharacterDamagePointConfig))
		{
			Require(hashSet.Contains("CharacterDamagePointConfig.replaceMediaTexturePath"), "Damage-point replacement texture path is missing from the technical baseline.");
		}
	}

	private void AuditNativeCategoryRoute(Resource resource, string path, string expected, HashSet<string> categories)
	{
		XWVisualEditorDescriptor descriptor;
		bool flag = GodotObject.IsInstanceValid(resource) && XWResourceEditorRegistry.TryGetEditor(resource, path, out descriptor) && descriptor.Category == expected;
		Require(flag, "Native representative did not prove the " + expected + " category route.");
		if (flag)
		{
			categories.Add(expected);
		}
	}

	private static bool HasDedicatedVisualRoute(XWInlineTextProperty property)
	{
		if (property != null)
		{
			if (!XWInlineTextCoverageContract.TryGetSpecializedCoverage(property, out var _))
			{
				return DedicatedVisualRoutes.ContainsKey(property.CoverageKey);
			}
			return true;
		}
		return false;
	}

	private async Task<(int Covered, int Missing, int DuplicateVisualFields)> VerifySpecializedTechnicalCoverage(IReadOnlyList<XWInlineTextProperty> properties)
	{
		if (properties.Count == 0)
		{
			return (Covered: 0, Missing: 0, DuplicateVisualFields: 0);
		}
		int coveredCount = 0;
		int missingCount = 0;
		int duplicateVisualFields = 0;
		foreach (XWInlineTextProperty property in properties)
		{
			if (XWInlineTextCoverageContract.TryGetSpecializedCoverage(property, out var animationCoverage))
			{
				AdobeAnimateData animation = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Splat/Spike/SpikeSplat.tres", "", ResourceLoader.CacheMode.Ignore);
				if (!GodotObject.IsInstanceValid(animation))
				{
					missingCount++;
					GD.Print("[MOD_EDITOR_INLINE_TEXT_SPECIALIZED] property=" + property.CoverageKey + " fixture=False covered=False");
					continue;
				}
				XWEditorInterface.Instance.EditResource(animation, XWResourceEditContext.ForRoot(animation, "res://Asset/Anime/Splat/Spike/SpikeSplat.tres", "animation_editor"));
				XWEditorInterface.Instance.FocusPanel("animation_editor");
				XWAnimationVisualResourceEditor animationEditor = null;
				for (int frame = 0; frame < 900; frame++)
				{
					animationEditor = XWEditorInterface.Instance.GetResourceEditor("animation_editor") as XWAnimationVisualResourceEditor;
					if (GodotObject.IsInstanceValid(animationEditor) && animationEditor.FindChild(animationCoverage.ControlName, recursive: true, owned: false) is Control)
					{
						break;
					}
					await WaitFrames(1);
				}
				bool flag = animationEditor?.FindChild(animationCoverage.ControlName, recursive: true, owned: false) is Control;
				bool flag2 = animationEditor?.FindChild(animationCoverage.PickerName, recursive: true, owned: false) is FileDialog;
				bool flag3 = animationEditor?.FindChild("DirectSettingsPanel", recursive: true, owned: false) is PanelContainer;
				bool flag4 = animationEditor?.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
				bool flag5 = GodotObject.IsInstanceValid(animation) && flag && flag2 && flag3 && flag4;
				GD.Print($"[MOD_EDITOR_INLINE_TEXT_SPECIALIZED] property={property.CoverageKey} surface={animationCoverage.Surface} control={animationCoverage.ControlName}:{flag} picker={animationCoverage.PickerName}:{flag2} evidence={animationCoverage.RuntimeEvidence} inspectorHidden={flag4} duplicateVisualFields=0 covered={flag5}");
				if (flag5)
				{
					coveredCount++;
				}
				else
				{
					missingCount++;
				}
				continue;
			}
			if (!DedicatedVisualRoutes.TryGetValue(property.CoverageKey, out var route) || !GodotObject.IsInstanceValid(property.Owner))
			{
				missingCount++;
				continue;
			}
			XWVisualEditorDescriptor descriptor;
			bool registryRoute = XWResourceEditorRegistry.TryGetEditor(property.Owner, "", out descriptor) && descriptor.Category == route.Category && descriptor.DockKey == route.DockKey;
			XWEditorInterface.Instance.EditResource(property.Owner, XWResourceEditContext.ForRoot(property.Owner, "", route.DockKey));
			XWEditorInterface.Instance.FocusPanel(route.DockKey);
			XWStateGuardVisualResourceEditor guardEditor = null;
			for (int frame = 0; frame < 900; frame++)
			{
				guardEditor = XWEditorInterface.Instance.GetResourceEditor(route.DockKey) as XWStateGuardVisualResourceEditor;
				if (GodotObject.IsInstanceValid(guardEditor) && guardEditor.EditingGuard == property.Owner && guardEditor.FindChild(route.ControlName, recursive: true, owned: false) is Control)
				{
					break;
				}
				await WaitFrames(1);
			}
			int num = (GodotObject.IsInstanceValid(guardEditor) ? guardEditor.FindChildren(route.ControlName, "", recursive: true, owned: false).Count : 0);
			string text = property.Property.ToString();
			int num2 = (GodotObject.IsInstanceValid(guardEditor) ? (guardEditor.FindChildren("Inline_" + text, "", recursive: true, owned: false).Count + guardEditor.FindChildren("Direct_" + text, "", recursive: true, owned: false).Count) : 0);
			int num3 = Math.Max(0, num - 1) + num2;
			duplicateVisualFields += num3;
			bool flag6 = guardEditor?.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
			bool flag7 = registryRoute && num == 1 && num2 == 0 && flag6;
			GD.Print($"[MOD_EDITOR_INLINE_TEXT_SPECIALIZED] property={property.CoverageKey} surface={route.Surface} dock={route.DockKey} route={registryRoute} control={route.ControlName}:{num == 1} evidence={route.RuntimeEvidence} inspectorHidden={flag6} duplicateVisualFields={num3} covered={flag7}");
			if (flag7)
			{
				coveredCount++;
			}
			else
			{
				missingCount++;
			}
			animationCoverage = null;
			route = null;
		}
		Require(missingCount == 0, $"{missingCount} specialized technical scalars did not map to verified dedicated controls.");
		Require(duplicateVisualFields == 0, $"{duplicateVisualFields} specialized technical scalars were rendered more than once.");
		return (Covered: coveredCount, Missing: missingCount, DuplicateVisualFields: duplicateVisualFields);
	}

	private static bool VerifyClassificationSamples()
	{
		string reason;
		XWInlineTextRole xWInlineTextRole = XWInlineTextCoverageContract.Classify("Sample", "eventMessage", Variant.Type.String, PropertyHint.None, "", out reason);
		string reason2;
		XWInlineTextRole xWInlineTextRole2 = XWInlineTextCoverageContract.Classify("Sample", "buttonLabel", Variant.Type.String, PropertyHint.None, "", out reason2);
		string reason3;
		XWInlineTextRole xWInlineTextRole3 = XWInlineTextCoverageContract.Classify("Sample", "stateDisplayName", Variant.Type.String, PropertyHint.None, "", out reason3);
		string reason4;
		XWInlineTextRole xWInlineTextRole4 = XWInlineTextCoverageContract.Classify("Sample", "resourcePath", Variant.Type.String, PropertyHint.File, "*.tres", out reason4);
		GD.Print($"[MOD_EDITOR_INLINE_TEXT_CLASSIFICATION_SAMPLES] eventMessage={xWInlineTextRole}:{reason} buttonLabel={xWInlineTextRole2}:{reason2} stateDisplayName={xWInlineTextRole3}:{reason3} resourcePath={xWInlineTextRole4}:{reason4}");
		if (xWInlineTextRole == XWInlineTextRole.HudMessage && xWInlineTextRole2 == XWInlineTextRole.ButtonLabel && xWInlineTextRole3 == XWInlineTextRole.Title)
		{
			return xWInlineTextRole4 == XWInlineTextRole.TechnicalPath;
		}
		return false;
	}

	private static bool VerifyPromotedUsageClassifications()
	{
		string reason;
		XWInlineTextRole xWInlineTextRole = XWInlineTextCoverageContract.Classify("CharacterCustomConfig", "customName", Variant.Type.String, PropertyHint.None, "", out reason);
		string reason2;
		XWInlineTextRole xWInlineTextRole2 = XWInlineTextCoverageContract.Classify("CharacterCustomConfig", "customHandbookName", Variant.Type.String, PropertyHint.None, "", out reason2);
		string reason3;
		XWInlineTextRole xWInlineTextRole3 = XWInlineTextCoverageContract.Classify("CharacterDamagePointConfig", "damagePointName", Variant.Type.String, PropertyHint.None, "", out reason3);
		string reason4;
		XWInlineTextRole xWInlineTextRole4 = XWInlineTextCoverageContract.Classify("TowerDefenseMapConfig", "translate", Variant.Type.String, PropertyHint.None, "", out reason4);
		string reason5;
		XWInlineTextRole xWInlineTextRole5 = XWInlineTextCoverageContract.Classify("TowerDefenseBackgroundMusicConfig", "translate", Variant.Type.String, PropertyHint.None, "", out reason5);
		GD.Print($"[MOD_EDITOR_INLINE_TEXT_PROMOTED] CharacterCustomConfig.customName={xWInlineTextRole}:{reason} CharacterCustomConfig.customHandbookName={xWInlineTextRole2}:{reason2} CharacterDamagePointConfig.damagePointName={xWInlineTextRole3}:{reason3} TowerDefenseMapConfig.translate={xWInlineTextRole4}:{reason4} TowerDefenseBackgroundMusicConfig.translate={xWInlineTextRole5}:{reason5}");
		if (xWInlineTextRole == XWInlineTextRole.Title && xWInlineTextRole2 == XWInlineTextRole.Title && xWInlineTextRole3 == XWInlineTextRole.Title && xWInlineTextRole4 == XWInlineTextRole.Title)
		{
			return xWInlineTextRole5 == XWInlineTextRole.Title;
		}
		return false;
	}

	private static bool IsAuthorableType(Type type)
	{
		foreach (XWVisualEditorDescriptor allEditor in XWResourceEditorRegistry.GetAllEditors())
		{
			if (allEditor.Category == "General")
			{
				continue;
			}
			Type type2 = type;
			while (type2 != null)
			{
				if (allEditor.ResourceClassNames.Contains(type2.Name))
				{
					return true;
				}
				type2 = type2.BaseType;
			}
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("resource_editor") is XWGenericVisualResourceEditor editor)
			{
				_editor = editor;
				_history = XWEditorInterface.Instance.GetUndoRedoManager();
				(XWEditorInterface.Instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<XWInspector> WaitForInspector(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetInspector() is XWInspector result)
			{
				return result;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static Button FindButtonByText(Node root, string text)
	{
		foreach (Node item in root.FindChildren("*", "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.Text == text)
			{
				return button;
			}
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_INLINE_TEXT_COVERAGE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_INLINE_TEXT_COVERAGE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
