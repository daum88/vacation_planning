namespace VacationRequestApi.Common
{
    /// <summary>
    /// Generic result pattern for operation outcomes
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string? ErrorMessage { get; }
        public List<string>? Errors { get; }

        private Result(bool isSuccess, T? data, string? errorMessage, List<string>? errors)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            Errors = errors;
        }

        public static Result<T> Success(T data) => new(true, data, null, null);
        
        public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage, null);
        
        public static Result<T> Failure(List<string> errors) => new(false, default, null, errors);
    }

    /// <summary>
    /// Non-generic result for operations without return value
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public List<string>? Errors { get; }

        private Result(bool isSuccess, string? errorMessage, List<string>? errors)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Errors = errors;
        }

        public static Result Success() => new(true, null, null);
        
        public static Result Failure(string errorMessage) => new(false, errorMessage, null);
        
        public static Result Failure(List<string> errors) => new(false, null, errors);
    }
}
