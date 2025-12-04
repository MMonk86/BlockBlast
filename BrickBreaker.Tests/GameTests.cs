using Xunit;
using BrickBreaker.Core;
using System.Linq;

namespace BrickBreaker.Tests
{
    public class GameTests
    {
        [Fact]
        public void Initialization_ShouldSetDefaults()
        {
            var game = new Game(800, 600);
            Assert.NotNull(game.Ball);
            Assert.NotNull(game.Paddle);
            Assert.NotEmpty(game.Blocks);
            Assert.Equal(50, game.Blocks.Count); // 5 rows * 10 cols
            Assert.False(game.IsGameOver);
            Assert.False(game.IsWon);
        }

        [Fact]
        public void Ball_ShouldMove()
        {
            var game = new Game(800, 600);
            double initialX = game.Ball.X;
            double initialY = game.Ball.Y;

            game.Update();

            Assert.NotEqual(initialX, game.Ball.X);
            Assert.NotEqual(initialY, game.Ball.Y);
        }

        [Fact]
        public void Ball_ShouldBounceOffWalls()
        {
            var game = new Game(800, 600);
            // Place ball near left wall moving left
            game.Ball.X = 5;
            game.Ball.VelocityX = -5;

            game.Update(); // Should hit wall and bounce

            Assert.True(game.Ball.VelocityX > 0);
        }

        [Fact]
        public void Ball_ShouldBounceOffPaddle()
        {
            var game = new Game(800, 600);
            // Place ball just above paddle moving down
            game.Ball.X = game.Paddle.X + game.Paddle.Width / 2;
            game.Ball.Y = game.Paddle.Y - game.Ball.Radius - 1;
            game.Ball.VelocityY = 5;

            game.Update();

            Assert.True(game.Ball.VelocityY < 0);
        }

        [Fact]
        public void Ball_ShouldDamageAndBreakBlock()
        {
            var game = new Game(800, 600);
            // Remove all blocks except one to avoid interference
            var block = game.Blocks.First();
            game.Blocks.Clear();
            game.Blocks.Add(block);

            block.Health = 1; // Set health to 1 so it breaks in one hit

            // Place ball just below block moving up
            game.Ball.X = block.X + block.Width / 2;
            game.Ball.Y = block.Y + block.Height + game.Ball.Radius + 0.1;
            // High speed collision for the new physics loop
            game.Ball.VelocityY = -20;
            game.Ball.VelocityX = 0;

            game.Update();

            Assert.Empty(game.Blocks);
            Assert.True(game.Ball.VelocityY > 0); // Should bounce down
        }

        [Fact]
        public void Ball_ShouldDamageButNotBreakBlock_WhenHealthGT1()
        {
            var game = new Game(800, 600);
            // Remove all blocks except one
            var block = game.Blocks.First();
            game.Blocks.Clear();
            game.Blocks.Add(block);

            block.Health = 2; // Health > 1

            game.Ball.X = block.X + block.Width / 2;
            game.Ball.Y = block.Y + block.Height + game.Ball.Radius + 0.1;
            game.Ball.VelocityY = -20;
            game.Ball.VelocityX = 0;

            game.Update();

            Assert.Single(game.Blocks); // Should not remove
            Assert.Equal(1, block.Health); // Should decrement
            Assert.True(game.Ball.VelocityY > 0); // Should bounce
        }

        [Fact]
        public void Game_ShouldEnd_WhenBallMissesPaddle()
        {
            var game = new Game(800, 600);
            // Place ball at bottom
            game.Ball.Y = 600 + game.Ball.Radius + 1; // Start clearly off screen
            game.Ball.VelocityY = 5;

            game.Update();

            Assert.True(game.IsGameOver);
        }
    }
}
