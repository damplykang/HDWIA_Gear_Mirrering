using ActUtlType64Lib;
using Newtonsoft.Json.Linq;
using ScottPlot.Colormaps;
using SkiaSharp;
using System.IO;
using System.Text;
using System.Text.Json;
using static WIA_ViewerProgram.HistoryManager;

namespace WIA_ViewerProgram
{
    internal sealed class PLCSettingJson
    {
        public string Ip { get; set; } = "";
        public int StationNumber { get; set; }
        public int MoniteringCycle { get; set; }
        public string MoniterAdrress { get; set; } = "D3000";
    }



    public class LogConfig
    {
        public string FTP { get; set; }

    }
    /// <summary>
    /// PLC 설정 및 MX Component(ActUtlType64Lib) 기반 모니터링.
    /// 논리 스테이션 번호는 MX Component 통신 설정 유틸리티에 등록된 번호와 <see cref="StationNumber"/>를 맞춥니다.
    /// </summary>
    public class PLC
    {
        private const string JsonFileName = "PLCSetting.json";

        private static string JsonPath => Path.Combine(AppContext.BaseDirectory, JsonFileName);
        LogConfig config;

        public string Ip { get; set; } = "";
        public int StationNumber { get; set; }
        public int MoniteringCycle { get; set; }

        public static volatile bool PictureEnd;
        private static ActUtlType64 ActUtlType;
        public bool MonieringCheck;// 연결이 되어 있다면 true 되어있지 않다면 false
        private HistroyManager logger = HistroyManager.Instance;
        private int result;
        private CancellationTokenSource? _monitoringCancellationTokenSource;
        private Task? _monitoringTask;
        int CurrentTrigerValue;

        /// <summary>모니터링할 디바이스 주소 문자열 (예: D3000, M100).</summary>
        public string MoniterAdrress = "D3000";
        private string hex;

        //"C:\\Users\\Admin\\Documents\\Keyence\\XG-X VisionTerminal\\USB\\SD2\\Vision\\"=> 수정해야함
        // 테스트용 경로
        const string FilePath = "C:\\Users\\Admin\\Documents\\Keyence\\XG-X VisionTerminal\\USB\\SD2\\Vision\\";//이폴더에 통합 CSV파일 저장!

        //거리 감지 센서값 저장하기 위해 필요한 변수들
        int SensingAdress = 6100;// 시작주소
        string SensingDataSavePath = "-";
        List<float> sensingDist = new List<float>();

        //시작할때 쓰레기 폴더 시행횟수 999에 있는 모든 데이터를 삭제함 
        string trashfolder="-";

        public PLC()
        {
            PictureEnd = false;
            MoniterAdrress = "D3000";
        }

        public void LoadFromJson()
        {
            ActUtlType = new ActUtlType64();
            hex = "0x";
            result = -1;
            MonieringCheck = false;
            try
            {
                if (!File.Exists(JsonPath))
                {
                    Ip = "";
                    StationNumber = 0;
                    MoniteringCycle = 0;
                    MoniterAdrress = "D3000";
                    logger.LogInfo("PLC", "PLCSetting.json 없음 — 기본값으로 새 파일 생성", "", JsonPath);
                    SaveToJson();
                    return;
                }

                string json = File.ReadAllText(JsonPath);
                var data = JsonSerializer.Deserialize<PLCSettingJson>(json);
                if (data == null)
                {
                    Ip = "";
                    StationNumber = 0;
                    MoniteringCycle = 0;
                    MoniterAdrress = "D3000";
                    logger.LogWarning("PLC", "PLCSetting.json 역직렬화 결과 null — 기본값 사용", "", JsonPath);
                    return;
                }

                Ip = data.Ip ?? "";
                StationNumber = data.StationNumber;
                MoniteringCycle = data.MoniteringCycle;
                MoniterAdrress = string.IsNullOrWhiteSpace(data.MoniterAdrress) ? "D3000" : data.MoniterAdrress.Trim();
                logger.LogInfo("PLC", "PLC 설정 로드 완료", "", $"Ip={Ip}, Station={StationNumber}, Cycle={MoniteringCycle}ms, Address={MoniterAdrress}");
            }
            catch (Exception ex)
            {
                Ip = "";
                StationNumber = 0;
                MoniteringCycle = 0;
                MoniterAdrress = "D3000";
                logger.LogError("PLC", "PLCSetting.json 로드 중 예외 — 기본값 사용", "", $"{ex.Message}");
            }
        }

        public void SaveToJson()
        {
            try
            {
                var data = new PLCSettingJson
                {
                    Ip = Ip,
                    StationNumber = StationNumber,
                    MoniteringCycle = MoniteringCycle,
                    MoniterAdrress = MoniterAdrress
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(JsonPath, json);
                logger.LogInfo("PLC", "PLC 설정 저장 완료", "", JsonPath);
            }
            catch (Exception ex)
            {
                logger.LogError("PLC", "PLC 설정 저장 실패", "", $"{JsonPath} | {ex.Message}");
            }
        }
        ~PLC()
        {
            ActUtlType.Close();

        }


        public void MoniteringStart()
        {
            ActUtlType.ActLogicalStationNumber = StationNumber;// 스테이션 넘버 설정
            result = ActUtlType.Open();//연결시도

            if (MonieringCheck)
            {
                logger.LogWarning("PLC", "모니터링 시작 요청 무시 — 이미 진행 중");
                var mesbox = MessageBox.Show(
                 "모니터링이 이미 진행중입니다.",
                 "모니터링 진행중",
                  MessageBoxButtons.OK,
                 MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string jsonString = File.ReadAllText("./Directory.json");
                config = JsonSerializer.Deserialize<LogConfig>(jsonString);
            }
            catch (Exception ex)
            {
                logger.LogError("PLC", "Directory.json 읽기/역직렬화 실패 — 모니터링을 시작할 수 없습니다", "", ex.Message);
                MessageBox.Show(
                    "Directory.json을 읽을 수 없습니다. FTP 경로 설정을 확인해 주세요.",
                    "설정 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (config == null)
            {
                logger.LogError("PLC", "Directory.json 역직렬화 결과 null — 모니터링 시작 불가");
                MessageBox.Show(
                    "Directory.json 형식이 올바르지 않습니다.",
                    "설정 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            //FTP셋팅이 안되어 있는 경우라면 에러발생
            if (string.IsNullOrWhiteSpace(config.FTP))
            {
                var mesbox = MessageBox.Show(
                "FTP(비전) 저장 경로가 설정되지 않았습니다.\nDirectory.json의 FTP 항목을 확인해 주세요.",
                "FTP 경로 미설정",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
                logger.LogWarning("PLC", "FTP 디렉토리 미설정 — 모니터링 시작 불가", "", "Directory.json FTP 항목이 비어 있음");
                return;
            }

            if (result == 0)//연결되면
            {
                var mesbox = MessageBox.Show(
                "PLC 모니터링을 시작했습니다.",
                "모니터링",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
                MonieringCheck = true;
                logger.LogInfo("PLC", "PLC Open 성공 — 모니터링 루프 시작", "", $"Station={StationNumber}, 트리거주소={MoniterAdrress}, FTP 루트={config.FTP}");
                //
                // CancellationTokenSource 생성
                _monitoringCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _monitoringCancellationTokenSource.Token;


                _monitoringTask = Task.Run(async () =>
                {
                    int previousValue = int.MinValue;
                    int errorCount = 0;
                    const int maxErrorCount = 5; // 연속 오류 허용 횟수

                    logger.LogInfo("PLC", "PLC 모니터링 Task 시작", "", $"모니터링 주기: {MoniteringCycle}ms");

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            int value;
                            result = ActUtlType.GetDevice(MoniterAdrress, out value);

                            if (result == 0)
                            {
                                // 읽기 성공
                                CurrentTrigerValue = value;
                                errorCount = 0; // 오류 카운트 리셋

                                // 값이 변경된 경우에만 로그 기록 (최초 한 번은 항상 기록)
                                if (previousValue != value || previousValue == int.MinValue)
                                {
                                    logger.LogDebug("PLC", $"트리거 레지스터 : {MoniterAdrress}의 메모리 값 변경: {previousValue} → {value}", "", $"Station Number: {ActUtlType.ActLogicalStationNumber}");
                                    previousValue = value;

                                    if (CurrentTrigerValue == 1)
                                    {
                                        logger.LogInfo("PLC", "트리거 레지스터=1 감지 — TXT→ResultOutput.csv 통합 실행", "", $"{MoniterAdrress}");
                                        //지정된 경로에 있는 csv파일을 읽어온다
                                        // csv파일에 저장된 값들을 이용해 경로를 만들고
                                        // 만들어 진 경로에 데이터를 저장한다
                                        // string csvFilename = "\\ResultOutput.csv"; 파일 이름은 이걸로 통일!
                                        //만들어진경로에 csv파일을 데이터별로 저장한다.                                        
                                        //AC작업 
                                        CSVFileCreate("AC");
                                        //DC작업
                                        CSVFileCreate("DC");
                                        //
                                        DistSensingDataSave();
                                        /// 시행횟수 999폴더는 무조건 삭제...
                                        /// 
                                        DeletTrashFolder();

                                       ActUtlType.SetDevice(MoniterAdrress, 0);
                                    }

                                }


                            }
                            else
                            {
                                // 읽기 실패
                                errorCount++;
                                hex = "0x" + result.ToString("X8");

                                logger.LogWarning("PLC", $"{MoniterAdrress} 메모리 읽기 실패 - 오류 코드: {hex}", "", $"Station Number: {ActUtlType.ActLogicalStationNumber}, 연속 오류 횟수: {errorCount}");
                                ActUtlType.SetDevice(MoniterAdrress, 0);
                                // 연속 오류가 너무 많으면 모니터링 중지
                                if (errorCount >= maxErrorCount)
                                {
                                    logger.LogError("PLC", $"{MoniterAdrress} 메모리 읽기 연속 오류 - 모니터링 중지", "", $"오류 코드: {hex}, 연속 오류 횟수: {errorCount}");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            logger.LogError("PLC", $"{MoniterAdrress} 메모리 모니터링 중 예외 발생: {ex.Message}", "", $"Station Number: {ActUtlType.ActLogicalStationNumber}, 연속 오류 횟수: {errorCount}, StackTrace: {ex.StackTrace}");
                            ActUtlType.SetDevice(MoniterAdrress, 0);
                            // 연속 오류가 너무 많으면 모니터링 중지
                            if (errorCount >= maxErrorCount)
                            {
                                logger.LogError("PLC", $"{MoniterAdrress} 메모리 모니터링 연속 예외 - 모니터링 중지", "", $"연속 오류 횟수: {errorCount}");

                                break;
                            }
                        }

                        // 모니터링 주기만큼 대기
                        try
                        {
                            await Task.Delay(MoniteringCycle, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // 정상적인 취소
                            break;
                        }
                    }
                    logger.LogInfo("PLC", "PLC 모니터링 Task 종료", "", $"Station Number: {ActUtlType.ActLogicalStationNumber}");
                    MonieringCheck = false;
                }, cancellationToken);

            }
            else//연결 안되면
            {
                hex = "0x" + result.ToString("X8");
                logger.LogWarning("PLC", "모니터링 시작 불가 — PLC Open 결과가 0이 아님", "", $"오류코드={hex}, StationNumber={StationNumber}, MX Component 논리 스테이션 설정을 확인하세요");
                var mesbox = MessageBox.Show(
                "StationNumber를  확인해주세요",
                "StatinNumber확인",
                 MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
                return;
            }

        }

        /// <summary>모니터링 스레드를 중지합니다.</summary>
        public void MoniteringEnd()
        {
            try
            {
                // 이미 연결되지 않은 경우
                if (!MonieringCheck)
                {
                    logger.LogWarning("PLC", "모니터링 해제 시도 모니터링 미실시 상태");
                    MessageBox.Show(
                        "모니터링이 진행중이지 않습니다.",
                        "연결 상태 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                logger.LogInfo("PLC", $"PLC 연결 해제 시도 시작 - Station Number: {ActUtlType.ActLogicalStationNumber}");

                // PLC 연결 해제 시도
                result = ActUtlType.Close();

                if (result != 0)
                {
                    // 0이 아니면 연결 해제 실패
                    hex = "0x" + result.ToString("X8");

                    string errorMessage = $"PLC 연결 해제 실패 - 오류 코드: {hex}, Station Number: {ActUtlType.ActLogicalStationNumber}";
                    logger.LogError("PLC", errorMessage, "", $"오류 코드: {hex}, Result: {result}");

                    MessageBox.Show(
                        $"PLC 연결 해제에 실패했습니다.\n오류 코드: {hex}",
                        "연결 해제 실패",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    throw new Exception($"PLC 연결 해제에 실패했습니다. 오류 코드: {hex}");
                }
                else
                {
                    // 0이면 연결 해제 성공
                    MonieringCheck = false;

                    if (_monitoringCancellationTokenSource != null && !_monitoringCancellationTokenSource.IsCancellationRequested)
                    {
                        _monitoringCancellationTokenSource.Cancel();
                    }
                    if (_monitoringCancellationTokenSource != null)
                    {
                        _monitoringCancellationTokenSource.Dispose();
                        _monitoringCancellationTokenSource = null;
                    }

                    logger.LogInfo("PLC", $"모니터링 해제 성공 - Station Number: {ActUtlType.ActLogicalStationNumber}");

                    MessageBox.Show(
                        $"Station Number {ActUtlType.ActLogicalStationNumber}와 연결이 해제되었습니다.",
                        "연결 해제 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // 예외 발생 시에도 연결 상태는 false로 설정
                MonieringCheck = false;
                logger.LogError("PLC", $"PLC 연결 해제 중 예외 발생: {ex.Message}", "", $"Station Number: {ActUtlType.ActLogicalStationNumber}, StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"PLC 연결 해제 중 오류가 발생했습니다.\n{ex.Message}",
                    "연결 해제 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                throw;
            }
        }

        public void CSVFileCreate(string CameraInfo)
        {
            string TXTfilePath = FilePath + CameraInfo;
            int linesProcessed = 0;
            int linesSkippedBadFormat = 0;
            int rowsAppended = 0;
            int txtFilesProcessed = 0;

            try
            {
                if (Directory.Exists(TXTfilePath))
                {
                    string[] files = Directory.GetFiles(TXTfilePath, "*.txt");

                    if (files.Length == 0)
                    {
                        logger.LogWarning("PLC", "TXT 소스 파일 없음 (트리거 처리)", "", $"경로={TXTfilePath}, 구역={CameraInfo}");
                        return;
                    }

                    foreach (string filePath in files)
                    {   //파일 하나씩 읽어서 다읽고 생성
                        foreach (string line in File.ReadLines(filePath))
                        {
                            // 여기서 각 줄(line)을 처리합니다.
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                linesProcessed++;
                                string[] values = line.Split(',');
                                const int minCols = 15; // 0~6 메타 + 데이터 8컬럼(7~14)
                                if (values.Length < minCols)
                                {
                                    linesSkippedBadFormat++;
                                    logger.LogWarning("PLC", "TXT 라인 컬럼 수 부족 — 건너뜀", "", $"file={filePath}, columns={values.Length}, need>={minCols}, 구역={CameraInfo}");
                                    continue;
                                }

                                string date = values[0] + values[1] + values[2]; //날짜
                                string model = values[3]; //모델 명
                                string bcr = values[4]; // bcr
                                string runcount = values[5]; //시행횟수
                                string geartype = values[6]; // ac/dc
                                string[] selectedValues = values.Skip(7).Take(8).ToArray();
                                string csvContent = string.Join(",", selectedValues);
                                //config.FTP를 활용해서 
                                string CSVPath = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + runcount + "\\" + geartype;
                                SensingDataSavePath = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + runcount;// 거리감지 센서를 져장하기 위한 패스
                                
                                string fullPath = Path.Combine(CSVPath, "ResultOutput.csv");
                                if (int.Parse(runcount)<999) { 
                                Directory.CreateDirectory(CSVPath);
                                using (StreamWriter sw = new StreamWriter(fullPath, append: true))
                                {
                                    sw.WriteLine(csvContent);
                                }

                                logger.LogInfo("Data", $" 데이터 저장 경로 {fullPath} / 데이터 : {selectedValues[0]},{selectedValues[1]},{selectedValues[2]}, {selectedValues[3]} etc");
                                } 
                                else
                                {
                                    trashfolder = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + runcount;// 999가 라는 폴더가 있다면 업데이트
                                    //runcount ==999는 쓰레기통이므로 아무작업 안함
                                }
                                rowsAppended++;
                            }
                        }

                        //작업이 끝나면 해당 파일 삭제!
                        File.Delete(filePath);
                        txtFilesProcessed++;
                        logger.LogInfo("PLC", "TXT 소스 파일 처리 후 삭제", "", filePath);
                    }

                    logger.LogInfo("PLC", "TXT→FTP CSV 통합 요약", "", $"구역={CameraInfo}, txt파일={txtFilesProcessed}개, 라인처리={linesProcessed}, CSV누적기록={rowsAppended}, 형식오류={linesSkippedBadFormat}");
                }
                else
                {
                    logger.LogWarning("PLC", "TXT 소스 폴더 없음 (트리거 처리)", "", $"경로={TXTfilePath}, 구역={CameraInfo}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("PLC", $"CSVFileCreate 예외 — 구역={CameraInfo}", "", $"{ex.Message} | {ex.StackTrace}");
            }


        }

        public void DistSensingDataSave()
        {
            int count = 1;
            int startAddress = SensingAdress;
            string fullPath = Path.Combine(SensingDataSavePath, "SensorData.csv");

            try
            {
                // [개선 2] 폴더 생성은 루프 밖에서 단 한 번만!
                Directory.CreateDirectory(SensingDataSavePath);

                // [개선 2] 파일 스트림을 루프 밖에서 열어 성능 최적화
                using (StreamWriter sw = new StreamWriter(fullPath, append: true))
                {
                    while (true)
                    {
                        int value1, value2, value3;

                        // [개선 5] 개별 GetDevice 대신 Block으로 한 번에 읽으면 훨씬 좋으나, 
                        // 기존 구조를 유지한다면 최소한 아래와 같이 에러 체크가 필요합니다.
                        int r1 = ActUtlType.GetDevice($"D{startAddress}", out value1);
                        int r2 = ActUtlType.GetDevice($"D{startAddress + 2}", out value2);
                        int r3 = ActUtlType.GetDevice($"D{startAddress + 4}", out value3);

                        // [개선 3] 통신 에러 발생 시 처리 (0이 아니면 에러)
                        if (r1 != 0 || r2 != 0 || r3 != 0)
                        {
                            // 로그를 남기거나 사용자 알림 후 루프 탈출
                            logger.LogError("PLC", $"통신에러 : (D{startAddress}번 :{r1}),(D{startAddress + 2}번 :{r2}),(D{startAddress + 4}번 :{r3})");
                            break;
                        }

                        // [개선 1] 정확한 종료 조건 검사 (합산이 아닌 각각 0인지 확인)
                        if (value1 == 0 && value2 == 0 && value3 == 0)
                        {
                            if (count>1) {
                                int last;
                                ActUtlType.GetDevice($"D{startAddress - 2}", out last);
                                logger.LogError("PLC", $" PLC읽기 종료 : D{startAddress - 2}어드레스에서 종료 됨 값: {last} ");
                                break;
                            }
                            else
                            {   //처음부터 조질경우        
                                logger.LogError("PLC", $" PLC읽기 종료 : D{startAddress}어드레스에서 종료 됨 값: {value1} ");
                            }
                        }

                        if (count==44)
                        {
                            //홈개수는 반드시 43개!
                            logger.LogInfo("PLC", $" PLC읽기 종료 총 43개의 데이터 읽기 완료 : 저장 경로 {fullPath} ");
                            break;
                        }

                        // CSV 데이터 행 작성
                        short temp = (short)value1;
                        sw.WriteLine($"{count},{temp / (float)1000}");

                        logger.LogInfo("PLC", $" PLC읽기 및 저장 완료 / 저장 경로 {fullPath} / 홈번호: {count} / data :{temp / (float)1000} ");

                        // 주소 및 카운트 증가
                        startAddress += 2;
                        count++;
                    }
                } // using 블록을 나가면서 파일이 안전하게 닫힙니다 (Close 자동 호출)
            }
            catch (Exception ex)
            {
                // 파일 쓰기 권한 오류, 경로 오류 등 예외 처리
                logger.LogError("PLC", $"오류 발생: {ex.Message}");
            }
        }



        public void DeletTrashFolder()
        {
            try
            {
                //trashfolder를 삭제 시도
                if (Directory.Exists(trashfolder))
                {
                    // 2. 폴더 삭제 (두 번째 인자를 true로 주어야 하위 파일 및 폴더까지 전부 삭제됩니다)
                    Directory.Delete(trashfolder, true);
                    logger.LogInfo("폴더 삭제 성공",$"폴더 경로 : {trashfolder}");
                }
                else
                {
                    logger.LogInfo("폴더 삭제 실패", $"폴더가 존재하지 않습니다. 폴더 경로 : {trashfolder}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // 권한이 없는 경우 예외 처리 (예: 관리자 권한 필요, 읽기 전용 등)
                logger.LogError("폴더 삭제 실패", $"권한이 없어 폴더를 삭제할 수 없습니다: {ex.Message} 폴더 경로 : {trashfolder}");
            }
            catch (IOException ex)
            {
                // 폴더 내 파일이 다른 프로그램에서 사용 중이거나 lock이 걸린 경우 예외 처리
                logger.LogError("폴더 삭제 실패", $"폴더가 사용 중이거나 입출력 오류가 발생했습니다: {ex.Message} 폴더 경로 : {trashfolder}");
            }
            catch (Exception ex)
            {
                // 그 외 예상치 못한 기타 예외 처리
                logger.LogError("폴더 삭제 실패", $"폴더 삭제중 오류가 발생했습니다: {ex.Message} 폴더 경로 : {trashfolder}");
            }
        }
    }
}
