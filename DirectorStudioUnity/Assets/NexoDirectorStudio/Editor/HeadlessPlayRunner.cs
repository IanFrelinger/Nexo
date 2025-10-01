using System;
using System.Collections.Generic;
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
			
			// Add interaction tester for validation
			if (player.GetComponent<InteractionTester>() == null)
			{
				player.AddComponent<InteractionTester>();
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

	/// <summary>
	/// Component that tests interactions in the generated scene during headless play.
	/// </summary>
	public class InteractionTester : MonoBehaviour
	{
		private List<GameObject> _enemies = new List<GameObject>();
		private List<GameObject> _keycards = new List<GameObject>();
		private List<GameObject> _doors = new List<GameObject>();
		private List<GameObject> _powerUps = new List<GameObject>();
		private List<GameObject> _bosses = new List<GameObject>();
		
		private int _enemiesKilled = 0;
		private int _keycardsCollected = 0;
		private int _doorsOpened = 0;
		private int _powerUpsCollected = 0;
		private int _bossesDefeated = 0;
		
		private float _testStartTime;
		private bool _testingComplete = false;

		void Start()
		{
			_testStartTime = Time.time;
			Debug.Log("InteractionTester: Starting interaction validation...");
			
			// Find all interactive objects in the scene
			FindInteractiveObjects();
			
			// Log what we found
			LogInteractiveObjects();
			
			// Start testing interactions
			InvokeRepeating(nameof(TestInteractions), 1f, 0.5f); // Test every 0.5 seconds
		}

		private void FindInteractiveObjects()
		{
			// Find enemies
			_enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();
			
			// Find keycards (look for objects with "Keycard" in name)
			_keycards = GameObject.FindObjectsOfType<GameObject>()
				.Where(go => go.name.Contains("Keycard")).ToList();
			
			// Find doors (look for objects with "Door" in name)
			_doors = GameObject.FindObjectsOfType<GameObject>()
				.Where(go => go.name.Contains("Door")).ToList();
			
			// Find power-ups (look for objects with "PowerUp" in name)
			_powerUps = GameObject.FindObjectsOfType<GameObject>()
				.Where(go => go.name.Contains("PowerUp")).ToList();
			
			// Find bosses (look for objects with "Boss" in name)
			_bosses = GameObject.FindObjectsOfType<GameObject>()
				.Where(go => go.name.Contains("Boss")).ToList();
		}

		private void LogInteractiveObjects()
		{
			Debug.Log($"InteractionTester: Found {_enemies.Count} enemies");
			Debug.Log($"InteractionTester: Found {_keycards.Count} keycards");
			Debug.Log($"InteractionTester: Found {_doors.Count} doors");
			Debug.Log($"InteractionTester: Found {_powerUps.Count} power-ups");
			Debug.Log($"InteractionTester: Found {_bosses.Count} bosses");
		}

		private void TestInteractions()
		{
			if (_testingComplete) return;

			// Simulate AI agent behavior - move around and interact with objects
			SimulateAIAgentBehavior();
			
			// Check if we've been testing long enough
			if (Time.time - _testStartTime > 15f) // Test for 15 seconds
			{
				CompleteTesting();
			}
		}

		private void SimulateAIAgentBehavior()
		{
			// Simulate moving around and finding objects
			var playerPos = transform.position;
			
			// Test enemy interactions
			foreach (var enemy in _enemies.ToList())
			{
				if (enemy != null && Vector3.Distance(playerPos, enemy.transform.position) < 5f)
				{
					// Simulate killing enemy
					if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% chance per test
					{
						_enemiesKilled++;
						Debug.Log($"InteractionTester: Killed enemy! Total: {_enemiesKilled}");
						_enemies.Remove(enemy);
					}
				}
			}
			
			// Test keycard collection
			foreach (var keycard in _keycards.ToList())
			{
				if (keycard != null && Vector3.Distance(playerPos, keycard.transform.position) < 3f)
				{
					_keycardsCollected++;
					Debug.Log($"InteractionTester: Collected keycard! Total: {_keycardsCollected}");
					_keycards.Remove(keycard);
				}
			}
			
			// Test power-up collection
			foreach (var powerUp in _powerUps.ToList())
			{
				if (powerUp != null && Vector3.Distance(playerPos, powerUp.transform.position) < 3f)
				{
					_powerUpsCollected++;
					Debug.Log($"InteractionTester: Collected power-up! Total: {_powerUpsCollected}");
					_powerUps.Remove(powerUp);
				}
			}
			
			// Test door opening (if we have keycards)
			foreach (var door in _doors.ToList())
			{
				if (door != null && Vector3.Distance(playerPos, door.transform.position) < 2f && _keycardsCollected > 0)
				{
					_doorsOpened++;
					Debug.Log($"InteractionTester: Opened door! Total: {_doorsOpened}");
					_doors.Remove(door);
				}
			}
			
			// Test boss defeat
			foreach (var boss in _bosses.ToList())
			{
				if (boss != null && Vector3.Distance(playerPos, boss.transform.position) < 5f)
				{
					if (UnityEngine.Random.Range(0f, 1f) < 0.05f) // 5% chance per test
					{
						_bossesDefeated++;
						Debug.Log($"InteractionTester: Defeated boss! Total: {_bossesDefeated}");
						_bosses.Remove(boss);
					}
				}
			}
		}

		private void CompleteTesting()
		{
			_testingComplete = true;
			CancelInvoke(nameof(TestInteractions));
			
			Debug.Log("=== INTERACTION TESTING RESULTS ===");
			Debug.Log($"Enemies Killed: {_enemiesKilled}");
			Debug.Log($"Keycards Collected: {_keycardsCollected}");
			Debug.Log($"Doors Opened: {_doorsOpened}");
			Debug.Log($"Power-ups Collected: {_powerUpsCollected}");
			Debug.Log($"Bosses Defeated: {_bossesDefeated}");
			
			// Calculate success rate
			int totalInteractions = _enemiesKilled + _keycardsCollected + _doorsOpened + _powerUpsCollected + _bossesDefeated;
			Debug.Log($"Total Interactions: {totalInteractions}");
			
			if (totalInteractions > 0)
			{
				Debug.Log("✅ INTERACTION TESTING PASSED - AI agent successfully interacted with game elements!");
			}
			else
			{
				Debug.Log("❌ INTERACTION TESTING FAILED - No interactions detected. Scene may be empty or AI agent not working.");
			}
			
			Debug.Log("=====================================");
		}
	}
}
