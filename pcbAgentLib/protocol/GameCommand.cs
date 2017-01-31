using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pcbAgentLib.protocol
{
    public class GameCommand
    {
        public string gsn { get; set; }
        public string verifyType { get; set; }
        public string exeFile { get; set; }
        public string verFile { get; set; }
        public string verFileFmt { get; set; }
        public string verKey { get; set; }

        public List<InstallPath> expectedPaths { get; set; }
    }
}
