using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.CmdShellHelper
{
	/// <summary>
	/// 命令行的返回值
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		public MessageEventArgs(MessageType type, string? message)
		{
			Type = type;
			Message = message ?? "[无信息]";
		}

		public MessageType Type { get; set; }
		public string Message { get; set; }

		public enum MessageType
		{
			None = 0,
			Info = 1,
			Error = 2
		}
	}

	public class CmdExecutor
	{
		public const string Process_Cmd = "cmd.exe";
		public const string Process_Powershell = "powershell.exe";
		public string CurrentProcess { get; set; }

		public CmdExecutor() : this(Process_Cmd)
		{
		}

		public CmdExecutor(string currentProcess)
		{
			CurrentProcess = currentProcess;
		}

		public Task<Tuple<string, string>> CmdRunAsync(string title, string str, string process = Process_Cmd, bool requireAdmin = false) => Task.FromResult(CmdRun(title, str, process, requireAdmin));

		public event EventHandler<MessageEventArgs>? OnMessage;

		public bool EnableRedirectStandardInput { get; set; } = true;
		public bool EnableRedirectStandardOutput { get; set; } = true;
		public bool EnableRedirectStandardError { get; set; } = true;
		public bool UseShellExecute { get; set; } = true;

		public Tuple<string, string> CmdRun(string title, string str, string? process = null, bool requireAdmin = false)
		{
			var p = new Process();
			p.StartInfo.FileName = process ?? CurrentProcess;
			var startInfo = p.StartInfo;
			if (requireAdmin) startInfo.Verb = "runas";
			if (UseShellExecute)
			{
				startInfo.UseShellExecute = false;    // 是否使用操作系统shell启动
				startInfo.CreateNoWindow = false; // 不显示程序窗口
			}
			if (EnableRedirectStandardInput) startInfo.RedirectStandardInput = true; // 接受来自调用程序的输入信息
			if (EnableRedirectStandardError)
			{
				startInfo.RedirectStandardError = true; // 重定向标准错误输出
				var errors = new StringBuilder();
				p.ErrorDataReceived += (sender, args) =>
				{
					Debug.WriteLine(args.Data);
					errors.AppendLine(args.Data);
					OnMessage?.Invoke(this, new MessageEventArgs(MessageEventArgs.MessageType.Error, args.Data));
				};
			}
			var outputs = new StringBuilder();
			if (EnableRedirectStandardOutput)
			{
				startInfo.RedirectStandardOutput = true; // 由调用程序获取输出信息
				p.OutputDataReceived += (sender, args) =>
				{
					Debug.WriteLine(args.Data);
					outputs.AppendLine(args.Data);
					OnMessage?.Invoke(this, new MessageEventArgs(MessageEventArgs.MessageType.Info, args.Data));
				};
			}
			p.Start();//启动程序
			if (EnableRedirectStandardError) p.BeginErrorReadLine();
			if (EnableRedirectStandardOutput) p.BeginOutputReadLine();
			if (EnableRedirectStandardInput)
			{
				p.StandardInput.AutoFlush = true;
				var lines = str.Split('\n').Select(l => l.Replace("\r", ""));
				//向cmd窗口发送输入信息
				foreach (var line in lines)
				{
					Debug.WriteLine($"{title}:{line}");
					p.StandardInput.WriteLine(line);
				}
				p.StandardInput.WriteLine("exit");
				//向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
				//同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令
			}
			p.WaitForExit();//等待程序执行完退出进程
			return new Tuple<string, string>(title, outputs.ToString());
		}
	}
}