using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace NexoDirectorStudio.Game
{
    /// <summary>
    /// UI controller for the Doom FPS game.
    /// Handles HUD display, health, ammo, and game state UI.
    /// </summary>
    public class DoomFPSUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Text healthText;
        public Text armorText;
        public Text ammoText;
        public Text timeText;
        public Text enemiesText;
        public Text crosshairText;
        
        [Header("Game State UI")]
        public GameObject gameOverPanel;
        public GameObject victoryPanel;
        public GameObject timeUpPanel;
        public Text gameOverText;
        public Text victoryText;
        public Text timeUpText;
        
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
            healthText = healthObj.AddComponent<Text>();
            healthText.text = "Health: 100";
            healthText.fontSize = 24;
            healthText.color = Color.red;
            var healthRect = healthObj.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 0);
            healthRect.anchorMax = new Vector2(0, 0);
            healthRect.anchoredPosition = new Vector2(20, 20);
            
            // Armor display
            var armorObj = new GameObject("Armor Text");
            armorObj.transform.SetParent(canvas.transform);
            armorText = armorObj.AddComponent<Text>();
            armorText.text = "Armor: 0";
            armorText.fontSize = 24;
            armorText.color = Color.blue;
            var armorRect = armorObj.GetComponent<RectTransform>();
            armorRect.anchorMin = new Vector2(0, 0);
            armorRect.anchorMax = new Vector2(0, 0);
            armorRect.anchoredPosition = new Vector2(20, 50);
            
            // Ammo display
            var ammoObj = new GameObject("Ammo Text");
            ammoObj.transform.SetParent(canvas.transform);
            ammoText = ammoObj.AddComponent<Text>();
            ammoText.text = "Ammo: 30";
            ammoText.fontSize = 24;
            ammoText.color = Color.yellow;
            var ammoRect = ammoObj.GetComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(1, 0);
            ammoRect.anchorMax = new Vector2(1, 0);
            ammoRect.anchoredPosition = new Vector2(-20, 20);
            
            // Time display
            var timeObj = new GameObject("Time Text");
            timeObj.transform.SetParent(canvas.transform);
            timeText = timeObj.AddComponent<Text>();
            timeText.text = "Time: 00:00";
            timeText.fontSize = 24;
            timeText.color = Color.white;
            var timeRect = timeObj.GetComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(1, 1);
            timeRect.anchorMax = new Vector2(1, 1);
            timeRect.anchoredPosition = new Vector2(-20, -20);
            
            // Enemies display
            var enemiesObj = new GameObject("Enemies Text");
            enemiesObj.transform.SetParent(canvas.transform);
            enemiesText = enemiesObj.AddComponent<Text>();
            enemiesText.text = "Enemies: 0";
            enemiesText.fontSize = 24;
            enemiesText.color = Color.white;
            var enemiesRect = enemiesObj.GetComponent<RectTransform>();
            enemiesRect.anchorMin = new Vector2(0, 1);
            enemiesRect.anchorMax = new Vector2(0, 1);
            enemiesRect.anchoredPosition = new Vector2(20, -20);
            
            // Crosshair
            var crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(canvas.transform);
            crosshairText = crosshairObj.AddComponent<Text>();
            crosshairText.text = "+";
            crosshairText.fontSize = 32;
            crosshairText.color = Color.white;
            var crosshairRect = crosshairText.GetComponent<RectTransform>();
            crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.anchoredPosition = Vector2.zero;
        }
        
        void CreateGameStatePanels()
        {
            // Game Over Panel
            gameOverPanel = new GameObject("Game Over Panel");
            gameOverPanel.transform.SetParent(canvas.transform);
            var gameOverImage = gameOverPanel.AddComponent<Image>();
            gameOverImage.color = new Color(0, 0, 0, 0.8f);
            var gameOverRect = gameOverPanel.GetComponent<RectTransform>();
            gameOverRect.anchorMin = Vector2.zero;
            gameOverRect.anchorMax = Vector2.one;
            gameOverRect.offsetMin = Vector2.zero;
            gameOverRect.offsetMax = Vector2.zero;
            gameOverPanel.SetActive(false);
            
            var gameOverTextObj = new GameObject("Game Over Text");
            gameOverTextObj.transform.SetParent(gameOverPanel.transform);
            gameOverText = gameOverTextObj.AddComponent<Text>();
            gameOverText.text = "GAME OVER\nYou Died!";
            gameOverText.fontSize = 48;
            gameOverText.color = Color.red;
            gameOverText.alignment = TextAnchor.MiddleCenter;
            var gameOverTextRect = gameOverText.GetComponent<RectTransform>();
            gameOverTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            gameOverTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            gameOverTextRect.anchoredPosition = Vector2.zero;
            
            // Victory Panel
            victoryPanel = new GameObject("Victory Panel");
            victoryPanel.transform.SetParent(canvas.transform);
            var victoryImage = victoryPanel.AddComponent<Image>();
            victoryImage.color = new Color(0, 0, 0, 0.8f);
            var victoryRect = victoryPanel.GetComponent<RectTransform>();
            victoryRect.anchorMin = Vector2.zero;
            victoryRect.anchorMax = Vector2.one;
            victoryRect.offsetMin = Vector2.zero;
            victoryRect.offsetMax = Vector2.zero;
            victoryPanel.SetActive(false);
            
            var victoryTextObj = new GameObject("Victory Text");
            victoryTextObj.transform.SetParent(victoryPanel.transform);
            victoryText = victoryTextObj.AddComponent<Text>();
            victoryText.text = "VICTORY!\nAll Enemies Defeated!";
            victoryText.fontSize = 48;
            victoryText.color = Color.green;
            victoryText.alignment = TextAnchor.MiddleCenter;
            var victoryTextRect = victoryText.GetComponent<RectTransform>();
            victoryTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            victoryTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            victoryTextRect.anchoredPosition = Vector2.zero;
            
            // Time Up Panel
            timeUpPanel = new GameObject("Time Up Panel");
            timeUpPanel.transform.SetParent(canvas.transform);
            var timeUpImage = timeUpPanel.AddComponent<Image>();
            timeUpImage.color = new Color(0, 0, 0, 0.8f);
            var timeUpRect = timeUpPanel.GetComponent<RectTransform>();
            timeUpRect.anchorMin = Vector2.zero;
            timeUpRect.anchorMax = Vector2.one;
            timeUpRect.offsetMin = Vector2.zero;
            timeUpRect.offsetMax = Vector2.zero;
            timeUpPanel.SetActive(false);
            
            var timeUpTextObj = new GameObject("Time Up Text");
            timeUpTextObj.transform.SetParent(timeUpPanel.transform);
            timeUpText = timeUpTextObj.AddComponent<Text>();
            timeUpText.text = "TIME'S UP!\nMission Failed!";
            timeUpText.fontSize = 48;
            timeUpText.color = Color.yellow;
            timeUpText.alignment = TextAnchor.MiddleCenter;
            var timeUpTextRect = timeUpText.GetComponent<RectTransform>();
            timeUpTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            timeUpTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            timeUpTextRect.anchoredPosition = Vector2.zero;
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
