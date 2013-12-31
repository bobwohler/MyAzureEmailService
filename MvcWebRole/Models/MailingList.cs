using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel.DataAnnotations;

namespace MvcWebRole.Models
{
    /// <summary>
    /// Data model entity class for MailingList Azure Table entries.
    /// </summary>
    public class MailingList : TableEntity
    {
        public MailingList()
        {
            this.RowKey = "mailinglist";
        }

        //The mailinglist table partition key is the list name. In this entity class the partition key value can 
        //be accessed either by using the PartitionKey property (defined in the TableEntity class) or the 
        //ListName property (defined in the MailingList class). The ListName property uses PartitionKey as its backing variable. 
        //Defining the ListName property enables you to use a more descriptive variable name in code and makes it easier to 
        //program the web UI, since formatting and validation DataAnnotations attributes can be added to the ListName 
        //property, but they can't be added directly to the PartitionKey property.
        //The RegularExpression attribute on the ListName property causes MVC to validate user input to ensure that the 
        //list name value entered only contains alphanumeric characters or underscores. This restriction was implemented in 
        //order to keep list names simple so that they can easily be used in query strings in URLs.
        //Note: If you wanted the list name format to be less restrictive, you could allow other characters and URL-encode 
        //list names when they are used in query strings. However, certain characters are not allowed in Windows Azure Table 
        //partition keys or row keys, and you would have to exclude at least those characters. For information about 
        //characters that are not allowed or cause problems in the partition key or row key fields, see 
        //Understanding the Table Service Data Model and % Character in PartitionKey or RowKey.
        //The MailingList class defines a default constructor that sets RowKey to the hard-coded string "mailinglist", 
        //because all of the mailing list rows in this table have that value as their row key. (For an explanation of the table structure, 
        //see the first tutorial in the series.) Any constant value could have been chosen for this purpose, as long as it 
        //could never be the same as an email address, which is the row key for the subscriber rows in this table.
        //The list name and the "from" email address must always be entered when a new MailingList entity is created, so they have Required attributes.
        //The Display attributes specify the default caption to be used for a field in the MVC UI.
        [Required]
        [RegularExpression(@"[\w]+", ErrorMessage=@"Only alphanumeric characters and underscore (_) are allowed.")]
        [Display(Name="List Name")]
        public string ListName
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        [Required]
        [Display(Name = "'From' Email Address")]
        public string FromEmailAddress { get; set; }

        public string Description { get; set; }
    }
}