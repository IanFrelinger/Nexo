using System;

namespace Playground.Server.Services.Templates
{
    public class WebAppTemplateGenerator
    {
        public static string GenerateTodoAppCode()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Todo App</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; margin-bottom: 30px; }
        .theme-toggle { position: absolute; top: 20px; right: 20px; }
        .todo-input { display: flex; gap: 10px; margin-bottom: 20px; }
        .todo-input input { flex: 1; padding: 12px; border: 1px solid #ddd; border-radius: 8px; }
        .todo-input button { padding: 12px 24px; background: #007bff; color: white; border: none; border-radius: 8px; cursor: pointer; }
        .todo-list { list-style: none; }
        .todo-item { background: white; margin: 8px 0; padding: 15px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); display: flex; align-items: center; gap: 10px; }
        .todo-item.completed { opacity: 0.6; text-decoration: line-through; }
        .todo-item input[type=""checkbox""] { margin-right: 10px; }
        .todo-item .delete-btn { margin-left: auto; background: #dc3545; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer; }
        .dark-mode { background: #1a1a1a; color: #ffffff; }
        .dark-mode .todo-item { background: #2d2d2d; color: #ffffff; }
        .dark-mode .todo-input input { background: #2d2d2d; color: #ffffff; border-color: #444; }
        .dark-mode .todo-input button { background: #0d6efd; }
    </style>
</head>
<body>
    <div class=""container"">
        <button class=""theme-toggle"" onclick=""toggleTheme()"">🌙</button>
        <div class=""header"">
            <h1>Todo App</h1>
            <p>Organize your tasks efficiently</p>
        </div>
        <div class=""todo-input"">
            <input type=""text"" id=""todoInput"" placeholder=""Add a new task..."">
            <button onclick=""addTodo()"">Add Task</button>
        </div>
        <ul class=""todo-list"" id=""todoList""></ul>
    </div>

    <script>
        let todos = JSON.parse(localStorage.getItem('todos')) || [];
        let isDarkMode = localStorage.getItem('darkMode') === 'true';

        function init() {
            applyTheme();
            renderTodos();
        }

        function toggleTheme() {
            isDarkMode = !isDarkMode;
            localStorage.setItem('darkMode', isDarkMode);
            applyTheme();
        }

        function applyTheme() {
            document.body.classList.toggle('dark-mode', isDarkMode);
            document.querySelector('.theme-toggle').textContent = isDarkMode ? '☀️' : '🌙';
        }

        function addTodo() {
            const input = document.getElementById('todoInput');
            const text = input.value.trim();
            if (text) {
                todos.push({ id: Date.now(), text, completed: false });
                input.value = '';
                saveTodos();
                renderTodos();
            }
        }

        function toggleTodo(id) {
            const todo = todos.find(t => t.id === id);
            if (todo) {
                todo.completed = !todo.completed;
                saveTodos();
                renderTodos();
            }
        }

        function deleteTodo(id) {
            todos = todos.filter(t => t.id !== id);
            saveTodos();
            renderTodos();
        }

        function renderTodos() {
            const list = document.getElementById('todoList');
            list.innerHTML = todos.map(todo => `
                <li class=""todo-item ${todo.completed ? 'completed' : ''}"">
                    <input type=""checkbox"" ${todo.completed ? 'checked' : ''} onchange=""toggleTodo(${todo.id})"">
                    <span>${todo.text}</span>
                    <button class=""delete-btn"" onclick=""deleteTodo(${todo.id})"">Delete</button>
                </li>
            `).join('');
        }

        function saveTodos() {
            localStorage.setItem('todos', JSON.stringify(todos));
        }

        document.getElementById('todoInput').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') addTodo();
        });

        init();
    </script>
</body>
</html>";
        }

        public static string GenerateEcommerceCode()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>E-commerce Store</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        .header { background: #007bff; color: white; padding: 1rem; }
        .nav { display: flex; justify-content: space-between; align-items: center; }
        .logo { font-size: 1.5rem; font-weight: bold; }
        .cart-btn { background: #28a745; color: white; border: none; padding: 0.5rem 1rem; border-radius: 4px; cursor: pointer; }
        .products { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; padding: 1rem; }
        .product { border: 1px solid #ddd; border-radius: 8px; padding: 1rem; text-align: center; }
        .product img { width: 100%; height: 200px; object-fit: cover; border-radius: 4px; }
        .product h3 { margin: 0.5rem 0; }
        .product .price { font-size: 1.2rem; font-weight: bold; color: #007bff; }
        .add-to-cart { background: #007bff; color: white; border: none; padding: 0.5rem 1rem; border-radius: 4px; cursor: pointer; margin-top: 0.5rem; }
    </style>
</head>
<body>
    <header class=""header"">
        <nav class=""nav"">
            <div class=""logo"">ShopNow</div>
            <button class=""cart-btn"" onclick=""showCart()"">Cart (0)</button>
        </nav>
    </header>
    
    <main>
        <div class=""products"" id=""products"">
            <!-- Products will be loaded here -->
        </div>
    </main>

    <script>
        let cart = [];
        let products = [
            { id: 1, name: 'Laptop', price: 999, image: 'https://via.placeholder.com/300x200' },
            { id: 2, name: 'Phone', price: 699, image: 'https://via.placeholder.com/300x200' },
            { id: 3, name: 'Tablet', price: 399, image: 'https://via.placeholder.com/300x200' }
        ];

        function renderProducts() {
            const container = document.getElementById('products');
            container.innerHTML = products.map(product => `
                <div class=""product"">
                    <img src=""${product.image}"" alt=""${product.name}"">
                    <h3>${product.name}</h3>
                    <div class=""price"">$${product.price}</div>
                    <button class=""add-to-cart"" onclick=""addToCart(${product.id})"">Add to Cart</button>
                </div>
            `).join('');
        }

        function addToCart(productId) {
            const product = products.find(p => p.id === productId);
            if (product) {
                cart.push(product);
                updateCartDisplay();
            }
        }

        function updateCartDisplay() {
            const cartBtn = document.querySelector('.cart-btn');
            cartBtn.textContent = `Cart (${cart.length})`;
        }

        function showCart() {
            alert(`Cart contains ${cart.length} items`);
        }

        renderProducts();
    </script>
</body>
</html>";
        }

        public static string GenerateChatAppCode()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Chat App</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; height: 100vh; display: flex; flex-direction: column; }
        .chat-container { flex: 1; display: flex; flex-direction: column; max-width: 800px; margin: 0 auto; border: 1px solid #ddd; }
        .chat-header { background: #007bff; color: white; padding: 1rem; text-align: center; }
        .chat-messages { flex: 1; padding: 1rem; overflow-y: auto; background: #f8f9fa; }
        .message { margin: 0.5rem 0; padding: 0.5rem; border-radius: 8px; max-width: 70%; }
        .message.sent { background: #007bff; color: white; margin-left: auto; }
        .message.received { background: white; border: 1px solid #ddd; }
        .chat-input { display: flex; padding: 1rem; background: white; border-top: 1px solid #ddd; }
        .chat-input input { flex: 1; padding: 0.5rem; border: 1px solid #ddd; border-radius: 4px; }
        .chat-input button { margin-left: 0.5rem; padding: 0.5rem 1rem; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; }
    </style>
</head>
<body>
    <div class=""chat-container"">
        <div class=""chat-header"">
            <h1>Chat Room</h1>
        </div>
        <div class=""chat-messages"" id=""messages""></div>
        <div class=""chat-input"">
            <input type=""text"" id=""messageInput"" placeholder=""Type a message..."">
            <button onclick=""sendMessage()"">Send</button>
        </div>
    </div>

    <script>
        function sendMessage() {
            const input = document.getElementById('messageInput');
            const message = input.value.trim();
            if (message) {
                addMessage(message, 'sent');
                input.value = '';
                // Simulate received message
                setTimeout(() => {
                    addMessage('Thanks for your message!', 'received');
                }, 1000);
            }
        }

        function addMessage(text, type) {
            const messages = document.getElementById('messages');
            const messageDiv = document.createElement('div');
            messageDiv.className = `message ${type}`;
            messageDiv.textContent = text;
            messages.appendChild(messageDiv);
            messages.scrollTop = messages.scrollHeight;
        }

        document.getElementById('messageInput').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') sendMessage();
        });
    </script>
</body>
</html>";
        }
    }
}
