shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;
uniform sampler2D noise : repeat_enable, filter_linear;
uniform float time = 0.0;
uniform float world_per_screen_px = 1.0;

group_uniforms clouds;
uniform float cloud_size = 1800.0;
uniform vec2 wind = vec2(-1.0, 0.3);
uniform float wind_speed = 35.0;
uniform float coverage : hint_range(0.0, 1.0) = 0.45;
uniform float softness : hint_range(0.01, 0.5) = 0.12;
uniform float shadow_opacity : hint_range(0.0, 1.0) = 0.4;
uniform vec3 shadow_tint : source_color = vec3(0.45, 0.55, 0.8);
uniform float shadow_strength : hint_range(0.0, 1.0) = 1.0;

group_uniforms fog;
uniform float fog_size = 1100.0;
uniform float fog_opacity : hint_range(0.0, 1.0) = 0.35;
uniform vec3 fog_color : source_color = vec3(0.88, 0.9, 0.97);
uniform float fog_amount : hint_range(0.0, 1.0) = 0.0;

group_uniforms night_grading;
uniform float night : hint_range(0.0, 1.0) = 0.0;
uniform float night_desaturation : hint_range(0.0, 1.0) = 0.6;
uniform vec3 night_tint = vec3(0.85, 1.0, 1.5);
uniform vec3 night_lift = vec3(0.004, 0.007, 0.02);
uniform vec3 ambient = vec3(1.0);

group_uniforms pixel_art;
uniform bool pixelate = true;
uniform float pixel_size = 4.0;
uniform float levels = 4.0;

varying vec2 world_pos;

const float BAYER[16] = float[](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float quantize(float v, float dither) {
	return pixelate ? clamp(floor(v * levels + dither) / levels, 0.0, 1.0) : v;
}

void fragment() {
	float px = max(pixel_size, world_per_screen_px);
	vec2 cell = floor(world_pos / px);
	vec2 p = pixelate ? (cell + 0.5) * px : world_pos;
	ivec2 b = ivec2(mod(cell, 4.0));
	float dither = (BAYER[b.x + b.y * 4] + 0.5) / 16.0;

	vec2 drift = normalize(wind) * wind_speed * time;
	float a = texture(noise, (p + drift) / cloud_size).r;
	float c = texture(noise, (p + drift * 1.4 + vec2(time * 6.0, -time * 4.0)) / (cloud_size * 0.4) + 0.37).r;
	float edge = 1.0 - coverage;
	float cloud = smoothstep(edge - softness, edge + softness, a * 0.7 + c * 0.3);
	cloud = quantize(cloud * shadow_strength, dither);

	float f = texture(noise, (p + drift * 0.25) / fog_size + vec2(0.61, 0.13)).r;
	float fog = quantize(smoothstep(0.35, 0.8, f) * fog_amount, dither);

	vec3 col = texture(screen_texture, SCREEN_UV).rgb;
	col *= mix(vec3(1.0), shadow_tint, cloud * shadow_opacity);
	col = mix(col, fog_color * ambient, fog * fog_opacity);

	float lum = dot(col, vec3(0.2126, 0.7152, 0.0722));
	float lit = smoothstep(0.08, 0.35, lum);
	float k = night * (1.0 - lit);
	col = mix(col, lum * night_tint, k * night_desaturation) + night_lift * k;

	COLOR = vec4(col, 1.0);
}
