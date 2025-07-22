extends AIController2D

@onready var main_collector_villager: CollectorVillager = $".."

func get_obs() -> Dictionary:
	# Return observations (e.g., player position, environment state)
	return {"obs": [main_collector_villager.nearest_res.x, main_collector_villager.nearest_res.y]}

func get_reward() -> float:
	# Define the reward (e.g., +1 for reaching a goal)
	return reward

func get_action_space() -> Dictionary:
	# Define action space (continuous or discrete)
	return {
		"move": {
			"size": 2,
			"action_type": "continuous"
			}
	}
func set_action(action) -> void:
	print_debug(action)
	if action["move"]:
		main_collector_villager.position.x += action["move"][0] * 10
		main_collector_villager.position.y += action["move"][1] * 10
