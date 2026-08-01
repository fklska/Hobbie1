#[compute]
#version 450

#define FNL_IMPL
#include "res://Light/Light_V3/FastNoiseLite.gdshaderinc"

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, binding = 0) uniform restrict writeonly image2D output_image;
layout(binding = 1) uniform sampler2D color_ramp;

layout(push_constant) uniform Params {
    vec2 offset;
    vec2 global_pos;
    float texture_scale;
    uint size;
} p;

void main() {
    ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
    if (pixel_coords.x >= p.size || pixel_coords.y >= p.size) return;

    fnl_state noise = fnlCreateState(0);
    noise.noise_type = FNL_NOISE_OPENSIMPLEX2;
    noise.fractal_type = FNL_FRACTAL_FBM;
    noise.frequency = 0.05;
    noise.octaves = 5;
    noise.lacunarity = 2.0;
    noise.gain = 0.5;

    vec2 pos = (p.global_pos / p.texture_scale) + vec2(pixel_coords) + p.offset;
    
    float val = fnlGetNoise2D(noise, pos.x, pos.y);
    val = (val + 1.0) * 0.5;

    vec4 color = texture(color_ramp, vec2(val, 0.5));
    imageStore(output_image, pixel_coords, color);
}