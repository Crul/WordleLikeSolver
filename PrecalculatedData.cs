using System;
using System.Collections.Generic;
using System.Linq;

namespace WordleSolver
{
    class PrecalculatedData
    {
        private const string CHARS = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";
        private readonly string[] Words;

        public Dictionary<char, List<int>> WordIdsByForbiddenChar { get; private set; }
        public Dictionary<char, List<int>>[] WordIdsByInPosForcedCharByPosition { get; private set; }
        public Dictionary<char, List<int>>[] WordIdsByNotInPosForcedCharByPosition { get; private set; }

        public PrecalculatedData(string[] words)
        {
            Words = words;

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
