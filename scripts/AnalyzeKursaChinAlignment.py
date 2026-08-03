import json
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE_BLEND = (
    ROOT
    / "artSample"
    / "enemies"
    / "kursa"
    / "appearance_reference_sync"
    / "blender"
    / "Kursa_Appearance_ReferenceSync.blend"
)
REPORT = (
    ROOT
    / "docs"
    / "validation"
    / "kursa_chin_alignment_2026-08-03"
    / "Kursa_ChinMesh_Analysis.json"
)

LEFT_EYE = Vector((3.343094, 151.815475, 24.579956))
RIGHT_EYE = Vector((5.916458, 152.454803, 19.357758))
EYE_MIDPOINT = (LEFT_EYE + RIGHT_EYE) * 0.5
HORIZONTAL = (RIGHT_EYE - LEFT_EYE).normalized()
VERTICAL_HINT = Vector((0.0, 1.0, 0.0))
VERTICAL = (
    VERTICAL_HINT - HORIZONTAL * VERTICAL_HINT.dot(HORIZONTAL)
).normalized()
FORWARD = HORIZONTAL.cross(VERTICAL).normalized()
if FORWARD.z < 0.0:
    FORWARD.negate()


def rounded_vector(value):
    return [round(float(component), 6) for component in value]


def main():
    if Path(bpy.data.filepath).resolve() != SOURCE_BLEND.resolve():
        raise RuntimeError("The Kursa source Blend must be opened before analysis.")

    scene = bpy.context.scene
    scene.frame_set(1)
    bpy.context.view_layer.update()
    armatures = [obj for obj in scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}.")
    armature = armatures[0]
    meshes = [
        obj
        for obj in scene.objects
        if obj.type == "MESH"
        and any(
            modifier.type == "ARMATURE" and modifier.object == armature
            for modifier in obj.modifiers
        )
    ]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one skinned mesh, found {len(meshes)}.")
    mesh_object = meshes[0]
    group_names = {
        group.index: group.name
        for group in mesh_object.vertex_groups
    }
    head_bone = armature.data.bones.get("Head")
    head_pose = armature.pose.bones.get("Head")
    if head_bone is None or head_pose is None:
        raise RuntimeError("The Kursa Head bone is missing.")
    head_deform = (
        mesh_object.matrix_world.inverted()
        @ armature.matrix_world
        @ head_pose.matrix
        @ head_bone.matrix_local.inverted()
        @ armature.matrix_world.inverted()
        @ mesh_object.matrix_world
    )
    material_names = [
        slot.material.name if slot.material else "<null>"
        for slot in mesh_object.material_slots
    ]
    face_indices = [
        index
        for index, name in enumerate(material_names)
        if "face" in name.lower() and "metal" in name.lower()
    ]
    if len(face_indices) != 1:
        raise RuntimeError(
            f"Expected one face-metal material, found {face_indices}: {material_names}"
        )
    face_index = face_indices[0]

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_object.to_mesh(
        preserve_all_data_layers=True,
        depsgraph=depsgraph,
    )
    try:
        face_polygons = [
            polygon
            for polygon in evaluated_mesh.polygons
            if polygon.material_index == face_index
        ]
        face_vertices = sorted(
            {index for polygon in face_polygons for index in polygon.vertices}
        )
        samples = []
        for index in face_vertices:
            position = evaluated_mesh.vertices[index].co.copy()
            base_position = mesh_object.data.vertices[index].co.copy()
            delta = position - EYE_MIDPOINT
            samples.append(
                {
                    "index": index,
                    "position": rounded_vector(position),
                    "base_position": rounded_vector(base_position),
                    "evaluation_delta": rounded_vector(position - base_position),
                    "head_deform_position": rounded_vector(
                        head_deform @ base_position
                    ),
                    "head_deform_error": round(
                        float((position - (head_deform @ base_position)).length),
                        8,
                    ),
                    "weights": [
                        [group_names[group.group], round(float(group.weight), 6)]
                        for group in mesh_object.data.vertices[index].groups
                    ],
                    "horizontal": round(float(delta.dot(HORIZONTAL)), 6),
                    "vertical": round(float(delta.dot(VERTICAL)), 6),
                    "forward": round(float(delta.dot(FORWARD)), 6),
                }
            )
        samples.sort(key=lambda item: (item["vertical"], abs(item["horizontal"])))
        minimum_vertical = samples[0]["vertical"]
        chin_band = [
            item
            for item in samples
            if item["vertical"] <= minimum_vertical + 6.0
        ]
        chin_tip = min(
            chin_band,
            key=lambda item: (item["vertical"], abs(item["horizontal"])),
        )
        bands = []
        maximum_vertical = max(item["vertical"] for item in samples)
        band_minimum = minimum_vertical
        while band_minimum <= maximum_vertical:
            band = [
                item
                for item in samples
                if band_minimum <= item["vertical"] < band_minimum + 2.0
            ]
            if band:
                horizontal_values = sorted(item["horizontal"] for item in band)
                bands.append(
                    {
                        "vertical_min": round(band_minimum, 6),
                        "vertical_max": round(band_minimum + 2.0, 6),
                        "count": len(band),
                        "horizontal_min": round(min(horizontal_values), 6),
                        "horizontal_max": round(max(horizontal_values), 6),
                        "horizontal_mean": round(
                            sum(horizontal_values) / len(horizontal_values), 6
                        ),
                    }
                )
            band_minimum += 2.0

        report = {
            "result": "ANALYSIS_ONLY",
            "source_blend": str(SOURCE_BLEND.relative_to(ROOT)).replace("\\", "/"),
            "mesh": mesh_object.name,
            "materials": material_names,
            "face_material_index": face_index,
            "face_polygons": len(face_polygons),
            "face_vertices": len(face_vertices),
            "eye_midpoint": rounded_vector(EYE_MIDPOINT),
            "horizontal_axis": rounded_vector(HORIZONTAL),
            "vertical_axis": rounded_vector(VERTICAL),
            "forward_axis": rounded_vector(FORWARD),
            "head_deform_matrix": [
                [round(float(head_deform[row][column]), 8) for column in range(4)]
                for row in range(4)
            ],
            "minimum_vertical": minimum_vertical,
            "chin_tip": chin_tip,
            "chin_band_count": len(chin_band),
            "chin_band": chin_band,
            "lowest_face_vertices": samples[:80],
            "vertical_bands": bands,
        }
    finally:
        evaluated_object.to_mesh_clear()

    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(
        json.dumps(report, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
