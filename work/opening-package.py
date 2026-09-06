from pathlib import Path
import shutil
out=Path('outputs/3d/opening');out.mkdir(exist_ok=True)
for p in Path('work/opening-review').glob('*'):
 if p.suffix in ['.png','.txt'] and p.name!='error.txt':shutil.copy2(p,out/p.name)
with Path('outputs/3d/README.md').open('a',encoding='utf-8') as f:f.write('\n## Illustrated opening flow\nReplaced the old setup dashboard with profession and dream card carousels inspired by the prototype. Added original portrait/dream atlases, a full-screen city backdrop, selected-card highlighting, short entrance animation, swipe/arrow navigation, financial previews and large Next/Start actions. Profession and dream selections still use the existing scene flow. Reused the new profession art for the board HUD. Removed the placeholder Resume controls, old setup rendering helpers, five unreferenced sprite sheets, the unused regular font and unused Main scene after reference checks. Opening layout tests passed at 1280x720, 2532x1170, 2340x1080 and 1024x768, including selection-to-game handoff. Artwork paths and final generation prompts are recorded in OPENING-ART.md.\n')
p=Path('work/package3d.py');s=p.read_text();s=s.replace("    z.write(out/'README.md','README.md')","    z.write(out/'README.md','README.md')\n    if (out/'OPENING-ART.md').exists(): z.write(out/'OPENING-ART.md','OPENING-ART.md')");p.write_text(s)
