from __future__ import annotations

import csv
import json
import shutil
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageChops, ImageStat


REFERENCE_DIR = Path("artSample/stage3_rework_review")
OUTPUT_DIR = Path("artSample/stage3_reproduction_sample")
RENDER_DIR = OUTPUT_DIR / "renders"
COMPARE_DIR = OUTPUT_DIR / "comparison"
ANALYSIS_DIR = OUTPUT_DIR / "analysis"


@dataclass(frozen=True)
class Stage3Target:
    item_id: str
    slug: str
    title: str
    breakdown: tuple[str, ...]
    unity_translation: tuple[str, ...]

    @property
    def reference_name(self) -> str:
        return f"{self.item_id}_{self.slug}_review.png"

    @property
    def lock_name(self) -> str:
        return f"{self.item_id}_{self.slug}_reference_lock.png"


TARGETS = (
    Stage3Target(
        "01",
        "cockpit_helm_and_status",
        "조종실 조타 장치와 상태 화면",
        (
            "좌측 상단: 각진 금속 콘솔 박스, 상단 경사 CRT 화면, 전면 하부 손잡이, 측면 환기구와 볼트.",
            "좌측 중단: 원형이 완전히 닫히지 않은 세그먼트 조타 링, 좌우 고무 그립, 붉은 상단 버튼.",
            "우측 적용 컷: 전면 유리 너머 어두운 화물선 내부, 중앙 조타 기둥, 좌우 녹색 상태 화면.",
            "하단: 큰 상태 화면 1개와 작은 상태 화면 4개, 모두 두꺼운 금속 프레임과 녹색 낡은 유리.",
            "재질: 칩 난 검은 금속, 밝게 닳은 모서리, 어두운 고무, 얕은 녹, 스크래치 난 CRT 유리.",
        ),
        (
            "CargoRunMvp 조종실 조종대/상태 화면 루트.",
            "기존 조종대 상호작용 앵커와 1인칭 접근 시야를 유지.",
            "보드의 좌측 부품 컷은 리뷰용이며, 런타임에는 우측 적용 컷 구도를 기준으로 배치.",
        ),
    ),
    Stage3Target(
        "02",
        "control_room_cctv_terminal",
        "통제실 단일 대형 CCTV 스크린",
        (
            "좌측 상단: 가로로 긴 대형 CCTV 프레임과 두꺼운 모서리 볼트.",
            "좌측 중단: 대형 스크린의 보조 가로 화면, 내부에는 간단한 선형 맵 표시.",
            "좌측 중하단: 우측 세로형 구역 스크린, 반복되는 밝은 녹색 슬롯.",
            "좌측 하단: 기능 버튼, A/D 전환 버튼, 상단 케이블 레일 클로즈업.",
            "우측 적용 컷: 한 장의 대형 메인 CCTV, 왼쪽 위 보조 화면, 오른쪽 세로 화면, 하단 버튼 패널.",
            "재질: 어두운 벽 패널, 관통 케이블, 벗겨진 프레임 모서리, 녹색 CCTV 노이즈.",
        ),
        (
            "ControlRoom CCTV 상호작용 루트.",
            "대형 메인 화면 1개, 상단 좌측 보조 가로 화면 1개, 우측 세로 화면 1개 구조를 유지.",
            "여러 모니터를 나란히 둔 CCTV 뱅크로 재해석하지 않음.",
        ),
    ),
    Stage3Target(
        "03",
        "engine_room_power_terminal",
        "동력실 전력 단말",
        (
            "좌측 상단: 벽걸이 전력 캐비닛, 경사 없는 직사각형 본체, 상단 녹색 CRT.",
            "중앙: 붉은 사선 경고 스트립과 좌우 차단기 두 개.",
            "좌측 하단: 두 종류의 금속 파이프 클로즈업.",
            "우측 적용 컷: 엔진 코어와 난간이 보이는 벽면에 단말이 설치됨.",
            "재질: 거친 회색 금속, 붉은 경고 페인트, 검은 케이블, 열에 그을린 주변 벽.",
        ),
        (
            "EngineRoom 전력/오버클럭 단말 루트.",
            "기존 동력실 오버클럭 상호작용 위치를 가리지 않는 벽걸이 단말로 배치.",
            "파이프와 하단 케이블은 장식이 아니라 동력실 정체성을 드러내는 부품.",
        ),
    ),
    Stage3Target(
        "04",
        "supply_room_storage_cabinet",
        "비품창고 보관장",
        (
            "좌측 상단: 금속 문 뒤판과 단일 문 클로즈업, 노란 낡은 가로 도색.",
            "좌측 중단: 굵은 검은 손잡이와 힌지 클로즈업.",
            "좌측 하단: 2x3 금속 캐비닛 묶음.",
            "우측 적용 컷: 선반과 상자가 있는 비품실 벽면에 2x3 캐비닛 설치.",
            "재질: 올리브 회색 칠, 넓게 닳은 노란 도색, 손잡이 고무/금속, 낡은 보관 상자.",
        ),
        (
            "SupplyRoom storage cabinet 루트.",
            "비품창고 3칸 보관 루프를 시각적으로 뒷받침하되 실제 슬롯 수와 UI 로직은 변경하지 않음.",
            "런타임 배치에서는 우측 적용 컷의 벽면 캐비닛 밀도와 선반 배경을 기준으로 함.",
        ),
    ),
    Stage3Target(
        "05",
        "cargo_hold_props_and_terminal",
        "운송창고 소품과 입출력 단말",
        (
            "좌측 상단: 화물 상태 패널, 중앙 녹색 화면, 우측 빨강/초록 표시등, 좌우 케이블.",
            "좌측 중단: 큰 의뢰 화물 컨테이너, 검은 고정 스트랩, 붉은 잠금 태그.",
            "좌측 하단: 작은 개인 화물 컨테이너, 색상 라벨 판, 경고 라벨.",
            "우측 적용 컷: 화물칸 벽면 패널, 큰 컨테이너, 작은 컨테이너, 오른쪽 다이제틱 단말.",
            "하단 우측: 입출력 단말의 정면/측면/후면 변형.",
            "재질: 청회색 금속 화물, 검은 고무 스트랩, 붉고 노란 낡은 라벨, 바닥 경고선.",
        ),
        (
            "CargoHold status panel, contract cargo, personal cargo, diegetic terminal 루트.",
            "화물은 직접 집는 물건이 아니라 운송 대상과 상태 오브젝트로 보이게 유지.",
            "기존 cargo/interactable anchors는 유지하고, 샘플용 여분 클로즈업 배치는 런타임에 넣지 않음.",
        ),
    ),
    Stage3Target(
        "06",
        "armory_turret_grip_mount",
        "무기실 포탑 손잡이 마운트",
        (
            "좌측 상단: 긴 수평 레일과 중앙 회전축 블록.",
            "좌측 중단: 축 마운트 베이스, 좌우 손잡이 클로즈업.",
            "좌측 하단: 조준 하우징과 붉은 방아쇠 바.",
            "우측 적용 컷: 벽면/창 프레임 안쪽의 선박 포탑 조작 장치, 양손 그립, 우측 조작 패널.",
            "재질: 검은 낡은 금속, 거친 고무 그립, 붉은 방아쇠 바, 벽면 오염과 파이프.",
        ),
        (
            "Armory manual turret interaction root.",
            "손에 드는 총이 아니라 선박 포탑 조작 장치로 보이게 함.",
            "상호작용 프롬프트와 포탑 모드 진입 동선은 기존 기능을 유지.",
        ),
    ),
    Stage3Target(
        "07",
        "first_person_equipment",
        "1인칭 장비와 양손 막대기",
        (
            "좌측: 긴 수직 막대 전체 실루엣, 하단 손잡이 감김, 상단 갈고리 끝.",
            "중앙 좌측: 갈고리 클로즈업, 하단 그립 클로즈업, 머스켓 참고 형상, 손목 표시 장치.",
            "우측 적용 컷: 어두운 복도에서 두 손으로 긴 막대기를 잡은 1인칭 구도.",
            "막대기는 짧은 한손 빠루가 아니라 긴 양손 무기이며 끝단만 빠루형 갈고리.",
            "재질: 긁힌 어두운 금속, 닳은 손잡이 감김, 검은 장갑, 녹색 손목 CRT.",
        ),
        (
            "First-person equipment view and default stick model.",
            "HUD/map 시야를 과하게 막지 않는 오른쪽 전방 배치.",
            "양손 그립 간격과 위에서 아래로 내려찍을 수 있는 자세가 보이게 함.",
        ),
    ),
)


def ensure_dirs() -> None:
    for directory in (OUTPUT_DIR, RENDER_DIR, COMPARE_DIR, ANALYSIS_DIR):
        directory.mkdir(parents=True, exist_ok=True)


def pixel_similarity(reference: Image.Image, candidate: Image.Image) -> tuple[float, float]:
    if candidate.size != reference.size:
        candidate = candidate.resize(reference.size, Image.Resampling.LANCZOS)
    diff = ImageChops.difference(reference.convert("RGB"), candidate.convert("RGB"))
    stat = ImageStat.Stat(diff)
    mae = sum(stat.mean) / 3.0
    return max(0.0, 1.0 - mae / 255.0), mae


def write_comparison_image(target: Stage3Target, reference: Image.Image, candidate: Image.Image) -> None:
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
    canvas.save(COMPARE_DIR / f"{target.item_id}_{target.slug}_side_by_side_diff.png")


def write_reference_lock_outputs() -> list[dict[str, object]]:
    metrics: list[dict[str, object]] = []
    for target in TARGETS:
        reference_path = REFERENCE_DIR / target.reference_name
        if not reference_path.exists():
            raise FileNotFoundError(reference_path)

        lock_path = RENDER_DIR / target.lock_name
        shutil.copyfile(reference_path, lock_path)

        reference = Image.open(reference_path)
        candidate = Image.open(lock_path)
        similarity, mae = pixel_similarity(reference, candidate)
        write_comparison_image(target, reference, candidate)

        metrics.append(
            {
                "item_id": target.item_id,
                "slug": target.slug,
                "title": target.title,
                "reference": str(reference_path).replace("\\", "/"),
                "candidate": str(lock_path).replace("\\", "/"),
                "width": reference.width,
                "height": reference.height,
                "pixel_similarity": round(similarity, 6),
                "mean_absolute_error": round(mae, 6),
                "passes_99_percent_gate": similarity >= 0.99,
                "note": "Reference-lock baseline. This proves the comparison path, not modeled-asset completion.",
            }
        )
    return metrics


def write_metrics(metrics: list[dict[str, object]]) -> None:
    with open(OUTPUT_DIR / "metrics.json", "w", encoding="utf-8") as handle:
        json.dump(metrics, handle, ensure_ascii=False, indent=2)

    with open(OUTPUT_DIR / "metrics.csv", "w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=metrics[0].keys())
        writer.writeheader()
        writer.writerows(metrics)


def write_breakdown() -> None:
    lines: list[str] = [
        "# Stage 3 Reproduction Breakdown",
        "",
        "이 문서는 `artSample/stage3_rework_review` 7장 이미지를 분위기 참고가 아니라 재현 목표로 분해한 작업 메모입니다.",
        "",
        "## 공통 재현 기준",
        "",
        "- 출력 해상도는 원본과 같은 `1536x1024`입니다.",
        "- 최종 후보 렌더는 보드 전체 기준 픽셀 유사도 99% 이상을 목표로 합니다.",
        "- 기준 PNG를 그대로 후보로 쓰는 것은 모델링 완료가 아닙니다. 현재 `reference_lock` 출력은 비교 파이프라인을 고정하기 위한 기준선입니다.",
        "- 실제 모델링 후보는 Blender/DCC 렌더로 생성하고, 이 기준선과 같은 비교 리포트를 통과해야 합니다.",
        "- Unity 런타임 연결은 사용자가 새 샘플을 승인한 뒤에만 진행합니다.",
        "",
    ]

    for target in TARGETS:
        lines.extend(
            [
                f"## {target.item_id}. {target.title}",
                "",
                "### 실루엣, 비율, 주요 형태",
                "",
            ]
        )
        lines.extend(f"- {item}" for item in target.breakdown)
        lines.extend(["", "### Unity 반영 전제", ""])
        lines.extend(f"- {item}" for item in target.unity_translation)
        lines.append("")

    (ANALYSIS_DIR / "reference_breakdown.md").write_text("\n".join(lines), encoding="utf-8")


def write_readme(metrics: list[dict[str, object]]) -> None:
    passed = sum(1 for item in metrics if item["passes_99_percent_gate"])
    readme = f"""# Stage 3 Reproduction Sample

이 폴더는 2026-06-12 이후 새 아트 검증 규칙에 맞춰 `artSample/stage3_rework_review`를 99% 이상 재현하기 위한 새 작업 경로입니다.

현재 상태:

- `renders/*_reference_lock.png`: 원본 PNG를 후보 비교 기준선으로 고정한 출력입니다.
- `comparison/*_side_by_side_diff.png`: 원본, 기준선, 차이 이미지를 나란히 둔 검증 이미지입니다.
- `metrics.json`, `metrics.csv`: 99% 유사도 게이트 산출값입니다.
- `candidate_metrics.json`, `candidate_metrics.csv`, `candidate_report.html`: exact 기준선과 Blender DCC 기준선을 함께 비교한 후보 리포트입니다.
- `blender/Stage3_Reproduction_ReferenceLock.blend`: 원본 PNG를 Blender 카메라 정합 평면으로 고정한 DCC 기준선입니다.
- `analysis/reference_breakdown.md`: 각 이미지의 실루엣, 비율, 주요 형태, 재질, 카메라 구도, Unity 반영 전제 분해입니다.

주의:

- 현재 reference-lock 기준선은 {passed}/7개가 99% 게이트를 통과하지만, 이것은 모델링 완료가 아닙니다.
- 기준 PNG를 그대로 복사한 출력은 비교 파이프라인 검증용입니다.
- Blender DCC reference-lock도 원본 이미지를 카메라 정합한 기준선일 뿐, 실제 3D 부품 모델링 완료가 아닙니다.
- 다음 단계는 Blender 모델 렌더를 같은 보드 레이아웃으로 생성하고, 이 기준선과 같은 side-by-side 비교에서 99% 이상으로 올리는 것입니다.
- 사용자 승인 전에는 Unity 씬, 프리팹, 런타임 자산, UI 흐름에 연결하지 않습니다.
"""
    (OUTPUT_DIR / "README.md").write_text(readme, encoding="utf-8")


def write_html(metrics: list[dict[str, object]]) -> None:
    rows: list[str] = []
    metrics_by_id = {str(item["item_id"]): item for item in metrics}
    for target in TARGETS:
        metric = metrics_by_id[target.item_id]
        score = float(metric["pixel_similarity"]) * 100.0
        rows.append(
            f"""
      <article>
        <h2>{target.item_id}. {target.title}</h2>
        <p class="score">reference-lock 유사도: {score:.3f}%</p>
        <div class="compare">
          <figure>
            <img src="../stage3_rework_review/{target.reference_name}" alt="{target.title} 원본">
            <figcaption>원본 artSample</figcaption>
          </figure>
          <figure>
            <img src="renders/{target.lock_name}" alt="{target.title} reference lock">
            <figcaption>reference-lock 기준선</figcaption>
          </figure>
        </div>
        <figure class="diff">
          <img src="comparison/{target.item_id}_{target.slug}_side_by_side_diff.png" alt="{target.title} 비교와 차이">
          <figcaption>좌: 원본, 중앙: 기준선, 우: 차이 5배 강조</figcaption>
        </figure>
      </article>
            """
        )

    html = f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Stage 3 Reproduction Sample</title>
  <style>
    :root {{
      color-scheme: dark;
      --bg: #0f1110;
      --panel: #171a18;
      --line: #363d38;
      --text: #ebe7dc;
      --muted: #aaa79d;
      --ok: #81d29a;
      --warn: #e1b45c;
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
      border-bottom: 1px solid var(--line);
      background: #121512;
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
    .score {{
      color: var(--ok);
      font-weight: 700;
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
    .diff {{
      margin-top: 12px;
    }}
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
    <h1>Stage 3 Reproduction Sample</h1>
    <p><code>artSample/stage3_rework_review</code>의 7개 PNG를 99% 이상 재현하기 위한 새 작업 기준선입니다.</p>
    <p class="notice">현재 화면은 reference-lock 기준선입니다. 모델링 완료가 아니라, 이후 Blender/DCC 렌더가 통과해야 할 비교 게이트와 분해 기준을 고정한 결과입니다.</p>
  </header>
  <main>
    {''.join(rows)}
  </main>
</body>
</html>
"""
    (OUTPUT_DIR / "index.html").write_text(html, encoding="utf-8")


def main() -> None:
    ensure_dirs()
    metrics = write_reference_lock_outputs()
    write_metrics(metrics)
    write_breakdown()
    write_readme(metrics)
    write_html(metrics)
    print(f"Stage 3 reproduction baseline generated at {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
