using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WIA_ViewerProgram
{
    internal class HistoryManager
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public string User { get; set; }
            public string Details { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
                sb.Append($"[{Level.ToString().ToUpper()}] ");
                if (!string.IsNullOrEmpty(User))
                    sb.Append($"[User: {User}] ");
                if (!string.IsNullOrEmpty(Category))
                    sb.Append($"[{Category}] ");
                sb.Append(Message);
                if (!string.IsNullOrEmpty(Details))
                    sb.Append($" | Details: {Details}");
                return sb.ToString();
            }
        }

        internal class HistroyManager
        {
            private static HistroyManager? _instance;
            private int categoryEnd;
            private static readonly object _lock = new object();
            private readonly string _logDirectory;
            private readonly string _logFileName;
            private readonly int _maxLogFileSize = 10 * 1024 * 1024; // 10MB
            private readonly int _maxLogFiles = 10; // 최대 보관 파일 수

            private HistroyManager()
            {
                _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
                _logFileName = Path.Combine(_logDirectory, $"HDMIndoPE_{DateTime.Now:yyyyMMdd}.log");
            }

            public static HistroyManager Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        lock (_lock)
                        {
                            if (_instance == null)
                            {
                                _instance = new HistroyManager();
                            }
                        }
                    }
                    return _instance;
                }
            }

            /// <summary>
            /// 로그를 기록합니다.
            /// </summary>
            /// <param name="level">로그 레벨</param>
            /// <param name="category">카테고리 (예: "Login", "Printer", "FileIO")</param>
            /// <param name="message">로그 메시지</param>
            /// <param name="user">사용자 정보 (선택)</param>
            /// <param name="details">상세 정보 (선택)</param>
            public void WriteLog(LogLevel level, string category, string message, string user = "", string details = "")
            {
                try
                {
                    var logEntry = new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = level,
                        Category = category,
                        Message = message,
                        User = user,
                        Details = details
                    };

                    string logLine = logEntry.ToString() + Environment.NewLine;

                    // 파일 크기 확인 및 로테이션
                    CheckAndRotateLogFile();

                    // 스레드 안전하게 파일에 쓰기
                    lock (_lock)
                    {
                        File.AppendAllText(_logFileName, logLine, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    // 로그 기록 실패 시 콘솔에 출력 (디버그 모드)
                    System.Diagnostics.Debug.WriteLine($"로그 기록 실패: {ex.Message}");
                }
            }

            /// <summary>
            /// Info 레벨 로그 기록
            /// </summary>
            public void LogInfo(string category, string message, string user = "", string details = "")
            {
                WriteLog(LogLevel.Info, category, message, user, details);
            }

            /// <summary>
            /// Warning 레벨 로그 기록
            /// </summary>
            public void LogWarning(string category, string message, string user = "", string details = "")
            {
                WriteLog(LogLevel.Warning, category, message, user, details);
            }

            /// <summary>
            /// Error 레벨 로그 기록
            /// </summary>
            public void LogError(string category, string message, string user = "", string details = "")
            {
                WriteLog(LogLevel.Error, category, message, user, details);
            }

            /// <summary>
            /// Debug 레벨 로그 기록
            /// </summary>
            public void LogDebug(string category, string message, string user = "", string details = "")
            {
                WriteLog(LogLevel.Debug, category, message, user, details);
            }

            /// <summary>
            /// 로그 파일 크기를 확인하고 필요시 로테이션
            /// </summary>
            private void CheckAndRotateLogFile()
            {
                try
                {
                    if (File.Exists(_logFileName))
                    {
                        var fileInfo = new FileInfo(_logFileName);
                        if (fileInfo.Length >= _maxLogFileSize)
                        {
                            // 기존 파일을 백업
                            string backupFileName = Path.Combine(_logDirectory,
                                $"HDMIndoPE_{DateTime.Now:yyyyMMdd}_{DateTime.Now:HHmmss}.log");
                            File.Move(_logFileName, backupFileName);

                            // 오래된 로그 파일 삭제
                            CleanOldLogFiles();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"로그 파일 로테이션 실패: {ex.Message}");
                }
            }

            /// <summary>
            /// 오래된 로그 파일 삭제
            /// </summary>
            private void CleanOldLogFiles()
            {
                try
                {
                    var logFiles = Directory.GetFiles(_logDirectory, "HDMIndoPE_*.log")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.CreationTime)
                        .ToList();

                    if (logFiles.Count > _maxLogFiles)
                    {
                        foreach (var file in logFiles.Skip(_maxLogFiles))
                        {
                            try
                            {
                                File.Delete(file.FullName);
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"오래된 로그 파일 삭제 실패: {ex.Message}");
                }
            }

            /// <summary>
            /// 로그를 읽어옵니다.
            /// </summary>
            /// <param name="count">읽어올 최대 로그 수 (0이면 전체)</param>
            /// <param name="level">필터링할 로그 레벨 (null이면 전체)</param>
            /// <param name="category">필터링할 카테고리 (null이면 전체)</param>
            /// <returns>로그 엔트리 리스트</returns>
            public List<LogEntry> ReadLogs(int count = 0, LogLevel? level = null, string category = null)
            {
                var logs = new List<LogEntry>();

                try
                {
                    if (!File.Exists(_logFileName))
                        return logs;

                    var lines = File.ReadAllLines(_logFileName, Encoding.UTF8);
                    var entries = new List<LogEntry>();

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        try
                        {
                            var entry = ParseLogLine(line);
                            if (entry != null)
                            {
                                // 필터링
                                if (level.HasValue && entry.Level != level.Value)
                                    continue;
                                if (!string.IsNullOrEmpty(category) && entry.Category != category)
                                    continue;

                                entries.Add(entry);
                            }
                        }
                        catch
                        {
                            // 파싱 실패한 라인은 무시
                        }
                    }

                    // 최신순으로 정렬
                    entries = entries.OrderByDescending(e => e.Timestamp).ToList();

                    // 개수 제한
                    if (count > 0)
                    {
                        logs = entries.Take(count).ToList();
                    }
                    else
                    {
                        logs = entries;
                    }
                }
                catch (Exception ex)
                {
                    LogError("HistroyManager", $"로그 읽기 실패: {ex.Message}");
                }

                return logs;
            }

            /// <summary>
            /// 로그 라인을 파싱합니다.
            /// </summary>
            private LogEntry ParseLogLine(string line)
            {
                try
                {
                    // [2024-01-01 12:00:00.000] [INFO] [User: admin] [Login] 메시지 | Details: 상세정보
                    var entry = new LogEntry();

                    // 타임스탬프 파싱
                    int timestampEnd = line.IndexOf(']');
                    if (timestampEnd > 0)
                    {
                        string timestampStr = line.Substring(1, timestampEnd - 1);
                        if (DateTime.TryParse(timestampStr, out DateTime timestamp))
                        {
                            entry.Timestamp = timestamp;
                        }
                    }

                    // 레벨 파싱
                    int levelStart = line.IndexOf('[', timestampEnd + 1);
                    int levelEnd = line.IndexOf(']', levelStart + 1);
                    if (levelStart > 0 && levelEnd > 0)
                    {
                        string levelStr = line.Substring(levelStart + 1, levelEnd - levelStart - 1);
                        if (Enum.TryParse<LogLevel>(levelStr, true, out LogLevel level))
                        {
                            entry.Level = level;
                        }
                    }

                    // User 파싱 (선택적)
                    int userStart = line.IndexOf("[User: ", levelEnd + 1);
                    if (userStart > 0)
                    {
                        int userEnd = line.IndexOf(']', userStart + 1);
                        if (userEnd > 0)
                        {
                            entry.User = line.Substring(userStart + 7, userEnd - userStart - 7);
                        }
                    }

                    // Category 파싱
                    int categoryStart = line.IndexOf('[', levelEnd + 1);
                    if (categoryStart > 0 && (userStart < 0 || categoryStart < userStart))
                    {
                        int categoryEnd = line.IndexOf(']', categoryStart + 1);
                        if (categoryEnd > 0)
                        {
                            entry.Category = line.Substring(categoryStart + 1, categoryEnd - categoryStart - 1);
                        }
                    }

                    // Message와 Details 파싱
                    int messageStart = line.IndexOf("] ", categoryStart > 0 ? categoryEnd + 1 : levelEnd + 1) + 2;
                    if (messageStart > 1)
                    {
                        int detailsStart = line.IndexOf(" | Details: ", messageStart);
                        if (detailsStart > 0)
                        {
                            entry.Message = line.Substring(messageStart, detailsStart - messageStart).Trim();
                            entry.Details = line.Substring(detailsStart + 12).Trim();
                        }
                        else
                        {
                            entry.Message = line.Substring(messageStart).Trim();
                        }
                    }

                    return entry;
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// 특정 날짜의 로그 파일 경로를 반환합니다.
            /// </summary>
            public string GetLogFilePath(DateTime date)
            {
                return Path.Combine(_logDirectory, $"HDMIndoPE_{date:yyyyMMdd}.log");
            }

            /// <summary>
            /// 오늘의 로그 파일 경로를 반환합니다.
            /// </summary>
            public string GetTodayLogFilePath()
            {
                return _logFileName;
            }
        }
    }
}
