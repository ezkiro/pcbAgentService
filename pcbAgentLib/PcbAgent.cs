using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using pcbAgentLib.httpSender;
using pcbAgentLib.gamePatchCheck;
using pcbAgentLib.protocol;

namespace pcbAgentLib.pcbAgent
{
    public sealed class PcbAgent
    {
        private static string AGENT_VERSION = "20170810";
        private static string API_HOST_ADDRESS = "www.e-gpms.co.kr";
        //private static string API_HOST_ADDRESS = "localhost";
        private static string API_HOST_PORT = "80";
        private static int DELAY_TIME_SEC = 70;

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

            Console.WriteLine("[sendPcbGamePatchToMaster] pcbGamePatch result:{0}", MsgConverter.pack<PcbGamePatch>(pcbGamePatch));

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

        public PcbGame executeGameCommand(GameCommand gameCmd)
        {
            Console.WriteLine("[executeGameCommand] gameCmd:" + gameCmd.ToString());

            //find exefile from expectedPaths
            List <string> targetPaths = new List<string>();

            foreach (InstallPath aPath in gameCmd.expectedPaths)
            {
                if (aPath.type.Equals("exe"))
                {
                    targetPaths.Add(aPath.path);
                }
            }

            QuickFinder finder = new QuickFinder(gameCmd.exeFile, targetPaths);
            string foundFile = finder.findInAllDrive();

            if (foundFile == null) return null;

            //check verify type
            if (gameCmd.verifyType.Equals("INSTALL"))
            {
                return new PcbGame(gameCmd.gsn, foundFile, "N/A", VersionChecker.checkLastWriteTime(foundFile));
            }
            else
            {
                //find verfile from expectedPaths
                List<string> verTargetPaths = new List<string>();
                foreach (InstallPath aPath in gameCmd.expectedPaths)
                {
                    if (aPath.type.Equals("ver"))
                    {
                        verTargetPaths.Add(aPath.path);
                    }
                }

                //verfile을 위한 path가 없다면 exefile의 path에서 찾는다.
                if (verTargetPaths.Count == 0)
                {
                    //foundFile에서 exefile명을 제외한 path
                    verTargetPaths.Add(foundFile.Substring(0, foundFile.Length - gameCmd.exeFile.Length));
                }

                QuickFinder verFinder = new QuickFinder(gameCmd.verFile, verTargetPaths);
//                string foundVerFile = verFinder.findRInAllDrive();
                string foundVerFile = verFinder.findInAllDrive();
                if (foundVerFile == null) return null;

                //check version file

                if (gameCmd.verFileFmt.Equals("XML"))
                {
                    return new PcbGame(gameCmd.gsn, foundFile, VersionChecker.checkXmlFile(foundVerFile), VersionChecker.checkLastWriteTime(foundFile));
                }
                else if (gameCmd.verFileFmt.Equals("JSON"))
                {
                    return new PcbGame(gameCmd.gsn, foundFile, VersionChecker.checkJsonFile(foundVerFile), VersionChecker.checkLastWriteTime(foundFile));
                }
                else if (gameCmd.verFileFmt.Equals("BIN"))
                {
                    return new PcbGame(gameCmd.gsn, foundFile, VersionChecker.checkLastWriteTime(foundVerFile), VersionChecker.checkLastWriteTime(foundFile));
                }
                else
                {
                    //no process
                    Console.WriteLine("[executeGameCommand] not supprot version formant:{0}", gameCmd.verFileFmt);
                }
            }

            return null;
        }

        //핵심 mission들 수행 test
        public string executeMissions(bool isForce)
        {
            //10초 ~ 1분 사이에 실행됨 실행
            Random random = new Random();
            Thread.Sleep(random.Next(20, DELAY_TIME_SEC) * 1000);

            if (!isForce && checkGamePatchPass())
            {
                return "PASS";
            }

            //request Agent Command
            AgentCommand agentCmd = requestAgentCommand();

            PcbGamePatch pcbGamePatch = new PcbGamePatch();
            pcbGamePatch.pcbGames = new List<PcbGame>();
            pcbGamePatch.version = PcbAgent.AGENT_VERSION;

            //GameCommand 처리
            foreach (GameCommand gameCmd in agentCmd.gameCommands)
            {
                PcbGame pcbGame = executeGameCommand(gameCmd);
                if (pcbGame != null)
                {
                    pcbGamePatch.pcbGames.Add(pcbGame);
                }
            }

            //gamepatch 정보 전송
            String result = sendPcbGamePatchToMaster(pcbGamePatch);
            Debug.WriteLine("[executeMissions] sendPcbGamePatchToMaster result:" + result);
            return result;
        }
    }
}
