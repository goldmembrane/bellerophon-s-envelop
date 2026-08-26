import json
import os
import struct
import sys
from collections import Counter

import bpy


REPORT_PREFIX = "PLAYER_FBX_ANIMATION_REPORT="
ANIMATION_NODE_NAMES = {
    "AnimationStack",
    "AnimationLayer",
    "AnimationCurveNode",
    "AnimationCurve",
}


def script_args():
    if "--" not in sys.argv:
        raise RuntimeError("FBX source path must follow --")
    return sys.argv[sys.argv.index("--") + 1 :]


def iter_action_fcurves(action):
    """Support both legacy actions and Blender 4.4+ layered actions."""
    seen = set()

    legacy_curves = getattr(action, "fcurves", None)
    if legacy_curves is not None:
        for curve in legacy_curves:
            pointer = curve.as_pointer()
            if pointer not in seen:
                seen.add(pointer)
                yield curve

    for layer in getattr(action, "layers", []):
        for strip in getattr(layer, "strips", []):
            for channel_bag in getattr(strip, "channelbags", []):
                for curve in getattr(channel_bag, "fcurves", []):
                    pointer = curve.as_pointer()
                    if pointer not in seen:
                        seen.add(pointer)
                        yield curve


def describe_action(action):
    curves = list(iter_action_fcurves(action))
    keyframe_count = sum(len(curve.keyframe_points) for curve in curves)
    data_paths = sorted({curve.data_path for curve in curves})
    slots = []
    for slot in getattr(action, "slots", []):
        slots.append(
            {
                "identifier": getattr(slot, "identifier", ""),
                "target_id_type": str(getattr(slot, "target_id_type", "")),
            }
        )

    return {
        "name": action.name,
        "frame_range": [float(action.frame_range[0]), float(action.frame_range[1])],
        "fcurve_count": len(curves),
        "keyframe_count": keyframe_count,
        "data_paths": data_paths,
        "slots": slots,
    }


def describe_animation_data(owner):
    animation_data = getattr(owner, "animation_data", None)
    if animation_data is None:
        return None

    nla_tracks = []
    for track in animation_data.nla_tracks:
        strips = []
        for strip in track.strips:
            strips.append(
                {
                    "name": strip.name,
                    "action": strip.action.name if strip.action else None,
                    "frame_start": float(strip.frame_start),
                    "frame_end": float(strip.frame_end),
                    "action_frame_start": float(strip.action_frame_start),
                    "action_frame_end": float(strip.action_frame_end),
                }
            )
        nla_tracks.append(
            {
                "name": track.name,
                "mute": bool(track.mute),
                "is_solo": bool(track.is_solo),
                "strips": strips,
            }
        )

    return {
        "action": animation_data.action.name if animation_data.action else None,
        "nla_tracks": nla_tracks,
    }


def read_fbx_property(stream):
    property_type = stream.read(1)
    if not property_type:
        raise EOFError("Unexpected end of FBX property list")

    scalar_formats = {
        b"Y": "<h",
        b"C": "<?",
        b"I": "<i",
        b"F": "<f",
        b"D": "<d",
        b"L": "<q",
    }
    if property_type in scalar_formats:
        value_format = scalar_formats[property_type]
        return struct.unpack(value_format, stream.read(struct.calcsize(value_format)))[0]

    if property_type in {b"R", b"S"}:
        byte_count = struct.unpack("<I", stream.read(4))[0]
        raw_value = stream.read(byte_count)
        if property_type == b"S":
            return raw_value.decode("utf-8", errors="replace")
        return {"raw_byte_count": byte_count}

    if property_type in {b"f", b"d", b"l", b"i", b"b", b"c"}:
        array_length, encoding, compressed_length = struct.unpack("<III", stream.read(12))
        stream.seek(compressed_length, os.SEEK_CUR)
        return {
            "array_length": array_length,
            "encoding": encoding,
            "stored_byte_count": compressed_length,
        }

    raise RuntimeError(f"Unsupported FBX property type: {property_type!r}")


def read_fbx_node(stream, use_64_bit_offsets):
    header_format = "<QQQB" if use_64_bit_offsets else "<IIIB"
    header_size = struct.calcsize(header_format)
    header = stream.read(header_size)
    if len(header) != header_size:
        return None

    end_offset, property_count, property_list_length, name_length = struct.unpack(
        header_format, header
    )
    if end_offset == 0 and property_count == 0 and property_list_length == 0 and name_length == 0:
        return None

    name = stream.read(name_length).decode("utf-8", errors="replace")
    properties_start = stream.tell()
    properties = [read_fbx_property(stream) for _ in range(property_count)]
    properties_end = properties_start + property_list_length
    if stream.tell() != properties_end:
        stream.seek(properties_end)

    children = []
    while stream.tell() < end_offset:
        child_start = stream.tell()
        child = read_fbx_node(stream, use_64_bit_offsets)
        if child is None:
            break
        children.append(child)
        if stream.tell() <= child_start:
            raise RuntimeError(f"FBX node parser did not advance at {child_start}")

    stream.seek(end_offset)
    return {"name": name, "properties": properties, "children": children}


def inspect_raw_fbx(source_path):
    with open(source_path, "rb") as stream:
        magic = stream.read(23)
        if magic != b"Kaydara FBX Binary  \x00\x1a\x00":
            return {"format": "not_binary_fbx"}

        version = struct.unpack("<I", stream.read(4))[0]
        use_64_bit_offsets = version >= 7500
        root_nodes = []
        while True:
            node = read_fbx_node(stream, use_64_bit_offsets)
            if node is None:
                break
            root_nodes.append(node)

    objects_node = next((node for node in root_nodes if node["name"] == "Objects"), None)
    definitions_node = next(
        (node for node in root_nodes if node["name"] == "Definitions"), None
    )
    takes_node = next((node for node in root_nodes if node["name"] == "Takes"), None)

    animation_objects = []
    if objects_node:
        for child in objects_node["children"]:
            if child["name"] in ANIMATION_NODE_NAMES:
                animation_objects.append(
                    {"type": child["name"], "properties": child["properties"][:3]}
                )

    definition_counts = {}
    if definitions_node:
        for child in definitions_node["children"]:
            if child["name"] != "ObjectType" or not child["properties"]:
                continue
            object_type = str(child["properties"][0])
            count_node = next(
                (item for item in child["children"] if item["name"] == "Count"), None
            )
            if object_type in ANIMATION_NODE_NAMES:
                definition_counts[object_type] = (
                    int(count_node["properties"][0])
                    if count_node and count_node["properties"]
                    else None
                )

    take_entries = []
    if takes_node:
        for child in takes_node["children"]:
            if child["name"] == "Take":
                take_entries.append(child["properties"][:2])

    animation_string_references = []

    def collect_animation_strings(node, path):
        node_path = path + [node["name"]]
        for index, value in enumerate(node["properties"]):
            if not isinstance(value, str):
                continue
            lowered = value.lower()
            if "anim" in lowered or "take" in lowered:
                animation_string_references.append(
                    {
                        "path": "/".join(node_path),
                        "property_index": index,
                        "value": value,
                    }
                )
        for child in node["children"]:
            collect_animation_strings(child, node_path)

    for root_node in root_nodes:
        collect_animation_strings(root_node, [])

    return {
        "format": "binary_fbx",
        "version": version,
        "animation_definition_counts": definition_counts,
        "animation_object_counts": dict(
            sorted(Counter(item["type"] for item in animation_objects).items())
        ),
        "animation_objects": animation_objects,
        "take_entries": take_entries,
        "animation_string_references": animation_string_references,
    }


def main():
    args = script_args()
    if len(args) != 1:
        raise RuntimeError("Expected exactly one FBX source path")

    source_path = os.path.abspath(args[0])
    if not os.path.isfile(source_path):
        raise FileNotFoundError(source_path)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source_path, use_anim=True)

    objects = []
    for obj in sorted(bpy.data.objects, key=lambda item: item.name):
        animation_data = describe_animation_data(obj)
        if animation_data is not None or obj.type == "ARMATURE":
            objects.append(
                {
                    "name": obj.name,
                    "type": obj.type,
                    "bone_count": len(obj.data.bones) if obj.type == "ARMATURE" else None,
                    "animation_data": animation_data,
                }
            )

    report = {
        "source_path": source_path,
        "blender_version": bpy.app.version_string,
        "scene_frame_start": int(bpy.context.scene.frame_start),
        "scene_frame_end": int(bpy.context.scene.frame_end),
        "actions": [
            describe_action(action)
            for action in sorted(bpy.data.actions, key=lambda item: item.name)
        ],
        "objects": objects,
        "raw_fbx": inspect_raw_fbx(source_path),
    }
    print(REPORT_PREFIX + json.dumps(report, ensure_ascii=False, separators=(",", ":")))


if __name__ == "__main__":
    main()
