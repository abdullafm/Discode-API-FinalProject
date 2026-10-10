namespace Service.Helpers.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base("The requested item was not found.")
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }
    }
}
