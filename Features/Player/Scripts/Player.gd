extends RigidBody3D

@onready var camera = $"../Camera3D"
@onready var direction_and_force_bar = $"../HUD/SubViewport/ProgressBar"
@onready var direction_and_force_bar_mesh_pivot = $"../HUD/ArrowPivot"
@onready var left_goal_score = $"../HUD/CanvasLayer/MarginContainer/VBoxContainer/HBoxContainer/LeftGoalScore"
@onready var right_goal_score = $"../HUD/CanvasLayer/MarginContainer/VBoxContainer/HBoxContainer/RightGoalScore"
@onready var start_countdown_label = $"../HUD/CanvasLayer/MarginContainer/VBoxContainer/StartCountdown"
@onready var ball: RigidBody3D = $"../Ball"
@onready var bot: RigidBody3D = $"../Bot"

var is_holding: bool = false
var force_increase_factor: float = 50.0
var last_mouse_world_direction: Vector3 = Vector3.ZERO
var start_countdown_timer: Timer
var ball_start_location: Vector3
var player_start_location: Vector3
var bot_start_location: Vector3

func reset() -> void:
	ball.global_position = ball_start_location
	bot.global_position = bot_start_location
	global_position = player_start_location
	
	start_countdown_label.visible = true
	
	ball.freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
	ball.freeze = true

	bot.freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
	bot.freeze = true

	freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
	freeze = true
	
	start_countdown()
	
	
func start_countdown() -> void:
	start_countdown_label.text = "3"
	
	if (start_countdown_timer == null):
		start_countdown_timer = Timer.new()
		start_countdown_timer.wait_time = 1.0
		start_countdown_timer.one_shot = false
		add_child(start_countdown_timer)
		start_countdown_timer.start()
		
		start_countdown_timer.connect("timeout", Callable(self, "_on_countdown_timer_timeout"))
	else:
		start_countdown_timer.start()
	
	
func start_game() -> void:
	start_countdown_label.text = "GO!"
	start_countdown_timer.stop()
	start_countdown_label.visible = false
	
	ball.freeze = false
	bot.freeze = false
	freeze = false

func _ready():
	ball_start_location = ball.global_position
	bot_start_location = bot.global_position
	player_start_location = global_position
	
	direction_and_force_bar_mesh_pivot.visible = false
	
	reset()
	
	start_countdown()
	
	
func _on_countdown_timer_timeout():
	var text_number: int = int(start_countdown_label.text)
	text_number -= 1
	
	if (text_number == 0):		
		start_game()
	else:		
		start_countdown_label.text = str(text_number)

func _input(event: InputEvent):
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:		
		match event:
			_ when event.is_pressed():
				var result: Dictionary = get_mouse_3D_info(event.position, 2)
				if result and result.collider and result.collider.name == "PlayerInteractArea":

					direction_and_force_bar_mesh_pivot.visible = true
					is_holding = true

			_ when event.is_released():

				is_holding = false

func _process(delta: float) -> void:
	direction_and_force_bar_mesh_pivot.global_position = Vector3(global_position.x, direction_and_force_bar_mesh_pivot.global_position.y, global_position.z)
	if is_holding:		
		direction_and_force_bar.value += force_increase_factor * delta
		var result: Dictionary = get_mouse_3D_info(get_viewport().get_mouse_position(), 1)
		if result and result.collider:
			var direction_to_mouse: Vector3 = (result.position - global_position).normalized()
			direction_to_mouse.y = 0.0
			last_mouse_world_direction = direction_to_mouse
			var angle: float = atan2(direction_to_mouse.x, direction_to_mouse.z)
			
			direction_and_force_bar_mesh_pivot.rotation.y = angle		
	else:
		direction_and_force_bar_mesh_pivot.visible = false
		apply_central_impulse(last_mouse_world_direction * -direction_and_force_bar.value)
		direction_and_force_bar.value = 0.0
		
func get_mouse_3D_info(mouse_position: Vector2, mask: int) -> Dictionary:
	var from: Vector3 = camera.project_ray_origin(mouse_position)
	var to: Vector3 = from + camera.project_ray_normal(mouse_position) * 1000

	var space_state: PhysicsDirectSpaceState3D = get_world_3d().direct_space_state
	var query := PhysicsRayQueryParameters3D.create(from, to)
	query.collide_with_areas = true
	query.collide_with_bodies = true
	query.collision_mask = mask

	return space_state.intersect_ray(query)


func _on_left_goal_body_entered(body: Node3D) -> void:	
	match body.name:
		"Ball":
			right_goal_score.text = str(int(right_goal_score.text) + 1)
			reset()


func _on_right_goal_body_entered(body: Node3D) -> void:
	match body.name:
		"Ball":
			left_goal_score.text = str(int(left_goal_score.text) + 1)
			reset()
		
