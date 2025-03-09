shader_type canvas_item;

// Параметры шейдера
uniform sampler2D noise_texture; // Текстура шума (NoiseTexture)
uniform vec2 noise_offset; // Смещение для шума
uniform float shadow_intensity : hint_range(0, 1) = 0.5; // Интенсивность тени
uniform float cloud_scale : hint_range(0.1, 10.0) = 1.0; // Масштаб шума

void fragment() {
    // Масштабируем UV-координаты для шума
    vec2 cloud_uv = UV * cloud_scale;

    // Применяем смещение к координатам шума
    cloud_uv += noise_offset;

    // Получаем значение шума (от 0 до 1)
    float noise_value = texture(noise_texture, cloud_uv).r;

    // Создаем тень на основе значения шума
    float shadow = mix(1.0, 1.0 - noise_value, shadow_intensity);

    // Применяем тень к цвету пикселя
    COLOR = vec4(vec3(shadow), 1.0);
}