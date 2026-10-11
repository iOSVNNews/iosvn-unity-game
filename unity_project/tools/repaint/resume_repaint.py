"""Resume downloaded painted sheets without losing the previous artist's work.

Run with --audit first. Default imports only sheets absent from ChatGPTRepaint.
All four monster poses are required; a missing pose must never be copied from idle.
"""
import argparse
import json
from pathlib import Path
import shutil
import split_all
from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--downloads', type=Path, default=Path.home() / 'Downloads')
    parser.add_argument('--audit', action='store_true')
    parser.add_argument('--sheet', action='append', help='Import selected sheets again after a splitter fix')
    parser.add_argument('--kind', choices=['skill', 'weapon', 'monster'])
    parser.add_argument('--force', action='store_true', help='Reimport selected sheets even when their log exists')
    parser.add_argument('--start', help='Resume from a sheet name, still applying species fixes last')
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[2]
    manifest = project / 'tools/repaint/manifest.json'
    output = project / 'ArtSource/ChatGPTRepaint'
    output.mkdir(parents=True, exist_ok=True)
    resources = project / 'Assets/Resources'
    backup = project / 'ArtSource/_old'
    report = []
    sheets = json.loads(manifest.read_text(encoding='utf-8'))
    fixes = project / 'tools/repaint/fixes.json'
    if fixes.exists():
        for fix in json.loads(fixes.read_text(encoding='utf-8')):
            fix['_fix'] = True
            sheets.append(fix)
    for sheet in sheets:
        name = sheet['sheet']
        if args.start and not sheet.get('_fix') and name < args.start:
            continue
        if args.sheet and name not in args.sheet and sheet.get('replaces') not in args.sheet:
            continue
        if args.kind and sheet['kind'] != args.kind:
            continue
        source = output / ('Fixes' if sheet.get('_fix') else '') / (name + '.png')
        downloaded = args.downloads / ('gem_cg4_' + name + '.png')
        if not source.exists() and downloaded.exists():
            if not args.audit:
                shutil.copy2(downloaded, source)
            source = downloaded if args.audit else source
        if not source.exists():
            print('NO SHEET', name, flush=True)
            report.append({'sheet': name, 'missingSheet': True})
            continue
        rows = len(sheet['items']) if sheet['kind'] == 'monster' else sheet['rows']
        cells = split_all.cells(split_all.key(Image.open(source)), 4, rows)
        count = len(sheet['items']) * (4 if sheet['kind'] == 'monster' else 1)
        missing = [i for i in range(count) if i not in cells]
        if missing:
            print('MISSING CELLS', name, missing, flush=True)
        report.append({'sheet': name, 'cells': len(cells), 'expected': count, 'missing': missing})
        log = source.with_suffix('.log')
        if not args.audit and (args.sheet or args.force or sheet.get('_fix') or not log.exists()) and not missing:
            split_all.main(str(source), str(fixes if sheet.get('_fix') else manifest), str(resources), str(backup))
            log.write_text('Imported all cells by resume_repaint.py\n', encoding='utf-8')
            print('IMPORTED', name, flush=True)
    (output / ('audit-selected.json' if args.sheet else 'audit.json')).write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('Audited', len(report), 'sheets', flush=True)


if __name__ == '__main__':
    main()
