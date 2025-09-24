using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Provides code templates for different application types.
/// </summary>
public class CodeTemplateService
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
            if (isDarkMode) document.body.classList.add('dark-mode');
            renderTodos();
        }

        function toggleTheme() {
            isDarkMode = !isDarkMode;
            document.body.classList.toggle('dark-mode', isDarkMode);
            localStorage.setItem('darkMode', isDarkMode);
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
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f8f9fa; }
        .header { background: #343a40; color: white; padding: 1rem; }
        .nav { display: flex; justify-content: space-between; align-items: center; }
        .logo { font-size: 1.5rem; font-weight: bold; }
        .cart-btn { background: #007bff; color: white; border: none; padding: 0.5rem 1rem; border-radius: 4px; cursor: pointer; }
        .container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
        .products { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 2rem; }
        .product { background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .product img { width: 100%; height: 200px; object-fit: cover; }
        .product-info { padding: 1rem; }
        .product-title { font-size: 1.2rem; margin-bottom: 0.5rem; }
        .product-price { font-size: 1.5rem; color: #28a745; font-weight: bold; }
        .add-to-cart { width: 100%; background: #28a745; color: white; border: none; padding: 0.75rem; border-radius: 4px; cursor: pointer; margin-top: 1rem; }
        .cart-modal { display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1000; }
        .cart-content { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); background: white; padding: 2rem; border-radius: 8px; max-width: 500px; width: 90%; }
        .close { float: right; font-size: 1.5rem; cursor: pointer; }
    </style>
</head>
<body>
    <header class=""header"">
        <nav class=""nav"">
            <div class=""logo"">ShopEasy</div>
            <button class=""cart-btn"" onclick=""openCart()"">Cart (<span id=""cartCount"">0</span>)</button>
        </nav>
    </header>

    <div class=""container"">
        <h1>Our Products</h1>
        <div class=""products"" id=""products""></div>
    </div>

    <div class=""cart-modal"" id=""cartModal"">
        <div class=""cart-content"">
            <span class=""close"" onclick=""closeCart()"">&times;</span>
            <h2>Shopping Cart</h2>
            <div id=""cartItems""></div>
            <div id=""cartTotal""></div>
            <button onclick=""checkout()"" style=""width: 100%; padding: 1rem; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; margin-top: 1rem;"">Checkout</button>
        </div>
    </div>

    <script>
        const products = [
            { id: 1, name: 'Wireless Headphones', price: 99.99, image: 'https://via.placeholder.com/300x200?text=Headphones' },
            { id: 2, name: 'Smart Watch', price: 199.99, image: 'https://via.placeholder.com/300x200?text=Smart+Watch' },
            { id: 3, name: 'Laptop', price: 999.99, image: 'https://via.placeholder.com/300x200?text=Laptop' },
            { id: 4, name: 'Phone', price: 699.99, image: 'https://via.placeholder.com/300x200?text=Phone' }
        ];

        let cart = JSON.parse(localStorage.getItem('cart')) || [];

        function renderProducts() {
            const container = document.getElementById('products');
            container.innerHTML = products.map(product => `
                <div class=""product"">
                    <img src=""${product.image}"" alt=""${product.name}"">
                    <div class=""product-info"">
                        <div class=""product-title"">${product.name}</div>
                        <div class=""product-price"">$${product.price}</div>
                        <button class=""add-to-cart"" onclick=""addToCart(${product.id})"">Add to Cart</button>
                    </div>
                </div>
            `).join('');
        }

        function addToCart(productId) {
            const product = products.find(p => p.id === productId);
            const existingItem = cart.find(item => item.id === productId);
            
            if (existingItem) {
                existingItem.quantity += 1;
            } else {
                cart.push({ ...product, quantity: 1 });
            }
            
            saveCart();
            updateCartCount();
        }

        function openCart() {
            document.getElementById('cartModal').style.display = 'block';
            renderCart();
        }

        function closeCart() {
            document.getElementById('cartModal').style.display = 'none';
        }

        function renderCart() {
            const cartItems = document.getElementById('cartItems');
            const cartTotal = document.getElementById('cartTotal');
            
            if (cart.length === 0) {
                cartItems.innerHTML = '<p>Your cart is empty</p>';
                cartTotal.innerHTML = '';
                return;
            }

            cartItems.innerHTML = cart.map(item => `
                <div style=""display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid #eee;"">
                    <span>${item.name} x${item.quantity}</span>
                    <span>$${(item.price * item.quantity).toFixed(2)}</span>
                </div>
            `).join('');

            const total = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
            cartTotal.innerHTML = `<h3>Total: $${total.toFixed(2)}</h3>`;
        }

        function updateCartCount() {
            const count = cart.reduce((sum, item) => sum + item.quantity, 0);
            document.getElementById('cartCount').textContent = count;
        }

        function saveCart() {
            localStorage.setItem('cart', JSON.stringify(cart));
        }

        function checkout() {
            alert('Thank you for your purchase! (This is a demo)');
            cart = [];
            saveCart();
            updateCartCount();
            closeCart();
        }

        renderProducts();
        updateCartCount();
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
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f0f2f5; height: 100vh; display: flex; }
        .sidebar { width: 250px; background: #2c3e50; color: white; padding: 1rem; }
        .chat-area { flex: 1; display: flex; flex-direction: column; }
        .chat-header { background: white; padding: 1rem; border-bottom: 1px solid #ddd; }
        .chat-messages { flex: 1; padding: 1rem; overflow-y: auto; }
        .message { margin-bottom: 1rem; }
        .message.own { text-align: right; }
        .message-bubble { display: inline-block; max-width: 70%; padding: 0.75rem 1rem; border-radius: 18px; }
        .message.own .message-bubble { background: #007bff; color: white; }
        .message:not(.own) .message-bubble { background: #e9ecef; color: #333; }
        .message-input { display: flex; padding: 1rem; background: white; border-top: 1px solid #ddd; }
        .message-input input { flex: 1; padding: 0.75rem; border: 1px solid #ddd; border-radius: 20px; margin-right: 0.5rem; }
        .message-input button { padding: 0.75rem 1.5rem; background: #007bff; color: white; border: none; border-radius: 20px; cursor: pointer; }
        .user-list { margin-top: 1rem; }
        .user-item { padding: 0.5rem; border-radius: 4px; margin-bottom: 0.5rem; background: rgba(255,255,255,0.1); }
        .user-item.online { background: rgba(40, 167, 69, 0.3); }
    </style>
</head>
<body>
    <div class=""sidebar"">
        <h3>Chat Rooms</h3>
        <div class=""user-list"">
            <div class=""user-item online"">General</div>
            <div class=""user-item"">Random</div>
            <div class=""user-item"">Tech Talk</div>
        </div>
        <h3>Online Users</h3>
        <div class=""user-list"">
            <div class=""user-item online"">You</div>
            <div class=""user-item online"">Alice</div>
            <div class=""user-item"">Bob</div>
        </div>
    </div>

    <div class=""chat-area"">
        <div class=""chat-header"">
            <h2>General Chat</h2>
            <p>Welcome to the chat room!</p>
        </div>
        <div class=""chat-messages"" id=""messages"">
            <div class=""message"">
                <div class=""message-bubble"">Welcome to the chat! 👋</div>
            </div>
            <div class=""message own"">
                <div class=""message-bubble"">Hello everyone!</div>
            </div>
        </div>
        <div class=""message-input"">
            <input type=""text"" id=""messageInput"" placeholder=""Type a message..."">
            <button onclick=""sendMessage()"">Send</button>
        </div>
    </div>

    <script>
        const messages = document.getElementById('messages');
        const messageInput = document.getElementById('messageInput');

        function sendMessage() {
            const text = messageInput.value.trim();
            if (text) {
                addMessage(text, true);
                messageInput.value = '';
                
                // Simulate other users responding
                setTimeout(() => {
                    const responses = ['That sounds interesting!', 'I agree!', 'Great point!', 'Thanks for sharing!'];
                    const randomResponse = responses[Math.floor(Math.random() * responses.length)];
                    addMessage(randomResponse, false);
                }, 1000 + Math.random() * 2000);
            }
        }

        function addMessage(text, isOwn) {
            const messageDiv = document.createElement('div');
            messageDiv.className = `message ${isOwn ? 'own' : ''}`;
            messageDiv.innerHTML = `<div class=""message-bubble"">${text}</div>`;
            messages.appendChild(messageDiv);
            messages.scrollTop = messages.scrollHeight;
        }

        messageInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') sendMessage();
        });
    </script>
</body>
</html>";
    }

    public static string GenerateMobileFitnessCode()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Fitness Tracker</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f8f9fa; }
        .container { max-width: 400px; margin: 0 auto; background: white; min-height: 100vh; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 2rem 1rem; text-align: center; }
        .stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; padding: 1rem; }
        .stat-card { background: white; padding: 1rem; border-radius: 8px; text-align: center; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .stat-number { font-size: 1.5rem; font-weight: bold; color: #007bff; }
        .stat-label { font-size: 0.8rem; color: #666; margin-top: 0.25rem; }
        .workout-section { padding: 1rem; }
        .workout-item { background: #f8f9fa; padding: 1rem; border-radius: 8px; margin-bottom: 0.5rem; display: flex; justify-content: space-between; align-items: center; }
        .workout-name { font-weight: bold; }
        .workout-duration { color: #666; font-size: 0.9rem; }
        .add-workout { background: #28a745; color: white; border: none; padding: 0.75rem 1rem; border-radius: 8px; cursor: pointer; width: 100%; margin-top: 1rem; }
        .progress-chart { height: 200px; background: #f8f9fa; border-radius: 8px; margin: 1rem; display: flex; align-items: center; justify-content: center; color: #666; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Fitness Tracker</h1>
            <p>Track your fitness journey</p>
        </div>
        
        <div class=""stats"">
            <div class=""stat-card"">
                <div class=""stat-number"">1,250</div>
                <div class=""stat-label"">Steps Today</div>
            </div>
            <div class=""stat-card"">
                <div class=""stat-number"">45</div>
                <div class=""stat-label"">Minutes</div>
            </div>
            <div class=""stat-card"">
                <div class=""stat-number"">320</div>
                <div class=""stat-label"">Calories</div>
            </div>
        </div>

        <div class=""workout-section"">
            <h3>Today's Workouts</h3>
            <div id=""workouts"">
                <div class=""workout-item"">
                    <div>
                        <div class=""workout-name"">Morning Run</div>
                        <div class=""workout-duration"">30 minutes</div>
                    </div>
                    <span>✅</span>
                </div>
                <div class=""workout-item"">
                    <div>
                        <div class=""workout-name"">Strength Training</div>
                        <div class=""workout-duration"">45 minutes</div>
                    </div>
                    <span>⏳</span>
                </div>
            </div>
            <button class=""add-workout"" onclick=""addWorkout()"">Add Workout</button>
        </div>

        <div class=""progress-chart"">
            📊 Progress Chart (Demo)
        </div>
    </div>

    <script>
        function addWorkout() {
            const workouts = document.getElementById('workouts');
            const workoutNames = ['Yoga', 'Swimming', 'Cycling', 'Pilates', 'Boxing'];
            const randomName = workoutNames[Math.floor(Math.random() * workoutNames.length)];
            const randomDuration = Math.floor(Math.random() * 60) + 15;
            
            const workoutDiv = document.createElement('div');
            workoutDiv.className = 'workout-item';
            workoutDiv.innerHTML = `
                <div>
                    <div class=""workout-name"">${randomName}</div>
                    <div class=""workout-duration"">${randomDuration} minutes</div>
                </div>
                <span>⏳</span>
            `;
            workouts.appendChild(workoutDiv);
        }
    </script>
</body>
</html>";
    }

    public static string GenerateApiServerCode()
    {
        return @"const express = require('express');
const cors = require('cors');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcrypt');
const mongoose = require('mongoose');

const app = express();
const PORT = process.env.PORT || 3000;
const JWT_SECRET = 'your-secret-key';

// Middleware
app.use(cors());
app.use(express.json());

// Connect to MongoDB
mongoose.connect('mongodb://localhost:27017/demo-api', {
    useNewUrlParser: true,
    useUnifiedTopology: true
});

// User Schema
const userSchema = new mongoose.Schema({
    username: { type: String, required: true, unique: true },
    email: { type: String, required: true, unique: true },
    password: { type: String, required: true },
    createdAt: { type: Date, default: Date.now }
});

const User = mongoose.model('User', userSchema);

// Product Schema
const productSchema = new mongoose.Schema({
    name: { type: String, required: true },
    description: String,
    price: { type: Number, required: true },
    category: String,
    inStock: { type: Boolean, default: true },
    createdAt: { type: Date, default: Date.now }
});

const Product = mongoose.model('Product', productSchema);

// Auth Middleware
const authenticateToken = (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1];

    if (!token) {
        return res.status(401).json({ error: 'Access token required' });
    }

    jwt.verify(token, JWT_SECRET, (err, user) => {
        if (err) {
            return res.status(403).json({ error: 'Invalid token' });
        }
        req.user = user;
        next();
    });
};

// Routes

// Auth Routes
app.post('/api/auth/register', async (req, res) => {
    try {
        const { username, email, password } = req.body;
        
        // Check if user exists
        const existingUser = await User.findOne({ $or: [{ email }, { username }] });
        if (existingUser) {
            return res.status(400).json({ error: 'User already exists' });
        }

        // Hash password
        const hashedPassword = await bcrypt.hash(password, 10);
        
        // Create user
        const user = new User({ username, email, password: hashedPassword });
        await user.save();

        // Generate token
        const token = jwt.sign({ userId: user._id }, JWT_SECRET, { expiresIn: '24h' });

        res.status(201).json({ 
            message: 'User created successfully',
            token,
            user: { id: user._id, username: user.username, email: user.email }
        });
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

app.post('/api/auth/login', async (req, res) => {
    try {
        const { email, password } = req.body;
        
        // Find user
        const user = await User.findOne({ email });
        if (!user) {
            return res.status(401).json({ error: 'Invalid credentials' });
        }

        // Check password
        const isValidPassword = await bcrypt.compare(password, user.password);
        if (!isValidPassword) {
            return res.status(401).json({ error: 'Invalid credentials' });
        }

        // Generate token
        const token = jwt.sign({ userId: user._id }, JWT_SECRET, { expiresIn: '24h' });

        res.json({ 
            message: 'Login successful',
            token,
            user: { id: user._id, username: user.username, email: user.email }
        });
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

// Product Routes
app.get('/api/products', async (req, res) => {
    try {
        const { page = 1, limit = 10, category, search } = req.query;
        const query = {};
        
        if (category) query.category = category;
        if (search) query.name = { $regex: search, $options: 'i' };

        const products = await Product.find(query)
            .limit(limit * 1)
            .skip((page - 1) * limit)
            .sort({ createdAt: -1 });

        const total = await Product.countDocuments(query);

        res.json({
            products,
            totalPages: Math.ceil(total / limit),
            currentPage: page,
            total
        });
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

app.get('/api/products/:id', async (req, res) => {
    try {
        const product = await Product.findById(req.params.id);
        if (!product) {
            return res.status(404).json({ error: 'Product not found' });
        }
        res.json(product);
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

app.post('/api/products', authenticateToken, async (req, res) => {
    try {
        const product = new Product(req.body);
        await product.save();
        res.status(201).json(product);
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

app.put('/api/products/:id', authenticateToken, async (req, res) => {
    try {
        const product = await Product.findByIdAndUpdate(req.params.id, req.body, { new: true });
        if (!product) {
            return res.status(404).json({ error: 'Product not found' });
        }
        res.json(product);
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

app.delete('/api/products/:id', authenticateToken, async (req, res) => {
    try {
        const product = await Product.findByIdAndDelete(req.params.id);
        if (!product) {
            return res.status(404).json({ error: 'Product not found' });
        }
        res.json({ message: 'Product deleted successfully' });
    } catch (error) {
        res.status(500).json({ error: 'Server error' });
    }
});

// Health check
app.get('/api/health', (req, res) => {
    res.json({ status: 'OK', timestamp: new Date().toISOString() });
});

// Error handling
app.use((err, req, res, next) => {
    console.error(err.stack);
    res.status(500).json({ error: 'Something went wrong!' });
});

// 404 handler
app.use('*', (req, res) => {
    res.status(404).json({ error: 'Route not found' });
});

app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
    console.log(`Health check: http://localhost:${PORT}/api/health`);
});

module.exports = app;";
    }

    public static string GenerateGameCode()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>2D Platformer Game</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { background: #87CEEB; overflow: hidden; }
        #gameCanvas { display: block; background: linear-gradient(to bottom, #87CEEB 0%, #98FB98 100%); }
        .game-ui { position: absolute; top: 20px; left: 20px; color: white; font-family: Arial, sans-serif; font-size: 18px; text-shadow: 2px 2px 4px rgba(0,0,0,0.5); }
        .game-over { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); background: rgba(0,0,0,0.8); color: white; padding: 2rem; border-radius: 10px; text-align: center; display: none; }
        .restart-btn { background: #4CAF50; color: white; border: none; padding: 1rem 2rem; border-radius: 5px; cursor: pointer; font-size: 16px; margin-top: 1rem; }
    </style>
</head>
<body>
    <div class=""game-ui"">
        <div>Score: <span id=""score"">0</span></div>
        <div>Lives: <span id=""lives"">3</span></div>
    </div>
    
    <canvas id=""gameCanvas"" width=""800"" height=""600""></canvas>
    
    <div class=""game-over"" id=""gameOver"">
        <h2>Game Over!</h2>
        <p>Final Score: <span id=""finalScore"">0</span></p>
        <button class=""restart-btn"" onclick=""restartGame()"">Play Again</button>
    </div>

    <script>
        const canvas = document.getElementById('gameCanvas');
        const ctx = canvas.getContext('2d');
        
        // Game state
        let gameRunning = true;
        let score = 0;
        let lives = 3;
        
        // Player
        const player = {
            x: 50,
            y: 400,
            width: 30,
            height: 30,
            velocityX: 0,
            velocityY: 0,
            onGround: false,
            color: '#FF6B6B'
        };
        
        // Platforms
        const platforms = [
            { x: 0, y: 550, width: 200, height: 50, color: '#8B4513' },
            { x: 200, y: 500, width: 150, height: 50, color: '#8B4513' },
            { x: 400, y: 450, width: 150, height: 50, color: '#8B4513' },
            { x: 600, y: 400, width: 200, height: 50, color: '#8B4513' },
            { x: 0, y: 300, width: 100, height: 50, color: '#8B4513' },
            { x: 200, y: 250, width: 100, height: 50, color: '#8B4513' },
            { x: 400, y: 200, width: 100, height: 50, color: '#8B4513' },
            { x: 600, y: 150, width: 200, height: 50, color: '#8B4513' }
        ];
        
        // Collectibles
        const collectibles = [
            { x: 250, y: 450, width: 20, height: 20, color: '#FFD700', collected: false },
            { x: 450, y: 400, width: 20, height: 20, color: '#FFD700', collected: false },
            { x: 650, y: 350, width: 20, height: 20, color: '#FFD700', collected: false },
            { x: 50, y: 250, width: 20, height: 20, color: '#FFD700', collected: false },
            { x: 250, y: 200, width: 20, height: 20, color: '#FFD700', collected: false },
            { x: 450, y: 150, width: 20, height: 20, color: '#FFD700', collected: false }
        ];
        
        // Enemies
        const enemies = [
            { x: 300, y: 450, width: 25, height: 25, velocityX: 1, color: '#8B0000' },
            { x: 500, y: 350, width: 25, height: 25, velocityX: -1, color: '#8B0000' },
            { x: 150, y: 200, width: 25, height: 25, velocityX: 1, color: '#8B0000' }
        ];
        
        // Input handling
        const keys = {};
        
        document.addEventListener('keydown', (e) => {
            keys[e.code] = true;
        });
        
        document.addEventListener('keyup', (e) => {
            keys[e.code] = false;
        });
        
        // Game loop
        function gameLoop() {
            if (!gameRunning) return;
            
            update();
            render();
            requestAnimationFrame(gameLoop);
        }
        
        function update() {
            // Player movement
            if (keys['ArrowLeft'] || keys['KeyA']) {
                player.velocityX = -5;
            } else if (keys['ArrowRight'] || keys['KeyD']) {
                player.velocityX = 5;
            } else {
                player.velocityX *= 0.8; // Friction
            }
            
            if ((keys['ArrowUp'] || keys['KeyW'] || keys['Space']) && player.onGround) {
                player.velocityY = -15;
                player.onGround = false;
            }
            
            // Apply gravity
            player.velocityY += 0.8;
            
            // Update player position
            player.x += player.velocityX;
            player.y += player.velocityY;
            
            // Platform collision
            player.onGround = false;
            platforms.forEach(platform => {
                if (player.x < platform.x + platform.width &&
                    player.x + player.width > platform.x &&
                    player.y < platform.y + platform.height &&
                    player.y + player.height > platform.y) {
                    
                    if (player.velocityY > 0 && player.y < platform.y) {
                        player.y = platform.y - player.height;
                        player.velocityY = 0;
                        player.onGround = true;
                    }
                }
            });
            
            // Collectible collision
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
            
            // Enemy collision
            enemies.forEach(enemy => {
                if (player.x < enemy.x + enemy.width &&
                    player.x + player.width > enemy.x &&
                    player.y < enemy.y + enemy.height &&
                    player.y + player.height > enemy.y) {
                    
                    lives--;
                    player.x = 50;
                    player.y = 400;
                    player.velocityX = 0;
                    player.velocityY = 0;
                    updateUI();
                    
                    if (lives <= 0) {
                        gameOver();
                    }
                }
            });
            
            // Update enemies
            enemies.forEach(enemy => {
                enemy.x += enemy.velocityX;
                
                // Check platform boundaries
                let onPlatform = false;
                platforms.forEach(platform => {
                    if (enemy.x < platform.x + platform.width &&
                        enemy.x + enemy.width > platform.x &&
                        enemy.y + enemy.height >= platform.y &&
                        enemy.y + enemy.height <= platform.y + 10) {
                        onPlatform = true;
                    }
                });
                
                if (!onPlatform || enemy.x <= 0 || enemy.x + enemy.width >= canvas.width) {
                    enemy.velocityX *= -1;
                }
            });
            
            // Keep player in bounds
            if (player.x < 0) player.x = 0;
            if (player.x + player.width > canvas.width) player.x = canvas.width - player.width;
            
            // Check if player fell
            if (player.y > canvas.height) {
                lives--;
                player.x = 50;
                player.y = 400;
                player.velocityX = 0;
                player.velocityY = 0;
                updateUI();
                
                if (lives <= 0) {
                    gameOver();
                }
            }
        }
        
        function render() {
            // Clear canvas
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            // Draw platforms
            platforms.forEach(platform => {
                ctx.fillStyle = platform.color;
                ctx.fillRect(platform.x, platform.y, platform.width, platform.height);
            });
            
            // Draw collectibles
            collectibles.forEach(collectible => {
                if (!collectible.collected) {
                    ctx.fillStyle = collectible.color;
                    ctx.fillRect(collectible.x, collectible.y, collectible.width, collectible.height);
                }
            });
            
            // Draw enemies
            enemies.forEach(enemy => {
                ctx.fillStyle = enemy.color;
                ctx.fillRect(enemy.x, enemy.y, enemy.width, enemy.height);
            });
            
            // Draw player
            ctx.fillStyle = player.color;
            ctx.fillRect(player.x, player.y, player.width, player.height);
        }
        
        function updateUI() {
            document.getElementById('score').textContent = score;
            document.getElementById('lives').textContent = lives;
        }
        
        function gameOver() {
            gameRunning = false;
            document.getElementById('finalScore').textContent = score;
            document.getElementById('gameOver').style.display = 'block';
        }
        
        function restartGame() {
            gameRunning = true;
            score = 0;
            lives = 3;
            player.x = 50;
            player.y = 400;
            player.velocityX = 0;
            player.velocityY = 0;
            
            collectibles.forEach(collectible => {
                collectible.collected = false;
            });
            
            document.getElementById('gameOver').style.display = 'none';
            updateUI();
            gameLoop();
        }
        
        // Start game
        updateUI();
        gameLoop();
    </script>
</body>
</html>";
    }
}
