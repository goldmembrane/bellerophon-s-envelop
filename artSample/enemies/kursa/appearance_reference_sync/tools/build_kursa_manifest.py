from pathlib import Path
import hashlib
import json


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
OUTPUT = SAMPLE / "ASSET_MANIFEST.json"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def main():
    files = {}
    for path in sorted(SAMPLE.rglob("*")):
        if not path.is_file() or path == OUTPUT or "__pycache__" in path.parts:
            continue
        relative = path.relative_to(SAMPLE).as_posix()
        files[relative] = {"bytes": path.stat().st_size, "sha256": sha256(path)}
    payload = {
        "status": "APPROVED_FOR_UNITY_NOT_APPLIED",
        "source_model": "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx",
        "source_model_sha256": sha256(ROOT / "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx"),
        "reference_image": "image/KUŠkursa(쿠르사).png",
        "reference_image_sha256": sha256(ROOT / "image/KUŠkursa(쿠르사).png"),
        "files": files,
    }
    OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Manifested {len(files)} files")


if __name__ == "__main__":
    main()
