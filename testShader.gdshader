shader_type canvas_item;

uniform vec2 tiling;
uniform vec2 offset;
void vertex() {
	UV = UV * tiling * offset;
}

void fragment() {

}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
