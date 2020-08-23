using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Amazon.Lambda.Core;
using AutoMapper;
using AWSLambdaRssNotification.Helper;
using AWSLambdaRssNotification.Model;
using Microsoft.Extensions.DependencyInjection;
using NPushover;
using NPushover.RequestObjects;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaRssNotification
{
   

    public class Function
    {
        private List<string> _filteringTerms;
        private List<string> _feeds;
        private const string keyName = "data.json";
        private readonly IMapper _mapper;
        public IConfigurationService _configService;

        public Function()
        {
            // Get dependency resolver
            var resolver = new DependencyResolver();

            _mapper = resolver.ServiceProvider.GetService<IMapper>();
            _configService = resolver.ServiceProvider.GetService<IConfigurationService>();

        }

        // Use this ctor from unit tests that can mock IProductRepository
        public Function( IMapper mapper, IConfigurationService configService)
        {
            _mapper = mapper;
            _configService = configService;

        }


        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <returns></returns>
        public async Task<string> FunctionHandler()
        {
            var pushOverUser = _configService.GetConfiguration()["PushOverUser"];
            //Init Notification to pushover api
            var po = new Pushover(_configService.GetConfiguration()["PushOverSecret"]);

            try
            {
                //Get items for rss feeds
                var itemsfeed = GetRssFeeds();
                List<Item> items = _mapper.Map<List<Item>>(itemsfeed);

                //filter items 
                _filteringTerms = _configService.GetConfiguration()["FilteringTerms"].Split(";").ToList();
                List<Item> newItems = items.Where(i =>
                    _filteringTerms.Any(t =>
                        i.Title.ToUpper().Contains(t) || i.Summary.ToUpper().Contains(t))).ToList();

                //get file from s3
                var client = new S3Utils();
                var jobs = await client.GetFileContent(keyName);

                //Get new items not in db
                var filteredItems = newItems.Where(item => jobs.All(dbi => dbi.Id != item.Id))
                    .ToList();

                // Quick message:
                foreach (var newItem in filteredItems)
                {
                    newItem.CreatedDate = DateTime.Now;
                    var msg = new Message(Sounds.Pushover)
                    {
                        Title = newItem.Id.ToLower().Contains("freelancer")
                            ? $"Freelancer : {newItem.Title}"
                            : $"Upwork : {newItem.Title}",
                        Body = SubStringBody(newItem.Summary),
                        Priority = Priority.Normal,
                        IsHtmlBody = true,
                        Timestamp = DateTime.Now,
                        SupplementaryUrl = new SupplementaryURL
                        {
                            Uri = new Uri(newItem.Link),
                            Title = newItem.Title
                        }
                    };
                    var sendtask = po.SendMessageAsync(msg, pushOverUser);

                }

                //Insert new items in db
                if (filteredItems.Any())
                {
                    try
                    {
                        jobs.AddRange(filteredItems);
                        var lastJobsOnly =  jobs.Where(i => i.CreatedDate >= DateTime.Now.AddDays(-7)).ToList();
                        await client.UploadFile(lastJobsOnly, keyName);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }


                return "ok";
            }
            catch (Exception e)
            {
                var msg = new Message(Sounds.Echo)
                {
                    Title = $"Error {e.InnerException}",
                    Body = SubStringBody(e.Message),
                    Priority = Priority.Emergency,
                    IsHtmlBody = true,
                    Timestamp = DateTime.Now
                };
                var sendtask = po.SendMessageAsync(msg, pushOverUser);
                throw e;
            }

        }

        private string SubStringBody(string newItemSummary)
        {
            //Max length  for message = 1024
            var text = newItemSummary.Replace("     ",string.Empty);
            if (text.Length > 1024)
            {
                var indexOfFooter = newItemSummary.IndexOf("<br /><b>Posted On", StringComparison.Ordinal);
                if (indexOfFooter != -1)
                {
                    //get the footer for upwork message
                    var footer = newItemSummary.Substring(indexOfFooter);

                    try
                    {
                        var footerLength = footer.Length > 1021 ? 1021 : 1021 - footer.Length;
                        text = newItemSummary.Substring(0, indexOfFooter).Substring(0, footerLength);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error encountered ***. Message:'{0}' when cutting text", e.Message);
                    }

                    text += "..." + footer;
                }
                else{
                    text = newItemSummary.Substring(0, 1024);
                }
            }

            return text;
        }

        private List<SyndicationItem> GetRssFeeds()
        {
            List<SyndicationItem> finalItems = new List<SyndicationItem>();
            _feeds = _configService.GetConfiguration()["Feeds"].Split(";").ToList();
            foreach (string feed in _feeds)
            {
                try
                {
                    XmlReader reader = XmlReader.Create(feed);
                    Rss20FeedFormatter formatter = new Rss20FeedFormatter();
                    formatter.ReadFrom(reader);
                    reader.Close();
                    finalItems.AddRange(formatter.Feed.Items);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            return finalItems.OrderBy(x => x.PublishDate).ToList();
        }
    }


}
