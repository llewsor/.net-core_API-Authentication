﻿namespace AuthApi.Exceptions
{
    public class RefreshTokenExpiredException : Exception
    {
        public RefreshTokenExpiredException()
          : base("Refresh token has expired.") { }
    }
}
