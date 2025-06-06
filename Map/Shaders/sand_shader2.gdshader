shader_type canvas_item;

// Текстура для замены красных участков
uniform sampler2D replacement_tex : repeat_enable, filter_nearest;
// Масштаб текстуры (1/размер текстуры в пикселях)
uniform float texture_scale = 0.006944444; // 1/144
// Порог красного цвета (0.99 для почти полностью красных пикселей)
uniform float red_threshold = 0.99;

varying vec2 world_pos;

void vertex() {
    // Получаем мировые координаты
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    // Определяем, насколько пиксель красный (с учетом порога)
    float is_red = step(red_threshold, COLOR.r) * (1.0 - step(0.01, COLOR.g)) * (1.0 - step(0.01, COLOR.b));
    
    // Получаем цвет из заменяющей текстуры
    vec4 replacement = texture(replacement_tex, world_pos * texture_scale);
    
    // Смешиваем цвета
    COLOR = mix(COLOR, replacement, is_red);
}