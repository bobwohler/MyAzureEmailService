using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using MvcWebRole.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

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
            // If you don't specify timeout parameters, the API automatically retries
            // 3 times with exponentially increasing timeout limits. This can be
            // unbearable for a web user waiting on a page to load.
            // Instead use a Linear Retry option to keep the timout interval from increasing and 
            // setting a reasonable number of retries.
            TableRequestOptions reqOptions = new TableRequestOptions()
            {
                MaximumExecutionTime = TimeSpan.FromSeconds(1.5),
                RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3)
            };

            List<MailingList> lists;
            try
            {
                var query = new TableQuery<MailingList>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "mailinglist"));
                lists = mailingListTable.ExecuteQuery(query, reqOptions).ToList();
            }
            catch (StorageException se)
            {
                ViewBag.errorMessage = "Timeout error, try again.";
                Trace.TraceError(se.Message);
                return View("Error");
            }
            return View(lists);
        }
        /// <summary>
        /// HTTP Post: /MailingList/Create/
        /// </summary>
        /// <param name="mailingList"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MailingList mailingList)
        {
            if (ModelState.IsValid)
            {
                var insertOperation = TableOperation.Insert(mailingList);
                mailingListTable.Execute(insertOperation);
                return RedirectToAction("Index");
            }

            return View(mailingList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string partitionKey, string rowKey, MailingList editedMailingList)
        {
            if (ModelState.IsValid)
            {
                var mailingList = new MailingList();
                UpdateModel(mailingList);
                try
                {
                    var replaceOperation = TableOperation.Replace(mailingList);
                    mailingListTable.Execute(replaceOperation);
                    return RedirectToAction("Index");
                }
                catch (StorageException se)
                {
                    if (se.RequestInformation.HttpStatusCode == 412)
                    {
                        // Concurrency error...look up the original row this update was
                        // attempting to change and retrieve the values that were changed.
                        var currentMailingList = FindRow(partitionKey, rowKey);
                        if (currentMailingList.FromEmailAddress != editedMailingList.FromEmailAddress)
                        {
                            ModelState.AddModelError("FromEmailAddress", "Current value: " + currentMailingList.FromEmailAddress);
                        }
                        if (currentMailingList.Description != editedMailingList.Description)
                        {
                            ModelState.AddModelError("Description", "Current value: " + currentMailingList.Description);
                        }
                        
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit was modified by another user after you got the original value." +
                            "The edit operation was canceled and the current values in the database have been displayed. If you still want to edit the this record, click the 'Save' button again. " +
                            "Otherwise click the 'Back to List' hyperlink.");

                        ModelState.SetModelValue("ETag", new ValueProviderResult(currentMailingList.ETag, currentMailingList.ETag, null));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(editedMailingList);
        }

        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        // Delete all rows for this mailing list, that is,
        // Subscriber wos as well as MailingList rows.
        // Therefore no need to specify a row key.
        public ActionResult DeleteConfirmed(string partionKey)
        {
            var query = new TableQuery<MailingList>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partionKey));
            var listRows = mailingListTable.ExecuteQuery(query).ToList();
            var batchOperation = new TableBatchOperation();
            int itemsInBatch = 0;
            // Iterate the returned rows and delete them.
            // NOTE: the MailingList object is used for both MailingList and SubscriberList objects
            // since they both contain the PartitionKey, RowKey and etag values, which are
            // all that's needed to delete the rows.
            foreach (MailingList listRow in listRows)
            {
                batchOperation.Delete(listRow);
                itemsInBatch++;
                // Execute the delete in batches of 100.
                if (itemsInBatch == 100)
                {
                    mailingListTable.ExecuteBatch(batchOperation);
                    itemsInBatch = 0;
                    batchOperation = new TableBatchOperation();
                }
            }
            // Process stragglers in last batch that is likely less than 100.
            if (itemsInBatch > 0)
            {
                mailingListTable.ExecuteBatch(batchOperation);
            }
            return RedirectToAction("Index");
        }
	}
}