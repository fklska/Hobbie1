shader_type canvas_item;

uniform sampler2D replacement_tex : repeat_enable, filter_nearest;
uniform float texture_scale = 0.006944444;
uniform float red_threshold = 0.99;

uniform float pixel_size = 2.0;
uniform vec2 wave_cell = vec2(112.0, 60.0);
uniform vec2 wave_drift = vec2(9.0, 3.0);
uniform float wave_period = 5.0;
uniform float wave_density = 0.4;
uniform vec4 swell_color : source_color = vec4(0.02, 0.05, 0.42, 1.0);
uniform vec4 trough_color : source_color = vec4(0.0, 0.0, 0.24, 1.0);
uniform vec4 crest_color : source_color = vec4(0.2, 0.36, 0.82, 1.0);
uniform vec4 foam_color : source_color = vec4(0.8, 0.92, 1.0, 1.0);

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

vec3 wavelet(vec2 p, vec2 shift) {
	vec2 q = p - TIME * wave_drift + shift;
	float row = floor(q.y / wave_cell.y);
	q.x += row * wave_cell.x * 0.5;
	vec2 cell = vec2(floor(q.x / wave_cell.x), row);
	vec2 local = q - cell * wave_cell;
	float seed = hash(cell + shift);
	if (seed > wave_density) {
		return vec3(0.0);
	}
	float cycle = fract(TIME / wave_period + hash(cell + 3.1));
	float life = sin(cycle * PI);
	float half_len = mix(10.0, 26.0, hash(cell + 7.7)) * smoothstep(0.0, 0.6, life);
	vec2 center = vec2(mix(0.3, 0.7, hash(cell + 5.3)), mix(0.35, 0.65, hash(cell + 9.1))) * wave_cell;
	float t = (local.x - center.x) / max(half_len, 1.0);
	float dy = local.y - center.y - t * t * 4.0;
	float inside = step(abs(t), 1.0);
	float crest = inside * step(-pixel_size, dy) * step(dy, 0.0);
	float trough = inside * step(abs(t), 0.85) * step(0.0, dy) * step(dy, pixel_size);
	float foam = crest * step(abs(t), 0.35) * step(0.75, life);
	return vec3(trough, crest, foam);
}

void fragment() {
	float is_red = step(red_threshold, COLOR.r) * (1.0 - step(0.01, COLOR.g)) * (1.0 - step(0.01, COLOR.b));
	vec4 base = mix(COLOR, texture(replacement_tex, world_pos * texture_scale), is_red);
	vec2 p = (floor(world_pos / pixel_size) + 0.5) * pixel_size;
	vec3 wave = max(wavelet(p, vec2(0.0)), wavelet(p, wave_cell * vec2(0.37, 0.5)));
	float swell = step(0.62, noise(p * vec2(0.005, 0.012) + vec2(TIME * 0.03, TIME * 0.02))) * mod(floor(p.x / pixel_size) + floor(p.y / pixel_size), 2.0);
	vec3 color = mix(base.rgb, swell_color.rgb, swell * is_red);
	color = mix(color, trough_color.rgb, wave.x * is_red);
	color = mix(color, crest_color.rgb, wave.y * is_red);
	color = mix(color, foam_color.rgb, wave.z * is_red);
	COLOR = vec4(color, base.a);
}
