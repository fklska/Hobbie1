@tool
extends PointLight2D

@export var color_ramp: Gradient:
	set(value):
		if color_ramp and color_ramp.changed.is_connected(_on_gradient_changed):
			color_ramp.changed.disconnect(_on_gradient_changed)
			
		color_ramp = value
		
		if color_ramp:
			color_ramp.changed.connect(_on_gradient_changed)
			_update_gradient_texture()

var size: int = 64


var noise_offset: Vector2 = Vector2.ZERO

var rd: RenderingDevice
var shader_rid: RID
var pipeline_rid: RID
var texture_rid: RID
var grad_tex_rid: RID

var rd_texture: ImageTexture

func _ready() -> void:
	rd_texture = ImageTexture.new()
	texture = rd_texture

	rd = RenderingServer.create_local_rendering_device()
	if not rd:
		return
	
	if color_ramp:
		if not color_ramp.changed.is_connected(_on_gradient_changed):
			color_ramp.changed.connect(_on_gradient_changed)
			
	var shader_file = load("res://Light/Light_V3/noise_render.glsl")
	var shader_spirv: RDShaderSPIRV = shader_file.get_spirv()
	
	if shader_spirv.compile_error_compute != "":
		push_error("Compute Shader Error: " + shader_spirv.compile_error_compute)
		return

	shader_rid = rd.shader_create_from_spirv(shader_spirv)
	pipeline_rid = rd.compute_pipeline_create(shader_rid)

	# 1. Текстура вывода (ДОБАВЛЕН ФЛАГ TEXTURE_USAGE_CAN_COPY_FROM_BIT)
	var fmt = RDTextureFormat.new()
	fmt.width = size
	fmt.height = size
	fmt.format = RenderingDevice.DATA_FORMAT_R8G8B8A8_UNORM
	fmt.usage_bits = (
		RenderingDevice.TEXTURE_USAGE_STORAGE_BIT | 
		RenderingDevice.TEXTURE_USAGE_CAN_UPDATE_BIT | 
		RenderingDevice.TEXTURE_USAGE_SAMPLING_BIT |
		RenderingDevice.TEXTURE_USAGE_CAN_COPY_FROM_BIT
	)
	texture_rid = rd.texture_create(fmt, RDTextureView.new())

	# 2. Текстура градиента
	var grad_image = Image.create(256, 1, false, Image.FORMAT_RGBA8)
	if color_ramp:
		for i in 256:
			grad_image.set_pixel(i, 0, color_ramp.sample(i / 255.0))
	
	var grad_fmt = RDTextureFormat.new()
	grad_fmt.width = 256
	grad_fmt.height = 1
	grad_fmt.format = RenderingDevice.DATA_FORMAT_R8G8B8A8_UNORM
	grad_fmt.usage_bits = RenderingDevice.TEXTURE_USAGE_SAMPLING_BIT | RenderingDevice.TEXTURE_USAGE_CAN_UPDATE_BIT
	grad_tex_rid = rd.texture_create(grad_fmt, RDTextureView.new(), [grad_image.get_data()])

func _process(delta: float) -> void:
	noise_offset += delta * Vector2(-2, 0)
	_run_compute()

func _run_compute() -> void:
	if not shader_rid.is_valid():
		return

	var img_uniform = RDUniform.new()
	img_uniform.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
	img_uniform.binding = 0
	img_uniform.add_id(texture_rid)

	var sampler_state = RDSamplerState.new()
	var sampler_rid = rd.sampler_create(sampler_state)
	
	var grad_uniform = RDUniform.new()
	grad_uniform.uniform_type = RenderingDevice.UNIFORM_TYPE_SAMPLER_WITH_TEXTURE
	grad_uniform.binding = 1
	grad_uniform.add_id(sampler_rid)
	grad_uniform.add_id(grad_tex_rid)

	var uniform_set = rd.uniform_set_create([img_uniform, grad_uniform], shader_rid, 0)

	# Push constants
	var push_constants = PackedByteArray()
	push_constants.resize(24)
	push_constants.encode_float(0, noise_offset.x)
	push_constants.encode_float(4, noise_offset.y)
	push_constants.encode_float(8, global_position.x)
	push_constants.encode_float(12, global_position.y)
	push_constants.encode_float(16, texture_scale)
	push_constants.encode_u32(20, size)

	var compute_list = rd.compute_list_begin()
	rd.compute_list_bind_compute_pipeline(compute_list, pipeline_rid)
	rd.compute_list_bind_uniform_set(compute_list, uniform_set, 0)
	rd.compute_list_set_push_constant(compute_list, push_constants, push_constants.size())
	
	var x_groups = int(ceil(size / 8.0))
	var y_groups = int(ceil(size / 8.0))
	rd.compute_list_dispatch(compute_list, x_groups, y_groups, 1)
	rd.compute_list_end()

	rd.submit()
	rd.sync()

	var byte_data = rd.texture_get_data(texture_rid, 0)
	if byte_data.size() > 0:
		var img = Image.create_from_data(size, size, false, Image.FORMAT_RGBA8, byte_data)
		rd_texture.set_image(img)

func _on_gradient_changed() -> void:
	_update_gradient_texture()

func _update_gradient_texture() -> void:
	if not rd or not grad_tex_rid.is_valid() or not color_ramp:
		return
	
	var grad_image = Image.create(256, 1, false, Image.FORMAT_RGBA8)
	for i in 256:
		grad_image.set_pixel(i, 0, color_ramp.sample(i / 255.0))
	
	# Загружаем новые байты в уже созданную текстуру на GPU
	rd.texture_update(grad_tex_rid, 0, grad_image.get_data())
