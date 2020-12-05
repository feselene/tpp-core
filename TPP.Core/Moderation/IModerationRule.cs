namespace TPP.Core.Moderation
{
    public interface IModerationRule
    {
        public RuleResult Check(Message message);
    }

    public abstract class RuleResult
    {
        private RuleResult()
        {
        }

        public sealed class Nothing : RuleResult
        {
        }

        public sealed class DeleteMessage : RuleResult
        {
        }

        public sealed class GivePoints : RuleResult
        {
            public int Points { get; }
            public GivePoints(int points) => Points = points;
        }

        public sealed class Timeout : RuleResult
        {
            public string Message { get; }
            public Timeout(string message) => Message = message;
        }
    }
}
