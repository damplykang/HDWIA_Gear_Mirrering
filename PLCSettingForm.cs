namespace WIA_ViewerProgram
{
    public partial class PLCSettingForm : Form
    {
        private readonly PLC _plc;
        private string _plcSnapshotIp = "";
        private int _plcSnapshotStationNumber;
        private int _plcSnapshotMoniteringCycle;
        private string _plcSnapshotMoniterAdrress = "";

        public PLCSettingForm(PLC plc)
        {
            _plc = plc;
            InitializeComponent();
        }

        private void PLCSettingForm_Load(object? sender, EventArgs e)
        {
            ApplyPlcToControls();
            _plcSnapshotIp = _plc.Ip;
            _plcSnapshotStationNumber = _plc.StationNumber;
            _plcSnapshotMoniteringCycle = _plc.MoniteringCycle;
            _plcSnapshotMoniterAdrress = _plc.MoniterAdrress;
        }

        private void ApplyPlcToControls()
        {
            IpTextBox.Text = _plc.Ip;
            StationNumberTextBox.Text = _plc.StationNumber > 0 ? _plc.StationNumber.ToString() : "";
            MoniteringCycleTextBox.Text = _plc.MoniteringCycle > 0 ? _plc.MoniteringCycle.ToString() : "";
            MoniterAdrressTextBox.Text = string.IsNullOrWhiteSpace(_plc.MoniterAdrress) ? "" : _plc.MoniterAdrress.Trim();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            string ip = IpTextBox.Text.Trim();
            string stationText = StationNumberTextBox.Text.Trim();
            string cycleText = MoniteringCycleTextBox.Text.Trim();
            string addressText = MoniterAdrressTextBox.Text.Trim();

            if (string.IsNullOrEmpty(stationText))
            {
                MessageBox.Show(this, "Station 번호를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(stationText, out int station) || station < 0 || station > 65535)
            {
                MessageBox.Show(this, "Station 번호는 0~65535 사이의 정수여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(cycleText))
            {
                MessageBox.Show(this, "MoniteringCycle(모니터링 주기)를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(cycleText, out int cycle) || cycle < 1 || cycle > 3_600_000)
            {
                MessageBox.Show(this, "MoniteringCycle은 1~3600000(ms) 사이의 정수여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(addressText))
            {
                MessageBox.Show(this, "MoniterAdrress(모니터 디바이스 주소)를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (addressText.Length > 64)
            {
                MessageBox.Show(this, "MoniterAdrress는 64자 이하로 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _plc.Ip = ip;
            _plc.StationNumber = station;
            _plc.MoniteringCycle = cycle;
            _plc.MoniterAdrress = addressText;
            _plc.SaveToJson();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void PLCCancelButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            ActiveControl = null;
            _plc.Ip = _plcSnapshotIp;
            _plc.StationNumber = _plcSnapshotStationNumber;
            _plc.MoniteringCycle = _plcSnapshotMoniteringCycle;
            _plc.MoniterAdrress = _plcSnapshotMoniterAdrress;
            ApplyPlcToControls();
        }
    }
}
