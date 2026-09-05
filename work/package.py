from pathlib import Path
import zipfile, shutil
root=Path(r'C:/Users/hp/Desktop/personal/UnityCashflow')
out=root/'outputs'
files=list((root/'Assets/Scripts').glob('*'))+list((root/'Assets/Editor').glob('*'))+[root/'Assets/Editor.meta']+list((root/'Assets/Resources/GameBoard').glob('*.meta'))
with zipfile.ZipFile(out/'Board-redesign-files.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in files:
        if p.is_file(): z.write(p,p.relative_to(root))
    z.write(out/'Board-redesign.md','Board-redesign.md')
for name in ['1280x720.png','1920x1080.png','2340x1080.png','1024x768.png','results.txt']:
    shutil.copy2(root/'work/board-review'/name,out/name)
print('Packaged',len(files),'project files')
