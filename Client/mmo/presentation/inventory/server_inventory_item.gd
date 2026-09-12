@tool
extends CtrlInventoryItemBase
## Compact GLoot item renderer for server snapshots. Content art can replace this fallback later.

const _SERVER_SLOT := &"server_slot"
const _DURABILITY := &"durability"
const _EQUIPPED := &"equipped"

var _background: ColorRect
var _label: Label
var _observed_item: InventoryItem


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_background = ColorRect.new()
	_background.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_background)
	_label = Label.new()
	_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.add_theme_font_size_override("font_size", 12)
	add_child(_label)
	resized.connect(_resize_children)
	item_changed.connect(_bind_item)
	_resize_children()
	_bind_item()


func _resize_children() -> void:
	if is_instance_valid(_background):
		_background.size = size
	if is_instance_valid(_label):
		_label.size = size


func _bind_item() -> void:
	if is_instance_valid(_observed_item) and _observed_item.property_changed.is_connected(_on_item_property_changed):
		_observed_item.property_changed.disconnect(_on_item_property_changed)
	_observed_item = item
	if is_instance_valid(_observed_item):
		_observed_item.property_changed.connect(_on_item_property_changed)
	_refresh()


func _on_item_property_changed(_property_name: String) -> void:
	_refresh()


func _refresh() -> void:
	if !is_instance_valid(_label) or !is_instance_valid(_background):
		return
	if !is_instance_valid(item):
		_label.text = ""
		_background.color = Color(0.08, 0.10, 0.11, 0.92)
		tooltip_text = ""
		return
	var slot := int(item.get_property(_SERVER_SLOT, -1))
	var quantity := item.get_stack_size()
	var durability := int(item.get_property(_DURABILITY, 0))
	var equipped := bool(item.get_property(_EQUIPPED, false))
	_label.text = "%02d\nx%d" % [slot + 1, quantity]
	_background.color = Color(0.12, 0.42, 0.45, 0.96) if equipped else Color(0.10, 0.14, 0.15, 0.94)
	var prototype_id := item.get_prototype().get_prototype_id() if item.get_prototype() != null else "desconocido"
	tooltip_text = "%s\nCantidad: %d\nDurabilidad: %d%s\nDoble clic o clic derecho: equipar/desequipar" % [
		prototype_id, quantity, durability, "\nEquipado" if equipped else ""]
