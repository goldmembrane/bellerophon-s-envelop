from __future__ import annotations

import csv
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


SAMPLE_ROOT = Path("artSample/stage3_hardsurface_reproduction_sample")
REFERENCE_ROOT = Path("artSample/stage3_rework_review")
RENDER_ROOT = SAMPLE_ROOT / "renders"
COMPARISON_ROOT = SAMPLE_ROOT / "comparison"
ITEM_ID = "01"
SLUG = "cockpit_helm_and_status"
TITLE = "조종실 조타 장치와 상태 화면"
VARIANT = "hardsurface-structure-v006"
FILE_SUFFIX = "hardsurface_structure_v006"
TITLE = "\uc870\uc885\uc2e4 \uc870\ud0c0 \uc7a5\uce58\uc640 \uc0c1\ud0dc \ud654\uba74"
STRUCTURAL_GATE_PERCENT = 95.0


def pixel_similarity(reference: Image.Image, candidate: Image.Image) -> tuple[float, float]:
    if candidate.size != reference.size:
        candidate = candidate.resize(reference.size, Image.Resampling.LANCZOS)
    diff = ImageChops.difference(reference.convert("RGB"), candidate.convert("RGB"))
    stat = ImageStat.Stat(diff)
    mae = sum(stat.mean) / 3.0
    return max(0.0, 1.0 - mae / 255.0), mae


def write_side_by_side(reference: Image.Image, candidate: Image.Image) -> Path:
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
    path = COMPARISON_ROOT / f"{ITEM_ID}_{SLUG}_{FILE_SUFFIX}_side_by_side_diff.png"
    canvas.save(path)
    return path


def write_html(score: float, mae: float) -> None:
    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Stage 3 Hard Surface Structure Sample</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #0b0d0c;
      --panel: #161a18;
      --line: #343b36;
      --text: #ece7d8;
      --muted: #aaa397;
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
    .bad {{ color: var(--bad); }}
    article {{
      border: 1px solid var(--line);
      background: var(--panel);
      border-radius: 6px;
      padding: 14px;
    }}
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
    img {{ display: block; width: 100%; height: auto; }}
    figcaption {{ color: var(--muted); font-size: 13px; padding-top: 7px; }}
    @media (max-width: 900px) {{
      body > header {{ padding: 22px 16px 16px; }}
      main {{ width: min(100vw - 24px, 1500px); }}
      .compare {{ grid-template-columns: 1fr; }}
    }}
  </style>
</head>
<body>
  <header>
    <h1>Stage 3 Hard Surface Structure Sample</h1>
    <p>현재 방향은 픽셀 단위 복제가 아니라 95% 구조 유사도입니다. 세부 마모와 작은 표면 디테일은 참고 요소입니다.</p>
    <p>이 페이지의 픽셀 유사도는 참고 진단값이며 승인 판정이 아닙니다.</p>
    <p>Blender 원본은 <code>blender/Stage3_HardSurfaceReproduction_01_structure_v006.blend</code>입니다.</p>
    <p class="notice"><span class="bad">구조 점수 84.0/100, 미통과</span>: {VARIANT} / 픽셀 참고값 {score:.4f}% / MAE {mae:.4f} / 구조 기준 {STRUCTURAL_GATE_PERCENT:.1f}% / 사용자 승인 전 Unity 적용 금지</p>
  </header>
  <main>
    <article>
      <h2>01. {TITLE}</h2>
      <div class="compare">
        <figure>
          <img src="../stage3_rework_review/{ITEM_ID}_{SLUG}_review.png" alt="{TITLE} 기준 이미지">
          <figcaption>기준 artSample</figcaption>
        </figure>
        <figure>
          <img src="renders/{ITEM_ID}_{SLUG}_{FILE_SUFFIX}.png" alt="{TITLE} 하드서피스 렌더">
          <figcaption>하드서피스 모델 렌더</figcaption>
        </figure>
      </div>
      <figure class="diff">
        <img src="comparison/{ITEM_ID}_{SLUG}_{FILE_SUFFIX}_side_by_side_diff.png" alt="{TITLE} 차이 비교">
        <figcaption>왼쪽: 기준, 가운데: 모델, 오른쪽: 차이 5배 강조. 이 이미지는 구조 승인 판단을 보조하는 참고 자료입니다.</figcaption>
      </figure>
    </article>
  </main>
</body>
</html>
"""
    (SAMPLE_ROOT / "index.html").write_text(html, encoding="utf-8")


def main() -> None:
    COMPARISON_ROOT.mkdir(parents=True, exist_ok=True)
    reference_path = REFERENCE_ROOT / f"{ITEM_ID}_{SLUG}_review.png"
    candidate_path = RENDER_ROOT / f"{ITEM_ID}_{SLUG}_{FILE_SUFFIX}.png"
    if not reference_path.exists():
        raise FileNotFoundError(reference_path)
    if not candidate_path.exists():
        raise FileNotFoundError(candidate_path)

    reference = Image.open(reference_path).convert("RGB")
    candidate = Image.open(candidate_path).convert("RGB")
    similarity, mae = pixel_similarity(reference, candidate)
    score = round(similarity * 100.0, 4)
    write_side_by_side(reference, candidate)

    metric = {
        "item_id": ITEM_ID,
        "slug": SLUG,
        "title": TITLE,
        "variant": VARIANT,
        "reference": str(reference_path),
        "candidate": str(candidate_path),
        "width": reference.width,
        "height": reference.height,
        "pixel_similarity": round(similarity, 6),
        "pixel_similarity_percent": score,
        "mean_absolute_error": round(mae, 6),
        "approval_gate": "structural_similarity_user_review",
        "pixel_metric_is_approval_gate": False,
    }
    with (SAMPLE_ROOT / "hardsurface_metrics.csv").open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(metric.keys()))
        writer.writeheader()
        writer.writerow(metric)

    status = {
        "variant": VARIANT,
        "scope": "01 cockpit only",
        "previous_pixel_similarity_percent": score,
        "previous_pixel_gate_passed": False,
        "current_approval_basis": "95% structural similarity, with fine details used as reference only",
        "structural_similarity_percent": 84.0,
        "structural_gate_percent": STRUCTURAL_GATE_PERCENT,
        "structural_gate_evaluated": True,
        "structural_score_document": "analysis/01_structural_score_v006.md",
        "structural_score_data": "analysis/01_structural_score_v006.json",
        "review_ready": True,
        "structural_review_document": "analysis/01_structural_review_v006.md",
        "passes_gate": False,
        "approval_ready": False,
        "user_approval_required_before_unity": True,
        "unity_application_allowed": False,
        "rule": "Approval now requires a completed Blender artSample that satisfies the 95% structural-similarity direction and receives explicit user approval before Unity application.",
    }
    (SAMPLE_ROOT / "approval_status.json").write_text(json.dumps(status, ensure_ascii=False, indent=2), encoding="utf-8")
    write_html(score, mae)
    print(f"Stage 3 hard-surface diagnostic comparison complete. Pixel similarity: {score:.4f}%")


if __name__ == "__main__":
    main()
