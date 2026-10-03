#!/usr/bin/env python3
"""Set the iOS app identity on the app target without changing UnityFramework."""

from pathlib import Path
import re
import sys


def block_for_id(source: str, object_id: str) -> re.Match[str]:
    pattern = re.compile(
        rf"(?ms)^\t\t{re.escape(object_id)} /\*[^\n]*?\*/ = \{{.*?^\t\t\}};"
    )
    match = pattern.search(source)
    if match is None:
        raise SystemExit(f"Xcode project object {object_id} was not found.")
    return match


def replace_build_setting(block: str, key: str, value: str) -> str:
    settings_start = block.find("buildSettings = {")
    if settings_start < 0:
        raise SystemExit("Xcode build configuration has no buildSettings dictionary.")
    settings_end = re.search(r"(?m)(^\t{3}\};|\t{3}\};)", block[settings_start:])
    if settings_end is None:
        raise SystemExit("Could not find the end of Xcode buildSettings dictionary.")
    end = settings_start + settings_end.start()
    settings = block[settings_start:end]
    setting = re.compile(rf"(?m)^(\t*)({re.escape(key)})\s*=\s*[^;]*;")
    replacement = f"\t\t\t\t{key} = {value};"
    if setting.search(settings):
        settings = setting.sub(replacement, settings, count=1)
    else:
        if not settings.endswith("\n"):
            settings += "\n"
        settings += f"{replacement}\n"
    return block[:settings_start] + settings + "\t\t\t};" + block[settings_start + settings_end.end():]


def configure(project_file: Path, bundle_id: str) -> None:
    if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_-]*(\.[A-Za-z_][A-Za-z0-9_-]*)+", bundle_id):
        raise SystemExit("Invalid iOS bundle identifier.")

    source = project_file.read_text(encoding="utf-8")
    target = re.search(
        r"(?ms)^\t\t([A-Fa-f0-9]{24}) /\* Unity-iPhone \*/ = \{"
        r".*?^\t\t\};",
        source,
    )
    if target is None:
        raise SystemExit("Unity-iPhone app target was not found.")
    target_block = target.group(0)
    config_list = re.search(r"buildConfigurationList = ([A-Fa-f0-9]{24})", target_block)
    if config_list is None:
        raise SystemExit("Unity-iPhone target has no build configuration list.")

    list_match = block_for_id(source, config_list.group(1))
    configurations = re.search(
        r"buildConfigurations\s*=\s*\((.*?)\);", list_match.group(0), re.DOTALL
    )
    if configurations is None:
        raise SystemExit("Unity-iPhone configuration list has no buildConfigurations array.")
    config_ids = re.findall(r"\b([A-Fa-f0-9]{24}) /\*", configurations.group(1))
    if not config_ids:
        raise SystemExit("Unity-iPhone target has no build configurations.")

    updates = []
    for config_id in config_ids:
        config_match = block_for_id(source, config_id)
        config_block = config_match.group(0)
        config_block = replace_build_setting(config_block, "PRODUCT_NAME", "TuTienGioi")
        config_block = replace_build_setting(
            config_block, "PRODUCT_BUNDLE_IDENTIFIER", bundle_id
        )
        config_block = replace_build_setting(
            config_block, "INFOPLIST_KEY_CFBundleDisplayName", '"Tu Tiên Giới"'
        )
        updates.append((config_match.start(), config_match.end(), config_block))

    for start, end, replacement in sorted(updates, reverse=True):
        source = source[:start] + replacement + source[end:]
    project_file.write_text(source, encoding="utf-8")
    print(f"Configured {len(config_ids)} Unity-iPhone build configurations; UnityFramework identity is unchanged.")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Usage: configure_ios_app_target.py <Unity-iPhone.xcodeproj> <bundle-id>")
    path = Path(sys.argv[1]).resolve()
    project_file = path / "project.pbxproj" if path.suffix == ".xcodeproj" else path / "Unity-iPhone.xcodeproj" / "project.pbxproj"
    if not project_file.is_file():
        raise SystemExit(f"Xcode project file is missing: {project_file}")
    configure(project_file, sys.argv[2])
