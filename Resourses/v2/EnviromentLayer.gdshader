shader_type canvas_item;

#include "res://shaders/hero_reveal.gdshaderinc"

varying vec2 world_pos;
varying float sort_y;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	sort_y = MODEL_MATRIX[3].y;
}

void fragment() {
	COLOR.a *= hero_reveal(world_pos, sort_y);
}
