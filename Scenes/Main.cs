using System;
using Godot;
using MemoryGame.BestRuns;
using MemoryGame.Core;
using MemoryGame.UI;

namespace MemoryGame;

/// <summary>
/// Owns the current run and the best runs table, and switches between screens in response to their signals.
/// </summary>
public partial class Main : Control
{
	private const int BestRunCapacity = 10;

	private readonly Random _rng = new();
	private Run? _run;
	private BestRunTable _bestRuns = null!;

	private MainMenuScreen _mainMenu = null!;
	private GameScreen _gameScreen = null!;
	private StageClearScreen _stageClear = null!;
	private GameOverScreen _gameOver = null!;
	private BestRunsScreen _bestRunsScreen = null!;
	private PauseMenu _pauseMenu = null!;
	private Control[] _screens = [];

	public override void _Ready()
	{
		_bestRuns = BestRunStorage.Load(BestRunCapacity);

		_mainMenu = GetNode<MainMenuScreen>("%MainMenuScreen");
		_gameScreen = GetNode<GameScreen>("%GameScreen");
		_stageClear = GetNode<StageClearScreen>("%StageClearScreen");
		_gameOver = GetNode<GameOverScreen>("%GameOverScreen");
		_bestRunsScreen = GetNode<BestRunsScreen>("%BestRunsScreen");
		_pauseMenu = GetNode<PauseMenu>("%PauseMenu");
		_screens = [_mainMenu, _gameScreen, _stageClear, _gameOver, _bestRunsScreen];

		_mainMenu.StartRequested += StartRun;
		_mainMenu.BestRunsRequested += ShowBestRuns;
		_gameScreen.BattleEnded += OnBattleEnded;
		_gameScreen.PauseRequested += PauseGame;
		_stageClear.UpgradeChosen += OnUpgradeChosen;
		_stageClear.NextStageRequested += StartNextStage;
		_pauseMenu.ResumeRequested += ResumeGame;
		_pauseMenu.MainMenuRequested += QuitToMainMenu;
		_gameOver.PlayAgainRequested += StartRun;
		_gameOver.MenuRequested += ShowMainMenu;
		_bestRunsScreen.BackRequested += ShowMainMenu;

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
			QuitToMainMenu();
	}

	private void ShowScreen(Control screen)
	{
		foreach (var s in _screens)
			s.Visible = s == screen;
	}

	private void ShowMainMenu() => ShowScreen(_mainMenu);

	private void ShowBestRuns()
	{
		_bestRunsScreen.Display(_bestRuns.Entries);
		ShowScreen(_bestRunsScreen);
	}

	private void StartRun()
	{
		_run = new Run(GameRules.Default, _rng);
		StartNextStage();
	}

	private void StartNextStage()
	{
		var run = _run!;
		var battle = run.StartNextStage();
		ShowScreen(_gameScreen);
		_gameScreen.StartBattle(battle, run.Stage);
	}

	private void OnBattleEnded(bool won)
	{
		var run = _run!;
		if (won)
		{
			// Every upgrade is on offer for now; unlocks (meta progression) will narrow this pool.
			_stageClear.Display(run, run.RollUpgradeOffer(UpgradeCatalog.All));
			ShowScreen(_stageClear);
			return;
		}

		int? rank = RecordRun();
		_gameOver.Display(run, rank);
		ShowScreen(_gameOver);
	}

	private void OnUpgradeChosen(string upgradeId)
	{
		_run!.Upgrades.Add(UpgradeCatalog.ById(upgradeId));
		StartNextStage();
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

	/// <summary>Ends the run early. It still counts for the best runs, up to the stage the player was on.</summary>
	private void QuitToMainMenu()
	{
		_gameScreen.Abandon();
		RecordRun();
		ResumeGame();
		ShowMainMenu();
	}

	/// <summary>Adds the finished run to the best runs and clears it. Returns its rank, or null.</summary>
	private int? RecordRun()
	{
		if (_run is null)
			return null;

		int? rank = _bestRuns.Add(BestRunEntry.FromRun(_run, DateTime.Now));
		if (rank is not null)
			BestRunStorage.Save(_bestRuns);
		_run = null;
		return rank;
	}
}
