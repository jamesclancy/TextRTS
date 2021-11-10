using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRTS.Contracts.Extensions
{
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
