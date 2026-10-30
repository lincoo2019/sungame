using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class FogPlanternBoundaryRuntimeTest : Node
{
	private const int MapColumns = 9;

	private const int MapRows = 5;

	private const int BeginColumn = 5;

	private const int ExtraColumns = 10;

	private const int ViewportWidth = 1100;

	private const int ViewportHeight = 700;

	private const int FadePhysicsFrames = 360;

	private int _checks;

	private int _failures;

	private TowerDefenseManager _previousManager;

	private TowerDefenseBattleFeatureFog _fogFeature;

	private TowerDefenseManager _testManager;

	public override async void _Ready()
	{
		string beforePath = string.Empty;
		string afterPath = string.Empty;
		int rightClearedPixels = 0;
		int bottomClearedPixels = 0;
		try
		{
			(beforePath, afterPath, rightClearedPixels, bottomClearedPixels) = await RunBoundaryScenario();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[FogPlanternBoundaryRuntimeTest] 未处理异常：{value}");
		}
		finally
		{
			SetTowerDefenseManager(_previousManager);
			if (GodotObject.IsInstanceValid(_testManager))
			{
				_testManager.Free();
			}
		}
		bool flag = _failures == 0;
		GD.Print($"FOG_PLANTERN_BOUNDARY_RESULT passed={flag} checks={_checks} failures={_failures} rightClearedPixels={rightClearedPixels} bottomClearedPixels={bottomClearedPixels} before={beforePath} after={afterPath} renderer={RenderingServer.GetCurrentRenderingMethod()}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task<(string BeforePath, string AfterPath, int RightClearedPixels, int BottomClearedPixels)> RunBoundaryScenario()
	{
		_previousManager = TowerDefenseManager.Instance;
		_testManager = new TowerDefenseManager
		{
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(80f, 98f),
			gridNum = new Vector2I(9, 5)
		};
		SetTowerDefenseManager(_testManager);
		ColorRect node = new ColorRect
		{
			Color = new Color(0.015f, 0.03f, 0.07f),
			Size = new Vector2(1100f, 700f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_fogFeature = CreateFogFeature();
		AddChild(_fogFeature.fogNode, forceReadableName: false, InternalMode.Disabled);
		_fogFeature.fogNode.AddChild(_fogFeature.fogBatch, forceReadableName: false, InternalMode.Disabled);
		await _fogFeature.GameInit();
		_fogFeature.fogNode.GlobalPosition = Vector2.Zero;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		TowerDefenseFog rightBoundaryFog = FindFog(12, 5);
		TowerDefenseFog bottomBoundaryFog = FindFog(9, 6);
		TowerDefenseFog cornerBoundaryFog = FindFog(12, 6);
		TowerDefenseFog towerDefenseFog = FindFog(13, 5);
		Check(GodotObject.IsInstanceValid(rightBoundaryFog) && rightBoundaryFog.lightOverlapEnabled, "地图右侧第 3 个外扩雾列必须参与路灯花清雾检测。");
		Check(GodotObject.IsInstanceValid(bottomBoundaryFog) && bottomBoundaryFog.lightOverlapEnabled, "地图底部第 1 个外扩雾行必须参与路灯花清雾检测。");
		Check(GodotObject.IsInstanceValid(cornerBoundaryFog) && cornerBoundaryFog.lightOverlapEnabled, "右下角外扩雾块必须同时参与路灯花清雾检测。");
		Check(GodotObject.IsInstanceValid(towerDefenseFog) && !towerDefenseFog.lightOverlapEnabled, "右侧再外一列必须保持纯视觉雾，避免扩大物理更新范围。");
		Check(_fogFeature.fogBatch.ActiveCount == 48, $"9x5 地图应仅批量管理 48 个边界内外扩雾块，实际为 {_fogFeature.fogBatch.ActiveCount}。");
		CharacterAabbAreaComponentDefinition characterAabbAreaComponentDefinition = GD.Load<CharacterAabbAreaComponentDefinition>("res://Asset/Anime/Character/Plant/Chapter0/Plantern/Scene/TowerDefensePlanternFogAreaComponentDefinition.tres");
		CircleShape2D planternFogShape = ((characterAabbAreaComponentDefinition != null && characterAabbAreaComponentDefinition.shapeResources.Count > 0) ? (characterAabbAreaComponentDefinition.shapeResources[0].Geometry as CircleShape2D) : null);
		Check(GodotObject.IsInstanceValid(planternFogShape) && Mathf.IsEqualApprox(planternFogShape.Radius, 200f), "运行探针必须使用路灯花资源中半径 200 的真实清雾形状。");
		string path = ResolveProofDirectory();
		string beforePath = Path.Combine(path, "fog_plantern_boundary_before.png");
		string afterPath = Path.Combine(path, "fog_plantern_boundary_after.png");
		Image beforeImage = await CaptureFrame(beforePath);
		float radius = planternFogShape.Radius;
		Vector2 vector = new Vector2(700f, 470f);
		Rect2 planternLightRect = new Rect2(vector - Vector2.One * radius, Vector2.One * radius * 2f);
		ApplyPlanternLight(planternLightRect);
		Check(rightBoundaryFog.sprite.Modulate.A <= 0.001f, $"右侧外扩雾列应被路灯花清除，当前透明度为 {rightBoundaryFog.sprite.Modulate.A:F6}。");
		Check(bottomBoundaryFog.sprite.Modulate.A <= 0.001f, $"底部外扩雾行应被路灯花清除，当前透明度为 {bottomBoundaryFog.sprite.Modulate.A:F6}。");
		Check(cornerBoundaryFog.sprite.Modulate.A <= 0.001f, $"右下角外扩雾块应被路灯花清除，当前透明度为 {cornerBoundaryFog.sprite.Modulate.A:F6}。");
		Image afterImage = await CaptureFrame(afterPath);
		int num = CountClearedPixels(beforeImage, afterImage, new Rect2I(805, 300, 78, 280));
		int num2 = CountClearedPixels(beforeImage, afterImage, new Rect2I(520, 590, 280, 44));
		Check(num >= 100, $"右侧边缘必须出现真实清雾像素，实际减少亮度的像素数为 {num}。");
		Check(num2 >= 100, $"底部边缘必须出现真实清雾像素，实际减少亮度的像素数为 {num2}。");
		return (BeforePath: beforePath, AfterPath: afterPath, RightClearedPixels: num, BottomClearedPixels: num2);
	}

	private TowerDefenseBattleFeatureFog CreateFogFeature()
	{
		TowerDefenseBattleFeatureFog towerDefenseBattleFeatureFog = new TowerDefenseBattleFeatureFog
		{
			beginColumn = 5,
			mapGridNum = new Vector2I(9, 5),
			config = new TowerDefenseLevelFogManagerConfig
			{
				extraColumns = 10
			},
			fogNode = new Node2D
			{
				Name = "FogBoundaryRuntimeNode"
			},
			fogBatch = new TowerDefenseFogBatch
			{
				Name = "FogBoundaryRuntimeBatch",
				ProcessMode = ProcessModeEnum.Disabled
			}
		};
		int num = Math.Max(2, 7);
		for (int i = 0; i < num; i++)
		{
			towerDefenseBattleFeatureFog.fogLine.Add(new Array<TowerDefenseFog>());
		}
		return towerDefenseBattleFeatureFog;
	}

	private TowerDefenseFog FindFog(int column, int row)
	{
		if (row < 0 || row >= _fogFeature.fogLine.Count)
		{
			return null;
		}
		foreach (TowerDefenseFog item in _fogFeature.fogLine[row])
		{
			if (GodotObject.IsInstanceValid(item) && Mathf.IsEqualApprox(item.gridPos.X, column))
			{
				return item;
			}
		}
		return null;
	}

	private void ApplyPlanternLight(Rect2 planternLightRect)
	{
		List<AabbAreaLayerRegistry.WorldRectSnapshot> lightAreas = new List<AabbAreaLayerRegistry.WorldRectSnapshot>
		{
			new AabbAreaLayerRegistry.WorldRectSnapshot(planternLightRect)
		};
		for (int i = 0; i < 360; i++)
		{
			for (int j = 0; j < _fogFeature.fogLine.Count; j++)
			{
				foreach (TowerDefenseFog item in _fogFeature.fogLine[j])
				{
					if (GodotObject.IsInstanceValid(item) && item.lightOverlapEnabled)
					{
						item.BatchPhysicsUpdate(1.0 / 60.0, lightAreas);
					}
				}
			}
		}
	}

	private async Task<Image> CaptureFrame(string path)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Image image = GetViewport().GetTexture().GetImage();
		Error error = image.SavePng(path);
		Check(error == Error.Ok, $"边界清雾截图保存失败：{path}，错误码 {error}。");
		return image;
	}

	private int CountClearedPixels(Image beforeImage, Image afterImage, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(region.End.X, Math.Min(beforeImage.GetWidth(), afterImage.GetWidth()));
		int num3 = Math.Min(region.End.Y, Math.Min(beforeImage.GetHeight(), afterImage.GetHeight()));
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
			{
				Color pixel = beforeImage.GetPixel(j, i);
				Color pixel2 = afterImage.GetPixel(j, i);
				float num4 = pixel.R * 0.2126f + pixel.G * 0.7152f + pixel.B * 0.0722f;
				float num5 = pixel2.R * 0.2126f + pixel2.G * 0.7152f + pixel2.B * 0.0722f;
				if (num4 - num5 >= 0.03f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private string ResolveProofDirectory()
	{
		string environment = OS.GetEnvironment("FOG_BOUNDARY_PROOF_DIRECTORY");
		string obj = (string.IsNullOrWhiteSpace(environment) ? ProjectSettings.GlobalizePath("user://fog-boundary-proof") : environment);
		Directory.CreateDirectory(obj);
		return obj;
	}

	private static void SetTowerDefenseManager(TowerDefenseManager manager)
	{
		typeof(TowerDefenseManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(null, manager);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FogPlanternBoundaryRuntimeTest] " + message);
		}
	}
}
