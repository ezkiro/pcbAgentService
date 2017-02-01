using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace pcbAgentLib.protocol
{
    public class MsgConverter
    {
        public static T unpack<T>(string jsonResponse)
        {
            T result = new JavaScriptSerializer().Deserialize<T>(jsonResponse);
            return result;
        }

        public static string pack<T>(T obj)
        {
            var jsonObj = new JavaScriptSerializer().Serialize(obj);
            return jsonObj.ToString();
        }
    }
}
