extends Control

@onready var height_map: TextureRect = $VBoxContainer/HBoxContainer2/HeightMap
@onready var heat_map: TextureRect = $VBoxContainer/HBoxContainer2/HeatMap
@onready var moisture_map: TextureRect = $VBoxContainer/HBoxContainer2/MoistureMap
@onready var biome_map: TextureRect = $VBoxContainer/HBoxContainer2/BiomeMap

@onready var seedLabel: Label = $VBoxContainer/HBoxContainer/VBoxContainer/Seed

@onready var progress_bar: ProgressBar = $VBoxContainer/ProgressBar

func _ready() -> void:
	pass
	
func PreRender():
	if (not GENERATOR.genData.HeightMap):
		printerr("Not initialized genDATA")
		return
		
	height_map.texture = GENERATOR.genData.HeightMap
	heat_map.texture = GENERATOR.genData.HeatMap
	moisture_map.texture = GENERATOR.genData.MoistureMap
	biome_map.texture = GENERATOR.genData.BiomeMap

func _on_new_seed_check_box_toggled(toggled_on: bool) -> void:
	Settings.NewSeed = toggled_on
	GENERATOR.UpdateSeedRule()

func _on_map_size_value_changed(value: float) -> void:
	Settings.MapSize = Vector2i(int(value), int(value))
	GENERATOR.UpdateMapSize()

func _on_generate_button_down() -> void:
	GENERATOR.Generate(progress_bar)
