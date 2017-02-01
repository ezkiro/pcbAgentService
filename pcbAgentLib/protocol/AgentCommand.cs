using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pcbAgentLib.protocol
{
    public class AgentCommand
    {
        public string cmd { get; set; }
        public List<GameCommand> gameCommands { get; set; }
    }
}
