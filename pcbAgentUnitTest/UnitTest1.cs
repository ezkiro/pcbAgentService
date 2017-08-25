using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pcbAgentLib.pcbAgent;
using pcbAgentLib.httpSender;
using pcbAgentLib.gamePatchCheck;
using pcbAgentLib.protocol;

namespace pcbAgentUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test_sendPcbGamePatchToMaster()
        {
            //for test
            PcbGamePatch pcbGamePatch = new PcbGamePatch();

            pcbGamePatch.pcbGames = new List<PcbGame>();

            pcbGamePatch.pcbGames.Add(new PcbGame("1", "game1", "100", "10"));
            pcbGamePatch.pcbGames.Add(new PcbGame("2", "game2", "200", "20"));
            pcbGamePatch.pcbGames.Add(new PcbGame("3", "game3", "300", "30"));
            pcbGamePatch.pcbGames.Add(new PcbGame("4", "game4", "400", "40"));
            pcbGamePatch.pcbGames.Add(new PcbGame("5", "game5", "500", "50"));

            String result = PcbAgent.Instance.sendPcbGamePatchToMaster(pcbGamePatch);
        }

        [TestMethod]
        public void Test_httpSender_requestNormal()
        {
            String ret = HttpSender.requestNormal("http://localhost:8080/agent/version", "GET", null);
        }

        [TestMethod]
        public void Test_getLocalIPAddress()
        {
            Console.WriteLine("ip address:{0}", PcbAgent.getLocalIPAddress());
        }

        [TestMethod]
        public void Test_findFile()
        {
            List<string> targetFiles = new List<string>();

            targetFiles.Add("Slugger.exe");
            targetFiles.Add("PUTTY.EXE");

            FileFinder finder = new FileFinder("*.exe", targetFiles);
            List<string> foundFiles = finder.findFilePath();

            foreach(string file in foundFiles)
            {
                Console.WriteLine("file path:{0}", file);
            }
        }

        [TestMethod]
        public void Test_executeMissions()
        {
            //gamepatch pass 가능 여부 체크
            //if (PcbAgent.Instance.checkGamePatchPass())
            //{
            //    Debug.WriteLine("[executeMissions] checkGamePatchPass PASS!!");
            //    return;
            //}

            PcbAgent.Instance.executeMissions(true);
        }

        [TestMethod]
        public void Test_requestAgentCommand()
        {
            AgentCommand agentCmd = PcbAgent.Instance.requestAgentCommand();

            Assert.AreNotEqual<AgentCommand>(null, agentCmd);

            Console.WriteLine("AgentCommand cmd:{0}", agentCmd.cmd);

            foreach (GameCommand gameCmd in agentCmd.gameCommands)
            {
                Console.WriteLine("GameCommand gsn:{0}, exefile:{1}", gameCmd.gsn, gameCmd.exeFile);

                foreach (InstallPath path in gameCmd.expectedPaths)
                {
                    Console.WriteLine("InstallPath gsn:{0}, path:{1}", path.gsn, path.path);
                }
            }
        }

        [TestMethod]
        public void Test_quickFinder()
        {
            List<string> targetPaths = new List<string>();

            targetPaths.Add("alba\\pcbagent\\games\\cave");
            targetPaths.Add("alba\\pcbagent\\games\\SW");


            QuickFinder finder = new QuickFinder("SoulWorker.exe", targetPaths);
            string foundFile = finder.findInAllDrive();

            Console.WriteLine("found file path:{0}", foundFile);

            QuickFinder finder2 = new QuickFinder("gamemanifest_11.upf", targetPaths);
            string foundFile2 = finder2.findInAllDrive();

            Console.WriteLine("found file path:{0}", foundFile2);
        }
    }
}
