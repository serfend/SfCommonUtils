using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Management;
using System.Collections;
using System.Net.NetworkInformation;
using System.Linq;
using System.Diagnostics;
using Common.CmdShellHelper;

namespace IpSwitch.Helper
{
	public static class NetworkAdapter
	{
		/// <summary>
		/// 网卡列表
		/// </summary>
		public static List<NetworkInterface> NetWorkList()
		{
			var ls = NetworkInterface.GetAllNetworkInterfaces().ToList();
			// .Where(a => a.NetworkInterfaceType == NetworkInterfaceType.Ethernet).Select(a => a)
			return ls;

			//string manage = "SELECT * From Win32_NetworkAdapter WHERE MACAddress IS NOT NULL";
			//ManagementObjectSearcher searcher = new ManagementObjectSearcher(manage);
			//ManagementObjectCollection collection = searcher.Get();
			//List<string> netWorkList = new List<string>();
			//foreach (ManagementObject obj in collection)
			//{
			//    netWorkList.Add(obj["Name"].ToString());

			//}
			//return netWorkList;
		}

		public static ManagementObject? GetNetwork(string name)
		{
			// 此方法获取到的网卡无法被正常操作
			//string manage = "SELECT * From Win32_NetworkAdapter WHERE MACAddress IS NOT NULL";
			//ManagementObjectSearcher searcher = new ManagementObjectSearcher(manage);
			//ManagementObjectCollection collection = searcher.Get();
			//foreach (ManagementObject obj in collection)
			//{
			//    Debug.Print(obj["Name"].ToString());
			//    if (obj["Name"].ToString() == name) return obj;
			//}
			//return null;

			ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
			ManagementObjectCollection moc = mc.GetInstances();
			foreach (ManagementObject item in moc)
			{
				if (!(bool)item["IPEnabled"])
					continue;
				var itemName = item["Description"].ToString();
				if (itemName == name) return item;
			}
			return null;
		}

		/// <summary>
		/// 禁用网卡
		/// </summary>
		/// <param name="netWorkName">网卡名</param>
		/// <returns></returns>
		public static NetworkHandleState DisableNetWork(ManagementObject? network)
		{
			if (network == null) return NetworkHandleState.InvalidOperation;
			try
			{
				var result = network.InvokeMethod("Disable", null);
				return Int2State((int)result);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return NetworkHandleState.Fail;
		}

		/// <summary>
		/// 启用网卡
		/// </summary>
		/// <param name="netWorkName">网卡名</param>
		/// <returns></returns>
		public static NetworkHandleState EnableNetWork(ManagementObject network)
		{
			try
			{
				var result = network.InvokeMethod("Enable", null);
				return Int2State((int)result);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return NetworkHandleState.Fail;
		}

		public static int DisableDhcp(this ManagementObject network) => DoAction(network, "DisableDHCP");

		public static int DoAction(this ManagementObject network, string method)
		{
			try
			{
				var result = network.InvokeMethod(method, null);
				return (int)result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return -1;
		}

		public static NetworkHandleState Int2State(int res)
		{
			switch (res)
			{
				case 0: return NetworkHandleState.Success;
				case 5: return NetworkHandleState.PermissionDenied;
				default: return NetworkHandleState.Fail;
			}
		}

		/// <summary>
		/// 网卡状态
		/// </summary>
		/// <param name="netWorkName">网卡名</param>
		/// <returns></returns>
		public static bool GetNetworkState(string netWorkName)
		{
			string netState = "SELECT * From Win32_NetworkAdapter";
			ManagementObjectSearcher searcher = new ManagementObjectSearcher(netState);
			ManagementObjectCollection collection = searcher.Get();
			foreach (ManagementObject manage in collection)
			{
				if (manage["Name"].ToString() == netWorkName)
				{
					return (bool)manage["IPEnabled"];
				}
			}
			return false;
		}

		/// <summary>
		/// 得到指定网卡
		/// </summary>
		/// <param name="networkname">网卡名字</param>
		/// <returns></returns>
		public static ManagementObject? GetNetworkByName(string networkname)
		{
			string netState = "SELECT * From Win32_NetworkAdapter ";

			ManagementObjectSearcher searcher = new ManagementObjectSearcher(netState);
			ManagementObjectCollection collection = searcher.Get();

			foreach (ManagementObject manage in collection)
			{
				var mname = manage["Name"].ToString();
				//System.Diagnostics.Debug.WriteLine(mname);
				if (mname == networkname)
				{
					return manage;
				}
			}
			return null;
		}

		public enum NetworkHandleState
		{
			Success = 0,
			PermissionDenied = 5,
			Fail = -1,
			InvalidOperation = -2
		}
	}

	public static class NetworkAdapterByShell
	{
		public static void Enable(string name) => RunNetShellSetInterface(new List<(string, string)>
			{
				{ ("Name", name)},
				{ ("admin", "ENABLE")},
			});

		public static void Disable(string name) => RunNetShellSetInterface(new List<(string, string)>
			{
				{ ("Name", name)},
				{ ("admin", "DISABLE")},
			});

		public static void RunNetShellSetInterface(IEnumerable<(string, string)> args) => RunNetShell("netsh interface set interface", args);

		public static void RunNetShell(string cmd, IEnumerable<(string, string)> args) => new CmdExecutor().CmdRun($"{nameof(NetworkAdapterByShell)}{nameof(RunNetShell)}", $"{cmd} {string.Join(' ', args.Select(i => i.ToStatement()))}");
	}

	public static class ArgumentExtensions
	{
		public static string ToStatement(this (string, string) arg) => $"{arg.Item1}=\"{arg.Item2}\"";
	}
}