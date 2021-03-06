using System.Linq;

namespace WordleSolver
{
    enum CharResult
    {
        NOT_IN_WORD,
        IN_WORD_WRONG_POSITION,
        IN_WORD_IN_POSITION
    }

    class StepResult
    {
        public CharResult[] Result { get; private set; }

        public StepResult(string secret, string candidate)
        {
            Result = new CharResult[]
            {
                GetStepCharResult(secret, candidate, 0),
                GetStepCharResult(secret, candidate, 1),
                GetStepCharResult(secret, candidate, 2),
                GetStepCharResult(secret, candidate, 3),
                GetStepCharResult(secret, candidate, 4)
            };
        }

        public bool IsWin() => Result.Count(r => r == CharResult.IN_WORD_IN_POSITION) == 5;

        public override string ToString()
            => string.Join(string.Empty, Result.Select(ToString));

        private static string ToString(CharResult charResult)
        {
            switch (charResult)
            {
                case CharResult.NOT_IN_WORD:
                    return "X";
                case CharResult.IN_WORD_WRONG_POSITION:
                    return "?";
                case CharResult.IN_WORD_IN_POSITION:
                    return "√";
                default:
                    return "E";
            }
        }

        private CharResult GetStepCharResult(string secret, string candidate, int idx)
        {
            var candidateChar = candidate[idx];
            if (candidateChar == secret[idx])
                return CharResult.IN_WORD_IN_POSITION;

            var charOccurrencesInSecret = 0;
            var charOccurrencesRevealed = 0;
            for (var jdx = 0; jdx < 5; jdx++)
            {
                if (secret[jdx] == candidateChar)
                    charOccurrencesInSecret++;

                if (idx != jdx
                    && candidate[jdx] == candidateChar
                    && (secret[jdx] == candidateChar || jdx < idx)
                )
                    charOccurrencesRevealed++;
            }

            if (charOccurrencesInSecret > charOccurrencesRevealed)
                return CharResult.IN_WORD_WRONG_POSITION;

            return CharResult.NOT_IN_WORD;
        }
    }
}
