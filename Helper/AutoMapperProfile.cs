using System;
using System.Linq;
using System.ServiceModel.Syndication;
using AutoMapper;
using AWSLambdaRssNotification.Model;

namespace AWSLambdaRssNotification.Helper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<SyndicationItem, Item>()
                .ForMember(dest => dest.Title, option => option.MapFrom(s => s.Title.Text))
                .ForMember(dest => dest.Categories,
                    option => option.MapFrom(s =>
                        s.Categories.Any() ? String.Join(',', s.Categories.Select(c => c.Name).ToList()) : null))
                .ForMember(dest => dest.Summary, option => option.MapFrom(s => s.Summary.Text))
                .ForMember(dest => dest.Link,
                    option => option.MapFrom(s =>
                        s.Links.Any() ? s.Links.FirstOrDefault().Uri.AbsoluteUri : string.Empty))
                .ForMember(dest => dest.PublishDate, option => option.MapFrom(s => s.PublishDate.DateTime));
        }

    }
}
