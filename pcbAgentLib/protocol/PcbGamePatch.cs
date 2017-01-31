using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace pcbAgentLib.protocol
{
    // {"pcbGames":[{"gsn":"10", "name":"game1", "major":"1234", "minor":""},{"gsn":"20", "name":"game2", "major":"1111", "minor":""}]}
    public class PcbGamePatch
    {
        public string version { get; set; }
        public List<PcbGame> pcbGames { get; set; }
    }
}
