from pathlib import Path
from PIL import Image
import shutil
p=Path('Assets/Editor/BoardReview.cs');p.write_text(p.read_text().rstrip()+'\n')
out=Path('outputs/3d/cards');out.mkdir(exist_ok=True)
for p in Path('work/deal-review').glob('*.png'):
 if not p.name.startswith('reveal-'):shutil.copy2(p,out/p.name)
shutil.copy2('work/deal-review/results.txt',out/'results.txt')
frames=[]
for p in sorted(Path('work/deal-review').glob('reveal-*.png'),key=lambda p:int(p.stem.split('-')[1])):
 im=Image.open(p).convert('RGB');im.thumbnail((960,540));frames.append(im)
if frames:frames[0].save(out/'card-reveal.gif',save_all=True,append_images=frames[1:],duration=[120]*(len(frames)-1)+[1400],loop=0)
with Path('outputs/3d/README.md').open('a',encoding='utf-8') as f:f.write('\n## Deal cards\nLanding on Deal now offers separate Small Deal and Big Deal decks using the existing investment amounts. Cards show down payment, income, available cash, and affordability; Buy updates balances exactly once and Pass closes the modal correctly. A shared cream-and-gold presentation improves event cards too. Deals flip from an opportunity card back with gold sparkles; purchases show a brief celebration. Reduced Motion uses a short fade. Deal-flow tests passed alongside the five-resolution board regression.\n')
