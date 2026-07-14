import bpy


print("ACCELERANDO_REFERENCE_BLEND_REPORT_BEGIN")
for obj in bpy.context.scene.objects:
    if obj.type == "MESH":
        material_names = [material.name if material else "None" for material in obj.data.materials]
        print(
            f"object={obj.name} type=MESH vertices={len(obj.data.vertices)} polygons={len(obj.data.polygons)} "
            f"groups={len(obj.vertex_groups)} modifiers={len(obj.modifiers)} materials={material_names}"
        )
        if obj.name == "Accelerando_ConnectedColored_Body":
            counts = {}
            bounds = {}
            for polygon in obj.data.polygons:
                material_name = material_names[polygon.material_index]
                counts[material_name] = counts.get(material_name, 0) + 1
                center = polygon.center
                if material_name not in bounds:
                    bounds[material_name] = [center.copy(), center.copy()]
                for axis in range(3):
                    bounds[material_name][0][axis] = min(bounds[material_name][0][axis], center[axis])
                    bounds[material_name][1][axis] = max(bounds[material_name][1][axis], center[axis])
            for material_name, count in counts.items():
                minimum, maximum = bounds[material_name]
                print(
                    f"body_material={material_name} polygons={count} "
                    f"center_min=({minimum.x:.4f},{minimum.y:.4f},{minimum.z:.4f}) "
                    f"center_max=({maximum.x:.4f},{maximum.y:.4f},{maximum.z:.4f})"
                )
    elif obj.type in {"ARMATURE", "CURVE"}:
        print(f"object={obj.name} type={obj.type}")
print("ACCELERANDO_REFERENCE_BLEND_REPORT_END")
