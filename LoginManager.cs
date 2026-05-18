using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static WIA_ViewerProgram.HistoryManager;

namespace WIA_ViewerProgram
{
    public class LoginItem
    {
        public string mode { get; set; }
        public string userid { get; set; }
        public string pw { get; set; }
    }
    public class LoginDataRoot
    {
        public List<LoginItem> LoginData { get; set; }
    }

    internal class LoginManager
    {
        public LoginDataRoot JsonLoginData;
        public string ProgramLoginMode;// 프로그램의 기본 로그인 모드
        public bool BoolLoginCheck;
        //json파일에 있는 작업자별 id비번 가져오기
        private string OperatorID;
        private string OperatorPW;
        private string AdminID;
        private string AdminPW;
        private string MasterID;
        private string MasterPW;
        public string UserInputID;
        public string UserInputPW;
        public string FixedProgramLoginMode;
        private HistroyManager logger = HistroyManager.Instance;
        public LoginManager()
        {
            ProgramLoginMode = "-";// 프로그램
            ReadJson();
            BoolLoginCheck = false;
        }
        public void ReadJson()
        {
            try
            {
                const string loginDataPath = "./LoginData.json";
                if (!File.Exists(loginDataPath))
                {
                    JsonLoginData = new LoginDataRoot
                    {
                        LoginData = new List<LoginItem>
                        {
                            new LoginItem { mode = "operator", userid = "operator", pw = "0000" },
                            new LoginItem { mode = "admin", userid = "admin", pw = "0000" },
                            new LoginItem { mode = "master", userid = "master", pw = "0000" }
                        }
                    };

                    string defaultJson = System.Text.Json.JsonSerializer.Serialize(JsonLoginData, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(loginDataPath, defaultJson);
                    logger.LogWarning("Login", "LoginData.json이 없어 기본 계정 파일을 생성했습니다.");
                }

                string json = File.ReadAllText(loginDataPath);
                JsonLoginData = JsonConvert.DeserializeObject<LoginDataRoot>(json);
                if (JsonLoginData?.LoginData == null || JsonLoginData.LoginData.Count < 3)
                {
                    throw new InvalidDataException("LoginData.json 형식이 올바르지 않거나 계정 데이터가 부족합니다.");
                }
                OperatorID = JsonLoginData.LoginData[0].userid;
                OperatorPW = JsonLoginData.LoginData[0].pw;
                AdminID = JsonLoginData.LoginData[1].userid;
                AdminPW = JsonLoginData.LoginData[1].pw;
                MasterID = JsonLoginData.LoginData[2].userid;
                MasterPW = JsonLoginData.LoginData[2].pw;
                logger.LogInfo("Login", "로그인 데이터 파일 로드 성공");
            }
            catch (FileNotFoundException ex)
            {
                logger.LogError("Login", $"로그인 데이터 파일을 찾을 수 없습니다: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError("Login", $"로그인 데이터 파일 읽기 실패: {ex.Message}", "", ex.StackTrace);
                throw;
            }
        }

        public void check_id_pw()
        {
            if (ProgramLoginMode == "operator")
            {
                if (UserInputID ==OperatorID)
                {
                    if (UserInputPW == OperatorPW)
                    {
                        BoolLoginCheck = true;
                        logger.LogInfo("Login", $"로그인 성공 - 모드: {ProgramLoginMode}", "operator");
                    }
                    else
                    {
                        logger.LogWarning("Login", $"로그인 실패 - 모드: {ProgramLoginMode}, 이유: 비밀번호 불일치", "operator");
                        var result = MessageBox.Show(
                           "비밀번호 확인해주세요",
                           "비밀번호 확인",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Question);
                    }

                }
                else
                {
                    logger.LogWarning("Login", $"로그인 실패 - 모드: {ProgramLoginMode}, ID: {UserInputID}, 이유: 아이디 불일치", UserInputID);
                    var result = MessageBox.Show(
                       "아이디 비밀번호 확인해주세요",
                       "아이디 비밀번호 확인",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Question);
                }

            }
            else if (ProgramLoginMode == "admin")
            {
                if (UserInputID == AdminID)
                {
                    if (UserInputPW == AdminPW)
                    {
                        BoolLoginCheck = true;
                        logger.LogInfo("Login", $"로그인 성공 - 모드: {ProgramLoginMode}, ID: {UserInputID}", UserInputID);
                    }
                    else
                    {
                        logger.LogWarning("Login", $"로그인 실패 - 모드: {ProgramLoginMode}, ID: {UserInputID}, 이유: 비밀번호 불일치", UserInputID);
                        var result = MessageBox.Show(
                         "아이디 비밀번호 확인해주세요",
                         "아이디 비밀번호 확인",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Question);
                    }
                }
                else
                {
                    logger.LogWarning("Login", $"로그인 실패 - 모드: {ProgramLoginMode}, ID: {UserInputID}, 이유: 아이디 불일치", UserInputID);
                    var result = MessageBox.Show(
                       "아이디 비밀번호 확인해주세요",
                       "아이디 비밀번호 확인",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Question);
                }

            }
            else if (ProgramLoginMode == "master")
            {
                if (UserInputID == MasterID)
                {
                    if (UserInputPW == MasterPW)
                    {
                        BoolLoginCheck = true;
                        logger.LogInfo("Login", $"로그인 성공 - 모드: {ProgramLoginMode}, ID: {UserInputID}", UserInputID);
                    }
                    else
                    {
                        logger.LogWarning("Login", $"로그인 실패 - 모드: {ProgramLoginMode}, ID: {UserInputID}, 이유: 비밀번호 불일치", UserInputID);
                        var result = MessageBox.Show(
                         "아이디 비밀번호 확인해주세요",
                         "아이디 비밀번호 확인",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Question);
                    }
                }
                else
                {
                    logger.LogWarning("Login", $"로그인 실패 - 모드: {ProgramLoginMode}, ID: {UserInputID}, 이유: 아이디 불일치", UserInputID);
                    var result = MessageBox.Show(
                       "아이디 비밀번호 확인해주세요",
                       "아이디 비밀번호 확인",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Question);
                }
            }
            else
            {
                logger.LogError("Login", $"잘못된 로그인 모드: {ProgramLoginMode}");
                var result = MessageBox.Show(
                       "모드가 수정되었습니다. 모드확인이 필요합니다.",
                       "프로그램 에러",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Question);
            }
        }

        public void pwchane()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(JsonLoginData, new JsonSerializerOptions
                {
                    WriteIndented = true   // 보기 좋게 들여쓰기
                });

                File.WriteAllText("./LoginData.json", json);
                logger.LogInfo("Login", "비밀번호 변경 완료", FixedProgramLoginMode ?? ProgramLoginMode);
            }
            catch (Exception ex)
            {
                logger.LogError("Login", $"비밀번호 변경 실패: {ex.Message}", FixedProgramLoginMode ?? ProgramLoginMode, ex.StackTrace);
                throw;
            }
        }

    }
}
