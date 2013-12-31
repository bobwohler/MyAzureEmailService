using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MvcWebRole
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /// <summary>
        /// The web application will use the MailingList table, the Message table, the 
        /// azuremailsubscribequeue queue, and the azuremailblobcontainer blob container. 
        /// You could create these manually by using a tool such as Azure Storage Explorer, 
        /// but then you would have to do that manually every time you started to use the 
        /// application with a new storage account. In this section you'll add code that 
        /// runs when the application starts, checks if the required tables, queues, and 
        /// blob containers exist, and creates them if they don't.
        /// You could add this one-time startup code to the OnStart method in the WebRole.cs 
        /// file, or to the Global.asax file. For this tutorial you'll initialize Windows Azure 
        /// Storage in the Global.asax file since that works with Windows Azure Web Sites as well as 
        /// Windows Azure Cloud Service web roles.
        /// </summary>
        private static void CreateTablesQueuesBlobContainers()
        {
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            // If this is running in a Windows Azure Web Site rather than a Cloud Service use the Web.config file:
            //      var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            
            // Verify or create tables...
            var tableClient = storageAccount.CreateCloudTableClient();
            var mailingListTable = tableClient.GetTableReference("MailingList");
            mailingListTable.CreateIfNotExists();
            var messageTable = tableClient.GetTableReference("Message");

            // Verify or create blob container...
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("azureemailblobcontainer");
            blobContainer.CreateIfNotExists();

            // Verify or create queue...
            var queueClient = storageAccount.CreateCloudQueueClient();
            var subscribeQueue = queueClient.GetQueueReference("azureemailsubscribequeue");
            subscribeQueue.CreateIfNotExists();
        }
    }
}
