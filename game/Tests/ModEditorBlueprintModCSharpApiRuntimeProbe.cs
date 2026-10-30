using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorBlueprintModCSharpApiRuntimeProbe : Node
{
	private const string QualifiedPlayerApi = "ProbeMod.Api.PlayerPuzzleApi";

	private readonly List<string> _failures = new List<string>();

	private string _probeParent = "";

	private XWBPEditor _blueprintEditor;

	public override async void _Ready()
	{
		bool window = false;
		bool methodCard = false;
		bool propertyCards = false;
		bool signalCards = false;
		bool portFilter = false;
		bool nodeCreated = false;
		bool generated = false;
		bool generationSemantics = false;
		bool backgroundIndex = false;
		bool backgroundCompile = false;
		bool compiled = false;
		bool saveReload = false;
		bool hostInheritance = false;
		bool hostApi = false;
		bool excluded = false;
		bool createRefresh = false;
		bool saveRefresh = false;
		bool modBVisible = false;
		bool modAIsolated = false;
		bool inspectorUntouched = false;
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
			SendF3();
			ModEditorPanel panel = await WaitForEditorPanel(900);
			window = GodotObject.IsInstanceValid(panel);
			Require(window, "F3 did not initialize the real ModEditorPanel.");
			if (!window)
			{
				Finish();
				return;
			}
			await WaitFrames(60);
			_blueprintEditor = XWEditorInterface.Instance?.GetBlueprintEditor() as XWBPEditor;
			XWScriptEditor scriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			XWFileSystemPanel fileSystemPanel = XWEditorInterface.Instance?.GetFileSystemPanel() as XWFileSystemPanel;
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Require(GodotObject.IsInstanceValid(_blueprintEditor), "The real F3 Blueprint editor is unavailable.");
			Require(GodotObject.IsInstanceValid(scriptEditor), "The real F3 C# Script editor is unavailable.");
			Require(GodotObject.IsInstanceValid(fileSystemPanel), "The real F3 FileSystem panel is unavailable.");
			Require(GodotObject.IsInstanceValid(inspector), "The real F3 Inspector is unavailable.");
			if (!GodotObject.IsInstanceValid(_blueprintEditor) || !GodotObject.IsInstanceValid(scriptEditor) || !GodotObject.IsInstanceValid(fileSystemPanel))
			{
				Finish();
				return;
			}
			_probeParent = ProjectSettings.GlobalizePath("user://BlueprintModCSharpApiProbe");
			TryDeleteProbeRoot(_probeParent);
			ModProject modA = ModProject.Create(_probeParent, "ModA", "1.0.0", "probe", "Blueprint active-Mod C# API probe");
			ModProject modB = ModProject.Create(_probeParent, "ModB", "1.0.0", "probe", "Blueprint active-Mod C# API isolation probe");
			Require(modA != null && modB != null, "Temporary Mod A/B projects could not be created.");
			if (modA == null || modB == null)
			{
				Finish();
				return;
			}
			string playerApiPath = Path.Combine(modA.ProjectPath, "Scripts", "PlayerPuzzleApi.cs");
			File.WriteAllText(playerApiPath, BuildPlayerApiSource(includeSavedMethod: false));
			File.WriteAllText(Path.Combine(modA.ProjectPath, "Scripts", "BulkIndexProbe.cs"), "/*" + new string('x', 4000000) + "*/\n");
			WriteIgnoredFixtures(modA.ProjectPath);
			File.WriteAllText(Path.Combine(modB.ProjectPath, "Scripts", "ModBOnlyApi.cs"), "using Godot;\npublic partial class ModBOnlyApi : Node\n{\n    public int ModBValue(int value) => value;\n}\n");
			MethodInfo enterProject = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			bool flag = modA != null && (bool)(enterProject?.Invoke(panel, new object[1] { modA }) ?? ((object)false));
			Require(flag, "Could not enter temporary Mod A through the real F3 project route.");
			if (!flag)
			{
				Finish();
				return;
			}
			XWBPCSharpMemberRegistry registry = XWBPCSharpMemberRegistry.Instance;
			Task<bool> indexTask = registry.EnsureProjectScannedAsync();
			bool indexReturnedPending = !indexTask.IsCompleted;
			int indexLiveFrames = 0;
			ulong indexDeadline = Time.GetTicksMsec() + 30000;
			while (!indexTask.IsCompleted && Time.GetTicksMsec() < indexDeadline)
			{
				await WaitFrames(1);
				indexLiveFrames++;
			}
			bool flag2 = indexTask.IsCompleted;
			if (flag2)
			{
				flag2 = await indexTask;
			}
			bool flag3 = flag2;
			backgroundIndex = indexReturnedPending && indexLiveFrames > 0 && flag3;
			Require(backgroundIndex, "The active Mod C# API index did not yield the F3 main thread.");
			await WaitFrames(2);
			XWBPCSharpMemberRegistry.CSharpClassDescriptor cSharpClassDescriptor = FindClassDescriptor(registry, "HostDerivedApi");
			hostInheritance = cSharpClassDescriptor != null && cSharpClassDescriptor.CanInherit && string.Equals(cSharpClassDescriptor.BaseClass, "ComponentManager", StringComparison.Ordinal);
			Require(hostInheritance, "A Mod class derived from the host ComponentManager was incorrectly blocked from Blueprint inheritance.");
			Require(await WaitForClass(registry, "PlayerPuzzleApi", expected: true, 180), "Mod A PlayerPuzzleApi did not enter the Blueprint C# catalog.");
			bool flag4 = HasMember(registry.GetClassMethodList("PlayerPuzzleApi"), "ComputeSun");
			bool flag5 = HasMember(registry.GetClassPropertyList("PlayerPuzzleApi"), "Bonus");
			bool flag6 = HasMember(registry.GetClassSignalList("PlayerPuzzleApi"), "SunChanged");
			Require(flag4 && flag5 && flag6, "Mod A method/property/signal members were not all indexed.");
			excluded = !registry.HasClass("IgnoredBinApi") && !registry.HasClass("IgnoredObjApi") && !registry.HasClass("IgnoredGodotApi") && !registry.HasClass("IgnoredGeneratedApi") && !registry.HasClass("IgnoredPair");
			Require(excluded, "bin/obj/.godot/generated or *.generated.cs entered the Blueprint C# catalog.");
			Node sentinel = new Node
			{
				Name = "BlueprintModCSharpApiInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(sentinel);
			await WaitFrames(2);
			string blueprintPath = Path.Combine(modA.ProjectPath, "Resources", "PlayerPuzzleApi.tres");
			XWBlueprintCreationService.Result result = XWBlueprintCreationService.Create(blueprintPath, "ProbeMod.Api.PlayerPuzzleApi", "PlayerPuzzleApi");
			Require(result.Success, "Could not create the Mod A Blueprint fixture.");
			if (!result.Success)
			{
				Finish();
				return;
			}
			XWBPScript xWBPScript = ResourceLoader.Load<XWBPScript>(blueprintPath, "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(xWBPScript), "The persisted Mod A Blueprint fixture could not be reopened.");
			_blueprintEditor.Init(xWBPScript);
			await WaitFrames(6);
			XWBPGraphData graph = FirstGraph(_blueprintEditor.BpScriptData);
			Require(graph != null, "The Mod A Blueprint fixture has no editable graph.");
			if (graph == null)
			{
				Finish();
				return;
			}
			XWBPNodePortData portData = new XWBPNodePortData("位置", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Vector2, "", Vector2.Zero);
			XWWindowBPNodeSelector xWWindowBPNodeSelector = await OpenSelector(graph, "", portData);
			int num = (xWWindowBPNodeSelector?.GetNodeOrNull<HFlowContainer>("%NodeCardGrid"))?.GetChildCount() ?? 0;
			Tree tree = xWWindowBPNodeSelector?.GetNodeOrNull<Tree>("%Tree");
			TreeItem treeItem = FindTreeItem(tree, "EchoPosition");
			TreeItem treeItem2 = FindTreeItem(tree, "ComputeSun");
			portFilter = num > 0 && num <= 36 && HasCard(xWWindowBPNodeSelector, "EchoPosition") && !HasCard(xWWindowBPNodeSelector, "ComputeSun") && GodotObject.IsInstanceValid(treeItem) && treeItem.Visible && treeItem.IsSelectable(0) && (!GodotObject.IsInstanceValid(treeItem2) || !treeItem2.Visible || !treeItem2.IsSelectable(0));
			Require(portFilter, "Deferred Blueprint members ignored the port compatibility filter or exceeded the 36-card materialization budget.");
			CloseSelector(xWWindowBPNodeSelector);
			await WaitFrames(2);
			XWWindowBPNodeSelector methodSelector = await OpenSelector(graph, "ComputeSun");
			Button button = FindCard(methodSelector, "ComputeSun");
			methodCard = GodotObject.IsInstanceValid(button);
			Require(methodCard, "ComputeSun is not visible as a concrete visual node card.");
			if (methodCard)
			{
				XWBPNodeType selectedType = null;
				int beforeCount = graph.Nodes.Count;
				methodSelector.NodeTypeSelected += delegate(XWBPNodeType nodeType)
				{
					selectedType = nodeType;
					_blueprintEditor.GetGraphEditor().AddNode(nodeType, new Vector2(260f, 170f));
				};
				button.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				methodSelector.EmitSignal(AcceptDialog.SignalName.Confirmed);
				await WaitFrames(6);
				nodeCreated = selectedType is XWBPNodeCallMethod { MethodType: XWBPNodeCallMethod.Type.Script } xWBPNodeCallMethod && MemberName(xWBPNodeCallMethod.MethodData) == "ComputeSun" && graph.Nodes.Count == beforeCount + 1;
			}
			else
			{
				CloseSelector(methodSelector);
				await WaitFrames(2);
			}
			Require(nodeCreated, "Selecting ComputeSun did not create its real Blueprint call node.");
			XWWindowBPNodeSelector selector = await OpenSelector(graph, "Bonus");
			propertyCards = HasCard(selector, "获取 Bonus") && HasCard(selector, "设置 Bonus");
			Require(propertyCards, "Bonus getter/setter are not concrete visual node cards.");
			CloseSelector(selector);
			await WaitFrames(2);
			XWWindowBPNodeSelector selector2 = await OpenSelector(graph, "SunChanged");
			signalCards = HasCard(selector2, "事件 SunChanged") && HasCard(selector2, "发射 SunChanged");
			Require(signalCards, "SunChanged event/emit are not concrete visual node cards.");
			CloseSelector(selector2);
			await WaitFrames(2);
			string error;
			bool flag7 = TryBuildGenerationGraph(_blueprintEditor.BpScriptData, graph, registry, out error);
			Require(flag7, error);
			bool flag8 = _blueprintEditor.FlushBlueprintPersistence();
			XWBPScript xWBPScript2 = (flag8 ? ResourceLoader.Load<XWBPScript>(blueprintPath, "", ResourceLoader.CacheMode.Ignore) : null);
			XWBPGraphData graph2 = (GodotObject.IsInstanceValid(xWBPScript2) ? FirstGraph(xWBPScript2.Deserialize()) : null);
			saveReload = flag8 && HasPersistedMethodNode(graph2, "ComputeSun");
			Require(saveReload, "The created ComputeSun node did not survive Blueprint save/reload.");
			string generatedSource = (GodotObject.IsInstanceValid(xWBPScript2) ? xWBPScript2.GenerateCode() : "");
			generationSemantics = generatedSource.Contains(": ProbeMod.Api.PlayerPuzzleApi", StringComparison.Ordinal) && generatedSource.Contains("this.ComputeSun(", StringComparison.Ordinal) && generatedSource.Contains("this.ComputeScale(", StringComparison.Ordinal) && generatedSource.Contains("ProbeMod.Api.PlayerPuzzleApi.StaticBonus(", StringComparison.Ordinal) && generatedSource.Contains("this.EchoToken(", StringComparison.Ordinal) && generatedSource.Contains("__XWBlueprintConvertValue<global::System.Guid>", StringComparison.Ordinal) && generatedSource.Contains("__XWBlueprintConvertValue<global::ProbeMod.Api.PuzzleMode>", StringComparison.Ordinal) && generatedSource.Contains("__XWBlueprintConvertValue<global::ProbeMod.Api.PuzzleToken>", StringComparison.Ordinal) && generatedSource.Contains("__XWBlueprintConvertValue<global::ProbeMod.Api.IPuzzleRule>", StringComparison.Ordinal) && generatedSource.Contains("this.Bonus =", StringComparison.Ordinal) && generatedSource.Contains("this.Scale =", StringComparison.Ordinal);
			generated = flag7 && !string.IsNullOrWhiteSpace(generatedSource) && generationSemantics;
			Require(generated, "The persisted Blueprint did not generate all required Mod C# API calls.");
			string path = Path.Combine(modA.ProjectPath, "Scripts", "PlayerPuzzleApiBlueprint.generated.cs");
			if (generated)
			{
				File.WriteAllText(path, generatedSource);
			}
			Require(File.Exists(path), "The generated Blueprint C# source was not written into Mod A.");
			XWScriptCompiler.CompileResult generatedCompile = null;
			if (File.Exists(path))
			{
				Task<XWScriptCompiler.CompileResult> compileTask = XWScriptCompiler.CompileModProjectAsync(modA.ProjectPath);
				bool returnedPending = !compileTask.IsCompleted;
				int beforeCount = 0;
				ulong compileDeadline = Time.GetTicksMsec() + 60000;
				while (!compileTask.IsCompleted && Time.GetTicksMsec() < compileDeadline)
				{
					await WaitFrames(1);
					beforeCount++;
				}
				backgroundCompile = returnedPending && beforeCount > 0;
				if (compileTask.IsCompleted)
				{
					generatedCompile = await compileTask;
				}
			}
			compiled = generatedCompile != null && generatedCompile.Success && generatedCompile.IsModProject && generatedCompile.ExitCode == 0 && generatedCompile.ScriptCount >= 2 && !string.IsNullOrWhiteSpace(generatedCompile.OutputAssemblyPath) && File.Exists(generatedCompile.OutputAssemblyPath) && !generatedCompile.Diagnostics.Exists((XWCodeErrorChecker.ErrorData diagnostic) => diagnostic.SeverityLevel == XWCodeErrorChecker.Severity.Error);
			Require(backgroundCompile, "The generated Mod C# dotnet build did not yield the F3 main thread.");
			Require(compiled, "dotnet failed to compile the generated Blueprint and its Mod C# parent.");
			if (!compiled)
			{
				GD.Print($"[MOD_EDITOR_BLUEPRINT_MOD_CSHARP_API_COMPILE] success={generatedCompile?.Success} exit={generatedCompile?.ExitCode} scripts={generatedCompile?.ScriptCount} project={generatedCompile?.ProjectPath} assembly={generatedCompile?.OutputAssemblyPath}");
				foreach (XWCodeErrorChecker.ErrorData item in generatedCompile?.Diagnostics ?? new List<XWCodeErrorChecker.ErrorData>())
				{
					GD.PrintErr($"[MOD_EDITOR_BLUEPRINT_MOD_CSHARP_API_COMPILE_DIAGNOSTIC] {item.FilePath}:{item.Line + 1}:{item.Column + 1} {item.Code} {item.Message}");
				}
				GD.PrintErr("[MOD_EDITOR_BLUEPRINT_MOD_CSHARP_API_GENERATED_SOURCE]\n" + generatedSource);
			}
			typeof(XWFileSystemPanel).GetMethod("ShowNewScriptDialog", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(fileSystemPanel, new object[2]
			{
				Path.Combine(modA.ProjectPath, "Scripts"),
				true
			});
			await WaitFrames(4);
			ConfirmationDialog confirmationDialog = FindCreateScriptDialog(fileSystemPanel);
			LineEdit lineEdit = confirmationDialog?.GetNodeOrNull<LineEdit>("%NameEdit");
			Require(GodotObject.IsInstanceValid(confirmationDialog) && GodotObject.IsInstanceValid(lineEdit), "The real FileSystem C# creation dialog did not open.");
			if (GodotObject.IsInstanceValid(confirmationDialog) && GodotObject.IsInstanceValid(lineEdit))
			{
				lineEdit.Text = "CreatedAfterOpenApi";
				confirmationDialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
				string path2 = Path.Combine(modA.ProjectPath, "Scripts", "CreatedAfterOpenApi.cs");
				flag2 = await WaitForFile(path2, 180);
				if (flag2)
				{
					flag2 = await WaitForClass(registry, "CreatedAfterOpenApi", expected: true, 180);
				}
				createRefresh = flag2;
			}
			Require(createRefresh, "A C# file created after opening Mod A did not refresh the Blueprint API catalog.");
			bool scriptOpened = scriptEditor.TryOpenFile(playerApiPath);
			await WaitFrames(5);
			XWCodeEdit xWCodeEdit = FindNodeOfType<XWCodeEdit>(scriptEditor);
			Require(scriptOpened && GodotObject.IsInstanceValid(xWCodeEdit), "PlayerPuzzleApi.cs could not be opened in the real C# Script editor.");
			if (scriptOpened && GodotObject.IsInstanceValid(xWCodeEdit))
			{
				xWCodeEdit.Text = BuildPlayerApiSource(includeSavedMethod: true);
				xWCodeEdit.EmitSignal(TextEdit.SignalName.TextChanged);
				flag2 = scriptEditor.SaveFile();
				if (flag2)
				{
					flag2 = await WaitForMethod(registry, "PlayerPuzzleApi", "SavedAfterOpen", 180);
				}
				saveRefresh = flag2;
			}
			Require(saveRefresh, "Saving PlayerPuzzleApi.cs did not refresh the Blueprint API catalog.");
			bool flag9 = (bool)(enterProject?.Invoke(panel, new object[1] { modB }) ?? ((object)false));
			Require(flag9, "Could not switch from Mod A to Mod B through the real F3 project route.");
			if (flag9)
			{
				await WaitFrames(10);
				bool returnedPending = await WaitForClass(registry, "ModBOnlyApi", expected: true, 180);
				bool aRemoved = await WaitForClass(registry, "PlayerPuzzleApi", expected: false, 180);
				bool createdRemoved = !registry.HasClass("CreatedAfterOpenApi");
				string path3 = Path.Combine(modB.ProjectPath, "Resources", "ModBOnlyApi.tres");
				XWBPScript xWBPScript3 = (XWBlueprintCreationService.Create(path3, "ModBOnlyApi", "ModBOnlyApi").Success ? ResourceLoader.Load<XWBPScript>(path3, "", ResourceLoader.CacheMode.Ignore) : null);
				Require(GodotObject.IsInstanceValid(xWBPScript3), "The persisted Mod B Blueprint fixture could not be reopened.");
				_blueprintEditor.Init(xWBPScript3);
				await WaitFrames(4);
				XWBPGraphData modBGraph = FirstGraph(_blueprintEditor.BpScriptData);
				XWWindowBPNodeSelector selector3 = await OpenSelector(modBGraph, "ModBValue");
				modBVisible = returnedPending && HasCard(selector3, "ModBValue");
				CloseSelector(selector3);
				await WaitFrames(2);
				XWWindowBPNodeSelector selector4 = await OpenSelector(modBGraph, "ComputeSun");
				bool staleACardAbsent = !HasCard(selector4, "ComputeSun");
				CloseSelector(selector4);
				await WaitFrames(2);
				modAIsolated = aRemoved && createdRemoved && staleACardAbsent;
			}
			Require(modBVisible, "Mod B C# API is not visible after the real F3 project switch.");
			Require(modAIsolated, "Mod A C# API remained visible after switching to Mod B.");
			bool hostClassRetained = registry.HasClass("ComponentManager") && HasMember(registry.GetClassMethodList("ComponentManager"), "InitializeResourceComponents");
			string path4 = Path.Combine(modB.ProjectPath, "Resources", "HostComponentManager.tres");
			XWBPScript xWBPScript4 = (XWBlueprintCreationService.Create(path4, "ComponentManager", "HostComponentManager").Success ? ResourceLoader.Load<XWBPScript>(path4, "", ResourceLoader.CacheMode.Ignore) : null);
			Require(GodotObject.IsInstanceValid(xWBPScript4), "The persisted host-API Blueprint fixture could not be reopened.");
			_blueprintEditor.Init(xWBPScript4);
			await WaitFrames(4);
			XWBPGraphData graph3 = FirstGraph(_blueprintEditor.BpScriptData);
			XWWindowBPNodeSelector selector5 = await OpenSelector(graph3, "InitializeResourceComponents");
			hostApi = hostClassRetained && HasCard(selector5, "InitializeResourceComponents");
			CloseSelector(selector5);
			Require(hostApi, "Host ComponentManager API disappeared while the active Mod root changed.");
			await WaitFrames(2);
			inspectorUntouched = inspector == null || inspector.CurrentObject == sentinel;
			Require(inspectorUntouched, "Blueprint API selection occupied or replaced the raw Inspector.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_BLUEPRINT_MOD_CSHARP_API_PROBE] window={window} methodCard={methodCard} propertyCards={propertyCards} signalCards={signalCards} portFilter={portFilter} nodeCreated={nodeCreated} generated={generated} generationSemantics={generationSemantics} backgroundIndex={backgroundIndex} backgroundCompile={backgroundCompile} compiled={compiled} saveReload={saveReload} hostInheritance={hostInheritance} hostApi={hostApi} excluded={excluded} createRefresh={createRefresh} saveRefresh={saveRefresh} modBVisible={modBVisible} modAIsolated={modAIsolated} inspectorUntouched={inspectorUntouched} failures={_failures.Count}");
		Finish();
	}

	private static bool TryBuildGenerationGraph(XWBPScriptData scriptData, XWBPGraphData graph, XWBPCSharpMemberRegistry registry, out string error)
	{
		error = "";
		if (scriptData == null || graph == null || registry == null)
		{
			error = "The generated-code fixture is missing its Blueprint graph or C# registry.";
			return false;
		}
		List<Dictionary> classMethodList = registry.GetClassMethodList("ProbeMod.Api.PlayerPuzzleApi");
		List<Dictionary> classPropertyList = registry.GetClassPropertyList("ProbeMod.Api.PlayerPuzzleApi");
		Dictionary dictionary = FindMember(classMethodList, "_ready");
		Dictionary dictionary2 = FindMember(classMethodList, "ComputeSun");
		Dictionary dictionary3 = FindMember(classMethodList, "ComputeScale");
		Dictionary dictionary4 = FindMember(classMethodList, "StaticBonus");
		Dictionary dictionary5 = FindMember(classMethodList, "EchoToken");
		Dictionary dictionary6 = FindMember(classMethodList, "EchoMode");
		Dictionary dictionary7 = FindMember(classMethodList, "EchoPuzzleToken");
		Dictionary dictionary8 = FindMember(classMethodList, "EchoRule");
		Dictionary dictionary9 = FindMember(classPropertyList, "Bonus");
		Dictionary dictionary10 = FindMember(classPropertyList, "Scale");
		if (dictionary == null || dictionary2 == null || dictionary3 == null || dictionary4 == null || dictionary5 == null || dictionary6 == null || dictionary7 == null || dictionary8 == null || dictionary9 == null || dictionary10 == null)
		{
			error = "The generation fixture could not resolve _Ready, primitive/System/custom namespace methods, static method, or properties.";
			return false;
		}
		scriptData.Variables.Clear();
		scriptData.NextVariableId = 1;
		XWBPVariableData xWBPVariableData = new XWBPVariableData
		{
			Name = "IntegerInput",
			Type = Variant.Type.Int,
			DefaultValue = Variant.From<long>(7L),
			Owner = scriptData
		};
		XWBPVariableData xWBPVariableData2 = new XWBPVariableData
		{
			Name = "FloatInput",
			Type = Variant.Type.Float,
			DefaultValue = Variant.From<double>(1.25),
			Owner = scriptData
		};
		scriptData.AddVariable(xWBPVariableData, 1);
		scriptData.AddVariable(xWBPVariableData2, 2);
		graph.Clear();
		graph.Owner = scriptData;
		AddProbeNode(graph, CreateVirtualEntry(dictionary), 1);
		AddProbeNode(graph, CreateVariableGetter(xWBPVariableData), 2);
		AddProbeNode(graph, CreateVariableGetter(xWBPVariableData2), 3);
		AddProbeNode(graph, CreateMethodCall(dictionary2, selfMember: true), 4);
		AddProbeNode(graph, CreateMethodCall(dictionary3, selfMember: true), 5);
		AddProbeNode(graph, CreateMethodCall(dictionary4, selfMember: false), 6);
		AddProbeNode(graph, CreatePropertySetter(dictionary9, selfMember: true), 7);
		AddProbeNode(graph, CreatePropertySetter(dictionary10, selfMember: true), 8);
		AddProbeNode(graph, CreateMethodCall(dictionary5, selfMember: true), 9);
		AddProbeNode(graph, CreateMethodCall(dictionary6, selfMember: true), 10);
		AddProbeNode(graph, CreateMethodCall(dictionary7, selfMember: true), 11);
		AddProbeNode(graph, CreateMethodCall(dictionary8, selfMember: true), 12);
		ConnectProbe(graph, 1, 0, 4, 0);
		ConnectProbe(graph, 4, 0, 5, 0);
		ConnectProbe(graph, 5, 0, 6, 0);
		ConnectProbe(graph, 6, 0, 9, 0);
		ConnectProbe(graph, 9, 0, 10, 0);
		ConnectProbe(graph, 10, 0, 11, 0);
		ConnectProbe(graph, 11, 0, 12, 0);
		ConnectProbe(graph, 12, 0, 7, 0);
		ConnectProbe(graph, 7, 0, 8, 0);
		ConnectProbeByName(graph, 2, "值", 4, "baseSun");
		ConnectProbeByName(graph, 3, "值", 5, "factor");
		ConnectProbeByName(graph, 2, "值", 6, "value");
		ConnectProbeByName(graph, 2, "值", 7, "值");
		ConnectProbeByName(graph, 3, "值", 8, "值");
		return true;
	}

	private static XWBPNodeData CreateVirtualEntry(Dictionary methodData)
	{
		XWBPNodeMethodEntry xWBPNodeMethodEntry = new XWBPNodeMethodEntry();
		xWBPNodeMethodEntry.MethodType = XWBPNodeMethodEntry.Type.Virtual;
		xWBPNodeMethodEntry.MethodData = methodData.Duplicate(deep: true);
		xWBPNodeMethodEntry.BuildVirtualMethod();
		return xWBPNodeMethodEntry.CreateNodeData();
	}

	private static XWBPNodeData CreateVariableGetter(XWBPVariableData variable)
	{
		XWBPNodeGetProperty xWBPNodeGetProperty = new XWBPNodeGetProperty();
		xWBPNodeGetProperty.MethodType = XWBPNodeGetProperty.Type.Bp;
		xWBPNodeGetProperty.VariableId = variable.Id;
		xWBPNodeGetProperty.BuildVariable(variable);
		return xWBPNodeGetProperty.CreateNodeData();
	}

	private static XWBPNodeData CreateMethodCall(Dictionary methodData, bool selfMember)
	{
		Dictionary dictionary = methodData.Duplicate(deep: true);
		if (selfMember)
		{
			dictionary["xw_self_member"] = true;
		}
		XWBPNodeCallMethod xWBPNodeCallMethod = new XWBPNodeCallMethod();
		xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Script;
		xWBPNodeCallMethod.MethodData = dictionary;
		xWBPNodeCallMethod.BuildMethod();
		return xWBPNodeCallMethod.CreateNodeData();
	}

	private static XWBPNodeData CreatePropertySetter(Dictionary propertyData, bool selfMember)
	{
		Dictionary dictionary = propertyData.Duplicate(deep: true);
		if (selfMember)
		{
			dictionary["xw_self_member"] = true;
		}
		XWBPNodeSetProperty xWBPNodeSetProperty = new XWBPNodeSetProperty();
		xWBPNodeSetProperty.MethodType = XWBPNodeSetProperty.Type.Script;
		xWBPNodeSetProperty.PropertyData = dictionary;
		xWBPNodeSetProperty.BuildProperty();
		return xWBPNodeSetProperty.CreateNodeData();
	}

	private static Dictionary FindMember(IReadOnlyList<Dictionary> members, string expected)
	{
		foreach (Dictionary member in members)
		{
			if (string.Equals(MemberName(member), expected, StringComparison.Ordinal) || (expected == "_ready" && string.Equals(MemberName(member), "_Ready", StringComparison.Ordinal)))
			{
				return member;
			}
		}
		return null;
	}

	private static XWBPCSharpMemberRegistry.CSharpClassDescriptor FindClassDescriptor(XWBPCSharpMemberRegistry registry, string expectedName)
	{
		foreach (XWBPCSharpMemberRegistry.CSharpClassDescriptor projectClassDescriptor in registry.GetProjectClassDescriptors())
		{
			if (string.Equals(projectClassDescriptor.Name, expectedName, StringComparison.Ordinal))
			{
				return projectClassDescriptor;
			}
		}
		return null;
	}

	private static void AddProbeNode(XWBPGraphData graph, XWBPNodeData node, int id)
	{
		node.Id = id;
		node.Position = new Vector2(id * 80, id * 45);
		graph.AddNodePreserveId(node);
	}

	private static void ConnectProbe(XWBPGraphData graph, int fromNode, int fromPort, int toNode, int toPort)
	{
		if (!graph.AddConnection(fromNode, fromPort, toNode, toPort))
		{
			throw new InvalidOperationException($"Could not connect generated-code fixture {fromNode}:{fromPort} -> {toNode}:{toPort}.");
		}
	}

	private static void ConnectProbeByName(XWBPGraphData graph, int fromNode, string fromPortName, int toNode, string toPortName)
	{
		XWBPNodeData node = graph.GetNode(fromNode);
		XWBPNodeData node2 = graph.GetNode(toNode);
		int num = node?.OutputPorts.FindIndex((XWBPNodePortData port) => port.Name == fromPortName) ?? (-1);
		int num2 = node2?.InputPorts.FindIndex((XWBPNodePortData port) => port.Name == toPortName) ?? (-1);
		if (num < 0 || num2 < 0)
		{
			throw new InvalidOperationException($"Could not resolve generated-code fixture ports '{fromPortName}' -> '{toPortName}'.");
		}
		ConnectProbe(graph, fromNode, num, toNode, num2);
	}

	private async Task<XWWindowBPNodeSelector> OpenSelector(XWBPGraphData graph, string query, XWBPNodePortData portData = null)
	{
		if (graph == null)
		{
			return null;
		}
		XWWindowBPNodeSelector selector = XWWindowBPNodeSelector.Create();
		selector.Editor = _blueprintEditor;
		selector.CurrentGraph = graph;
		if (GodotObject.IsInstanceValid(portData))
		{
			selector.SetPortFilter(portData, sourceIsOutput: true);
		}
		_blueprintEditor.AddChild(selector, forceReadableName: false, InternalMode.Disabled);
		selector.PopupCentered();
		await WaitFrames(5);
		selector.Search(query);
		await WaitFrames(4);
		return selector;
	}

	private static Button FindCard(XWWindowBPNodeSelector selector, string text)
	{
		if (!GodotObject.IsInstanceValid(selector))
		{
			return null;
		}
		HFlowContainer nodeOrNull = selector.GetNodeOrNull<HFlowContainer>("%NodeCardGrid");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return null;
		}
		foreach (Node child in nodeOrNull.GetChildren())
		{
			if (child is Button button && button.Text.Contains(text, StringComparison.Ordinal))
			{
				return button;
			}
		}
		return null;
	}

	private static bool HasCard(XWWindowBPNodeSelector selector, string text)
	{
		return GodotObject.IsInstanceValid(FindCard(selector, text));
	}

	private static TreeItem FindTreeItem(Tree tree, string text)
	{
		if (!GodotObject.IsInstanceValid(tree))
		{
			return null;
		}
		TreeItem treeItem = tree.GetRoot();
		while (GodotObject.IsInstanceValid(treeItem))
		{
			if (treeItem.GetText(0).Contains(text, StringComparison.Ordinal))
			{
				return treeItem;
			}
			treeItem = treeItem.GetNextInTree();
		}
		return null;
	}

	private static void CloseSelector(XWWindowBPNodeSelector selector)
	{
		if (GodotObject.IsInstanceValid(selector))
		{
			selector.QueueFree();
		}
	}

	private static bool HasMember(IReadOnlyList<Dictionary> members, string expected)
	{
		foreach (Dictionary member in members)
		{
			if (MemberName(member) == expected)
			{
				return true;
			}
		}
		return false;
	}

	private static string MemberName(Dictionary member)
	{
		if (member == null)
		{
			return "";
		}
		if (member.TryGetValue("cs_name", out var value))
		{
			return value.AsString();
		}
		if (!member.TryGetValue("name", out var value2))
		{
			return "";
		}
		return value2.AsString();
	}

	private static bool HasPersistedMethodNode(XWBPGraphData graph, string expectedMethod)
	{
		if (graph == null)
		{
			return false;
		}
		foreach (XWBPNodeData value in graph.Nodes.Values)
		{
			if (value != null && !(value.TypeId.ToString() != "__XWBPGraphNode_CallMethod"))
			{
				Variant metaData = value.GetMetaData("MethodData");
				if (metaData.VariantType == Variant.Type.Dictionary && MemberName(metaData.As<Dictionary>()) == expectedMethod)
				{
					return true;
				}
			}
		}
		return false;
	}

	private async Task<bool> WaitForClass(XWBPCSharpMemberRegistry registry, string className, bool expected, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (registry.HasClass(className) == expected)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForMethod(XWBPCSharpMemberRegistry registry, string className, string methodName, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (HasMember(registry.GetClassMethodList(className), methodName))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForFile(string path, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (File.Exists(path))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<ModEditorPanel> WaitForEditorPanel(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			if (GodotObject.IsInstanceValid(modEditorPanel) && modEditorPanel.IsInsideTree())
			{
				return modEditorPanel;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static ConfirmationDialog FindCreateScriptDialog(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is ConfirmationDialog confirmationDialog && GodotObject.IsInstanceValid(confirmationDialog.GetNodeOrNull<LineEdit>("%NameEdit")))
		{
			return confirmationDialog;
		}
		foreach (Node child in root.GetChildren())
		{
			ConfirmationDialog confirmationDialog2 = FindCreateScriptDialog(child);
			if (GodotObject.IsInstanceValid(confirmationDialog2))
			{
				return confirmationDialog2;
			}
		}
		return null;
	}

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static XWBPGraphData FirstGraph(XWBPScriptData data)
	{
		if (data == null)
		{
			return null;
		}
		using (System.Collections.Generic.Dictionary<int, XWBPGraphData>.ValueCollection.Enumerator enumerator = data.Graphs.Values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	private static void SendF3()
	{
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
	}

	private static string BuildPlayerApiSource(bool includeSavedMethod)
	{
		return "using System;\nusing Godot;\nnamespace ProbeMod.Api;\npublic enum PuzzleMode { Normal, Boost }\npublic readonly struct PuzzleToken { }\npublic interface IPuzzleRule { }\npublic partial class HostDerivedApi : ComponentManager\n{\n    public int HostValue() => 1;\n}\npublic partial class PlayerPuzzleApi : Node\n{\n    [Signal] public delegate void SunChangedEventHandler(int value);\n    public int Bonus { get; set; } = 2;\n    public float Scale { get; set; } = 1.25f;\n    public int ComputeSun(int baseSun) => baseSun + Bonus;\n    public float ComputeScale(float factor) => factor * Scale;\n    public static int StaticBonus(int value) => value + 3;\n    public Guid EchoToken(Guid token) => token;\n    public Vector2 EchoPosition(Vector2 position) => position;\n    public PuzzleMode EchoMode(PuzzleMode mode) => mode;\n    public PuzzleToken EchoPuzzleToken(PuzzleToken token) => token;\n    public IPuzzleRule EchoRule(IPuzzleRule rule) => rule;\n" + (includeSavedMethod ? "    public int SavedAfterOpen(int value) => value + 1;\n" : "") + "}\n";
	}

	private static void WriteIgnoredFixtures(string projectRoot)
	{
		System.Collections.Generic.Dictionary<string, string> obj = new System.Collections.Generic.Dictionary<string, string>
		{
			[Path.Combine(projectRoot, "bin", "IgnoredBinApi.cs")] = "public class IgnoredBinApi { }\n",
			[Path.Combine(projectRoot, "obj", "IgnoredObjApi.cs")] = "public class IgnoredObjApi { }\n",
			[Path.Combine(projectRoot, ".godot", "IgnoredGodotApi.cs")] = "public class IgnoredGodotApi { }\n"
		};
		string value = Path.Combine(projectRoot, "generated", "IgnoredGeneratedApi.cs");
		obj[value] = "public class IgnoredGeneratedApi { }\n";
		string key4 = Path.Combine(projectRoot, "Scripts", "IgnoredPair.generated.cs");
		obj[key4] = "public class IgnoredPair { }\n";
		foreach (KeyValuePair<string, string> item in obj)
		{
			item.Deconstruct(out key4, out value);
			string path = key4;
			string contents = value;
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, contents);
		}
	}

	private static void TryDeleteProbeRoot(string path)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				string text = Path.GetFullPath(path).Replace('\\', '/');
				string value = Path.GetFullPath(ProjectSettings.GlobalizePath("user://")).Replace('\\', '/').TrimEnd('/') + "/";
				if (text.StartsWith(value, StringComparison.OrdinalIgnoreCase) && Directory.Exists(text))
				{
					Directory.Delete(text, recursive: true);
				}
			}
		}
		catch
		{
		}
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
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_MOD_CSHARP_API_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_MOD_CSHARP_API_PROBE_FAILURE] " + failure);
		}
		Console.Out.Flush();
		Console.Error.Flush();
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
