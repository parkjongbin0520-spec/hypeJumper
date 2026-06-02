# -*- mode: python ; coding: utf-8 -*-
# PyInstaller onefile 빌드 설정 — assets 폴더 전체를 exe 안에 번들.
# 빌드:  pyinstaller hypejumper.spec
# 결과:  dist/HypeJumper.exe (단일 실행 파일)

block_cipher = None

a = Analysis(
    ['main.py'],
    pathex=[],
    binaries=[],
    datas=[('assets', 'assets')],   # 스프라이트/사운드/타일맵 전부 번들 → sys._MEIPASS/assets 로 풀림
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name='HypeJumper',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,        # 게임 창만 — 콘솔 창 숨김
    disable_windowed_traceback=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
