using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ProjectileSkinRenderTemplateRuntimeTest : Node
{
	public override async void _Ready()
	{
		List<string> failures = new List<string>();
		int baseProjectiles = 0;
		int customSkins = 0;
		int validatedTemplates = 0;
		BulletField bulletField = null;
		try
		{
			TowerDefenseProjectileRegistry.Init();
			bulletField = new BulletField
			{
				Name = "ProjectileSkinRenderTemplateBulletField"
			};
			AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			foreach (KeyValuePair<StringName, TowerDefenseProjectileSkinData> item in TowerDefenseProjectileRegistry.ProjectileSkinDictionary.OrderBy<KeyValuePair<StringName, TowerDefenseProjectileSkinData>, string>((KeyValuePair<StringName, TowerDefenseProjectileSkinData> entry) => entry.Key.ToString(), StringComparer.Ordinal))
			{
				TowerDefenseProjectileSkinData value = item.Value;
				if (value == null)
				{
					failures.Add($"{item.Key}: skin registry data is null");
					continue;
				}
				foreach (StringName item2 in value.SkinList.OrderBy<StringName, string>((StringName name) => name.ToString(), StringComparer.Ordinal))
				{
					if (item2 == new StringName("Default"))
					{
						baseProjectiles++;
					}
					else
					{
						customSkins++;
					}
					TowerDefenseProjectileConfig towerDefenseProjectileConfig = BulletField.BuildTemplateConfig(item.Key, item2);
					string errorCode;
					string errorReason;
					if (towerDefenseProjectileConfig?.projectileScene == null)
					{
						failures.Add($"{item.Key}/{item2}: projectile scene is unavailable");
					}
					else if (!bulletField.TryValidateRenderTemplateForTest(towerDefenseProjectileConfig, out errorCode, out errorReason))
					{
						failures.Add($"{item.Key}/{item2}: {errorCode} {errorReason} ({towerDefenseProjectileConfig.projectileScene.ResourcePath})");
					}
					else
					{
						validatedTemplates++;
					}
				}
			}
			if (baseProjectiles < 50)
			{
				failures.Add($"Base projectile audit was unexpectedly small: {baseProjectiles}");
			}
			if (customSkins < 1)
			{
				failures.Add("No custom projectile skins were audited");
			}
			if (validatedTemplates != baseProjectiles + customSkins)
			{
				failures.Add($"Validated {validatedTemplates} of {baseProjectiles + customSkins} templates");
			}
		}
		catch (Exception value2)
		{
			failures.Add($"Unexpected exception: {value2}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		foreach (string item3 in failures)
		{
			GD.PushError("[ProjectileSkinRenderTemplateRuntimeTest] " + item3);
		}
		bool flag = failures.Count == 0;
		GD.Print($"PROJECTILE_SKIN_RENDER_TEMPLATE_RESULT passed={flag} baseProjectiles={baseProjectiles} customSkins={customSkins} validatedTemplates={validatedTemplates} failures={failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
