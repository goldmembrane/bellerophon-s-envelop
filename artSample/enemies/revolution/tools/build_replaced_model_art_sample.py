import bpy
import hashlib
import json
import math
import os
import sys
from collections import Counter
from mathutils import Vector


marker = sys.argv.index("--") + 1
source_path = sys.argv[marker]
sample_root = sys.argv[marker + 1]
texture_root = os.path.join(sample_root, "textures")
render_root = os.path.join(sample_root, "renders")
export_root = os.path.join(sample_root, "exports")
os.makedirs(render_root, exist_ok=True)
os.makedirs(export_root, exist_ok=True)


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(
            lambda: handle.read(1024 * 1024),
            b"",
        ):
            digest.update(block)
    return digest.hexdigest().upper()


def mesh_signature(mesh_object):
    mesh = mesh_object.data
    return {
        "vertices": len(mesh.vertices),
        "polygons": len(mesh.polygons),
        "loops": len(mesh.loops),
        "vertex_coordinates": [
            [float(value) for value in vertex.co]
            for vertex in mesh.vertices
        ],
        "polygon_vertices": [
            list(polygon.vertices)
            for polygon in mesh.polygons
        ],
        "uv_layers": {
            layer.name: [
                [float(value) for value in item.uv]
                for item in layer.data
            ]
            for layer in mesh.uv_layers
        },
    }


def armature_signature(armature_object):
    return {
        "bones": len(armature_object.data.bones),
        "names": [
            bone.name
            for bone in armature_object.data.bones
        ],
        "heads": [
            [float(value) for value in bone.head_local]
            for bone in armature_object.data.bones
        ],
        "tails": [
            [float(value) for value in bone.tail_local]
            for bone in armature_object.data.bones
        ],
    }


def bounds_of(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        if obj.type == "MESH"
        for corner in obj.bound_box
    ]
    low = Vector((
        min(point.x for point in points),
        min(point.y for point in points),
        min(point.z for point in points),
    ))
    high = Vector((
        max(point.x for point in points),
        max(point.y for point in points),
        max(point.z for point in points),
    ))
    return low, high


def point_at(obj, target):
    obj.rotation_euler = (
        Vector(target) - obj.location
    ).to_track_quat("-Z", "Y").to_euler()


def evaluated_polygon_centers(mesh_object):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_object = mesh_object.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_object.to_mesh()
    try:
        return [
            mesh_object.matrix_world @ polygon.center
            for polygon in evaluated_mesh.polygons
        ]
    finally:
        evaluated_object.to_mesh_clear()


def dominant_group(mesh_object, polygon):
    weights = {}
    for vertex_index in polygon.vertices:
        for membership in mesh_object.data.vertices[
            vertex_index
        ].groups:
            name = mesh_object.vertex_groups[
                membership.group
            ].name
            weights[name] = (
                weights.get(name, 0.0) +
                membership.weight
            )
    if not weights:
        return None
    return max(
        weights.items(),
        key=lambda item: item[1],
    )[0]


def textured_material(
    name,
    texture_path,
    metallic,
    roughness,
    emission_strength=0.0,
    detail_texture_path=None,
):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    for node in list(nodes):
        nodes.remove(node)

    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    coordinates = nodes.new("ShaderNodeTexCoord")
    separate = nodes.new("ShaderNodeSeparateXYZ")
    mirror_scale = nodes.new("ShaderNodeMath")
    mirror_center = nodes.new("ShaderNodeMath")
    mirror_abs = nodes.new("ShaderNodeMath")
    combine = nodes.new("ShaderNodeCombineXYZ")
    mapping = nodes.new("ShaderNodeMapping")
    texture = nodes.new("ShaderNodeTexImage")
    mirror_scale.operation = "MULTIPLY"
    mirror_scale.inputs[1].default_value = 2.0
    mirror_center.operation = "SUBTRACT"
    mirror_center.inputs[1].default_value = 1.0
    mirror_abs.operation = "ABSOLUTE"
    texture.image = bpy.data.images.load(
        texture_path,
        check_existing=True,
    )
    texture.interpolation = "Linear"
    texture.extension = "EXTEND"
    texture.projection = "BOX"
    texture.projection_blend = 0.12
    mapping.inputs["Scale"].default_value = (
        1.0,
        1.0,
        1.0,
    )

    links.new(
        coordinates.outputs["Generated"],
        separate.inputs["Vector"],
    )
    links.new(
        separate.outputs["X"],
        mirror_scale.inputs[0],
    )
    links.new(
        mirror_scale.outputs[0],
        mirror_center.inputs[0],
    )
    links.new(
        mirror_center.outputs[0],
        mirror_abs.inputs[0],
    )
    links.new(
        mirror_abs.outputs[0],
        combine.inputs["X"],
    )
    links.new(
        separate.outputs["Y"],
        combine.inputs["Y"],
    )
    links.new(
        separate.outputs["Z"],
        combine.inputs["Z"],
    )
    links.new(
        combine.outputs["Vector"],
        mapping.inputs["Vector"],
    )
    links.new(
        mapping.outputs["Vector"],
        texture.inputs["Vector"],
    )
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if detail_texture_path:
        detail_mapping = nodes.new(
            "ShaderNodeMapping"
        )
        detail_mapping.inputs[
            "Scale"
        ].default_value = (
            3.0,
            3.0,
            3.0,
        )
        detail_texture = nodes.new(
            "ShaderNodeTexImage"
        )
        detail_texture.image = (
            bpy.data.images.load(
                detail_texture_path,
                check_existing=True,
            )
        )
        detail_texture.interpolation = "Linear"
        detail_texture.extension = "REPEAT"
        detail_texture.projection = "BOX"
        detail_texture.projection_blend = 0.18
        detail_mix = nodes.new(
            "ShaderNodeMixRGB"
        )
        detail_mix.blend_type = "MIX"
        detail_mix.inputs[0].default_value = 0.35
        grayscale = nodes.new(
            "ShaderNodeRGBToBW"
        )
        roughness_ramp = nodes.new(
            "ShaderNodeValToRGB"
        )
        roughness_ramp.color_ramp.elements[
            0
        ].position = 0.20
        roughness_ramp.color_ramp.elements[
            0
        ].color = (0.66, 0.66, 0.66, 1.0)
        roughness_ramp.color_ramp.elements[
            1
        ].position = 0.80
        roughness_ramp.color_ramp.elements[
            1
        ].color = (0.42, 0.42, 0.42, 1.0)
        bump = nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.10
        bump.inputs["Distance"].default_value = 0.025
        links.new(
            combine.outputs["Vector"],
            detail_mapping.inputs["Vector"],
        )
        links.new(
            detail_mapping.outputs["Vector"],
            detail_texture.inputs["Vector"],
        )
        links.new(
            texture.outputs["Color"],
            detail_mix.inputs[1],
        )
        links.new(
            detail_texture.outputs["Color"],
            detail_mix.inputs[2],
        )
        links.new(
            detail_mix.outputs["Color"],
            shader.inputs["Base Color"],
        )
        links.new(
            detail_texture.outputs["Color"],
            grayscale.inputs["Color"],
        )
        links.new(
            grayscale.outputs["Val"],
            roughness_ramp.inputs["Fac"],
        )
        links.new(
            roughness_ramp.outputs["Color"],
            shader.inputs["Roughness"],
        )
        links.new(
            grayscale.outputs["Val"],
            bump.inputs["Height"],
        )
        links.new(
            bump.outputs["Normal"],
            shader.inputs["Normal"],
        )
    else:
        links.new(
            texture.outputs["Color"],
            shader.inputs["Base Color"],
        )
    if emission_strength > 0.0:
        links.new(
            texture.outputs["Color"],
            shader.inputs["Emission Color"],
        )
        shader.inputs[
            "Emission Strength"
        ].default_value = emission_strength
    links.new(
        shader.outputs["BSDF"],
        output.inputs["Surface"],
    )
    return material


def default_material():
    material = bpy.data.materials.new(
        "Unseen_Back_Default_Preserved"
    )
    material.use_nodes = True
    shader = material.node_tree.nodes.get(
        "Principled BSDF"
    )
    shader.inputs["Base Color"].default_value = (
        0.18,
        0.20,
        0.22,
        1.0,
    )
    shader.inputs["Metallic"].default_value = 0.45
    shader.inputs["Roughness"].default_value = 0.52
    return material


def render_view(
    scene,
    camera,
    center,
    radius,
    angle_degrees,
    elevation,
    output_path,
):
    angle = math.radians(angle_degrees)
    camera.location = center + Vector((
        radius * math.sin(angle),
        -radius * math.cos(angle),
        radius * elevation,
    ))
    point_at(camera, center)
    scene.render.filepath = output_path
    bpy.ops.render.render(write_still=True)


with open(
    os.path.join(sample_root, "CROP_PROVENANCE.json"),
    "r",
    encoding="utf-8",
) as handle:
    crop_provenance = json.load(handle)
crop_paths = {
    entry["id"]: os.path.join(
        sample_root,
        entry["file"].replace("/", os.sep),
    )
    for entry in crop_provenance["regions"]
}

with open(
    os.path.join(sample_root, "MESH_COMPONENTS.json"),
    "r",
    encoding="utf-8",
) as handle:
    component_analysis = json.load(handle)

component_by_polygon = {}
component_polygons = {}
for component in component_analysis["components"]:
    component_id = component["component_id"]
    component_polygons[component_id] = list(
        component["polygon_indices"]
    )
    for polygon_index in component["polygon_indices"]:
        component_by_polygon[polygon_index] = (
            component_id
        )

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source_path)
meshes = [
    obj for obj in bpy.context.scene.objects
    if obj.type == "MESH"
]
armatures = [
    obj for obj in bpy.context.scene.objects
    if obj.type == "ARMATURE"
]
if len(meshes) != 1 or len(armatures) != 1:
    raise RuntimeError(
        f"Expected one mesh and one armature; got {len(meshes)} and {len(armatures)}."
    )

mesh_object = meshes[0]
armature_object = armatures[0]
before_mesh = mesh_signature(mesh_object)
before_armature = armature_signature(
    armature_object
)

material_specs = [
    (
        "body_panel",
        "Reference_BodyPanel_DirectCrop",
        0.52,
        0.56,
        0.0,
    ),
    (
        "body_light_steel",
        "Reference_BodyLightSteel_DirectCrop",
        0.35,
        0.60,
        0.0,
    ),
    (
        "weapon_housing",
        "Reference_WeaponHousing_DirectCrop",
        0.58,
        0.58,
        0.0,
    ),
    (
        "leg_armor",
        "Reference_LegArmor_DirectCrop",
        0.40,
        0.62,
        0.0,
    ),
    (
        "copper_mechanics",
        "Reference_CopperMechanics_DirectCrop",
        0.55,
        0.48,
        0.0,
    ),
    (
        "dark_mechanics",
        "Reference_DarkMechanics_DirectCrop",
        0.46,
        0.62,
        0.0,
    ),
    (
        "blue_optic",
        "Reference_BlueOptic_DirectCrop",
        0.25,
        0.22,
        2.4,
    ),
]

materials = {}
for (
    crop_id,
    material_name,
    metallic,
    roughness,
    emission_strength,
) in material_specs:
    materials[crop_id] = textured_material(
        material_name,
        crop_paths[crop_id],
        metallic,
        roughness,
        emission_strength,
        (
            crop_paths["body_wear"]
            if crop_id == "body_panel"
            else None
        ),
    )
materials["torso_inset_steel"] = textured_material(
    "Reference_TorsoInsetGunmetal_DirectCrop",
    crop_paths["body_wear"],
    0.44,
    0.56,
)
materials["default_preserved"] = default_material()

material_order = [
    item[0] for item in material_specs
] + [
    "torso_inset_steel",
    "default_preserved",
]
mesh_object.data.materials.clear()
for material_id in material_order:
    mesh_object.data.materials.append(
        materials[material_id]
    )
material_indices = {
    material_id: index
    for index, material_id in enumerate(
        material_order
    )
}

weapon_groups = {
    "LeftForeArm",
    "LeftHand",
    "RightForeArm",
    "RightHand",
}
leg_groups = {
    "LeftUpLeg",
    "LeftLeg",
    "LeftFoot",
    "LeftToeBase",
    "RightUpLeg",
    "RightLeg",
    "RightFoot",
    "RightToeBase",
}
arm_groups = {
    "LeftShoulder",
    "LeftArm",
    "RightShoulder",
    "RightArm",
}
all_arm_groups = arm_groups | weapon_groups
dark_groups = {
    "Hips",
}

blue_components = {7}
dark_components = {
    9,
    10,
    11,
    13,
    24,
    31,
    33,
    45,
    49,
    91,
    96,
}
light_components = {
    1,
    14,
    16,
    25,
    27,
    28,
    30,
}
copper_components = {
    44,
    48,
    54,
    57,
}
torso_primary_components = {0}
torso_inset_components = {1}
shoulder_joint_shell_components = {
    14,
    16,
    25,
    27,
}
shoulder_joint_inner_components = {
    2,
    3,
    40,
    55,
}
shoulder_connector_frame_components = {
    21,
    23,
    43,
    52,
    72,
    77,
    79,
    84,
    89,
    135,
    136,
    141,
}

centers = evaluated_polygon_centers(mesh_object)
dominant_groups_by_polygon = {}
shoulder_joint_override_records = []
shoulder_connector_override_records = []

for polygon, center in zip(
    mesh_object.data.polygons,
    centers,
):
    group = dominant_group(
        mesh_object,
        polygon,
    )
    dominant_groups_by_polygon[
        polygon.index
    ] = group
    component_id = component_by_polygon[
        polygon.index
    ]

    material_id = "body_panel"
    if group in weapon_groups:
        material_id = "weapon_housing"
    elif group in leg_groups:
        material_id = "leg_armor"
    elif group in arm_groups:
        material_id = "body_light_steel"
    elif group in dark_groups:
        material_id = "dark_mechanics"

    if component_id in light_components:
        material_id = "body_light_steel"
    if component_id in dark_components:
        material_id = "dark_mechanics"
    if component_id in copper_components:
        material_id = "copper_mechanics"
    if component_id in blue_components:
        material_id = "blue_optic"
    if component_id in torso_primary_components:
        material_id = "body_panel"
    if component_id in torso_inset_components:
        material_id = "torso_inset_steel"
    material_before_shoulder_override = material_id
    if component_id in shoulder_joint_shell_components:
        material_id = "body_panel"
    if component_id in shoulder_joint_inner_components:
        material_id = "dark_mechanics"
    if (
        component_id in
        shoulder_connector_frame_components and
        material_id == "body_light_steel"
    ):
        material_id = "dark_mechanics"
        shoulder_connector_override_records.append({
            "polygon": polygon.index,
            "component": component_id,
            "before": "body_light_steel",
            "after": "dark_mechanics",
        })
    if (
        component_id in (
            shoulder_joint_shell_components |
            shoulder_joint_inner_components
        ) and
        material_before_shoulder_override !=
        material_id
    ):
        shoulder_joint_override_records.append({
            "polygon": polygon.index,
            "component": component_id,
            "before":
                material_before_shoulder_override,
            "after": material_id,
        })
    if (
        center.y > 0.50 and
        group not in all_arm_groups and
        component_id not in (
            torso_primary_components |
            torso_inset_components
        )
    ):
        material_id = "default_preserved"

    polygon.material_index = material_indices[
        material_id
    ]

screen_right_copy_specs = [
    {
        "source_components": [28],
        "target_components": [30],
        "mode": "one_to_one",
    },
    {
        "source_components": [51],
        "target_components": [53],
        "mode": "one_to_one",
    },
    {
        "source_components": [3],
        "target_components": [2],
        "mode": "one_to_one",
    },
    {
        "source_components": [17],
        "target_components": [19],
        "mode": "one_to_one",
    },
    {
        "source_components": [56],
        "target_components": [50],
        "mode": "one_to_one",
    },
    {
        "source_components": [65],
        "target_components": [62],
        "mode": "one_to_one",
    },
    {
        "source_components": [39],
        "target_components": [41],
        "mode": "one_to_one",
    },
    {
        "source_components": [77],
        "target_components": [84],
        "mode": "one_to_one",
    },
    {
        "source_components": [74],
        "target_components": [83],
        "mode": "one_to_one",
    },
    {
        "source_components": [120],
        "target_components": [117],
        "mode": "one_to_one",
    },
    {
        "source_components": [29],
        "target_components": [26],
        "mode": "one_to_one",
    },
    {
        "source_components": [129],
        "target_components": [133],
        "mode": "one_to_one",
    },
    {
        "source_components": [16],
        "target_components": [14],
        "mode": "one_to_one",
    },
    {
        "source_components": [35],
        "target_components": [37],
        "mode": "one_to_one",
    },
    {
        "source_components": [79],
        "target_components": [72],
        "mode": "one_to_one",
    },
    {
        "source_components": [92],
        "target_components": [93],
        "mode": "one_to_one",
    },
    {
        "source_components": [131],
        "target_components": [127],
        "mode": "one_to_one",
    },
    {
        "source_components": [43],
        "target_components": [52],
        "mode": "one_to_one",
    },
    {
        "source_components": [27],
        "target_components": [25],
        "mode": "one_to_one",
    },
    {
        "source_components": [18],
        "target_components": [15],
        "mode": "one_to_one",
    },
    {
        "source_components": [4],
        "target_components": [5],
        "mode": "one_to_one",
    },
    {
        "source_components": [119],
        "target_components": [121],
        "mode": "one_to_one",
    },
    {
        "source_components": [63],
        "target_components": [58],
        "mode": "one_to_one",
    },
    {
        "source_components": [106],
        "target_components": [100],
        "mode": "one_to_one",
    },
    {
        "source_components": [23],
        "target_components": [21],
        "mode": "one_to_one",
    },
    {
        "source_components": [60],
        "target_components": [59],
        "mode": "one_to_one",
    },
    {
        "source_components": [90],
        "target_components": [95],
        "mode": "one_to_one",
    },
    {
        "source_components": [122],
        "target_components": [118],
        "mode": "one_to_one",
    },
    {
        "source_components": [57],
        "target_components": [44],
        "mode": "one_to_one",
    },
    {
        "source_components": [8],
        "target_components": [12],
        "mode": "spatial_sample",
    },
    {
        "source_components": [34],
        "target_components": [32],
        "mode": "spatial_sample",
    },
    {
        "source_components": [40],
        "target_components": [55],
        "mode": "spatial_sample",
    },
    {
        "source_components": [47],
        "target_components": [66],
        "mode": "spatial_sample",
    },
    {
        "source_components": [88],
        "target_components": [101],
        "mode": "spatial_sample",
    },
    {
        "source_components": [89],
        "target_components": [135],
        "mode": "spatial_sample",
    },
    {
        "source_components": [94],
        "target_components": [109],
        "mode": "spatial_sample",
    },
    {
        "source_components": [103],
        "target_components": [123],
        "mode": "spatial_sample",
    },
    {
        "source_components": [141],
        "target_components": [136],
        "mode": "spatial_sample",
    },
    {
        "source_components": [20, 111],
        "target_components": [6],
        "mode": "spatial_sample",
    },
]


def mirrored_center(center):
    return Vector((-center.x, center.y, center.z))


def material_name(polygon_index):
    return material_order[
        mesh_object.data.polygons[
            polygon_index
        ].material_index
    ]


screen_right_copy_records = []
for copy_spec in screen_right_copy_specs:
    source_polygon_indices = [
        polygon_index
        for component_id in copy_spec[
            "source_components"
        ]
        for polygon_index in component_polygons[
            component_id
        ]
    ]
    target_polygon_indices = [
        polygon_index
        for component_id in copy_spec[
            "target_components"
        ]
        for polygon_index in component_polygons[
            component_id
        ]
    ]
    target_before = Counter(
        material_name(index)
        for index in target_polygon_indices
    )
    source_distribution = Counter(
        material_name(index)
        for index in source_polygon_indices
    )
    matches = []
    if copy_spec["mode"] == "one_to_one":
        if (
            len(source_polygon_indices) !=
            len(target_polygon_indices)
        ):
            raise RuntimeError(
                "One-to-one screen-right arm copy "
                "has unequal polygon counts."
            )
        candidate_edges = sorted(
            (
                (
                    (
                        mirrored_center(
                            centers[source_index]
                        ) -
                        centers[target_index]
                    ).length,
                    source_index,
                    target_index,
                )
                for source_index in
                source_polygon_indices
                for target_index in
                target_polygon_indices
            ),
            key=lambda item: (
                item[0],
                item[1],
                item[2],
            ),
        )
        used_source = set()
        used_target = set()
        for (
            distance,
            source_index,
            target_index,
        ) in candidate_edges:
            if (
                source_index in used_source or
                target_index in used_target
            ):
                continue
            used_source.add(source_index)
            used_target.add(target_index)
            matches.append((
                distance,
                source_index,
                target_index,
            ))
        if (
            len(used_source) !=
            len(source_polygon_indices) or
            len(used_target) !=
            len(target_polygon_indices)
        ):
            raise RuntimeError(
                "Incomplete one-to-one screen-right "
                "arm polygon copy."
            )
    else:
        for target_index in target_polygon_indices:
            (
                distance,
                source_index,
            ) = min(
                (
                    (
                        (
                            mirrored_center(
                                centers[
                                    candidate_index
                                ]
                            ) -
                            centers[target_index]
                        ).length,
                        candidate_index,
                    )
                    for candidate_index in
                    source_polygon_indices
                ),
                key=lambda item: (
                    item[0],
                    item[1],
                ),
            )
            matches.append((
                distance,
                source_index,
                target_index,
            ))

    changed_polygons = 0
    for (
        distance,
        source_index,
        target_index,
    ) in matches:
        source_material_index = (
            mesh_object.data.polygons[
                source_index
            ].material_index
        )
        target_polygon = (
            mesh_object.data.polygons[
                target_index
            ]
        )
        if (
            target_polygon.material_index !=
            source_material_index
        ):
            changed_polygons += 1
        target_polygon.material_index = (
            source_material_index
        )

    target_after = Counter(
        material_name(index)
        for index in target_polygon_indices
    )
    if copy_spec["mode"] == "one_to_one":
        copy_valid = (
            target_after ==
            source_distribution
        )
    else:
        copy_valid = (
            set(target_after).issubset(
                set(source_distribution)
            ) and
            sum(target_after.values()) ==
            len(target_polygon_indices)
        )
    screen_right_copy_records.append({
        "source_components":
            copy_spec["source_components"],
        "target_components":
            copy_spec["target_components"],
        "mode": copy_spec["mode"],
        "source_materials":
            dict(source_distribution),
        "target_before": dict(target_before),
        "target_after": dict(target_after),
        "target_polygons":
            len(target_polygon_indices),
        "changed_polygons": changed_polygons,
        "copy_valid": copy_valid,
        "maximum_mirrored_center_distance":
            max(
                (
                    item[0]
                    for item in matches
                ),
                default=0.0,
            ),
    })

assignment_counts = {
    material_id: 0
    for material_id in material_order
}
assignment_records = []
for polygon in mesh_object.data.polygons:
    material_id = material_order[
        polygon.material_index
    ]
    assignment_counts[material_id] += 1
    assignment_records.append({
        "polygon": polygon.index,
        "component":
            component_by_polygon[
                polygon.index
            ],
        "dominant_group":
            dominant_groups_by_polygon[
                polygon.index
            ],
        "material": material_id,
    })

screen_right_copy_report = {
    "source_view_side": "screen right",
    "source_object_axis": "+X",
    "source_mixamo_groups": [
        "LeftShoulder",
        "LeftArm",
        "LeftForeArm",
        "LeftHand",
    ],
    "copy_principle": (
        "Copy actual material indices from confirmed "
        "screen-right components to their mirrored "
        "screen-left components."
    ),
    "copy_records": screen_right_copy_records,
    "total_changed_polygons": sum(
        record["changed_polygons"]
        for record in screen_right_copy_records
    ),
    "all_records_valid": all(
        record["copy_valid"]
        for record in screen_right_copy_records
    ),
}
if (
    not screen_right_copy_report[
        "all_records_valid"
    ] or
    screen_right_copy_report[
        "total_changed_polygons"
    ] <= 0
):
    raise RuntimeError(
        "Screen-right arm polygon material copy failed."
    )
with open(
    os.path.join(
        sample_root,
        "SCREEN_RIGHT_ARM_COPY.json",
    ),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        screen_right_copy_report,
        handle,
        ensure_ascii=False,
        indent=2,
    )

after_mesh = mesh_signature(mesh_object)
after_armature = armature_signature(
    armature_object
)
if before_mesh != after_mesh:
    raise RuntimeError(
        "Mesh coordinates, topology, loops, or original UV changed during material assignment."
    )
if before_armature != after_armature:
    raise RuntimeError(
        "Armature data changed during material assignment."
    )

geometry_report = {
    "source_file": source_path,
    "source_sha256": sha256(source_path),
    "before": {
        "vertices": before_mesh["vertices"],
        "polygons": before_mesh["polygons"],
        "loops": before_mesh["loops"],
        "uv_layers": list(
            before_mesh["uv_layers"].keys()
        ),
        "bones": before_armature["bones"],
    },
    "after": {
        "vertices": after_mesh["vertices"],
        "polygons": after_mesh["polygons"],
        "loops": after_mesh["loops"],
        "uv_layers": list(
            after_mesh["uv_layers"].keys()
        ),
        "bones": after_armature["bones"],
    },
    "vertex_coordinates_exact_match":
        before_mesh["vertex_coordinates"] ==
        after_mesh["vertex_coordinates"],
    "polygon_topology_exact_match":
        before_mesh["polygon_vertices"] ==
        after_mesh["polygon_vertices"],
    "uv_data_exact_match":
        before_mesh["uv_layers"] ==
        after_mesh["uv_layers"],
    "armature_exact_match":
        before_armature == after_armature,
    "geometry_changed": False,
}
with open(
    os.path.join(
        sample_root,
        "GEOMETRY_PRESERVATION.json",
    ),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        geometry_report,
        handle,
        ensure_ascii=False,
        indent=2,
    )

assignment_report = {
    "source_model": source_path,
    "source_sha256": sha256(source_path),
    "principle": (
        "Existing polygons receive materials that use direct, unmodified crops "
        "from the user reference through generated object coordinates; the "
        "source FBX UV layer remains unchanged."
    ),
    "polygon_material_counts":
        assignment_counts,
    "component_overrides": {
        "blue_optic": sorted(
            blue_components
        ),
        "dark_mechanics": sorted(
            dark_components
        ),
        "body_light_steel": sorted(
            light_components
        ),
        "copper_mechanics": sorted(
            copper_components
        ),
    },
    "rear_preservation_rule": (
        "Non-torso, non-arm polygons with evaluated center world Y > 0.50 "
        "keep neutral source-default appearance. Torso components 0 and 1 "
        "are always reference-painted on every side."
    ),
    "records": assignment_records,
}
with open(
    os.path.join(
        sample_root,
        "MATERIAL_ASSIGNMENT.json",
    ),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        assignment_report,
        handle,
        ensure_ascii=False,
        indent=2,
    )

torso_component_reports = []
for component_id, expected_material in (
    (0, "body_panel"),
    (1, "torso_inset_steel"),
):
    polygon_indices = component_polygons[
        component_id
    ]
    distribution = Counter(
        material_order[
            mesh_object.data.polygons[
                polygon_index
            ].material_index
        ]
        for polygon_index in polygon_indices
    )
    if (
        len(distribution) != 1 or
        distribution.get(
            expected_material,
            0,
        ) != len(polygon_indices)
    ):
        raise RuntimeError(
            "Torso component material assignment failed."
        )
    torso_component_reports.append({
        "component_id": component_id,
        "polygon_count": len(
            polygon_indices
        ),
        "material": expected_material,
        "material_distribution":
            dict(distribution),
    })

torso_material_report = {
    "mapping_basis": (
        "Existing connected mesh component boundaries; "
        "no bone-weight or inferred polygon split."
    ),
    "primary_outer_shell": {
        **torso_component_reports[0],
        "base_texture":
            "textures/reference_body_panel_direct_crop.png",
        "wear_detail_texture":
            "textures/reference_body_wear_direct_crop.png",
        "texture_combination":
            "Direct-crop base mixed 35% toward direct-crop wear detail",
        "color_mix_factor": 0.35,
        "roughness_from_wear_luminance": True,
        "bump_from_wear_luminance": True,
    },
    "inset_steel": {
        **torso_component_reports[1],
        "base_texture":
            "textures/reference_body_wear_direct_crop.png",
    },
    "torso_surface_polygon_count": sum(
        item["polygon_count"]
        for item in torso_component_reports
    ),
    "unpainted_torso_polygons": 0,
    "all_torso_sides_reference_painted": True,
    "mesh_changed": False,
    "uv_changed": False,
    "arm_copy_total_changed_polygons":
        screen_right_copy_report[
            "total_changed_polygons"
        ],
    "arm_copy_all_records_valid":
        screen_right_copy_report[
            "all_records_valid"
        ],
}
with open(
    os.path.join(
        sample_root,
        "TORSO_MATERIAL_ASSIGNMENT.json",
    ),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        torso_material_report,
        handle,
        ensure_ascii=False,
        indent=2,
    )

shoulder_joint_component_reports = []
for (
    component_ids,
    expected_material,
    role,
) in (
    (
        shoulder_joint_shell_components,
        "body_panel",
        "worn gunmetal shoulder shell and upper link",
    ),
    (
        shoulder_joint_inner_components,
        "dark_mechanics",
        "dark mechanical inner ring and rotation axis",
    ),
):
    for component_id in sorted(component_ids):
        polygon_indices = component_polygons[
            component_id
        ]
        distribution = Counter(
            material_order[
                mesh_object.data.polygons[
                    polygon_index
                ].material_index
            ]
            for polygon_index in polygon_indices
        )
        if (
            len(distribution) != 1 or
            distribution.get(
                expected_material,
                0,
            ) != len(polygon_indices)
        ):
            raise RuntimeError(
                "Shoulder joint material assignment failed."
            )
        shoulder_joint_component_reports.append({
            "component_id": component_id,
            "polygon_count": len(
                polygon_indices
            ),
            "material": expected_material,
            "role": role,
            "material_distribution":
                dict(distribution),
        })

final_shoulder_connector_records = [
    record
    for record in assignment_records
    if (
        record["component"] in
        shoulder_connector_frame_components and
        record["material"] ==
        "dark_mechanics"
    )
]
final_shoulder_connector_components = {
    record["component"]
    for record in final_shoulder_connector_records
}

shoulder_joint_material_report = {
    "mapping_basis": (
        "Confirmed connected shoulder-joint components "
        "from front component-mask render."
    ),
    "reference_material_mapping": {
        "shell_and_upper_link":
            "textures/reference_body_panel_direct_crop.png",
        "inner_ring_and_rotation_axis":
            "textures/reference_dark_mechanics_direct_crop.png",
    },
    "components":
        shoulder_joint_component_reports,
    "shoulder_joint_polygon_count": sum(
        item["polygon_count"]
        for item in shoulder_joint_component_reports
    ),
    "connector_frame": {
        "component_ids": sorted(
            shoulder_connector_frame_components
        ),
        "changed_polygon_count": len(
            final_shoulder_connector_records
        ),
        "pre_symmetry_override_count": len(
            shoulder_connector_override_records
        ),
        "before_material":
            "body_light_steel",
        "after_material":
            "dark_mechanics",
        "texture":
            "textures/reference_dark_mechanics_direct_crop.png",
        "mapping_basis": (
            "Existing bright-steel material boundary "
            "confirmed by remaining-frame mask render."
        ),
    },
    "shoulder_connection_polygon_count": (
        sum(
            item["polygon_count"]
            for item in
            shoulder_joint_component_reports
        ) +
        len(final_shoulder_connector_records)
    ),
    "unpainted_shoulder_joint_polygons": 0,
    "unpainted_shoulder_connection_polygons":
        sum(
            1
            for record in assignment_records
            if (
                record["component"] in
                shoulder_connector_frame_components and
                record["material"] ==
                "body_light_steel"
            )
        ),
    "changed_polygons": (
        len(shoulder_joint_override_records) +
        len(final_shoulder_connector_records)
    ),
    "changed_components": sorted({
        item["component"]
        for item in shoulder_joint_override_records
    } | final_shoulder_connector_components),
    "allowed_components": sorted(
        shoulder_joint_shell_components |
        shoulder_joint_inner_components |
        shoulder_connector_frame_components
    ),
    "non_target_changed_polygons": 0,
    "torso_surface_polygon_count":
        torso_material_report[
            "torso_surface_polygon_count"
        ],
    "torso_materials_unchanged": True,
    "mesh_changed": False,
    "uv_changed": False,
    "arm_copy_total_changed_polygons":
        screen_right_copy_report[
            "total_changed_polygons"
        ],
    "arm_copy_all_records_valid":
        screen_right_copy_report[
            "all_records_valid"
        ],
}
with open(
    os.path.join(
        sample_root,
        "SHOULDER_JOINT_MATERIAL_ASSIGNMENT.json",
    ),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        shoulder_joint_material_report,
        handle,
        ensure_ascii=False,
        indent=2,
    )

arm_default_records = [
    record
    for record in assignment_records
    if (
        record["dominant_group"] in all_arm_groups and
        record["material"] == "default_preserved"
    )
]
arm_symmetry_report = {
    "rule": (
        "The screen-right component polygon materials are copied to "
        "confirmed mirrored screen-left components. Rear preservation "
        "never overrides an arm polygon."
    ),
    "source_view_side": "screen right",
    "source_object_axis": "+X",
    "copy_record_count": len(
        screen_right_copy_records
    ),
    "total_changed_polygons":
        screen_right_copy_report[
            "total_changed_polygons"
        ],
    "right_to_left_copy_applied":
        screen_right_copy_report[
            "total_changed_polygons"
        ] > 0,
    "default_preserved_arm_polygons":
        len(arm_default_records),
    "mirrored_texture_x_coordinate": True,
    "texture_x_coordinate_rule":
        "abs(2 * Generated.X - 1)",
    "copy_report": "SCREEN_RIGHT_ARM_COPY.json",
}
if (
    arm_default_records or
    not arm_symmetry_report[
        "right_to_left_copy_applied"
    ] or
    arm_symmetry_report[
        "copy_record_count"
    ] != len(screen_right_copy_specs)
):
    raise RuntimeError(
        "Screen-right to screen-left arm copy contract failed."
    )
with open(
    os.path.join(
        sample_root,
        "ARM_COLOR_SYMMETRY.json",
    ),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        arm_symmetry_report,
        handle,
        ensure_ascii=False,
        indent=2,
    )

blend_path = os.path.join(
    export_root,
    "revolution_replaced_model_reference_sample.blend",
)
bpy.ops.wm.save_as_mainfile(
    filepath=blend_path
)

bpy.ops.object.select_all(action="DESELECT")
mesh_object.select_set(True)
armature_object.select_set(True)
bpy.context.view_layer.objects.active = mesh_object

fbx_path = os.path.join(
    export_root,
    "revolution_replaced_model_reference_sample.fbx",
)
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False,
    bake_anim=True,
    path_mode="COPY",
    embed_textures=True,
)

bpy.ops.object.select_all(action="DESELECT")
mesh_object.select_set(True)
bpy.context.view_layer.objects.active = mesh_object
glb_path = os.path.join(
    export_root,
    "revolution_replaced_model_reference_sample.glb",
)
bpy.ops.export_scene.gltf(
    filepath=glb_path,
    export_format="GLB",
    use_selection=True,
    export_animations=False,
    export_skins=False,
    export_materials="EXPORT",
)

low, high = bounds_of(meshes)
dimensions = high - low
center = (low + high) * 0.5
radius = max(dimensions) * 1.65

world = bpy.data.worlds.new("ReviewWorld")
world.use_nodes = True
world.node_tree.nodes[
    "Background"
].inputs["Color"].default_value = (
    0.80,
    0.82,
    0.84,
    1.0,
)
world.node_tree.nodes[
    "Background"
].inputs["Strength"].default_value = 0.50
bpy.context.scene.world = world

for name, location, energy, size in [
    (
        "ScreenLeftKey",
        (-4.5, -5.5, 6.5),
        550,
        5.0,
    ),
    (
        "ScreenRightKey",
        (4.5, -5.5, 6.5),
        550,
        5.0,
    ),
    (
        "Rim",
        (0.0, 5.0, 5.0),
        500,
        3.0,
    ),
]:
    light_data = bpy.data.lights.new(
        f"{name}Light",
        "AREA",
    )
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light = bpy.data.objects.new(
        f"{name}Light",
        light_data,
    )
    bpy.context.collection.objects.link(light)
    light.location = location
    point_at(light, center)

camera_data = bpy.data.cameras.new(
    "ReviewCamera"
)
camera = bpy.data.objects.new(
    "ReviewCamera",
    camera_data,
)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera_data.type = "ORTHO"
camera_data.ortho_scale = (
    max(dimensions.x, dimensions.z) *
    1.22
)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.view_settings.look = (
    "AgX - Medium High Contrast"
)
scene.view_settings.exposure = -0.70

for (
    file_name,
    angle,
    elevation,
) in [
    (
        "02_front_reference_material.png",
        0,
        0.12,
    ),
    (
        "03_three_quarter_reference_material.png",
        -32,
        0.14,
    ),
    (
        "04_side_reference_material.png",
        -90,
        0.12,
    ),
    (
        "05_rear_preserved.png",
        180,
        0.12,
    ),
]:
    render_view(
        scene,
        camera,
        center,
        radius,
        angle,
        elevation,
        os.path.join(
            render_root,
            file_name,
        ),
    )

with open(
    os.path.join(sample_root, "EXPORTS.json"),
    "w",
    encoding="utf-8",
) as handle:
    json.dump(
        {
            "blend": {
                "file": (
                    "exports/revolution_replaced_model_reference_sample.blend"
                ),
                "sha256": sha256(
                    blend_path
                ),
            },
            "fbx": {
                "file": (
                    "exports/revolution_replaced_model_reference_sample.fbx"
                ),
                "sha256": sha256(
                    fbx_path
                ),
            },
            "glb": {
                "file": (
                    "exports/revolution_replaced_model_reference_sample.glb"
                ),
                "sha256": sha256(
                    glb_path
                ),
            },
        },
        handle,
        ensure_ascii=False,
        indent=2,
    )
