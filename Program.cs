using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WordleSolver
{
    class Program
    {
        private const string WORDS_FILEPATH = @"C:\workspace\wordle\words.txt";

        private static string[] WORDS = null;
        private static int WORD_COUNT = 0;

        static void Main(string[] args)
        {
            GetBestInitialWord(args);
        }

        private static void GetBestInitialWord(string[] args)
        {
            WORDS = System.IO.File.ReadAllText(WORDS_FILEPATH).Split(" ");
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // var rnd = new Random();
            // WORDS = WORDS.OrderBy(item => rnd.Next()).Take(500).ToArray();
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            WORD_COUNT = WORDS.Length;
            var candidateWords = WORDS.Where(word => word.Distinct().Count() == 5).ToList();

            var candidateStartIdx = 0;
            var candidateEndIdx = candidateWords.Count;
            if (args.Length == 2)
            {
                var chunks = int.Parse(args[0]);
                var chunkIdx = int.Parse(args[1]);
                var wordsPerChunk = WORD_COUNT / chunks;
                candidateStartIdx = chunkIdx * wordsPerChunk;
                candidateEndIdx = candidateStartIdx + wordsPerChunk;
                Console.WriteLine($"Interval: {candidateStartIdx} {candidateEndIdx}");
            }

            var precalculatedData = new PrecalculatedData(WORDS);
            Console.WriteLine("Data precalculated");

            var watch = Stopwatch.StartNew();
            var avgCandidatesOn2ndStepByWordIdx = new float[candidateWords.Count];
            for (int candidateIdx = candidateStartIdx; candidateIdx < candidateEndIdx; candidateIdx++)
            {
                for (int secretIdx = 0; secretIdx < WORD_COUNT; secretIdx++)
                {
                    var candidateWord = candidateWords[candidateIdx];
                    var secretWord = WORDS[secretIdx];
                    if (candidateWord != secretWord)
                    {
                        var stepResult = new StepResult(secretWord, candidateWord);

                        avgCandidatesOn2ndStepByWordIdx[candidateIdx] +=
                            GetCandidatesOn2ndStep(stepResult, precalculatedData, candidateWord);
                    }
                }

                avgCandidatesOn2ndStepByWordIdx[candidateIdx] /= WORD_COUNT;
                var percentage = 100.0 * (candidateIdx - candidateStartIdx + 1) / candidateEndIdx;
                var ellapsed = watch.ElapsedMilliseconds / 1000.0;
                Console.Write(
                    $"\r   {percentage:0.00000}" +
                    $" {ellapsed:0.00000}s" +
                    $" ETA: {100.0 * ellapsed / (60.0 * percentage):0.000}min"
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

        private static int GetCandidatesOn2ndStep(
            StepResult stepResult,
            PrecalculatedData precalculatedData,
            string candidateWord)
        {
            // no loops for performance (not tested, so... premature optimization anti-pattern?)

            var candidatesOn2ndStepByChar0 = precalculatedData
                .FilterByCharResult(stepResult, candidateWord, 0);

            var candidatesOn2ndStepByChar1 = precalculatedData
                .FilterByCharResult(stepResult, candidateWord, 1);

            var candidatesOn2ndStepByChar2 = precalculatedData
                .FilterByCharResult(stepResult, candidateWord, 2);

            var candidatesOn2ndStepByChar3 = precalculatedData
                .FilterByCharResult(stepResult, candidateWord, 3);

            var candidatesOn2ndStepByChar4 = precalculatedData
                .FilterByCharResult(stepResult, candidateWord, 4);

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
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4);

            else if (minCount == countChar1)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4);

            else if (minCount == countChar2)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar4);

            else if (minCount == countChar3)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar3,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar4);

            else // if (minCount == countChar4)
                return FilterStepCandidates(
                    candidatesOn2ndStepByChar4,
                    candidatesOn2ndStepByChar0,
                    candidatesOn2ndStepByChar1,
                    candidatesOn2ndStepByChar2,
                    candidatesOn2ndStepByChar3);
        }

        private static int FilterStepCandidates(
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
    }
}
