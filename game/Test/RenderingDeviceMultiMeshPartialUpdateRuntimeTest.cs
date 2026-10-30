using System;
using System.Threading.Tasks;
using Godot;

public sealed partial class RenderingDeviceMultiMeshPartialUpdateRuntimeTest : Node
{
	private enum ProbeColorClass
	{
		Red,
		Green,
		Blue,
		Yellow
	}

	private sealed class DispatcherBatchResult
	{
		public long AppliedUploadDelta { get; }

		public long AppliedVisibilityDelta { get; }

		public long BufferRidRefreshDelta { get; }

		public long BufferUpdateFailureDelta { get; }

		public long StaleGenerationDropDelta { get; }

		public long FailedBatchDelta { get; }

		public DispatcherBatchResult(AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot before, AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot after)
		{
			AppliedUploadDelta = after.AppliedUploadCount - before.AppliedUploadCount;
			AppliedVisibilityDelta = after.AppliedVisibilityCount - before.AppliedVisibilityCount;
			BufferRidRefreshDelta = after.BufferRidRefreshCount - before.BufferRidRefreshCount;
			BufferUpdateFailureDelta = after.BufferUpdateFailureCount - before.BufferUpdateFailureCount;
			StaleGenerationDropDelta = after.StaleGenerationDropCount - before.StaleGenerationDropCount;
			FailedBatchDelta = after.FailedBatchCount - before.FailedBatchCount;
		}
	}

	private sealed class RenderThreadUpdateResult
	{
		public bool ExecutedOnRenderThread { get; set; }

		public bool RenderingDeviceAvailable { get; set; }

		public bool BufferRidValid { get; set; }

		public ulong BufferRidId { get; set; }

		public Error UpdateError { get; set; } = Error.Failed;
	}

	private const string ResultMarker = "RENDERING_DEVICE_MULTIMESH_PARTIAL_UPDATE_RESULT";

	private const int InstanceStrideFloats = 16;

	private const int InstanceStrideBytes = 64;

	private const uint SecondInstanceByteOffset = 64u;

	private const int InitialCapacity = 4;

	private const int RebuiltCapacity = 8;

	private const int StabilizationFrameCount = 3;

	private const int RenderThreadTimeoutMilliseconds = 10000;

	private const int DispatcherCompletionTimeoutFrames = 120;

	private const int MinimumMatchingPixelCount = 256;

	private const int MinimumZSamplePixelCount = 16;

	private const int MaximumUnexpectedPixelCount = 8;

	private const long InitialBufferGeneration = 1L;

	private const long RebuiltBufferGeneration = 2L;

	private const long DispatcherFirstFrameVersion = 900001L;

	private static readonly Vector2I ProbeViewportSize = new Vector2I(800, 300);

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.02f);

	private static readonly Vector2 FirstInstancePosition = new Vector2(120f, 140f);

	private static readonly Vector2 BaselineSecondPosition = new Vector2(300f, 140f);

	private static readonly Vector2 FirstUpdatedSecondPosition = new Vector2(500f, 140f);

	private static readonly Vector2 RebuiltUpdatedSecondPosition = new Vector2(680f, 140f);

	private static readonly Vector2 ZLowInstancePosition = new Vector2(400f, 245f);

	private static readonly Vector2 ZHighInstancePosition = new Vector2(415f, 245f);

	private static readonly Vector2 ZLowSamplePosition = new Vector2(374f, 245f);

	private static readonly Vector2 ZMiddleSamplePosition = new Vector2(394f, 245f);

	private static readonly Vector2 ZHighSamplePosition = new Vector2(415f, 245f);

	private static readonly Color FirstInstanceColor = new Color(0.35f, 1f, 0.35f);

	private static readonly Color FirstInstanceCustomData = new Color(0.25f, 1f, 0.25f);

	private static readonly Color BaselineSecondColor = new Color(1f, 0.55f, 0.55f);

	private static readonly Color BaselineSecondCustomData = new Color(1f, 0.15f, 0.15f);

	private static readonly Color FirstUpdatedSecondColor = new Color(1f, 0.55f, 0.55f);

	private static readonly Color FirstUpdatedSecondCustomData = new Color(0.1f, 0.1f, 1f);

	private static readonly Color RebuiltUpdatedSecondColor = new Color(1f, 1f, 0.3f);

	private static readonly Color RebuiltUpdatedSecondCustomData = new Color(1f, 0.85f, 0.1f);

	private SubViewport _probeViewport;

	private MultiMeshInstance2D _multiMeshInstance;

	private MultiMesh _multiMesh;

	private QuadMesh _quadMesh;

	private ShaderMaterial _shaderMaterial;

	private Shader _shader;

	private MultiMeshInstance2D _zLowInstance;

	private MultiMeshInstance2D _zHighInstance;

	private MultiMesh _zLowMultiMesh;

	private MultiMesh _zHighMultiMesh;

	private QuadMesh _zLowQuadMesh;

	private QuadMesh _zHighQuadMesh;

	private readonly AdobeAnimateMultiMeshRdUploadDispatcher.GenerationToken _dispatcherGenerationToken = new AdobeAnimateMultiMeshRdUploadDispatcher.GenerationToken();

	public override async void _Ready()
	{
		bool passed = false;
		int exitCode = 2;
		string failure = "none";
		bool initialBaselinePassed = false;
		bool zInterleavePassed = false;
		bool firstPartialUpdatePassed = false;
		bool sameBatchPublicationPassed = false;
		bool visibleHidePassed = false;
		bool visibleRestorePassed = false;
		bool visibleRestoreUsedBaselineRecord = false;
		bool rebuildBaselinePassed = false;
		bool rebuiltPartialUpdatePassed = false;
		bool staleGenerationPassed = false;
		RenderThreadUpdateResult initialBaselineUploadResult = null;
		RenderThreadUpdateResult firstUpdateResult = null;
		RenderThreadUpdateResult rebuildBaselineUploadResult = null;
		DispatcherBatchResult firstPublicationResult = null;
		DispatcherBatchResult hidePublicationResult = null;
		DispatcherBatchResult restorePublicationResult = null;
		DispatcherBatchResult rebuiltPublicationResult = null;
		DispatcherBatchResult stalePublicationResult = null;
		try
		{
			base.ProcessMode = ProcessModeEnum.Always;
			CreateProbeScene();
			byte[] data = CreateBaselineActivePrefix();
			initialBaselineUploadResult = await UpdateMultiMeshBufferOnRenderThread(data, 0u);
			RequireSuccessfulRenderThreadUpdate(initialBaselineUploadResult, "初始 RD 基线上传");
			_multiMesh.VisibleInstanceCount = 2;
			await WaitForRenderedFrames(3);
			using (Image image = CaptureCurrentImage())
			{
				initialBaselinePassed = IsRegionColor(image, FirstInstancePosition, ProbeColorClass.Green) && IsRegionColor(image, BaselineSecondPosition, ProbeColorClass.Red) && IsRegionEmpty(image, FirstUpdatedSecondPosition) && IsRegionEmpty(image, RebuiltUpdatedSecondPosition);
				zInterleavePassed = VerifyZInterleave(image);
			}
			Require(initialBaselinePassed, "初始 MultiMesh 像素基线未通过");
			Require(zInterleavePassed, "Z=0 MultiMesh、Z=1 CanvasItem 与 Z=2 MultiMesh 的像素插层未通过");
			byte[] firstUpdatedRecord = CreateInstanceRecord(FirstUpdatedSecondPosition, FirstUpdatedSecondColor, FirstUpdatedSecondCustomData);
			firstUpdateResult = await UpdateMultiMeshBufferOnRenderThread(firstUpdatedRecord, 64u);
			RequireSuccessfulRenderThreadUpdate(firstUpdateResult, "首次非零偏移 RD 上传");
			await WaitForRenderedFrames(3);
			using (Image image2 = CaptureCurrentImage())
			{
				firstPartialUpdatePassed = IsRegionColor(image2, FirstInstancePosition, ProbeColorClass.Green) && IsRegionEmpty(image2, BaselineSecondPosition) && IsRegionColor(image2, FirstUpdatedSecondPosition, ProbeColorClass.Blue);
			}
			Require(firstPartialUpdatePassed, "非零偏移 RD 更新没有生成预期蓝色像素");
			firstPublicationResult = await QueueDispatcherBatch(900001L, 1L, firstUpdatedRecord, 64u, 2);
			sameBatchPublicationPassed = firstPublicationResult.AppliedUploadDelta == 1 && firstPublicationResult.AppliedVisibilityDelta == 1 && firstPublicationResult.FailedBatchDelta == 0;
			Require(sameBatchPublicationPassed, "同一 RD 调度批次未先完成 BufferUpdate 再发布 VisibleInstanceCount=2");
			hidePublicationResult = await QueueDispatcherBatch(900002L, 1L, null, 0u, 1);
			Require(hidePublicationResult.AppliedVisibilityDelta == 1 && hidePublicationResult.FailedBatchDelta == 0, "调度器未发布 VisibleInstanceCount 2->1");
			await WaitForRenderedFrames(3);
			using (Image image3 = CaptureCurrentImage())
			{
				visibleHidePassed = IsRegionColor(image3, FirstInstancePosition, ProbeColorClass.Green) && IsRegionEmpty(image3, FirstUpdatedSecondPosition);
			}
			Require(visibleHidePassed, "VisibleInstanceCount 2->1 未隐藏第二个实例");
			restorePublicationResult = await QueueDispatcherBatch(900003L, 1L, null, 0u, 2);
			Require(restorePublicationResult.AppliedVisibilityDelta == 1 && restorePublicationResult.FailedBatchDelta == 0, "调度器未发布 VisibleInstanceCount 1->2");
			await WaitForRenderedFrames(3);
			using (Image image4 = CaptureCurrentImage())
			{
				visibleRestorePassed = IsRegionColor(image4, FirstInstancePosition, ProbeColorClass.Green) && IsRegionColor(image4, FirstUpdatedSecondPosition, ProbeColorClass.Blue);
				visibleRestoreUsedBaselineRecord = IsRegionColor(image4, BaselineSecondPosition, ProbeColorClass.Red);
			}
			Require(visibleRestorePassed, $"VisibleInstanceCount 1->2 未恢复 RD 更新后的实例, baselineRecord={visibleRestoreUsedBaselineRecord}");
			RebuildCapacity();
			await WaitForRenderedFrames(3);
			byte[] data2 = CreateBaselineActivePrefix();
			rebuildBaselineUploadResult = await UpdateMultiMeshBufferOnRenderThread(data2, 0u);
			RequireSuccessfulRenderThreadUpdate(rebuildBaselineUploadResult, "容量重建后 RD 基线上传");
			_multiMesh.VisibleInstanceCount = 2;
			await WaitForRenderedFrames(3);
			using (Image image5 = CaptureCurrentImage())
			{
				rebuildBaselinePassed = IsRegionColor(image5, FirstInstancePosition, ProbeColorClass.Green) && IsRegionColor(image5, BaselineSecondPosition, ProbeColorClass.Red) && IsRegionEmpty(image5, FirstUpdatedSecondPosition);
			}
			Require(rebuildBaselinePassed, "容量重建后的 MultiMesh 像素基线未通过");
			byte[] uploadData = CreateInstanceRecord(RebuiltUpdatedSecondPosition, RebuiltUpdatedSecondColor, RebuiltUpdatedSecondCustomData);
			rebuiltPublicationResult = await QueueDispatcherBatch(900004L, 2L, uploadData, 64u, 2);
			Require(rebuiltPublicationResult.AppliedUploadDelta == 1 && rebuiltPublicationResult.AppliedVisibilityDelta == 1 && rebuiltPublicationResult.BufferRidRefreshDelta >= 1 && rebuiltPublicationResult.FailedBatchDelta == 0, "容量重建后调度器未重新取得 RID 并完成同批发布");
			await WaitForRenderedFrames(3);
			using (Image image6 = CaptureCurrentImage())
			{
				rebuiltPartialUpdatePassed = IsRegionColor(image6, FirstInstancePosition, ProbeColorClass.Green) && IsRegionEmpty(image6, BaselineSecondPosition) && IsRegionEmpty(image6, FirstUpdatedSecondPosition) && IsRegionColor(image6, RebuiltUpdatedSecondPosition, ProbeColorClass.Yellow);
			}
			Require(rebuiltPartialUpdatePassed, "容量重建后重新取得 RID 的局部更新没有生成预期黄色像素");
			byte[] uploadData2 = CreateInstanceRecord(BaselineSecondPosition, BaselineSecondColor, BaselineSecondCustomData);
			stalePublicationResult = await QueueDispatcherBatchThenInvalidateGeneration(900005L, uploadData2, 64u, 0);
			await WaitForRenderedFrames(3);
			using (Image image7 = CaptureCurrentImage())
			{
				staleGenerationPassed = stalePublicationResult.StaleGenerationDropDelta >= 2 && stalePublicationResult.AppliedUploadDelta == 0L && stalePublicationResult.AppliedVisibilityDelta == 0L && stalePublicationResult.FailedBatchDelta == 0L && IsRegionColor(image7, FirstInstancePosition, ProbeColorClass.Green) && IsRegionColor(image7, RebuiltUpdatedSecondPosition, ProbeColorClass.Yellow) && IsRegionEmpty(image7, BaselineSecondPosition);
			}
			Require(staleGenerationPassed, "旧缓冲代际仍然覆盖了新缓冲或可见数");
			passed = initialBaselinePassed && zInterleavePassed && firstPartialUpdatePassed && sameBatchPublicationPassed && visibleHidePassed && visibleRestorePassed && rebuildBaselinePassed && rebuiltPartialUpdatePassed && staleGenerationPassed;
			exitCode = ((!passed) ? 2 : 0);
		}
		catch (Exception ex)
		{
			failure = SanitizeResultValue(ex.GetType().Name + ":" + ex.Message);
		}
		finally
		{
			ulong num = initialBaselineUploadResult?.BufferRidId ?? firstUpdateResult?.BufferRidId ?? 0;
			ulong num2 = rebuildBaselineUploadResult?.BufferRidId ?? 0;
			bool flag = num != 0 && num2 != 0;
			bool value = flag && num != num2;
			string value2 = initialBaselineUploadResult?.UpdateError.ToString() ?? "NotRun";
			string value3 = firstUpdateResult?.UpdateError.ToString() ?? "NotRun";
			string value4 = rebuildBaselineUploadResult?.UpdateError.ToString() ?? "NotRun";
			string value5 = SanitizeResultValue(RenderingServer.GetCurrentRenderingMethod());
			string value6 = SanitizeResultValue(RenderingServer.GetCurrentRenderingDriverName());
			GD.Print($"{"RENDERING_DEVICE_MULTIMESH_PARTIAL_UPDATE_RESULT"} passed={passed} initialBaseline={initialBaselinePassed} zInterleave={zInterleavePassed} firstPartialUpdate={firstPartialUpdatePassed} sameBatchPublication={sameBatchPublicationPassed} visible2To1={visibleHidePassed} visible1To2={visibleRestorePassed} visibleRestoreUsedBaselineRecord={visibleRestoreUsedBaselineRecord} rebuildBaseline={rebuildBaselinePassed} rebuiltPartialUpdate={rebuiltPartialUpdatePassed} staleGeneration={staleGenerationPassed} lifecycleInvalidation={staleGenerationPassed} initialCapacity={4} rebuiltCapacity={8} rawBaselineUploadCalls={((initialBaselineUploadResult != null) ? 1 : 0) + ((rebuildBaselineUploadResult != null) ? 1 : 0)} rawPartialUpdateCalls={((firstUpdateResult != null) ? 1 : 0)} dispatcherAppliedUploads={(firstPublicationResult?.AppliedUploadDelta ?? 0) + (rebuiltPublicationResult?.AppliedUploadDelta ?? 0)} dispatcherAppliedVisibility={(firstPublicationResult?.AppliedVisibilityDelta ?? 0) + (hidePublicationResult?.AppliedVisibilityDelta ?? 0) + (restorePublicationResult?.AppliedVisibilityDelta ?? 0) + (rebuiltPublicationResult?.AppliedVisibilityDelta ?? 0)} staleDropDelta={stalePublicationResult?.StaleGenerationDropDelta ?? 0} partialUpdateOffset={64u} partialUpdateBytes={64} initialBaselineOnRenderThread={initialBaselineUploadResult?.ExecutedOnRenderThread ?? false} firstOnRenderThread={firstUpdateResult?.ExecutedOnRenderThread ?? false} rebuildBaselineOnRenderThread={rebuildBaselineUploadResult?.ExecutedOnRenderThread ?? false} firstBufferRid={num} rebuiltBufferRid={num2} bufferRidReacquired={flag} bufferRidChanged={value} initialBaselineUploadError={value2} firstUpdateError={value3} rebuildBaselineUploadError={value4} dispatcherBufferUpdateFailures={rebuiltPublicationResult?.BufferUpdateFailureDelta ?? 0} bufferReadbacks=0 viewportImageCaptures=7 syncCalls=0 multimeshSetBufferCalls=0 renderer={value5} driver={value6} failure={failure}");
			DisposeProbeScene();
			GetTree().Quit(exitCode);
		}
	}

	private void CreateProbeScene()
	{
		_probeViewport = new SubViewport();
		_probeViewport.Name = "RenderingDeviceMultiMeshProbeViewport";
		_probeViewport.Size = ProbeViewportSize;
		_probeViewport.Disable3D = true;
		_probeViewport.TransparentBg = false;
		_probeViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		AddChild(_probeViewport, forceReadableName: false, InternalMode.Disabled);
		ColorRect colorRect = new ColorRect();
		colorRect.Name = "ProbeBackground";
		colorRect.Color = BackgroundColor;
		colorRect.Position = Vector2.Zero;
		colorRect.Size = ProbeViewportSize;
		colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		colorRect.ZIndex = -10;
		_probeViewport.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		_shader = new Shader();
		_shader.Code = "shader_type canvas_item;\nrender_mode unshaded, blend_mix;\n\nvarying vec4 probe_instance_color;\n\nvoid vertex() {\n\tprobe_instance_color = COLOR * INSTANCE_CUSTOM;\n}\n\nvoid fragment() {\n\tCOLOR = probe_instance_color;\n}";
		_shaderMaterial = new ShaderMaterial();
		_shaderMaterial.Shader = _shader;
		_quadMesh = new QuadMesh();
		_quadMesh.Size = new Vector2(56f, 56f);
		_multiMesh = new MultiMesh();
		_multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
		_multiMesh.UseColors = true;
		_multiMesh.UseCustomData = true;
		_multiMesh.Mesh = _quadMesh;
		_multiMesh.CustomAabb = new Aabb(new Vector3(0f, 0f, -1f), new Vector3(ProbeViewportSize.X, ProbeViewportSize.Y, 2f));
		_multiMesh.InstanceCount = 4;
		_multiMesh.VisibleInstanceCount = 0;
		Require(_dispatcherGenerationToken.Advance() == 1, "探针初始缓冲代际不正确");
		_multiMeshInstance = new MultiMeshInstance2D();
		_multiMeshInstance.Name = "RenderingDeviceMultiMeshProbe";
		_multiMeshInstance.Multimesh = _multiMesh;
		_multiMeshInstance.Material = _shaderMaterial;
		_multiMeshInstance.ZIndex = 1;
		_probeViewport.AddChild(_multiMeshInstance, forceReadableName: false, InternalMode.Disabled);
		CreateZInterleaveProbe();
	}

	private void CreateZInterleaveProbe()
	{
		_zLowQuadMesh = new QuadMesh();
		_zLowQuadMesh.Size = new Vector2(80f, 80f);
		_zLowMultiMesh = CreateSingleInstanceMultiMesh(_zLowQuadMesh, ZLowInstancePosition, new Color(0.1f, 1f, 0.1f));
		_zLowInstance = new MultiMeshInstance2D();
		_zLowInstance.Name = "RenderingDeviceZLowMultiMesh";
		_zLowInstance.Multimesh = _zLowMultiMesh;
		_zLowInstance.Material = _shaderMaterial;
		_zLowInstance.ZAsRelative = false;
		_zLowInstance.ZIndex = 0;
		_probeViewport.AddChild(_zLowInstance, forceReadableName: false, InternalMode.Disabled);
		ColorRect colorRect = new ColorRect();
		colorRect.Name = "RenderingDeviceZMiddleCanvasItem";
		colorRect.Color = new Color(1f, 0.08f, 0.08f);
		colorRect.Position = new Vector2(390f, 215f);
		colorRect.Size = new Vector2(60f, 60f);
		colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		colorRect.ZAsRelative = false;
		colorRect.ZIndex = 1;
		_probeViewport.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		_zHighQuadMesh = new QuadMesh();
		_zHighQuadMesh.Size = new Vector2(30f, 30f);
		_zHighMultiMesh = CreateSingleInstanceMultiMesh(_zHighQuadMesh, ZHighInstancePosition, new Color(0.08f, 0.08f, 1f));
		_zHighInstance = new MultiMeshInstance2D();
		_zHighInstance.Name = "RenderingDeviceZHighMultiMesh";
		_zHighInstance.Multimesh = _zHighMultiMesh;
		_zHighInstance.Material = _shaderMaterial;
		_zHighInstance.ZAsRelative = false;
		_zHighInstance.ZIndex = 2;
		_probeViewport.AddChild(_zHighInstance, forceReadableName: false, InternalMode.Disabled);
	}

	private static MultiMesh CreateSingleInstanceMultiMesh(QuadMesh mesh, Vector2 position, Color color)
	{
		MultiMesh multiMesh = new MultiMesh();
		multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
		multiMesh.UseColors = true;
		multiMesh.UseCustomData = true;
		multiMesh.Mesh = mesh;
		multiMesh.CustomAabb = new Aabb(new Vector3(0f, 0f, -1f), new Vector3(ProbeViewportSize.X, ProbeViewportSize.Y, 2f));
		multiMesh.InstanceCount = 1;
		multiMesh.SetInstanceTransform2D(0, new Transform2D(0f, position));
		multiMesh.SetInstanceColor(0, color);
		multiMesh.SetInstanceCustomData(0, Colors.White);
		multiMesh.VisibleInstanceCount = 1;
		return multiMesh;
	}

	private void RebuildCapacity()
	{
		_multiMesh.VisibleInstanceCount = 0;
		_multiMesh.InstanceCount = 8;
		Require(_dispatcherGenerationToken.Advance() == 2, "探针容量重建后的缓冲代际不正确");
	}

	private static byte[] CreateBaselineActivePrefix()
	{
		byte[] src = CreateInstanceRecord(FirstInstancePosition, FirstInstanceColor, FirstInstanceCustomData);
		byte[] src2 = CreateInstanceRecord(BaselineSecondPosition, BaselineSecondColor, BaselineSecondCustomData);
		byte[] array = new byte[128];
		Buffer.BlockCopy(src, 0, array, 0, 64);
		Buffer.BlockCopy(src2, 0, array, 64, 64);
		return array;
	}

	private async Task<DispatcherBatchResult> QueueDispatcherBatch(long frameVersion, long bufferGeneration, byte[] uploadData, uint destinationByteOffset, int visibleInstanceCount)
	{
		AdobeAnimateMultiMeshRdUploadDispatcher shared = AdobeAnimateMultiMeshRdUploadDispatcher.Shared;
		AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot before = shared.Statistics;
		AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch = shared.BeginBatch(frameVersion);
		if (uploadData != null)
		{
			Require(batch.TryQueueUpload(_multiMesh, _dispatcherGenerationToken, bufferGeneration, destinationByteOffset, uploadData), $"调度器拒绝了帧 {frameVersion} 的上传命令");
		}
		Require(batch.TryQueueVisibility(_multiMesh, _dispatcherGenerationToken, bufferGeneration, visibleInstanceCount), $"调度器拒绝了帧 {frameVersion} 的可见数命令");
		Require(batch.EndBatch(), $"调度器未能封结帧 {frameVersion} 的批次");
		AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot after = await WaitForDispatcherBatch(frameVersion, before.CurrentQueueDepth);
		return new DispatcherBatchResult(before, after);
	}

	private async Task<DispatcherBatchResult> QueueDispatcherBatchThenInvalidateGeneration(long frameVersion, byte[] uploadData, uint destinationByteOffset, int visibleInstanceCount)
	{
		AdobeAnimateMultiMeshRdUploadDispatcher shared = AdobeAnimateMultiMeshRdUploadDispatcher.Shared;
		AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot before = shared.Statistics;
		long currentGeneration = _dispatcherGenerationToken.CurrentGeneration;
		AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch = shared.BeginBatch(frameVersion);
		Require(batch.TryQueueUpload(_multiMesh, _dispatcherGenerationToken, currentGeneration, destinationByteOffset, uploadData), $"调度器拒绝了帧 {frameVersion} 的生命周期竞态上传命令");
		Require(batch.TryQueueVisibility(_multiMesh, _dispatcherGenerationToken, currentGeneration, visibleInstanceCount), $"调度器拒绝了帧 {frameVersion} 的生命周期竞态可见数命令");
		Require(_dispatcherGenerationToken.Advance() == currentGeneration + 1, "生命周期推进没有使已捕获命令立即失效");
		Require(batch.EndBatch(), $"调度器未能封结帧 {frameVersion} 的生命周期竞态批次");
		AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot after = await WaitForDispatcherBatch(frameVersion, before.CurrentQueueDepth);
		return new DispatcherBatchResult(before, after);
	}

	private async Task<AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot> WaitForDispatcherBatch(long frameVersion, long queueDepthBefore)
	{
		for (int frame = 0; frame < 120; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot statistics = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.Statistics;
			if (statistics.LastAppliedFrameVersion >= frameVersion && statistics.CurrentQueueDepth <= queueDepthBefore)
			{
				return statistics;
			}
		}
		throw new TimeoutException($"等待 RD 调度批次 {frameVersion} 完成超时");
	}

	private async Task<RenderThreadUpdateResult> UpdateMultiMeshBufferOnRenderThread(byte[] data, uint destinationByteOffset)
	{
		if (data == null || data.Length == 0 || data.Length % 64 != 0)
		{
			throw new ArgumentException($"上传数据长度必须为 {64} 的正整数倍", "data");
		}
		uint num;
		uint num2;
		checked
		{
			num = (uint)(_multiMesh.InstanceCount * 64);
			num2 = (uint)data.Length;
		}
		if (num2 > num || destinationByteOffset > num - num2)
		{
			throw new ArgumentOutOfRangeException("destinationByteOffset", "上传区间超出当前 MultiMesh GPU 容量");
		}
		TaskCompletionSource<RenderThreadUpdateResult> completionSource = new TaskCompletionSource<RenderThreadUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		Rid multiMeshRid = _multiMesh.GetRid();
		RenderingServer.CallOnRenderThread(Callable.From(delegate
		{
			ExecuteRenderThreadUpdate(multiMeshRid, data, destinationByteOffset, completionSource);
		}));
		if (await Task.WhenAny(completionSource.Task, Task.Delay(10000)) != completionSource.Task)
		{
			throw new TimeoutException("等待 RenderingServer.CallOnRenderThread 超时");
		}
		return await completionSource.Task;
	}

	private static void ExecuteRenderThreadUpdate(Rid multiMeshRid, byte[] data, uint destinationByteOffset, TaskCompletionSource<RenderThreadUpdateResult> completionSource)
	{
		RenderThreadUpdateResult renderThreadUpdateResult = new RenderThreadUpdateResult();
		try
		{
			renderThreadUpdateResult.ExecutedOnRenderThread = RenderingServer.IsOnRenderThread();
			RenderingDevice renderingDevice = RenderingServer.GetRenderingDevice();
			renderThreadUpdateResult.RenderingDeviceAvailable = GodotObject.IsInstanceValid(renderingDevice);
			if (!renderThreadUpdateResult.RenderingDeviceAvailable)
			{
				completionSource.TrySetResult(renderThreadUpdateResult);
				return;
			}
			Rid buffer = RenderingServer.MultimeshGetBufferRdRid(multiMeshRid);
			renderThreadUpdateResult.BufferRidId = buffer.Id;
			renderThreadUpdateResult.BufferRidValid = buffer.IsValid;
			if (!renderThreadUpdateResult.BufferRidValid)
			{
				completionSource.TrySetResult(renderThreadUpdateResult);
				return;
			}
			renderThreadUpdateResult.UpdateError = renderingDevice.BufferUpdate(buffer, destinationByteOffset, checked((uint)data.Length), data);
			completionSource.TrySetResult(renderThreadUpdateResult);
		}
		catch (Exception exception)
		{
			completionSource.TrySetException(exception);
		}
	}

	private static void RequireSuccessfulRenderThreadUpdate(RenderThreadUpdateResult result, string stage)
	{
		Require(result != null, stage + ": 未返回结果");
		Require(result.RenderingDeviceAvailable, stage + ": 全局 RenderingDevice 不可用");
		Require(result.ExecutedOnRenderThread, stage + ": BufferUpdate 未在渲染线程执行");
		Require(result.BufferRidValid, stage + ": MultiMesh 底层 RD 缓冲 RID 无效");
		Require(result.UpdateError == Error.Ok, $"{stage}: BufferUpdate 失败 {result.UpdateError}");
	}

	private static byte[] CreateInstanceRecord(Vector2 position, Color color, Color customData)
	{
		float[] src = new float[16]
		{
			1f, 0f, 0f, position.X, 0f, 1f, 0f, position.Y, color.R, color.G,
			color.B, color.A, customData.R, customData.G, customData.B, customData.A
		};
		byte[] array = new byte[64];
		Buffer.BlockCopy(src, 0, array, 0, 64);
		return array;
	}

	private async Task WaitForRenderedFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}
	}

	private Image CaptureCurrentImage()
	{
		ViewportTexture texture = _probeViewport.GetTexture();
		if (!GodotObject.IsInstanceValid(texture))
		{
			throw new InvalidOperationException("探针视口纹理不可用");
		}
		Image image = texture.GetImage();
		if (!GodotObject.IsInstanceValid(image) || image.IsEmpty())
		{
			image?.Dispose();
			throw new InvalidOperationException("探针视口没有生成可读的完整帧");
		}
		return image;
	}

	private static bool IsRegionColor(Image image, Vector2 center, ProbeColorClass expectedColor)
	{
		return CountRegionPixels(image, center, (Color color) => MatchesColorClass(color, expectedColor)) >= 256;
	}

	private static bool IsRegionEmpty(Image image, Vector2 center)
	{
		return CountRegionPixels(image, center, IsSignalPixel) <= 8;
	}

	private static bool VerifyZInterleave(Image image)
	{
		if (IsZSampleColor(image, ZLowSamplePosition, ProbeColorClass.Green) && IsZSampleColor(image, ZMiddleSamplePosition, ProbeColorClass.Red))
		{
			return IsZSampleColor(image, ZHighSamplePosition, ProbeColorClass.Blue);
		}
		return false;
	}

	private static bool IsZSampleColor(Image image, Vector2 center, ProbeColorClass expectedColor)
	{
		return CountPixelsAround(image, center, 3, (Color color) => MatchesColorClass(color, expectedColor)) >= 16;
	}

	private static int CountRegionPixels(Image image, Vector2 center, Func<Color, bool> predicate)
	{
		return CountPixelsAround(image, center, 20, predicate);
	}

	private static int CountPixelsAround(Image image, Vector2 center, int halfExtent, Func<Color, bool> predicate)
	{
		int num = Mathf.RoundToInt(center.X);
		int num2 = Mathf.RoundToInt(center.Y);
		int num3 = Math.Max(0, num - halfExtent);
		int num4 = Math.Min(image.GetWidth(), num + halfExtent);
		int num5 = Math.Max(0, num2 - halfExtent);
		int num6 = Math.Min(image.GetHeight(), num2 + halfExtent);
		int num7 = 0;
		for (int i = num5; i < num6; i++)
		{
			for (int j = num3; j < num4; j++)
			{
				if (predicate(image.GetPixel(j, i)))
				{
					num7++;
				}
			}
		}
		return num7;
	}

	private static bool IsSignalPixel(Color color)
	{
		float num = Mathf.Abs(color.R - BackgroundColor.R) + Mathf.Abs(color.G - BackgroundColor.G) + Mathf.Abs(color.B - BackgroundColor.B);
		if (color.A > 0.8f)
		{
			return num > 0.18f;
		}
		return false;
	}

	private static bool MatchesColorClass(Color color, ProbeColorClass expectedColor)
	{
		if (color.A < 0.8f)
		{
			return false;
		}
		switch (expectedColor)
		{
		case ProbeColorClass.Red:
			if (color.R > color.G + 0.25f)
			{
				return color.R > color.B + 0.25f;
			}
			return false;
		case ProbeColorClass.Green:
			if (color.G > color.R + 0.25f)
			{
				return color.G > color.B + 0.25f;
			}
			return false;
		case ProbeColorClass.Blue:
			if (color.B > color.R + 0.25f)
			{
				return color.B > color.G + 0.25f;
			}
			return false;
		default:
			if (color.R > 0.45f && color.G > 0.35f)
			{
				return color.B < Math.Min(color.R, color.G) * 0.55f;
			}
			return false;
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}

	private static string SanitizeResultValue(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "none";
		}
		return value.Replace('\r', ' ').Replace('\n', ' ').Replace(' ', '_');
	}

	private void DisposeProbeScene()
	{
		if (GodotObject.IsInstanceValid(_zLowInstance))
		{
			_zLowInstance.Multimesh = null;
			_zLowInstance.Material = null;
			_zLowInstance.QueueFree();
		}
		_zLowInstance = null;
		if (GodotObject.IsInstanceValid(_zHighInstance))
		{
			_zHighInstance.Multimesh = null;
			_zHighInstance.Material = null;
			_zHighInstance.QueueFree();
		}
		_zHighInstance = null;
		if (GodotObject.IsInstanceValid(_zLowMultiMesh))
		{
			_zLowMultiMesh.Mesh = null;
			_zLowMultiMesh.Dispose();
		}
		_zLowMultiMesh = null;
		if (GodotObject.IsInstanceValid(_zHighMultiMesh))
		{
			_zHighMultiMesh.Mesh = null;
			_zHighMultiMesh.Dispose();
		}
		_zHighMultiMesh = null;
		if (GodotObject.IsInstanceValid(_zLowQuadMesh))
		{
			_zLowQuadMesh.Dispose();
		}
		_zLowQuadMesh = null;
		if (GodotObject.IsInstanceValid(_zHighQuadMesh))
		{
			_zHighQuadMesh.Dispose();
		}
		_zHighQuadMesh = null;
		if (GodotObject.IsInstanceValid(_multiMeshInstance))
		{
			_multiMeshInstance.Multimesh = null;
			_multiMeshInstance.Material = null;
			_multiMeshInstance.QueueFree();
		}
		_multiMeshInstance = null;
		if (GodotObject.IsInstanceValid(_multiMesh))
		{
			_multiMesh.Mesh = null;
			_multiMesh.Dispose();
		}
		_multiMesh = null;
		if (GodotObject.IsInstanceValid(_quadMesh))
		{
			_quadMesh.Dispose();
		}
		_quadMesh = null;
		if (GodotObject.IsInstanceValid(_shaderMaterial))
		{
			_shaderMaterial.Shader = null;
			_shaderMaterial.Dispose();
		}
		_shaderMaterial = null;
		if (GodotObject.IsInstanceValid(_shader))
		{
			_shader.Dispose();
		}
		_shader = null;
		if (GodotObject.IsInstanceValid(_probeViewport))
		{
			_probeViewport.QueueFree();
		}
		_probeViewport = null;
	}
}
