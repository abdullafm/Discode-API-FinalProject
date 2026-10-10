namespace Service.Helpers.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
            Errors = new[] { message };
        }

        public BadRequestException(string[] errors) : base(errors.Length > 0 ? errors[0] : "The request is not valid.")
        {
            Errors = errors.Length > 0 ? errors : new[] { "The request is not valid." };
        }

        public string[] Errors { get; }
    }
}
