using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Authorization;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using System.Security.Claims;
using System.Text;

namespace LinkTMS.ApiGateway.Helpers
{
    public static class StartupHelper
    {
        public static void AddAuthenticationConfig(this IServiceCollection services, IConfiguration configuration)
        {
            
            var authorityUrl = configuration["Auth0:Domain"];
            var audience = configuration["Auth0:Audience"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = authorityUrl;                
                options.Audience = audience;
                
            });
        }

        public static void AddOcelotServices(this IServiceCollection services)
        {
            var ocelotBuilder = services.AddOcelot();

            // aggregation
            //ocelotBuilder.AddSingletonDefinedAggregator<FinanceAggregator>();

            // caching
            ocelotBuilder.AddCacheManager(x =>
            {
                x.WithDictionaryHandle();
            });

        }


        public static IServiceCollection DecorateClaimAuthoriser(this IServiceCollection services)
        {
            var serviceDescriptor = services.First(x => x.ServiceType == typeof(IClaimsAuthorizer));
            services.Remove(serviceDescriptor);

            var newServiceDescriptor = new ServiceDescriptor(serviceDescriptor.ImplementationType, serviceDescriptor.ImplementationType, serviceDescriptor.Lifetime);
            services.Add(newServiceDescriptor);

            services.AddTransient<IClaimsAuthorizer, ClaimAuthorizerDecorator>();

            return services;
        }

    }


    public class ClaimAuthorizerDecorator : IClaimsAuthorizer
    {
        private readonly ClaimsAuthorizer _authoriser;

        public ClaimAuthorizerDecorator(ClaimsAuthorizer authoriser)
        {
            _authoriser = authoriser;
        }

        public Response<bool> Authorize(ClaimsPrincipal claimsPrincipal, Dictionary<string, string> routeClaimsRequirement, List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
        {
            var newRouteClaimsRequirement = new Dictionary<string, string>();
            foreach (var kvp in routeClaimsRequirement)
            {
                if (kvp.Key.StartsWith("http$//"))
                {
                    var key = kvp.Key.Replace("http$//", "http://");
                    newRouteClaimsRequirement.Add(key, kvp.Value);
                }
                else
                {
                    newRouteClaimsRequirement.Add(kvp.Key, kvp.Value);
                }
            }

            return _authoriser.Authorize(claimsPrincipal, newRouteClaimsRequirement, urlPathPlaceholderNameAndValues);
        }

    }
}
