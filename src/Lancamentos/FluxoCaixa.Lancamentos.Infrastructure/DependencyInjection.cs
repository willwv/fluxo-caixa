using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Infrastructure.Auth;
using FluxoCaixa.Lancamentos.Infrastructure.Health;
using FluxoCaixa.Lancamentos.Infrastructure.Messaging;
using FluxoCaixa.Lancamentos.Infrastructure.Outbox;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;
using FluxoCaixa.Lancamentos.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluxoCaixa.Lancamentos.Infrastructure;

public static class DependencyInjection
{
    // Nota: toda leitura de configuração aqui é feita via IServiceProvider/IOptions dentro dos
    // callbacks (lazy), nunca capturada em variável local antes de devolver `services`. Ler a
    // config antecipadamente quebra o WebApplicationFactory em apps com hosting mínimo: os
    // overrides de configuração dos testes só são mesclados no builder pouco antes do Build(),
    // depois que este método já teria rodado - uma variável capturada ficaria com o valor antigo.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LancamentosDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("LancamentosDb")
                ?? throw new InvalidOperationException("Connection string 'LancamentosDb' não configurada."))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PasswordHashOptions>(configuration.GetSection(PasswordHashOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });
            });
        });

        services.AddHostedService<OutboxPublisherService>();

        services.AddHealthChecks()
            .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("LancamentosDb")!, name: "lancamentos-db")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}
