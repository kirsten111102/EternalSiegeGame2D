using System;

[Serializable]
public class CloudSaveRequest
{
    public int schemaVersion = 1;
    public long expectedRevision;
    public SaveData saveData;
}

[Serializable]
public class CloudSaveResponse
{
    public int schemaVersion;
    public long revision;
    public SaveData saveData;
    public bool isCompleted;
}

[Serializable]
public class SaveAcknowledgement
{
    public long revision;
}
