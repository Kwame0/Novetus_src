﻿/*
 * Created by SharpDevelop.
 * User: Bitl
 * Date: 10/10/2019
 * Time: 6:59 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Linq;
using System.ComponentModel;
using System.Net;

/// <summary>
/// Description of SecurityFuncs.
/// </summary>
public class SecurityFuncs
{
	[DllImport("user32.dll")]
	static extern int SetWindowText(IntPtr hWnd, string text);
		
	public SecurityFuncs()
	{
	}
		
	public static string RandomString(int length)
	{
		CryptoRandom random = new CryptoRandom();
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
		return new string(Enumerable.Repeat(chars, length)
      			.Select(s => s[random.Next(s.Length)]).ToArray());
	}
		
	public static string RandomString()
	{
		CryptoRandom random = new CryptoRandom();
		return RandomString(random.Next(5, 20));
	}
		
	public static string Base64Decode(string base64EncodedData)
	{
		var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
		return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
	}
		
	public static string Base64Encode(string plainText)
	{
		var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
		return System.Convert.ToBase64String(plainTextBytes);
	}
		
	public static bool IsBase64String(string s)
	{
		s = s.Trim();
		return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
	}
		
	public static void RegisterURLProtocol(string protocolName, string applicationPath, string description)
	{
		RegistryKey subKey = Registry.ClassesRoot.CreateSubKey(protocolName);
		subKey.SetValue((string)null, (object)description);
		subKey.SetValue("URL Protocol", (object)string.Empty);
		Registry.ClassesRoot.CreateSubKey(protocolName + "\\Shell");
		Registry.ClassesRoot.CreateSubKey(protocolName + "\\Shell\\open");
		Registry.ClassesRoot.CreateSubKey(protocolName + "\\Shell\\open\\command").SetValue((string)null, (object)("\"" + applicationPath + "\" \"%1\""));
	}
		
	public static long UnixTimeNow()
	{
		var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
		return (long)timeSpan.TotalSeconds;
	}
		
	public static bool checkClientMD5(string client)
	{
		if (GlobalVars.AdminMode != true) {
			if (GlobalVars.AlreadyHasSecurity != true) {
				string rbxexe = "";
				if (GlobalVars.LegacyMode == true) {
					rbxexe = GlobalVars.BasePath + "\\clients\\" + client + "\\RobloxApp.exe";
				} else {
					rbxexe = GlobalVars.BasePath + "\\clients\\" + client + "\\RobloxApp_client.exe";
				}
				using (var md5 = MD5.Create()) {
					using (var stream = File.OpenRead(rbxexe)) {
						byte[] hash = md5.ComputeHash(stream);
						string clientMD5 = BitConverter.ToString(hash).Replace("-", "");
						if (clientMD5.Equals(GlobalVars.SelectedClientMD5)) {
							return true;
						} else {
							return false;
						}
					}
				}
			} else {
				return true;
			}
		} else {
			return true;
		}
	}
		
	public static bool checkScriptMD5(string client)
	{
		if (GlobalVars.AdminMode != true) {
			if (GlobalVars.AlreadyHasSecurity != true) {
				string rbxscript = GlobalVars.BasePath + "\\clients\\" + client + "\\content\\scripts\\" + GlobalVars.ScriptName + ".lua";
				using (var md5 = MD5.Create()) {
					using (var stream = File.OpenRead(rbxscript)) {
						byte[] hash = md5.ComputeHash(stream);
						string clientMD5 = BitConverter.ToString(hash).Replace("-", "");
						if (clientMD5.Equals(GlobalVars.SelectedClientScriptMD5)) {
							return true;
						} else {
							return false;
						}
					}
				}
			} else {
				return true;
			}
		} else {
			return true;
		}
	}
		
	public static string CalculateMD5(string filename)
	{
		using (var md5 = MD5.Create()) {
			using (var stream = File.OpenRead(filename)) {
				return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
			}
		}
	}
		
	public static bool IsElevated {
		get {
			return WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
		}
	}
		
	public static string RandomStringTitle()
	{
		CryptoRandom random = new CryptoRandom();
		return new String(' ', random.Next(20));
	}
		
	public static void RenameWindow(Process exe, ScriptGenerator.ScriptType type)
	{
		if (GlobalVars.AlreadyHasSecurity != true) {
			int time = 500;
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (obj, e) => WorkerDoWork(exe, type, time, worker, GlobalVars.SelectedClient);
			worker.RunWorkerAsync();
		}
	}
		
	private static void WorkerDoWork(Process exe, ScriptGenerator.ScriptType type, int time, BackgroundWorker worker, string clientname)
	{
		if (exe.IsRunning() == true) {
			while (exe.IsRunning() == true) {
				if (exe.IsRunning() != true) {
					worker.DoWork -= (obj, e) => WorkerDoWork(exe, type, time, worker, clientname);
					worker.CancelAsync();
					worker.Dispose();
					break;
				}
					
				if (type == ScriptGenerator.ScriptType.Client) {
					SetWindowText(exe.MainWindowHandle, "Novetus " + GlobalVars.Version + " - " + clientname + " " + ScriptGenerator.GetNameForType(type) + " [" + GlobalVars.IP + ":" + GlobalVars.RobloxPort + "]" + RandomStringTitle());
				} else if (type == ScriptGenerator.ScriptType.Server || type == ScriptGenerator.ScriptType.Solo || type == ScriptGenerator.ScriptType.Studio) {
					SetWindowText(exe.MainWindowHandle, "Novetus " + GlobalVars.Version + " - " + clientname + " " + ScriptGenerator.GetNameForType(type) + " [" + GlobalVars.Map + "]" + RandomStringTitle());
				}
				Thread.Sleep(time);
			}
		} else {
			Thread.Sleep(time);
			RenameWindow(exe, type);
		}
	}

    public static string GetExternalIPAddress()
    {
        string ipAddress;
        using (WebClient wc = new WebClient())
        {
            try
            {
                ipAddress = wc.DownloadString("http://ipv4.icanhazip.com/");
            }
            catch (Exception)
            {
                ipAddress = "localhost" + Environment.NewLine;
            }
        }

        return ipAddress;
    }
}