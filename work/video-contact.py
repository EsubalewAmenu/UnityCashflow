import cv2
from PIL import Image,ImageDraw
v=cv2.VideoCapture('C:/Users/hp/Downloads/good_can_you_show_me_in_actio.mp4'); n=v.get(cv2.CAP_PROP_FRAME_COUNT); fps=v.get(cv2.CAP_PROP_FPS); print(n/fps)
sheet=Image.new('RGB',(1280,900))
for i in range(16):
 t=(n/fps)*i/16;v.set(cv2.CAP_PROP_POS_MSEC,t*1000); ok,f=v.read()
 if ok:
  im=Image.fromarray(cv2.cvtColor(f,cv2.COLOR_BGR2RGB));im.thumbnail((320,200));x=i%4*320;y=i//4*225;sheet.paste(im,(x,y));ImageDraw.Draw(sheet).text((x,y+202),str(round(t,1)),fill='white')
sheet.save('work/video-contact.jpg')
