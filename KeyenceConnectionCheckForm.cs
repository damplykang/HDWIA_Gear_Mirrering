using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WIA_ViewerProgram
{
    public partial class KeyenceConnectionCheckForm : Form
    {
        private readonly Keyence _keyence;
        private const int Attempts = 5;
        private const int TcpTimeoutMs = 5000;

        public KeyenceConnectionCheckForm(Keyence keyence)
        {
            _keyence = keyence;
            InitializeComponent();
        }

        private async void KeyenceConnectionCheckForm_Shown(object? sender, EventArgs e)
        {
            await RunChecksAsync();
        }

        private async Task RunChecksAsync()
        {
            string ip = _keyence.Ip.Trim();
            int port = _keyence.PortNumber;

            LogLine("설정된 IP / Port 기준으로 연결 상태를 확인합니다.");
            LogLine("");
            LogLine($"대상 IP: {ip}");
            LogLine($"대상 Port: {port}");
            LogLine("");

            int icmpOk = 0;
            LogLine($"--- ICMP Ping ({Attempts}회) ---");
            for (int i = 1; i <= Attempts; i++)
            {
                string line;
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ip, TcpTimeoutMs).ConfigureAwait(true);
                    if (reply.Status == IPStatus.Success)
                    {
                        icmpOk++;
                        line = $"[{i}/{Attempts}] 성공 (RTT {reply.RoundtripTime} ms)";
                    }
                    else
                    {
                        line = $"[{i}/{Attempts}] 실패 ({reply.Status})";
                    }
                }
                catch (Exception ex)
                {
                    line = $"[{i}/{Attempts}] 실패: {ex.Message}";
                }

                LogLine(line);
            }

            LogLine("");

            int tcpOk = 0;
            LogLine($"--- TCP 연결 ({Attempts}회, IP:Port) ---");
            for (int i = 1; i <= Attempts; i++)
            {
                var sw = Stopwatch.StartNew();
                string line;
                try
                {
                    using var cts = new CancellationTokenSource(TcpTimeoutMs);
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(ip, port, cts.Token).ConfigureAwait(true);
                    sw.Stop();
                    tcpOk++;
                    line = $"[{i}/{Attempts}] 성공 ({sw.ElapsedMilliseconds} ms)";
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    line = $"[{i}/{Attempts}] 실패: {ex.Message}";
                }

                LogLine(line);
            }

            LogLine("");
            LogLine("--- 검사 완료 ---");
            if (icmpOk < Attempts && tcpOk == Attempts)
            {
                LogLine("(참고: Ping은 차단되었을 수 있으나 TCP는 정상입니다.)");
            }

            bool allOk = tcpOk == Attempts;
            if (allOk)
            {
                MessageBox.Show(
                    this,
                    "설정한 IP/Port로 TCP 연결 5회가 모두 성공했습니다.\nKeyence에 정상적으로 연결되었습니다.",
                    "연결 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    this,
                    $"연결이 정상적으로 이루어지지 않았습니다.\n\nTCP 성공: {tcpOk}/{Attempts}\n(ICMP 성공: {icmpOk}/{Attempts})",
                    "연결 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            Close();
        }

        private void LogLine(string line)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                void Act() => LogLine(line);
                BeginInvoke(Act);
                return;
            }

            ResultTextBox.AppendText(line + Environment.NewLine);
        }
    }
}
