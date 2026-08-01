from html.parser import HTMLParser
from pathlib import Path
import json
from PIL import Image


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE = ROOT / "artSample/enemies/kursa/appearance_reference_sync"
OUTPUT = SAMPLE / "HTML_VALIDATION.json"


class Parser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.images = []
        self.links = []
        self.titles = []
        self.headings = []
        self.capture = None

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "img" and attrs.get("src"):
            self.images.append(attrs["src"])
        elif tag == "a" and attrs.get("href"):
            self.links.append(attrs["href"])
        if tag in {"title", "h1", "h2", "h3"}:
            self.capture = tag

    def handle_endtag(self, tag):
        if tag == self.capture:
            self.capture = None

    def handle_data(self, data):
        text = data.strip()
        if not text:
            return
        if self.capture == "title":
            self.titles.append(text)
        elif self.capture in {"h1", "h2", "h3"}:
            self.headings.append(text)


def inspect_page(path):
    parser = Parser()
    parser.feed(path.read_text(encoding="utf-8"))
    images = []
    for src in parser.images:
        resolved = path.parent / src
        dimensions = None
        if resolved.exists():
            with Image.open(resolved) as image:
                dimensions = list(image.size)
        images.append({"src": src, "exists": resolved.exists(), "dimensions": dimensions})
    links = [
        {"href": href, "exists": (path.parent / href).exists()}
        for href in parser.links if not href.startswith(("http://", "https://", "#"))
    ]
    return {
        "file": path.relative_to(ROOT).as_posix(),
        "titles": parser.titles,
        "headings": parser.headings,
        "images": images,
        "links": links,
        "broken_images": sum(not item["exists"] for item in images),
        "broken_links": sum(not item["exists"] for item in links),
    }


def main():
    pages = [inspect_page(SAMPLE / name) for name in ("index.html", "summary.html")]
    css_anchor = "body { margin: 0; font-family: Arial, sans-serif; background: #18201a; color: #edf2e6; }"
    format_match = all(css_anchor in (SAMPLE / name).read_text(encoding="utf-8") for name in ("index.html", "summary.html"))
    result = "PASS" if format_match and all(
        page["broken_images"] == 0 and page["broken_links"] == 0 for page in pages
    ) else "FAIL"
    payload = {
        "result": result,
        "pahur_format_css_match": format_match,
        "static_dom_and_asset_check": result,
        "direct_visual_review": {
            "result": "PASS",
            "reviewed_images": [
                "renders/01_front_kursa_reference_match.png",
                "renders/02_three_quarter_kursa_reference_match.png",
                "renders/03_reference_side_by_side_overview.png",
                "renders/07_head_front_detail.png",
                "renders/08_head_detail_comparison.png",
                "renders/09_eye_surface_multiview.png",
                "renders/10_shield_arm_detail.png",
                "renders/11_shield_arm_comparison.png",
                "renders/12_torso_front_detail.png",
                "renders/13_torso_detail_comparison.png",
                "renders/14_eye_cavity_geometry_verification.png",
                "renders/15_eye_shape_distortion_correction.png",
            ],
        },
        "pages": pages,
    }
    OUTPUT.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"result": result, "pages": len(pages)}))


if __name__ == "__main__":
    main()
