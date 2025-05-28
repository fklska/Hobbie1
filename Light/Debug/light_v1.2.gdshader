shader_type canvas_item;
render_mode blend_add;

// Настройки эффекта
uniform float glow_intensity : hint_range(0, 5) = 1.5;
uniform float glow_threshold : hint_range(0, 1) = 0.6;
uniform vec4 glow_color = vec4(1.0, 0.9, 0.7, 1.0);
uniform sampler2D noise_texture;

void fragment() {
    // Получаем цвет из текстуры PointLight2D
    vec4 light_color = texture(TEXTURE, UV);
    
    // Получаем значение шума (если используете отдельную текстуру)
    float noise_value = texture(noise_texture, UV).r;
    
    // Комбинируем с исходным светом (если нужно)
    float brightness = max(light_color.r, max(light_color.g, light_color.b));
    
    // Эффект свечения только для светлых областей
    if (brightness > glow_threshold) {
        float glow_factor = pow((brightness - glow_threshold) / (1.0 - glow_threshold), 2.0) * glow_intensity;
        light_color.rgb += glow_color.rgb * glow_factor * glow_color.a;
    }
    
    COLOR = light_color;
}