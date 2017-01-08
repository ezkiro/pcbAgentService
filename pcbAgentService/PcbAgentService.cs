using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using pcbAgentLib.pcbAgent;


namespace pcbAgentService
{
    public partial class PcbAgentService : ServiceBase
    {
        private System.Timers.Timer _timer;

        public PcbAgentService()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("PcbAgentSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource("PcbAgentSource", "Application");
            }

            eventLog1.Source = "PcbAgentSource";
            eventLog1.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("PcbAgentService OnStart!!");

            //Set up a timer to trigger every minute.
            _timer = new System.Timers.Timer();
            _timer.Interval = 60000; // 60 seconds
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            _timer.Start();
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("PcbAgentService OnStop!!");
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            eventLog1.WriteEntry("PcbAgentService OnTimer event!!", EventLogEntryType.Information, 9000);
            _timer.Stop();
            _timer = null;

            if (PcbAgent.Instance.checkGamePatchPass())
            {
                eventLog1.WriteEntry("PcbAgentService GamePatch Pass!! client_ip:" + PcbAgent.getLocalIPAddress(), EventLogEntryType.Information, 9001);
                return;
            }

            string result = PcbAgent.Instance.sendPcbGamePatchToMaster(PcbAgent.Instance.buildPcbGamePatch());

            eventLog1.WriteEntry("PcbAgentService sendPcbGamePatchToMaster result:" + result, EventLogEntryType.Information, 9002);
        }
    }
}
