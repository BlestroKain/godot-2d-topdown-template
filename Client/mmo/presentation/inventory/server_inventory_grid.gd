@tool
extends CtrlInventoryGrid
class_name ServerInventoryGrid
## GLoot grid adapter that emits intents without mutating the authoritative projection.

signal server_item_activated(item_id: String)
signal server_item_move_requested(item_id: String, target_index: int)

const _SERVER_ITEM_ID := &"server_item_id"
const _SERVER_SLOT := &"server_slot"

class IntentDropReceiver extends Control:
	var target

	func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
		return target != null and target._can_accept_drop(data)

	func _drop_data(at_position: Vector2, data: Variant) -> void:
		if target != null:
			target._accept_drop(at_position, data)


var _drop_receiver: IntentDropReceiver


func _ready() -> void:
	super._ready()
	mouse_filter = Control.MOUSE_FILTER_STOP
	inventory_item_activated.connect(_on_inventory_item_activated)
	inventory_item_clicked.connect(_on_inventory_item_clicked)
	_drop_receiver = IntentDropReceiver.new()
	_drop_receiver.name = "IntentDropReceiver"
	_drop_receiver.target = self
	_drop_receiver.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_drop_receiver)
	_drop_receiver.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


# Intentionally replaces CtrlInventoryGrid's drag notification. A topmost adapter receives
# drops while the wrapped GLoot grid stays read-only and therefore cannot move local state.
func _notification(what: int) -> void:
	if !is_instance_valid(_drop_receiver):
		return
	if what == NOTIFICATION_DRAG_BEGIN:
		_drop_receiver.mouse_filter = Control.MOUSE_FILTER_STOP
	elif what == NOTIFICATION_DRAG_END:
		_drop_receiver.mouse_filter = Control.MOUSE_FILTER_IGNORE


func _can_accept_drop(data: Variant) -> bool:
	return data is InventoryItem and is_instance_valid(inventory) and inventory.has_item(data)


func _accept_drop(at_position: Vector2, data: Variant) -> void:
	var item := data as InventoryItem
	if !is_instance_valid(item) or !is_instance_valid(inventory):
		return
	var grid_constraint := inventory.get_constraint(GridConstraint) as GridConstraint
	if grid_constraint == null or inventory.get_item_count() < 1:
		return

	var local_offset := _CtrlDraggableInventoryItem.get_grab_offset_local_to(self)
	var adjusted := at_position - local_offset + field_dimensions / 2.0
	var stride := field_dimensions + Vector2(item_spacing, item_spacing)
	var coords := Vector2i(floori(adjusted.x / stride.x), floori(adjusted.y / stride.y))
	coords.x = clampi(coords.x, 0, grid_constraint.size.x - 1)
	coords.y = clampi(coords.y, 0, grid_constraint.size.y - 1)
	var target_index := mini(coords.y * grid_constraint.size.x + coords.x, inventory.get_item_count() - 1)
	var source_index := int(item.get_property(_SERVER_SLOT, -1))
	if target_index == source_index:
		return
	var item_id := str(item.get_property(_SERVER_ITEM_ID, ""))
	if !item_id.is_empty():
		server_item_move_requested.emit(item_id, target_index)


func _on_inventory_item_activated(item: InventoryItem) -> void:
	_emit_activation(item)


func _on_inventory_item_clicked(item: InventoryItem, _at_position: Vector2, mouse_button_index: int) -> void:
	if mouse_button_index == MOUSE_BUTTON_RIGHT:
		_emit_activation(item)


func _emit_activation(item: InventoryItem) -> void:
	if !is_instance_valid(item):
		return
	var item_id := str(item.get_property(_SERVER_ITEM_ID, ""))
	if !item_id.is_empty():
		server_item_activated.emit(item_id)
