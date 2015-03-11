using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ResetterService
{
    public partial class RestarterService : ServiceBase
    {
        private static System.Timers.Timer ResetterJob;
        public static string[] ServiceNames = null ;
        private static Lazy<ConfigFileConfigurationProvider> configuration = new Lazy<ConfigFileConfigurationProvider>(() =>
            {
                ConfigFileConfigurationProvider configProvider = new ConfigFileConfigurationProvider();
                return configProvider;
            }
            , true);

        public RestarterService()
        {
            InitializeComponent();
            Init();            
        }

        private void Init()
        {
            ReadConfig();           
            try
            {
                ResetterJob = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds); //Satte bir kontrol et.
                ResetterJob.Elapsed += new System.Timers.ElapsedEventHandler(ResetterJob_Elapsed);
                ResetterJob.Start();                
            }
            catch (Exception excep)
            {                        
                this.EventLog.WriteEntry("Init error:" + excep.Message, EventLogEntryType.Error);
            }
            finally{
                logInfo("ApplicationStarted");
            }                                
        }

        private void ReadConfig()
        {
            ServiceNames = configuration.Value.GetServiceNames();
            foreach (var service in ServiceNames)
            {
                Console.WriteLine(service);
            }
        }

        private void Reset(string serviceName)
        {
            // Check whether the Alerter service is started.

            ServiceController sc = new ServiceController();
            
            sc.ServiceName = serviceName;
            string infoMessageCurrentStatus=string.Format("The {0} service status is currently set to {1}",serviceName, sc.Status.ToString());
            logInfo(infoMessageCurrentStatus);

            if (sc.Status == ServiceControllerStatus.Running)
            {
                logInfo(string.Format("Stopping the {0} service...", serviceName));
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);

                logInfo(string.Format("The {0} service status is now set to {1}.", serviceName, sc.Status.ToString()));
            }

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                // Start the service if the current status is stopped.

                logInfo(string.Format("Starting the {0} service...",serviceName));
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);

                    // Display the current service status.
                    logInfo(string.Format("The {0} service status is now set to {1}.",serviceName, sc.Status.ToString()));

                }
                catch (InvalidOperationException)
                {
                    string errorMessage=string.Format("Could not start the {0} service.",serviceName);
                    logError(errorMessage);
                }
            }
            
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }

        void logInfo(string infoMessage)
        {
            Console.WriteLine(infoMessage);
            this.EventLog.WriteEntry(infoMessage, EventLogEntryType.Information);
            logCommon(string.Format("[INFO]->{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),infoMessage));
        }

        void logError(string errorMessage)
        { 
            Console.WriteLine(errorMessage);
            this.EventLog.WriteEntry(errorMessage, EventLogEntryType.Error);
            string mailToAddress=ConfigFileConfigurationProvider.configuration.Value.AppSettings.Settings["mailtoAddress"].Value;
            string mailCCAddress=ConfigFileConfigurationProvider.configuration.Value.AppSettings.Settings["mailCCAddress"].Value;
            Helpers.SendMail(mailToAddress, "ResetterAppliciton", errorMessage, mailCCAddress);
            logCommon(string.Format("[ERROR]->{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), errorMessage));
        }

        void logCommon(string text)
        {
            logToFile(text);
        }

        void logToFile(string text)
        {
            string path = string.Format("{0}_{1}.txt", Helpers.ApplicationExecutableName ,DateTime.Now.ToString("yyyy-MM-dd"));
            using (StreamWriter sw = (File.Exists(path)) ? File.AppendText(path) : File.CreateText(path))
            {
                sw.WriteLine(text);
                sw.Flush();
            } 
        }

        void ResetterJob_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var hours=ConfigFileConfigurationProvider.configuration.Value.AppSettings.Settings["WorkingHours"].Value.Split(',');
                if (!hours.Contains(DateTime.Now.Hour.ToString()))
                    return;

                ResetterJob.Stop();
                foreach (var serviceName in ServiceNames)
                {
                    Reset(serviceName);
                }
            }
            catch (Exception ex)
            {
                string errorMessage="ResetterJob servis Hatası :" + ex == null ? "exception is null" : "ResetterJob servis Hatası  :" + ex.Message ?? "exception.Message null";
                logError(errorMessage);
            }
            finally
            {
                ResetterJob.Start();
            }
        }

        static void Main(string[] args)
        {
            RestarterService service = new RestarterService();

            if (Environment.UserInteractive)
            {
                service.OnStart(args);
                Console.WriteLine("Press any key to stop program");
                Console.Read();
                service.OnStop();
            }
            else
            {
                ServiceBase.Run(service);
            }

        }

    }

    public interface IServiceNameProvider
    {
        string[] GetServiceNames();
    }

    public partial class ConfigFileConfigurationProvider : IServiceNameProvider
    {
        public static Lazy<Configuration> configuration = new Lazy<Configuration>(() =>
            {
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                return configuration;
            },
            true);

        public string[] GetServiceNames()
        {
            string serviceNamesToReset=configuration.Value.AppSettings.Settings["ServiceNamesToRestart"].Value;    
            var serviceNamesArray = serviceNamesToReset.Split(',');
            return serviceNamesArray;
        }
    }
}