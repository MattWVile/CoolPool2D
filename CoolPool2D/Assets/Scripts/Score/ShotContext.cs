using System.Collections.Generic;
using System.Linq; 
public class ShotContext
{
    public List<ShotEvent> Events = new List<ShotEvent>();
    public void Record(ShotEvent shotEvent) => Events.Add(shotEvent);
    public void Reset() => Events.Clear();

    // convenience
    public int CountKisses() => Events.Count(shotEvent => shotEvent.Type == ShotEventType.Kiss);
    public int CountObjRails() => Events.Count(shotEvent => shotEvent.Type == ShotEventType.ObjRail);
    public int CountCueRails() => Events.Count(shotEvent => shotEvent.Type == ShotEventType.CueRail);
    public int CountPots() => Events.Count(shotEvent => shotEvent.Type == ShotEventType.Pot);
}

public class ShotEvent
{
    public ShotEventType Type;
    public BallData Ball;
    public float Time; // optional timestamp
}

public enum ShotEventType { Kiss, ObjRail, CueRail, Pot }

