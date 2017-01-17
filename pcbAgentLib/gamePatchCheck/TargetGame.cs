using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace pcbAgentLib.gamePatchCheck
{
    /// <summary>
    /// 체크해야 게임들의 정보를 미리 hard 코딩해둔 정보
    /// GSN, exefile, 상위 dir name,  versionfile , version format, version keyword, isDirCheck, isVersionFileCheck
    /// TODO: API서버로 부터 받은 정보로 갱신이 되어야 한다.
    /// </summary>
    public class TargetGame
    {
        public string Gsn { get; set; }
        public string Exefile { get; set; }
        public string DirName { get; set; } //혹시 실행파일이 2개일 경우 한번더 판단하기 위해서
        public string VersionFile { get; set; }
        public Fileformat VersionFileFormat { get; set; }
        public string VersionKey { get; set; }
        public bool IsDirCheck { get; set; }
        public bool IsVerFileOtherPath { get; set; } // 실행파일과 버전파일이 분리되어 저장되는 경우 버전 파일을 검색해야 함

        public string ExeFilePath { get; set; }
        public string VerionFilePath { get; set; }

        public TargetGame(string gsn, string exefile, string dirName)
        {
            this.Gsn = gsn;
            this.Exefile = exefile;
            this.DirName = dirName;
            this.IsDirCheck = false;
            this.IsVerFileOtherPath = false;
        }
    }

    public enum Fileformat
    {
        JSON,
        XML,
        INI,
        TXT,
        BIN
    }

    public static class TargetGameFactory
    {
        public static List<TargetGame> buildAllGames()
        {
            List<TargetGame> targetGames = new List<TargetGame>();

            //for soulworker
            TargetGame targame1 = new TargetGame("1", "SWOS.exe", "soulworker");

            /*
            {
                "gameinfo": {
                    "game_no": "11",
                    "version": "0",
                    "existexceptfile" : "0"
                }
            }
            */
            targame1.VersionFile = "gamemanifest_11.upf";
            targame1.VersionFileFormat = Fileformat.JSON;
            targame1.VersionKey = "gameinfo:version";
            targame1.IsVerFileOtherPath = true;

            targetGames.Add(targame1);

            //for free style
            TargetGame targame2 = new TargetGame("2", "FreeStyle.exe", "FreeStyle");
            targame2.VersionFile = "Fullfileinfo.patch";
            targame2.VersionFileFormat = Fileformat.BIN;

            targetGames.Add(targame2);

            //for free style2
            TargetGame targame3 = new TargetGame("3", "FreeStyle2.exe", "FS2");
            targame3.VersionFile = "Fullfileinfo.patch";
            targame3.VersionFileFormat = Fileformat.BIN;

            targetGames.Add(targame3);

            //for free style bootball Z
            TargetGame targame4 = new TargetGame("4", "FSeFootball.exe", "FreestyleFootballZ");

            targetGames.Add(targame4);

            //for crossfile
            TargetGame targame5 = new TargetGame("5", "crossfire.exe", "CrossFire");

            /*
            {
                "gameinfo": {
                    "game_no": "8",
                    "version": "16",
                    "existexceptfile" : "0"
                }
            }
            */
            targame5.VersionFile = "gamemanifest_8.upf";
            targame5.VersionFileFormat = Fileformat.JSON;
            targame5.VersionKey = "gameinfo:version";
            targame5.IsVerFileOtherPath = true;

            targetGames.Add(targame5);


            //for crossfire naver  아래 crossfire 보다 먼저 넣어야 문제가 없다.
            TargetGame targame6 = new TargetGame("6", "crossfire.exe", "CrossFire_naver");
            targame6.VersionFile = "CFFSVersion.xml";
            targame6.VersionFileFormat = Fileformat.XML;
            targame6.VersionKey = "version";
            targame6.IsDirCheck = true;

            targetGames.Add(targame6);

            return targetGames;
        }

        /// <summary>
        /// 검색된 파일 리스트를 받아서 target game에 맞도록 검증하고 맵핑하는 작업
        /// 중복된 파일들이 오는 경우도 처리를 해야 한다.
        /// </summary>
        /// <param name="foundFiles"></param>
        /// <returns></returns>
        public static List<TargetGame> matchFoundExeFileAndVerFile(List<string> foundFiles, List<string> foundVerFiles, List<TargetGame> targetGames)
        {
            //key:gsn, value: filepath
            //Dictionary<string, string> foundExeFileMap = new Dictionary<string, string>();

            foreach (TargetGame aGame in targetGames)
            {
                //후보 파일들을 찾아낸다.
                var candidateFiles = foundFiles.Where<string>(path => path.Contains(aGame.Exefile));

                foreach (string path in candidateFiles) //crossfire 만 복수개로 나올수 있다.
                {
                    //Dir을 체크가 필요하면 체크
                    if (aGame.IsDirCheck)
                    {
                        if (path.Contains(aGame.DirName))
                        {                            
                            aGame.ExeFilePath = path;
                            //Console.WriteLine("[matchFoundExeFileAndVerFile] check upper dir aGame:{0} candidateFile:{1}", aGame.Gsn, aGame.ExeFilePath);
                            break;
                        }
                    }
                    else
                    {
                        aGame.ExeFilePath = path;
                        //Console.WriteLine("[matchFoundExeFileAndVerFile] aGame:{0} candidateFile:{1}", aGame.Gsn, aGame.ExeFilePath);
                        break;
                    }
                }

                if (aGame.ExeFilePath == null) continue;

                if (aGame.IsVerFileOtherPath)
                {
                    //version file match
                    var candidateVerFiles = foundVerFiles.Where<string>(path => path.Contains(aGame.VersionFile));

                    foreach (string path in candidateVerFiles)
                    {
                        //버전파일은 하나밖에 없다.
                        aGame.VerionFilePath = path;
                        //Console.WriteLine("[matchFoundExeFileAndVerFile] aGame:{0} candidateVerFile:{1}", aGame.Gsn, aGame.VerionFilePath);
                        break;
                    }
                }
                else
                {
                    //versionfile path가 실행파일과 동일한 경우
                    string gameInstallPath = aGame.ExeFilePath.Substring(0, aGame.ExeFilePath.Length - aGame.Exefile.Length);
                    aGame.VerionFilePath = gameInstallPath + aGame.VersionFile;
                    //Console.WriteLine("[matchFoundExeFileAndVerFile] aGame:{0} VerionFilePath:{1}", aGame.Gsn, aGame.VerionFilePath);
                }
            }

            return targetGames;
        }
    }
}
