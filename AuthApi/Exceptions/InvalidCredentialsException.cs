namespace AuthApi.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException()
          : base("Username or password is incorrect.") { }
    }
}
