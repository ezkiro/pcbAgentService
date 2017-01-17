using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace pcbAgentLib.httpSender
{
    public class HttpSender
    {
        public static string requestNormal(string uri, string method, Dictionary<string, string> paramsMap)
        {
            if(uri == null)
            {
                Console.WriteLine("requestNormal:: uri is null!");
            }

            string dataParams = "";

            if (paramsMap != null)
            {
                dataParams = buildQueryString(paramsMap);
            }

            HttpWebRequest request;

            if("GET".Equals(method))
            {
                request = (HttpWebRequest)WebRequest.Create(uri + "?" + dataParams);
                request.Method = "GET"; //기본값은 GET

                return sendAndRecv(request, null);
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";    // 기본값 "GET"
                request.ContentType = "application/json";

                return sendAndRecv(request, dataParams);
            }
        }

        public static string requestJson(string uri, string jsonString)
        {
            if (uri == null)
            {
                Console.WriteLine("requestJson:: uri is null!");
            }

            //Console.WriteLine("requestJson: uri:[{0}], jsonString:{1}", uri, jsonString);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.ContentType = "application/json";

            try
            {
                //jsonString의 경우 아래와 같이 보내야 한다.
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonString);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                // 요청, 응답 받기
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // 응답 Stream 읽기
                Stream stReadData = response.GetResponseStream();
                StreamReader srReadData = new StreamReader(stReadData, Encoding.Default);

                // 응답 Stream -> 응답 String 변환
                string strResult = srReadData.ReadToEnd();

                Console.WriteLine("requestJson result:{0}", strResult);
                return strResult;
            }
            catch (Exception e)
            {
                return e.Message;
            }

        }

        public static string buildQueryString(Dictionary<string, string> queryParams)
        {
            // POST, GET 보낼 데이터 입력
            StringBuilder dataParams = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in queryParams)
            {
                dataParams.Append(kvp.Key + "=" + kvp.Value + "&");
            }

            Console.WriteLine("buildQueryString:: queryString = {0}", dataParams.ToString());

            return dataParams.ToString();
        }

        private static string sendAndRecv(HttpWebRequest request, string dataParams)
        {
            try
            {
                if ("POST".Equals(request.Method))
                {
                    // 요청 String -> 요청 Byte 변환
                    byte[] byteDataParams = UTF8Encoding.UTF8.GetBytes(dataParams.ToString());
                    request.ContentLength = byteDataParams.Length;

                    // 요청 Byte -> 요청 Stream 변환
                    Stream stDataParams = request.GetRequestStream();
                    stDataParams.Write(byteDataParams, 0, byteDataParams.Length);
                    stDataParams.Close();
                }

                // 요청, 응답 받기
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // 응답 Stream 읽기
                Stream stReadData = response.GetResponseStream();
                StreamReader srReadData = new StreamReader(stReadData, Encoding.Default);

                // 응답 Stream -> 응답 String 변환
                string strResult = srReadData.ReadToEnd();

                //Console.WriteLine("sendAndRecv request:[{0}], response:{1}", request.RequestUri, strResult);
                return strResult;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
