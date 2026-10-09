global uniform vec2 hero_position;

const vec2 HERO_REVEAL_CENTER = vec2(0.0, -22.0);
const vec2 HERO_REVEAL_RADIUS = vec2(24.0, 32.0);
const float HERO_REVEAL_ALPHA = 0.3;

float hero_reveal(vec2 world_pos, float sort_y) {
	if (sort_y <= hero_position.y) {
		return 1.0;
	}
	vec2 d = (world_pos - hero_position - HERO_REVEAL_CENTER) / HERO_REVEAL_RADIUS;
	return mix(HERO_REVEAL_ALPHA, 1.0, smoothstep(0.55, 1.0, length(d)));
}
