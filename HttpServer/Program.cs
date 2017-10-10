using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpServer
{
	class MainClass
	{
		static TcpListener serverSocket;
		static bool status;

		public static void Main(string[] args)
		{
			serverSocket = new TcpListener(IPAddress.Parse(args[0]), int.Parse(args[1]));
			serverSocket.Start();
			Console.WriteLine($"Server started on {args[0]}:{args[1]}.");
			status = true;
			Task.Run(() => HttpListener());
			Console.WriteLine("Server is listening for HTTP requests.\n");
			Task.Run(() => Stopper(args[0], 8081));
			Task.Run(() => Terminal());
			while (status) { };
		}

		static void HttpListener()
		{
			while (status)
			{
				if (serverSocket.Pending())
				{
					TcpClient connectionSocket = serverSocket.AcceptTcpClient();
					NetworkStream ns = connectionSocket.GetStream();
					StreamReader sr = new StreamReader(ns);
					StreamWriter sw = new StreamWriter(ns);
					sw.AutoFlush = true;

					try
					{
						bool firstLine = true;
						string[] requestLine;
						string requestedFile = "";
						Dictionary<string, string> requestFields = new Dictionary<string, string>();

						while (connectionSocket.Connected)
						{
							string request = sr.ReadLine();
							if (request == "") break;
							if (firstLine)
							{
								requestLine = request.Split(' ');
								requestedFile = requestLine[1];
								firstLine = false;
							}
							else
							{
								string[] requestField = Regex.Split(request, @": ");
								requestFields.Add(requestField[0], requestField[1]);
							}
						}
						if (requestedFile == "/") requestedFile = "/index.html";

						string responseLine = "HTTP/1.1 200 OK";

						string contentType;
						requestFields.TryGetValue("Accept", out contentType);
						if (contentType.Contains("text/html")) contentType = "text/html";
						else if (contentType.Contains("text/css")) contentType = "text/css";
						else if (contentType.Contains("text/javascript")) contentType = "text/javascript";

						if (!File.Exists($"../../htdocs{requestedFile}"))
						{
							responseLine = "HTTP/1.1 404 Not Found";
							contentType = "text/html";
							requestedFile = "/404.html";
						}

						sw.WriteLine($"{responseLine}\n" +
									 "Server: Marcin's HTTP Server\n" +
									 //"Content-Encoding: gzip\n" +
									 $"Content-Type: {contentType}\n" +
									 "Accept-Ranges: bytes\n" +
									 $"Date: {DateTime.Now}\n" +
									 "Connection: close\n");
						using (FileStream fs = File.Open($"../../htdocs{requestedFile}", FileMode.Open)) fs.CopyTo(ns);

					}
					catch
					{
						sw.WriteLine("HTTP/1.1 500 Internal Server Error\n" +
									 "Server: Marcin's HTTP Server\n" +
									 //"Content-Encoding: gzip\n" +
									 "Content-Type: text/html\n" +
									 "Accept-Ranges: bytes\n" +
									 "Date: {DateTime.Now}\n" +
									 "Connection: close\n");
						using (FileStream fs = File.Open($"../../htdocs/500.html", FileMode.Open)) fs.CopyTo(ns);
					}
					finally
					{
						connectionSocket.Close();
					}
				}
			}
		}

		static void Stopper(string ipAddress, int port)
		{
			TcpListener stopperSocket = new TcpListener(IPAddress.Parse(ipAddress), port);
			stopperSocket.Start();
			TcpClient connectionSocket = stopperSocket.AcceptTcpClient();
			Console.WriteLine("\nServer was shut down remotely.");
			status = false;
		}

		static void Terminal()
		{
			string command = "";
			while (status && command != "quit")
			{
				Console.Write("$ ");
				command = Console.ReadLine();
			}
			Console.WriteLine("Server is going to close now...");
			status = false;
		}
	}
}
