shader_type canvas_item;

uniform bool blink ; 

void fragment() {
	if ( blink ){
		float blinkColor = 0.75 + sin( TIME * 15.0 ) * 0.25 ;
		COLOR *= vec4( blinkColor , blinkColor , blinkColor , 1.0 ) ;
	}
}

