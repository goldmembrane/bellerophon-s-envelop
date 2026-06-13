from __future__ import annotations

import csv
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


SAMPLE_ROOT = Path("artSample/stage3_true_model_approval_sample")
REFERENCE_ROOT = Path("artSample/stage3_rework_review")
RENDER_ROOT = SAMPLE_ROOT / "renders"
COMPARISON_ROOT = SAMPLE_ROOT / "comparison"
VARIANT = "true-model-v018"
FILE_SUFFIX = "true_model_v018"
GATE_PERCENT = 99.0

TARGETS = [
    ("01", "cockpit_helm_and_status", "조종실 조타 장치와 상태 화면"),
    ("02", "control_room_cctv_terminal", "통제실 CCTV 단말"),
    ("03", "engine_room_power_terminal", "동력실 전력 단말"),
    ("04", "supply_room_storage_cabinet", "비품창고 보관 캐비닛"),
    ("05", "cargo_hold_props_and_terminal", "화물창고 화물과 반출 단말"),
    ("06", "armory_turret_grip_mount", "무기실 수동 포탑 조작 마운트"),
    ("07", "first_person_equipment", "1인칭 장비와 막대기"),
]


def pixel_similarity(reference: Image.Image, candidate: Image.Image) -> tuple[float, float]:
    if candidate.size != reference.size:
        candidate = candidate.resize(reference.size, Image.Resampling.LANCZOS)
    diff = ImageChops.difference(reference.convert("RGB"), candidate.convert("RGB"))
    stat = ImageStat.Stat(diff)
    mae = sum(stat.mean) / 3.0
    return max(0.0, 1.0 - mae / 255.0), mae


def write_side_by_side(item_id: str, slug: str, reference: Image.Image, candidate: Image.Image) -> Path:
    if candidate.size != reference.size:
        candidate = candidate.resize(reference.size, Image.Resampling.LANCZOS)
    diff = ImageChops.difference(reference.convert("RGB"), candidate.convert("RGB"))
    boosted = diff.point(lambda value: min(255, value * 5))
    width, height = reference.size
    gutter = 12
    canvas = Image.new("RGB", (width * 3 + gutter * 2, height), (16, 18, 17))
    canvas.paste(reference.convert("RGB"), (0, 0))
    canvas.paste(candidate.convert("RGB"), (width + gutter, 0))
    canvas.paste(boosted.convert("RGB"), (width * 2 + gutter * 2, 0))
    path = COMPARISON_ROOT / f"{item_id}_{slug}_{FILE_SUFFIX}_side_by_side_diff.png"
    canvas.save(path)
    return path


def write_metrics(metrics: list[dict[str, object]]) -> None:
    csv_path = SAMPLE_ROOT / "true_model_metrics.csv"
    with csv_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(metrics[0].keys()))
        writer.writeheader()
        writer.writerows(metrics)

    min_score = min(float(metric["similarity_percent"]) for metric in metrics)
    status = {
        "variant": VARIANT,
        "gate_percent": GATE_PERCENT,
        "min_similarity_percent": round(min_score, 4),
        "passes_gate": min_score >= GATE_PERCENT,
        "approval_ready": min_score >= GATE_PERCENT,
        "rule": "Only real Blender modeling is eligible. Projection, trace scaffold, reference lock, and board-gallery outputs are excluded.",
    }
    (SAMPLE_ROOT / "approval_status.json").write_text(
        json.dumps(status, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def write_html(metrics: list[dict[str, object]]) -> None:
    min_score = min(float(metric["similarity_percent"]) for metric in metrics)
    passes = min_score >= GATE_PERCENT
    status_label = "승인 가능" if passes else "승인 불가"
    status_class = "ok" if passes else "bad"
    summary = f"최저 유사도는 {min_score:.4f}%입니다. {GATE_PERCENT:.1f}% 기준을 {'통과했습니다' if passes else '통과하지 못했습니다'}."

    rows = []
    for metric in metrics:
        item_id = str(metric["item_id"])
        slug = str(metric["slug"])
        title = str(metric["title"])
        score = float(metric["similarity_percent"])
        item_passes = score >= GATE_PERCENT
        rows.append(
            f"""
      <article>
        <header class="item-head">
          <h2>{item_id}. {title}</h2>
          <p class="score {'ok' if item_passes else 'bad'}">{VARIANT} / {score:.4f}% / 99% 기준: {item_passes}</p>
        </header>
        <div class="compare">
          <figure>
            <img src="../stage3_rework_review/{item_id}_{slug}_review.png" alt="{title} 기준 이미지">
            <figcaption>기준 artSample</figcaption>
          </figure>
          <figure>
            <img src="renders/{item_id}_{slug}_{FILE_SUFFIX}.png" alt="{title} 실제 Blender 모델 렌더">
            <figcaption>실제 Blender 모델 렌더</figcaption>
          </figure>
        </div>
        <figure class="diff">
          <img src="comparison/{item_id}_{slug}_{FILE_SUFFIX}_side_by_side_diff.png" alt="{title} 차이 비교">
          <figcaption>왼쪽: 기준, 가운데: 실제 모델, 오른쪽: 차이 5배 강조</figcaption>
        </figure>
      </article>
            """
        )

    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Stage 3 True Model Approval Sample</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #0b0d0c;
      --panel: #161a18;
      --line: #343b36;
      --text: #ece7d8;
      --muted: #aaa397;
      --ok: #88d79b;
      --bad: #e07b69;
      --warn-bg: #201410;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: "Malgun Gothic", "Apple SD Gothic Neo", Arial, sans-serif;
      line-height: 1.55;
    }}
    body > header {{
      padding: 26px 32px 20px;
      border-bottom: 1px solid var(--line);
      background: #111512;
    }}
    main {{
      width: min(1500px, calc(100vw - 40px));
      margin: 24px auto 48px;
      display: grid;
      gap: 24px;
    }}
    h1 {{ margin: 0 0 8px; font-size: 27px; letter-spacing: 0; }}
    h2 {{ margin: 0; font-size: 19px; letter-spacing: 0; }}
    p {{ margin: 0; color: var(--muted); }}
    code {{
      color: var(--text);
      background: #080a09;
      border: 1px solid #2a302c;
      border-radius: 4px;
      padding: 1px 4px;
      font-family: Consolas, "Courier New", monospace;
    }}
    .notice {{
      margin-top: 14px;
      padding: 13px 15px;
      border: 1px solid #57362e;
      background: var(--warn-bg);
      color: #efc4b7;
      border-radius: 6px;
      max-width: 1180px;
      font-weight: 700;
    }}
    .ok {{ color: var(--ok); }}
    .bad {{ color: var(--bad); }}
    article {{
      border: 1px solid var(--line);
      background: var(--panel);
      border-radius: 6px;
      padding: 14px;
    }}
    .item-head {{
      display: flex;
      gap: 12px;
      justify-content: space-between;
      align-items: baseline;
      margin-bottom: 12px;
    }}
    .score {{ font-weight: 700; }}
    .compare {{
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }}
    figure {{
      margin: 0;
      border: 1px solid #303732;
      background: #070807;
      padding: 7px;
    }}
    .diff {{ margin-top: 12px; }}
    img {{
      display: block;
      width: 100%;
      height: auto;
    }}
    figcaption {{
      color: var(--muted);
      font-size: 13px;
      padding-top: 7px;
    }}
    @media (max-width: 900px) {{
      body > header {{ padding: 22px 16px 16px; }}
      main {{ width: min(100vw - 24px, 1500px); }}
      .item-head {{ display: block; }}
      .score {{ margin-top: 6px; }}
      .compare {{ grid-template-columns: 1fr; }}
    }}
  </style>
</head>
<body>
  <header>
    <h1>Stage 3 True Model Approval Sample</h1>
    <p>Unity 적용 전 실제 Blender 모델링 승인 후보를 기준 artSample과 비교합니다.</p>
    <p>원본 PNG projection, trace scaffold, reference-lock, board gallery는 승인 후보에서 제외합니다.</p>
    <p>Blender 원본은 <code>blender/Stage3_TrueModelApproval_v018.blend</code>이고, 내보내기 파일은 <code>exports/</code>에 있습니다.</p>
    <p class="notice"><span class="{status_class}">{status_label}</span>: {summary}</p>
  </header>
  <main>
    {''.join(rows)}
  </main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def main() -> None:
    COMPARISON_ROOT.mkdir(parents=True, exist_ok=True)
    metrics: list[dict[str, object]] = []
    for item_id, slug, title in TARGETS:
        reference_path = REFERENCE_ROOT / f"{item_id}_{slug}_review.png"
        candidate_path = RENDER_ROOT / f"{item_id}_{slug}_{FILE_SUFFIX}.png"
        if not reference_path.exists():
            raise FileNotFoundError(reference_path)
        if not candidate_path.exists():
            raise FileNotFoundError(candidate_path)

        reference = Image.open(reference_path).convert("RGB")
        candidate = Image.open(candidate_path).convert("RGB")
        similarity, mae = pixel_similarity(reference, candidate)
        write_side_by_side(item_id, slug, reference, candidate)
        score = round(similarity * 100.0, 4)
        metrics.append(
            {
                "item_id": item_id,
                "slug": slug,
                "title": title,
                "variant": VARIANT,
                "reference": str(reference_path),
                "candidate": str(candidate_path),
                "width": reference.width,
                "height": reference.height,
                "pixel_similarity": round(similarity, 6),
                "similarity_percent": score,
                "mean_absolute_error": round(mae, 6),
                "passes_99_percent_gate": score >= GATE_PERCENT,
            }
        )

    write_metrics(metrics)
    write_html(metrics)
    min_score = min(float(metric["similarity_percent"]) for metric in metrics)
    print(f"Stage 3 true model comparison complete. Min similarity: {min_score:.4f}%")


if __name__ == "__main__":
    main()
