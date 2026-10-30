using System.Collections.Generic;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

internal sealed record XWEditorResourceIndexResult(List<XWEditorResourceFile> Files, List<XWEditorBuiltInResourceCandidate> BuiltInResources);
