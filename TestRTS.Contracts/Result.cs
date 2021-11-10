namespace TestRTS.Contracts
{
    public abstract record Result<TSuccess, TFailure> : IFailureOfCertainType<TFailure>, ISuccessOfCertainType<TSuccess>
    {
        public bool IsSuccess => this is Success;
        public bool IsFailure => this is Failure;

        public TSuccess AsSuccess
        {
            get
            {
                var suc = this as Success;
                if (suc == null) throw new InvalidOperationException();
                return suc;
            }
        }

        public TFailure AsFailure
        {
            get
            {
                var failure = this as Failure;
                if (failure == null) throw new InvalidOperationException();
                return failure;
            }
        }

        public record Success(TSuccess Value) : Result<TSuccess, TFailure>
        {
            public override string ToString() => $"SUCCESS: {Value?.ToString()}";
            public static implicit operator TSuccess(Success success) => success.Value;
        }

        public record Failure(TFailure Value) : Result<TSuccess, TFailure>
        {
            public override string ToString() => $"FAILURE: {Value?.ToString()}";
            public static implicit operator TFailure(Failure failure) => failure.Value;
        }
    }
}