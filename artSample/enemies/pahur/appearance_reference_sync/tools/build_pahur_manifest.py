from pathlib import Path
import hashlib
import json


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
OUTPUT = SAMPLE_ROOT / "ASSET_MANIFEST.json"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def files_under(relative_directory, suffix=None):
    directory = SAMPLE_ROOT / relative_directory
    return [
        str(path.relative_to(SAMPLE_ROOT)).replace("\\", "/")
        for path in sorted(directory.rglob("*"))
        if path.is_file() and (suffix is None or path.suffix.lower() == suffix)
    ]


def main():
    exports = files_under("exports")
    blender = files_under("blender", ".blend")
    final_renders = [
        f"renders/{name}"
        for name in (
            "01_front_pahur_reference_match.png",
            "02_three_quarter_pahur_reference_match.png",
            "03_reference_side_by_side_overview.png",
            "04_side_current_model_material.png",
            "05_rear_current_model_material.png",
            "06_texture_atlas_and_material_breakdown.png",
        )
    ]
    analysis_renders = [
        path
        for path in files_under("renders", ".png")
        if path not in final_renders
    ]
    textures = files_under("textures", ".png")
    tools = files_under("tools", ".py")
    analysis_reports = ["EYE_SURFACE_ANALYSIS.json"]
    tracked = exports + blender + final_renders + textures + analysis_reports
    report = {
        "enemy_id": "pahur",
        "sample_root": "artSample/enemies/pahur/appearance_reference_sync",
        "review_entry": "index.html",
        "summary_entry": "summary.html",
        "source_model": "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx",
        "reference_images": ["image/pāḫḫur(파후르).png"],
        "status": "PENDING_USER_REVIEW",
        "approved_for_unity": False,
        "unity_runtime_applied": False,
        "blender": blender,
        "exports": exports,
        "renders": final_renders,
        "analysis_renders": analysis_renders,
        "textures": textures,
        "tools": tools,
        "analysis_reports": analysis_reports,
        "integrity": {
            path: {
                "bytes": (SAMPLE_ROOT / path).stat().st_size,
                "sha256": sha256(SAMPLE_ROOT / path),
            }
            for path in tracked
        },
    }
    OUTPUT.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        json.dumps(
            {
                "exports": len(exports),
                "blender": len(blender),
                "renders": len(final_renders),
                "analysis_renders": len(analysis_renders),
                "textures": len(textures),
                "tools": len(tools),
                "analysis_reports": len(analysis_reports),
            }
        )
    )


if __name__ == "__main__":
    main()
