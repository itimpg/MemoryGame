using System;
using System.Collections.Generic;
using System.Linq;
using MemoryGame.Core;
using Xunit;

namespace MemoryGame.Tests;

public class ScoringTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Theory]
	[InlineData(3, 0f, 60)]   // Instant: full bonus, 3 × (10 + 10).
	[InlineData(3, 2.5f, 45)] // Half bonus.
	[InlineData(3, 5f, 30)]   // Bonus gone.
	[InlineData(3, 30f, 30)]  // Never below the per-digit base.
	[InlineData(9, 0f, 180)]
	public void Points_scale_with_digits_and_speed(int digits, float answerTime, int expected) =>
		Assert.Equal(expected, Scoring.PointsFor(digits, answerTime, Rules));
}

public class DifficultyTrackerTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Fact]
	public void Two_correct_in_a_row_adds_a_digit()
	{
		var tracker = new DifficultyTracker(Rules);
		Assert.Equal(0, tracker.RecordAnswer(true));
		Assert.Equal(+1, tracker.RecordAnswer(true));
		Assert.Equal(Rules.MinDigits + 1, tracker.Digits);
	}

	[Fact]
	public void Two_wrong_in_a_row_removes_a_digit()
	{
		var tracker = new DifficultyTracker(Rules);
		tracker.RecordAnswer(true);
		tracker.RecordAnswer(true);
		Assert.Equal(0, tracker.RecordAnswer(false));
		Assert.Equal(-1, tracker.RecordAnswer(false));
		Assert.Equal(Rules.MinDigits, tracker.Digits);
	}

	[Fact]
	public void Alternating_answers_never_change_digits()
	{
		var tracker = new DifficultyTracker(Rules);
		for (int i = 0; i < 10; i++)
			Assert.Equal(0, tracker.RecordAnswer(i % 2 == 0));
	}

	[Fact]
	public void Streak_restarts_after_a_change()
	{
		var tracker = new DifficultyTracker(Rules);
		tracker.RecordAnswer(true);
		tracker.RecordAnswer(true);
		Assert.Equal(0, tracker.RecordAnswer(true)); // Third in a row starts a new streak.
		Assert.Equal(+1, tracker.RecordAnswer(true));
	}

	[Fact]
	public void Digits_stay_within_limits()
	{
		var tracker = new DifficultyTracker(Rules);
		Assert.Equal(0, tracker.RecordAnswer(false));
		Assert.Equal(0, tracker.RecordAnswer(false));
		Assert.Equal(Rules.MinDigits, tracker.Digits);

		for (int i = 0; i < 100; i++)
			tracker.RecordAnswer(true);
		Assert.Equal(Rules.MaxDigits, tracker.Digits);
	}
}

public class GameSessionTests
{
	private static readonly GameRules Rules = GameRules.Default;

	private static GameSession StartedSession(List<RoundPhase>? phases = null, List<AnswerResult>? answers = null)
	{
		var session = new GameSession(Rules, new Random(42));
		if (phases is not null)
			session.PhaseChanged += phases.Add;
		if (answers is not null)
			session.AnswerChecked += answers.Add;
		session.Start();
		return session;
	}

	private static void Type(GameSession session, string digits)
	{
		foreach (char c in digits)
			session.EnterDigit(c - '0');
	}

	private static string WrongAnswerFor(string number) =>
		new(number.Select(c => c == '9' ? '0' : (char)(c + 1)).ToArray());

	[Fact]
	public void Starts_by_showing_a_number_without_a_leading_zero()
	{
		var session = StartedSession();
		Assert.Equal(RoundPhase.Showing, session.Phase);
		Assert.Equal(Rules.MinDigits, session.CurrentNumber.Length);
		Assert.NotEqual('0', session.CurrentNumber[0]);
		Assert.True(session.CurrentNumber.All(char.IsAsciiDigit));
	}

	[Fact]
	public void Hides_the_number_after_the_show_duration()
	{
		var session = StartedSession();
		session.Tick(Rules.ShowDuration - 0.01f);
		Assert.Equal(RoundPhase.Showing, session.Phase);
		session.Tick(0.02f);
		Assert.Equal(RoundPhase.Input, session.Phase);
	}

	[Fact]
	public void Ignores_digits_while_the_number_is_showing()
	{
		var session = StartedSession();
		session.EnterDigit(1);
		Assert.Equal("", session.Entered);
	}

	[Fact]
	public void Checks_the_answer_when_the_last_digit_is_entered()
	{
		var answers = new List<AnswerResult>();
		var session = StartedSession(answers: answers);
		session.Tick(Rules.ShowDuration);
		session.Tick(1f);

		string number = session.CurrentNumber;
		Type(session, number[..^1]);
		Assert.Empty(answers);
		Type(session, number[^1..]);

		var answer = Assert.Single(answers);
		Assert.True(answer.IsCorrect);
		Assert.Equal(Scoring.PointsFor(number.Length, 1f, Rules), answer.Points);
		Assert.Equal(answer.Points, session.Score);
		Assert.Equal(RoundPhase.Feedback, session.Phase);
	}

	[Fact]
	public void Wrong_answer_scores_nothing()
	{
		var answers = new List<AnswerResult>();
		var session = StartedSession(answers: answers);
		session.Tick(Rules.ShowDuration);
		Type(session, WrongAnswerFor(session.CurrentNumber));

		Assert.False(Assert.Single(answers).IsCorrect);
		Assert.Equal(0, session.Score);
		Assert.Equal(1, session.Attempts);
	}

	[Fact]
	public void Two_correct_answers_make_the_next_number_longer()
	{
		var session = StartedSession();
		for (int round = 0; round < 2; round++)
		{
			session.Tick(Rules.ShowDuration);
			Type(session, session.CurrentNumber);
			session.Tick(Rules.FeedbackDuration);
		}
		Assert.Equal(RoundPhase.Showing, session.Phase);
		Assert.Equal(Rules.MinDigits + 1, session.CurrentNumber.Length);
	}

	[Fact]
	public void Finishes_when_time_runs_out()
	{
		var phases = new List<RoundPhase>();
		var session = StartedSession(phases);
		for (int i = 0; i < 1000 && session.Phase != RoundPhase.Finished; i++)
			session.Tick(0.1f);

		Assert.Equal(RoundPhase.Finished, session.Phase);
		Assert.Equal(RoundPhase.Finished, phases[^1]);
		Assert.Equal(0f, session.TimeLeft);
	}
}
