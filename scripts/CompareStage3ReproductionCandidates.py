from __future__ import annotations

import csv
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


REFERENCE_DIR = Path("artSample/stage3_rework_review")
SAMPLE_DIR = Path("artSample/stage3_reproduction_sample")
RENDER_DIR = SAMPLE_DIR / "renders"
COMPARE_DIR = SAMPLE_DIR / "comparison"

TARGETS = (
    ("01", "cockpit_helm_and_status", "조종실 조타 장치와 상태 화면"),
    ("02", "control_room_cctv_terminal", "통제실 단일 대형 CCTV 스크린"),
    ("03", "engine_room_power_terminal", "동력실 전력 단말"),
    ("04", "supply_room_storage_cabinet", "비품창고 보관장"),
    ("05", "cargo_hold_props_and_terminal", "운송창고 소품과 입출력 단말"),
    ("06", "armory_turret_grip_mount", "무기실 포탑 손잡이 마운트"),
    ("07", "first_person_equipment", "1인칭 장비와 양손 막대기"),
)

CANDIDATE_SUFFIXES = (
    (
        "reference_lock",
        "reference-lock",
        "원본 PNG를 직접 복제한 비교 파이프라인 기준선입니다. 모델링 완료가 아닙니다.",
    ),
    (
        "dcc_reference_lock",
        "dcc-reference-lock",
        "Blender에서 원본 PNG를 카메라 정합 평면으로 렌더한 DCC 기준선입니다. 모델링 완료가 아닙니다.",
    ),
    (
        "modeled_v001",
        "modeled-v001",
        "실제 Blender 모델 슬롯 렌더를 원본 보드 레이아웃으로 합성한 첫 반복 후보입니다.",
    ),
    (
        "modeled_v002_tonefit",
        "modeled-v002-tonefit",
        "modeled-v001에 원본 보드의 평균/표준편차 톤을 맞춘 분석용 후보입니다. 형태 모델링 완료가 아닙니다.",
    ),
    (
        "trace_scaffold_v001",
        "trace-scaffold-v001",
        "원본 보드를 의미 있는 슬롯 평면으로 쪼갠 Blender 카메라 매치 scaffold입니다. 최종 모델링 완료가 아닙니다.",
    ),
    (
        "camera_matched_model_v001",
        "camera-matched-model-v001",
        "trace scaffold 슬롯을 두께와 relief가 있는 Blender 모델로 바꾸고 원본 가시 면을 투영한 승인용 카메라 매치 후보입니다.",
    ),
)


def write_html(metrics: list[dict[str, object]]) -> None:
    rows: list[str] = []
    for metric in metrics:
        item_id = str(metric["item_id"])
        slug = str(metric["slug"])
        title = str(metric["title"])
        variant = str(metric["variant"])
        score = float(metric["similarity_percent"])
        candidate = Path(str(metric["candidate"])).name
        rows.append(
            f"""
      <article>
        <h2>{item_id}. {title}</h2>
        <p class="meta">{variant} / pixel similarity {score:.4f}% / 99% gate: {metric["passes_99_percent_gate"]}</p>
        <p class="note">{metric["note"]}</p>
        <div class="compare">
          <figure>
            <img src="../stage3_rework_review/{item_id}_{slug}_review.png" alt="{title} 원본">
            <figcaption>원본 artSample</figcaption>
          </figure>
          <figure>
            <img src="renders/{candidate}" alt="{title} 후보">
            <figcaption>{variant} 후보</figcaption>
          </figure>
        </div>
        <figure class="diff">
          <img src="comparison/{item_id}_{slug}_{variant}_side_by_side_diff.png" alt="{title} {variant} 차이 비교">
          <figcaption>좌: 원본, 중앙: 후보, 우: 차이 5배 강조</figcaption>
        </figure>
      </article>
            """
        )

    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Stage 3 Reproduction Candidate Report</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #0e100f;
      --panel: #171a18;
      --line: #343b36;
      --text: #ece8dc;
      --muted: #aaa79d;
      --ok: #83d59c;
      --warn: #e3bd69;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: "Malgun Gothic", "Apple SD Gothic Neo", Arial, sans-serif;
      line-height: 1.55;
    }}
    header {{
      padding: 26px 32px 18px;
      background: #121512;
      border-bottom: 1px solid var(--line);
    }}
    main {{
      width: min(1500px, calc(100vw - 40px));
      margin: 24px auto 48px;
      display: grid;
      gap: 24px;
    }}
    h1 {{ margin: 0 0 8px; font-size: 27px; letter-spacing: 0; }}
    h2 {{ margin: 0 0 8px; font-size: 19px; letter-spacing: 0; }}
    p {{ margin: 0; color: var(--muted); }}
    .notice {{
      margin-top: 12px;
      padding: 12px 14px;
      border: 1px solid #574a2c;
      background: #1a1710;
      color: #e8d8ac;
      border-radius: 6px;
      max-width: 1180px;
    }}
    article {{
      border: 1px solid var(--line);
      background: var(--panel);
      border-radius: 6px;
      padding: 14px;
    }}
    .meta {{
      color: var(--ok);
      font-weight: 700;
      margin-bottom: 6px;
    }}
    .note {{
      color: var(--warn);
      margin-bottom: 12px;
    }}
    .compare {{
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }}
    figure {{
      margin: 0;
      border: 1px solid #303732;
      background: #090a09;
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
    code {{
      color: #e8e2d4;
      background: #0b0d0c;
      border: 1px solid #2a302b;
      border-radius: 4px;
      padding: 1px 4px;
      font-family: Consolas, "Courier New", monospace;
    }}
    @media (max-width: 900px) {{
      header {{ padding: 22px 16px 16px; }}
      main {{ width: min(100vw - 24px, 1500px); }}
      .compare {{ grid-template-columns: 1fr; }}
    }}
  </style>
</head>
<body>
  <header>
    <h1>Stage 3 Reproduction Candidate Report</h1>
    <p><code>artSample/stage3_rework_review</code> 기준 99% 재현 게이트 후보 비교입니다.</p>
    <p class="notice">현재 통과 후보들은 reference-lock 계열입니다. 이는 비교와 카메라 정합 기준선이며, 실제 모델링 완료 판정이 아닙니다.</p>
  </header>
  <main>
    {''.join(rows)}
  </main>
</body>
</html>
"""
    (SAMPLE_DIR / "candidate_report.html").write_text(html, encoding="utf-8")


def pixel_similarity(reference: Image.Image, candidate: Image.Image) -> tuple[float, float]:
    if candidate.size != reference.size:
        candidate = candidate.resize(reference.size, Image.Resampling.LANCZOS)
    diff = ImageChops.difference(reference.convert("RGB"), candidate.convert("RGB"))
    stat = ImageStat.Stat(diff)
    mae = sum(stat.mean) / 3.0
    return max(0.0, 1.0 - mae / 255.0), mae


def write_comparison(
    item_id: str,
    slug: str,
    variant_id: str,
    reference: Image.Image,
    candidate: Image.Image,
) -> None:
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
    canvas.save(COMPARE_DIR / f"{item_id}_{slug}_{variant_id}_side_by_side_diff.png")


def main() -> None:
    COMPARE_DIR.mkdir(parents=True, exist_ok=True)
    metrics: list[dict[str, object]] = []

    for item_id, slug, title in TARGETS:
        reference_path = REFERENCE_DIR / f"{item_id}_{slug}_review.png"
        reference = Image.open(reference_path).convert("RGB")

        for suffix, variant_id, note in CANDIDATE_SUFFIXES:
            candidate_path = RENDER_DIR / f"{item_id}_{slug}_{suffix}.png"
            if not candidate_path.exists():
                continue

            candidate = Image.open(candidate_path).convert("RGB")
            similarity, mae = pixel_similarity(reference, candidate)
            write_comparison(item_id, slug, variant_id, reference, candidate)
            metrics.append(
                {
                    "item_id": item_id,
                    "slug": slug,
                    "title": title,
                    "variant": variant_id,
                    "reference": str(reference_path).replace("\\", "/"),
                    "candidate": str(candidate_path).replace("\\", "/"),
                    "width": reference.width,
                    "height": reference.height,
                    "pixel_similarity": round(similarity, 6),
                    "similarity_percent": round(similarity * 100.0, 4),
                    "mean_absolute_error": round(mae, 6),
                    "passes_99_percent_gate": similarity >= 0.99,
                    "note": note,
                }
            )

    with open(SAMPLE_DIR / "candidate_metrics.json", "w", encoding="utf-8") as handle:
        json.dump(metrics, handle, ensure_ascii=False, indent=2)

    with open(SAMPLE_DIR / "candidate_metrics.csv", "w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=metrics[0].keys())
        writer.writeheader()
        writer.writerows(metrics)

    write_html(metrics)

    for metric in metrics:
        print(
            f"{metric['item_id']} {metric['variant']}: "
            f"{metric['similarity_percent']:.4f}% "
            f"pass={metric['passes_99_percent_gate']}"
        )


if __name__ == "__main__":
    main()
