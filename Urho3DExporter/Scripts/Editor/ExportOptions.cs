﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Urho3DExporter
{
    public class ExportOptions : EditorWindow
    {
        private static readonly string _dataPathKey = "Urho3DExporter.DataPath";
        private static readonly string _exportUpdatedOnlyKey = "Urho3DExporter.UpdatedOnly";
        private static readonly string _selectedKey = "Urho3DExporter.Selected";
        private static readonly string _exportSceneAsPrefabKey = "Urho3DExporter.SceneAsPrefab";
        private static readonly string _skipDisabledKey = "Urho3DExporter.SkipDisabled";
        private static readonly string _exportReflectionProbesKey = "Urho3DExporter.ReflectionProbes";
        private string _exportFolder = "";
        private bool _exportUpdatedOnly = false;
        private bool _selected = true;
        private bool _exportSceneAsPrefab = false;
        private bool _skipDisabled = false;
        private bool _exportReflectionProbes = false;
        private Stopwatch _stopwatch = new Stopwatch();
        private IEnumerator<ProgressBarReport> _progress;
        private string _progressLabel;


        [MenuItem("Assets/Export Assets and Scene To Urho3D")]
        private static void Init()
        {
            var window = (ExportOptions) GetWindow(typeof(ExportOptions));
            window.Show();
        }



        private void OnGUI()
        {
            // if (string.IsNullOrWhiteSpace(_exportFolder)) {
            //    PickFolder();
            // }

            _exportFolder = EditorGUILayout.TextField("Export Folder", _exportFolder);
            if (GUILayout.Button("Pick")) PickFolder();
            _exportUpdatedOnly = EditorGUILayout.Toggle("Export only updated resources", _exportUpdatedOnly);

            var selected = false;
            if (Selection.assetGUIDs.Length != 0)
            {
                _selected = EditorGUILayout.Toggle("Export selected assets", _selected);
                selected = _selected;
            }

            _exportSceneAsPrefab = EditorGUILayout.Toggle("Export scene as prefab", _exportSceneAsPrefab);

            _skipDisabled = EditorGUILayout.Toggle("Skip disabled entities", _skipDisabled);

            _exportReflectionProbes = EditorGUILayout.Toggle("Export reflection probes", _exportReflectionProbes);

            if (_progressLabel != null)
            {
                GUILayout.Label(_progressLabel);
            }
            if (_progress == null)
            {
                if (!string.IsNullOrWhiteSpace(_exportFolder))
                {
                    if (GUILayout.Button("Export"))
                    {
                        var urhoDataPath = new DestinationFolder(_exportFolder, _exportUpdatedOnly);
                        _progress = ExportAssets.ExportToUrho(_exportFolder, urhoDataPath, selected, _exportSceneAsPrefab, _skipDisabled).GetEnumerator();
                        EditorApplication.update += Advance;
                    }
                }
            }

            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.EndHorizontal();
        }

        private void Advance()
        {
            try
            {
                _stopwatch.Restart();
                do
                {
                    if (!_progress.MoveNext())
                    {
                        Stop();
                        break;
                    }
                } while (_stopwatch.Elapsed.TotalMilliseconds < 16);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                Stop();
                _progressLabel = "Error: " + ex.Message;
            }
            if (_progress != null)
            {
                _progressLabel = _progress.Current.Message;
            }
            else
            {
                //Close();
            }
        }

        private void Stop()
        {
            if (_progress != null)
            {
                _progress.Dispose();
                _progress = null;
            }

            _progressLabel = "Done.";
            EditorApplication.update -= Advance;
        }


        private void PickFolder()
        {
            retry:
            var exportFolder = EditorUtility.SaveFolderPanel("Save assets to Data folder", _exportFolder, "");
            if (string.IsNullOrWhiteSpace(exportFolder))
            {
                if (string.IsNullOrWhiteSpace(_exportFolder))
                    Close();
            }
            else
            {
                if (exportFolder.StartsWith(Path.GetDirectoryName(Application.dataPath).FixDirectorySeparator(),
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    EditorUtility.DisplayDialog("Error",
                        "Selected path is inside Unity folder. Please select a different folder.", "Ok");
                    goto retry;
                }

                _exportFolder = exportFolder;
                SaveConfig();
            }
        }

        private void OnFocus()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (EditorPrefs.HasKey(_dataPathKey))
            {
                _exportFolder = EditorPrefs.GetString(_dataPathKey);
            }
            else
            {
                _exportFolder = Application.dataPath;
            }
            if (EditorPrefs.HasKey(_exportUpdatedOnlyKey))
                _exportUpdatedOnly = EditorPrefs.GetBool(_exportUpdatedOnlyKey);
            if (EditorPrefs.HasKey(_selectedKey))
                _selected = EditorPrefs.GetBool(_selectedKey);
            if (EditorPrefs.HasKey(_exportSceneAsPrefabKey))
                _exportSceneAsPrefab = EditorPrefs.GetBool(_exportSceneAsPrefabKey);
            if (EditorPrefs.HasKey(_skipDisabledKey))
                _skipDisabled = EditorPrefs.GetBool(_skipDisabledKey);
            if (EditorPrefs.HasKey(_exportReflectionProbesKey))
                _exportReflectionProbes = EditorPrefs.GetBool(_exportReflectionProbesKey);
        }

        private void OnLostFocus()
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            EditorPrefs.SetString(_dataPathKey, _exportFolder);
            EditorPrefs.SetBool(_exportUpdatedOnlyKey, _exportUpdatedOnly);
            EditorPrefs.SetBool(_selectedKey, _selected);
            EditorPrefs.SetBool(_exportSceneAsPrefabKey, _exportSceneAsPrefab);
            EditorPrefs.SetBool(_skipDisabledKey, _skipDisabled);
            EditorPrefs.SetBool(_exportReflectionProbesKey, _exportReflectionProbes);
            
        }

        private void OnDestroy()
        {
            SaveConfig();
        }
    }
}