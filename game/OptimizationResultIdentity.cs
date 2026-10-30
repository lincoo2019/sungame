public readonly struct OptimizationResultIdentity(string task, OptimizationWorkloadKind workload, string roleId, string scene, string componentTypeId, string componentDefinitionId, string componentInstanceId, string stateMachineDefinitionId, string stateId, string phase, OptimizationScheduleKind schedule, string renderer, int physicsHz, int renderTargetFps)
{
	public string Task { get; } = task;

	public OptimizationWorkloadKind Workload { get; } = workload;

	public string RoleId { get; } = roleId;

	public string Scene { get; } = scene;

	public string ComponentTypeId { get; } = componentTypeId;

	public string ComponentDefinitionId { get; } = componentDefinitionId;

	public string ComponentInstanceId { get; } = componentInstanceId;

	public string StateMachineDefinitionId { get; } = stateMachineDefinitionId;

	public string StateId { get; } = stateId;

	public string Phase { get; } = phase;

	public OptimizationScheduleKind Schedule { get; } = schedule;

	public string Renderer { get; } = renderer;

	public int PhysicsHz { get; } = physicsHz;

	public int RenderTargetFps { get; } = renderTargetFps;
}
