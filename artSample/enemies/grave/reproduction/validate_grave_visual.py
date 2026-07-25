from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parent
REFERENCE_PATH = ROOT / "source" / "grave_reference.png"
PREVIEW_PATH = ROOT / "review" / "work_preview.png"
REPORT_PATH = ROOT / "review" / "visual_validation.txt"
FOREGROUND_THRESHOLD = 205


def load_gray(path):
    return np.asarray(Image.open(path).convert("L"), dtype=np.uint8)


def bbox(mask):
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        raise RuntimeError("foreground mask is empty")
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def bbox_metrics(box):
    x0, y0, x1, y1 = box
    return x1 - x0 + 1, y1 - y0 + 1, (x0 + x1) / 2.0, (y0 + y1) / 2.0


reference = load_gray(REFERENCE_PATH)
preview = load_gray(PREVIEW_PATH)
if reference.shape != preview.shape:
    raise RuntimeError(f"image shape mismatch: {reference.shape} != {preview.shape}")

reference_mask = reference < FOREGROUND_THRESHOLD
preview_mask = preview < FOREGROUND_THRESHOLD
reference_box = bbox(reference_mask)
preview_box = bbox(preview_mask)
reference_width, reference_height, reference_cx, reference_cy = bbox_metrics(reference_box)
preview_width, preview_height, preview_cx, preview_cy = bbox_metrics(preview_box)

width_error = abs(preview_width - reference_width) / reference_width
height_error = abs(preview_height - reference_height) / reference_height
center_error = ((preview_cx - reference_cx) ** 2 + (preview_cy - reference_cy) ** 2) ** 0.5
intersection = int(np.logical_and(reference_mask, preview_mask).sum())
union = int(np.logical_or(reference_mask, preview_mask).sum())
iou = intersection / union
reference_median = float(np.median(reference[reference_mask]))
preview_median = float(np.median(preview[preview_mask]))
median_difference = abs(preview_median - reference_median)

checks = {
    "width_error<=0.03": width_error <= 0.03,
    "height_error<=0.03": height_error <= 0.03,
    "center_error<=12px": center_error <= 12.0,
    "silhouette_iou>=0.75": iou >= 0.75,
    "foreground_median_difference<=20": median_difference <= 20.0,
}
status = "통과" if all(checks.values()) else "실패"
lines = [
    f"Grave 정면 시각 일치 검증: {status}",
    f"기준 바운딩 박스: {reference_box}, {reference_width} x {reference_height}px, 중심 ({reference_cx:.1f}, {reference_cy:.1f})",
    f"샘플 바운딩 박스: {preview_box}, {preview_width} x {preview_height}px, 중심 ({preview_cx:.1f}, {preview_cy:.1f})",
    f"폭 오차: {width_error * 100:.3f}%",
    f"높이 오차: {height_error * 100:.3f}%",
    f"중심 오차: {center_error:.3f}px",
    f"실루엣 IoU: {iou:.6f}",
    f"전경 명도 중앙값: 기준 {reference_median:.1f}, 샘플 {preview_median:.1f}, 차이 {median_difference:.1f}",
    f"판정: {checks}",
]
REPORT_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"GRAVE_VISUAL_VALIDATION={'PASS' if all(checks.values()) else 'FAIL'}")
print(f"GRAVE_VISUAL_IOU={iou:.6f}")
print(f"GRAVE_VISUAL_BBOX_ERROR={width_error * 100:.3f}%,{height_error * 100:.3f}%")
print(f"GRAVE_VISUAL_CENTER_ERROR={center_error:.3f}px")
print(f"GRAVE_VISUAL_MEDIAN_DIFFERENCE={median_difference:.1f}")
print(f"GRAVE_VISUAL_REPORT={REPORT_PATH}")
if not all(checks.values()):
    raise SystemExit(1)
