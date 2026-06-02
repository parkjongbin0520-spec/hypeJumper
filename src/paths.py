"""리소스 경로 해석 — 개발 실행과 PyInstaller 번들 양쪽에서 동일하게 동작."""

import os
import sys


def project_root():
    """프로젝트 루트(또는 PyInstaller 번들 추출 루트) 절대 경로를 반환."""
    # PyInstaller onefile 실행 시 sys._MEIPASS = 임시 추출 폴더 (asset 풀리는 곳)
    base = getattr(sys, "_MEIPASS", None)
    if base is not None:
        return base
    # 개발 실행: 이 파일=src/paths.py → 상위의 상위가 프로젝트 루트
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def resource_path(*parts):
    """프로젝트 루트 기준 상대 경로를 절대 경로로 변환 (번들/개발 공통)."""
    return os.path.join(project_root(), *parts)
