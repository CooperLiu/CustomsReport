using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Rebus.Activation;
using Rebus.Auditing.Messages;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.NewtonsoftJson;
using Rebus.NLog;

namespace Boss.Scm.CustomsReportHost
{
    public class RebusBootstrapper
    {
        private static readonly string MqConnectionString =
            ConfigurationManager.ConnectionStrings["RabbitMqServer"].ConnectionString;

        public const int DefaultPrefetchQty = 50;
        public const int DefaultNumberOfWorkers = 1;
        public const int DefaultMaxParallelism = 1;
        public const string DefaultMessageAudtingQueue = "logstash-input-mqmessage-auditing";


        public Assembly[] MqHandlersAssemblies { get; set; }

        public bool IsEnableMessageAudting { get; set; } = false;

        public string QueueName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name;

        public string MessageAudtingQueue { get; set; }

        public int PrefetchQty { get; set; } = DefaultPrefetchQty;

        public int NumberOfWorkers { get; set; } = DefaultNumberOfWorkers;

        public int MaxParallelism { get; set; } = DefaultMaxParallelism;

        public BuiltinHandlerActivator _ioc;


        public RebusBootstrapper(BuiltinHandlerActivator ioc)
        {
            _ioc = ioc;
        }
        
        /// <summary>
        /// 程序集注册MqHandler
        /// </summary>
        /// <param name="assembly"></param>
        public void RegisterMqHandlersFromAssembly(params Assembly[] assembly)
        {
            MqHandlersAssemblies = assembly.ToArray();
        }

        /// <summary>
        /// 启用异步消息审计
        /// </summary>
        /// <param name="messageAudtingQueue"></param>
        public void EnableMessageAudting(string messageAudtingQueue)
        {
            if (string.IsNullOrEmpty(messageAudtingQueue))
            {
                return;
            }

            IsEnableMessageAudting = true;
            MessageAudtingQueue = messageAudtingQueue;
        }

        /// <summary>
        /// 消费端预加载消息个数
        /// </summary>
        /// <param name="perfetcQty"></param>
        public void SetPrefetchQty(int perfetcQty)
        {
            PrefetchQty = perfetcQty > DefaultPrefetchQty ? perfetcQty : DefaultPrefetchQty;
        }

        /// <summary>
        /// 设置消费端消费属性
        /// </summary>
        /// <param name="numberOfWorkers"></param>
        /// <param name="maxParallelism"></param>
        public void SetWorkProperties(int numberOfWorkers = DefaultNumberOfWorkers, int maxParallelism = DefaultMaxParallelism)
        {
            NumberOfWorkers = numberOfWorkers;
            MaxParallelism = maxParallelism;
        }

        /// <summary>
        /// 消费队列名称
        /// </summary>
        /// <param name="queueName"></param>
        public void SetQueueName(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                return;
            }

            QueueName = queueName;
        }

        /// <summary>
        /// 启动消息端
        /// </summary>
        /// <returns></returns>
        public IBus Bootstrap()
        {
            var rebusConfig = Configure.With(_ioc);
            rebusConfig.Logging(c => c.NLog());
            rebusConfig.Serialization(s =>
                s.UseNewtonsoftJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }));

            rebusConfig.Options(o =>
            {
                o.SetNumberOfWorkers(NumberOfWorkers);
                o.SetMaxParallelism(MaxParallelism);
            });

            if (IsEnableMessageAudting)
            {
                rebusConfig.Options(o => o.EnableMessageAuditing(MessageAudtingQueue));
            }

            //https://github.com/rebus-org/Rebus.RabbitMq/tree/master/Rebus.RabbitMq/Config  more option about RabbitMqOptionsBuilder
            var bus = rebusConfig
                    .Transport(c => c.UseRabbitMq(MqConnectionString, QueueName)
                    .Prefetch(PrefetchQty))
                    .Start();

            _ioc.Register(() => new CustomsReportHandler());

            if (MqHandlersAssemblies != null && MqHandlersAssemblies.Any())
            {
                var mqMessageTypes = new List<Type>();
                //Register handlers first!
                foreach (var assembly in MqHandlersAssemblies)
                {
                    mqMessageTypes.AddRange(assembly.GetTypes()
                        .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>)))
                        .SelectMany(t => t.GetInterfaces())
                        .Distinct()
                        .SelectMany(t => t.GetGenericArguments())
                        .Distinct());
                }

                //Subscribe messages
                mqMessageTypes = mqMessageTypes.Distinct().ToList();

                foreach (var mqMessageType in mqMessageTypes)
                {
                    bus.Subscribe(mqMessageType);
                }
            }

            return bus;
        }
    }
}