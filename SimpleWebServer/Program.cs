using SimpleWebServer.Core;
using System;
using System.Threading;

namespace SimpleWebServer
{
    internal static class Program
    {
        public const bool IsDebugEnabled = true;
        public const string DefaultFileName = "index.html";
        public const string FileFolder = "www";

        private static void Main(string[] args)
        {
            var maxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(maxThreadsCount, maxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);

            var server = new Server();

            Console.ReadLine();
        }
    }
}
