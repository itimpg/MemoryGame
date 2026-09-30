using System;
using Godot;
using MemoryGame.Core;
using MemoryGame.HighScores;
using MemoryGame.UI;

namespace MemoryGame;

/// <summary>Owns the high score table and switches between screens in response to their signals.</summary>
public partial class Main : Control
{
	private const int HighScoreCapacity = 10;

	private HighScoreTable _highScores = null!;
	private MainMenuScreen _mainMenu = null!;
	private GameScreen _gameScreen = null!;
	private GameOverScreen _gameOver = null!;
	private HighScoresScreen _highScoresScreen = null!;
	private PauseMenu _pauseMenu = null!;
	private Control[] _screens = [];

	public override void _Ready()
	{
		_highScores = HighScoreStorage.Load(HighScoreCapacity);

		_mainMenu = GetNode<MainMenuScreen>("%MainMenuScreen");
		_gameScreen = GetNode<GameScreen>("%GameScreen");
		_gameOver = GetNode<GameOverScreen>("%GameOverScreen");
		_highScoresScreen = GetNode<HighScoresScreen>("%HighScoresScreen");
		_pauseMenu = GetNode<PauseMenu>("%PauseMenu");
		_screens = [_mainMenu, _gameScreen, _gameOver, _highScoresScreen];

		_mainMenu.StartRequested += StartGame;
		_mainMenu.HighScoresRequested += ShowHighScores;
		_gameScreen.GameFinished += OnGameFinished;
		_gameScreen.PauseRequested += PauseGame;
		_pauseMenu.ResumeRequested += ResumeGame;
		_pauseMenu.MainMenuRequested += QuitToMainMenu;
		_gameOver.PlayAgainRequested += StartGame;
		_gameOver.MenuRequested += ShowMainMenu;
		_highScoresScreen.BackRequested += ShowMainMenu;

		ShowMainMenu();
	}

	public override void _Notification(int what)
	{
		// Android back button. Needs application/config/quit_on_go_back off so it doesn't just quit.
		if (what != NotificationWMGoBackRequest)
			return;

		if (GetTree().Paused)
			ResumeGame();
		else if (_gameScreen.IsPlaying)
			PauseGame();
		else if (_mainMenu.Visible)
			GetTree().Quit();
		else
			ShowMainMenu();
	}

	private void ShowScreen(Control screen)
	{
		foreach (var s in _screens)
			s.Visible = s == screen;
	}

	private void ShowMainMenu() => ShowScreen(_mainMenu);

	private void ShowHighScores()
	{
		_highScoresScreen.Display(_highScores.Entries);
		ShowScreen(_highScoresScreen);
	}

	private void StartGame()
	{
		ShowScreen(_gameScreen);
		_gameScreen.StartGame();
	}

	private void PauseGame()
	{
		// Also requested when the app loses focus, which can happen while already paused.
		if (GetTree().Paused)
			return;
		GetTree().Paused = true;
		_pauseMenu.Show();
	}

	private void ResumeGame()
	{
		_pauseMenu.Hide();
		GetTree().Paused = false;
	}

	/// <summary>Leaves the paused game. The unfinished game isn't added to the high scores.</summary>
	private void QuitToMainMenu()
	{
		_gameScreen.Abandon();
		ResumeGame();
		ShowMainMenu();
	}

	private void OnGameFinished(int score, int correct, int attempts, float totalCorrectAnswerTime)
	{
		var result = new GameResult(score, correct, attempts, totalCorrectAnswerTime);
		int? rank = _highScores.Add(HighScoreEntry.FromResult(result, DateTime.Now));
		if (rank is not null)
			HighScoreStorage.Save(_highScores);

		_gameOver.Display(result, rank);
		ShowScreen(_gameOver);
	}
}
