shader_type canvas_item;

uniform float wind_strength : hint_range(0, 1) = 0.1; // Сила ветра
uniform float wind_speed : hint_range(0, 10) = 1.0;  // Скорость ветра
uniform vec2 wind_direction = vec2(-1.0, 0.0);       // Направление ветра

uniform vec4 color : source_color = vec4(1.0);
uniform float width : hint_range(0, 10) = 1.0;
uniform int pattern : hint_range(0, 2) = 2; // diamond, circle, square
uniform bool inside = false;
uniform bool add_margins = false; // only useful when inside is false

uniform bool enable = false;

void vertex() {
    // Инвертируем высоту дерева (1 внизу, 0 вверху)
    float height_factor = 1.0 - UV.y;

    // Вычисляем смещение вершины
    float wave = sin(TIME * wind_speed) * wind_strength * height_factor;

    // Применяем смещение к вершине
    VERTEX.x += wave * wind_direction.x;
    VERTEX.y += wave * wind_direction.y;
	
	if (add_margins) {
		VERTEX += (UV * 2.0 - 1.0) * width;
	}
}

bool hasContraryNeighbour(vec2 uv, vec2 texture_pixel_size, sampler2D texture) {
	if (enable) {
		for (float i = -ceil(width); i <= ceil(width); i++) {
			float x = abs(i) > width ? width * sign(i) : i;
			float offset;
			
			if (pattern == 0) {
				offset = width - abs(x);
			} else if (pattern == 1) {
				offset = floor(sqrt(pow(width + 0.5, 2) - x * x));
			} else if (pattern == 2) {
				offset = width;
			}
			
			for (float j = -ceil(offset); j <= ceil(offset); j++) {
				float y = abs(j) > offset ? offset * sign(j) : j;
				vec2 xy = uv + texture_pixel_size * vec2(x, y);
				
				if ((xy != clamp(xy, vec2(0.0), vec2(1.0)) || texture(texture, xy).a <= 0.0) == inside) {
					return true;
				}
			}
		}
		return false;
	}
}

void fragment() {
	vec2 uv = UV;
	
	if (add_margins) {
		vec2 texture_pixel_size = vec2(1.0) / (vec2(1.0) / TEXTURE_PIXEL_SIZE + vec2(width * 2.0));
		
		uv = (uv - texture_pixel_size * width) * TEXTURE_PIXEL_SIZE / texture_pixel_size;
		
		if (uv != clamp(uv, vec2(0.0), vec2(1.0))) {
			COLOR.a = 0.0;
		} else {
			COLOR = texture(TEXTURE, uv);
		}
	} else {
		COLOR = texture(TEXTURE, uv);
	}
	
	if ((COLOR.a > 0.0) == inside && hasContraryNeighbour(uv, TEXTURE_PIXEL_SIZE, TEXTURE)) {
		COLOR.rgb = inside ? mix(COLOR.rgb, color.rgb, color.a) : color.rgb;
		COLOR.a += (1.0 - COLOR.a) * color.a;
	}
}