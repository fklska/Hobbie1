shader_type canvas_item;
uniform sampler2D noise_tex;
uniform sampler2D clouds_noise;

void fragment() {
    vec4 light_color = texture(TEXTURE, UV);
    float noise = texture(noise_tex, UV).r;
    COLOR = light_color * mix(1.0, noise, 0.3);
}