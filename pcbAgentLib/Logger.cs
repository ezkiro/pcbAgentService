using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pcbAgentLib.pcbAgent
{
    public class Logger
    {
        public static void Log(string logMessage)
        {
            try
            {
                StreamWriter w = File.AppendText("log.txt");

                w.Write("[{0} {1}]", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
                w.WriteLine("{0}", logMessage);
                w.Close();

            }
            catch (System.Exception excpt)
            {

            }

        }
    }
}
