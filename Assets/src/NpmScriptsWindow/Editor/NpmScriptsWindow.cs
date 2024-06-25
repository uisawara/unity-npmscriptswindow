using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class NpmScriptsWindow : EditorWindow
{
    private Dictionary<string, string> _scripts = new();

    private void OnGUI()
    {
        if (GUILayout.Button("Load Scripts"))
        {
            LoadScripts();
        }

        if (GUILayout.Button("Run npm install"))
        {
            RunNpmInstall();
        }

        foreach (var script in _scripts)
        {
            if (GUILayout.Button(script.Key))
            {
                RunScript(script.Value);
            }
        }
    }

    [MenuItem("Window/npm scripts window")]
    public static void ShowWindow()
    {
        GetWindow<NpmScriptsWindow>("Package Scripts");
    }

    private void LoadScripts()
    {
        var path = Path.Combine(Application.dataPath, "../package.json");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var jObject = JObject.Parse(json);
            var scriptsSection = jObject["scripts"]?.ToObject<Dictionary<string, string>>();

            if (scriptsSection != null)
            {
                _scripts = scriptsSection;
            }
            else
            {
                Debug.LogWarning("No scripts section found in package.json");
            }
        }
        else
        {
            Debug.LogWarning("package.json not found at project root");
        }
    }

    private void RunScript(string script)
    {
        RunCommandAsync(script);
    }

    private void RunNpmInstall()
    {
        RunCommandAsync("npm install");
    }

    private async void RunCommandAsync(string command)
    {
        var startInfo = CreateProcessStartInfo();
        if (startInfo == null)
        {
            return;
        }

        startInfo.Arguments = GetShellArguments(command);

        try
        {
            await Task.Run(() => RunProcess(startInfo));
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while running the script: {ex.Message}");
        }
    }

    private ProcessStartInfo CreateProcessStartInfo()
    {
        var startInfo = new ProcessStartInfo();
        var os = SystemInfo.operatingSystemFamily.ToString();

        if (os == "Windows")
        {
            startInfo.FileName = "cmd.exe";
            startInfo.UseShellExecute = true;
        }
        else if (os == "MacOSX")
        {
            startInfo.FileName = "/bin/bash";
            startInfo.UseShellExecute = true;
        }
        else
        {
            Debug.LogError("Unsupported OS");
            return null;
        }

        startInfo.WorkingDirectory = Path.Combine(Application.dataPath, "..");
        startInfo.CreateNoWindow = false;
        startInfo.RedirectStandardOutput = false;
        startInfo.RedirectStandardError = false;

        return startInfo;
    }

    private string GetShellArguments(string command)
    {
        var os = SystemInfo.operatingSystemFamily.ToString();
        if (os == "Windows")
        {
            return $"/C {command}";
        }

        if (os == "MacOSX")
        {
            return $"-c \"{command}\"";
        }

        return string.Empty;
    }

    private void RunProcess(ProcessStartInfo startInfo)
    {
        using (var process = Process.Start(startInfo))
        {
            process.WaitForExit();
        }
    }
}
