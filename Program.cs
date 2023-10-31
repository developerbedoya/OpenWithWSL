using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OpenWithWSL;

class Program
{
    private static string ContextMenuName = "DBAOpenWithWSL";
    private static string ContextMenuDescription = "Open with WSL";
    
    static void Main(string[] args)
    {
        string command = args.Length > 0 ? args[0] : "--help";
        switch (command)
        {
            case "--register":
                Register();
                break;
            case "--unregister":
                Unregister();
                break;
            case "--open":
                if (args.Length == 2)
                {
                    Open(args[1]);
                }
                else
                {
                    Usage();
                }
                
                break;
            case "--help":
                Usage();
                break;
            default:
                PrintUnknownCommand(command);
                Usage();
                break;
        }
    }

    static void Register()
    {
        try
        {
            string appPath = Assembly.GetExecutingAssembly().Location;
            string command = $"\"{appPath}\" --open \"%1\"";

            using var key = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\*\\shell\\{ContextMenuName}");
            key.SetValue(null, ContextMenuDescription);
            key.SetValue("Icon", appPath);

            using var commandKey = key.CreateSubKey("command");
            commandKey.SetValue(null, command.Replace(".dll", ".exe"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKey($"Software\\Classes\\*\\shell\\{ContextMenuName}");
    }

    static void Open(string filePath)
    {
        if (filePath?.Length == 0)
        {
            Usage();
            return;
        }
        
        filePath = filePath.Substring(0, 1).ToLower() + filePath.Substring(1);
        string linuxFilePath = "/mnt/" + filePath.Replace(":", "").Replace("\\", "/");
        string wslCommand = $"wsl xdg-open \"{linuxFilePath}\"";
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            }
        };

        process.Start();

        // Send the WSL command to the CMD process.
        process.StandardInput.WriteLine(wslCommand);
        
        // Close the CMD process.
        process.Close();
    }
    
    static void Usage()
    {
        Console.WriteLine("Usage: OpenWithWSL [--register|--unregister|--open <path>]");
    }
    
    static void PrintUnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
    }
}