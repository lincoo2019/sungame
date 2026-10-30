extends Node2D





enum Shapes{
	CURSOR_ARROW, 
	CURSOR_IBEAM, 
	CURSOR_POINTING_HAND, 
	CURSOR_CROSS, 
	CURSOR_WAIT, 
	CURSOR_BUSY, 
	CURSOR_DRAG, 
	CURSOR_CAN_DROP, 
	CURSOR_FORBIDDEN, 
	CURSOR_VSIZE, 
	CURSOR_HSIZE, 
	CURSOR_BDIAGSIZE, 
	CURSOR_FDIAGSIZE, 
	CURSOR_MOVE, 
	CURSOR_HSPLIT, 
	CURSOR_VSPLIT, 
	CURSOR_HELP, 
}



@export var sprite: AnimatedSprite2D

@onready var previous_mouse_shape: int = Input.get_current_cursor_shape()

var shape: int = Input.CURSOR_ARROW: set = set_shape
var current_animation: StringName
var current_frame_texture: Texture2D
var mod_editor_cursor_block_count: int = 0
var _custom_cursor_suspended: bool = false

func _ready():
	sprite.hide()

func _exit_tree() -> void :




	_clear_custom_mouse_cursors()
	current_frame_texture = null
	current_animation = &""

func _process(_delta: float):
	if OS.get_name() == "Android":
		sprite.hide()
		return
	if _custom_cursor_suspended:
		sprite.hide()
		return
	var current_mouse_shape = Input.get_current_cursor_shape()

	if current_mouse_shape != previous_mouse_shape:
		set_shape(current_mouse_shape)
		previous_mouse_shape = current_mouse_shape

	current_animation = sprite.animation
	current_frame_texture = sprite.sprite_frames.get_frame_texture(
			current_animation, sprite.frame
		)

	Input.set_custom_mouse_cursor(
		current_frame_texture, 
		Input.get_current_cursor_shape(), 
		sprite.offset
	)

func set_mod_editor_cursor_blocked(blocked: bool) -> void :
	if blocked:
		mod_editor_cursor_block_count += 1
	else:
		mod_editor_cursor_block_count = maxi(0, mod_editor_cursor_block_count - 1)

	_set_custom_cursor_suspended(mod_editor_cursor_block_count > 0)

func is_mod_editor_cursor_blocked() -> bool:
	return mod_editor_cursor_block_count > 0

func _set_custom_cursor_suspended(suspended: bool) -> void :
	if _custom_cursor_suspended == suspended:
		return

	_custom_cursor_suspended = suspended
	if suspended:
		sprite.hide()
		_clear_custom_mouse_cursors()
	else:
		previous_mouse_shape = -1

func _clear_custom_mouse_cursors() -> void :
	for cursor_shape in Shapes.values():
		Input.set_custom_mouse_cursor(null, cursor_shape)


func get_shape_name() -> StringName:
	return Shapes.keys()[shape].to_lower().trim_prefix("cursor_")

func set_shape(value: int) -> void :
	if !sprite: return

	shape = value

	if shape <= - 1:
		shape = Input.CURSOR_ARROW

	var anim_name: StringName = get_shape_name()
	var sprite_frames: SpriteFrames = sprite.sprite_frames

	var has_animation: bool = sprite_frames.has_animation(anim_name)

	if has_animation:
		sprite.play(anim_name)
		sprite.offset = get_offset()
		return
	else:
		push_warning("Animation %s does not exist" % anim_name)

func get_offset() -> Vector2:
	if !current_frame_texture:
		return Vector2.ZERO

	match shape:
		Input.CURSOR_ARROW, Input.CURSOR_POINTING_HAND, Input.CURSOR_ARROW:
			return Vector2.ZERO

	return current_frame_texture.get_size() * 0.5
