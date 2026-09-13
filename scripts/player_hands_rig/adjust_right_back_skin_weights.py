"""Analyze or repair the isolated lower right-back RightArm weight outliers.

The repair changes skin influences only. Vertex positions, UVs, topology, the
source FBX, finger rows, and every animation value remain untouched.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import re
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WEIGHTS_PATH = ROOT / "Assets/_Project/Art/Player/HandsRig/player_hands_skin_weights.json"
REPORT_PATH = ROOT / "docs/validation/consumable_right_back_fix_2026-09-10/right_back_weight_analysis.txt"
ROUND2_REPORT_PATH = ROOT / (
    "docs/validation/consumable_right_back_fix_2026-09-10/"
    "review_103229/weight_candidate_analysis.txt"
)
ROUND2_BASE_WEIGHTS_PATH = ROOT / (
    "Backups/ConsumableRightBackFix_2026-09-10/round2_before/"
    "player_hands_skin_weights.json"
)
ROUND2_TOPOLOGY_PATH = ROUND2_REPORT_PATH.with_name("topology_pose.json")
ROUND2_SMOOTH_REPORT_PATH = ROUND2_REPORT_PATH.with_name("weight_smoothing.txt")
ROUND2_NATIVE_WEIGHTS_PATH = ROUND2_REPORT_PATH.with_name("native_fbx_weights.json")
ROUND2_NATIVE_REPORT_PATH = ROUND2_REPORT_PATH.with_name("native_weight_restore.txt")
ROUND2_NATIVE_MANIFEST_PATH = ROUND2_REPORT_PATH.with_name("native_transfer_manifest.json")

LOWER_RIGHT_BACK_BONES = {"Hips", "RightUpLeg", "Spine", "Spine01", "Spine02"}
ARM_BONE = "RightArm"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Remove RightArm from only the classified lower right-back rows.",
    )
    parser.add_argument(
        "--pose-layout",
        action="store_true",
        help="Report the current read-only hand-space item and hand bounds.",
    )
    parser.add_argument(
        "--round2-analyze",
        action="store_true",
        help="Audit all remaining arm/shoulder and torso mixed rows around the visible panel.",
    )
    parser.add_argument(
        "--round2-apply",
        action="store_true",
        help="Remove arm/forearm pull from torso-mixed visible-panel positions.",
    )
    parser.add_argument(
        "--round2-smooth-analyze",
        action="store_true",
        help="Predict topology-aware right-back weight smoothing without writing weights.",
    )
    parser.add_argument(
        "--round2-smooth-apply",
        action="store_true",
        help="Apply topology-aware smoothing from the pre-repair right-back weights.",
    )
    parser.add_argument(
        "--round2-native-analyze",
        action="store_true",
        help="Predict a localized native-FBX right-back weight restore without writing weights.",
    )
    parser.add_argument(
        "--round2-native-apply",
        action="store_true",
        help="Apply the smallest numerically safe localized native-FBX right-back restore.",
    )
    return parser.parse_args()


def position_key(row: dict) -> tuple[float, float, float]:
    position = row["position"]
    return tuple(round(float(position[axis]), 6) for axis in ("x", "y", "z"))


def influence_map(row: dict) -> dict[str, float]:
    return {item["bone"]: float(item["weight"]) for item in row["influences"]}


def is_lower_right_back_outlier(row: dict) -> bool:
    """Exclude the anatomical shoulder/upper arm and select the visible spike strip."""
    position = row["position"]
    influences = influence_map(row)
    return (
        0.05 <= float(position["x"]) <= 0.20
        and 1.00 <= float(position["y"]) <= 1.20
        and 0.10 <= float(position["z"]) <= 0.30
        and influences.get(ARM_BONE, 0.0) >= 0.01
        and any(influences.get(bone, 0.0) > 0.0 for bone in LOWER_RIGHT_BACK_BONES)
        and "RightShoulder" not in influences
        and "RightForeArm" not in influences
        and "RightHand" not in influences
    )


def format_influences(row: dict) -> str:
    return ",".join(
        f'{item["bone"]}:{float(item["weight"]):.6f}' for item in row["influences"]
    )


def normalized_without_bones(row: dict, removed_bones: set[str]) -> list[dict]:
    retained = [
        {"bone": item["bone"], "weight": float(item["weight"])}
        for item in row["influences"]
        if item["bone"] not in removed_bones
    ]
    total = sum(item["weight"] for item in retained)
    if total <= 0.0:
        raise RuntimeError(f"Outlier has no retained weight at {position_key(row)}")
    for item in retained:
        item["weight"] /= total
    return retained


def normalized_without_right_arm(row: dict) -> list[dict]:
    return normalized_without_bones(row, {ARM_BONE})


def is_round2_audit_candidate(row: dict) -> bool:
    """Broad read-only region around the panel visible in the user's second video."""
    position = row["position"]
    influences = influence_map(row)
    return (
        0.00 <= float(position["x"]) <= 0.25
        and 0.90 <= float(position["y"]) <= 1.30
        and 0.05 <= float(position["z"]) <= 0.36
        and (influences.get("RightArm", 0.0) >= 0.01 or influences.get("RightForeArm", 0.0) >= 0.01)
        and any(influences.get(bone, 0.0) > 0.0 for bone in LOWER_RIGHT_BACK_BONES)
        and "RightHand" not in influences
    )


def is_round2_repair_source(row: dict) -> bool:
    """Select arm-driven rows on the torso-weighted back panel, not the anatomical arm."""
    position = row["position"]
    influences = influence_map(row)
    return (
        0.0 <= float(position["x"]) <= 0.25
        and 0.90 <= float(position["y"]) <= 1.30
        and 0.05 <= float(position["z"]) <= 0.36
        and (influences.get("RightArm", 0.0) >= 0.01 or influences.get("RightForeArm", 0.0) >= 0.01)
        and any(influences.get(bone, 0.0) > 0.0 for bone in LOWER_RIGHT_BACK_BONES)
        and "RightHand" not in influences
    )


def apply_round2_repair(data: dict, source_bytes: bytes) -> None:
    original = copy.deepcopy(data)
    rows = data["vertices"]
    selected_positions = {position_key(row) for row in rows if is_round2_repair_source(row)}
    selected_indices = [
        index
        for index, row in enumerate(rows)
        if position_key(row) in selected_positions and is_round2_repair_source(row)
    ]
    if not selected_indices:
        raise RuntimeError("No remaining Hips/RightArm visible-panel rows matched the round-two region.")

    report = [
        "Round-2 right-back visible-panel repair",
        f"source={WEIGHTS_PATH.relative_to(ROOT).as_posix()}",
        f"sourceSha256={digest_bytes(source_bytes)}",
        f"selectedRows={len(selected_indices)}",
        f"selectedUniquePositions={len(selected_positions)}",
        "selection=x[0.00,0.25] y[0.90,1.30] z[0.05,0.36]; RightArm or RightForeArm>=0.01; "
        "co-influenced by torso; excludes hand; duplicate positions kept continuous",
        "",
    ]
    for index in selected_indices:
        row = rows[index]
        report.append(
            f"row={index} position={position_key(row)} "
            f"uv=({float(row['uv']['x']):.6f},{float(row['uv']['y']):.6f}) "
            f"before={format_influences(row)}"
        )
        row["influences"] = normalized_without_bones(row, {"RightArm", "RightForeArm"})
        report.append(f"row={index} after={format_influences(row)}")

    selected_set = set(selected_indices)
    for index, (before, after) in enumerate(zip(original["vertices"], rows)):
        if before["position"] != after["position"] or before["uv"] != after["uv"]:
            raise RuntimeError(f"Geometry or UV changed at row {index}")
        if index not in selected_set and before != after:
            raise RuntimeError(f"Unselected row changed at {index}")
        if index in selected_set and (
            influence_map(after).get("RightArm", 0.0) > 0.0
            or influence_map(after).get("RightForeArm", 0.0) > 0.0
        ):
            raise RuntimeError(f"Right arm or forearm remained on selected row {index}")
        total = sum(float(item["weight"]) for item in after["influences"])
        if len(after["influences"]) > 4 or abs(total - 1.0) > 0.0001:
            raise RuntimeError(f"Invalid normalized influences at row {index}: {total}")

    output_bytes = json.dumps(data, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    WEIGHTS_PATH.write_bytes(output_bytes)
    report.extend(
        [
            "",
            f"outputSha256={digest_bytes(output_bytes)}",
            "geometryChanged=False",
            "uvChanged=False",
            "topologyChanged=False",
            "fingerRowsChanged=False",
            "animationChanged=False",
        ]
    )
    ROUND2_REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    output_report = ROUND2_REPORT_PATH.with_name("weight_application.txt")
    output_report.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(
        "RIGHT_BACK_ROUND2_APPLIED "
        f"rows={len(selected_indices)} positions={len(selected_positions)} report={output_report}"
    )


def analyze_round2_candidates(data: dict) -> None:
    rows = data["vertices"]
    selected = [(index, row) for index, row in enumerate(rows) if is_round2_audit_candidate(row)]
    unique_positions = {position_key(row) for _, row in selected}
    bone_sets = Counter(tuple(item["bone"] for item in row["influences"]) for _, row in selected)
    bins: Counter[tuple[float, float, float]] = Counter()
    for _, row in selected:
        position = row["position"]
        bins[
            (
                round(float(position["x"]) / 0.04) * 0.04,
                round(float(position["y"]) / 0.04) * 0.04,
                round(float(position["z"]) / 0.04) * 0.04,
            )
        ] += 1

    report = [
        "Round-2 right-back visible-panel weight audit",
        f"source={WEIGHTS_PATH.relative_to(ROOT).as_posix()}",
        f"totalRows={len(rows)}",
        f"candidateRows={len(selected)}",
        f"candidateUniquePositions={len(unique_positions)}",
        "selection=x[0.00,0.25] y[0.90,1.30] z[0.05,0.36]; remaining RightArm or RightForeArm; "
        "co-influenced by torso; excludes hand",
        "boneSets=" + ",".join(
            f"{'/'.join(names)}:{count}" for names, count in sorted(bone_sets.items())
        ),
        "positionBins=" + ",".join(
            f"({x:.2f},{y:.2f},{z:.2f}):{count}" for (x, y, z), count in sorted(bins.items())
        ),
        "",
    ]
    for index, row in selected:
        report.append(
            f"row={index} position={position_key(row)} "
            f"uv=({float(row['uv']['x']):.6f},{float(row['uv']['y']):.6f}) "
            f"influences={format_influences(row)}"
        )
    ROUND2_REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    ROUND2_REPORT_PATH.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(
        "RIGHT_BACK_ROUND2_ANALYZED "
        f"rows={len(selected)} positions={len(unique_positions)} report={ROUND2_REPORT_PATH}"
    )


def parse_influence_label(value: str) -> list[dict]:
    influences = []
    for item in value.split(","):
        bone, weight = item.rsplit(":", 1)
        influences.append({"bone": bone, "weight": float(weight)})
    total = sum(item["weight"] for item in influences)
    for item in influences:
        item["weight"] /= total
    return influences


def reconstructed_pre_repair_data() -> tuple[dict, bytes]:
    source_bytes = ROUND2_BASE_WEIGHTS_PATH.read_bytes()
    data = json.loads(source_bytes.decode("utf-8-sig"))
    previous_report = REPORT_PATH.read_text(encoding="utf-8")
    restored = 0
    for line in previous_report.splitlines():
        match = re.match(r"row=(\d+) .* before=(.+)$", line)
        if not match:
            continue
        index = int(match.group(1))
        data["vertices"][index]["influences"] = parse_influence_label(match.group(2))
        restored += 1
    if restored != 280:
        raise RuntimeError(f"Expected 280 first-pass rows to reconstruct, found {restored}")
    return data, source_bytes


def topology_rows(data: dict, topology: dict) -> list[int]:
    rows = data["vertices"]

    def cell(point: dict) -> tuple[int, int, int]:
        return tuple(math.floor(float(point[axis]) / 0.0001) for axis in ("x", "y", "z"))

    cells: dict[tuple[int, int, int], list[int]] = defaultdict(list)
    for index, row in enumerate(rows):
        cells[cell(row["position"])].append(index)
    result = []
    for vertex, (point, uv) in enumerate(zip(topology["vertices"], topology["uv"])):
        base = cell(point)
        match = None
        best = 4e-10
        for x in range(-1, 2):
            for y in range(-1, 2):
                for z in range(-1, 2):
                    for row_index in cells.get((base[0] + x, base[1] + y, base[2] + z), []):
                        row = rows[row_index]
                        row_point = row["position"]
                        distance = sum(
                            (float(row_point[axis]) - float(point[axis])) ** 2
                            for axis in ("x", "y", "z")
                        )
                        uv_distance = (
                            (float(row["uv"]["x"]) - float(uv["x"])) ** 2
                            + (float(row["uv"]["y"]) - float(uv["y"])) ** 2
                        )
                        if distance > best or uv_distance > 4e-8:
                            continue
                        match = row_index
                        best = distance
        if match is None:
            raise RuntimeError(f"Topology correspondence missing at Unity vertex {vertex}")
        result.append(match)
    return result


def compressed_weights(values: dict[int, float]) -> dict[int, float]:
    kept = sorted(((bone, weight) for bone, weight in values.items() if weight > 1e-9),
                  key=lambda item: item[1], reverse=True)[:4]
    total = sum(weight for _, weight in kept)
    if total <= 0.0:
        raise RuntimeError("Weight smoothing produced an empty influence set")
    return {bone: weight / total for bone, weight in kept}


def mixed_weights(first: dict[int, float], second: dict[int, float], second_ratio: float) -> dict[int, float]:
    combined = defaultdict(float)
    for bone, weight in first.items():
        combined[bone] += weight * (1.0 - second_ratio)
    for bone, weight in second.items():
        combined[bone] += weight * second_ratio
    return compressed_weights(combined)


def average_weights(values: list[dict[int, float]]) -> dict[int, float]:
    combined = defaultdict(float)
    for weights in values:
        for bone, weight in weights.items():
            combined[bone] += weight / len(values)
    return compressed_weights(combined)


def smoothed_weight_step(
    weights: list[dict[int, float]],
    ratios: list[tuple[float, int, int]],
    editable: set[int],
    position_groups: dict[tuple[float, float, float], list[int]],
) -> list[dict[int, float]]:
    bad_neighbors: dict[int, list[int]] = defaultdict(list)
    for ratio, first, second in ratios:
        if ratio <= 1.12:
            break
        if first in editable:
            bad_neighbors[first].append(second)
        if second in editable:
            bad_neighbors[second].append(first)
    if not bad_neighbors:
        return weights
    updated = list(weights)
    for vertex, neighbors in bad_neighbors.items():
        neighbor_average = average_weights([weights[neighbor] for neighbor in neighbors])
        updated[vertex] = mixed_weights(weights[vertex], neighbor_average, 0.68)
    for group in position_groups.values():
        if len(group) <= 1:
            continue
        common = average_weights([updated[vertex] for vertex in group])
        for vertex in group:
            updated[vertex] = common
    return updated


def transform_point(flat: list[float], bone: int, point: tuple[float, float, float]) -> tuple[float, float, float]:
    offset = bone * 16
    x, y, z = point
    return (
        flat[offset] * x + flat[offset + 1] * y + flat[offset + 2] * z + flat[offset + 3],
        flat[offset + 4] * x + flat[offset + 5] * y + flat[offset + 6] * z + flat[offset + 7],
        flat[offset + 8] * x + flat[offset + 9] * y + flat[offset + 10] * z + flat[offset + 11],
    )


def distance(first: tuple[float, float, float], second: tuple[float, float, float]) -> float:
    return math.sqrt(sum((first[axis] - second[axis]) ** 2 for axis in range(3)))


def smoothing_metric(
    points: list[tuple[float, float, float]],
    weights: list[dict[int, float]],
    transforms: list[float],
    edges: list[tuple[int, int]],
) -> tuple[float, list[tuple[float, int, int]]]:
    posed = []
    for point, vertex_weights in zip(points, weights):
        result = [0.0, 0.0, 0.0]
        for bone, weight in vertex_weights.items():
            transformed = transform_point(transforms, bone, point)
            for axis in range(3):
                result[axis] += transformed[axis] * weight
        posed.append(tuple(result))
    ratios = []
    maximum = 1.0
    for first, second in edges:
        rest_length = distance(points[first], points[second])
        if rest_length <= 1e-6:
            continue
        ratio = distance(posed[first], posed[second]) / rest_length
        maximum = max(maximum, ratio)
        ratios.append((ratio, first, second))
    return maximum, sorted(ratios, reverse=True)


def smoothing_metric_across_poses(
    points: list[tuple[float, float, float]],
    weights: list[dict[int, float]],
    poses: list[tuple[str, list[float]]],
    edges: list[tuple[int, int]],
    evaluated: set[int],
) -> tuple[float, list[tuple[float, int, int]]]:
    edge_maximum = {edge: 1.0 for edge in edges}
    for _, transforms in poses:
        posed = {}
        for vertex in evaluated:
            point = points[vertex]
            result = [0.0, 0.0, 0.0]
            for bone, weight in weights[vertex].items():
                transformed = transform_point(transforms, bone, point)
                for axis in range(3):
                    result[axis] += transformed[axis] * weight
            posed[vertex] = tuple(result)
        for edge in edges:
            first, second = edge
            rest_length = distance(points[first], points[second])
            if rest_length <= 1e-6:
                continue
            ratio = distance(posed[first], posed[second]) / rest_length
            edge_maximum[edge] = max(edge_maximum[edge], ratio)
    ratios = sorted(
        ((ratio, edge[0], edge[1]) for edge, ratio in edge_maximum.items()),
        reverse=True,
    )
    return (ratios[0][0] if ratios else 1.0), ratios


def smooth_round2_topology(apply: bool) -> None:
    data, base_bytes = reconstructed_pre_repair_data()
    original = copy.deepcopy(data)
    topology = json.loads(ROUND2_TOPOLOGY_PATH.read_text(encoding="utf-8"))
    if len(topology["vertices"]) != 17230 or len(topology["boneNames"]) != 54:
        raise RuntimeError("Unexpected shared player topology snapshot")
    if float(topology["posedPredictionMaxError"]) > 0.0001:
        raise RuntimeError("Exported skinning matrices did not reproduce the observed pose")
    mapping = topology_rows(data, topology)
    names = topology["boneNames"]
    name_to_bone = {name: index for index, name in enumerate(names)}
    points = [tuple(float(point[axis]) for axis in ("x", "y", "z")) for point in topology["vertices"]]
    weights = [
        compressed_weights({name_to_bone[item["bone"]]: float(item["weight"]) for item in data["vertices"][row]["influences"]})
        for row in mapping
    ]
    triangles = topology["triangles"]

    def audit_point(point: tuple[float, float, float]) -> bool:
        return 0.02 <= point[0] <= 0.24 and 0.92 <= point[1] <= 1.44 and 0.06 <= point[2] <= 0.36

    mesh_triangles = [tuple(triangles[offset:offset + 3]) for offset in range(0, len(triangles), 3)]
    scanned_triangles = []
    editable = set()
    for triangle_index, triangle in enumerate(mesh_triangles):
        if not any(audit_point(points[vertex]) for vertex in triangle):
            continue
        scanned_triangles.append(triangle_index)
        editable.update(triangle)
    if len(scanned_triangles) != 897:
        raise RuntimeError(f"Expected 897 audited triangles, found {len(scanned_triangles)}")

    expanded_triangles = set(scanned_triangles)
    expansion_rings = 3
    for _ in range(expansion_rings):
        added = {
            index for index, triangle in enumerate(mesh_triangles)
            if any(vertex in editable for vertex in triangle)
        }
        expanded_triangles.update(added)
        for index in added:
            editable.update(mesh_triangles[index])
    constraint_triangles = {
        index for index, triangle in enumerate(mesh_triangles)
        if any(vertex in editable for vertex in triangle)
    }
    constraint_vertices = {
        vertex for index in constraint_triangles for vertex in mesh_triangles[index]
    }
    edge_set = set()
    for index in constraint_triangles:
        triangle = mesh_triangles[index]
        for first, second in ((triangle[0], triangle[1]), (triangle[1], triangle[2]), (triangle[2], triangle[0])):
            if first != second:
                edge_set.add(tuple(sorted((first, second))))
    edges = sorted(edge_set)
    constraint_edges = edges

    # UV seams are separate Unity vertices. Keeping coincident vertices on the same
    # smoothed weight prevents a hidden seam from recreating the fan deformation.
    position_groups: dict[tuple[float, float, float], list[int]] = defaultdict(list)
    for vertex in editable:
        position_groups[tuple(round(value, 6) for value in points[vertex])].append(vertex)

    pose_files = sorted(ROUND2_REPORT_PATH.with_name("pose_matrices").glob("*.json"))
    if len(pose_files) != 64:
        raise RuntimeError(f"Expected 64 natural-playback bone poses, found {len(pose_files)}")
    poses = []
    maximum_prediction_error = 0.0
    for pose_file in pose_files:
        pose = json.loads(pose_file.read_text(encoding="utf-8"))
        maximum_prediction_error = max(maximum_prediction_error, float(pose["posedPredictionMaxError"]))
        poses.append((f'{pose["target"]}/{int(pose["phase"])}', pose["boneTransforms"]))
    if maximum_prediction_error > 0.0001:
        raise RuntimeError("A natural-playback bone pose did not reproduce its BakeMesh result")

    initial_maximum, initial_ratios = smoothing_metric_across_poses(points, weights, poses, constraint_edges, constraint_vertices)
    stage1_iterations = 0
    for stage1_iterations in range(1, 161):
        stage1_single_maximum, stage1_single_ratios = smoothing_metric(
            points, weights, topology["boneTransforms"], constraint_edges
        )
        if stage1_single_maximum <= 1.35:
            break
        updated = smoothed_weight_step(weights, stage1_single_ratios, editable, position_groups)
        if updated is weights:
            break
        weights = updated
    stage1_maximum, _ = smoothing_metric_across_poses(points, weights, poses, constraint_edges, constraint_vertices)
    iterations = 0
    for iterations in range(1, 161):
        maximum, ratios = smoothing_metric_across_poses(points, weights, poses, constraint_edges, constraint_vertices)
        if maximum <= 1.35:
            break
        updated = smoothed_weight_step(weights, ratios, editable, position_groups)
        if updated is weights:
            break
        weights = updated

    topology_neighbors: dict[int, list[int]] = defaultdict(list)
    for first, second in edges:
        if first in editable:
            topology_neighbors[first].append(second)
        if second in editable:
            topology_neighbors[second].append(first)
    global_iterations = 40
    for _ in range(global_iterations):
        updated = list(weights)
        for vertex, neighbors in topology_neighbors.items():
            neighbor_average = average_weights([weights[neighbor] for neighbor in neighbors])
            updated[vertex] = mixed_weights(weights[vertex], neighbor_average, 0.38)
        for group in position_groups.values():
            if len(group) <= 1:
                continue
            common = average_weights([updated[vertex] for vertex in group])
            for vertex in group:
                updated[vertex] = common
        weights = updated

    refinement_iterations = 0
    for refinement_iterations in range(1, 81):
        maximum, ratios = smoothing_metric_across_poses(points, weights, poses, constraint_edges, constraint_vertices)
        if maximum <= 1.35:
            break
        updated = smoothed_weight_step(weights, ratios, editable, position_groups)
        if updated is weights:
            break
        weights = updated
    final_maximum, final_ratios = smoothing_metric_across_poses(points, weights, poses, constraint_edges, constraint_vertices)

    maximum_weight_discontinuity = 0.0
    for first, second in edges:
        bones = set(weights[first]) | set(weights[second])
        maximum_weight_discontinuity = max(
            maximum_weight_discontinuity,
            sum(abs(weights[first].get(bone, 0.0) - weights[second].get(bone, 0.0)) for bone in bones),
        )

    row_proposals: dict[int, list[dict[int, float]]] = defaultdict(list)
    for vertex in editable:
        row_proposals[mapping[vertex]].append(weights[vertex])
    changed_rows = []
    for row_index, proposals in row_proposals.items():
        final_weights = average_weights(proposals)
        influences = [
            {"bone": names[bone], "weight": weight}
            for bone, weight in sorted(final_weights.items(), key=lambda item: item[1], reverse=True)
        ]
        if influences != data["vertices"][row_index]["influences"]:
            data["vertices"][row_index]["influences"] = influences
            changed_rows.append(row_index)

    for index, (before, after) in enumerate(zip(original["vertices"], data["vertices"])):
        if before["position"] != after["position"] or before["uv"] != after["uv"]:
            raise RuntimeError(f"Geometry or UV changed at row {index}")
        if index not in row_proposals and before != after:
            raise RuntimeError(f"Non-topological row changed at {index}")
        total = sum(float(item["weight"]) for item in after["influences"])
        if len(after["influences"]) > 4 or abs(total - 1.0) > 0.0001:
            raise RuntimeError(f"Invalid smoothed influences at row {index}: {total}")

    editable_points = [points[vertex] for vertex in editable]
    report = [
        "Round-2 topology-aware right-back weight smoothing",
        f"mode={'APPLY' if apply else 'ANALYZE'}",
        f"base={ROUND2_BASE_WEIGHTS_PATH.relative_to(ROOT).as_posix()}",
        f"baseSha256={digest_bytes(base_bytes)}",
        "firstPassRowsReconstructed=280",
        f"naturalPlaybackBonePoses={len(poses)}; predictionMaxError={maximum_prediction_error:.9f}",
        f"topologyVertices={len(points)}; coreTriangles={len(scanned_triangles)}; expansionRings={expansion_rings}; "
        f"expandedTriangles={len(expanded_triangles)}",
        f"editableVertices={len(editable)}; constraintTriangles={len(constraint_triangles)}; "
        f"constraintVertices={len(constraint_vertices)}; constraintEdges={len(edges)}",
        f"proposedRows={len(row_proposals)}; changedRows={len(changed_rows)}",
        "editableBoundsMin=" + ",".join(f"{min(point[axis] for point in editable_points):.6f}" for axis in range(3)),
        "editableBoundsMax=" + ",".join(f"{max(point[axis] for point in editable_points):.6f}" for axis in range(3)),
        f"initialPredictedMaxStretch={initial_maximum:.6f}",
        f"singlePoseSeedMaxStretchAcross64Poses={stage1_maximum:.6f}; stage1Iterations={stage1_iterations}",
        f"finalPredictedMaxStretch={final_maximum:.6f}",
        f"multiPoseIterations={iterations}; globalTopologySmoothingIterations={global_iterations}; "
        f"refinementIterations={refinement_iterations}; target=1.350000",
        f"maximumTopologicalWeightL1Discontinuity={maximum_weight_discontinuity:.6f}",
        "initialWorstEdges=" + ",".join(f"{ratio:.6f}:{first}-{second}" for ratio, first, second in initial_ratios[:12]),
        "finalWorstEdges=" + ",".join(f"{ratio:.6f}:{first}-{second}" for ratio, first, second in final_ratios[:12]),
        "geometryChanged=False",
        "uvChanged=False",
        "topologyChanged=False",
        "fingerRowsChanged=False",
        "animationChanged=False",
    ]
    if apply:
        output_bytes = json.dumps(data, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        WEIGHTS_PATH.write_bytes(output_bytes)
        report.append(f"outputSha256={digest_bytes(output_bytes)}")
    ROUND2_SMOOTH_REPORT_PATH.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(
        f"RIGHT_BACK_ROUND2_SMOOTH_{'APPLIED' if apply else 'ANALYZED'} "
        f"initial={initial_maximum:.6f} final={final_maximum:.6f} rows={len(changed_rows)} "
        f"report={ROUND2_SMOOTH_REPORT_PATH}"
    )


def native_round2_restore(apply: bool) -> None:
    current_bytes = WEIGHTS_PATH.read_bytes()
    current = json.loads(current_bytes.decode("utf-8-sig"))
    pre_repair, _ = reconstructed_pre_repair_data()
    native = json.loads(ROUND2_NATIVE_WEIGHTS_PATH.read_text(encoding="utf-8-sig"))
    topology = json.loads(ROUND2_TOPOLOGY_PATH.read_text(encoding="utf-8"))
    if len(topology["vertices"]) != 17230 or len(topology["boneNames"]) != 54:
        raise RuntimeError("Unexpected shared player topology snapshot")
    if len(native.get("vertices", [])) != 17230:
        raise RuntimeError("Unexpected native FBX weight export")

    mapping = topology_rows(current, topology)
    names = topology["boneNames"]
    name_to_bone = {name: index for index, name in enumerate(names)}
    points = [tuple(float(point[axis]) for axis in ("x", "y", "z")) for point in topology["vertices"]]

    def row_weights(row: dict) -> dict[int, float]:
        return compressed_weights({
            name_to_bone[item["bone"]]: float(item["weight"])
            for item in row["influences"]
        })

    current_weights = [row_weights(current["vertices"][row]) for row in mapping]
    pre_repair_weights = [row_weights(pre_repair["vertices"][row]) for row in mapping]
    native_weights = []
    maximum_native_position_error = 0.0
    maximum_native_uv_error = 0.0
    for vertex, (native_row, point, uv) in enumerate(zip(
        native["vertices"], topology["vertices"], topology["uv"]
    )):
        native_point = native_row["position"]
        maximum_native_position_error = max(maximum_native_position_error, math.sqrt(sum(
            (float(native_point[axis]) - float(point[axis])) ** 2 for axis in ("x", "y", "z")
        )))
        maximum_native_uv_error = max(maximum_native_uv_error, math.sqrt(
            (float(native_row["uv"]["x"]) - float(uv["x"])) ** 2
            + (float(native_row["uv"]["y"]) - float(uv["y"])) ** 2
        ))
        native_weights.append(row_weights(native_row))
    if maximum_native_position_error > 0.00002 or maximum_native_uv_error > 0.00002:
        raise RuntimeError(
            "Native FBX export does not match the audited topology: "
            f"position={maximum_native_position_error} uv={maximum_native_uv_error}"
        )

    pose_files = sorted(ROUND2_REPORT_PATH.with_name("pose_matrices").glob("*.json"))
    if len(pose_files) != 64:
        raise RuntimeError(f"Expected 64 natural-playback bone poses, found {len(pose_files)}")
    poses = []
    maximum_prediction_error = 0.0
    for pose_file in pose_files:
        pose = json.loads(pose_file.read_text(encoding="utf-8"))
        maximum_prediction_error = max(maximum_prediction_error, float(pose["posedPredictionMaxError"]))
        poses.append((f'{pose["target"]}/{int(pose["phase"])}', pose["boneTransforms"]))
    if maximum_prediction_error > 0.0001:
        raise RuntimeError("A natural-playback bone pose did not reproduce its BakeMesh result")

    triangles = topology["triangles"]
    mesh_triangles = [tuple(triangles[offset:offset + 3]) for offset in range(0, len(triangles), 3)]

    def audit_point(point: tuple[float, float, float]) -> bool:
        return 0.02 <= point[0] <= 0.24 and 0.92 <= point[1] <= 1.44 and 0.06 <= point[2] <= 0.36

    core_triangles = {
        index for index, triangle in enumerate(mesh_triangles)
        if any(audit_point(points[vertex]) for vertex in triangle)
    }
    if len(core_triangles) != 897:
        raise RuntimeError(f"Expected 897 audited triangles, found {len(core_triangles)}")

    def expanded_vertices(rings: int) -> set[int]:
        selected_triangles = set(core_triangles)
        selected_vertices = {
            vertex for triangle in selected_triangles for vertex in mesh_triangles[triangle]
        }
        for _ in range(rings):
            added = {
                index for index, triangle in enumerate(mesh_triangles)
                if any(vertex in selected_vertices for vertex in triangle)
            }
            selected_triangles.update(added)
            selected_vertices.update(
                vertex for triangle in added for vertex in mesh_triangles[triangle]
            )
        return selected_vertices

    def weight_l1(first: dict[int, float], second: dict[int, float]) -> float:
        return sum(abs(first.get(bone, 0.0) - second.get(bone, 0.0)) for bone in set(first) | set(second))

    def project_weights(
        selected: set[int], source_weights: list[dict[int, float]]
    ) -> tuple[list[dict[int, float]], set[int], set[int]]:
        proposals: dict[int, list[dict[int, float]]] = defaultdict(list)
        for vertex in selected:
            proposals[mapping[vertex]].append(source_weights[vertex])
        row_values = {row: average_weights(values) for row, values in proposals.items()}
        changed_rows = {
            row for row, value in row_values.items()
            if weight_l1(row_weights(current["vertices"][row]), value) > 0.000001
        }
        affected = {vertex for vertex, row in enumerate(mapping) if row in changed_rows}
        projected = list(current_weights)
        for vertex in affected:
            projected[vertex] = row_values[mapping[vertex]]
        return projected, changed_rows, affected

    def local_edges(affected: set[int]) -> tuple[list[tuple[int, int]], set[int]]:
        local_triangles = {
            index for index, triangle in enumerate(mesh_triangles)
            if any(vertex in affected for vertex in triangle)
        }
        evaluated = {vertex for index in local_triangles for vertex in mesh_triangles[index]}
        edge_set = set()
        for index in local_triangles:
            first, second, third = mesh_triangles[index]
            for edge in ((first, second), (second, third), (third, first)):
                if edge[0] != edge[1]:
                    edge_set.add(tuple(sorted(edge)))
        return sorted(edge_set), evaluated

    candidate_results = []
    for rings in range(0, 7):
        selected = expanded_vertices(rings)
        projected, changed_rows, affected = project_weights(selected, native_weights)
        edges, evaluated = local_edges(affected)
        before_maximum, _ = smoothing_metric_across_poses(
            points, current_weights, poses, edges, evaluated
        )
        pre_repair_maximum, _ = smoothing_metric_across_poses(
            points, pre_repair_weights, poses, edges, evaluated
        )
        native_maximum, native_ratios = smoothing_metric_across_poses(
            points, native_weights, poses, edges, evaluated
        )
        projected_maximum, ratios = smoothing_metric_across_poses(
            points, projected, poses, edges, evaluated
        )
        affected_points = [points[vertex] for vertex in affected]
        finger_vertices = [
            vertex for vertex in affected
            if any(names[bone].startswith((
                "RightThumb", "RightIndex", "RightMiddle", "RightRing", "RightLittle"
            )) or names[bone] == "RightHand" for bone in current_weights[vertex])
        ]
        candidate_results.append({
            "rings": rings,
            "selected": selected,
            "changed_rows": changed_rows,
            "affected": affected,
            "projected": projected,
            "before": before_maximum,
            "pre_repair": pre_repair_maximum,
            "native": native_maximum,
            "native_ratios": native_ratios,
            "after": projected_maximum,
            "ratios": ratios,
            "bounds_min": tuple(min(point[axis] for point in affected_points) for axis in range(3)),
            "bounds_max": tuple(max(point[axis] for point in affected_points) for axis in range(3)),
            "finger_vertices": finger_vertices,
        })

    torso_bones = {
        index for index, name in enumerate(names)
        if name in {"Hips", "Spine", "Spine01", "Spine02", "Neck", "Head"}
    }
    right_limb_bones = {
        index for index, name in enumerate(names)
        if name in {"RightShoulder", "RightArm", "RightForeArm", "RightHand"}
    }

    def smoothstep(value: float) -> float:
        value = max(0.0, min(1.0, value))
        return value * value * (3.0 - 2.0 * value)

    core_vertices = expanded_vertices(0)
    parent = list(range(len(points)))

    def find(vertex: int) -> int:
        while parent[vertex] != vertex:
            parent[vertex] = parent[parent[vertex]]
            vertex = parent[vertex]
        return vertex

    def union(first: int, second: int) -> None:
        first_root, second_root = find(first), find(second)
        if first_root != second_root:
            parent[second_root] = first_root

    for first, second, third in mesh_triangles:
        union(first, second)
        union(second, third)
    component_vertices: dict[int, list[int]] = defaultdict(list)
    for vertex in range(len(points)):
        component_vertices[find(vertex)].append(vertex)
    core_components = sorted(
        ({find(vertex) for vertex in core_vertices}),
        key=lambda component: min(component_vertices[component]),
    )
    corrective_results = []
    for z_start in (0.12, 0.16, 0.20):
        for torso_minimum in (0.05, 0.20):
            for strength in (0.50, 0.75, 1.00):
                corrected = list(native_weights)
                correction_vertices = set()
                for vertex in core_vertices:
                    point = points[vertex]
                    if not (-0.06 <= point[0] <= 0.28 and 0.88 <= point[1] <= 1.50 and point[2] >= z_start):
                        continue
                    vertex_weights = native_weights[vertex]
                    torso_total = sum(vertex_weights.get(bone, 0.0) for bone in torso_bones)
                    limb_total = sum(vertex_weights.get(bone, 0.0) for bone in right_limb_bones)
                    retained = {
                        bone: weight for bone, weight in vertex_weights.items()
                        if bone not in right_limb_bones
                    }
                    if torso_total <= torso_minimum or limb_total <= 0.01 or not retained:
                        continue
                    depth_fade = smoothstep((point[2] - z_start) / 0.08)
                    torso_fade = smoothstep((torso_total - torso_minimum) / 0.30)
                    lower_fade = smoothstep((point[1] - 0.88) / 0.08)
                    upper_fade = smoothstep((1.50 - point[1]) / 0.10)
                    side_fade = min(
                        smoothstep((point[0] + 0.06) / 0.08),
                        smoothstep((0.28 - point[0]) / 0.08),
                    )
                    ratio = strength * depth_fade * torso_fade * lower_fade * upper_fade * side_fade
                    if ratio <= 0.0001:
                        continue
                    repaired = compressed_weights(retained)
                    corrected[vertex] = mixed_weights(vertex_weights, repaired, ratio)
                    correction_vertices.add(vertex)
                projected, changed_rows, affected = project_weights(core_vertices, corrected)
                target_triangles = {
                    index for index, triangle in enumerate(mesh_triangles)
                    if any(vertex in correction_vertices for vertex in triangle)
                }
                target_vertices = {
                    vertex for index in target_triangles for vertex in mesh_triangles[index]
                }
                target_edges = set()
                for index in target_triangles:
                    first, second, third = mesh_triangles[index]
                    for edge in ((first, second), (second, third), (third, first)):
                        if edge[0] != edge[1]:
                            target_edges.add(tuple(sorted(edge)))
                target_maximum, target_ratios = smoothing_metric_across_poses(
                    points, projected, poses, sorted(target_edges), target_vertices
                )
                all_edges, all_vertices = local_edges(affected)
                all_maximum, _ = smoothing_metric_across_poses(
                    points, projected, poses, all_edges, all_vertices
                )
                corrective_results.append({
                    "z_start": z_start,
                    "torso_minimum": torso_minimum,
                    "strength": strength,
                    "corrected": corrected,
                    "correction_vertices": correction_vertices,
                    "changed_rows": changed_rows,
                    "affected": affected,
                    "projected": projected,
                    "target_maximum": target_maximum,
                    "all_maximum": all_maximum,
                    "target_ratios": target_ratios,
                })

    selected_corrective = min(
        corrective_results,
        key=lambda candidate: (
            candidate["target_maximum"], len(candidate["correction_vertices"]), candidate["strength"]
        ),
    )

    rigid_component_results = []
    for maximum_x in (0.20, 0.23):
        for maximum_y in (1.34, 1.38):
            for maximum_z_minimum in (0.16, 0.19):
                for torso_mean_minimum in (0.03, 0.10):
                    selected_components = []
                    corrected = list(native_weights)
                    correction_vertices = set()
                    for component in core_components:
                        vertices = component_vertices[component]
                        if len(vertices) > 60:
                            continue
                        component_points = [points[vertex] for vertex in vertices]
                        minimum = tuple(min(point[axis] for point in component_points) for axis in range(3))
                        maximum = tuple(max(point[axis] for point in component_points) for axis in range(3))
                        if not (
                            minimum[0] >= -0.08 and maximum[0] <= maximum_x
                            and minimum[1] >= 0.95 and maximum[1] <= maximum_y
                            and minimum[2] >= 0.05 and maximum[2] >= maximum_z_minimum
                            and maximum[2] <= 0.32
                        ):
                            continue
                        limb_mean = sum(
                            sum(native_weights[vertex].get(bone, 0.0) for bone in right_limb_bones)
                            for vertex in vertices
                        ) / len(vertices)
                        torso_mean = sum(
                            sum(native_weights[vertex].get(bone, 0.0) for bone in torso_bones)
                            for vertex in vertices
                        ) / len(vertices)
                        if limb_mean <= 0.03 or torso_mean <= torso_mean_minimum:
                            continue
                        torso_accumulator = defaultdict(float)
                        for vertex in vertices:
                            for bone in torso_bones:
                                torso_accumulator[bone] += native_weights[vertex].get(bone, 0.0)
                        uniform = compressed_weights(torso_accumulator)
                        for vertex in vertices:
                            corrected[vertex] = uniform
                            correction_vertices.add(vertex)
                        selected_components.append((component, len(vertices), limb_mean, torso_mean))

                    transfer_vertices = core_vertices | correction_vertices
                    projected, changed_rows, affected = project_weights(transfer_vertices, corrected)
                    component_edges = set()
                    for component, _, _, _ in selected_components:
                        component_set = set(component_vertices[component])
                        for first, second, third in mesh_triangles:
                            if first not in component_set or second not in component_set or third not in component_set:
                                continue
                            for edge in ((first, second), (second, third), (third, first)):
                                if edge[0] != edge[1]:
                                    component_edges.add(tuple(sorted(edge)))
                    component_maximum, component_ratios = smoothing_metric_across_poses(
                        points, projected, poses, sorted(component_edges), correction_vertices
                    )
                    all_edges, all_vertices = local_edges(affected)
                    all_maximum, _ = smoothing_metric_across_poses(
                        points, projected, poses, all_edges, all_vertices
                    )
                    rigid_component_results.append({
                        "maximum_x": maximum_x,
                        "maximum_y": maximum_y,
                        "maximum_z_minimum": maximum_z_minimum,
                        "torso_mean_minimum": torso_mean_minimum,
                        "selected_components": selected_components,
                        "corrected": corrected,
                        "correction_vertices": correction_vertices,
                        "transfer_vertices": transfer_vertices,
                        "changed_rows": changed_rows,
                        "affected": affected,
                        "projected": projected,
                        "component_maximum": component_maximum,
                        "all_maximum": all_maximum,
                        "component_ratios": component_ratios,
                    })

    eligible_rigid = [
        candidate for candidate in rigid_component_results
        if candidate["selected_components"] and candidate["component_maximum"] <= 1.5
    ]
    selected_rigid = max(
        eligible_rigid,
        key=lambda candidate: (
            len(candidate["correction_vertices"]),
            len(candidate["selected_components"]),
            -candidate["component_maximum"],
        ),
    ) if eligible_rigid else min(
        rigid_component_results, key=lambda candidate: candidate["component_maximum"]
    )

    eligible = [
        candidate for candidate in candidate_results
        if candidate["after"] <= 1.5 and not candidate["finger_vertices"]
    ]
    if not eligible:
        selected_candidate = min(candidate_results, key=lambda candidate: candidate["after"])
    else:
        selected_candidate = min(eligible, key=lambda candidate: candidate["rings"])

    pre_native_differences = [
        weight_l1(pre_repair_weights[vertex], native_weights[vertex])
        for vertex in range(len(points))
    ]
    report = [
        "Round-2 localized native-FBX right-back weight restore",
        f"mode={'APPLY' if apply else 'ANALYZE'}",
        f"currentSha256={digest_bytes(current_bytes)}",
        f"nativeSource={ROUND2_NATIVE_WEIGHTS_PATH.relative_to(ROOT).as_posix()}",
        f"nativeSourceSha256={digest_bytes(ROUND2_NATIVE_WEIGHTS_PATH.read_bytes())}",
        f"topologyVertices={len(points)}; coreTriangles={len(core_triangles)}",
        f"naturalPlaybackBonePoses={len(poses)}; predictionMaxError={maximum_prediction_error:.9f}",
        f"nativeTopologyPositionMaxError={maximum_native_position_error:.9f}; "
        f"nativeTopologyUvMaxError={maximum_native_uv_error:.9f}",
        "preRepairVsNativeL1Counts=" + ",".join(
            f">{threshold:.3f}:{sum(value > threshold for value in pre_native_differences)}"
            for threshold in (0.001, 0.01, 0.05, 0.10, 0.50)
        ),
    ]
    report.append(f"meshComponents={len(component_vertices)}; coreComponents={len(core_components)}")
    for component in core_components:
        vertices = component_vertices[component]
        core_count = sum(vertex in core_vertices for vertex in vertices)
        component_points = [points[vertex] for vertex in vertices]
        dominant = Counter(
            names[max(native_weights[vertex].items(), key=lambda item: item[1])[0]]
            for vertex in vertices
        )
        report.append(
            f"coreComponent={component}; vertices={len(vertices)}; coreVertices={core_count}; "
            "boundsMin=" + ",".join(
                f"{min(point[axis] for point in component_points):.6f}" for axis in range(3)
            ) + "; boundsMax=" + ",".join(
                f"{max(point[axis] for point in component_points):.6f}" for axis in range(3)
            ) + "; dominantBones=" + ",".join(
                f"{bone}:{count}" for bone, count in dominant.most_common(8)
            )
        )
    for candidate in candidate_results:
        report.append(
            f"candidateRing={candidate['rings']}; selectedVertices={len(candidate['selected'])}; "
            f"affectedVertices={len(candidate['affected'])}; changedRows={len(candidate['changed_rows'])}; "
            f"beforeMaxStretch={candidate['before']:.6f}; "
            f"preRepairMaxStretch={candidate['pre_repair']:.6f}; "
            f"fullNativeMaxStretch={candidate['native']:.6f}; "
            f"projectedMaxStretch={candidate['after']:.6f}; "
            f"fingerVertices={len(candidate['finger_vertices'])}; "
            "boundsMin=" + ",".join(f"{value:.6f}" for value in candidate["bounds_min"]) + "; "
            "boundsMax=" + ",".join(f"{value:.6f}" for value in candidate["bounds_max"])
        )
    report.append("correctiveCandidateDetails=")
    for candidate in corrective_results:
        report.append(
            f"zStart={candidate['z_start']:.2f}; torsoMinimum={candidate['torso_minimum']:.2f}; "
            f"strength={candidate['strength']:.2f}; "
            f"correctionVertices={len(candidate['correction_vertices'])}; "
            f"changedRows={len(candidate['changed_rows'])}; "
            f"targetMaxStretch={candidate['target_maximum']:.6f}; "
            f"allLocalMaxStretch={candidate['all_maximum']:.6f}"
        )
    report.append(
        f"selectedCorrective=zStart:{selected_corrective['z_start']:.2f},"
        f"torsoMinimum:{selected_corrective['torso_minimum']:.2f},"
        f"strength:{selected_corrective['strength']:.2f}; "
        f"correctionVertices={len(selected_corrective['correction_vertices'])}; "
        f"changedRows={len(selected_corrective['changed_rows'])}; "
        f"targetMaxStretch={selected_corrective['target_maximum']:.6f}; "
        f"allLocalMaxStretch={selected_corrective['all_maximum']:.6f}"
    )
    report.append("rigidComponentCandidateDetails=")
    for candidate in rigid_component_results:
        report.append(
            f"maximumX={candidate['maximum_x']:.2f}; maximumY={candidate['maximum_y']:.2f}; "
            f"maximumZMinimum={candidate['maximum_z_minimum']:.2f}; "
            f"torsoMeanMinimum={candidate['torso_mean_minimum']:.2f}; "
            f"components={len(candidate['selected_components'])}; "
            f"correctionVertices={len(candidate['correction_vertices'])}; "
            f"changedRows={len(candidate['changed_rows'])}; "
            f"componentMaxStretch={candidate['component_maximum']:.6f}; "
            f"allLocalMaxStretch={candidate['all_maximum']:.6f}; "
            "componentIds=" + ",".join(str(item[0]) for item in candidate["selected_components"])
        )
    report.append(
        f"selectedRigid=maximumX:{selected_rigid['maximum_x']:.2f},"
        f"maximumY:{selected_rigid['maximum_y']:.2f},"
        f"maximumZMinimum:{selected_rigid['maximum_z_minimum']:.2f},"
        f"torsoMeanMinimum:{selected_rigid['torso_mean_minimum']:.2f}; "
        f"components={len(selected_rigid['selected_components'])}; "
        f"correctionVertices={len(selected_rigid['correction_vertices'])}; "
        f"changedRows={len(selected_rigid['changed_rows'])}; "
        f"componentMaxStretch={selected_rigid['component_maximum']:.6f}; "
        f"allLocalMaxStretch={selected_rigid['all_maximum']:.6f}; "
        "componentIds=" + ",".join(str(item[0]) for item in selected_rigid["selected_components"])
    )
    reference_candidate = candidate_results[0]
    report.append("nativeWorstEdgeDetails=")
    for ratio, first, second in reference_candidate["native_ratios"][:20]:
        first_weights = ",".join(
            f"{names[bone]}:{weight:.6f}" for bone, weight in native_weights[first].items()
        )
        second_weights = ",".join(
            f"{names[bone]}:{weight:.6f}" for bone, weight in native_weights[second].items()
        )
        report.append(
            f"ratio={ratio:.6f}; edge={first}-{second}; rest={distance(points[first], points[second]):.9f}; "
            "firstPoint=" + ",".join(f"{value:.6f}" for value in points[first]) + "; "
            f"firstWeights={first_weights}; "
            "secondPoint=" + ",".join(f"{value:.6f}" for value in points[second]) + "; "
            f"secondWeights={second_weights}; "
            f"weightL1={weight_l1(native_weights[first], native_weights[second]):.6f}"
        )
    report.extend([
        f"selectedRing={selected_candidate['rings']}",
        f"selectedChangedRows={len(selected_candidate['changed_rows'])}",
        f"selectedAffectedVertices={len(selected_candidate['affected'])}",
        f"selectedPredictedMaxStretch={selected_candidate['after']:.6f}",
        "selectedWorstEdges=" + ",".join(
            f"{ratio:.6f}:{first}-{second}"
            for ratio, first, second in selected_candidate["ratios"][:12]
        ),
        f"fingerRowsChanged={bool(selected_candidate['finger_vertices'])}",
        "geometryChanged=False",
        "uvChanged=False",
        "topologyChanged=False",
        "animationChanged=False",
        "itemOrGripChanged=False",
        "originalFbxChanged=False",
    ])

    if apply:
        if selected_rigid["component_maximum"] > 1.5 or not selected_rigid["selected_components"]:
            raise RuntimeError(
                "No numerically safe disconnected right-back component candidate was found; analysis only"
            )
        changed_rows = selected_rigid["changed_rows"]
        row_proposals: dict[int, list[dict[int, float]]] = defaultdict(list)
        for vertex in selected_rigid["transfer_vertices"]:
            row_proposals[mapping[vertex]].append(selected_rigid["corrected"][vertex])
        for row in changed_rows:
            final_weights = average_weights(row_proposals[row])
            current["vertices"][row]["influences"] = [
                {"bone": names[bone], "weight": weight}
                for bone, weight in sorted(final_weights.items(), key=lambda item: item[1], reverse=True)
            ]

        affected = sorted(selected_rigid["affected"])
        affected_set = set(affected)
        correction_rows = {
            mapping[vertex] for vertex in selected_rigid["correction_vertices"]
        }
        correction_affected = sorted(
            vertex for vertex, row in enumerate(mapping) if row in correction_rows
        )
        for row, (before, after) in enumerate(zip(
            json.loads(current_bytes.decode("utf-8-sig"))["vertices"], current["vertices"]
        )):
            if before["position"] != after["position"] or before["uv"] != after["uv"]:
                raise RuntimeError(f"Geometry or UV changed at row {row}")
            if row not in changed_rows and before != after:
                raise RuntimeError(f"Unselected row changed at {row}")
        if any(
            point[1] < 0.80 or point[1] > 1.60
            for vertex, point in enumerate(points) if vertex in affected_set
        ):
            raise RuntimeError("Native transfer escaped the approved right-back vertical region")

        output_bytes = json.dumps(current, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        WEIGHTS_PATH.write_bytes(output_bytes)
        manifest = {
            "unityVertices": affected,
            "correctiveVertices": correction_affected,
            "changedRows": len(changed_rows),
            "selectedRing": 0,
            "predictedMaxStretch": selected_rigid["component_maximum"],
            "allLocalMaxStretch": selected_rigid["all_maximum"],
            "componentIds": [item[0] for item in selected_rigid["selected_components"]],
        }
        ROUND2_NATIVE_MANIFEST_PATH.write_text(
            json.dumps(manifest, separators=(",", ":")), encoding="utf-8"
        )
        report.extend([
            f"outputSha256={digest_bytes(output_bytes)}",
            f"manifest={ROUND2_NATIVE_MANIFEST_PATH.relative_to(ROOT).as_posix()}",
        ])

    ROUND2_NATIVE_REPORT_PATH.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(
        f"RIGHT_BACK_ROUND2_NATIVE_{'APPLIED' if apply else 'ANALYZED'} "
        f"ring=0 rows={len(selected_rigid['changed_rows'])} "
        f"vertices={len(selected_rigid['affected'])} "
        f"stretch={selected_rigid['component_maximum']:.6f} report={ROUND2_NATIVE_REPORT_PATH}"
    )


def digest_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def analyze_pose_layout() -> None:
    pose_folder = ROOT / "docs/validation/consumable_item_grips_2026-09-09/weight_pose_readout"
    output_path = (
        ROOT
        / "docs/validation/consumable_right_back_fix_2026-09-10/grip_item_bounds.txt"
    )
    names = (
        "Battery_Aux_Plug",
        "Converter_Shield_Plug",
        "Buffer_Shield_Plug",
        "Nanomachine_Inject",
    )
    report = ["Read-only current hand-space grip layout"]

    def bounds(points: list[dict]) -> tuple[list[float], list[float], list[float]]:
        minimum = [min(float(point[axis]) for point in points) for axis in ("x", "y", "z")]
        maximum = [max(float(point[axis]) for point in points) for axis in ("x", "y", "z")]
        center = [(minimum[index] + maximum[index]) * 0.5 for index in range(3)]
        return minimum, maximum, center

    for name in names:
        data = json.loads((pose_folder / f"{name}.json").read_text(encoding="utf-8-sig"))
        item_minimum, item_maximum, item_center = bounds(data["itemVertices"])
        hand_indices = []
        for index, weight in enumerate(data["weights"]):
            dominant = data["names"][int(weight["m_BoneIndex0"])]
            if dominant == "RightHand" or any(
                dominant.startswith(f"Right{digit}")
                for digit in ("Thumb", "Index", "Middle", "Ring", "Little")
            ):
                hand_indices.append(index)
        hand_points = [data["bakedHandPoints"][index] for index in hand_indices]
        hand_minimum, hand_maximum, hand_center = bounds(hand_points)
        report.extend(
            [
                f"target={name}",
                "itemMin=" + ",".join(f"{value:.6f}" for value in item_minimum),
                "itemMax=" + ",".join(f"{value:.6f}" for value in item_maximum),
                "itemCenter=" + ",".join(f"{value:.6f}" for value in item_center),
                "handMin=" + ",".join(f"{value:.6f}" for value in hand_minimum),
                "handMax=" + ",".join(f"{value:.6f}" for value in hand_maximum),
                "handCenter=" + ",".join(f"{value:.6f}" for value in hand_center),
            ]
        )
    output_path.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(f"GRIP_POSE_LAYOUT_ANALYZED report={output_path}")


def main() -> None:
    args = parse_args()
    if args.pose_layout:
        analyze_pose_layout()
        return
    if args.round2_smooth_analyze or args.round2_smooth_apply:
        smooth_round2_topology(args.round2_smooth_apply)
        return
    if args.round2_native_analyze or args.round2_native_apply:
        native_round2_restore(args.round2_native_apply)
        return
    source_bytes = WEIGHTS_PATH.read_bytes()
    data = json.loads(source_bytes.decode("utf-8-sig"))
    if args.round2_analyze:
        analyze_round2_candidates(data)
        return
    if args.round2_apply:
        apply_round2_repair(data, source_bytes)
        return
    original = copy.deepcopy(data)
    rows = data["vertices"]

    arm_rows = [row for row in rows if influence_map(row).get(ARM_BONE, 0.0) > 0.0]
    selected_positions = {position_key(row) for row in rows if is_lower_right_back_outlier(row)}
    selected_indices = [index for index, row in enumerate(rows) if position_key(row) in selected_positions]

    y_bins: dict[float, int] = defaultdict(int)
    for row in arm_rows:
        y = float(row["position"]["y"])
        y_bins[round(int(y / 0.05) * 0.05, 2)] += 1

    report = [
        "Right-back skin-weight analysis",
        f"mode={'APPLY' if args.apply else 'ANALYZE'}",
        f"source={WEIGHTS_PATH.relative_to(ROOT).as_posix()}",
        f"sourceSha256={digest_bytes(source_bytes)}",
        f"totalRows={len(rows)}",
        f"rightArmRows={len(arm_rows)}",
        f"selectedRows={len(selected_indices)}",
        f"selectedUniquePositions={len(selected_positions)}",
        "selection=x[0.05,0.20] y[1.00,1.20] z[0.10,0.30]; RightArm>=0.01; "
        "co-influenced by lower torso; excludes RightShoulder/RightForeArm/RightHand",
        "rightArmYBins=" + ",".join(f"{key:.2f}:{y_bins[key]}" for key in sorted(y_bins)),
        "selectedBoneSets="
        + ",".join(
            f"{'/'.join(key)}:{count}"
            for key, count in sorted(
                Counter(tuple(item["bone"] for item in rows[index]["influences"]) for index in selected_indices).items()
            )
        ),
        "",
    ]

    for index in selected_indices:
        row = rows[index]
        report.append(
            f"row={index} position={position_key(row)} "
            f"uv=({float(row['uv']['x']):.6f},{float(row['uv']['y']):.6f}) "
            f"before={format_influences(row)}"
        )
        if args.apply:
            row["influences"] = normalized_without_right_arm(row)
            report.append(f"row={index} after={format_influences(row)}")

    if not selected_indices:
        raise RuntimeError("No isolated lower right-back RightArm outliers matched the approved region.")

    for index, (before, after) in enumerate(zip(original["vertices"], rows)):
        if before["position"] != after["position"] or before["uv"] != after["uv"]:
            raise RuntimeError(f"Geometry or UV changed at row {index}")
        if index not in selected_indices and before != after:
            raise RuntimeError(f"Unselected row changed at {index}")
        total = sum(float(item["weight"]) for item in after["influences"])
        if len(after["influences"]) > 4 or abs(total - 1.0) > 0.0001:
            raise RuntimeError(f"Invalid normalized influences at row {index}: {total}")

    if args.apply:
        output_bytes = json.dumps(data, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        WEIGHTS_PATH.write_bytes(output_bytes)
        report.extend(
            [
                "",
                f"outputSha256={digest_bytes(output_bytes)}",
                "geometryChanged=False",
                "uvChanged=False",
                "topologyChanged=False",
                "fingerRowsChanged=False",
                "animationChanged=False",
            ]
        )

    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(
        f"RIGHT_BACK_WEIGHT_{'APPLIED' if args.apply else 'ANALYZED'} "
        f"rows={len(selected_indices)} positions={len(selected_positions)} report={REPORT_PATH}"
    )


if __name__ == "__main__":
    main()
