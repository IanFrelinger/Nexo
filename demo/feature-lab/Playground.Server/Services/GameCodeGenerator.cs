using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Generates game code based on requirements.
/// </summary>
public class GameCodeGenerator
{
    public string GenerateGameCode(string prompt, List<string> features)
    {
        var baseCode = @"using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header(""Game Settings"")]
    public float gameSpeed = 1f;
    public int score = 0;
    public int lives = 3;
    
    [Header(""UI References"")]
    public UnityEngine.UI.Text scoreText;
    public UnityEngine.UI.Text livesText;
    public UnityEngine.UI.Text gameOverText;
    
    [Header(""Game Objects"")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public GameObject collectiblePrefab;
    
    private bool gameRunning = true;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<GameObject> activeCollectibles = new List<GameObject>();
    
    void Start()
    {
        InitializeGame();
        StartCoroutine(GameLoop());
    }
    
    void InitializeGame()
    {
        // Initialize game components
        {INITIALIZATION_CODE}
        
        // Setup UI
        UpdateUI();
        
        // Spawn initial objects
        SpawnInitialObjects();
    }
    
    IEnumerator GameLoop()
    {
        while (gameRunning)
        {
            UpdateGame();
            yield return new WaitForSeconds(0.016f); // ~60 FPS
        }
    }
    
    void UpdateGame()
    {
        if (!gameRunning) return;
        
        // Update game logic
        {GAME_LOGIC_CODE}
        
        // Check win/lose conditions
        CheckGameConditions();
    }
    
    void SpawnInitialObjects()
    {
        // Spawn initial game objects
        {SPAWN_CODE}
    }
    
    void CheckGameConditions()
    {
        // Check win/lose conditions
        {CONDITIONS_CODE}
    }
    
    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }
    
    public void LoseLife()
    {
        lives--;
        UpdateUI();
        
        if (lives <= 0)
        {
            GameOver();
        }
    }
    
    void GameOver()
    {
        gameRunning = false;
        gameOverText.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
    
    void UpdateUI()
    {
        if (scoreText) scoreText.text = ""Score: "" + score;
        if (livesText) livesText.text = ""Lives: "" + lives;
    }
    
    // Additional game functionality
    {ADDITIONAL_CODE}
}

// Player controller
public class PlayerController : MonoBehaviour
{
    [Header(""Movement Settings"")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        // Movement
        float horizontal = Input.GetAxis(""Horizontal"");
        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        
        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(""Ground""))
        {
            isGrounded = true;
        }
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(""Ground""))
        {
            isGrounded = false;
        }
    }
}

// Enemy AI
public class EnemyAI : MonoBehaviour
{
    [Header(""AI Settings"")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    public float attackRange = 1f;
    
    private Transform player;
    private Rigidbody2D rb;
    private bool isAttacking = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag(""Player"").transform;
    }
    
    void Update()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                MoveTowardsPlayer();
                
                if (distanceToPlayer <= attackRange)
                {
                    Attack();
                }
            }
        }
    }
    
    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
    }
    
    void Attack()
    {
        if (!isAttacking)
        {
            StartCoroutine(AttackCoroutine());
        }
    }
    
    IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        // Attack logic here
        yield return new WaitForSeconds(1f);
        isAttacking = false;
    }
}

// Collectible system
public class Collectible : MonoBehaviour
{
    [Header(""Collectible Settings"")]
    public int value = 10;
    public float rotationSpeed = 50f;
    
    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(""Player""))
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.AddScore(value);
            }
            Destroy(gameObject);
        }
    }
}";

        var initializationCode = GenerateInitializationCode(features);
        var gameLogicCode = GenerateGameLogicCode(features);
        var spawnCode = GenerateSpawnCode(features);
        var conditionsCode = GenerateConditionsCode(features);
        var additionalCode = GenerateAdditionalCode(features);

        return baseCode
            .Replace("{INITIALIZATION_CODE}", initializationCode)
            .Replace("{GAME_LOGIC_CODE}", gameLogicCode)
            .Replace("{SPAWN_CODE}", spawnCode)
            .Replace("{CONDITIONS_CODE}", conditionsCode)
            .Replace("{ADDITIONAL_CODE}", additionalCode);
    }

    private string GenerateInitializationCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("2D Graphics"))
        {
            code.Add(@"
        // Initialize 2D graphics
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 5f;");
        }

        if (features.Contains("Physics"))
        {
            code.Add(@"
        // Initialize physics
        Physics2D.gravity = new Vector2(0, -9.81f);");
        }

        if (features.Contains("Animation"))
        {
            code.Add(@"
        // Initialize animation system
        AnimationManager.Initialize();");
        }

        if (features.Contains("Audio"))
        {
            code.Add(@"
        // Initialize audio system
        AudioManager.Initialize();");
        }

        return string.Join("\n        ", code);
    }

    private string GenerateGameLogicCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Physics"))
        {
            code.Add(@"
        // Update physics
        Physics2D.Simulate(Time.fixedDeltaTime);");
        }

        if (features.Contains("Animation"))
        {
            code.Add(@"
        // Update animations
        AnimationManager.UpdateAnimations();");
        }

        if (features.Contains("AI"))
        {
            code.Add(@"
        // Update AI
        UpdateEnemyAI();");
        }

        if (features.Contains("Collision"))
        {
            code.Add(@"
        // Check collisions
        CheckCollisions();");
        }

        return string.Join("\n        ", code);
    }

    private string GenerateSpawnCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Enemies"))
        {
            code.Add(@"
        // Spawn enemies
        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-10f, 10f), Random.Range(0f, 5f), 0);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            activeEnemies.Add(enemy);
        }");
        }

        if (features.Contains("Collectibles"))
        {
            code.Add(@"
        // Spawn collectibles
        for (int i = 0; i < 10; i++)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-10f, 10f), Random.Range(0f, 5f), 0);
            GameObject collectible = Instantiate(collectiblePrefab, spawnPos, Quaternion.identity);
            activeCollectibles.Add(collectible);
        }");
        }

        if (features.Contains("Power-ups"))
        {
            code.Add(@"
        // Spawn power-ups
        SpawnPowerUps();");
        }

        return string.Join("\n        ", code);
    }

    private string GenerateConditionsCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Score"))
        {
            code.Add(@"
        // Check score conditions
        if (score >= 1000)
        {
            // Level complete
            Debug.Log(""Level Complete!"");
        }");
        }

        if (features.Contains("Time"))
        {
            code.Add(@"
        // Check time conditions
        if (Time.time >= 300f) // 5 minutes
        {
            GameOver();
        }");
        }

        if (features.Contains("Lives"))
        {
            code.Add(@"
        // Check lives conditions
        if (lives <= 0)
        {
            GameOver();
        }");
        }

        return string.Join("\n        ", code);
    }

    private string GenerateAdditionalCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Levels"))
        {
            code.Add(@"
    // Level management
    public void LoadNextLevel()
    {
        // Implement level loading
        Debug.Log(""Loading next level..."");
    }
    
    public void RestartLevel()
    {
        // Implement level restart
        Debug.Log(""Restarting level..."");
    }");
        }

        if (features.Contains("Save System"))
        {
            code.Add(@"
    // Save system
    public void SaveGame()
    {
        PlayerPrefs.SetInt(""Score"", score);
        PlayerPrefs.SetInt(""Lives"", lives);
        PlayerPrefs.Save();
    }
    
    public void LoadGame()
    {
        score = PlayerPrefs.GetInt(""Score"", 0);
        lives = PlayerPrefs.GetInt(""Lives"", 3);
    }");
        }

        if (features.Contains("Multiplayer"))
        {
            code.Add(@"
    // Multiplayer functionality
    public void StartMultiplayer()
    {
        // Implement multiplayer
        Debug.Log(""Starting multiplayer game..."");
    }
    
    public void JoinGame(string gameId)
    {
        // Implement joining game
        Debug.Log(""Joining game: "" + gameId);
    }");
        }

        return string.Join("\n", code);
    }
}
