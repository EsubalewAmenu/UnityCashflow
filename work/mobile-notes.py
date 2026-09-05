from pathlib import Path
p=Path('outputs/3d/README.md')
with p.open('a',encoding='utf-8') as f:
 f.write('\n## Mobile controls and framing\nRemoved the right-side zoom/rotate controls and the minimap camera/render texture. Settings now lives in the bottom bar; its Camera View section switches between Overview and Follow. The scene renders across the full display width while the HUD retains safe-area padding. Perspective framing fits the curved board perimeter dynamically for different aspect ratios. Editor review includes 2532x1170 (iPhone 12) and 2340x1080 (Pixel 5), plus 1280x720, 1920x1080 and 1024x768. These are editor resolution checks, not physical-device tests.\n')
p=Path('work/package3d.py');s=p.read_text();s=s.replace("'2340x1080.png',","'2340x1080.png','2532x1170.png',");p.write_text(s)
