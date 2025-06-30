using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AuthApi.IntegrationTests
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "TestScheme";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Items.TryGetValue("TestUser", out var testUserObject) && testUserObject is ClaimsPrincipal testUser)
            {
                var ticket = new AuthenticationTicket(testUser, AuthenticationScheme);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            // If no test user is set, return no result.
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }

}
