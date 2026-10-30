using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

public partial class ModEditorModernCharacterComponentCreationRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWCharacterComponentVisualResourceEditor _editor;

	private readonly XWModComponentAssemblyCatalog _componentCatalog = new XWModComponentAssemblyCatalog();

	private const string CallbackSource = "\npublic static class StagedCallbackProbe { [StateMachineCallback(\"component-staged-probe\", StateMachineCallbackPhase.Enter)] public static void Enter(in StateMachineCallbackContext context) { } }\n";

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The probe verifies player-authored public parameterless component definitions discovered at runtime.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The probe reads public properties from its generated Mod Runtime to verify configuration binding.")]
	public override async void _Ready()
	{
		_ = 20;
		try
		{
			bool f3 = await OpenModEditor();
			Require(f3, "F3 did not open the ModEditor character-component workbench.");
			if (!f3)
			{
				Finish(f3: false, routeCreate: false, files: false, modern: false, collision: false, manifest: false, backgroundCompile: false, compiled: false, types: false, factory: false, editorRoute: false, resourceReload: false, inspectorFree: false, cardAndAdd: false, configuration: false, atomicCleanup: false, rollback: false, runtimeRelease: false, catalogOnly: false, hotRefresh: false, unload: false);
				return;
			}
			string modRoot = ProjectSettings.GlobalizePath("user://modern_component_create_probe_" + Guid.NewGuid().ToString("N"));
			string text = Path.Combine(modRoot, "Resources", "CharacterComponents");
			Directory.CreateDirectory(text);
			XWModManifest xWModManifest = new XWModManifest();
			xWModManifest.Id = "modern-component-create-probe";
			xWModManifest.Name = "Modern Component Create Probe";
			xWModManifest.Save(Path.Combine(modRoot, "mod.json"));
			XWFileSystem.GetSingleton()?.SetProjectFolderPath(modRoot);
			bool condition = XWResourceCreateRoute.GetActionsForDirectory(text, modRoot).Any((XWResourceCreateRoute.CreateAction action) => action.Id == "new-character-component-definition" && action.TemplateId == "character-component");
			Require(condition, "Resources/CharacterComponents does not expose the modern component action.");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-character-component-definition", text, "ProbeAbility");
			int num;
			if (templateCreateResult.Success)
			{
				List<string> createdPaths = templateCreateResult.CreatedPaths;
				if (createdPaths != null && createdPaths.Count == 3)
				{
					num = (templateCreateResult.CreatedPath.EndsWith("ProbeAbilityComponentDefinition.tres", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
					goto IL_022b;
				}
			}
			num = 0;
			goto IL_022b;
			IL_022b:
			bool routeCreate = (byte)num != 0;
			Require(routeCreate, "Production route did not create the modern package: " + templateCreateResult.Error);
			string definitionScript = FindCreated(templateCreateResult, "ProbeAbilityComponentDefinition.cs");
			string runtimeScript = FindCreated(templateCreateResult, "ProbeAbilityComponent.cs");
			string definitionResource = FindCreated(templateCreateResult, "ProbeAbilityComponentDefinition.tres");
			bool files = templateCreateResult.CreatedPaths.All(File.Exists) && File.Exists(definitionScript) && File.Exists(runtimeScript) && File.Exists(definitionResource);
			Require(files, "Modern component package did not atomically produce all three files.");
			string text2 = (files ? File.ReadAllText(definitionScript) : string.Empty);
			string text3 = (files ? File.ReadAllText(runtimeScript) : string.Empty);
			string text4 = (files ? File.ReadAllText(definitionResource) : string.Empty);
			text2 = text2.Replace("    public override CharacterComponentRuntime CreateRuntime()", "    [Export(PropertyHint.Range, \"0,100,1\")] public int Power { get; set; } = 25;\n    [Export(PropertyHint.Enum, \"Passive,Active\")] public string Mode { get; set; } = \"Passive\";\n    [Export] public string Label { get; set; } = \"Probe\";\n\n    public override CharacterComponentRuntime CreateRuntime()", StringComparison.Ordinal);
			text3 = text3.Replace("    protected override void OnBound()", "    public int ProbePower => GetConfiguration<int>(new Godot.StringName(\"Power\"), -1);\n    public string ProbeMode => GetConfiguration<string>(new Godot.StringName(\"Mode\"), \"Missing\");\n\n    protected override void OnBound()", StringComparison.Ordinal);
			if (files)
			{
				text3 += "\npublic static class StagedCallbackProbe { [StateMachineCallback(\"component-staged-probe\", StateMachineCallbackPhase.Enter)] public static void Enter(in StateMachineCallbackContext context) { } }\n";
				text3 += "\npublic sealed class ProbeThrowCtor : CharacterComponentRuntime { public ProbeThrowCtor() { throw new System.Exception(\"probe ctor\"); } }\npublic sealed class ProbeThrowBind : CharacterComponentRuntime { protected override void OnBound() { throw new System.Exception(\"probe bind\"); } }\npublic sealed class ProbeThrowRelease : CharacterComponentRuntime { protected override void OnReleased() { throw new System.Exception(\"probe release\"); } }\n";
				File.WriteAllText(definitionScript, text2);
				File.WriteAllText(runtimeScript, text3);
			}
			string text5 = text2 + "\n" + text3 + "\n" + text4;
			bool modern = text2.Contains(": CharacterComponentDefinition", StringComparison.Ordinal) && text2.Contains("CreateRuntime()", StringComparison.Ordinal) && text3.Contains(": CharacterComponentRuntime", StringComparison.Ordinal) && text3.Contains("OnDetaching(ComponentDetachReason reason)", StringComparison.Ordinal) && text4.Contains("ComponentTypeId = \"ProbeAbilityComponent\"", StringComparison.Ordinal) && text4.Contains("WireIndex = -1", StringComparison.Ordinal) && !text5.Contains("ComponentBase", StringComparison.Ordinal) && !text5.Contains("Node2D", StringComparison.Ordinal) && !text5.Contains(".tscn", StringComparison.Ordinal);
			Require(modern, "Generated package still contains the legacy node-component model.");
			string[] second = templateCreateResult.CreatedPaths.Select(File.ReadAllText).ToArray();
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = XWResourceCreateRoute.CreateFromAction("new-character-component-definition", text, "ProbeAbility");
			bool collision = !templateCreateResult2.Success && templateCreateResult.CreatedPaths.Select(File.ReadAllText).SequenceEqual(second) && Directory.GetFiles(Path.GetDirectoryName(definitionResource) ?? text).Length == 3;
			Require(collision, "Duplicate creation overwrote or partially changed the existing package.");
			bool atomicCleanup = ProbeAtomicBackupCleanup(text);
			Require(atomicCleanup, "Post-commit backup cleanup failure rolled back or damaged the promoted package.");
			XWModManifestSyncService.RegisterPaths(modRoot, templateCreateResult.CreatedPaths);
			XWModManifest xWModManifest2 = XWModManifest.Load(Path.Combine(modRoot, "mod.json"));
			bool manifestOk = xWModManifest2 != null && xWModManifest2.Scripts.Count((string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) == 2 && xWModManifest2.Resources.Count((string path) => path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)) == 1 && xWModManifest2.Resources.All((string path) => !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
			Require(manifestOk, "Manifest did not classify generated C# as Scripts and Definition as Resources.");
			Task<XWModComponentAssemblyCatalog.RefreshResult> compileTask = _componentCatalog.RefreshAsync(modRoot);
			int responsiveFrames = 0;
			while (!compileTask.IsCompleted && responsiveFrames < 1800)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				responsiveFrames++;
			}
			bool backgroundCompile = responsiveFrames > 1;
			Require(backgroundCompile, "Mod compilation blocked the Godot main thread.");
			XWModComponentAssemblyCatalog.RefreshResult refreshResult = await compileTask;
			XWScriptCompiler.CompileResult compileResult = refreshResult.CompileResult;
			bool compiled = refreshResult.Success && compileResult != null && compileResult.Success && compileResult.IsModProject && compileResult.ScriptCount == 2 && File.Exists(compileResult.OutputAssemblyPath);
			Require(compiled, $"Modern component catalog failed: {refreshResult.Message}; compileSuccess={compileResult?.Success}, isModProject={compileResult?.IsModProject}, scriptCount={compileResult?.ScriptCount}, assemblyExists={File.Exists(compileResult?.OutputAssemblyPath)}.\n{compileResult?.Output}");
			Type definitionType = null;
			bool types = false;
			bool factory = false;
			bool editorRoute = false;
			bool resourceReload = false;
			bool card = false;
			bool add = false;
			bool configuration = false;
			bool rollback = false;
			bool runtimeRelease = false;
			bool catalogOnly = false;
			bool inspectorFree = false;
			bool hotRefresh = false;
			bool unload = false;
			if (compiled)
			{
				XWModAssemblyLoader.LoadedModAssembly loadedAssembly = refreshResult.LoadedAssembly;
				Require(loadedAssembly.StateMachineCallbackCount == 1, "Committed component callbacks were not counted.");
				XWModAssemblyLoader xWModAssemblyLoader = new XWModAssemblyLoader();
				string stagedId = "component-staging-" + Guid.NewGuid().ToString("N");
				XWModAssemblyLoader.LoadedModAssembly loadedModAssembly = xWModAssemblyLoader.LoadStagedModAssembly(stagedId, compileResult.OutputAssemblyPath);
				Require(!StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries.Any((StateMachineCallbackCatalogEntry entry) => entry.OwnerId == stagedId) && xWModAssemblyLoader.FindTypesAssignableTo(stagedId, typeof(CharacterComponentRuntime)).All((Type type) => !CharacterComponentRuntimeTypeRegistry.TryGetOwner(type, out var _)), "Staged callbacks or component types were visible before commit.");
				Action<AssemblyLoadContext> value = delegate
				{
					throw new InvalidOperationException("expected unload failure");
				};
				loadedModAssembly.LoadContext.Unloading += value;
				Require(!xWModAssemblyLoader.UnloadMod(stagedId) && xWModAssemblyLoader.LoadedAssemblies.ContainsKey(stagedId) && loadedModAssembly.RegistrationsDetached && !string.IsNullOrEmpty(loadedModAssembly.LastUnloadDiagnostic), "ALC cleanup failure was swallowed or its retry record was lost.");
				loadedModAssembly.LoadContext.Unloading -= value;
				Require(xWModAssemblyLoader.UnloadMod(stagedId) && !xWModAssemblyLoader.LoadedAssemblies.ContainsKey(stagedId), "Detached candidate could not retry cleanup.");
				definitionType = refreshResult.DefinitionTypes.SingleOrDefault((Type type) => type.Name == "ProbeAbilityComponentDefinition");
				Type runtimeType = refreshResult.RuntimeTypes.SingleOrDefault((Type type) => type.Name == "ProbeAbilityComponent");
				types = definitionType != null && runtimeType != null;
				Require(types, "Loaded assembly did not expose the generated Definition and Runtime types.");
				if (types)
				{
					CharacterComponentDefinition dynamicDefinition = Activator.CreateInstance(definitionType) as CharacterComponentDefinition;
					CharacterComponentRuntime runtime = dynamicDefinition?.CreateRuntime();
					factory = dynamicDefinition != null && runtime?.GetType() == runtimeType;
					Require(factory, "Generated Definition.CreateRuntime did not create its matching Runtime.");
					editorRoute = XWResourceEditorRegistry.TryGetEditor(dynamicDefinition, definitionResource, out var descriptor) && descriptor?.DockKey == "character_component_editor";
					Require(editorRoute, "Generated Definition did not route to the character-component visual editor.");
					ModCharacterComponentDefinition modCharacterComponentDefinition = ResourceLoader.Load<ModCharacterComponentDefinition>(definitionResource, "", ResourceLoader.CacheMode.Ignore);
					bool flag = modCharacterComponentDefinition?.SynchronizeConfiguration(dynamicDefinition) ?? false;
					Error error = (flag ? ResourceSaver.Save(modCharacterComponentDefinition, definitionResource, ResourceSaver.SaverFlags.None) : Error.Failed);
					CharacterComponentRuntime proxyRuntime = modCharacterComponentDefinition?.CreateRuntime();
					resourceReload = GodotObject.IsInstanceValid(modCharacterComponentDefinition) && flag && error == Error.Ok && modCharacterComponentDefinition.DefinitionTypeName == definitionType.FullName && modCharacterComponentDefinition.RuntimeTypeName == runtimeType.FullName && modCharacterComponentDefinition.ComponentTypeId == "ProbeAbilityComponent" && modCharacterComponentDefinition.Configuration.ContainsKey("Power") && modCharacterComponentDefinition.Configuration["Power"].AsInt32() == 25 && modCharacterComponentDefinition.GetPropertyList().Any((Dictionary property) => property["name"].AsString() == "Configuration/Power" && property["hint"].AsInt64() == 1 && property["hint_string"].AsString() == "0,100,1") && proxyRuntime?.GetType() == runtimeType;
					Require(resourceReload, "Generated Definition.tres could not reload as the compiled dynamic type.");
					_editor.SetModComponentDefinitionTypes(loadedAssembly.Assembly, refreshResult.DefinitionTypes);
					CharacterComponentSet componentSet = new CharacterComponentSet
					{
						ResourceName = "ProbeAbilitySet"
					};
					XWEditorInterface.Instance.EditResource(componentSet);
					await WaitFrames(5);
					_editor.SetWorkbenchPage(2);
					await WaitFrames(5);
					XWGameVisualChoiceCard xWGameVisualChoiceCard = (_editor.FindChild("ComponentTypeGrid", recursive: true, owned: false) as HFlowContainer)?.GetChildren().OfType<XWGameVisualChoiceCard>().FirstOrDefault((XWGameVisualChoiceCard item) => item.ChoiceKey == definitionType.FullName);
					card = GodotObject.IsInstanceValid(xWGameVisualChoiceCard);
					Require(card, "Compiled Definition did not appear as a visual component-library card.");
					ModCharacterComponentDefinition addedProxy = null;
					if (card)
					{
						xWGameVisualChoiceCard.EmitSignal(BaseButton.SignalName.Pressed);
						(_editor.FindChild("AddComponentButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						await WaitFrames(5);
						addedProxy = componentSet.Components.OfType<ModCharacterComponentDefinition>().SingleOrDefault((ModCharacterComponentDefinition proxy) => proxy.DefinitionTypeName == definitionType.FullName);
						add = addedProxy?.RuntimeTypeName == runtimeType.FullName;
					}
					Require(add, "Visual library card could not add the compiled component to a real set.");
					if (addedProxy != null)
					{
						_editor.SetWorkbenchPage(1);
						await WaitFrames(3);
						SpinBox spinBox = FindPropertyControl<SpinBox>(_editor, "Configuration/Power");
						bool powerControlRendered = GodotObject.IsInstanceValid(spinBox);
						if (powerControlRendered)
						{
							spinBox.EmitSignal(Control.SignalName.FocusEntered);
							spinBox.Value = 40.0;
							spinBox.EmitSignal(Control.SignalName.FocusExited);
							await WaitFrames(3);
						}
						Error error2 = ResourceSaver.Save(addedProxy, definitionResource, ResourceSaver.SaverFlags.None);
						ModCharacterComponentDefinition savedProxy = ResourceLoader.Load<ModCharacterComponentDefinition>(definitionResource, "", ResourceLoader.CacheMode.Ignore);
						TowerDefenseCharacter runtimeOwner = new TowerDefenseCharacter();
						ComponentManager runtimeManager = new ComponentManager
						{
							parent = runtimeOwner
						};
						CharacterComponentRuntime configuredRuntime = runtimeManager.AddRuntimeComponent(savedProxy);
						object obj = configuredRuntime?.GetType().GetProperty("ProbePower")?.GetValue(configuredRuntime);
						object obj2 = configuredRuntime?.GetType().GetProperty("ProbeMode")?.GetValue(configuredRuntime);
						bool flag2 = powerControlRendered;
						Variant value2;
						bool flag3 = addedProxy.Configuration.TryGetValue("Power", out value2) && value2.AsInt32() == 40;
						Variant value3;
						bool flag4 = GodotObject.IsInstanceValid(savedProxy) && savedProxy.Configuration.TryGetValue("Power", out value3) && value3.AsInt32() == 40;
						bool flag5 = obj != null && Convert.ToInt32(obj) == 40 && string.Equals(obj2 as string, "Passive", StringComparison.Ordinal);
						Require(flag2, "Configuration/Power did not render as a direct SpinBox.");
						Require(flag3, $"Visual Power edit did not commit (value={value2}).");
						Require(error2 == Error.Ok, $"Configured proxy save failed: {error2}.");
						Require(flag4, "Configured Power did not survive CacheMode.Ignore reload.");
						Require(configuredRuntime != null, "Configured proxy did not bind a Runtime through ComponentManager.");
						Require(flag5, $"Bound Runtime did not read configuration (power={obj}, mode={obj2}).");
						configuration = flag2 && flag3 && error2 == Error.Ok && flag4 && configuredRuntime != null && flag5;
						Require(configuration, "Visual Export configuration did not save, reload, and reach the bound Runtime.");
						Require(!(await _componentCatalog.UnloadAsync()), "A bound Mod component did not block unload.");
						configuredRuntime.Detach(ComponentDetachReason.TemporaryTreeExit);
						Require(!(await _componentCatalog.UnloadAsync()), "Detached Mod runtime lost its lease before final release.");
						configuredRuntime.Bind(runtimeManager, runtimeOwner, savedProxy);
						int leaseCount = CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers(loadedAssembly.ModId).LeaseCount;
						Require(CharacterComponentRuntimeTypeRegistry.Create("ProbeThrowCtor") == null && CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers(loadedAssembly.ModId).LeaseCount == leaseCount, "Throwing constructor retained its creation reservation.");
						ModCharacterComponentDefinition modCharacterComponentDefinition2 = new ModCharacterComponentDefinition
						{
							ComponentTypeId = "ProbeThrowBind",
							InstanceId = "failure",
							RuntimeTypeName = "ProbeThrowBind"
						};
						Require(runtimeManager.AddRuntimeComponent(modCharacterComponentDefinition2) == null && CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers(loadedAssembly.ModId).LeaseCount == leaseCount, "Bind failure retained a component lease.");
						modCharacterComponentDefinition2.RuntimeTypeName = "ProbeThrowRelease";
						CharacterComponentRuntime characterComponentRuntime = runtimeManager.AddRuntimeComponent(modCharacterComponentDefinition2);
						Require(characterComponentRuntime != null && runtimeManager.RemoveRuntimeComponent(characterComponentRuntime) && characterComponentRuntime.IsReleased && characterComponentRuntime.Manager == null && CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers(loadedAssembly.ModId).LeaseCount == leaseCount, "Release-hook failure retained host references or a lease.");
						CharacterComponentRuntime characterComponentRuntime2 = dynamicDefinition.CreateRuntime();
						characterComponentRuntime2.Bind(runtimeManager, runtimeOwner, dynamicDefinition);
						Require(CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers(loadedAssembly.ModId).LeaseCount == leaseCount + 1, "Direct Definition factory bypassed the host binding lease.");
						characterComponentRuntime2.Release();
						runtimeManager.RemoveRuntimeComponent(configuredRuntime);
						runtimeManager.Dispose();
						runtimeOwner.Dispose();
					}
					PanelContainer panelContainer = _editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
					VBoxContainer vBoxContainer = _editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
					inspectorFree = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0;
					Require(inspectorFree, "Modern component creation reintroduced a raw Inspector surface.");
					runtime?.Release();
					proxyRuntime?.Release();
					definitionType = null;
					_editor.SetWorkbenchPage(0);
					await WaitFrames(5);
					CharacterComponentRuntime oldPreview = GetEditorRuntimePreview();
					Assembly oldAssembly = loadedAssembly.Assembly;
					Label label = _editor.FindChild("PreviewStatusLabel", recursive: true, owned: false) as Label;
					Require(oldPreview?.GetType().Assembly == oldAssembly && !oldPreview.IsReleased, "The visual workbench did not create a live old-generation Runtime preview. Status: " + (label?.Text ?? "missing"));
					string validDefinitionSource = File.ReadAllText(definitionScript);
					string validRuntimeSource = File.ReadAllText(runtimeScript);
					File.WriteAllText(definitionScript, "public sealed class ProbeAbilityComponentDefinition { }\n");
					File.WriteAllText(runtimeScript, "public sealed class ProbeAbilityComponent { }\n");
					XWModComponentAssemblyCatalog.RefreshResult refreshResult2 = await _componentCatalog.RefreshAsync(modRoot);
					CharacterComponentRuntime obj3 = addedProxy?.CreateRuntime();
					bool flag6 = !refreshResult2.Success;
					bool flag7 = (object)_componentCatalog.ActiveAssembly == oldAssembly;
					bool flag8 = obj3?.GetType().Assembly == oldAssembly;
					bool flag9 = oldPreview != null && !oldPreview.IsReleased;
					Require(flag6, $"Invalid candidate unexpectedly succeeded (definitions={refreshResult2.DefinitionTypes.Count}, runtimes={refreshResult2.RuntimeTypes.Count}, message={refreshResult2.Message}).");
					Require(flag7, "Invalid candidate replaced the active Assembly.");
					Require(flag8, "Invalid candidate removed the last known-good Runtime registry owner.");
					rollback = flag6 && flag7 && flag8 && flag9;
					Require(rollback, "A valid active component catalog was not preserved after candidate validation failed.");
					obj3?.Release();
					validDefinitionSource = validDefinitionSource.Replace("    [Export] public string Label { get; set; } = \"Probe\";", "    [Export] public string Label { get; set; } = \"Probe\";\n    [Export(PropertyHint.Range, \"0,10,0.1\")] public double Cooldown { get; set; } = 1.5;", StringComparison.Ordinal);
					File.WriteAllText(definitionScript, validDefinitionSource);
					File.WriteAllText(runtimeScript, validRuntimeSource);
					XWModComponentAssemblyCatalog.RefreshResult refreshResult3 = await _componentCatalog.RefreshAsync(modRoot);
					ModEditorModernCharacterComponentCreationRuntimeProbe modEditorModernCharacterComponentCreationRuntimeProbe = this;
					int condition2;
					if (!refreshResult3.Success && (object)_componentCatalog.ActiveAssembly == oldAssembly)
					{
						condition2 = ((oldPreview != null && !oldPreview.IsReleased) ? 1 : 0);
					}
					else
					{
						condition2 = 0;
					}
					modEditorModernCharacterComponentCreationRuntimeProbe.Require((byte)condition2 != 0, "Live preview refresh did not preserve the old catalog.");
					_editor.ReleaseComponentAssemblyReferences(oldAssembly);
					string[] callbacksBefore = StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries.Select((StateMachineCallbackCatalogEntry entry) => entry.Key).OrderBy<string, string>((string key) => key, StringComparer.Ordinal).ToArray();
					File.WriteAllText(definitionScript, "public sealed partial class ProbeAbilityComponentDefinition : CharacterComponentDefinition { public override CharacterComponentRuntime CreateRuntime() => null; }\n");
					File.WriteAllText(runtimeScript, "public sealed class ProbeAbilityComponent : CharacterComponentRuntime { private ProbeAbilityComponent() { } }\n\npublic static class StagedCallbackProbe { [StateMachineCallback(\"component-staged-probe\", StateMachineCallbackPhase.Enter)] public static void Enter(in StateMachineCallbackContext context) { } }\n");
					XWModComponentAssemblyCatalog.RefreshResult refreshResult4 = await _componentCatalog.RefreshAsync(modRoot);
					Require(!refreshResult4.Success && (refreshResult4.CompileResult?.Success ?? false) && (object)_componentCatalog.ActiveAssembly == oldAssembly && callbacksBefore.SequenceEqual(StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries.Select((StateMachineCallbackCatalogEntry entry) => entry.Key).OrderBy<string, string>((string key) => key, StringComparer.Ordinal)), "Rejected candidate changed the old catalog or retained staged callbacks.");
					File.WriteAllText(definitionScript, validDefinitionSource);
					File.WriteAllText(runtimeScript, validRuntimeSource);
					Require(!(await _componentCatalog.RefreshAsync(modRoot, new CancellationToken(canceled: true))).Success && (object)_componentCatalog.ActiveAssembly == oldAssembly, "Cancelled refresh replaced the old catalog.");
					Task<XWModComponentAssemblyCatalog.RefreshResult> hotRefreshTask = _componentCatalog.RefreshAsync(modRoot);
					int hotRefreshFrames = 0;
					while (!hotRefreshTask.IsCompleted && hotRefreshFrames < 1800)
					{
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						hotRefreshFrames++;
					}
					XWModComponentAssemblyCatalog.RefreshResult refreshed = await hotRefreshTask;
					Type refreshedDefinitionType = refreshed.DefinitionTypes.SingleOrDefault((Type type) => type.Name == "ProbeAbilityComponentDefinition");
					Type refreshedRuntimeType = refreshed.RuntimeTypes.SingleOrDefault((Type type) => type.Name == "ProbeAbilityComponent");
					_editor.ReleaseComponentAssemblyReferences(refreshed.ReplacedAssembly);
					runtimeRelease = (oldPreview?.IsReleased ?? false) && GetEditorRuntimePreview() == null;
					Require(runtimeRelease, "Old-generation visual Runtime was not released before catalog replacement.");
					_editor.SetModComponentDefinitionTypes(refreshed.LoadedAssembly?.Assembly, refreshed.DefinitionTypes);
					_editor.SetWorkbenchPage(2);
					await WaitFrames(5);
					HFlowContainer obj4 = _editor.FindChild("ComponentTypeGrid", recursive: true, owned: false) as HFlowContainer;
					CharacterComponentRuntime characterComponentRuntime3 = addedProxy?.CreateRuntime();
					List<Type> editorComponentTypes = GetEditorComponentTypes();
					int num2 = obj4?.GetChildren().OfType<XWGameVisualChoiceCard>().Count((XWGameVisualChoiceCard item) => item.ChoiceKey == refreshedDefinitionType?.FullName) ?? 0;
					catalogOnly = editorComponentTypes.All((Type type) => type.Assembly != oldAssembly) && editorComponentTypes.Any((Type type) => type.Assembly == refreshed.LoadedAssembly?.Assembly) && num2 == 1;
					Require(catalogOnly, "Component library retained an old collectible Type or duplicated the refreshed card.");
					hotRefresh = refreshed.Success && (object)refreshed.ReplacedAssembly == oldAssembly && (object)_componentCatalog.ActiveAssembly == refreshed.LoadedAssembly?.Assembly && hotRefreshFrames > 1 && refreshedDefinitionType != null && refreshedRuntimeType != null && num2 == 1 && characterComponentRuntime3?.GetType() == refreshedRuntimeType && addedProxy.Configuration["Power"].AsInt32() == 40 && Math.Abs(addedProxy.Configuration["Cooldown"].AsDouble() - 1.5) < 0.001;
					Require(refreshed.Success, "Restored valid component source did not refresh: " + refreshed.Message);
					Require((object)refreshed.ReplacedAssembly == oldAssembly, "Valid refresh did not report the replaced Assembly.");
					Require(addedProxy.Configuration.ContainsKey("Cooldown"), "Hot refresh did not add the new Cooldown Export schema.");
					Require(addedProxy.Configuration["Power"].AsInt32() == 40, "Hot refresh overwrote the edited Power value.");
					Require(hotRefresh, "An already-open component library did not refresh to the recompiled assembly.");
					characterComponentRuntime3?.Release();
					_editor.ReleaseComponentAssemblyReferences(refreshed.LoadedAssembly?.Assembly);
					refreshedDefinitionType = null;
					await WaitFrames(3);
					unload = await _componentCatalog.UnloadAsync();
					await WaitFrames(1);
					Require(unload, "Compiled component assembly did not unload after its editor references were released.");
				}
			}
			Finish(f3, routeCreate, files, modern, collision, manifestOk, backgroundCompile, compiled, types, factory, editorRoute, resourceReload, inspectorFree, card && add, configuration, atomicCleanup, rollback, runtimeRelease, catalogOnly, hotRefresh, unload);
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
			GD.PrintErr("[MOD_EDITOR_MODERN_COMPONENT_CREATE_PROBE_FAILURE] " + ex);
			Finish(f3: false, routeCreate: false, files: false, modern: false, collision: false, manifest: false, backgroundCompile: false, compiled: false, types: false, factory: false, editorRoute: false, resourceReload: false, inspectorFree: false, cardAndAdd: false, configuration: false, atomicCleanup: false, rollback: false, runtimeRelease: false, catalogOnly: false, hotRefresh: false, unload: false);
		}
	}

	private async Task<bool> OpenModEditor()
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
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			return false;
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
		for (int frame = 0; frame < 900; frame++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("character_component_editor") is XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor && GodotObject.IsInstanceValid(xWCharacterComponentVisualResourceEditor))
			{
				_editor = xWCharacterComponentVisualResourceEditor;
				break;
			}
			await WaitFrames(1);
		}
		if (!GodotObject.IsInstanceValid(_editor))
		{
			return false;
		}
		for (int frame = 0; frame < 900; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance.FocusPanel("character_component_editor");
				await WaitFrames(3);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static string FindCreated(XWTemplateLibrary.TemplateCreateResult result, string fileName)
	{
		return result.CreatedPaths?.FirstOrDefault((string path) => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
	}

	private static bool ProbeAtomicBackupCleanup(string componentRoot)
	{
		XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
		XWTemplateLibrary.TemplateCreateResult templateCreateResult = xWTemplateLibrary.CreateFromTemplateInDirectory("character-component", componentRoot, "AtomicSafety");
		if (!templateCreateResult.Success)
		{
			return false;
		}
		string path = FindCreated(templateCreateResult, "AtomicSafetyComponent.cs");
		File.AppendAllText(path, "\n// OLD_PACKAGE_MUST_SURVIVE_IN_BACKUP\n");
		File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
		string text = "";
		try
		{
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = xWTemplateLibrary.CreateFromTemplateInDirectory("character-component", componentRoot, "AtomicSafety", overwrite: true);
			text = Directory.GetDirectories(componentRoot, "AtomicSafetyComponent.backup-*", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? "";
			string path2 = FindCreated(templateCreateResult2, "AtomicSafetyComponent.cs");
			string path3 = (string.IsNullOrWhiteSpace(text) ? "" : Path.Combine(text, "AtomicSafetyComponent.cs"));
			return templateCreateResult2.Success && !string.IsNullOrWhiteSpace(templateCreateResult2.Warning) && templateCreateResult2.CreatedPaths.Count == 3 && templateCreateResult2.CreatedPaths.All(File.Exists) && File.Exists(path2) && !File.ReadAllText(path2).Contains("OLD_PACKAGE_MUST_SURVIVE_IN_BACKUP", StringComparison.Ordinal) && File.Exists(path3) && File.ReadAllText(path3).Contains("OLD_PACKAGE_MUST_SURVIVE_IN_BACKUP", StringComparison.Ordinal);
		}
		finally
		{
			ClearReadOnlyAndDelete(text);
			ClearReadOnlyAndDelete(Path.Combine(componentRoot, "AtomicSafetyComponent"));
		}
	}

	private static void ClearReadOnlyAndDelete(string directory)
	{
		if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
		{
			string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
			for (int i = 0; i < files.Length; i++)
			{
				File.SetAttributes(files[i], FileAttributes.Normal);
			}
			Directory.Delete(directory, recursive: true);
		}
	}

	private CharacterComponentRuntime GetEditorRuntimePreview()
	{
		return typeof(XWCharacterComponentVisualResourceEditor).GetField("_runtimePreview", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_editor) as CharacterComponentRuntime;
	}

	private List<Type> GetEditorComponentTypes()
	{
		return (typeof(XWCharacterComponentVisualResourceEditor).GetField("_componentTypes", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_editor) as IEnumerable<Type>)?.ToList() ?? new List<Type>();
	}

	private static T FindPropertyControl<T>(Node root, string propertyName) where T : Control
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is HBoxContainer hBoxContainer && hBoxContainer.TooltipText.StartsWith(propertyName, StringComparison.Ordinal))
		{
			return FindDescendant<T>(hBoxContainer);
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindPropertyControl<T>(child, propertyName);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static T FindDescendant<T>(Node root) where T : Control
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is T result)
			{
				return result;
			}
			T val = FindDescendant<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
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
			GD.PrintErr("[MOD_EDITOR_MODERN_COMPONENT_CREATE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish(bool f3, bool routeCreate, bool files, bool modern, bool collision, bool manifest, bool backgroundCompile, bool compiled, bool types, bool factory, bool editorRoute, bool resourceReload, bool inspectorFree, bool cardAndAdd, bool configuration, bool atomicCleanup, bool rollback, bool runtimeRelease, bool catalogOnly, bool hotRefresh, bool unload)
	{
		GD.Print($"[MOD_EDITOR_MODERN_COMPONENT_CREATE_PROBE] f3={f3} routeCreate={routeCreate} files={files} modern={modern} collision={collision} manifest={manifest} backgroundCompile={backgroundCompile} compiled={compiled} types={types} factory={factory} editorRoute={editorRoute} resourceReload={resourceReload} inspectorFree={inspectorFree} card={cardAndAdd} add={cardAndAdd} configuration={configuration} atomicCleanup={atomicCleanup} rollback={rollback} runtimeRelease={runtimeRelease} catalogOnly={catalogOnly} hotRefresh={hotRefresh} unload={unload} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		int exitCode = ((_failures.Count != 0) ? 1 : 0);
		GetTree().Quit(exitCode);
	}
}
