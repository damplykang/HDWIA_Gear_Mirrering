# HDWIA_Gear (WIA Viewer)

기어(GEAR) 비전 검사 데이터를 **조회·분석·등급 산출**하는 Windows Forms 데스크톱 애플리케이션입니다.  
Mitsubishi PLC 트리거 연동, Keyence TXT 수집, OpenCV 기반 곡면 이미지 평활화, ScottPlot 통계 시각화를 **1920×1080** 단일 화면에서 제공합니다.

> **최종 갱신:** 2026-06-02

---

## 개요

| 항목 | 내용 |
|------|------|
| 솔루션 | `WIA_ViewerProgram.slnx` |
| 시작 폼 | `ViewerForm` |
| 대상 OS | Windows 10 이상 (x64 권장) |
| 프레임워크 | `.NET 10` (`net10.0-windows7.0`, Windows Forms) |
| 저장소 | `http://10.0.0.20:30080/TaeWon/hdwia_gear.git` |

앱은 **뷰어**와 **라인 후처리 엔진** 역할을 동시에 합니다.

- **뷰어 (`ViewerForm`)** — RECIPE 검색, 단일/복수 통계 차트, 단일 이미지 검사, 캘리브레이션
- **후처리 (`PLC`)** — PLC 트리거 시 Keyence TXT → CSV 통합, DC 정렬·이미지 리네임, 센서 저장, 등급 계산

---

## 주요 기능

| 영역 | 설명 |
|------|------|
| **로그인** | Operator / Admin / Master 역할별 접근 (`LoginData.json`). 미로그인 시 RECIPE·TCP/IP 등 제한 |
| **TCP/IP** | PLC 설정·모니터링, Keyence IP/Port 설정, TCP 수신·연결 확인 |
| **PLC 모니터링** | D레지스터(기본 `D6000`) 폴링, 트리거=1 시 자동 데이터 파이프라인 실행 |
| **RECIPE** | 기간·모델별 BCR/시행 폴더 목록, 단일/복수 통계(ScottPlot), 단일 이미지 검사 |
| **Cal Data** | Front/Rear 원본 이미지 선택, OpenCV 4×22 그리드 기반 Perspective 평활화 |
| **Front / Rear** | 검사 이미지·좌표·각도·치수 표시 (`Acceleration` / `Deceleration` 폴더) |
| **로그** | `Logs/HDMIndoPE_yyyyMMdd.log` (10MB 회전, 최대 10파일 보관) |

### PLC 트리거 시 자동 처리 순서

`MoniterAdrress` 값이 **1**이 되면 아래가 순서대로 실행됩니다.

1. `CSVFileCreate("AC")` / `CSVFileCreate("DC")` — Keyence TXT → FTP 경로 `ResultOutput.csv`
2. `DcDataSort()` — DC `ResultOutput.csv` A열(COUNT) 오름차순 정렬
3. `DcJpgRename()` — DC 폴더 JPG 파일명 AC 홈 번호에 맞게 리네임
4. `DistSensingDataSave()` — PLC D6100대 거리 센서 → `SensorData.csv`
5. `GradeCSVCreate()` — 단일치·인접치·누적치·R/OUT 계산 및 `ScoreGrade.csv` 생성
6. `DeletTrashFolder()` — 시행횟수 **999** 폴더(쓰레기통) 삭제
7. 트리거 레지스터 **0**으로 리셋

---

## 기술 스택

| 구분 | 기술 |
|------|------|
| UI | Windows Forms (.NET 10) |
| 이미지 | OpenCvSharp4, OpenCvSharp4.Extensions, OpenCvSharp4.runtime.win |
| 차트 | ScottPlot 5.x (WinForms) |
| JSON | Newtonsoft.Json, System.Text.Json (혼용) |
| PLC | Mitsubishi MX Component — `ActUtlType64Lib` COM |
| 비전 SDK | Euresys Open eVision 25.10 (`Open_eVision_NetApi.dll`, 참조만 존재·런타임 API 미사용) |

**NuGet:** OpenCvSharp4 `4.13.0.20260528`, ScottPlot `5.1.58`, Newtonsoft.Json `13.0.4`

---

## 요구 사항

- Windows 10 이상 (x64 권장)
- [.NET SDK 10](https://dotnet.microsoft.com/) — `global.json` 명시 버전 또는 `rollForward` 호환 버전
- **Visual Studio 2022** (또는 .NET Framework용 MSBuild)  
  PLC COM 참조(`ActUtlType64Lib`) 때문에 `dotnet build`만으로 **MSB4803** 오류가 날 수 있습니다.
- **Euresys Open eVision 25.10** (빌드 참조용)  
  기본 경로: `C:\Program Files\Euresys\Open eVision 25.10\Bin\Open_eVision_NetApi.dll`  
  설치 경로가 다르면 `WIA_ViewerProgram.csproj`의 `HintPath` 수정
- **Mitsubishi MX Component** — 논리 스테이션 번호 = `PLCSetting.json`의 `StationNumber`

---

## 프로젝트 구조

```
hdwia_gear/
├── WIA_ViewerProgram.csproj      # 메인 프로젝트
├── WIA_ViewerProgram.slnx
├── ViewerForm.cs                 # 메인 UI·RECIPE·통계·이미지 표시 (~3,300줄)
├── ViewerForm.Designer.cs        # UI·ScottPlot 컨트롤 (~11,000줄)
├── PLC.cs                        # PLC 모니터링·TXT→CSV·등급 산출 (~2,400줄)
├── PLCSettingForm.*              # PLC 설정 UI
├── Keyence.cs                    # Keyence IP/Port JSON
├── KeyenceTcpReceiver.cs         # Keyence TCP 백그라운드 수신
├── Keyence*Form.*                # Keyence 설정·연결 확인
├── OpenCVManager.cs              # 4×22 그리드 평활화 (GearGridWarpPerspective)
├── DirectoryManager.cs           # FTP(비전 저장) 루트 경로
├── LoginManager.cs               # 로그인·비밀번호 변경
├── HistoryManager.cs             # 파일 로그 (싱글톤)
├── LoginData.json / Directory.json
├── OpenCvSharpExtern.dll         # 출력 폴더로 복사
└── global.json
```

---

## 빌드 및 실행

### Visual Studio (권장)

1. `WIA_ViewerProgram.slnx` 열기
2. Open eVision DLL 경로·MX Component COM 참조 확인
3. **빌드** 후 실행 (시작 폼: `ViewerForm`)

### 명령줄 (COM 환경에서)

```powershell
cd c:\Git\hdwia_gear
dotnet build WIA_ViewerProgram.csproj
dotnet run --project WIA_ViewerProgram.csproj
```

### 배포 (win-x64)

```powershell
dotnet publish WIA_ViewerProgram.csproj -p:PublishProfile=FolderProfile
```

출력: `bin\Release\net10.0-windows7.0\publish\win-x64\`

---

## 설정 파일

프로그램은 **실행 파일 디렉터리**(또는 작업 디렉터리) 기준으로 JSON을 읽고 씁니다. 최초 실행 시 일부 파일은 기본값으로 자동 생성됩니다.

### `LoginData.json`

```json
{
  "LoginData": [
    { "mode": "operator", "userid": "operator", "pw": "0000" },
    { "mode": "admin", "userid": "admin", "pw": "0000" },
    { "mode": "master", "userid": "master", "pw": "0000" }
  ]
}
```

### `Directory.json`

비전 데이터(FTP) 루트 경로. **PLC 모니터링 시작 전 필수.**

```json
{
  "FTP": "D:\\VisionData\\"
}
```

### `PLCSetting.json`

```json
{
  "Ip": "192.168.0.10",
  "StationNumber": 1,
  "MoniteringCycle": 1000,
  "MoniterAdrress": "D6000"
}
```

### `KeyenceSetting.json`

```json
{
  "Ip": "169.254.231.135",
  "PortNumber": 21
}
```

### 코드 내 하드코딩 경로 (배포 시 확인)

`PLC.cs`의 Keyence TXT 소스 경로는 소스에 상수로 지정되어 있습니다.

```csharp
const string FilePath = @"C:\Users\Admin\Documents\Keyence\XG-X VisionTerminal\USB\SD2\Vision\";
```

- `FilePath + "AC"` — 가속(Acceleration) TXT 수신 폴더
- `FilePath + "DC"` — 감속(Deceleration) TXT 수신 폴더

현장 PC 경로에 맞게 `PLC.cs`를 수정해야 합니다.

### 기타 리소스

- `Logo/Nvilogo.jpg` — 상단 로고 (없으면 미표시)
- `캘리브레이션 예시 이미지/` — 캘리브레이션 참고용 샘플

---

## 데이터 폴더 구조

### FTP 루트 (`Directory.json` → `FTP`)

```
{FTP}/
  yyyyMMdd/
    {모델명}/
      {BCR명}/
        {시행번호}/
          Acceleration/     ← UI에서 Front (AC)
            *.jpg
            ResultOutput.csv
            ScoreGrade.csv      ← PLC 등급 산출 결과
          Deceleration/     ← UI에서 Rear (DC)
            *.jpg
            ResultOutput.csv
            ScoreGrade.csv
          SensorData.csv        ← 시행 폴더 루트, 거리 센서값
```

- 시행번호 **999** — 쓰레기통 폴더 (트리거 후 자동 삭제)
- RECIPE **목록** 탭에서 기간·모델 지정 후 스캔, 행 선택 시 `Acceleration` / `Deceleration` 경로 연결

### `ResultOutput.csv` 컬럼 (8열)

| 열 | 내용 |
|----|------|
| 0 | COUNT (홀 번호) |
| 1 | Peak X |
| 2 | Peak Y |
| 3 | Width |
| 4 | Height |
| 5 | Area |
| 6 | Pattern X |
| 7 | Pattern Y |

뷰어에서 `ResultOutput.csv`가 없으면 폴더 내 다른 `.csv`를 병합해 생성하는 `makeResultOutput()`을 시도합니다.

### `ScoreGrade.csv`

PLC `GradeCSVCreate()`가 생성합니다. Peak X/Y, Width, Height, Area별 **단일치·인접치·누적치·R/OUT·등급·점수** 및 Total 점수/등급을 포함합니다.

---

## 화면 네비게이션

| 메뉴 | 패널 |
|------|------|
| TCP/IP | PLC·Keyence 설정, 모니터링 시작/종료, Keyence TCP 수신 |
| Cal Data | 캘리브레이션 이미지 선택, OpenCV 그리드 평활화 미리보기 |
| RECIPE | Recipe 선택 / 목록 / Single·Plural Static / 단일 이미지 검사 |
| Login | 로그인·ID/PW 변경 |
| EXIT | 프로그램 종료 |

### RECIPE 하위 탭

| 탭 | 기능 |
|----|------|
| Recipe 선택 | FTP 루트·기간·모델 설정 |
| 목록 | BCR·시행 폴더 스캔·선택 |
| Single Static | 단일 시행 Peak/Pattern/Width/Height/Area ScottPlot (패널 1~4분할) |
| Plural Static | 복수 시행 통계 (가중치 점수, MAD, AC/DC 점수·MAD 등 7페이지) |
| 단일 이미지 검사 | 선택 행의 Front/Rear 이미지·수치 표시 |

---

## 저장소 클론

```bash
git clone http://10.0.0.20:30080/TaeWon/hdwia_gear.git
cd hdwia_gear
```

---

## 문제 해결

| 증상 | 확인 사항 |
|------|-----------|
| MSB4803 (ResolveComReference) | Visual Studio 또는 .NET Framework MSBuild로 빌드 |
| Open eVision DLL 없음 | Euresys 설치 및 csproj `HintPath` 확인 |
| PLC 모니터링 불가 | `Directory.json` FTP 경로, MX Component 스테이션 번호 |
| 트리거 후 CSV 미생성 | `PLC.cs` Keyence TXT 경로, `AC`/`DC` 폴더 내 `.txt` 존재 여부 |
| Keyence TCP 연결 실패 | `KeyenceSetting.json` IP/Port, 방화벽 |
| 단일 통계 `OverflowException` | `ResultOutput.csv`가 **비어 있음** — 파일은 있으나 유효 행 0건. PLC 파이프라인·원본 CSV 확인 |
| 단일 통계 "CSV 데이터 없음" | 위와 동일. `makeResultOutput`이 빈 파일만 생성한 경우 |
| `ScoreGrade.csv` 없음 | PLC 모니터링 트리거로 `GradeCSVCreate()` 실행 여부 확인 |
| DC 이미지 리네임 실패 | `Deceleration` 폴더 JPG **43장** 여부 (`DcJpgRename`) |

---

## 알려진 제약

- 로그인 비밀번호는 JSON **평문** 저장
- Keyence TXT 경로·기어 홀 개수(43)·DC 오프셋(20) 등이 소스에 하드코딩 — 레시피별 가변화는 추후 과제
- `ViewerForm`·`PLC`에 UI·비즈니스 로직이 집중된 모놀리식 구조
- Open eVision DLL은 빌드 참조만 있으며, 현재 코드에서 API 호출 없음

---

## 라이선스

사내 프로젝트입니다. 외부 배포·라이선스 정책은 저장소 관리자에게 문의하세요.
