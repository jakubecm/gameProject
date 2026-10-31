using System;
using System.Linq;
using NUnit.Framework;
using ColdShutdown.Core;

namespace ColdShutdown.Tests.EditMode
{
    public class PasswordPuzzleTests
    {
        private static readonly string[] Pool =
        {
            "SERVER", "SYSTEM", "SIGNAL", "KABELY", "ZALOHA", "BRANKA", "SKENER", "KLIENT", "ADRESA", "KODEKS", "SMYCKA", "CHLADI",
            "HESLO", "UZEL", "PROXY",
        };

        [Test]
        public void Likeness_CountsLettersOnTheSamePlace()
        {
            Assert.AreEqual(6, PasswordPuzzle.Likeness("SERVER", "SERVER"));
            Assert.AreEqual(2, PasswordPuzzle.Likeness("SERVER", "SYSTEM"), "the S at the start and the E on the fifth place");
            Assert.AreEqual(0, PasswordPuzzle.Likeness("ABC", "XYZ"));
            Assert.AreEqual(4, PasswordPuzzle.Likeness("abcd", "ABCD"), "case does not matter");
            Assert.AreEqual(2, PasswordPuzzle.Likeness("ABCD", "ABXC"), "letters on other places do not count");
        }

        [Test]
        public void Likeness_ComparesOnlyTheShorterLength()
        {
            Assert.AreEqual(3, PasswordPuzzle.Likeness("ABCDE", "ABC"));
            Assert.AreEqual(0, PasswordPuzzle.Likeness(null, "ABC"));
        }

        [Test]
        public void Puzzle_HasTheRightNumberOfCandidates_AllOfTheSameLength()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var puzzle = PasswordPuzzle.Create(Pool, seed);

                Assert.AreEqual(PasswordPuzzle.DefaultCandidateCount, puzzle.Candidates.Count, $"seed {seed}");
                Assert.AreEqual(puzzle.Candidates.Count, puzzle.Candidates.Distinct().Count(), $"seed {seed}: duplicates");
                Assert.IsTrue(puzzle.Candidates.All(word => word.Length == puzzle.WordLength), $"seed {seed}");
                Assert.AreEqual(PasswordPuzzle.DefaultTries, puzzle.TriesLeft);
                Assert.AreEqual(PasswordState.Playing, puzzle.State);
            }
        }

        [Test]
        public void Puzzle_IsTheSameForTheSameSeed()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var first = PasswordPuzzle.Create(Pool, seed);
                var second = PasswordPuzzle.Create(Pool, seed);
                CollectionAssert.AreEqual(first.Candidates, second.Candidates, $"seed {seed}");

                // The password is the same too: the right word wins on the first try for both.
                string answer = first.Candidates.Single(word => TriesToWin(PasswordPuzzle.Create(Pool, seed), word));
                Assert.IsTrue(TriesToWin(second, answer));
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentPuzzles()
        {
            var puzzles = Enumerable.Range(0, 40).Select(seed => string.Join(",", PasswordPuzzle.Create(Pool, seed).Candidates)).Distinct();
            Assert.Greater(puzzles.Count(), 10);
        }

        [Test]
        public void TheSecret_IsHiddenWhilePlaying_AndShownAtTheEnd()
        {
            var puzzle = PasswordPuzzle.Create(Pool, 3);
            Assert.IsNull(puzzle.Secret);

            string answer = puzzle.Candidates.Single(word => TriesToWin(PasswordPuzzle.Create(Pool, 3), word));
            puzzle.Guess(answer);

            Assert.AreEqual(answer, puzzle.Secret);
            Assert.AreEqual(PasswordState.Won, puzzle.State);
        }

        [Test]
        public void TheRightWord_Wins_AndDoesNotCostATry()
        {
            var puzzle = PasswordPuzzle.Create(Pool, 5);
            string answer = puzzle.Candidates.Single(word => TriesToWin(PasswordPuzzle.Create(Pool, 5), word));

            var guess = puzzle.Guess(answer);

            Assert.IsTrue(guess.IsCorrect);
            Assert.AreEqual(guess.Length, guess.Likeness);
            Assert.AreEqual(PasswordState.Won, puzzle.State);
            Assert.AreEqual(PasswordPuzzle.DefaultTries, puzzle.TriesLeft);
        }

        [Test]
        public void WrongWords_CostTries_AndFourOfThemLockTheTerminal()
        {
            var puzzle = PasswordPuzzle.Create(Pool, 7);
            string answer = puzzle.Candidates.Single(word => TriesToWin(PasswordPuzzle.Create(Pool, 7), word));
            var wrong = puzzle.Candidates.Where(word => word != answer).Take(4).ToList();

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(PasswordState.Playing, puzzle.State, $"before wrong guess {i + 1}");
                var guess = puzzle.Guess(wrong[i]);
                Assert.IsFalse(guess.IsCorrect);
                Assert.Less(guess.Likeness, guess.Length);
                Assert.AreEqual(3 - i, puzzle.TriesLeft);
            }

            Assert.AreEqual(PasswordState.Lost, puzzle.State);
            Assert.AreEqual(answer, puzzle.Secret);
            Assert.Throws<InvalidOperationException>(() => puzzle.Guess(answer), "the terminal is locked");
        }

        [Test]
        public void Guessing_RejectsUnknownAndRepeatedWords()
        {
            var puzzle = PasswordPuzzle.Create(Pool, 9);
            string answer = puzzle.Candidates.Single(word => TriesToWin(PasswordPuzzle.Create(Pool, 9), word));
            string wrong = puzzle.Candidates.First(word => word != answer);

            Assert.Throws<ArgumentException>(() => puzzle.Guess("NEEXISTUJE"));
            puzzle.Guess(wrong.ToLowerInvariant());
            Assert.IsTrue(puzzle.HasBeenTried(wrong));
            Assert.Throws<InvalidOperationException>(() => puzzle.Guess(wrong));
            Assert.AreEqual(PasswordPuzzle.DefaultTries - 1, puzzle.TriesLeft, "a rejected guess costs nothing");
        }

        [Test]
        public void SomeCandidates_LookLikeThePassword()
        {
            // The answers have to be useful: some candidate shares a letter on the same place with the password
            // (the pool here is small; the real word list has more words, so more look-alikes).
            for (int seed = 0; seed < 100; seed++)
            {
                var puzzle = PasswordPuzzle.Create(Pool, seed);
                string answer = puzzle.Candidates.Single(word => TriesToWin(PasswordPuzzle.Create(Pool, seed), word));
                int similar = puzzle.Candidates.Count(word => word != answer && PasswordPuzzle.Likeness(word, answer) > 0);
                Assert.GreaterOrEqual(similar, 1, $"seed {seed}");
            }
        }

        [Test]
        public void ARandomPlayer_UsingTheAnswers_UsuallyWinsInFourTries()
        {
            // A player who throws out the words that do not fit the answers wins within 4 tries almost always.
            int wins = 0;
            const int games = 300;
            for (int seed = 0; seed < games; seed++)
            {
                var puzzle = PasswordPuzzle.Create(Pool, seed);
                var possible = puzzle.Candidates.ToList();
                var random = new Random(seed);
                while (puzzle.State == PasswordState.Playing)
                {
                    string word = possible[random.Next(possible.Count)];
                    var guess = puzzle.Guess(word);
                    possible = possible.Where(candidate => candidate != word && PasswordPuzzle.Likeness(candidate, word) == guess.Likeness).ToList();
                    if (possible.Count == 0) break;
                }
                if (puzzle.State == PasswordState.Won) wins++;
            }

            TestContext.WriteLine($"Eliminating player won {wins} of {games} puzzles.");
            Assert.GreaterOrEqual(wins, games * 0.85, "the puzzle is too hard to solve by elimination");
        }

        [Test]
        public void Create_RejectsBadInput()
        {
            Assert.Throws<ArgumentNullException>(() => PasswordPuzzle.Create(null, 1));
            Assert.Throws<ArgumentException>(() => PasswordPuzzle.Create(new[] { "ABC", "ABD", "XYZZ" }, 1), "no length has enough words");
            Assert.Throws<ArgumentOutOfRangeException>(() => PasswordPuzzle.Create(Pool, 1, candidateCount: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => PasswordPuzzle.Create(Pool, 1, tries: 0));
        }

        [Test]
        public void Create_IgnoresBlankAndRepeatedWords_AndCase()
        {
            var messy = Pool.Concat(new[] { "", "  ", "server", "SERVER" }).ToList();
            var puzzle = PasswordPuzzle.Create(messy, 2);
            Assert.AreEqual(puzzle.Candidates.Count, puzzle.Candidates.Distinct().Count());
            Assert.IsTrue(puzzle.Candidates.All(word => word == word.ToUpperInvariant()));
        }

        // True if guessing this word wins at once. Used to find the password without a back door into the puzzle.
        private static bool TriesToWin(PasswordPuzzle puzzle, string word) => puzzle.Guess(word).IsCorrect;
    }
}
