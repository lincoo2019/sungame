internal readonly struct AdobeAnimateCrowdAggregateStats
{
	public int ActiveMounts { get; init; }

	public int ActiveZBuckets { get; init; }

	public int CrowdRoots { get; init; }

	public int CrowdStateTexels { get; init; }

	public int CrowdStateCapacityTexels { get; init; }

	public int ResourceSignatures { get; init; }

	public int StateTextureUploads { get; init; }

	public int StateTextureUpdatedLayers { get; init; }

	public long StateTextureUploadedBytes { get; init; }

	public int MultiMeshUploads { get; init; }

	public long RdSubmittedBatches { get; init; }

	public long RdAppliedBatches { get; init; }

	public long RdQueuedUploads { get; init; }

	public long RdAppliedUploads { get; init; }

	public long RdUploadedBytes { get; init; }

	public long RdBufferRidRefreshes { get; init; }

	public long RdFailedBatches { get; init; }

	public long RdBufferUpdateFailures { get; init; }

	public long RdInvalidTargets { get; init; }

	public long RdStaleDrops { get; init; }

	public long RdQueueDepth { get; init; }

	public long RdMaximumQueueDepth { get; init; }

	public long RdLastQueuedFrameVersion { get; init; }

	public long RdLastAppliedFrameVersion { get; init; }

	public int SignatureConflictBuckets { get; init; }

	public int FallbackRuns { get; init; }

	public int FallbackRoots { get; init; }

	public int ArenaGrowthCount { get; init; }

	public int CrowdMeshQuadCapacity { get; init; }

	public int CrowdMeshVersion { get; init; }

	public int MeshRebinds { get; init; }

	public int CpuRoots { get; init; }

	public int CpuFallbackRoots { get; init; }

	public int CpuValidationFailures { get; init; }

	public int CpuMeshRebuilds { get; init; }

	public int CpuVertexUploads { get; init; }

	public int CpuUploadedVertices { get; init; }

	public int CpuCapacityGrowths { get; init; }

	public int CpuSkippedUploads { get; init; }

	public int CpuSurfaceCount { get; init; }

	public int CpuSharedMaterialRoots { get; init; }

	public int CpuSharedMaterialInstances { get; init; }

	public int CpuRetiredRoots { get; init; }

	public int CpuRetiredSurfaceCount { get; init; }
}
