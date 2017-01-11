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
    /// GSN, exefile, 상위 dir name,  versionfile , version format, version keyword
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
        public string Etc { get; set; }

        public TargetGame(string gsn, string exefile, string dirName)
        {
            this.Gsn = gsn;
            this.Exefile = exefile;
            this.DirName = dirName;
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

            //for tales runner
            TargetGame targame5 = new TargetGame("5", "trgame.exe", "TalesRunner");

            targetGames.Add(targame5);

            //for crossfire naver  아래 crossfire 보다 먼저 넣어야 문제가 없다.
            TargetGame targame6 = new TargetGame("6", "crossfire.exe", "CrossFire_naver");
            targame6.VersionFile = "CFFSVersion.xml";
            targame6.VersionFileFormat = Fileformat.XML;
            targame6.VersionKey = "version";

            targetGames.Add(targame6);


            //for crossfile
            TargetGame targame7 = new TargetGame("7", "crossfire.exe", "CrossFire");

            /*
             *  <?xml version="1.0" encoding="utf-8"?>
                <version ver="17" shortcut="false"/> 
             */

            targame7.VersionFile = "CFFSVersion.xml";
            targame7.VersionFileFormat = Fileformat.XML;
            targame7.VersionKey = "version";

            targetGames.Add(targame7);


            return targetGames;
        }

        /// <summary>
        /// 검색된 파일 리스트를 받아서 target game에 맞도록 검증하고 맵핑하는 작업
        /// 중복된 파일들이 오는 경우도 처리를 해야 한다.
        /// </summary>
        /// <param name="foundFiles"></param>
        /// <returns></returns>
        public static Dictionary<string, string> buildFoundExeFileMap(List<string> foundFiles, List<TargetGame> targetGames)
        {
            //key:gsn, value: filepath
            Dictionary<string, string> foundExeFileMap = new Dictionary<string, string>();

            foreach (TargetGame aGame in targetGames)
            {
                var firstCheck = foundFiles.Where<string>(path => path.Contains(aGame.Exefile));

                List<string> doubleCheck = new List<string>();

                foreach (string path in firstCheck)
                {
                    Console.WriteLine("[buildFoundExeFileMap] aGame:{0} doubleCheck:{1}", aGame.Gsn, path);
                    doubleCheck.Add(path);
                }

                if (doubleCheck.Count == 0) {
                    continue; // next game
                }
                else if(doubleCheck.Count == 1)
                {
                    foundExeFileMap.Add(aGame.Gsn, doubleCheck[0]);
                }
                else if (doubleCheck.Count > 1)
                {
                    //중복된 경우는 DirName으로 한번더 필터링 한다.
                    foreach (string path in doubleCheck)
                    {
                        if (path.Contains(aGame.DirName))
                        {
                            Console.WriteLine("[buildFoundExeFileMap] aGame:{0} onlyOne:{1}", aGame.Gsn, path);
                            foundExeFileMap.Add(aGame.Gsn, path);

                            //넣은 것은 목록에서 제거한다. CrossFire 때문에 문제가 발생한다.
                            foundFiles.Remove(path);
                            break;
                        }
                    }
                }
            }

            return foundExeFileMap;
        }
    }
}
