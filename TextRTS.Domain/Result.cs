namespace TextRTS.Domain
{
    public abstract record Result<TSuccess, TFailure>
    {
        public bool IsSuccess => this is Success;
        public bool IsFailure => this is Failure;


        public TSuccess? AsSuccess => this as Success;
        public TFailure? AsFailure => this as Failure;


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