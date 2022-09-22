﻿#if (UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WakaTime
{
    /// <summary>
    /// Catches the events and creates heartbeats
    /// </summary>
    public class HeartbeatCollector : IDisposable
    {
        private string ProjectName { get; }
        public event EventHandler<Heartbeat> OnHeartbeat;
        private Logger Logger { get; }

        Dictionary<string, DateTime> heartbeatHistory = new Dictionary<string, DateTime>();
        

        public HeartbeatCollector(Logger logger, string projectName)
        {
            Logger = logger;
            ProjectName = projectName;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
            EditorApplication.contextualPropertyMenu += EditorApplication_contextualPropertyMenu;
            EditorApplication.hierarchyChanged += EditorApplication_hierarchyChanged;
            EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosing += EditorSceneManager_sceneClosing;
            EditorSceneManager.newSceneCreated += EditorSceneManager_newSceneCreated;
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.contextualPropertyMenu -= EditorApplication_contextualPropertyMenu;
            EditorApplication.hierarchyChanged -= EditorApplication_hierarchyChanged;
            EditorSceneManager.sceneSaved -= EditorSceneManager_sceneSaved;
            EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosing -= EditorSceneManager_sceneClosing;
            EditorSceneManager.newSceneCreated -= EditorSceneManager_newSceneCreated;
        }

        private void ThrowHeartbeat(Heartbeat heartbeat)
        {
            if(heartbeatHistory.TryGetValue(heartbeat.entity, out DateTime value))
            {
                if ((DateTime.Now - value) < Settings.SameFileTimeout)
                    return;//Don't spam endpoint with same file 
            }
            heartbeatHistory[heartbeat.entity] = DateTime.Now;
            OnHeartbeat?.Invoke(this, heartbeat);
        }


        private void EditorApplication_contextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            ThrowHeartbeat(heartbeat);
        }

        private void EditorSceneManager_newSceneCreated(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            ThrowHeartbeat(heartbeat);
        }

        private void EditorSceneManager_sceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            ThrowHeartbeat(heartbeat);
        }

        private void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            ThrowHeartbeat(heartbeat);
        }

        private void EditorSceneManager_sceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            heartbeat.is_write = true;
            ThrowHeartbeat(heartbeat);
        }

        private void EditorApplication_hierarchyChanged()
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            ThrowHeartbeat(heartbeat);
        }

        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            Logger.Log(Logger.Levels.Debug, "Created heartbeat");
            var heartbeat = CreateHeartbeat();
            ThrowHeartbeat(heartbeat);
        }

        private Heartbeat CreateHeartbeat()
        {
            var currentScene = EditorSceneManager.GetActiveScene().path;
            string entity = "Unsaved Scene";
            if (!string.IsNullOrEmpty(currentScene))
                entity = Application.dataPath + "/" + currentScene.Substring("Assets/".Length);
            string type = "file";

            Heartbeat heartbeat = new Heartbeat(entity, type);
            heartbeat.project = ProjectName;
            heartbeat.language = "Unity";
            heartbeat.branch = GetBranchName(Application.dataPath);
            return heartbeat;
        }

        private string GetBranchName(string workingDir)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git"); //No .exe, I assume this work on linux and macos.

                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = workingDir;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.Arguments = "rev-parse --abbrev-ref HEAD";

                using Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                string branchname = process.StandardOutput.ReadLine();
                return branchname;
            }
            catch(Exception ex)
            {
                //Todo, figure out if git exists on this machine.
                //Also, figure out if this is even a git repo.
                Logger.Log(Logger.Levels.Warning, "Couln't determine branchname, is git installed?");
            }
            return null;
        }
        
    }
}


#endif
