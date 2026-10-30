using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class AdobeAnimateRenderStaticArrayEscapeRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_RENDER_STATIC_ARRAY_ESCAPE_RESULT";

	private const int MaxAuditWaitPhysicsFrames = 180;

	private AdobeAnimateRenderBackend _originalBackend;

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			AdobeAnimateSprite getterSprite = CreatePeaSprite(new Vector2(360f, 260f));
			AdobeAnimateSprite setterSprite = CreatePeaSprite(new Vector2(720f, 260f));
			AdobeAnimateSprite firstMediaExposureSprite = CreatePeaSprite(new Vector2(540f, 420f));
			bool characterSceneSealed = ValidateCharacterPackedSceneSeal();
			await WaitProcessFrames(24);
			await WaitPhysicsFrames(3);
			bool defaultSealed = !ReadField<bool>(getterSprite, "_renderStaticLayerArrayEscaped") && !ReadField<bool>(getterSprite, "_renderStaticMediaArraysEscaped") && ReadField<ulong>(getterSprite, "_renderStaticStateNextAuditPhysicsFrame") == 18446744073709551615uL;
			ulong previousValue = ReadField<ulong>(getterSprite, "_cachedLayerVisibleSignature");
			Array<bool> escapedLayers = getterSprite.layerVisible;
			ulong getterDeadline = ReadField<ulong>(getterSprite, "_renderStaticStateNextAuditPhysicsFrame");
			bool getterArmed = escapedLayers.Count > 0 && ReadField<bool>(getterSprite, "_renderStaticLayerArrayEscaped") && (!ReadField<bool>(getterSprite, "_renderStaticStateCached") || ReadField<bool>(getterSprite, "_renderStaticArrayAuditSubmissionPending") || ReadField<ulong>(getterSprite, "_renderStaticStateNextAuditPhysicsFrame") != 18446744073709551615uL);
			escapedLayers[0] = !escapedLayers[0];
			bool getterMutationObserved = await WaitForFieldChange(getterSprite, "_cachedLayerVisibleSignature", previousValue);
			ulong getterFrameAfterWait = Engine.GetPhysicsFrames();
			bool getterSnapshotsMatchAfterWait = InvokeMethod<bool>(getterSprite, "RenderStaticArrayAuditSnapshotsMatch");
			bool unchangedAuditRetainedOutput = await VerifyUnchangedAuditRetainsOutput(getterSprite, escapedLayers);
			Array<bool> setterSource = (setterSprite.layerVisible = ReadField<Array<bool>>(setterSprite, "_layerVisible").Duplicate());
			await WaitForFieldChange(setterSprite, "_cachedLayerStateVersion", ReadField<int>(setterSprite, "_cachedLayerStateVersion"));
			ulong previousValue2 = ReadField<ulong>(setterSprite, "_cachedLayerVisibleSignature");
			bool setterArmed = setterSource.Count > 0 && ReadField<bool>(setterSprite, "_renderStaticLayerArrayEscaped");
			setterSource[0] = !setterSource[0];
			bool setterAliasMutationObserved = await WaitForFieldChange(setterSprite, "_cachedLayerVisibleSignature", previousValue2);
			int previousValue3 = ReadField<int>(getterSprite, "_mediaReplaceStateVersion");
			Array<bool> escapedMediaUse = getterSprite.mediaReplaceUse;
			bool mediaGetterArmed = ReadField<bool>(getterSprite, "_renderStaticMediaArraysEscaped") && ReadField<ulong>(getterSprite, "_renderStaticStateNextAuditPhysicsFrame") != 18446744073709551615uL;
			bool mediaMutationObserved = false;
			if (escapedMediaUse.Count > 0)
			{
				bool previousUse = escapedMediaUse[0];
				escapedMediaUse[0] = true;
				mediaMutationObserved = await WaitForFieldChange(getterSprite, "_mediaReplaceStateVersion", previousValue3);
				escapedMediaUse[0] = previousUse;
			}
			int previousValue4 = ReadField<int>(firstMediaExposureSprite, "_mediaReplaceStateVersion");
			Array<bool> mediaReplaceUse = firstMediaExposureSprite.mediaReplaceUse;
			Array<string> mediaReplaceAtlasPaths = firstMediaExposureSprite.mediaReplaceAtlasPaths;
			bool firstCombinedMediaMutationObserved = false;
			if (mediaReplaceUse.Count > 0 && mediaReplaceAtlasPaths.Count > 0)
			{
				mediaReplaceUse[0] = true;
				mediaReplaceAtlasPaths[0] = "res://icon.svg";
				firstCombinedMediaMutationObserved = await WaitForFieldChange(firstMediaExposureSprite, "_mediaReplaceStateVersion", previousValue4);
			}
			Array<string> mediaReplaceAtlasPaths2 = getterSprite.mediaReplaceAtlasPaths;
			bool combinedMediaPathMutationObserved = false;
			if (escapedMediaUse.Count > 0 && mediaReplaceAtlasPaths2.Count > 0)
			{
				InvokeMethod<object>(getterSprite, "CaptureRenderStaticArrayAuditSnapshots");
				bool flag = escapedMediaUse[0];
				string text = mediaReplaceAtlasPaths2[0];
				getterSprite.needMediaReplaceUpdate = false;
				escapedMediaUse[0] = !flag;
				mediaReplaceAtlasPaths2[0] = text + "#audit-combined";
				combinedMediaPathMutationObserved = !InvokeMethod<bool>(getterSprite, "RenderStaticArrayAuditSnapshotsMatch") && getterSprite.needMediaReplaceUpdate;
				escapedMediaUse[0] = flag;
				mediaReplaceAtlasPaths2[0] = text;
				getterSprite.needMediaReplaceUpdate = false;
			}
			RemoveChild(getterSprite);
			AddChild(getterSprite, forceReadableName: false, InternalMode.Disabled);
			await WaitProcessFrames(3);
			await WaitPhysicsFrames(3);
			ulong previousValue5 = ReadField<ulong>(getterSprite, "_cachedLayerVisibleSignature");
			escapedLayers[0] = !escapedLayers[0];
			bool flag2 = await WaitForFieldChange(getterSprite, "_cachedLayerVisibleSignature", previousValue5);
			bool flag3 = defaultSealed && unchangedAuditRetainedOutput && characterSceneSealed && getterArmed && getterMutationObserved && setterArmed && setterAliasMutationObserved && mediaGetterArmed && mediaMutationObserved && firstCombinedMediaMutationObserved && combinedMediaPathMutationObserved && flag2;
			GD.Print($"{"ADOBE_ANIMATE_RENDER_STATIC_ARRAY_ESCAPE_RESULT"} passed={flag3} default_sealed={defaultSealed} unchanged_audit_retained_output={unchangedAuditRetainedOutput} character_scene_sealed={characterSceneSealed} getter_armed={getterArmed} getter_mutation={getterMutationObserved} setter_armed={setterArmed} setter_alias_mutation={setterAliasMutationObserved} media_armed={mediaGetterArmed} media_mutation={mediaMutationObserved} first_media_combined_mutation={firstCombinedMediaMutationObserved} media_path_combined_mutation={combinedMediaPathMutationObserved} quick_readd_audit={flag2} getter_deadline={getterDeadline} getter_frame_after={getterFrameAfterWait} getter_snapshot_match_after={getterSnapshotsMatchAfterWait} getter_cached={ReadField<bool>(getterSprite, "_renderStaticStateCached")} getter_gpu_graph={ReadField<bool>(getterSprite, "_hasCachedGpuGraphCrowdState")} getter_runtime_manager={ReadField<bool>(getterSprite, "_usingRuntimeManager")} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag3) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_RENDER_STATIC_ARRAY_ESCAPE_RESULT"} exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
		}
		GetTree().Quit(exitCode);
	}

	private async Task<bool> VerifyUnchangedAuditRetainsOutput(AdobeAnimateSprite sprite, Array<bool> alias)
	{
		await WaitProcessFrames(3);
		long publishedFrame = ReadField<long>(sprite, "_runtimeNativeCanvasPublishedFrameVersion");
		ulong auditDeadline = ReadField<ulong>(sprite, "_renderStaticStateNextAuditPhysicsFrame");
		await WaitPhysicsFrames(180);
		ulong num = ReadField<ulong>(sprite, "_renderStaticStateNextAuditPhysicsFrame");
		bool retained = publishedFrame != -9223372036854775808L && auditDeadline != 18446744073709551615uL && num != 18446744073709551615uL && num > auditDeadline && ReadField<long>(sprite, "_runtimeNativeCanvasPublishedFrameVersion") == publishedFrame;
		ulong previousValue = ReadField<ulong>(sprite, "_cachedLayerVisibleSignature");
		bool original = alias[0];
		alias[0] = !original;
		bool mutationObserved = await WaitForFieldChange(sprite, "_cachedLayerVisibleSignature", previousValue);
		ulong previousValue2 = ReadField<ulong>(sprite, "_cachedLayerVisibleSignature");
		alias[0] = original;
		return (retained && mutationObserved) & await WaitForFieldChange(sprite, "_cachedLayerVisibleSignature", previousValue2);
	}

	private AdobeAnimateSprite CreatePeaSprite(Vector2 position)
	{
		AdobeAnimateSprite adobeAnimateSprite = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/PeaShooter.tscn")?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
		{
			throw new InvalidOperationException("PeaShooter animation scene could not be instantiated.");
		}
		adobeAnimateSprite.Position = position;
		adobeAnimateSprite.SetAnimation("Idle");
		AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
		return adobeAnimateSprite;
	}

	private bool ValidateCharacterPackedSceneSeal()
	{
		Node node = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(node))
		{
			throw new InvalidOperationException("PeaShooter character scene could not be instantiated.");
		}
		bool foundSprite = false;
		bool flag = ValidatePackedSceneSubtree(node, ref foundSprite);
		node.Free();
		return foundSprite && flag;
	}

	private bool ValidatePackedSceneSubtree(Node node, ref bool foundSprite)
	{
		bool flag = true;
		if (node is AdobeAnimateSprite owner)
		{
			foundSprite = true;
			flag = !ReadField<bool>(owner, "_renderStaticLayerArrayEscaped") && !ReadField<bool>(owner, "_renderStaticMediaArraysEscaped");
		}
		int childCount = node.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			flag &= ValidatePackedSceneSubtree(node.GetChild(i), ref foundSprite);
		}
		return flag;
	}

	private async Task<bool> WaitForFieldChange<T>(AdobeAnimateSprite sprite, string fieldName, T previousValue)
	{
		for (int frame = 0; frame < 180; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (!object.Equals(ReadField<T>(sprite, fieldName), previousValue))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<bool> WaitForFieldValue<T>(AdobeAnimateSprite sprite, string fieldName, T expectedValue)
	{
		for (int frame = 0; frame < 180; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (object.Equals(ReadField<T>(sprite, fieldName), expectedValue))
			{
				return true;
			}
		}
		return false;
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Runtime regression probe intentionally reflects private fields across the AdobeAnimateSprite inheritance chain.")]
	private static T ReadField<T>(object owner, string fieldName)
	{
		Type type = owner.GetType();
		FieldInfo fieldInfo = null;
		Type type2 = type;
		while (type2 != null && fieldInfo == null)
		{
			fieldInfo = type2.GetField(fieldName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
			type2 = type2.BaseType;
		}
		if (fieldInfo == null)
		{
			throw new MissingFieldException(type.FullName, fieldName);
		}
		return (T)fieldInfo.GetValue(owner);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Runtime regression probe intentionally invokes one private audit method across the AdobeAnimateSprite inheritance chain.")]
	private static T InvokeMethod<T>(object owner, string methodName)
	{
		Type type = owner.GetType();
		MethodInfo methodInfo = null;
		Type type2 = type;
		while (type2 != null && methodInfo == null)
		{
			methodInfo = type2.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
			type2 = type2.BaseType;
		}
		if (methodInfo == null)
		{
			throw new MissingMethodException(type.FullName, methodName);
		}
		return (T)methodInfo.Invoke(owner, null);
	}
}
