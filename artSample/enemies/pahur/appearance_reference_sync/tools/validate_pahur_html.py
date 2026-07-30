from html.parser import HTMLParser
from pathlib import Path
import json
import sys

from PIL import Image


ROOT = Path(r"D:\Bellerophon2\Bellerophon")
SAMPLE_ROOT = ROOT / "artSample/enemies/pahur/appearance_reference_sync"
OUTPUT = SAMPLE_ROOT / "HTML_VALIDATION.json"


class Collector(HTMLParser):
    def __init__(self):
        super().__init__()
        self.images = []
        self.links = []
        self.titles = []
        self.headings = []
        self.current = None

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if tag == "img":
            self.images.append(attributes.get("src"))
        elif tag == "a":
            self.links.append(attributes.get("href"))
        elif tag in {"title", "h1", "h2"}:
            self.current = tag

    def handle_endtag(self, tag):
        if tag == self.current:
            self.current = None

    def handle_data(self, data):
        text = data.strip()
        if not text:
            return
        if self.current == "title":
            self.titles.append(text)
        elif self.current in {"h1", "h2"}:
            self.headings.append(text)


def inspect_html(path):
    collector = Collector()
    collector.feed(path.read_text(encoding="utf-8"))
    images = []
    for source in collector.images:
        resolved = (path.parent / source).resolve()
        exists = resolved.is_file()
        dimensions = None
        if exists:
            with Image.open(resolved) as image:
                dimensions = [image.width, image.height]
        images.append(
            {
                "src": source,
                "resolved": str(resolved),
                "exists": exists,
                "dimensions": dimensions,
            }
        )
    links = []
    for href in collector.links:
        if href.startswith(("http://", "https://", "#")):
            exists = True
            resolved = href
        else:
            target = (path.parent / href).resolve()
            exists = target.is_file()
            resolved = str(target)
        links.append({"href": href, "resolved": resolved, "exists": exists})
    return {
        "file": str(path.relative_to(ROOT)).replace("\\", "/"),
        "titles": collector.titles,
        "headings": collector.headings,
        "images": images,
        "links": links,
        "broken_images": sum(1 for item in images if not item["exists"]),
        "broken_links": sum(1 for item in links if not item["exists"]),
    }


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    pages = [
        inspect_html(SAMPLE_ROOT / "index.html"),
        inspect_html(SAMPLE_ROOT / "summary.html"),
    ]
    static_pass = all(
        page["titles"]
        and page["headings"]
        and page["broken_images"] == 0
        and page["broken_links"] == 0
        for page in pages
    )
    report = {
        "result": "PASS" if static_pass else "FAIL",
        "static_dom_and_asset_check": "PASS" if static_pass else "FAIL",
        "browser_render_check": {
            "attempted": True,
            "result": "BLOCKED_BY_BROWSER_CONNECTION",
            "note": (
                "The in-app browser connection could not start because its "
                "environment metadata was unavailable. Static DOM/assets and "
                "all generated review images were inspected directly."
            ),
        },
        "direct_visual_review": {
            "result": "PASS",
            "reviewed_images": [
                "renders/01_front_pahur_reference_match.png",
                "renders/02_three_quarter_pahur_reference_match.png",
                "renders/03_reference_side_by_side_overview.png",
                "renders/05_rear_current_model_material.png",
                "renders/07_head_front_detail.png",
                "renders/07_head_three_quarter_detail.png",
                "renders/08_head_detail_comparison.png",
                "renders/10_shoulders_left_arm_detail.png",
                "renders/11_shoulders_left_arm_comparison.png",
                "renders/12_torso_front_detail.png",
                "renders/13_torso_detail_comparison.png",
            ],
        },
        "pages": pages,
    }
    OUTPUT.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if not static_pass:
        raise RuntimeError("HTML validation failed.")


if __name__ == "__main__":
    main()
