using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NexoDirectorStudio.Agents;

namespace NexoDirectorStudio.Editor
{
	public static class HeadlessPlayRunner
	{
		// Usage example:
		// -executeMethod NexoDirectorStudio.Editor.HeadlessPlayRunner.Run --scene "Assets/GeneratedScenes/MyScene.unity" --seconds 10
		public static void Run()
		{
			try
			{
				var args = Environment.GetCommandLineArgs();
				var scenePath = ReadArg(args, "--scene");
				var secondsStr = ReadArg(args, "--seconds") ?? "10";
				if (string.IsNullOrEmpty(scenePath))
				{
					System.Console.Error.WriteLine("HeadlessPlayRunner: --scene <path> is required");
					EditorApplication.Exit(2);
					return;
				}

				if (!System.IO.File.Exists(scenePath))
				{
					System.Console.Error.WriteLine($"HeadlessPlayRunner: Scene not found: {scenePath}");
					EditorApplication.Exit(3);
					return;
				}

				if (!int.TryParse(secondsStr, out var seconds)) seconds = 10;

				System.Console.WriteLine($"Headless validation: opening {scenePath} and running for {seconds}s...");
				EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

				EnsureAgentSetup();

				var startTime = EditorApplication.timeSinceStartup;
				bool inPlay = false;
				EditorApplication.update += Tick;

				void Tick()
				{
					if (!inPlay)
					{
						inPlay = true;
						EditorApplication.isPlaying = true;
						return;
					}

					var elapsed = EditorApplication.timeSinceStartup - startTime;
					if (elapsed >= seconds)
					{
						EditorApplication.update -= Tick;
						EditorApplication.isPlaying = false;
						System.Console.WriteLine("Headless validation complete. Exiting with code 0.");
						EditorApplication.Exit(0);
					}
				}
			}
			catch (Exception ex)
			{
				System.Console.Error.WriteLine($"HeadlessPlayRunner failed: {ex.Message}\n{ex.StackTrace}");
				EditorApplication.Exit(1);
			}
		}

		private static void EnsureAgentSetup()
		{
			var player = GameObject.Find("Player") ?? new GameObject("Player");
			if (player.GetComponent<CharacterController>() == null)
			{
				player.AddComponent<CharacterController>();
			}
			var cam = Camera.main;
			if (cam == null)
			{
				var camGO = new GameObject("Main Camera");
				camGO.tag = "MainCamera";
				cam = camGO.AddComponent<Camera>();
			}

			if (player.GetComponent<AgentDirector>() == null)
			{
				player.AddComponent<AgentDirector>();
			}
			if (player.GetComponent<AIAutoplayer>() == null)
			{
				player.AddComponent<AIAutoplayer>();
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
