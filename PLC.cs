using ActUtlType64Lib;
using Newtonsoft.Json.Linq;
using ScottPlot.Colormaps;
using SkiaSharp;
using System.IO;
using System.Text;
using System.Text.Json;
using static WIA_ViewerProgram.HistoryManager;
using static WIA_ViewerProgram.ViewerForm;

namespace WIA_ViewerProgram
{
    internal sealed class PLCSettingJson
    {
        public string Ip { get; set; } = "";
        public int StationNumber { get; set; }
        public int MoniteringCycle { get; set; }
        public string MoniterAdrress { get; set; } = "D6000";
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

        /// <summary>모니터링할 디바이스 주소 문자열 (예: D6000, M100).</summary>
        public string MoniterAdrress = "D6000";
        private string hex;

        //"C:\\Users\\Admin\\Documents\\Keyence\\XG-X VisionTerminal\\USB\\SD2\\Vision\\"=> 수정해야함
        // 테스트용 경로
        //"C:\\Users\\xodnj\\Desktop\\txt\\"
        const string FilePath = "C:\\Users\\Admin\\Documents\\Keyence\\XG-X VisionTerminal\\USB\\SD2\\Vision\\";//이폴더에 통합 CSV파일 저장!

        //거리 감지 센서값 저장하기 위해 필요한 변수들
        int SensingAdress = 6100;// 시작주소
        string SensingDataSavePath = "-";
        List<float> sensingDist = new List<float>();

        //시작할때 쓰레기 폴더 시행횟수 999에 있는 모든 데이터를 삭제함 
        string trashfolder = "-";
        string AC_CSVFullPath = "-";
        string DC_CSVFullPath = "-";
        string CurrentModel="";
        public PLC()
        {
            PictureEnd = false;
            MoniterAdrress = "D6000";
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
                    StationNumber = 2;
                    MoniteringCycle = 1000;
                    MoniterAdrress = "D6000";
                    logger.LogInfo("PLC", "PLCSetting.json 없음 — 기본값으로 새 파일 생성", "", JsonPath);
                    SaveToJson();
                    return;
                }

                string json = File.ReadAllText(JsonPath);
                var data = JsonSerializer.Deserialize<PLCSettingJson>(json);
                if (data == null)
                {
                    Ip = "";
                    StationNumber = 2;
                    MoniteringCycle = 1000;
                    MoniterAdrress = "D6000";
                    logger.LogWarning("PLC", "PLCSetting.json 역직렬화 결과 null — 기본값 사용", "", JsonPath);
                    return;
                }

                Ip = data.Ip ?? "";
                StationNumber = data.StationNumber;
                MoniteringCycle = data.MoniteringCycle;
                MoniterAdrress = string.IsNullOrWhiteSpace(data.MoniterAdrress) ? "D6000" : data.MoniterAdrress.Trim();
                logger.LogInfo("PLC", "PLC 설정 로드 완료", "", $"Ip={Ip}, Station={StationNumber}, Cycle={MoniteringCycle}ms, Address={MoniterAdrress}");
            }
            catch (Exception ex)
            {
                Ip = "";
                StationNumber = 2;
                MoniteringCycle = 1000;
                MoniterAdrress = "D6000";
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
                                        DcJpgRename();// DC의 이미지 순서 저장
                                        DistSensingDataSave();//=> 센싱 데이터 저장 
                                        //AC/ DC의 RESULT OUTPUT 데이터에서 PEAK X , PEAK Y, WIDTH, HEIGHT, AREA에 대해서 
                                        //단일치, 인접치, 누적치, R/OUT을 우선 계산
                                        // 이후 등급 계산 
                                        GradeCSVCreate();
                                        /// 시행횟수 999폴더는 무조건 삭제...
                                        DeletTrashFolder();
                                        DC_CSVFullPath = "DC_CSVFullPath- 초기화 : 등급 점수를 두번 저장하지 않기 위한 초기화";
                                        AC_CSVFullPath = "ACC_CSVFullPath- 초기화 : 등급 점수를 두번 저장하지 않기 위한 초기화 ";



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
            //수정 해야함
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
                        string[] FileAllLines = File.ReadAllLines(filePath);
                        foreach (string line in FileAllLines)
                        {
                            // 여기서 각 줄(line)을 처리합니다.
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                linesProcessed++;
                                string[] values = line.Split(',');
                                
                                const int minCols = 16; // 0~6 메타 + 데이터 9컬럼(7~15)
                                if (values.Length < minCols)
                                {
                                    linesSkippedBadFormat++;
                                    logger.LogWarning("PLC", "TXT 라인 컬럼 수 부족 — 건너뜀", "", $"file={filePath}, columns={values.Length}, need>={minCols}, 구역={CameraInfo}");
                                    continue;
                                }

                                string date = values[0] + values[1] + values[2]; //날짜
                                string model = values[3]; //모델 명
                                CurrentModel = model;
                                string bcr = values[4]; // bcr
                                string folderNUM = values[5]; //시행횟수
                                string geartype = values[6]; // ac/dc 타입
                                //감속쪽 데이터는 이후에도 맞춰줘야함
                                // ---------------- 여기나오는 20 , 43은 txt파일을 읽어서 만드는거기 때문에 
                                //모든 데이터를 읽어 올순 없고 나중에 레시피마다 기어개수가 달라지는 것을 고려하여 수정해주자

                                //--------------★기억 개수에 따른 수정이 필요 한 부분-------------------------
                                if (CameraInfo == "DC")//DC의 홈과 AC의 홈의 데이터를 맞추기 위한 작업
                                {
                                    int count = FileAllLines.Length;

                                    if (count == 43)
                                    {
                                        values[7] = (((int.Parse(values[7]) + 21) % count) + 1).ToString();
                                    }
                                    else if (count == 46)
                                    {
                                        values[7] = (((int.Parse(values[7]) + 23) % count) + 1).ToString();
                                    }
                                    else if (count == 41)
                                    {
                                        values[7] = (((int.Parse(values[7]) + 100000) % count) + 1).ToString();
                                    }
                                    else if (count == 48)
                                    {
                                        values[7] = (((int.Parse(values[7]) + 23) % count) + 1).ToString();
                                    }
                                    else if (count == 50)
                                    {
                                        values[7] = (((int.Parse(values[7]) + 100000) % count) + 1).ToString();
                                    }
                                    else
                                    {
                                        logger.LogError("기어개수 에러",$"TXT파일에서 읽어온 기어 개수가 맞지 않습니다. 입력된 데이터의 수 {count}");
                                    }                                   
                                }

                                string[] selectedValues = values.Skip(7).Take(9).ToArray();
                                string csvContent = string.Join(",", selectedValues);
                               
                                //config.FTP를 활용해서 
                                string CSVPath = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + folderNUM + "\\" + geartype;
                                SensingDataSavePath = config.FTP + date + "\\" + model + "\\" + bcr + "\\" + folderNUM;// 거리감지 센서를 져장하기 위한 패스

                                string fullPath = Path.Combine(CSVPath, "ResultOutput.csv");
                                if (CameraInfo == "AC")
                                {
                                    AC_CSVFullPath = CSVPath;
                                }
                                else if (CameraInfo == "DC")
                                {
                                    DC_CSVFullPath = CSVPath;
                                }
                                if (int.Parse(folderNUM) < 999)
                                { //폴더 번호가 999 아니라면
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
                            if (count > 1)
                            {
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
                    logger.LogInfo("트래쉬 폴더{999} 폴더 삭제 성공", $"폴더 경로 : {trashfolder}");
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


        private static bool TryGetThresholds(Dictionary<string, List<double>> dict, string key, out List<double> thresholds)
        {
            thresholds = new List<double>();
            if (dict == null || !dict.TryGetValue(key, out List<double>? values) || values == null || values.Count < 4)
            {
                return false;
            }

            thresholds = values;
            return true;
        }

        /// <summary>thresholds[0]=60, [1]=70, [2]=80, [3]=90 (UI/GradeBaseline 저장 순서).</summary>
        private static double CalcBandScore(double value, IReadOnlyList<double> thresholds, double weight)
        {
            if (value >= thresholds[0])
            {
                return weight * 60;
            }

            if (value >= thresholds[1])
            {
                return weight * 70;
            }

            if (value >= thresholds[2])
            {
                return weight * 80;
            }

            if (value >= thresholds[3])
            {
                return weight * 90;
            }

            return weight * 100;
        }

        private static string CalcLetterGrade(double finalScore)
        {
            if (finalScore >= 96)
            {
                return "A";
            }

            if (finalScore >= 91)
            {
                return "B";
            }

            if (finalScore >= 86)
            {
                return "C";
            }

            if (finalScore >= 81)
            {
                return "D";
            }

            return "E";
        }

        private void NotifyMissingBaseline(string channelName, string key)
        {
            logger.LogError("Json-GradeBaseline.json", $"모델:{CurrentModel} {channelName}.{key} 누락/부족 — ScoreGrade.csv 생성 중단");
        }

        private bool TryComputeMetricScores(
            Dictionary<string, List<double>> channelDict,
            string channelName,
            string jsonPrefix,
            double measuredMax,
            double measuredMaxInterval,
            double measuredRms,
            double measuredRout,
            out double maxScore,
            out double maxIntervalScore,
            out double nugeockScore,
            out double routScore,
            out double finalScore,
            out string grade)
        {
            maxScore = 0;
            maxIntervalScore = 0;
            nugeockScore = 0;
            routScore = 0;
            finalScore = 0;
            grade = "E";

            string keyMax = $"{jsonPrefix}_Max";
            string keyMaxInterval = $"{jsonPrefix}_MaxInterval";
            string keyRms = $"{jsonPrefix}_RMS";
            string keyRout = $"{jsonPrefix}_ROUT";

            if (!TryGetThresholds(channelDict, keyMax, out List<double> tMax))
            {
                NotifyMissingBaseline(channelName, keyMax);
                return false;
            }

            if (!TryGetThresholds(channelDict, keyMaxInterval, out List<double> tMaxInterval))
            {
                NotifyMissingBaseline(channelName, keyMaxInterval);
                return false;
            }

            if (!TryGetThresholds(channelDict, keyRms, out List<double> tRms))
            {
                NotifyMissingBaseline(channelName, keyRms);
                return false;
            }

            if (!TryGetThresholds(channelDict, keyRout, out List<double> tRout))
            {
                NotifyMissingBaseline(channelName, keyRout);
                return false;
            }

            maxScore = CalcBandScore(measuredMax, tMax, 0.2);
            maxIntervalScore = CalcBandScore(measuredMaxInterval, tMaxInterval, 0.3);
            nugeockScore = CalcBandScore(measuredRms, tRms, 0.4);
            routScore = CalcBandScore(measuredRout, tRout, 0.1);
            finalScore = maxScore + maxIntervalScore + nugeockScore + routScore;
            grade = CalcLetterGrade(finalScore);
            return true;
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
           // AC_CSVFullPath = "C:\\Users\\xodnj\\Desktop\\Vision\\20260625\\WRJKICEOPEN8535TR3909\\RG1260521035A21-9-3-0-0-\\1\\Acceleration";
            //DC_CSVFullPath = "C:\\Users\\xodnj\\Desktop\\Vision\\20260625\\WRJKICEOPEN8535TR3909\\RG1260521035A21-9-3-0-0-\\1\\Deceleration";

            double AC_FinalScore;
            int AC_FinalGrade;

            double DC_FinalScore;
            int DC_FinalGrade;

            //MODEL마다의 기준점을 잡아야 한다!
            //JSON 파일로 기준점들을 저장한다.
            //CurrentModel
            string filePath = "./GradeBaseline.json";
            Console.WriteLine(File.Exists(filePath));
            if (!File.Exists(filePath))
            {
                logger.LogError("Json-GradeBaseline.json", $"기준치 파일 없음{filePath}");
                return;
            }
            else
            {
                //파일 정보저장 
                string jsonString = File.ReadAllText(filePath);
                RootData? rootData = JsonSerializer.Deserialize<RootData>(jsonString);

                if (rootData == null)
                {
                    logger.LogError("Json-GradeBaseline.json", $"해당 파일 파싱 실패 {filePath}");
                    return;
                }



                if (rootData.TryGetValue(CurrentModel, out SignalMetrics? CurrentModelBaseData))
                {
                    if (File.Exists(AC_CSVFullPath + "\\ResultOutput.csv"))
                    {

                        //AC 파일이 제대로 만들어져서 존재 하는 경우
                        string firstLine = File.ReadLines(AC_CSVFullPath + "\\ResultOutput.csv").FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(firstLine))
                        {
                            // 파일은 있지만 내용이 비어 있는 겨우
                            logger.LogError("CSV", $"AC - ResultOutput.csv 파일이 비어 있습니다.  \n파일 경로 :{AC_CSVFullPath + "\\ResultOutput.csv"}");
                        }
                        else
                        {
                            //파일도있고 내부에 내용도 있는 경우
                            // 라인들을 읽어서 새로운 파일들 만들어야함
                            // 0 : PeakX/ 1 : PeakY /2 : AreaX /3 : AreaY/ 4: Length / 5:Heigth /6: Area /7: Distance/
                            string[] lines = File.ReadAllLines(AC_CSVFullPath + "\\ResultOutput.csv");
                            List<double> Peakx_total = new List<double>();
                            List<double> Peaky_total = new List<double>();
                            List<double> Areax_total = new List<double>();
                            List<double> Areay_total = new List<double>();
                            List<double> Width_total = new List<double>();
                            List<double> Height_total = new List<double>();
                            List<double> Area_total = new List<double>();
                            List<double> Distance_total = new List<double>();
                            int DataMaxCount = lines.Length; // 기어마다 개수가 달라지므로!

                            foreach (string line in lines)
                            {
                                string[] values = line.Split(',');
                                Peakx_total.Add(double.Parse(values[1]));
                                Peaky_total.Add(double.Parse(values[2]));
                                Areax_total.Add(double.Parse(values[3]));
                                Areay_total.Add(double.Parse(values[4]));
                                Width_total.Add(double.Parse(values[5]));
                                Height_total.Add(double.Parse(values[6]));
                                Area_total.Add(double.Parse(values[7]));
                                Distance_total.Add(double.Parse(values[8]));
                            }

                            double Peakx_avg = Peakx_total.Average();
                            double Peaky_avg = Peaky_total.Average();
                            double Areax_avg = Areax_total.Average();
                            double Areay_avg = Areay_total.Average();
                            double Width_avg = Width_total.Average();
                            double Height_avg = Height_total.Average();
                            double Area_avg = Area_total.Average();
                            double Distance_avg = Distance_total.Average();

                            //-----------------AC Distance 에 대한 단일 인접 누적 r/out 구하기----------------------
                            double Distance_Max = -1;//Distance의 단일치
                            double Distance_MaxInterval = -1;//Distance의 인접치 차이중 가장큰거
                            double Pre_Distance_data = Distance_total[0];
                            double Distance_nugeock = 0; //peak 누적치;
                            double Distance_MaxDATA = -1;
                            double Distance_MinDATA = 99999;
                            foreach (double Value in Distance_total)
                            {
                                //단일치 구하기
                                if (Distance_Max <= Math.Abs(Value - Distance_avg))
                                {
                                    Distance_Max = Math.Abs(Value - Distance_avg);
                                }
                                //인접치 구하기
                                if (Distance_MaxInterval <= Math.Abs(Value - Pre_Distance_data))
                                {
                                    Distance_MaxInterval = Math.Abs(Value - Pre_Distance_data);
                                }
                                Pre_Distance_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Distance_MaxDATA < Value)
                                {
                                    Distance_MaxDATA = Value;
                                }
                                if (Distance_MinDATA > Value)
                                {
                                    Distance_MinDATA = Value;
                                }

                                Distance_nugeock += Value * Value;
                            }
                            Distance_nugeock /= (double)Distance_total.Count;
                            Distance_nugeock = Math.Sqrt(Distance_nugeock);
                            //R/OUT 구하기
                            double Distance_ROUT = Distance_MaxDATA - Distance_MinDATA;

                            //-----------------AC Areay 에 대한 단일 인접 누적 r/out 구하기----------------------
                            double Areay_Max = -1;//Areay의 단일치
                            double Areay_MaxInterval = -1;//Areay의 인접치 차이중 가장큰거
                            double Pre_Areay_data = Areay_total[0];
                            double Areay_nugeock = 0; //peak 누적치;
                            double Areay_MaxDATA = -1;
                            double Areay_MinDATA = 99999;
                            foreach (double Value in Areay_total)
                            {
                                //단일치 구하기
                                if (Areay_Max <= Math.Abs(Value - Areay_avg))
                                {
                                    Areay_Max = Math.Abs(Value - Areay_avg);
                                }
                                //인접치 구하기
                                if (Areay_MaxInterval <= Math.Abs(Value - Pre_Areay_data))
                                {
                                    Areay_MaxInterval = Math.Abs(Value - Pre_Areay_data);
                                }
                                Pre_Areay_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Areay_MaxDATA < Value)
                                {
                                    Areay_MaxDATA = Value;
                                }
                                if (Areay_MinDATA > Value)
                                {
                                    Areay_MinDATA = Value;
                                }

                                Areay_nugeock += Value * Value;
                            }
                            Areay_nugeock /= (double)Areay_total.Count;
                            Areay_nugeock = Math.Sqrt(Areay_nugeock);
                            //R/OUT 구하기
                            double Areay_ROUT = Areay_MaxDATA - Areay_MinDATA;

                            //-----------------AC AreaX 에 대한 단일 인접 누적 r/out 구하기----------------------
                            double Areax_Max = -1;//Areax의 단일치
                            double Areax_MaxInterval = -1;//Areax의 인접치 차이중 가장큰거
                            double Pre_Areax_data = Areax_total[0];
                            double Areax_nugeock = 0; //peak 누적치;
                            double Areax_MaxDATA = -1;
                            double Areax_MinDATA = 99999;
                            foreach (double Value in Areax_total)
                            {
                                //단일치 구하기
                                if (Areax_Max <= Math.Abs(Value - Areax_avg))
                                {
                                    Areax_Max = Math.Abs(Value - Areax_avg);
                                }
                                //인접치 구하기
                                if (Areax_MaxInterval <= Math.Abs(Value - Pre_Areax_data))
                                {
                                    Areax_MaxInterval = Math.Abs(Value - Pre_Areax_data);
                                }
                                Pre_Areax_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Areax_MaxDATA < Value)
                                {
                                    Areax_MaxDATA = Value;
                                }
                                if (Areax_MinDATA > Value)
                                {
                                    Areax_MinDATA = Value;
                                }

                                Areax_nugeock += Value * Value;
                            }
                            Areax_nugeock /= (double)Areax_total.Count;
                            Areax_nugeock = Math.Sqrt(Areax_nugeock);
                            //R/OUT 구하기
                            double Areax_ROUT = Areax_MaxDATA - Areax_MinDATA;

                            //----------------AC Peakx 단일치 인접치 누적치 r/out 구하기--------------------
                            double Peakx_Max = -1;//peakx의 단일치
                            double Peakx_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                            double Pre_pkakx_data = Peakx_total[0];
                            double Peakx_nugeock = 0; //peak 누적치;
                            double Peakx_MaxDATA = -1;
                            double Peakx_MinDATA = 99999;
                            foreach (double Value in Peakx_total)
                            {
                                //단일치 구하기
                                if (Peakx_Max <= Math.Abs(Value - Peakx_avg))
                                {
                                    Peakx_Max = Math.Abs(Value - Peakx_avg);
                                }
                                //인접치 구하기
                                if (Peakx_MaxInterval <= Math.Abs(Value - Pre_pkakx_data))
                                {
                                    Peakx_MaxInterval = Math.Abs(Value - Pre_pkakx_data);
                                }
                                Pre_pkakx_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Peakx_MaxDATA < Value)
                                {
                                    Peakx_MaxDATA = Value;
                                }
                                if (Peakx_MinDATA > Value)
                                {
                                    Peakx_MinDATA = Value;
                                }

                                Peakx_nugeock += Value * Value;
                            }
                            Peakx_nugeock /= (double)Peakx_total.Count;
                            Peakx_nugeock = Math.Sqrt(Peakx_nugeock);
                            //R/OUT 구하기
                            double Peakx_ROUT = Peakx_MaxDATA - Peakx_MinDATA;

                            //----------------AC Peaky 단일치 인접치 누적치 r/out 구하기--------------------
                            double Peaky_Max = -1;//peakx의 단일치
                            double Peaky_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                            double Pre_pkaky_data = Peaky_total[0];
                            double Peaky_nugeock = 0; //peak 누적치;
                            double Peaky_MaxDATA = -1;
                            double Peaky_MinDATA = 99999;
                            foreach (double peaky_data in Peaky_total)
                            {
                                //단일치 구하기
                                if (Peaky_Max <= Math.Abs(peaky_data - Peaky_avg))
                                {
                                    Peaky_Max = Math.Abs(peaky_data - Peaky_avg);
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
                            Peaky_nugeock /= (double)Peaky_total.Count;
                            Peaky_nugeock = Math.Sqrt(Peaky_nugeock);
                            double Peaky_ROUT = Peaky_MaxDATA - Peaky_MinDATA;

                            //----------------AC Width 단일치 인접치 누적치 r/out 구하기--------------------
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
                            Width_nugeock /= (double)Width_total.Count;
                            Width_nugeock = Math.Sqrt(Width_nugeock);
                            //R/OUT 구하기
                            double Width_ROUT = Width_MaxDATA - Width_MinDATA;


                            //----------------AC Height 단일치 인접치 누적치 r/out 구하기--------------------
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
                            Height_nugeock /= (double)Height_total.Count;
                            Height_nugeock = Math.Sqrt(Height_nugeock);
                            //R/OUT 구하기
                            double Height_ROUT = Height_MaxDATA - Height_MinDATA;

                            //----------------AC Area 단일치 인접치 누적치 r/out 구하기--------------------
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
                            Area_nugeock /= (double)Area_total.Count;
                            Area_nugeock = Math.Sqrt(Area_nugeock);
                            //R/OUT 구하기
                            double Area_ROUT = Area_MaxDATA - Area_MinDATA;
                            //--------------------------------AC Distance의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "Distance", Distance_Max, Distance_MaxInterval, Distance_nugeock, Distance_ROUT,
                                out double Distance_Max_score, out double Distance_MaxInterval_score, out double Distance_nugeock_score, out double Distance_ROUT_score, out double Distance_FinalScore, out string Distance_Grade))
                            {
                                return;
                            }

                            //--------------------------------AC Areay의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "AreaY", Areay_Max, Areay_MaxInterval, Areay_nugeock, Areay_ROUT,
                                out double Areay_Max_score, out double Areay_MaxInterval_score, out double Areay_nugeock_score, out double Areay_ROUT_score, out double Areay_FinalScore, out string Areay_Grade))
                            {
                                return;
                            }

                            //--------------------------------AC Areax의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "AreaX", Areax_Max, Areax_MaxInterval, Areax_nugeock, Areax_ROUT,
                                out double Areax_Max_score, out double Areax_MaxInterval_score, out double Areax_nugeock_score, out double Areax_ROUT_score, out double Areax_FinalScore, out string Areax_Grade))
                            {
                                return;
                            }

                            //--------------------------------AC Peakx의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "PeakX", Peakx_Max, Peakx_MaxInterval, Peakx_nugeock, Peakx_ROUT,
                                out double peakx_Max_score, out double Peakx_MaxInterval_score, out double Peakx_nugeock_score, out double Peakx_ROUT_score, out double Peakx_FinalScore, out string Peakx_Grade))
                            {
                                return;
                            }

                            //--------------------------------AC Peaky의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "PeakY", Peaky_Max, Peaky_MaxInterval, Peaky_nugeock, Peaky_ROUT,
                                out double peaky_Max_score, out double Peaky_MaxInterval_score, out double Peaky_nugeock_score, out double Peaky_ROUT_score, out double Peaky_FinalScore, out string Peaky_Grade))
                            {
                                return;
                            }

                            //-------------------------------AC Width의 스코어 계산! (JSON Length_*)-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "Length", Width_Max, Width_MaxInterval, Width_nugeock, Width_ROUT,
                                out double Width_Max_score, out double Width_MaxInterval_score, out double Width_nugeock_score, out double Width_ROUT_score, out double Width_FinalScore, out string Width_Grade))
                            {
                                return;
                            }

                            //--------------------------------AC Height의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "Height", Height_Max, Height_MaxInterval, Height_nugeock, Height_ROUT,
                                out double Height_Max_score, out double Height_MaxInterval_score, out double Height_nugeock_score, out double Height_ROUT_score, out double Height_FinalScore, out string Height_Grade))
                            {
                                return;
                            }

                            //--------------------------------AC Area의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.AC, "AC", "Area", Area_Max, Area_MaxInterval, Area_nugeock, Area_ROUT,
                                out double Area_Max_score, out double Area_MaxInterval_score, out double Area_nugeock_score, out double Area_ROUT_score, out double Area_FinalScore, out string Area_Grade))
                            {
                                return;
                            }
                            // 가속쪽 최종 점수및 등급 계산
                            // double AC_FinalScore;
                            //int AC_FinalGrade
                            AC_FinalScore = Peakx_FinalScore * 0.2 + Peaky_FinalScore * 0.15 + Areax_FinalScore * 0.2 + Areay_FinalScore * 0.15 + Width_FinalScore * 0.05 + Height_FinalScore * 0.1 + Area_FinalScore * 0.05 + Distance_FinalScore * 0.1;
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
                            string Peaky_scroe = $"Peaky_가중치반영 점수,{peaky_Max_score},{Peaky_MaxInterval_score},{Peaky_nugeock_score},{Peaky_ROUT_score},{Peaky_Grade},{Peaky_FinalScore}";

                            string Areax_value = $"Areax_측정값,{Areax_Max},{Areax_MaxInterval},{Areax_nugeock},{Areax_ROUT},{Areax_Grade},{Areax_FinalScore}";
                            string Areax_scroe = $"Areax_가중치반영 점수,{Areax_Max_score},{Areax_MaxInterval_score},{Areax_nugeock_score},{Areax_ROUT_score},{Areax_Grade},{Areax_FinalScore}";

                            string Areay_value = $"Areay_측정값,{Areay_Max},{Areay_MaxInterval},{Areay_nugeock},{Areay_ROUT},{Areay_Grade},{Areay_FinalScore}";
                            string Areay_scroe = $"Areay_가중치반영 점수,{Areay_Max_score},{Areay_MaxInterval_score},{Areay_nugeock_score},{Areay_ROUT_score},{Areay_Grade},{Areay_FinalScore}";


                            string Width_value = $"Width_측정값,{Width_Max},{Width_MaxInterval},{Width_nugeock},{Width_ROUT},{Width_Grade},{Width_FinalScore}";
                            string Width_scroe = $"Width_가중치반영 점수,{Width_Max_score},{Width_MaxInterval_score},{Width_nugeock_score},{Width_ROUT_score},{Width_Grade},{Width_FinalScore}";

                            string Height_value = $"Height_측정값,{Height_Max},{Height_MaxInterval},{Height_nugeock},{Height_ROUT},{Height_Grade},{Height_FinalScore}";
                            string Height_scroe = $"Height_가중치반영 점수,{Height_Max_score},{Height_MaxInterval_score},{Height_nugeock_score},{Height_ROUT_score},{Height_Grade},{Height_FinalScore}";

                            string Area_value = $"Area_측정값,{Area_Max},{Area_MaxInterval},{Area_nugeock},{Area_ROUT},{Area_Grade},{Area_FinalScore}";
                            string Area_scroe = $"Area_가중치반영 점수,{Area_Max_score},{Area_MaxInterval_score},{Area_nugeock_score},{Area_ROUT_score},{Area_Grade},{Area_FinalScore}";

                            string Distance_value = $"Distance_측정값,{Distance_Max},{Distance_MaxInterval},{Distance_nugeock},{Distance_ROUT},{Distance_Grade},{Distance_FinalScore}";
                            string Distance_scroe = $"Distance_가중치반영 점수,{Distance_Max_score},{Distance_MaxInterval_score},{Distance_nugeock_score},{Distance_ROUT_score},{Distance_Grade},{Distance_FinalScore}";

                            string Total_score = $"Total_score ,{AC_FinalScore}";
                            string Total_Grade = $"Total_Grade ,{AC_FinalGrade}";
                            if (Peakx_avg == 0.0)
                            {
                                Peakx_value = $"Peakx_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Peakx_scroe = $"Peakx_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }
                            if (Peaky_avg == 0.0)
                            {
                                Peaky_value = $"Peaky_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Peaky_scroe = $"Peaky_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Width_avg == 0.0)
                            {
                                Width_value = $"Width_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Width_scroe = $"Width_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Height_avg == 0.0)
                            {
                                Height_value = $"Height_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Height_scroe = $"Height_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Area_avg == 0.0)
                            {
                                Area_value = $"Area_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Area_scroe = $"Area_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Areax_avg == 0.0)
                            {
                                Areax_value = $"Areax_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Areax_scroe = $"Areax_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }


                            if (Areay_avg == 0.0)
                            {
                                Areay_value = $"Areay_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Areay_scroe = $"Areay_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }


                            if (Distance_avg == 0.0)
                            {
                                Distance_value = $"Distance_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Distance_scroe = $"Distance_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }


                            if (Peakx_avg == 0.0 && Peaky_avg == 0.0 && Width_avg == 0.0 && Height_avg == 0.0 && Area_avg == 0.0)
                            {
                                Total_score = $"Total_score , -1";
                                Total_Grade = $"Total_Grade , -1";
                            }

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
                                        sw.WriteLine(Areax_value);
                                        sw.WriteLine(Areax_scroe);
                                        sw.WriteLine(Areay_value);
                                        sw.WriteLine(Areay_scroe);
                                        sw.WriteLine(Width_value);
                                        sw.WriteLine(Width_scroe);
                                        sw.WriteLine(Height_value);
                                        sw.WriteLine(Height_scroe);
                                        sw.WriteLine(Area_value);
                                        sw.WriteLine(Area_scroe);
                                        sw.WriteLine(Distance_value);
                                        sw.WriteLine(Distance_scroe);
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
                        logger.LogError("CSV", $"AC - ResultOutput.csv 파일이 없습니다.  \n파일 경로 :{AC_CSVFullPath + "\\ResultOutput.csv"}");
                    }



                    if (File.Exists(DC_CSVFullPath + "\\ResultOutput.csv"))
                    {
                        //DC 파일이 제대로 만들어져서 존재 하는 경우
                        string firstLine = File.ReadLines(DC_CSVFullPath + "\\ResultOutput.csv").FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(firstLine))
                        {
                            // 파일은 있지만 내용이 비어 있는 겨우
                            logger.LogError("CSV", $"DC - ResultOutput.csv 파일이 비어 있습니다.  \n파일 경로 :{DC_CSVFullPath + "\\ResultOutput.csv"}");
                        }
                        else
                        {
                            //파일도있고 내부에 내용도 있는 경우
                            // 라인들을 읽어서 새로운 파일들 만들어야함
                            double[] sum = new double[5];
                            string[] lines = File.ReadAllLines(DC_CSVFullPath + "\\ResultOutput.csv");
                            List<double> Peakx_total = new List<double>();
                            List<double> Peaky_total = new List<double>();
                            List<double> Areax_total = new List<double>();
                            List<double> Areay_total = new List<double>();
                            List<double> Width_total = new List<double>();
                            List<double> Height_total = new List<double>();
                            List<double> Area_total = new List<double>();
                            List<double> Distance_total = new List<double>();
                            int DataMaxCount = lines.Length; // 기어마다 개수가 달라지므로!
                            foreach (string line in lines)
                            {
                                string[] values = line.Split(',');
                                Peakx_total.Add(double.Parse(values[1]));
                                Peaky_total.Add(double.Parse(values[2]));
                                Areax_total.Add(double.Parse(values[3]));
                                Areay_total.Add(double.Parse(values[4]));
                                Width_total.Add(double.Parse(values[5]));
                                Height_total.Add(double.Parse(values[6]));
                                Area_total.Add(double.Parse(values[7]));
                                Distance_total.Add(double.Parse(values[8]));
                            }
                            double Peakx_avg = Peakx_total.Average();
                            double Peaky_avg = Peaky_total.Average();
                            double Areax_avg = Areax_total.Average();
                            double Areay_avg = Areay_total.Average();
                            double Width_avg = Width_total.Average();
                            double Height_avg = Height_total.Average();
                            double Area_avg = Area_total.Average();
                            double Distance_avg = Distance_total.Average();

                            //-----------------DC Distance 에 대한 단일 인접 누적 r/out 구하기----------------------
                            double Distance_Max = -1;//Distance의 단일치
                            double Distance_MaxInterval = -1;//Distance의 인접치 차이중 가장큰거
                            double Pre_Distance_data = Distance_total[0];
                            double Distance_nugeock = 0; //peak 누적치;
                            double Distance_MaxDATA = -1;
                            double Distance_MinDATA = 99999;
                            foreach (double Value in Distance_total)
                            {
                                //단일치 구하기
                                if (Distance_Max <= Math.Abs(Value - Distance_avg))
                                {
                                    Distance_Max = Math.Abs(Value - Distance_avg);
                                }
                                //인접치 구하기
                                if (Distance_MaxInterval <= Math.Abs(Value - Pre_Distance_data))
                                {
                                    Distance_MaxInterval = Math.Abs(Value - Pre_Distance_data);
                                }
                                Pre_Distance_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Distance_MaxDATA < Value)
                                {
                                    Distance_MaxDATA = Value;
                                }
                                if (Distance_MinDATA > Value)
                                {
                                    Distance_MinDATA = Value;
                                }

                                Distance_nugeock += Value * Value;
                            }
                            Distance_nugeock /= (double)Distance_total.Count;
                            Distance_nugeock = Math.Sqrt(Distance_nugeock);
                            //R/OUT 구하기
                            double Distance_ROUT = Distance_MaxDATA - Distance_MinDATA;

                            //-----------------DC Areay 에 대한 단일 인접 누적 r/out 구하기----------------------
                            double Areay_Max = -1;//Areay의 단일치
                            double Areay_MaxInterval = -1;//Areay의 인접치 차이중 가장큰거
                            double Pre_Areay_data = Areay_total[0];
                            double Areay_nugeock = 0; //peak 누적치;
                            double Areay_MaxDATA = -1;
                            double Areay_MinDATA = 99999;
                            foreach (double Value in Areay_total)
                            {
                                //단일치 구하기
                                if (Areay_Max <= Math.Abs(Value - Areay_avg))
                                {
                                    Areay_Max = Math.Abs(Value - Areay_avg);
                                }
                                //인접치 구하기
                                if (Areay_MaxInterval <= Math.Abs(Value - Pre_Areay_data))
                                {
                                    Areay_MaxInterval = Math.Abs(Value - Pre_Areay_data);
                                }
                                Pre_Areay_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Areay_MaxDATA < Value)
                                {
                                    Areay_MaxDATA = Value;
                                }
                                if (Areay_MinDATA > Value)
                                {
                                    Areay_MinDATA = Value;
                                }

                                Areay_nugeock += Value * Value;
                            }
                            Areay_nugeock /= (double)Areay_total.Count;
                            Areay_nugeock = Math.Sqrt(Areay_nugeock);
                            //R/OUT 구하기
                            double Areay_ROUT = Areay_MaxDATA - Areay_MinDATA;

                            //-----------------DC AreaX 에 대한 단일 인접 누적 r/out 구하기----------------------
                            double Areax_Max = -1;//Areax의 단일치
                            double Areax_MaxInterval = -1;//Areax의 인접치 차이중 가장큰거
                            double Pre_Areax_data = Areax_total[0];
                            double Areax_nugeock = 0; //peak 누적치;
                            double Areax_MaxDATA = -1;
                            double Areax_MinDATA = 99999;
                            foreach (double Value in Areax_total)
                            {
                                //단일치 구하기
                                if (Areax_Max <= Math.Abs(Value - Areax_avg))
                                {
                                    Areax_Max = Math.Abs(Value - Areax_avg);
                                }
                                //인접치 구하기
                                if (Areax_MaxInterval <= Math.Abs(Value - Pre_Areax_data))
                                {
                                    Areax_MaxInterval = Math.Abs(Value - Pre_Areax_data);
                                }
                                Pre_Areax_data = Value;

                                //R/OUT 구하기 위한 사전 준비
                                if (Areax_MaxDATA < Value)
                                {
                                    Areax_MaxDATA = Value;
                                }
                                if (Areax_MinDATA > Value)
                                {
                                    Areax_MinDATA = Value;
                                }

                                Areax_nugeock += Value * Value;
                            }
                            Areax_nugeock /= (double)Areax_total.Count;
                            Areax_nugeock = Math.Sqrt(Areax_nugeock);
                            //R/OUT 구하기
                            double Areax_ROUT = Areax_MaxDATA - Areax_MinDATA;



                            //----------------DC peakx에 대한 단일치 인접치 누적치 r/out 구하기--------------------
                            double Peakx_Max = -1;//peakx의 단일치
                            double Peakx_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                            double Pre_pkakx_data = Peakx_total[0];
                            double Peakx_nugeock = 0; //peak 누적치;
                            double Peakx_MaxDATA = -1;
                            double Peakx_MinDATA = 99999;
                            foreach (double pkakx_data in Peakx_total)
                            {
                                //단일치 구하기
                                if (Peakx_Max <= Math.Abs(pkakx_data - Peakx_avg))
                                {
                                    Peakx_Max = Math.Abs(pkakx_data - Peakx_avg);
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
                            Peakx_nugeock /= (double)Peakx_total.Count;
                            Peakx_nugeock = Math.Sqrt(Peakx_nugeock);
                            //R/OUT 구하기
                            double Peakx_ROUT = Peakx_MaxDATA - Peakx_MinDATA;

                            //-------------- DC peak y에 대한 값 단일치/ 인접치 / 누적치/ rout 구하기------------------
                            double Peaky_Max = -1;//peakx의 단일치
                            double Peaky_MaxInterval = -1;//peakx의 인접치 차이중 가장큰거
                            double Pre_pkaky_data = Peaky_total[0];
                            double Peaky_nugeock = 0; //peak 누적치;
                            double Peaky_MaxDATA = -1;
                            double Peaky_MinDATA = 99999;
                            foreach (double peaky_data in Peaky_total)
                            {
                                //단일치 구하기
                                if (Peaky_Max <= Math.Abs(peaky_data - Peaky_avg))
                                {
                                    Peaky_Max = Math.Abs(peaky_data - Peaky_avg);
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
                            Peaky_nugeock /= (double)Peaky_total.Count;
                            Peaky_nugeock = Math.Sqrt(Peaky_nugeock);
                            //R/OUT 구하기
                            double Peaky_ROUT = Peaky_MaxDATA - Peaky_MinDATA;

                            //--------------DC Width에 대한 값 단일치/ 인접치 / 누적치/ rout 구하기-------------------
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
                            Width_nugeock /= (double)Width_total.Count;
                            Width_nugeock = Math.Sqrt(Width_nugeock);
                            //R/OUT 구하기
                            double Width_ROUT = Width_MaxDATA - Width_MinDATA;


                            //--------------DC height 에대한 단일/ 인점 /누적 /rout 구하기---------------------
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
                            Height_nugeock /= (double)Height_total.Count;
                            Height_nugeock = Math.Sqrt(Height_nugeock);
                            //R/OUT 구하기
                            double Height_ROUT = Height_MaxDATA - Height_MinDATA;

                            //--------------------DC area에 대한 단일/ 인접 /누적 /rout 구하기-------------------
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
                            Area_nugeock /= (double)Area_total.Count;
                            Area_nugeock = Math.Sqrt(Area_nugeock);
                            //R/OUT 구하기
                            double Area_ROUT = Area_MaxDATA - Area_MinDATA;


                    //--------------------------------DCDistance의 스코어 계산!-------------------------------------------------
                    double Distance_Max_score = 0;
                    if (Distance_Max >= 7.7)
                    {
                        Distance_Max_score = 0.2 * 60;
                    }
                    else if (Distance_Max >= 6.7)
                    {
                        Distance_Max_score = 0.2 * 70;
                    }
                    else if (Distance_Max >= 5.9)
                    {
                        Distance_Max_score = 0.2 * 80;
                    }
                    else if (Peakx_Max >= 5.1)
                    {
                        Distance_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Distance_Max_score = 0.2 * 100;
                    }

                    double Distance_MaxInterval_score = 0;
                    if (Distance_MaxInterval >= 4.8)
                    {
                        Distance_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Distance_MaxInterval >= 4.3)
                    {
                        Distance_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Distance_MaxInterval >= 4.0)
                    {
                        Distance_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Distance_MaxInterval >= 3.6)
                    {
                        Distance_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Distance_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Distance_nugeock_score = 0;
                    if (Distance_nugeock >= 3.6)
                    {
                        Distance_nugeock_score = 0.4 * 60;
                    }
                    else if (Distance_nugeock >= 3.0)
                    {
                        Distance_nugeock_score = 0.4 * 70;
                    }
                    else if (Distance_nugeock >= 2.5)
                    {
                        Distance_nugeock_score = 0.4 * 80;
                    }
                    else if (Distance_nugeock >= 2.1)
                    {
                        Distance_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Distance_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Distance_ROUT_score = 0.0;
                    if (Distance_ROUT >= 10.7)
                    {
                        Distance_ROUT_score = 0.1 * 60;
                    }
                    else if (Distance_ROUT >= 9.8)
                    {
                        Distance_ROUT_score = 0.1 * 70;
                    }
                    else if (Distance_ROUT >= 8.9)
                    {
                        Distance_ROUT_score = 0.1 * 80;
                    }
                    else if (Distance_ROUT >= 8.1)
                    {
                        Distance_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Distance_ROUT_score = 0.1 * 100;
                    }
                    double Distance_FinalScore = Distance_Max_score + Distance_MaxInterval_score + Distance_nugeock_score + Distance_ROUT_score;
                    string Distance_Grade;
                    if (Distance_FinalScore >= 96)
                    {
                        Distance_Grade = "A";
                    }
                    else if (Distance_FinalScore >= 91)
                    {
                        Distance_Grade = "B";
                    }
                    else if (Distance_FinalScore >= 86)
                    {
                        Distance_Grade = "C";
                    }
                    else if (Distance_FinalScore >= 81)
                    {
                        Distance_Grade = "D";
                    }
                    else
                    {
                        Distance_Grade = "E";
                    }


                    //--------------------------------DCAreay의 스코어 계산!-------------------------------------------------
                    double Areay_Max_score = 0;
                    if (Areay_Max >= 7.7)
                    {
                        Areay_Max_score = 0.2 * 60;
                    }
                    else if (Areay_Max >= 6.7)
                    {
                        Areay_Max_score = 0.2 * 70;
                    }
                    else if (Areay_Max >= 5.9)
                    {
                        Areay_Max_score = 0.2 * 80;
                    }
                    else if (Peakx_Max >= 5.1)
                    {
                        Areay_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Areay_Max_score = 0.2 * 100;
                    }

                    double Areay_MaxInterval_score = 0;
                    if (Areay_MaxInterval >= 4.8)
                    {
                        Areay_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Areay_MaxInterval >= 4.3)
                    {
                        Areay_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Areay_MaxInterval >= 4.0)
                    {
                        Areay_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Areay_MaxInterval >= 3.6)
                    {
                        Areay_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Areay_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK y 누적치 점수
                    double Areay_nugeock_score = 0;
                    if (Areay_nugeock >= 3.6)
                    {
                        Areay_nugeock_score = 0.4 * 60;
                    }
                    else if (Areay_nugeock >= 3.0)
                    {
                        Areay_nugeock_score = 0.4 * 70;
                    }
                    else if (Areay_nugeock >= 2.5)
                    {
                        Areay_nugeock_score = 0.4 * 80;
                    }
                    else if (Areay_nugeock >= 2.1)
                    {
                        Areay_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Areay_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Areay_ROUT_score = 0.0;
                    if (Areay_ROUT >= 10.7)
                    {
                        Areay_ROUT_score = 0.1 * 60;
                    }
                    else if (Areay_ROUT >= 9.8)
                    {
                        Areay_ROUT_score = 0.1 * 70;
                    }
                    else if (Areay_ROUT >= 8.9)
                    {
                        Areay_ROUT_score = 0.1 * 80;
                    }
                    else if (Areay_ROUT >= 8.1)
                    {
                        Areay_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Areay_ROUT_score = 0.1 * 100;
                    }
                    double Areay_FinalScore = Areay_Max_score + Areay_MaxInterval_score + Areay_nugeock_score + Areay_ROUT_score;
                    string Areay_Grade;
                    if (Areay_FinalScore >= 96)
                    {
                        Areay_Grade = "A";
                    }
                    else if (Areay_FinalScore >= 91)
                    {
                        Areay_Grade = "B";
                    }
                    else if (Areay_FinalScore >= 86)
                    {
                        Areay_Grade = "C";
                    }
                    else if (Areay_FinalScore >= 81)
                    {
                        Areay_Grade = "D";
                    }
                    else
                    {
                        Areay_Grade = "E";
                    }

                    //--------------------------------DC Areax의 스코어 계산!-------------------------------------------------
                    // Areax의 스코어 계산!
                    double Areax_Max_score = 0;
                    if (Areax_Max >= 18.4)
                    {
                        Areax_Max_score = 0.2 * 60;
                    }
                    else if (Areax_Max >= 16.0)
                    {
                        Areax_Max_score = 0.2 * 70;
                    }
                    else if (Areax_Max >= 13.9)
                    {
                        Areax_Max_score = 0.2 * 80;
                    }
                    else if (Areax_Max >= 12.1)
                    {
                        Areax_Max_score = 0.2 * 90;
                    }
                    else
                    {
                        Areax_Max_score = 0.2 * 100;
                    }
                    double Areax_MaxInterval_score = 0;
                    if (Areax_MaxInterval >= 8.1)
                    {
                        Areax_MaxInterval_score = 0.3 * 60;
                    }
                    else if (Areax_MaxInterval >= 7.4)
                    {
                        Areax_MaxInterval_score = 0.3 * 70;
                    }
                    else if (Areax_MaxInterval >= 6.7)
                    {
                        Areax_MaxInterval_score = 0.3 * 80;
                    }
                    else if (Areax_MaxInterval >= 6.1)
                    {
                        Areax_MaxInterval_score = 0.3 * 90;
                    }
                    else
                    {
                        Areax_MaxInterval_score = 0.3 * 100;
                    }
                    /// PEAK X 누적치
                    double Areax_nugeock_score = 0;
                    if (Areax_nugeock >= 8.7)
                    {
                        Areax_nugeock_score = 0.4 * 60;
                    }
                    else if (Areax_nugeock >= 7.3)
                    {
                        Areax_nugeock_score = 0.4 * 70;
                    }
                    else if (Areax_nugeock >= 6.1)
                    {
                        Areax_nugeock_score = 0.4 * 80;
                    }
                    else if (Areax_nugeock >= 6.1)
                    {
                        Areax_nugeock_score = 0.4 * 90;
                    }
                    else
                    {
                        Areax_nugeock_score = 0.4 * 100;
                    }
                    //PEAK X R/OUT
                    double Areax_ROUT_score = 0.0;
                    if (Areax_ROUT >= 26.7)
                    {
                        Areax_ROUT_score = 0.1 * 60;
                    }
                    else if (Areax_ROUT >= 24.3)
                    {
                        Areax_ROUT_score = 0.1 * 70;
                    }
                    else if (Areax_ROUT >= 22.1)
                    {
                        Areax_ROUT_score = 0.1 * 80;
                    }
                    else if (Areax_ROUT >= 20.1)
                    {
                        Areax_ROUT_score = 0.1 * 90;
                    }
                    else
                    {
                        Areax_ROUT_score = 0.1 * 100;
                    }
                    double Areax_FinalScore = Areax_Max_score + Areax_MaxInterval_score + Areax_nugeock_score + Areax_ROUT_score;
                    string Areax_Grade;
                    if (Areax_FinalScore >= 96)
                    {
                        Areax_Grade = "A";
                    }
                    else if (Areax_FinalScore >= 91)
                    {
                        Areax_Grade = "B";
                    }
                    else if (Areax_FinalScore >= 86)
                    {
                        Areax_Grade = "C";
                    }
                    else if (Areax_FinalScore >= 81)
                    {
                        Areax_Grade = "D";
                    }
                    else
                    {
                        Areax_Grade = "E";
                    }

                            //--------------------------------DC Distance의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "Distance", Distance_Max, Distance_MaxInterval, Distance_nugeock, Distance_ROUT,
                                out double Distance_Max_score, out double Distance_MaxInterval_score, out double Distance_nugeock_score, out double Distance_ROUT_score, out double Distance_FinalScore, out string Distance_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Areay의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "AreaY", Areay_Max, Areay_MaxInterval, Areay_nugeock, Areay_ROUT,
                                out double Areay_Max_score, out double Areay_MaxInterval_score, out double Areay_nugeock_score, out double Areay_ROUT_score, out double Areay_FinalScore, out string Areay_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Areax의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "AreaX", Areax_Max, Areax_MaxInterval, Areax_nugeock, Areax_ROUT,
                                out double Areax_Max_score, out double Areax_MaxInterval_score, out double Areax_nugeock_score, out double Areax_ROUT_score, out double Areax_FinalScore, out string Areax_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Peakx의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "PeakX", Peakx_Max, Peakx_MaxInterval, Peakx_nugeock, Peakx_ROUT,
                                out double peakx_Max_score, out double Peakx_MaxInterval_score, out double Peakx_nugeock_score, out double Peakx_ROUT_score, out double Peakx_FinalScore, out string Peakx_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Peaky의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "PeakY", Peaky_Max, Peaky_MaxInterval, Peaky_nugeock, Peaky_ROUT,
                                out double peaky_Max_score, out double Peaky_MaxInterval_score, out double Peaky_nugeock_score, out double Peaky_ROUT_score, out double Peaky_FinalScore, out string Peaky_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Width의 스코어 계산! (JSON Length_*)-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "Length", Width_Max, Width_MaxInterval, Width_nugeock, Width_ROUT,
                                out double Width_Max_score, out double Width_MaxInterval_score, out double Width_nugeock_score, out double Width_ROUT_score, out double Width_FinalScore, out string Width_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Height의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "Height", Height_Max, Height_MaxInterval, Height_nugeock, Height_ROUT,
                                out double Height_Max_score, out double Height_MaxInterval_score, out double Height_nugeock_score, out double Height_ROUT_score, out double Height_FinalScore, out string Height_Grade))
                            {
                                return;
                            }

                            //--------------------------------DC Area의 스코어 계산!-------------------------------------------------
                            if (!TryComputeMetricScores(CurrentModelBaseData.DC, "DC", "Area", Area_Max, Area_MaxInterval, Area_nugeock, Area_ROUT,
                                out double Area_Max_score, out double Area_MaxInterval_score, out double Area_nugeock_score, out double Area_ROUT_score, out double Area_FinalScore, out string Area_Grade))
                            {
                                return;
                            }
                            // 감속 쪽
                            // double DC_FinalScore;
                            //int DC_FinalGrade
                            DC_FinalScore = Peakx_FinalScore * 0.2 + Peaky_FinalScore * 0.15 + Areax_FinalScore * 0.2 + Areay_FinalScore * 0.15 + Width_FinalScore * 0.05 + Height_FinalScore * 0.1 + Area_FinalScore * 0.05 + Distance_FinalScore * 0.1;
                            if (DC_FinalScore >= 96)
                            {
                                DC_FinalGrade = 1;
                            }
                            else if (DC_FinalScore >= 91)
                            {
                                DC_FinalGrade = 2;
                            }
                            else if (DC_FinalScore >= 86)
                            {
                                DC_FinalGrade = 3;
                            }
                            else if (DC_FinalScore >= 81)
                            {
                                DC_FinalGrade = 4;
                            }
                            else
                            {
                                DC_FinalGrade = 5;
                            }

                            //---------------- 등급표 저장-------------- 파일이름 ScoreGrade.csv

                            //---------------- 등급표 저장-------------- 파일이름 ScoreGrade.csv
                            string head = $"Acceleration,단일치,인접치,누적치,R/OUT,등급,점수";
                            string Peakx_value = $"Peakx_측정값,{Peakx_Max},{Peakx_MaxInterval},{Peakx_nugeock},{Peakx_ROUT},{Peakx_Grade},{Peakx_FinalScore}";
                            string Peakx_scroe = $"Peakx_가중치반영 점수,{peakx_Max_score},{Peakx_MaxInterval_score},{Peakx_nugeock_score},{Peakx_ROUT_score},{Peakx_Grade},{Peakx_FinalScore}";

                            string Peaky_value = $"Peaky_측정값,{Peaky_Max},{Peaky_MaxInterval},{Peaky_nugeock},{Peaky_ROUT},{Peaky_Grade},{Peaky_FinalScore}";
                            string Peaky_scroe = $"Peaky_가중치반영 점수,{peaky_Max_score},{Peaky_MaxInterval_score},{Peaky_nugeock_score},{Peaky_ROUT_score},{Peaky_Grade},{Peaky_FinalScore}";

                            string Areax_value = $"Areax_측정값,{Areax_Max},{Areax_MaxInterval},{Areax_nugeock},{Areax_ROUT},{Areax_Grade},{Areax_FinalScore}";
                            string Areax_scroe = $"Areax_가중치반영 점수,{Areax_Max_score},{Areax_MaxInterval_score},{Areax_nugeock_score},{Areax_ROUT_score},{Areax_Grade},{Areax_FinalScore}";

                            string Areay_value = $"Areay_측정값,{Areay_Max},{Areay_MaxInterval},{Areay_nugeock},{Areay_ROUT},{Areay_Grade},{Areay_FinalScore}";
                            string Areay_scroe = $"Areay_가중치반영 점수,{Areay_Max_score},{Areay_MaxInterval_score},{Areay_nugeock_score},{Areay_ROUT_score},{Areay_Grade},{Areay_FinalScore}";


                            string Width_value = $"Width_측정값,{Width_Max},{Width_MaxInterval},{Width_nugeock},{Width_ROUT},{Width_Grade},{Width_FinalScore}";
                            string Width_scroe = $"Width_가중치반영 점수,{Width_Max_score},{Width_MaxInterval_score},{Width_nugeock_score},{Width_ROUT_score},{Width_Grade},{Width_FinalScore}";

                            string Height_value = $"Height_측정값,{Height_Max},{Height_MaxInterval},{Height_nugeock},{Height_ROUT},{Height_Grade},{Height_FinalScore}";
                            string Height_scroe = $"Height_가중치반영 점수,{Height_Max_score},{Height_MaxInterval_score},{Height_nugeock_score},{Height_ROUT_score},{Height_Grade},{Height_FinalScore}";

                            string Area_value = $"Area_측정값,{Area_Max},{Area_MaxInterval},{Area_nugeock},{Area_ROUT},{Area_Grade},{Area_FinalScore}";
                            string Area_scroe = $"Area_가중치반영 점수,{Area_Max_score},{Area_MaxInterval_score},{Area_nugeock_score},{Area_ROUT_score},{Area_Grade},{Area_FinalScore}";

                            string Distance_value = $"Distance_측정값,{Distance_Max},{Distance_MaxInterval},{Distance_nugeock},{Distance_ROUT},{Distance_Grade},{Distance_FinalScore}";
                            string Distance_scroe = $"Distance_가중치반영 점수,{Distance_Max_score},{Distance_MaxInterval_score},{Distance_nugeock_score},{Distance_ROUT_score},{Distance_Grade},{Distance_FinalScore}";

                            string Total_score = $"Total_score ,{DC_FinalScore}";
                            string Total_Grade = $"Total_Grade ,{DC_FinalGrade}";
                            if (Peakx_avg == 0.0)
                            {
                                Peakx_value = $"Peakx_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Peakx_scroe = $"Peakx_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }
                            if (Peaky_avg == 0.0)
                            {
                                Peaky_value = $"Peaky_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Peaky_scroe = $"Peaky_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Width_avg == 0.0)
                            {
                                Width_value = $"Width_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Width_scroe = $"Width_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Height_avg == 0.0)
                            {
                                Height_value = $"Height_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Height_scroe = $"Height_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Area_avg == 0.0)
                            {
                                Area_value = $"Area_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Area_scroe = $"Area_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }

                            if (Areax_avg == 0.0)
                            {
                                Areax_value = $"Areax_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Areax_scroe = $"Areax_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }


                            if (Areay_avg == 0.0)
                            {
                                Areay_value = $"Areay_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Areay_scroe = $"Areay_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }


                            


                            if (Distance_avg == 0.0)
                            {
                                Distance_value = $"Distance_측정값,{-1},{-1},{-1},{-1},{-1},{-1}";
                                Distance_scroe = $"Distance_가중치반영 점수,{-1},{-1},{-1},{-1},{-1},{-1}";
                            }


                            if (Peakx_avg == 0.0 && Peaky_avg == 0.0 && Width_avg == 0.0 && Height_avg == 0.0 && Area_avg == 0.0)
                            {
                                Total_score = $"Total_score , -1";
                                Total_Grade = $"Total_Grade , -1";
                            }


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
                                        sw.WriteLine(Areax_value);
                                        sw.WriteLine(Areax_scroe);
                                        sw.WriteLine(Areay_value);
                                        sw.WriteLine(Areay_scroe);
                                        sw.WriteLine(Width_value);
                                        sw.WriteLine(Width_scroe);
                                        sw.WriteLine(Height_value);
                                        sw.WriteLine(Height_scroe);
                                        sw.WriteLine(Area_value);
                                        sw.WriteLine(Area_scroe);
                                        sw.WriteLine(Distance_value);
                                        sw.WriteLine(Distance_scroe);
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
                        logger.LogError("CSV", $"DC - ResultOutput.csv 파일이 없습니다.  \n파일 경로 :{DC_CSVFullPath + "\\ResultOutput.csv"}");
                    }
                }
                else//모델에 대한 정보가 없는 경우 
                {
                    logger.LogInfo("json", $"선택된 모델:{CurrentModel}에 대한 기준치 데이터없어서 ScoreGrade.csv파일 생성 불가 ");
                }
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
            //--------------★기억 개수에 따른 수정이 필요 한 부분-------------------------
            int MaxCount = jpgFiles.Length;
            if (MaxCount == 43)
            {
                AC_DC_countsub = 21;//21개차이 1 22
            }
            else if (MaxCount == 46)
            {
                AC_DC_countsub = 23;// 23개차이 1,24
            }
            else if(MaxCount == 41)
            {
                AC_DC_countsub = 100000;// 이값들이 기어의 개수에따라 달라짐
            }
            else if(MaxCount == 48)
            {
                AC_DC_countsub = 23;// 23개차이 1,24
            }
            else if(MaxCount == 50)
            {
                AC_DC_countsub = 100000;// 이값들이 기어의 개수에따라 달라짐
            }
            else
            {
                logger.LogError("이미지 파일 오류", $"저장된 이미지 파일의 개수 \n오류 파일 개수{MaxCount} \n 이미지 경로 : {DC_CSVFullPath}", "", "");
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
                    fileNumber = ((fileNumber + AC_DC_countsub) % MaxCount) + 1;

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
