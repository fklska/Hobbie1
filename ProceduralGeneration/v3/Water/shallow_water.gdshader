shader_type canvas_item;

uniform sampler2D replacement_tex : repeat_enable, filter_nearest;
uniform float texture_scale = 0.006944444;
uniform float red_threshold = 0.99;

uniform float pixel_size = 2.0;
uniform float max_distance = 4.0;
uniform float wave_reach = 2.4;
uniform float wave_period = 3.6;
uniform float wave_width = 0.05;
uniform vec4 foam_color : source_color = vec4(0.92, 1.0, 1.0, 1.0);
uniform vec4 glint_color : source_color = vec4(0.62, 0.97, 1.0, 1.0);
uniform vec4 trough_color : source_color = vec4(0.27, 0.87, 0.97, 1.0);
uniform vec4 depth_color : source_color = vec4(0.25, 0.81, 0.95, 1.0);
uniform float depth_start = 1.7;
uniform float depth_full = 3.1;

global uniform sampler2D water_field : filter_linear;
global uniform vec2 water_field_size;
global uniform sampler2D water_trails : filter_nearest;
global uniform int water_trail_count;

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float shore_distance(vec2 p) {
	return (1.0 - texture(water_field, p / water_field_size).r) * max_distance;
}

vec2 waves(vec2 p, float d) {
	float dd = d + (noise(p * 0.03) - 0.5) * 0.35 + (noise(p * 0.11 + TIME * 0.3) - 0.5) * 0.1;
	vec2 result = vec2(0.0);
	for (int i = 0; i < 2; i++) {
		float phase = TIME / wave_period + float(i) * 0.5 + noise(p * 0.004) * 0.35;
		float t = fract(phase);
		float crest = wave_reach * pow(1.0 - t, 1.3);
		float width = wave_width * (1.0 + 1.5 * t);
		float visible = smoothstep(wave_reach, wave_reach * 0.7, crest) * step(0.45, noise(p * 0.025 + vec2(float(i) * 13.7, floor(phase) * 7.1)));
		float k = dd - crest;
		result.x = max(result.x, step(abs(k), width) * visible);
		result.y = max(result.y, step(width, k) * step(k, width + 0.07) * visible);
	}
	float wash = 0.08 + 0.05 * sin(TIME * 2.0 + noise(p * 0.05) * 6.0);
	result.x = max(result.x, step(dd, wash) * step(0.3, noise(p * 0.06 + TIME * 0.2)));
	return result;
}

vec2 trails(vec2 p) {
	vec2 result = vec2(0.0);
	float dither = mod(floor(p.x / pixel_size) + floor(p.y / pixel_size), 2.0);
	for (int i = 0; i < water_trail_count; i++) {
		vec4 s = texelFetch(water_trails, ivec2(i, 0), 0);
		bool moving = s.w > 0.0;
		float size = moving ? floor(s.w) / 16.0 : -s.w;
		vec2 offset = p - s.xy;
		float r = length(offset * vec2(1.0, 1.6));
		if (r > 72.0 * size) {
			continue;
		}
		float k = s.z;
		if (moving) {
			float angle = fract(s.w) * TAU;
			float along = dot(offset, vec2(cos(angle), sin(angle))) / max(length(offset), 0.001);
			float ring = step(abs(r - size * (3.0 + 56.0 * k)), 1.5) * step(along, 0.25) * step(-0.85, along);
			result.x = max(result.x, ring * max(step(k, 0.4), dither * step(k, 0.7)));
			result.y = max(result.y, ring * step(k, 0.9));
			result.x = max(result.x, step(r, size * 6.0 * (1.0 - k / 0.45)));
		} else {
			float ring = step(abs(r - size * (3.0 + 22.0 * k)), 1.2);
			result.x = max(result.x, ring * dither * step(k, 0.35));
			result.y = max(result.y, ring * step(k, 0.8));
		}
	}
	return result;
}

void fragment() {
	float is_red = step(red_threshold, COLOR.r) * (1.0 - step(0.01, COLOR.g)) * (1.0 - step(0.01, COLOR.b));
	vec4 base = mix(COLOR, texture(replacement_tex, world_pos * texture_scale), is_red);
	vec2 p = (floor(world_pos / pixel_size) + 0.5) * pixel_size;
	float d = shore_distance(p);
	float depth = d + (noise(p * 0.02) - 0.5) * 0.9 + (noise(p * 0.08) - 0.5) * 0.25;
	base.rgb = mix(base.rgb, depth_color.rgb, (step(depth_start, depth) + step(depth_full, depth)) * 0.5 * is_red);
	vec2 wave = waves(p, d);
	vec2 trail = trails(p);
	float glint = step(0.8, noise(p * vec2(0.03, 0.08) + vec2(TIME * 0.12, TIME * 0.04))) * step(0.5, noise(p * 0.01 - TIME * 0.05));
	vec3 color = mix(base.rgb, glint_color.rgb, max(glint * 0.8, trail.y));
	color = mix(color, trough_color.rgb, wave.y * (1.0 - trail.x));
	color = mix(color, foam_color.rgb, max(wave.x, trail.x));
	COLOR = vec4(color, base.a);
}
