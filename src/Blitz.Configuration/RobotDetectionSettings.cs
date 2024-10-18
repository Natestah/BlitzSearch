namespace Blitz;

public class RobotDetectionSettings
{
    public double MaxFileSizeInMB { get; set; } = 3.0;
    public int MaxLineSizeChars { get; set; } = 30000;

    public bool BehaviorDefer { get; set; } = false;
    public bool BehaviorSkipAndReport { get; set; } = true;
    public bool BehaviorIgnoreRobotFiles { get; set; } = false;
}

