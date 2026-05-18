namespace WIA_ViewerProgram
{
    public partial class KeyenceSettingForm : Form
    {
        private readonly Keyence _keyence;
        private string _keyenceSnapshotIp = "";
        private int _keyenceSnapshotPortNumber;

        public KeyenceSettingForm(Keyence keyence)
        {
            _keyence = keyence;
            InitializeComponent();
        }

        private void KeyenceSettingForm_Load(object? sender, EventArgs e)
        {
            ApplyKeyenceToControls();
            _keyenceSnapshotIp = _keyence.Ip;
            _keyenceSnapshotPortNumber = _keyence.PortNumber;
        }

        private void ApplyKeyenceToControls()
        {
            IpTextBox.Text = _keyence.Ip;
            PortNumberTextBox.Text = _keyence.PortNumber > 0 ? _keyence.PortNumber.ToString() : "";
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            string ip = IpTextBox.Text.Trim();
            string portText = PortNumberTextBox.Text.Trim();

            if (string.IsNullOrEmpty(portText))
            {
                MessageBox.Show(this, "포트 번호를 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port < 0 || port > 65535)
            {
                MessageBox.Show(this, "포트 번호는 0~65535 사이의 정수여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _keyence.Ip = ip;
            _keyence.PortNumber = port;
            _keyence.SaveToJson();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void KeyenceCancelButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            ActiveControl = null;
            _keyence.Ip = _keyenceSnapshotIp;
            _keyence.PortNumber = _keyenceSnapshotPortNumber;
            ApplyKeyenceToControls();
        }
    }
}
