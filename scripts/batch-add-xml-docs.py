#!/usr/bin/env python3
"""Batch-add XML documentation comments to C# files missing /// summaries."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SEARCH_ROOTS = ["src", "application", "commercial", "apps", "samples", "spikes", "tools"]
SKIP_DIRS = {"obj", "bin", "node_modules"}
SKIP_PATH_PARTS = {
    "tools/unity-demo-output",
    "tools/ui-demo-output",
}

TYPE_PATTERN = re.compile(
    r"^(?P<indent>\s*)(?P<attrs>(?:\[[^\]]+\]\s*)*)"
    r"(?P<mods>(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|new|required)\s+)*"
    r"(?P<kind>class|record|struct|interface|enum)\s+"
    r"(?P<name>\w+)"
    r"(?P<rest>[\s\S]*?)$",
    re.MULTILINE,
)

METHOD_PATTERN = re.compile(
    r"^(?P<indent>\s*)(?P<attrs>(?:\[[^\]]+\]\s*)*)"
    r"(?P<mods>(?:public|internal|protected|private|static|virtual|override|async|sealed|partial|new|extern|unsafe|required)\s+)*"
    r"(?P<ret>[\w<>,.?[\]\s]+?)\s+"
    r"(?P<name>(?!class|record|struct|interface|enum|get|set|if|for|while|switch|return)\w+)\s*"
    r"\((?P<params>[^)]*)\)"
    r"(?:\s*(?:where\s+[^{;]+)?)?\s*(?:=>|[;{])",
    re.MULTILINE,
)

PROPERTY_PATTERN = re.compile(
    r"^(?P<indent>\s*)(?P<attrs>(?:\[[^\]]+\]\s*)*)"
    r"(?P<mods>(?:public|internal|protected|private|static|virtual|override|required)\s+)*"
    r"(?P<type>[\w<>,.?[\]\s]+?)\s+"
    r"(?P<name>\w+)\s*\{\s*(?:get|init|set)",
    re.MULTILINE,
)

RECORD_PRIMARY_CTOR = re.compile(
    r"^(?P<indent>\s*)(?P<mods>(?:public|internal|private|protected|sealed|partial|readonly|required)\s+)*"
    r"record\s+(?P<name>\w+)\s*\((?P<params>[^)]*)\)",
    re.MULTILINE,
)


def split_words(name: str) -> str:
    if name.startswith("I") and len(name) > 1 and name[1].isupper():
        name = name[1:]
    parts = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", name)
    parts = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1 \2", parts)
    return parts.lower()


def summarize_type(name: str, kind: str, file_path: Path) -> str:
    path_str = str(file_path).replace("\\", "/")
    if name.endswith("Tests") or "/Tests/" in path_str or name.endswith("Test"):
        target = name.removesuffix("Tests").removesuffix("Test")
        return f"Tests for {split_words(target) or name}."
    if kind == "interface":
        if name.startswith("I"):
            return f"Contract for {split_words(name[1:])}."
        return f"Contract for {split_words(name)}."
    if kind == "enum":
        return f"Enumerates {split_words(name.removesuffix('s'))} values."
    if name.endswith("Dto"):
        return f"Wire DTO for {split_words(name.removesuffix('Dto'))}."
    if name.endswith("Options"):
        return f"Configuration options for {split_words(name.removesuffix('Options'))}."
    if name.endswith("Exception"):
        return f"Exception thrown when {split_words(name.removesuffix('Exception'))} fails."
    if name.endswith("Handler"):
        return f"Handles {split_words(name.removesuffix('Handler'))} requests."
    if name.endswith("Service"):
        return f"Service for {split_words(name.removesuffix('Service'))} operations."
    if name.endswith("Client"):
        return f"Client for {split_words(name.removesuffix('Client'))} interactions."
    if name.endswith("Middleware"):
        return f"Middleware for {split_words(name.removesuffix('Middleware'))}."
    if name.endswith("Command"):
        return f"CLI command for {split_words(name.removesuffix('Command'))}."
    if kind == "record":
        return f"{split_words(name).capitalize()} value."
    return f"{split_words(name).capitalize()}."


def summarize_member(name: str, is_method: bool) -> str:
    words = split_words(name)
    if is_method:
        if name.startswith("Try") and len(name) > 3:
            return f"Attempts to {split_words(name[3:])}; returns false on failure."
        if name.startswith("Get"):
            return f"Gets {split_words(name[3:]) or 'the value'}."
        if name.startswith("Set"):
            return f"Sets {split_words(name[3:]) or 'the value'}."
        if name.startswith("Is") or name.startswith("Has") or name.startswith("Can"):
            return f"Returns whether {words[2:] if len(words) > 2 else words}."
        if name.startswith("Create") or name.startswith("Build"):
            return f"Creates {split_words(name[6:] if name.startswith('Create') else name[5:]) or 'an instance'}."
        return f"{words.capitalize()}."
    return f"{words.capitalize()}."


def has_doc_above(lines: list[str], index: int) -> bool:
    i = index - 1
    while i >= 0 and lines[i].strip() == "":
        i -= 1
    if i < 0:
        return False
    stripped = lines[i].strip()
    if stripped.startswith("///"):
        return True
    if stripped.startswith("[") or stripped.startswith("#"):
        return has_doc_above(lines, i)
    if stripped.startswith("//") and not stripped.startswith("///"):
        return True
    return False


def parse_params(params: str) -> list[tuple[str, str]]:
    if not params.strip():
        return []
    result: list[tuple[str, str]] = []
    for part in params.split(","):
        part = part.strip()
        if not part:
            continue
        tokens = part.split()
        if len(tokens) < 2:
            continue
        pname = tokens[-1].lstrip("@")
        ptype = " ".join(tokens[:-1])
        result.append((pname, ptype))
    return result


def insert_doc(lines: list[str], index: int, doc_lines: list[str]) -> None:
    indent_match = re.match(r"^(\s*)", lines[index])
    base_indent = indent_match.group(1) if indent_match else ""
    for offset, doc in enumerate(doc_lines):
        lines.insert(index + offset, f"{base_indent}{doc}")


def process_file(path: Path) -> bool:
    text = path.read_text(encoding="utf-8")
    if "///" in text or re.search(r"^//[^/]", text, re.MULTILINE):
        return False

    lines = text.splitlines(keepends=True)
    if not lines:
        return False

    changed = False
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        record_match = RECORD_PRIMARY_CTOR.match(line)
        if record_match and not has_doc_above(lines, i):
            name = record_match.group("name")
            params = parse_params(record_match.group("params") or "")
            doc = [f"/// <summary>{summarize_type(name, 'record', path)}</summary>\n"]
            for pname, _ in params:
                doc.append(f"/// <param name=\"{pname}\">{summarize_member(pname, False)}</param>\n")
            insert_doc(lines, i, doc)
            changed = True
            i += len(doc) + 1
            continue

        type_match = TYPE_PATTERN.match(line)
        if type_match and type_match.group("kind") != "record" and not has_doc_above(lines, i):
            name = type_match.group("name")
            kind = type_match.group("kind")
            if kind == "class" and " record " in f" {type_match.group('mods') or ''} ":
                i += 1
                continue
            insert_doc(lines, i, [f"/// <summary>{summarize_type(name, kind, path)}</summary>\n"])
            changed = True
            i += 2
            continue

        if not has_doc_above(lines, i):
            method_match = METHOD_PATTERN.match(line)
            if method_match and " class " not in line and " interface " not in line:
                mods = method_match.group("mods") or ""
                if "private " in mods and "/Tests/" not in str(path).replace("\\", "/"):
                    i += 1
                    continue
                name = method_match.group("name")
                params = parse_params(method_match.group("params") or "")
                doc = [f"/// <summary>{summarize_member(name, True)}</summary>\n"]
                for pname, _ in params:
                    if pname not in {"cancellationToken", "ct"}:
                        doc.append(f"/// <param name=\"{pname}\">{summarize_member(pname, False)}</param>\n")
                    else:
                        doc.append(f"/// <param name=\"{pname}\">Cancellation token.</param>\n")
                insert_doc(lines, i, doc)
                changed = True
                i += len(doc) + 1
                continue

            prop_match = PROPERTY_PATTERN.match(line)
            if prop_match and "{" in line:
                name = prop_match.group("name")
                insert_doc(lines, i, [f"/// <summary>{summarize_member(name, False)}</summary>\n"])
                changed = True
                i += 2
                continue

        i += 1

    if changed:
        path.write_text("".join(lines), encoding="utf-8")
    return changed


def should_process(path: Path) -> bool:
    rel = path.relative_to(ROOT).as_posix()
    for skip in SKIP_PATH_PARTS:
        if rel.startswith(skip):
            return False
    return True


def main() -> int:
    changed_files = 0
    for root_name in SEARCH_ROOTS:
        root = ROOT / root_name
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            if any(part in SKIP_DIRS for part in path.parts):
                continue
            if not should_process(path):
                continue
            if process_file(path):
                changed_files += 1
                print(f"documented: {path.relative_to(ROOT)}")

    print(f"\nTotal files updated: {changed_files}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
