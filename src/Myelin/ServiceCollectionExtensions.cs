using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnambitiousFx.Myelin;

/// <summary>Registers Myelin in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <see cref="SafeHttpClient" /> as a singleton, with <see cref="DnsHostResolver" /> unless an
    ///     <see cref="IHostResolver" /> is already registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the default <see cref="MyelinOptions" />; bind them from configuration instead if you prefer.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMyelin(this IServiceCollection services, Action<MyelinOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<MyelinOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IHostResolver, DnsHostResolver>();
        services.TryAddSingleton(sp => new SafeHttpClient(
            new HttpMessageInvoker(MyelinHandler.Create(sp.GetRequiredService<IHostResolver>())),
            sp.GetRequiredService<IOptions<MyelinOptions>>().Value,
            sp.GetRequiredService<ILogger<SafeHttpClient>>()));
        return services;
    }

    /// <summary>
    ///     Makes a client built by <c>IHttpClientFactory</c> SSRF-safe: its primary handler becomes
    ///     <see cref="MyelinHandler.Create" /> (public addresses only, no automatic redirect, no proxy), and every request
    ///     is checked for HTTPS, credentials and the allow-list. A refusal throws <see cref="OutboundRefusedException" />.
    /// </summary>
    /// <param name="builder">The client builder from <c>AddHttpClient</c>.</param>
    /// <param name="configure">Configures the <see cref="MyelinOptions" /> of this client only; <c>Timeout</c> and <c>MaxRedirects</c> do not apply (use <see cref="HttpClient.Timeout" />; redirects are returned, not followed).</param>
    /// <returns>The client builder.</returns>
    public static IHttpClientBuilder UseMyelin(this IHttpClientBuilder builder, Action<MyelinOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (configure is not null)
        {
            builder.Services.Configure(builder.Name, configure);
        }

        builder.Services.TryAddSingleton<IHostResolver, DnsHostResolver>();
        var name = builder.Name;
        builder.ConfigurePrimaryHttpMessageHandler(sp => MyelinHandler.Create(sp.GetRequiredService<IHostResolver>()));
        builder.AddHttpMessageHandler(sp => new MyelinPolicyHandler(sp.GetRequiredService<IOptionsMonitor<MyelinOptions>>(), name));
        return builder;
    }
}
