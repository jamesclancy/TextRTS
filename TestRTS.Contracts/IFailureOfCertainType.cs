namespace TestRTS.Contracts
{
    public interface IFailureOfCertainType<TFailure>
    {
        public bool IsFailure { get; }
        public TFailure AsFailure { get; }
    }
}