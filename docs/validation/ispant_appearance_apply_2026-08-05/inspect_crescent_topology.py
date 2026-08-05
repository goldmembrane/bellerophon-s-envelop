import bpy
from mathutils import geometry


OBJECT_NAME = "Ispant_Crescent_Ornament"
EPSILONS = (0.0, 1.0e-16, 1.0e-14, 1.0e-12, 1.0e-10, 1.0e-8)


obj = bpy.data.objects.get(OBJECT_NAME)
if obj is None:
    raise RuntimeError(f"Missing object: {OBJECT_NAME}")

depsgraph = bpy.context.evaluated_depsgraph_get()
evaluated = obj.evaluated_get(depsgraph)
mesh = evaluated.to_mesh(preserve_all_data_layers=True, depsgraph=depsgraph)
try:
    mesh.calc_loop_triangles()
    areas = []
    repeated_index_count = 0
    repeated_position_count = 0
    for triangle in mesh.loop_triangles:
        indices = tuple(triangle.vertices)
        if len(set(indices)) < 3:
            repeated_index_count += 1
        points = [mesh.vertices[index].co.copy() for index in indices]
        if len({tuple(point) for point in points}) < 3:
            repeated_position_count += 1
        areas.append(geometry.area_tri(*points))

    print("CRESCENT_DIAGNOSTIC_BEGIN")
    print(f"control_vertices={len(obj.data.vertices)}")
    print(f"control_polygons={len(obj.data.polygons)}")
    print(f"evaluated_vertices={len(mesh.vertices)}")
    print(f"evaluated_polygons={len(mesh.polygons)}")
    print(f"evaluated_loop_triangles={len(mesh.loop_triangles)}")
    print(f"repeated_index_triangles={repeated_index_count}")
    print(f"repeated_position_triangles={repeated_position_count}")
    print(f"minimum_area={min(areas):.18g}")
    print(f"maximum_area={max(areas):.18g}")
    for epsilon in EPSILONS:
        count = sum(area <= epsilon for area in areas)
        print(f"area_le_{epsilon:.0e}={count}")
    positive = sorted(area for area in areas if area > 0.0)
    print("smallest_positive_areas=" + ",".join(f"{area:.18g}" for area in positive[:12]))
    print("CRESCENT_DIAGNOSTIC_END")
finally:
    evaluated.to_mesh_clear()

body = bpy.data.objects.get("Ispant_Armed_Body")
if body is None:
    raise RuntimeError("Missing object: Ispant_Armed_Body")
print("BODY_MATERIAL_DIAGNOSTIC_BEGIN")
for index, material in enumerate(body.data.materials):
    polygon_count = sum(polygon.material_index == index for polygon in body.data.polygons)
    print(f"slot_{index}={material.name if material else '<null>'};polygons={polygon_count}")
print("BODY_MATERIAL_DIAGNOSTIC_END")
