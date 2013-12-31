using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;

namespace MvcWebRole
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            ConfigureDiagnostics(); // Enable custom tracing setup I created.
            return base.OnStart();
        }

        /// <summary>
        /// Add override to OnStop to handle OS restarts from Azure during their maintenance cycles.
        /// This will help ensure all current processing completes before the shutdown.
        /// The OnStop method has up to 5 minutes to exit before the application is shut down. 
        /// You could add a sleep call for 5 minutes to the OnStop method to give your application the 
        /// maximum amount of time to process the current requests, but if your application is scaled correctly, 
        /// it should be able to process the remaining requests in much less than 5 minutes. 
        /// It is best to stop as quickly as possible, so that the application can restart as quickly as 
        /// possible and continue processing requests.
        /// Once a role is taken off-line by Windows Azure, the load balancer stops sending requests to the 
        /// role instance, and after that the OnStop method is called. If you don't have another instance of your role, 
        /// no requests will be processed until your role completes shutting down and is restarted 
        /// (which typically takes several minutes). That is one reason why the Windows Azure service level agreement 
        /// requires you to have at least two instances of each role in order to take advantage of the up-time guarantee.
        /// In the code shown for the OnStop method, an ASP.NET performance counter is created for Requests Current. 
        /// The Requests Current counter value contains the current number of requests, including those that are queued, 
        /// currently executing, or waiting to be written to the client. The Requests Current value is checked every second, 
        /// and once it falls to zero, the OnStop method returns. Once OnStop returns, the role shuts down.
        /// Trace data is not saved when called from the OnStop method without performing an On-Demand Transfer. 
        /// You can view the OnStop trace information in real time with the dbgview utility from a remote desktop connection.
        /// </summary>
        public override void OnStop()
        {
            Trace.TraceInformation("OnStop called from WebRole");
            var rcCounter = new PerformanceCounter("ASP.NET", "Requests Current", "");
            while (rcCounter.NextValue()>0)
            {
                Trace.TraceInformation("ASP.NET Requests Current = " + rcCounter.NextValue().ToString());
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void ConfigureDiagnostics()
        {
            // Configure tracing.
            DiagnosticMonitorConfiguration config = DiagnosticMonitor.GetDefaultInitialConfiguration();
            config.Logs.BufferQuotaInMB = 500;
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1d);
            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", config);
        }
    }
}
