shader_type canvas_item;

// 符文风暴底纹贴图（三张，由 RuneStormEffect 全部传入）。
// 平铺时以“一个平铺格”为单位随机挑一张来采样，所以整屏不会重复同一种纹样。
uniform sampler2D runeTexture0 : repeat_enable;
uniform sampler2D runeTexture1 : repeat_enable;
uniform sampler2D runeTexture2 : repeat_enable;
// 当前可用的底纹张数；由 RuneStormEffect 运行时写入。
uniform int runeTextureCount = 3;
// 当前风暴颜色；由 RuneStormEffect 运行时写入。
uniform vec4 runeColor : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 effectSize = vec2(1080.0, 600.0);
uniform float timer = 0.0;
// 整体不透明度：由 RuneStormEffect 在开场淡入、收尾淡出时写入。
uniform float fade : hint_range(0.0, 1.0) = 1.0;
// 本次风暴的随机种子：让每一次风暴“哪个格子用哪张图”的分布都不一样。
uniform float seed = 0.0;

// 平铺格的伪随机值。格子编号取自同一套 UV（含滚动偏移），
// 所以“哪一格用哪张图”会跟着底纹一起滚动，不会逐帧闪烁。
float cellNoise(vec2 cell) {
	return fract(sin(dot(cell, vec2(12.9898, 78.233)) + seed) * 43758.5453);
}

void fragment() {
	vec2 textureSizeValue = vec2(textureSize(runeTexture0, 0));
	// 与下雨一致：倾斜滚动 UV，速度略慢并带轻微横向偏移，形成“飘落”观感。
	vec2 uv = effectSize / (textureSizeValue * 2.0) * UV + vec2(timer * 0.3, -timer) * 3.0;

	// 以整格为单位随机：每格从三张底纹里挑一张，格内保持一致、不会碎。
	vec2 cell = floor(uv);
	int index = int(floor(cellNoise(cell) * float(max(runeTextureCount, 1))));
	vec4 texel;
	if (index <= 0)
		texel = texture(runeTexture0, uv);
	else if (index == 1)
		texel = texture(runeTexture1, uv);
	else
		texel = texture(runeTexture2, uv);
	COLOR = vec4(runeColor.rgb, texel.a * runeColor.a * fade);
}
