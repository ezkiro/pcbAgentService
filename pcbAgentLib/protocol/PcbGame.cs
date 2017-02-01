using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pcbAgentLib.protocol
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
}
