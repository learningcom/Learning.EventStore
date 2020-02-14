using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Learning.Cqrs;
using Learning.EventStore.Cache;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.DataStores;
using Learning.EventStore.Domain;
using Learning.MessageQueue;
using Learning.MessageQueue.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Learning.EventStore.Sample.Web
{
    public class Startup
    {
        private readonly Tuple<Type, Type>[] _allTypes;

        public Startup(IConfiguration configuration)
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            var assemblies = entryAssembly
                .GetReferencedAssemblies()
                .Select(library => Assembly.Load(new AssemblyName(library.Name)))
                .ToList();

            assemblies.Add(entryAssembly);

            _allTypes = assemblies
                .ToList()
                .SelectMany(x => x.GetTypes())
                .Select(x => Tuple.Create(x, x.GetInterfaces().FirstOrDefault()))
                .ToArray();

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Cqrs services
            LoadQueryHandler(typeof(IQueryHandler<,>), services);
            LoadQueryHandler(typeof(IAsyncQueryHandler<,>), services);
            LoadCommandHandler(typeof(ICommandHandler<>), services);
            LoadCommandHandler(typeof(IAsyncCommandHandler<>), services);
            services.AddScoped<IHub>(c => new Hub(c.GetService));

            // Configure Redis Connection
            var redisConfigOptions = ConfigurationOptions.Parse("localhost:6379");
            redisConfigOptions.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfigOptions));

            // Set up EventStore
            var keyPrefix = "Learning.EventStore.Sample.Web";
            services.AddMemoryCache();
            var eventStoreSettings = new RedisEventStoreSettings
            {
                ApplicationName = keyPrefix,
                EnableCompression = false
            };
            services.AddSingleton<IRedisClient>(y => new RedisClient(y.GetService<IConnectionMultiplexer>()));
            services.AddSingleton<IEventSubscriber>(y => new RedisEventSubscriber(y.GetService<IRedisClient>(), keyPrefix, y.GetService<IHostingEnvironment>().EnvironmentName));
            services.AddScoped<ISession, Session>();
            services.AddSingleton<IMessageQueue>(y => new RedisMessageQueue(y.GetService<IRedisClient>(), keyPrefix, y.GetService<IHostingEnvironment>().EnvironmentName));
            services.AddSingleton<IEventStore>(y => new RedisEventStore(y.GetService<IRedisClient>(), eventStoreSettings, y.GetService<IMessageQueue>()));
            services.AddSingleton<ICache, MemoryCache>();
            services.AddScoped<IRepository>(y => new Repository(y.GetService<IEventStore>()));

            //Register subscriptions
            var serviceProvider = services.BuildServiceProvider();
            LoadSubscriptionHandlers(serviceProvider);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void LoadCommandHandler(Type handlerType, IServiceCollection services)
        {
            // iterate all that are ICommandHandler
            foreach (var commandHandler in _allTypes
                .Where(x => x.Item2 != null && x.Item2.GetTypeInfo().IsGenericType && x.Item2.GetGenericTypeDefinition() == handlerType))
            {
                var type = commandHandler.Item1;
                var command = commandHandler.Item2.GetGenericArguments()[0];

                var genericType = handlerType.MakeGenericType(command);

                services.AddScoped(genericType, type);
            }
        }

        private void LoadQueryHandler(Type handlerType, IServiceCollection services)
        {
            foreach (var queryHandler in _allTypes
                .Where(x => x.Item2 != null && x.Item2.GetTypeInfo().IsGenericType && x.Item2.GetGenericTypeDefinition() == handlerType))
            {
                var type = queryHandler.Item1;
                var query = queryHandler.Item2.GetGenericArguments()[0];
                var result = queryHandler.Item2.GetGenericArguments()[1];

                var genericType = handlerType.MakeGenericType(query, result);

                services.AddScoped(genericType, type);
            }
        }

        private void LoadSubscriptionHandlers(IServiceProvider serviceLocator)
        {
            var subscriber = serviceLocator.GetService<IEventSubscriber>();
            var loggerFactory = serviceLocator.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Learning.EventStore.Sample.Web");

            var subscribers = _allTypes.Where(x => x.Item1 != null && x.Item1.GetInterfaces().Contains(typeof(ISubscription)) && !x.Item1.IsAbstract)
                .Select(x => Activator.CreateInstance(x.Item1, subscriber, logger) as ISubscription)
                .ToList();

            var subscriptionTaskList = new List<Task>();
            foreach (var handler in subscribers)
            {
                subscriptionTaskList.Add(Task.Run(async () => { await handler.SubscribeAsync(); }));
            }

            Task.WaitAll(subscriptionTaskList.ToArray());
        }
    }
}
