extends Node
## Godot 4: fetch LOD tile pyramid JSON from Ashlar.API.
## Set ashlar_base_url export before running. Duplicated from docs/engine-bridge/snippets for addon layout.

@export var ashlar_base_url: String = "http://localhost:5000"

func _ready() -> void:
	var http := HTTPRequest.new()
	add_child(http)
	http.request_completed.connect(_on_pyramid_done)
	var err := http.request("%s/api/forge/map/tile-pyramid?finestZoom=14" % ashlar_base_url.rstrip("/"))
	if err != OK:
		push_error("HTTP request failed: %s" % err)

func _on_pyramid_done(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if response_code != 200:
		push_warning("Pyramid HTTP %s" % response_code)
		return
	var text := body.get_string_from_utf8()
	print("Forge tile pyramid: ", text)
