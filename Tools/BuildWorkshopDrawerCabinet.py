import hashlib
import json
from pathlib import Path
import shutil

from PIL import Image, ImageDraw, ImageFont

root = Path.cwd()
build = root / '.keyboard-drawer-cabinet-build'
(build / 'Assets/Editor').mkdir(parents=True, exist_ok=True)
(build / 'Packages').mkdir(exist_ok=True)
(build / 'ProjectSettings').mkdir(exist_ok=True)
shutil.copy2(root / 'Tools/WorkshopDrawerCabinetBuilder.cs', build / 'Assets/Editor/WorkshopDrawerCabinetBuilder.cs')
shutil.copy2(root / 'ProjectSettings/ProjectVersion.txt', build / 'ProjectSettings/ProjectVersion.txt')
packages = {}
for item in (root / 'Library/PackageCache').glob('*/package.json'):
    data = json.loads(item.read_text(encoding='utf-8'))
    packages[data['name']] = (item.parent, data)
deps = {}
def add_package(name, version):
    if name in deps:
        return
    if name in packages:
        folder, data = packages[name]
        deps[name] = 'file:' + folder.as_posix()
        for dep, ver in data.get('dependencies', {}).items():
            add_package(dep, ver)
    else:
        deps[name] = version
add_package('com.unity.render-pipelines.universal', '17.3.0')
for name in ['imageconversion', 'video', 'audio', 'physics', 'imgui', 'screencapture']:
    add_package('com.unity.modules.' + name, '1.0.0')
(build / 'Packages/manifest.json').write_text(json.dumps({'dependencies': deps}, indent=2))

sources = ['Assets/Resource/Materials/KeycapWoodWalnut.mat',
           'Assets/Resource/Materials/KeycapWoodGrain.asset',
           'Assets/Resource/Materials/PBT_MicroGrain.asset',
           'Assets/Resource/Materials/WorkshopDisplay/DisplayGraphite.mat',
           'Assets/Resource/Materials/WorkshopDisplay/DisplayChampagne.mat']
for name in sources:
    path = build / name
    path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(root / name, path)
    shutil.copy2(root / (name + '.meta'), str(path) + '.meta')

labels = ['키캡', '스위치(축)', '나사/도구', 'LED/커스텀 이미지', '상부 하우징', '보강판', 'PCB', '하부 하우징']
atlas = Image.new('RGB', (2048, 1024), '#f0e5cc')
draw = ImageDraw.Draw(atlas)
font = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 82)
for index, label in enumerate(labels):
    draw.text((1024, index * 128 + 64), label, font=font, fill='#29221b', anchor='mm')
dest = build / 'Assets/Resource/Prefabs/Workshop/DrawerCabinet/Labels.png'
dest.parent.mkdir(parents=True, exist_ok=True)
atlas.save(dest)

record = {'build': str(build), 'scene_sha256': hashlib.sha256((root / 'Assets/Scenes/W08_Main_Test.unity').read_bytes()).hexdigest()}
(root / 'output/WorkshopDrawerBuild.json').write_text(json.dumps(record, indent=2))
print(build)
