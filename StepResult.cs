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

        private CharResult GetStepCharResult(string secret, string candidate, int idx)
        {
            if (candidate[idx] == secret[idx])
                return CharResult.IN_WORD_IN_POSITION;

            if (secret.Contains(candidate[idx]))
                return CharResult.IN_WORD_WRONG_POSITION;

            return CharResult.NOT_IN_WORD;
        }
    }
}
