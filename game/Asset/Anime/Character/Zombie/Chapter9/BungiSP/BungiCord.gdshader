shader_type canvas_item;

uniform float _length = 600.0 ;

void fragment() {
	COLOR = texture( TEXTURE , UV * vec2( 1.0 , _length / 64.0 ) ) ;
}
