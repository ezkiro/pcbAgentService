using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using pcbAgentLib.httpSender;
using pcbAgentLib.gamePatchCheck;
using pcbAgentLib.protocol;

namespace pcbAgentLib.pcbAgent
{
    public sealed class PcbAgent
    {
        private static string AGENT_VERSION = "20170118";
//        private static string API_HOST_ADDRESS = "www.e-gpms.co.kr";
        private static string API_HOST_ADDRESS = "localhost";
        private static string API_HOST_PORT = "8080";

        //TODO: API request 가 3개이상 늘어나면 별도 class로 분리
        private static string REQUEST_GAME_PATCH = "/agent/gamepatch?client_ip="; // + {client_ip}
        private static string REQUEST_PRECHECK = "/agent/check"; // + { client_ip}
        private static string REQUEST_AGENT_COMMAND = "/agent/command"; // + { client_ip}

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
            List<string> verFiles = new List<string>();

            foreach (TargetGame aGame in targetGames)
            {
                exeFiles.Add(aGame.Exefile);
                if (aGame.IsVerFileOtherPath)
                {
                    verFiles.Add(aGame.VersionFile);
                }
            }

            FileFinder finder = new FileFinder("*.exe", exeFiles);
            List<string> foundFiles = finder.findFilePath();

            finder = new FileFinder("*.upf", verFiles);
            List<string> foundVerFiles = finder.findFilePath();

            //            Dictionary<string, string> foundExeFileMap = TargetGameFactory.buildFoundExeFileMap(foundFiles, targetGames);
            targetGames = TargetGameFactory.matchFoundExeFileAndVerFile(foundFiles, foundVerFiles, targetGames);

            //target game 별로 version 체크
            PcbGamePatch pcbGamePatch = new PcbGamePatch();
            pcbGamePatch.pcbGames = new List<PcbGame>();

            foreach (TargetGame aGame in targetGames)
            {
                //exefile 존재 여부 체크
                string exeFilePath = aGame.ExeFilePath;
                if (exeFilePath == null)
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
                    //string versionFilePath = FileFinder.findOneFilePath(gameInstallPath, aGame.VersionFile);

                    if (aGame.VerionFilePath == null) continue;

                    //Console.WriteLine("[buildPcbGamePatch] versionFilePath:{0}", aGame.VerionFilePath);

                    //버전파일 체크
                    switch (aGame.VersionFileFormat)
                    {
                        case Fileformat.XML:
                            pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, VersionChecker.checkXmlFile(aGame.VerionFilePath), VersionChecker.checkLastWriteTime(exeFilePath)));
                            break;
                        case Fileformat.JSON:
                            pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, VersionChecker.checkJsonFile(aGame.VerionFilePath), VersionChecker.checkLastWriteTime(exeFilePath)));
                            break;
                        case Fileformat.BIN:
                            pcbGamePatch.pcbGames.Add(new PcbGame(aGame.Gsn, aGame.Exefile, VersionChecker.checkLastWriteTime(aGame.VerionFilePath), VersionChecker.checkLastWriteTime(exeFilePath)));
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

            string urlPath = PcbAgent.buildUriForApiRequest(PcbAgent.REQUEST_GAME_PATCH + getLocalIPAddress());

            return HttpSender.requestJson(urlPath, MsgConverter.pack<PcbGamePatch>(pcbGamePatch));
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

        public AgentCommand requestAgentCommand()
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();

            queryParams.Add("client_ip", getLocalIPAddress());

            string urlPath = PcbAgent.buildUriForApiRequest(PcbAgent.REQUEST_AGENT_COMMAND);

            string result = HttpSender.requestNormal(urlPath, "GET", queryParams);

            Console.WriteLine("[requestAgentCommand] requestAgentCommand result:{0}", result);

            AgentCommand agentCmd = MsgConverter.unpack<AgentCommand>(result);

            return agentCmd;
        }

        //핵심 mission들 수행 test
        public void executeMissions(bool isSend)
        {
            //gamepatch 정보 전송
            PcbGamePatch pcbGamePatch = PcbAgent.Instance.buildPcbGamePatch();

            string jsonString = MsgConverter.pack<PcbGamePatch>(pcbGamePatch);

            Debug.WriteLine("[executeMissions] buildGamePatch result:" + jsonString);

            if (!isSend) return;

            String result = PcbAgent.Instance.sendPcbGamePatchToMaster(pcbGamePatch);

            Debug.WriteLine("[executeMissions] sendAndReceive result:" + result);
        }
    }
}
