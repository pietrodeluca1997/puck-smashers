extends Node3D

@onready var camera = $"../Camera3D"

func _input(event: InputEvent):
	if event is InputEventMouseButton:		
		if  event.button_index == MOUSE_BUTTON_LEFT and event.is_pressed():
			
			var from: Vector3 = camera.project_ray_origin(event.position);
			var to: Vector3 = from + camera.project_ray_normal(event.position) * 1000
			
			var space_state: PhysicsDirectSpaceState3D = get_world_3d().direct_space_state
			var query := PhysicsRayQueryParameters3D.create(from, to)
			query.collide_with_areas = true
			query.collide_with_bodies = true
			
			var result: Dictionary = space_state.intersect_ray(query)
			
			if result and result.collider:
				pass
