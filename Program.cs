using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WordleSolver
{
    class Program
    {
        private static string WordsFilepath = @"C:\workspace\wordle\words-";
        private const int CANDIDATES_TO_EXPLORE_LIMIT = 50;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            WordsFilepath += args[0] + ".txt";
            args = args.Skip(1).ToArray();

            var command = args[0];
            switch (command)
            {
                case "best-first-word":
                    GetBestInitialWord(args.Length > 1 ? args[1] : string.Empty);
                    return;

                case "solve-all":
                    if (args.Length < 2)
                        break;

                    SolveAll(args.Skip(1).ToList());
                    return;

                case "solve":
                    if (args.Length < 3)
                        break;

                    Solve(
                        secretWord: args[1],
                        attempts: args.Skip(2).ToList()
                    );
                    return;

                case "candidates":
                    if (args.Length < 2)
                        break;

                    var candidates = GetCandidates(
                        secretWord: args[1],
                        attempts: args.Skip(2).ToList()
                    );
                    Console.WriteLine(
                        string.Join(Environment.NewLine, candidates.OrderBy(c => c))
                    );
                    return;
            }

            PrintHelp();
        }

        private static void SolveAll(List<string> attempts)
        {
            Console.WriteLine($"Solving all secret words using {attempts} as guess...");
            var watch = Stopwatch.StartNew();
            var words = System.IO.File.ReadAllText(WordsFilepath);
            var precalculatedData = new PrecalculatedData(words);
            var wordsCount = precalculatedData.Words.Length;
            var totalTurns = 0;
            var looses = 0;
            for (int i = 0; i < wordsCount; i++)
            {
                var secretWord = precalculatedData.Words[i];
                var turns = Solve(precalculatedData, secretWord, attempts, printResults: false);
                totalTurns += turns;
                if (turns > 6) looses++;

                // if (turns > 4)
                Console.WriteLine($"Secret Word {secretWord}: {turns} turns");
                var percentage = 100.0 * (i + 1) / wordsCount;
                var ellapsed = watch.ElapsedMilliseconds / 1000.0;
                Console.WriteLine(
                    $"     {percentage:0.00000} %" +
                    $" ({i + 1} / {wordsCount })" +
                    $" {ellapsed:0.00000}s" +
                    $" ETA: {(100.0 - percentage) * ellapsed / (60.0 * percentage):0.000}min"
                );
            }

            Console.WriteLine($"Using {attempts} as first guess, " +
                $"it takes an average of {totalTurns / wordsCount } " +
                $"turns to guess the word");

            Console.WriteLine($"Win rate: {100 * (wordsCount - looses) / wordsCount }");
        }

        private static void Solve(string secretWord, List<string> attempts)
        {
            var words = System.IO.File.ReadAllText(WordsFilepath);
            var precalculatedData = new PrecalculatedData(words);
            Solve(precalculatedData, secretWord, attempts, printResults: true);
        }

        private static int Solve(
            PrecalculatedData precalculatedData,
            string secretWord,
            List<string> attempts,
            bool printResults)
        {
            List<int> candidates = null;
            foreach (var attempt in attempts.Take(attempts.Count - 1).ToList())
            {
                var stepResult = new StepResult(secretWord, attempt);
                candidates = GetCandidatesOnNextStep(
                    candidates, stepResult, precalculatedData, attempt);

                if (printResults)
                    Console.WriteLine($"{attempt}    {stepResult}");
            }

            var candidate = attempts.Last();
            var turn = 0;
            while (true)
            {
                turn++;
                var stepResult = new StepResult(secretWord, candidate);
                if (printResults)
                    Console.WriteLine($"{candidate}    {stepResult}");

                candidates = GetCandidatesOnNextStep(
                    candidates, stepResult, precalculatedData, candidate);

                if (stepResult.IsWin())
                    break;

                if (candidates.Count == 0)
                {
                    Console.WriteLine("ERROR: Word not found");
                    return turn;
                }

                if (candidates.Count < 3)
                {
                    candidate = precalculatedData.Words[candidates[0]];
                    continue;
                }

                // TODO prioritize candidates if same score

                var minNextCandidates = int.MaxValue;
                var candidatesToExplore = candidates.ToList();
                if (candidatesToExplore.Count > CANDIDATES_TO_EXPLORE_LIMIT)
                {
                    candidatesToExplore = candidatesToExplore
                        .Where(idx => precalculatedData.Words[idx].Distinct().Count() == 5)
                        .ToList();

                    if (candidatesToExplore.Count == 0)
                        candidatesToExplore = candidates.ToList();
                }

                for (int nextCandidateIdx = 0; nextCandidateIdx < precalculatedData.Words.Length; nextCandidateIdx++)
                {
                    var nextCandidatesCount = 0;
                    var nextCandidate = precalculatedData.Words[nextCandidateIdx];
                    if (!attempts.Contains(nextCandidate))
                    {
                        for (int secretIdx = 0; secretIdx < candidatesToExplore.Count; secretIdx++)
                        {
                            var hypotheticalSecret = precalculatedData.Words[candidatesToExplore[secretIdx]];
                            stepResult = new StepResult(hypotheticalSecret, nextCandidate);

                            var hypoteticalCandidates
                                = GetCandidatesOnNextStep(
                                    candidatesToExplore, stepResult, precalculatedData, nextCandidate
                                ).ToList();

                            nextCandidatesCount += hypoteticalCandidates.Count;
                        }

                        if (nextCandidatesCount < minNextCandidates)
                        {
                            minNextCandidates = nextCandidatesCount;
                            candidate = nextCandidate;
                        }
                    }
                }
            }

            if (printResults)
                Console.WriteLine($"{Environment.NewLine}SOLVED: {candidate}");

            return turn;
        }

        private static string[] GetCandidates(
            string secretWord,
            List<string> attempts
        )
        {
            List<int> candidates = null;
            var allWords = System.IO.File.ReadAllText(WordsFilepath);
            var precalculatedData = new PrecalculatedData(allWords);
            foreach (var attempt in attempts)
            {
                var stepResult = new StepResult(secretWord, attempt);
                candidates = GetCandidatesOnNextStep(
                    candidates, stepResult, precalculatedData, attempt);
            }

            return candidates
                .Select(c => precalculatedData.Words[c])
                .ToArray();
        }

        private static List<int> GetCandidatesOnNextStep(
            List<int> candidates,
            StepResult stepResult,
            PrecalculatedData precalculatedData,
            string candidateWord)
        {
            // no loops for performance (not tested, so... premature optimization anti-pattern?)

            var candidatesOn2ndStepByChar0 = precalculatedData
                .FilterByCharResult(candidates, stepResult, candidateWord, 0);

            var candidatesOn2ndStepByChar1 = precalculatedData
                .FilterByCharResult(candidates, stepResult, candidateWord, 1);

            var candidatesOn2ndStepByChar2 = precalculatedData
                .FilterByCharResult(candidates, stepResult, candidateWord, 2);

            var candidatesOn2ndStepByChar3 = precalculatedData
                .FilterByCharResult(candidates, stepResult, candidateWord, 3);

            var candidatesOn2ndStepByChar4 = precalculatedData
                .FilterByCharResult(candidates, stepResult, candidateWord, 4);

            var candidatesCount = candidates?.Count ?? int.MaxValue;
            var countChar0 = candidatesOn2ndStepByChar0.Count;
            var countChar1 = candidatesOn2ndStepByChar1.Count;
            var countChar2 = candidatesOn2ndStepByChar2.Count;
            var countChar3 = candidatesOn2ndStepByChar3.Count;
            var countChar4 = candidatesOn2ndStepByChar4.Count;

            if (countChar0 == 0 || countChar1 == 0 || countChar2 == 0 || countChar3 == 0 || countChar4 == 0)
                return Enumerable.Empty<int>().ToList();

            var minCount = Math.Min(candidatesCount,
                Math.Min(countChar0,
                Math.Min(countChar1,
                Math.Min(countChar2,
                Math.Min(countChar3, countChar4))))
            );

            if (minCount == candidatesCount)
                return FilterStepCandidates(
                    candidates,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4).ToList();

            if (minCount == countChar0)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4,
                    candidates).ToList();

            else if (minCount == countChar1)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4,
                    candidates).ToList();

            else if (minCount == countChar2)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4,
                    candidates).ToList();

            else if (minCount == countChar3)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar4,
                    candidates).ToList();

            else // if (minCount == countChar4)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar4,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidates).ToList();
        }

        private static IEnumerable<int> FilterStepCandidates(
            List<int> candidateList0,
            List<int> candidateList1,
            List<int> candidateList2,
            List<int> candidateList3,
            List<int> candidateList4,
            List<int> candidateList5
        )
        {
            for (int i = 0; i < candidateList0.Count; i++)
            {
                var candidate = candidateList0[i];
                var isValidCandidate =
                    IsCandidateInList(candidate, candidateList1)
                    && IsCandidateInList(candidate, candidateList2)
                    && IsCandidateInList(candidate, candidateList3)
                    && IsCandidateInList(candidate, candidateList4)
                    && (candidateList5 == null || IsCandidateInList(candidate, candidateList5));

                if (isValidCandidate)
                    yield return candidate;
            }
        }

        private static void GetBestInitialWord(string arg)
        {
            var words = System.IO.File.ReadAllText(WordsFilepath);
            var precalculatedData = new PrecalculatedData(words, GetMaxCandidateCount(arg));
            Console.WriteLine("Data precalculated");

            var watch = Stopwatch.StartNew();
            var wordCount = precalculatedData.Words.Length;
            var candidateWords = precalculatedData.CandidateWords;
            var avgCandidatesOn2ndStepByWordIdx = new float[candidateWords.Count];
            for (int candidateIdx = 0; candidateIdx < candidateWords.Count; candidateIdx++)
            {
                for (int secretIdx = 0; secretIdx < wordCount; secretIdx++)
                {
                    var candidateWord = candidateWords[candidateIdx];
                    var secretWord = precalculatedData.Words[secretIdx];
                    if (candidateWord != secretWord)
                    {
                        var stepResult = new StepResult(secretWord, candidateWord);

                        avgCandidatesOn2ndStepByWordIdx[candidateIdx] +=
                            GetCandidatesOn2ndStep(stepResult, precalculatedData, candidateWord);
                    }
                }

                avgCandidatesOn2ndStepByWordIdx[candidateIdx] /= wordCount;
                var percentage = 100.0 * (candidateIdx + 1) / candidateWords.Count;
                var ellapsed = watch.ElapsedMilliseconds / 1000.0;
                Console.Write(
                    $"\r   {percentage:0.00000} %" +
                    $" {ellapsed:0.00000}s" +
                    $" ETA: {(100.0 - percentage) * ellapsed / (60.0 * percentage):0.000}min"
                );
            }

            Console.WriteLine("");

            avgCandidatesOn2ndStepByWordIdx
                .Select((avg, idx) => (idx, avg))
                .OrderByDescending(x => x.avg)
                .ToList()
                .ForEach(data => Console.WriteLine($"     {candidateWords[data.idx]}: {data.avg:00.000}"));

            Console.ReadLine();
        }

        private static int? GetMaxCandidateCount(string arg)
        {
            int maxCandidateCount;
            if (!int.TryParse(arg, out maxCandidateCount))
                return null;

            return maxCandidateCount;
        }

        private static int GetCandidatesOn2ndStep(
            StepResult stepResult,
            PrecalculatedData precalculatedData,
            string candidateWord)
        {
            // no loops for performance (not tested, so... premature optimization anti-pattern?)

            var candidatesOn2ndStepByChar0 = precalculatedData
                .FilterByCharResult(null, stepResult, candidateWord, 0);

            var candidatesOn2ndStepByChar1 = precalculatedData
                .FilterByCharResult(null, stepResult, candidateWord, 1);

            var candidatesOn2ndStepByChar2 = precalculatedData
                .FilterByCharResult(null, stepResult, candidateWord, 2);

            var candidatesOn2ndStepByChar3 = precalculatedData
                .FilterByCharResult(null, stepResult, candidateWord, 3);

            var candidatesOn2ndStepByChar4 = precalculatedData
                .FilterByCharResult(null, stepResult, candidateWord, 4);

            var countChar0 = candidatesOn2ndStepByChar0.Count;
            var countChar1 = candidatesOn2ndStepByChar1.Count;
            var countChar2 = candidatesOn2ndStepByChar2.Count;
            var countChar3 = candidatesOn2ndStepByChar3.Count;
            var countChar4 = candidatesOn2ndStepByChar4.Count;

            if (countChar0 == 0 || countChar1 == 0 || countChar2 == 0 || countChar3 == 0 || countChar4 == 0)
                return 0;

            var minCount = Math.Min(countChar0,
                Math.Min(countChar1,
                Math.Min(countChar2,
                Math.Min(countChar3, countChar4)))
            );

            if (minCount == countChar0)
                return FilterStepCandidatesCount(
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4);

            else if (minCount == countChar1)
                return FilterStepCandidatesCount(
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4);

            else if (minCount == countChar2)
                return FilterStepCandidatesCount(
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4);

            else if (minCount == countChar3)
                return FilterStepCandidatesCount(
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar4);

            else // if (minCount == countChar4)
                return FilterStepCandidatesCount(
                    candidatesOn2ndStepByChar4,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3);
        }

        private static int FilterStepCandidatesCount(
            List<int> candidateList0,
            List<int> candidateList1,
            List<int> candidateList2,
            List<int> candidateList3,
            List<int> candidateList4
        )
        {
            var total = 0;
            for (int i = 0; i < candidateList0.Count; i++)
            {
                var candidate = candidateList0[i];
                var isValidCandidate =
                    IsCandidateInList(candidate, candidateList1)
                    && IsCandidateInList(candidate, candidateList2)
                    && IsCandidateInList(candidate, candidateList3)
                    && IsCandidateInList(candidate, candidateList4);

                if (isValidCandidate)
                    total++;
            }
            return total;
        }

        private static bool IsCandidateInList(int candidate, List<int> candidateList)
        {
            var candidateListCount = candidateList.Count;
            var lowerBoundIdx = 0;
            var upperBoundIdx = candidateListCount;
            var currentIdx = candidateListCount / 2;
            while (true)
            {
                var currentValue = candidateList[currentIdx];
                if (candidate == currentValue)
                    return true;

                if (candidate < currentValue)
                {
                    if (currentIdx == 0 || candidate > candidateList[currentIdx - 1])
                        return false;

                    upperBoundIdx = Math.Min(upperBoundIdx, currentIdx);
                    currentIdx = (lowerBoundIdx + currentIdx) / 2;
                }
                else // if (candidate > midValue)
                {
                    if (currentIdx == (candidateListCount - 1) || candidate < candidateList[currentIdx + 1])
                        return false;

                    lowerBoundIdx = Math.Max(lowerBoundIdx, currentIdx);
                    currentIdx = (currentIdx + upperBoundIdx) / 2;
                }
            }
        }

        private static void PrintHelp()
            => Console.WriteLine(
                "Specify command:" + Environment.NewLine +
                " > WordleSolver.exe [ES|EN] best-first-word [MAX_CANDIDATE_COUNT (optional)] " + Environment.NewLine +
                " > WordleSolver.exe [ES|EN] solve [SECRET_WORD] [FIRST_WORD]" + Environment.NewLine +
                " > WordleSolver.exe [ES|EN] candidates [SECRET_WORD] [ [FIRST_WORD], [SECOND_WORD], ... ]" + Environment.NewLine
            );
    }
}
