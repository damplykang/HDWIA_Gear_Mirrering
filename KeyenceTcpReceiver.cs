using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace WIA_ViewerProgram
{
    /// <summary>
    /// Keyence 설정(IP/Port)으로 TCP 연결 후 스트림을 지속 수신하고 콘솔에 출력합니다.
    /// WinForms는 기본적으로 콘솔이 없으므로 <see cref="ConsoleAttach"/>로 별도 콘솔 창을 띄웁니다.
    /// </summary>
    internal sealed class KeyenceTcpReceiver
    {
        /// <summary>백그라운드 수신 스레드와 UI에서 Toggle/Stop이 동시에 호출될 때를 막기 위한 락입니다.</summary>
        private readonly object _sync = new();

        /// <summary>연결 취소·중지 시 ReadAsync를 깨우기 위해 사용합니다.</summary>
        private CancellationTokenSource? _cts;

        /// <summary>현재 TCP 연결(백그라운드 루프에서 생성·해제).</summary>
        private TcpClient? _client;

        /// <summary>수신 루프가 동작 중인지 여부(버튼 문구·중복 시작 방지에 사용).</summary>
        public bool IsReceiving { get; private set; }

        /// <summary>
        /// 데이터 수신 시작/중지를 전환합니다.
        /// - 이미 수신 중이면: 연결을 끊고 수신 루프를 중단한 뒤 <paramref name="syncUi"/>(false) 호출.
        /// - 수신 중이 아니면: IP/Port 검사 후 백그라운드에서 <see cref="ReceiveLoopAsync"/> 실행, <paramref name="syncUi"/>(true) 호출.
        /// </summary>
        /// <param name="syncUi">
        /// UI 스레드에서 버튼 텍스트 등을 바꿀 때 사용합니다.
        /// 인자 true = 수신 시작 상태, false = 수신 종료 상태.
        /// 백그라운드에서 호출될 수 있으므로 호출부에서 Invoke/BeginInvoke로 UI 스레드에 맞춥니다.
        /// </param>
        /// <returns>새로 수신을 시작했으면 true, 중지만 했거나 IP/Port 미설정이면 false.</returns>
        public bool Toggle(Keyence keyence, Action<bool> syncUi)
        {
            lock (_sync)
            {
                // ----- 이미 수신 중이면 → 사용자가 같은 버튼으로 중지 요청 -----
                if (IsReceiving)
                {
                    StopUnsafe();
                    syncUi(false);
                    return false;
                }

                // ----- 새로 시작하려 할 때 IP/Port 없으면 시작하지 않음 (호출측에서 안내 메시지) -----
                if (string.IsNullOrWhiteSpace(keyence.Ip) || keyence.PortNumber <= 0)
                {
                    return false;
                }

                // WinExe 실행 파일에도 검은 콘솔 창을 한 번 할당 (AllocConsole)
                ConsoleAttach.EnsureConsole();

                IsReceiving = true;
                syncUi(true);

                _cts = new CancellationTokenSource();
                CancellationToken ct = _cts.Token;
                string host = keyence.Ip.Trim();
                int port = keyence.PortNumber;

                // UI 스레드를 막지 않도록 TCP 연결·Read는 전부 백그라운드 작업으로 실행
                _ = Task.Run(() => ReceiveLoopAsync(host, port, ct, syncUi), ct);
                return true;
            }
        }

        /// <summary>폼 종료 등에서 안전하게 수신만 중지할 때 사용합니다(UI 동기화는 호출부에서 처리).</summary>
        public void Stop()
        {
            lock (_sync)
            {
                if (!IsReceiving)
                {
                    return;
                }

                StopUnsafe();
            }
        }

        /// <summary>
        /// 취소 토큰 발행 후 소켓을 닫아 ReadAsync가 깨어나도록 합니다.
        /// 주의: 호출 전에 lock(_sync) 잡혀 있어야 하는 경우와 아닌 경우가 있음 → 현재는 Stop/Toggle 안에서만 호출.
        /// </summary>
        private void StopUnsafe()
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // ignored
            }

            try
            {
                _client?.Close();
            }
            catch
            {
                // ignored
            }

            _client = null;
            try
            {
                _cts?.Dispose();
            }
            catch
            {
                // ignored
            }

            _cts = null;
            IsReceiving = false;
        }

        /// <summary>
        /// 지정 주소로 TCP 연결 후, 데이터가 올 때마다 콘솔에 한 줄씩 출력합니다.
        /// 원격이 연결을 끊거나 오류·취소 시 루프를 빠져나와 리소스를 정리합니다.
        /// </summary>
        private async Task ReceiveLoopAsync(string host, int port, CancellationToken ct, Action<bool> syncUi)
        {
            try
            {
                lock (_sync)
                {
                    _client = new TcpClient();
                }

                Console.WriteLine($"[Keyence] 연결 시도 {host}:{port} ...");
                await _client.ConnectAsync(host, port, ct).ConfigureAwait(false);
                Console.WriteLine($"[Keyence] TCP 연결됨. 수신 대기 중... (중지: 같은 버튼)");

                await using NetworkStream stream = _client.GetStream();
                var buffer = new byte[16384];

                // 데이터가 도착할 때마다 ReadAsync가 반환됨. 상대가 소켓을 닫으면 n==0.
                while (!ct.IsCancellationRequested)
                {
                    int n = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);// 버퍼에 스트림 쓰기!
                    if (n == 0)
                    {
                        Console.WriteLine($"[{Now()}] [Keyence] 원격에서 연결을 닫았습니다.");
                        break;
                    }
                    // 데이터 누적! ★중요★
                    // 여기서 이제 PLC에서 완료 신호를 주면 
                    // 누적된 데이터를 CSV파일로 만들어서 저장 시킨다!
                    Console.WriteLine($"[{Now()}] 수신 {n} bytes — {FormatPayload(buffer, n)}");// 여기가 데이터 출력부! 
                }
            }
            catch (OperationCanceledException)
            {
                // 사용자 중지 또는 폼 종료 시 Cancel과 연계된 정상 종료
                Console.WriteLine("[Keyence] 수신을 중지했습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Keyence] 오류: {ex.Message}");
            }
            finally
            {
                lock (_sync)
                {
                    try
                    {
                        _client?.Close();
                    }
                    catch
                    {
                        // ignored
                    }

                    _client = null;
                    try
                    {
                        _cts?.Dispose();
                    }
                    catch
                    {
                        // ignored
                    }

                    _cts = null;
                    IsReceiving = false;
                }

                // 수신이 끝난 시점에 버튼을 다시 "데이터수신"으로 돌리기 위해 알림
                try
                {
                    syncUi(false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static string Now() => DateTime.Now.ToString("HH:mm:ss.fff");

        /// <summary>
        /// 바이너리/텍스트 혼합 가능성을 고려해 표시용 문자열을 만듭니다.
        /// 출력 가능한 ASCII 범위면 UTF-8 문자열로, 아니면 HEX 일부만 잘라 표시합니다.
        /// 여기가 파싱해서 사용하는 부분이다!
        /// </summary>
        private static string FormatPayload(byte[] buf, int n)
        {
            ReadOnlySpan<byte> slice = buf.AsSpan(0, n);
            if (slice.Length == 0)
            {
                return "(empty)";
            }

            bool printable = true;
            foreach (byte b in slice)
            {
                // 탭·개행·캐리지리턴 외 제어문자면 바이너리로 간주
                if (b < 32 && b != 9 && b != 10 && b != 13)
                {
                    printable = false;
                    break;
                }
            }

            if (printable)
            {
                try
                {
                    return Encoding.UTF8.GetString(slice);
                }
                catch
                {
                    printable = false;
                }
            }

            const int maxHexBytes = 128;
            int show = Math.Min(slice.Length, maxHexBytes);
            string hex = Convert.ToHexString(slice[..show]);
            if (slice.Length > maxHexBytes)
            {
                hex += $" … (+{slice.Length - maxHexBytes} bytes)";
            }

            return $"HEX {hex}";
        }
    }

    /// <summary>
    /// 일반 WinForms(.exe, GUI만)에는 표준 출력용 콘솔이 없습니다.
    /// Windows API AllocConsole으로 프로세스에 콘솔을 붙이면 Console.WriteLine이 별도 창에 보입니다.
    /// </summary>
    internal static class ConsoleAttach
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        private static bool _attached;
        private static readonly object Gate = new();

        /// <summary>프로세스당 한 번만 AllocConsole 호출합니다.</summary>
        public static void EnsureConsole()
        {
            lock (Gate)
            {
                if (_attached)
                {
                    return;
                }

                AllocConsole();
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.Title = "Keyence TCP 수신";
                }
                catch
                {
                    // ignored
                }

                _attached = true;
            }
        }
    }
}
