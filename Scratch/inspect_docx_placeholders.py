from pathlib import Path
from zipfile import ZipFile
import re
import xml.etree.ElementTree as ET


docx_path = Path(r"F:\Git Repos\Time Tracker\Templates\Dani invoice template.docx")
ns = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}


def paragraph_text(paragraph):
    return "".join(node.text or "" for node in paragraph.findall(".//w:t", ns))


with ZipFile(docx_path) as zf:
    names = [name for name in zf.namelist() if name.endswith(".xml") and (name.startswith("word/") or name.startswith("docProps/"))]
    print(f"Package XML parts: {len(names)}")
    for name in names:
        xml = zf.read(name).decode("utf-8", errors="replace")
        if "{{" in xml:
            print(f"\nPART: {name}")
            for token in sorted(set(re.findall(r"\{\{[^}]+\}\}", xml))):
                print(f"  raw token: {token}")
            try:
                root = ET.fromstring(xml)
                for p in root.findall(".//w:p", ns):
                    text = paragraph_text(p)
                    if "{{" in text or "}}" in text:
                        print(f"  paragraph text: {text!r}")
                        print(f"  raw paragraph: {ET.tostring(p, encoding='unicode')[:900]}")
            except ET.ParseError as exc:
                print(f"  parse error: {exc}")
