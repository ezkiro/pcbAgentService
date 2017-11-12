using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pcbAgentLib.gamePatchCheck
{
    public class StoveVersion
    {
        /*
        {
            "gameinfo": {
                "game_no": "11",
                "version": "19",
                "existexceptfile" : "0"
            }
        }
        */

        public class StoveItem
        {
            public string game_no { get; set; }
            public string version { get; set; }
            public string existexceptfile { get; set; }
        }

        public StoveItem gameinfo { get; set; }

        public string getVersion()
        {
            return gameinfo.version;
        }
    }

    public class EpicVersion
    {
        /*
        {
            "InstallationList": [
                {
                    "InstallLocation": "D:\\Program Files (x86)\\Epic Games\\Paragon",
                    "AppName": "OrionLive",
                    "AppID": 1,
                    "AppVersion": "++Orion+Release-44.1-CL-3736488-Windows"
                }
            ]
        }
        */

        public class EpicItem
        {
            public string InstallLocation { get; set; }
            public string AppName { get; set; }
            public int AppID { get; set; }
            public string AppVersion { get; set; }
        }

        public List<EpicItem> InstallationList { get; set; }

        public string getVersion(string AppName)
        {
            foreach (EpicItem item in InstallationList)
            {
                if (item.AppName.Equals(AppName))
                {
                    return item.AppVersion;
                }
            }

            return null;
        }
    }
}
