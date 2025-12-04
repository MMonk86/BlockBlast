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

        public MainForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.Text = "Brick Breaker - Dual Mode";
            this.ClientSize = new Size(1600, 600); // Widened for two games
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Split width in half for each game
            _gameLeft = new Game(800, 550); // Height reduced for status bar space
            _gameRight = new Game(800, 550);

            // Set different start positions
            _gameLeft.Initialize(200); // 1/4 width
            _gameRight.Initialize(600); // 3/4 width

            _gameTimer = new Stopwatch();
            _gameTimer.Start();

            _timer = new Timer();
            _timer.Interval = 16; // ~60 FPS
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _gameLeft.Update();
            _gameRight.Update();
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
             // Restart only if both games are finished
             bool leftDone = _gameLeft.IsGameOver || _gameLeft.IsWon;
             bool rightDone = _gameRight.IsGameOver || _gameRight.IsWon;

             if (e.KeyCode == Keys.R && leftDone && rightDone)
             {
                 _gameLeft.Initialize(200);
                 _gameRight.Initialize(600);
                 _gameTimer.Restart();
             }
        }
    }
}
