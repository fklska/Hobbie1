shader_type canvas_item;

uniform vec2 player_pos;
uniform float reveal_radius = 32.0;

varying vec2 world_pos;

void vertex() {
    // Переводим локальную вершину в мировые координаты
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    if (distance(world_pos, player_pos) > reveal_radius) {
        color.a *= 0.0;
    }
    COLOR.a = color.a;
}