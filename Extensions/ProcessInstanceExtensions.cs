using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
	public class ProcessInstance
	{
		public ProcessInstance(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
	public class ProcessInstanceException : Exception
	{
		public ProcessInstanceException() : base("多开禁止") { }
	}
	public static class ProcessInstanceExtensions
	{
		private static readonly ConcurrentDictionary<string, Mutex> mutices = new();
		/// <summary>
		/// 判断信号是否已存在
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static bool CheckInstaceByMutex(this ProcessInstance instance)
		{
			if (mutices.ContainsKey(instance.Name)) return true;
			var mutex = new Mutex(false, instance.Name, out var createdNew);
			if (!createdNew) return true;
			mutices[instance.Name] = mutex;
			return false;
		}
		/// <summary>
		/// 通过创建命名管道检查是否存在进程重复开启的情况
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="clientCallback">客户端侧重复开启时回调</param>
		/// <param name="serverCallback">服务器侧重复开启时回调</param>
		public static void CheckInstaceByNamedPipe(this ProcessInstance instance, Action? clientCallback = null, Action? serverCallback = null)
		{
			//var buffer = new byte[1024];
			var clientPipe = new NamedPipeClientStream(".", instance.Name, PipeDirection.InOut, PipeOptions.Asynchronous);
			//异步链接服务端
			clientPipe
				.ConnectAsync(1000)
				.ContinueWith(x =>
			{
				if (x.Exception == null)
				{
					clientCallback?.Invoke();
				}
			}).Wait();
			NewConnection(instance.Name, serverCallback); // 建立监听
		}

		private static void NewConnection(string pipeName, Action? serverCallback = null)
		{
			var serverPipe =
				new NamedPipeServerStream(pipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			serverPipe
				.WaitForConnectionAsync()
				.ContinueWith(x =>
				{
					if (x.Exception == null)
					{
						serverCallback?.Invoke();
					}
					NewConnection(pipeName, serverCallback);
				}); // 连接成功后开始下一次的监听
		}
	}
}
