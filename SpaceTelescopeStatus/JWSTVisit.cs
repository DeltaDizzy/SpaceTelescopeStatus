using System.Runtime.InteropServices;

internal class JWSTVisit
{
    public string VisitID { get; private set; } = "none";
    public string PCSMode { get; private set; } = "none";
    public string VisitType { get; private set; } = "none";
    public DateTime ScheduledStartTime { get; private set; } = DateTime.MinValue;
    public TimeSpan Duration { get; private set; } = TimeSpan.Zero;
    public string InstrumentMode { get; private set; } = "none";
    public string TargetName { get; private set; } = "none";
    public string Category { get; private set; } = "none";
    public string Keywords { get; private set; } = "none";

    public JWSTVisit(string visitInfo)
    {
        string[] chunks = new string[9];
        List<string> splits = visitInfo.Split("  ").ToList();
        splits.RemoveAll(s => s == "");
        for (int i = 0; i < splits.Count; i++)
        {
            chunks[i] = splits[i];
        }

        if (chunks[0] is not null or " ")
        {
            VisitID = chunks[0];
        }
        if (chunks[1] is not null or " ") 
        {
            PCSMode = chunks[1];
        }
        if (chunks[2] is not null or " ")
        {
            VisitType = chunks[2];
        }
        if (chunks[3] is not null or " ")
        {
            if (chunks[3].Contains("PRIME"))
            {
                // attched to prime baby
                ScheduledStartTime = DateTime.MaxValue;
            }
            else
            {
                ScheduledStartTime = DateTime.Parse(chunks[3]);
            }
        }
        if (chunks[4] is not null or " ")
        {
            // remove "00/"

            try
            {
                Duration = TimeSpan.Parse(chunks[4].Substring(3));
            }
            catch (Exception)
            {
                Duration = TimeSpan.Zero;
                Console.WriteLine("DURATION PARSE INVALID");
            }
        }
        if (chunks[5] is not null or " ")
        {
            InstrumentMode = chunks[5];
        }
        if (chunks[6] is not null or " ")
        {
            TargetName = chunks[6];
        }
        if (chunks[7] is not null or " ")
        {
            Category = chunks[7];
        }
        if (chunks[8] is not null or " ")
        {
            Keywords = chunks[8];
        }
    }
}