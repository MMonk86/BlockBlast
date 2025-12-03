using System;
using System.Collections.Generic;

namespace BrickBreaker.Core
{
    public class Game
    {
        public double Width { get; private set; }
        public double Height { get; private set; }
        public Ball Ball { get; private set; }
        public Paddle Paddle { get; private set; }
        public List<Block> Blocks { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsWon { get; private set; }

        public event Action<Block> OnBlockBroken;

        public Game(double width, double height)
        {
            Width = width;
            Height = height;
            Initialize();
        }

        public void Initialize()
        {
            IsGameOver = false;
            IsWon = false;

            // Paddle at bottom center
            Paddle = new Paddle(Width / 2 - 50, Height - 30, 100, 20);

            // Ball just above paddle
            Ball = new Ball(Width / 2, Height - 40, 5, 3, -3);

            // Blocks
            Blocks = new List<Block>();
            int rows = 5;
            int cols = 10;
            double blockWidth = (Width - 20) / cols;
            double blockHeight = 20;

            string[] colors = { "Red", "Orange", "Yellow", "Green", "Blue" };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Blocks.Add(new Block(
                        10 + c * blockWidth,
                        10 + r * blockHeight,
                        blockWidth - 2,
                        blockHeight - 2,
                        colors[r % colors.Length]
                    ));
                }
            }
        }

        public void Update()
        {
            if (IsGameOver || IsWon) return;

            // Move Ball
            Ball.X += Ball.VelocityX;
            Ball.Y += Ball.VelocityY;

            // Wall Collisions
            if (Ball.X - Ball.Radius < 0)
            {
                Ball.X = Ball.Radius;
                Ball.VelocityX = -Ball.VelocityX;
            }
            else if (Ball.X + Ball.Radius > Width)
            {
                Ball.X = Width - Ball.Radius;
                Ball.VelocityX = -Ball.VelocityX;
            }

            if (Ball.Y - Ball.Radius < 0)
            {
                Ball.Y = Ball.Radius;
                Ball.VelocityY = -Ball.VelocityY;
            }
            // Ball radius is typically small, but we want to make sure the ball is fully off screen or just passed the paddle.
            // Paddle is at Height - 30.
            // If Ball.Y > Height, it is definitely gone.
            else if (Ball.Y - Ball.Radius > Height)
            {
                // Missed paddle
                IsGameOver = true;
                return;
            }

            // Paddle Collision
            if (CheckCollision(Ball, Paddle))
            {
                Ball.VelocityY = -Math.Abs(Ball.VelocityY); // Bounce up

                // Adjust angle based on where it hit the paddle
                double hitPoint = Ball.X - (Paddle.X + Paddle.Width / 2);
                Ball.VelocityX = hitPoint * 0.15; // Simple angle change
            }

            // Block Collision
            for (int i = Blocks.Count - 1; i >= 0; i--)
            {
                if (CheckCollision(Ball, Blocks[i]))
                {
                    ResolveBlockCollision(Ball, Blocks[i]);

                    OnBlockBroken?.Invoke(Blocks[i]);
                    Blocks.RemoveAt(i);

                    if (Blocks.Count == 0)
                    {
                        IsWon = true;
                    }
                    break; // Handle one block collision per frame prevents tunneling usually
                }
            }
        }

        public void MovePaddle(double dx)
        {
            Paddle.X += dx;
            if (Paddle.X < 0) Paddle.X = 0;
            if (Paddle.X + Paddle.Width > Width) Paddle.X = Width - Paddle.Width;
        }

        private bool CheckCollision(Ball ball, IRect rect)
        {
            // Simple Circle-Rectangle collision
            // Find the closest point on the rectangle to the center of the circle
            double closestX = Math.Max(rect.X, Math.Min(ball.X, rect.X + rect.Width));
            double closestY = Math.Max(rect.Y, Math.Min(ball.Y, rect.Y + rect.Height));

            double distanceX = ball.X - closestX;
            double distanceY = ball.Y - closestY;

            double distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
            return distanceSquared < (ball.Radius * ball.Radius);
        }

        private void ResolveBlockCollision(Ball ball, Block block)
        {
            // Determine intersection details to find which side was hit
            double ballCenterX = ball.X;
            double ballCenterY = ball.Y;

            double blockCenterX = block.X + block.Width / 2;
            double blockCenterY = block.Y + block.Height / 2;

            double dx = ballCenterX - blockCenterX;
            double dy = ballCenterY - blockCenterY;

            double combinedHalfWidth = (block.Width / 2) + ball.Radius;
            double combinedHalfHeight = (block.Height / 2) + ball.Radius;

            // Check overlap
            double overlapX = combinedHalfWidth - Math.Abs(dx);
            double overlapY = combinedHalfHeight - Math.Abs(dy);

            // If overlaps are valid
            if (overlapX > 0 && overlapY > 0)
            {
                // Collision on the side with minimal overlap
                if (overlapX < overlapY)
                {
                    // Hit left or right
                    ball.VelocityX = -ball.VelocityX;
                    // Correct position to avoid sticking
                    if (dx > 0) ball.X += overlapX; else ball.X -= overlapX;
                }
                else
                {
                    // Hit top or bottom
                    ball.VelocityY = -ball.VelocityY;
                     // Correct position
                    if (dy > 0) ball.Y += overlapY; else ball.Y -= overlapY;
                }
            }
        }
    }

    public interface IRect
    {
        double X { get; }
        double Y { get; }
        double Width { get; }
        double Height { get; }
    }

    public class Ball
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }

        public Ball(double x, double y, double radius, double vx, double vy)
        {
            X = x;
            Y = y;
            Radius = radius;
            VelocityX = vx;
            VelocityY = vy;
        }
    }

    public class Paddle : IRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Paddle(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public class Block : IRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Color { get; set; }

        public Block(double x, double y, double width, double height, string color)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Color = color;
        }
    }
}
