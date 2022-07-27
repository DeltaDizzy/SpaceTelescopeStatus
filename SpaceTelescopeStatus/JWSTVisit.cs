using System.Runtime.InteropServices;

public class JWSTVisit
{
    public string VisitID { get; set; } = "none";
    public string PCSMode { get; set; } = "none";
    public string VisitType { get; set; } = "none";
    public DateTime ScheduledStartTime { get; set; } = DateTime.MinValue;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public string InstrumentMode { get; set; } = "none";
    public string TargetName { get; set; } = "none";
    public string Category { get; set; } = "none";
    public string Keywords { get; set; } = "none";
    public bool VisitOngoing { get; set; } = true;

    public JWSTVisit()
    {
        
    }

    public JWSTVisit(bool visitActive)
    {
        VisitOngoing = visitActive;
    }
}