# 가우시안 스플래팅 LOD 시스템

가우시안 스플랫과 메시 간의 동적 전환을 지원하는 LOD(Level of Detail) 시스템이 적용된 Unity HDRP 프로젝트입니다.

## 주요 기능

- **LOD 관리**: 거리에 따른 메시와 가우시안 스플랫 표현 간의 자동 전환
- **다중 오브젝트 생성**: LOD 컴포넌트가 포함된 그리드 기반 생성 시스템
- **거리 기반 컬링**: 오브젝트 컬링을 통한 성능 최적화
- **런타임 제어**: LOD 상태 강제 설정 및 성능 모니터링을 위한 GUI 컨트롤

## 스크립트

### 핵심 컴포넌트

- `LODSwitcher.cs` - 개별 오브젝트 LOD 전환 로직
- `LODManager.cs` - 전역 LOD 관리 및 통계
- `LODData.cs` - LOD 설정을 위한 데이터 구조

### 생성 및 관리

- `MultiObjectSpawner.cs` - 자동 LOD 설정을 포함한 그리드 기반 오브젝트 생성
- `SimpleSwapper.cs` - 간단한 메시/가우시안 스플랫 토글 기능
- `GSPlyExporter.cs` - PLY 파일 생성용 에디터
## 사용법

1. 전역 LOD 제어를 위해 씬에 `LODManager` 추가
2. LOD 오브젝트 그리드 생성을 위해 `MultiObjectSpawner` 사용
3. 개별 오브젝트는 커스텀 LOD 동작을 위해 `LODSwitcher` 사용

## 컨트롤

- **R 키**: LOD 스위처 감지 새로고침
- **T 키**: 통계 표시 토글
- **GUI 버튼**: 모든 오브젝트를 메시 또는 가우시안 스플랫 표현으로 강제 전환

## 요구사항

- Unity with HDRP
- 가우시안 스플래팅 패키지

## Reference
- https://github.com/aras-p/UnityGaussianSplatting - 가우시안 스플래팅 렌더
