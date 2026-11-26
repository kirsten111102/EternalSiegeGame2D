using System;

[Serializable]
public class ScoreRequest
{
    public long playerId;
    public string runId;
    public int level;
    public int score;
}