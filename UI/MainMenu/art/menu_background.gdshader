shader_type canvas_item;

uniform sampler2D clouds : filter_nearest, repeat_enable;
uniform float cloud_speed = 0.0035;
uniform float cloud_top = 0.1;
uniform float cloud_height = 0.4074;
uniform vec2 fire_pos = vec2(304.0, 214.0);
uniform vec2 art_size = vec2(480.0, 270.0);
uniform vec4 fire_color : source_color = vec4(1.0, 0.62, 0.28, 1.0);

void fragment() {
	vec4 base = texture(TEXTURE, UV);
	float sky = step(base.a, 0.99);
	vec2 cuv = vec2(UV.x + TIME * cloud_speed, (UV.y - cloud_top) / cloud_height);
	if (cuv.y > 0.0 && cuv.y < 1.0) {
		vec4 c = texture(clouds, cuv);
		base.rgb = mix(base.rgb, c.rgb, c.a * sky);
	}
	vec2 p = floor(UV * art_size);
	float d = distance(p, fire_pos);
	float flicker = 0.8 + 0.2 * sin(TIME * 9.0) * sin(TIME * 3.1 + 1.3);
	float glow = clamp(1.0 - d / 30.0, 0.0, 1.0);
	glow = floor(glow * glow * flicker * 6.0) / 6.0;
	base.rgb += fire_color.rgb * glow * 0.28;
	COLOR = vec4(base.rgb, 1.0);
}
