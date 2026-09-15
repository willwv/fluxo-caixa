using FluxoCaixa.Consolidado.Application.Interfaces;
using FluxoCaixa.Consolidado.Infrastructure.Cache;
using FluxoCaixa.Consolidado.Infrastructure.Health;
using FluxoCaixa.Consolidado.Infrastructure.Messaging;
using FluxoCaixa.Consolidado.Infrastructure.Persistence;
using FluxoCaixa.Consolidado.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FluxoCaixa.Consolidado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConsolidadoWriteDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("ConsolidadoPrimaryDb")
                ?? throw new InvalidOperationException("Connection string 'ConsolidadoPrimaryDb' não configurada."))
            .UseSnakeCaseNamingConvention());

        services.AddDbContext<ConsolidadoReadDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("ConsolidadoReplicaDb")
                ?? throw new InvalidOperationException("Connection string 'ConsolidadoReplicaDb' não configurada."))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ISaldoDiarioWriteRepository, SaldoDiarioWriteRepository>();
        services.AddScoped<ISaldoDiarioReadRepository, SaldoDiarioReadRepository>();

        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configurationOptions = ConfigurationOptions.Parse(sp.GetRequiredService<IOptions<CacheOptions>>().Value.ConnectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.BacklogPolicy = BacklogPolicy.FailFast;
            configurationOptions.ConnectTimeout = 1000;
            configurationOptions.ConnectRetry = 1;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });
        services.AddSingleton<IRedisCache, RedisCache>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<LancamentoRegistradoConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.ReceiveEndpoint("consolidado-lancamento-registrado", e =>
                {
                    e.ConfigureConsumer<LancamentoRegistradoConsumer>(context);
                });
            });
        });

        services.AddHealthChecks()
            .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("ConsolidadoPrimaryDb")!, name: "consolidado-db-primary")
            .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("ConsolidadoReplicaDb")!, name: "consolidado-db-replica")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq")
            .AddCheck<RedisHealthCheck>("redis");

        return services;
    }
}
