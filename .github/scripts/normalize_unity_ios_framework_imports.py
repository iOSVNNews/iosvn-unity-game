#!/usr/bin/env python3
"""Normalize Unity's local UnityFramework imports before Xcode compiles the export.

Unity's generated sources can refer to their own not-yet-staged framework headers
through <UnityFramework/...>. Quoted paths keep these imports inside the export
tree and avoid depending on Xcode's framework header staging order.
"""

from pathlib import Path
import sys


IMPORTS = {
    Path("Classes/UnityAppController.h"): (
        "#import <UnityFramework/RenderPluginDelegate.h>",
        '#import "PluginBase/RenderPluginDelegate.h"',
    ),
    Path("Classes/PluginBase/RenderPluginDelegate.h"): (
        "#import <UnityFramework/LifeCycleListener.h>",
        '#import "LifeCycleListener.h"',
    ),
    Path("UnityFramework/UnityFramework.h"): (
        "#import <UnityFramework/UnityAppController.h>",
        '#import "../Classes/UnityAppController.h"',
    ),
    Path("MainApp/main.mm"): (
        "#import <UnityFramework/UnityFramework.h>",
        '#import "../UnityFramework/UnityFramework.h"',
    ),
    Path("Classes/main.mm"): (
        "#import <UnityFramework/UnityFramework.h>",
        '#import "../UnityFramework/UnityFramework.h"',
    ),
}


def normalize(project_dir: Path) -> int:
    project_dir = project_dir.resolve()
    if project_dir.suffix == ".xcodeproj":
        project_dir = project_dir.parent
    project_file = project_dir / "Unity-iPhone.xcodeproj" / "project.pbxproj"
    if not project_file.is_file():
        raise SystemExit(f"Unity Xcode project not found under {project_dir}")

    changed = 0
    for relative_path, (generated_import, local_import) in IMPORTS.items():
        path = project_dir / relative_path
        if not path.is_file():
            raise SystemExit(f"Expected Unity-generated file is missing: {path}")
        source = path.read_text(encoding="utf-8")
        generated_count = source.count(generated_import)
        local_count = source.count(local_import)
        if generated_count == 1 and local_count == 0:
            path.write_text(source.replace(generated_import, local_import), encoding="utf-8")
            changed += 1
        elif generated_count == 0 and local_count == 1:
            continue
        else:
            raise SystemExit(
                f"Unexpected Unity import layout in {path}: "
                f"generated={generated_count}, normalized={local_count}; "
                "review this Unity export before changing the normalizer."
            )

    print(f"Unity Xcode import normalization complete ({changed} file(s) updated).")
    return changed


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: normalize_unity_ios_framework_imports.py <xcode-project-dir>")
    normalize(Path(sys.argv[1]))
