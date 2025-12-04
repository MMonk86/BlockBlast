using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using BrickBreaker.Core;

namespace BrickBreaker.App
{
    public partial class MainForm : Form
    {
        private Game _gameLeft;
        private Game _gameRight;
        private Timer _timer;
        private Stopwatch _gameTimer;
        private Stopwatch _frameTimer;
        private bool _leftKeyPressed;
        private bool _rightKeyPressed;

        public MainForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.Text = "Brick Breaker - Dual Mode";
            this.ClientSize = new Size(1600, 600); // Widened for two games
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Create Menu
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem modeMenu = new ToolStripMenuItem("Mode");
            ToolStripMenuItem aiModeItem = new ToolStripMenuItem("AI vs AI (Default)", null, (s, e) => StartGame(true));
            ToolStripMenuItem playerModeItem = new ToolStripMenuItem("Player (Left) vs AI (Right)", null, (s, e) => StartGame(false));

            modeMenu.DropDownItems.Add(aiModeItem);
            modeMenu.DropDownItems.Add(playerModeItem);
            menuStrip.Items.Add(modeMenu);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // Split width in half for each game
            _gameLeft = new Game(800, 550); // Height reduced for status bar space
            _gameRight = new Game(800, 550);

            StartGame(true); // Default to AI vs AI

            _frameTimer = new Stopwatch();
            _frameTimer.Start();

            _timer = new Timer();
            _timer.Interval = 16; // ~60 FPS
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void StartGame(bool aiVsAi)
        {
            _gameLeft.Initialize(200);
            _gameRight.Initialize(600);

            _gameLeft.IsAIControlled = aiVsAi;
            _gameRight.IsAIControlled = true; // Right is always AI

            if (_gameTimer == null) _gameTimer = new Stopwatch();
            _gameTimer.Restart();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            double dt = _frameTimer.Elapsed.TotalSeconds;
            _frameTimer.Restart();

            // Cap dt to avoid huge jumps
            if (dt > 0.1) dt = 0.1;

            // Handle Manual Input for Left Game
            if (!_gameLeft.IsAIControlled)
            {
                double speed = 40.0; // Same as AI speed
                if (_leftKeyPressed) _gameLeft.MovePaddle(-speed);
                if (_rightKeyPressed) _gameLeft.MovePaddle(speed);
            }

            _gameLeft.Update(dt);
            _gameRight.Update(dt);
            this.Invalidate(); // Redraw
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw Left Game
            DrawGame(g, _gameLeft, 0, 0);

            // Draw Right Game
            DrawGame(g, _gameRight, 800, 0);

            // Draw Separator
            g.DrawLine(Pens.White, 800, 0, 800, 550);

            // Draw Status Bar
            DrawStatusBar(g);
        }

        private void DrawGame(Graphics g, Game game, float offsetX, float offsetY)
        {
            // Clip or translate
            var state = g.Save();
            g.TranslateTransform(offsetX, offsetY);

            // Background for game area
            g.FillRectangle(Brushes.Black, 0, 0, (float)game.Width, (float)game.Height);

            if (game.IsGameOver)
            {
                string msg = "Game Over!";
                Font font = new Font("Arial", 24);
                SizeF size = g.MeasureString(msg, font);
                g.DrawString(msg, font, Brushes.Red, ((float)game.Width - size.Width) / 2, ((float)game.Height - size.Height) / 2);
            }
            else if (game.IsWon)
            {
                string msg = "You Won!";
                Font font = new Font("Arial", 24);
                SizeF size = g.MeasureString(msg, font);
                g.DrawString(msg, font, Brushes.Green, ((float)game.Width - size.Width) / 2, ((float)game.Height - size.Height) / 2);
            }
            else
            {
                // Draw Paddle
                g.FillRectangle(Brushes.Blue, (float)game.Paddle.X, (float)game.Paddle.Y, (float)game.Paddle.Width, (float)game.Paddle.Height);

                // Draw Ball
                g.FillEllipse(Brushes.White, (float)(game.Ball.X - game.Ball.Radius), (float)(game.Ball.Y - game.Ball.Radius), (float)(game.Ball.Radius * 2), (float)(game.Ball.Radius * 2));

                // Draw Falling Items
                foreach (var item in game.FallingItems)
                {
                    if (item.Type == ItemType.Star)
                    {
                        // Yellow Star (approximated as diamond/circle for simplicity or simple polygon)
                        PointF[] starPoints = new PointF[]
                        {
                            new PointF((float)item.X, (float)item.Y - 10),
                            new PointF((float)item.X + 8, (float)item.Y + 8),
                            new PointF((float)item.X - 8, (float)item.Y + 8)
                        };
                         g.FillEllipse(Brushes.Yellow, (float)item.X - 8, (float)item.Y - 8, 16, 16);
                         // g.FillPolygon(Brushes.Yellow, starPoints); // Simple triangle/star
                    }
                    else if (item.Type == ItemType.Triangle)
                    {
                        // Green Triangle
                        PointF[] triPoints = new PointF[]
                        {
                            new PointF((float)item.X, (float)item.Y - 10),
                            new PointF((float)item.X + 10, (float)item.Y + 10),
                            new PointF((float)item.X - 10, (float)item.Y + 10)
                        };
                        g.FillPolygon(Brushes.LightGreen, triPoints);
                    }
                }

                // Draw Blocks
                Font numberFont = new Font("Arial", 10, FontStyle.Bold);
                foreach (var block in game.Blocks)
                {
                    Brush brush;
                    switch (block.Color)
                    {
                        case "Red": brush = Brushes.Red; break;
                        case "Orange": brush = Brushes.Orange; break;
                        case "Yellow": brush = Brushes.Yellow; break;
                        case "Green": brush = Brushes.Green; break;
                        case "Blue": brush = Brushes.Blue; break;
                        default: brush = Brushes.Gray; break;
                    }
                    g.FillRectangle(brush, (float)block.X, (float)block.Y, (float)block.Width, (float)block.Height);

                    // Draw Health Number
                    string healthText = block.Health.ToString();
                    SizeF textSize = g.MeasureString(healthText, numberFont);
                    g.DrawString(healthText, numberFont, Brushes.Black,
                        (float)(block.X + (block.Width - textSize.Width) / 2),
                        (float)(block.Y + (block.Height - textSize.Height) / 2));
                }
            }

            g.Restore(state);
        }

        private void DrawStatusBar(Graphics g)
        {
            float barY = 550;
            float barHeight = 50;
            g.FillRectangle(Brushes.DarkGray, 0, barY, 1600, barHeight);

            Font font = new Font("Arial", 16);
            Brush brush = Brushes.White;

            // Timer
            string timeStr = $"Time: {_gameTimer.Elapsed:mm\\:ss}";
            g.DrawString(timeStr, font, brush, 750, barY + 10);

            // Scores
            g.DrawString($"Left Score: {_gameLeft.Score}", font, brush, 50, barY + 10);
            g.DrawString($"Right Score: {_gameRight.Score}", font, brush, 1350, barY + 10);
        }

        // Handle Restart
        protected override void OnKeyDown(KeyEventArgs e)
        {
             base.OnKeyDown(e);

             if (e.KeyCode == Keys.Left) _leftKeyPressed = true;
             if (e.KeyCode == Keys.Right) _rightKeyPressed = true;

             // Restart only if both games are finished
             bool leftDone = _gameLeft.IsGameOver || _gameLeft.IsWon;
             bool rightDone = _gameRight.IsGameOver || _gameRight.IsWon;

             if (e.KeyCode == Keys.R && leftDone && rightDone)
             {
                 // Maintain current AI mode setting on restart
                 bool currentAI = _gameLeft.IsAIControlled;
                 _gameLeft.Initialize(200);
                 _gameRight.Initialize(600);
                 _gameLeft.IsAIControlled = currentAI;
                 _gameRight.IsAIControlled = true;
                 _gameTimer.Restart();
             }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Left) _leftKeyPressed = false;
            if (e.KeyCode == Keys.Right) _rightKeyPressed = false;
        }
    }
}
