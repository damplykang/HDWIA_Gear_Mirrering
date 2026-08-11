

using ScottPlot.ArrowShapes;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using SkiaSharp;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.LinkLabel;
using static WIA_ViewerProgram.HistoryManager;

using static WIA_ViewerProgram.HistoryManager;

namespace WIA_ViewerProgram
{

    public partial class ViewerForm : Form
    {


       
        public class RangeVluetData
        {
            [JsonPropertyName("PeakX_Base")] public double PeakX_Base { get; set; }
            [JsonPropertyName("PeakX_A_Range")] public double PeakX_A_Range { get; set; }
            [JsonPropertyName("PeakX_B_Range")] public double PeakX_B_Range { get; set; }
            [JsonPropertyName("PeakX_C_Range")] public double PeakX_C_Range { get; set; }
            [JsonPropertyName("PeakX_D_Range")] public double PeakX_D_Range { get; set; }
            [JsonPropertyName("PeakX_E_Range")] public double PeakX_E_Range { get; set; }

            [JsonPropertyName("PeakY_Base")] public double PeakY_Base { get; set; }
            [JsonPropertyName("PeakY_A_Range")] public double PeakY_A_Range { get; set; }
            [JsonPropertyName("PeakY_B_Range")] public double PeakY_B_Range { get; set; }
            [JsonPropertyName("PeakY_C_Range")] public double PeakY_C_Range { get; set; } 
            [JsonPropertyName("PeakY_D_Range")] public double PeakY_D_Range { get; set; }
            [JsonPropertyName("PeakY_E_Range")] public double PeakY_E_Range { get; set; }

            [JsonPropertyName("AreaX_Base")] public double AreaX_Base { get; set; }
            [JsonPropertyName("AreaX_A_Range")] public double AreaX_A_Range { get; set; } 
            [JsonPropertyName("AreaX_B_Range")] public double AreaX_B_Range { get; set; } 
            [JsonPropertyName("AreaX_C_Range")] public double AreaX_C_Range { get; set; }
            [JsonPropertyName("AreaX_D_Range")] public double AreaX_D_Range { get; set; }
            [JsonPropertyName("AreaX_E_Range")] public double AreaX_E_Range { get; set; }

            [JsonPropertyName("AreaY_Base")] public double AreaY_Base { get; set; } 
            [JsonPropertyName("AreaY_A_Range")] public double AreaY_A_Range { get; set; } 
            [JsonPropertyName("AreaY_B_Range")] public double AreaY_B_Range { get; set; }
            [JsonPropertyName("AreaY_C_Range")] public double AreaY_C_Range { get; set; }
            [JsonPropertyName("AreaY_D_Range")] public double AreaY_D_Range { get; set; }
            [JsonPropertyName("AreaY_E_Range")] public double AreaY_E_Range { get; set; }

            [JsonPropertyName("Length_Base")] public double Length_Base { get; set; }
            [JsonPropertyName("Length_A_Range")] public double Length_A_Range { get; set; }
            [JsonPropertyName("Length_B_Range")] public double Length_B_Range { get; set; }
            [JsonPropertyName("Length_C_Range")] public double Length_C_Range { get; set; }
            [JsonPropertyName("Length_D_Range")] public double Length_D_Range { get; set; }
            [JsonPropertyName("Length_E_Range")] public double Length_E_Range { get; set; }

            [JsonPropertyName("Height_Base")] public double Height_Base { get; set; }
            [JsonPropertyName("Height_A_Range")] public double Height_A_Range { get; set; } 
            [JsonPropertyName("Height_B_Range")] public double Height_B_Range { get; set; } 
            [JsonPropertyName("Height_C_Range")] public double Height_C_Range { get; set; } 
            [JsonPropertyName("Height_D_Range")] public double Height_D_Range { get; set; } 
            [JsonPropertyName("Height_E_Range")] public double Height_E_Range { get; set; } 

            [JsonPropertyName("Area_Base")] public double Area_Base { get; set; }
            [JsonPropertyName("Area_A_Range")] public double Area_A_Range { get; set; } 
            [JsonPropertyName("Area_B_Range")] public double Area_B_Range { get; set; } 
            [JsonPropertyName("Area_C_Range")] public double Area_C_Range { get; set; } 
            [JsonPropertyName("Area_D_Range")] public double Area_D_Range { get; set; } 
            [JsonPropertyName("Area_E_Range")] public double Area_E_Range { get; set; } 

            [JsonPropertyName("Distance_Base")] public double Distance_Base { get; set; }
            [JsonPropertyName("Distance_A_Range")] public double Distance_A_Range { get; set; }
            [JsonPropertyName("Distance_B_Range")] public double Distance_B_Range { get; set; }
            [JsonPropertyName("Distance_C_Range")] public double Distance_C_Range { get; set; }
            [JsonPropertyName("Distance_D_Range")] public double Distance_D_Range { get; set; }
            [JsonPropertyName("Distance_E_Range")] public double Distance_E_Range { get; set; }
        }

        public class ModelRangeJson
        {
            [JsonPropertyName("AC")] public RangeVluetData AC { get; set; } = new();
            [JsonPropertyName("DC")] public RangeVluetData DC { get; set; } = new();
        }




        /// <summary>
        /// 검색 결과 목록 한 행: 날짜 루트 아래 BCR명 폴더 / 숫자(시행횟수) 폴더.
        /// </summary>
        internal sealed class ListRowScanEntry
        {
            public string DateStr { get; init; } = "";
            public string BasePath { get; init; } = "";
            public string BcrFolderName { get; init; } = "";
            public int TrialNumber { get; init; }
            /// <summary>시행 횟수 폴더 경로 (끝 구분자 없음).</summary>
            public string TrialFolderPath { get; init; } = "";
        }

        private readonly List<Label> _navigationLabels = new();
        private readonly List<Label> _loginRoleLabels = new();
        private readonly List<Label> _recipeTopLabels = new();
        private readonly List<Label> _imgRangeLabels = new();
        private LoginManager _LoginManager = new LoginManager();
        private string changemode;
        private string seletedcmodel;
        private string Unchangedseletedcmodel;
        private string startdate;
        private string enddate;
        private DirectoryManager _DirectoryManager = new DirectoryManager();
        private readonly Keyence _keyence = new Keyence();
        /// <summary>Keyence TCP 지속 수신(콘솔 출력). KeyenceConnetingbtr 버튼과 연동됩니다.</summary>
        private readonly KeyenceTcpReceiver _keyenceTcpReceiver = new KeyenceTcpReceiver();
        private readonly PLC _plc = new PLC();
        // Start~End(포함) 사이 날짜들을 yyyyMMdd 형태로 저장
        private string[] dateStrArray = Array.Empty<string>();
        private int dateCount;
        private int selectedListSelectRowNumber = -1;
        public string FrontPath;
        public string RearPath;
        string csvFilename = "\\ResultOutput.csv";
        private int SingleStaticPanelCount;
        private int PerulStaticPanelCount;
        private string CalFrontOriginImgPath = "";
        private string CalRearOriginImgPath = "";
        string ComenttxtFileName = "Coment.txt";
        private List<string> _ListFrontPath = new();
        private List<string> _ListRearPath = new();
        OpenCVManager _CV = new OpenCVManager();
        List<string> FtpDateModelPath = new List<string>();
        private readonly List<ListRowScanEntry> _listRowEntries = new();
        /// <summary>목록 BCR 셀 말줄임 시 전체 경로를 표시합니다.</summary>
        private readonly ToolTip _listBcrCellToolTip = new ToolTip();

        private HistoryManager.HistroyManager Logger => HistoryManager.HistroyManager.Instance;

        int SingleStaticSavePoint;

        int ModelBaseLineSettingPanelCount;
        int ModelGradePanelCount = 1;
        public ViewerForm()
        {
            ModelGradePanelCount = 1;
            ModelBaseLineSettingPanelCount = 1;
            SingleStaticPanelCount = 1;
            InitializeComponent();
            SetupModelBaseLineSettingPanelDecimalInput();
            Disposed += (_, _) => _listBcrCellToolTip.Dispose();
            SingleStaticSavePoint = -1;
            // 로고 이미지 로드 및 가운데 정렬 표시
            WIALogoPicotureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Logo", "Nvilogo.jpg");
            if (File.Exists(logoPath))
            {
                WIALogoPicotureBox.Image = Image.FromFile(logoPath);
            }

            // 폼 로드시 날짜 라벨 설정
            Load += ViewerForm_Load;
            // 프로그램 종료 시 TCP 수신 스레드·소켓 정리
            FormClosing += ViewerForm_FormClosing;

            // 시작 위치 강제 설정
            LoginPanel.Location = new Point(210, 162);
            LoginPanel.Size = new Size(1710, 1018);
            TCPIPPanel.Visible = false;
            RECIPEPanel.Visible = false;
            RecipeSelectPanel.Visible = false;

            _navigationLabels.AddRange(new[]
            {
                NavlTCPIPLabel,
                NaviCaldataLabel,
                NaviRecipeLabel,
                NaviLoginLabel,
                NaviEXITLabel
            });

            foreach (var label in _navigationLabels)
            {
                label.Click += OnNavigationLabelClick;
                label.BackColor = Color.White;
            }

            // 로그인 역할 라벨 클릭 토글 설정
            _loginRoleLabels.AddRange(new[]
            {
                LoginMasterLabel,
                LoginAdminLabel,
                LoginOperatorLabel
            });
            foreach (var roleLabel in _loginRoleLabels)
            {
                roleLabel.Click += OnLoginRoleLabelClick;
                roleLabel.BackColor = Color.Silver;
            }

            // 시작 시 로그인 패널을 최우선으로 보이게
            LoginPanel.Visible = true;
            LoginPanel.BringToFront();

            // RECIPE 상단 탭 라벨 토글 설정 (선택된 것만: 글자 Black + 배경 White)
            _recipeTopLabels.AddRange(new[] { RecipeSelectLabel, ListLabel, SingleStaticLabel, PluralStaticLabel });
            foreach (var l in _recipeTopLabels)
            {
                l.Click += OnRecipeTopLabelClick;
            }
            SetRecipeTopSelected(RecipeSelectLabel);

            SingleImgCheckButton.Click += SingleImgCheckButton_Click;

            _imgRangeLabels.AddRange(new[]
            {
                Img1to5Label,
                Img6to10Label,
                Img11to15Label,
                Img16to20Label,
                Img21to25Label,
                Img26to30Label,
                Img31to35Label,
                Img36to40Label,
                Img41to45Label,
                Img46to50Label
            });
            foreach (var imgLabel in _imgRangeLabels)
            {
                imgLabel.Click += OnImgRangeLabelClick;
            }

            // Front/Rear 표시용 좌표/각도/치수 라벨 기본값 초기화
            InitializeFrontRearInfoLabelsToDash();

            SingleStaticPanelCountUpButton.Click += SingleStaticPanelCountUpButton_Click;
            SingleStaticPanelCountDownButton.Click += SingleStaticPanelCountDownButton_Click;
            if (1 <= SingleStaticPanelCount && SingleStaticPanelCount <= 4)
                ApplySingleStaticPanelCountUI();

            FrontOriginPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            RearOriginPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            CalFrontImgSelectButton.Click += CalFrontImgSelectButton_Click;
            CalRearImgSelectButton.Click += CalRearImgSelectButton_Click;

            _keyence.LoadFromJson();
            KeyenceSettingbtr.Click += KeyenceSettingbtr_Click;
            KeyenceConnetingCheckbtr.Click += KeyenceConnetingCheckbtr_Click;
            // 데이터수신 / 수신중지 토글 → Keyence IP·Port로 TCP 연결 후 콘솔에 수신 로그


            _plc.LoadFromJson();
            PLCSettingbtr.Click += PLCSettingbtr_Click;
            MoniteringStartbtr.Click += MoniteringStartbtr_Click;
            MoniteringEndbtr.Click += MoniteringEndbtr_Click;
        }

        private void ShowSingleStaticPanel()
        {
            SingleStaticPanel.Location = new Point(10, 97);
            SingleStaticPanel.Size = new Size(1699, 819);
            SingleStaticPanel.Visible = true;
            ApplySingleStaticPanelCountUI();
        }

        private void ApplySingleStaticPanelCountUI()
        {
            SingletaticDisplayLabel.Text = SingleStaticPanelCount switch
            {
                1 => "단일 통계 AC/DC PeakX/Y",
                2 => "단일 통계 AC/DC Length,Height,Area",
                3 => "단일 통계 AC/DC AreaX/Y Distance(Area-Peak)",
                4 => "단일 통계 등급 및 이상치",
                5 => "단일 통계 배면 런아웃 결과",
                _ => $"-"
            };

            if (SingleStaticPanelCount == 1)
            {
                SingleStaticPanel_1.Location = new Point(0, 70);
                SingleStaticPanel_1.Size = new Size(1700, 746);
                SingleStaticPanel_1.BackColor = Color.Black;
                SingleStaticPanel_1.Visible = true;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_5.Visible = false;

            }
            else if (SingleStaticPanelCount == 2)
            {
                SingleStaticPanel_2.Location = new Point(0, 70);
                SingleStaticPanel_2.Size = new Size(1700, 746);
                SingleStaticPanel_2.BackColor = Color.Black;
                SingleStaticPanel_2.Visible = true;
                SingleStaticPanel_1.Visible = false;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_4.Visible = false;
                SingleStaticPanel_5.Visible = false;
            }

            else if (SingleStaticPanelCount == 3)
            {
                SingleStaticPanel_3.Location = new Point(0, 70);
                SingleStaticPanel_3.Size = new Size(1700, 746);
                SingleStaticPanel_3.BackColor = Color.Black;
                SingleStaticPanel_3.Visible = true;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_1.Visible = false;
                SingleStaticPanel_4.Visible = false;
                SingleStaticPanel_5.Visible = false;
            }
            else if (SingleStaticPanelCount == 4)
            {
                SingleStaticPanel_4.Location = new Point(0, 70);
                SingleStaticPanel_4.Size = new Size(1700, 746);
                SingleStaticPanel_4.BackColor = Color.Black;
                SingleStaticPanel_4.Visible = true;
                SingleStaticPanel_1.Visible = false;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_5.Visible = false;
            }
            else if (SingleStaticPanelCount == 5)
            {
                SingleStaticPanel_5.Location = new Point(0, 70);
                SingleStaticPanel_5.Size = new Size(1700, 746);
                SingleStaticPanel_5.BackColor = Color.Black;
                SingleStaticPanel_5.Visible = true;
                SingleStaticPanel_1.Visible = false;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_4.Visible = false;
            }
            else
            {
                SingleStaticPanel_1.Visible = false;
                SingleStaticPanel_4.Visible = false;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_5.Visible = false;
            }
        }


        private void SingleStaticPanelCountUpButton_Click(object? sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }
            SingleStaticPanelCount++;

            if (SingleStaticPanelCount >= 6)
            {
                SingleStaticPanelCount = 5;
                return;
            }

            ApplySingleStaticPanelCountUI();
        }

        private void SingleStaticPanelCountDownButton_Click(object? sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            SingleStaticPanelCount--;
            if (SingleStaticPanelCount <= 0)
            {
                SingleStaticPanelCount = 0;
                return;
            }
            ApplySingleStaticPanelCountUI();
        }

        private void InitializeFrontRearInfoLabelsToDash()
        {
            for (int i = 1; i <= 5; i++)
            {
                // Front: F{n}AreaX, F{n}AreaY, F{n}PeakX, F{n}PeakY, F{n}Width, F{n}Height, F{n}Area, F{n}Angle
                SetLabelTextRecursive($"F{i}AreaX", "-");
                SetLabelTextRecursive($"F{i}AreaY", "-");
                SetLabelTextRecursive($"F{i}PeakX", "-");
                SetLabelTextRecursive($"F{i}PeakY", "-");
                SetLabelTextRecursive($"F{i}Width", "-");
                SetLabelTextRecursive($"F{i}Height", "-");
                SetLabelTextRecursive($"F{i}Area", "-");
                SetLabelTextRecursive($"F{i}Angle", "-");

                // Rear: R{n}AreaX, R{n}AreaY, R{n}PeakX, R{n}PeakY, R{n}Width, R{n}Height, R{n}Area, R{n}Angle
                SetLabelTextRecursive($"R{i}AreaX", "-");
                SetLabelTextRecursive($"R{i}AreaY", "-");
                SetLabelTextRecursive($"R{i}PeakX", "-");
                SetLabelTextRecursive($"R{i}PeakY", "-");
                SetLabelTextRecursive($"R{i}Width", "-");
                SetLabelTextRecursive($"R{i}Height", "-");
                SetLabelTextRecursive($"R{i}Area", "-");
                SetLabelTextRecursive($"R{i}Angle", "-");
            }
        }

        //라벨 이름과 text넣으면 text가 바뀜
        //중요!
        private void SetLabelTextRecursive(string labelName, string text)
        {
            var matches = this.Controls.Find(labelName, true);//라벨 이름을 찾는 함수
            if (matches.Length == 0)
            {
                return;
            }

            if (matches[0] is Label lb)
            {
                lb.Text = text;
            }
        }

        private static void SetImgRangeLabelDefault(Label label)
        {
            label.BackColor = Color.DimGray;
            label.ForeColor = Color.White;
        }

        private static void SetImgRangeLabelSelected(Label label)
        {
            label.BackColor = Color.White;
            label.ForeColor = Color.Black;
        }

        private void OnImgRangeLabelClick(object? sender, EventArgs e)
        {

            int MaxGearHallCount = 43;


            if (!EnsureLoggedIn())
            {
                return;
            }

            if (sender is not Label clicked || !_imgRangeLabels.Contains(clicked))
            {
                return;
            }

            foreach (var l in _imgRangeLabels)
            {
                SetImgRangeLabelDefault(l);
            }

            SetImgRangeLabelSelected(clicked);

            // Img1to5Label, Img6to10Label 등 라벨 이름에서 시작/끝 번호를 추출
            // 형식: "Img{start}to{end}Label"
            var name = clicked.Name; // 예: "Img6to10Label"
            var core = name.Replace("Img", "").Replace("Label", ""); // "6to10"
            var parts = core.Split("to", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var start)
                || !int.TryParse(parts[1], out var end))
            {
                return;
            }

            // start~end 구간을 5개로 F1~F5, R1~R5에 매핑
            // 예: start=1 → [1,2,3,4,5], start=6 → [6,7,8,9,10]
            for (int i = 0; i < 5; i++)
            {
                var value = start + i;
                switch (i)
                {
                    case 0:
                        F1.Text = value.ToString();
                        R1.Text = value.ToString();
                        break;
                    case 1:
                        F2.Text = value.ToString();
                        R2.Text = value.ToString();
                        break;
                    case 2:
                        F3.Text = value.ToString();
                        R3.Text = value.ToString();
                        break;
                    case 3:
                        F4.Text = value.ToString();
                        R4.Text = value.ToString();
                        break;
                    case 4:
                        F5.Text = value.ToString();
                        R5.Text = value.ToString();
                        break;
                }
            }


            // 이미지 구간 선택 시 앞/뒤 이미지 패널 표시
            SicngleImgCheckPanel.Visible = true;
            SicngleImgCheckPanel.BringToFront();
            FrontDisplayPanel.Visible = true;
            RearDisplayPanel.Visible = true;

            // 우선순위 보이기
            SicngleImgCheckPanel.BringToFront();
            FrontDisplayPanel.BringToFront();
            RearDisplayPanel.BringToFront();



            // 해당 폴더에 RESULTOUTPUT파일이 있는지 확인!
            //만약 있다면 모든 데이터 읽어오기 

            string frontcsv = FrontPath + csvFilename;
            string reartcsv = RearPath + csvFilename;

            //최종 파일이 없는 경우라면 최종파일을 만들어 줘야함
            if (!(File.Exists(frontcsv)))
            {
                if (Directory.Exists(FrontPath)) //디렉토리가 있는 경우라면 찾아 들어가서 ac의 결과를 만들어준다
                {
                    makeResultOutput(FrontPath);
                }
                else
                {
                    ShowMissingFileWarning("해당 디렉토리 없음", new List<string> { FrontPath });
                }
            }

            if (!(File.Exists(reartcsv)))
            {
                if (Directory.Exists(RearPath)) //디렉토리가 있는 경우라면 찾아 들어가서 dc의 결과를 만들어준다
                {
                    makeResultOutput(RearPath);
                }
                else
                {
                    ShowMissingFileWarning("해당 디렉토리 없음", new List<string> { RearPath });
                }
            }



            //CSV에 있는 값들읽어서 로딩해야함
            //private void SetLabelTextRecursive(string labelName, string text)

            //front의 csv파일 읽어와서 업데이트
            var missingCsv = new List<string>();
            if (!File.Exists(frontcsv)) missingCsv.Add(frontcsv);
            if (!File.Exists(reartcsv)) missingCsv.Add(reartcsv);
            if (missingCsv.Count > 0)
            {
                Logger.LogWarning("FileIO", "CSV 파일 없음 (이미지 체크)", _LoginManager?.UserInputID ?? "", string.Join(" | ", missingCsv));
                ShowMissingFileWarning("CSV 파일 없음", missingCsv);
                InitializeFrontRearInfoLabelsToDash();
                return;
            }

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    int temp;
                    int.TryParse(F1.Text, out temp);
                    string? frontLine = ReadLinesShared(frontcsv).Skip(temp + i - 1).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(frontLine))
                    {
                        Logger.LogWarning("FileIO", "Acceleration CSV 라인 없음", _LoginManager?.UserInputID ?? "", $"{frontcsv} | index={temp + i - 1}");
                        SetLabelTextRecursive($"F{i + 1}AreaX", "-");
                        SetLabelTextRecursive($"F{i + 1}AreaY", "-");
                        SetLabelTextRecursive($"F{i + 1}PeakX", "-");
                        SetLabelTextRecursive($"F{i + 1}PeakY", "-");
                        SetLabelTextRecursive($"F{i + 1}Width", "-");
                        SetLabelTextRecursive($"F{i + 1}Height", "-");
                        SetLabelTextRecursive($"F{i + 1}Area", "-");
                        SetLabelTextRecursive($"F{i + 1}Angle", "-");
                    }
                    else
                    {
                        string[] frontData = frontLine.Split(',');
                        if (frontData.Length < 9)
                        {
                            Logger.LogWarning("FileIO", "Acceleration CSV 포맷 이상", _LoginManager?.UserInputID ?? "", $"{frontcsv} | line={frontLine}");
                        }
                        SetLabelTextRecursive($"F{i + 1}AreaX", frontData.Length > 1 ? frontData[1] : "-");
                        SetLabelTextRecursive($"F{i + 1}AreaY", frontData.Length > 2 ? frontData[2] : "-");
                        SetLabelTextRecursive($"F{i + 1}PeakX", frontData.Length > 3 ? frontData[3] : "-");
                        SetLabelTextRecursive($"F{i + 1}PeakY", frontData.Length > 4 ? frontData[4] : "-");
                        SetLabelTextRecursive($"F{i + 1}Width", frontData.Length > 5 ? frontData[5] : "-");
                        SetLabelTextRecursive($"F{i + 1}Height", frontData.Length > 6 ? frontData[6] : "-");
                        SetLabelTextRecursive($"F{i + 1}Area", frontData.Length > 7 ? frontData[7] : "-");
                        SetLabelTextRecursive($"F{i + 1}Angle", frontData.Length > 8 ? frontData[8] : "-");
                    }

                    int.TryParse(R1.Text, out temp);
                    string? rearLine = ReadLinesShared(reartcsv).Skip(temp + i - 1).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(rearLine))
                    {
                        Logger.LogWarning("FileIO", "Rear CSV 라인 없음", _LoginManager?.UserInputID ?? "", $"{reartcsv} | index={temp + i - 1}");
                        SetLabelTextRecursive($"R{i + 1}AreaX", "-");
                        SetLabelTextRecursive($"R{i + 1}AreaY", "-");
                        SetLabelTextRecursive($"R{i + 1}PeakX", "-");
                        SetLabelTextRecursive($"R{i + 1}PeakY", "-");
                        SetLabelTextRecursive($"R{i + 1}Width", "-");
                        SetLabelTextRecursive($"R{i + 1}Height", "-");
                        SetLabelTextRecursive($"R{i + 1}Area", "-");
                        SetLabelTextRecursive($"R{i + 1}Angle", "-");
                    }
                    else
                    {
                        string[] rearData = rearLine.Split(',');
                        if (rearData.Length < 9)
                        {
                            Logger.LogWarning("FileIO", "Rear CSV 포맷 이상", _LoginManager?.UserInputID ?? "", $"{reartcsv} | line={rearLine}");
                        }
                        SetLabelTextRecursive($"R{i + 1}AreaX", rearData.Length > 1 ? rearData[1] : "-");
                        SetLabelTextRecursive($"R{i + 1}AreaY", rearData.Length > 2 ? rearData[2] : "-");
                        SetLabelTextRecursive($"R{i + 1}PeakX", rearData.Length > 3 ? rearData[3] : "-");
                        SetLabelTextRecursive($"R{i + 1}PeakY", rearData.Length > 4 ? rearData[4] : "-");
                        SetLabelTextRecursive($"R{i + 1}Width", rearData.Length > 5 ? rearData[5] : "-");
                        SetLabelTextRecursive($"R{i + 1}Height", rearData.Length > 6 ? rearData[6] : "-");
                        SetLabelTextRecursive($"R{i + 1}Area", rearData.Length > 7 ? rearData[7] : "-");
                        SetLabelTextRecursive($"R{i + 1}Angle", rearData.Length > 8 ? rearData[8] : "-");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("FileIO", "CSV 읽기/반영 실패 (이미지 체크)", _LoginManager?.UserInputID ?? "", $"{frontcsv} | {reartcsv} | {ex}");
                MessageBox.Show(
                    this,
                    "CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.",
                    "CSV 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                InitializeFrontRearInfoLabelsToDash();
                return;
            }



            ///f1~f5picture 박스와 r1~r5의 픽처박스를 업데이트하자!
            /* public string FrontPath;
              public string RearPath;*/
            F1picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            F2picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            F3picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            F4picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            F5picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            R1picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            R2picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            R3picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            R4picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            R5picturebox.SizeMode = PictureBoxSizeMode.StretchImage;
            var missingImages = new List<string>();
            string f1Path = null;
            string f2Path = null;
            string f3Path = null;
            string f4Path = null;
            string f5Path = null;
            string r1Path = null;
            string r2Path = null;
            string r3Path = null;
            string r4Path = null;
            string r5Path = null;



            if (int.Parse(F1.Text) >= 10)
            {
                f1Path = Path.Combine(FrontPath, $"{F1.Text}.jpg");
            }
            else
            {
                f1Path = Path.Combine(FrontPath, $"0{F1.Text}.jpg");
            }

            if (int.Parse(F2.Text) >= 10)
            {
                f2Path = Path.Combine(FrontPath, $"{F2.Text}.jpg");
            }
            else
            {
                f2Path = Path.Combine(FrontPath, $"0{F2.Text}.jpg");
            }

            if (int.Parse(F3.Text) >= 10)
            {
                f3Path = Path.Combine(FrontPath, $"{F3.Text}.jpg");
            }
            else
            {
                f3Path = Path.Combine(FrontPath, $"0{F3.Text}.jpg");
            }

            if (int.Parse(F4.Text) >= 10)
            {
                f4Path = Path.Combine(FrontPath, $"{F4.Text}.jpg");
            }
            else
            {
                f4Path = Path.Combine(FrontPath, $"0{F4.Text}.jpg");
            }
            if (int.Parse(F5.Text) >= 10)
            {
                f5Path = Path.Combine(FrontPath, $"{F5.Text}.jpg");
            }
            else
            {
                f5Path = Path.Combine(FrontPath, $"0{F5.Text}.jpg");
            }


            if (int.Parse(R1.Text) >= 10)
            {
                r1Path = Path.Combine(RearPath, $"{R1.Text}.jpg");
            }
            else if(MaxGearHallCount==46) {// 23개차이
                AC_DC_countsub = 22;
            }
            else if (MaxGearHallCount == 41)
            {
                r1Path = Path.Combine(RearPath, $"0{R1.Text}.jpg");
            }
            else if (MaxGearHallCount == 48)
            {

            if (int.Parse(R2.Text) >= 10)
            {
                r2Path = Path.Combine(RearPath, $"{R2.Text}.jpg");
            }



            if (int.Parse(R1.Text) <= MaxGearHallCount)
            {
                r1_Count = ((int.Parse(R1.Text) + AC_DC_countsub) % MaxGearHallCount) + 1;
            }
            else
            {
                r2Path = Path.Combine(RearPath, $"0{R2.Text}.jpg");
            }

            if (int.Parse(R3.Text) >= 10)
            {
                r3Path = Path.Combine(RearPath, $"{R3.Text}.jpg");
            }
            else
            {
                r3Path = Path.Combine(RearPath, $"0{R3.Text}.jpg");
            }

            if (int.Parse(R4.Text) >= 10)
            {
                r4Path = Path.Combine(RearPath, $"{R4.Text}.jpg");
            }
            else
            {
                r4Path = Path.Combine(RearPath, $"0{R4.Text}.jpg");
            }
            if (int.Parse(R5.Text) >= 10)
            {
                r5Path = Path.Combine(RearPath, $"{R5.Text}.jpg");
            }
            else
            {
                r5Path = Path.Combine(RearPath, $"0{R5.Text}.jpg");
            }

            if (int.Parse(R5.Text) <= MaxGearHallCount)
            {
                r5_Count = ((int.Parse(R5.Text) + AC_DC_countsub) % MaxGearHallCount) + 1;
            }
        }

        private static int CountListSelectWithV(Control listDisplayPanel)
        {
            var count = 0;
            foreach (Control row in listDisplayPanel.Controls)
            {
                if (row is not Panel)
                {
                    continue;
                }

                foreach (Control child in row.Controls)
                {
                    if (child is Label lb
                        && lb.Name.StartsWith("ListSelect", StringComparison.Ordinal)
                        && lb.Name != "ListSelect"
                        && lb.Text == "V")
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static Label? FindListSelectLabelWithV(Control listDisplayPanel)
        {
            foreach (Control row in listDisplayPanel.Controls)
            {
                if (row is not Panel)
                {
                    continue;
                }

                foreach (Control child in row.Controls)
                {
                    if (child is Label lb
                        && lb.Name.StartsWith("ListSelect", StringComparison.Ordinal)
                        && lb.Name != "ListSelect"
                        && lb.Text == "V")
                    {
                        return lb;
                    }
                }
            }

            return null;
        }

        private static string ExtractListSelectRowNumber(string labelName)
        {
            const string prefix = "ListSelect";
            if (!labelName.StartsWith(prefix, StringComparison.Ordinal)
                || labelName.Length <= prefix.Length)
            {
                return "";
            }

            return labelName[prefix.Length..];
        }

        /// <summary>
        /// {날짜}\{모델}\ 아래 1단계(BCR명)·2단계(숫자=시행) 폴더만 행으로 인식합니다.
        /// </summary>
        private static List<ListRowScanEntry> ScanBcrTrialRowsUnderDateModelRoot(string dateStr, string basePath)
        {
            var list = new List<ListRowScanEntry>();
            if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
            {
                return list;
            }

            foreach (var bcrFull in Directory.GetDirectories(basePath))
            {
                var bcrName = Path.GetFileName(bcrFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                foreach (var trialFull in Directory.GetDirectories(bcrFull))
                {
                    var trialFolderName = Path.GetFileName(trialFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (!int.TryParse(trialFolderName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var trialNum))
                    {
                        continue;
                    }

                    list.Add(new ListRowScanEntry
                    {
                        DateStr = dateStr,
                        BasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        BcrFolderName = bcrName,
                        TrialNumber = trialNum,
                        TrialFolderPath = trialFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    });
                }
            }

            return list;
        }

        /// <summary>목록 TEST 일시 열: 날짜(yyyyMMdd) + 시행 폴더 마지막 수정 시분초.</summary>
        private static string FormatListDateWithFolderLastWrite(string dateStr, string trialFolderPath)
        {


            string temp = trialFolderPath + "\\Acceleration\\01.jpg";
            if (!File.Exists(temp))
            {
                return dateStr + " - ";
            }

            try
            {
                var lastWrite = File.GetLastWriteTime(temp);
                return $"{dateStr} {lastWrite:HH:mm:ss}"; // 필요하면 ss 해서 초 추가 하면 됨
            }
            catch
            {
                return dateStr + " - "; ;
            }
        }

        /// <summary>목록에서 V로 선택된 행의 시행 항목을 행 번호 순으로 반환합니다.</summary>
        private List<ListRowScanEntry> CollectSelectedListRowEntriesOrderedByRow()
        {
            var indices = new SortedSet<int>();
            foreach (Control row in ListDisplyPanel.Controls)
            {
                if (row is not Panel)
                {
                    continue;
                }

                foreach (Control child in row.Controls)
                {
                    if (child is not Label selectLb
                        || !selectLb.Name.StartsWith("ListSelect", StringComparison.Ordinal)
                        || selectLb.Name == "ListSelect"
                        || selectLb.Text != "V")
                    {
                        continue;
                    }

                    var suffix = ExtractListSelectRowNumber(selectLb.Name);
                    if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    {
                        indices.Add(n);
                    }

                    break;
                }
            }

            var result = new List<ListRowScanEntry>();
            foreach (var n in indices)
            {
                if (n >= 1 && n <= _listRowEntries.Count)
                {
                    result.Add(_listRowEntries[n - 1]);
                }
            }

            return result;
        }

        private void SingleImgCheckButton_Click(object? sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }



            var vCount = CountListSelectWithV(ListDisplyPanel);
            if (vCount == 0 || vCount >= 2)
            {
                MessageBox.Show(
                    this,
                    "1개만 선택해 주세요.",
                    "선택 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var vLabel = FindListSelectLabelWithV(ListDisplyPanel);
            if (vLabel == null)
            {
                return;
            }

            SicngleImgCheckPanel_setup();
            SicngleImgCheckPanel.Visible = true;
            SingleStaticPanel.Visible = false;
            SicngleImgCheckPanel.BringToFront();
            selectedListSelectRowNumber = -1;
            string temp = ExtractListSelectRowNumber(vLabel.Name);
            selectedListSelectRowNumber = Convert.ToInt32(temp);
            if (selectedListSelectRowNumber < 1 || selectedListSelectRowNumber > _listRowEntries.Count)
            {
                MessageBox.Show(
                    this,
                    "선택한 행에 해당하는 폴더 정보가 없습니다. 검색을 다시 실행해 주세요.",
                    "목록 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var rowEntry = _listRowEntries[selectedListSelectRowNumber - 1];

            FrontPath = Path.Combine(rowEntry.TrialFolderPath, "Acceleration");
            RearPath = Path.Combine(rowEntry.TrialFolderPath, "Deceleration");


            SicngleImgCheckPanel_setup();
        }
        private void SicngleImgCheckPanel_setup()
        {
            SicngleImgCheckPanel.Location = new Point(0, 0);
            SicngleImgCheckPanel.Size = new Size(1709, 916);
            SicngleImgCheckPanel.Visible = true;
            //frontdisplaypanel / reardisplaypanel

        }

        private void ViewerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 종료 시점에도 백그라운드 TcpClient가 남지 않도록 수신 중지
            _keyenceTcpReceiver.Stop();
            UpdateKeyenceConnetingbtrText(false);
        }

        private void ViewerForm_Load(object? sender, EventArgs e)
        {
            TopDateLabel.Text = DateTime.Now.ToString("MM/dd/yy");
            TodayDateLabel.Text = DateTime.Now.ToString("yyyy.MM.dd");
            RefreshKeyenceTcpDisplay();
            RefreshPlcTcpDisplay();
        }

        private void RefreshKeyenceTcpDisplay()
        {
            KeyencIP.Text = string.IsNullOrWhiteSpace(_keyence.Ip) ? "-" : _keyence.Ip;
            label8.Text = _keyence.PortNumber > 0
                ? _keyence.PortNumber.ToString()
                : "-";
        }

        private void KeyenceSettingbtr_Click(object? sender, EventArgs e)
        {
            _keyence.LoadFromJson();
            using var dlg = new KeyenceSettingForm(_keyence);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshKeyenceTcpDisplay();
            }
        }

        private void KeyenceConnetingCheckbtr_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_keyence.Ip) || _keyence.PortNumber <= 0)
            {
                MessageBox.Show(
                    this,
                    "Keyence IP와 Port를 먼저 설정해 주세요.",
                    "알림",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dlg = new KeyenceConnectionCheckForm(_keyence);
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// KeyenceConnetingbtr 클릭: 수신 중이 아닐 때는 TCP 수신 시작, 수신 중일 때는 중지.
        /// Toggle 내부에서 백그라운드 스레드가 돌아가므로 UI 갱신은 SyncUi로 UI 스레드에 보냅니다.
        /// </summary>
        private void KeyenceConnetingbtr_Click(object? sender, EventArgs e)
        {
            // KeyenceTcpReceiver가 syncUi를 백그라운드에서 호출할 수 있음 → 반드시 Invoke
            void SyncUi(bool receiving)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() => UpdateKeyenceConnetingbtrText(receiving));
                }
                else
                {
                    UpdateKeyenceConnetingbtrText(receiving);
                }
            }

            // 시작 전에만 IP/Port 필수 (수신 중일 때는 같은 버튼으로 중지만 허용)
            if (!_keyenceTcpReceiver.IsReceiving
                && (string.IsNullOrWhiteSpace(_keyence.Ip) || _keyence.PortNumber <= 0))
            {
                MessageBox.Show(
                    this,
                    "Keyence IP와 Port를 먼저 설정해 주세요.",
                    "알림",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _ = _keyenceTcpReceiver.Toggle(_keyence, SyncUi);
        }

        /// <summary>수신 중이면 버튼 문구를 "수신중지", 아니면 "데이터수신".</summary>
        private void UpdateKeyenceConnetingbtrText(bool receiving)
        {

        }

        private void RefreshPlcTcpDisplay()
        {
            PLCIP.Text = string.IsNullOrWhiteSpace(_plc.Ip) ? "-" : _plc.Ip;
            StationNumber.Text = _plc.StationNumber >= 0
                ? _plc.StationNumber.ToString()
                : "-";
            label12.Text = _plc.MoniteringCycle > 0
                ? $"{_plc.MoniteringCycle} ms"
                : "-";
            label14.Text = string.IsNullOrWhiteSpace(_plc.MoniterAdrress)
                ? "-"
                : _plc.MoniterAdrress.Trim();
        }

        private void PLCSettingbtr_Click(object? sender, EventArgs e)
        {
            _plc.LoadFromJson();
            using var dlg = new PLCSettingForm(_plc);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshPlcTcpDisplay();
            }
        }

        private void MoniteringStartbtr_Click(object? sender, EventArgs e)
        {
            if (_plc.StationNumber < 0
                || _plc.MoniteringCycle < 1
                || string.IsNullOrWhiteSpace(_plc.MoniterAdrress))
            {
                MessageBox.Show(
                    this,
                    "MX Component 논리 스테이션 번호(Station 번호), MoniteringCycle, 모니터 주소(MoniterAdrress)를 설정한 뒤 다시 시도하세요.\n"
                    + "(ActUtlType64는 PC에 MX Component가 설치되어 있어야 합니다.)",
                    "모니터링 시작 불가",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _plc.MoniteringStart();
        }

        private void MoniteringEndbtr_Click(object? sender, EventArgs e)
        {
            _plc.MoniteringEnd();
        }

        private void HideFeaturePanelsForLoginGate()
        {
            TCPIPPanel.Visible = false;
            RECIPEPanel.Visible = false;
            RecipeSelectPanel.Visible = false;
            ListPanel.Visible = false;
            SingleStaticPanel.Visible = false;
            PerulStaticPanel.Visible = false;
            SicngleImgCheckPanel.Visible = false;
            FrontDisplayPanel.Visible = false;
            RearDisplayPanel.Visible = false;
        }

        private void ShowLoginRequiredFocusLogin()
        {
            MessageBox.Show(
                this,
                "로그인이 필요합니다.\n로그인 후 이용해 주세요.",
                "로그인 필요",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            HideFeaturePanelsForLoginGate();

            LoginPanel.Visible = true;
            LoginPanel.BringToFront();

            foreach (var label in _navigationLabels)
            {
                label.BackColor = label == NaviLoginLabel ? Color.Silver : Color.White;
            }
        }

        private bool EnsureLoggedIn()
        {
            if (_LoginManager.BoolLoginCheck)
            {
                return true;
            }

            ShowLoginRequiredFocusLogin();
            return false;
        }

        private void ShowMissingFileWarning(string title, IEnumerable<string> paths)
        {
            var list = paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (list.Count == 0)
            {
                return;
            }

            var preview = string.Join(Environment.NewLine, list.Take(10));
            var suffix = list.Count > 10 ? $"{Environment.NewLine}... (총 {list.Count}개)" : "";

            MessageBox.Show(
                this,
                $"파일을 찾을 수 없습니다.{Environment.NewLine}{Environment.NewLine}{preview}{suffix}",
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private bool TryLoadPicture(PictureBox pb, string path, string context)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Logger.LogWarning("FileIO", $"이미지 파일 없음 ({context})", _LoginManager?.UserInputID ?? "", path);
                    pb.Image = null;
                    return false;
                }

                pb.LoadAsync(path);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("FileIO", $"이미지 로드 실패 ({context})", _LoginManager?.UserInputID ?? "", $"{path} | {ex}");
                pb.Image = null;
                return false;
            }
        }

        /// <summary>
        /// 다른 프로세스가 동일 CSV를 쓰기/확장 중일 때도 읽을 수 있도록 공유 모드로 연다.
        /// </summary>
        private static IEnumerable<string> ReadLinesShared(string path)
        {
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                FileOptions.SequentialScan);
            using var sr = new StreamReader(fs);
            while (sr.ReadLine() is { } line)
            {
                yield return line;
            }
        }

        private bool TryParseCsvFloat(string raw, out float value)
        {
            return float.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// CSV 유효 행 수를 반환합니다. 첫 열 최댓값이 있으면 행 수와 비교해 더 큰 값을 씁니다(배열 크기·읽기 상한).
        /// </summary>
        private static int ResolveCsvRowCount(string csvPath, int maxHallIndexFromFirstColumn)
        {
            int lineCount = 0;
            foreach (string line in ReadLinesShared(csvPath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineCount++;
                }
            }

            if (maxHallIndexFromFirstColumn < 0)
            {
                return lineCount;
            }

            return Math.Max(maxHallIndexFromFirstColumn, lineCount);
        }

        private void OnNavigationLabelClick(object? sender, EventArgs e)
        {
            if (sender is not Label clickedLabel)
            {
                return;
            }

            // EXIT 라벨은 종료 확인 메시지 처리 후 색 변경을 건너뜁니다.
            if (clickedLabel == NaviEXITLabel)
            {
                var result = MessageBox.Show(
                    this,
                    "프로그램을 종료하시겠습니까?",
                    "종료 확인",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }

                return;
            }

            if (!_LoginManager.BoolLoginCheck && clickedLabel != NaviLoginLabel)
            {
                ShowLoginRequiredFocusLogin();
                return;
            }

            // 로그인 패널 표시 여부: 로그인 라벨일 때만 표시
            LoginPanel.Visible = clickedLabel == NaviLoginLabel;
            if (LoginPanel.Visible)
            {
                LoginPanel.BringToFront();
            }

            // TCP/IP 패널 표시 여부: TCP/IP 라벨일 때만 표시
            TCPIPPanel.Visible = clickedLabel == NavlTCPIPLabel;
            if (TCPIPPanel.Visible)
            {
                TCPIPPanel.Location = new Point(210, 162);
                TCPIPPanel.Size = new Size(1710, 1018);
                TCPIPPanel.BringToFront();
                PLCSettingbtr.BringToFront();
                MoniteringStartbtr.BringToFront();
                MoniteringEndbtr.BringToFront();
                KeyenceSettingbtr.BringToFront();
                KeyenceConnetingCheckbtr.BringToFront();

            }

            // RECIPE 패널 표시 여부: RECIPE 라벨일 때만 표시
            RECIPEPanel.Visible = clickedLabel == NaviRecipeLabel;
            if (RECIPEPanel.Visible)
            {
                RECIPEPanel.Location = new Point(210, 162);
                RECIPEPanel.Size = new Size(1710, 1018);
                RECIPEPanel.BringToFront();
            }

            foreach (var label in _navigationLabels)
            {
                label.BackColor = label == clickedLabel ? Color.Silver : Color.White;
            }
        }

        // 디자이너에서 직접 연결된 이벤트 핸들러(기존 네비게이션 공통 처리로 위임)
        private void NaviRecipeLabel_Click(object sender, EventArgs e)
        {
            OnNavigationLabelClick(sender, e);
            RecipeSelectPanel.Location = new Point(24, 97);
            RecipeSelectPanel.Size = new Size(1661, 807);
            RecipeSelectPanel.Visible = true;
            RecipeSelectLabel.BackColor = Color.White;
            RecipeSelectLabel.ForeColor = Color.Black;
            PluralStaticLabel.BackColor = Color.FromArgb(64, 64, 64);
            SingleStaticLabel.BackColor = Color.FromArgb(64, 64, 64);
            ListLabel.BackColor = Color.FromArgb(64, 64, 64);
            PluralStaticLabel.ForeColor = Color.White;
            SingleStaticLabel.ForeColor = Color.White;
            ListLabel.ForeColor = Color.White;
            SelectedModeDisplaylLabel.Text = "-";
            seletedcmodel = "-";
            TopDetatilLabel.Text = "-";
            CaldataPanel.Visible = false;


        }

        private void OnRecipeTopLabelClick(object? sender, EventArgs e)
        {
            if (sender is Label clicked && _recipeTopLabels.Contains(clicked))
            {
                if (!EnsureLoggedIn())
                {
                    return;
                }

                SetRecipeTopSelected(clicked);

                // 공통: 먼저 하위 패널 숨김
                RecipeSelectPanel.Visible = false;
                ListPanel.Visible = false;
                SingleStaticPanel.Visible = false;

                // RecipeSelectLabel 이 눌렸을 때는 RecipeSelectPanel 만 보이게
                if (clicked == RecipeSelectLabel)
                {
                    RecipeSelectPanel.Location = new Point(24, 97);
                    RecipeSelectPanel.Size = new Size(1661, 807);
                    RecipeSelectPanel.Visible = true;
                    return;
                }

                // ListLabel 클릭 시 ListDisplyPanel 활성화 및 위치/크기 설정
                if (clicked == ListLabel)
                {
                    ListPanel.Location = new Point(24, 97);
                    ListPanel.Size = new Size(1661, 807);
                    ListPanel.Visible = true;
                    return;
                }

                if (clicked == SingleStaticLabel)
                {
                    ShowSingleStaticPanel();
                    return;
                }
            }
        }

        private void SetRecipeTopSelected(Label selected)
        {
            foreach (var l in _recipeTopLabels)
            {
                var isSelected = l == selected;
                l.BackColor = isSelected ? Color.White : Color.FromArgb(64, 64, 64);
                l.ForeColor = isSelected ? Color.Black : Color.White;
            }
        }

        private void OnLoginRoleLabelClick(object? sender, EventArgs e)
        {
            if (sender is not Label clickedRole)
            {
                return;
            }

            foreach (var role in _loginRoleLabels)
            {
                role.BackColor = role == clickedRole ? Color.White : Color.Silver;
            }
        }



        private void LoginOperatorLabel_Click(object sender, EventArgs e)
        {
            _LoginManager.ProgramLoginMode = "operator";
        }

        private void LoginAdminLabel_Click(object sender, EventArgs e)
        {
            _LoginManager.ProgramLoginMode = "admin";
        }

        private void LoginMasterLabel_Click(object sender, EventArgs e)
        {
            _LoginManager.ProgramLoginMode = "master";
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {

            if (_LoginManager.ProgramLoginMode == "-")
            {
                var result = MessageBox.Show(
                     "로그인할 계정을 선택해주세요",
                     "계정 선택 확인",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Question);
            }
            else
            {
                _LoginManager.UserInputID = LoginIDTextBox.Text.ToString();
                _LoginManager.UserInputPW = LoginPWTextBox.Text.ToString();
                _LoginManager.check_id_pw();
                if (_LoginManager.BoolLoginCheck)
                {
                    var result = MessageBox.Show(
                     "로그인이 완료되었습니다.",
                     "로그인 완료",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Question);
                    TopIDLabel.Text = _LoginManager.UserInputID;
                    LoginPanel.Visible = false;
                    RECIPEPanel.Location = new Point(210, 162);
                    RECIPEPanel.Size = new Size(1710, 1018);
                    RECIPEPanel.BringToFront();
                    RECIPEPanel.Visible = true;
                    RecipeSelectPanel.Location = new Point(24, 97);
                    RecipeSelectPanel.Size = new Size(1661, 807);
                    RecipeSelectPanel.Visible = true;
                    RecipeSelectLabel.BackColor = Color.White;
                    RecipeSelectLabel.ForeColor = Color.Black;
                    PluralStaticLabel.BackColor = Color.FromArgb(64, 64, 64);
                    SingleStaticLabel.BackColor = Color.FromArgb(64, 64, 64);
                    ListLabel.BackColor = Color.FromArgb(64, 64, 64);
                    PluralStaticLabel.ForeColor = Color.White;
                    SingleStaticLabel.ForeColor = Color.White;
                    ListLabel.ForeColor = Color.White;
                    SelectedModeDisplaylLabel.Text = "-";
                    seletedcmodel = "-";
                    TopDetatilLabel.Text = "-";
                    CaldataPanel.Visible = false;
                    NaviLoginLabel.BackColor = Color.White;
                    NaviRecipeLabel.BackColor = Color.Silver;
                    LoginIDTextBox.Clear();
                    LoginPWTextBox.Clear();
                }
            }


        }

        private void LoginChangeIDPWButton_Click(object sender, EventArgs e)
        {
            if (_LoginManager.BoolLoginCheck)
            {
                changemode = "-";
                IDPWChangePanel.Visible = true;
            }
            else
            {
                var result = MessageBox.Show(
                              "로그인을 진행해주세요",
                              "로그인 필요",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Question);
            }
        }

        private void CANCELButton_Click(object sender, EventArgs e)
        {
            IDPWChangePanel.Visible = false;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (changemode == "-")
            {
                var result = MessageBox.Show(
                              "변경할 계정을 선택해주세요",
                              "계정 선택 필요",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Question);
            }
            else
            {
                if (changemode == "operator")
                {
                    _LoginManager.JsonLoginData.LoginData[0].userid = ChangeIDTextbox.Text.ToString();
                    _LoginManager.JsonLoginData.LoginData[0].pw = ChangePWTextbox.Text.ToString();

                }
                else if (changemode == "admin")
                {
                    _LoginManager.JsonLoginData.LoginData[1].userid = ChangeIDTextbox.Text.ToString();
                    _LoginManager.JsonLoginData.LoginData[1].pw = ChangePWTextbox.Text.ToString();
                }
                else if (changemode == "master")
                {
                    _LoginManager.JsonLoginData.LoginData[2].userid = ChangeIDTextbox.Text.ToString();
                    _LoginManager.JsonLoginData.LoginData[2].pw = ChangePWTextbox.Text.ToString();
                }
                var result = MessageBox.Show(
                    $"{changemode}의 ID/PW 변경 완료 \n 재로그인 해주시길 바랍니다",
                    "ID/PW변경 완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Question);
                _LoginManager.pwchane();
                IDPWChangePanel.Visible = false;
                LoginPanel.Visible = true;
                LoginPanel.BringToFront();
            }

        }

        private void ChangeOperator_Click(object sender, EventArgs e)
        {
            changemode = "operator";
            ChangeOperator.BackColor = Color.White;
            ChangeAdmin.BackColor = Color.Silver;
            ChangeMaster.BackColor = Color.Silver;
        }

        private void ChangeAdmin_Click(object sender, EventArgs e)
        {
            changemode = "admin";
            ChangeOperator.BackColor = Color.Silver;
            ChangeAdmin.BackColor = Color.White;
            ChangeMaster.BackColor = Color.Silver;
        }

        private void ChangeMaster_Click(object sender, EventArgs e)
        {
            changemode = "master";
            ChangeOperator.BackColor = Color.Silver;
            ChangeAdmin.BackColor = Color.Silver;
            ChangeMaster.BackColor = Color.White;
        }

        private void Model_Click(object sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            Label clickedLabel = sender as Label;
            SelectedModeDisplaylLabel.Text = clickedLabel.Name.ToString();
            seletedcmodel = clickedLabel.Name.ToString();
            TopDetatilLabel.Text = clickedLabel.Name.ToString();
        }

        private void RecipeSelectLabel_Click(object sender, EventArgs e)
        {
            if (!_LoginManager.BoolLoginCheck)
            {
                return;
            }

            RecipeSelectPanel.Location = new Point(24, 97);
            RecipeSelectPanel.Size = new Size(1661, 807);
            PerulStaticPanel.Visible = false;
        }

        private void SearchStratButton_Click(object sender, EventArgs e)
        {
            /// 



            if (!EnsureLoggedIn())
            {
                return;
            }



            var start = StartdateTimePicker.Value.Date;
            var end = EnddateTimePicker.Value.Date;
            startdate = start.ToString("yyyyMMdd");
            enddate = end.ToString("yyyyMMdd");
            if (int.Parse(startdate) > int.Parse(enddate))
            {
                MessageBox.Show(
                  "시작일과 종료일을 다시 선택해주세요",
                  "시작일 종료일 선택 에러",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Warning);
                return;
            }

            if (seletedcmodel == "-" || seletedcmodel.Contains("label"))
            {
                MessageBox.Show(
                 "모델을 선택해주세요",
                 "모델 선택에러",
                 MessageBoxButtons.OK,
                 MessageBoxIcon.Warning);
                return;
            }
            Unchangedseletedcmodel = seletedcmodel;
            //
            RecipeSelectLabel.BackColor = Color.FromArgb(64, 64, 64);
            RecipeSelectLabel.ForeColor = Color.White;
            ListLabel.BackColor = Color.White;
            ListLabel.ForeColor = Color.Black;


            // 날짜 순서가 거꾸로 들어올 수도 있으니 안전하게 정렬(포함 범위 계산용)
            if (start > end)
            {
                var temp = start;
                start = end;
                end = temp;
            }

            startdate = start.ToString("yyyyMMdd");
            enddate = end.ToString("yyyyMMdd");

            // 포함 범위: (end-start) + 1
            dateCount = (end - start).Days + 1;
            dateStrArray = new string[dateCount];
            FtpDateModelPath.Clear();
            for (int i = 0; i < dateCount; i++)
            {

                dateStrArray[i] = start.AddDays(i).ToString("yyyyMMdd");
                string temp = _DirectoryManager.ftpdirectory + dateStrArray[i] + "\\" + seletedcmodel + "\\";
                FtpDateModelPath.Add(temp);

            }

            _listRowEntries.Clear();
            SingleStaticSavePoint = -1; // 리스트가 초기화 되면 savepoint 역시 초기화 함
            for (int i = 0; i < dateCount; i++)
            {
                _listRowEntries.AddRange(ScanBcrTrialRowsUnderDateModelRoot(dateStrArray[i], FtpDateModelPath[i]));
            }

            // 날짜 → 시행횟수 오름차순 (BCR명은 정렬 키에서 제외)
            _listRowEntries.Sort((a, b) =>
            {
                int cmp = string.CompareOrdinal(a.DateStr, b.DateStr);
                if (cmp != 0)
                {
                    return cmp;
                }

                return a.TrialNumber.CompareTo(b.TrialNumber);
            });

            if (_listRowEntries.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "선택한 날짜와 모델에서 검색 기록을 확인할 수 없습니다. \nFTP, 모델, 날짜를 확인해 주세요!",
                    "검색 결과 없음",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                RecipeSelectPanel.BringToFront();
                ListLabel.BackColor = Color.FromArgb(64, 64, 64);
                ListLabel.ForeColor = Color.White;

                RecipeSelectLabel.BackColor = Color.White;
                RecipeSelectLabel.ForeColor = Color.Black;


                return;

            }

            // 2. SearchStratButton 클릭 시 ListDisplyPanel 활성화 및 위치/크기 설정
            ListPanel.Location = new Point(24, 97);
            ListPanel.Size = new Size(1661, 807);
            ListPanel.Visible = true;
            RecipeSelectPanel.Visible = false;

            // 스캔된 시행(행)만큼 목록 행 생성
            BuildListDateRowControls();
        }

        private void BuildListDateRowControls()
        {
            // FlowLayoutPanel은 런타임에 레이아웃이 재배치되므로,
            // "한 날짜 = 한 행"을 Panel로 묶어서 추가합니다.
            ListDisplyPanel.SuspendLayout();
            _listBcrCellToolTip.RemoveAll();
            // 기존 디자이너 기본 라벨을 완전히 제거(Dispose) 후 다시 생성
            foreach (Control c in ListDisplyPanel.Controls)
            {
                c.Dispose();
            }
            ListDisplyPanel.Controls.Clear();
            ListDisplyPanel.FlowDirection = FlowDirection.TopDown;
            ListDisplyPanel.WrapContents = false;

            var headerPanelHeight = 53;
            var cellFont = new Font("맑은 고딕", 15F, FontStyle.Bold);

            // 헤더/데이터 셀 스타일
            var headerBackColor = Color.FromArgb(64, 64, 64);
            var headerForeColor = Color.White;
            var rowBackColor = Color.FromArgb(64, 64, 64);
            var rowForeColor = Color.White;

            string GetListTypeTextFromModel(string model)
            {
                if (string.IsNullOrWhiteSpace(model) || model.Length < 2)
                    return "NONE";

                var prefix2 = model.Substring(0, 2);
                if (prefix2 == "WR") return "래핑기어";
                if (prefix2 == "WD") return "용접기어";
                return "NONE";
            }

            // 선택된 모델(seletedcmodel)의 앞 2자리 기준으로 ListType 기본값 결정
            var listTypeText = GetListTypeTextFromModel(seletedcmodel);

            // 선택된 모델 문자열에 포함된 구동 타입 기준으로 ListDriveTrain 텍스트 결정
            string getDriveTrainTextFromModel(string model)
            {
                if (string.IsNullOrWhiteSpace(model))
                    return "";

                // "ICE가라는 STRING" / "HEV가라는 STRING" 포함 여부
                if (model.Contains("ICE", StringComparison.OrdinalIgnoreCase))
                    return "ICE";
                if (model.Contains("HEV", StringComparison.OrdinalIgnoreCase))
                    return "HEV";
                return "";
            }

            var driveTrainText = getDriveTrainTextFromModel(seletedcmodel);

            // ListAutoSpec 텍스트 구성: AutoString + "/" + SpecString
            // AutoString: RG3, JK, JX, RS4 중 seletedcmodel에 포함된 값
            // SpecString: 25T -> 2.5T, 35T -> 3.5T
            string getAutoStringFromModel(string model)
            {
                if (string.IsNullOrWhiteSpace(model))
                    return "NONE";

                if (model.Contains("RG3", StringComparison.OrdinalIgnoreCase)) return "RG3";
                if (model.Contains("JK", StringComparison.OrdinalIgnoreCase)) return "JK";
                if (model.Contains("JX", StringComparison.OrdinalIgnoreCase)) return "JX";
                if (model.Contains("RS4", StringComparison.OrdinalIgnoreCase)) return "RS4";
                return "NONE";
            }

            string getSpecStringFromModel(string model)
            {
                if (string.IsNullOrWhiteSpace(model))
                    return "NONE";

                if (model.Contains("25T", StringComparison.OrdinalIgnoreCase)) return "2.5T";
                if (model.Contains("35T", StringComparison.OrdinalIgnoreCase)) return "3.5T";
                return "NONE";
            }

            var autoString = getAutoStringFromModel(seletedcmodel);
            var specString = getSpecStringFromModel(seletedcmodel);
            var autoSpecText = $"{autoString}/{specString}";

            const int rowPanelMinWidth = 1293;

            // 열 위치/크기 (디자이너 기준) — TEST 일시 오른쪽에 시행횟수
            var colX = new Dictionary<string, int>
            {
                ["ListCount"] = 3,
                ["ListSelect"] = 83,
                ["ListEvaluateResult"] = 163,
                ["ListBCR"] = 265,
                ["ListType"] = 486,
                ["ListDriveTrain"] = 634,
                ["ListAutoSpec"] = 782,
                ["ListDate"] = 930,
                ["ListTrialCount"] = 1198
            };

            var colSize = new Dictionary<string, Size>
            {
                ["ListCount"] = new Size(74, 53),
                ["ListSelect"] = new Size(74, 53),
                ["ListEvaluateResult"] = new Size(96, 53),
                ["ListBCR"] = new Size(215, 53),
                ["ListType"] = new Size(142, 53),
                ["ListDriveTrain"] = new Size(142, 53),
                ["ListAutoSpec"] = new Size(142, 53),
                ["ListDate"] = new Size(266, 53),
                ["ListTrialCount"] = new Size(95, 53)
            };

            var usePanelWidth = Math.Max(ListDisplyPanel.ClientSize.Width, rowPanelMinWidth);

            Label CreateCellLabel(string name, string text, Point location, Size size, Color backColor, Color foreColor, bool autoEllipsis = false, string? toolTipText = null)
            {
                var lbl = new Label
                {
                    Name = name,
                    Text = text,
                    Location = location,
                    Size = size,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    Font = cellFont,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoEllipsis = autoEllipsis
                };
                if (!string.IsNullOrEmpty(toolTipText))
                {
                    _listBcrCellToolTip.SetToolTip(lbl, toolTipText);
                }

                return lbl;
            }

            // 헤더 행
            var headerPanel = new Panel
            {
                Height = headerPanelHeight,
                Width = usePanelWidth,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 3, 0, 0)
            };

            headerPanel.Controls.Add(CreateCellLabel("ListCount", "순서", new Point(colX["ListCount"], 0), colSize["ListCount"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListSelect", "선택", new Point(colX["ListSelect"], 0), colSize["ListSelect"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListEvaluateResult", "평가결과", new Point(colX["ListEvaluateResult"], 0), colSize["ListEvaluateResult"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListBCR", "BCR", new Point(colX["ListBCR"], 0), colSize["ListBCR"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListType", "TYPE", new Point(colX["ListType"], 0), colSize["ListType"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListDriveTrain", "구동", new Point(colX["ListDriveTrain"], 0), colSize["ListDriveTrain"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListAutoSpec", "자동/사양", new Point(colX["ListAutoSpec"], 0), colSize["ListAutoSpec"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListDate", "TEST 일시", new Point(colX["ListDate"], 0), colSize["ListDate"], headerBackColor, headerForeColor));
            headerPanel.Controls.Add(CreateCellLabel("ListTrialCount", "시행횟수", new Point(colX["ListTrialCount"], 0), colSize["ListTrialCount"], headerBackColor, headerForeColor));

            ListDisplyPanel.Controls.Add(headerPanel);

            // BCR/시행 폴더별 데이터 행
            for (int i = 0; i < _listRowEntries.Count; i++)
            {
                var entry = _listRowEntries[i];
                var rowPanel = new Panel
                {
                    Height = headerPanelHeight,
                    Width = usePanelWidth,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 3, 0, 0)
                };

                var rowIndex = i + 1;
                rowPanel.Controls.Add(CreateCellLabel($"ListCount{rowIndex}", rowIndex.ToString(), new Point(colX["ListCount"], 0), colSize["ListCount"], rowBackColor, rowForeColor));
                var listSelectLabel = CreateCellLabel(
                    $"ListSelect{rowIndex}",
                    "-",
                    new Point(colX["ListSelect"], 0),
                    colSize["ListSelect"],
                    rowBackColor,
                    rowForeColor
                );

                // 선택 체크: 눌리면 "-" -> "V"
                listSelectLabel.Click += (s, e) =>
                {
                    listSelectLabel.Text = listSelectLabel.Text == "-" ? "V" : "-";
                    //선택된 row에 따라 코멘트 내용 수정
                    var vCount = CountListSelectWithV(ListDisplyPanel);
                    if (vCount == 1)
                    {
                        //v표시가 된 list의 열을 찾아야하네
                        var selectedRowEntries = CollectSelectedListRowEntriesOrderedByRow();

                        foreach (var rowEntry in selectedRowEntries)
                        {
                            ComentTextBox.ReadOnly = false;
                            string commenttxtPath = Path.Combine(rowEntry.TrialFolderPath, ComenttxtFileName);
                            if (File.Exists(commenttxtPath))
                            {
                                try
                                {
                                    string content = File.ReadAllText(commenttxtPath);
                                    ComentTextBox.Text = content;
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError($"CommenttxtFile : {commenttxtPath}", $"파일을 읽어오는데 에러가 발생했습니다 ex : {ex}");
                                }
                            }
                            else
                            {
                                Logger.LogError($"CommenttxtFile : {commenttxtPath}", $"해당 파일 없음");
                                ComentTextBox.Text = "-";
                            }
                        }

                    }
                    else if (vCount == 0)
                    {
                        ComentTextBox.ReadOnly = true;
                        ComentTextBox.Text = "1개이상의 행을 선택해주세요\n 읽기만 가능합니다";
                    }

                    else
                    {
                        ComentTextBox.ReadOnly = true;
                        ComentTextBox.Text = "2개이상의 행을 선택하셨습니다. 다시 선택해주세요 \n 읽기만 가능합니다";
                    }

                };
                rowPanel.Controls.Add(listSelectLabel);
                rowPanel.Controls.Add(CreateCellLabel($"ListEvaluateResult{rowIndex}", "", new Point(colX["ListEvaluateResult"], 0), colSize["ListEvaluateResult"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListBCR{rowIndex}", entry.BcrFolderName, new Point(colX["ListBCR"], 0), colSize["ListBCR"], rowBackColor, rowForeColor, autoEllipsis: true, toolTipText: entry.BcrFolderName ?? ""));
                rowPanel.Controls.Add(CreateCellLabel($"ListType{rowIndex}", listTypeText, new Point(colX["ListType"], 0), colSize["ListType"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListDriveTrain{rowIndex}", driveTrainText, new Point(colX["ListDriveTrain"], 0), colSize["ListDriveTrain"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListAutoSpec{rowIndex}", autoSpecText, new Point(colX["ListAutoSpec"], 0), colSize["ListAutoSpec"], rowBackColor, rowForeColor));
                var listDateText = FormatListDateWithFolderLastWrite(entry.DateStr, entry.TrialFolderPath);
                rowPanel.Controls.Add(CreateCellLabel($"ListDate{rowIndex}", listDateText, new Point(colX["ListDate"], 0), colSize["ListDate"], rowBackColor, rowForeColor, autoEllipsis: true, toolTipText: listDateText));
                rowPanel.Controls.Add(CreateCellLabel($"ListTrialCount{rowIndex}", entry.TrialNumber.ToString(CultureInfo.InvariantCulture), new Point(colX["ListTrialCount"], 0), colSize["ListTrialCount"], rowBackColor, rowForeColor));

                ListDisplyPanel.Controls.Add(rowPanel);
            }

            ListDisplyPanel.ResumeLayout();
        }

        private void FTPSettingButton_Click(object sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                // 초기 설명 문구 설정
                fbd.Description = "데이터를 저장할 폴더를 선택하세요.";

                // 새 폴더 만들기 버튼 표시 여부
                fbd.ShowNewFolderButton = true;

                // 사용자가 '확인'을 눌렀을 때만 실행
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    // 선택된 경로를 변수에 저장
                    string selectedPath = fbd.SelectedPath + @"\";
                    _DirectoryManager.SetFtpDirectory(selectedPath);
                }
            }
        }

        private void SicngleImgCheckPanelExitButton_Click(object sender, EventArgs e)
        {
            SicngleImgCheckPanel.Visible = false;
            FrontDisplayPanel.Visible = false;
            RearDisplayPanel.Visible = false;
        }

        private void SingleStaticButton_Click(object? sender, EventArgs e)
        {


            int FrontHallMaxCount = -1; //AC쪽에 홀의 전체 개수
            int RearHallMaxCount = -1; //DCV쪽에 홀의 전체 개수

            if (!EnsureLoggedIn())
            {
                return;
            }



            var vCount = CountListSelectWithV(ListDisplyPanel);
            if (vCount == 0 || vCount >= 2)
            {
                MessageBox.Show(
                    this,
                    "1개만 선택해 주세요.",
                    "선택 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var vLabel = FindListSelectLabelWithV(ListDisplyPanel);
            if (vLabel == null)
            {
                return;
            }


            string temp = ExtractListSelectRowNumber(vLabel.Name);
            selectedListSelectRowNumber = Convert.ToInt32(temp);
            if (selectedListSelectRowNumber < 1 || selectedListSelectRowNumber > _listRowEntries.Count)
            {
                MessageBox.Show(
                    this,
                    "선택한 행에 해당하는 폴더 정보가 없습니다. 검색을 다시 실행해 주세요.",
                    "목록 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var rowEntry = _listRowEntries[selectedListSelectRowNumber - 1];
            SingleStaticSavePoint = selectedListSelectRowNumber - 1; /// 여기서만 바뀐다!
           
            FrontPath = Path.Combine(rowEntry.TrialFolderPath, "Acceleration");
            RearPath = Path.Combine(rowEntry.TrialFolderPath, "Deceleration");
            string frontcsvpath = Path.Combine(FrontPath, "ResultOutput.csv");
            string reartcsvpath = Path.Combine(RearPath, "ResultOutput.csv");
            //최종 파일이 없는 경우라면 최종파일을 만들어 줘야함
            if (!(File.Exists(frontcsvpath)))
            {
                if (Directory.Exists(FrontPath)) //디렉토리가 있는 경우라면 
                {
                    makeResultOutput(FrontPath);
                }
                else
                {
                    ShowMissingFileWarning("해당 디렉토리 없음", new List<string> { FrontPath });
                }
            }

            if (!(File.Exists(reartcsvpath)))
            {
                if (Directory.Exists(RearPath)) //디렉토리가 있는 경우라면 
                {
                    makeResultOutput(RearPath);
                }
                else
                {
                    ShowMissingFileWarning("해당 디렉토리 없음", new List<string> { RearPath });
                }
            }

            var missingCsv = new List<string>();
            if (!File.Exists(frontcsvpath)) missingCsv.Add(frontcsvpath);
            if (!File.Exists(reartcsvpath)) missingCsv.Add(reartcsvpath);
            if (missingCsv.Count > 0)
            {
                Logger.LogWarning("FileIO", "CSV 파일 없음 (단일 통계)", _LoginManager?.UserInputID ?? "", string.Join(" | ", missingCsv));
                ShowMissingFileWarning("CSV 파일 없음", missingCsv);
                return;
            }


            //SingleStaticLabel.ForeColor = Color.Black;
            //FrontHallMaxCount RearHallMaxCount => 첫 열(홀 번호) 최댓값; 없으면 유효 행 수로 대체
            foreach (string line in ReadLinesShared(frontcsvpath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values = line.Split(',');
                if (values.Length > 0 && int.TryParse(values[0].Trim(), out int hallIndex))
                {
                    FrontHallMaxCount = Math.Max(FrontHallMaxCount, hallIndex);
                }
            }
            foreach (string line in ReadLinesShared(reartcsvpath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values = line.Split(',');
                if (values.Length > 0 && int.TryParse(values[0].Trim(), out int hallIndex))
                {
                    RearHallMaxCount = Math.Max(RearHallMaxCount, hallIndex);
                }
            }

            FrontHallMaxCount = ResolveCsvRowCount(frontcsvpath, FrontHallMaxCount);
            RearHallMaxCount = ResolveCsvRowCount(reartcsvpath, RearHallMaxCount);
            if (FrontHallMaxCount <= 0 || RearHallMaxCount <= 0)
            {
                Logger.LogWarning("FileIO", "CSV 데이터 없음 (단일 통계)", _LoginManager?.UserInputID ?? "",
                    $"FrontRows={FrontHallMaxCount}, RearRows={RearHallMaxCount} | {frontcsvpath} | {reartcsvpath}");
                MessageBox.Show(
                    this,
                    "ResultOutput.csv 파일은 있으나 유효한 검사 데이터가 없습니다.\n"
                    + "· 폴더에 원본 CSV가 있는지 확인하세요.\n"
                    + "· PLC 모니터링으로 데이터가 적재되었는지 확인하세요.",
                    "CSV 데이터 없음",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                SingleStaticPanel.Visible = false;
                return;
            }

            // csv 파일 데이터 지표
            //카운트, 최대점X, 최대점Y, 마모점 길이, 마모점 폭,마모점 크기, 패턴 x, 패턴 y              
            float[] FSinglePeakX = new float[FrontHallMaxCount];
            float[] FSinglePeakY = new float[FrontHallMaxCount];
            float[] FSingleAreaX = new float[FrontHallMaxCount];
            float[] FSingleAreaY = new float[FrontHallMaxCount];
            float[] FSingleWidth = new float[FrontHallMaxCount];
            float[] FSingleHeight = new float[FrontHallMaxCount];
            float[] FSingleArea = new float[FrontHallMaxCount];
            float[] FSDistance = new float[FrontHallMaxCount];

            //모든 라인을 우선 다읽어와서 저장 후 하나씩 처리
            string[] ACLines = null;
            string[] DCLines = null;

            try
            {
                ACLines = File.ReadAllLines(frontcsvpath);
                DCLines = File.ReadAllLines(reartcsvpath);
            }
            catch (Exception ex)
            {
                Logger.LogError("FileIO", $"파일을 읽어오는데 에러가 발생했습니다 ex : {ex}");
            }

            try
            {
                int count = 0;
                // 한 줄씩 읽어오기
                foreach (string line in ACLines)
                {
                    if (count >= FrontHallMaxCount)
                    {
                        break;
                    }

                    // 쉼표로 분리하여 배열에 담기
                    string[] values = line.Split(',');
                    if (values.Length < 8)
                    {
                        Logger.LogWarning("FileIO", "Acceleration CSV 포맷 이상 (단일 통계)", _LoginManager?.UserInputID ?? "", $"{frontcsvpath} | line={line}");
                        break;
                    }

                    if (!TryParseCsvFloat(values[1], out FSinglePeakX[count])
                        || !TryParseCsvFloat(values[2], out FSinglePeakY[count])
                        || !TryParseCsvFloat(values[5], out FSingleWidth[count])
                        || !TryParseCsvFloat(values[6], out FSingleHeight[count])
                        || !TryParseCsvFloat(values[7], out FSingleArea[count])
                        || !TryParseCsvFloat(values[3], out FSingleAreaX[count])
                        || !TryParseCsvFloat(values[4], out FSingleAreaY[count])
                        || !TryParseCsvFloat(values[8], out FSDistance[count])
                       )
                    {
                        Logger.LogWarning("FileIO", "Acceleration CSV 숫자 파싱 실패 (단일 통계)", _LoginManager?.UserInputID ?? "", $"{frontcsvpath} | line={line}");
                        break;
                    }
                    count++;
                }

            }
            catch (Exception ex)
            {
                Logger.LogError("FileIO", "Acceleration CSV 읽기 실패 (단일 통계)", _LoginManager?.UserInputID ?? "", $"{frontcsvpath} | {ex}");
                MessageBox.Show(this, "Acceleration CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.", "CSV 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            float[] RSinglePeakX = new float[RearHallMaxCount];
            float[] RSinglePeakY = new float[RearHallMaxCount];
            float[] RSingleAreaX = new float[RearHallMaxCount];
            float[] RSingleAreaY = new float[RearHallMaxCount];
            float[] RSingleWidth = new float[RearHallMaxCount];
            float[] RSingleHeight = new float[RearHallMaxCount];
            float[] RSingleArea = new float[RearHallMaxCount];
            float[] RSDistance = new float[RearHallMaxCount];


            //단일 Rear의 데이터 읽어와서 전체 저장
            try
            {
                int count = 0;
                // 한 줄씩 읽어오기
                foreach (string line in DCLines)
                {
                    // 쉼표로 분리하여 배열에 담기
                    string[] values = line.Split(',');
                    if (values.Length < 8)
                    {
                        Logger.LogWarning("FileIO", "Deceleration CSV 포맷 이상 (단일 통계)", _LoginManager?.UserInputID ?? "", $"{reartcsvpath} | line={line}");
                        break;
                    }
                    if (!TryParseCsvFloat(values[1], out RSinglePeakX[count])
                        || !TryParseCsvFloat(values[2], out RSinglePeakY[count])
                        || !TryParseCsvFloat(values[5], out RSingleWidth[count])
                        || !TryParseCsvFloat(values[6], out RSingleHeight[count])
                        || !TryParseCsvFloat(values[7], out RSingleArea[count])
                        || !TryParseCsvFloat(values[3], out RSingleAreaX[count])
                        || !TryParseCsvFloat(values[4], out RSingleAreaY[count])
                        || !TryParseCsvFloat(values[8], out RSDistance[count])
                       )
                    {
                        Logger.LogWarning("FileIO", "Deceleration CSV 숫자 파싱 실패 (단일 통계)", _LoginManager?.UserInputID ?? "", $"{reartcsvpath} | line={line}");
                        break;
                    }
                    count++;
                    if (count >= RearHallMaxCount)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("FileIO", "Rear CSV 읽기 실패 (단일 통계)", _LoginManager?.UserInputID ?? "", $"{reartcsvpath} | {ex}");
                MessageBox.Show(this, "Rear CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.", "CSV 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // front하고 rear의 csv파일을 모두 읽어왔으니 저장하고
            // 이제 여기에 필요한 데이터들을 가시화할수 있게 그래프 그리고 내용들을 추정해야함
            //데이터 가시화 해야됨!
            int CountCsvLines(string path, int maxLines)
            {
                if (!File.Exists(path))
                {
                    return 0;
                }

                int n = 0;
                foreach (var line in ReadLinesShared(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    n++;
                    if (n >= maxLines)
                    {
                        break;
                    }
                }

                return n;
            }

            int frontPointCount = CountCsvLines(frontcsvpath, FrontHallMaxCount);
            int rearPointCount = CountCsvLines(reartcsvpath, RearHallMaxCount);
            //RSinglePeakX
            //RSinglePeakY
            //RSingleWidth
            //RSingleHeight
            //RSingleArea
            //RSPatternX
            //RSPatternY
            //+-범위를 정해서 수정



            double AC_PeakX_Base = -1;
            double AC_PeakX_A_Range = -1;
            double AC_PeakX_B_Range = -1;
            double AC_PeakX_C_Range = -1;
            double AC_PeakX_D_Range = -1;
            double AC_PeakX_E_Range = -1;

            double AC_PeakY_Base = -1;
            double AC_PeakY_A_Range = -1;
            double AC_PeakY_B_Range = -1;
            double AC_PeakY_C_Range = -1;
            double AC_PeakY_D_Range = -1;
            double AC_PeakY_E_Range = -1;

            double AC_AreaX_Base = -1;
            double AC_AreaX_A_Range = -1;
            double AC_AreaX_B_Range = -1;
            double AC_AreaX_C_Range = -1;
            double AC_AreaX_D_Range = -1;
            double AC_AreaX_E_Range = -1;

            double AC_AreaY_Base = -1;
            double AC_AreaY_A_Range = -1;
            double AC_AreaY_B_Range = -1;
            double AC_AreaY_C_Range = -1;
            double AC_AreaY_D_Range = -1;
            double AC_AreaY_E_Range = -1;

            double AC_Length_Base = -1;
            double AC_Length_A_Range = -1;
            double AC_Length_B_Range = -1;
            double AC_Length_C_Range = -1;
            double AC_Length_D_Range = -1;
            double AC_Length_E_Range = -1;

            double AC_Height_Base = -1;
            double AC_Height_A_Range = -1;
            double AC_Height_B_Range = -1;
            double AC_Height_C_Range = -1;
            double AC_Height_D_Range = -1;
            double AC_Height_E_Range = -1;

            double AC_Area_Base = -1;
            double AC_Area_A_Range = -1;
            double AC_Area_B_Range = -1;
            double AC_Area_C_Range = -1;
            double AC_Area_D_Range = -1;
            double AC_Area_E_Range = -1;

            double AC_Distance_Base = -1;
            double AC_Distance_A_Range = -1;
            double AC_Distance_B_Range = -1;
            double AC_Distance_C_Range = -1;
            double AC_Distance_D_Range = -1;
            double AC_Distance_E_Range = -1;

            double DC_PeakX_Base = -1;
            double DC_PeakX_A_Range = -1;
            double DC_PeakX_B_Range = -1;
            double DC_PeakX_C_Range = -1;
            double DC_PeakX_D_Range = -1;
            double DC_PeakX_E_Range = -1;

            double DC_PeakY_Base = -1;
            double DC_PeakY_A_Range = -1;
            double DC_PeakY_B_Range = -1;
            double DC_PeakY_C_Range = -1;
            double DC_PeakY_D_Range = -1;
            double DC_PeakY_E_Range = -1;

            double DC_AreaX_Base = -1;
            double DC_AreaX_A_Range = -1;
            double DC_AreaX_B_Range = -1;
            double DC_AreaX_C_Range = -1;
            double DC_AreaX_D_Range = -1;
            double DC_AreaX_E_Range = -1;

            double DC_AreaY_Base = -1;
            double DC_AreaY_A_Range = -1;
            double DC_AreaY_B_Range = -1;
            double DC_AreaY_C_Range = -1;
            double DC_AreaY_D_Range = -1;
            double DC_AreaY_E_Range = -1;

            double DC_Length_Base = -1;
            double DC_Length_A_Range = -1;
            double DC_Length_B_Range = -1;
            double DC_Length_C_Range = -1;
            double DC_Length_D_Range = -1;
            double DC_Length_E_Range = -1;

            double DC_Height_Base = -1;
            double DC_Height_A_Range = -1;
            double DC_Height_B_Range = -1;
            double DC_Height_C_Range = -1;
            double DC_Height_D_Range = -1;
            double DC_Height_E_Range = -1;

            double DC_Area_Base = -1;
            double DC_Area_A_Range = -1;
            double DC_Area_B_Range = -1;
            double DC_Area_C_Range = -1;
            double DC_Area_D_Range = -1;
            double DC_Area_E_Range = -1;

            double DC_Distance_Base = -1;
            double DC_Distance_A_Range = -1;
            double DC_Distance_B_Range = -1;
            double DC_Distance_C_Range = -1;
            double DC_Distance_D_Range = -1;
            double DC_Distance_E_Range = -1;
            //1차적으로 파일이 있는지 확인 
            //2차적으로 파일안에 값들이 있는지 확인
            //
            string filePath = "./RangeSetting.json";
            try
            {
                // 1. 파일이 실제로 존재하는지 확인
                if (!File.Exists(filePath))
                {
                    Logger.LogError("RangeSetting.json", $"기준치 파일이 없습니다. 그래프의 등급 기준 표시 값을 셋팅해주세요");
                }
                else
                {
                    //파일이 있는 경우
                    // 2. 파일 전체 내용 읽기
                    string jsonString = File.ReadAllText(filePath);

                    // 3. JSON 문자열을 C# 객체로 변환 (역직렬화)
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 대소문자 구분 없이 매핑
                    };

                    Dictionary<string, ModelRangeJson> rootData = JsonSerializer.Deserialize<Dictionary<string, ModelRangeJson>>(jsonString, options);

                    // 4. 읽어온 데이터 확인 (예시로 첫 번째 모델의 AC 데이터 중 PeakX_Base 값 출력)
                    if (rootData != null && rootData.ContainsKey(Unchangedseletedcmodel))
                    {
                        var record = rootData[Unchangedseletedcmodel];
                        //textbox에 해당 값들을 저장
                        AC_PeakX_Base = record.AC.PeakX_Base;
                        AC_PeakX_A_Range = record.AC.PeakX_A_Range;
                        AC_PeakX_B_Range = record.AC.PeakX_B_Range;
                        AC_PeakX_C_Range = record.AC.PeakX_C_Range;
                        AC_PeakX_D_Range = record.AC.PeakX_D_Range;
                        AC_PeakX_E_Range = record.AC.PeakX_E_Range;

                        AC_PeakY_Base = record.AC.PeakY_Base;
                        AC_PeakY_A_Range = record.AC.PeakY_A_Range;
                        AC_PeakY_B_Range = record.AC.PeakY_B_Range;
                        AC_PeakY_C_Range = record.AC.PeakY_C_Range;
                        AC_PeakY_D_Range = record.AC.PeakY_D_Range;
                        AC_PeakY_E_Range = record.AC.PeakY_E_Range;

                        AC_AreaX_Base = record.AC.AreaX_Base;
                        AC_AreaX_A_Range = record.AC.AreaX_A_Range;
                        AC_AreaX_B_Range = record.AC.AreaX_B_Range;
                        AC_AreaX_C_Range = record.AC.AreaX_C_Range;
                        AC_AreaX_D_Range = record.AC.AreaX_D_Range;
                        AC_AreaX_E_Range = record.AC.AreaX_E_Range;

                        AC_AreaY_Base = record.AC.AreaY_Base;
                        AC_AreaY_A_Range = record.AC.AreaY_A_Range;
                        AC_AreaY_B_Range = record.AC.AreaY_B_Range;
                        AC_AreaY_C_Range = record.AC.AreaY_C_Range;
                        AC_AreaY_D_Range = record.AC.AreaY_D_Range;
                        AC_AreaY_E_Range = record.AC.AreaY_E_Range;

                        AC_Length_Base = record.AC.Length_Base;
                        AC_Length_A_Range = record.AC.Length_A_Range;
                        AC_Length_B_Range = record.AC.Length_B_Range;
                        AC_Length_C_Range = record.AC.Length_C_Range;
                        AC_Length_D_Range = record.AC.Length_D_Range;
                        AC_Length_E_Range = record.AC.Length_E_Range;

                        AC_Height_Base = record.AC.Height_Base;
                        AC_Height_A_Range = record.AC.Height_A_Range;
                        AC_Height_B_Range = record.AC.Height_B_Range;
                        AC_Height_C_Range = record.AC.Height_C_Range;
                        AC_Height_D_Range = record.AC.Height_D_Range;
                        AC_Height_E_Range = record.AC.Height_E_Range;

                        AC_Distance_Base = record.AC.Distance_Base;
                        AC_Distance_A_Range = record.AC.Distance_A_Range;
                        AC_Distance_B_Range = record.AC.Distance_B_Range;
                        AC_Distance_C_Range = record.AC.Distance_C_Range;
                        AC_Distance_D_Range = record.AC.Distance_D_Range;
                        AC_Distance_E_Range = record.AC.Distance_E_Range;


                        AC_Area_Base = record.AC.Area_Base;
                        AC_Area_A_Range = record.AC.Area_A_Range;
                        AC_Area_B_Range = record.AC.Area_B_Range;
                        AC_Area_C_Range = record.AC.Area_C_Range;
                        AC_Area_D_Range = record.AC.Area_D_Range;
                        AC_Area_E_Range = record.AC.Area_E_Range;


                        DC_PeakX_Base = record.DC.PeakX_Base;
                        DC_PeakX_A_Range = record.DC.PeakX_A_Range;
                        DC_PeakX_B_Range = record.DC.PeakX_B_Range;
                        DC_PeakX_C_Range = record.DC.PeakX_C_Range;
                        DC_PeakX_D_Range = record.DC.PeakX_D_Range;
                        DC_PeakX_E_Range = record.DC.PeakX_E_Range;

                        DC_PeakY_Base = record.DC.PeakY_Base;
                        DC_PeakY_A_Range = record.DC.PeakY_A_Range;
                        DC_PeakY_B_Range = record.DC.PeakY_B_Range;
                        DC_PeakY_C_Range = record.DC.PeakY_C_Range;
                        DC_PeakY_D_Range = record.DC.PeakY_D_Range;
                        DC_PeakY_E_Range = record.DC.PeakY_E_Range;

                        DC_AreaX_Base = record.DC.AreaX_Base;
                        DC_AreaX_A_Range = record.DC.AreaX_A_Range;
                        DC_AreaX_B_Range = record.DC.AreaX_B_Range;
                        DC_AreaX_C_Range = record.DC.AreaX_C_Range;
                        DC_AreaX_D_Range = record.DC.AreaX_D_Range;
                        DC_AreaX_E_Range = record.DC.AreaX_E_Range;

                        DC_AreaY_Base = record.DC.AreaY_Base;
                        DC_AreaY_A_Range = record.DC.AreaY_A_Range;
                        DC_AreaY_B_Range = record.DC.AreaY_B_Range;
                        DC_AreaY_C_Range = record.DC.AreaY_C_Range;
                        DC_AreaY_D_Range = record.DC.AreaY_D_Range;
                        DC_AreaY_E_Range = record.DC.AreaY_E_Range;

                        DC_Length_Base = record.DC.Length_Base;
                        DC_Length_A_Range = record.DC.Length_A_Range;
                        DC_Length_B_Range = record.DC.Length_B_Range;
                        DC_Length_C_Range = record.DC.Length_C_Range;
                        DC_Length_D_Range = record.DC.Length_D_Range;
                        DC_Length_E_Range = record.DC.Length_E_Range;

                        DC_Height_Base = record.DC.Height_Base;
                        DC_Height_A_Range = record.DC.Height_A_Range;
                        DC_Height_B_Range = record.DC.Height_B_Range;
                        DC_Height_C_Range = record.DC.Height_C_Range;
                        DC_Height_D_Range = record.DC.Height_D_Range;
                        DC_Height_E_Range = record.DC.Height_E_Range;

                        DC_Distance_Base = record.DC.Distance_Base;
                        DC_Distance_A_Range = record.DC.Distance_A_Range;
                        DC_Distance_B_Range = record.DC.Distance_B_Range;
                        DC_Distance_C_Range = record.DC.Distance_C_Range;
                        DC_Distance_D_Range = record.DC.Distance_D_Range;
                        DC_Distance_E_Range = record.DC.Distance_E_Range;


                        DC_Area_Base = record.DC.Area_Base;
                        DC_Area_A_Range = record.DC.Area_A_Range;
                        DC_Area_B_Range = record.DC.Area_B_Range;
                        DC_Area_C_Range = record.DC.Area_C_Range;
                        DC_Area_D_Range = record.DC.Area_D_Range;
                        DC_Area_E_Range = record.DC.Area_E_Range;




                    }
                }
            }
            catch (JsonException ex)
            {

                Logger.LogError("RangeSetting.json", $"[그래프 그리기]JSON 파일의 형식이 잘못되었거나 손상되었습니다.\n\n오류 내용: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogError("RangeSetting.json", $"[그래프 그리기]파일 읽기 권한이 없습니다.\n\n오류 내용: {ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.LogError("RangeSetting.json", $"[그래프 그리기]입출력 작업 중 문제가 발생했습니다.\n\n오류 내용: {ex.Message}");
            }
            catch (Exception ex)
            {

                Logger.LogError("RangeSetting.json", $"[그래프 그리기]알 수 없는 예외가 발생했습니다.\n\n오류 내용: {ex.Message}");
            }




            if (AC_PeakX_Base == -1 ||
                AC_PeakY_Base == -1 ||
                AC_AreaX_Base == -1 ||
                AC_AreaY_Base == -1 ||
                AC_Length_Base == -1 ||
                AC_Height_Base == -1 ||
                AC_Area_Base == -1 ||
                AC_Distance_Base == -1 ||
                DC_PeakX_Base == -1 ||
                DC_PeakY_Base == -1 ||
                DC_AreaX_Base == -1 ||
                DC_AreaY_Base == -1 ||
                DC_Length_Base == -1 ||
                DC_Height_Base == -1 ||
                DC_Area_Base == -1 ||
                DC_Distance_Base == -1
                )
            {

                PlotIndexScatter(AccelerationPeakX, FSinglePeakX, frontPointCount, "AC_PeakX");
                PlotIndexScatter(AccelerationPeakY, FSinglePeakY, frontPointCount, "AC_PeakY");
                PlotIndexScatter(DecelerationPeakX, RSinglePeakX, rearPointCount, "DC_PeakX");
                PlotIndexScatter(DecelerationPeakY, RSinglePeakY, rearPointCount, "DC_PeakY");
                PlotIndexScatter(AccelerationWidth, FSingleWidth, frontPointCount, "AC_Length");
                PlotIndexScatter(AccelerationHeight, FSingleHeight, frontPointCount, "AC_Height");
                PlotIndexScatter(AccelerationArea, FSingleArea, frontPointCount, "AC_Area");
                PlotIndexScatter(DecelerationWidth, RSingleWidth, rearPointCount, "DC_Length");
                PlotIndexScatter(DecelerationHeight, RSingleHeight, rearPointCount, "DC_Height");
                PlotIndexScatter(DecelerationarArea, RSingleArea, rearPointCount, "DC_Area");

                PlotIndexScatter(AccelerationAreaX, FSingleAreaX, frontPointCount, "AC_AreaX");
                PlotIndexScatter(AccelerationAreaY, FSingleAreaY, frontPointCount, "AC_AreaY");
                PlotIndexScatter(AccelerationDistance, FSDistance, frontPointCount, "AC_Distance");

                PlotIndexScatter(DecelerationAreaX, RSingleAreaX, rearPointCount, "DC_AreaX");
                PlotIndexScatter(DecelerationAreaY, RSingleAreaY, rearPointCount, "DC_AreaY");
                PlotIndexScatter(DecelerationDistance, RSDistance, rearPointCount, "DC_Distance");
            }
            else // base와 range셋팅이 다되어 있으면 일단 그린다!
            {//
                PlotIndexScatter(AccelerationPeakX, FSinglePeakX, frontPointCount, "AC_PeakX", AC_PeakX_Base, AC_PeakX_A_Range, AC_PeakX_B_Range, AC_PeakX_C_Range, AC_PeakX_D_Range, AC_PeakX_E_Range);
                PlotIndexScatter(AccelerationPeakY, FSinglePeakY, frontPointCount, "AC_PeakY", AC_PeakY_Base, AC_PeakY_A_Range, AC_PeakY_B_Range, AC_PeakY_C_Range, AC_PeakY_D_Range, AC_PeakY_E_Range);

                PlotIndexScatter(DecelerationPeakX, RSinglePeakX, rearPointCount, "DC_PeakX",DC_PeakX_Base, DC_PeakX_A_Range, DC_PeakX_B_Range, DC_PeakX_C_Range, DC_PeakX_D_Range, DC_PeakX_E_Range);
                PlotIndexScatter(DecelerationPeakY, RSinglePeakY, rearPointCount, "DC_PeakY",DC_PeakY_Base, DC_PeakY_A_Range, DC_PeakY_B_Range, DC_PeakY_C_Range, DC_PeakY_D_Range, DC_PeakY_E_Range);

                PlotIndexScatter(AccelerationWidth, FSingleWidth, frontPointCount, "AC_Length",   AC_Length_Base, AC_Length_A_Range, AC_Length_B_Range, AC_Length_C_Range, AC_Length_D_Range, AC_Length_E_Range);
                PlotIndexScatter(AccelerationHeight, FSingleHeight, frontPointCount, "AC_Height", AC_Height_Base, AC_Height_A_Range, AC_Height_B_Range, AC_Height_C_Range, AC_Height_D_Range, AC_Height_E_Range);
                PlotIndexScatter(AccelerationArea, FSingleArea, frontPointCount, "AC_Area",       AC_Area_Base, AC_Area_A_Range, AC_Area_B_Range, AC_Area_C_Range, AC_Area_D_Range, AC_Area_E_Range);

                PlotIndexScatter(DecelerationWidth, RSingleWidth, rearPointCount, "DC_Length"  ,  DC_Length_Base, DC_Length_A_Range, DC_Length_B_Range, DC_Length_C_Range, DC_Length_D_Range, DC_Length_E_Range);
                PlotIndexScatter(DecelerationHeight, RSingleHeight, rearPointCount, "DC_Height",  DC_Height_Base, DC_Height_A_Range, DC_Height_B_Range, DC_Height_C_Range, DC_Height_D_Range, DC_Height_E_Range);
                PlotIndexScatter(DecelerationarArea, RSingleArea, rearPointCount, "DC_Area"    , DC_Area_Base, DC_Area_A_Range, DC_Area_B_Range, DC_Area_C_Range, DC_Area_D_Range, DC_Area_E_Range);

                PlotIndexScatter(AccelerationAreaX, FSingleAreaX, frontPointCount, "AC_AreaX"      , AC_AreaX_Base, AC_AreaX_A_Range, AC_AreaX_B_Range, AC_AreaX_C_Range, AC_AreaX_D_Range, AC_AreaX_E_Range);
                PlotIndexScatter(AccelerationAreaY, FSingleAreaY, frontPointCount, "AC_AreaY"        ,AC_AreaY_Base, AC_AreaY_A_Range, AC_AreaY_B_Range, AC_AreaY_C_Range, AC_AreaY_D_Range, AC_AreaY_E_Range);
                PlotIndexScatter(AccelerationDistance, FSDistance, frontPointCount, "AC_Distance"    ,AC_Distance_Base, AC_Distance_A_Range, AC_Distance_B_Range, AC_Distance_C_Range, AC_Distance_D_Range, AC_Distance_E_Range);

                PlotIndexScatter(DecelerationAreaX, RSingleAreaX, rearPointCount, "DC_AreaX"     , DC_AreaX_Base, DC_AreaX_A_Range, DC_AreaX_B_Range, DC_AreaX_C_Range, DC_AreaX_D_Range, DC_AreaX_E_Range);
                PlotIndexScatter(DecelerationAreaY, RSingleAreaY, rearPointCount, "DC_AreaY"       ,DC_AreaY_Base, DC_AreaY_A_Range, DC_AreaY_B_Range, DC_AreaY_C_Range, DC_AreaY_D_Range, DC_AreaY_E_Range);
                PlotIndexScatter(DecelerationDistance, RSDistance, rearPointCount, "DC_Distance", DC_Distance_Base, DC_Distance_A_Range, DC_Distance_B_Range, DC_Distance_C_Range, DC_Distance_D_Range, DC_Distance_E_Range);

            }



            double ACTotaqlScore = -1;
            double DCTotaqlScore = -1;

            // label 값들 업데이트 해야함 인접치 누적치 단일치 rout 그레이드에 대한 값들을 업데이트 해야함
            string front_ScoreGradepath = Path.Combine(FrontPath, "ScoreGrade.csv");
            string rear_ScoreGradetcsvpath = Path.Combine(RearPath, "ScoreGrade.csv");

            try
            {
                // 파일을 배타적 모드(None)로 열어봅니다.
                using (FileStream stream = File.Open(front_ScoreGradepath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // 파일이 다른 프로세스에 의해 사용 중이면 IOException이 발생합니다.
                MessageBox.Show($"{front_ScoreGradepath}파일이 다른 프로그램에서 사용 중입니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogWarning("CSV", $"Acceleratrion 파일 생성 중 접근 시도 \n파일경로 :{front_ScoreGradepath}");
                return;
            }


            try
            {
                // 파일을 배타적 모드(None)로 열어봅니다.
                using (FileStream stream = File.Open(rear_ScoreGradetcsvpath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // 파일이 다른 프로세스에 의해 사용 중이면 IOException이 발생합니다.
                MessageBox.Show($"{rear_ScoreGradetcsvpath}파일이 다른 프로그램에서 사용 중입니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogWarning("CSV", $"Deceleratrion 파일 생성 중 접근 시도 \n파일경로 :{rear_ScoreGradetcsvpath}");

                return;
            }

            if (File.Exists(front_ScoreGradepath))
            {
                //파일들을 읽어와야함
                string[] lines = File.ReadAllLines(front_ScoreGradepath);
                //string[] values = line.Split(',');
                string[] Values = lines[1].Split(',');
                ACPeakX_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                ACPeakX_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                ACPeakX__nugeock.Text = double.Parse(Values[3]).ToString("f1");
                ACPeakX_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                ACPeakX_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[3].Split(',');
                ACPeakY_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                ACPeakY_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                ACPeakY__nugeock.Text = double.Parse(Values[3]).ToString("f1");
                ACPeakY_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                ACPeakY_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[9].Split(',');
                ACWidth_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                ACWidth_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                ACWidth_Sum.Text = double.Parse(Values[3]).ToString("f1");
                ACWidth_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                ACWidth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[11].Split(',');
                ACHeigth_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                ACHeigth_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                ACHeigth_Sum.Text = double.Parse(Values[3]).ToString("f1");
                ACHeigth_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                ACHeigth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[13].Split(',');
                ACArea_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                ACArea_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                ACArea_Sum.Text = double.Parse(Values[3]).ToString("f1");
                ACArea_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                ACArea_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                ///----------------------- AC AREAX
                Values = lines[5].Split(',');
                label519.Text = double.Parse(Values[1]).ToString("f1");
                label518.Text = double.Parse(Values[2]).ToString("f1");
                label517.Text = double.Parse(Values[3]).ToString("f1");
                label516.Text = double.Parse(Values[4]).ToString("f1");
                label515.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";
                ///----------------------- AC AREAY
                Values = lines[7].Split(',');
                label503.Text = double.Parse(Values[1]).ToString("f1");
                label502.Text = double.Parse(Values[2]).ToString("f1");
                label501.Text = double.Parse(Values[3]).ToString("f1");
                label500.Text = double.Parse(Values[4]).ToString("f1");
                label499.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";
                ///----------------------- AC DISTANCE
                Values = lines[15].Split(',');
                label487.Text = double.Parse(Values[1]).ToString("f1");
                label486.Text = double.Parse(Values[2]).ToString("f1");
                label485.Text = double.Parse(Values[3]).ToString("f1");
                label484.Text = double.Parse(Values[4]).ToString("f1");
                label483.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[17].Split(',');
                ACTotaqlScore = double.Parse(Values[1]);
                Logger.LogInfo("CSV", $"Acceleratrion 점수, 등급 동록 완료 \n파일경로 :{front_ScoreGradepath}");
            }
            else
            {
                //파일없음 로그 남겨야함
                Logger.LogWarning("CSV", $"Acceleratrion 점수, 등급 파일 없음 \n파일경로 :{front_ScoreGradepath}");
            }


            if (File.Exists(rear_ScoreGradetcsvpath))
            {
                //파일들을 읽어와야함
                string[] lines = File.ReadAllLines(rear_ScoreGradetcsvpath);
                //string[] values = line.Split(',');
                string[] Values = lines[1].Split(',');
                DCPeakX_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                DCPeakX_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                DCPeakX__nugeock.Text = double.Parse(Values[3]).ToString("f1");
                DCPeakX_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                DCPeakX_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[3].Split(',');
                DCPeakY_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                DCPeakY_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                DCPeakY__nugeock.Text = double.Parse(Values[3]).ToString("f1");
                DCPeakY_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                DCPeakY_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[9].Split(',');
                DCWidth_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                DCWidth_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                DCWidth_Sum.Text = double.Parse(Values[3]).ToString("f1");
                DCWidth_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                DCWidth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[11].Split(',');
                DCHeigth_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                DCHeigth_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                DCHeigth_Sum.Text = double.Parse(Values[3]).ToString("f1");
                DCHeigth_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                DCHeigth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[13].Split(',');
                DCArea_MaxOne.Text = double.Parse(Values[1]).ToString("f1");
                DCArea_MaxInterval.Text = double.Parse(Values[2]).ToString("f1");
                DCArea_Sum.Text = double.Parse(Values[3]).ToString("f1");
                DCArea_ROUT.Text = double.Parse(Values[4]).ToString("f1");
                DCArea_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                ///----------------------- dC AREAX
                Values = lines[5].Split(',');
                label471.Text = double.Parse(Values[1]).ToString("f1");
                label470.Text = double.Parse(Values[2]).ToString("f1");
                label469.Text = double.Parse(Values[3]).ToString("f1");
                label468.Text = double.Parse(Values[4]).ToString("f1");
                label467.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";
                ///----------------------- dC AREAY
                Values = lines[7].Split(',');
                label451.Text = double.Parse(Values[1]).ToString("f1");
                label450.Text = double.Parse(Values[2]).ToString("f1");
                label428.Text = double.Parse(Values[3]).ToString("f1");
                label427.Text = double.Parse(Values[4]).ToString("f1");
                label426.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";
                ///----------------------- dC DISTANCE
                Values = lines[15].Split(',');
                label403.Text = double.Parse(Values[1]).ToString("f1");
                label391.Text = double.Parse(Values[2]).ToString("f1");
                label352.Text = double.Parse(Values[3]).ToString("f1");
                label351.Text = double.Parse(Values[4]).ToString("f1");
                label390.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[17].Split(',');
                DCTotaqlScore = double.Parse(Values[1]);
                Logger.LogInfo("CSV", $"Deceleratrion 점수, 등급 동록 완료 \n파일경로 :{rear_ScoreGradetcsvpath}");
            }
            else
            {
                //파일없음 로그 남겨야함
                Logger.LogWarning("CSV", $"Deceleratrion 점수, 등급 파일 없음 \n파일경로 :{rear_ScoreGradetcsvpath}");
            }
            //Total 등급 및 스코어
            double TotalScore = (ACTotaqlScore * 0.5 + DCTotaqlScore * 0.5);
            int TotalGrade = TotalScore switch
            {
                >= 96 => 1,
                >= 91 => 2,
                >= 86 => 3,
                >= 81 => 4,
                _ => 5  // else와 같은 역할 (기본값)
            };

            int ACTotalGrade = ACTotaqlScore switch
            {
                >= 96 => 1,
                >= 91 => 2,
                >= 86 => 3,
                >= 81 => 4,
                _ => 5  // else와 같은 역할 (기본값)
            };

            int DCTotalGrade = DCTotaqlScore switch
            {
                >= 96 => 1,
                >= 91 => 2,
                >= 86 => 3,
                >= 81 => 4,
                _ => 5  // else와 같은 역할 (기본값)
            };

            TotalGradeScore.Text = $"기어 등급 : {TotalGrade}[{TotalScore.ToString("F0")}]";
            ACFinalGradelabel.Text = $"Drive : {ACTotalGrade}[{ACTotaqlScore.ToString("F0")}]";
            DCFinalGradelabel.Text = $"Coast : {DCTotalGrade}[{DCTotaqlScore.ToString("F0")}]";
            ////------------------------------------- 그레이 등급표 추가하자
            //단일치 g1~g5 그레이드 개수 표기
            //FSinglePeakX
            //FSinglePeakY
            //RSinglePeakX
            //RSinglePeakYVV
            //FrontHallMaxCount
            //RearHallMaxCount
            int[] ACpeakxMaxGradeCount = new int[5];
            int[] ACpeakyMaxGradeCount = new int[5];
            int[] DCpeakxMaxGradeCount = new int[5];
            int[] DCpeakyMaxGradeCount = new int[5];

            int[] ACAreaxMaxGradeCount = new int[5];
            int[] ACAreayMaxGradeCount = new int[5];
            int[] DCAreaxMaxGradeCount = new int[5];
            int[] DCAreayMaxGradeCount = new int[5];
            
            
            //★ 반드시 수정이 필요함! Unchangedseletedcmodel이걸 활용 한다
            //단일 통계에서 g1~g5에 대한 등급 peak x 및 peak y에 대한 범위를 불러와서 기준표를 만들어야함 
            string GradeBaselinefilePath = "./GradeBaseline.json";
            int flag = 0;
            // JSON 직렬화 옵션 (보기 좋게 들여쓰기)
            if (!File.Exists(GradeBaselinefilePath))
            {
                Logger.LogError("Json-GradeBaseline.json", $"단일 통계 G1~G5등급 판단에 필요한 파일 없음 {GradeBaselinefilePath}");
                flag = -1;
            }

            string GradeBaseLinejson = File.ReadAllText(GradeBaselinefilePath);
            RootData? GraderootData = JsonSerializer.Deserialize<RootData>(GradeBaseLinejson);

            if (GraderootData == null)
            {
                Logger.LogError("Json-GradeBaseline.json", $"단일 통계 G1~G5등급 판단에 필요한 파일 직렬화 실패 {GradeBaselinefilePath}");
                flag = -1;
            }


            
            if (GraderootData.TryGetValue(Unchangedseletedcmodel, out SignalMetrics? model1Data))
            {
                //모델에 대한 등급 범위 데이터가 있는 경우
                model1Data.AC.TryGetValue("PeakX_Max", out List<double>? AC_PeakX_Max);
                
                foreach (double ACpeakX in FSinglePeakX)
                {
                    double Value = Math.Abs(ACpeakX - FSinglePeakX.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 
                    if (Value >= AC_PeakX_Max[0])
                    {
                        ACpeakxMaxGradeCount[4]++;
                    }
                    else if (Value >= AC_PeakX_Max[2])
                    {
                        ACpeakxMaxGradeCount[3]++;
                    }
                    else if (Value >= AC_PeakX_Max[2])
                    {
                        ACpeakxMaxGradeCount[2]++;
                    }
                    else if (Value >= AC_PeakX_Max[3])
                    {
                        ACpeakxMaxGradeCount[1]++;
                    }
                    else
                    {
                        ACpeakxMaxGradeCount[0]++;
                    }
                }

                model1Data.AC.TryGetValue("PeakY_Max", out List<double>? AC_PeakY_Max);
                foreach (double ACpeaky in FSinglePeakY)
                {
                    double Value = Math.Abs(ACpeaky - FSinglePeakY.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= AC_PeakY_Max[0])
                    {
                        ACpeakyMaxGradeCount[4]++;
                    }
                    else if (Value >= AC_PeakY_Max[1])
                    {
                        ACpeakyMaxGradeCount[3]++;
                    }
                    else if (Value >= AC_PeakY_Max[2])
                    {
                        ACpeakyMaxGradeCount[2]++;
                    }
                    else if (Value >= AC_PeakY_Max[3])
                    {
                        ACpeakyMaxGradeCount[1]++;
                    }
                    else
                    {
                        ACpeakyMaxGradeCount[0]++;
                    }
                }

                model1Data.DC.TryGetValue("PeakX_Max", out List<double>? DC_PeakX_Max);
                foreach (double DCpeakX in RSinglePeakX)
                {
                    double Value = Math.Abs(DCpeakX - RSinglePeakX.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= DC_PeakX_Max[0])
                    {
                        DCpeakxMaxGradeCount[4]++;
                    }
                    else if (Value >= DC_PeakX_Max[1])
                    {
                        DCpeakxMaxGradeCount[3]++;
                    }
                    else if (Value >= DC_PeakX_Max[2])
                    {
                        DCpeakxMaxGradeCount[2]++;
                    }
                    else if (Value >= DC_PeakX_Max[3])
                    {
                        DCpeakxMaxGradeCount[1]++;
                    }
                    else
                    {
                        DCpeakxMaxGradeCount[0]++;
                    }
                }



                model1Data.DC.TryGetValue("PeakY_Max", out List<double>? DC_PeakY_Max);
                foreach (double DCpeaky in RSinglePeakY)
                {

                    double Value = Math.Abs(DCpeaky - RSinglePeakY.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= DC_PeakY_Max[0])
                    {
                        DCpeakyMaxGradeCount[4]++;
                    }
                    else if (Value >= DC_PeakY_Max[1])
                    {
                        DCpeakyMaxGradeCount[3]++;
                    }
                    else if (Value >= DC_PeakY_Max[2])
                    {
                        DCpeakyMaxGradeCount[2]++;
                    }
                    else if (Value >= DC_PeakY_Max[3])
                    {
                        DCpeakyMaxGradeCount[1]++;
                    }
                    else
                    {
                        DCpeakyMaxGradeCount[0]++;
                    }
                }


                model1Data.AC.TryGetValue("AreaX_Max", out List<double>? AC_AeraX_Max);
                foreach (double ACAreaX in FSingleAreaX)
                {
                    double Value = Math.Abs(ACAreaX - FSingleAreaX.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= AC_AeraX_Max[0])
                    {
                        ACAreaxMaxGradeCount[4]++;
                    }
                    else if (Value >= AC_AeraX_Max[1])
                    {
                        ACAreaxMaxGradeCount[3]++;
                    }
                    else if (Value >= AC_AeraX_Max[2])
                    {
                        ACAreaxMaxGradeCount[2]++;
                    }
                    else if (Value >= AC_AeraX_Max[3])
                    {
                        ACAreaxMaxGradeCount[1]++;
                    }
                    else
                    {
                        ACAreaxMaxGradeCount[0]++;
                    }
                }

                model1Data.AC.TryGetValue("AreaY_Max", out List<double>? AC_AeraY_Max);
                foreach (double ACAreay in FSingleAreaY)
                {
                    double Value = Math.Abs(ACAreay - FSingleAreaY.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= AC_AeraY_Max[0])
                    {
                        ACAreayMaxGradeCount[4]++;
                    }
                    else if (Value >= AC_AeraY_Max[1])
                    {
                        ACAreayMaxGradeCount[3]++;
                    }
                    else if (Value >= AC_AeraY_Max[2])
                    {
                        ACAreayMaxGradeCount[2]++;
                    }
                    else if (Value >= AC_AeraY_Max[3])
                    {
                        ACAreayMaxGradeCount[1]++;
                    }
                    else
                    {
                        ACAreayMaxGradeCount[0]++;
                    }
                }

                model1Data.DC.TryGetValue("AreaX_Max", out List<double>? DC_AeraX_Max);
                foreach (double DCAreaX in RSingleAreaX)
                {
                    double Value = Math.Abs(DCAreaX - RSingleAreaX.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= DC_AeraX_Max[0])
                    {
                        DCAreaxMaxGradeCount[4]++;
                    }
                    else if (Value >= DC_AeraX_Max[1])
                    {
                        DCAreaxMaxGradeCount[3]++;
                    }
                    else if (Value >= DC_AeraX_Max[2])
                    {
                        DCAreaxMaxGradeCount[2]++;
                    }
                    else if (Value >= DC_AeraX_Max[3])
                    {
                        DCAreaxMaxGradeCount[1]++;
                    }
                    else
                    {
                        DCAreaxMaxGradeCount[0]++;
                    }
                }

                model1Data.DC.TryGetValue("AreaY_Max", out List<double>? DC_AeraY_Max);
                foreach (double DCAreay in RSingleAreaY)
                {

                    double Value = Math.Abs(DCAreay - RSingleAreaY.Average());//이 부분도 기준 선을 평균으로 할건지 지정으로 할건지에 대해서 논의가 필요함 

                    if (Value >= DC_AeraY_Max[0])
                    {
                        DCAreayMaxGradeCount[4]++;
                    }
                    else if (Value >= DC_AeraY_Max[1])
                    {
                        DCAreayMaxGradeCount[3]++;
                    }
                    else if (Value >= DC_AeraY_Max[2])
                    {
                        DCAreayMaxGradeCount[2]++;
                    }
                    else if (Value >= DC_AeraY_Max[3])
                    {
                        DCAreayMaxGradeCount[1]++;
                    }
                    else
                    {
                        DCAreayMaxGradeCount[0]++;
                    }
                }

            }
            else
            {
                //모델에 설정된 데이터 값이 없는경우
                flag = -1;
                Logger.LogError("Json-GradeBaseline.json", $"선택된 모델 : {seletedcmodel}에 대한 설정된 데이터 값이 없습니다.");
            }

            if (flag==-1)
            {
                MessageBox.Show(
                this,
                $"{seletedcmodel}에 대한 등급 기준표 설정에 문제가 있습니다.\n 로그를 확인해주세요",
                " 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }

            label274.Text = ACpeakxMaxGradeCount[0].ToString();
            label272.Text = ACpeakxMaxGradeCount[1].ToString();
            label270.Text = ACpeakxMaxGradeCount[2].ToString();
            label244.Text = ACpeakxMaxGradeCount[3].ToString();
            label268.Text = ACpeakxMaxGradeCount[4].ToString();

            label535.Text = ACAreaxMaxGradeCount[0].ToString();
            label534.Text = ACAreaxMaxGradeCount[1].ToString();
            label533.Text = ACAreaxMaxGradeCount[2].ToString();
            label421.Text = ACAreaxMaxGradeCount[3].ToString();
            label422.Text = ACAreaxMaxGradeCount[4].ToString();

            label284.Text = ACpeakyMaxGradeCount[0].ToString();
            label282.Text = ACpeakyMaxGradeCount[1].ToString();
            label280.Text = ACpeakyMaxGradeCount[2].ToString();
            label276.Text = ACpeakyMaxGradeCount[3].ToString();
            label278.Text = ACpeakyMaxGradeCount[4].ToString();

            label541.Text = ACAreayMaxGradeCount[0].ToString();
            label540.Text = ACAreayMaxGradeCount[1].ToString();
            label539.Text = ACAreayMaxGradeCount[2].ToString();
            label537.Text = ACAreayMaxGradeCount[3].ToString();
            label538.Text = ACAreayMaxGradeCount[4].ToString();

            label294.Text = (ACpeakyMaxGradeCount[0] + ACpeakxMaxGradeCount[0] + ACAreayMaxGradeCount[0] + ACAreaxMaxGradeCount[0]).ToString();
            label292.Text = (ACpeakyMaxGradeCount[1] + ACpeakxMaxGradeCount[1] + ACAreayMaxGradeCount[1] + ACAreaxMaxGradeCount[1]).ToString();
            label290.Text = (ACpeakyMaxGradeCount[2] + ACpeakxMaxGradeCount[2] + ACAreayMaxGradeCount[2] + ACAreaxMaxGradeCount[2]).ToString();
            label286.Text = (ACpeakyMaxGradeCount[3] + ACpeakxMaxGradeCount[3] + ACAreayMaxGradeCount[3] + ACAreaxMaxGradeCount[3]).ToString();
            label288.Text = (ACpeakyMaxGradeCount[4] + ACpeakxMaxGradeCount[4] + ACAreayMaxGradeCount[4] + ACAreaxMaxGradeCount[4]).ToString();

            label315.Text = DCpeakxMaxGradeCount[0].ToString();
            label314.Text = DCpeakxMaxGradeCount[1].ToString();
            label313.Text = DCpeakxMaxGradeCount[2].ToString();
            label311.Text = DCpeakxMaxGradeCount[3].ToString();
            label312.Text = DCpeakxMaxGradeCount[4].ToString();
            //--------
            label553.Text = DCAreaxMaxGradeCount[0].ToString();
            label552.Text = DCAreaxMaxGradeCount[1].ToString();
            label551.Text = DCAreaxMaxGradeCount[2].ToString();
            label549.Text = DCAreaxMaxGradeCount[3].ToString();
            label550.Text = DCAreaxMaxGradeCount[4].ToString();

            label310.Text = DCpeakyMaxGradeCount[0].ToString();
            label309.Text = DCpeakyMaxGradeCount[1].ToString();
            label306.Text = DCpeakyMaxGradeCount[2].ToString();
            label304.Text = DCpeakyMaxGradeCount[3].ToString();
            label305.Text = DCpeakyMaxGradeCount[4].ToString();

            //--------------
            label547.Text = DCAreayMaxGradeCount[0].ToString();
            label546.Text = DCAreayMaxGradeCount[1].ToString();
            label545.Text = DCAreayMaxGradeCount[2].ToString();
            label543.Text = DCAreayMaxGradeCount[3].ToString();
            label544.Text = DCAreayMaxGradeCount[4].ToString();

            label303.Text = (DCpeakyMaxGradeCount[0] + DCpeakxMaxGradeCount[0] + DCAreayMaxGradeCount[0] + DCAreaxMaxGradeCount[0]).ToString();
            label302.Text = (DCpeakyMaxGradeCount[1] + DCpeakxMaxGradeCount[1] + DCAreayMaxGradeCount[1] + DCAreaxMaxGradeCount[1]).ToString();
            label300.Text = (DCpeakyMaxGradeCount[2] + DCpeakxMaxGradeCount[2] + DCAreayMaxGradeCount[2] + DCAreaxMaxGradeCount[2]).ToString();
            label296.Text = (DCpeakyMaxGradeCount[3] + DCpeakxMaxGradeCount[3] + DCAreayMaxGradeCount[3] + DCAreaxMaxGradeCount[3]).ToString();
            label298.Text = (DCpeakyMaxGradeCount[4] + DCpeakxMaxGradeCount[4] + DCAreayMaxGradeCount[4] + DCAreaxMaxGradeCount[4]).ToString();

            //이상치- outlier계산 필요
            /*
            FSinglePeakX = new float[FrontHallMaxCount];
            FSinglePeakY = new float[FrontHallMaxCount];
            FSingleWidth = new float[FrontHallMaxCount];
            FSingleHeight = new float[FrontHallMaxCount];
            FSingleArea = new float[FrontHallMaxCount];
            FSPatternX = new float[FrontHallMaxCount];
            FSPatternY = new float[FrontHallMaxCount];
            RSinglePeakX = new float[FrontHallMaxCount];
            RSinglePeakY = new float[FrontHallMaxCount];
            RSingleWidth = new float[FrontHallMaxCount];
            RSingleHeight = new float[FrontHallMaxCount];
            RSingleArea = new float[FrontHallMaxCount];
            RSPatternX = new float[FrontHallMaxCount];
            RSPatternY = new float[FrontHallMaxCount];              
             */
            int ACPeakxOutlierCount = 0;
            int ACPeakyOutlierCount = 0;
            int ACWidthOutlierCount = 0;
            int ACHeightOutlierCount = 0;
            int ACAreaOutlierCount = 0;

            int ACAreaxOutlierCount = 0;
            int ACAreayOutlierCount = 0;
            int ACDistaanceOutlierCount = 0;



            int DCPeakxOutlierCount = 0;
            int DCPeakyOutlierCount = 0;
            int DCWidthOutlierCount = 0;
            int DCHeightOutlierCount = 0;
            int DCAreaOutlierCount = 0;


            int DCAreaxOutlierCount = 0;
            int DCAreayOutlierCount = 0;
            int DCDistaanceOutlierCount = 0;
            //이상치 기준!
            double addRatio = 0.7;

            foreach (double value in RSDistance)
            {
                if (value <= addRatio * RSDistance.Average())
                {
                    DCDistaanceOutlierCount++;
                }
            }

            foreach (double value in RSingleAreaY)
            {
                if (value <= addRatio * RSingleAreaY.Average())
                {
                    DCAreayOutlierCount++;
                }
            }


            foreach (double value in RSingleAreaX)
            {
                if (value <= addRatio * RSingleAreaX.Average())
                {
                    DCAreaxOutlierCount++;
                }
            }




            foreach (double value in FSDistance)
            {
                if (value <= addRatio * FSDistance.Average())
                {
                    ACDistaanceOutlierCount++;
                }
            }

            foreach (double value in FSingleAreaY)
            {
                if (value <= addRatio * FSingleAreaY.Average())
                {
                    ACAreayOutlierCount++;
                }
            }


            foreach (double value in FSingleAreaX)
            {
                if (value <= addRatio * FSingleAreaX.Average())
                {
                    ACAreaxOutlierCount++;
                }
            }

            foreach (double value in FSinglePeakX)
            {
                if (value <= addRatio * FSinglePeakX.Average())
                {
                    ACPeakxOutlierCount++;
                }
            }


            foreach (double value in FSinglePeakY)
            {
                if (value <= addRatio * FSinglePeakY.Average())
                {
                    ACPeakyOutlierCount++;
                }
            }

            foreach (double value in FSingleWidth)
            {
                if (value <= addRatio * FSingleWidth.Average())
                {
                    ACWidthOutlierCount++;
                }
            }

            foreach (double value in FSingleHeight)
            {
                if (value <= addRatio * FSingleHeight.Average())
                {
                    ACHeightOutlierCount++;
                }
            }

            foreach (double value in FSingleArea)
            {
                if (value <= addRatio * FSingleArea.Average())
                {
                    ACAreaOutlierCount++;
                }
            }

            foreach (double value in RSinglePeakX)
            {
                if (value <= addRatio * RSinglePeakX.Average())
                {
                    DCPeakxOutlierCount++;
                }
            }


            foreach (double value in RSinglePeakY)
            {
                if (value <= addRatio * RSinglePeakY.Average())
                {
                    DCPeakyOutlierCount++;
                }
            }

            foreach (double value in RSingleWidth)
            {
                if (value <= addRatio * RSingleWidth.Average())
                {
                    DCWidthOutlierCount++;
                }
            }

            foreach (double value in RSingleHeight)
            {
                if (value <= addRatio * RSingleHeight.Average())
                {
                    DCHeightOutlierCount++;
                }
            }

            foreach (double value in RSingleArea)
            {
                if (value <= addRatio * RSingleArea.Average())
                {
                    DCAreaOutlierCount++;
                }
            }

            label334.Text = ACPeakxOutlierCount.ToString();
            label333.Text = ACPeakyOutlierCount.ToString();
            label332.Text = ACWidthOutlierCount.ToString();
            label331.Text = ACHeightOutlierCount.ToString();
            label330.Text = ACAreaOutlierCount.ToString();


            label556.Text = ACAreaxOutlierCount.ToString();
            label559.Text = ACAreayOutlierCount.ToString();
            label562.Text = ACDistaanceOutlierCount.ToString();



            label328.Text = (ACAreaxOutlierCount + ACAreayOutlierCount + ACDistaanceOutlierCount + ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount).ToString();
            label327.Text = (((ACAreaxOutlierCount + ACAreayOutlierCount + ACDistaanceOutlierCount + ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount) / ((double)8 * (double)FSinglePeakX.Length)) * 100).ToString("f1");

            label342.Text = DCPeakxOutlierCount.ToString();
            label341.Text = DCPeakyOutlierCount.ToString();
            label340.Text = DCWidthOutlierCount.ToString();
            label339.Text = DCHeightOutlierCount.ToString();
            label338.Text = DCAreaOutlierCount.ToString();

            label555.Text = DCAreaxOutlierCount.ToString();
            label558.Text = DCAreayOutlierCount.ToString();
            label561.Text = DCDistaanceOutlierCount.ToString();

            label336.Text = (DCAreaxOutlierCount + DCAreayOutlierCount + DCDistaanceOutlierCount + DCPeakxOutlierCount + DCPeakyOutlierCount + DCWidthOutlierCount + DCHeightOutlierCount + DCAreaOutlierCount).ToString();
            label335.Text = (((DCAreaxOutlierCount + DCAreayOutlierCount + DCDistaanceOutlierCount + DCPeakxOutlierCount + DCPeakyOutlierCount + DCWidthOutlierCount + DCHeightOutlierCount + DCAreaOutlierCount) / ((double)8 * (double)RSinglePeakX.Length)) * 100).ToString("f1");

            //배면 런아웃 최대 최소 값의 차이를 구한다!
            string SensingDataPath = Path.Combine(rowEntry.TrialFolderPath, "SensorData.csv");
            List<double> secondColumn = new List<double>();
            try
            {
                // FileShare.ReadWrite 권한을 주어 파일 잠금 충돌을 방지합니다.
                using (FileStream fs = new FileStream(SensingDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            // 쉼표로 분리하여 리스트에 저장
                            secondColumn.Add(double.Parse(line.Split(',')[1]));
                        }
                    }
                }


                label564.Text = $"배면 런아웃 : {(secondColumn.Max() - secondColumn.Min()).ToString("F3")}";
                label575.Text = $"{secondColumn.Max()}";
                label582.Text = $"{secondColumn.Min()}";
                label584.Text = (secondColumn.Max() - secondColumn.Min()).ToString("F3");

                double[] doubles = secondColumn.ToArray();
                float[] floatArray = doubles.Select(d => (float)d).ToArray();
                PlotIndexScatter(SensingROUT, floatArray, floatArray.Length, "Sensor_Data");
            }
            catch (Exception ex)
            {
                //
                label564.Text = "배면 런아웃 : -";
                Logger.LogError("CSV-배면 런아웃 CSV에러", $"파일 읽기 실패: {ex.Message}");
                label575.Text = $"-";
                label582.Text = $"-";
                label584.Text = $"-";
            }


            ListPanel.Visible = false;

            SingleStaticPanel.Location = new Point(10, 97);
            SingleStaticPanel.Size = new Size(1699, 819);
            SingleStaticPanel.Visible = true;
            selectedListSelectRowNumber = -1;

            ListLabel.BackColor = Color.FromArgb(64, 64, 64);
            ListLabel.ForeColor = Color.White;
            SingleStaticLabel.BackColor = Color.White;
            SingleStaticLabel.ForeColor = Color.FromArgb(64, 64, 64);

        }

        /* y축과 평행한 수선을 그리는 방법
         * // Y = 2 위치에 빨간색 가로 기준선 추가
var hline = formsPlot1.Plot.Add.HorizontalLine(2);
hline.Color = ScottPlot.Colors.Red;               // 선 색상
hline.LinePattern = ScottPlot.LinePattern.Dashed; // 점선 스타일
hline.LineWidth = 2;                              // 선 두께
         */
        //scatert데이터 그리기!
        private void PlotIndexScatter(FormsPlot plot, float[] yValues, int count, string yAxisLabel)
        {
            plot.Plot.Clear();
            if (count <= 0)
            {
                plot.Refresh();
                return;
            }

            double[] xs = new double[count];
            double[] ys = new double[count];
            for (int i = 0; i < count; i++)
            {
                xs[i] = i + 1;
                ys[i] = yValues[i];
            }



            var scatterValue = plot.Plot.Add.Scatter(xs, ys);// 이걸 한번 더하면 추가 데이터를 생성
            var tickGen = (ScottPlot.TickGenerators.NumericAutomatic)plot.Plot.Axes.Bottom.TickGenerator;
            tickGen.IntegerTicksOnly = true;
            plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            plot.Plot.Axes.Bottom.Label.Text = "Gear index";
            plot.Plot.Axes.AutoScale();
            plot.Refresh();
        }


        /* y축과 평행한 수선을 그리는 방법
 * // Y = 2 위치에 빨간색 가로 기준선 추가
var hline = formsPlot1.Plot.Add.HorizontalLine(2);
hline.Color = ScottPlot.Colors.Red;               // 선 색상
hline.LinePattern = ScottPlot.LinePattern.Dashed; // 점선 스타일
hline.LineWidth = 2;                              // 선 두께
 */
        //scatert데이터 그리기!
        private void PlotIndexScatter(FormsPlot plot, float[] yValues, int count, string yAxisLabel, double BaseValue, double RangeA, double RangeB, double RangeC, double RangeD, double RangeE)
        {
            plot.Plot.Clear();
            if (count <= 0)
            {
                plot.Refresh();
                return;
            }

            double[] xs = new double[count];
            double[] ys = new double[count];
            for (int i = 0; i < count; i++)
            {
                xs[i] = i + 1;
                ys[i] = yValues[i];
            }



            //A그레이드에 범위 설정 및 그리기
            var scatterbasevlue = plot.Plot.Add.HorizontalLine(BaseValue);
            scatterbasevlue.Color = ScottPlot.Colors.Black;
            scatterbasevlue.LinePattern = ScottPlot.LinePattern.Dashed;
            scatterbasevlue.LineWidth = 1;


            var RangeA_Top = plot.Plot.Add.HorizontalLine(BaseValue + RangeA);
            RangeA_Top.Color = ScottPlot.Colors.Green;
            RangeA_Top.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeA_Top.LineWidth = 1;
            var RangeA_Bottom = plot.Plot.Add.HorizontalLine(BaseValue - RangeA);
            RangeA_Bottom.Color = ScottPlot.Colors.Green;
            RangeA_Bottom.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeA_Bottom.LineWidth = 1;

            var RangeB_Top = plot.Plot.Add.HorizontalLine(BaseValue + RangeB);
            RangeB_Top.Color = ScottPlot.Colors.LimeGreen;
            RangeB_Top.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeB_Top.LineWidth = 1;
            var RangeB_Bottom = plot.Plot.Add.HorizontalLine(BaseValue - RangeB);
            RangeB_Bottom.Color = ScottPlot.Colors.LimeGreen;
            RangeB_Bottom.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeB_Bottom.LineWidth = 1;

            var RangeC_Top = plot.Plot.Add.HorizontalLine(BaseValue + RangeC);
            RangeC_Top.Color = ScottPlot.Colors.Yellow;
            RangeC_Top.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeC_Top.LineWidth = 1;
            var RangeC_Bottom = plot.Plot.Add.HorizontalLine(BaseValue - RangeC);
            RangeC_Bottom.Color = ScottPlot.Colors.Yellow;
            RangeC_Bottom.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeC_Bottom.LineWidth = 1;

            var RangeD_Top = plot.Plot.Add.HorizontalLine(BaseValue + RangeD);
            RangeD_Top.Color = ScottPlot.Colors.Orange;
            RangeD_Top.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeD_Top.LineWidth = 1;
            var RangeD_Bottom = plot.Plot.Add.HorizontalLine(BaseValue - RangeD);
            RangeD_Bottom.Color = ScottPlot.Colors.Orange;
            RangeD_Bottom.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeD_Bottom.LineWidth = 1;


            var RangeE_Top = plot.Plot.Add.HorizontalLine(BaseValue + RangeE);
            RangeE_Top.Color = ScottPlot.Colors.Red;
            RangeE_Top.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeE_Top.LineWidth = 1;
            var RangeE_Bottom = plot.Plot.Add.HorizontalLine(BaseValue - RangeE);
            RangeE_Bottom.Color = ScottPlot.Colors.Red;
            RangeE_Bottom.LinePattern = ScottPlot.LinePattern.Dashed;
            RangeE_Bottom.LineWidth = 1;


            var scatterValue = plot.Plot.Add.Scatter(xs, ys);// 이걸 한번 더하면 추가 데이터를 생성
            var tickGen = (ScottPlot.TickGenerators.NumericAutomatic)plot.Plot.Axes.Bottom.TickGenerator;
            tickGen.IntegerTicksOnly = true;
            plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            plot.Plot.Axes.Bottom.Label.Text = "Gear index";
            plot.Plot.Axes.AutoScale();
            plot.Refresh();
        }

        //Bar 그리기!
        private void PlotIndexBar(FormsPlot plot, float[] yValues, int count, string yAxisLabel)
        {
            plot.Plot.Clear();
            if (count <= 0)
            {
                plot.Refresh();
                return;
            }

            double[] xs = new double[count];
            double[] ys = new double[count];
            for (int i = 0; i < count; i++)
            {
                xs[i] = i + 1;
                ys[i] = yValues[i];
            }

            var barPlot = plot.Plot.Add.Bars(xs, ys);
            foreach (var bar in barPlot.Bars)
            {
                bar.Label = bar.Value.ToString("F0");
            }

            var tickGen = (ScottPlot.TickGenerators.NumericAutomatic)plot.Plot.Axes.Bottom.TickGenerator;
            tickGen.IntegerTicksOnly = true;

            var LefttickGen = (ScottPlot.TickGenerators.NumericAutomatic)plot.Plot.Axes.Left.TickGenerator;
            LefttickGen.IntegerTicksOnly = true;

            plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            plot.Plot.Axes.Bottom.Label.Text = "Grade";
            plot.Plot.Axes.AutoScale();
            plot.Refresh();
        }

        private void PluralStaticButton_Click(object sender, EventArgs e)
        {
            // 패널 초기화
            for (int i = PlurerFlowPanel1.Controls.Count - 1; i >= 0; i--)
            {
                Control control = PlurerFlowPanel1.Controls[i];

                // 패널에서 도구 제거
                PlurerFlowPanel1.Controls.RemoveAt(i);

                // 메모리 자원 반환
                control.Dispose();
            }

            for (int i = PlurerFlowPanel2.Controls.Count - 1; i >= 0; i--)
            {
                Control control = PlurerFlowPanel2.Controls[i];

                // 패널에서 도구 제거
                PlurerFlowPanel2.Controls.RemoveAt(i);

                // 메모리 자원 반환
                control.Dispose();
            }

            for (int i = PlurerFlowPanel2.Controls.Count - 1; i >= 0; i--)
            {
                Control control = PlurerFlowPanel2.Controls[i];

                // 패널에서 도구 제거
                PlurerFlowPanel2.Controls.RemoveAt(i);

                // 메모리 자원 반환
                control.Dispose();
            }



            if (!EnsureLoggedIn())
            {
                return;
            }
            //선택된 행의 패스들을 읽어오기 
            var selectedRowEntries = CollectSelectedListRowEntriesOrderedByRow();
            if (selectedRowEntries.Count < 2)
            {
                MessageBox.Show(
                    this,
                    "선택된 시행 행이 부족합니다. 검색 후 V 표시를 두 개 이상 해 주세요.",
                    "선택 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            int count = 0;
            float[] ACPeakXScores = new float[selectedRowEntries.Count];
            float[] ACPeakYScores = new float[selectedRowEntries.Count];
            float[] ACWidthScores = new float[selectedRowEntries.Count];
            float[] ACHeightScores = new float[selectedRowEntries.Count];
            float[] ACAreaScores = new float[selectedRowEntries.Count];
            float[] ACAreaXScores = new float[selectedRowEntries.Count];
            float[] ACAreaYScores = new float[selectedRowEntries.Count];
            float[] ACDistanceScores = new float[selectedRowEntries.Count];

            float[] ACPeakXMAD = new float[selectedRowEntries.Count];
            float[] ACPeakYMAD = new float[selectedRowEntries.Count];
            float[] ACWidthMAD = new float[selectedRowEntries.Count];
            float[] ACHeightMAD = new float[selectedRowEntries.Count];
            float[] ACAreaMAD = new float[selectedRowEntries.Count];
            float[] ACAreaXMAD = new float[selectedRowEntries.Count];
            float[] ACAreaYMAD = new float[selectedRowEntries.Count];
            float[] ACDistanceMAD = new float[selectedRowEntries.Count];

            float[] DCPeakXScores = new float[selectedRowEntries.Count];
            float[] DCPeakYScores = new float[selectedRowEntries.Count];
            float[] DCWidthScores = new float[selectedRowEntries.Count];
            float[] DCHeightScores = new float[selectedRowEntries.Count];
            float[] DCAreaScores = new float[selectedRowEntries.Count];
            float[] DCAreaXScores = new float[selectedRowEntries.Count];
            float[] DCAreaYScores = new float[selectedRowEntries.Count];
            float[] DCDistanceScores = new float[selectedRowEntries.Count];

            float[] DCPeakXMAD = new float[selectedRowEntries.Count];
            float[] DCPeakYMAD = new float[selectedRowEntries.Count];
            float[] DCWidthMAD = new float[selectedRowEntries.Count];
            float[] DCHeightMAD = new float[selectedRowEntries.Count];
            float[] DCAreaMAD = new float[selectedRowEntries.Count];
            float[] DCAreaXMAD = new float[selectedRowEntries.Count];
            float[] DCAreaYMAD = new float[selectedRowEntries.Count];
            float[] DCDistanceMAD = new float[selectedRowEntries.Count];

            int[] FinalGradeCount = new int[5];
            int[] ACGradeCount = new int[5];
            int[] DCGradeCount = new int[5];
            double AC_DCFinalScore = 0;
            double ACAddRatio = 0.5;
            Padding LabelPadding = new Padding(1, 1, 1, 1);
            Size Plurer2LabelSize = new Size(60, 57);

            foreach (var rowEntry in selectedRowEntries)
            {
                AC_DCFinalScore = 0;
                count++;

                var accelDir = Path.Combine(rowEntry.TrialFolderPath, "Acceleration");
                var decelDir = Path.Combine(rowEntry.TrialFolderPath, "Deceleration");
                string ACPath = Path.Combine(accelDir, "ScoreGrade.csv");
                string DCPath = Path.Combine(decelDir, "ScoreGrade.csv");





                string SensingDataPath = Path.Combine(rowEntry.TrialFolderPath, "SensorData.csv");
                double SensingRunOut = 0.0;
                try
                {
                    List<double> secondColumn = new List<double>();
                    // FileShare.ReadWrite 권한을 주어 파일 잠금 충돌을 방지합니다.
                    using (FileStream fs = new FileStream(SensingDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                // 쉼표로 분리하여 리스트에 저장
                                secondColumn.Add(double.Parse(line.Split(',')[1]));
                            }
                        }
                    }

                    SensingRunOut = secondColumn.Max() - secondColumn.Min();
                }
                catch (Exception ex)
                {
                    //
                    SensingRunOut = 0.0;
                    Logger.LogError("CSV-배면 런아웃 CSV에러", $"파일 읽기 실패: {ex.Message}");
                }

                ////파일이 있는지 확인 
                //AC 
                if (File.Exists(ACPath))
                {
                    Label Countlabel = new Label();
                    Countlabel.Text = $"{count}";
                    Countlabel.Name = $"CoutLabel{count}";
                    Countlabel.AutoSize = false;
                    Countlabel.Size = new Size(47, 57);
                    Countlabel.Visible = true;
                    Countlabel.ForeColor = Color.White;
                    Countlabel.BackColor = Color.FromArgb(64, 64, 64);
                    Countlabel.TextAlign = ContentAlignment.MiddleCenter;
                    Countlabel.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                    Countlabel.Margin = LabelPadding;
                    PlurerFlowPanel1.Controls.Add(Countlabel);
                    //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                    Label Countlabel2 = new Label();
                    Countlabel2.Text = $"{count}";
                    Countlabel2.Name = $"CoutLabel{count}";
                    Countlabel2.AutoSize = false;
                    Countlabel2.Size = new Size(47, 57);
                    Countlabel2.Visible = true;
                    Countlabel2.ForeColor = Color.White;
                    Countlabel2.BackColor = Color.FromArgb(64, 64, 64);
                    Countlabel2.TextAlign = ContentAlignment.MiddleCenter;
                    Countlabel2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                    Countlabel2.Margin = LabelPadding;
                    PlurerFlowPanel2.Controls.Add(Countlabel2);

                    //SNO 라벨 생성 데이터는 음...?
                    Label SNolabel = new Label();
                    SNolabel.Text = rowEntry.BcrFolderName;
                    SNolabel.Name = $"SNOLabel{count}"; // 이후 수정해야함
                    SNolabel.AutoSize = false;
                    SNolabel.Size = new Size(108, 57);
                    SNolabel.Visible = true;
                    SNolabel.ForeColor = Color.White;
                    SNolabel.BackColor = Color.FromArgb(64, 64, 64);
                    SNolabel.TextAlign = ContentAlignment.MiddleCenter;
                    SNolabel.Font = new Font("맑은 고딕", 10, FontStyle.Bold);
                    SNolabel.Margin = LabelPadding;
                    PlurerFlowPanel1.Controls.Add(SNolabel);


                    Label SNolabel2 = new Label();
                    SNolabel2.Text = rowEntry.BcrFolderName;
                    SNolabel2.Name = $"SNOLabel{count}"; // 이후 수정해야함
                    SNolabel2.AutoSize = false;
                    SNolabel2.Size = new Size(108, 57);
                    SNolabel2.Visible = true;
                    SNolabel2.ForeColor = Color.White;
                    SNolabel2.BackColor = Color.FromArgb(64, 64, 64);
                    SNolabel2.TextAlign = ContentAlignment.MiddleCenter;
                    SNolabel2.Font = new Font("맑은 고딕", 10, FontStyle.Bold);
                    SNolabel2.Margin = LabelPadding;
                    PlurerFlowPanel2.Controls.Add(SNolabel2);


                    //
                    Label RoutLabel = new Label();
                    RoutLabel.Text = $"{SensingRunOut}";
                    RoutLabel.Name = $"ROUT{count}"; // 이후 수정해야함
                    RoutLabel.AutoSize = false;
                    RoutLabel.Size = new Size(58, 57);
                    RoutLabel.Visible = true;
                    RoutLabel.ForeColor = Color.White;
                    RoutLabel.BackColor = Color.FromArgb(64, 64, 64);
                    RoutLabel.TextAlign = ContentAlignment.MiddleCenter;
                    RoutLabel.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                    RoutLabel.Margin = LabelPadding;
                    PlurerFlowPanel1.Controls.Add(RoutLabel);


                    Label RoutLabel2 = new Label();
                    RoutLabel2.Text = $"{SensingRunOut}";
                    RoutLabel2.Name = $"ROUT{count}"; // 이후 수정해야함
                    RoutLabel2.AutoSize = false;
                    RoutLabel2.Size = new Size(58, 57);
                    RoutLabel2.Visible = true;
                    RoutLabel2.ForeColor = Color.White;
                    RoutLabel2.BackColor = Color.FromArgb(64, 64, 64);
                    RoutLabel2.TextAlign = ContentAlignment.MiddleCenter;
                    RoutLabel2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                    RoutLabel2.Margin = LabelPadding;
                    PlurerFlowPanel2.Controls.Add(RoutLabel2);

                    //
                    Label RoutLabel = new Label();
                    RoutLabel.Text = $"{SensingRunOut}";
                    RoutLabel.Name = $"ROUT{count}"; // 이후 수정해야함
                    RoutLabel.AutoSize = false;
                    RoutLabel.Size = new Size(58, 57);
                    RoutLabel.Visible = true;
                    RoutLabel.ForeColor = Color.White;
                    RoutLabel.BackColor = Color.FromArgb(64, 64, 64);
                    RoutLabel.TextAlign = ContentAlignment.MiddleCenter;
                    RoutLabel.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                    RoutLabel.Margin = LabelPadding;
                    PlurerFlowPanel1.Controls.Add(RoutLabel);


                    Label RoutLabel2 = new Label();
                    RoutLabel2.Text = $"{SensingRunOut}";
                    RoutLabel2.Name = $"ROUT{count}"; // 이후 수정해야함
                    RoutLabel2.AutoSize = false;
                    RoutLabel2.Size = new Size(58, 57);
                    RoutLabel2.Visible = true;
                    RoutLabel2.ForeColor = Color.White;
                    RoutLabel2.BackColor = Color.FromArgb(64, 64, 64);
                    RoutLabel2.TextAlign = ContentAlignment.MiddleCenter;
                    RoutLabel2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                    RoutLabel2.Margin = LabelPadding;
                    PlurerFlowPanel2.Controls.Add(RoutLabel2);



                    string[] lines = File.ReadAllLines(ACPath);
                    if (lines.Length > 2)
                    {
                        //파일에 있고 데이터있는 경우
                        //PeakX_Score
                        string[] values = lines[2].Split(",");
                        string PeakX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label AcPeakX_Score = new Label();
                        AcPeakX_Score.Text = PeakX_Score;
                        AcPeakX_Score.Name = $"ACPeakX{count}";
                        AcPeakX_Score.AutoSize = false;
                        AcPeakX_Score.Size = new Size(60, 57);
                        AcPeakX_Score.Visible = true;
                        AcPeakX_Score.ForeColor = Color.White;
                        AcPeakX_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakX_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakX_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcPeakX_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AcPeakX_Score);
                        ACPeakXScores[count - 1] = float.Parse(PeakX_Score);

                        values = lines[4].Split(",");
                        string PeakY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label AcPeakY_Score = new Label();
                        AcPeakY_Score.Text = PeakY_Score;
                        AcPeakY_Score.Name = $"ACPeakY{count}";
                        AcPeakY_Score.AutoSize = false;
                        AcPeakY_Score.Size = new Size(60, 57);
                        AcPeakY_Score.Visible = true;
                        AcPeakY_Score.ForeColor = Color.White;
                        AcPeakY_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakY_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakY_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcPeakY_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AcPeakY_Score);
                        ACPeakYScores[count - 1] = float.Parse(PeakY_Score);

                        values = lines[6].Split(",");
                        string AreaX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label AcAreaX_Score = new Label();
                        AcAreaX_Score.Text = PeakY_Score;
                        AcAreaX_Score.Name = $"ACPeakY{count}";
                        AcAreaX_Score.AutoSize = false;
                        AcAreaX_Score.Size = new Size(60, 57);
                        AcAreaX_Score.Visible = true;
                        AcAreaX_Score.ForeColor = Color.White;
                        AcAreaX_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AcAreaX_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AcAreaX_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcAreaX_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AcAreaX_Score);
                        ACAreaXScores[count - 1] = float.Parse(AreaX_Score);

                        values = lines[8].Split(",");
                        string AreaY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label AcAreaY_Score = new Label();
                        AcAreaY_Score.Text = PeakY_Score;
                        AcAreaY_Score.Name = $"ACPeakY{count}";
                        AcAreaY_Score.AutoSize = false;
                        AcAreaY_Score.Size = new Size(60, 57);
                        AcAreaY_Score.Visible = true;
                        AcAreaY_Score.ForeColor = Color.White;
                        AcAreaY_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AcAreaY_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AcAreaY_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcAreaY_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AcAreaY_Score);
                        ACAreaYScores[count - 1] = float.Parse(AreaY_Score);



                        values = lines[10].Split(",");
                        string Width = (double.Parse(values[6]) * 0.2).ToString("f1");
                        Label Width_Score = new Label();
                        Width_Score.Text = Width;
                        Width_Score.Name = $"ACWidth{count}";
                        Width_Score.AutoSize = false;
                        Width_Score.Size = new Size(60, 57);
                        Width_Score.Visible = true;
                        Width_Score.ForeColor = Color.White;
                        Width_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Width_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Width_Score);
                        ACWidthScores[count - 1] = float.Parse(Width);

                        values = lines[12].Split(",");
                        string Height = (double.Parse(values[6]) * 0.1).ToString("f1");
                        Label Height_Score = new Label();
                        Height_Score.Text = Height;
                        Height_Score.Name = $"ACHeight{count}";
                        Height_Score.AutoSize = false;
                        Height_Score.Size = new Size(60, 57);
                        Height_Score.Visible = true;
                        Height_Score.ForeColor = Color.White;
                        Height_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Height_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Height_Score);
                        ACHeightScores[count - 1] = float.Parse(Height);


                        values = lines[14].Split(",");
                        string Area = (double.Parse(values[6]) * 0.1).ToString("f1");
                        Label Area_Score = new Label();
                        Area_Score.Text = Area;
                        Area_Score.Name = $"ACArea{count}";
                        Area_Score.AutoSize = false;
                        Area_Score.Size = new Size(60, 57);
                        Area_Score.Visible = true;
                        Area_Score.ForeColor = Color.White;
                        Area_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Area_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Area_Score);
                        ACAreaScores[count - 1] = float.Parse(Area);

                        values = lines[16].Split(",");
                        string Distance = (double.Parse(values[6]) * 0.1).ToString("f1");
                        Label Distance_Score = new Label();
                        Distance_Score.Text = Distance;
                        Distance_Score.Name = $"ACDistance{count}";
                        Distance_Score.AutoSize = false;
                        Distance_Score.Size = new Size(60, 57);
                        Distance_Score.Visible = true;
                        Distance_Score.ForeColor = Color.White;
                        Distance_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Distance_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Distance_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Distance_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Distance_Score);
                        ACDistanceScores[count - 1] = float.Parse(Distance);

                        values = lines[17].Split(",");
                        string FinalScore = (double.Parse(values[1])).ToString("f1");
                        Label AC_Final_Score = new Label();
                        AC_Final_Score.Text = FinalScore;
                        AC_Final_Score.Name = $"ACFinalScore{count}";
                        AC_Final_Score.AutoSize = false;
                        AC_Final_Score.Size = new Size(60, 57);
                        AC_Final_Score.Visible = true;
                        AC_Final_Score.ForeColor = Color.White;
                        AC_Final_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AC_Final_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AC_Final_Score);
                        AC_DCFinalScore += (double.Parse(FinalScore) * ACAddRatio);
                        //최종 등급
                        values = lines[18].Split(",");
                        string FinalGrade = (double.Parse(values[1])).ToString("F0");
                        Label AC_Final_Grade = new Label();
                        AC_Final_Grade.Text = FinalGrade;
                        AC_Final_Grade.Name = $"ACFinalGeade{count}";
                        AC_Final_Grade.AutoSize = false;
                        AC_Final_Grade.Size = new Size(60, 57);
                        AC_Final_Grade.Visible = true;
                        AC_Final_Grade.ForeColor = Color.White;
                        AC_Final_Grade.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Grade.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Grade.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AC_Final_Grade.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AC_Final_Grade);
                        int ACGrade = int.Parse(FinalGrade);
                        ACGradeCount[ACGrade - 1]++;




                        //이상치 비율 계산 필요
                        int ACPeakxOutlierCount = 0;
                        int ACPeakyOutlierCount = 0;
                        int ACAreaxOutlierCount = 0;
                        int ACAreayOutlierCount = 0;
                        int ACWidthOutlierCount = 0;
                        int ACHeightOutlierCount = 0;
                        int ACAreaOutlierCount = 0;
                        int ACDistanceOutlierCount = 0;

                        double addRatio = 0.7;

                        string[] ACLines = File.ReadAllLines(Path.Combine(accelDir, "ResultOutput.csv"));
                        float[] FSinglePeakX = new float[ACLines.Length];
                        float[] FSinglePeakY = new float[ACLines.Length];
                        float[] FSingleAreaX = new float[ACLines.Length];
                        float[] FSingleAreaY = new float[ACLines.Length];
                        float[] FSingleWidth = new float[ACLines.Length];
                        float[] FSingleHeight = new float[ACLines.Length];
                        float[] FSingleArea = new float[ACLines.Length];
                        float[] FSingleDistance = new float[ACLines.Length];
                        int ACcount = 0;
                        try
                        {
                            // 한 줄씩 읽어오기
                            foreach (string line in ACLines)
                            {
                                // 쉼표로 분리하여 배열에 담기
                                string[] va = line.Split(',');
                                if (va.Length < 9)
                                {
                                    Logger.LogWarning("FileIO", "Acceleration CSV 포맷 이상 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | line={line}");
                                    break;
                                }
                                if (!TryParseCsvFloat(va[1], out FSinglePeakX[ACcount])
                                    || !TryParseCsvFloat(va[2], out FSinglePeakY[ACcount])
                                    || !TryParseCsvFloat(va[3], out FSingleAreaX[ACcount])
                                    || !TryParseCsvFloat(va[4], out FSingleAreaY[ACcount])
                                    || !TryParseCsvFloat(va[5], out FSingleWidth[ACcount])
                                    || !TryParseCsvFloat(va[6], out FSingleHeight[ACcount])
                                    || !TryParseCsvFloat(va[7], out FSingleArea[ACcount])
                                    || !TryParseCsvFloat(va[8], out FSingleDistance[ACcount])

                                   )
                                {
                                    Logger.LogWarning("FileIO", "Acceleration CSV 숫자 파싱 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | line={line}");
                                    break;
                                }
                                ACcount++;
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("FileIO", "Acceleration CSV 읽기 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | {ex}");
                            MessageBox.Show(this, "Acceleration CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.", "CSV 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        foreach (double value in FSinglePeakX)
                        {
                            if (value <= addRatio * FSinglePeakX.Average())
                            {
                                ACPeakxOutlierCount++;
                            }
                        }


                        foreach (double value in FSinglePeakY)
                        {
                            if (value <= addRatio * FSinglePeakY.Average())
                            {
                                ACPeakyOutlierCount++;
                            }
                        }

                        foreach (double value in FSingleAreaX)
                        {
                            if (value <= addRatio * FSingleAreaX.Average())
                            {
                                ACAreaxOutlierCount++;
                            }
                        }


                        foreach (double value in FSingleAreaY)
                        {
                            if (value <= addRatio * FSingleAreaY.Average())
                            {
                                ACAreayOutlierCount++;
                            }
                        }

                        foreach (double value in FSingleWidth)
                        {
                            if (value <= addRatio * FSingleWidth.Average())
                            {
                                ACWidthOutlierCount++;
                            }
                        }

                        foreach (double value in FSingleHeight)
                        {
                            if (value <= addRatio * FSingleHeight.Average())
                            {
                                ACHeightOutlierCount++;
                            }
                        }

                        foreach (double value in FSingleArea)
                        {
                            if (value <= addRatio * FSingleArea.Average())
                            {
                                ACAreaOutlierCount++;
                            }
                        }

                        foreach (double value in FSingleDistance)
                        {
                            if (value <= addRatio * FSingleDistance.Average())
                            {
                                ACDistanceOutlierCount++;
                            }
                        }
                        double AC_OutlierRatio = ((ACAreayOutlierCount + ACAreaxOutlierCount + ACDistanceOutlierCount + ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount) / ((double)8 * (double)FSinglePeakX.Length)) * 100;

                        foreach (double value in FSingleDistance)
                        {
                            if (value <= addRatio * FSingleDistance.Average())
                            {
                                ACDistanceOutlierCount++;
                            }
                        }
                        double AC_OutlierRatio = ((ACAreayOutlierCount + ACAreaxOutlierCount + ACDistanceOutlierCount + ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount) / ((double)8 * (double)FSinglePeakX.Length)) * 100;

                        Label AC_Final_Outlier = new Label();
                        AC_Final_Outlier.Text = AC_OutlierRatio.ToString("f1");
                        AC_Final_Outlier.Name = $"ACFinalOutlier{count}";
                        AC_Final_Outlier.AutoSize = false;
                        AC_Final_Outlier.Size = new Size(60, 57);
                        AC_Final_Outlier.Visible = true;
                        AC_Final_Outlier.ForeColor = Color.White;
                        AC_Final_Outlier.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Outlier.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Outlier.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AC_Final_Outlier.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(AC_Final_Outlier);



                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.

                        double SinglePeakXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSinglePeakX.Average())).Average();
                        double SinglePeakYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSinglePeakY.Average())).Average();

                        double SingleAreaXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSingleAreaX.Average())).Average();
                        double SingleAreaYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSingleAreaY.Average())).Average();

                        double SingleWidthYMAD = FSingleWidth.Select(num => Math.Abs(num - FSingleWidth.Average())).Average();
                        double SingleHeightMAD = FSingleHeight.Select(num => Math.Abs(num - FSingleHeight.Average())).Average();
                        double SingleAreaMAD = FSingleArea.Select(num => Math.Abs(num - FSingleArea.Average())).Average();

                        double SingleDistanceMAD = FSinglePeakY.Select(num => Math.Abs(num - FSingleDistance.Average())).Average();
                        ACPeakXMAD[count - 1] = (float)SinglePeakXMAD;
                        ACPeakYMAD[count - 1] = (float)SinglePeakYMAD;
                        ACAreaXMAD[count - 1] = (float)SingleAreaXMAD;
                        ACAreaYMAD[count - 1] = (float)SingleAreaYMAD;
                        ACWidthMAD[count - 1] = (float)SingleWidthYMAD;
                        ACHeightMAD[count - 1] = (float)SingleHeightMAD;
                        ACAreaMAD[count - 1] = (float)SingleAreaMAD;
                        ACDistanceMAD[count - 1] = (float)SingleDistanceMAD;

                        Label AcPeakX_Score2 = new Label();
                        AcPeakX_Score2.Text = SinglePeakXMAD.ToString("f1");
                        AcPeakX_Score2.Name = $"ACPeakX{count}";
                        AcPeakX_Score2.AutoSize = false;
                        AcPeakX_Score2.Size = Plurer2LabelSize;
                        AcPeakX_Score2.Visible = true;
                        AcPeakX_Score2.ForeColor = Color.White;
                        AcPeakX_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakX_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakX_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcPeakX_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcPeakX_Score2);


                        Label AcPeakY_Score2 = new Label();
                        AcPeakY_Score2.Text = SinglePeakYMAD.ToString("f1");
                        AcPeakY_Score2.Name = $"ACPeakY{count}";
                        AcPeakY_Score2.AutoSize = false;
                        AcPeakY_Score2.Size = Plurer2LabelSize;
                        AcPeakY_Score2.Visible = true;
                        AcPeakY_Score2.ForeColor = Color.White;
                        AcPeakY_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakY_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakY_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcPeakY_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcPeakY_Score2);

                        Label AcAreaX_Score2 = new Label();
                        AcAreaX_Score2.Text = SingleAreaXMAD.ToString("f1");
                        AcAreaX_Score2.Name = $"ACAreaX{count}";
                        AcAreaX_Score2.AutoSize = false;
                        AcAreaX_Score2.Size = Plurer2LabelSize;
                        AcAreaX_Score2.Visible = true;
                        AcAreaX_Score2.ForeColor = Color.White;
                        AcAreaX_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcAreaX_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcAreaX_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcAreaX_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcAreaX_Score2);


                        Label AcAreaY_Score2 = new Label();
                        AcAreaY_Score2.Text = SingleAreaYMAD.ToString("f1");
                        AcAreaY_Score2.Name = $"ACAreaY{count}";
                        AcAreaY_Score2.AutoSize = false;
                        AcAreaY_Score2.Size = Plurer2LabelSize;
                        AcAreaY_Score2.Visible = true;
                        AcAreaY_Score2.ForeColor = Color.White;
                        AcAreaY_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcAreaY_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcAreaY_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcAreaY_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcAreaY_Score2);

                        Label Width_Score2 = new Label();
                        Width_Score2.Text = SingleWidthYMAD.ToString("f1");
                        Width_Score2.Name = $"ACWidth{count}";
                        Width_Score2.AutoSize = false;
                        Width_Score2.Size = Plurer2LabelSize;
                        Width_Score2.Visible = true;
                        Width_Score2.ForeColor = Color.White;
                        Width_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Width_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Width_Score2);

                        Label Height_Score2 = new Label();
                        Height_Score2.Text = SingleHeightMAD.ToString("f1");
                        Height_Score2.Name = $"ACHeight{count}";
                        Height_Score2.AutoSize = false;
                        Height_Score2.Size = Plurer2LabelSize;
                        Height_Score2.Visible = true;
                        Height_Score2.ForeColor = Color.White;
                        Height_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Height_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Height_Score2);

                        Label Area_Score2 = new Label();
                        Area_Score2.Text = SingleAreaMAD.ToString("f1");
                        Area_Score2.Name = $"ACArea{count}";
                        Area_Score2.AutoSize = false;
                        Area_Score2.Size = Plurer2LabelSize;
                        Area_Score2.Visible = true;
                        Area_Score2.ForeColor = Color.White;
                        Area_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Area_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Area_Score2);

                        Label AcDistance_Score2 = new Label();
                        AcDistance_Score2.Text = SingleDistanceMAD.ToString("f1");
                        AcDistance_Score2.Name = $"ACDistance{count}";
                        AcDistance_Score2.AutoSize = false;
                        AcDistance_Score2.Size = Plurer2LabelSize;
                        AcDistance_Score2.Visible = true;
                        AcDistance_Score2.ForeColor = Color.White;
                        AcDistance_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcDistance_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcDistance_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcDistance_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcDistance_Score2);

                        //최종 등급 => 평균 편차의 절대값 평균 패널에 붙일거
                        values = lines[18].Split(",");
                        string FinalGrade2 = (double.Parse(values[1])).ToString("F0");
                        Label AC_Final_Grade2 = new Label();
                        AC_Final_Grade2.Text = FinalGrade;
                        AC_Final_Grade2.Name = $"ACFinalGeade{count}";
                        AC_Final_Grade2.AutoSize = false;
                        AC_Final_Grade2.Size = Plurer2LabelSize;
                        AC_Final_Grade2.Visible = true;
                        AC_Final_Grade2.ForeColor = Color.White;
                        AC_Final_Grade2.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Grade2.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Grade2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AC_Final_Grade2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AC_Final_Grade2);

                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                        Label AC_Final_Outlier2 = new Label();
                        AC_Final_Outlier2.Text = AC_OutlierRatio.ToString("f1");
                        AC_Final_Outlier2.Name = $"ACFinalOutlier{count}";
                        AC_Final_Outlier2.AutoSize = false;
                        AC_Final_Outlier2.Size = Plurer2LabelSize;
                        AC_Final_Outlier2.Visible = true;
                        AC_Final_Outlier2.ForeColor = Color.White;
                        AC_Final_Outlier2.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Outlier2.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Outlier2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AC_Final_Outlier2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AC_Final_Outlier2);



                    }
                    else
                    {
                        Logger.LogError("CSV", $"CSV 파일에 데이터 없음 없음 \n 파일 경로 : {ACPath}");
                    }


                }
                else
                {
                    Logger.LogError("CSV", $"CSV 파일 없음 \n 파일 경로 : {ACPath}");
                    MessageBox.Show(
                        this,
                        $"CSV 파일이 존재 하지 않습니다.{ACPath}",
                        "AC 데이터 확인 필요",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }



                //DC
                if (File.Exists(DCPath))
                {
                    string[] lines = File.ReadAllLines(DCPath);
                    if (lines.Length > 2)
                    {
                        //파일에 있고 데이터있는 경우
                        //PeakX_Score
                        string[] values = lines[2].Split(",");
                        string PeakX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label DcPeakX_Score = new Label();
                        DcPeakX_Score.Text = PeakX_Score;
                        DcPeakX_Score.Name = $"ACPeakX{count}";
                        DcPeakX_Score.AutoSize = false;
                        DcPeakX_Score.Size = Plurer2LabelSize;
                        DcPeakX_Score.Visible = true;
                        DcPeakX_Score.ForeColor = Color.White;
                        DcPeakX_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DcPeakX_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DcPeakX_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DcPeakX_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DcPeakX_Score);
                        DCPeakXScores[count - 1] = float.Parse(PeakX_Score);

                        values = lines[4].Split(",");
                        string PeakY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label DcPeakY_Score = new Label();
                        DcPeakY_Score.Text = PeakY_Score;
                        DcPeakY_Score.Name = $"ACPeakY{count}";
                        DcPeakY_Score.AutoSize = false;
                        DcPeakY_Score.Size = Plurer2LabelSize;
                        DcPeakY_Score.Visible = true;
                        DcPeakY_Score.ForeColor = Color.White;
                        DcPeakY_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DcPeakY_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DcPeakY_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DcPeakY_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DcPeakY_Score);
                        DCPeakYScores[count - 1] = float.Parse(PeakY_Score);

                        values = lines[6].Split(",");
                        string AreaX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label DcAreaX_Score = new Label();
                        DcAreaX_Score.Text = AreaX_Score;
                        DcAreaX_Score.Name = $"ACAreaX{count}";
                        DcAreaX_Score.AutoSize = false;
                        DcAreaX_Score.Size = Plurer2LabelSize;
                        DcAreaX_Score.Visible = true;
                        DcAreaX_Score.ForeColor = Color.White;
                        DcAreaX_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DcAreaX_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DcAreaX_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DcAreaX_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DcAreaX_Score);
                        DCAreaXScores[count - 1] = float.Parse(AreaX_Score);

                        values = lines[8].Split(",");
                        string AreaY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                        Label DcAreaY_Score = new Label();
                        DcAreaY_Score.Text = AreaY_Score;
                        DcAreaY_Score.Name = $"ACAreaY{count}";
                        DcAreaY_Score.AutoSize = false;
                        DcAreaY_Score.Size = Plurer2LabelSize;
                        DcAreaY_Score.Visible = true;
                        DcAreaY_Score.ForeColor = Color.White;
                        DcAreaY_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DcAreaY_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DcAreaY_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DcAreaY_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DcAreaY_Score);
                        DCAreaYScores[count - 1] = float.Parse(AreaY_Score);

                        values = lines[10].Split(",");
                        string Width = (double.Parse(values[6]) * 0.2).ToString("f1");
                        Label Width_Score = new Label();
                        Width_Score.Text = Width;
                        Width_Score.Name = $"ACWidth{count}";
                        Width_Score.AutoSize = false;
                        Width_Score.Size = Plurer2LabelSize;
                        Width_Score.Visible = true;
                        Width_Score.ForeColor = Color.White;
                        Width_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Width_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Width_Score);
                        DCWidthScores[count - 1] = float.Parse(Width);

                        values = lines[12].Split(",");
                        string Height = (double.Parse(values[6]) * 0.1).ToString("f1");
                        Label Height_Score = new Label();
                        Height_Score.Text = Height;
                        Height_Score.Name = $"ACHeight{count}";
                        Height_Score.AutoSize = false;
                        Height_Score.Size = Plurer2LabelSize;
                        Height_Score.Visible = true;
                        Height_Score.ForeColor = Color.White;
                        Height_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Height_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Height_Score);
                        DCHeightScores[count - 1] = float.Parse(Height);


                        values = lines[14].Split(",");
                        string Area = (double.Parse(values[6]) * 0.1).ToString("f1");
                        Label Area_Score = new Label();
                        Area_Score.Text = Area;
                        Area_Score.Name = $"ACArea{count}";
                        Area_Score.AutoSize = false;
                        Area_Score.Size = Plurer2LabelSize;
                        Area_Score.Visible = true;
                        Area_Score.ForeColor = Color.White;
                        Area_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Area_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Area_Score);
                        DCAreaScores[count - 1] = float.Parse(Area);

                        values = lines[16].Split(",");
                        string Distance = (double.Parse(values[6]) * 0.1).ToString("f1");
                        Label Distance_Score = new Label();
                        Distance_Score.Text = Distance;
                        Distance_Score.Name = $"ACDistance{count}";
                        Distance_Score.AutoSize = false;
                        Distance_Score.Size = Plurer2LabelSize;
                        Distance_Score.Visible = true;
                        Distance_Score.ForeColor = Color.White;
                        Distance_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Distance_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Distance_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Distance_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(Distance_Score);
                        DCDistanceScores[count - 1] = float.Parse(Distance);

                        values = lines[17].Split(",");
                        string FinalScore = (double.Parse(values[1])).ToString("f1");
                        Label DC_Final_Score = new Label();
                        DC_Final_Score.Text = FinalScore;
                        DC_Final_Score.Name = $"ACFinalScore{count}";
                        DC_Final_Score.AutoSize = false;
                        DC_Final_Score.Size = Plurer2LabelSize;
                        DC_Final_Score.Visible = true;
                        DC_Final_Score.ForeColor = Color.White;
                        DC_Final_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Score.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DC_Final_Score.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DC_Final_Score);
                        AC_DCFinalScore += (double.Parse(FinalScore) * (1 - ACAddRatio));

                        if (AC_DCFinalScore >= 96)
                        {
                            FinalGradeCount[0]++;
                        }
                        else if (AC_DCFinalScore >= 91)
                        {
                            FinalGradeCount[1]++;
                        }

                        else if (AC_DCFinalScore >= 86)
                        {
                            FinalGradeCount[2]++;
                        }
                        else if (AC_DCFinalScore >= 81)
                        {
                            FinalGradeCount[3]++;
                        }
                        else
                        {
                            FinalGradeCount[4]++;
                        }


                        //최종 등급
                        values = lines[18].Split(",");
                        string FinalGrade = (double.Parse(values[1])).ToString("F0");
                        Label DC_Final_Grade = new Label();
                        DC_Final_Grade.Text = FinalGrade;
                        DC_Final_Grade.Name = $"DCFinalGeade{count}";
                        DC_Final_Grade.AutoSize = false;
                        DC_Final_Grade.Size = Plurer2LabelSize;
                        DC_Final_Grade.Visible = true;
                        DC_Final_Grade.ForeColor = Color.White;
                        DC_Final_Grade.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Grade.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Grade.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DC_Final_Grade.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DC_Final_Grade);
                        DCGradeCount[int.Parse(FinalGrade) - 1]++;

                        //

                        //이상치 비율 계산 필요
                        int DCPeakxOutlierCount = 0;
                        int DCPeakyOutlierCount = 0;
                        int DCAreaxOutlierCount = 0;
                        int DCAreayOutlierCount = 0;
                        int DCWidthOutlierCount = 0;
                        int DCHeightOutlierCount = 0;
                        int DCAreaOutlierCount = 0;
                        int DCDistanceOutlierCount = 0;

                        double addRatio = 0.7;

                        string[] DCLines = File.ReadAllLines(Path.Combine(decelDir, "ResultOutput.csv"));
                        float[] RSinglePeakX = new float[DCLines.Length];
                        float[] RSinglePeakY = new float[DCLines.Length];
                        float[] RSingleAreaX = new float[DCLines.Length];
                        float[] RSingleAreaY = new float[DCLines.Length];
                        float[] RSingleWidth = new float[DCLines.Length];
                        float[] RSingleHeight = new float[DCLines.Length];
                        float[] RSingleArea = new float[DCLines.Length];
                        float[] RSingleDistance = new float[DCLines.Length];
                        int DCCcount = 0;
                        try
                        {
                            // 한 줄씩 읽어오기
                            foreach (string line in DCLines)
                            {

                                // 쉼표로 분리하여 배열에 담기
                                string[] va = line.Split(',');
                                if (va.Length < 8)
                                {
                                    Logger.LogWarning("FileIO", "Dcceleration CSV 포맷 이상 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(decelDir, "ResultOutput.csv")} | line={line}");
                                    break;
                                }
                                if (!TryParseCsvFloat(va[1], out RSinglePeakX[DCCcount])
                                    || !TryParseCsvFloat(va[2], out RSinglePeakY[DCCcount])
                                    || !TryParseCsvFloat(va[3], out RSingleAreaX[DCCcount])
                                    || !TryParseCsvFloat(va[4], out RSingleAreaY[DCCcount])
                                    || !TryParseCsvFloat(va[5], out RSingleWidth[DCCcount])
                                    || !TryParseCsvFloat(va[6], out RSingleHeight[DCCcount])
                                    || !TryParseCsvFloat(va[7], out RSingleArea[DCCcount])
                                    || !TryParseCsvFloat(va[8], out RSingleDistance[DCCcount])
                                   )
                                {
                                    Logger.LogWarning("FileIO", "Dcceleration CSV 숫자 파싱 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(decelDir, "ResultOutput.csv")} | line={line}");
                                    break;
                                }
                                DCCcount++;
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("FileIO", "Dcceleration CSV 읽기 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | {ex}");
                            MessageBox.Show(this, "Dcceleration CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.", "CSV 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        foreach (double value in RSinglePeakX)
                        {
                            if (value <= addRatio * RSinglePeakX.Average())
                            {
                                DCPeakxOutlierCount++;
                            }
                        }


                        foreach (double value in RSinglePeakY)
                        {
                            if (value <= addRatio * RSinglePeakY.Average())
                            {
                                DCPeakyOutlierCount++;
                            }
                        }

                        foreach (double value in RSingleAreaX)
                        {
                            if (value <= addRatio * RSingleAreaX.Average())
                            {
                                DCAreaxOutlierCount++;
                            }
                        }


                        foreach (double value in RSingleAreaY)
                        {
                            if (value <= addRatio * RSingleAreaY.Average())
                            {
                                DCAreayOutlierCount++;
                            }
                        }

                        foreach (double value in RSingleWidth)
                        {
                            if (value <= addRatio * RSingleWidth.Average())
                            {
                                DCWidthOutlierCount++;
                            }
                        }

                        foreach (double value in RSingleHeight)
                        {
                            if (value <= addRatio * RSingleHeight.Average())
                            {
                                DCHeightOutlierCount++;
                            }
                        }

                        foreach (double value in RSingleArea)
                        {
                            if (value <= addRatio * RSingleArea.Average())
                            {
                                DCAreaOutlierCount++;
                            }
                        }
                        foreach (double value in RSingleDistance)
                        {
                            if (value <= addRatio * RSingleDistance.Average())
                            {
                                DCDistanceOutlierCount++;
                            }
                        }

                        double DC_OutlierRatio = ((DCAreaxOutlierCount + DCAreayOutlierCount + DCDistanceOutlierCount + DCPeakxOutlierCount + DCPeakyOutlierCount + DCWidthOutlierCount + DCHeightOutlierCount + DCAreaOutlierCount) / ((double)8 * (double)RSinglePeakX.Length)) * 100;

                        Label DC_Final_Outlier = new Label();
                        DC_Final_Outlier.Text = DC_OutlierRatio.ToString("f1");
                        DC_Final_Outlier.Name = $"DCFinalOutlier{count}";
                        DC_Final_Outlier.AutoSize = false;
                        DC_Final_Outlier.Size = Plurer2LabelSize;
                        DC_Final_Outlier.Visible = true;
                        DC_Final_Outlier.ForeColor = Color.White;
                        DC_Final_Outlier.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Outlier.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Outlier.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DC_Final_Outlier.Margin = LabelPadding;
                        PlurerFlowPanel1.Controls.Add(DC_Final_Outlier);



                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                        // 1. 데이터의 원본 평균 구하기
                        // 2. 각 요소에서 평균을 뺀 절대값(Math.Abs)들의 평균을 다시 구하기                       
                        double SinglePeakXMAD = RSinglePeakX.Select(num => Math.Abs(num - RSinglePeakX.Average())).Average();
                        double SinglePeakYMAD = RSinglePeakY.Select(num => Math.Abs(num - RSinglePeakY.Average())).Average();
                        double SingleAreaXMAD = RSingleAreaX.Select(num => Math.Abs(num - RSingleAreaX.Average())).Average();
                        double SingleAreaYMAD = RSingleAreaY.Select(num => Math.Abs(num - RSingleAreaY.Average())).Average();
                        double SingleWidthYMAD = RSingleWidth.Select(num => Math.Abs(num - RSingleWidth.Average())).Average();
                        double SingleHeightMAD = RSingleHeight.Select(num => Math.Abs(num - RSingleHeight.Average())).Average();
                        double SingleAreaMAD = RSingleArea.Select(num => Math.Abs(num - RSingleArea.Average())).Average();
                        double SingleDistanceMAD = RSingleDistance.Select(num => Math.Abs(num - RSingleDistance.Average())).Average();
                        DCPeakXMAD[count - 1] = (float)SinglePeakXMAD;
                        DCPeakYMAD[count - 1] = (float)SinglePeakYMAD;
                        DCAreaXMAD[count - 1] = (float)SingleAreaXMAD;
                        DCAreaYMAD[count - 1] = (float)SingleAreaYMAD;
                        DCWidthMAD[count - 1] = (float)SingleWidthYMAD;
                        DCHeightMAD[count - 1] = (float)SingleHeightMAD;
                        DCAreaMAD[count - 1] = (float)SingleAreaMAD;
                        DCDistanceMAD[count - 1] = (float)SingleDistanceMAD;

                        Label AcPeakX_Score2 = new Label();
                        AcPeakX_Score2.Text = SinglePeakXMAD.ToString("f1");
                        AcPeakX_Score2.Name = $"ACPeakX{count}";
                        AcPeakX_Score2.AutoSize = false;
                        AcPeakX_Score2.Size = Plurer2LabelSize;
                        AcPeakX_Score2.Visible = true;
                        AcPeakX_Score2.ForeColor = Color.White;
                        AcPeakX_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakX_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakX_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcPeakX_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcPeakX_Score2);


                        Label AcPeakY_Score2 = new Label();
                        AcPeakY_Score2.Text = SinglePeakYMAD.ToString("f1");
                        AcPeakY_Score2.Name = $"ACPeakY{count}";
                        AcPeakY_Score2.AutoSize = false;
                        AcPeakY_Score2.Size = Plurer2LabelSize;
                        AcPeakY_Score2.Visible = true;
                        AcPeakY_Score2.ForeColor = Color.White;
                        AcPeakY_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakY_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakY_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcPeakY_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcPeakY_Score2);

                        Label AcAreaX_Score2 = new Label();
                        AcAreaX_Score2.Text = SingleAreaXMAD.ToString("f1");
                        AcAreaX_Score2.Name = $"ACAreaX{count}";
                        AcAreaX_Score2.AutoSize = false;
                        AcAreaX_Score2.Size = Plurer2LabelSize;
                        AcAreaX_Score2.Visible = true;
                        AcAreaX_Score2.ForeColor = Color.White;
                        AcAreaX_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcAreaX_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcAreaX_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcAreaX_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcAreaX_Score2);


                        Label AcAreaY_Score2 = new Label();
                        AcAreaY_Score2.Text = SingleAreaYMAD.ToString("f1");
                        AcAreaY_Score2.Name = $"ACAreaY{count}";
                        AcAreaY_Score2.AutoSize = false;
                        AcAreaY_Score2.Size = Plurer2LabelSize;
                        AcAreaY_Score2.Visible = true;
                        AcAreaY_Score2.ForeColor = Color.White;
                        AcAreaY_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcAreaY_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcAreaY_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        AcAreaY_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(AcAreaY_Score2);

                        Label Width_Score2 = new Label();
                        Width_Score2.Text = SingleWidthYMAD.ToString("f1");
                        Width_Score2.Name = $"ACWidth{count}";
                        Width_Score2.AutoSize = false;
                        Width_Score2.Size = Plurer2LabelSize;
                        Width_Score2.Visible = true;
                        Width_Score2.ForeColor = Color.White;
                        Width_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Width_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Width_Score2);

                        Label Height_Score2 = new Label();
                        Height_Score2.Text = SingleHeightMAD.ToString("f1");
                        Height_Score2.Name = $"ACHeight{count}";
                        Height_Score2.AutoSize = false;
                        Height_Score2.Size = Plurer2LabelSize;
                        Height_Score2.Visible = true;
                        Height_Score2.ForeColor = Color.White;
                        Height_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Height_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Height_Score2);

                        Label Area_Score2 = new Label();
                        Area_Score2.Text = SingleAreaMAD.ToString("f1");
                        Area_Score2.Name = $"ACArea{count}";
                        Area_Score2.AutoSize = false;
                        Area_Score2.Size = Plurer2LabelSize;
                        Area_Score2.Visible = true;
                        Area_Score2.ForeColor = Color.White;
                        Area_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Area_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Area_Score2);

                        Label Distance_Score2 = new Label();
                        Distance_Score2.Text = SingleDistanceMAD.ToString("f1");
                        Distance_Score2.Name = $"ACDistance{count}";
                        Distance_Score2.AutoSize = false;
                        Distance_Score2.Size = Plurer2LabelSize;
                        Distance_Score2.Visible = true;
                        Distance_Score2.ForeColor = Color.White;
                        Distance_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Distance_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Distance_Score2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        Distance_Score2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(Distance_Score2);

                        //최종 등급 => 평균 편차의 절대값 평균 패널에 붙일거
                        values = lines[18].Split(",");
                        string FinalGrade2 = (double.Parse(values[1])).ToString("F0");
                        Label DC_Final_Grade2 = new Label();
                        DC_Final_Grade2.Text = FinalGrade;
                        DC_Final_Grade2.Name = $"ACFinalGeade{count}";
                        DC_Final_Grade2.AutoSize = false;
                        DC_Final_Grade2.Size = Plurer2LabelSize;
                        DC_Final_Grade2.Visible = true;
                        DC_Final_Grade2.ForeColor = Color.White;
                        DC_Final_Grade2.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Grade2.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Grade2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DC_Final_Grade2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(DC_Final_Grade2);

                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                        Label DC_Final_Outlier2 = new Label();
                        DC_Final_Outlier2.Text = DC_OutlierRatio.ToString("f1");
                        DC_Final_Outlier2.Name = $"ACFinalOutlier{count}";
                        DC_Final_Outlier2.AutoSize = false;
                        DC_Final_Outlier2.Size = Plurer2LabelSize;
                        DC_Final_Outlier2.Visible = true;
                        DC_Final_Outlier2.ForeColor = Color.White;
                        DC_Final_Outlier2.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Outlier2.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Outlier2.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
                        DC_Final_Outlier2.Margin = LabelPadding;
                        PlurerFlowPanel2.Controls.Add(DC_Final_Outlier2);
                    }
                    else
                    {
                        Logger.LogError("CSV", $"CSV 파일에 데이터 없음 없음 \n 파일 경로 : {DCPath}");
                    }


                }
                else
                {
                    Logger.LogError("CSV", $"CSV 파일 없음 \n 파일 경로 : {DCPath}");
                    MessageBox.Show(
                            this,
                            $"CSV 파일이 존재 하지 않습니다.{DCPath}",
                            "DC 데이터 확인 필요",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                    return;
                }



            }

            PlotIndexScatter(ACPeakXScorePlot, ACPeakXScores, ACPeakXScores.Length, "AC_PeakX_Scores");
            PlotIndexScatter(ACPeakYScorePlot, ACPeakYScores, ACPeakYScores.Length, "AC_PeakY_Scores");
            PlotIndexScatter(ACAreaXScorePlot, ACAreaXScores, ACAreaXScores.Length, "AC_AreaX_Scores");
            PlotIndexScatter(ACAreaYScorePlot, ACAreaYScores, ACAreaYScores.Length, "AC_AreaY_Scores");

            PlotIndexScatter(ACWidthScorePlot, ACWidthScores, ACWidthScores.Length, "AC_Length_Scores");
            PlotIndexScatter(ACHeightScorePlot, ACHeightScores, ACHeightScores.Length, "AC_Height_Scores");
            PlotIndexScatter(ACAreaScorePlot, ACAreaScores, ACAreaScores.Length, "AC_Area_Scores");
            PlotIndexScatter(ACDistanceScorePlot, ACDistanceScores, ACDistanceScores.Length, "AC_Distance_Scores");

            PlotIndexScatter(ACPeakXMADPlot, ACPeakXMAD, ACPeakXMAD.Length, "AC_PeakX_MAD");
            PlotIndexScatter(ACPeakYMADPlot, ACPeakYMAD, ACPeakYMAD.Length, "AC_PeakY_MAD");
            PlotIndexScatter(ACAreaXMADPlot, ACAreaXMAD, ACAreaXMAD.Length, "AC_AreaX_MAD");
            PlotIndexScatter(ACAreaYMADPlot, ACAreaYMAD, ACAreaYMAD.Length, "AC_AreaY_MAD");

            PlotIndexScatter(ACWidthMADPlot, ACWidthMAD, ACWidthMAD.Length, "AC_Length_MAD");
            PlotIndexScatter(ACHeightMADPlot, ACHeightMAD, ACHeightMAD.Length, "AC_Height_MAD");
            PlotIndexScatter(ACAreaMADPlot, ACAreaMAD, ACAreaMAD.Length, "AC_Area_MAD");
            PlotIndexScatter(ACDistanceMADPlot, ACDistanceMAD, ACDistanceMAD.Length, "AC_Distance_MAD");


            PlotIndexScatter(DCPeakXScorePlot, DCPeakXScores, DCPeakXScores.Length, "DC_PeakX_Scores");
            PlotIndexScatter(DCPeakYScorePlot, DCPeakYScores, DCPeakYScores.Length, "DC_PeakY_Scores");
            PlotIndexScatter(DCAreaXScorePlot, DCAreaXScores, DCAreaXScores.Length, "DC_AreaX_Scores");
            PlotIndexScatter(DCAreaYScorePlot, DCAreaYScores, DCAreaYScores.Length, "DC_AreaY_Scores");

            PlotIndexScatter(DCWidthScorePlot, DCWidthScores, DCWidthScores.Length, "DC_Length_Scores");
            PlotIndexScatter(DCHeightScorePlot, DCHeightScores, DCHeightScores.Length, "DC_Height_Scores");
            PlotIndexScatter(DCAreaScorePlot, DCAreaScores, DCAreaScores.Length, "DC_Area_Scores");
            PlotIndexScatter(DCDistanceScorePlot, DCDistanceScores, DCDistanceScores.Length, "DC_Distance_Scores");

            PlotIndexScatter(DCPeakXMADPlot, DCPeakXMAD, DCPeakXMAD.Length, "DC_PeakX_MAD");
            PlotIndexScatter(DCPeakYMADPlot, DCPeakYMAD, DCPeakYMAD.Length, "DC_PeakY_MAD");
            PlotIndexScatter(DCAreaXMADPlot, DCAreaXMAD, DCAreaXMAD.Length, "DC_AreaX_MAD");
            PlotIndexScatter(DCAreaYMADPlot, DCAreaYMAD, DCAreaYMAD.Length, "DC_AreaY_MAD");

            PlotIndexScatter(DCWidthMADPlot, DCWidthMAD, DCWidthMAD.Length, "DC_Length_MAD");
            PlotIndexScatter(DCHeightMADPlot, DCHeightMAD, DCHeightMAD.Length, "DC_Height_MAD");
            PlotIndexScatter(DCAreaMADPlot, DCAreaMAD, DCAreaMAD.Length, "DC_Area_MAD");
            PlotIndexScatter(DCDistanceMADPlot, DCDistanceMAD, DCDistanceMAD.Length, "DC_Distance_MAD");

            //float로 형변환 하기
            float[] tempdoubleArray = FinalGradeCount.Select(f => (float)f).ToArray();
            PlotIndexBar(FinalGradeCountPlot, tempdoubleArray, FinalGradeCount.Length, "FinalGrade");
            tempdoubleArray = ACGradeCount.Select(f => (float)f).ToArray();
            PlotIndexBar(FinalACGradeCountPlot, tempdoubleArray, FinalGradeCount.Length, "FinalACGrade");
            tempdoubleArray = DCGradeCount.Select(f => (float)f).ToArray();
            PlotIndexBar(FinalDCGradeCountPlot, tempdoubleArray, FinalGradeCount.Length, "FinalDCGrade");




            SettingPerulStaticPanel();
        }






        private void PluralStaticLabel_Click(object sender, EventArgs e)
        {
            SettingPerulStaticPanel();
        }





        private void SingleStaticLabel_Click(object sender, EventArgs e)
        {
            if (!_LoginManager.BoolLoginCheck)
            {
                return;
            }

            PerulStaticPanel.Visible = false;
        }

        private void ListLabel_Click(object sender, EventArgs e)
        {
            if (!_LoginManager.BoolLoginCheck)
            {
                return;
            }

            PerulStaticPanel.Visible = false;
        }

        private void NaviCaldataLabel_Click(object sender, EventArgs e)
        {
            CaldataPanel.Visible = true;
            CaldataPanel.Location = new Point(210, 162);
            CaldataPanel.Size = new Size(1710, 1018);

        }

        private void NavlTCPIPLabel_Click(object sender, EventArgs e)
        {
            CaldataPanel.Visible = false;
        }


        private void CalFrontImgSelectButton_Click(object? sender, EventArgs e)
        {
            SelectAndShowCalibrationImage(FrontOriginPictureBox, 0);
        }

        private void CalRearImgSelectButton_Click(object? sender, EventArgs e)
        {
            SelectAndShowCalibrationImage(RearOriginPictureBox, 1);
        }

        private void SelectAndShowCalibrationImage(PictureBox target, int FrontRear)
        {

            using var dlg = new OpenFileDialog
            {
                Filter = "이미지 파일|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif;*.tiff|모든 파일|*.*",
                Title = "이미지 선택"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var newImage = Image.FromFile(dlg.FileName);
                if (FrontRear == 0)
                {
                    CalFrontOriginImgPath = dlg.FileName;
                }
                else if (FrontRear == 1)
                {
                    CalRearOriginImgPath = dlg.FileName;
                }
                var old = target.Image;
                target.Image = newImage;
                old?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지를 불러올 수 없습니다.\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }





        private void CaliBtr_Click(object sender, EventArgs e)
        {

            //이후에는 이게 AC/DC인지 구분하고 // 레시피별로 구분해야됨
            Bitmap DC_bitmap = _CV.DC_GearGridWarpPerspective(CalRearOriginImgPath);
            Bitmap AC_bitmap = _CV.AC_GearGridWarpPerspective(CalFrontOriginImgPath);

            if (!(null == DC_bitmap))
            {
                RearCalResult.SizeMode = PictureBoxSizeMode.StretchImage;
                RearCalResult.Image = DC_bitmap;
            }

            if (!(null == AC_bitmap))
            {
                FrontCalResult.SizeMode = PictureBoxSizeMode.StretchImage;
                FrontCalResult.Image = AC_bitmap;
            }
        }


        private void makeResultOutput(string path)
        {

            string outputPath = "\\ResultOutput.csv";

            // 데이터를 저장할 리스트 (string 배열의 리스트)
            List<string[]> allData = new List<string[]>();

            try
            {
                // 2. 디렉토리 내의 모든 .csv 파일 가져오기
                string[] files = Directory.GetFiles(path, "*.csv");

                foreach (string file in files)
                {
                    // 파일의 모든 줄을 읽어옴
                    var lines = File.ReadAllLines(file);


                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // 쉼표로 분리하여 배열로 저장
                        string[] row = line.Split(',');
                        allData.Add(row);
                    }
                    //읽은 파일 삭제
                    File.Delete(file);
                }

                // 3. 첫 번째 열(COUNT)을 기준으로 오름차순 정렬
                // int.Parse를 통해 문자열을 숫자로 변환하여 비교합니다.
                var sortedData = allData
                    .OrderBy(row => int.Parse(row[0].Trim()))
                    .ToList();


                // 4. 결과를 ResultOutput.csv에 저장
                List<string> outputLines = sortedData
                    .Select(row => string.Join(",", row))
                    .ToList();


                File.WriteAllLines(path + outputPath, outputLines);

                //Console.WriteLine($"작업 완료! 총 {sortedData.Count}개의 행이 {outputPath}에 저장되었습니다.");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"오류 발생: {ex.Message}");
            }

        }

        private void PluerPageDownbtr_Click(object sender, EventArgs e)
        {
            PerulStaticPanelCount--;
            if (PerulStaticPanelCount <= 1)
            {
                PerulStaticPanelCount = 1;
            }

            PerulStaticPanelUpdate();
        }

        private void PluerPageUpbtr_Click(object sender, EventArgs e)
        {
            PerulStaticPanelCount++;
            if (PerulStaticPanelCount >= 12)
            {
                PerulStaticPanelCount = 11;
            }

            PerulStaticPanelUpdate();

        }

        public void PerulStaticPanelUpdate()
        {
            if (PerulStaticPanelCount == 1)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 가중치 반영 최종 점수";
                PerulStaticPanel_1.BringToFront();

            }
            else if (PerulStaticPanelCount == 2)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 평균편차(MAD)";
                PerulStaticPanel_2.BringToFront();
            }
            else if (PerulStaticPanelCount == 3)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 가속 점수 PeakX,Y /AreaX,Y";
                PerulStaticPanel_3.BringToFront();
            }
            else if (PerulStaticPanelCount == 4)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 가속 점수 Length/Hegight/Area/Distance";
                PerulStaticPanel_4.BringToFront();
            }
            else if (PerulStaticPanelCount == 5)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 가속 평균 편차(MAD) PeakX,Y /AreaX,Y";
                PerulStaticPanel_5.BringToFront();
            }
            else if (PerulStaticPanelCount == 6)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 가속 평균 편차(MAD) Length/Hegight/Area/Distance";
                PerulStaticPanel_6.BringToFront();
            }
            else if (PerulStaticPanelCount == 7)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 감속 점수 PeakX,Y /AreaX,Y";
                PerulStaticPanel_7.BringToFront();
            }
            else if (PerulStaticPanelCount == 8)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 감속 점수 Length/Hegight/Area/Distance";
                PerulStaticPanel_8.BringToFront();
            }
            else if (PerulStaticPanelCount == 9)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 감속 평균 편차(MAD) PeakX,Y /AreaX,Y";
                PerulStaticPanel_9.BringToFront();
            }
            else if (PerulStaticPanelCount == 10)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 감속 평균 편차(MAD) Length/Hegight/Area/Distance";
                PerulStaticPanel_10.BringToFront();
            }
            else if (PerulStaticPanelCount == 11)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 등급 통계";
                PerulStaticPanel_11.BringToFront();
            }
            else
            {
                return;
            }
        }

        private void SettingPerulStaticPanel()
        {
            PerulStaticPanelCount = 1;
            PerulStaticPanel.Location = new Point(10, 90);
            PerulStaticPanel.Size = new Size(1700, 810);
            PerulStaticPanel.Visible = true;
            ListPanel.Visible = false;
            ListLabel.ForeColor = Color.White;
            ListLabel.BackColor = Color.FromArgb(64, 64, 64);
            PluralStaticLabel.ForeColor = Color.Black;
            PluralStaticLabel.BackColor = Color.White;
            PerulStaticPanel_1.Visible = true;
            PerulStaticPanel_1.Location = new Point(3, 61);
            PerulStaticPanel_1.Size = new Size(1676, 700);
            PerulStaticDisplayLabel.Text = "복수 통계 : 가중치 반영 최종 점수";
            PerulStaticPanel_2.Location = new Point(3, 61);
            PerulStaticPanel_2.Size = new Size(1676, 700);
            PerulStaticPanel_2.Visible = true;

            PerulStaticPanel_3.Location = new Point(3, 61);
            PerulStaticPanel_3.Size = new Size(1676, 700);
            PerulStaticPanel_3.Visible = true;


            PerulStaticPanel_4.Location = new Point(3, 61);
            PerulStaticPanel_4.Size = new Size(1676, 700);
            PerulStaticPanel_4.Visible = true;


            PerulStaticPanel_5.Location = new Point(3, 61);
            PerulStaticPanel_5.Size = new Size(1676, 700);
            PerulStaticPanel_5.Visible = true;

            PerulStaticPanel_6.Location = new Point(3, 61);
            PerulStaticPanel_6.Size = new Size(1676, 700);
            PerulStaticPanel_6.Visible = true;

            PerulStaticPanel_7.Location = new Point(3, 61);
            PerulStaticPanel_7.Size = new Size(1676, 700);
            PerulStaticPanel_7.Visible = true;

            PerulStaticPanel_8.Location = new Point(3, 61);
            PerulStaticPanel_8.Size = new Size(1676, 700);
            PerulStaticPanel_8.Visible = true;

            PerulStaticPanel_9.Location = new Point(3, 61);
            PerulStaticPanel_9.Size = new Size(1676, 700);
            PerulStaticPanel_9.Visible = true;

            PerulStaticPanel_10.Location = new Point(3, 61);
            PerulStaticPanel_10.Size = new Size(1676, 700);
            PerulStaticPanel_10.Visible = true;

            PerulStaticPanel_11.Location = new Point(3, 61);
            PerulStaticPanel_11.Size = new Size(1676, 700);
            PerulStaticPanel_11.Visible = true;




            PerulStaticPanel_9.Location = new Point(3, 61);
            PerulStaticPanel_9.Size = new Size(1676, 700);
            PerulStaticPanel_9.Visible = true;

            PerulStaticPanel_10.Location = new Point(3, 61);
            PerulStaticPanel_10.Size = new Size(1676, 700);
            PerulStaticPanel_10.Visible = true;

            PerulStaticPanel_11.Location = new Point(3, 61);
            PerulStaticPanel_11.Size = new Size(1676, 700);
            PerulStaticPanel_11.Visible = true;




            PerulStaticPanel_1.BringToFront();
        }

        private void LockButton_Click(object sender, EventArgs e)
        {
            if (!_LoginManager.BoolLoginCheck)
            {
                ShowLoginRequiredFocusLogin();
                return;
            }

            UnlockPasswordtb.Text = "";

            CurrentUserLabel.Text = "현재 사용자 : " + _LoginManager.UserInputID;
            LockPanel.Size = new Size(1920, 1080);
            LockPanel.Location = new Point(0, 0);
            LockPanel.Visible = true;
            LockPanel.BringToFront();
        }

        private void button1_Click(object sender, EventArgs e)
        {


            var result = MessageBox.Show(
                   this,
                   "프로그램을 종료하시겠습니까?",
                   "종료 확인",
                   MessageBoxButtons.OKCancel,
                   MessageBoxIcon.Question
               );

            if (result == DialogResult.OK)
            {
                Application.Exit();
            }

            return;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {


            if (_LoginManager.UnlockCheck(UnlockPasswordtb.Text.ToString()))
            {
                var result = MessageBox.Show(
                 "잠금 해제 완료되었습니다.",
                 "잠금 해제 완료",
                 MessageBoxButtons.OK,
                 MessageBoxIcon.Question);

                LockPanel.Visible = false;
            }
            else
            {
                var result = MessageBox.Show(
                "비밀 번호 오류",
                "잠금 해제 실패",
                MessageBoxButtons.OK,
                MessageBoxIcon.Question);
                return;
            }
        }

        private void PlurStaticCSVSaveBtr_Click(object sender, EventArgs e)
        {
            //리스트에서 v표시된 것들의 자료 가져오기
            var selectedRowEntries = CollectSelectedListRowEntriesOrderedByRow();
            if (selectedRowEntries.Count < 2)
            {
                MessageBox.Show(
                    this,
                    "선택된 시행 행이 부족합니다. 검색 후 V 표시를 두 개 이상 해 주세요.",
                    "선택 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string selectedPath = "";
            //저장 경로 설정
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                // 초기 설명 문구 설정
                fbd.Description = "데이터를 저장할 폴더를 선택하세요.";

                // 새 폴더 만들기 버튼 표시 여부
                fbd.ShowNewFolderButton = true;

                // 사용자가 '확인'을 눌렀을 때만 실행
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    // 선택된 경로를 변수에 저장
                    selectedPath = fbd.SelectedPath + @"\";

                    if (!TryPromptPluralSaveBaseName(out string baseName))
                    {
                        return;
                    }

                    string scoresPath = Path.Combine(selectedPath, $"{baseName}_Scores.csv");
                    string madPath = Path.Combine(selectedPath, $"{baseName}_MAD.csv");

                    if (File.Exists(scoresPath) || File.Exists(madPath))
                    {
                        var overwrite = MessageBox.Show(
                            this,
                            $"동일한 이름의 파일이 존재합니다.{Environment.NewLine}" +
                            $"{scoresPath}{Environment.NewLine}{madPath}{Environment.NewLine}{Environment.NewLine}" +
                            "덮어쓰시겠습니까?",
                            "동일 파일 존재",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        if (overwrite != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                    //------------------------------복수 S/NO 배면 런아웃 저장 변수-------------------------------
                    string[] SNO = new string[selectedRowEntries.Count];
                    double[] SensorROUT = new double[selectedRowEntries.Count];

                    //------------------------------복수 SCORE 저장-------------------------------
                    float[] ACPeakXScores = new float[selectedRowEntries.Count];
                    float[] ACPeakYScores = new float[selectedRowEntries.Count];
                    float[] ACWidthScores = new float[selectedRowEntries.Count];
                    float[] ACHeightScores = new float[selectedRowEntries.Count];
                    float[] ACAreaScores = new float[selectedRowEntries.Count];
                    float[] ACAreaXScores = new float[selectedRowEntries.Count];
                    float[] ACAreaYScores = new float[selectedRowEntries.Count];
                    float[] ACDistanceScores = new float[selectedRowEntries.Count];
                    float[] DCPeakXScores = new float[selectedRowEntries.Count];
                    float[] DCPeakYScores = new float[selectedRowEntries.Count];
                    float[] DCWidthScores = new float[selectedRowEntries.Count];
                    float[] DCHeightScores = new float[selectedRowEntries.Count];
                    float[] DCAreaScores = new float[selectedRowEntries.Count];
                    float[] DCAreaXScores = new float[selectedRowEntries.Count];
                    float[] DCAreaYScores = new float[selectedRowEntries.Count];
                    float[] DCDistanceScores = new float[selectedRowEntries.Count];


                    //--------------------------------복수 MAD 저장 하기----------------------------------------------
                    float[] ACPeakXMAD = new float[selectedRowEntries.Count];
                    float[] ACPeakYMAD = new float[selectedRowEntries.Count];
                    float[] ACWidthMAD = new float[selectedRowEntries.Count];
                    float[] ACHeightMAD = new float[selectedRowEntries.Count];
                    float[] ACAreaMAD = new float[selectedRowEntries.Count];
                    float[] ACAreaXMAD = new float[selectedRowEntries.Count];
                    float[] ACAreaYMAD = new float[selectedRowEntries.Count];
                    float[] ACDistanceMAD = new float[selectedRowEntries.Count];
                    float[] DCPeakXMAD = new float[selectedRowEntries.Count];
                    float[] DCPeakYMAD = new float[selectedRowEntries.Count];
                    float[] DCWidthMAD = new float[selectedRowEntries.Count];
                    float[] DCHeightMAD = new float[selectedRowEntries.Count];
                    float[] DCAreaMAD = new float[selectedRowEntries.Count];
                    float[] DCAreaXMAD = new float[selectedRowEntries.Count];
                    float[] DCAreaYMAD = new float[selectedRowEntries.Count];
                    float[] DCDistanceMAD = new float[selectedRowEntries.Count];

                    int count = 0;

                    // 결과 파일을 싹 불러와서 위의 데이터를 만들고 나서 저장
                    foreach (var rowEntry in selectedRowEntries)
                    {
                        var accelDir = Path.Combine(rowEntry.TrialFolderPath, "Acceleration");
                        var decelDir = Path.Combine(rowEntry.TrialFolderPath, "Deceleration");
                        string ACPath = Path.Combine(accelDir, "ScoreGrade.csv");
                        string DCPath = Path.Combine(decelDir, "ScoreGrade.csv");
                        //sno 정보 저장 
                        SNO[count] = rowEntry.BcrFolderName;
                        // 배면 런아웃 정보 
                        string SensingDataPath = Path.Combine(rowEntry.TrialFolderPath, "SensorData.csv");
                        double SensingRunOut = 0.0;
                        try
                        {
                            List<double> secondColumn = new List<double>();
                            // FileShare.ReadWrite 권한을 주어 파일 잠금 충돌을 방지합니다.
                            using (FileStream fs = new FileStream(SensingDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                            {
                                while (!sr.EndOfStream)
                                {
                                    string line = sr.ReadLine();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        // 쉼표로 분리하여 리스트에 저장
                                        secondColumn.Add(double.Parse(line.Split(',')[1]));
                                    }
                                }
                            }

                            SensingRunOut = secondColumn.Max() - secondColumn.Min();

                            SensorROUT[count] = SensingRunOut;
                        }
                        catch (Exception ex)
                        {
                            //
                            SensingRunOut = 0.0;
                            Logger.LogError("CSV-배면 런아웃 CSV에러", $"파일 읽기 실패: {ex.Message}");
                        }

                        string[] lines = File.ReadAllLines(ACPath);
                        if (lines.Length > 2)
                        {
                            string[] values = lines[2].Split(",");
                            string PeakX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            ACPeakXScores[count] = float.Parse(PeakX_Score);

                            values = lines[4].Split(",");
                            string PeakY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            ACPeakYScores[count] = float.Parse(PeakY_Score);

                            values = lines[6].Split(",");
                            string AreaX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            ACAreaXScores[count] = float.Parse(AreaX_Score);

                            values = lines[8].Split(",");
                            string AreaY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            ACAreaYScores[count] = float.Parse(AreaY_Score);


                            values = lines[10].Split(",");
                            string Width = (double.Parse(values[6]) * 0.2).ToString("f1");
                            ACWidthScores[count] = float.Parse(Width);

                            values = lines[12].Split(",");
                            string Height = (double.Parse(values[6]) * 0.1).ToString("f1");
                            ACHeightScores[count] = float.Parse(Height);

                            values = lines[14].Split(",");
                            string Area = (double.Parse(values[6]) * 0.1).ToString("f1");
                            ACAreaScores[count] = float.Parse(Area);

                            values = lines[16].Split(",");
                            string Distance = (double.Parse(values[6]) * 0.1).ToString("f1");
                            ACDistanceScores[count] = float.Parse(Distance);



                            string[] ACLines = File.ReadAllLines(Path.Combine(accelDir, "ResultOutput.csv"));
                            float[] FSinglePeakX = new float[ACLines.Length];
                            float[] FSinglePeakY = new float[ACLines.Length];
                            float[] FSingleAreaX = new float[ACLines.Length];
                            float[] FSingleAreaY = new float[ACLines.Length];
                            float[] FSingleWidth = new float[ACLines.Length];
                            float[] FSingleHeight = new float[ACLines.Length];
                            float[] FSingleArea = new float[ACLines.Length];
                            float[] FSingleDistance = new float[ACLines.Length];
                            int ACcount = 0;
                            try
                            {
                                // 한 줄씩 읽어오기
                                foreach (string line in ACLines)
                                {
                                    // 쉼표로 분리하여 배열에 담기
                                    string[] va = line.Split(',');
                                    if (va.Length < 9)
                                    {
                                        Logger.LogWarning("FileIO", "Acceleration CSV 포맷 이상 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | line={line}");
                                        break;
                                    }
                                    if (!TryParseCsvFloat(va[1], out FSinglePeakX[ACcount])
                                        || !TryParseCsvFloat(va[2], out FSinglePeakY[ACcount])
                                        || !TryParseCsvFloat(va[3], out FSingleAreaX[ACcount])
                                        || !TryParseCsvFloat(va[4], out FSingleAreaY[ACcount])
                                        || !TryParseCsvFloat(va[5], out FSingleWidth[ACcount])
                                        || !TryParseCsvFloat(va[6], out FSingleHeight[ACcount])
                                        || !TryParseCsvFloat(va[7], out FSingleArea[ACcount])
                                        || !TryParseCsvFloat(va[8], out FSingleDistance[ACcount])

                                       )
                                    {
                                        Logger.LogWarning("FileIO", "Acceleration CSV 숫자 파싱 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | line={line}");
                                        break;
                                    }
                                    ACcount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("FileIO", "Acceleration CSV 읽기 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | {ex}");
                                MessageBox.Show(this, "Acceleration CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.", "CSV 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }


                            double SinglePeakXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSinglePeakX.Average())).Average();
                            double SinglePeakYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSinglePeakY.Average())).Average();

                            double SingleAreaXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSingleAreaX.Average())).Average();
                            double SingleAreaYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSingleAreaY.Average())).Average();

                            double SingleWidthYMAD = FSingleWidth.Select(num => Math.Abs(num - FSingleWidth.Average())).Average();
                            double SingleHeightMAD = FSingleHeight.Select(num => Math.Abs(num - FSingleHeight.Average())).Average();
                            double SingleAreaMAD = FSingleArea.Select(num => Math.Abs(num - FSingleArea.Average())).Average();

                            double SingleDistanceMAD = FSinglePeakY.Select(num => Math.Abs(num - FSingleDistance.Average())).Average();

                            ACPeakXMAD[count] = (float)SinglePeakXMAD;
                            ACPeakYMAD[count] = (float)SinglePeakYMAD;
                            ACAreaXMAD[count] = (float)SingleAreaXMAD;
                            ACAreaYMAD[count] = (float)SingleAreaYMAD;
                            ACWidthMAD[count] = (float)SingleWidthYMAD;
                            ACHeightMAD[count] = (float)SingleHeightMAD;
                            ACAreaMAD[count] = (float)SingleAreaMAD;
                            ACDistanceMAD[count] = (float)SingleDistanceMAD;
                        }

                        lines = File.ReadAllLines(DCPath);
                        if (lines.Length > 2)
                        {
                            string[] values = lines[2].Split(",");
                            string PeakX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            DCPeakXScores[count] = float.Parse(PeakX_Score);

                            values = lines[4].Split(",");
                            string PeakY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            DCPeakYScores[count] = float.Parse(PeakY_Score);

                            values = lines[6].Split(",");
                            string AreaX_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            DCAreaXScores[count] = float.Parse(AreaX_Score);

                            values = lines[8].Split(",");
                            string AreaY_Score = (double.Parse(values[6]) * 0.3).ToString("f1");
                            DCAreaYScores[count] = float.Parse(AreaY_Score);


                            values = lines[10].Split(",");
                            string Width = (double.Parse(values[6]) * 0.2).ToString("f1");
                            DCWidthScores[count] = float.Parse(Width);

                            values = lines[12].Split(",");
                            string Height = (double.Parse(values[6]) * 0.1).ToString("f1");
                            DCHeightScores[count] = float.Parse(Height);

                            values = lines[14].Split(",");
                            string Area = (double.Parse(values[6]) * 0.1).ToString("f1");
                            DCAreaScores[count] = float.Parse(Area);

                            values = lines[16].Split(",");
                            string Distance = (double.Parse(values[6]) * 0.1).ToString("f1");
                            DCDistanceScores[count] = float.Parse(Distance);



                            string[] DCLines = File.ReadAllLines(Path.Combine(decelDir, "ResultOutput.csv"));
                            float[] FSinglePeakX = new float[DCLines.Length];
                            float[] FSinglePeakY = new float[DCLines.Length];
                            float[] FSingleAreaX = new float[DCLines.Length];
                            float[] FSingleAreaY = new float[DCLines.Length];
                            float[] FSingleWidth = new float[DCLines.Length];
                            float[] FSingleHeight = new float[DCLines.Length];
                            float[] FSingleArea = new float[DCLines.Length];
                            float[] FSingleDistance = new float[DCLines.Length];
                            int DCcount = 0;
                            try
                            {
                                // 한 줄씩 읽어오기
                                foreach (string line in DCLines)
                                {
                                    // 쉼표로 분리하여 배열에 담기
                                    string[] va = line.Split(',');
                                    if (va.Length < 9)
                                    {
                                        Logger.LogWarning("FileIO", "DCceleration CSV 포맷 이상 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(decelDir, "ResultOutput.csv")} | line={line}");
                                        break;
                                    }
                                    if (!TryParseCsvFloat(va[1], out FSinglePeakX[DCcount])
                                        || !TryParseCsvFloat(va[2], out FSinglePeakY[DCcount])
                                        || !TryParseCsvFloat(va[3], out FSingleAreaX[DCcount])
                                        || !TryParseCsvFloat(va[4], out FSingleAreaY[DCcount])
                                        || !TryParseCsvFloat(va[5], out FSingleWidth[DCcount])
                                        || !TryParseCsvFloat(va[6], out FSingleHeight[DCcount])
                                        || !TryParseCsvFloat(va[7], out FSingleArea[DCcount])
                                        || !TryParseCsvFloat(va[8], out FSingleDistance[DCcount])

                                       )
                                    {
                                        Logger.LogWarning("FileIO", "DCceleration CSV 숫자 파싱 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(decelDir, "ResultOutput.csv")} | line={line}");
                                        break;
                                    }
                                    DCcount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("FileIO", "DCceleration CSV 읽기 실패 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(decelDir, "ResultOutput.csv")} | {ex}");
                                MessageBox.Show(this, "DCceleration CSV 파일을 읽는 중 오류가 발생했습니다.\n로그를 확인해 주세요.", "CSV 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }


                            double SinglePeakXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSinglePeakX.Average())).Average();
                            double SinglePeakYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSinglePeakY.Average())).Average();

                            double SingleAreaXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSingleAreaX.Average())).Average();
                            double SingleAreaYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSingleAreaY.Average())).Average();

                            double SingleWidthYMAD = FSingleWidth.Select(num => Math.Abs(num - FSingleWidth.Average())).Average();
                            double SingleHeightMAD = FSingleHeight.Select(num => Math.Abs(num - FSingleHeight.Average())).Average();
                            double SingleAreaMAD = FSingleArea.Select(num => Math.Abs(num - FSingleArea.Average())).Average();

                            double SingleDistanceMAD = FSinglePeakY.Select(num => Math.Abs(num - FSingleDistance.Average())).Average();

                            DCPeakXMAD[count] = (float)SinglePeakXMAD;
                            DCPeakYMAD[count] = (float)SinglePeakYMAD;
                            DCAreaXMAD[count] = (float)SingleAreaXMAD;
                            DCAreaYMAD[count] = (float)SingleAreaYMAD;
                            DCWidthMAD[count] = (float)SingleWidthYMAD;
                            DCHeightMAD[count] = (float)SingleHeightMAD;
                            DCAreaMAD[count] = (float)SingleAreaMAD;
                            DCDistanceMAD[count] = (float)SingleDistanceMAD;
                        }
                        count++;
                    }
                    // 저장 해야함
                    string head = "순서,S/NO,배면R/OUT,가속_PeakX,가속_PeakY,가속_AreaX,가속_AreaY,가속_Distance,가속_Length,가속_Height,가속_Area,감속_PeakX,감속_PeakY,감속_AreaX,감속_AreaY,감속_Distance,감속_Length,감속_Height,감속_Area";

                    try
                    {
                        using (StreamWriter sw = new StreamWriter(scoresPath, false, Encoding.UTF8))
                        {
                            sw.WriteLine(head);

                            for (int i = 0; i < selectedRowEntries.Count; i++)
                            {
                                sw.WriteLine($"{i + 1},{SNO[i]},{SensorROUT[i]},{ACPeakXScores[i]},{ACPeakYScores[i]},{ACAreaXScores[i]},{ACAreaYScores[i]},{ACDistanceScores[i]},{ACWidthScores[i]},{ACHeightScores[i]},{ACAreaScores[i]}," +
                                    $"{DCPeakXScores[i]},{DCPeakYScores[i]},{DCAreaXScores[i]},{DCAreaYScores[i]},{DCDistanceScores[i]},{DCWidthScores[i]},{DCHeightScores[i]},{DCAreaScores[i]}");
                            }
                        }

                        Logger.LogInfo("CSV", $"복수 통계 Scores 파일 생성 완료.  \n파일 경로 :{scoresPath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfo("CSV", $"복수 통계 Scores 파일 생성 실패.  \n파일 경로 :{scoresPath}\n 오류내용 : {ex.Message}");
                    }

                    try
                    {
                        using (StreamWriter sw = new StreamWriter(madPath, false, Encoding.UTF8))
                        {
                            sw.WriteLine(head);

                            for (int i = 0; i < selectedRowEntries.Count; i++)
                            {
                                sw.WriteLine($"{i + 1},{SNO[i]},{SensorROUT[i]},{ACPeakXMAD[i]},{ACPeakYMAD[i]},{ACAreaXMAD[i]},{ACAreaYMAD[i]},{ACDistanceMAD[i]},{ACWidthMAD[i]},{ACHeightMAD[i]},{ACAreaMAD[i]}," +
                                    $"{DCPeakXMAD[i]},{DCPeakYMAD[i]},{DCAreaXMAD[i]},{DCAreaYMAD[i]},{DCDistanceMAD[i]},{DCWidthMAD[i]},{DCHeightMAD[i]},{DCAreaMAD[i]}");
                            }
                        }

                        Logger.LogInfo("CSV", $"복수 통계 MAD 파일 생성 완료.  \n파일 경로 :{madPath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfo("CSV", $"복수 통계 MAD 파일 생성 실패.  \n파일 경로 :{madPath}\n 오류내용 : {ex.Message}");
                    }
                }
            }


        }

        /// <summary>복수 통계 저장용 기본 이름 입력. 결과는 {base}_Scores.csv / {base}_MAD.csv.</summary>
        private bool TryPromptPluralSaveBaseName(out string baseName)
        {
            baseName = "Plur";

            using var prompt = new Form
            {
                Text = "복수 통계 저장 이름",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(420, 140),
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            var label = new Label
            {
                Text = "기본 이름을 입력하세요. (예: Plur → Plur_Scores.csv, Plur_MAD.csv)",
                AutoSize = false,
                Location = new Point(16, 16),
                Size = new Size(388, 40)
            };
            var textBox = new TextBox
            {
                Text = "Plur",
                Location = new Point(16, 60),
                Size = new Size(388, 27)
            };
            var okButton = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(228, 100),
                Size = new Size(85, 28)
            };
            var cancelButton = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(319, 100),
                Size = new Size(85, 28)
            };

            prompt.Controls.Add(label);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(okButton);
            prompt.Controls.Add(cancelButton);
            prompt.AcceptButton = okButton;
            prompt.CancelButton = cancelButton;

            if (prompt.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            var raw = (textBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show(
                    this,
                    "기본 이름을 입력해 주세요.",
                    "이름 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                if (raw.Contains(c))
                {
                    MessageBox.Show(
                        this,
                        "파일 이름에 사용할 수 없는 문자가 포함되어 있습니다.",
                        "이름 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }

            // 사용자가 이미 접미사를 넣은 경우 정리
            if (raw.EndsWith("_Scores", StringComparison.OrdinalIgnoreCase) ||
                raw.EndsWith("_MAD", StringComparison.OrdinalIgnoreCase))
            {
                int us = raw.LastIndexOf('_');
                if (us > 0)
                {
                    raw = raw[..us];
                }
            }
            if (raw.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw[..^4];
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show(
                    this,
                    "기본 이름을 입력해 주세요.",
                    "이름 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            baseName = raw;
            return true;
        }

        private void SingleStaticSavebtr_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "단일 통계 저장";
                sfd.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
                sfd.FilterIndex = 1;
                sfd.DefaultExt = "csv";
                sfd.AddExtension = true;
                sfd.FileName = "SingleStatic.csv";
                sfd.OverwritePrompt = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string saveFilePath = sfd.FileName;
                    var rowEntry = _listRowEntries[SingleStaticSavePoint]; // simgle static 버튼이 눌릴ㄸ대만 반영됨

                    //리스트에서 v표시된 것들의 자료 가져오기
                    var selectedRowEntries = CollectSelectedListRowEntriesOrderedByRow();
                    FrontPath = Path.Combine(rowEntry.TrialFolderPath, "Acceleration");
                    RearPath = Path.Combine(rowEntry.TrialFolderPath, "Deceleration");
                    string SensingDatacsvpath = Path.Combine(rowEntry.TrialFolderPath, "SensorData.csv");
                    string ACDatacsvpath = Path.Combine(FrontPath, "ResultOutput.csv");
                    string DCDatacsvpath = Path.Combine(RearPath, "ResultOutput.csv");
                    string ACScorecsvpath = Path.Combine(FrontPath, "ScoreGrade.csv");
                    string DCScorecsvpath = Path.Combine(RearPath, "ScoreGrade.csv");

                    //AC데이터 읽어서 저장


                    //모든 라인을 우선 다읽어와서 저장 후 하나씩 처리
                    string[] ACData = null;
                    string[] ACScore = null;
                    string[] DCData = null;
                    string[] DCScore = null;
                    string[] SensingData = null;

                    try
                    {
                        ACData = File.ReadAllLines(ACDatacsvpath);
                        ACScore = File.ReadAllLines(ACScorecsvpath);
                        DCData = File.ReadAllLines(DCDatacsvpath);
                        DCScore = File.ReadAllLines(DCScorecsvpath);
                        SensingData = File.ReadAllLines(SensingDatacsvpath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("FileIO", $"파일을 읽어오는데 에러가 발생했습니다 ex : {ex}");
                    }

                    //DATA부터 읽어와서 저장
                    float[] DCSinglePeakX = new float[DCData.Length];
                    float[] DCSinglePeakY = new float[DCData.Length];
                    float[] DCSingleAreaX = new float[DCData.Length];
                    float[] DCSingleAreaY = new float[DCData.Length];
                    float[] DCSingleWidth = new float[DCData.Length];
                    float[] DCSingleHeight = new float[DCData.Length];
                    float[] DCSingleArea = new float[DCData.Length];
                    float[] DCSDistance = new float[DCData.Length];

                    float[] ACSinglePeakX = new float[ACData.Length];
                    float[] ACSinglePeakY = new float[ACData.Length];
                    float[] ACSingleAreaX = new float[ACData.Length];
                    float[] ACSingleAreaY = new float[ACData.Length];
                    float[] ACSingleWidth = new float[ACData.Length];
                    float[] ACSingleHeight = new float[ACData.Length];
                    float[] ACSingleArea = new float[ACData.Length];
                    float[] ACSDistance = new float[ACData.Length];
                    int count = 0;
                    foreach (string value in ACData)
                    {
                        string[] DATA = value.Split(',');
                        ACSinglePeakX[count] = float.Parse(DATA[1]);
                        ACSinglePeakY[count] = float.Parse(DATA[2]);
                        ACSingleAreaX[count] = float.Parse(DATA[3]);
                        ACSingleAreaY[count] = float.Parse(DATA[4]);
                        ACSingleWidth[count] = float.Parse(DATA[5]);
                        ACSingleHeight[count] = float.Parse(DATA[6]);
                        ACSingleArea[count] = float.Parse(DATA[7]);
                        ACSDistance[count] = float.Parse(DATA[8]);
                        count++;
                    }

                    count = 0;
                    foreach (string value in DCData)
                    {
                        string[] DATA = value.Split(',');
                        DCSinglePeakX[count] = float.Parse(DATA[1]);
                        DCSinglePeakY[count] = float.Parse(DATA[2]);
                        DCSingleAreaX[count] = float.Parse(DATA[3]);
                        DCSingleAreaY[count] = float.Parse(DATA[4]);
                        DCSingleWidth[count] = float.Parse(DATA[5]);
                        DCSingleHeight[count] = float.Parse(DATA[6]);
                        DCSingleArea[count] = float.Parse(DATA[7]);
                        DCSDistance[count] = float.Parse(DATA[8]);
                        count++;
                    }
                    //SCORE 및 그레이드 저장                   
                    string[] values = ACScore[1].Split(",");
                    double ACPeakXMax_AVG = double.Parse(values[1]);//단일치
                    double ACPeakXMaxInterval = double.Parse(values[2]);//인접치
                    double ACPeakXRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACPeakXROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[3].Split(",");
                    double ACPeaYMax_AVG = double.Parse(values[1]);//단일치
                    double ACPeaYMaxInterval = double.Parse(values[2]);//인접치
                    double ACPeaYRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACPeaYROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[5].Split(",");
                    double ACAreaXMax_AVG = double.Parse(values[1]);//단일치
                    double ACAreaXMaxInterval = double.Parse(values[2]);//인접치
                    double ACAreaXRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACAreaXROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[7].Split(",");
                    double ACAreaYMax_AVG = double.Parse(values[1]);//단일치
                    double ACAreaYMaxInterval = double.Parse(values[2]);//인접치
                    double ACAreaYRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACAreaYROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[9].Split(",");
                    double ACWidthMax_AVG = double.Parse(values[1]);//단일치
                    double ACWidthMaxInterval = double.Parse(values[2]);//인접치
                    double ACWidthRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACWidthROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[11].Split(",");
                    double ACHeightMax_AVG = double.Parse(values[1]);//단일치
                    double ACHeightMaxInterval = double.Parse(values[2]);//인접치
                    double ACHeightRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACHeightROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[13].Split(",");
                    double ACAreaMax_AVG = double.Parse(values[1]);//단일치
                    double ACAreaMaxInterval = double.Parse(values[2]);//인접치
                    double ACAreaRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACAreaROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = ACScore[15].Split(",");
                    double ACDistanceMax_AVG = double.Parse(values[1]);//단일치
                    double ACDistanceMaxInterval = double.Parse(values[2]);//인접치
                    double ACDistanceRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double ACDistanceROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    ///--------------------DC 스코어 -----------------------------------
                    double DCPeakXMax = (double)DCSinglePeakX.Max();
                    double DCPeakXMin = (double)DCSinglePeakX.Min();
                    double DCPeakXAvg = (double)DCSinglePeakX.Average();

                    values = DCScore[1].Split(",");
                    double DCPeakXMax_AVG = double.Parse(values[1]);//단일치
                    double DCPeakXMaxInterval = double.Parse(values[2]);//인접치
                    double DCPeakXRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCPeakXROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[3].Split(",");
                    double DCPeaYMax_AVG = double.Parse(values[1]);//단일치
                    double DCPeaYMaxInterval = double.Parse(values[2]);//인접치
                    double DCPeaYRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCPeaYROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[5].Split(",");
                    double DCAreaXMax_AVG = double.Parse(values[1]);//단일치
                    double DCAreaXMaxInterval = double.Parse(values[2]);//인접치
                    double DCAreaXRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCAreaXROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[7].Split(",");
                    double DCAreaYMax_AVG = double.Parse(values[1]);//단일치
                    double DCAreaYMaxInterval = double.Parse(values[2]);//인접치
                    double DCAreaYRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCAreaYROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[9].Split(",");
                    double DCWidthMax_AVG = double.Parse(values[1]);//단일치
                    double DCWidthMaxInterval = double.Parse(values[2]);//인접치
                    double DCWidthRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCWidthROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[11].Split(",");
                    double DCHeightMax_AVG = double.Parse(values[1]);//단일치
                    double DCHeightMaxInterval = double.Parse(values[2]);//인접치
                    double DCHeightRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCHeightROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[13].Split(",");
                    double DCAreaMax_AVG = double.Parse(values[1]);//단일치
                    double DCAreaMaxInterval = double.Parse(values[2]);//인접치
                    double DCAreaRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCAreaROUT = double.Parse(values[4]);//누적치-루트민스퀘어

                    values = DCScore[15].Split(",");
                    double DCDistanceMax_AVG = double.Parse(values[1]);//단일치
                    double DCDistanceMaxInterval = double.Parse(values[2]);//인접치
                    double DCDistanceRMS = double.Parse(values[3]);//누적치-루트민스퀘어
                    double DCDistanceROUT = double.Parse(values[4]);//누적치-루트민스퀘어
                    //배면 런아웃 계산
                    //
                    double[] sensings = new double[SensingData.Length];
                    count = 0; ;
                    foreach (var value in SensingData)
                    {
                        string[] line = value.Split(",");
                        sensings[count] = double.Parse(line[1]);
                        count++;
                    }
                    double SensingROUT = sensings.Max() - sensings.Min();
                    //---------------------------저장---------------------------------

                    try
                    {
                        using (StreamWriter sw = new StreamWriter(saveFilePath, false, Encoding.UTF8))
                        {
                            //라벨 찾아서 데스트 일시 같이 저장
                            //$"ListDate{SingleStaticSavePoint+1}" //특정 라벨 찾아서 
                            Control[] foundControls = this.Controls.Find($"ListDate{SingleStaticSavePoint + 1}", true);

                            if (foundControls.Length > 0 && foundControls[0] is Label)
                            {
                                Label myLabel = (Label)foundControls[0];
                                sw.WriteLine($"Test일시,{myLabel.Text}");
                            }
                            else
                            {
                                sw.WriteLine($"Test일시,-");
                            }

                            sw.WriteLine($"S/NO,{rowEntry.BcrFolderName}");

                            sw.WriteLine($"배면런아웃,{SensingROUT}");

                            sw.WriteLine("구분,가속,가속,가속,가속,가속,가속,가속,가속,감속,감속,감속,감속,감속,감속,감속,감속");
                            sw.WriteLine("Touch NO.,PeakX,PeakY,AreaX,AreaY,거리차,길이,높이,면적,PeakX,PeakY,AreaX,AreaY,거리차,길이,높이,면적");

                            for (int i = 0; i < DCData.Length; i++)
                            {
                                sw.WriteLine($"{i + 1},{ACSinglePeakX[i]},{ACSinglePeakY[i]},{ACSingleAreaX[i]},{ACSingleAreaY[i]},{ACSDistance[i]},{ACSingleWidth[i]},{ACSingleHeight[i]},{ACSingleArea[i]}" +
                                    $",{DCSinglePeakX[i]},{DCSinglePeakY[i]},{DCSingleAreaX[i]},{DCSingleAreaY[i]},{DCSDistance[i]},{DCSingleWidth[i]},{DCSingleHeight[i]},{DCSingleArea[i]}");
                            }


                            sw.WriteLine($"최소값,{ACSinglePeakX.Min()},{ACSinglePeakY.Min()},{ACSingleAreaX.Min()},{ACSingleAreaY.Min()},{ACSDistance.Min()},{ACSingleWidth.Min()},{ACSingleHeight.Min()},{ACSingleArea.Min()}" +
                            $",{DCSinglePeakX.Min()},{DCSinglePeakY.Min()},{DCSingleAreaX.Min()},{DCSingleAreaY.Min()},{DCSDistance.Min()},{DCSingleWidth.Min()},{DCSingleHeight.Min()},{DCSingleArea.Min()}");
                            sw.WriteLine($"최대값,{ACSinglePeakX.Max()},{ACSinglePeakY.Max()},{ACSingleAreaX.Max()},{ACSingleAreaY.Max()},{ACSDistance.Max()},{ACSingleWidth.Max()},{ACSingleHeight.Max()},{ACSingleArea.Max()}" +
                            $",{DCSinglePeakX.Max()},{DCSinglePeakY.Max()},{DCSingleAreaX.Max()},{DCSingleAreaY.Max()},{DCSDistance.Max()},{DCSingleWidth.Max()},{DCSingleHeight.Max()},{DCSingleArea.Max()}");


                            sw.WriteLine($"평균값,{ACSinglePeakX.Average()},{ACSinglePeakY.Average()},{ACSingleAreaX.Average()},{ACSingleAreaY.Average()},{ACSDistance.Average()},{ACSingleWidth.Average()},{ACSingleHeight.Average()},{ACSingleArea.Average()}" +
                            $",{DCSinglePeakX.Average()},{DCSinglePeakY.Average()},{DCSingleAreaX.Average()},{DCSingleAreaY.Average()},{DCSDistance.Average()},{DCSingleWidth.Average()},{DCSingleHeight.Average()},{DCSingleArea.Average()}");


                            sw.WriteLine($"표준편차,{CalculateStandardDeviation(ACSinglePeakX, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSinglePeakY, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSingleAreaX, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSingleAreaY, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSDistance, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSingleWidth, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSingleHeight, isSample: false)}," +
                                $"{CalculateStandardDeviation(ACSingleArea, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSinglePeakX, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSinglePeakY, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSingleAreaX, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSingleAreaY, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSDistance, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSingleWidth, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSingleHeight, isSample: false)}," +
                                $"{CalculateStandardDeviation(DCSingleArea, isSample: false)}");

                            sw.WriteLine($"단일치,{ACPeakXMax_AVG},{ACPeaYMax_AVG},{ACAreaXMax_AVG},{ACAreaYMax_AVG},{ACWidthMax_AVG},{ACHeightMax_AVG},{ACAreaMax_AVG},{ACDistanceMax_AVG},{DCPeakXMax_AVG},{DCPeaYMax_AVG},{DCAreaXMax_AVG},{DCAreaYMax_AVG},{DCWidthMax_AVG},{DCHeightMax_AVG},{DCAreaMax_AVG},{DCDistanceMax_AVG}");
                            sw.WriteLine($"인접치,{ACPeakXMaxInterval},{ACPeaYMaxInterval},{ACAreaXMaxInterval},{ACAreaYMaxInterval},{ACWidthMaxInterval},{ACHeightMaxInterval},{ACAreaMaxInterval},{ACDistanceMaxInterval},{DCPeakXMaxInterval},{DCPeaYMaxInterval},{DCAreaXMaxInterval},{DCAreaYMaxInterval},{DCWidthMaxInterval},{DCHeightMaxInterval},{DCAreaMaxInterval},{DCDistanceMaxInterval}");
                            sw.WriteLine($"누적치,{ACPeakXRMS},{ACPeaYRMS},{ACAreaXRMS},{ACAreaYRMS},{ACWidthRMS},{ACHeightRMS},{ACAreaRMS},{ACDistanceRMS},{DCPeakXRMS},{DCPeaYRMS},{DCAreaXRMS},{DCAreaYRMS},{DCWidthRMS},{DCHeightRMS},{DCAreaRMS},{DCDistanceRMS}");
                            sw.WriteLine($"R/OUT,{ACPeakXROUT},{ACPeaYROUT},{ACAreaXROUT},{ACAreaYROUT},{ACWidthROUT},{ACHeightROUT},{ACAreaROUT},{ACDistanceROUT},{DCPeakXROUT},{DCPeaYROUT},{DCAreaXROUT},{DCAreaYROUT},{DCWidthROUT},{DCHeightROUT},{DCAreaROUT},{DCDistanceROUT}");

                        }


                        Logger.LogInfo("CSV", $"SingleStatic.csv 파일 생성 완료.  \n파일 경로 :{saveFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfo("CSV", $"SingleStatic.csv 파일 생성 실패.  \n파일 경로 :{saveFilePath}\n 오류내용 : {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// float 배열의 표준 편차를 계산합니다.
        /// </summary>
        /// <param name="data">계산할 float 배열</param>
        /// <param name="isSample">true일 경우 표본표준편차(N-1), false일 경우 모표준편차(N)</param>
        /// <returns>표준 편차 값 (float)</returns>
        public static float CalculateStandardDeviation(float[] data, bool isSample = false)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("배열이 비어있거나 null입니다.", nameof(data));
            }

            int length = data.Length;

            // 표본표준편차인 경우 데이터 개수가 1개 이하면 계산 불가 (ZeroDivision 방지)
            if (isSample && length <= 1)
            {
                return 0f;
            }

            // 1. 평균 계산
            double mean = data.Average();

            // 2. 편차 제곱의 합 계산 (정밀도를 위해 double 사용)
            double sumOfSquaresOfDifferences = data.Sum(val => Math.Pow(val - mean, 2));

            // 3. 분산 계산 (모집단은 N, 표본은 N-1)
            double divisor = isSample ? (length - 1) : length;
            double variance = sumOfSquaresOfDifferences / divisor;

            // 4. 제곱근(표준편차) 반환
            return (float)Math.Sqrt(variance);
        }


        private void ComentSavBtr_Click(object sender, EventArgs e)
        {

            //V 표시가 되어 있는 ROW들을 확인 
            //2개이상리면 1개만 선택하라고 진행

            var vCount = CountListSelectWithV(ListDisplyPanel);
            if (vCount == 1)
            {
                //v표시가 된 list의 열을 찾아야하네
                var selectedRowEntries = CollectSelectedListRowEntriesOrderedByRow();

                foreach (var rowEntry in selectedRowEntries)
                {
                    string commenttxtPath = Path.Combine(rowEntry.TrialFolderPath, ComenttxtFileName);


                    File.WriteAllText(commenttxtPath, ComentTextBox.Text);

                }

                MessageBox.Show(
                this,
                "메모가 정상적으로 저장 되었습니다.",
                "메모 저장 완료",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
                return;

            }
            else if (vCount == 0)
            {
                //1개 이상의 row를 선택해 달라는 메시지박스
                MessageBox.Show(
                this,
                "1개 이상의 열을 선택해 주세요.",
                "선택 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
                return;
            }

            else
            {
                //1개만 선택 해달라는 메세지 박스
                MessageBox.Show(
                this,
                "1개의 열만 선택해 주세요.",
                "선택 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
                return;
            }
        }

        private void Deletebtr_Click(object sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            var selectedRowEntries = CollectSelectedListRowEntriesOrderedByRow();
            if (selectedRowEntries.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "삭제할 열을 선택해 주세요.",
                    "선택 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var folderPaths = selectedRowEntries
                .Select(entry => entry.TrialFolderPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var pathPreview = string.Join(Environment.NewLine, folderPaths);
            var confirm = MessageBox.Show(
                this,
                $"아래 시행 폴더를 삭제하시겠습니까?{Environment.NewLine}{Environment.NewLine}{pathPreview}",
                "삭제 확인",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.OK)
            {
                return;
            }

            var deletedCount = 0;
            var failedPaths = new List<string>();

            foreach (var folderPath in folderPaths)
            {
                try
                {
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, recursive: true);
                        deletedCount++;
                        Logger.LogInfo("선택 내역 삭제", $"폴더 삭제 성공: {folderPath}");
                    }
                    else
                    {
                        Logger.LogWarning("선택 내역 삭제", $"폴더가 존재하지 않습니다: {folderPath}");
                        failedPaths.Add($"{folderPath} (없음)");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("선택 내역 삭제", $"폴더 삭제 실패: {folderPath} / {ex.Message}");
                    failedPaths.Add($"{folderPath} ({ex.Message})");
                }
            }

            // 삭제 후 목록 다시 스캔·표시
            RefreshListPanel();

            if (failedPaths.Count == 0)
            {
                MessageBox.Show(
                    this,
                    $"{deletedCount}개 폴더를 삭제했습니다.",
                    "삭제 완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                var failPreview = string.Join(Environment.NewLine, failedPaths.Take(10));
                var failSuffix = failedPaths.Count > 10
                    ? $"{Environment.NewLine}... (실패 {failedPaths.Count}개)"
                    : "";
                MessageBox.Show(
                    this,
                    $"삭제 완료: {deletedCount}개{Environment.NewLine}실패: {failedPaths.Count}개{Environment.NewLine}{Environment.NewLine}{failPreview}{failSuffix}",
                    "삭제 결과",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ListPanelUpdatebtr_Click(object? sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            if (dateCount <= 0 || dateStrArray.Length == 0 || FtpDateModelPath.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "먼저 레시피 선택에서 기간·모델을 검색해 주세요.",
                    "리스트 업데이트",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            RefreshListPanel();
        }

        /// <summary>현재 검색 조건(기간·모델)으로 목록을 다시 스캔·표시합니다.</summary>
        private void RefreshListPanel()
        {
            SingleStaticSavePoint = -1;
            selectedListSelectRowNumber = -1;
            ComentTextBox.ReadOnly = true;
            ComentTextBox.Text = "1개이상의 행을 선택해주세요\n 읽기만 가능합니다";

            _listRowEntries.Clear();
            for (int i = 0; i < dateCount && i < dateStrArray.Length && i < FtpDateModelPath.Count; i++)
            {
                _listRowEntries.AddRange(ScanBcrTrialRowsUnderDateModelRoot(dateStrArray[i], FtpDateModelPath[i]));
            }

            // 날짜 → 시행횟수 오름차순 (BCR명은 정렬 키에서 제외)
            _listRowEntries.Sort((a, b) =>
            {
                int cmp = string.CompareOrdinal(a.DateStr, b.DateStr);
                if (cmp != 0)
                {
                    return cmp;
                }

                return a.TrialNumber.CompareTo(b.TrialNumber);
            });

            BuildListDateRowControls();
        }
        //JSON 파일 예시

        public class RootData : Dictionary<string, SignalMetrics>
        {
        }

        public class SignalMetrics
        {
            [JsonPropertyName("AC")]
            public Dictionary<string, List<double>> AC { get; set; } = new();

            [JsonPropertyName("DC")]
            public Dictionary<string, List<double>> DC { get; set; } = new();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (seletedcmodel == "-" || seletedcmodel.Contains("label"))
            {
                MessageBox.Show(
                 "모델을 선택해주세요",
                 "모델 선택에러",
                 MessageBoxButtons.OK,
                 MessageBoxIcon.Warning);
                return;
            }
            ModelBaseLineSettingPanelCount = 1;
            ModelBaseLineSettingPanel.Size = new Size(1255, 795);
            ModelBaseLineSettingPanel.Location = new Point(0, 0);
            ModelBaseLineSettingPanel.Visible = true;
            ModelBaseLineSettingPanel.BringToFront();
            //json파일에 저장된 값들이 있다면 불러와서 각 text박스에 표시함 없으면 그냥 0
            string filePath = "./GradeBaseline.json";

            // JSON 직렬화 옵션 (보기 좋게 들여쓰기)
            if (!File.Exists(filePath))
            {
                Logger.LogError("Json-GradeBaseline.json", $"해당 파일 없음 {filePath}");
                return;
            }

            string jsonString = File.ReadAllText(filePath);
            RootData? rootData = JsonSerializer.Deserialize<RootData>(jsonString);

            if (rootData == null)
            {
                Logger.LogError("Json-GradeBaseline.json", $"해당 파일 파싱 실패 {filePath}");
                return;
            }


            ///// 모델데이터가 있는 경우 
            if (rootData.TryGetValue(seletedcmodel, out SignalMetrics? model1Data))
            {


                // AC
                {
                    if (model1Data.AC.TryGetValue("PeakX_Max", out List<double>? PeakX_Max))
                    {
                        AC_PeakX90tb.Text = PeakX_Max[3].ToString();
                        AC_PeakX80tb.Text = PeakX_Max[2].ToString();
                        textBox16.Text = PeakX_Max[1].ToString();
                        textBox20.Text = PeakX_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("PeakX_MaxInterval", out List<double>? PeakX_MaxInterval))
                    {

                        textBox7.Text = PeakX_MaxInterval[3].ToString();
                        textBox11.Text = PeakX_MaxInterval[2].ToString();
                        textBox15.Text = PeakX_MaxInterval[1].ToString();
                        textBox19.Text = PeakX_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("PeakX_RMS", out List<double>? PeakX_RMS))
                    {
                        textBox6.Text = PeakX_RMS[3].ToString();
                        textBox10.Text = PeakX_RMS[2].ToString();
                        textBox14.Text = PeakX_RMS[1].ToString();
                        textBox18.Text = PeakX_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("PeakX_ROUT", out List<double>? PeakX_ROUT))
                    {
                        textBox5.Text = PeakX_ROUT[3].ToString();
                        textBox9.Text = PeakX_ROUT[2].ToString();
                        textBox13.Text = PeakX_ROUT[1].ToString();
                        textBox17.Text = PeakX_ROUT[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("PeakY_Max", out List<double>? PeakY_Max))
                    {
                        textBox36.Text = PeakY_Max[3].ToString();
                        textBox32.Text = PeakY_Max[2].ToString();
                        textBox28.Text = PeakY_Max[1].ToString();
                        textBox24.Text = PeakY_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("PeakY_MaxInterval", out List<double>? PeakY_MaxInterval))
                    {

                        textBox35.Text = PeakY_MaxInterval[3].ToString();
                        textBox31.Text = PeakY_MaxInterval[2].ToString();
                        textBox27.Text = PeakY_MaxInterval[1].ToString();
                        textBox23.Text = PeakY_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("PeakY_RMS", out List<double>? PeakY_RMS))
                    {
                        textBox34.Text = PeakY_RMS[3].ToString();
                        textBox30.Text = PeakY_RMS[2].ToString();
                        textBox26.Text = PeakY_RMS[1].ToString();
                        textBox22.Text = PeakY_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("PeakY_ROUT", out List<double>? PeakY_ROUT))
                    {
                        textBox33.Text = PeakY_ROUT[3].ToString();
                        textBox29.Text = PeakY_ROUT[2].ToString();
                        textBox25.Text = PeakY_ROUT[1].ToString();
                        textBox21.Text = PeakY_ROUT[0].ToString();
                    }



                    if (model1Data.AC.TryGetValue("AreaX_Max", out List<double>? AreaX_Max))
                    {
                        textBox76.Text = AreaX_Max[3].ToString();
                        textBox72.Text = AreaX_Max[2].ToString();
                        textBox68.Text = AreaX_Max[1].ToString();
                        textBox64.Text = AreaX_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("AreaX_MaxInterval", out List<double>? AreaX_MaxInterval))
                    {

                        textBox75.Text = AreaX_MaxInterval[3].ToString();
                        textBox71.Text = AreaX_MaxInterval[2].ToString();
                        textBox67.Text = AreaX_MaxInterval[1].ToString();
                        textBox63.Text = AreaX_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("AreaX_RMS", out List<double>? AreaX_RMS))
                    {
                        textBox74.Text = AreaX_RMS[3].ToString();
                        textBox70.Text = AreaX_RMS[2].ToString();
                        textBox66.Text = AreaX_RMS[1].ToString();
                        textBox62.Text = AreaX_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("AreaX_ROUT", out List<double>? AreaX_ROUT))
                    {
                        textBox73.Text = AreaX_ROUT[3].ToString();
                        textBox69.Text = AreaX_ROUT[2].ToString();
                        textBox65.Text = AreaX_ROUT[1].ToString();
                        textBox61.Text = AreaX_ROUT[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("AreaY_Max", out List<double>? AreaY_Max))
                    {
                        textBox56.Text = AreaY_Max[3].ToString();
                        textBox52.Text = AreaY_Max[2].ToString();
                        textBox48.Text = AreaY_Max[1].ToString();
                        textBox44.Text = AreaY_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("AreaY_MaxInterval", out List<double>? AreaY_MaxInterval))
                    {

                        textBox55.Text = AreaY_MaxInterval[3].ToString();
                        textBox51.Text = AreaY_MaxInterval[2].ToString();
                        textBox47.Text = AreaY_MaxInterval[1].ToString();
                        textBox43.Text = AreaY_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("AreaY_RMS", out List<double>? AreaY_RMS))
                    {
                        textBox54.Text = AreaY_RMS[3].ToString();
                        textBox50.Text = AreaY_RMS[2].ToString();
                        textBox46.Text = AreaY_RMS[1].ToString();
                        textBox42.Text = AreaY_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("AreaY_ROUT", out List<double>? AreaY_ROUT))
                    {
                        textBox53.Text = AreaY_ROUT[3].ToString();
                        textBox49.Text = AreaY_ROUT[2].ToString();
                        textBox45.Text = AreaY_ROUT[1].ToString();
                        textBox41.Text = AreaY_ROUT[0].ToString();
                    }



                    if (model1Data.AC.TryGetValue("Length_Max", out List<double>? Length_Max))
                    {
                        textBox156.Text = Length_Max[3].ToString();
                        textBox152.Text = Length_Max[2].ToString();
                        textBox148.Text = Length_Max[1].ToString();
                        textBox144.Text = Length_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("Length_MaxInterval", out List<double>? Length_MaxInterval))
                    {

                        textBox155.Text = Length_MaxInterval[3].ToString();
                        textBox151.Text = Length_MaxInterval[2].ToString();
                        textBox147.Text = Length_MaxInterval[1].ToString();
                        textBox143.Text = Length_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Length_RMS", out List<double>? Length_RMS))
                    {
                        textBox154.Text = Length_RMS[3].ToString();
                        textBox150.Text = Length_RMS[2].ToString();
                        textBox146.Text = Length_RMS[1].ToString();
                        textBox142.Text = Length_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Length_ROUT", out List<double>? Length_ROUT))
                    {
                        textBox153.Text = Length_ROUT[3].ToString();
                        textBox149.Text = Length_ROUT[2].ToString();
                        textBox145.Text = Length_ROUT[1].ToString();
                        textBox141.Text = Length_ROUT[0].ToString();
                    }




                    if (model1Data.AC.TryGetValue("Height_Max", out List<double>? Height_Max))
                    {
                        textBox136.Text = Height_Max[3].ToString();
                        textBox132.Text = Height_Max[2].ToString();
                        textBox128.Text = Height_Max[1].ToString();
                        textBox124.Text = Height_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("Height_MaxInterval", out List<double>? Height_MaxInterval))
                    {

                        textBox135.Text = Height_MaxInterval[3].ToString();
                        textBox131.Text = Height_MaxInterval[2].ToString();
                        textBox127.Text = Height_MaxInterval[1].ToString();
                        textBox123.Text = Height_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Height_RMS", out List<double>? Height_RMS))
                    {
                        textBox134.Text = Height_RMS[3].ToString();
                        textBox130.Text = Height_RMS[2].ToString();
                        textBox126.Text = Height_RMS[1].ToString();
                        textBox122.Text = Height_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Height_ROUT", out List<double>? Height_ROUT))
                    {
                        textBox133.Text = Height_ROUT[3].ToString();
                        textBox129.Text = Height_ROUT[2].ToString();
                        textBox125.Text = Height_ROUT[1].ToString();
                        textBox121.Text = Height_ROUT[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("Area_Max", out List<double>? Area_Max))
                    {
                        textBox116.Text = Area_Max[3].ToString();
                        textBox112.Text = Area_Max[2].ToString();
                        textBox108.Text = Area_Max[1].ToString();
                        textBox104.Text = Area_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("Area_MaxInterval", out List<double>? Area_MaxInterval))
                    {

                        textBox115.Text = Area_MaxInterval[3].ToString();
                        textBox111.Text = Area_MaxInterval[2].ToString();
                        textBox107.Text = Area_MaxInterval[1].ToString();
                        textBox103.Text = Area_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Area_RMS", out List<double>? Area_RMS))
                    {
                        textBox114.Text = Area_RMS[3].ToString();
                        textBox110.Text = Area_RMS[2].ToString();
                        textBox106.Text = Area_RMS[1].ToString();
                        textBox102.Text = Area_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Area_ROUT", out List<double>? Area_ROUT))
                    {
                        textBox113.Text = Area_ROUT[3].ToString();
                        textBox109.Text = Area_ROUT[2].ToString();
                        textBox105.Text = Area_ROUT[1].ToString();
                        textBox101.Text = Area_ROUT[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("Distance_Max", out List<double>? Distance_Max))
                    {
                        textBox96.Text = Distance_Max[3].ToString();
                        textBox92.Text = Distance_Max[2].ToString();
                        textBox88.Text = Distance_Max[1].ToString();
                        textBox84.Text = Distance_Max[0].ToString();
                    }


                    if (model1Data.AC.TryGetValue("Distance_MaxInterval", out List<double>? Distance_MaxInterval))
                    {

                        textBox95.Text = Distance_MaxInterval[3].ToString();
                        textBox91.Text = Distance_MaxInterval[2].ToString();
                        textBox87.Text = Distance_MaxInterval[1].ToString();
                        textBox83.Text = Distance_MaxInterval[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Distance_RMS", out List<double>? Distance_RMS))
                    {
                        textBox94.Text = Distance_RMS[3].ToString();
                        textBox90.Text = Distance_RMS[2].ToString();
                        textBox86.Text = Distance_RMS[1].ToString();
                        textBox82.Text = Distance_RMS[0].ToString();
                    }

                    if (model1Data.AC.TryGetValue("Distance_ROUT", out List<double>? Distance_ROUT))
                    {
                        textBox93.Text = Distance_ROUT[3].ToString();
                        textBox89.Text = Distance_ROUT[2].ToString();
                        textBox85.Text = Distance_ROUT[1].ToString();
                        textBox81.Text = Distance_ROUT[0].ToString();
                    }
                }
                //DC
                {
                    if (model1Data.DC.TryGetValue("PeakX_Max", out List<double>? PeakX_Max))
                    {
                        textBox316.Text = PeakX_Max[3].ToString();
                        textBox312.Text = PeakX_Max[2].ToString();
                        textBox308.Text = PeakX_Max[1].ToString();
                        textBox304.Text = PeakX_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("PeakX_MaxInterval", out List<double>? PeakX_MaxInterval))
                    {

                        textBox315.Text = PeakX_MaxInterval[3].ToString();
                        textBox311.Text = PeakX_MaxInterval[2].ToString();
                        textBox307.Text = PeakX_MaxInterval[1].ToString();
                        textBox303.Text = PeakX_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("PeakX_RMS", out List<double>? PeakX_RMS))
                    {
                        textBox314.Text = PeakX_RMS[3].ToString();
                        textBox310.Text = PeakX_RMS[2].ToString();
                        textBox306.Text = PeakX_RMS[1].ToString();
                        textBox302.Text = PeakX_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("PeakX_ROUT", out List<double>? PeakX_ROUT))
                    {
                        textBox313.Text = PeakX_ROUT[3].ToString();
                        textBox309.Text = PeakX_ROUT[2].ToString();
                        textBox305.Text = PeakX_ROUT[1].ToString();
                        textBox301.Text = PeakX_ROUT[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("PeakY_Max", out List<double>? PeakY_Max))
                    {
                        textBox296.Text = PeakY_Max[3].ToString();
                        textBox292.Text = PeakY_Max[2].ToString();
                        textBox288.Text = PeakY_Max[1].ToString();
                        textBox284.Text = PeakY_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("PeakY_MaxInterval", out List<double>? PeakY_MaxInterval))
                    {

                        textBox295.Text = PeakY_MaxInterval[3].ToString();
                        textBox291.Text = PeakY_MaxInterval[2].ToString();
                        textBox287.Text = PeakY_MaxInterval[1].ToString();
                        textBox283.Text = PeakY_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("PeakY_RMS", out List<double>? PeakY_RMS))
                    {
                        textBox294.Text = PeakY_RMS[3].ToString();
                        textBox290.Text = PeakY_RMS[2].ToString();
                        textBox286.Text = PeakY_RMS[1].ToString();
                        textBox282.Text = PeakY_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("PeakY_ROUT", out List<double>? PeakY_ROUT))
                    {
                        textBox293.Text = PeakY_ROUT[3].ToString();
                        textBox289.Text = PeakY_ROUT[2].ToString();
                        textBox285.Text = PeakY_ROUT[1].ToString();
                        textBox281.Text = PeakY_ROUT[0].ToString();
                    }



                    if (model1Data.DC.TryGetValue("AreaX_Max", out List<double>? AreaX_Max))
                    {
                        textBox276.Text = AreaX_Max[3].ToString();
                        textBox272.Text = AreaX_Max[2].ToString();
                        textBox268.Text = AreaX_Max[1].ToString();
                        textBox264.Text = AreaX_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("AreaX_MaxInterval", out List<double>? AreaX_MaxInterval))
                    {

                        textBox275.Text = AreaX_MaxInterval[3].ToString();
                        textBox271.Text = AreaX_MaxInterval[2].ToString();
                        textBox267.Text = AreaX_MaxInterval[1].ToString();
                        textBox263.Text = AreaX_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("AreaX_RMS", out List<double>? AreaX_RMS))
                    {
                        textBox274.Text = AreaX_RMS[3].ToString();
                        textBox270.Text = AreaX_RMS[2].ToString();
                        textBox266.Text = AreaX_RMS[1].ToString();
                        textBox262.Text = AreaX_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("AreaX_ROUT", out List<double>? AreaX_ROUT))
                    {
                        textBox273.Text = AreaX_ROUT[3].ToString();
                        textBox269.Text = AreaX_ROUT[2].ToString();
                        textBox265.Text = AreaX_ROUT[1].ToString();
                        textBox261.Text = AreaX_ROUT[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("AreaY_Max", out List<double>? AreaY_Max))
                    {
                        textBox256.Text = AreaY_Max[3].ToString();
                        textBox252.Text = AreaY_Max[2].ToString();
                        textBox248.Text = AreaY_Max[1].ToString();
                        textBox244.Text = AreaY_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("AreaY_MaxInterval", out List<double>? AreaY_MaxInterval))
                    {

                        textBox255.Text = AreaY_MaxInterval[3].ToString();
                        textBox251.Text = AreaY_MaxInterval[2].ToString();
                        textBox247.Text = AreaY_MaxInterval[1].ToString();
                        textBox243.Text = AreaY_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("AreaY_RMS", out List<double>? AreaY_RMS))
                    {
                        textBox254.Text = AreaY_RMS[3].ToString();
                        textBox250.Text = AreaY_RMS[2].ToString();
                        textBox246.Text = AreaY_RMS[1].ToString();
                        textBox242.Text = AreaY_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("AreaY_ROUT", out List<double>? AreaY_ROUT))
                    {
                        textBox253.Text = AreaY_ROUT[3].ToString();
                        textBox249.Text = AreaY_ROUT[2].ToString();
                        textBox245.Text = AreaY_ROUT[1].ToString();
                        textBox241.Text = AreaY_ROUT[0].ToString();
                    }



                    if (model1Data.DC.TryGetValue("Length_Max", out List<double>? Length_Max))
                    {
                        textBox236.Text = Length_Max[3].ToString();
                        textBox232.Text = Length_Max[2].ToString();
                        textBox228.Text = Length_Max[1].ToString();
                        textBox224.Text = Length_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("Length_MaxInterval", out List<double>? Length_MaxInterval))
                    {

                        textBox235.Text = Length_MaxInterval[3].ToString();
                        textBox231.Text = Length_MaxInterval[2].ToString();
                        textBox227.Text = Length_MaxInterval[1].ToString();
                        textBox223.Text = Length_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Length_RMS", out List<double>? Length_RMS))
                    {
                        textBox234.Text = Length_RMS[3].ToString();
                        textBox230.Text = Length_RMS[2].ToString();
                        textBox226.Text = Length_RMS[1].ToString();
                        textBox222.Text = Length_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Length_ROUT", out List<double>? Length_ROUT))
                    {
                        textBox233.Text = Length_ROUT[3].ToString();
                        textBox229.Text = Length_ROUT[2].ToString();
                        textBox225.Text = Length_ROUT[1].ToString();
                        textBox221.Text = Length_ROUT[0].ToString();
                    }




                    if (model1Data.DC.TryGetValue("Height_Max", out List<double>? Height_Max))
                    {
                        textBox216.Text = Height_Max[3].ToString();
                        textBox212.Text = Height_Max[2].ToString();
                        textBox208.Text = Height_Max[1].ToString();
                        textBox204.Text = Height_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("Height_MaxInterval", out List<double>? Height_MaxInterval))
                    {

                        textBox215.Text = Height_MaxInterval[3].ToString();
                        textBox211.Text = Height_MaxInterval[2].ToString();
                        textBox207.Text = Height_MaxInterval[1].ToString();
                        textBox203.Text = Height_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Height_RMS", out List<double>? Height_RMS))
                    {
                        textBox214.Text = Height_RMS[3].ToString();
                        textBox210.Text = Height_RMS[2].ToString();
                        textBox206.Text = Height_RMS[1].ToString();
                        textBox202.Text = Height_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Height_ROUT", out List<double>? Height_ROUT))
                    {
                        textBox213.Text = Height_ROUT[3].ToString();
                        textBox209.Text = Height_ROUT[2].ToString();
                        textBox205.Text = Height_ROUT[1].ToString();
                        textBox201.Text = Height_ROUT[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("Area_Max", out List<double>? Area_Max))
                    {
                        textBox196.Text = Area_Max[3].ToString();
                        textBox192.Text = Area_Max[2].ToString();
                        textBox188.Text = Area_Max[1].ToString();
                        textBox184.Text = Area_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("Area_MaxInterval", out List<double>? Area_MaxInterval))
                    {

                        textBox195.Text = Area_MaxInterval[3].ToString();
                        textBox191.Text = Area_MaxInterval[2].ToString();
                        textBox187.Text = Area_MaxInterval[1].ToString();
                        textBox183.Text = Area_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Area_RMS", out List<double>? Area_RMS))
                    {
                        textBox194.Text = Area_RMS[3].ToString();
                        textBox190.Text = Area_RMS[2].ToString();
                        textBox186.Text = Area_RMS[1].ToString();
                        textBox182.Text = Area_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Area_ROUT", out List<double>? Area_ROUT))
                    {
                        textBox193.Text = Area_ROUT[3].ToString();
                        textBox189.Text = Area_ROUT[2].ToString();
                        textBox185.Text = Area_ROUT[1].ToString();
                        textBox181.Text = Area_ROUT[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("Distance_Max", out List<double>? Distance_Max))
                    {
                        textBox176.Text = Distance_Max[3].ToString();
                        textBox172.Text = Distance_Max[2].ToString();
                        textBox168.Text = Distance_Max[1].ToString();
                        textBox164.Text = Distance_Max[0].ToString();
                    }


                    if (model1Data.DC.TryGetValue("Distance_MaxInterval", out List<double>? Distance_MaxInterval))
                    {

                        textBox175.Text = Distance_MaxInterval[3].ToString();
                        textBox171.Text = Distance_MaxInterval[2].ToString();
                        textBox167.Text = Distance_MaxInterval[1].ToString();
                        textBox163.Text = Distance_MaxInterval[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Distance_RMS", out List<double>? Distance_RMS))
                    {
                        textBox174.Text = Distance_RMS[3].ToString();
                        textBox170.Text = Distance_RMS[2].ToString();
                        textBox166.Text = Distance_RMS[1].ToString();
                        textBox162.Text = Distance_RMS[0].ToString();
                    }

                    if (model1Data.DC.TryGetValue("Distance_ROUT", out List<double>? Distance_ROUT))
                    {
                        textBox173.Text = Distance_ROUT[3].ToString();
                        textBox169.Text = Distance_ROUT[2].ToString();
                        textBox165.Text = Distance_ROUT[1].ToString();
                        textBox161.Text = Distance_ROUT[0].ToString();
                    }
                }
                Logger.LogInfo("json 파일 읽음 완료", $"선택된 모델:{seletedcmodel}에 대한 기준치 데이터 존재 -> 업로드 완료 ");
            }
            else// 모델데이터가 없는 경우
            {
                Logger.LogInfo("json 파일 읽음 완료", $"선택된 모델:{seletedcmodel}에 대한 기준치 데이터 없음 ");
            }

            ModelBaseLineSettingPanel_Display();


        }

        private void ModelBaseLineSettingPanel_Downbtr_Click(object sender, EventArgs e)
        {
            ModelBaseLineSettingPanelCount--;
            if (ModelBaseLineSettingPanelCount <= 0)
            {
                ModelBaseLineSettingPanelCount = 1;
            }

            ModelBaseLineSettingPanel_Display();
        }

        private void ModelBaseLineSettingPanel_UPbtr_Click(object sender, EventArgs e)
        {
            ModelBaseLineSettingPanelCount++;
            if (ModelBaseLineSettingPanelCount >= 5)// 수정 필요
            {
                ModelBaseLineSettingPanelCount = 4;
            }

            ModelBaseLineSettingPanel_Display();
        }

        private void ModelBaseLineSettingPanel_Display()
        {
            //특정 panel을 찾아서 해당 패널의 visible값을 true로 변경
            Control[] founds = this.Controls.Find($"ModelBaseLineSettingPanel_{ModelBaseLineSettingPanelCount}", true);
            if (founds.Length > 0 && founds[0] is Panel targetPanel)
            {
                // 찾은 패널 처리
                targetPanel.Location = new Point(3, 3);
                targetPanel.Size = new Size(1249, 730);
                targetPanel.Visible = true;
                targetPanel.BringToFront();
            }
        }

        private void SetupModelBaseLineSettingPanelDecimalInput()
        {
            AttachDecimalInputToTextBoxes(ModelBaseLineSettingPanel);
            AttachDecimalInputToTextBoxes(ModelGraderRangeSettingPanel);
        }

        private static void AttachDecimalInputToTextBoxes(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.ImeMode = ImeMode.Disable;
                    textBox.KeyPress += ModelBaseLineDecimalTextBox_KeyPress;
                }
                else if (control.HasChildren)
                {
                    AttachDecimalInputToTextBoxes(control);
                }
            }
        }

        private static void ModelBaseLineDecimalTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            if (char.IsDigit(e.KeyChar))
            {
                return;
            }

            if (e.KeyChar == '.' && sender is TextBox textBox && !textBox.Text.Contains('.'))
            {
                return;
            }

            e.Handled = true;
        }

        private void ModelBaseLineSavebtr_Click(object sender, EventArgs e)
        {
            if (!TryValidateModelBaseLineInputs(out TextBox? invalidTextBox))
            {
                MessageBox.Show(
                    "입력되지 않았거나 숫자가 아닌 항목이 있습니다.\n모든 기준치를 입력한 뒤 다시 저장해주세요.",
                    "입력 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                if (invalidTextBox != null)
                {
                    BringModelBaseLineTextBoxIntoView(invalidTextBox);
                    invalidTextBox.Focus();
                }

                return;
            }



            DialogResult result = MessageBox.Show(
             "입력을 완료 할까요?", // 본문 내용
             "확인 요청",                           // 상자 제목
             MessageBoxButtons.OKCancel,            // 확인/취소 버튼
             MessageBoxIcon.Question                // 물음표 아이콘
            );

            // 2. 사용자의 선택 결과 처리
            if (result == DialogResult.OK)
            {

                string filePath = "./GradeBaseline.json";
                var options = new JsonSerializerOptions { WriteIndented = true };

                // 1. 기존 파일 읽기 (없으면 새로운 객체 생성)
                RootData rootData;
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    rootData = JsonSerializer.Deserialize<RootData>(jsonString) ?? new RootData();
                }
                else
                {
                    rootData = new RootData();
                }

                // 2. 새로운 모델 생성 (예: MODEL3)
                string newModelName = seletedcmodel;

                var newModelMetrics = new SignalMetrics
                {
                    AC = new Dictionary<string, List<double>>
                    {
                        { "PeakX_Max", new List<double> { double.Parse(textBox20.Text), double.Parse(textBox16.Text), double.Parse(AC_PeakX80tb.Text), double.Parse(AC_PeakX90tb.Text) } },
                        { "PeakX_MaxInterval", new List<double> { double.Parse(textBox19.Text), double.Parse(textBox15.Text), double.Parse(textBox11.Text), double.Parse(textBox7.Text) } },
                        { "PeakX_RMS", new List<double> { double.Parse(textBox18.Text), double.Parse(textBox14.Text), double.Parse(textBox10.Text), double.Parse(textBox6.Text) } },
                        { "PeakX_ROUT", new List<double> { double.Parse(textBox17.Text), double.Parse(textBox13.Text), double.Parse(textBox9.Text), double.Parse(textBox5.Text) } },

                        { "PeakY_Max", new List<double> { double.Parse(textBox24.Text), double.Parse(textBox28.Text), double.Parse(textBox32.Text), double.Parse(textBox36.Text) } },
                        { "PeakY_MaxInterval", new List<double> { double.Parse(textBox23.Text), double.Parse(textBox27.Text), double.Parse(textBox31.Text), double.Parse(textBox35.Text) } },
                        { "PeakY_RMS", new List<double> { double.Parse(textBox22.Text), double.Parse(textBox26.Text), double.Parse(textBox30.Text), double.Parse(textBox34.Text) } },
                        { "PeakY_ROUT", new List<double> { double.Parse(textBox21.Text), double.Parse(textBox25.Text), double.Parse(textBox29.Text), double.Parse(textBox33.Text) } },

                        { "AreaX_Max", new List<double> { double.Parse(textBox64.Text), double.Parse(textBox68.Text), double.Parse(textBox72.Text), double.Parse(textBox76.Text) } },
                        { "AreaX_MaxInterval", new List<double> { double.Parse(textBox63.Text), double.Parse(textBox67.Text), double.Parse(textBox71.Text), double.Parse(textBox75.Text) } },
                        { "AreaX_RMS", new List<double> { double.Parse(textBox62.Text), double.Parse(textBox66.Text), double.Parse(textBox70.Text), double.Parse(textBox74.Text) } },
                        { "AreaX_ROUT", new List<double> { double.Parse(textBox61.Text), double.Parse(textBox65.Text), double.Parse(textBox69.Text), double.Parse(textBox73.Text) } },

                        { "AreaY_Max", new List<double> { double.Parse(textBox44.Text), double.Parse(textBox48.Text), double.Parse(textBox52.Text), double.Parse(textBox56.Text) } },
                        { "AreaY_MaxInterval", new List<double> { double.Parse(textBox43.Text), double.Parse(textBox47.Text), double.Parse(textBox51.Text), double.Parse(textBox55.Text) } },
                        { "AreaY_RMS", new List<double> { double.Parse(textBox42.Text), double.Parse(textBox46.Text), double.Parse(textBox50.Text), double.Parse(textBox54.Text) } },
                        { "AreaY_ROUT", new List<double> { double.Parse(textBox41.Text), double.Parse(textBox45.Text), double.Parse(textBox49.Text), double.Parse(textBox53.Text) } },

                        { "Length_Max", new List<double> { double.Parse(textBox144.Text), double.Parse(textBox148.Text), double.Parse(textBox152.Text), double.Parse(textBox156.Text) } },
                        { "Length_MaxInterval", new List<double> { double.Parse(textBox143.Text), double.Parse(textBox147.Text), double.Parse(textBox151.Text), double.Parse(textBox155.Text) } },
                        { "Length_RMS", new List<double> { double.Parse(textBox142.Text), double.Parse(textBox146.Text), double.Parse(textBox150.Text), double.Parse(textBox154.Text) } },
                        { "Length_ROUT", new List<double> { double.Parse(textBox141.Text), double.Parse(textBox145.Text), double.Parse(textBox149.Text), double.Parse(textBox153.Text) } },

                        { "Height_Max", new List<double> { double.Parse(textBox124.Text), double.Parse(textBox128.Text), double.Parse(textBox132.Text), double.Parse(textBox136.Text) } },
                        { "Height_MaxInterval", new List<double> { double.Parse(textBox123.Text), double.Parse(textBox127.Text), double.Parse(textBox131.Text), double.Parse(textBox135.Text) } },
                        { "Height_RMS", new List<double> { double.Parse(textBox122.Text), double.Parse(textBox126.Text), double.Parse(textBox130.Text), double.Parse(textBox134.Text) } },
                        { "Height_ROUT", new List<double> { double.Parse(textBox121.Text), double.Parse(textBox125.Text), double.Parse(textBox129.Text), double.Parse(textBox133.Text) } },

                        { "Area_Max", new List<double> { double.Parse(textBox104.Text), double.Parse(textBox108.Text), double.Parse(textBox112.Text), double.Parse(textBox116.Text) } },
                        { "Area_MaxInterval", new List<double> { double.Parse(textBox103.Text), double.Parse(textBox107.Text), double.Parse(textBox111.Text), double.Parse(textBox115.Text) } },
                        { "Area_RMS", new List<double> { double.Parse(textBox102.Text), double.Parse(textBox106.Text), double.Parse(textBox110.Text), double.Parse(textBox114.Text) } },
                        { "Area_ROUT", new List<double> { double.Parse(textBox101.Text), double.Parse(textBox105.Text), double.Parse(textBox109.Text), double.Parse(textBox113.Text) } },

                        { "Distance_Max", new List<double> { double.Parse(textBox84.Text), double.Parse(textBox88.Text), double.Parse(textBox92.Text), double.Parse(textBox96.Text) } },
                        { "Distance_MaxInterval", new List<double> { double.Parse(textBox83.Text), double.Parse(textBox87.Text), double.Parse(textBox91.Text), double.Parse(textBox95.Text) } },
                        { "Distance_RMS", new List<double> { double.Parse(textBox82.Text), double.Parse(textBox86.Text), double.Parse(textBox90.Text), double.Parse(textBox94.Text) } },
                        { "Distance_ROUT", new List<double> { double.Parse(textBox81.Text), double.Parse(textBox85.Text), double.Parse(textBox89.Text), double.Parse(textBox93.Text) } },
                    },

                    DC = new Dictionary<string, List<double>>
                    {
                        { "PeakX_Max", new List<double> { double.Parse(textBox304.Text), double.Parse(textBox308.Text), double.Parse(textBox312.Text), double.Parse(textBox316.Text) } },
                        { "PeakX_MaxInterval", new List<double> { double.Parse(textBox303.Text), double.Parse(textBox307.Text), double.Parse(textBox311.Text), double.Parse(textBox315.Text) } },
                        { "PeakX_RMS", new List<double> { double.Parse(textBox302.Text), double.Parse(textBox306.Text), double.Parse(textBox310.Text), double.Parse(textBox314.Text) } },
                        { "PeakX_ROUT", new List<double> { double.Parse(textBox301.Text), double.Parse(textBox305.Text), double.Parse(textBox309.Text), double.Parse(textBox313.Text) } },

                        { "PeakY_Max", new List<double> { double.Parse(textBox284.Text), double.Parse(textBox288.Text), double.Parse(textBox292.Text), double.Parse(textBox296.Text) } },
                        { "PeakY_MaxInterval", new List<double> { double.Parse(textBox283.Text), double.Parse(textBox287.Text), double.Parse(textBox291.Text), double.Parse(textBox295.Text) } },
                        { "PeakY_RMS", new List<double> { double.Parse(textBox282.Text), double.Parse(textBox286.Text), double.Parse(textBox290.Text), double.Parse(textBox294.Text) } },
                        { "PeakY_ROUT", new List<double> { double.Parse(textBox281.Text), double.Parse(textBox285.Text), double.Parse(textBox289.Text), double.Parse(textBox293.Text) } },

                        { "AreaX_Max", new List<double> { double.Parse(textBox264.Text), double.Parse(textBox268.Text), double.Parse(textBox272.Text), double.Parse(textBox276.Text) } },
                        { "AreaX_MaxInterval", new List<double> { double.Parse(textBox263.Text), double.Parse(textBox267.Text), double.Parse(textBox271.Text), double.Parse(textBox275.Text) } },
                        { "AreaX_RMS", new List<double> { double.Parse(textBox262.Text), double.Parse(textBox266.Text), double.Parse(textBox270.Text), double.Parse(textBox274.Text) } },
                        { "AreaX_ROUT", new List<double> { double.Parse(textBox261.Text), double.Parse(textBox265.Text), double.Parse(textBox269.Text), double.Parse(textBox273.Text) } },

                        { "AreaY_Max", new List<double> { double.Parse(textBox244.Text), double.Parse(textBox248.Text), double.Parse(textBox252.Text), double.Parse(textBox256.Text) } },
                        { "AreaY_MaxInterval", new List<double> { double.Parse(textBox243.Text), double.Parse(textBox247.Text), double.Parse(textBox251.Text), double.Parse(textBox255.Text) } },
                        { "AreaY_RMS", new List<double> { double.Parse(textBox242.Text), double.Parse(textBox246.Text), double.Parse(textBox250.Text), double.Parse(textBox254.Text) } },
                        { "AreaY_ROUT", new List<double> { double.Parse(textBox241.Text), double.Parse(textBox245.Text), double.Parse(textBox249.Text), double.Parse(textBox253.Text) } },

                        { "Length_Max", new List<double> { double.Parse(textBox224.Text), double.Parse(textBox228.Text), double.Parse(textBox232.Text), double.Parse(textBox236.Text) } },
                        { "Length_MaxInterval", new List<double> { double.Parse(textBox223.Text), double.Parse(textBox227.Text), double.Parse(textBox231.Text), double.Parse(textBox235.Text) } },
                        { "Length_RMS", new List<double> { double.Parse(textBox222.Text), double.Parse(textBox226.Text), double.Parse(textBox230.Text), double.Parse(textBox234.Text) } },
                        { "Length_ROUT", new List<double> { double.Parse(textBox221.Text), double.Parse(textBox225.Text), double.Parse(textBox229.Text), double.Parse(textBox233.Text) } },

                        { "Height_Max", new List<double> { double.Parse(textBox204.Text), double.Parse(textBox208.Text), double.Parse(textBox212.Text), double.Parse(textBox216.Text) } },
                        { "Height_MaxInterval", new List<double> { double.Parse(textBox203.Text), double.Parse(textBox207.Text), double.Parse(textBox211.Text), double.Parse(textBox215.Text) } },
                        { "Height_RMS", new List<double> { double.Parse(textBox202.Text), double.Parse(textBox206.Text), double.Parse(textBox210.Text), double.Parse(textBox214.Text) } },
                        { "Height_ROUT", new List<double> { double.Parse(textBox201.Text), double.Parse(textBox205.Text), double.Parse(textBox209.Text), double.Parse(textBox213.Text) } },

                        { "Area_Max", new List<double> { double.Parse(textBox184.Text), double.Parse(textBox188.Text), double.Parse(textBox192.Text), double.Parse(textBox196.Text) } },
                        { "Area_MaxInterval", new List<double> { double.Parse(textBox183.Text), double.Parse(textBox187.Text), double.Parse(textBox191.Text), double.Parse(textBox195.Text) } },
                        { "Area_RMS", new List<double> { double.Parse(textBox182.Text), double.Parse(textBox186.Text), double.Parse(textBox190.Text), double.Parse(textBox194.Text) } },
                        { "Area_ROUT", new List<double> { double.Parse(textBox181.Text), double.Parse(textBox185.Text), double.Parse(textBox189.Text), double.Parse(textBox193.Text) } },

                        { "Distance_Max", new List<double> { double.Parse(textBox164.Text), double.Parse(textBox168.Text), double.Parse(textBox172.Text), double.Parse(textBox176.Text) } },
                        { "Distance_MaxInterval", new List<double> { double.Parse(textBox163.Text), double.Parse(textBox167.Text), double.Parse(textBox171.Text), double.Parse(textBox175.Text) } },
                        { "Distance_RMS", new List<double> { double.Parse(textBox162.Text), double.Parse(textBox166.Text), double.Parse(textBox170.Text), double.Parse(textBox174.Text) } },
                        { "Distance_ROUT", new List<double> { double.Parse(textBox161.Text), double.Parse(textBox165.Text), double.Parse(textBox169.Text), double.Parse(textBox173.Text) } },
                    }
                };

                // 3. RootData(Dictionary)에 새로운 모델 추가
                // 만약 "MODEL3"가 이미 존재한다면 기존 값을 최신 데이터로 덮어씁니다.
                rootData[newModelName] = newModelMetrics;

                // 4. JSON 파일로 다시 저장
                string updatedJson = JsonSerializer.Serialize(rootData, options);
                File.WriteAllText(filePath, updatedJson);

                Logger.LogInfo("json 파일 저장 완료", $"선택된 모델:{seletedcmodel}에 대한 기준치 데이터 저장 완료 , \n 경로 {filePath}");

                ClearAllTextBoxes(ModelBaseLineSettingPanel);
                ModelBaseLineSettingPanelCount = 1;
                ModelBaseLineSettingPanel.Visible = false;
            }
            else if (result == DialogResult.Cancel)
            {
                return;
            }


        }

        private void ModelBaseLineSettingPanelExitbtr_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
             "평가 기준 입력을 종료 하시겠습니까?", // 본문 내용
             "확인 요청",                           // 상자 제목
             MessageBoxButtons.OKCancel,            // 확인/취소 버튼
             MessageBoxIcon.Question                // 물음표 아이콘
            );

            // 2. 사용자의 선택 결과 처리
            if (result == DialogResult.OK)
            {
                ClearAllTextBoxes(ModelBaseLineSettingPanel);
                ModelBaseLineSettingPanelCount = 1;
                ModelBaseLineSettingPanel.Visible = false;
            }
            else if (result == DialogResult.Cancel)
            {
                return;
            }


        }

        // 패널 내부에 모든 text박스 초기화
        private void ClearAllTextBoxes(Control parentControl)
        {
            foreach (Control control in parentControl.Controls)
            {
                // 1. 컨트롤이 텍스트박스면 초기화
                if (control is TextBox textBox)
                {
                    textBox.Clear();
                }

                // 2. 컨트롤이 다른 자식 컨트롤을 가지고 있다면 재귀 호출
                if (control.HasChildren)
                {
                    ClearAllTextBoxes(control);
                }
            }
        }

        private bool TryValidateModelBaseLineInputs(out TextBox? invalidTextBox)
        {
            invalidTextBox = null;

            foreach (TextBox textBox in EnumerateTextBoxes(ModelBaseLineSettingPanel))
            {
                string value = textBox.Text.Trim();
                if (string.IsNullOrEmpty(value) || !double.TryParse(value, out _))
                {
                    invalidTextBox = textBox;
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<TextBox> EnumerateTextBoxes(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TextBox textBox)
                {
                    yield return textBox;
                }

                if (control.HasChildren)
                {
                    foreach (TextBox child in EnumerateTextBoxes(control))
                    {
                        yield return child;
                    }
                }
            }
        }

        private void BringModelBaseLineTextBoxIntoView(TextBox textBox)
        {
            Control? parent = textBox.Parent;
            while (parent != null)
            {
                if (parent.Name.StartsWith("ModelBaseLineSettingPanel_", StringComparison.Ordinal))
                {
                    if (int.TryParse(parent.Name.AsSpan("ModelBaseLineSettingPanel_".Length), out int page))
                    {
                        ModelBaseLineSettingPanelCount = page;
                        ModelBaseLineSettingPanel_Display();
                    }

                    break;
                }

                parent = parent.Parent;
            }
        }

        private void GraphRangeSettingbtr_Click(object sender, EventArgs e)
        {
            if (seletedcmodel == "-" || seletedcmodel.Contains("label"))
            {
                MessageBox.Show(
                 "모델을 선택해주세요",
                 "모델 선택 에러",
                 MessageBoxButtons.OK,
                 MessageBoxIcon.Warning);
                return;
            }




            //json파일 읽어와서 Text박스들에 업로드 하기!
            string filePath = "./RangeSetting.json";
            try
            {
                // 1. 파일이 실제로 존재하는지 확인
                if (!File.Exists(filePath))
                {
                    MessageBox.Show(
                        $"JSON 파일이 없습니다. \n설정을 진행해주세요",
                        "파일 없음",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
                else
                {
                    //파일이 있는 경우
                    // 2. 파일 전체 내용 읽기
                    string jsonString = File.ReadAllText(filePath);

                    // 3. JSON 문자열을 C# 객체로 변환 (역직렬화)
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 대소문자 구분 없이 매핑
                    };

                    Dictionary<string, ModelRangeJson> rootData = JsonSerializer.Deserialize<Dictionary<string, ModelRangeJson>>(jsonString, options);

                    // 4. 읽어온 데이터 확인 (예시로 첫 번째 모델의 AC 데이터 중 PeakX_Base 값 출력)
                    if (rootData != null && rootData.ContainsKey(seletedcmodel))
                    {
                        var record = rootData[seletedcmodel];
                        //textbox에 해당 값들을 저장
                        textBox1.Text = record.AC.PeakX_Base.ToString();
                        textBox2.Text = record.AC.PeakX_A_Range.ToString();
                        textBox3.Text = record.AC.PeakX_B_Range.ToString();
                        textBox4.Text = record.AC.PeakX_C_Range.ToString();
                        textBox8.Text = record.AC.PeakX_D_Range.ToString();
                        textBox12.Text = record.AC.PeakX_E_Range.ToString();

                        textBox58.Text = record.AC.PeakY_Base.ToString();
                        textBox57.Text = record.AC.PeakY_A_Range.ToString();
                        textBox40.Text = record.AC.PeakY_B_Range.ToString();
                        textBox39.Text = record.AC.PeakY_C_Range.ToString();
                        textBox38.Text = record.AC.PeakY_D_Range.ToString();
                        textBox37.Text = record.AC.PeakY_E_Range.ToString();

                        textBox80.Text = record.AC.AreaX_Base.ToString();
                        textBox79.Text = record.AC.AreaX_A_Range.ToString();
                        textBox78.Text = record.AC.AreaX_B_Range.ToString();
                        textBox77.Text = record.AC.AreaX_C_Range.ToString();
                        textBox60.Text = record.AC.AreaX_D_Range.ToString();
                        textBox59.Text = record.AC.AreaX_E_Range.ToString();

                        textBox118.Text = record.AC.AreaY_Base.ToString();
                        textBox117.Text = record.AC.AreaY_A_Range.ToString();
                        textBox100.Text = record.AC.AreaY_B_Range.ToString();
                        textBox99.Text = record.AC.AreaY_C_Range.ToString();
                        textBox98.Text = record.AC.AreaY_D_Range.ToString();
                        textBox97.Text = record.AC.AreaY_E_Range.ToString();

                        textBox238.Text = record.AC.Length_Base.ToString();
                        textBox237.Text = record.AC.Length_A_Range.ToString();
                        textBox220.Text = record.AC.Length_B_Range.ToString();
                        textBox219.Text = record.AC.Length_C_Range.ToString();
                        textBox218.Text = record.AC.Length_D_Range.ToString();
                        textBox217.Text = record.AC.Length_E_Range.ToString();


                        textBox200.Text = record.AC.Height_Base.ToString();
                        textBox199.Text = record.AC.Height_A_Range.ToString();
                        textBox198.Text = record.AC.Height_B_Range.ToString();
                        textBox197.Text = record.AC.Height_C_Range.ToString();
                        textBox180.Text = record.AC.Height_D_Range.ToString();
                        textBox179.Text = record.AC.Height_E_Range.ToString();


                        textBox178.Text = record.AC.Area_Base.ToString();
                        textBox177.Text = record.AC.Area_A_Range.ToString();
                        textBox160.Text = record.AC.Area_B_Range.ToString();
                        textBox159.Text = record.AC.Area_C_Range.ToString();
                        textBox158.Text = record.AC.Area_D_Range.ToString();
                        textBox157.Text = record.AC.Area_E_Range.ToString();

                        textBox140.Text = record.AC.Distance_Base.ToString();
                        textBox139.Text = record.AC.Distance_A_Range.ToString();
                        textBox138.Text = record.AC.Distance_B_Range.ToString();
                        textBox137.Text = record.AC.Distance_C_Range.ToString();
                        textBox120.Text = record.AC.Distance_D_Range.ToString();
                        textBox119.Text = record.AC.Distance_E_Range.ToString();


                        textBox350.Text = record.DC.PeakX_Base.ToString();
                        textBox349.Text = record.DC.PeakX_A_Range.ToString();
                        textBox348.Text = record.DC.PeakX_B_Range.ToString();
                        textBox347.Text = record.DC.PeakX_C_Range.ToString();
                        textBox346.Text = record.DC.PeakX_D_Range.ToString();
                        textBox345.Text = record.DC.PeakX_E_Range.ToString();

                        textBox344.Text = record.DC.PeakY_Base.ToString();
                        textBox343.Text = record.DC.PeakY_A_Range.ToString();
                        textBox342.Text = record.DC.PeakY_B_Range.ToString();
                        textBox341.Text = record.DC.PeakY_C_Range.ToString();
                        textBox340.Text = record.DC.PeakY_D_Range.ToString();
                        textBox339.Text = record.DC.PeakY_E_Range.ToString();

                        textBox338.Text = record.DC.AreaX_Base.ToString();
                        textBox337.Text = record.DC.AreaX_A_Range.ToString();
                        textBox336.Text = record.DC.AreaX_B_Range.ToString();
                        textBox335.Text = record.DC.AreaX_C_Range.ToString();
                        textBox334.Text = record.DC.AreaX_D_Range.ToString();
                        textBox333.Text = record.DC.AreaX_E_Range.ToString();

                        textBox332.Text = record.DC.AreaY_Base.ToString();
                        textBox331.Text = record.DC.AreaY_A_Range.ToString();
                        textBox330.Text = record.DC.AreaY_B_Range.ToString();
                        textBox329.Text = record.DC.AreaY_C_Range.ToString();
                        textBox328.Text = record.DC.AreaY_D_Range.ToString();
                        textBox327.Text = record.DC.AreaY_E_Range.ToString();

                        textBox326.Text = record.DC.Length_Base.ToString();
                        textBox325.Text = record.DC.Length_A_Range.ToString();
                        textBox324.Text = record.DC.Length_B_Range.ToString();
                        textBox323.Text = record.DC.Length_C_Range.ToString();
                        textBox322.Text = record.DC.Length_D_Range.ToString();
                        textBox321.Text = record.DC.Length_E_Range.ToString();


                        textBox320.Text = record.DC.Height_Base.ToString();
                        textBox319.Text = record.DC.Height_A_Range.ToString();
                        textBox318.Text = record.DC.Height_B_Range.ToString();
                        textBox317.Text = record.DC.Height_C_Range.ToString();
                        textBox300.Text = record.DC.Height_D_Range.ToString();
                        textBox299.Text = record.DC.Height_E_Range.ToString();


                        textBox298.Text = record.DC.Area_Base.ToString();
                        textBox297.Text = record.DC.Area_A_Range.ToString();
                        textBox280.Text = record.DC.Area_B_Range.ToString();
                        textBox279.Text = record.DC.Area_C_Range.ToString();
                        textBox278.Text = record.DC.Area_D_Range.ToString();
                        textBox277.Text = record.DC.Area_E_Range.ToString();

                        textBox260.Text = record.DC.Distance_Base.ToString();
                        textBox259.Text = record.DC.Distance_A_Range.ToString();
                        textBox258.Text = record.DC.Distance_B_Range.ToString();
                        textBox257.Text = record.DC.Distance_C_Range.ToString();
                        textBox240.Text = record.DC.Distance_D_Range.ToString();
                        textBox239.Text = record.DC.Distance_E_Range.ToString();


                    }
                }
            }
            catch (JsonException ex)
            {

                Logger.LogError("RangeSetting.json", $"JSON 파일의 형식이 잘못되었거나 손상되었습니다.\n\n오류 내용: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogError("RangeSetting.json", $"파일 읽기 권한이 없습니다.\n\n오류 내용: {ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.LogError("RangeSetting.json", $"입출력 작업 중 문제가 발생했습니다.\n\n오류 내용: {ex.Message}");
            }
            catch (Exception ex)
            {

                Logger.LogError("RangeSetting.json", $"알 수 없는 예외가 발생했습니다.\n\n오류 내용: {ex.Message}");
            }
            ModelGradePanelCount = 1;
            ModelGraderRangeSettingPanel.Size = new Size(1255, 795);
            ModelGraderRangeSettingPanel.Location = new Point(0, 0);
            ModelGraderRangeSettingPanel.Visible = true;
            ModelGraderRangeSettingPanel.BringToFront();



            //하위 패널 디스플레이 하기
            ModelGradeSttingPanelDisplayUpdate();


        }

        private void ModelGradeDownbtr_Click(object sender, EventArgs e)
        {
            ModelGradePanelCount--;
            if (ModelGradePanelCount <= 0)
            {
                ModelGradePanelCount = 1;
            }
            ModelGradeSttingPanelDisplayUpdate();
        }

        private void ModelGRUPbtr_Click(object sender, EventArgs e)
        {
            ModelGradePanelCount++;
            if (ModelGradePanelCount >= 3)
            {
                ModelGradePanelCount = 2;
            }
            ModelGradeSttingPanelDisplayUpdate();
        }
        private void ModelGradeSttingPanelDisplayUpdate()
        {
            Control[] founds = this.Controls.Find($"ModelGraderRangeSettingPanel_{ModelGradePanelCount}", true);
            if (founds.Length > 0 && founds[0] is Panel targetPanel)
            {
                // 찾은 패널 처리
                targetPanel.Location = new Point(3, 3);
                targetPanel.Size = new Size(1249, 730);
                targetPanel.Visible = true;
                targetPanel.BringToFront();
            }
        }

        private void ModelGradeRabgeSavebtr_Click(object sender, EventArgs e)
        {
            if (!TryValidateModelRangeStiingLineInputs(out TextBox? invalidTextBox))
            {
                MessageBox.Show(
                    "입력되지 않았거나 숫자가 아닌 항목이 있습니다.\n모든 기준치를 입력한 뒤 다시 저장해주세요.",
                    "입력 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                if (invalidTextBox != null)
                {
                    BringModelBaseLineTextBoxIntoView(invalidTextBox);
                    invalidTextBox.Focus();
                }

                return;
            }


            DialogResult result = MessageBox.Show(
                 "입력을 완료 할까요?", // 본문 내용
                 "확인 요청",                           // 상자 제목
                 MessageBoxButtons.OKCancel,            // 확인/취소 버튼
                 MessageBoxIcon.Question                // 물음표 아이콘
            );

            if (result == DialogResult.Cancel)
            {
                return;
            }

                //패널 안에 모든 텍스트 박스가 비어 있는지 확인 하는 코드를 넣어야함
                //패널안 모든 텍스트 박스에 double형 데이터만 들어가도록 세싱하는 방법

                //1. 저장되어야할 데이터들 만들기
                var record = new ModelRangeJson();
            record.AC.PeakX_Base = double.Parse(textBox1.Text);
            record.AC.PeakX_A_Range = double.Parse(textBox2.Text);
            record.AC.PeakX_B_Range = double.Parse(textBox3.Text);
            record.AC.PeakX_C_Range = double.Parse(textBox4.Text);
            record.AC.PeakX_D_Range = double.Parse(textBox8.Text);
            record.AC.PeakX_E_Range = double.Parse(textBox12.Text);
            record.AC.PeakY_Base = double.Parse(textBox58.Text);
            record.AC.PeakY_A_Range = double.Parse(textBox57.Text);
            record.AC.PeakY_B_Range = double.Parse(textBox40.Text);
            record.AC.PeakY_C_Range = double.Parse(textBox39.Text);
            record.AC.PeakY_D_Range = double.Parse(textBox38.Text);
            record.AC.PeakY_E_Range = double.Parse(textBox37.Text);
            record.AC.AreaX_Base = double.Parse(textBox80.Text);
            record.AC.AreaX_A_Range = double.Parse(textBox79.Text);
            record.AC.AreaX_B_Range = double.Parse(textBox78.Text);
            record.AC.AreaX_C_Range = double.Parse(textBox77.Text);
            record.AC.AreaX_D_Range = double.Parse(textBox60.Text);
            record.AC.AreaX_E_Range = double.Parse(textBox59.Text);
            record.AC.AreaY_Base = double.Parse(textBox118.Text);
            record.AC.AreaY_A_Range = double.Parse(textBox117.Text);
            record.AC.AreaY_B_Range = double.Parse(textBox100.Text);
            record.AC.AreaY_C_Range = double.Parse(textBox99.Text);
            record.AC.AreaY_D_Range = double.Parse(textBox98.Text);
            record.AC.AreaY_E_Range = double.Parse(textBox97.Text);
            record.AC.Length_Base = double.Parse(textBox238.Text);
            record.AC.Length_A_Range = double.Parse(textBox237.Text);
            record.AC.Length_B_Range = double.Parse(textBox220.Text);
            record.AC.Length_C_Range = double.Parse(textBox219.Text);
            record.AC.Length_D_Range = double.Parse(textBox218.Text);
            record.AC.Length_E_Range = double.Parse(textBox217.Text);
            record.AC.Height_Base = double.Parse(textBox200.Text);
            record.AC.Height_A_Range = double.Parse(textBox199.Text);
            record.AC.Height_B_Range = double.Parse(textBox198.Text);
            record.AC.Height_C_Range = double.Parse(textBox197.Text);
            record.AC.Height_D_Range = double.Parse(textBox180.Text);
            record.AC.Height_E_Range = double.Parse(textBox179.Text);
            record.AC.Area_Base = double.Parse(textBox178.Text);
            record.AC.Area_A_Range = double.Parse(textBox177.Text);
            record.AC.Area_B_Range = double.Parse(textBox160.Text);
            record.AC.Area_C_Range = double.Parse(textBox159.Text);
            record.AC.Area_D_Range = double.Parse(textBox158.Text);
            record.AC.Area_E_Range = double.Parse(textBox157.Text);
            record.AC.Distance_Base = double.Parse(textBox140.Text);
            record.AC.Distance_A_Range = double.Parse(textBox139.Text);
            record.AC.Distance_B_Range = double.Parse(textBox138.Text);
            record.AC.Distance_C_Range = double.Parse(textBox137.Text);
            record.AC.Distance_D_Range = double.Parse(textBox120.Text);
            record.AC.Distance_E_Range = double.Parse(textBox119.Text);
            record.DC.PeakX_Base = double.Parse(textBox350.Text);
            record.DC.PeakX_A_Range = double.Parse(textBox349.Text);
            record.DC.PeakX_B_Range = double.Parse(textBox348.Text);
            record.DC.PeakX_C_Range = double.Parse(textBox347.Text);
            record.DC.PeakX_D_Range = double.Parse(textBox346.Text);
            record.DC.PeakX_E_Range = double.Parse(textBox345.Text);
            record.DC.PeakY_Base = double.Parse(textBox344.Text);
            record.DC.PeakY_A_Range = double.Parse(textBox343.Text);
            record.DC.PeakY_B_Range = double.Parse(textBox342.Text);
            record.DC.PeakY_C_Range = double.Parse(textBox341.Text);
            record.DC.PeakY_D_Range = double.Parse(textBox340.Text);
            record.DC.PeakY_E_Range = double.Parse(textBox339.Text);
            record.DC.AreaX_Base = double.Parse(textBox338.Text);
            record.DC.AreaX_A_Range = double.Parse(textBox337.Text);
            record.DC.AreaX_B_Range = double.Parse(textBox336.Text);
            record.DC.AreaX_C_Range = double.Parse(textBox335.Text);
            record.DC.AreaX_D_Range = double.Parse(textBox334.Text);
            record.DC.AreaX_E_Range = double.Parse(textBox333.Text);
            record.DC.AreaY_Base = double.Parse(textBox332.Text);
            record.DC.AreaY_A_Range = double.Parse(textBox331.Text);
            record.DC.AreaY_B_Range = double.Parse(textBox330.Text);
            record.DC.AreaY_C_Range = double.Parse(textBox329.Text);
            record.DC.AreaY_D_Range = double.Parse(textBox328.Text);
            record.DC.AreaY_E_Range = double.Parse(textBox327.Text);
            record.DC.Length_Base = double.Parse(textBox326.Text);
            record.DC.Length_A_Range = double.Parse(textBox325.Text);
            record.DC.Length_B_Range = double.Parse(textBox324.Text);
            record.DC.Length_C_Range = double.Parse(textBox323.Text);
            record.DC.Length_D_Range = double.Parse(textBox322.Text);
            record.DC.Length_E_Range = double.Parse(textBox321.Text);
            record.DC.Height_Base = double.Parse(textBox320.Text);
            record.DC.Height_A_Range = double.Parse(textBox319.Text);
            record.DC.Height_B_Range = double.Parse(textBox318.Text);
            record.DC.Height_C_Range = double.Parse(textBox317.Text);
            record.DC.Height_D_Range = double.Parse(textBox300.Text);
            record.DC.Height_E_Range = double.Parse(textBox299.Text);
            record.DC.Area_Base = double.Parse(textBox298.Text);
            record.DC.Area_A_Range = double.Parse(textBox297.Text);
            record.DC.Area_B_Range = double.Parse(textBox280.Text);
            record.DC.Area_C_Range = double.Parse(textBox279.Text);
            record.DC.Area_D_Range = double.Parse(textBox278.Text);
            record.DC.Area_E_Range = double.Parse(textBox277.Text);
            record.DC.Distance_Base = double.Parse(textBox260.Text);
            record.DC.Distance_A_Range = double.Parse(textBox259.Text);
            record.DC.Distance_B_Range = double.Parse(textBox258.Text);
            record.DC.Distance_C_Range = double.Parse(textBox257.Text);
            record.DC.Distance_D_Range = double.Parse(textBox240.Text);
            record.DC.Distance_E_Range = double.Parse(textBox239.Text);


            //만들어진 데이터 선택된 모델에 저장!
            var rootData = new Dictionary<string, ModelRangeJson>
            {
                { seletedcmodel, record }
            };

            // 3. JSON 옵션 설정 (들여쓰기 적용 및 한글 깨짐 방지 등)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, // 보기 좋게 들여쓰기 (Pretty Print)
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };


            // 5. 파일로 저장
            string filePath = "./RangeSetting.json";
            try
            {
                // JSON 문자열로 변환
                string jsonString = JsonSerializer.Serialize(rootData, options);

                // 파일로 저장
                File.WriteAllText(filePath, jsonString);
                MessageBox.Show(
                $"JSON 파일이 성공적으로 저장되었습니다.\n경로: {Path.GetFullPath(filePath)}",
                "저장 성공",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
                );

                Logger.LogInfo("RangeSetting.json", $"JSON 파일이 성공적으로 저장되었습니다: {Path.GetFullPath(filePath)}");
                ModelGraderRangeSettingPanel.Visible= false;
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                
                Logger.LogInfo("RangeSetting.json", $"JSON 파일 저장 실패 쓰기 권한 없음 :{ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.LogInfo("RangeSetting.json", $"JSON 파일 저장 실패 지정한 디렉토리 찾을 수 없음 :{ex.Message}");
               
            }
            catch (IOException ex)
            {
             
                Logger.LogInfo("RangeSetting.json", $"JSON 파일 저장 실패 입출력 에러 발생 :{ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogInfo("RangeSetting.json", $"JSON 파일 저장 실패 알수없는 예외 발생 :{ex.Message}");            
            }

            MessageBox.Show(
                $"RangeSetting.json파일 저장시 오류 발생 \n로그를 확인하세요",
                "json파일 저장 에러",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private void ModelGradeRangeSettingExitbtr_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
             "모델에 대한 등급 범위 설정을 종료 할까요?", // 본문 내용
             "확인 요청",                           // 상자 제목
             MessageBoxButtons.OKCancel,            // 확인/취소 버튼
             MessageBoxIcon.Question                // 물음표 아이콘
            );

            // 2. 사용자의 선택 결과 처리
            if (result == DialogResult.OK)
            {
                ClearAllTextBoxes(ModelBaseLineSettingPanel);
                ModelGradePanelCount = 1;
                ModelGraderRangeSettingPanel.Visible = false;
            }
            else if (result == DialogResult.Cancel)
            {
                return;
            }


        }

        private bool TryValidateModelRangeStiingLineInputs(out TextBox? invalidTextBox)
        {
            invalidTextBox = null;

            foreach (TextBox textBox in EnumerateTextBoxes(ModelGraderRangeSettingPanel))
            {
                string value = textBox.Text.Trim();
                if (string.IsNullOrEmpty(value) || !double.TryParse(value, out _))
                {
                    invalidTextBox = textBox;
                    return false;
                }
            }

            return true;
        }
    }
}


