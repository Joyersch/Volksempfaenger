namespace Volksempfaenger.Configurations;

public class BotBehaviourConfiguration
{
    public AudioConfiguration Audio { get; set; }
    public DebugConfiguration Debug { get; set; }

    public class AudioConfiguration
    {
        public bool JoinOnMove { get; set; }
    }

    public class DebugConfiguration
    {
        public bool DisableLeave { get; set; }
    }
}