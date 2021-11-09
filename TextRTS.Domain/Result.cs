namespace TextRTS.Domain
{
    public abstract record Result<TSuccess, TFailure> : IFailureOfCertainType<TFailure>, ISuccessOfCertainType<TSuccess>
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

    public interface IFailureOfCertainType<TFailure>
    {
        public bool IsFailure { get; }
        public TFailure? AsFailure { get; }
    }

    public interface ISuccessOfCertainType<TSuccess>
    {
        public bool IsSuccess { get; }
        public TSuccess? AsSuccess { get; }
    }

    public static class ResultExtensions
    {
        public static Result<TSuccess2, TFailure> Bind<TSuccess, TSuccess2, TFailure>(this Result<TSuccess, TFailure> result, Func<TSuccess, Result<TSuccess2, TFailure>> func)
        {
            if (result.IsFailure)
                return new Result<TSuccess2, TFailure>.Failure(result.AsFailure);
            return func.Invoke(result.AsSuccess);
        }


        public static (bool anyFailures, IEnumerable<J> listOfFailureMessages) GetAllFailures<J>(params IFailureOfCertainType<J>[] potentiallyFailingOperations)
        {
            List<J> listOfFailureMessages = new List<J>();

            foreach (var failure in potentiallyFailingOperations.Where(x => x.IsFailure))
                listOfFailureMessages.Add(failure.AsFailure);

            return (listOfFailureMessages.Any(), listOfFailureMessages);
        }
    }
}