extends SceneTree

const SKIN := "res://UI/Skin/"
const OUT := "res://UI/theme.tres"
const THEME_UID := "uid://cedhnxugilj7g"

const TEXT := Color("f4e7c5")
const TEXT_HOVER := Color("fff6dc")
const TEXT_PRESSED := Color("e6d3a8")
const TEXT_MUTED := Color("b3a283")
const TEXT_DISABLED := Color("8a7e70")
const GOLD := Color("f6cf6a")
const DARK := Color("2d2118")
const OUTLINE := Color("140d0a")

var theme := Theme.new()


func _initialize() -> void:
	var font: FontFile = load("res://UI/fonts/Kurland.ttf")
	theme.default_font = font
	theme.default_font_size = 18

	build_buttons()
	build_labels()
	build_panels()
	build_inputs()
	build_bars()
	build_toggles()
	build_tabs()
	build_scroll()
	build_popups()

	var err := ResourceSaver.save(theme, OUT)
	add_uids()
	print("theme saved: ", err)
	quit()


func add_uids() -> void:
	var text := FileAccess.get_file_as_string(OUT)
	var lines := text.split("\n")
	var regex := RegEx.create_from_string('path="([^"]+)"')
	for i in lines.size():
		var line := lines[i]
		if line.begins_with("[gd_resource") and not line.contains("uid="):
			lines[i] = line.replace("format=3", 'format=3 uid="%s"' % THEME_UID)
		elif line.begins_with("[ext_resource") and not line.contains("uid="):
			var found := regex.search(line)
			var id := ResourceLoader.get_resource_uid(found.get_string(1))
			if id != ResourceUID.INVALID_ID:
				lines[i] = line.replace(" path=", ' uid="%s" path=' % ResourceUID.id_to_text(id))
	var file := FileAccess.open(OUT, FileAccess.WRITE)
	file.store_string("\n".join(lines))


func tex(name: String) -> Texture2D:
	return load(SKIN + name + ".png")


func box(name: String, margins: Array, content: Array = [], tile := false) -> StyleBoxTexture:
	var sb := StyleBoxTexture.new()
	sb.texture = tex(name)
	sb.texture_margin_left = margins[0]
	sb.texture_margin_top = margins[1]
	sb.texture_margin_right = margins[2]
	sb.texture_margin_bottom = margins[3]
	if content.is_empty():
		content = margins
	sb.content_margin_left = content[0]
	sb.content_margin_top = content[1]
	sb.content_margin_right = content[2]
	sb.content_margin_bottom = content[3]
	if tile:
		sb.axis_stretch_horizontal = StyleBoxTexture.AXIS_STRETCH_MODE_TILE
		sb.axis_stretch_vertical = StyleBoxTexture.AXIS_STRETCH_MODE_TILE
	return sb


func flat(color: Color, content := 4, radius := 0) -> StyleBoxFlat:
	var sb := StyleBoxFlat.new()
	sb.bg_color = color
	sb.set_content_margin_all(content)
	sb.set_corner_radius_all(radius)
	return sb


func empty(content := 0) -> StyleBoxEmpty:
	var sb := StyleBoxEmpty.new()
	sb.set_content_margin_all(content)
	return sb


func set_font_colors(type: String, normal: Color, hover: Color, pressed: Color, disabled: Color) -> void:
	theme.set_color("font_color", type, normal)
	theme.set_color("font_hover_color", type, hover)
	theme.set_color("font_focus_color", type, normal)
	theme.set_color("font_pressed_color", type, pressed)
	theme.set_color("font_hover_pressed_color", type, hover)
	theme.set_color("font_disabled_color", type, disabled)
	theme.set_color("font_outline_color", type, OUTLINE)


func button_styles(type: String, prefix: String) -> void:
	var tm := [6, 6, 6, 8]
	theme.set_stylebox("normal", type, box(prefix + "_normal", tm, [16, 7, 16, 11]))
	theme.set_stylebox("hover", type, box(prefix + "_hover", tm, [16, 7, 16, 11]))
	theme.set_stylebox("pressed", type, box(prefix + "_pressed", [6, 8, 6, 6], [16, 10, 16, 8]))
	theme.set_stylebox("hover_pressed", type, box(prefix + "_pressed", [6, 8, 6, 6], [16, 10, 16, 8]))
	theme.set_stylebox("disabled", type, box(prefix + "_disabled", tm, [16, 7, 16, 11]))
	theme.set_stylebox("focus", type, box("focus", [4, 4, 4, 4]))


func build_buttons() -> void:
	button_styles("Button", "button")
	set_font_colors("Button", TEXT, TEXT_HOVER, TEXT_PRESSED, TEXT_DISABLED)
	theme.set_constant("outline_size", "Button", 4)
	theme.set_constant("h_separation", "Button", 8)
	theme.set_color("icon_disabled_color", "Button", Color(1, 1, 1, 0.45))

	for variation in [["PrimaryButton", "button_green"], ["DangerButton", "button_red"]]:
		theme.set_type_variation(variation[0], "Button")
		button_styles(variation[0], variation[1])

	theme.set_type_variation("FlatButton", "Button")
	theme.set_stylebox("normal", "FlatButton", empty(6))
	theme.set_stylebox("hover", "FlatButton", flat(Color(1, 0.9, 0.6, 0.12), 6, 2))
	theme.set_stylebox("pressed", "FlatButton", flat(Color(0, 0, 0, 0.25), 6, 2))
	theme.set_stylebox("hover_pressed", "FlatButton", flat(Color(0, 0, 0, 0.25), 6, 2))
	theme.set_stylebox("disabled", "FlatButton", empty(6))
	theme.set_constant("outline_size", "FlatButton", 0)
	set_font_colors("FlatButton", TEXT_MUTED, TEXT_HOVER, GOLD, TEXT_DISABLED)

	theme.set_type_variation("TabButton", "Button")
	theme.set_stylebox("normal", "TabButton", box("tab_unselected", [6, 6, 6, 4], [14, 8, 14, 6]))
	theme.set_stylebox("hover", "TabButton", box("tab_hover", [6, 6, 6, 4], [14, 8, 14, 6]))
	theme.set_stylebox("pressed", "TabButton", box("tab_selected", [6, 6, 6, 2], [14, 8, 14, 6]))
	theme.set_stylebox("hover_pressed", "TabButton", box("tab_selected", [6, 6, 6, 2], [14, 8, 14, 6]))
	theme.set_stylebox("disabled", "TabButton", box("tab_unselected", [6, 6, 6, 4], [14, 8, 14, 6]))
	set_font_colors("TabButton", TEXT_MUTED, TEXT_HOVER, GOLD, TEXT_DISABLED)
	theme.set_constant("outline_size", "TabButton", 3)

	theme.set_type_variation("SlotButton", "Button")
	theme.set_stylebox("normal", "SlotButton", box("slot", [6, 6, 6, 6]))
	theme.set_stylebox("hover", "SlotButton", box("slot_hover", [6, 6, 6, 6]))
	theme.set_stylebox("pressed", "SlotButton", box("slot_selected", [8, 8, 8, 8], [6, 6, 6, 6]))
	theme.set_stylebox("hover_pressed", "SlotButton", box("slot_selected", [8, 8, 8, 8], [6, 6, 6, 6]))
	theme.set_stylebox("disabled", "SlotButton", box("slot", [6, 6, 6, 6]))
	theme.set_stylebox("focus", "SlotButton", empty())

	theme.set_stylebox("normal", "OptionButton", box("button_normal", [6, 6, 6, 8], [14, 7, 14, 11]))
	theme.set_stylebox("hover", "OptionButton", box("button_hover", [6, 6, 6, 8], [14, 7, 14, 11]))
	theme.set_stylebox("pressed", "OptionButton", box("button_pressed", [6, 8, 6, 6], [14, 10, 14, 8]))
	theme.set_stylebox("disabled", "OptionButton", box("button_disabled", [6, 6, 6, 8], [14, 7, 14, 11]))
	theme.set_stylebox("focus", "OptionButton", box("focus", [4, 4, 4, 4]))
	for state in ["normal", "hover", "pressed", "disabled"]:
		theme.set_stylebox(state + "_mirrored", "OptionButton", theme.get_stylebox(state, "OptionButton"))
	set_font_colors("OptionButton", TEXT, TEXT_HOVER, TEXT_PRESSED, TEXT_DISABLED)
	theme.set_constant("outline_size", "OptionButton", 4)
	theme.set_constant("arrow_margin", "OptionButton", 12)
	theme.set_icon("arrow", "OptionButton", tex("arrow_down"))


func label_variation(name: String, size: int, color: Color, outline := 0, shadow := false) -> void:
	theme.set_type_variation(name, "Label")
	theme.set_font_size("font_size", name, size)
	theme.set_color("font_color", name, color)
	theme.set_color("font_outline_color", name, OUTLINE)
	theme.set_constant("outline_size", name, outline)
	if shadow:
		theme.set_color("font_shadow_color", name, Color(0, 0, 0, 0.55))
		theme.set_constant("shadow_offset_x", name, 0)
		theme.set_constant("shadow_offset_y", name, 4)
		theme.set_constant("shadow_outline_size", name, outline)


func build_labels() -> void:
	theme.set_color("font_color", "Label", TEXT)
	theme.set_color("font_outline_color", "Label", OUTLINE)
	theme.set_constant("line_spacing", "Label", 2)
	label_variation("TitleLabel", 72, GOLD, 10, true)
	label_variation("HeaderLabel", 30, GOLD, 6)
	label_variation("SubheaderLabel", 21, GOLD, 4)
	label_variation("SubtleLabel", 15, TEXT_MUTED)
	label_variation("HudLabel", 18, TEXT, 5)
	label_variation("DarkLabel", 18, DARK)
	label_variation("ValueLabel", 18, GOLD, 4)

	theme.set_color("default_color", "RichTextLabel", TEXT)
	theme.set_color("font_outline_color", "RichTextLabel", OUTLINE)
	theme.set_font_size("normal_font_size", "RichTextLabel", 18)
	theme.set_font_size("bold_font_size", "RichTextLabel", 18)


func build_panels() -> void:
	var main := box("panel", [10, 10, 10, 10], [20, 18, 20, 18], true)
	theme.set_stylebox("panel", "PanelContainer", main)
	theme.set_stylebox("panel", "Panel", main)

	theme.set_type_variation("HudPanel", "PanelContainer")
	theme.set_stylebox("panel", "HudPanel", box("hud_panel", [4, 4, 4, 4], [12, 8, 12, 8]))

	theme.set_type_variation("FramePanel", "PanelContainer")
	theme.set_stylebox("panel", "FramePanel", box("panel_hud", [10, 10, 10, 10], [16, 14, 16, 14], true))

	theme.set_type_variation("ParchmentPanel", "PanelContainer")
	theme.set_stylebox("panel", "ParchmentPanel", box("parchment", [6, 6, 6, 6], [14, 12, 14, 12], true))

	theme.set_type_variation("InsetPanel", "PanelContainer")
	theme.set_stylebox("panel", "InsetPanel", box("field", [6, 6, 6, 6], [10, 10, 10, 10]))

	theme.set_type_variation("RibbonPanel", "PanelContainer")
	theme.set_stylebox("panel", "RibbonPanel", box("ribbon", [12, 6, 12, 6], [28, 6, 28, 8]))

	for variation in [["SlotPanel", "slot", 6], ["SlotPanelHover", "slot_hover", 6], ["SlotPanelSelected", "slot_selected", 8]]:
		theme.set_type_variation(variation[0], "Panel")
		theme.set_stylebox("panel", variation[0], box(variation[1], [variation[2], variation[2], variation[2], variation[2]], [6, 6, 6, 6]))

	theme.set_type_variation("ClearPanel", "PanelContainer")
	theme.set_stylebox("panel", "ClearPanel", empty())

	theme.set_stylebox("separator", "HSeparator", box("separator", [2, 0, 2, 0], [0, 0, 0, 0]))
	theme.set_constant("separation", "HSeparator", 14)
	var vline := StyleBoxLine.new()
	vline.color = Color("805a26")
	vline.thickness = 2
	vline.vertical = true
	theme.set_stylebox("separator", "VSeparator", vline)
	theme.set_constant("separation", "VSeparator", 14)

	theme.set_stylebox("panel", "TooltipPanel", box("parchment", [6, 6, 6, 6], [10, 6, 10, 6]))
	theme.set_color("font_color", "TooltipLabel", DARK)
	theme.set_font_size("font_size", "TooltipLabel", 16)

	theme.set_stylebox("embedded_border", "Window", box("panel", [10, 10, 10, 10], [16, 40, 16, 16], true))
	theme.set_stylebox("embedded_unfocused_border", "Window", box("panel", [10, 10, 10, 10], [16, 40, 16, 16], true))
	theme.set_color("title_color", "Window", GOLD)
	theme.set_stylebox("panel", "AcceptDialog", empty(8))


func build_inputs() -> void:
	var field := box("field", [6, 6, 6, 6], [12, 8, 12, 8])
	var field_focus := box("field_focus", [6, 6, 6, 6], [12, 8, 12, 8])
	theme.set_stylebox("normal", "LineEdit", field)
	theme.set_stylebox("focus", "LineEdit", field_focus)
	theme.set_stylebox("read_only", "LineEdit", field)
	theme.set_color("font_color", "LineEdit", TEXT)
	theme.set_color("font_uneditable_color", "LineEdit", TEXT_MUTED)
	theme.set_color("font_placeholder_color", "LineEdit", Color(TEXT_MUTED, 0.6))
	theme.set_color("font_selected_color", "LineEdit", TEXT_HOVER)
	theme.set_color("caret_color", "LineEdit", GOLD)
	theme.set_color("selection_color", "LineEdit", Color(GOLD, 0.35))

	theme.set_icon("up", "SpinBox", tex("arrow_up"))
	theme.set_icon("up_hover", "SpinBox", tex("arrow_up"))
	theme.set_icon("up_pressed", "SpinBox", tex("arrow_up"))
	theme.set_icon("up_disabled", "SpinBox", tex("arrow_up"))
	theme.set_icon("down", "SpinBox", tex("arrow_down"))
	theme.set_icon("down_hover", "SpinBox", tex("arrow_down"))
	theme.set_icon("down_pressed", "SpinBox", tex("arrow_down"))
	theme.set_icon("down_disabled", "SpinBox", tex("arrow_down"))
	for part in ["up", "down"]:
		theme.set_stylebox(part + "_background", "SpinBox", empty(2))
		theme.set_stylebox(part + "_background_hovered", "SpinBox", flat(Color(1, 0.9, 0.6, 0.15), 2))
		theme.set_stylebox(part + "_background_pressed", "SpinBox", flat(Color(0, 0, 0, 0.3), 2))
		theme.set_stylebox(part + "_background_disabled", "SpinBox", empty(2))
		theme.set_color(part + "_disabled_icon_modulate", "SpinBox", Color(1, 1, 1, 0.4))
	theme.set_stylebox("field_and_buttons_separator", "SpinBox", empty())
	theme.set_stylebox("up_down_buttons_separator", "SpinBox", empty())
	theme.set_constant("buttons_width", "SpinBox", 24)
	theme.set_constant("field_and_buttons_separation", "SpinBox", 0)

	var track := box("field", [6, 6, 6, 6], [4, 5, 4, 5])
	theme.set_stylebox("slider", "HSlider", track)
	theme.set_stylebox("grabber_area", "HSlider", box("fill_gold", [2, 2, 2, 4], [0, 5, 0, 5]))
	theme.set_stylebox("grabber_area_highlight", "HSlider", box("fill_gold", [2, 2, 2, 4], [0, 5, 0, 5]))
	theme.set_icon("grabber", "HSlider", tex("grabber"))
	theme.set_icon("grabber_highlight", "HSlider", tex("grabber_hover"))
	theme.set_icon("grabber_disabled", "HSlider", tex("grabber_disabled"))
	theme.set_icon("tick", "HSlider", ImageTexture.create_from_image(Image.create_empty(1, 1, false, Image.FORMAT_RGBA8)))


func bar_variation(name: String, fill_name: String) -> void:
	theme.set_type_variation(name, "ProgressBar")
	theme.set_stylebox("fill", name, box(fill_name, [2, 2, 2, 4], [0, 0, 0, 0]))


func build_bars() -> void:
	theme.set_stylebox("background", "ProgressBar", box("field", [6, 6, 6, 6], [4, 4, 4, 4]))
	theme.set_stylebox("fill", "ProgressBar", box("fill_gold", [2, 2, 2, 4], [0, 0, 0, 0]))
	theme.set_color("font_color", "ProgressBar", TEXT)
	theme.set_color("font_outline_color", "ProgressBar", OUTLINE)
	theme.set_constant("outline_size", "ProgressBar", 4)
	theme.set_font_size("font_size", "ProgressBar", 16)
	bar_variation("HealthBar", "fill_red")
	bar_variation("ManaBar", "fill_blue")
	bar_variation("GreenBar", "fill_green")


func build_toggles() -> void:
	for type in ["CheckBox", "CheckButton"]:
		theme.set_stylebox("normal", type, empty(4))
		theme.set_stylebox("pressed", type, empty(4))
		theme.set_stylebox("disabled", type, empty(4))
		theme.set_stylebox("hover", type, flat(Color(1, 0.9, 0.6, 0.08), 4, 2))
		theme.set_stylebox("hover_pressed", type, flat(Color(1, 0.9, 0.6, 0.08), 4, 2))
		theme.set_stylebox("focus", type, empty())
		set_font_colors(type, TEXT, TEXT_HOVER, TEXT, TEXT_DISABLED)
		theme.set_constant("h_separation", type, 10)
	theme.set_icon("checked", "CheckBox", tex("check_on"))
	theme.set_icon("unchecked", "CheckBox", tex("check_off"))
	theme.set_icon("checked_disabled", "CheckBox", tex("check_on_disabled"))
	theme.set_icon("unchecked_disabled", "CheckBox", tex("check_off_disabled"))
	theme.set_icon("radio_checked", "CheckBox", tex("check_on"))
	theme.set_icon("radio_unchecked", "CheckBox", tex("check_off"))
	for suffix in ["", "_mirrored"]:
		theme.set_icon("checked" + suffix, "CheckButton", tex("toggle_on"))
		theme.set_icon("unchecked" + suffix, "CheckButton", tex("toggle_off"))
		theme.set_icon("checked_disabled" + suffix, "CheckButton", tex("toggle_on_disabled"))
		theme.set_icon("unchecked_disabled" + suffix, "CheckButton", tex("toggle_off_disabled"))


func build_tabs() -> void:
	for type in ["TabContainer", "TabBar"]:
		theme.set_stylebox("tab_selected", type, box("tab_selected", [6, 6, 6, 2], [16, 8, 16, 6]))
		theme.set_stylebox("tab_unselected", type, box("tab_unselected", [6, 6, 6, 4], [16, 8, 16, 6]))
		theme.set_stylebox("tab_hovered", type, box("tab_hover", [6, 6, 6, 4], [16, 8, 16, 6]))
		theme.set_stylebox("tab_disabled", type, box("tab_unselected", [6, 6, 6, 4], [16, 8, 16, 6]))
		theme.set_stylebox("tab_focus", type, empty())
		theme.set_color("font_selected_color", type, GOLD)
		theme.set_color("font_hovered_color", type, TEXT_HOVER)
		theme.set_color("font_unselected_color", type, TEXT_MUTED)
		theme.set_color("font_disabled_color", type, TEXT_DISABLED)
		theme.set_color("font_outline_color", type, OUTLINE)
		theme.set_constant("outline_size", type, 3)
	theme.set_stylebox("panel", "TabContainer", box("panel", [10, 10, 10, 10], [20, 18, 20, 18], true))
	theme.set_constant("side_margin", "TabContainer", 10)


func build_scroll() -> void:
	for type in ["VScrollBar", "HScrollBar"]:
		theme.set_stylebox("scroll", type, box("scroll_track", [4, 4, 4, 4], [5, 5, 5, 5]))
		theme.set_stylebox("scroll_focus", type, box("scroll_track", [4, 4, 4, 4], [5, 5, 5, 5]))
		theme.set_stylebox("grabber", type, box("scroll_grab", [4, 4, 4, 4], [5, 5, 5, 5]))
		theme.set_stylebox("grabber_highlight", type, box("scroll_hover", [4, 4, 4, 4], [5, 5, 5, 5]))
		theme.set_stylebox("grabber_pressed", type, box("scroll_press", [4, 4, 4, 4], [5, 5, 5, 5]))
	theme.set_stylebox("panel", "ScrollContainer", empty())


func build_popups() -> void:
	theme.set_stylebox("panel", "PopupMenu", box("panel", [10, 10, 10, 10], [8, 8, 8, 8], true))
	theme.set_stylebox("hover", "PopupMenu", flat(Color(0.96, 0.8, 0.4, 0.22), 4, 2))
	theme.set_color("font_color", "PopupMenu", TEXT)
	theme.set_color("font_hover_color", "PopupMenu", TEXT_HOVER)
	theme.set_color("font_disabled_color", "PopupMenu", TEXT_DISABLED)
	theme.set_constant("v_separation", "PopupMenu", 8)
	theme.set_constant("item_start_padding", "PopupMenu", 10)
	theme.set_constant("item_end_padding", "PopupMenu", 10)
	theme.set_icon("checked", "PopupMenu", tex("check_on"))
	theme.set_icon("unchecked", "PopupMenu", tex("check_off"))
	theme.set_icon("radio_checked", "PopupMenu", tex("check_on"))
	theme.set_icon("radio_unchecked", "PopupMenu", tex("check_off"))

	theme.set_stylebox("panel", "ItemList", box("field", [6, 6, 6, 6], [6, 6, 6, 6]))
	theme.set_stylebox("selected", "ItemList", flat(Color(0.96, 0.8, 0.4, 0.25), 4, 2))
	theme.set_stylebox("selected_focus", "ItemList", flat(Color(0.96, 0.8, 0.4, 0.3), 4, 2))
	theme.set_stylebox("hovered", "ItemList", flat(Color(1, 0.9, 0.6, 0.1), 4, 2))
	theme.set_stylebox("focus", "ItemList", empty())
	theme.set_color("font_color", "ItemList", TEXT)
	theme.set_color("font_selected_color", "ItemList", GOLD)
