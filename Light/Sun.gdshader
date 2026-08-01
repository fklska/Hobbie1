shader_type canvas_item;

uniform float scale = 0.01; // 1 мировая единица = 0.01 UV, подберите

varying vec2 world_uv;

void vertex() {
    vec2 world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
    world_uv = world_pos * scale;
}

void fragment() {
    COLOR = texture(TEXTURE, world_uv);
}