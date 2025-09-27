using System;

namespace Playground.Server.Services.Templates
{
    public partial class GameTemplateGenerator
    {
        public static string GenerateGameCode()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>2D Platformer Game</title>
    <style>
        body { margin: 0; padding: 0; background: #87CEEB; overflow: hidden; }
        #gameCanvas { display: block; background: linear-gradient(to bottom, #87CEEB 0%, #98FB98 100%); }
        .game-ui { position: absolute; top: 10px; left: 10px; color: white; font-family: Arial, sans-serif; }
        .game-over { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); 
                     background: rgba(0,0,0,0.8); color: white; padding: 20px; border-radius: 10px; 
                     text-align: center; display: none; }
    </style>
</head>
<body>
    <canvas id=""gameCanvas"" width=""800"" height=""600""></canvas>
    <div class=""game-ui"">
        <div>Score: <span id=""score"">0</span></div>
        <div>Lives: <span id=""lives"">3</span></div>
    </div>
    <div class=""game-over"" id=""gameOver"">
        <h2>Game Over!</h2>
        <p>Final Score: <span id=""finalScore"">0</span></p>
        <button onclick=""restartGame()"">Play Again</button>
    </div>

    <script>
        const canvas = document.getElementById('gameCanvas');
        const ctx = canvas.getContext('2d');
        
        let gameState = 'playing';
        let score = 0;
        let lives = 3;
        
        // Player object
        const player = {
            x: 50,
            y: 400,
            width: 30,
            height: 30,
            velocityX: 0,
            velocityY: 0,
            speed: 5,
            jumpPower: 15,
            onGround: false,
            color: '#FF6B6B'
        };
        
        // Platforms
        const platforms = [
            { x: 0, y: 550, width: 200, height: 50 },
            { x: 250, y: 500, width: 150, height: 50 },
            { x: 450, y: 450, width: 150, height: 50 },
            { x: 650, y: 400, width: 150, height: 50 }
        ];
        
        // Collectibles
        const collectibles = [
            { x: 300, y: 450, width: 20, height: 20, collected: false },
            { x: 500, y: 400, width: 20, height: 20, collected: false },
            { x: 700, y: 350, width: 20, height: 20, collected: false }
        ];
        
        // Game loop
        function gameLoop() {
            if (gameState !== 'playing') return;
            
            update();
            render();
            requestAnimationFrame(gameLoop);
        }
        
        function update() {
            // Update player physics
            player.velocityY += 0.8; // Gravity
            player.x += player.velocityX;
            player.y += player.velocityY;
            
            // Check platform collisions
            player.onGround = false;
            platforms.forEach(platform => {
                if (player.x < platform.x + platform.width &&
                    player.x + player.width > platform.x &&
                    player.y < platform.y + platform.height &&
                    player.y + player.height > platform.y) {
                    
                    if (player.velocityY > 0) {
                        player.y = platform.y - player.height;
                        player.velocityY = 0;
                        player.onGround = true;
                    }
                }
            });
            
            // Check collectible collisions
            collectibles.forEach(collectible => {
                if (!collectible.collected &&
                    player.x < collectible.x + collectible.width &&
                    player.x + player.width > collectible.x &&
                    player.y < collectible.y + collectible.height &&
                    player.y + player.height > collectible.y) {
                    
                    collectible.collected = true;
                    score += 100;
                    updateUI();
                }
            });
            
            // Check if player fell off screen
            if (player.y > canvas.height) {
                lives--;
                updateUI();
                if (lives <= 0) {
                    gameOver();
                } else {
                    resetPlayer();
                }
            }
            
            // Check if all collectibles collected
            if (collectibles.every(c => c.collected)) {
                score += 500;
                updateUI();
                // Add more collectibles or level complete logic
            }
        }
        
        function render() {
            // Clear canvas
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            // Draw platforms
            ctx.fillStyle = '#8B4513';
            platforms.forEach(platform => {
                ctx.fillRect(platform.x, platform.y, platform.width, platform.height);
            });
            
            // Draw collectibles
            ctx.fillStyle = '#FFD700';
            collectibles.forEach(collectible => {
                if (!collectible.collected) {
                    ctx.fillRect(collectible.x, collectible.y, collectible.width, collectible.height);
                }
            });
            
            // Draw player
            ctx.fillStyle = player.color;
            ctx.fillRect(player.x, player.y, player.width, player.height);
        }
        
        function updateUI() {
            document.getElementById('score').textContent = score;
            document.getElementById('lives').textContent = lives;
        }
        
        function resetPlayer() {
            player.x = 50;
            player.y = 400;
            player.velocityX = 0;
            player.velocityY = 0;
        }
        
        function gameOver() {
            gameState = 'gameOver';
            document.getElementById('finalScore').textContent = score;
            document.getElementById('gameOver').style.display = 'block';
        }
        
        function restartGame() {
            gameState = 'playing';
            score = 0;
            lives = 3;
            collectibles.forEach(c => c.collected = false);
            resetPlayer();
            updateUI();
            document.getElementById('gameOver').style.display = 'none';
            gameLoop();
        }
        
        // Event listeners
        document.addEventListener('keydown', (e) => {
            if (gameState !== 'playing') return;
            
            switch(e.key) {
                case 'ArrowLeft':
                    player.velocityX = -player.speed;
                    break;
                case 'ArrowRight':
                    player.velocityX = player.speed;
                    break;
                case 'ArrowUp':
                case ' ':
                    if (player.onGround) {
                        player.velocityY = -player.jumpPower;
                    }
                    break;
            }
        });
        
        document.addEventListener('keyup', (e) => {
            if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
                player.velocityX = 0;
            }
        });
        
        // Start game
        updateUI();
        gameLoop();
    </script>
</body>
</html>";
        }
    }
}
