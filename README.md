# Brick Breaker

A simple Block Breaker game written in C# targeting .NET Framework 4.7.2.

## Structure

- **BrickBreaker.Core**: The game logic (Physics, State). Targets .NET Standard 2.0.
- **BrickBreaker.App**: The Windows Forms UI. Targets .NET Framework 4.7.2.
- **BrickBreaker.Tests**: Unit tests for the core logic. Targets .NET 6/8+.

## How to Run

1.  Open `BrickBreaker.sln` in Visual Studio (Windows).
2.  Set `BrickBreaker.App` as the startup project.
3.  Press Start (F5).

## Controls

-   **Left Arrow**: Move Paddle Left
-   **Right Arrow**: Move Paddle Right
-   **R**: Restart Game (when Game Over or Won)

## Development Environment (Linux)

The project was developed in a Linux environment.
-   `dotnet build` was used to verify compilation.
-   `dotnet test` was used to verify game logic.
-   The WinForms app was built but cannot be executed in a headless Linux environment.
