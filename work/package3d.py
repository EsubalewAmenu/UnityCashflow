from pathlib import Path
import zipfile, shutil
root=Path(r'C:/Users/hp/Desktop/personal/UnityCashflow')
out=root/'outputs/3d'
with zipfile.ZipFile(out/'Cashflow-3D-project.zip','w',zipfile.ZIP_DEFLATED) as z:
    for folder in ['Assets','Packages','ProjectSettings']:
        for p in (root/folder).rglob('*'):
            if p.is_file(): z.write(p,p.relative_to(root))
    z.write(out/'README.md','README.md')
for name in ['1280x720.png','1920x1080.png','2340x1080.png','2532x1170.png','1024x768.png','follow-camera.png','landed-event.png','results.txt']:
    shutil.copy2(root/'work/3d-review'/name,out/name)
print('Complete Unity source project packaged without Library or temporary files.')
