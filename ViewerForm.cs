
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.LinkLabel;

namespace WIA_ViewerProgram
{

    public partial class ViewerForm : Form
    {
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

        private List<string> _ListFrontPath = new();
        private List<string> _ListRearPath = new();
        OpenCVManager _CV = new OpenCVManager();
        List<string> FtpDateModelPath = new List<string>();
        private readonly List<ListRowScanEntry> _listRowEntries = new();
        /// <summary>목록 BCR 셀 말줄임 시 전체 경로를 표시합니다.</summary>
        private readonly ToolTip _listBcrCellToolTip = new ToolTip();

        private HistoryManager.HistroyManager Logger => HistoryManager.HistroyManager.Instance;

        public ViewerForm()
        {
            SingleStaticPanelCount = 1;
            InitializeComponent();
            Disposed += (_, _) => _listBcrCellToolTip.Dispose();

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
                1 => "단일 통계 [1]",
                2 => "단일 통계 [2]",
                3 => "단일 통계 [3]",
                4 => "단일 통계 [4]",
                _ => $"-"
            };

            if (SingleStaticPanelCount == 1)
            {
                SingleStaticPanel_1.Location = new Point(0, 70);
                SingleStaticPanel_1.Size = new Size(1700, 746);
                SingleStaticPanel_1.BackColor = Color.Black;
                SingleStaticPanel_1.Visible = true;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_4.Visible = false;
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
            }
            else if (SingleStaticPanelCount == 4)
            {
                SingleStaticPanel_4.Location = new Point(0, 70);
                SingleStaticPanel_4.Size = new Size(1700, 746);
                SingleStaticPanel_4.BackColor = Color.Black;
                SingleStaticPanel_4.Visible = true;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_2.Visible = false;
                SingleStaticPanel_1.Visible = false;
            }
            else
            {
                SingleStaticPanel_1.Visible = false;
                SingleStaticPanel_3.Visible = false;
                SingleStaticPanel_2.Visible = false;
            }
        }


        private void SingleStaticPanelCountUpButton_Click(object? sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            if (SingleStaticPanelCount >= 5)
            {
                SingleStaticPanelCount = 4;
                return;
            }

            SingleStaticPanelCount++;
            if (1 <= SingleStaticPanelCount && SingleStaticPanelCount <= 4)
                ApplySingleStaticPanelCountUI();
        }

        private void SingleStaticPanelCountDownButton_Click(object? sender, EventArgs e)
        {
            if (!EnsureLoggedIn())
            {
                return;
            }

            if (SingleStaticPanelCount <= 0)
            {
                SingleStaticPanelCount = 0;
                return;
            }

            SingleStaticPanelCount--;
            if (1 <= SingleStaticPanelCount && SingleStaticPanelCount <= 4)
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
            FrontDisplayPanel.Visible = true;
            RearDisplayPanel.Visible = true;

            // 우선순위 보이기
            SicngleImgCheckPanel.BringToFront();
            FrontDisplayPanel.BringToFront();
            RearDisplayPanel.BringToFront();

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
            // 기어개수가 43개라면!
            // 레시피에 따라 맞춰 줘야하는 개 달라지므로 이걸 유념해야함!
            int r1_Count = (int.Parse(R1.Text) + 20 % 43) + 1;
            int r2_Count = (int.Parse(R2.Text) + 20 % 43) + 1;
            int r3_Count = (int.Parse(R3.Text) + 20 % 43) + 1;
            int r4_Count = (int.Parse(R4.Text) + 20 % 43) + 1;
            int r5_Count = (int.Parse(R5.Text) + 20 % 43) + 1;
            if (r1_Count >= 10)
            {
                r1Path = Path.Combine(RearPath, $"{r1_Count}.jpg");
            }
            else
            {
                r1Path = Path.Combine(RearPath, $"0{r1_Count}.jpg");
            }

            if (r2_Count >= 10)
            {
                r2Path = Path.Combine(RearPath, $"{r2_Count}.jpg");
            }
            else
            {
                r2Path = Path.Combine(RearPath, $"0{r2_Count}.jpg");
            }

            if (r3_Count >= 10)
            {
                r3Path = Path.Combine(RearPath, $"{r3_Count}.jpg");
            }
            else
            {
                r3Path = Path.Combine(RearPath, $"0{r3_Count}.jpg");
            }

            if (r4_Count >= 10)
            {
                r4Path = Path.Combine(RearPath, $"{r4_Count}.jpg");
            }
            else
            {
                r4Path = Path.Combine(RearPath, $"0{r4_Count}.jpg");
            }

            if (r5_Count >= 10)
            {
                r5Path = Path.Combine(RearPath, $"{r5_Count}.jpg");
            }
            else
            {
                r5Path = Path.Combine(RearPath, $"0{r5_Count}.jpg");
            }

            if (!TryLoadPicture(F1picturebox, f1Path, "Front F1")) missingImages.Add(f1Path);
            if (!TryLoadPicture(F2picturebox, f2Path, "Front F2")) missingImages.Add(f2Path);
            if (!TryLoadPicture(F3picturebox, f3Path, "Front F3")) missingImages.Add(f3Path);
            if (!TryLoadPicture(F4picturebox, f4Path, "Front F4")) missingImages.Add(f4Path);
            if (!TryLoadPicture(F5picturebox, f5Path, "Front F5")) missingImages.Add(f5Path);
            if (!TryLoadPicture(R1picturebox, r1Path, "Rear R1")) missingImages.Add(r1Path);
            if (!TryLoadPicture(R2picturebox, r2Path, "Rear R2")) missingImages.Add(r2Path);
            if (!TryLoadPicture(R3picturebox, r3Path, "Rear R3")) missingImages.Add(r3Path);
            if (!TryLoadPicture(R4picturebox, r4Path, "Rear R4")) missingImages.Add(r4Path);
            if (!TryLoadPicture(R5picturebox, r5Path, "Rear R5")) missingImages.Add(r5Path);

            if (missingImages.Count > 0)
            {
                ShowMissingFileWarning("이미지 파일 없음", missingImages);
            }

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
                        SetLabelTextRecursive($"F{i + 1}AreaX", frontData.Length > 0 ? frontData[0] : "-");
                        SetLabelTextRecursive($"F{i + 1}AreaY", frontData.Length > 1 ? frontData[1] : "-");
                        SetLabelTextRecursive($"F{i + 1}PeakX", frontData.Length > 2 ? frontData[2] : "-");
                        SetLabelTextRecursive($"F{i + 1}PeakY", frontData.Length > 3 ? frontData[3] : "-");
                        SetLabelTextRecursive($"F{i + 1}Width", frontData.Length > 4 ? frontData[4] : "-");
                        SetLabelTextRecursive($"F{i + 1}Height", frontData.Length > 5 ? frontData[5] : "-");
                        SetLabelTextRecursive($"F{i + 1}Area", frontData.Length > 6 ? frontData[6] : "-");
                        SetLabelTextRecursive($"F{i + 1}Angle", frontData.Length > 7 ? frontData[7] : "-");
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
                        SetLabelTextRecursive($"R{i + 1}AreaX", rearData.Length > 0 ? rearData[0] : "-");
                        SetLabelTextRecursive($"R{i + 1}AreaY", rearData.Length > 1 ? rearData[1] : "-");
                        SetLabelTextRecursive($"R{i + 1}PeakX", rearData.Length > 2 ? rearData[2] : "-");
                        SetLabelTextRecursive($"R{i + 1}PeakY", rearData.Length > 3 ? rearData[3] : "-");
                        SetLabelTextRecursive($"R{i + 1}Width", rearData.Length > 4 ? rearData[4] : "-");
                        SetLabelTextRecursive($"R{i + 1}Height", rearData.Length > 5 ? rearData[5] : "-");
                        SetLabelTextRecursive($"R{i + 1}Area", rearData.Length > 6 ? rearData[6] : "-");
                        SetLabelTextRecursive($"R{i + 1}Angle", rearData.Length > 7 ? rearData[7] : "-");
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
                    $"{changemode}의 ID/PW 변경 완료",
                    "ID/PW변경 완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Question);
                _LoginManager.pwchane();
                IDPWChangePanel.Visible = false;
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
            for (int i = 0; i < dateCount; i++)
            {
                _listRowEntries.AddRange(ScanBcrTrialRowsUnderDateModelRoot(dateStrArray[i], FtpDateModelPath[i]));
            }

            _listRowEntries.Sort((a, b) =>
            {
                int cmp = string.CompareOrdinal(a.DateStr, b.DateStr);
                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = string.Compare(a.BcrFolderName, b.BcrFolderName, StringComparison.OrdinalIgnoreCase);
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
                };
                rowPanel.Controls.Add(listSelectLabel);
                rowPanel.Controls.Add(CreateCellLabel($"ListEvaluateResult{rowIndex}", "", new Point(colX["ListEvaluateResult"], 0), colSize["ListEvaluateResult"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListBCR{rowIndex}", entry.BcrFolderName, new Point(colX["ListBCR"], 0), colSize["ListBCR"], rowBackColor, rowForeColor, autoEllipsis: true, toolTipText: entry.BcrFolderName ?? ""));
                rowPanel.Controls.Add(CreateCellLabel($"ListType{rowIndex}", listTypeText, new Point(colX["ListType"], 0), colSize["ListType"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListDriveTrain{rowIndex}", driveTrainText, new Point(colX["ListDriveTrain"], 0), colSize["ListDriveTrain"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListAutoSpec{rowIndex}", autoSpecText, new Point(colX["ListAutoSpec"], 0), colSize["ListAutoSpec"], rowBackColor, rowForeColor));
                rowPanel.Controls.Add(CreateCellLabel($"ListDate{rowIndex}", entry.DateStr, new Point(colX["ListDate"], 0), colSize["ListDate"], rowBackColor, rowForeColor));
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


            SingleStaticLabel.ForeColor = Color.Black;
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
            float[] FSingleWidth = new float[FrontHallMaxCount];
            float[] FSingleHeight = new float[FrontHallMaxCount];
            float[] FSingleArea = new float[FrontHallMaxCount];
            float[] FSPatternX = new float[FrontHallMaxCount];
            float[] FSPatternY = new float[FrontHallMaxCount];

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
                        || !TryParseCsvFloat(values[3], out FSingleWidth[count])
                        || !TryParseCsvFloat(values[4], out FSingleHeight[count])
                        || !TryParseCsvFloat(values[5], out FSingleArea[count])
                        || !TryParseCsvFloat(values[6], out FSPatternX[count])
                        || !TryParseCsvFloat(values[7], out FSPatternY[count])
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
            float[] RSingleWidth = new float[RearHallMaxCount];
            float[] RSingleHeight = new float[RearHallMaxCount];
            float[] RSingleArea = new float[RearHallMaxCount];
            float[] RSPatternX = new float[RearHallMaxCount];
            float[] RSPatternY = new float[RearHallMaxCount];


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
                        || !TryParseCsvFloat(values[3], out RSingleWidth[count])
                        || !TryParseCsvFloat(values[4], out RSingleHeight[count])
                        || !TryParseCsvFloat(values[5], out RSingleArea[count])
                        || !TryParseCsvFloat(values[6], out RSPatternX[count])
                        || !TryParseCsvFloat(values[7], out RSPatternY[count])
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


            PlotIndexScatter(AccelerationPeakX, FSinglePeakX, frontPointCount, "Acceleration Peak X");
            PlotIndexScatter(AccelerationPeakY, FSinglePeakY, frontPointCount, "Acceleration Peak Y");
            PlotIndexScatter(DecelerationPeakX, RSinglePeakX, rearPointCount, "Deceleration Peak X");
            PlotIndexScatter(DecelerationPeakY, RSinglePeakY, rearPointCount, "Deceleration Peak Y");

            PlotIndexScatter(AccelerationPatternX, FSPatternX, frontPointCount, "Acceleration Pattern X");
            PlotIndexScatter(AccelerationPatternY, FSPatternY, frontPointCount, "Acceleration Pattern Y");
            PlotIndexScatter(DecelerationPatternX, RSPatternX, rearPointCount, "Deceleration Pattern X");
            PlotIndexScatter(DecelerationPatternY, RSPatternY, rearPointCount, "Deceleration Pattern Y");


            PlotIndexScatter(AccelerationWidth, FSingleWidth, frontPointCount, "Acceleration Width");
            PlotIndexScatter(AccelerationHeight, FSingleHeight, frontPointCount, "Acceleration Height");
            PlotIndexScatter(AccelerationArea, FSingleArea, frontPointCount, "Acceleration Area");
            PlotIndexScatter(DecelerationWidth, RSingleWidth, rearPointCount, "Deceleration Width");
            PlotIndexScatter(DecelerationHeight, RSingleHeight, rearPointCount, "Deceleration Height");
            PlotIndexScatter(DecelerationarArea, RSingleArea, rearPointCount, "Decelerationar Area");

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
                ACPeakX_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                ACPeakX_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                ACPeakX__nugeock.Text = double.Parse(Values[3]).ToString("F2");
                ACPeakX_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                ACPeakX_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[3].Split(',');
                ACPeakY_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                ACPeakY_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                ACPeakY__nugeock.Text = double.Parse(Values[3]).ToString("F2");
                ACPeakY_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                ACPeakY_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[5].Split(',');
                ACWidth_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                ACWidth_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                ACWidth_Sum.Text = double.Parse(Values[3]).ToString("F2");
                ACWidth_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                ACWidth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[7].Split(',');
                ACHeigth_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                ACHeigth_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                ACHeigth_Sum.Text = double.Parse(Values[3]).ToString("F2");
                ACHeigth_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                ACHeigth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[9].Split(',');
                ACArea_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                ACArea_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                ACArea_Sum.Text = double.Parse(Values[3]).ToString("F2");
                ACArea_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                ACArea_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[11].Split(',');
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
                DCPeakX_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                DCPeakX_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                DCPeakX__nugeock.Text = double.Parse(Values[3]).ToString("F2");
                DCPeakX_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                DCPeakX_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[3].Split(',');
                DCPeakY_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                DCPeakY_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                DCPeakY__nugeock.Text = double.Parse(Values[3]).ToString("F2");
                DCPeakY_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                DCPeakY_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[5].Split(',');
                DCWidth_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                DCWidth_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                DCWidth_Sum.Text = double.Parse(Values[3]).ToString("F2");
                DCWidth_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                DCWidth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[7].Split(',');
                DCHeigth_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                DCHeigth_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                DCHeigth_Sum.Text = double.Parse(Values[3]).ToString("F2");
                DCHeigth_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                DCHeigth_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[9].Split(',');
                DCArea_MaxOne.Text = double.Parse(Values[1]).ToString("F2");
                DCArea_MaxInterval.Text = double.Parse(Values[2]).ToString("F2");
                DCArea_Sum.Text = double.Parse(Values[3]).ToString("F2");
                DCArea_ROUT.Text = double.Parse(Values[4]).ToString("F2");
                DCArea_Grade.Text = Values[5] + $"\n[{double.Parse(Values[6]).ToString()}]";

                Values = lines[11].Split(',');
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

            //단일치 g1~g5 그레이드 개수 표기
            //FSinglePeakX
            //FSinglePeakY
            //RSinglePeakX
            //RSinglePeakY
            //FrontHallMaxCount
            //RearHallMaxCount
            int[] ACpeakxMaxGradeCount = new int[5];
            int[] ACpeakyMaxGradeCount = new int[5];
            int[] DCpeakxMaxGradeCount = new int[5];
            int[] DCpeakyMaxGradeCount = new int[5];

            foreach (double ACpeakX in FSinglePeakX)
            {
                double Value = Math.Abs(ACpeakX - FSinglePeakX.Average());
                if (Value >= 38.1)
                {
                    ACpeakxMaxGradeCount[4]++;
                }
                else if (Value >= 33.2)
                {
                    ACpeakxMaxGradeCount[3]++;
                }
                else if (Value >= 28.9)
                {
                    ACpeakxMaxGradeCount[2]++;
                }
                else if (Value >= 25.1)
                {
                    ACpeakxMaxGradeCount[1]++;
                }
                else
                {
                    ACpeakxMaxGradeCount[0]++;
                }
            }

            foreach (double ACpeaky in FSinglePeakY)
            {
                double Value = Math.Abs(ACpeaky - FSinglePeakY.Average());
                if (Value >= 9.2)
                {
                    ACpeakyMaxGradeCount[4]++;
                }
                else if (Value >= 8.0)
                {
                    ACpeakyMaxGradeCount[3]++;
                }
                else if (Value >= 7.0)
                {
                    ACpeakyMaxGradeCount[2]++;
                }
                else if (Value >= 6.1)
                {
                    ACpeakyMaxGradeCount[1]++;
                }
                else
                {
                    ACpeakyMaxGradeCount[0]++;
                }
            }

            foreach (double DCpeakX in RSinglePeakX)
            {
                double Value = Math.Abs(DCpeakX - RSinglePeakX.Average());
                if (Value >= 18.4)
                {
                    DCpeakxMaxGradeCount[4]++;
                }
                else if (Value >= 16.0)
                {
                    DCpeakxMaxGradeCount[3]++;
                }
                else if (Value >= 13.9)
                {
                    DCpeakxMaxGradeCount[2]++;
                }
                else if (Value >= 12.1)
                {
                    DCpeakxMaxGradeCount[1]++;
                }
                else
                {
                    DCpeakxMaxGradeCount[0]++;
                }
            }

            foreach (double DCpeaky in RSinglePeakY)
            {

                double Value = Math.Abs(DCpeaky - RSinglePeakY.Average());
                if (Value >= 7.7)
                {
                    DCpeakyMaxGradeCount[4]++;
                }
                else if (Value >= 6.7)
                {
                    DCpeakyMaxGradeCount[3]++;
                }
                else if (Value >= 5.9)
                {
                    DCpeakyMaxGradeCount[2]++;
                }
                else if (Value >= 5.1)
                {
                    DCpeakyMaxGradeCount[1]++;
                }
                else
                {
                    DCpeakyMaxGradeCount[0]++;
                }
            }

            label274.Text = ACpeakxMaxGradeCount[0].ToString();
            label272.Text = ACpeakxMaxGradeCount[1].ToString();
            label270.Text = ACpeakxMaxGradeCount[2].ToString();
            label244.Text = ACpeakxMaxGradeCount[3].ToString();
            label268.Text = ACpeakxMaxGradeCount[4].ToString();

            label284.Text = ACpeakyMaxGradeCount[0].ToString();
            label282.Text = ACpeakyMaxGradeCount[1].ToString();
            label280.Text = ACpeakyMaxGradeCount[2].ToString();
            label276.Text = ACpeakyMaxGradeCount[3].ToString();
            label278.Text = ACpeakyMaxGradeCount[4].ToString();

            label294.Text = (ACpeakyMaxGradeCount[0] + ACpeakxMaxGradeCount[0]).ToString();
            label292.Text = (ACpeakyMaxGradeCount[1] + ACpeakxMaxGradeCount[1]).ToString();
            label290.Text = (ACpeakyMaxGradeCount[2] + ACpeakxMaxGradeCount[2]).ToString();
            label286.Text = (ACpeakyMaxGradeCount[3] + ACpeakxMaxGradeCount[3]).ToString();
            label288.Text = (ACpeakyMaxGradeCount[4] + ACpeakxMaxGradeCount[4]).ToString();

            label315.Text = DCpeakxMaxGradeCount[0].ToString();
            label314.Text = DCpeakxMaxGradeCount[1].ToString();
            label313.Text = DCpeakxMaxGradeCount[2].ToString();
            label311.Text = DCpeakxMaxGradeCount[3].ToString();
            label312.Text = DCpeakxMaxGradeCount[4].ToString();
            label310.Text = DCpeakyMaxGradeCount[0].ToString();
            label309.Text = DCpeakyMaxGradeCount[1].ToString();
            label306.Text = DCpeakyMaxGradeCount[2].ToString();
            label304.Text = DCpeakyMaxGradeCount[3].ToString();
            label305.Text = DCpeakyMaxGradeCount[4].ToString();
            label303.Text = (DCpeakyMaxGradeCount[0] + DCpeakxMaxGradeCount[0]).ToString();
            label302.Text = (DCpeakyMaxGradeCount[1] + DCpeakxMaxGradeCount[1]).ToString();
            label300.Text = (DCpeakyMaxGradeCount[2] + DCpeakxMaxGradeCount[2]).ToString();
            label296.Text = (DCpeakyMaxGradeCount[3] + DCpeakxMaxGradeCount[3]).ToString();
            label298.Text = (DCpeakyMaxGradeCount[4] + DCpeakxMaxGradeCount[4]).ToString();

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
            int DCPeakxOutlierCount = 0;
            int DCPeakyOutlierCount = 0;
            int DCWidthOutlierCount = 0;
            int DCHeightOutlierCount = 0;
            int DCAreaOutlierCount = 0;
            //이상치 기준!
            double addRatio = 0.7;

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
            label328.Text = (ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount).ToString();
            label327.Text = (((ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount) / ((double)5 * (double)FSinglePeakX.Length)) * 100).ToString("F2");

            label342.Text = DCPeakxOutlierCount.ToString();
            label341.Text = DCPeakyOutlierCount.ToString();
            label340.Text = DCWidthOutlierCount.ToString();
            label339.Text = DCHeightOutlierCount.ToString();
            label338.Text = DCAreaOutlierCount.ToString();
            label336.Text = (DCPeakxOutlierCount + DCPeakyOutlierCount + DCWidthOutlierCount + DCHeightOutlierCount + DCAreaOutlierCount).ToString();
            label335.Text = (((DCPeakxOutlierCount + DCPeakyOutlierCount + DCWidthOutlierCount + DCHeightOutlierCount + DCAreaOutlierCount) / ((double)5 * (double)RSinglePeakX.Length)) * 100).ToString("F2");




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



            var scatterValue=plot.Plot.Add.Scatter(xs, ys);
            plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            plot.Plot.Axes.Bottom.Label.Text = "Index";
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
                bar.Label = bar.Value.ToString("F1");
            }
            plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            plot.Plot.Axes.Bottom.Label.Text = "Index";
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
            float[] ACPeakXMAD = new float[selectedRowEntries.Count];
            float[] ACPeakYMAD = new float[selectedRowEntries.Count];
            float[] ACWidthMAD = new float[selectedRowEntries.Count];
            float[] ACHeightMAD = new float[selectedRowEntries.Count];
            float[] ACAreaMAD = new float[selectedRowEntries.Count];

            float[] DCPeakXScores = new float[selectedRowEntries.Count];
            float[] DCPeakYScores = new float[selectedRowEntries.Count];
            float[] DCWidthScores = new float[selectedRowEntries.Count];
            float[] DCHeightScores = new float[selectedRowEntries.Count];
            float[] DCAreaScores = new float[selectedRowEntries.Count];
            float[] DCPeakXMAD = new float[selectedRowEntries.Count];
            float[] DCPeakYMAD = new float[selectedRowEntries.Count];
            float[] DCWidthMAD = new float[selectedRowEntries.Count];
            float[] DCHeightMAD = new float[selectedRowEntries.Count];
            float[] DCAreaMAD = new float[selectedRowEntries.Count];

            int[] FinalGradeCount=new int[5];
            int[] ACGradeCount = new int[5];
            int[] DCGradeCount = new int[5];
            double AC_DCFinalScore = 0;
            double ACAddRatio = 0.5;

            foreach (var rowEntry in selectedRowEntries)
            {
                AC_DCFinalScore = 0;
                count++;
                Label Countlabel = new Label();
                Countlabel.Text = $"{count}";
                Countlabel.Name = $"CoutLabel{count}";
                Countlabel.AutoSize = false;
                Countlabel.Size = new Size(75, 57);
                Countlabel.Visible = true;
                Countlabel.ForeColor = Color.White;
                Countlabel.BackColor = Color.FromArgb(64, 64, 64);
                Countlabel.TextAlign = ContentAlignment.MiddleCenter;
                Countlabel.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                Countlabel.Margin = new Padding(3, 4, 3, 3);
                PlurerFlowPanel1.Controls.Add(Countlabel);



                Size Plurer2LabelSize = new Size(90, 57);

                //SNO 라벨 생성 데이터는 음...?
                Label SNolabel = new Label();
                SNolabel.Text = $"S/NO{count}";
                SNolabel.Name = $"SNOLabel{count}"; // 이후 수정해야함
                SNolabel.AutoSize = false;
                SNolabel.Size = new Size(131, 57);
                SNolabel.Visible = true;
                SNolabel.ForeColor = Color.White;
                SNolabel.BackColor = Color.FromArgb(64, 64, 64);
                SNolabel.TextAlign = ContentAlignment.MiddleCenter;
                SNolabel.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                SNolabel.Margin = new Padding(3, 4, 3, 3);
                PlurerFlowPanel1.Controls.Add(SNolabel);
                //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                Label Countlabel2 = new Label();
                Countlabel2.Text = $"{count}";
                Countlabel2.Name = $"CoutLabel{count}";
                Countlabel2.AutoSize = false;
                Countlabel2.Size = new Size(74, 57);
                Countlabel2.Visible = true;
                Countlabel2.ForeColor = Color.White;
                Countlabel2.BackColor = Color.FromArgb(64, 64, 64);
                Countlabel2.TextAlign = ContentAlignment.MiddleCenter;
                Countlabel2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                Countlabel2.Margin = new Padding(3, 4, 3, 3);
                PlurerFlowPanel2.Controls.Add(Countlabel2);

                Label SNolabel2 = new Label();
                SNolabel2.Text = $"S/NO{count}";
                SNolabel2.Name = $"SNOLabel{count}"; // 이후 수정해야함
                SNolabel2.AutoSize = false;
                SNolabel2.Size = new Size(131, 57);
                SNolabel2.Visible = true;
                SNolabel2.ForeColor = Color.White;
                SNolabel2.BackColor = Color.FromArgb(64, 64, 64);
                SNolabel2.TextAlign = ContentAlignment.MiddleCenter;
                SNolabel2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                SNolabel2.Margin = new Padding(3, 4, 3, 3);
                PlurerFlowPanel2.Controls.Add(SNolabel2);



                var accelDir = Path.Combine(rowEntry.TrialFolderPath, "Acceleration");
                var decelDir = Path.Combine(rowEntry.TrialFolderPath, "Deceleration");
                string ACPath = Path.Combine(accelDir, "ScoreGrade.csv");
                string DCPath = Path.Combine(decelDir, "ScoreGrade.csv");

                ////파일이 있는지 확인 
                //AC 
                if (File.Exists(ACPath))
                {
                    string[] lines = File.ReadAllLines(ACPath);
                    if (lines.Length > 2)
                    {
                        //파일에 있고 데이터있는 경우
                        //PeakX_Score
                        string[] values = lines[2].Split(",");
                        string PeakX_Score = (double.Parse(values[6]) * 0.3).ToString("F2");
                        Label AcPeakX_Score = new Label();
                        AcPeakX_Score.Text = PeakX_Score;
                        AcPeakX_Score.Name = $"ACPeakX{count}";
                        AcPeakX_Score.AutoSize = false;
                        AcPeakX_Score.Size = new Size(79, 57);
                        AcPeakX_Score.Visible = true;
                        AcPeakX_Score.ForeColor = Color.White;
                        AcPeakX_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakX_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakX_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AcPeakX_Score.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(AcPeakX_Score);
                        ACPeakXScores[count - 1] = float.Parse(PeakX_Score);

                        values = lines[4].Split(",");
                        string PeakY_Score = (double.Parse(values[6]) * 0.3).ToString("F2");
                        Label AcPeakY_Score = new Label();
                        AcPeakY_Score.Text = PeakY_Score;
                        AcPeakY_Score.Name = $"ACPeakY{count}";
                        AcPeakY_Score.AutoSize = false;
                        AcPeakY_Score.Size = new Size(74, 57);
                        AcPeakY_Score.Visible = true;
                        AcPeakY_Score.ForeColor = Color.White;
                        AcPeakY_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakY_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakY_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AcPeakY_Score.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(AcPeakY_Score);
                        ACPeakYScores[count - 1] = float.Parse(PeakY_Score);

                        values = lines[6].Split(",");
                        string Width = (double.Parse(values[6]) * 0.2).ToString("F2");
                        Label Width_Score = new Label();
                        Width_Score.Text = Width;
                        Width_Score.Name = $"ACWidth{count}";
                        Width_Score.AutoSize = false;
                        Width_Score.Size = new Size(89, 57);
                        Width_Score.Visible = true;
                        Width_Score.ForeColor = Color.White;
                        Width_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Width_Score.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(Width_Score);
                        ACWidthScores[count - 1] = float.Parse(Width);

                        values = lines[8].Split(",");
                        string Height = (double.Parse(values[6]) * 0.1).ToString("F2");
                        Label Height_Score = new Label();
                        Height_Score.Text = Height;
                        Height_Score.Name = $"ACHeight{count}";
                        Height_Score.AutoSize = false;
                        Height_Score.Size = new Size(92, 57);
                        Height_Score.Visible = true;
                        Height_Score.ForeColor = Color.White;
                        Height_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Height_Score.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(Height_Score);
                        ACHeightScores[count - 1] = float.Parse(Height);


                        values = lines[10].Split(",");
                        string Area = (double.Parse(values[6]) * 0.1).ToString("F2");
                        Label Area_Score = new Label();
                        Area_Score.Text = Area;
                        Area_Score.Name = $"ACArea{count}";
                        Area_Score.AutoSize = false;
                        Area_Score.Size = new Size(83, 57);
                        Area_Score.Visible = true;
                        Area_Score.ForeColor = Color.White;
                        Area_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Area_Score.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(Area_Score);
                        ACAreaScores[count - 1] = float.Parse(Area);

                        values = lines[11].Split(",");
                        string FinalScore = (double.Parse(values[1])).ToString("F2");
                        Label AC_Final_Score = new Label();
                        AC_Final_Score.Text = FinalScore;
                        AC_Final_Score.Name = $"ACFinalScore{count}";
                        AC_Final_Score.AutoSize = false;
                        AC_Final_Score.Size = new Size(76, 57);
                        AC_Final_Score.Visible = true;
                        AC_Final_Score.ForeColor = Color.White;
                        AC_Final_Score.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Score.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AC_Final_Score.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(AC_Final_Score);
                        AC_DCFinalScore += (double.Parse(FinalScore)* ACAddRatio);
                        //최종 등급
                        values = lines[12].Split(",");
                        string FinalGrade = (double.Parse(values[1])).ToString("F0");
                        Label AC_Final_Grade = new Label();
                        AC_Final_Grade.Text = FinalGrade;
                        AC_Final_Grade.Name = $"ACFinalGeade{count}";
                        AC_Final_Grade.AutoSize = false;
                        AC_Final_Grade.Size = new Size(76, 57);
                        AC_Final_Grade.Visible = true;
                        AC_Final_Grade.ForeColor = Color.White;
                        AC_Final_Grade.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Grade.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Grade.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AC_Final_Grade.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(AC_Final_Grade);
                        int ACGrade = int.Parse(FinalGrade);
                        ACGradeCount[ACGrade-1]++;
                        



                        //이상치 비율 계산 필요
                        int ACPeakxOutlierCount = 0;
                        int ACPeakyOutlierCount = 0;
                        int ACWidthOutlierCount = 0;
                        int ACHeightOutlierCount = 0;
                        int ACAreaOutlierCount = 0;

                        double addRatio = 0.7;

                        string[] ACLines = File.ReadAllLines(Path.Combine(accelDir, "ResultOutput.csv"));
                        float[] FSinglePeakX = new float[ACLines.Length];
                        float[] FSinglePeakY = new float[ACLines.Length];
                        float[] FSingleWidth = new float[ACLines.Length];
                        float[] FSingleHeight = new float[ACLines.Length];
                        float[] FSingleArea = new float[ACLines.Length];
                        int ACcount = 0;
                        try
                        {
                            // 한 줄씩 읽어오기
                            foreach (string line in ACLines)
                            {
                                // 쉼표로 분리하여 배열에 담기
                                string[] va = line.Split(',');
                                if (va.Length < 8)
                                {
                                    Logger.LogWarning("FileIO", "Acceleration CSV 포맷 이상 (복수 통계)", _LoginManager?.UserInputID ?? "", $"{Path.Combine(accelDir, "ResultOutput.csv")} | line={line}");
                                    break;
                                }
                                if (!TryParseCsvFloat(va[1], out FSinglePeakX[ACcount])
                                    || !TryParseCsvFloat(va[2], out FSinglePeakY[ACcount])
                                    || !TryParseCsvFloat(va[3], out FSingleWidth[ACcount])
                                    || !TryParseCsvFloat(va[4], out FSingleHeight[ACcount])
                                    || !TryParseCsvFloat(va[5], out FSingleArea[ACcount])

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
                        double AC_OutlierRatio = ((ACPeakxOutlierCount + ACPeakyOutlierCount + ACWidthOutlierCount + ACHeightOutlierCount + ACAreaOutlierCount) / ((double)5 * (double)FSinglePeakX.Length)) * 100;

                        Label AC_Final_Outlier = new Label();
                        AC_Final_Outlier.Text = AC_OutlierRatio.ToString("F2");
                        AC_Final_Outlier.Name = $"ACFinalOutlier{count}";
                        AC_Final_Outlier.AutoSize = false;
                        AC_Final_Outlier.Size = new Size(76, 57);
                        AC_Final_Outlier.Visible = true;
                        AC_Final_Outlier.ForeColor = Color.White;
                        AC_Final_Outlier.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Outlier.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Outlier.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AC_Final_Outlier.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel1.Controls.Add(AC_Final_Outlier);



                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.

                        double SinglePeakXMAD = FSinglePeakX.Select(num => Math.Abs(num - FSinglePeakX.Average())).Average();
                        double SinglePeakYMAD = FSinglePeakY.Select(num => Math.Abs(num - FSinglePeakY.Average())).Average();
                        double SingleWidthYMAD = FSingleWidth.Select(num => Math.Abs(num - FSingleWidth.Average())).Average();
                        double SingleHeightMAD = FSingleHeight.Select(num => Math.Abs(num - FSingleHeight.Average())).Average();
                        double SingleAreaMAD = FSingleArea.Select(num => Math.Abs(num - FSingleArea.Average())).Average();
                        ACPeakXMAD[count - 1] = (float)SinglePeakXMAD;
                        ACPeakYMAD[count - 1] = (float)SinglePeakYMAD;
                        ACWidthMAD[count - 1] = (float)SingleWidthYMAD;
                        ACHeightMAD[count - 1] = (float)SingleHeightMAD;
                        ACAreaMAD[count - 1] = (float)SingleAreaMAD;

                        Label AcPeakX_Score2 = new Label();
                        AcPeakX_Score2.Text = SinglePeakXMAD.ToString("F2");
                        AcPeakX_Score2.Name = $"ACPeakX{count}";
                        AcPeakX_Score2.AutoSize = false;
                        AcPeakX_Score2.Size = Plurer2LabelSize;
                        AcPeakX_Score2.Visible = true;
                        AcPeakX_Score2.ForeColor = Color.White;
                        AcPeakX_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakX_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakX_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AcPeakX_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(AcPeakX_Score2);


                        Label AcPeakY_Score2 = new Label();
                        AcPeakY_Score2.Text = SinglePeakYMAD.ToString("F2");
                        AcPeakY_Score2.Name = $"ACPeakY{count}";
                        AcPeakY_Score2.AutoSize = false;
                        AcPeakY_Score2.Size = Plurer2LabelSize;
                        AcPeakY_Score2.Visible = true;
                        AcPeakY_Score2.ForeColor = Color.White;
                        AcPeakY_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakY_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakY_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AcPeakY_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(AcPeakY_Score2);

                        Label Width_Score2 = new Label();
                        Width_Score2.Text = SingleWidthYMAD.ToString("F2");
                        Width_Score2.Name = $"ACWidth{count}";
                        Width_Score2.AutoSize = false;
                        Width_Score2.Size = Plurer2LabelSize;
                        Width_Score2.Visible = true;
                        Width_Score2.ForeColor = Color.White;
                        Width_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Width_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(Width_Score2);

                        Label Height_Score2 = new Label();
                        Height_Score2.Text = SingleHeightMAD.ToString("F2");
                        Height_Score2.Name = $"ACHeight{count}";
                        Height_Score2.AutoSize = false;
                        Height_Score2.Size = Plurer2LabelSize;
                        Height_Score2.Visible = true;
                        Height_Score2.ForeColor = Color.White;
                        Height_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Height_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(Height_Score2);

                        Label Area_Score2 = new Label();
                        Area_Score2.Text = SingleAreaMAD.ToString("F2");
                        Area_Score2.Name = $"ACArea{count}";
                        Area_Score2.AutoSize = false;
                        Area_Score2.Size = Plurer2LabelSize;
                        Area_Score2.Visible = true;
                        Area_Score2.ForeColor = Color.White;
                        Area_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Area_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(Area_Score2);

                        //최종 등급 => 평균 편차의 절대값 평균 패널에 붙일거
                        values = lines[12].Split(",");
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
                        AC_Final_Grade2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AC_Final_Grade2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(AC_Final_Grade2);

                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                        Label AC_Final_Outlier2 = new Label();
                        AC_Final_Outlier2.Text = AC_OutlierRatio.ToString("F2");
                        AC_Final_Outlier2.Name = $"ACFinalOutlier{count}";
                        AC_Final_Outlier2.AutoSize = false;
                        AC_Final_Outlier2.Size = Plurer2LabelSize;
                        AC_Final_Outlier2.Visible = true;
                        AC_Final_Outlier2.ForeColor = Color.White;
                        AC_Final_Outlier2.BackColor = Color.FromArgb(64, 64, 64);
                        AC_Final_Outlier2.TextAlign = ContentAlignment.MiddleCenter;
                        AC_Final_Outlier2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AC_Final_Outlier2.Margin = new Padding(3, 4, 3, 3);
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
                        string PeakX_Score = (double.Parse(values[6]) * 0.3).ToString("F2");
                        Label DcPeakX_Score = new Label();
                        DcPeakX_Score.Text = PeakX_Score;
                        DcPeakX_Score.Name = $"ACPeakX{count}";
                        DcPeakX_Score.AutoSize = false;
                        DcPeakX_Score.Size = new Size(83, 57);
                        DcPeakX_Score.Visible = true;
                        DcPeakX_Score.ForeColor = Color.White;
                        DcPeakX_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DcPeakX_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DcPeakX_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DcPeakX_Score.Margin = new Padding(3, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(DcPeakX_Score);
                        DCPeakXScores[count - 1] = float.Parse(PeakX_Score);

                        values = lines[4].Split(",");
                        string PeakY_Score = (double.Parse(values[6]) * 0.3).ToString("F2");
                        Label DcPeakY_Score = new Label();
                        DcPeakY_Score.Text = PeakY_Score;
                        DcPeakY_Score.Name = $"ACPeakY{count}";
                        DcPeakY_Score.AutoSize = false;
                        DcPeakY_Score.Size = new Size(90, 57);
                        DcPeakY_Score.Visible = true;
                        DcPeakY_Score.ForeColor = Color.White;
                        DcPeakY_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DcPeakY_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DcPeakY_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DcPeakY_Score.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(DcPeakY_Score);
                        DCPeakYScores[count - 1] = float.Parse(PeakY_Score);

                        values = lines[6].Split(",");
                        string Width = (double.Parse(values[6]) * 0.2).ToString("F2");
                        Label Width_Score = new Label();
                        Width_Score.Text = Width;
                        Width_Score.Name = $"ACWidth{count}";
                        Width_Score.AutoSize = false;
                        Width_Score.Size = new Size(92, 57);
                        Width_Score.Visible = true;
                        Width_Score.ForeColor = Color.White;
                        Width_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Width_Score.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(Width_Score);
                        DCWidthScores[count - 1] = float.Parse(Width);

                        values = lines[8].Split(",");
                        string Height = (double.Parse(values[6]) * 0.1).ToString("F2");
                        Label Height_Score = new Label();
                        Height_Score.Text = Height;
                        Height_Score.Name = $"ACHeight{count}";
                        Height_Score.AutoSize = false;
                        Height_Score.Size = new Size(95, 57);
                        Height_Score.Visible = true;
                        Height_Score.ForeColor = Color.White;
                        Height_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Height_Score.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(Height_Score);
                        DCHeightScores[count - 1] = float.Parse(Height);


                        values = lines[10].Split(",");
                        string Area = (double.Parse(values[6]) * 0.1).ToString("F2");
                        Label Area_Score = new Label();
                        Area_Score.Text = Area;
                        Area_Score.Name = $"ACArea{count}";
                        Area_Score.AutoSize = false;
                        Area_Score.Size = new Size(95, 57);
                        Area_Score.Visible = true;
                        Area_Score.ForeColor = Color.White;
                        Area_Score.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Area_Score.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(Area_Score);
                        DCAreaScores[count - 1] = float.Parse(Area);

                        values = lines[11].Split(",");
                        string FinalScore = (double.Parse(values[1])).ToString("F2");
                        Label DC_Final_Score = new Label();
                        DC_Final_Score.Text = FinalScore;
                        DC_Final_Score.Name = $"ACFinalScore{count}";
                        DC_Final_Score.AutoSize = false;
                        DC_Final_Score.Size = new Size(88, 57);
                        DC_Final_Score.Visible = true;
                        DC_Final_Score.ForeColor = Color.White;
                        DC_Final_Score.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Score.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Score.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DC_Final_Score.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(DC_Final_Score);
                        AC_DCFinalScore += (double.Parse(FinalScore)*(1- ACAddRatio));

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
                            values = lines[12].Split(",");
                        string FinalGrade = (double.Parse(values[1])).ToString("F0");
                        Label DC_Final_Grade = new Label();
                        DC_Final_Grade.Text = FinalGrade;
                        DC_Final_Grade.Name = $"DCFinalGeade{count}";
                        DC_Final_Grade.AutoSize = false;
                        DC_Final_Grade.Size = new Size(75, 57);
                        DC_Final_Grade.Visible = true;
                        DC_Final_Grade.ForeColor = Color.White;
                        DC_Final_Grade.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Grade.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Grade.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DC_Final_Grade.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(DC_Final_Grade);
                        DCGradeCount[int.Parse(FinalGrade) - 1]++;

                        //

                        //이상치 비율 계산 필요
                        int DCPeakxOutlierCount = 0;
                        int DCPeakyOutlierCount = 0;
                        int DCWidthOutlierCount = 0;
                        int DCHeightOutlierCount = 0;
                        int DCAreaOutlierCount = 0;

                        double addRatio = 0.7;

                        string[] DCLines = File.ReadAllLines(Path.Combine(decelDir, "ResultOutput.csv"));
                        float[] RSinglePeakX = new float[DCLines.Length];
                        float[] RSinglePeakY = new float[DCLines.Length];
                        float[] RSingleWidth = new float[DCLines.Length];
                        float[] RSingleHeight = new float[DCLines.Length];
                        float[] RSingleArea = new float[DCLines.Length];
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
                                    || !TryParseCsvFloat(va[3], out RSingleWidth[DCCcount])
                                    || !TryParseCsvFloat(va[4], out RSingleHeight[DCCcount])
                                    || !TryParseCsvFloat(va[5], out RSingleArea[DCCcount])
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

                        double DC_OutlierRatio = ((DCPeakxOutlierCount + DCPeakyOutlierCount + DCWidthOutlierCount + DCHeightOutlierCount + DCAreaOutlierCount) / ((double)5 * (double)RSinglePeakX.Length)) * 100;

                        Label DC_Final_Outlier = new Label();
                        DC_Final_Outlier.Text = DC_OutlierRatio.ToString("F2");
                        DC_Final_Outlier.Name = $"DCFinalOutlier{count}";
                        DC_Final_Outlier.AutoSize = false;
                        DC_Final_Outlier.Size = new Size(75, 57);
                        DC_Final_Outlier.Visible = true;
                        DC_Final_Outlier.ForeColor = Color.White;
                        DC_Final_Outlier.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Outlier.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Outlier.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DC_Final_Outlier.Margin = new Padding(2, 3, 3, 3);
                        PlurerFlowPanel1.Controls.Add(DC_Final_Outlier);



                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                        // 1. 데이터의 원본 평균 구하기
                        // 2. 각 요소에서 평균을 뺀 절대값(Math.Abs)들의 평균을 다시 구하기                       
                        double SinglePeakXMAD = RSinglePeakX.Select(num => Math.Abs(num - RSinglePeakX.Average())).Average();
                        double SinglePeakYMAD = RSinglePeakY.Select(num => Math.Abs(num - RSinglePeakY.Average())).Average();
                        double SingleWidthYMAD = RSingleWidth.Select(num => Math.Abs(num - RSingleWidth.Average())).Average();
                        double SingleHeightMAD = RSingleHeight.Select(num => Math.Abs(num - RSingleHeight.Average())).Average();
                        double SingleAreaMAD = RSingleArea.Select(num => Math.Abs(num - RSingleArea.Average())).Average();
                        DCPeakXMAD[count - 1] = (float)SinglePeakXMAD;
                        DCPeakYMAD[count - 1] = (float)SinglePeakYMAD;
                        DCWidthMAD[count - 1] = (float)SingleWidthYMAD;
                        DCHeightMAD[count - 1] = (float)SingleHeightMAD;
                        DCAreaMAD[count - 1] = (float)SingleAreaMAD;

                        Label AcPeakX_Score2 = new Label();
                        AcPeakX_Score2.Text = SinglePeakXMAD.ToString("F2");
                        AcPeakX_Score2.Name = $"ACPeakX{count}";
                        AcPeakX_Score2.AutoSize = false;
                        AcPeakX_Score2.Size = Plurer2LabelSize;
                        AcPeakX_Score2.Visible = true;
                        AcPeakX_Score2.ForeColor = Color.White;
                        AcPeakX_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakX_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakX_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AcPeakX_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(AcPeakX_Score2);


                        Label AcPeakY_Score2 = new Label();
                        AcPeakY_Score2.Text = SinglePeakYMAD.ToString("F2");
                        AcPeakY_Score2.Name = $"ACPeakY{count}";
                        AcPeakY_Score2.AutoSize = false;
                        AcPeakY_Score2.Size = Plurer2LabelSize;
                        AcPeakY_Score2.Visible = true;
                        AcPeakY_Score2.ForeColor = Color.White;
                        AcPeakY_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        AcPeakY_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        AcPeakY_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        AcPeakY_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(AcPeakY_Score2);

                        Label Width_Score2 = new Label();
                        Width_Score2.Text = SingleWidthYMAD.ToString("F2");
                        Width_Score2.Name = $"ACWidth{count}";
                        Width_Score2.AutoSize = false;
                        Width_Score2.Size = Plurer2LabelSize;
                        Width_Score2.Visible = true;
                        Width_Score2.ForeColor = Color.White;
                        Width_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Width_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Width_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Width_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(Width_Score2);

                        Label Height_Score2 = new Label();
                        Height_Score2.Text = SingleHeightMAD.ToString("F2");
                        Height_Score2.Name = $"ACHeight{count}";
                        Height_Score2.AutoSize = false;
                        Height_Score2.Size = Plurer2LabelSize;
                        Height_Score2.Visible = true;
                        Height_Score2.ForeColor = Color.White;
                        Height_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Height_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Height_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Height_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(Height_Score2);

                        Label Area_Score2 = new Label();
                        Area_Score2.Text = SingleAreaMAD.ToString("F2");
                        Area_Score2.Name = $"ACArea{count}";
                        Area_Score2.AutoSize = false;
                        Area_Score2.Size = Plurer2LabelSize;
                        Area_Score2.Visible = true;
                        Area_Score2.ForeColor = Color.White;
                        Area_Score2.BackColor = Color.FromArgb(64, 64, 64);
                        Area_Score2.TextAlign = ContentAlignment.MiddleCenter;
                        Area_Score2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        Area_Score2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(Area_Score2);

                        //최종 등급 => 평균 편차의 절대값 평균 패널에 붙일거
                        values = lines[12].Split(",");
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
                        DC_Final_Grade2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DC_Final_Grade2.Margin = new Padding(3, 4, 3, 3);
                        PlurerFlowPanel2.Controls.Add(DC_Final_Grade2);

                        //평균 편차의 절대값 평균에 해당하는 부분도 같이 만든다.
                        Label DC_Final_Outlier2 = new Label();
                        DC_Final_Outlier2.Text = DC_OutlierRatio.ToString("F2");
                        DC_Final_Outlier2.Name = $"ACFinalOutlier{count}";
                        DC_Final_Outlier2.AutoSize = false;
                        DC_Final_Outlier2.Size = Plurer2LabelSize;
                        DC_Final_Outlier2.Visible = true;
                        DC_Final_Outlier2.ForeColor = Color.White;
                        DC_Final_Outlier2.BackColor = Color.FromArgb(64, 64, 64);
                        DC_Final_Outlier2.TextAlign = ContentAlignment.MiddleCenter;
                        DC_Final_Outlier2.Font = new Font("맑은 고딕", 15, FontStyle.Bold);
                        DC_Final_Outlier2.Margin = new Padding(3, 4, 3, 3);
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
                }



            }
            PlotIndexScatter(ACPeakXScorePlot, ACPeakXScores, ACPeakXScores.Length, "ACPeakXScore");
            PlotIndexScatter(ACPeakYScorePlot, ACPeakYScores, ACPeakYScores.Length, "ACPeakYScores");
            PlotIndexScatter(ACWidthScorePlot, ACWidthScores, ACWidthScores.Length, "ACWidthScores");
            PlotIndexScatter(ACHeightScorePlot, ACHeightScores, ACHeightScores.Length, "ACHeightScores");
            PlotIndexScatter(ACAreaScorePlot, ACAreaScores, ACAreaScores.Length, "ACAreaScores");

            PlotIndexScatter(ACPeakXMADPlot, ACPeakXMAD, ACPeakXMAD.Length, "ACPeakXMAD");
            PlotIndexScatter(ACPeakYMADPlot, ACPeakYMAD, ACPeakYMAD.Length, "ACPeakYMAD");
            PlotIndexScatter(ACWidthMADPlot, ACWidthMAD, ACWidthMAD.Length, "ACWidthMAD");
            PlotIndexScatter(ACHeightMADPlot, ACHeightMAD, ACHeightMAD.Length, "ACHeightMAD");
            PlotIndexScatter(ACAreaMADPlot, ACAreaMAD, ACAreaMAD.Length, "ACAreaMAD");


            PlotIndexScatter(DCPeakXScorePlot,  DCPeakXScores,   DCPeakXScores.Length,  "DCPeakXScore");
            PlotIndexScatter(DCPeakYScorePlot,  DCPeakYScores,   DCPeakYScores.Length,  "DCPeakYScores");
            PlotIndexScatter(DCWidthScorePlot,  DCWidthScores,   DCWidthScores.Length,  "DCWidthScores");
            PlotIndexScatter(DCHeightScorePlot, DCHeightScores,  DCHeightScores.Length, "DCHeightScores");
            PlotIndexScatter(DCAreaScorePlot,   DCAreaScores,    DCAreaScores.Length,   "DCAreaScores");

            PlotIndexScatter(DCPeakXMADPlot,  DCPeakXMAD, DCPeakXMAD.Length,  "DCPeakXMAD");
            PlotIndexScatter(DCPeakYMADPlot,  DCPeakYMAD, DCPeakYMAD.Length,  "DCPeakYMAD");
            PlotIndexScatter(DCWidthMADPlot,  DCWidthMAD, DCWidthMAD.Length,  "DCWidthMAD");
            PlotIndexScatter(DCHeightMADPlot, DCHeightMAD,DCHeightMAD.Length, "DCHeightMAD");
            PlotIndexScatter(DCAreaMADPlot,   DCAreaMAD,  DCAreaMAD.Length,   "DCAreaMAD");


            //float로 형변환 하기
            float[] tempdoubleArray = FinalGradeCount.Select(f => (float)f).ToArray();
            PlotIndexBar(FinalGradeCountPlot, tempdoubleArray, FinalGradeCount.Length,"FinalGrade");
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
            Bitmap bitmap = _CV.GearGridWarpPerspective(CalRearOriginImgPath);

            RearCalResult.SizeMode = PictureBoxSizeMode.StretchImage;
            RearCalResult.Image = bitmap;
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
            if (PerulStaticPanelCount >= 8)
            {
                PerulStaticPanelCount = 7;
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
                PerulStaticDisplayLabel.Text = "복수 통계 : MAD";
                PerulStaticPanel_2.BringToFront();
            }
            else if (PerulStaticPanelCount == 3)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : Acceleration 점수";
                PerulStaticPanel_3.BringToFront();
            }
            else if (PerulStaticPanelCount == 4)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : Acceleration MAD";
                PerulStaticPanel_4.BringToFront();
            }
            else if (PerulStaticPanelCount == 5)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : Dcceleration 점수";
                PerulStaticPanel_5.BringToFront();
            }
            else if (PerulStaticPanelCount == 6)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : Dcceleration MAD";
                PerulStaticPanel_6.BringToFront();
            }
            else if (PerulStaticPanelCount == 7)
            {
                PerulStaticDisplayLabel.Text = "복수 통계 : 등급 통계";
                PerulStaticPanel_7.BringToFront();
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


            PerulStaticPanel_1.BringToFront();
        }

    }
}
