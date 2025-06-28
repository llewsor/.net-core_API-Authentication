namespace AuthApi.Exceptions
{
    public class UserBlockedException : Exception
    {
        public UserBlockedException()
            : base("User is blocked. Please contact support for assistance.") { }
    }
}
