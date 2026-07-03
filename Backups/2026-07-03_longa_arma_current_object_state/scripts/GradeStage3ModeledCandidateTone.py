from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image


REFERENCE_DIR = Path("artSample/stage3_rework_review")
SAMPLE_DIR = Path("artSample/stage3_reproduction_sample")
RENDER_DIR = SAMPLE_DIR / "renders"

TARGETS = (
    ("01", "cockpit_helm_and_status"),
    ("02", "control_room_cctv_terminal"),
    ("03", "engine_room_power_terminal"),
    ("04", "supply_room_storage_cabinet"),
    ("05", "cargo_hold_props_and_terminal"),
    ("06", "armory_turret_grip_mount"),
    ("07", "first_person_equipment"),
)


def transfer_mean_std(reference: Image.Image, candidate: Image.Image) -> Image.Image:
    ref = np.asarray(reference.convert("RGB"), dtype=np.float32)
    cand = np.asarray(candidate.convert("RGB"), dtype=np.float32)

    ref_mean = ref.reshape(-1, 3).mean(axis=0)
    ref_std = ref.reshape(-1, 3).std(axis=0)
    cand_mean = cand.reshape(-1, 3).mean(axis=0)
    cand_std = cand.reshape(-1, 3).std(axis=0)

    normalized = (cand - cand_mean) / np.maximum(cand_std, 1.0)
    transferred = normalized * np.maximum(ref_std, 1.0) + ref_mean
    transferred = np.clip(transferred, 0, 255).astype(np.uint8)
    return Image.fromarray(transferred, mode="RGB")


def main() -> None:
    for item_id, slug in TARGETS:
        reference_path = REFERENCE_DIR / f"{item_id}_{slug}_review.png"
        candidate_path = RENDER_DIR / f"{item_id}_{slug}_modeled_v001.png"
        output_path = RENDER_DIR / f"{item_id}_{slug}_modeled_v002_tonefit.png"
        reference = Image.open(reference_path)
        candidate = Image.open(candidate_path)
        graded = transfer_mean_std(reference, candidate)
        graded.save(output_path)
        print(f"Wrote tone-fit modeled candidate {output_path.name}")


if __name__ == "__main__":
    main()
