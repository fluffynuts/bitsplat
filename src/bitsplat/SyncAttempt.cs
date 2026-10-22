namespace bitsplat
{
    public class SyncAttempt
    {
        public int Number { get; }

        /// <summary>
        /// null when retrying forever
        /// </summary>
        public int? MaxAttempts { get; }

        public SyncAttempt(int number, int? maxAttempts)
        {
            Number = number;
            MaxAttempts = maxAttempts;
        }

        public override string ToString()
        {
            return MaxAttempts is null
                       ? $"attempt {Number} (retrying forever)"
                       : $"attempt {Number} of {MaxAttempts}";
        }
    }
}
