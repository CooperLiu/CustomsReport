using System.Configuration;
using System.Reflection;
using NLog;
using NLog.Config;
using Rebus.Activation;
using Rebus.Bus;

namespace Boss.Scm.CustomsReportHost
{
    public class CustomsReportHostBootstrap
    {

        static readonly BuiltinHandlerActivator ContainerAdapter = new BuiltinHandlerActivator();

        public void Start()
        {
            LogManager.Configuration = new XmlLoggingConfiguration(ConfigurationManager.AppSettings["NlogConfigFilePath"]);

            //var bootstrapper = new RebusBootstrapper(ContainerAdapter);

            //bootstrapper.RegisterMqHandlersFromAssembly(Assembly.GetExecutingAssembly(), typeof(CustomsReportHostBootstrap).Assembly);

            //bootstrapper.SetPrefetchQty(50);

            //bootstrapper.Bootstrap();

            if (DebugHelper.IsDebug)
            {
                var test = new TestClass(ContainerAdapter);

                test.Trigger();
            }
        }

        public void Stop()
        {
            ContainerAdapter.Bus.Dispose();
            ContainerAdapter.Dispose();
        }
    }
}