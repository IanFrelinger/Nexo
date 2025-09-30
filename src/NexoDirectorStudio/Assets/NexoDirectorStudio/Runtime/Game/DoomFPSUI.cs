using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// UI controller for the Doom FPS game.
    /// Handles HUD display, health, ammo, and game state UI.
    /// </summary>
    public class DoomFPSUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI healthText;
        public TextMeshProUGUI armorText;
        public TextMeshProUGUI ammoText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI enemiesText;
        public TextMeshProUGUI crosshairText;
        
        [Header("Game State UI")]
        public GameObject gameOverPanel;
        public GameObject victoryPanel;
        public GameObject timeUpPanel;
        public TextMeshProUGUI gameOverText;
        public TextMeshProUGUI victoryText;
        public TextMeshProUGUI timeUpText;
        
        private DoomFPSGame game;
        private Canvas canvas;
        
        void Start()
        {
            // Create UI canvas
            CreateUICanvas();
        }
        
        public void Initialize(DoomFPSGame gameInstance)
        {
            game = gameInstance;
            UpdateUI();
        }
        
        void Update()
        {
            if (game != null)
            {
                UpdateUI();
            }
        }
        
        void CreateUICanvas()
        {
            // Create main canvas
            var canvasObj = new GameObject("UI Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create HUD elements
            CreateHUD();
            CreateGameStatePanels();
        }
        
        void CreateHUD()
        {
            // Health display
            var healthObj = new GameObject("Health Text");
            healthObj.transform.SetParent(canvas.transform);
            healthText = healthObj.AddComponent<TextMeshProUGUI>();
            healthText.text = "Health: 100";
            healthText.fontSize = 24;
            healthText.color = Color.red;
            healthText.rectTransform.anchorMin = new Vector2(0, 0);
            healthText.rectTransform.anchorMax = new Vector2(0, 0);
            healthText.rectTransform.anchoredPosition = new Vector2(20, 20);
            
            // Armor display
            var armorObj = new GameObject("Armor Text");
            armorObj.transform.SetParent(canvas.transform);
            armorText = armorObj.AddComponent<TextMeshProUGUI>();
            armorText.text = "Armor: 0";
            armorText.fontSize = 24;
            armorText.color = Color.blue;
            armorText.rectTransform.anchorMin = new Vector2(0, 0);
            armorText.rectTransform.anchorMax = new Vector2(0, 0);
            armorText.rectTransform.anchoredPosition = new Vector2(20, 50);
            
            // Ammo display
            var ammoObj = new GameObject("Ammo Text");
            ammoObj.transform.SetParent(canvas.transform);
            ammoText = ammoObj.AddComponent<TextMeshProUGUI>();
            ammoText.text = "Ammo: 30";
            ammoText.fontSize = 24;
            ammoText.color = Color.yellow;
            ammoObj.rectTransform.anchorMin = new Vector2(1, 0);
            ammoObj.rectTransform.anchorMax = new Vector2(1, 0);
            ammoObj.rectTransform.anchoredPosition = new Vector2(-20, 20);
            
            // Time display
            var timeObj = new GameObject("Time Text");
            timeObj.transform.SetParent(canvas.transform);
            timeText = timeObj.AddComponent<TextMeshProUGUI>();
            timeText.text = "Time: 00:00";
            timeText.fontSize = 24;
            timeText.color = Color.white;
            timeObj.rectTransform.anchorMin = new Vector2(1, 1);
            timeObj.rectTransform.anchorMax = new Vector2(1, 1);
            timeObj.rectTransform.anchoredPosition = new Vector2(-20, -20);
            
            // Enemies display
            var enemiesObj = new GameObject("Enemies Text");
            enemiesObj.transform.SetParent(canvas.transform);
            enemiesText = enemiesObj.AddComponent<TextMeshProUGUI>();
            enemiesText.text = "Enemies: 0";
            enemiesText.fontSize = 24;
            enemiesText.color = Color.white;
            enemiesObj.rectTransform.anchorMin = new Vector2(0, 1);
            enemiesObj.rectTransform.anchorMax = new Vector2(0, 1);
            enemiesObj.rectTransform.anchoredPosition = new Vector2(20, -20);
            
            // Crosshair
            var crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(canvas.transform);
            crosshairText = crosshairObj.AddComponent<TextMeshProUGUI>();
            crosshairText.text = "+";
            crosshairText.fontSize = 32;
            crosshairText.color = Color.white;
            crosshairText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairText.rectTransform.anchoredPosition = Vector2.zero;
        }
        
        void CreateGameStatePanels()
        {
            // Game Over Panel
            gameOverPanel = new GameObject("Game Over Panel");
            gameOverPanel.transform.SetParent(canvas.transform);
            var gameOverImage = gameOverPanel.AddComponent<Image>();
            gameOverImage.color = new Color(0, 0, 0, 0.8f);
            gameOverPanel.rectTransform.anchorMin = Vector2.zero;
            gameOverPanel.rectTransform.anchorMax = Vector2.one;
            gameOverPanel.rectTransform.offsetMin = Vector2.zero;
            gameOverPanel.rectTransform.offsetMax = Vector2.zero;
            gameOverPanel.SetActive(false);
            
            var gameOverTextObj = new GameObject("Game Over Text");
            gameOverTextObj.transform.SetParent(gameOverPanel.transform);
            gameOverText = gameOverTextObj.AddComponent<TextMeshProUGUI>();
            gameOverText.text = "GAME OVER\nYou Died!";
            gameOverText.fontSize = 48;
            gameOverText.color = Color.red;
            gameOverText.alignment = TextAlignmentOptions.Center;
            gameOverText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            gameOverText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            gameOverText.rectTransform.anchoredPosition = Vector2.zero;
            
            // Victory Panel
            victoryPanel = new GameObject("Victory Panel");
            victoryPanel.transform.SetParent(canvas.transform);
            var victoryImage = victoryPanel.AddComponent<Image>();
            victoryImage.color = new Color(0, 0, 0, 0.8f);
            victoryPanel.rectTransform.anchorMin = Vector2.zero;
            victoryPanel.rectTransform.anchorMax = Vector2.one;
            victoryPanel.rectTransform.offsetMin = Vector2.zero;
            victoryPanel.rectTransform.offsetMax = Vector2.zero;
            victoryPanel.SetActive(false);
            
            var victoryTextObj = new GameObject("Victory Text");
            victoryTextObj.transform.SetParent(victoryPanel.transform);
            victoryText = victoryTextObj.AddComponent<TextMeshProUGUI>();
            victoryText.text = "VICTORY!\nAll Enemies Defeated!";
            victoryText.fontSize = 48;
            victoryText.color = Color.green;
            victoryText.alignment = TextAlignmentOptions.Center;
            victoryText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            victoryText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            victoryText.rectTransform.anchoredPosition = Vector2.zero;
            
            // Time Up Panel
            timeUpPanel = new GameObject("Time Up Panel");
            timeUpPanel.transform.SetParent(canvas.transform);
            var timeUpImage = timeUpPanel.AddComponent<Image>();
            timeUpImage.color = new Color(0, 0, 0, 0.8f);
            timeUpPanel.rectTransform.anchorMin = Vector2.zero;
            timeUpPanel.rectTransform.anchorMax = Vector2.one;
            timeUpPanel.rectTransform.offsetMin = Vector2.zero;
            timeUpPanel.rectTransform.offsetMax = Vector2.zero;
            timeUpPanel.SetActive(false);
            
            var timeUpTextObj = new GameObject("Time Up Text");
            timeUpTextObj.transform.SetParent(timeUpPanel.transform);
            timeUpText = timeUpTextObj.AddComponent<TextMeshProUGUI>();
            timeUpText.text = "TIME'S UP!\nMission Failed!";
            timeUpText.fontSize = 48;
            timeUpText.color = Color.yellow;
            timeUpText.alignment = TextAlignmentOptions.Center;
            timeUpText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            timeUpText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            timeUpText.rectTransform.anchoredPosition = Vector2.zero;
        }
        
        void UpdateUI()
        {
            if (game == null) return;
            
            // Update health
            if (healthText != null)
            {
                healthText.text = $"Health: {game.health}";
                healthText.color = game.health > 50 ? Color.red : Color.white;
            }
            
            // Update armor
            if (armorText != null)
            {
                armorText.text = $"Armor: {game.armor}";
                armorText.color = game.armor > 0 ? Color.blue : Color.gray;
            }
            
            // Update ammo
            if (ammoText != null)
            {
                ammoText.text = $"Ammo: {game.currentAmmo}";
                ammoText.color = game.currentAmmo > 10 ? Color.yellow : Color.red;
            }
            
            // Update time
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(game.gameTime / 60);
                int seconds = Mathf.FloorToInt(game.gameTime % 60);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
            
            // Update enemies
            if (enemiesText != null)
            {
                enemiesText.text = $"Enemies: {game.enemiesKilled}";
            }
        }
        
        public void ShowGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }
        
        public void ShowVictory()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }
        
        public void ShowTimeUp()
        {
            if (timeUpPanel != null)
            {
                timeUpPanel.SetActive(true);
            }
        }
    }
}
