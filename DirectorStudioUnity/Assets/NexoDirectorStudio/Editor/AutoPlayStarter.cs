using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NexoDirectorStudio.Editor
{
	public static class AutoPlayStarter
	{
		// Usage: -executeMethod NexoDirectorStudio.Editor.AutoPlayStarter.Run --scene "Assets/GeneratedScenes/Scene.unity"
		public static void Run()
		{
			try
			{
				var args = Environment.GetCommandLineArgs();
				var scenePath = ReadArg(args, "--scene");
				if (string.IsNullOrEmpty(scenePath))
				{
					System.Console.Error.WriteLine("AutoPlayStarter: --scene <path> is required");
					EditorApplication.Exit(2);
					return;
				}

				EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
				EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
			}
			catch (Exception ex)
			{
				System.Console.Error.WriteLine($"AutoPlayStarter failed: {ex.Message}\n{ex.StackTrace}");
				EditorApplication.Exit(1);
			}
		}

		private static string ReadArg(string[] args, string key)
		{
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
				{
					return args[i + 1];
				}
			}
			return null;
		}
	}
}
