using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class PythonServerManager
{
    private Process pythonProcess;
    private string SERVER_URL = "http://localhost:5000";
    private bool isServerRunning = false;

    void Start()
    {
        StartPythonServer();
    }

    void OnApplicationQuit()
    {
        StopPythonServer();
    }

    private void StartPythonServer()
    {
        try
        {
            string executablePath;
            string processArguments;

            if (Application.isEditor)
            {
                executablePath = "python";
                processArguments = Path.Combine(Application.streamingAssetsPath, "PythonServer.py");
            }
            else
            {
                executablePath = Path.Combine(Application.streamingAssetsPath, "PythonServer.exe");
                processArguments = "";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{processArguments}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            pythonProcess = Process.Start(startInfo);

            UnityEngine.Debug.Log("Server started");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start server: {e.Message}");
        }
    }

    private void StopPythonServer()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
            UnityEngine.Debug.Log("Python server stopped");
        }
    }
}
