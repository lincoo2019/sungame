shader_type canvas_item;

// STATIC 子弹 MultiMesh 批渲染 shader（BulletField Task 1.3）
//
// 纹理图集 + per-instance custom_data 选 sprite：
//   - 图集把所有 STATIC 子弹的 Sprite2D 纹理拼到 Texture2DArray（uniform atlasTextureArray）。
//   - MultiMesh.UseCustomData = true，每帧通过 MultiMesh.SetInstanceCustomData(i, Color(...))
//     写入该 instance 在图集中的 UV 偏移与尺寸。
//   - canvas_item 的 instance custom data built-in 为 INSTANCE_CUSTOM（vec4，单数）。
//
// INSTANCE_CUSTOM 约定：
//   xy = 该 sprite 在图集中的 UV 偏移（归一化，左上角）
//   zw = 该 sprite 在图集中的 UV 尺寸（归一化，宽高）
// 单 sprite 场景下 uvOffset=(0,0)/uvSize=(1,1)，UV 不变，等同直接采样整张图集。

uniform sampler2DArray atlasTextureArray : source_color, repeat_disable, filter_linear;
uniform float atlasLayer = 0.0;

void vertex() {
	vec2 uvOffset = INSTANCE_CUSTOM.xy;
	vec2 uvSize = INSTANCE_CUSTOM.zw;
	// QuadMesh UV 左下角(0,0)、右上角(1,1)，而纹理图像左上角(0,0)、右下角(1,1)，
	// 需翻转 V 轴使 sprite 上下正确（与 Sprite2D 默认朝向一致）
	UV = uvOffset + vec2(UV.x, 1.0 - UV.y) * uvSize;
}

void fragment() {
	COLOR = texture(atlasTextureArray, vec3(UV, floor(atlasLayer + 0.5)));
}
