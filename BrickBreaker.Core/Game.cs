using System;
using System.Collections.Generic;
using System.Linq;

namespace BrickBreaker.Core
{
    public enum ItemType
    {
        None,
        Star,       // Slows ball
        Triangle    // Widens paddle
    }

    public class Game
    {
        public double Width { get; private set; }
        public double Height { get; private set; }
        public Ball Ball { get; private set; }
        public Paddle Paddle { get; private set; }
        public List<Block> Blocks { get; private set; }
        public List<FallingItem> FallingItems { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsWon { get; private set; }
        public int Score { get; private set; }

        // Timers
        public double BlockDescentTimer { get; private set; }
        public double StarEffectTimer { get; private set; }
        public double TriangleEffectTimer { get; private set; }

        public event Action<Block> OnBlockBroken;

        private const double BaseBallSpeed = 15.0; // Reduced by 2x from 30
        private const double BasePaddleWidth = 100.0;
        private const double BasePaddleSpeed = 40.0;

        public Game(double width, double height)
        {
            Width = width;
            Height = height;
            Initialize();
        }

        public void Initialize(double? ballStartX = null)
        {
            IsGameOver = false;
            IsWon = false;
            Score = 0;
            BlockDescentTimer = 0;
            StarEffectTimer = 0;
            TriangleEffectTimer = 0;
            FallingItems = new List<FallingItem>();

            // Paddle at bottom center
            Paddle = new Paddle(Width / 2 - 50, Height - 30, BasePaddleWidth, 20);

            // Ball just above paddle
            // Speed reduced (150 -> 30 -> 15)
            double startX = ballStartX ?? (Width / 2);
            Ball = new Ball(startX, Height - 40, 5, BaseBallSpeed, -BaseBallSpeed);

            // Blocks
            Blocks = new List<Block>();
            int rows = 5;
            int cols = 10;
            double blockWidth = (Width - 20) / cols;
            double blockHeight = 20;

            string[] colors = { "Red", "Orange", "Yellow", "Green", "Blue" };
            Random rnd = new Random();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Blocks.Add(new Block(
                        10 + c * blockWidth,
                        10 + r * blockHeight,
                        blockWidth - 2,
                        blockHeight - 2,
                        colors[r % colors.Length],
                        rnd.Next(1, 21) // Random Health 1-20
                    ));
                }
            }

            // Assign Items to 10% of blocks
            int itemCount = (int)(Blocks.Count * 0.1);
            var shuffledBlocks = Blocks.OrderBy(x => rnd.Next()).Take(itemCount).ToList();

            // Split between Star and Triangle
            for (int i = 0; i < shuffledBlocks.Count; i++)
            {
                if (i % 2 == 0) shuffledBlocks[i].Item = ItemType.Star;
                else shuffledBlocks[i].Item = ItemType.Triangle;
            }
        }

        public void Update(double deltaTime)
        {
            if (IsGameOver || IsWon) return;

            // --- Effect Timers ---
            if (StarEffectTimer > 0) StarEffectTimer -= deltaTime;
            if (TriangleEffectTimer > 0) TriangleEffectTimer -= deltaTime;

            // Apply Effects
            double currentSpeedScale = (StarEffectTimer > 0) ? 0.8 : 1.0;
            double currentPaddleWidth = (TriangleEffectTimer > 0) ? BasePaddleWidth * 1.2 : BasePaddleWidth;

            Paddle.Width = currentPaddleWidth;

            // --- Block Descent ---
            BlockDescentTimer += deltaTime;
            if (BlockDescentTimer >= 30.0)
            {
                BlockDescentTimer = 0;
                foreach (var block in Blocks)
                {
                    block.Y += block.Height;
                }
            }

            // --- AI Paddle Movement ---
            double targetX = Ball.X;

            // If ball is coming down, try to aim
            if (Ball.VelocityY > 0)
            {
                double predictedX = PredictBallXAtPaddle();
                double centroidX = GetBlockCentroidX();

                double offset = 0;
                if (Blocks.Count > 0)
                {
                    if (centroidX < predictedX) offset = 35; // Aim Left
                    else offset = -35; // Aim Right
                }
                targetX = predictedX + offset;
            }

            double paddleCenter = Paddle.X + Paddle.Width / 2;
            double diff = targetX - paddleCenter;
            double aiSpeed = BasePaddleSpeed;

            if (Math.Abs(diff) > 5)
            {
                if (diff > 0) MovePaddle(aiSpeed);
                else MovePaddle(-aiSpeed);
            }

            // --- Falling Items ---
            for (int i = FallingItems.Count - 1; i >= 0; i--)
            {
                var item = FallingItems[i];
                item.Y += 5; // Fall speed

                // Check collision with paddle
                if (CheckItemCollision(item, Paddle))
                {
                    if (item.Type == ItemType.Star) StarEffectTimer = 10.0;
                    if (item.Type == ItemType.Triangle) TriangleEffectTimer = 10.0;
                    FallingItems.RemoveAt(i);
                }
                else if (item.Y > Height)
                {
                    FallingItems.RemoveAt(i);
                }
            }

            // --- Ball Physics Sub-stepping ---
            double currentBallSpeed = BaseBallSpeed * currentSpeedScale;
            // Normalize current velocity to match currentBallSpeed
            double currentVelMag = Math.Sqrt(Ball.VelocityX * Ball.VelocityX + Ball.VelocityY * Ball.VelocityY);
            if (Math.Abs(currentVelMag - currentBallSpeed) > 0.1 && currentVelMag > 0.001)
            {
                double scale = currentBallSpeed / currentVelMag;
                Ball.VelocityX *= scale;
                Ball.VelocityY *= scale;
            }

            double speed = currentBallSpeed;
            double stepSize = Ball.Radius; // Safe step size
            if (stepSize < 2) stepSize = 2; // Min step

            double distanceRemaining = speed;

            if (speed < 0.001) return;

            while (distanceRemaining > 0)
            {
                double moveDist = Math.Min(stepSize, distanceRemaining);
                double ratio = moveDist / speed;

                // Move Ball partial step
                Ball.X += Ball.VelocityX * ratio;
                Ball.Y += Ball.VelocityY * ratio;

                distanceRemaining -= moveDist;

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
                else if (Ball.Y - Ball.Radius > Height)
                {
                    IsGameOver = true;
                    return;
                }

                // Paddle Collision
                if (CheckCollision(Ball, Paddle))
                {
                    Ball.VelocityY = -Math.Abs(Ball.VelocityY); // Bounce up

                    double hitPoint = Ball.X - (Paddle.X + Paddle.Width / 2);
                    double currentSpeed = Math.Sqrt(Ball.VelocityX*Ball.VelocityX + Ball.VelocityY*Ball.VelocityY);

                    double normalizedHit = hitPoint / (Paddle.Width / 2);
                    Ball.VelocityX = normalizedHit * currentSpeed * 0.75;

                    double newSpeed = Math.Sqrt(Ball.VelocityX*Ball.VelocityX + Ball.VelocityY*Ball.VelocityY);
                    Ball.VelocityX = (Ball.VelocityX / newSpeed) * currentSpeed;
                    Ball.VelocityY = (Ball.VelocityY / newSpeed) * currentSpeed;

                    if (Ball.VelocityY > 0) Ball.VelocityY = -Ball.VelocityY;
                }

                // Block Collision
                for (int i = Blocks.Count - 1; i >= 0; i--)
                {
                    if (CheckCollision(Ball, Blocks[i]))
                    {
                        ResolveBlockCollision(Ball, Blocks[i]);

                        var block = Blocks[i];
                        block.Health--;
                        Score += 10;

                        if (block.Health <= 0)
                        {
                            if (block.Item != ItemType.None)
                            {
                                FallingItems.Add(new FallingItem(block.X + block.Width/2, block.Y + block.Height/2, block.Item));
                            }

                            OnBlockBroken?.Invoke(block);
                            Blocks.RemoveAt(i);
                            Score += 100;
                        }

                        if (Blocks.Count == 0)
                        {
                            IsWon = true;
                            return;
                        }
                        break;
                    }
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
            double closestX = Math.Max(rect.X, Math.Min(ball.X, rect.X + rect.Width));
            double closestY = Math.Max(rect.Y, Math.Min(ball.Y, rect.Y + rect.Height));

            double distanceX = ball.X - closestX;
            double distanceY = ball.Y - closestY;

            double distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
            return distanceSquared < (ball.Radius * ball.Radius);
        }

        private bool CheckItemCollision(FallingItem item, Paddle paddle)
        {
            return (item.X >= paddle.X && item.X <= paddle.X + paddle.Width &&
                    item.Y >= paddle.Y && item.Y <= paddle.Y + paddle.Height);
        }

        private double GetBlockCentroidX()
        {
            if (Blocks.Count == 0) return Width / 2;
            double sum = 0;
            foreach (var b in Blocks) sum += (b.X + b.Width / 2);
            return sum / Blocks.Count;
        }

        private double PredictBallXAtPaddle()
        {
            if (Math.Abs(Ball.VelocityY) < 0.001) return Ball.X;
            double timeSteps = (Paddle.Y - Ball.Y) / Ball.VelocityY;
            if (timeSteps <= 0) return Ball.X;

            double futureX = Ball.X + Ball.VelocityX * timeSteps;

            while (futureX < 0 || futureX > Width)
            {
                if (futureX < 0) futureX = -futureX;
                if (futureX > Width) futureX = 2 * Width - futureX;
            }
            return futureX;
        }

        private void ResolveBlockCollision(Ball ball, Block block)
        {
            double ballCenterX = ball.X;
            double ballCenterY = ball.Y;
            double blockCenterX = block.X + block.Width / 2;
            double blockCenterY = block.Y + block.Height / 2;
            double dx = ballCenterX - blockCenterX;
            double dy = ballCenterY - blockCenterY;
            double combinedHalfWidth = (block.Width / 2) + ball.Radius;
            double combinedHalfHeight = (block.Height / 2) + ball.Radius;
            double overlapX = combinedHalfWidth - Math.Abs(dx);
            double overlapY = combinedHalfHeight - Math.Abs(dy);

            if (overlapX > 0 && overlapY > 0)
            {
                if (overlapX < overlapY)
                {
                    Ball.VelocityX = -Ball.VelocityX;
                    if (dx > 0) Ball.X += overlapX; else Ball.X -= overlapX;
                }
                else
                {
                    Ball.VelocityY = -Ball.VelocityY;
                    if (dy > 0) Ball.Y += overlapY; else Ball.Y -= overlapY;
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
        public int MaxHealth { get; set; }
        public int Health { get; set; }
        public ItemType Item { get; set; }

        public Block(double x, double y, double width, double height, string color, int health)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Color = color;
            MaxHealth = health;
            Health = health;
            Item = ItemType.None;
        }
    }

    public class FallingItem
    {
        public double X { get; set; }
        public double Y { get; set; }
        public ItemType Type { get; set; }

        public FallingItem(double x, double y, ItemType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}
