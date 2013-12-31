using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using MvcWebRole.Models;

namespace MvcWebRole.Controllers
{
    public class MailingListController : Controller
    {
        private CloudTable mailingListTable;

        public MailingListController()
        {
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            // If this is running in a Windows Azure Web Site rather than a Cloud Service use the Web.config file:
            //      var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            mailingListTable = tableClient.GetTableReference("mailinglist");
        }
        
        /// <summary>
        /// Next is a FindRow method that is called whenever the controller needs to look up a 
        /// specific mailing list entry of the MailingList table, for example to edit a mailing list entry. 
        /// The code retrieves a single MailingList entity by using the partition key and row key values 
        /// passed in to it. The rows that this controller edits are the ones that have "MailingList" as 
        /// the row key, so "MailingList" could have been hard-coded for the row key, but specifying both 
        /// partition key and row key is a pattern used for the FindRow methods in all of the controllers.
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        /// <returns></returns>
        private MailingList FindRow(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<MailingList>(partitionKey, rowKey);
            var retrievedResult = mailingListTable.Execute(retrieveOperation);
            var mailingList = retrievedResult.Result as MailingList;
            if(mailingList == null)
            {
                throw new Exception("No mailing list found for:" + partitionKey);
            }
            return mailingList;
        }

        //
        // GET: /MailingList/
        public ActionResult Index()
        {
            return View();
        }
	}
}