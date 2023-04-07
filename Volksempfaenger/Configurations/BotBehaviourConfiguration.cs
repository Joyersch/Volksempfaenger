namespace Volksempfaenger.Configurations;

public class BotBehaviourConfiguration
{
    public AudioConfiguration Audio { get; set; }
    public class AudioConfiguration
    {
        public bool JoinOnMove { get; set; }
    }
}