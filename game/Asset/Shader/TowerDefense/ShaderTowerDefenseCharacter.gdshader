shader_type canvas_item;
const vec4 iceSpeedDownColor = vec4( 0.2 , 0.35 , 1.0 , 1.0 ) ;
const vec4 charmColor = vec4( 0.83 , 0.34 , 0.81 , 1.0 ) ;
const vec4 redHeatColor = vec4( 0.78 , 0.0 , 0.0 , 1.0 ) ;
const vec4 puzzleColor = vec4( 0.81 , 0.74 , 0.5 , 1.0 ) ;
const vec4 poisoningColor = vec4( 0.35 , 1.0 , 0.0 , 1.0 ) ;

instance uniform float brightStrength : hint_range(0.0, 0.5, 0.001) = 0.0 ;
instance uniform float whiteStrength : hint_range(0.0, 1.0, 0.001) = 0.0 ;
instance uniform float discardUpPos : hint_range(-10000, 10000.0, 0.001) = -10000.0 ;
instance uniform float discardDownPos : hint_range(-10000, 10000.0, 0.001) = 10000.0 ;
instance uniform float surfacePos : hint_range(-10000, 10000.0, 0.001) = 0.0 ;
instance uniform int effectFlags = 0 ;
instance uniform int layerDrawMin = -1 ;
instance uniform int layerDrawMax = 64 ;

const int EFFECT_ASH = 1 << 0 ;
const int EFFECT_ICE_SPEED_DOWN = 1 << 1 ;
const int EFFECT_COVER = 1 << 2 ;
const int EFFECT_HYPNOSES = 1 << 3 ;
const int EFFECT_IMITATER = 1 << 4 ;
const int EFFECT_RED_HEAT = 1 << 5 ;
const int EFFECT_PUZZLE = 1 << 6 ;
const int EFFECT_POISONING = 1 << 7 ;
const int EFFECT_BLINK = 1 << 8 ;
const int EFFECT_HOLOGRAM = 1 << 9 ;

instance uniform float frameIndex ;
instance uniform float blendWeight ;
uniform float frameBlend = -1 ;
uniform bool blend = false ;

uniform sampler2D imageCustomData : repeat_enable , filter_nearest ;
uniform sampler2D imageAnimeData : repeat_enable , filter_linear ;
uniform sampler2D imageCustomBlendData : repeat_enable , filter_nearest ;
uniform sampler2D imageAnimeBlendData : repeat_enable , filter_linear ;
uniform vec2 imageAtlasSize ;
uniform vec4 uvs[ 64 ] ;
uniform bool layerVisible[ 64 ] ;
uniform sampler2D mediaReplaceAtlas ;
uniform vec4 mediaReplaceRect[ 64 ] ;
uniform bool mediaReplaceUse[ 64 ] ;
uniform vec4 modulate : source_color = vec4( 1.0 ) ;

varying flat int mediaId ;
varying flat int mediaIdNext ;
varying flat int layerId ;

varying flat vec2 uvBegin ;
varying flat vec2 uvScale ;
varying highp vec4 baseModulate ;

vec4 GetCustomData( int x , float y ){
	return texture( imageCustomData , 1.0 / vec2( textureSize( imageCustomData , 0 ) ) * vec2( float( x ) + 0.5 , floor( y ) + 0.5 ) ) ;
}

vec4 GetAnimeData( int x , float y ){
	return texture( imageAnimeData , 1.0 / vec2( textureSize( imageAnimeData , 0 ) ) * vec2( float( x ) + 0.5 , y + 0.5 ) ) ;
}

vec4 GetCustomBlendData( int x , float y ){
	return texture( imageCustomBlendData , 1.0 / vec2( textureSize( imageCustomBlendData , 0 ) ) * vec2( float( x ) + 0.5 , floor( y ) + 0.5 ) ) ;
}

vec4 GetAnimeBlendData( int x , float y ){
	return texture( imageAnimeBlendData , 1.0 / vec2( textureSize( imageAnimeBlendData , 0 ) ) * vec2( float( x ) + 0.5 , y + 0.5 ) ) ;
}

void vertex() {
	int quadId = VERTEX_ID / 6 ;
	int animeBase = quadId * 3 ;
	vec4 customData ;
	vec4 customDataNext ;
	if ( blend ){
		customData = GetCustomBlendData( quadId , frameBlend ) ;
		customDataNext = GetCustomData( quadId , frameIndex + 1.0 ) ;
	}
	else{
		customData = GetCustomData( quadId , frameIndex ) ;
		customDataNext = GetCustomData( quadId , frameIndex + 1.0 ) ;
	}
	mediaId = int( customData.r ) ;
	mediaIdNext = int( customDataNext.r ) ;
	layerId = int( customData.g ) ;
	bool sameMedia = mediaId == mediaIdNext ;
	bool blendSkip = ( mediaId == 65535 && mediaIdNext != 65535 ) ;
	if ( mediaId != 65535 || blendSkip ){
		vec4 uvRect ;
		if ( !sameMedia ){
			mediaId = mediaIdNext ;
		}
		if ( !mediaReplaceUse[ mediaId ] ){
			uvRect = uvs[ mediaId ] ;
			uvBegin = ( uvRect.rg + vec2( 0.5 ) ) / imageAtlasSize ;
			uvScale = ( uvRect.ba - vec2( 1.0 ) ) / imageAtlasSize ;
		}
		else{
			uvRect = mediaReplaceRect[ mediaId ] ;
			vec2 textureSiae = vec2( textureSize( mediaReplaceAtlas , 0 ) ) ;
			uvBegin = ( uvRect.rg + vec2( 0.5 ) ) / textureSiae ;
			uvScale = ( uvRect.ba - vec2( 1.0 ) ) / textureSiae ;
		}
		switch( VERTEX_ID % 6 ){
			case 1 :
				VERTEX.y += uvRect.a ;
				break;
			case 2 :
				VERTEX.xy += uvRect.ba ;
				break;
			case 3 :
				VERTEX.xy += uvRect.ba ;
				break;
			case 4 :
				VERTEX.x += uvRect.b ;
				break;
		}
		vec4 animeData1 ;
		vec4 animeData2 ;
		mat3 transform ;
		if ( blend ){
			if ( !blendSkip ){
				animeData1 = GetAnimeBlendData( animeBase + 0 , frameBlend ) ;
				animeData2 = GetAnimeBlendData( animeBase + 1 , frameBlend ) ;
				vec4 animeNextData1 = GetAnimeData( animeBase + 0 , frameIndex ) ;
				vec4 animeNextData2 = GetAnimeData( animeBase + 1 , frameIndex ) ;
				baseModulate = mix( GetAnimeBlendData( animeBase + 2 , frameBlend ) , GetAnimeData( animeBase + 2 , frameIndex ) , blendWeight ) ;
				transform = mat3(
					mix( vec3( animeData1.r , animeData1.g , 0.0 ) , vec3( animeNextData1.r , animeNextData1.g , 0.0 ) , blendWeight ) ,
					mix( vec3( animeData1.b , animeData1.a , 0.0 ) , vec3( animeNextData1.b , animeNextData1.a , 0.0 ) , blendWeight ) ,
					mix( vec3( animeData2.r , animeData2.g , 1.0 ) , vec3( animeNextData2.r , animeNextData2.g , 1.0 ) , blendWeight )
				);
			}
			else{
				vec4 animeNextData1 = GetAnimeData( animeBase + 0 , frameIndex + 0.5 ) ;
				vec4 animeNextData2 = GetAnimeData( animeBase + 1 , frameIndex + 0.5 ) ;
				baseModulate = GetAnimeData( animeBase + 2 , frameIndex + 0.5 ) ;
				transform = mat3(
					vec3( animeNextData1.r , animeNextData1.g , 0.0 ) ,
					vec3( animeNextData1.b , animeNextData1.a , 0.0 ) ,
					vec3( animeNextData2.r , animeNextData2.g , 1.0 )
				);
			}
		}
		else{
			if ( !blendSkip ){
				animeData1 = GetAnimeData( animeBase + 0 , frameIndex ) ;
				animeData2 = GetAnimeData( animeBase + 1 , frameIndex ) ;
				baseModulate = GetAnimeData( animeBase + 2 , frameIndex ) ;
				transform = mat3(
					vec3( animeData1.r , animeData1.g , 0.0 ) ,
					vec3( animeData1.b , animeData1.a , 0.0 ) ,
					vec3( animeData2.r , animeData2.g , 1.0 )
				);
			}
			else{
				animeData1 = GetAnimeData( animeBase + 0 , floor( frameIndex ) + 1.0 ) ;
				animeData2 = GetAnimeData( animeBase + 1 , floor( frameIndex ) + 1.0 ) ;
				baseModulate = GetAnimeData( animeBase + 2 , floor( frameIndex ) + 1.0 ) ;
				transform = mat3(
					vec3( animeData1.r , animeData1.g , 0.0 ) ,
					vec3( animeData1.b , animeData1.a , 0.0 ) ,
					vec3( animeData2.r , animeData2.g , 1.0 )
				);
			}
		}
		VERTEX = ( transform * vec3( VERTEX , 1.0 ) ).xy ;
	}
	if ( surfacePos > 0.0 ){
		VERTEX.y += surfacePos ;
	}
}

void fragment() {
	if ( mediaId == 65535 && mediaIdNext == 65535 ){
		discard ;
	}
	if ( !layerVisible[ layerId ] ){
		discard ;
	}
	if ( layerId < layerDrawMin || layerId > layerDrawMax ){
		discard ;
	}
	if ( VERTEX.y < discardUpPos || VERTEX.y > discardDownPos ){
		discard ;
	}
	if ( mediaReplaceUse[ mediaId ] ){
		COLOR = texture( mediaReplaceAtlas , uvBegin + UV * uvScale , 0.0 ) * modulate * vec4( 1.0 , 1.0 , 1.0 , baseModulate.a ) ;
	}
	else{
		COLOR = texture( TEXTURE , uvBegin + UV * uvScale , 0.0 ) * modulate * vec4( 1.0 , 1.0 , 1.0 , baseModulate.a ) ;
	}

	if ( effectFlags != 0 ){
		if ( ( effectFlags & EFFECT_HOLOGRAM ) != 0 ){
			float lineN = floor((UV.y - TIME * 0.4 ) * float( 30 ));
			float line_grade = abs(sin(lineN*PI/4.0));
			
			vec4 line_color = mix(vec4( 0.0, 0.839, 1.0 , 1.0 ), vec4(0.333, 1.0, 1.0 , 1.0), line_grade);
			
			COLOR.rgb = mix(COLOR.rgb, line_color.rgb, 0.4);
			COLOR.a = 0.75 * COLOR.a * line_color.a;
		}
		
		if ( ( effectFlags & EFFECT_IMITATER ) != 0 ){
			float lumi = ( COLOR.r + COLOR.g + COLOR.b ) / 3.0 ;
			COLOR.rgb = vec3( lumi ) ;
		}
		if ( ( effectFlags & EFFECT_ICE_SPEED_DOWN ) != 0 ){
			COLOR *= iceSpeedDownColor ;
		}
		if ( ( effectFlags & EFFECT_HYPNOSES ) != 0 ){
			COLOR *= charmColor ;
		}
		if ( ( effectFlags & EFFECT_RED_HEAT ) != 0 ){
			COLOR *= redHeatColor ;
		}
		if ( ( effectFlags & EFFECT_POISONING ) != 0 ){
			COLOR *= poisoningColor ;
		}
		if ( ( effectFlags & EFFECT_PUZZLE ) != 0 ){
			COLOR *= puzzleColor ;
		}
		if ( ( effectFlags & EFFECT_ASH ) != 0 ){
			COLOR *= vec4( 0.0 , 0.0 , 0.0 , 1.0 ) ;
		}
		if ( ( effectFlags & EFFECT_COVER ) != 0 ){
			float coverColor = 0.75 + sin( TIME * 5.0 ) * 0.25 ;
			COLOR.rgb *= coverColor ;
		}
		if ( ( effectFlags & EFFECT_BLINK ) != 0 ){
			float blinkColor = 0.75 + sin( TIME * 15.0 ) * 0.25 ;
			COLOR.rgb *= blinkColor ;
		}
	}
	COLOR += whiteStrength ;
	COLOR *= ( 1.0 + brightStrength ) ;
}