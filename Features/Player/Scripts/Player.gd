extends RigidBody3D

@onready var camera = $"../Camera3D"
@onready var direction_and_force_bar = $"../SubViewport/ProgressBar"
@onready var direction_and_force_bar_mesh_pivot = $"../ArrowPivot"

var is_holding: bool = false
var force_increase_factor: float = 50.0
var last_mouse_world_direction: Vector3 = Vector3.ZERO

func _input(event: InputEvent):
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:		
		match event:
			_ when event.is_pressed():
				var result: Dictionary = get_mouse_3D_info(event.position)
				if result and result.collider and result.collider.name == "Player":
					print("crico no preier")
					is_holding = true

			_ when event.is_released():
				print("sorto o crique")
				is_holding = false

func _process(delta: float) -> void:
	direction_and_force_bar_mesh_pivot.global_position = global_position
	if is_holding:		
		direction_and_force_bar.value += force_increase_factor * delta
		var result: Dictionary = get_mouse_3D_info(get_viewport().get_mouse_position())
		if result and result.collider:
			var direction_to_mouse: Vector3 = (result.position - global_position).normalized()
			direction_to_mouse.y = 0.0
			last_mouse_world_direction = direction_to_mouse
			var angle: float = atan2(direction_to_mouse.x, direction_to_mouse.z)
			
			direction_and_force_bar_mesh_pivot.rotation.y = angle		
	else:
		apply_central_impulse(last_mouse_world_direction * -direction_and_force_bar.value)
		direction_and_force_bar.value = 0.0
		
func get_mouse_3D_info(mouse_position: Vector2) -> Dictionary:
	var from: Vector3 = camera.project_ray_origin(mouse_position)
	var to: Vector3 = from + camera.project_ray_normal(mouse_position) * 1000

	var space_state: PhysicsDirectSpaceState3D = get_world_3d().direct_space_state
	var query := PhysicsRayQueryParameters3D.create(from, to)
	query.collide_with_areas = true
	query.collide_with_bodies = true

	return space_state.intersect_ray(query)
