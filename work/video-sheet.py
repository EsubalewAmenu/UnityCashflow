from PIL import Image,ImageDraw
from pathlib import Path
files=sorted(Path('work/video-frames').glob('*.png'),key=lambda p:int(p.stem));sheet=Image.new('RGB',(1280,((len(files)+3)//4)*200))
for i,p in enumerate(files):
 im=Image.open(p);im.thumbnail((320,180));x=i%4*320;y=i//4*200;sheet.paste(im,(x,y));ImageDraw.Draw(sheet).text((x,y+181),p.stem,fill='white')
sheet.save('work/video-contact.jpg')
