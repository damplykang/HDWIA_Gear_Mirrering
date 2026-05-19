using ActUtlType64Lib;
using Euresys.Open_eVision;
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
        string AC_CSVFullPath = "-";
        string DC_CSVFullPath = "-";

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
                                        DcDataSort(); //Dc데이터 정렬하기
                                        
                                        DcJpgRename();
                                        DistSensingDataSave();//=> 센싱 데이터 저장 
                                        
                                        //AC/ DC의 RESULT OUTPUT 데이터에서 PEAK X , PEAK Y, WIDTH, HEIGHT, AREA에 대해서 
                                        //단일치, 인접치, 누적치, R/OUT을 우선 계산
                                        // 이후 등급 계산 
                                        GradeCSVCreate();
                                        /// 시행횟수 999폴더는 무조건 삭제...
                                        DeletTrashFolder();
                                        DC_CSVFullPath = "-";
                                        AC_CSVFullPath = "-"; 

                                      

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

        //ac/ dc 데이터를 만들어서 저장해야함
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
                        logger.LogWarning("PLC", "txt 소스 파일 없음 (트리거 처리)", "", $"경로={TXTfilePath}, 구역={CameraInfo}");
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
                                string folderNUM = values[5]; //시행횟수
                                string geartype = values[6]; // ac/dc 타입
                                //감속쪽 데이터는 이후에도 맞춰줘야함
                                // ---------------- 여기나오는 20 , 43은 txt파일을 읽어서 만드는거기 때문에 
                                //모든 데이터를 읽어 올순 없고 나중에 레시피마다 기어개수가 달라지는 것을 고려하여 수정해주자
                                if(CameraInfo == "DC")//DC의 홈과 AC의 홈의 데이터를 맞추기 위한 작업
                                {
                                    values[7] = (((int.Parse(values[7])+20)%43)+1).ToString();
                                }

                                string[] selectedValues = values.Skip(7).Take(8).ToArray();
                                string csvContent = string.Join(",", selectedValues);
                                //config.FTP를 활용해서 
                                string CSVPath = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + folderNUM + "\\" + geartype;
                                SensingDataSavePath = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + folderNUM;// 거리감지 센서를 져장하기 위한 패스
                                
                                string fullPath = Path.Combine(CSVPath, "ResultOutput.csv");
                                if (CameraInfo=="AC")
                                {
                                    AC_CSVFullPath = CSVPath;
                                }
                                else if(CameraInfo == "DC")
                                {
                                    DC_CSVFullPath = CSVPath;
                                }
                                if (int.Parse(folderNUM) <999) { //폴더 번호가 999 아니라면
                                    Directory.CreateDirectory(CSVPath);
                                    using (StreamWriter sw = new StreamWriter(fullPath, append: true))
                                    {
                                    sw.WriteLine(csvContent);
                                    }    
                                    logger.LogInfo("Data", $"{CameraInfo} 데이터 저장 경로 {fullPath} \n 데이터 : {selectedValues[0]},{selectedValues[1]},{selectedValues[2]}, {selectedValues[3]}...etc");
                                } 
                                else
                                {
                                    trashfolder = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + folderNUM;// 999가 라는 폴더가 있다면 업데이트
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
                    logger.LogInfo("트래쉬 폴더{999} 폴더 삭제 성공",$"폴더 경로 : {trashfolder}");
                }
                else
                {
                    logger.LogInfo("트래쉬 폴더{999} 폴더 삭제 실패", $"폴더가 존재하지 않습니다. 폴더 경로 : {trashfolder}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // 권한이 없는 경우 예외 처리 (예: 관리자 권한 필요, 읽기 전용 등)
                logger.LogError("트래쉬 폴더{999} 폴더 삭제 실패", $"권한이 없어 폴더를 삭제할 수 없습니다: {ex.Message} 폴더 경로 : {trashfolder}");
            }
            catch (IOException ex)
            {
                // 폴더 내 파일이 다른 프로그램에서 사용 중이거나 lock이 걸린 경우 예외 처리
                logger.LogError("트래쉬 폴더{999} 폴더 삭제 실패", $"폴더가 사용 중이거나 입출력 오류가 발생했습니다: {ex.Message} 폴더 경로 : {trashfolder}");
            }
            catch (Exception ex)
            {
                // 그 외 예상치 못한 기타 예외 처리
                logger.LogError("트래쉬 폴더{999} 삭제 실패", $"폴더 삭제중 오류가 발생했습니다: {ex.Message} 폴더 경로 : {trashfolder}");
            }
        }


        public void GradeCSVCreate()
        {
            //AC_CSVFullPath;
            //DC_CSVFullPath;
            //AC_CSVFullPath 이경로 안에 각각 ResultOutput.csv파일이 들어 있음 
            // 만약 데이터가 없거나, 파일이 없다면 에러처리하고 log 남기자!
            //csv 파일 라인별로 읽어 오기! 
            // grade에 관련된 내용 계산해야함

            //여기는 이후에 삭제 된다!
            //AC_CSVFullPath = "C:\\Users\\dampl\\OneDrive\\Desktop\\Vision\\20260513\\WRRG3ICEOPEN8025TR3909\\JX1260123179A27-7-3-7-0-\\02\\Acceleration";
            //DC_CSVFullPath = "C:\\Users\\dampl\\OneDrive\\Desktop\\Vision\\20260513\\WRRG3ICEOPEN8025TR3909\\JX1260123179A27-7-3-7-0-\\02\\Deceleration";

            double AC_FinalScore;
            int AC_FinalGrade;

            double DC_FinalScore;
            int DC_FinalGrade;


            if (File.Exists(AC_CSVFullPath+ "\\ResultOutput.csv"))
            {
                //AC 파일이 제대로 만들어져서 존재 하는 경우
                string firstLine = File.ReadLines(AC_CSVFullPath+ "\\ResultOutput.csv").FirstOrDefault();

                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    // 파일은 있지만 내용이 비어 있는 겨우
                    logger.LogError("CSV", $"AC - ResultOutput.csv 파일이 비어 있습니다.  \n파일 경로 :{AC_CSVFullPath+ "\\ResultOutput.csv"}");
                }
                else
                {
                    //파일도있고 내부에 내용도 있는 경우
                    // 라인들을 읽어서 새로운 파일들 만들어야함
                    double[] sum = new double[5]; 
                    string[] lines = File.ReadAllLines(AC_CSVFullPath + "\\ResultOutput.csv");
                    List <double> Peakx_total = new List<double>();
                    List<double> Peaky_total = new List<double>();
                    List<double> Width_total = new List<double>();
                    List<double> Height_total = new List<double>();
                    List<double> Area_total = new List<double>();
                    int DataMaxCount = lines.Length; // 기어마다 개수가 달라지므로!

                    foreach (string line in lines)
                    {
                        string[] values = line.Split(',');
                        Peakx_total.Add(double.Parse(values[1]));
                        Peaky_total.Add(double.Parse(values[2]));
                        Width_total.Add(double.Parse(values[3]));
                        Height_total.Add(double.Parse(values[4]));
                        Area_total.Add(double.Parse(values[5]));
                        sum[0] += double.Parse(values[1]); // peak x
                        sum[1] += double.Parse(values[2]); // peak y
                        sum[2] += double.Parse(values[3]); // width
                        sum[3] += double.Parse(values[4]); // height
                        sum[4] += double.Parse(values[5]); // area
                    }
                    
                    double peakx_avg = sum[0] / (double)DataMaxCount;
                    double peaky_avg = sum[1] / (double)DataMaxCount;
                    double Width_avg = sum[2] / (double)DataMaxCount;
                    double Height_avg = sum[3] / (double)DataMaxCount;
                    double Area_avg = sum[4] / (double)DataMaxCount;

                    //----------------peakx에 대한 단일치 인접치 누적치 r/out 구하기--------------------
                    double Peakx_Max= -1;//peakx의 단일치
                    double Peakx_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_pkakx_data = Peakx_total[0];
                    double Peakx_nugeock = 0; //peak 누적치;
                    double Peakx_MaxDATA = -1;
                    double Peakx_MinDATA = 99999;
                    foreach (double pkakx_data in Peakx_total)
                    {
                        //단일치 구하기
                        if (Peakx_Max <= Math.Abs(pkakx_data - peakx_avg))
                        {
                            Peakx_Max = Math.Abs(pkakx_data - peakx_avg);
                        }
                        //인접치 구하기
                        if (Peakx_MaxInterval <= Math.Abs(pkakx_data - Pre_pkakx_data))
                        {
                            Peakx_MaxInterval = Math.Abs(pkakx_data - Pre_pkakx_data);
                        }
                        Pre_pkakx_data = pkakx_data;

                        //R/OUT 구하기 위한 사전 준비
                        if (Peakx_MaxDATA < pkakx_data)
                        {
                            Peakx_MaxDATA = pkakx_data;
                        }
                        if (Peakx_MinDATA > pkakx_data)
                        {
                            Peakx_MinDATA = pkakx_data;
                        }

                        Peakx_nugeock += pkakx_data * pkakx_data;
                    }
                    Peakx_nugeock /= (double)DataMaxCount ;
                    Peakx_nugeock = Math.Sqrt(Peakx_nugeock);
                    //R/OUT 구하기
                    double Peakx_ROUT = Peakx_MaxDATA - Peakx_MinDATA;
                    
                    //--------------peak y에 대한 값 단일치/ 인접치 / 누적치/ rout 구하기------------------
                    double Peaky_Max = -1;//peakx의 단일치
                    double Peaky_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_pkaky_data = Peaky_total[0];
                    double Peaky_nugeock = 0; //peak 누적치;
                    double Peaky_MaxDATA = -1;
                    double Peaky_MinDATA = 99999;
                    foreach (double peaky_data in Peaky_total)
                    {
                        //단일치 구하기
                        if (Peaky_Max <= Math.Abs(peaky_data - peaky_avg))
                        {
                            Peaky_Max = Math.Abs(peaky_data - peaky_avg);
                        }
                        //인접치 구하기
                        if (Peaky_MaxInterval <= Math.Abs(peaky_data - Pre_pkaky_data))
                        {
                            Peaky_MaxInterval = Math.Abs(peaky_data - Pre_pkaky_data);
                        }
                        Pre_pkaky_data = peaky_data;
                        //R/OUT 구하기 위한 사전 준비
                        if (Peaky_MaxDATA < peaky_data)
                        {
                            Peaky_MaxDATA = peaky_data;
                        }
                        if (Peaky_MinDATA > peaky_data)
                        {
                            Peaky_MinDATA = peaky_data;
                        }
                        Peaky_nugeock += peaky_data * peaky_data;
                    }
                    Peaky_nugeock /= (double)DataMaxCount;
                    Peaky_nugeock = Math.Sqrt(Peaky_nugeock);
                    double Peaky_ROUT = Peaky_MaxDATA - Peaky_MinDATA;

                    //--------------Width에 대한 값 단일치/ 인접치 / 누적치/ rout 구하기-------------------
                    double Width_Max = -1;//peakx의 단일치
                    double Width_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_Width_data = Width_total[0];
                    double Width_nugeock = 0; //peak 누적치;
                    double Width_MaxDATA = -1;
                    double Width_MinDATA = 99999;
                    foreach (double Width_data in Width_total)
                    {
                        //단일치 구하기
                        if (Width_Max <= Math.Abs(Width_data - Width_avg))
                        {
                            Width_Max = Math.Abs(Width_data - Width_avg);
                        }
                        //인접치 구하기
                        if (Width_MaxInterval <= Math.Abs(Width_data - Pre_Width_data))
                        {
                            Width_MaxInterval = Math.Abs(Width_data - Pre_Width_data);
                        }
                        Pre_Width_data = Width_data;
                  
                        //R/OUT 구하기 위한 사전 준비
                        if (Width_MaxDATA < Width_data)
                        {
                            Width_MaxDATA = Width_data;
                        }
                        if (Width_MinDATA > Width_data)
                        {
                            Width_MinDATA = Width_data;
                        }
                        Width_nugeock += Width_data * Width_data;
                    }
                    Width_nugeock /= (double)DataMaxCount;
                    Width_nugeock = Math.Sqrt(Width_nugeock);
                    //R/OUT 구하기
                    double Width_ROUT = Width_MaxDATA - Width_MinDATA;


                    //--------------height 에대한 단일/ 인점 /누적 /rout 구하기---------------------
                    double Height_Max = -1;//peakx의 단일치
                    double Height_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_Height_data = Height_total[0];
                    double Height_nugeock = 0; //peak 누적치;
                    double Height_MaxDATA = -1;
                    double Height_MinDATA = 99999;
                    foreach (double Height_data in Height_total)
                    {
                        //단일치 구하기
                        if (Height_Max <= Math.Abs(Height_data - Height_avg))
                        {
                            Height_Max = Math.Abs(Height_data - Height_avg);
                        }
                        //인접치 구하기
                        if (Height_MaxInterval <= Math.Abs(Height_data - Pre_Height_data))
                        {
                            Height_MaxInterval = Math.Abs(Height_data - Pre_Height_data);
                        }
                        Pre_Height_data = Height_data;
                        //R/OUT 구하기 위한 사전 준비
                        if (Height_MaxDATA < Height_data)
                        {
                            Height_MaxDATA = Height_data;
                        }
                        if (Height_MinDATA > Height_data)
                        {
                            Height_MinDATA = Height_data;
                        }
                        Height_nugeock += Height_data * Height_data;
                    }
                    Height_nugeock /= (double)DataMaxCount;
                    Height_nugeock = Math.Sqrt(Height_nugeock);
                    //R/OUT 구하기
                    double Height_ROUT = Height_MaxDATA - Height_MinDATA;

                    //--------------------area에 대한 단일/ 인접 /누적 /rout 구하기-------------------
                    double Area_Max = -1;//peakx의 단일치
                    double Area_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_Area_data = Area_total[0];
                    double Area_nugeock = 0; //peak 누적치;
                    double Area_MaxDATA = -1;
                    double Area_MinDATA = 99999;
                    foreach (double Area_data in Area_total)
                    {
                        //단일치 구하기
                        if (Area_Max <= Math.Abs(Area_data - Area_avg))
                        {
                            Area_Max = Math.Abs(Area_data - Area_avg);
                        }
                        //인접치 구하기
                        if (Area_MaxInterval <= Math.Abs(Area_data - Pre_Area_data))
                        {
                            Area_MaxInterval = Math.Abs(Area_data - Pre_Area_data);
                        }
                        Pre_Area_data = Area_data;

                        //R/OUT 구하기 위한 사전 준비
                        if (Area_MaxDATA < Area_data)
                        {
                            Area_MaxDATA = Area_data;
                        }
                        if (Area_MinDATA > Area_data)
                        {
                            Area_MinDATA = Area_data;
                        }
                        Area_nugeock += Area_data * Area_data;
                    }
                    Area_nugeock /= (double)DataMaxCount;
                    Area_nugeock = Math.Sqrt(Area_nugeock);
                    //R/OUT 구하기
                    double Area_ROUT = Area_MaxDATA - Area_MinDATA;


                    //--------------------------------peakx의 스코어 계산!-------------------------------------------------
                    // peakx의 스코어 계산!
                    double peakx_Max_score = 0;
                    if (Peakx_Max>= 38.1)
                    {
                        peakx_Max_score = 0.2 * 60;
                    }
                    else if (Peakx_Max>=33.2)
                    {
                        peakx_Max_score = 0.2 * 70;
                    }
                    else if (Peakx_Max >= 28.9)
                    {
                        peakx_Max_score = 0.2 * 80;
                    }
                    else if (Peakx_Max >= 25.1)
                    {
                        peakx_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        peakx_Max_score = 0.2 * 100;
                    }
                    double Peakx_MaxInterval_score = 0;
                    if (Peakx_MaxInterval >= 17.3)
                    {
                        Peakx_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Peakx_MaxInterval >= 15.8)
                    {
                        Peakx_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Peakx_MaxInterval >= 14.4)
                    {
                        Peakx_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Peakx_MaxInterval >= 13.1)
                    {
                        Peakx_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Peakx_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK X 누적치
                    double Peakx_nugeock_score = 0;
                    if (Peakx_nugeock >= 12.2)
                    {
                        Peakx_nugeock_score = 0.4 * 60;
                    }
                    else if (Peakx_nugeock >= 10.2)
                    {
                        Peakx_nugeock_score = 0.4 * 70;
                    }
                    else if (Peakx_nugeock >= 8.5)
                    {
                        Peakx_nugeock_score = 0.4 * 80;
                    }
                    else if (Peakx_nugeock >= 7.1)
                    {
                        Peakx_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Peakx_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Peakx_ROUT_score = 0.0;
                    if (Peakx_ROUT >= 54.7)
                    {
                        Peakx_ROUT_score = 0.1 * 60;
                    }
                    else if (Peakx_ROUT >= 49.7)
                    {
                        Peakx_ROUT_score = 0.1 * 70;
                    }
                    else if (Peakx_ROUT >= 45.2)
                    {
                        Peakx_ROUT_score = 0.1 * 80;
                    }
                    else if (Peakx_ROUT >= 40.1)
                    {
                        Peakx_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Peakx_ROUT_score = 0.1 * 100;
                    }
                    double Peakx_FinalScore = peakx_Max_score + Peakx_MaxInterval_score + Peakx_nugeock_score + Peakx_ROUT_score;
                    string Peakx_Grade;
                    if (Peakx_FinalScore >= 96)
                    {
                        Peakx_Grade = "A";
                    }else if (Peakx_FinalScore >= 91)
                    {
                        Peakx_Grade = "B";
                    }
                    else if (Peakx_FinalScore >= 86)
                    {
                        Peakx_Grade = "C";
                    }
                    else if (Peakx_FinalScore >= 81)
                    {
                        Peakx_Grade = "D";
                    }
                    else{
                        Peakx_Grade = "E";
                    }

                    //--------------------------------peaky의 스코어 계산!-------------------------------------------------
                    double peaky_Max_score = 0;
                    if (Peaky_Max >= 9.2)
                    {
                        peaky_Max_score = 0.2 * 60;
                    }
                    else if (Peaky_Max >= 8.0)
                    {
                        peaky_Max_score = 0.2 * 70;
                    }
                    else if (Peaky_Max >= 7.0)
                    {
                        peaky_Max_score = 0.2 * 80;
                    }
                    else if (Peakx_Max >= 6.1)
                    {
                        peaky_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        peaky_Max_score = 0.2 * 100;
                    }

                    double Peaky_MaxInterval_score = 0;
                    if (Peaky_MaxInterval >= 4.4)
                    {
                        Peaky_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Peaky_MaxInterval >= 3.7)
                    {
                        Peaky_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Peaky_MaxInterval >= 3.4)
                    {
                        Peaky_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Peaky_MaxInterval >= 3.1)
                    {
                        Peaky_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Peaky_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Peaky_nugeock_score = 0;
                    if (Peaky_nugeock >= 3.4)
                    {
                        Peaky_nugeock_score = 0.4 * 60;
                    }
                    else if (Peaky_nugeock >= 2.8)
                    {
                        Peaky_nugeock_score = 0.4 * 70;
                    }
                    else if (Peaky_nugeock >= 2.4)
                    {
                        Peaky_nugeock_score = 0.4 * 80;
                    }
                    else if (Peaky_nugeock >= 2.0)
                    {
                        Peaky_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Peaky_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Peaky_ROUT_score = 0.0;
                    if (Peaky_ROUT >= 13.4)
                    {
                        Peaky_ROUT_score = 0.1 * 60;
                    }
                    else if (Peaky_ROUT >= 12.2)
                    {
                        Peaky_ROUT_score = 0.1 * 70;
                    }
                    else if (Peaky_ROUT >= 11.1)
                    {
                        Peaky_ROUT_score = 0.1 * 80;
                    }
                    else if (Peaky_ROUT >= 10.1)
                    {
                        Peaky_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Peaky_ROUT_score = 0.1 * 100;
                    }
                    double Peaky_FinalScore = peaky_Max_score + Peaky_MaxInterval_score + Peaky_nugeock_score + Peaky_ROUT_score;
                    string Peaky_Grade;
                    if (Peaky_FinalScore >= 96)
                    {
                        Peaky_Grade = "A";
                    }
                    else if (Peaky_FinalScore >= 91)
                    {
                        Peaky_Grade = "B";
                    }
                    else if (Peaky_FinalScore >= 86)
                    {
                        Peaky_Grade = "C";
                    }
                    else if (Peaky_FinalScore >= 81)
                    {
                        Peaky_Grade = "D";
                    }
                    else
                    {
                        Peaky_Grade = "E";
                    }
                    //--------------------------------Width의 스코어 계산!-------------------------------------------------
                    double Width_Max_score = 0;
                    if (Width_Max >= 50.3)
                    {
                        Width_Max_score = 0.2 * 60;
                    }
                    else if (Width_Max >= 43.7)
                    {
                        Width_Max_score = 0.2 * 70;
                    }
                    else if (Width_Max >= 38.1)
                    {
                        Width_Max_score = 0.2 * 80;
                    }
                    else if (Width_Max >= 33.1)
                    {
                        Width_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Width_Max_score = 0.2 * 100;
                    }

                    double Width_MaxInterval_score = 0;
                    if (Width_MaxInterval >= 28.1)
                    {
                        Width_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Width_MaxInterval >= 25.5)
                    {
                        Width_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Width_MaxInterval >= 23.2)
                    {
                        Width_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Width_MaxInterval >= 21.1)
                    {
                        Width_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Width_MaxInterval_score = 0.3 * 100;
                    }

                    double Width_nugeock_score = 0;
                    if (Width_nugeock >= 22.6)
                    {
                        Width_nugeock_score = 0.4 * 60;
                    }
                    else if (Width_nugeock >= 18.8)
                    {
                        Width_nugeock_score = 0.4 * 70;
                    }
                    else if (Width_nugeock >= 15.7)
                    {
                        Width_nugeock_score = 0.4 * 80;
                    }
                    else if (Width_nugeock >= 13.0)
                    {
                        Width_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Width_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Width_ROUT_score = 0.0;
                    if (Width_ROUT >= 80.0)
                    {
                        Width_ROUT_score = 0.1 * 60;
                    }
                    else if (Width_ROUT >= 72.7)
                    {
                        Width_ROUT_score = 0.1 * 70;
                    }
                    else if (Width_ROUT >= 66.1)
                    {
                        Width_ROUT_score = 0.1 * 80;
                    }
                    else if (Width_ROUT >= 60.1)
                    {
                        Width_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Width_ROUT_score = 0.1 * 100;
                    }
                    double Width_FinalScore = peaky_Max_score + Width_MaxInterval_score + Width_nugeock_score + Width_ROUT_score;
                    string Width_Grade;
                    if (Width_FinalScore >= 96)
                    {
                        Width_Grade = "A";
                    }
                    else if (Width_FinalScore >= 91)
                    {
                        Width_Grade = "B";
                    }
                    else if (Width_FinalScore >= 86)
                    {
                        Width_Grade = "C";
                    }
                    else if (Width_FinalScore >= 81)
                    {
                        Width_Grade = "D";
                    }
                    else
                    {
                        Width_Grade = "E";
                    }
                    //--------------------------------Height의 스코어 계산!-------------------------------------------------
                    double Height_Max_score = 0;
                    if (Height_Max >=13.8)
                    {
                        Height_Max_score = 0.2 * 60;
                    }
                    else if (Height_Max >= 12.0)
                    {
                        Height_Max_score = 0.2 * 70;
                    }
                    else if (Height_Max >= 10.5)
                    {
                        Height_Max_score = 0.2 * 80;
                    }
                    else if (Height_Max >= 9.1)
                    {
                        Height_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Height_Max_score = 0.2 * 100;
                    }

                    double Height_MaxInterval_score = 0;
                    if (Height_MaxInterval >= 9.4)
                    {
                        Height_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Height_MaxInterval >= 8.6)
                    {
                        Height_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Height_MaxInterval >= 7.8)
                    {
                        Height_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Height_MaxInterval >= 7.1)
                    {
                        Height_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Height_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Height_nugeock_score = 0;
                    if (Height_nugeock >= 3.6)
                    {
                        Height_nugeock_score = 0.4 * 60;
                    }
                    else if (Height_nugeock >= 3.0)
                    {
                        Height_nugeock_score = 0.4 * 70;
                    }
                    else if (Height_nugeock >= 2.5)
                    {
                        Height_nugeock_score = 0.4 * 80;
                    }
                    else if (Height_nugeock >= 2.1)
                    {
                        Height_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Height_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Height_ROUT_score = 0.0;
                    if (Height_ROUT >= 18.7)
                    {
                        Height_ROUT_score = 0.1 * 60;
                    }
                    else if (Height_ROUT >= 17.0)
                    {
                        Height_ROUT_score = 0.1 * 70;
                    }
                    else if (Height_ROUT >= 15.5)
                    {
                        Height_ROUT_score = 0.1 * 80;
                    }
                    else if (Height_ROUT >= 14.1)
                    {
                        Height_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Height_ROUT_score = 0.1 * 100;
                    }
                    double Height_FinalScore = peaky_Max_score + Height_MaxInterval_score + Height_nugeock_score + Height_ROUT_score;
                    string Height_Grade;
                    if (Height_FinalScore >= 96)
                    {
                        Height_Grade = "A";
                    }
                    else if (Height_FinalScore >= 91)
                    {
                        Height_Grade = "B";
                    }
                    else if (Height_FinalScore >= 86)
                    {
                        Height_Grade = "C";
                    }
                    else if (Height_FinalScore >= 81)
                    {
                        Height_Grade = "D";
                    }
                    else
                    {
                        Height_Grade = "E";
                    }

                    //--------------------------------Area의 스코어 계산!-------------------------------------------------
                    double Area_Max_score = 0;
                    if (Area_Max >= 8383.1)
                    {
                        Area_Max_score = 0.2 * 60;
                    }
                    else if (Area_Max >= 7290.1)
                    {
                        Area_Max_score = 0.2 * 70;
                    }
                    else if (Area_Max >= 6339.1)
                    {
                        Area_Max_score = 0.2 * 80;
                    }
                    else if (Area_Max >=5512.1)                       
                    {
                        Area_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Area_Max_score = 0.2 * 100;
                    }

                    double Area_MaxInterval_score = 0;
                    if (Area_MaxInterval >= 2279.1)
                    {
                        Area_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Area_MaxInterval >= 2072.1)
                    {
                        Area_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Area_MaxInterval >= 1883.1)
                    {
                        Area_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Area_MaxInterval >= 1712.1)
                    {
                        Area_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Area_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Area_nugeock_score = 0;
                    if (Area_nugeock >= 4320.1)
                    {
                        Area_nugeock_score = 0.4 * 60;
                    }
                    else if (Area_nugeock >= 3600.1)
                    {
                        Area_nugeock_score = 0.4 * 70;
                    }
                    else if (Area_nugeock >= 3000.1)
                    {
                        Area_nugeock_score = 0.4 * 80;
                    }
                    else if (Area_nugeock >= 2500.1)
                    {
                        Area_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Area_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Area_ROUT_score = 0.0;
                    if (Area_ROUT >= 12137.1)
                    {
                        Area_ROUT_score = 0.1 * 60;
                    }
                    else if (Area_ROUT >= 11034.1)
                    {
                        Area_ROUT_score = 0.1 * 70;
                    }
                    else if (Area_ROUT >= 10031.1)
                    {
                        Area_ROUT_score = 0.1 * 80;
                    }
                    else if (Area_ROUT >= 9119.1)
                    {
                        Area_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Area_ROUT_score = 0.1 * 100;
                    }
                    double Area_FinalScore = peaky_Max_score + Area_MaxInterval_score + Area_nugeock_score + Area_ROUT_score;
                    string Area_Grade;
                    if (Area_FinalScore >= 96)
                    {
                        Area_Grade = "A";
                    }
                    else if (Area_FinalScore >= 91)
                    {
                        Area_Grade = "B";
                    }
                    else if (Area_FinalScore >= 86)
                    {
                        Area_Grade = "C";
                    }
                    else if (Area_FinalScore >= 81)
                    {
                        Area_Grade = "D";
                    }
                    else
                    {
                        Area_Grade = "E";
                    }
                    // 가속쪽 최종 점수및 등급 계산
                    // double AC_FinalScore;
                    //int AC_FinalGrade
                    AC_FinalScore = Peakx_FinalScore*0.3 + Peaky_FinalScore * 0.3+ Width_FinalScore * 0.1+Height_FinalScore*0.2+Area_FinalScore*0.1;
                    if (AC_FinalScore>=96)
                    {
                        AC_FinalGrade = 1;
                    }else if (AC_FinalScore >= 91)
                    {
                        AC_FinalGrade = 2;
                    }else if (AC_FinalScore >= 86)
                    {
                        AC_FinalGrade = 3;  
                    }else if (AC_FinalScore >= 81)
                    {
                        AC_FinalGrade = 4;
                    }
                    else
                    {
                        AC_FinalGrade = 5;
                    }

                    //---------------- 등급표 저장-------------- 파일이름 ScoreGrade.csv
                    string head = $"Acceleration,단일치,인접치,누적치,R/OUT,등급,점수";
                    string Peakx_value = $"Peakx_측정값,{Peakx_Max},{Peakx_MaxInterval},{Peakx_nugeock},{Peakx_ROUT},{Peakx_Grade},{Peakx_FinalScore}";
                    string Peakx_scroe = $"Peakx_가중치반영 점수,{peakx_Max_score},{Peakx_MaxInterval_score},{Peakx_nugeock_score},{Peakx_ROUT_score},{Peakx_Grade},{Peakx_FinalScore}";
                    string Peaky_value = $"Peaky_측정값,{Peaky_Max},{Peaky_MaxInterval},{Peaky_nugeock},{Peaky_ROUT},{Peaky_Grade},{Peaky_FinalScore}";
                    string Peaky_scroe = $"Peaky_가중치반영 점수,{peakx_Max_score},{Peaky_MaxInterval_score},{Peaky_nugeock_score},{Peaky_ROUT_score},{Peaky_Grade},{Peaky_FinalScore}";

                    string Width_value = $"Width_측정값,{Width_Max},{Width_MaxInterval},{Width_nugeock},{Width_ROUT},{Width_Grade},{Width_FinalScore}";
                    string Width_scroe = $"Width_가중치반영 점수,{peakx_Max_score},{Width_MaxInterval_score},{Width_nugeock_score},{Width_ROUT_score},{Width_Grade},{Width_FinalScore}";
                    
                    string Height_value = $"Height_측정값,{Height_Max},{Height_MaxInterval},{Height_nugeock},{Height_ROUT},{Height_Grade},{Height_FinalScore}";
                    string Height_scroe = $"Height_가중치반영 점수,{peakx_Max_score},{Height_MaxInterval_score},{Height_nugeock_score},{Height_ROUT_score},{Height_Grade},{Height_FinalScore}";

                    string Area_value = $"Area_측정값,{Area_Max},{Area_MaxInterval},{Area_nugeock},{Area_ROUT},{Area_Grade},{Area_FinalScore}";
                    string Area_scroe = $"Area_가중치반영 점수,{peakx_Max_score},{Area_MaxInterval_score},{Area_nugeock_score},{Area_ROUT_score},{Area_Grade},{Area_FinalScore}";

                    string Total_score = $"Total_score ,{AC_FinalScore}";
                    string Total_Grade = $"Total_Grade ,{AC_FinalGrade}";

                    if (!File.Exists(Path.Combine(AC_CSVFullPath, "ScoreGrade.csv"))) //파일이 존재 하지 않으면
                    { 
                        try
                        {
                            using (StreamWriter sw = new StreamWriter(Path.Combine(AC_CSVFullPath, "ScoreGrade.csv"), true, Encoding.UTF8))
                            {
                                sw.WriteLine(head);
                                sw.WriteLine(Peakx_value);
                                sw.WriteLine(Peakx_scroe);
                                sw.WriteLine(Peaky_value);
                                sw.WriteLine(Peaky_scroe);
                                sw.WriteLine(Width_value);
                                sw.WriteLine(Width_scroe);
                                sw.WriteLine(Height_value);
                                sw.WriteLine(Height_scroe);
                                sw.WriteLine(Area_value);
                                sw.WriteLine(Area_scroe);
                                sw.WriteLine(Total_score);
                                sw.WriteLine(Total_Grade);
                            }

                            
                            logger.LogInfo("CSV", $"AC - ScoreGrade.csv 파일 생성 완료.  \n파일 경로 :{AC_CSVFullPath + "\\ScoreGrade.csv"}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo("CSV", $"AC - ScoreGrade.csv 파일 생성 실패.  \n파일 경로 :{AC_CSVFullPath + "\\ScoreGrade.csv"}\n 오류내용 : {ex.Message}");
                        }
                    }
                    else
                    {
                        logger.LogWarning("CSV", $"AC - ScoreGrade.csv 파일이 이미 존재합니다.  \n파일 경로 :{AC_CSVFullPath + "\\ScoreGrade.csv"}");
                    }


                }
            }
            else
            {
                logger.LogError("CSV",$"AC - ResultOutput.csv 파일이 없습니다.  \n파일 경로 :{AC_CSVFullPath + "\\ResultOutput.csv"}");
            }



            if (File.Exists(DC_CSVFullPath+ "\\ResultOutput.csv"))
            {
                //DC 파일이 제대로 만들어져서 존재 하는 경우
                string firstLine = File.ReadLines(DC_CSVFullPath+ "\\ResultOutput.csv").FirstOrDefault();

                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    // 파일은 있지만 내용이 비어 있는 겨우
                    logger.LogError("CSV", $"DC - ResultOutput.csv 파일이 비어 있습니다.  \n파일 경로 :{DC_CSVFullPath+ "\\ResultOutput.csv"}");
                }
                else
                {
                    //파일도있고 내부에 내용도 있는 경우
                    // 라인들을 읽어서 새로운 파일들 만들어야함
                    double[] sum = new double[5];
                    string[] lines = File.ReadAllLines(DC_CSVFullPath + "\\ResultOutput.csv");
                    List<double> Peakx_total = new List<double>();
                    List<double> Peaky_total = new List<double>();
                    List<double> Width_total = new List<double>();
                    List<double> Height_total = new List<double>();
                    List<double> Area_total = new List<double>();
                    int DataMaxCount = lines.Length; // 기어마다 개수가 달라지므로!
                    foreach (string line in lines)
                    {
                        string[] values = line.Split(',');
                        Peakx_total.Add(double.Parse(values[1]));
                        Peaky_total.Add(double.Parse(values[2]));
                        Width_total.Add(double.Parse(values[3]));
                        Height_total.Add(double.Parse(values[4]));
                        Area_total.Add(double.Parse(values[5]));
                        sum[0] += double.Parse(values[1]); // peak x
                        sum[1] += double.Parse(values[2]); // peak y
                        sum[2] += double.Parse(values[3]); // width
                        sum[3] += double.Parse(values[4]); // height
                        sum[4] += double.Parse(values[5]); // area
                    }
                    double peakx_avg = sum[0] / (double)DataMaxCount;
                    double peaky_avg = sum[1] / (double)DataMaxCount;
                    double Width_avg = sum[2] / (double)DataMaxCount;
                    double Height_avg = sum[3] / (double)DataMaxCount;
                    double Area_avg = sum[4] / (double)DataMaxCount;

                    //----------------peakx에 대한 단일치 인접치 누적치 r/out 구하기--------------------
                    double Peakx_Max = -1;//peakx의 단일치
                    double Peakx_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_pkakx_data = Peakx_total[0];
                    double Peakx_nugeock = 0; //peak 누적치;
                    double Peakx_MaxDATA = -1;
                    double Peakx_MinDATA = 99999;
                    foreach (double pkakx_data in Peakx_total)
                    {
                        //단일치 구하기
                        if (Peakx_Max <= Math.Abs(pkakx_data - peakx_avg))
                        {
                            Peakx_Max = Math.Abs(pkakx_data - peakx_avg);
                        }
                        //인접치 구하기
                        if (Peakx_MaxInterval <= Math.Abs(pkakx_data - Pre_pkakx_data))
                        {
                            Peakx_MaxInterval = Math.Abs(pkakx_data - Pre_pkakx_data);
                        }
                        Pre_pkakx_data = pkakx_data;
             
                        //R/OUT 구하기 위한 사전 준비
                        if (Peakx_MaxDATA < pkakx_data)
                        {
                            Peakx_MaxDATA = pkakx_data;
                        }
                        if (Peakx_MinDATA > pkakx_data)
                        {
                            Peakx_MinDATA = pkakx_data;
                        }

                        Peakx_nugeock += pkakx_data * pkakx_data;
                    }
                    Peakx_nugeock /= (double)DataMaxCount;
                    Peakx_nugeock = Math.Sqrt(Peakx_nugeock);
                    //R/OUT 구하기
                    double Peakx_ROUT = Peakx_MaxDATA - Peakx_MinDATA;

                    //--------------peak y에 대한 값 단일치/ 인접치 / 누적치/ rout 구하기------------------
                    double Peaky_Max = -1;//peakx의 단일치
                    double Peaky_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_pkaky_data = Peaky_total[0];
                    double Peaky_nugeock = 0; //peak 누적치;
                    double Peaky_MaxDATA = -1;
                    double Peaky_MinDATA = 99999;
                    foreach (double peaky_data in Peaky_total)
                    {
                        //단일치 구하기
                        if (Peaky_Max <= Math.Abs(peaky_data - peaky_avg))
                        {
                            Peaky_Max = Math.Abs(peaky_data - peaky_avg);
                        }
                        //인접치 구하기
                        if (Peaky_MaxInterval <= Math.Abs(peaky_data - Pre_pkaky_data))
                        {
                            Peaky_MaxInterval = Math.Abs(peaky_data - Pre_pkaky_data);
                        }
                        Pre_pkaky_data = peaky_data;
            
                        //R/OUT 구하기 위한 사전 준비
                        if (Peaky_MaxDATA < peaky_data)
                        {
                            Peaky_MaxDATA = peaky_data;
                        }
                        if (Peaky_MinDATA > peaky_data)
                        {
                            Peaky_MinDATA = peaky_data;
                        }
                        Peaky_nugeock += peaky_data * peaky_data;
                    }
                    Peaky_nugeock /= (double)DataMaxCount;
                    Peaky_nugeock = Math.Sqrt(Peaky_nugeock);
                    //R/OUT 구하기
                    double Peaky_ROUT = Peaky_MaxDATA - Peaky_MinDATA;

                    //--------------Width에 대한 값 단일치/ 인접치 / 누적치/ rout 구하기-------------------
                    double Width_Max = -1;//peakx의 단일치
                    double Width_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_Width_data = Width_total[0];
                    double Width_nugeock = 0; //peak 누적치;
                    double Width_MaxDATA = -1;
                    double Width_MinDATA = 99999;
                    foreach (double Width_data in Width_total)
                    {
                        //단일치 구하기
                        if (Width_Max <= Math.Abs(Width_data - Width_avg))
                        {
                            Width_Max = Math.Abs(Width_data - Width_avg);
                        }
                        //인접치 구하기
                        if (Width_MaxInterval <= Math.Abs(Width_data - Pre_Width_data))
                        {
                            Width_MaxInterval = Math.Abs(Width_data - Pre_Width_data);
                        }
                        Pre_Width_data = Width_data;

                        //R/OUT 구하기 위한 사전 준비
                        if (Width_MaxDATA < Width_data)
                        {
                            Width_MaxDATA = Width_data;
                        }
                        if (Width_MinDATA > Width_data)
                        {
                            Width_MinDATA = Width_data;
                        }
                        Width_nugeock += Width_data * Width_data;
                    }
                    Width_nugeock /= (double)DataMaxCount;
                    Width_nugeock = Math.Sqrt(Width_nugeock);
                    //R/OUT 구하기
                    double Width_ROUT = Width_MaxDATA - Width_MinDATA;


                    //--------------height 에대한 단일/ 인점 /누적 /rout 구하기---------------------
                    double Height_Max = -1;//peakx의 단일치
                    double Height_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_Height_data = Height_total[0];
                    double Height_nugeock = 0; //peak 누적치;
                    double Height_MaxDATA = -1;
                    double Height_MinDATA = 99999;
                    foreach (double Height_data in Height_total)
                    {
                        //단일치 구하기
                        if (Height_Max <= Math.Abs(Height_data - Height_avg))
                        {
                            Height_Max = Math.Abs(Height_data - Height_avg);
                        }
                        //인접치 구하기
                        if (Height_MaxInterval <= Math.Abs(Height_data - Pre_Height_data))
                        {
                            Height_MaxInterval = Math.Abs(Height_data - Pre_Height_data);
                        }
                        Pre_Height_data = Height_data;
                        //R/OUT 구하기 위한 사전 준비
                        if (Height_MaxDATA < Height_data)
                        {
                            Height_MaxDATA = Height_data;
                        }
                        if (Height_MinDATA > Height_data)
                        {
                            Height_MinDATA = Height_data;
                        }
                        Height_nugeock += Height_data * Height_data;
                    }
                    Height_nugeock /= (double)DataMaxCount;
                    Height_nugeock = Math.Sqrt(Height_nugeock);
                    //R/OUT 구하기
                    double Height_ROUT = Height_MaxDATA - Height_MinDATA;

                    //--------------------area에 대한 단일/ 인접 /누적 /rout 구하기-------------------
                    double Area_Max = -1;//peakx의 단일치
                    double Area_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                    double Pre_Area_data = Area_total[0];
                    double Area_nugeock = 0; //peak 누적치;
                    double Area_MaxDATA = -1;
                    double Area_MinDATA = 99999;
                    foreach (double Area_data in Area_total)
                    {
                        //단일치 구하기
                        if (Area_Max <= Math.Abs(Area_data - Area_avg))
                        {
                            Area_Max = Math.Abs(Area_data - Area_avg);
                        }
                        //인접치 구하기
                        if (Area_MaxInterval <= Math.Abs(Area_data - Pre_Area_data))
                        {
                            Area_MaxInterval = Math.Abs(Area_data - Pre_Area_data);
                        }
                        Pre_Area_data = Area_data;
                        //R/OUT 구하기 위한 사전 준비
                        if (Area_MaxDATA < Area_data)
                        {
                            Area_MaxDATA = Area_data;
                        }
                        if (Area_MinDATA > Area_data)
                        {
                            Area_MinDATA = Area_data;
                        }
                        Area_nugeock += Area_data * Area_data;
                    }
                    Area_nugeock /= (double)DataMaxCount;
                    Area_nugeock = Math.Sqrt(Area_nugeock);
                    //R/OUT 구하기
                    double Area_ROUT = Area_MaxDATA - Area_MinDATA;


                    //--------------------------------peakx의 스코어 계산!-------------------------------------------------
                    // peakx의 스코어 계산!
                    double peakx_Max_score = 0;
                    if (Peakx_Max >= 18.4)
                    {
                        peakx_Max_score = 0.2 * 60;
                    }
                    else if (Peakx_Max >= 16.0)
                    {
                        peakx_Max_score = 0.2 * 70;
                    }
                    else if (Peakx_Max >= 13.9)
                    {
                        peakx_Max_score = 0.2 * 80;
                    }
                    else if (Peakx_Max >= 12.1)
                    {
                        peakx_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        peakx_Max_score = 0.2 * 100;
                    }
                    double Peakx_MaxInterval_score = 0;
                    if (Peakx_MaxInterval >= 8.1)
                    {
                        Peakx_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Peakx_MaxInterval >= 7.4)
                    {
                        Peakx_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Peakx_MaxInterval >= 6.7)
                    {
                        Peakx_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Peakx_MaxInterval >= 6.1)
                    {
                        Peakx_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Peakx_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK X 누적치
                    double Peakx_nugeock_score = 0;
                    if (Peakx_nugeock >= 8.7)
                    {
                        Peakx_nugeock_score = 0.4 * 60;
                    }
                    else if (Peakx_nugeock >= 7.3)
                    {
                        Peakx_nugeock_score = 0.4 * 70;
                    }
                    else if (Peakx_nugeock >= 6.1)
                    {
                        Peakx_nugeock_score = 0.4 * 80;
                    }
                    else if (Peakx_nugeock >= 6.1)
                    {
                        Peakx_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Peakx_nugeock_score = 0.1 * 100;
                    }
                    //PEAK X R/OUT
                    double Peakx_ROUT_score = 0.0;
                    if (Peakx_ROUT >= 26.7)
                    {
                        Peakx_ROUT_score = 0.1 * 60;
                    }
                    else if (Peakx_ROUT >= 24.3)
                    {
                        Peakx_ROUT_score = 0.1 * 70;
                    }
                    else if (Peakx_ROUT >= 22.1)
                    {
                        Peakx_ROUT_score = 0.1 * 80;
                    }
                    else if (Peakx_ROUT >= 20.1)
                    {
                        Peakx_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Peakx_ROUT_score = 0.1 * 100;
                    }
                    double Peakx_FinalScore = peakx_Max_score + Peakx_MaxInterval_score + Peakx_nugeock_score + Peakx_ROUT_score;
                    string Peakx_Grade;
                    if (Peakx_FinalScore >= 96)
                    {
                        Peakx_Grade = "A";
                    }
                    else if (Peakx_FinalScore >= 91)
                    {
                        Peakx_Grade = "B";
                    }
                    else if (Peakx_FinalScore >= 86)
                    {
                        Peakx_Grade = "C";
                    }
                    else if (Peakx_FinalScore >= 81)
                    {
                        Peakx_Grade = "D";
                    }
                    else
                    {
                        Peakx_Grade = "E";
                    }

                    //--------------------------------peaky의 스코어 계산!-------------------------------------------------
                    double peaky_Max_score = 0;
                    if (Peaky_Max >= 7.7)
                    {
                        peaky_Max_score = 0.2 * 60;
                    }
                    else if (Peaky_Max >= 6.7)
                    {
                        peaky_Max_score = 0.2 * 70;
                    }
                    else if (Peaky_Max >= 5.9)
                    {
                        peaky_Max_score = 0.2 * 80;
                    }
                    else if (Peakx_Max >= 5.1)
                    {
                        peaky_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        peaky_Max_score = 0.2 * 100;
                    }

                    double Peaky_MaxInterval_score = 0;
                    if (Peaky_MaxInterval >= 4.8)
                    {
                        Peaky_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Peaky_MaxInterval >= 4.3)
                    {
                        Peaky_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Peaky_MaxInterval >= 4.0)
                    {
                        Peaky_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Peaky_MaxInterval >= 3.6)
                    {
                        Peaky_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Peaky_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Peaky_nugeock_score = 0;
                    if (Peaky_nugeock >= 3.6)
                    {
                        Peaky_nugeock_score = 0.4 * 60;
                    }
                    else if (Peaky_nugeock >= 3.0)
                    {
                        Peaky_nugeock_score = 0.4 * 70;
                    }
                    else if (Peaky_nugeock >= 2.5)
                    {
                        Peaky_nugeock_score = 0.4 * 80;
                    }
                    else if (Peaky_nugeock >= 2.1)
                    {
                        Peaky_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Peaky_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Peaky_ROUT_score = 0.0;
                    if (Peaky_ROUT >= 10.7)
                    {
                        Peaky_ROUT_score = 0.1 * 60;
                    }
                    else if (Peaky_ROUT >= 9.8)
                    {
                        Peaky_ROUT_score = 0.1 * 70;
                    }
                    else if (Peaky_ROUT >= 8.9)
                    {
                        Peaky_ROUT_score = 0.1 * 80;
                    }
                    else if (Peaky_ROUT >= 8.1)
                    {
                        Peaky_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Peaky_ROUT_score = 0.1 * 100;
                    }
                    double Peaky_FinalScore = peaky_Max_score + Peaky_MaxInterval_score + Peaky_nugeock_score + Peaky_ROUT_score;
                    string Peaky_Grade;
                    if (Peaky_FinalScore >= 96)
                    {
                        Peaky_Grade = "A";
                    }
                    else if (Peaky_FinalScore >= 91)
                    {
                        Peaky_Grade = "B";
                    }
                    else if (Peaky_FinalScore >= 86)
                    {
                        Peaky_Grade = "C";
                    }
                    else if (Peaky_FinalScore >= 81)
                    {
                        Peaky_Grade = "D";
                    }
                    else
                    {
                        Peaky_Grade = "E";
                    }
                    //--------------------------------Width의 스코어 계산!-------------------------------------------------
                    double Width_Max_score = 0;
                    if (Width_Max >= 44.2)
                    {
                        Width_Max_score = 0.2 * 60;
                    }
                    else if (Width_Max >= 38.5)
                    {
                        Width_Max_score = 0.2 * 70;
                    }
                    else if (Width_Max >= 33.5)
                    {
                        Width_Max_score = 0.2 * 80;
                    }
                    else if (Width_Max >= 29.1)
                    {
                        Width_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Width_Max_score = 0.2 * 100;
                    }

                    double Width_MaxInterval_score = 0;
                    if (Width_MaxInterval >= 26.7)
                    {
                        Width_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Width_MaxInterval >= 24.3)
                    {
                        Width_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Width_MaxInterval >= 22.1)
                    {
                        Width_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Width_MaxInterval >= 20.1)
                    {
                        Width_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Width_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Width_nugeock_score = 0;
                    if (Width_nugeock >= 20.8)
                    {
                        Width_nugeock_score = 0.4 * 60;
                    }
                    else if (Width_nugeock >= 17.4)
                    {
                        Width_nugeock_score = 0.4 * 70;
                    }
                    else if (Width_nugeock >= 14.5)
                    {
                        Width_nugeock_score = 0.4 * 80;
                    }
                    else if (Width_nugeock >= 12.1)
                    {
                        Width_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Width_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Width_ROUT_score = 0.0;
                    if (Width_ROUT >= 64.0)
                    {
                        Width_ROUT_score = 0.1 * 60;
                    }
                    else if (Width_ROUT >= 58.2)
                    {
                        Width_ROUT_score = 0.1 * 70;
                    }
                    else if (Width_ROUT >= 52.9)
                    {
                        Width_ROUT_score = 0.1 * 80;
                    }
                    else if (Width_ROUT >= 48.1)
                    {
                        Width_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Width_ROUT_score = 0.1 * 100;
                    }
                    double Width_FinalScore = peaky_Max_score + Width_MaxInterval_score + Width_nugeock_score + Width_ROUT_score;
                    string Width_Grade;
                    if (Width_FinalScore >= 96)
                    {
                        Width_Grade = "A";
                    }
                    else if (Width_FinalScore >= 91)
                    {
                        Width_Grade = "B";
                    }
                    else if (Width_FinalScore >= 86)
                    {
                        Width_Grade = "C";
                    }
                    else if (Width_FinalScore >= 81)
                    {
                        Width_Grade = "D";
                    }
                    else
                    {
                        Width_Grade = "E";
                    }
                    //--------------------------------Height의 스코어 계산!-------------------------------------------------
                    double Height_Max_score = 0;
                    if (Height_Max >= 10.7)
                    {
                        Height_Max_score = 0.2 * 60;
                    }
                    else if (Height_Max >= 9.4)
                    {
                        Height_Max_score = 0.2 * 70;
                    }
                    else if (Height_Max >= 8.2)
                    {
                        Height_Max_score = 0.2 * 80;
                    }
                    else if (Height_Max >= 7.1)
                    {
                        Height_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Height_Max_score = 0.2 * 100;
                    }

                    double Height_MaxInterval_score = 0;
                    if (Height_MaxInterval >= 9.4)
                    {
                        Height_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Height_MaxInterval >= 8.6)
                    {
                        Height_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Height_MaxInterval >= 7.8)
                    {
                        Height_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Height_MaxInterval >= 7.1)
                    {
                        Height_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Height_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Height_nugeock_score = 0;
                    if (Height_nugeock >= 4.6)
                    {
                        Height_nugeock_score = 0.4 * 60;
                    }
                    else if (Height_nugeock >= 3.8)
                    {
                        Height_nugeock_score = 0.4 * 70;
                    }
                    else if (Height_nugeock >= 3.2)
                    {
                        Height_nugeock_score = 0.4 * 80;
                    }
                    else if (Height_nugeock >= 2.7)
                    {
                        Height_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Height_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Height_ROUT_score = 0.0;
                    if (Height_ROUT >= 16.1)
                    {
                        Height_ROUT_score = 0.1 * 60;
                    }
                    else if (Height_ROUT >= 14.6)
                    {
                        Height_ROUT_score = 0.1 * 70;
                    }
                    else if (Height_ROUT >= 13.3)
                    {
                        Height_ROUT_score = 0.1 * 80;
                    }
                    else if (Height_ROUT >= 12.1)
                    {
                        Height_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Height_ROUT_score = 0.1 * 100;
                    }
                    double Height_FinalScore = peaky_Max_score + Height_MaxInterval_score + Height_nugeock_score + Height_ROUT_score;
                    string Height_Grade;
                    if (Height_FinalScore >= 96)
                    {
                        Height_Grade = "A";
                    }
                    else if (Height_FinalScore >= 91)
                    {
                        Height_Grade = "B";
                    }
                    else if (Height_FinalScore >= 86)
                    {
                        Height_Grade = "C";
                    }
                    else if (Height_FinalScore >= 81)
                    {
                        Height_Grade = "D";
                    }
                    else
                    {
                        Height_Grade = "E";
                    }

                    //--------------------------------Area의 스코어 계산!-------------------------------------------------
                    double Area_Max_score = 0;
                    if (Area_Max >= 4899.1)
                    {
                        Area_Max_score = 0.2 * 60;
                    }
                    else if (Area_Max >= 4260.1)
                    {
                        Area_Max_score = 0.2 * 70;
                    }
                    else if (Area_Max >= 3704.1)
                    {
                        Area_Max_score = 0.2 * 80;
                    }
                    else if (Area_Max >= 3221.1)
                    {
                        Area_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Area_Max_score = 0.2 * 100;
                    }

                    double Area_MaxInterval_score = 0;
                    if (Area_MaxInterval >= 4074.1)
                    {
                        Area_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Area_MaxInterval >= 3704.1)
                    {
                        Area_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Area_MaxInterval >= 3367.1)
                    {
                        Area_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Area_MaxInterval >= 3061.1)
                    {
                        Area_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Area_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Area_nugeock_score = 0;
                    if (Area_nugeock >= 2074.1)
                    {
                        Area_nugeock_score = 0.4 * 60;
                    }
                    else if (Area_nugeock >= 1708.1)
                    {
                        Area_nugeock_score = 0.4 * 70;
                    }
                    else if (Area_nugeock >= 1440.1)
                    {
                        Area_nugeock_score = 0.4 * 80;
                    }
                    else if (Area_nugeock >= 1200.1)
                    {
                        Area_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Area_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Area_ROUT_score = 0.0;
                    if (Area_ROUT >= 6925.1)
                    {
                        Area_ROUT_score = 0.1 * 60;
                    }
                    else if (Area_ROUT >= 6296.1)
                    {
                        Area_ROUT_score = 0.1 * 70;
                    }
                    else if (Area_ROUT >= 5723.1)
                    {
                        Area_ROUT_score = 0.1 * 80;
                    }
                    else if (Area_ROUT >= 5203.1)
                    {
                        Area_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Area_ROUT_score = 0.1 * 100;
                    }
                    double Area_FinalScore = peaky_Max_score + Area_MaxInterval_score + Area_nugeock_score + Area_ROUT_score;
                    string Area_Grade;
                    if (Area_FinalScore >= 96)
                    {
                        Area_Grade = "A";
                    }
                    else if (Area_FinalScore >= 91)
                    {
                        Area_Grade = "B";
                    }
                    else if (Area_FinalScore >= 86)
                    {
                        Area_Grade = "C";
                    }
                    else if (Area_FinalScore >= 81)
                    {
                        Area_Grade = "D";
                    }
                    else
                    {
                        Area_Grade = "E";
                    }
                    // 가속쪽 최종 점수및 등급 계산
                    // double AC_FinalScore;
                    //int AC_FinalGrade
                    AC_FinalScore = Peakx_FinalScore * 0.3 + Peaky_FinalScore * 0.3 + Width_FinalScore * 0.1 + Height_FinalScore * 0.2 + Area_FinalScore * 0.1;
                    if (AC_FinalScore >= 96)
                    {
                        AC_FinalGrade = 1;
                    }
                    else if (AC_FinalScore >= 91)
                    {
                        AC_FinalGrade = 2;
                    }
                    else if (AC_FinalScore >= 86)
                    {
                        AC_FinalGrade = 3;
                    }
                    else if (AC_FinalScore >= 81)
                    {
                        AC_FinalGrade = 4;
                    }
                    else
                    {
                        AC_FinalGrade = 5;
                    }

                    //---------------- 등급표 저장-------------- 파일이름 ScoreGrade.csv
                    string head = $"Acceleration,단일치,인접치,누적치,R/OUT,등급,점수";
                    string Peakx_value = $"Peakx_측정값,{Peakx_Max},{Peakx_MaxInterval},{Peakx_nugeock},{Peakx_ROUT},{Peakx_Grade},{Peakx_FinalScore}";
                    string Peakx_scroe = $"Peakx_가중치반영 점수,{peakx_Max_score},{Peakx_MaxInterval_score},{Peakx_nugeock_score},{Peakx_ROUT_score},{Peakx_Grade},{Peakx_FinalScore}";
                    string Peaky_value = $"Peaky_측정값,{Peaky_Max},{Peaky_MaxInterval},{Peaky_nugeock},{Peaky_ROUT},{Peaky_Grade},{Peaky_FinalScore}";
                    string Peaky_scroe = $"Peaky_가중치반영 점수,{peakx_Max_score},{Peaky_MaxInterval_score},{Peaky_nugeock_score},{Peaky_ROUT_score},{Peaky_Grade},{Peaky_FinalScore}";

                    string Width_value = $"Width_측정값,{Width_Max},{Width_MaxInterval},{Width_nugeock},{Width_ROUT},{Width_Grade},{Width_FinalScore}";
                    string Width_scroe = $"Width_가중치반영 점수,{peakx_Max_score},{Width_MaxInterval_score},{Width_nugeock_score},{Width_ROUT_score},{Width_Grade},{Width_FinalScore}";

                    string Height_value = $"Height_측정값,{Height_Max},{Height_MaxInterval},{Height_nugeock},{Height_ROUT},{Height_Grade},{Height_FinalScore}";
                    string Height_scroe = $"Height_가중치반영 점수,{peakx_Max_score},{Height_MaxInterval_score},{Height_nugeock_score},{Height_ROUT_score},{Height_Grade},{Height_FinalScore}";

                    string Area_value = $"Area_측정값,{Area_Max},{Area_MaxInterval},{Area_nugeock},{Area_ROUT},{Area_Grade},{Area_FinalScore}";
                    string Area_scroe = $"Area_가중치반영 점수,{peakx_Max_score},{Area_MaxInterval_score},{Area_nugeock_score},{Area_ROUT_score},{Area_Grade},{Area_FinalScore}";

                    string Total_score = $"Total_score ,{AC_FinalScore}";
                    string Total_Grade = $"Total_Grade ,{AC_FinalGrade}";
                    if (!File.Exists(Path.Combine(DC_CSVFullPath, "ScoreGrade.csv"))) 
                    { 
                         try
                         {
                             using (StreamWriter sw = new StreamWriter(Path.Combine(DC_CSVFullPath, "ScoreGrade.csv"), true, Encoding.UTF8))
                             {
                                 sw.WriteLine(head);
                                 sw.WriteLine(Peakx_value);
                                 sw.WriteLine(Peakx_scroe);
                                 sw.WriteLine(Peaky_value);
                                 sw.WriteLine(Peaky_scroe);
                                 sw.WriteLine(Width_value);
                                 sw.WriteLine(Width_scroe);
                                 sw.WriteLine(Height_value);
                                 sw.WriteLine(Height_scroe);
                                 sw.WriteLine(Area_value);
                                 sw.WriteLine(Area_scroe);
                                 sw.WriteLine(Total_score);
                                 sw.WriteLine(Total_Grade);
                             }
                             logger.LogInfo("CSV", $"DC - ScoreGrade.csv 파일 생성 완료.  \n파일 경로 :{DC_CSVFullPath + "\\ScoreGrade.csv"}");
                         }
                         catch (Exception ex)
                         {
                             logger.LogInfo("CSV", $"DC - ScoreGrade.csv 파일 생성 실패.  \n파일 경로 :{DC_CSVFullPath + "\\ScoreGrade.csv"}\n 오류내용 {ex.Message}");
                         }
                    }
                    else
                    {
                        logger.LogInfo("CSV", $"DC - ScoreGrade.csv 파일이 이미 존재합니다. \n파일 경로 :{DC_CSVFullPath + "\\ScoreGrade.csv"}");
                    }
                }
            }
            else
            {
                logger.LogError("CSV", $"DC - ResultOutput.csv 파일이 없습니다.  \n파일 경로 :{DC_CSVFullPath+ "\\ResultOutput.csv"}");
            }
        }

        /// <summary>
        /// DC ResultOutput.csv를 A열(COUNT) 기준 오름차순으로 정렬하고,
        /// </summary>
        public void DcDataSort()
        {
            const string fileName = "ResultOutput.csv";
            string csvPath = Path.Combine(DC_CSVFullPath, fileName);

            try
            {
                if (string.IsNullOrWhiteSpace(DC_CSVFullPath))
                {
                    logger.LogWarning("CSV", "DC CSV 경로 미설정 — ResultOutput 정렬 건너뜀");
                    return;
                }

                if (!File.Exists(csvPath))
                {
                    logger.LogError("CSV", $"DC - ResultOutput.csv 파일이 없습니다.\n파일 경로: {csvPath}");
                    return;
                }

                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length == 0)
                {
                    logger.LogWarning("CSV", "DC - ResultOutput.csv 파일이 비어 있습니다.", "", csvPath);
                    return;
                }

                var rows = new List<(int count, string line)>();
                int skipped = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] values = line.Split(',');
                    if (values.Length == 0 || !int.TryParse(values[0].Trim(), out int count))
                    {
                        skipped++;
                        logger.LogWarning("CSV", "DC ResultOutput 정렬 — A열(COUNT) 파싱 실패, 건너뜀", "", line);
                        continue;
                    }

                    rows.Add((count, line.Trim()));
                }

                if (rows.Count == 0)
                {
                    logger.LogWarning("CSV", "DC - ResultOutput.csv에 유효한 데이터 행이 없습니다.", "", csvPath);
                    return;
                }

                var sortedLines = rows
                    .OrderBy(r => r.count)
                    .Select(r => r.line)
                    .ToList();

                File.WriteAllLines(csvPath, sortedLines, Encoding.UTF8);
                logger.LogInfo("CSV", "DC - ResultOutput.csv A열(COUNT) 기준 정렬 완료", "", $"경로={csvPath}, 행={sortedLines.Count}, 건너뜀={skipped}");
            }
            catch (Exception ex)
            {
                logger.LogError("CSV", "DC - ResultOutput.csv 정렬 중 예외 발생", "", $"{ex.Message} | {ex.StackTrace}");
            }
        }

        public void DcJpgRename()
        {
            if (!Directory.Exists(DC_CSVFullPath))
            {
                // 예외처리
                return;
            }
            // 1. 폴더 내의 모든 JPG 파일 가져오기
            string[] jpgFiles = Directory.GetFiles(DC_CSVFullPath, "*.jpg");

            int AC_DC_countsub = 0;
            int MaxCount = jpgFiles.Length;
            if(MaxCount==43){
                AC_DC_countsub = 20;// 이값들이 기어의 개수에따라 달라짐
            }
            else 
            {
                logger.LogError("이미지 파일 오류",$"저장된 이미지 파일의 개수 \n오류 파일 개수{MaxCount} \n 이미지 경로 : {DC_CSVFullPath}","","");
                return;
            }

            foreach (string filePath in jpgFiles)
            {
                // 2. 경로에서 순수 파일 이름만 추출 (예: "01.jpg" -> "01")
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                // 3. 파일 이름이 순수 숫자로만 이루어져 있는지 확인
                if (int.TryParse(fileNameWithoutExt, out int fileNumber))
                {
                    // 4. 숫자를 3자리 포맷(001, 002...)으로 변경하고 확장자 붙이기
                    string newFileName = fileNumber.ToString("D3") + ".jpg";

                    // 5. 전체 새로운 경로 생성W
                    string newFilePath = Path.Combine(DC_CSVFullPath, newFileName);

                    try
                    {
                        // 6. 파일 이름 변경 실행
                        if (!File.Exists(newFilePath)) // 같은 이름의 파일이 없을 때만
                        {
                            File.Move(filePath, newFilePath);
                        }                       
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("파일 이름 변경", $"예외 내용 : {ex.Message}", "", "");
                    }
                }

            }


            jpgFiles = Directory.GetFiles(DC_CSVFullPath, "*.jpg");
            foreach (string filePath in jpgFiles)
            {
                // 2. 경로에서 순수 파일 이름만 추출 (예: "001.jpg" -> "001")
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                
                // 3. 파일 이름이 순수 숫자로만 이루어져 있는지 확인
                if (int.TryParse(fileNameWithoutExt, out int fileNumber))
                {

                    //4.jpg파일 이름을 csv순서대로 맞추기
                    fileNumber = (fileNumber + AC_DC_countsub) % MaxCount + 1;

                    // 5. 숫자를 2자리 포맷(01, 02...)으로 변경하고 확장자 붙이기
                    string newFileName = fileNumber.ToString("D2") + ".jpg";

                    // 6. 전체 새로운 경로 생성
                    string newFilePath = Path.Combine(DC_CSVFullPath, newFileName);

                    try
                    {
                        // 6. 파일 이름 변경 실행
                        if (!File.Exists(newFilePath)) // 같은 이름의 파일이 없을 때만
                        {
                            File.Move(filePath, newFilePath);                            
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("파일 이름 변경 오류", $"예외 내용 : {ex.Message}", "", "");
                    }
                }
            }
        
        
        }
    }
}
