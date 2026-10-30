using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseEffectParticlesOnce : TowerDefenseEffectBase
{
	private const int RetainedPerSceneLimit = 16;

	private const string BuiltInParticleRoot = "res://Prefab/Particles/";

	private const string BuiltInParticleScriptPath = "res://Extends/Particles/GPUParticles2DOnece.cs";

	private static readonly Dictionary<PackedScene, Stack<TowerDefenseEffectParticlesOnce>> RetainedByScene = new Dictionary<PackedScene, Stack<TowerDefenseEffectParticlesOnce>>();

	private static readonly Dictionary<PackedScene, bool> SimpleParticleSceneCache = new Dictionary<PackedScene, bool>();

	private static Script _builtInParticleScript;

	private bool _particleRestartRequested;

	private bool _particleRestartQueued;

	private static PackedScene _towerDefenseEffectParticlesOnce;

	private PackedScene _retainedScene;

	private bool _retainedIdle;

	[Export(PropertyHint.None, "")]
	public ObjectManagerConfig.OBJECT objectId;

	[Export(PropertyHint.None, "")]
	public GPUParticles2DOnece particles;

	private static PackedScene TOWER_DEFENSE_EFFECT_PARTICLES_ONCE => _towerDefenseEffectParticlesOnce ?? (_towerDefenseEffectParticlesOnce = GD.Load<PackedScene>("uid://dbyd0mqkya1j3"));

	public static TowerDefenseEffectParticlesOnce Create()
	{
		return TOWER_DEFENSE_EFFECT_PARTICLES_ONCE.Instantiate<TowerDefenseEffectParticlesOnce>(PackedScene.GenEditState.Disabled);
	}

	public static bool TrySpawnRetained(PackedScene particlesScene, Node2D parent, Vector2I gridPosition, Vector2 globalPosition, out TowerDefenseEffectParticlesOnce effect)
	{
		effect = null;
		if (particlesScene == null || !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree() || !globalPosition.IsFinite() || !IsSimpleBuiltInParticleScene(particlesScene))
		{
			return false;
		}
		if (RetainedByScene.TryGetValue(particlesScene, out var value))
		{
			while (value.Count > 0)
			{
				TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = value.Pop();
				if (GodotObject.IsInstanceValid(towerDefenseEffectParticlesOnce) && towerDefenseEffectParticlesOnce.IsInsideTree() && towerDefenseEffectParticlesOnce._retainedIdle)
				{
					effect = towerDefenseEffectParticlesOnce;
					break;
				}
			}
		}
		if (!GodotObject.IsInstanceValid(effect))
		{
			Node node = particlesScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is GPUParticles2DOnece scene))
			{
				node?.Free();
				return false;
			}
			effect = Create();
			effect._retainedScene = particlesScene;
			effect.InitScene(scene);
			effect.gridPos = gridPosition;
			parent.AddChild(effect, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			if (effect.GetParent() != parent)
			{
				effect.Reparent(parent);
			}
			effect.gridPos = gridPosition;
			effect.GlobalPosition = globalPosition;
			effect.ResumeRetained();
		}
		effect.GlobalPosition = globalPosition;
		return true;
	}

	public void Refresh()
	{
		AddToGroup("Effect");
		_retainedIdle = false;
		base.Visible = true;
		SetPhysicsProcess(enable: true);
		if (GodotObject.IsInstanceValid(particles))
		{
			particles.Visible = true;
			RequestParticleRestart();
		}
	}

	public void Recycle()
	{
		RemoveFromGroup("Effect");
		base.Visible = false;
		SetPhysicsProcess(enable: false);
		_particleRestartRequested = false;
		StopParticleTree(particles);
	}

	public override void _Ready()
	{
		base._Ready();
		if (particles != null)
		{
			if (!particles.IsConnected(GpuParticles2D.SignalName.Finished, Callable.From(Finish)))
			{
				particles.Finished += Finish;
			}
			RequestParticleRestart();
		}
	}

	public void InitScene(GPUParticles2DOnece scene)
	{
		AddToGroup("Effect");
		_retainedIdle = false;
		particles = scene;
		StopParticleTree(particles);
		AddChild(particles, forceReadableName: false, InternalMode.Disabled);
		if (!particles.IsConnected(GpuParticles2D.SignalName.Finished, Callable.From(Finish)))
		{
			particles.Finished += Finish;
		}
		if (IsInsideTree())
		{
			RequestParticleRestart();
		}
	}

	public void Init(PackedScene particlesScene)
	{
		InitScene(particlesScene.Instantiate<GPUParticles2DOnece>(PackedScene.GenEditState.Disabled));
	}

	public void Finish()
	{
		if (_retainedIdle)
		{
			return;
		}
		if (_retainedScene != null && IsInsideTree())
		{
			if (!RetainedByScene.TryGetValue(_retainedScene, out var value))
			{
				value = new Stack<TowerDefenseEffectParticlesOnce>(16);
				RetainedByScene.Add(_retainedScene, value);
			}
			if (value.Count < 16)
			{
				_retainedIdle = true;
				StopParticleTree(particles);
				Recycle();
				value.Push(this);
				return;
			}
		}
		if (objectId == ObjectManagerConfig.OBJECT.NOONE || !ObjectManager.TryPoolPush(objectId, this))
		{
			QueueFree();
		}
	}

	private void ResumeRetained()
	{
		_retainedIdle = false;
		base.Visible = true;
		AddToGroup("Effect");
		SetPhysicsProcess(enable: true);
		if (GodotObject.IsInstanceValid(particles))
		{
			particles.Visible = true;
			RequestParticleRestart();
		}
	}

	private void RequestParticleRestart()
	{
		_particleRestartRequested = true;
		if (!_particleRestartQueued)
		{
			_particleRestartQueued = true;
			Callable.From(FlushParticleRestart).CallDeferred();
		}
	}

	private void FlushParticleRestart()
	{
		_particleRestartQueued = false;
		if (_particleRestartRequested && !_retainedIdle && IsInsideTree() && base.Visible && GodotObject.IsInstanceValid(particles))
		{
			_particleRestartRequested = false;
			particles.Visible = true;
			particles.Init();
		}
	}

	private static void StopParticleTree(GpuParticles2D root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}
		root.Emitting = false;
		foreach (Node child in root.GetChildren())
		{
			if (child is GpuParticles2D root2)
			{
				StopParticleTree(root2);
			}
		}
	}

	private static bool IsSimpleBuiltInParticleScene(PackedScene scene)
	{
		if (SimpleParticleSceneCache.TryGetValue(scene, out var value))
		{
			return value;
		}
		bool flag = false;
		string text = scene.ResourcePath ?? string.Empty;
		if (text.StartsWith("res://Prefab/Particles/", StringComparison.Ordinal) && text.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) && ResourceLoader.Load<PackedScene>(text, "", ResourceLoader.CacheMode.Reuse) == scene)
		{
			SceneState state = scene.GetState();
			flag = GodotObject.IsInstanceValid(state) && state.GetNodeCount() > 0 && string.Equals(state.GetNodeType(0).ToString(), "GPUParticles2D", StringComparison.Ordinal) && HasOnlyBuiltInRootScript(state);
		}
		SimpleParticleSceneCache[scene] = flag;
		return flag;
	}

	private static bool HasOnlyBuiltInRootScript(SceneState state)
	{
		if (!GodotObject.IsInstanceValid(_builtInParticleScript))
		{
			_builtInParticleScript = ResourceLoader.Load<Script>("res://Extends/Particles/GPUParticles2DOnece.cs", "", ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_builtInParticleScript))
		{
			return false;
		}
		for (int i = 0; i < state.GetNodeCount(); i++)
		{
			Script script = null;
			for (int j = 0; j < state.GetNodePropertyCount(i); j++)
			{
				if (string.Equals(state.GetNodePropertyName(i, j).ToString(), "script", StringComparison.Ordinal))
				{
					script = state.GetNodePropertyValue(i, j).AsGodotObject() as Script;
					break;
				}
			}
			if (i == 0)
			{
				if (script != _builtInParticleScript)
				{
					return false;
				}
			}
			else if (GodotObject.IsInstanceValid(script))
			{
				return false;
			}
		}
		return true;
	}
}
