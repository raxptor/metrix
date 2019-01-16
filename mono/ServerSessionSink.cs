using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;

namespace Metrix
{
	public static class ServerSessionSink
	{
		static string _apiURL;
		static Thread _thread;
		static Queue.Sink _sink = new Queue.Sink();

		static string MakeUrl(string endPoint)
		{
			return _apiURL + endPoint;
		}

		static void PostThread(string insertUrl)
		{
			Queue.Entry[] tmp = new Queue.Entry[128];
			byte[] buf = new byte[64];
			while (true)
			{
				int count;
				Globals.Queue.Extract(ref _sink, tmp, out count);
				if (count < 0)
				{
					break;
				}
				if (count == 0)
				{
					Thread.Sleep(100);
					continue;
				}

				while (true)
				{
					try
					{
						var wr = WebRequest.Create(insertUrl);
						wr.Method = "POST";
						wr.ContentType = "text/plain";
						StringBuilder sb = new StringBuilder();
						for (int i = 0; i < count; i++)
						{
							if (tmp[i].Type == EventType.LOG)
							{
								sb.Append("log$" + tmp[i].Sequence + "$" + tmp[i].Type + "$" + tmp[i].Time.ToString("s") + "$" + tmp[i].Data + "\n");
							}
						}
						if (sb.Length == 0)
							break;

						byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
						wr.ContentLength = b.Length;
						using (var stream = wr.GetRequestStream())
						{
							stream.Write(b, 0, b.Length);
						}
						var response = wr.GetResponse();
						using (var rstream = response.GetResponseStream())
						{
							rstream.Read(buf, 0, buf.Length);
						}
						Console.WriteLine("Posted");
						break;
					}
					catch (Exception exc)
					{
						Thread.Sleep(1000);
						Console.WriteLine(exc.ToString());
					}
				}

				Console.WriteLine("Extracted [" + count + "] entries");
			};
			Console.WriteLine("ServerSessionSink reached end.");
		}

		public static void Start(string apiURL, Dictionary<string, string> configs)
		{
			Globals.Queue.AddSink(ref _sink);
			_apiURL = apiURL;
			_thread = new Thread(() =>
			{
				while (true)
				{
					try
					{
						var wr = WebRequest.Create(MakeUrl("session/create"));
						wr.Method = "POST";
						wr.ContentType = "text/plain";
						StringBuilder sb = new StringBuilder();
						foreach (var k in configs)
						{
							sb.Append(k.Key + ":" + k.Value + "\n");
						}

						byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
						wr.ContentLength = b.Length;
						using (var stream = wr.GetRequestStream())
						{
							stream.Write(b, 0, b.Length);
						}
						byte[] buf = new byte[65536];
						int read = 0;
						var response = wr.GetResponse();
						using (var rstream = response.GetResponseStream())
						{
							read = rstream.Read(buf, 0, buf.Length);
						}
	
						string insertUrl = MakeUrl(Encoding.UTF8.GetString(buf, 0, read));
						Thread _th2 = new Thread(() =>
						{
							PostThread(insertUrl);
						});

						Console.WriteLine("Initialized session [" + insertUrl + "]");
						_th2.Start();
						return;
					}
					catch (Exception e)
					{
						Console.WriteLine("Server session init failed");
						Console.WriteLine(e.ToString());
						Thread.Sleep(1000);
					}
				}
			});
			_thread.Start();
		}
	}
}
