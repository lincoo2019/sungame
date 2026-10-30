shader_type canvas_item;

uniform sampler2DArray atlas_texture_array : source_color;

void fragment() {
	vec4 tint = COLOR;
	if (UV.x < 0.0) {
		vec2 replacement_uv = vec2(-UV.x - 1.0, UV.y);
		COLOR = texture(TEXTURE, replacement_uv) * tint;
	} else {
		float atlas_layer = floor(UV.y);
		vec2 atlas_uv = vec2(UV.x, UV.y - atlas_layer);
		COLOR = texture(atlas_texture_array, vec3(atlas_uv, atlas_layer)) * tint;
	}
}
