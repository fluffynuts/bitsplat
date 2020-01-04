namespace bitsplat.Pipes
{
    public class SimplePercentageConsoleProgressReporter 
        : SimpleConsoleProgressReporter
    {
        public SimplePercentageConsoleProgressReporter(IMessageWriter messageWriter)
            : base(messageWriter)
        {
        }

        protected override void ReportIntermediateState(string label, int percentageComplete)
        {
            Rewrite(label, $"[ {percentageComplete,2} ]");
        }
    }
}