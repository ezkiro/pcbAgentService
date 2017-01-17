using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;
using System.Xml;
using System.Diagnostics;

namespace pcbAgentLib.gamePatchCheck
{
    /// <summary>
    /// game version을 체크하는 class 기능은 크게 json/xml 형식의 version 파일 체크 및 수정 날짜 체크
    /// </summary>
    public static class VersionChecker
    {
        public static string readTxtFile(string file)
        {
            //Console.WriteLine("[VersionChecker] readTxtFile filePath:{0}", file);

            StreamReader reader = new StreamReader(file);
            try
            {
                string result = reader.ReadToEnd();

               //Console.WriteLine("[VersionChecker] readTxtFile ReadToEnd {0}", result);

                return result;
            }
            catch (Exception excpt)
            {
                Console.WriteLine("[VersionChecker] readTxtFile exception {0}:{1}", excpt.GetType(), excpt.Message);
                //오류발생시 null 을  반환하도록 한다.
                return null;
            }

            finally
            {
                if(reader != null)
                    reader.Close();
            }
        }

        //Stove 전용
        public static string checkJsonFile(string file)
        {
            string jsonString = readTxtFile(file);

            if (jsonString == null) return null;

            Dictionary<string, object> jsonObj = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(jsonString);

            Dictionary<string, object> gameinfo = jsonObj["gameinfo"] as Dictionary<string, object>;

            return (gameinfo == null) ? null: gameinfo["version"] as string;
        }

        //일단 crossfire 전용
        public static string checkXmlFile(string file)
        {
            string xmlString = readTxtFile(file);

            if (xmlString == null) return null;

            string version = null;

            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
            {
                reader.ReadToFollowing("version");
                reader.MoveToFirstAttribute(); //ver
                version = reader.Value;
            }

            return version;
        }

        //수정날짜를 체크해서 YYYYMMDD 형태로 반환
        public static string checkLastWriteTime(string file)
        {
            try
            {
                FileInfo targetFile = new FileInfo(file);
                return targetFile.LastWriteTime.ToString("yyyyMMdd");
            }
            catch(Exception excpt)
            {
                Console.WriteLine("[VersionChecker] readTxtFile exception {0}:{1}", excpt.GetType(), excpt.Message);
                //오류발생시 null 을  반환하도록 한다.
                return null;
            }
        }
    }
}
