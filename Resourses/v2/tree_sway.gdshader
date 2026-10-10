shader_type canvas_item;

#include "res://shaders/hero_reveal.gdshaderinc"

uniform float sway = 0.03;
uniform float speed = 1.4;
uniform float gust_speed = 0.45;
uniform float gust_scale = 0.004;

varying vec2 world_pos;
varying float sort_y;

void vertex() {
	sort_y = MODEL_MATRIX[3].y;
	vec2 world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
	vec2 cell = floor(vec2(world.x - (UV.x - 0.5) / TEXTURE_PIXEL_SIZE.x, sort_y) / 64.0);
	float phase = fract(sin(dot(mod(cell, 256.0), vec2(12.9898, 78.233))) * 43758.5453) * 6.2831;
	float gust = 0.6 + 0.4 * sin(TIME * gust_speed - (cell.x + cell.y) * 64.0 * gust_scale);
	VERTEX.x += sin(TIME * speed + phase) * gust * sway * max(sort_y - world.y, 0.0);
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	COLOR.a *= hero_reveal(world_pos, sort_y);
}
