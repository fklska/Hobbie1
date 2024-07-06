shader_type canvas_item;

uniform sampler2D noise: repeat_enable;
uniform vec2 scroll = vec2(0.5, 0.5);
uniform bool goScroll = false;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	if (goScroll)
	{
		vec4 noiseColor = texture(noise, UV + scroll * TIME);
		COLOR = noiseColor;
	}
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
