using System;
using Godot;
using MemoryGame.Audio;
using MemoryGame.Core;
using MemoryGame.Progress;
using MemoryGame.UI;

namespace MemoryGame;

/// <summary>
/// Owns the current run and the player profile, and switches between screens in response to their signals.
/// </summary>
public partial class Main : Control
{
	private readonly Random _rng = new();
	private Run? _run;
	private PlayerProfile _profile = null!;

	private MainMenuScreen _mainMenu = null!;
	private LoadoutScreen _loadout = null!;
	private GameScreen _gameScreen = null!;
	private StageClearScreen _stageClear = null!;
	private GameOverScreen _gameOver = null!;
	private PauseMenu _pauseMenu = null!;
	private SkillUnlockPopup _skillUnlockPopup = null!;
	private Control[] _screens = [];

	public override void _Ready()
	{
		_profile = ProfileStorage.Load();

		_mainMenu = GetNode<MainMenuScreen>("%MainMenuScreen");
		_loadout = GetNode<LoadoutScreen>("%LoadoutScreen");
		_gameScreen = GetNode<GameScreen>("%GameScreen");
		_stageClear = GetNode<StageClearScreen>("%StageClearScreen");
		_gameOver = GetNode<GameOverScreen>("%GameOverScreen");
		_pauseMenu = GetNode<PauseMenu>("%PauseMenu");
		_skillUnlockPopup = CreateSkillUnlockPopup();
		_screens = [_mainMenu, _loadout, _gameScreen, _stageClear, _gameOver];

		_mainMenu.StartRequested += OnStartRequested;
		_mainMenu.ClearSaveRequested += ClearSaveData;
		_loadout.StartRequested += StartRunWithLoadout;
		_loadout.BackRequested += ShowMainMenu;
		_gameScreen.BattleEnded += OnBattleEnded;
		_gameScreen.PauseRequested += PauseGame;
		_stageClear.UpgradeChosen += OnUpgradeChosen;
		_stageClear.NextStageRequested += StartNextStage;
		_pauseMenu.ResumeRequested += ResumeGame;
		_pauseMenu.MainMenuRequested += QuitToMainMenu;
		_gameOver.PlayAgainRequested += OnStartRequested;
		_gameOver.MenuRequested += ShowMainMenu;

		ShowMainMenu();
	}

	/// <summary>
	/// Added from code rather than placed in main.tscn, so an editor that still has an older copy of
	/// main.tscn open can't drop it when it saves. Sits above the screens but below the pause menu.
	/// </summary>
	private SkillUnlockPopup CreateSkillUnlockPopup()
	{
		var popup = GD.Load<PackedScene>("res://Scenes/Components/skill_unlock_popup.tscn").Instantiate<SkillUnlockPopup>();
		popup.Visible = false;
		AddChild(popup);
		MoveChild(popup, _pauseMenu.GetIndex());
		return popup;
	}

	public override void _Notification(int what)
	{
		// Android back button. Needs application/config/quit_on_go_back off so it doesn't just quit.
		if (what != NotificationWMGoBackRequest)
			return;

		if (_mainMenu.Visible && _mainMenu.CloseDialog())
			return;

		if (_skillUnlockPopup.Visible)
			_skillUnlockPopup.Hide();
		else if (GetTree().Paused)
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
		AudioManager.Instance.PlayMusic(screen == _gameScreen ? MusicTrack.Battle : MusicTrack.Menu);
	}

	private void ShowMainMenu()
	{
		_mainMenu.ShowProgress(_profile);
		ShowScreen(_mainMenu);
	}

	private void ClearSaveData()
	{
		ProfileStorage.DeleteAll();
		_profile = new PlayerProfile();
		ShowMainMenu();
	}

	/// <summary>Goes to skill selection first, unless there's nothing to choose from yet.</summary>
	private void OnStartRequested()
	{
		if (_profile.UnlockedSkills.Count == 0)
		{
			StartRun();
			return;
		}
		_loadout.Display(_profile);
		ShowScreen(_loadout);
	}

	private void StartRunWithLoadout()
	{
		_profile.Equip([.. _loadout.SelectedSkills]);
		ProfileStorage.Save(_profile);
		StartRun();
	}

	private void StartRun()
	{
		_run = new Run(GameRules.Default, _rng, _profile.EquippedSkills);
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
			// Every upgrade is on offer for now; unlocks (meta progression) could narrow this pool later.
			_stageClear.Display(run, run.RollUpgradeOffer(UpgradeCatalog.All));
			AudioManager.Instance.Play(Sfx.StageClear);
			ShowScreen(_stageClear);
			return;
		}

		ShowGameOver(run, RecordRun(run));
	}

	private void OnUpgradeChosen(string upgradeId)
	{
		_run!.Upgrades.Add(UpgradeCatalog.ById(upgradeId));
		AudioManager.Instance.Play(Sfx.UpgradePick);
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

	/// <summary>
	/// Ends the run early. It still counts toward skill unlocks, up to the stages already cleared;
	/// if it unlocked one, the game over screen shows it instead of going straight to the menu.
	/// </summary>
	private void QuitToMainMenu()
	{
		_gameScreen.Abandon();
		ResumeGame();

		if (_run is { } run && RecordRun(run) is { UnlockedSkill: not null } reward)
			ShowGameOver(run, reward);
		else
			ShowMainMenu();
	}

	private void ShowGameOver(Run run, RunReward reward)
	{
		_gameOver.Display(run, reward, _profile);
		ShowScreen(_gameOver);
		if (reward.UnlockedSkill is { } skill)
			_skillUnlockPopup.Present(skill);
	}

	/// <summary>Adds the finished run to the profile (best stage, skill unlock), saves it and clears the run.</summary>
	private RunReward RecordRun(Run run)
	{
		var reward = _profile.RecordRun(run.Stage, run.Stats.StagesCleared);
		ProfileStorage.Save(_profile);
		_run = null;
		return reward;
	}
}
