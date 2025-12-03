using System;
using System.Drawing;
using System.Windows.Forms;
using BrickBreaker.Core;

namespace BrickBreaker.App
{
    public partial class MainForm : Form
    {
        private Game _game;
        private Timer _timer;
        private bool _leftPressed;
        private bool _rightPressed;

        public MainForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.Text = "Brick Breaker";
            this.ClientSize = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _game = new Game(ClientSize.Width, ClientSize.Height);
            _game.OnBlockBroken += (b) => { /* Sound? */ };

            _timer = new Timer();
            _timer.Interval = 16; // ~60 FPS
            _timer.Tick += Timer_Tick;
            _timer.Start();

            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_leftPressed) _game.MovePaddle(-5);
            if (_rightPressed) _game.MovePaddle(5);

            _game.Update();
            this.Invalidate(); // Redraw
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) _leftPressed = true;
            if (e.KeyCode == Keys.Right) _rightPressed = true;
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) _leftPressed = false;
            if (e.KeyCode == Keys.Right) _rightPressed = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_game.IsGameOver)
            {
                string msg = "Game Over! Press R to Restart.";
                Font font = new Font("Arial", 24);
                SizeF size = g.MeasureString(msg, font);
                g.DrawString(msg, font, Brushes.Red, (ClientSize.Width - size.Width) / 2, (ClientSize.Height - size.Height) / 2);
                return;
            }

            if (_game.IsWon)
            {
                string msg = "You Won! Press R to Restart.";
                Font font = new Font("Arial", 24);
                SizeF size = g.MeasureString(msg, font);
                g.DrawString(msg, font, Brushes.Green, (ClientSize.Width - size.Width) / 2, (ClientSize.Height - size.Height) / 2);
                return;
            }

            // Draw Paddle
            g.FillRectangle(Brushes.Blue, (float)_game.Paddle.X, (float)_game.Paddle.Y, (float)_game.Paddle.Width, (float)_game.Paddle.Height);

            // Draw Ball
            g.FillEllipse(Brushes.White, (float)(_game.Ball.X - _game.Ball.Radius), (float)(_game.Ball.Y - _game.Ball.Radius), (float)(_game.Ball.Radius * 2), (float)(_game.Ball.Radius * 2));

            // Draw Blocks
            foreach (var block in _game.Blocks)
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
            }
        }

        // Handle Restart
        protected override void OnKeyDown(KeyEventArgs e)
        {
             base.OnKeyDown(e);
             if (e.KeyCode == Keys.R && (_game.IsGameOver || _game.IsWon))
             {
                 _game.Initialize();
             }
        }
    }
}
