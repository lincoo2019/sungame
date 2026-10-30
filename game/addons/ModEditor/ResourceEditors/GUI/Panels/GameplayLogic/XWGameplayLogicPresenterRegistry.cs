using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWGameplayLogicPresenterRegistry
{
	public sealed record CoverageResult(string TypeName, int MatchCount, string Status);

	public const string Missing = "Missing";

	public const string Ambiguous = "Ambiguous";

	public const string Covered = "Covered";

	private readonly List<IXWGameplayLogicPresenter> _presenters = new List<IXWGameplayLogicPresenter>();

	public XWGameplayLogicPresenterRegistry()
	{
		Register(new XWLevelSpawnPresenter());
		Register(new XWLevelEventPresenter());
		Register(new XWBattleFeaturePresenter());
		Register(new XWBattleProcessPresenter());
		Register(new XWBroadcastPresenter());
	}

	public void Register(IXWGameplayLogicPresenter presenter)
	{
		if (presenter != null && !_presenters.Contains(presenter))
		{
			_presenters.Add(presenter);
		}
	}

	public IReadOnlyList<IXWGameplayLogicPresenter> Find(Resource resource)
	{
		List<IXWGameplayLogicPresenter> list = new List<IXWGameplayLogicPresenter>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (IXWGameplayLogicPresenter presenter in _presenters)
		{
			if (presenter.CanPresent(resource))
			{
				list.Add(presenter);
			}
		}
		return list;
	}

	public IReadOnlyList<CoverageResult> AuditCoverage(IEnumerable<Resource> resources)
	{
		List<CoverageResult> list = new List<CoverageResult>();
		if (resources == null)
		{
			return list;
		}
		foreach (Resource resource in resources)
		{
			if (GodotObject.IsInstanceValid(resource))
			{
				IReadOnlyList<IXWGameplayLogicPresenter> readOnlyList = Find(resource);
				string status = ((readOnlyList.Count == 1) ? "Covered" : ((readOnlyList.Count == 0) ? "Missing" : "Ambiguous"));
				list.Add(new CoverageResult(resource.GetType().Name, readOnlyList.Count, status));
			}
		}
		return list;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Editor-only coverage audit creates known Godot Resource types discovered from this assembly.")]
	public IReadOnlyList<CoverageResult> AuditCoverage(IEnumerable<Type> resourceTypes)
	{
		List<Resource> list = new List<Resource>();
		if (resourceTypes != null)
		{
			foreach (Type resourceType in resourceTypes)
			{
				if (resourceType == null || resourceType.IsAbstract || !typeof(Resource).IsAssignableFrom(resourceType))
				{
					continue;
				}
				try
				{
					if (Activator.CreateInstance(resourceType) is Resource item)
					{
						list.Add(item);
					}
				}
				catch (Exception ex)
				{
					GD.PushWarning("[ModEditor Coverage] 无法创建 " + resourceType.Name + ": " + ex.Message);
				}
			}
		}
		return AuditCoverage(list);
	}
}
