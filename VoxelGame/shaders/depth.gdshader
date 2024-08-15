shader_type spatial;
render_mode unshaded;
render_mode depth_draw_always;

uniform sampler2D depth_texture : source_color, hint_depth_texture;

void vertex() {
	// Called for every vertex the material is visible on.
	
}

void fragment() {
	// Called for every pixel the material is visible on.
	float depth = texture(depth_texture, SCREEN_UV).x;
	ALBEDO=vec3(1.-depth);
	//DEPTH=depth;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
