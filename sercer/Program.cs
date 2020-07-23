using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.RepresentationModel;
using System.Linq;
using System.Threading.Tasks;

namespace sercer
{
    public class HttpServer
    {
        public static int port;
        public static string prefix;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Spider v0.1");
            Console.WriteLine("Written by Mason Meirs on a shitty 2020 summer evening whilst on the phone with Kallie");
            if (!File.Exists("sconfig.yml")){
                var serializer = new YamlDotNet.Serialization.Serializer();
                using(TextWriter wrt = File.CreateText("sconfig.yml"))
                {
                    serializer.Serialize(wrt, new
                    {
                        port = "80",
                        startPage = "index.html",
                        prefix = "http://localhost/"
                    });
                }
                
            } else
            {
                string config = File.ReadAllText(Directory.GetCurrentDirectory() + @"\sconfig.yml");
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                var dict = deserializer.Deserialize<Dictionary<string, string>>(config);
                port = int.Parse(dict["port"]);
                prefix = dict["prefix"];
            }
            Console.WriteLine("Init sequence complete...");
            Spider spd = new Spider();
            spd.StartServer(port, prefix);

        }
    }

    public class Spider : HttpServer
    {
        public HttpListener htplst = new HttpListener();
        public bool serverRunning;
        public string pageContents;
        public void StartServer(int port, string prefix)
        {
            Console.WriteLine("Starting the Spider Http Server");
            string config = File.ReadAllText(Directory.GetCurrentDirectory() + @"\sconfig.yml");
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var dict = deserializer.Deserialize<Dictionary<string, string>>(config);
            if (!File.Exists(dict["startPage"])){
                Console.WriteLine("startPage does not exist! Please look at your sconfig.");
                StopServer("Invalid startPage in sconfig");
            } else
            {
                pageContents = File.ReadAllText(dict["startPage"]);
            }
            htplst.Prefixes.Add(prefix);
            htplst.Start();
            Console.WriteLine("Server started. Listening...");
            serverRunning = true;
            while (serverRunning)
            {
                var t = Task.Factory.StartNew(() => Listen());
                if (t.IsCompleted)
                {
                    t.Start();
                }
                System.AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    StopServer("User Exited");
                };
            }
        }
        public void Listen()
        {
            HttpListenerContext context = htplst.GetContext();
            Console.WriteLine("Incoming Request");
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            string responseString = pageContents;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }
        public void StopServer(string error)
        {
            Console.WriteLine("Stopping server...");
            Console.WriteLine(error);
            htplst.Stop();
            serverRunning = false;
            Environment.Exit(0);
        }
    }
}
