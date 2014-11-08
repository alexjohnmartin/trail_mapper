using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using trail_mapper.ViewModels;

namespace trail_mapper
{
    internal class UploadHelper
    {
        public void UploadTrailMap(TrailMap trailMap)
        {
            _postBody = JsonConvert.SerializeObject(trailMap);

            var uri = "http://192.168.0.16:3000/upload";
            var request = (HttpWebRequest)WebRequest.Create(uri);
            //request.ContentType = "application/x-www-form-urlencoded";
            request.ContentType = "application/json";
            request.Method = "POST";
            request.BeginGetRequestStream(ReadCallback, request);
        }

        private string _postBody = string.Empty;
        private void ReadCallback(IAsyncResult asynchronousResult)
        {
            var request = (HttpWebRequest)asynchronousResult.AsyncState;

            using (var postStream = request.EndGetRequestStream(asynchronousResult))
            using (var memStream = new MemoryStream())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(_postBody);

                memStream.Write(bytes, 0, bytes.Length);
                memStream.Position = 0;
                var tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                postStream.Write(tempBuffer, 0, tempBuffer.Length);
            }

            request.BeginGetResponse(ResponseCallback, request);
        }

        private void ResponseCallback(IAsyncResult asynchronousResult)
        {
            var request = (HttpWebRequest)asynchronousResult.AsyncState;

            try
            {
                using (var resp = (HttpWebResponse)request.EndGetResponse(asynchronousResult))
                using (var streamResponse = resp.GetResponseStream())
                using (var streamRead = new StreamReader(streamResponse))
                {
                    string responseString = streamRead.ReadToEnd();
                    //Console.WriteLine(responseString);
                    //txtStatus.Text = "Upload status: " + responseString;
                }
            }
            catch (Exception)
            {
                //TODO:log error
            }
        }

    }
}
