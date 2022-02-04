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

            var candidateStartIdx = 0;
            var candidateEndIdx = WORD_COUNT;
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
            var avgCandidatesOn2ndStepByWordIdx = new float[WORD_COUNT];
            for (int candidateIdx = candidateStartIdx; candidateIdx < candidateEndIdx; candidateIdx++)
            {
                for (int secretIdx = 0; secretIdx < WORD_COUNT; secretIdx++)
                {
                    var candidateWord = WORDS[candidateIdx];
                    var secretWord = WORDS[secretIdx];
                    if (candidateWord != secretWord)
                    {
                        var stepResult = new StepResult(secretWord, candidateWord);

                        avgCandidatesOn2ndStepByWordIdx[candidateIdx] +=
                            GetCandidatesOn2ndStep(stepResult, precalculatedData, candidateWord).Count;
                    }
                }

                avgCandidatesOn2ndStepByWordIdx[candidateIdx] /= WORD_COUNT;
                var percentage = 100.0 * (candidateIdx - candidateStartIdx + 1) / candidateEndIdx;
                var ellapsed = watch.ElapsedMilliseconds / 1000.0;
                Console.Write(
                    $"\r   {percentage:0.00000}" +
                    $" {ellapsed:0.00000}s" +
                    $" ETA: {100.0 * ellapsed / (3600.0 * percentage):0.000}h"
                );
            }

            Console.WriteLine("");

            avgCandidatesOn2ndStepByWordIdx
                .Select((avg, idx) => (idx, avg))
                .OrderByDescending(x => x.avg)
                .ToList()
                .ForEach(data => Console.WriteLine($"     {WORDS[data.idx]}: {data.avg:00.000}"));

            Console.ReadLine();
        }

        private static List<int> GetCandidatesOn2ndStep(
            StepResult stepResult,
            PrecalculatedData precalculatedData,
            string candidateWord)
        {
            var candidatesOn2ndStep = precalculatedData
                .FilterByCharResult(stepResult, candidateWord, 0)
                .AsEnumerable();

            for (var i = 1; i < 5; i++) // starts at 1 (in the 1st step an Intersect with all would be inefficient
                candidatesOn2ndStep = candidatesOn2ndStep
                    .Intersect(
                        precalculatedData.FilterByCharResult(stepResult, candidateWord, i)
                    );

            return candidatesOn2ndStep.ToList();
        }
    }
}
