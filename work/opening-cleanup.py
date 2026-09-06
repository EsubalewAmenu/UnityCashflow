from pathlib import Path
p=Path('Assets/Scripts/CashflowOpening.cs');s=p.read_text(encoding='utf-8').replace('.2f,.68f','.2f,.55f').replace('dreamsPage?"1  PROFESSION   —   2  DREAM":"1  PROFESSION   —   2  DREAM"','dreamsPage?"STEP 2 OF 2  •  YOUR DREAM":"STEP 1 OF 2  •  YOUR PROFESSION"');p.write_text(s,encoding='utf-8')
p=Path('Assets/Scripts/CashflowGame.cs');s=p.read_text(encoding='utf-8').replace('sceneName == "ProfessionSelection" || sceneName == "Main"','sceneName == "ProfessionSelection"');p.write_text(s,encoding='utf-8')
# Check serialized GUID dependencies before removing retired assets.
root=Path('Assets');files=[root/'Resources/GameBoard'/f'{name}.png' for name in ['brand-sheet','cards-sheet','currency-dice-sheet','event-icons-sheet','character-pieces-sheet']]+[root/'Resources/Fonts/ModernRegular.ttf',root/'Scenes/Main.unity']
serialized=[p for p in root.rglob('*') if p.suffix in ['.unity','.prefab','.asset']]+list(Path('ProjectSettings').glob('*.asset'))
for p in files:
 meta=Path(str(p)+'.meta');guid=next(line.split(': ')[1] for line in meta.read_text().splitlines() if line.startswith('guid:'))
 refs=[str(q) for q in serialized if q!=p and guid in q.read_text(errors='ignore')]
 if refs:raise RuntimeError(f'{p}: referenced by {refs}')
for p in files:p.unlink();Path(str(p)+'.meta').unlink()
print('Removed 5 unused sprite sheets, unused regular font and unused Main scene after GUID checks.')
