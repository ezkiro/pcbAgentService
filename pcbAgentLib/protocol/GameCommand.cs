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

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("gsn:{0}", gsn)
              .AppendFormat(", verifyType:{0}", verifyType)
              .AppendFormat(", exeFile:{0}", exeFile)
              .AppendFormat(", verFile:{0}", verFile)
              .AppendFormat(", verFileFmt:{0}", verFileFmt)
              .AppendFormat(", verKey:{0}", verKey);

            return sb.ToString();
        }
    }
}
