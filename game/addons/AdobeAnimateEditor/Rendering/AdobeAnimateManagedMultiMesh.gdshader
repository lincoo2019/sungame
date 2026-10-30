shader_type canvas_item;

uniform sampler2DArray atlasTextureArray : source_color;
uniform sampler2D instanceDataTexture : repeat_disable, filter_nearest;
uniform sampler2DArray crowdStateTexture : repeat_disable, filter_nearest;
uniform sampler2D simpleAnimationTexture : repeat_disable, filter_nearest;
uniform sampler2DArray poseTextureArray : repeat_disable, filter_nearest;
uniform sampler2DArray gpuRenderGraphTextureArray : repeat_disable, filter_nearest;
uniform sampler2D gpuDynamicOverrideTexture : repeat_disable, filter_nearest;
uniform sampler2D rasterCompositeAtlas : source_color, repeat_disable, filter_linear;
uniform vec2 atlasSize = vec2(1.0, 1.0);
uniform vec2 instanceDataTextureSize = vec2(1.0, 1.0);
uniform vec2 crowdStateTextureSize = vec2(1.0, 1.0);
uniform vec2 simpleAnimationTextureSize = vec2(1.0, 1.0);
uniform vec2 poseTextureSize = vec2(1.0, 1.0);
uniform vec2 gpuRenderGraphTextureSize = vec2(1.0, 1.0);
uniform vec2 gpuDynamicOverrideTextureSize = vec2(1.0, 1.0);
uniform vec2 rasterCompositeAtlasSize = vec2(1.0, 1.0);
uniform vec2 rasterCompositeTileSize = vec2(1.0, 1.0);
uniform vec2 rasterCompositeOrigin = vec2(0.0, 0.0);
uniform float rasterCompositeColumns = 1.0;
uniform float instanceDataStride = 3.0;
uniform float animationClock = 0.0;
uniform float physicsInterpolationFraction = 1.0;

varying vec4 instanceColor;
varying float instanceGrayscale;
// 白闪在纹理和 Buff 染色完成后单独合成，避免乘色互相抵消。
varying float instanceWhiteFlash;
varying vec4 instanceUvRect;
varying float instanceAtlasLayer;
varying float clipUp;
varying float clipDown;
varying float clipEnabled;
varying float clipLeft;
varying float clipRight;
varying vec2 localPos;
varying float clipLocalY;
varying float clipOffsetY;
varying float instanceVisible;
varying float instanceRasterComposite;

vec4 read_instance_texel(float index) {
	float row = floor(index / instanceDataTextureSize.x);
	float col = index - row * instanceDataTextureSize.x;
	ivec2 texel = ivec2(int(col), int(row));
	return texelFetch(instanceDataTexture, texel, 0);
}

vec4 read_crowd_state_texel(float index) {
	float pageTexels = crowdStateTextureSize.x * crowdStateTextureSize.y;
	float layer = floor(index / pageTexels);
	float pageIndex = index - layer * pageTexels;
	float row = floor(pageIndex / crowdStateTextureSize.x);
	float col = pageIndex - row * crowdStateTextureSize.x;
	ivec3 texel = ivec3(int(col), int(row), int(layer));
	return texelFetch(crowdStateTexture, texel, 0);
}

vec4 read_simple_animation_texel(float index) {
	float row = floor(index / simpleAnimationTextureSize.x);
	float col = index - row * simpleAnimationTextureSize.x;
	vec2 uv = (vec2(col, row) + vec2(0.5)) / simpleAnimationTextureSize;
	return texture(simpleAnimationTexture, uv);
}

vec4 read_simple_animation_texel_address(vec2 baseAddress, float offset) {
	float absoluteCol = baseAddress.x + offset;
	float rowOffset = floor(absoluteCol / simpleAnimationTextureSize.x);
	float col = absoluteCol - rowOffset * simpleAnimationTextureSize.x;
	ivec2 texel = ivec2(int(col), int(baseAddress.y + rowOffset));
	return texelFetch(simpleAnimationTexture, texel, 0);
}

float read_crowd_state_base(vec3 encodedAddress) {
	vec3 digits = floor(encodedAddress + vec3(0.5));
	return digits.x + digits.y * 256.0 + digits.z * 65536.0;
}

vec4 read_pose_texel(float index, float layer) {
	float row = floor(index / poseTextureSize.x);
	float col = index - row * poseTextureSize.x;
	ivec3 texel = ivec3(
		int(col),
		int(row),
		int(floor(layer + 0.5)));
	return texelFetch(poseTextureArray, texel, 0);
}

vec4 read_pose_texel_address(vec2 baseAddress, float offset, float layer) {
	float absoluteCol = baseAddress.x + offset;
	float rowOffset = floor(absoluteCol / poseTextureSize.x);
	float col = absoluteCol - rowOffset * poseTextureSize.x;
	ivec3 texel = ivec3(
		int(col),
		int(baseAddress.y + rowOffset),
		int(floor(layer + 0.5)));
	return texelFetch(poseTextureArray, texel, 0);
}

vec4 read_gpu_graph_texel(float index, float layer) {
	float row = floor(index / gpuRenderGraphTextureSize.x);
	float col = index - row * gpuRenderGraphTextureSize.x;
	vec2 uv = (vec2(col, row) + vec2(0.5)) / gpuRenderGraphTextureSize;
	return texture(gpuRenderGraphTextureArray, vec3(uv, floor(layer + 0.5)));
}

vec4 read_gpu_dynamic_override_texel(float index) {
	float row = floor(index / gpuDynamicOverrideTextureSize.x);
	float col = index - row * gpuDynamicOverrideTextureSize.x;
	vec2 uv = (vec2(col, row) + vec2(0.5)) / gpuDynamicOverrideTextureSize;
	return texture(gpuDynamicOverrideTexture, uv);
}

vec2 apply_gpu_graph_basis(vec4 basis, vec2 point) {
	return vec2(
		basis.x * point.x + basis.z * point.y,
		basis.y * point.x + basis.w * point.y);
}

vec2 apply_root_motion(float motionBase, vec2 point) {
	vec4 previousBasis = read_crowd_state_texel(motionBase + 0.0);
	vec4 previousOriginAndFlags = read_crowd_state_texel(motionBase + 1.0);
	if (previousOriginAndFlags.z < 0.5) {
		return point;
	}
	float weight = clamp(physicsInterpolationFraction, 0.0, 1.0);
	vec4 basis = mix(previousBasis, vec4(1.0, 0.0, 0.0, 1.0), weight);
	vec2 origin = mix(previousOriginAndFlags.xy, vec2(0.0), weight);
	return apply_gpu_graph_basis(basis, point) + origin;
}

vec4 multiply_gpu_graph_basis(vec4 left, vec4 right) {
	return vec4(
		apply_gpu_graph_basis(left, right.xy),
		apply_gpu_graph_basis(left, right.zw));
}

vec4 gpu_graph_rotation_basis(float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return vec4(c, s, -s, c);
}

vec4 compose_gpu_graph_basis(float rotation, float skew, vec2 scale) {
	return vec4(
		cos(rotation) * scale.x,
		sin(rotation) * scale.x,
		-sin(rotation + skew) * scale.y,
		cos(rotation + skew) * scale.y);
}

bool sample_gpu_graph_managed_attachment(
	float attachmentPoseBase,
	float frameIndex,
	float nextFrameIndex,
	float frameT,
	float graphPage,
	out vec4 basis,
	out vec2 origin) {
	vec4 pose = read_gpu_graph_texel(attachmentPoseBase + frameIndex * 2.0, graphPage);
	vec4 scale = read_gpu_graph_texel(attachmentPoseBase + frameIndex * 2.0 + 1.0, graphPage);
	basis = compose_gpu_graph_basis(pose.z, pose.w, scale.xy);
	origin = pose.xy;
	if (scale.z < 0.5) {
		return false;
	}
	if (frameT > 0.0 && nextFrameIndex != frameIndex) {
		vec4 nextPose = read_gpu_graph_texel(attachmentPoseBase + nextFrameIndex * 2.0, graphPage);
		vec4 nextScale = read_gpu_graph_texel(attachmentPoseBase + nextFrameIndex * 2.0 + 1.0, graphPage);
		if (nextScale.z > 0.5) {
			// Match body and CPU Slot matrix interpolation during fast rotations.
			basis = mix(basis, compose_gpu_graph_basis(nextPose.z, nextPose.w, nextScale.xy), frameT);
			origin = mix(origin, nextPose.xy, frameT);
		}
	}
	return true;
}

vec2 resolve_gpu_graph_frame(vec4 ownerPose, vec4 ownerClock, vec4 ownerClip, float frameCount) {
	float frameFloat = ownerPose.z + ownerPose.w;
	if (ownerClock.w > 0.5) {
		frameFloat = ownerClock.y + (animationClock - ownerClock.x) * ownerClock.z;
		float clipStart = ownerClip.x;
		float clipEndExclusive = max(ownerClip.y, clipStart + 1.0);
		float clipLength = max(clipEndExclusive - clipStart, 1.0);
		if (ownerClip.z > 0.5) {
			float localFrame = mod(frameFloat - clipStart, clipLength);
			if (localFrame < 0.0) {
				localFrame += clipLength;
			}
			frameFloat = clipStart + localFrame;
		} else {
			frameFloat = clamp(frameFloat, clipStart, max(clipStart, clipEndExclusive - 1.0));
		}
	}
	float rawFrame = floor(frameFloat);
	float frameIndex = clamp(rawFrame, 0.0, max(frameCount - 1.0, 0.0));
	float poseT = rawFrame == frameIndex
		? clamp(frameFloat - rawFrame, 0.0, 1.0)
		: 0.0;
	return vec2(frameIndex, poseT);
}

float resolve_gpu_hit_flash(vec4 flashState) {
	if (flashState.w < 0.5 || flashState.y <= 0.0 || flashState.z <= 0.0) {
		return 0.0;
	}
	float elapsed = max(animationClock - flashState.x, 0.0);
	return flashState.y * max(1.0 - elapsed / flashState.z, 0.0);
}

void blend_gpu_graph_pose(
	float frameLookupBase,
	float ownerLocalSlotCount,
	float ownerLocalSlot,
	float frameCount,
	float poseLayer,
	float graphPage,
	vec4 blendState,
	inout vec4 targetPose0,
	inout vec4 targetPose1) {
	if (blendState.w < 0.5 || blendState.z >= 1.0 || frameCount < 0.5) {
		return;
	}
	float sourceFrameIndex = clamp(blendState.x, 0.0, frameCount - 1.0);
	vec4 sourceFrameSlot = read_gpu_graph_texel(
		frameLookupBase + sourceFrameIndex * ownerLocalSlotCount + ownerLocalSlot,
		graphPage);
	if (sourceFrameSlot.w < 0.5) {
		return;
	}
	float sourcePoseTexel = sourceFrameSlot.x;
	vec4 sourcePose0 = read_pose_texel(sourcePoseTexel + 0.0, poseLayer);
	vec4 sourcePose1 = read_pose_texel(sourcePoseTexel + 1.0, poseLayer);
	vec4 sourceDelta0 = read_pose_texel(sourcePoseTexel + 3.0, poseLayer);
	vec4 sourceDelta1 = read_pose_texel(sourcePoseTexel + 4.0, poseLayer);
	sourcePose0 += sourceDelta0 * blendState.y;
	sourcePose1.xyz += sourceDelta1.xyz * blendState.y;
	targetPose0 = mix(sourcePose0, targetPose0, blendState.z);
	targetPose1.xyz = mix(sourcePose1.xyz, targetPose1.xyz, blendState.z);
}

void blend_gpu_graph_attachment_pose(
	float attachmentPoseLookupBase,
	float frameCount,
	float poseLayer,
	float graphPage,
	vec4 blendState,
	inout vec4 targetPose0,
	inout vec4 targetPose1) {
	if (blendState.w < 0.5 || blendState.z >= 1.0 || frameCount < 0.5) {
		return;
	}
	float sourceFrameIndex = clamp(blendState.x, 0.0, frameCount - 1.0);
	vec4 sourceEntry = read_gpu_graph_texel(
		attachmentPoseLookupBase + sourceFrameIndex,
		graphPage);
	if (sourceEntry.y < 0.5) {
		return;
	}
	vec4 sourcePose0 = read_pose_texel(sourceEntry.x + 0.0, poseLayer);
	vec4 sourcePose1 = read_pose_texel(sourceEntry.x + 1.0, poseLayer);
	if (blendState.y > 0.0) {
		float nextSourceFrameIndex = min(sourceFrameIndex + 1.0, frameCount - 1.0);
		vec4 nextSourceEntry = read_gpu_graph_texel(
			attachmentPoseLookupBase + nextSourceFrameIndex,
			graphPage);
		if (nextSourceEntry.y > 0.5) {
			vec4 nextSourcePose0 = read_pose_texel(nextSourceEntry.x + 0.0, poseLayer);
			vec4 nextSourcePose1 = read_pose_texel(nextSourceEntry.x + 1.0, poseLayer);
			sourcePose0 = mix(sourcePose0, nextSourcePose0, blendState.y);
			sourcePose1.xy = mix(sourcePose1.xy, nextSourcePose1.xy, blendState.y);
		}
	}
	targetPose0 = mix(sourcePose0, targetPose0, blendState.z);
	targetPose1.xy = mix(sourcePose1.xy, targetPose1.xy, blendState.z);
}

float read_gpu_graph_layer_mask(float maskBase, float layerCount, float layerId) {
	if (layerCount < 0.5 || layerId < 0.0 || layerId >= layerCount) {
		return 1.0;
	}
	float chunk = floor(layerId / 16.0);
	float maskTexel = floor(chunk / 4.0);
	float component = chunk - maskTexel * 4.0;
	float bit = layerId - chunk * 16.0;
	vec4 masks = read_crowd_state_texel(maskBase + maskTexel);
	float mask = masks.x;
	if (component >= 2.5) {
		mask = masks.w;
	} else if (component >= 1.5) {
		mask = masks.z;
	} else if (component >= 0.5) {
		mask = masks.y;
	}
	float divisor = pow(2.0, bit);
	return mod(floor(mask / divisor), 2.0);
}

float read_crowd_layer_mask(float stateBase, float layerId) {
	vec4 layerMeta = read_crowd_state_texel(stateBase + 2.0);
	float layerCount = layerMeta.x;
	if (layerCount < 0.5 || layerId < 0.0 || layerId >= layerCount) {
		return 1.0;
	}
	float chunk = floor(layerId / 16.0);
	float maskTexel = floor(chunk / 4.0);
	float component = chunk - maskTexel * 4.0;
	float bit = layerId - chunk * 16.0;
	vec4 masks = read_crowd_state_texel(stateBase + 5.0 + maskTexel);
	float mask = masks.x;
	if (component >= 2.5) {
		mask = masks.w;
	} else if (component >= 1.5) {
		mask = masks.z;
	} else if (component >= 0.5) {
		mask = masks.y;
	}
	float divisor = pow(2.0, bit);
	return mod(floor(mask / divisor), 2.0);
}

void vertex() {
	instanceColor = COLOR;
	clipOffsetY = 0.0;
	instanceVisible = 1.0;
	instanceRasterComposite = 0.0;
	instanceWhiteFlash = 0.0;
	clipLeft = 0.0;
	clipRight = 0.0;

	if (INSTANCE_CUSTOM.w > 7.5) {
		float tileIndex = max(floor(INSTANCE_CUSTOM.x + 0.5), 0.0);
		float columns = max(floor(rasterCompositeColumns + 0.5), 1.0);
		float tileX = mod(tileIndex, columns);
		float tileY = floor(tileIndex / columns);
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		if (slot >= 1.0) {
			instanceVisible = 0.0;
		}
		vec2 safeAtlasSize = max(rasterCompositeAtlasSize, vec2(1.0));
		vec2 safeTileSize = max(rasterCompositeTileSize, vec2(1.0));
		instanceUvRect = vec4(
			vec2(tileX, tileY) * safeTileSize / safeAtlasSize,
			safeTileSize / safeAtlasSize);
		instanceAtlasLayer = 0.0;
		clipUp = INSTANCE_CUSTOM.y;
		clipDown = INSTANCE_CUSTOM.z;
		clipEnabled = INSTANCE_CUSTOM.w > 8.5 ? 1.0 : 0.0;
		localPos = crowdVertex * safeTileSize + rasterCompositeOrigin;
		clipLocalY = localPos.y;
		instanceRasterComposite = 1.0;
		VERTEX = localPos;
	} else if (INSTANCE_CUSTOM.w > 6.5) {
		vec2 metadataAddress = floor(INSTANCE_CUSTOM.xy + vec2(0.5));
		vec4 animationHeader = read_simple_animation_texel_address(metadataAddress, 0.0);
		float frameMax = max(animationHeader.x, 1.0);
		float frameFloat = COLOR.r + animationClock * COLOR.g;
		float normalizedFrame = mod(frameFloat, frameMax);
		if (normalizedFrame < 0.0) {
			normalizedFrame += frameMax;
		}
		float localFrame = floor(normalizedFrame);
		vec4 frameMeta = read_simple_animation_texel_address(metadataAddress, 1.0 + localFrame);
		float poseT = (normalizedFrame - localFrame) * frameMeta.w;
		float frameCount = frameMeta.z;
		float poseLayer = animationHeader.y;
		instanceColor = vec4(1.0);
		clipUp = 0.0;
		clipDown = 0.0;
		clipEnabled = 0.0;
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		if (slot >= frameCount) {
			instanceVisible = 0.0;
			slot = max(frameCount - 1.0, 0.0);
		}
		float poseOffset = slot * 5.0;
		vec4 pose0 = read_pose_texel_address(frameMeta.xy, poseOffset + 0.0, poseLayer);
		vec4 pose1 = read_pose_texel_address(frameMeta.xy, poseOffset + 1.0, poseLayer);
		instanceUvRect = read_pose_texel_address(frameMeta.xy, poseOffset + 2.0, poseLayer);
		vec4 poseDelta0 = read_pose_texel_address(frameMeta.xy, poseOffset + 3.0, poseLayer);
		vec4 poseDelta1 = read_pose_texel_address(frameMeta.xy, poseOffset + 4.0, poseLayer);
		pose0 += poseDelta0 * poseT;
		pose1.xyz += poseDelta1.xyz * poseT;
		instanceAtlasLayer = pose1.w;
		vec2 instanceSize = max(instanceUvRect.zw * atlasSize, vec2(0.0001));
		vec2 sourcePos = crowdVertex * instanceSize;
		localPos = vec2(
			pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
			pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
		clipLocalY = sourcePos.y;
		clipOffsetY = pose1.y;
		instanceColor.a *= pose1.z * instanceVisible;
		VERTEX = localPos;
	} else if (INSTANCE_CUSTOM.w > 5.5) {
		float frameOffset = floor(INSTANCE_CUSTOM.x + 0.5);
		float frameCount = floor(INSTANCE_CUSTOM.y + 0.5);
		float poseLayer = floor(INSTANCE_CUSTOM.z + 0.5);
		float poseBaseTexel = read_crowd_state_base(COLOR.rgb);
		float poseT = clamp(COLOR.a, 0.0, 1.0);
		instanceColor = vec4(1.0);
		clipUp = 0.0;
		clipDown = 0.0;
		clipEnabled = 0.0;
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		if (slot >= frameCount) {
			instanceVisible = 0.0;
			slot = max(frameCount - 1.0, 0.0);
		}
		float poseTexel = poseBaseTexel + (frameOffset + slot) * 5.0;
		vec4 pose0 = read_pose_texel(poseTexel + 0.0, poseLayer);
		vec4 pose1 = read_pose_texel(poseTexel + 1.0, poseLayer);
		instanceUvRect = read_pose_texel(poseTexel + 2.0, poseLayer);
		vec4 poseDelta0 = read_pose_texel(poseTexel + 3.0, poseLayer);
		vec4 poseDelta1 = read_pose_texel(poseTexel + 4.0, poseLayer);
		pose0 += poseDelta0 * poseT;
		pose1.xyz += poseDelta1.xyz * poseT;
		instanceAtlasLayer = pose1.w;
		vec2 instanceSize = max(instanceUvRect.zw * atlasSize, vec2(0.0001));
		vec2 sourcePos = crowdVertex * instanceSize;
		localPos = vec2(
			pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
			pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
		clipLocalY = sourcePos.y;
		clipOffsetY = pose1.y;
		instanceColor.a *= pose1.z * instanceVisible;
		VERTEX = localPos;
	} else if (INSTANCE_CUSTOM.w > 4.5) {
		float stateBase = read_crowd_state_base(INSTANCE_CUSTOM.xyz);
		vec4 graphState = read_crowd_state_texel(stateBase + 0.0);
		vec4 instanceState = read_crowd_state_texel(stateBase + 1.0);
		float graphBase = graphState.x;
		float graphPage = graphState.y;
		float renderSlotCount = graphState.z;
		float ownerCount = instanceState.x;
		float ownerMetadataBase = instanceState.y;
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		bool graphVertexValid = slot < renderSlotCount
			&& renderSlotCount >= 0.5
			&& ownerCount >= 0.5;
		vec4 graphHeader = vec4(0.0);
		if (graphVertexValid) {
			graphHeader = read_gpu_graph_texel(graphBase + 0.0, graphPage);
			graphVertexValid = graphHeader.x >= 0.5
				&& abs(graphHeader.x - graphState.w) <= 0.1;
		}

		if (graphVertexValid) {
		vec4 graphAddresses = read_gpu_graph_texel(graphBase + 1.0, graphPage);
		float ownerBase = graphAddresses.x;
		float renderSlotBase = graphAddresses.y;
		vec4 renderSlot = read_gpu_graph_texel(renderSlotBase + slot, graphPage);
		float ownerIndex = floor(renderSlot.x + 0.5);
		float ownerLocalSlot = floor(renderSlot.y + 0.5);
		float renderSlotKind = floor(renderSlot.z + 0.5);
		if (ownerIndex < 0.0 || ownerIndex >= ownerCount || renderSlotKind > 1.5) {
			instanceVisible = 0.0;
			ownerIndex = 0.0;
		}

		vec4 ownerDescriptor = read_gpu_graph_texel(ownerBase + ownerIndex * 6.0 + 0.0, graphPage);
		vec4 ownerDefinition = read_gpu_graph_texel(ownerBase + ownerIndex * 6.0 + 1.0, graphPage);
		float ownerLocalSlotCount = ownerDescriptor.w;
		float frameCount = ownerDefinition.x;
		float frameLookupBase = ownerDefinition.y;
		float ownerStateBase = stateBase + 4.0 + ownerIndex * 10.0;
		vec4 ownerBasis = read_crowd_state_texel(ownerStateBase + 0.0);
		vec4 ownerPose = read_crowd_state_texel(ownerStateBase + 1.0);
		vec4 ownerRender = read_crowd_state_texel(ownerStateBase + 2.0);
		vec4 ownerFlags = read_crowd_state_texel(ownerStateBase + 3.0);
		vec4 ownerColor = read_crowd_state_texel(ownerStateBase + 4.0);
		vec4 ownerClock = read_crowd_state_texel(ownerStateBase + 5.0);
		vec4 ownerClip = read_crowd_state_texel(ownerStateBase + 6.0);
		vec4 ownerBlend = read_crowd_state_texel(ownerStateBase + 7.0);
		vec4 ownerBrightFlash = read_crowd_state_texel(ownerStateBase + 8.0);
		vec4 ownerWhiteFlash = read_crowd_state_texel(ownerStateBase + 9.0);
		vec4 ownerMetadata = read_crowd_state_texel(ownerMetadataBase + ownerIndex);
		float ownerBrightFlashStrength = resolve_gpu_hit_flash(ownerBrightFlash);
		float ownerWhiteFlashStrength = resolve_gpu_hit_flash(ownerWhiteFlash);
		ownerColor.rgb *= 1.0 + ownerBrightFlashStrength;
		instanceWhiteFlash = ownerWhiteFlashStrength;
		vec2 resolvedFrame = resolve_gpu_graph_frame(ownerPose, ownerClock, ownerClip, frameCount);
		float frameIndex = resolvedFrame.x;
		vec2 sourcePos = vec2(0.0);
		vec2 ownerLocalPos = vec2(0.0);
		float poseAlpha = 1.0;
		float layerVisible = 1.0;
		if (renderSlotKind < 0.5) {
			if (ownerLocalSlot < 0.0
				|| ownerLocalSlot >= ownerLocalSlotCount
				|| frameCount < 0.5
				|| ownerFlags.y < 0.5) {
				instanceVisible = 0.0;
				ownerLocalSlot = 0.0;
			}

			float lookupIndex = frameLookupBase + frameIndex * ownerLocalSlotCount + ownerLocalSlot;
			vec4 frameSlot = read_gpu_graph_texel(lookupIndex, graphPage);
			if (frameSlot.w < 0.5) {
				instanceVisible = 0.0;
			}

			float poseTexel = frameSlot.x;
			float poseLayer = ownerDefinition.w;
			float poseT = resolvedFrame.y;
			vec4 pose0 = read_pose_texel(poseTexel + 0.0, poseLayer);
			vec4 pose1 = read_pose_texel(poseTexel + 1.0, poseLayer);
			vec4 poseUvRect = read_pose_texel(poseTexel + 2.0, poseLayer);
			vec4 poseDelta0 = read_pose_texel(poseTexel + 3.0, poseLayer);
			vec4 poseDelta1 = read_pose_texel(poseTexel + 4.0, poseLayer);
			bool usedLoopTerminalPose = false;
			if (ownerClip.z > 0.5 && frameIndex >= ownerClip.y - 1.0) {
				float loopFrameIndex = clamp(ownerClip.x, 0.0, frameCount - 1.0);
				vec4 loopFrameSlot = read_gpu_graph_texel(
					frameLookupBase + loopFrameIndex * ownerLocalSlotCount + ownerLocalSlot,
					graphPage);
				if (loopFrameSlot.w > 0.5) {
					vec4 loopPose0 = read_pose_texel(loopFrameSlot.x + 0.0, poseLayer);
					vec4 loopPose1 = read_pose_texel(loopFrameSlot.x + 1.0, poseLayer);
					pose0 = mix(pose0, loopPose0, poseT);
					pose1.xyz = mix(pose1.xyz, loopPose1.xyz, poseT);
					usedLoopTerminalPose = true;
				}
			}
			if (!usedLoopTerminalPose) {
				pose0 += poseDelta0 * poseT;
				pose1.xyz += poseDelta1.xyz * poseT;
			}
			blend_gpu_graph_pose(
				frameLookupBase,
				ownerLocalSlotCount,
				ownerLocalSlot,
				frameCount,
				poseLayer,
				graphPage,
				ownerBlend,
				pose0,
				pose1);

			instanceUvRect = poseUvRect;
			instanceAtlasLayer = pose1.w;
			vec2 sourceSize = max(poseUvRect.zw * atlasSize, vec2(0.0001));
			float mediaId = frameSlot.y;
			if (ownerMetadata.z >= 0.0 && mediaId >= 0.0 && mediaId < ownerMetadata.w) {
				float mediaOverrideBase = ownerMetadata.z + mediaId * 2.0;
				vec4 mediaUvRect = read_gpu_dynamic_override_texel(mediaOverrideBase + 0.0);
				vec4 mediaOverride = read_gpu_dynamic_override_texel(mediaOverrideBase + 1.0);
				if (mediaOverride.w > 0.5) {
					instanceUvRect = mediaUvRect;
					sourceSize = max(mediaOverride.xy, vec2(0.0001));
					instanceAtlasLayer = mediaOverride.z;
				}
			}
			sourcePos = crowdVertex * sourceSize;
			vec2 posePos = vec2(
				pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
				pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
			ownerLocalPos = posePos + ownerRender.xy;
			layerVisible = read_gpu_graph_layer_mask(ownerFlags.z, ownerFlags.w, frameSlot.z);
			clipUp = ownerRender.z;
			clipDown = ownerRender.w;
			clipEnabled = ownerFlags.x;
			clipLocalY = sourcePos.y;
			clipOffsetY = pose1.y;
			instanceColor = ownerColor;
			poseAlpha = pose1.z;
		} else {
			vec4 graphManagedVisuals = read_gpu_graph_texel(graphBase + 2.0, graphPage);
			float managedVisualBindingBase = graphManagedVisuals.x;
			float managedVisualBindingCount = graphManagedVisuals.y;
			float managedVisualStateBase = instanceState.z;
			float managedVisualStateCount = instanceState.w;
			float managedVisualIndex = floor(renderSlot.w + 0.5);
			if (managedVisualIndex < 0.0
				|| managedVisualIndex >= managedVisualBindingCount
				|| managedVisualIndex >= managedVisualStateCount
				|| managedVisualBindingCount < 0.5
				|| managedVisualStateBase < 0.0
				|| ownerFlags.y < 0.5) {
				instanceVisible = 0.0;
				managedVisualIndex = 0.0;
			}

			vec4 managedBinding = read_gpu_graph_texel(
				managedVisualBindingBase + managedVisualIndex,
				graphPage);
			if (floor(managedBinding.x + 0.5) != ownerIndex) {
				instanceVisible = 0.0;
			}
			float visualStateBase = managedVisualStateBase + managedVisualIndex * 7.0;
			vec4 visualGeometry = read_crowd_state_texel(visualStateBase + 0.0);
			vec4 visualUv = read_crowd_state_texel(visualStateBase + 1.0);
			vec4 visualDraw = read_crowd_state_texel(visualStateBase + 2.0);
			vec4 visualBasis = read_crowd_state_texel(visualStateBase + 3.0);
			vec4 visualOriginAndOffset = read_crowd_state_texel(visualStateBase + 4.0);
			vec4 visualColor = read_crowd_state_texel(visualStateBase + 5.0);
			vec4 visualFlags = read_crowd_state_texel(visualStateBase + 6.0);

			instanceUvRect = visualUv;
			instanceAtlasLayer = visualGeometry.z;
			instanceVisible *= visualGeometry.w;
			instanceColor = ownerColor * visualColor;
			if (visualFlags.w > 0.5) {
				float blinkColor = 0.75 + sin(TIME * 15.0) * 0.25;
				instanceColor.rgb *= blinkColor;
			}
			vec2 sourceSize = max(visualGeometry.xy, vec2(0.0001));
			sourcePos = crowdVertex * sourceSize;
			vec2 spritePoint = apply_gpu_graph_basis(
				visualBasis,
				visualDraw.xy + sourcePos * visualDraw.zw)
				+ visualOriginAndOffset.xy;

			float attachmentPoseBase = managedBinding.w;
			bool hasManagedAttachment = attachmentPoseBase >= 0.0 && frameCount >= 0.5;
			float nextFrameIndex = min(frameIndex + 1.0, frameCount - 1.0);
			if (ownerClip.z > 0.5 && frameIndex >= ownerClip.y - 1.0) {
				nextFrameIndex = clamp(ownerClip.x, 0.0, frameCount - 1.0);
			}
			vec4 slotBasis = vec4(1.0, 0.0, 0.0, 1.0);
			vec2 attachmentOrigin = vec2(0.0);
			if (hasManagedAttachment) {
				if (!sample_gpu_graph_managed_attachment(
					attachmentPoseBase, frameIndex, nextFrameIndex, resolvedFrame.y,
					graphPage, slotBasis, attachmentOrigin)) {
					instanceVisible = 0.0;
				}
				// Apply the same captured source pose and blend weight as the body.
				if (ownerBlend.w > 0.5 && ownerBlend.z < 1.0) {
					float sourceFrameIndex = clamp(ownerBlend.x, 0.0, frameCount - 1.0);
					vec4 sourceBasis;
					vec2 sourceOrigin;
					if (sample_gpu_graph_managed_attachment(
						attachmentPoseBase, sourceFrameIndex, min(sourceFrameIndex + 1.0, frameCount - 1.0),
						ownerBlend.y, graphPage, sourceBasis, sourceOrigin)) {
						slotBasis = mix(sourceBasis, slotBasis, ownerBlend.z);
						attachmentOrigin = mix(sourceOrigin, attachmentOrigin, ownerBlend.z);
					}
				}
			}
			vec2 ownerVisualOffset = hasManagedAttachment
				? ownerRender.xy
				: vec2(0.0);
			vec2 slotOrigin = attachmentOrigin
				+ ownerVisualOffset
				+ apply_gpu_graph_basis(slotBasis, visualOriginAndOffset.zw);
			if (visualFlags.x < 0.5) {
				float slotRotation = atan(slotBasis.y, slotBasis.x);
				slotBasis = multiply_gpu_graph_basis(slotBasis, gpu_graph_rotation_basis(-slotRotation));
			}
			if (visualFlags.y < 0.5) {
				float slotScaleX = max(length(slotBasis.xy), 0.000001);
				float slotScaleY = max(length(slotBasis.zw), 0.000001);
				slotBasis.xy /= slotScaleX;
				slotBasis.zw /= slotScaleY;
			}
			if (visualFlags.z < 0.5) {
				float slotRotation = atan(slotBasis.y, slotBasis.x);
				float slotScaleX = length(slotBasis.xy);
				float slotScaleY = length(slotBasis.zw);
				float determinant = slotBasis.x * slotBasis.w - slotBasis.y * slotBasis.z;
				if (determinant < 0.0) {
					slotScaleY = -slotScaleY;
				}
				vec4 noSkewBasis = gpu_graph_rotation_basis(slotRotation);
				noSkewBasis.xy *= slotScaleX;
				noSkewBasis.zw *= slotScaleY;
				slotBasis = noSkewBasis;
			}
			ownerLocalPos = apply_gpu_graph_basis(slotBasis, spritePoint) + slotOrigin;
			clipUp = 0.0;
			clipDown = 0.0;
			clipEnabled = 0.0;
			clipLocalY = sourcePos.y;
			clipOffsetY = 0.0;
		}
		vec4 resolvedOwnerBasis = vec4(1.0, 0.0, 0.0, 1.0);
		vec2 resolvedOwnerOrigin = vec2(0.0);
		float chainOwnerIndex = ownerIndex;
		for (int depth = 0; depth < 8; depth++) {
			vec4 chainDescriptor = read_gpu_graph_texel(ownerBase + chainOwnerIndex * 6.0 + 0.0, graphPage);
			float parentOwnerIndex = floor(chainDescriptor.x + 0.5);
			vec4 localOwnerBasis = vec4(1.0, 0.0, 0.0, 1.0);
			vec2 localOwnerOrigin = vec2(0.0);
			if (parentOwnerIndex < 0.0) {
				float chainStateBase = stateBase + 4.0 + chainOwnerIndex * 10.0;
				localOwnerBasis = read_crowd_state_texel(chainStateBase + 0.0);
				localOwnerOrigin = read_crowd_state_texel(chainStateBase + 1.0).xy;
			} else {
				float attachmentPoseLookupBase = chainDescriptor.z;
				vec4 attachmentFlags = read_gpu_graph_texel(ownerBase + chainOwnerIndex * 6.0 + 2.0, graphPage);
				vec4 attachmentOffsets = read_gpu_graph_texel(ownerBase + chainOwnerIndex * 6.0 + 3.0, graphPage);
				vec4 staticLocalBasis = read_gpu_graph_texel(ownerBase + chainOwnerIndex * 6.0 + 4.0, graphPage);
				vec4 staticLocalPose = read_gpu_graph_texel(ownerBase + chainOwnerIndex * 6.0 + 5.0, graphPage);
				vec4 parentDefinition = read_gpu_graph_texel(ownerBase + parentOwnerIndex * 6.0 + 1.0, graphPage);
				float parentFrameCount = parentDefinition.x;
				float parentStateBase = stateBase + 4.0 + parentOwnerIndex * 10.0;
				vec4 parentStatePose = read_crowd_state_texel(parentStateBase + 1.0);
				vec4 parentStateRender = read_crowd_state_texel(parentStateBase + 2.0);
				vec4 parentClock = read_crowd_state_texel(parentStateBase + 5.0);
				vec4 parentClip = read_crowd_state_texel(parentStateBase + 6.0);
				vec4 parentBlend = read_crowd_state_texel(parentStateBase + 7.0);
				vec2 resolvedParentFrame = resolve_gpu_graph_frame(
					parentStatePose,
					parentClock,
					parentClip,
					parentFrameCount);
				float parentFrameIndex = resolvedParentFrame.x;
				if (attachmentPoseLookupBase < 0.0
					|| parentOwnerIndex >= ownerCount
					|| parentFrameCount < 0.5) {
					instanceVisible = 0.0;
					break;
				}
				float nextParentFrameIndex = min(parentFrameIndex + 1.0, parentFrameCount - 1.0);
				if (parentClip.z > 0.5 && parentFrameIndex >= parentClip.y - 1.0) {
					nextParentFrameIndex = clamp(parentClip.x, 0.0, parentFrameCount - 1.0);
				}
				vec4 attachmentPoseEntry = read_gpu_graph_texel(
					attachmentPoseLookupBase + parentFrameIndex,
					graphPage);
				vec4 nextAttachmentPoseEntry = read_gpu_graph_texel(
					attachmentPoseLookupBase + nextParentFrameIndex,
					graphPage);
				if (attachmentPoseEntry.y < 0.5 || nextAttachmentPoseEntry.y < 0.5) {
					instanceVisible = 0.0;
					break;
				}
				float attachmentPoseTexel = attachmentPoseEntry.x;
				float attachmentPoseLayer = parentDefinition.w;
				float attachmentPoseT = resolvedParentFrame.y;
				vec4 attachmentPose0 = read_pose_texel(attachmentPoseTexel + 0.0, attachmentPoseLayer);
				vec4 attachmentPose1 = read_pose_texel(attachmentPoseTexel + 1.0, attachmentPoseLayer);
				vec4 nextAttachmentPose0 = read_pose_texel(nextAttachmentPoseEntry.x + 0.0, attachmentPoseLayer);
				vec4 nextAttachmentPose1 = read_pose_texel(nextAttachmentPoseEntry.x + 1.0, attachmentPoseLayer);
				attachmentPose0 += (nextAttachmentPose0 - attachmentPose0) * attachmentPoseT;
				attachmentPose1.xy += (nextAttachmentPose1.xy - attachmentPose1.xy) * attachmentPoseT;
				blend_gpu_graph_attachment_pose(
					attachmentPoseLookupBase,
					parentFrameCount,
					attachmentPoseLayer,
					graphPage,
					parentBlend,
					attachmentPose0,
					attachmentPose1);

				vec4 attachmentBasis = attachmentPose0;
				vec2 attachmentOrigin = attachmentPose1.xy;
				if (chainDescriptor.y > 2.5) {
					vec4 slotBasis = attachmentBasis;
					vec2 slotOrigin = attachmentOrigin
						+ parentStateRender.xy
						+ apply_gpu_graph_basis(attachmentBasis, attachmentOffsets.zw);
					if (attachmentFlags.y < 0.5) {
						float slotRotation = atan(slotBasis.y, slotBasis.x);
						slotBasis = multiply_gpu_graph_basis(slotBasis, gpu_graph_rotation_basis(-slotRotation));
					}
					if (attachmentFlags.z < 0.5) {
						float slotScaleX = max(length(slotBasis.xy), 0.000001);
						float slotScaleY = max(length(slotBasis.zw), 0.000001);
						slotBasis.xy /= slotScaleX;
						slotBasis.zw /= slotScaleY;
					}
					if (attachmentFlags.w < 0.5) {
						float slotRotation = atan(slotBasis.y, slotBasis.x);
						float slotScaleX = length(slotBasis.xy);
						float slotScaleY = length(slotBasis.zw);
						float determinant = slotBasis.x * slotBasis.w - slotBasis.y * slotBasis.z;
						if (determinant < 0.0) {
							slotScaleY = -slotScaleY;
						}
						vec4 noSkewBasis = gpu_graph_rotation_basis(slotRotation);
						noSkewBasis.xy *= slotScaleX;
						noSkewBasis.zw *= slotScaleY;
						slotBasis = noSkewBasis;
					}
					localOwnerBasis = multiply_gpu_graph_basis(slotBasis, staticLocalBasis);
					localOwnerOrigin = apply_gpu_graph_basis(slotBasis, staticLocalPose.xy) + slotOrigin;
				} else if (attachmentFlags.x > 0.5) {
					localOwnerOrigin = attachmentOrigin + parentStateRender.xy;
					if (attachmentFlags.y > 0.5) {
						float attachmentRotation = atan(attachmentBasis.y, attachmentBasis.x) + staticLocalPose.z;
						localOwnerBasis = multiply_gpu_graph_basis(gpu_graph_rotation_basis(attachmentRotation), staticLocalBasis);
					} else {
						localOwnerBasis = staticLocalBasis;
					}
				} else {
					localOwnerBasis = staticLocalBasis;
					localOwnerOrigin = staticLocalPose.xy;
				}
			}

			resolvedOwnerOrigin = apply_gpu_graph_basis(localOwnerBasis, resolvedOwnerOrigin) + localOwnerOrigin;
			resolvedOwnerBasis = multiply_gpu_graph_basis(localOwnerBasis, resolvedOwnerBasis);
			if (parentOwnerIndex < 0.0) {
				break;
			}
			chainOwnerIndex = parentOwnerIndex;
		}
		vec2 resolvedPos = apply_gpu_graph_basis(resolvedOwnerBasis, ownerLocalPos) + resolvedOwnerOrigin;
		resolvedPos = apply_root_motion(stateBase + 2.0, resolvedPos);
		instanceColor.a *= poseAlpha * instanceVisible * layerVisible;
		localPos = resolvedPos;
		VERTEX = resolvedPos;
		} else {
			instanceVisible = 0.0;
			instanceColor.a = 0.0;
			instanceUvRect = vec4(0.0);
			instanceAtlasLayer = 0.0;
			clipUp = 0.0;
			clipDown = 0.0;
			clipEnabled = 0.0;
			localPos = vec2(0.0);
			clipLocalY = 0.0;
			VERTEX = vec2(-1048576.0);
		}
	} else if (INSTANCE_CUSTOM.w > 3.5) {
		float stateBase = read_crowd_state_base(INSTANCE_CUSTOM.xyz);
		vec4 header = read_crowd_state_texel(stateBase);
		float commandCount = header.x;
		float commandTexelsPerItem = header.y;
		float ownerCount = header.w;
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		if (slot >= commandCount || ownerCount < 0.5 || header.z < 1.5) {
			instanceVisible = 0.0;
			slot = max(commandCount - 1.0, 0.0);
		}
		float cachedCommandBase = stateBase + 3.0 + ownerCount * 4.0 + slot * commandTexelsPerItem;
		vec4 commandMeta = read_crowd_state_texel(cachedCommandBase + 0.0);
		float ownerIndex = floor(commandMeta.y + 0.5);
		if (ownerIndex < 0.0 || ownerIndex >= ownerCount) {
			instanceVisible = 0.0;
			ownerIndex = 0.0;
		}
		float cachedOwnerBase = stateBase + 3.0 + ownerIndex * 4.0;
		vec4 ownerBasis = read_crowd_state_texel(cachedOwnerBase + 0.0);
		vec4 ownerPose = read_crowd_state_texel(cachedOwnerBase + 1.0);
		vec4 ownerClip = read_crowd_state_texel(cachedOwnerBase + 2.0);
		vec4 ownerColor = read_crowd_state_texel(cachedOwnerBase + 3.0);
		float poseTexel = commandMeta.x;
		float poseLayer = ownerPose.z;
		float poseT = ownerPose.w;
		vec4 pose0 = read_pose_texel(poseTexel + 0.0, poseLayer);
		vec4 pose1 = read_pose_texel(poseTexel + 1.0, poseLayer);
		vec4 poseUvRect = read_pose_texel(poseTexel + 2.0, poseLayer);
		vec4 poseDelta0 = read_pose_texel(poseTexel + 3.0, poseLayer);
		vec4 poseDelta1 = read_pose_texel(poseTexel + 4.0, poseLayer);
		pose0 += poseDelta0 * poseT;
		pose1.xyz += poseDelta1.xyz * poseT;

		instanceUvRect = poseUvRect;
		instanceAtlasLayer = pose1.w;
		vec2 sourceSize = max(poseUvRect.zw * atlasSize, vec2(0.0001));
		float visualOverrideIndexPlusOne = commandMeta.z;
		if (visualOverrideIndexPlusOne > 0.5) {
			float cachedVisualBase = stateBase + 3.0 + ownerCount * 4.0
				+ commandCount * commandTexelsPerItem
				+ (visualOverrideIndexPlusOne - 1.0) * 2.0;
			vec4 visualGeometry = read_crowd_state_texel(cachedVisualBase + 0.0);
			vec4 visualData = read_crowd_state_texel(cachedVisualBase + 1.0);
			sourceSize = max(visualGeometry.xy, vec2(0.0001));
			instanceUvRect = vec4(visualGeometry.zw, visualData.xy);
			instanceAtlasLayer = visualData.z;
		}
		vec2 sourcePos = crowdVertex * sourceSize;
		vec2 posePos = vec2(
			pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
			pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
		vec2 resolvedPos = vec2(
			ownerBasis.x * posePos.x + ownerBasis.z * posePos.y + ownerPose.x,
			ownerBasis.y * posePos.x + ownerBasis.w * posePos.y + ownerPose.y);
		clipUp = ownerClip.x;
		clipDown = ownerClip.y;
		clipEnabled = ownerClip.z;
		clipLocalY = sourcePos.y;
		clipOffsetY = pose1.y;
		instanceColor = ownerColor;
		instanceColor.a *= pose1.z * instanceVisible;
		resolvedPos = apply_root_motion(stateBase + 1.0, resolvedPos);
		localPos = resolvedPos;
		VERTEX = resolvedPos;
	} else if (INSTANCE_CUSTOM.w > 2.5) {
		float stateBase = read_crowd_state_base(INSTANCE_CUSTOM.xyz);
		vec4 header = read_crowd_state_texel(stateBase);
		float commandCount = header.x;
		float commandTexelsPerItem = header.y;
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		if (slot >= commandCount || header.z < 0.5) {
			instanceVisible = 0.0;
			slot = max(commandCount - 1.0, 0.0);
		}
		float commandBase = stateBase + 3.0 + slot * commandTexelsPerItem;
		vec4 poseMeta = read_crowd_state_texel(commandBase + 0.0);
		vec4 basis = read_crowd_state_texel(commandBase + 1.0);
		vec4 geometry = read_crowd_state_texel(commandBase + 2.0);
		vec4 visual = read_crowd_state_texel(commandBase + 3.0);
		vec4 renderMeta = read_crowd_state_texel(commandBase + 4.0);
		vec4 commandColor = read_crowd_state_texel(commandBase + 5.0);
		float commandMode = poseMeta.w;
		instanceUvRect = visual;
		instanceAtlasLayer = renderMeta.x;
		clipUp = renderMeta.y;
		clipDown = renderMeta.z;
		clipEnabled = renderMeta.w;
		instanceColor = commandColor;
		vec2 sourceSize = max(geometry.zw, vec2(0.0001));
		vec2 sourcePos = crowdVertex * sourceSize;
		vec2 resolvedPos = sourcePos;
		clipLocalY = sourcePos.y;
		clipOffsetY = 0.0;

		if (commandMode > 0.5) {
			float poseTexel = poseMeta.x;
			float poseLayer = poseMeta.y;
			float poseT = poseMeta.z;
			vec4 pose0 = read_pose_texel(poseTexel + 0.0, poseLayer);
			vec4 pose1 = read_pose_texel(poseTexel + 1.0, poseLayer);
			vec4 poseUvRect = read_pose_texel(poseTexel + 2.0, poseLayer);
			vec4 poseDelta0 = read_pose_texel(poseTexel + 3.0, poseLayer);
			vec4 poseDelta1 = read_pose_texel(poseTexel + 4.0, poseLayer);
			pose0 += poseDelta0 * poseT;
			pose1.xyz += poseDelta1.xyz * poseT;
			if (commandMode < 1.5) {
				instanceUvRect = poseUvRect;
				instanceAtlasLayer = pose1.w;
				sourceSize = max(poseUvRect.zw * atlasSize, vec2(0.0001));
				sourcePos = crowdVertex * sourceSize;
			}
			vec2 posePos = vec2(
				pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
				pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
			resolvedPos = vec2(
				basis.x * posePos.x + basis.z * posePos.y + geometry.x,
				basis.y * posePos.x + basis.w * posePos.y + geometry.y);
			clipLocalY = sourcePos.y;
			clipOffsetY = pose1.y;
			instanceColor.a *= pose1.z;
		} else {
			resolvedPos = vec2(
				basis.x * sourcePos.x + basis.z * sourcePos.y + geometry.x,
				basis.y * sourcePos.x + basis.w * sourcePos.y + geometry.y);
		}
		instanceColor.a *= instanceVisible;
		resolvedPos = apply_root_motion(stateBase + 1.0, resolvedPos);
		localPos = resolvedPos;
		VERTEX = resolvedPos;
	} else if (INSTANCE_CUSTOM.w > 1.5) {
		float stateBase = read_crowd_state_base(INSTANCE_CUSTOM.xyz);
		vec4 state0 = read_crowd_state_texel(stateBase + 0.0);
		vec4 state1 = read_crowd_state_texel(stateBase + 1.0);
		float frameOffset = state0.x;
		float frameCount = state0.y;
		float poseBaseTexel = state0.z;
		float poseLayer = state0.w;
		float poseT = state1.x;
		clipUp = state1.y;
		clipDown = state1.z;
		clipEnabled = state1.w;
		vec4 layerMeta = read_crowd_state_texel(stateBase + 2.0);
		float maskTexelCount = layerMeta.y;
		float visualOverrideEnabled = layerMeta.z;
		float slot = floor(VERTEX.y / 2.0);
		vec2 crowdVertex = vec2(VERTEX.x, VERTEX.y - slot * 2.0);
		if (slot >= frameCount) {
			instanceVisible = 0.0;
			slot = max(frameCount - 1.0, 0.0);
		}
		float sourceIndex = frameOffset + slot;
		float poseTexel = poseBaseTexel + sourceIndex * 5.0;
		vec4 pose0 = read_pose_texel(poseTexel + 0.0, poseLayer);
		vec4 pose1 = read_pose_texel(poseTexel + 1.0, poseLayer);
		instanceUvRect = read_pose_texel(poseTexel + 2.0, poseLayer);
		vec4 poseDelta0 = read_pose_texel(poseTexel + 3.0, poseLayer);
		vec4 poseDelta1 = read_pose_texel(poseTexel + 4.0, poseLayer);
		float layerVisible = read_crowd_layer_mask(stateBase, poseDelta1.w);
		pose0 += poseDelta0 * poseT;
		pose1.xyz += poseDelta1.xyz * poseT;
		instanceAtlasLayer = pose1.w;
		if (visualOverrideEnabled > 0.5) {
			float visualBase = stateBase + 5.0 + maskTexelCount + slot * 2.0;
			vec4 visualUvRect = read_crowd_state_texel(visualBase + 0.0);
			vec4 visualMeta = read_crowd_state_texel(visualBase + 1.0);
			if (visualMeta.y > 0.5) {
				instanceUvRect = visualUvRect;
				instanceAtlasLayer = visualMeta.x;
			}
		}
		vec2 instanceSize = max(instanceUvRect.zw * atlasSize, vec2(0.0001, 0.0001));
		vec2 sourcePos = crowdVertex * instanceSize;
		localPos = vec2(
			pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
			pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
		localPos = apply_root_motion(stateBase + 3.0, localPos);
		clipLocalY = sourcePos.y;
		clipOffsetY = pose1.y;
		instanceColor.a *= pose1.z * instanceVisible * layerVisible;
		VERTEX = localPos;
	} else if (INSTANCE_CUSTOM.w > 0.5) {
		float base = INSTANCE_CUSTOM.x * instanceDataStride;
		vec4 posePack = read_instance_texel(base + 0.0);
		float poseLayer = INSTANCE_CUSTOM.y;
		float poseT = INSTANCE_CUSTOM.z;
		vec4 visualUvRect = read_instance_texel(base + 1.0);
		vec4 visualMeta = read_instance_texel(base + 2.0);
		float visualOverrideEnabled = visualMeta.y;
		vec4 pose0 = read_pose_texel_address(posePack.xy, 0.0, poseLayer);
		vec4 pose1 = read_pose_texel_address(posePack.xy, 1.0, poseLayer);
		instanceUvRect = read_pose_texel_address(posePack.xy, 2.0, poseLayer);
		vec4 poseDelta0 = read_pose_texel_address(posePack.xy, 3.0, poseLayer);
		vec4 poseDelta1 = read_pose_texel_address(posePack.xy, 4.0, poseLayer);
		pose0 += poseDelta0 * poseT;
		pose1.xyz += poseDelta1.xyz * poseT;
		instanceAtlasLayer = pose1.w;
		if (visualOverrideEnabled > 0.5) {
			instanceUvRect = visualUvRect;
			instanceAtlasLayer = visualMeta.x;
		}
		clipUp = posePack.z;
		clipDown = posePack.w;
		clipEnabled = visualMeta.z;
		clipLeft = floor(visualMeta.w / 4096.0);
		clipRight = visualMeta.w - clipLeft * 4096.0;
		vec2 instanceSize = max(instanceUvRect.zw * atlasSize, vec2(0.0001, 0.0001));
		vec2 sourcePos = VERTEX * instanceSize;
		localPos = vec2(
			pose0.x * sourcePos.x + pose0.z * sourcePos.y + pose1.x,
			pose0.y * sourcePos.x + pose0.w * sourcePos.y + pose1.y);
		clipLocalY = sourcePos.y;
		clipOffsetY = pose1.y;
		instanceColor.a *= pose1.z;
		VERTEX = localPos;
	} else {
		vec2 instanceSize = max(INSTANCE_CUSTOM.yz, vec2(0.0001, 0.0001));
		float base = INSTANCE_CUSTOM.x * instanceDataStride;
		instanceUvRect = read_instance_texel(base + 0.0);
		vec4 packed = read_instance_texel(base + 1.0);
		instanceAtlasLayer = packed.x;
		clipUp = packed.y;
		clipDown = packed.z;
		clipEnabled = packed.w;
		vec4 horizontalClip = read_instance_texel(base + 2.0);
		clipLeft = horizontalClip.x;
		clipRight = horizontalClip.y;
		localPos = VERTEX * instanceSize;
		clipLocalY = localPos.y;
		VERTEX = localPos;
	}
	instanceGrayscale = instanceColor.a < 0.0 ? 1.0 : 0.0;
	instanceColor.a = abs(instanceColor.a);
}

void fragment() {
	if (instanceVisible < 0.5) {
		discard;
	}

	if (clipEnabled > 0.5 && (FRAGCOORD.y < clipUp || FRAGCOORD.y > clipDown)) {
		discard;
	}
	if (clipEnabled > 0.5
		&& clipRight > clipLeft
		&& (FRAGCOORD.x < clipLeft || FRAGCOORD.x > clipRight)) {
		discard;
	}
	vec2 uv = instanceUvRect.xy + UV * instanceUvRect.zw;
	COLOR = (instanceRasterComposite > 0.5
		? texture(rasterCompositeAtlas, uv)
		: texture(atlasTextureArray, vec3(uv, floor(instanceAtlasLayer + 0.5)))) * instanceColor;
	if (instanceGrayscale > 0.5) {
		float luminance = (COLOR.r + COLOR.g + COLOR.b) / 3.0;
		COLOR.rgb = vec3(luminance);
	}
	COLOR.rgb = mix(COLOR.rgb, vec3(1.0), clamp(instanceWhiteFlash, 0.0, 1.0));
}
