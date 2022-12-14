using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Visualizer.API.Services.Config;
using Visualizer.API.Services.Services;
using Visualizer.API.Services.Services.Impl;

namespace Visualizer.API.Services;

public static class VisualizerServicesRegistrator
{
    public static void Register(WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddIngestionServiceProxy();
        webApplicationBuilder.AddRedisServerAndConfigurator();

        ConnectionMultiplexer.SetFeatureFlag("preventthreadtheft", true);
        webApplicationBuilder.AddRedisOMConnectionProvider();
        webApplicationBuilder.AddRedisGraph();
        webApplicationBuilder.AddHashtagService();

        // Query
        webApplicationBuilder.Services.AddScoped<ITweetDbQueryService, TweetDbQueryService>();
        webApplicationBuilder.Services.AddScoped<ITweetGraphService, TweetGraphService>();
    }
}
