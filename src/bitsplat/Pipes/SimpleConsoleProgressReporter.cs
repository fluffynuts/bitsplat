namespace bitsplat.Pipes
{
    public class SimpleProgressReporter: IProgressReporter
    {
        public void NotifyCurrent(
            string label, 
            int percentComplete)
        {
            if (percentComplete == 0)
            {
            }
            else if (percentComplete == 100)
            {
            }
        }
        
        public void NotifyOverall(
            int current, 
            int total)
        {
        }
    }
}