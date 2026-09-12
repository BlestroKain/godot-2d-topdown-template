extends Inventory
class_name ServerInventoryProjection
## Read-only GLoot projection of the last inventory snapshot accepted by the C# client.
##
## The authoritative inventory remains on the .NET server. This node never grants,
## removes, equips or transfers an item on its own; UI gestures must become network
## intents and wait for a new server snapshot before this projection changes.

signal server_projection_rebuilt(stack_count: int)

const SERVER_ITEM_ID := &"server_item_id"
const SERVER_SLOT := &"server_slot"
const DURABILITY := &"durability"

var _last_fingerprint := ""


func apply_server_snapshot(server_slots: Array[Dictionary]) -> void:
	var ordered := server_slots.duplicate(true)
	ordered.sort_custom(func(left: Dictionary, right: Dictionary) -> bool:
		return int(left.get("slot", -1)) < int(right.get("slot", -1)))

	var fingerprint := JSON.stringify(ordered)
	if fingerprint == _last_fingerprint:
		return

	var prototypes := {}
	for slot in ordered:
		var prototype_id := str(slot.get("definition_id", ""))
		var quantity := maxi(1, int(slot.get("quantity", 1)))
		if prototype_id.is_empty():
			continue
		prototypes[prototype_id] = {
			"name": prototype_id,
			"max_stack_size": quantity
		}

	var next_protoset := JSON.new()
	var parse_error := next_protoset.parse(JSON.stringify(prototypes))
	if parse_error != OK:
		push_error("Could not build the GLoot prototype set from the authoritative snapshot.")
		return
	protoset = next_protoset

	for slot in ordered:
		var prototype_id := str(slot.get("definition_id", ""))
		if prototype_id.is_empty():
			continue
		var item := create_and_add_item(prototype_id)
		if item == null:
			push_error("GLoot rejected an authoritative inventory projection item: %s" % prototype_id)
			continue
		var quantity := maxi(1, int(slot.get("quantity", 1)))
		item.set_max_stack_size(quantity)
		item.set_stack_size(quantity)
		item.set_property(SERVER_ITEM_ID, str(slot.get("item_id", "")))
		item.set_property(SERVER_SLOT, int(slot.get("slot", -1)))
		item.set_property(DURABILITY, int(slot.get("durability", 0)))

	_last_fingerprint = fingerprint
	server_projection_rebuilt.emit(get_item_count())


func reset_server_projection() -> void:
	_last_fingerprint = ""
	reset()
	server_projection_rebuilt.emit(0)
