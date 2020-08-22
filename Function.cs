using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Amazon.Lambda.Core;
using AutoMapper;
using AWSLambdaRssNotification.Helper;
using AWSLambdaRssNotification.Model;
using JsonFlatFileDataStore;
using Microsoft.Extensions.DependencyInjection;
using NPushover;
using NPushover.RequestObjects;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaRssNotification
{

    public class Function
    {
        private readonly List<string> _filteringTerms = new List<string>(){ ".NET", ".NET CORE", "ASP.NET", "ASP.NET MVC", "C#" };
        private readonly List<string> _feeds = new List<string>(){ "https://www.freelancer.com/rss.xml",
            "https://www.upwork.com/ab/feed/jobs/rss?contractor_tier=2%2C3&proposals=0-4%2C5-9%2C10-14&q=%28.NET+Core+OR+React+OR+asp.net+mvc+OR+.NET+Framework+OR+Gastby%29&sort=recency&paging=0%3B10&api_params=1&securityToken=58e44659ae871d542fa6eff3ced8a927d26735d111ff61b699fde1f8be90cd1b14bb86e07f97035ab69f5712c7654bc41451aa5bccce3df882d038c6cfeea50c&userUid=1215640702124244992&orgUid=1215640702136827905"      };
        private readonly IMapper _mapper;

        public Function()
        {
            // Get dependency resolver
            var resolver = new DependencyResolver();

            _mapper = resolver.ServiceProvider.GetService<IMapper>();
        }

        // Use this ctor from unit tests that can mock IProductRepository
        public Function( IMapper mapper)
        {
            _mapper = mapper;
        }


        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <returns></returns>
        public string FunctionHandler()
        {
            //Init Notification to pushover api
            var po = new Pushover("agkxq9gsn3v16gsnuz4summp7haxch");

            try 
            {
                //Get items for rss feeds
                var itemsfeed = GetRssFeeds();
                List<Item> items = _mapper.Map<List<Item>>(itemsfeed);

                //filter items 
                List<Item> newItems = items.Where(i =>
                    _filteringTerms.Any(t =>
                        i.Title.ToUpper().Contains(t) || i.Summary.ToUpper().Contains(t))).ToList();

                //Get new items not in db
                using (var store = new DataStore("data.json"))
                {
                    var dbItems = store.GetCollection<Item>();
                    var filteredItems = newItems.Where(item => dbItems.AsQueryable().All(dbi => dbi.Id != item.Id))
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
                        var sendtask = po.SendMessageAsync(msg, "u789prun7x9xeqbdvgsusybysa5cra");

                    }

                    //Insert new items in db
                    if (filteredItems.Any())
                    {
                        try
                        {
                            dbItems.InsertMany(filteredItems);
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
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
                var sendtask = po.SendMessageAsync(msg, "u789prun7x9xeqbdvgsusybysa5cra");
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
                        text = newItemSummary.Substring(0, indexOfFooter).Substring(0, 1021-footer.Length);
                    }
                    catch (Exception e)
                    {
                        
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
