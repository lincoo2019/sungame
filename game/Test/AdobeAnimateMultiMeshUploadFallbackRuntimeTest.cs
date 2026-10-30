using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateMultiMeshUploadFallbackRuntimeTest : Node
{
	private readonly AdobeAnimateMultiMeshRdUploadDispatcher.GenerationToken _generation = new AdobeAnimateMultiMeshRdUploadDispatcher.GenerationToken();

	private SubViewport _viewport;

	private MultiMesh _multiMesh;

	private QuadMesh _mesh;

	private long _frameVersion = 1000000L;

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			_ = 8;
			try
			{
				_viewport = new SubViewport
				{
					Size = new Vector2I(400, 160),
					TransparentBg = true,
					RenderTargetUpdateMode = SubViewport.UpdateMode.Always
				};
				AddChild(_viewport, forceReadableName: false, InternalMode.Disabled);
				_mesh = new QuadMesh
				{
					Size = new Vector2(32f, 32f)
				};
				_multiMesh = new MultiMesh
				{
					TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
					UseColors = true,
					UseCustomData = true,
					Mesh = _mesh,
					InstanceCount = 4,
					VisibleInstanceCount = 0
				};
				_viewport.AddChild(new MultiMeshInstance2D
				{
					Multimesh = _multiMesh
				}, forceReadableName: false, InternalMode.Disabled);
				_generation.Advance();
				AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot before = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.Statistics;
				float[] array = new float[32];
				Record(80f, Colors.Green).CopyTo(array, 0);
				Record(200f, Colors.Red).CopyTo(array, 16);
				Submit(array, 0u, 2);
				await VerifyPixels(80, Colors.Green, 200, Colors.Red);
				Submit(Record(300f, Colors.Blue), 64u, 2);
				await VerifyPixels(80, Colors.Green, 300, Colors.Blue, 200);
				Submit(null, 0u, 1);
				await VerifyPixels(80, Colors.Green, 80, Colors.Green, 300);
				Submit(null, 0u, 2);
				await VerifyPixels(80, Colors.Green, 300, Colors.Blue);
				using (AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.BeginBatch(++_frameVersion))
				{
					long currentGeneration = _generation.CurrentGeneration;
					Require(batch.TryQueueUpload(_multiMesh, _generation, currentGeneration, 64u, MemoryMarshal.AsBytes(Record(200f, Colors.Red).AsSpan())), "旧代际测试上传未入队。");
					Require(batch.TryQueueVisibility(_multiMesh, _generation, currentGeneration, 0), "旧代际隐藏命令未入队。");
					_generation.Advance();
					Require(batch.EndBatch(), "旧代际测试批次未提交。");
				}
				await VerifyPixels(80, Colors.Green, 300, Colors.Blue, 200);
				_multiMesh.InstanceCount = 0;
				_multiMesh.InstanceCount = 4;
				_generation.Advance();
				Submit(Record(80f, Colors.Green), 0u, 2);
				await VerifyPixels(80, Colors.Green, 80, Colors.Green, 300);
				_multiMesh.InstanceCount = 8;
				_generation.Advance();
				Submit(Record(80f, Colors.Green), 0u, 2);
				await VerifyPixels(80, Colors.Green, 80, Colors.Green, 300);
				Submit(Record(240f, Colors.Yellow), 64u, 2);
				await VerifyPixels(80, Colors.Green, 240, Colors.Yellow);
				Submit(null, 0u, 0);
				await VerifyPixels(80, Colors.Transparent, 240, Colors.Transparent);
				AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot statistics = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.Statistics;
				Require(statistics.AppliedUploadCount - before.AppliedUploadCount == 5, "实际上传次数不符。");
				Require(statistics.StaleGenerationDropCount - before.StaleGenerationDropCount == 2, "旧代际上传和隐藏命令没有同时淘汰。");
				Require(statistics.FailedBatchCount == before.FailedBatchCount && statistics.InvalidTargetCount == before.InvalidTargetCount, "上传过程中出现失败批次或失效目标。");
				Require((!(RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility")) ? (statistics.RenderingDeviceUnavailableCount == before.RenderingDeviceUnavailableCount && statistics.BufferRidRefreshCount > before.BufferRidRefreshCount) : (statistics.RenderingDeviceUnavailableCount > before.RenderingDeviceUnavailableCount && statistics.BufferRidRefreshCount == before.BufferRidRefreshCount), "没有选择正确的设备上传路径。");
				GD.Print("ADOBE_ANIMATE_MULTIMESH_UPLOAD_FALLBACK_RESULT passed=True partial=True visibility=True recreated=True resized=True staleDrops=2 uploads=5 renderer=" + RenderingServer.GetCurrentRenderingMethod());
				exitCode = 0;
			}
			catch (Exception value)
			{
				GD.PrintErr($"ADOBE_ANIMATE_MULTIMESH_UPLOAD_FALLBACK_RESULT passed=False exception={value}");
			}
		}
		finally
		{
			_generation.Advance();
			if (GodotObject.IsInstanceValid(_viewport))
			{
				_viewport.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			_multiMesh?.Dispose();
			_mesh?.Dispose();
		}
		GetTree().Quit(exitCode);
	}

	private static float[] Record(float x, Color color)
	{
		float[] obj = new float[16]
		{
			1f, 0f, 0f, 0f, 0f, 1f, 0f, 80f, 0f, 0f,
			0f, 0f, 0f, 0f, 0f, 0f
		};
		obj[3] = x;
		obj[8] = color.R;
		obj[9] = color.G;
		obj[10] = color.B;
		obj[11] = color.A;
		return obj;
	}

	private void Submit(float[] data, uint offset, int visibleCount)
	{
		using AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.BeginBatch(++_frameVersion);
		long currentGeneration = _generation.CurrentGeneration;
		if (data != null)
		{
			Require(batch.TryQueueUpload(_multiMesh, _generation, currentGeneration, offset, MemoryMarshal.AsBytes(data.AsSpan())), "上传命令未入队。");
		}
		Require(batch.TryQueueVisibility(_multiMesh, _generation, currentGeneration, visibleCount), "可见数命令未入队。");
		Require(batch.EndBatch(), "批次未提交。");
	}

	private async Task VerifyPixels(int firstX, Color firstColor, int secondX, Color secondColor, int emptyX = -1)
	{
		for (int frame = 0; frame < 3; frame++)
		{
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}
		using Image image = _viewport.GetTexture().GetImage();
		CheckColor(image, firstX, firstColor);
		CheckColor(image, secondX, secondColor);
		if (emptyX >= 0)
		{
			CheckColor(image, emptyX, Colors.Transparent);
		}
	}

	private static void CheckColor(Image image, int centerX, Color expected)
	{
		for (int i = 76; i <= 84; i++)
		{
			for (int j = centerX - 4; j <= centerX + 4; j++)
			{
				Color pixel = image.GetPixel(j, i);
				Require(Math.Abs(pixel.A - expected.A) < 0.08f && (expected.A == 0f || (Math.Abs(pixel.R - expected.R) < 0.08f && Math.Abs(pixel.G - expected.G) < 0.08f && Math.Abs(pixel.B - expected.B) < 0.08f)), $"像素不符: ({j},{i}) expected={expected} actual={pixel}");
			}
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
