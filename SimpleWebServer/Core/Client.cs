using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleWebServer.Core
{
    public class Client
    {
        private readonly TcpClient _currentTcpClient;
        private string _request = string.Empty;
        private readonly byte[] _buffer = new byte[1024];
        private int _count;

        private string RequestUrl { get; set; }
        private string RequestedFilePath { get; set; }
        private string RequestedFileExtension { get; set; }
        private string ContentType { get; set; }

        public Client(TcpClient client)
        {
            _currentTcpClient = client;
            Console.WriteLine($"Connection accepted");

            ReadRequest();
        }

        private void ReadRequest()
        {
            while ((_count = _currentTcpClient.GetStream().Read(_buffer, 0, _buffer.Length)) > 0)
            {
                _request += Encoding.ASCII.GetString(_buffer, 0, _count);
                if (_request.Contains("\r\n\r\n") || _request.Length > 4096) break;
            }

            ParseRequest();
        }

        private void ParseRequest()
        {
            var reqMatch = Regex.Match(_request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|"); // GET / HTTP/1.1

            if (reqMatch == Match.Empty)
            {
                SendError(HttpStatusCode.BadRequest);
                return;
            }

            RequestUrl = reqMatch.Groups[1].Value;

            Console.WriteLine("{0} requested: {1}", _currentTcpClient.Client.RemoteEndPoint, RequestUrl);

            RequestUrl = Uri.UnescapeDataString(RequestUrl); // %20 => " "

            if (_request.Contains("..")) // pc.odstudio.site/../../../
            {
                SendError(HttpStatusCode.BadRequest);
                return;
            }

            if (RequestUrl.EndsWith('/')) RequestUrl += Program.DefaultFileName;

            RequestedFilePath = $"www/{RequestUrl}";
            if (true) Console.WriteLine($"Requested File: {RequestedFilePath}");
            FileWork();
        }
        private void FileWork()
        {
            if (!File.Exists(RequestedFilePath))
            {
                SendError(HttpStatusCode.NotFound);
                return;
            }


            RequestedFileExtension = RequestUrl[RequestUrl.LastIndexOf('.')..];
            ContentType = GetContentType(RequestedFileExtension);

            try
            {
                var fs = new FileStream(RequestedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var headers = $"HTTP/1.1 200 OK\nContent-Type: {ContentType}\nContent-Lenght: {fs.Length}\n\n";
                var headersBuffer = Encoding.ASCII.GetBytes(headers);
                _currentTcpClient.GetStream().Write(headersBuffer, 0, headersBuffer.Length);

                while (fs.Position < fs.Length)
                {
                    _count = fs.Read(_buffer, 0, _buffer.Length);
                    _currentTcpClient.GetStream().Write(_buffer, 0, _count);
                }

                fs.Close();
                _currentTcpClient.Close();

            }
            catch (Exception )
            {
                SendError(HttpStatusCode.Forbidden);
            }
        }

        private static string GetContentType(string fileExtension)
        {
            switch (fileExtension)
            {
                case ".htm":
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/stylesheet";
                case ".js":
                    return "text/javascript";
                case ".jpg":
                    return "image/jpeg";
                case ".jpeg":
                case ".png":
                case ".gif":
                    return $"image/{fileExtension[1..]}";
                default:
                    return fileExtension.Length > 1 ? $"application/{fileExtension[1..]}" : "application/unknown";
            }
        }

        private void SendError(HttpStatusCode code)
        {
            var codeStr = $"{(int)code} {code.ToString()}"; // 404 NotFound
            var htmlResponse = $"<html><body><h1>{codeStr}</h1></body></html>";
            var headers = $"HTTP/1.1 {codeStr}\nContent-Type: text/html\nContent-Lenght: {htmlResponse.Length}\n\n{htmlResponse}";

            var responseBuffer = Encoding.ASCII.GetBytes(headers);
            _currentTcpClient.GetStream().Write(responseBuffer, 0, responseBuffer.Length);
            _currentTcpClient.Close();
        }
    }
}


