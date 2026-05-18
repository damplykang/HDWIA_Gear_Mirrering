# HDWIA_Gear (WIA Viewer)

기어(GEAR) 비전 검사 데이터를 조회·분석하는 Windows Forms 데스크톱 뷰어입니다. Mitsubishi PLC 모니터링, Keyence TCP 연동, Open eVision / OpenCV 기반 이미지 처리, Front/Rear 검사 결과 시각화를 하나의 화면(1920×1080)에서 제공합니다.

## 주요 기능

| 영역 | 설명 |
|------|------|
| **로그인** | Operator / Admin / Master 역할별 접근 (`LoginData.json`) |
| **TCP/IP** | PLC 설정·모니터링, Keyence IP/Port 설정, TCP 수신·연결 확인 |
| **RECIPE** | 모델·날짜별 BCR 폴더 목록, 단일/복수 통계(ScottPlot), 단일 이미지 검사 |
| **Cal Data** | Front/Rear 캘리브레이션 이미지 선택, OpenCV 그리드(44점) 워핑 |
| **Front / Rear** | 검사 이미지·좌표·각도·치수 표시, `ResultOutput.csv` 생성·조회 |
| **로그** | `Logs/HDMIndoPE_yyyyMMdd.log` (회전·크기 제한) |

## 기술 스택

- **.NET** `net10.0-windows7.0` (Windows Forms)
- **OpenCvSharp4** — 이미지 로드, 마우스 그리드 지정, Perspective 변환
- **ScottPlot** — 통계 차트
- **Euresys Open eVision 25.10** — `Open_eVision_NetApi.dll` (별도 설치 필요)
- **Mitsubishi MX Component** — `ActUtlType64Lib` COM (PLC 통신)
- **Newtonsoft.Json** — 설정·로그인 JSON

## 요구 사항

- Windows 10 이상 (x64 권장)
- [.NET SDK 10](https://dotnet.microsoft.com/) — 저장소 `global.json`에 명시된 버전 또는 `rollForward` 호환 버전
- **Visual Studio 2022** (또는 .NET Framework용 MSBuild) — PLC용 COM 참조(`ActUtlType64Lib`) 빌드 시 필요  
  `dotnet build`만으로는 `ResolveComReference` 오류(MSB4803)가 날 수 있습니다.
- **Euresys Open eVision 25.10** 설치  
  기본 참조 경로: `C:\Program Files\Euresys\Open eVision 25.10\Bin\Open_eVision_NetApi.dll`  
  설치 경로가 다르면 `WIA_ViewerProgram.csproj`의 `HintPath`를 수정하세요.
- **Mitsubishi MX Component** — 논리 스테이션 번호가 `PLCSetting.json`의 `StationNumber`와 일치해야 합니다.

## 프로젝트 구조

```
hdwia_gear/
├── WIA_ViewerProgram.csproj   # 메인 프로젝트
├── WIA_ViewerProgram.slnx
├── ViewerForm.cs              # 메인 UI·업무 로직
├── PLC.cs / PLCSettingForm.*  # PLC 모니터링·설정
├── Keyence.cs / KeyenceTcpReceiver.cs / Keyence*Form.*
├── OpenCVManager.cs           # 캘리브레이션·그리드 워핑
├── DirectoryManager.cs        # FTP(비전 저장) 경로
├── LoginManager.cs
├── HistoryManager.cs
├── OpenCvSharpExtern.dll      # 출력 폴더로 복사됨
└── global.json
```

## 빌드 및 실행

### Visual Studio

1. `WIA_ViewerProgram.slnx` 열기
2. Open eVision DLL 경로·MX Component COM 참조 확인
3. **빌드** 후 실행 (시작 폼: `ViewerForm`)

### 명령줄 (COM 참조 환경에서)

```powershell
cd c:\Git\hdwia_gear
dotnet build WIA_ViewerProgram.csproj
dotnet run --project WIA_ViewerProgram.csproj
```

### 단일 파일 배포 (win-x64)

```powershell
dotnet publish WIA_ViewerProgram.csproj -p:PublishProfile=FolderProfile
```

출력: `bin\Release\net10.0-windows7.0\publish\win-x64\`

## 실행 시 설정 파일

프로그램은 **실행 파일이 있는 디렉터리**(또는 작업 디렉터리) 기준으로 JSON을 읽고 씁니다. 최초 실행 시 일부 파일은 기본값으로 자동 생성됩니다.

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

비전 데이터(FTP) 루트 경로입니다. PLC 모니터링 시작 전에 반드시 설정해야 합니다.

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
  "MoniterAdrress": "D3000"
}
```

### `KeyenceSetting.json`

```json
{
  "Ip": "169.254.231.135",
  "PortNumber": 21
}
```

### 기타

- `Logo/Nvilogo.jpg` — 상단 로고(없으면 미표시)
- 검사 결과 CSV: 각 시행 폴더 내 `ResultOutput.csv`

## 데이터 폴더 구조 (개요)

FTP 루트 아래 대략 다음과 같이 날짜·BCR·시행 횟수 폴더가 구성됩니다.

```
{FTP}/
  yyyyMMdd/
    {BCR명}/
      {시행번호}/
        Front/   … jpg, ResultOutput.csv
        Rear/    … jpg, ResultOutput.csv
```

RECIPE **목록** 탭에서 기간·모델을 지정해 위 구조를 스캔하고, 행을 선택하면 Front/Rear 경로가 연결됩니다.

## 화면 네비게이션

| 메뉴 | 패널 |
|------|------|
| TCP/IP | PLC·Keyence 설정, 모니터링 시작/종료, Keyence TCP 수신 |
| Cal Data | 캘리브레이션 이미지·그리드 포인트 설정 |
| RECIPE | Recipe 선택 / 목록 / Single·Plural Static / 단일 이미지 검사 |
| Login | 로그인·ID/PW 변경 |
| EXIT | 프로그램 종료 |

로그인하지 않은 상태에서는 RECIPE·TCP/IP 등 주요 기능이 제한됩니다.

## 저장소

```text
http://10.0.0.20:30080/TaeWon/hdwia_gear.git
```

```bash
git clone http://10.0.0.20:30080/TaeWon/hdwia_gear.git
cd hdwia_gear
```

## 문제 해결

| 증상 | 확인 사항 |
|------|-----------|
| MSB4803 (ResolveComReference) | Visual Studio 또는 .NET Framework MSBuild로 빌드 |
| Open eVision DLL 없음 | Euresys 설치 및 csproj `HintPath` |
| PLC 모니터링 불가 | `Directory.json` FTP 경로, MX Component 스테이션 번호 |
| Keyence TCP 연결 실패 | `KeyenceSetting.json` IP/Port, 방화벽 |

## 라이선스

사내 프로젝트입니다. 외부 배포·라이선스 정책은 저장소 관리자에게 문의하세요.
