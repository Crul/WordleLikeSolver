﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WordleSolver
{
    class PrecalculatedData
    {
        private const string CHARS = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

        public string[] Words { get; set; }
        public List<string> CandidateWords { get; set; }
        public Dictionary<char, List<int>> WordIdsByForbiddenChar { get; }
        public Dictionary<char, List<int>>[] WordIdsByInPosForcedCharByPosition { get; }
        public Dictionary<char, List<int>>[] WordIdsByNotInPosForcedCharByPosition { get; }
        private Dictionary<char, int> CharactersCount { get; }

        public PrecalculatedData(string words, int? maxCandidateCount = null)
        {
            Words = words.Split(" ");

            CharactersCount = CHARS.ToDictionary(
                character => character,
                character => words.Count(ch => ch == character)
            );

            if (maxCandidateCount.HasValue)
            {
                Console.WriteLine($"Searghing only in top {maxCandidateCount.Value}");
                CandidateWords = FilterBestTopCandidates(Words, maxCandidateCount.Value);
            }
            else
                CandidateWords = Words
                    .Where(word => word.Distinct().Count() == 5)
                    .ToList();

            WordIdsByForbiddenChar = new Dictionary<char, List<int>>();
            foreach (var character in CHARS)
                WordIdsByForbiddenChar[character]
                    = CreateFilteredWordIdList(word => !word.Contains(character));

            WordIdsByInPosForcedCharByPosition =
                GetArrayOfFilteredWordIdListByPosition(
                    (idx, word, character) => word[idx] == character
                );

            WordIdsByNotInPosForcedCharByPosition =
                GetArrayOfFilteredWordIdListByPosition(
                    (idx, word, character) => word[idx] != character && word.Contains(character)
                );
        }

        private List<string> FilterBestTopCandidates(string[] candidates, int limit)
            => candidates
                .Where(word => word.Distinct().Count() == 5)
                .Select(word => (word, score: word.Sum(ch => CharactersCount[ch])))
                .OrderByDescending(data => data.score)
                .Take(limit)
                .Select(data => data.word)
                .ToList();

        public List<int> FilterByCharResult(StepResult stepResult, string candidateWord, int idx)
        {
            switch (stepResult.Result[idx])
            {
                case CharResult.NOT_IN_WORD:
                    return WordIdsByForbiddenChar[candidateWord[idx]];

                case CharResult.IN_WORD_WRONG_POSITION:
                    return WordIdsByNotInPosForcedCharByPosition[idx][candidateWord[idx]];

                case CharResult.IN_WORD_IN_POSITION:
                    return WordIdsByInPosForcedCharByPosition[idx][candidateWord[idx]];
            }

            return null;
        }

        private Dictionary<char, List<int>>[] GetArrayOfFilteredWordIdListByPosition(
            Func<int, string, char, bool> filter)
        {
            var dict = new Dictionary<char, List<int>>[5];
            for (var i = 0; i < 5; i++)
            {
                var filteredWordIdsByChar = new Dictionary<char, List<int>>();
                foreach (var character in CHARS)
                    filteredWordIdsByChar[character]
                        = CreateFilteredWordIdList(word => filter(i, word, character));

                dict[i] = filteredWordIdsByChar;
            }

            return dict;
        }

        private List<int> CreateFilteredWordIdList(Func<string, bool> filter)
            => Words
                .Select((item, index) => (item, index))
                .Where(data => filter(data.item))
                .Select(data => data.index)
                .ToList();
    }
}
