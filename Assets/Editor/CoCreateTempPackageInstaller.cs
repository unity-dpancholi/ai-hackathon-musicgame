
      using System;
      using System.IO;
      using UnityEditor;
      using UnityEditor.PackageManager;
      using UnityEditor.PackageManager.Requests;
      using UnityEngine;

      [InitializeOnLoad]
      public static class CoCreateTempPackageInstaller
      {
          private static AddAndRemoveRequest s_Request;

          // Paths to clean up
          private const string k_ScriptPath = "Assets/Editor/CoCreateTempPackageInstaller.cs";
          private const string k_EditorFolderPath = "Assets/Editor";

          static CoCreateTempPackageInstaller()
          {
              Debug.Log("CoCreateTempPackageInstaller executed");
              // 1. Define the packages you want to install
              string[] packagesToAdd = new[]
              {
                  "file:../../../Users/deeppancholi/AppData/Roaming/UnityHub/cocreate-bundled-packages/com.unity.assistant-desktop"
              };

              string[] packagesToRemove = Array.Empty<string>();

              // 2. Start the batched package installation
              s_Request = Client.AddAndRemove(packagesToAdd, packagesToRemove);
              EditorApplication.update += Progress;
          }

          static void Progress()
          {
              // Wait until the batched UPM request is completed
              if (s_Request == null || !s_Request.IsCompleted) return;

              // Unsubscribe from the update loop
              EditorApplication.update -= Progress;

              // Log results
              if (s_Request.Status == StatusCode.Success)
                  Debug.Log("[Hub Injector] Successfully installed all packages.");
              else if (s_Request.Status >= StatusCode.Failure)
                  Debug.LogError($"[Hub Injector] Installation failed: {s_Request.Error.message}");

              // 3. Self-Delete the script first
              AssetDatabase.DeleteAsset(k_ScriptPath);

              // 4. Safely check and delete the Editor folder if it is now empty
              CleanupEmptyEditorFolder();
          }

          static void CleanupEmptyEditorFolder()
          {
              string absolutePath = Path.GetFullPath(k_EditorFolderPath);

              if (!Directory.Exists(absolutePath)) return;

              // Get all files and subfolders
              string[] files = Directory.GetFiles(absolutePath);
              string[] dirs = Directory.GetDirectories(absolutePath);

              bool isEmpty = true;

              // If there are subdirectories, the folder is not empty
              if (dirs.Length > 0)
              {
                  isEmpty = false;
              }

              // Check for remaining files, intentionally ignoring macOS hidden files
              foreach (string file in files)
              {
                  if (!file.EndsWith(".DS_Store"))
                  {
                      isEmpty = false;
                      break;
                  }
              }

              // If the folder is truly empty (or only contains a .DS_Store), delete it
              if (isEmpty)
              {
                  // Using AssetDatabase.DeleteAsset ensures Unity also deletes the Assets/Editor.meta file
                  AssetDatabase.DeleteAsset(k_EditorFolderPath);
              }
          }
      }