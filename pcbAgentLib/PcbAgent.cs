using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Diagnostics;
using pcbAgentLib.httpSender;
using pcbAgentLib.gamePatchCheck;

namespace pcbAgentLib.pcbAgent
{
    public class PcbGame
    {
        public PcbGame(string gsn, string name, string major, string minor)
        {
            this.gsn = gsn;
            this.name = name;
            this.major = major;
            this.minor = minor;
        }
        public string gsn { get; set; }
        public string name { get; set; }
        public string major { get; set; }
        public string minor { get; set; }
    }

    // {"pcbGames":[{"gsn":"10", "name":"game1", "major":"1234", "minor":""},{"gsn":"20", "name":"game2", "major":"1111", "minor":""}]}
    public class PcbGamePatch
    {
        public string version { get; set; }
        public List<PcbGame> pcbGames { get; set; }
    }

    public sealed class PcbAgent
    {
        private static string AGENT_VERSION = "20170116";
//        private static string API_HOST_ADDRESS = "61.34.180.89";
        private static string API_HOST_ADDRESS = "www.e-gpms.co.kr";
        private static string API_HOST_PORT = "80";

        //TODO: API request 가 3개이상 늘어나면 별도 class로 분리
        private static string REQUEST_GAME_PATCH = "/agent/gamepatch?client_ip="; // + {client_ip}
        private static string REQUEST_PRECHECK = "/agent/check"; // + { client_ip}

        private static string buildUriForApiRequest(string urlPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("http://")
              .Append(API_HOST_ADDRESS)
              .Append(":")
              .Append(API_HOST_PORT)
              .Append(urlPath);
            return sb.ToString();
        }

        //for singleton
        //reference https://msdn.microsoft.com/en-us/library/ff650316.aspx
        private static readonly PcbAgent _instance = new PcbAgent();

        private PcbAgent() { }

        public static PcbAgent Instance
        {
            get
            {
                return _instance;
            }
        }

        public static string getLocalIPAddress()
        {
            if(!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return "127.0.0.1";
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public PcbGamePatch buildPcbGamePatch()
        {
            //target game 목록 생성
            List<TargetGame> targetGames = TargetGameFactory.buildAllGames();

            //target game의 exefile들을 검색하여 위치 파악
            List<string> exeFiles = new List<string>();

            foreach (TargetGame aGame in targetGames)
            {
                exeFiles.Add(aGame.Exefile);
            }

            FileFinder finder = new FileFinder("*.exe", exeFiles);
            List<string> foundFiles = finder.findFilePath();

            Dictionary<string, string> foundExeFileMap = TargetGameFactory.buildFoundExeFileMap(foundFiles, targetGames);

            //target game 별로 version 체크
            PcbGamePatch pcbGamePatch = new PcbGamePatch();
            pcbGamePatch.pcbGames = new List<PcbGame>();

            foreach (TargetGame aGame in targetGames)
            {
                //exefile 존재 여부 체크
                string exeFilePath = null;
                if (!foundExeFileMap.TryGetValue(aGame.Gsn, out exeFilePath))
                {
                    Console.WriteLine("[buildPcbGamePatch] exeFile is not found! gsn:{0}, exefile:{1}", aGame.Gsn, aGame.Exefile);
                    continue;
                }

                //version file 여부 체크
                if (aGame.VersionFile == null)
                {
                    //설치여부만 체크
                    pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, "N/A", VersionChecker.checkLastWriteTime(exeFilePath)));
                }
                else
                {
                    //version 파일 path는 실행파일과 디렉토리 위치가 동일하다고 가정
                    string gameInstallPath = exeFilePath.Substring(0, exeFilePath.Length - aGame.Exefile.Length);
                    string versionFilePath = gameInstallPath + aGame.VersionFile;

                    //string versionFilePath = FileFinder.findOneFilePath(gameInstallPath, aGame.VersionFile);

                    Console.WriteLine("[buildPcbGamePatch] versionFilePath:{0}", versionFilePath);

                    //버전파일 체크
                    switch (aGame.VersionFileFormat)
                    {
                        case Fileformat.XML:
                            pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, VersionChecker.checkXmlFile(versionFilePath), VersionChecker.checkLastWriteTime(exeFilePath)));
                            break;
                        case Fileformat.JSON:
                            pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, VersionChecker.checkJsonFile(versionFilePath), VersionChecker.checkLastWriteTime(exeFilePath)));
                            break;
                        case Fileformat.BIN:
                            pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, VersionChecker.checkLastWriteTime(versionFilePath), VersionChecker.checkLastWriteTime(exeFilePath)));
                            break;
                        default:
                            //no process
                            Console.WriteLine("[buildPcbGamePatch] not supprot version formant:{0}", aGame.VersionFileFormat);
                            break;
                    }

                }
            }

            //set agent version
            pcbGamePatch.version = PcbAgent.AGENT_VERSION;

            return pcbGamePatch;
        }

        public string sendPcbGamePatchToMaster(PcbGamePatch pcbGamePatch)
        {
            if (pcbGamePatch == null) return "pcbGamePatch is null!";
            if (pcbGamePatch.pcbGames.Count == 0) return "pcbGamePatch.pcbGames is empty!";

            var jsonObj = new JavaScriptSerializer().Serialize(pcbGamePatch);

            string urlPath = PcbAgent.buildUriForApiRequest(PcbAgent.REQUEST_GAME_PATCH + getLocalIPAddress());

            return HttpSender.requestJson(urlPath, jsonObj.ToString());
        }

        public bool checkGamePatchPass()
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();

            queryParams.Add("client_ip", getLocalIPAddress());

            string urlPath = PcbAgent.buildUriForApiRequest(PcbAgent.REQUEST_PRECHECK);

            string result = HttpSender.requestNormal(urlPath, "GET", queryParams);

            Console.WriteLine("[checkGamePatchPass] checkGamePatchPass result:{0}", result);

            if ("CHECK".Equals(result))
            {
                return false;
            }

            //통신 오류가 발생해도 GamePatch check 를 pass 시킨다.
            return true;
        }

        //핵심 mission들 수행 test
        public void executeMissions(bool isSend)
        {
            //gamepatch pass 가능 여부 체크
            if (isSend && checkGamePatchPass())
            {
                Console.WriteLine("[executeMissions] checkGamePatchPass PASS!!");
                return;
            }

            //gamepatch 정보 전송
            PcbGamePatch pcbGamePatch = PcbAgent.Instance.buildPcbGamePatch();

            var jsonObj = new JavaScriptSerializer().Serialize(pcbGamePatch);

            Console.WriteLine("[executeMissions] buildGamePatch result:{0}", jsonObj.ToString());

            if (!isSend) return;

            String result = PcbAgent.Instance.sendPcbGamePatchToMaster(pcbGamePatch);

            Console.WriteLine("[executeMissions] sendAndReceive result:{0}", result);
        }
    }
}
