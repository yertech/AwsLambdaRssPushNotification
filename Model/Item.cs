using System;
using System.Collections.Generic;
using System.Text;

namespace AWSLambdaRssNotification.Model
{
    public class Item
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Categories { get; set; }
        public string Link { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
