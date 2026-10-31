using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdShutdown.Core
{
    public enum PasswordState
    {
        Playing,
        Won,
        Lost,
    }

    /// <summary>The answer to one guess: how many letters are on the right place (the "likeness").</summary>
    public readonly struct PasswordGuess
    {
        public readonly string Word;
        public readonly int Likeness;

        /// <summary>Number of letters in the word; a guess is the password when the likeness equals it.</summary>
        public readonly int Length;

        public PasswordGuess(string word, int likeness, int length)
        {
            Word = word;
            Likeness = likeness;
            Length = length;
        }

        public bool IsCorrect => Likeness == Length;
    }

    /// <summary>
    /// The Password minigame (task 2.5): a list of candidate words of the same length, one of them is the
    /// password. Each guess says how many letters stand on the right place; four wrong guesses lock the
    /// terminal. The candidates are chosen so that some of them look like the password, which makes it
    /// possible to narrow the choice down by the answers instead of guessing blindly.
    /// Plain data and System.Random(seed): the same pool and seed always give the same puzzle.
    /// </summary>
    public sealed class PasswordPuzzle
    {
        public const int DefaultCandidateCount = 10;
        public const int DefaultTries = 4;

        private readonly string secret;
        private readonly List<PasswordGuess> guesses = new List<PasswordGuess>();
        private readonly HashSet<string> guessedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The words to choose from, in the order they are shown.</summary>
        public IReadOnlyList<string> Candidates { get; }

        public int MaxTries { get; }

        public int TriesLeft => MaxTries - guesses.Count(guess => !guess.IsCorrect);

        public PasswordState State { get; private set; } = PasswordState.Playing;

        public IReadOnlyList<PasswordGuess> Guesses => guesses;

        /// <summary>The password, but only once the puzzle is over; null while it is being played.</summary>
        public string Secret => State == PasswordState.Playing ? null : secret;

        public int WordLength => secret.Length;

        private PasswordPuzzle(string secret, List<string> candidates, int tries)
        {
            this.secret = secret;
            Candidates = candidates;
            MaxTries = tries;
        }

        /// <summary>
        /// Makes a puzzle from a pool of words (upper case, no duplicates needed). All candidates have the length
        /// of the password, so the pool needs at least <paramref name="candidateCount"/> words of one length.
        /// </summary>
        public static PasswordPuzzle Create(IReadOnlyList<string> pool, int seed, int candidateCount = DefaultCandidateCount, int tries = DefaultTries)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (candidateCount < 2) throw new ArgumentOutOfRangeException(nameof(candidateCount), "At least two candidates are needed.");
            if (tries < 1) throw new ArgumentOutOfRangeException(nameof(tries), "At least one try is needed.");

            var words = pool
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Select(word => word.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();
            var usableLengths = words.GroupBy(word => word.Length)
                .Where(group => group.Count() >= candidateCount)
                .Select(group => group.Key)
                .OrderBy(length => length)
                .ToList();
            if (usableLengths.Count == 0)
            {
                throw new ArgumentException($"The pool needs at least {candidateCount} different words of the same length.", nameof(pool));
            }

            var random = new Random(seed);
            int length = usableLengths[random.Next(usableLengths.Count)];
            var sameLength = words.Where(word => word.Length == length).OrderBy(word => word, StringComparer.Ordinal).ToList();

            string secret = sameLength[random.Next(sameLength.Count)];
            var others = sameLength.Where(word => word != secret).ToList();

            // About half of the other candidates share letters with the password, so the answers can be used to narrow it down.
            int similar = (candidateCount - 1) / 2;
            var chosen = others
                .Where(word => Likeness(word, secret) > 0)
                .OrderByDescending(word => Likeness(word, secret))
                .ThenBy(word => word, StringComparer.Ordinal)
                .Take(similar)
                .ToList();
            var rest = others.Where(word => !chosen.Contains(word)).ToList();
            Shuffle(rest, random);
            chosen.AddRange(rest.Take(candidateCount - 1 - chosen.Count));

            var candidates = new List<string>(chosen) { secret };
            Shuffle(candidates, random);
            return new PasswordPuzzle(secret, candidates, tries);
        }

        /// <summary>Letters of two words that stand on the same place.</summary>
        public static int Likeness(string first, string second)
        {
            if (first == null || second == null) return 0;

            int count = 0;
            int length = Math.Min(first.Length, second.Length);
            for (int i = 0; i < length; i++)
            {
                if (char.ToUpperInvariant(first[i]) == char.ToUpperInvariant(second[i])) count++;
            }
            return count;
        }

        /// <summary>Tries a word. A wrong word costs a try; the right one wins; no tries left locks the terminal.</summary>
        public PasswordGuess Guess(string word)
        {
            if (State != PasswordState.Playing) throw new InvalidOperationException("The puzzle is over.");

            string match = Candidates.FirstOrDefault(candidate => string.Equals(candidate, word, StringComparison.OrdinalIgnoreCase));
            if (match == null) throw new ArgumentException($"'{word}' is not one of the candidates.", nameof(word));
            if (!guessedWords.Add(match)) throw new InvalidOperationException($"'{match}' has been tried already.");

            var guess = new PasswordGuess(match, Likeness(match, secret), secret.Length);
            guesses.Add(guess);

            if (guess.IsCorrect) State = PasswordState.Won;
            else if (TriesLeft <= 0) State = PasswordState.Lost;
            return guess;
        }

        public bool HasBeenTried(string word) => guessedWords.Contains(word);

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
