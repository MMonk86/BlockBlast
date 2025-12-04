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
        public int Score { get; private set; }

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
            Score = 0;

            // Paddle at bottom center
            Paddle = new Paddle(Width / 2 - 50, Height - 30, 100, 20);

            // Ball just above paddle
            // Speed increased by 10x from "current" (15).
            // NOTE: 150 pixels per frame is extremely fast and requires sub-stepping in Update loop.
            Ball = new Ball(Width / 2, Height - 40, 5, 150, -150);

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
        }

        public void Update()
        {
            if (IsGameOver || IsWon) return;

            // AI Paddle Movement
            double targetX = Ball.X;

            // If ball is coming down, try to aim
            if (Ball.VelocityY > 0)
            {
                double predictedX = PredictBallXAtPaddle();
                double centroidX = GetBlockCentroidX();

                // If blocks are to the Left of impact, we want to hit ball with Right side of paddle (send Left)
                // This means PaddleCenter should be to the Right of Ball.
                // Offset = +35

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
            double aiSpeed = 20; // Fast enough to catch up

            if (Math.Abs(diff) > 5)
            {
                if (diff > 0) MovePaddle(aiSpeed);
                else MovePaddle(-aiSpeed);
            }

            // Physics Sub-stepping
            // With very high speeds, we must move in small steps to prevent tunneling
            double speed = Math.Sqrt(Ball.VelocityX * Ball.VelocityX + Ball.VelocityY * Ball.VelocityY);
            double stepSize = Ball.Radius; // Safe step size
            if (stepSize < 2) stepSize = 2; // Min step

            double distanceRemaining = speed;
            double stepRatio = stepSize / speed;

            // If speed is 0 (shouldn't happen) avoid div by zero
            if (speed < 0.001) return;

            while (distanceRemaining > 0)
            {
                double moveDist = Math.Min(stepSize, distanceRemaining);
                double ratio = moveDist / speed;

                // Move Ball partial step
                Ball.X += Ball.VelocityX * ratio;
                Ball.Y += Ball.VelocityY * ratio;

                distanceRemaining -= moveDist;

                // --- Collision Checks at this sub-step ---

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
                    // Keep the high speed magnitude but change direction
                    double currentSpeed = Math.Sqrt(Ball.VelocityX*Ball.VelocityX + Ball.VelocityY*Ball.VelocityY);

                    // Calculate new direction vector
                    // We want a spread of angles.
                    // Hit point ranges from -50 to +50
                    // Normalized: -1 to 1
                    double normalizedHit = hitPoint / (Paddle.Width / 2);

                    // Simple deviation: Add horizontal velocity proportional to hit
                    Ball.VelocityX = normalizedHit * currentSpeed * 0.75;

                    // Re-normalize to maintain speed
                    double newSpeed = Math.Sqrt(Ball.VelocityX*Ball.VelocityX + Ball.VelocityY*Ball.VelocityY);
                    Ball.VelocityX = (Ball.VelocityX / newSpeed) * currentSpeed;
                    Ball.VelocityY = (Ball.VelocityY / newSpeed) * currentSpeed;

                    // Ensure Y is negative (up)
                    if (Ball.VelocityY > 0) Ball.VelocityY = -Ball.VelocityY;
                }

                // Block Collision
                bool hitBlock = false;
                for (int i = Blocks.Count - 1; i >= 0; i--)
                {
                    if (CheckCollision(Ball, Blocks[i]))
                    {
                        ResolveBlockCollision(Ball, Blocks[i]);

                        // Handle health
                        var block = Blocks[i];
                        block.Health--;
                        Score += 10;

                        if (block.Health <= 0)
                        {
                            OnBlockBroken?.Invoke(block);
                            Blocks.RemoveAt(i);
                            Score += 100; // Bonus for breaking
                        }

                        if (Blocks.Count == 0)
                        {
                            IsWon = true;
                            return;
                        }
                        hitBlock = true;
                        break; // Handle one block collision per step
                    }
                }

                // If we hit something major (paddle or block), we might want to stop this frame's movement
                // or just continue. For simplicity, continue, but re-evaluating velocities in next loop iteration
                // would be complex because we are inside a fixed loop based on initial velocity.
                // However, since we modify VelocityX/Y upon collision, the next x += vx * ratio will use the NEW velocity.
                // But `speed` variable is constant for the loop.
                // We should update the `speed` variable if velocity changes direction, but magnitude should stay roughly same.
                // Actually, if we bounce, we change direction. The loop moves by `moveDist` along the velocity vector.
                // If we change velocity vector mid-loop, the next sub-step should use the new vector.
                // So: `Ball.X += Ball.VelocityX * ratio` works fine because it reads current Velocity.
                // But `distanceRemaining` assumes constant speed.
                // If speed magnitude changes significantly, this loop logic is slightly flawed, but for Pong physics (constant speed), it's fine.
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

        private double GetBlockCentroidX()
        {
            if (Blocks.Count == 0) return Width / 2;
            double sum = 0;
            foreach (var b in Blocks) sum += (b.X + b.Width / 2);
            return sum / Blocks.Count;
        }

        private double PredictBallXAtPaddle()
        {
            // Simple prediction assuming straight line or simple bounce
            // Not perfect but better than tracking current X

            double timeSteps = (Paddle.Y - Ball.Y) / Ball.VelocityY;
            if (timeSteps <= 0) return Ball.X;

            double futureX = Ball.X + Ball.VelocityX * timeSteps;

            // Handle simple single wall bounce estimation
            // (Iterative bounce handling is better but complex for this scope)
            while (futureX < 0 || futureX > Width)
            {
                if (futureX < 0) futureX = -futureX;
                if (futureX > Width) futureX = 2 * Width - futureX;
            }
            return futureX;
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
        public int MaxHealth { get; set; }
        public int Health { get; set; }

        public Block(double x, double y, double width, double height, string color, int health)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Color = color;
            MaxHealth = health;
            Health = health;
        }
    }
}
