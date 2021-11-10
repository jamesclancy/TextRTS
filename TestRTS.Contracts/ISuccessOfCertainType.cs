namespace TestRTS.Contracts
{
    public interface ISuccessOfCertainType<TSuccess>
    {
        public bool IsSuccess { get; }
        public TSuccess AsSuccess { get; }
    }
}