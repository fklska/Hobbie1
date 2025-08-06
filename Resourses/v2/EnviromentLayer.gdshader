shader_type canvas_item;

uniform vec2 player_pos;  // Позиция игрока (должна передаваться из скрипта)
uniform float player_height = 32.0;  // Высота игрока в пикселях

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    
    // Если текущий пиксель находится выше позиции игрока и в пределах его ширины
    if (UV.y * TEXTURE_PIXEL_SIZE.y < player_pos.y && 
        abs(UV.x * TEXTURE_PIXEL_SIZE.x - player_pos.x) < 16.0) {
        // Делаем пиксель полупрозрачным
        color.a *= 0.5;
    }
    
    COLOR = color;
}