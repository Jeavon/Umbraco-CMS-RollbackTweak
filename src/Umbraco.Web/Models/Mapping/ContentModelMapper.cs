﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Mapping;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.Models.ContentEditing;
using umbraco;
using Umbraco.Web.Routing;

namespace Umbraco.Web.Models.Mapping
{

    /// <summary>
    /// Declares how model mappings for content
    /// </summary>
    internal class ContentModelMapper : MapperConfiguration
    {
        public override void ConfigureMappings(IConfiguration config, ApplicationContext applicationContext)
        {
            
            //FROM IContent TO ContentItemDisplay
            config.CreateMap<IContent, ContentItemDisplay>()
                  .ForMember(
                      dto => dto.Owner,
                      expression => expression.ResolveUsing<OwnerResolver<IContent>>())
                  .ForMember(
                      dto => dto.Updator,
                      expression => expression.ResolveUsing<CreatorResolver>())
                  .ForMember(
                      dto => dto.Icon,
                      expression => expression.MapFrom(content => content.ContentType.Icon))
                  .ForMember(
                      dto => dto.ContentTypeAlias,
                      expression => expression.MapFrom(content => content.ContentType.Alias))
                  .ForMember(
                      dto => dto.ContentTypeName,
                      expression => expression.MapFrom(content => content.ContentType.Name))
                  .ForMember(
                      dto => dto.PublishDate,
                      expression => expression.MapFrom(content => GetPublishedDate(content, applicationContext)))
                  .ForMember(
                      dto => dto.Template,
                      expression => expression.MapFrom(content => new TemplateBasic
                          {
                              Alias = content.Template.Alias,
                              Id = content.Template.Id,
                              Name = content.Template.Name
                          }))
                  .ForMember(
                      dto => dto.Urls,
                      expression => expression.MapFrom(content =>
                                                       UmbracoContext.Current == null
                                                           ? new[] {"Cannot generate urls without a current Umbraco Context"}
                                                           : content.GetContentUrls()))
                  .ForMember(display => display.Properties, expression => expression.Ignore())
                  .ForMember(display => display.Tabs, expression => expression.ResolveUsing<TabsAndPropertiesResolver>())
                  .AfterMap((content, display) => TabsAndPropertiesResolver.MapGenericProperties(
                      content, display,
                      new ContentPropertyDisplay
                          {
                              Alias = string.Format("{0}releasedate", Constants.PropertyEditors.InternalGenericPropertiesPrefix),
                              Label = ui.Text("content", "releaseDate"),
                              Value = display.ReleaseDate.HasValue ? display.ReleaseDate.Value.ToIsoString() : null,
                              View = "datepicker" //TODO: Hard coding this because the templatepicker doesn't necessarily need to be a resolvable (real) property editor
                          },
                      new ContentPropertyDisplay
                          {
                              Alias = string.Format("{0}expiredate", Constants.PropertyEditors.InternalGenericPropertiesPrefix),
                              Label = ui.Text("content", "removeDate"),
                              Value = display.ExpireDate.HasValue ? display.ExpireDate.Value.ToIsoString() : null,
                              View = "datepicker" //TODO: Hard coding this because the templatepicker doesn't necessarily need to be a resolvable (real) property editor
                          },
                      new ContentPropertyDisplay
                          {
                              Alias = string.Format("{0}template", Constants.PropertyEditors.InternalGenericPropertiesPrefix),
                              Label = "Template", //TODO: localize this?
                              Value = JsonConvert.SerializeObject(display.Template),
                              View = "templatepicker" //TODO: Hard coding this because the templatepicker doesn't necessarily need to be a resolvable (real) property editor
                          },
                      new ContentPropertyDisplay
                          {
                              Alias = string.Format("{0}urls", Constants.PropertyEditors.InternalGenericPropertiesPrefix),
                              Label = ui.Text("content", "urls"),
                              Value = string.Join(",", display.Urls),
                              View = "urllist" //TODO: Hard coding this because the templatepicker doesn't necessarily need to be a resolvable (real) property editor
                          }));

            //FROM IContent TO ContentItemBasic<ContentPropertyBasic, IContent>
            config.CreateMap<IContent, ContentItemBasic<ContentPropertyBasic, IContent>>()
                  .ForMember(
                      dto => dto.Owner,
                      expression => expression.ResolveUsing<OwnerResolver<IContent>>())
                  .ForMember(
                      dto => dto.Updator,
                      expression => expression.ResolveUsing<CreatorResolver>())
                  .ForMember(
                      dto => dto.Icon,
                      expression => expression.MapFrom(content => content.ContentType.Icon))
                  .ForMember(
                      dto => dto.ContentTypeAlias,
                      expression => expression.MapFrom(content => content.ContentType.Alias));

            //FROM IContent TO ContentItemDto<IContent>
            config.CreateMap<IContent, ContentItemDto<IContent>>()
                  .ForMember(
                      dto => dto.Owner,
                      expression => expression.ResolveUsing<OwnerResolver<IContent>>());

            
        }

        /// <summary>
        /// Gets the published date value for the IContent object
        /// </summary>
        /// <param name="content"></param>
        /// <param name="applicationContext"></param>
        /// <returns></returns>
        private static DateTime? GetPublishedDate(IContent content, ApplicationContext applicationContext)
        {
            if (content.Published)
            {
                return content.UpdateDate;
            }
            if (content.HasPublishedVersion())
            {
                var published = applicationContext.Services.ContentService.GetPublishedVersion(content.Id);
                return published.UpdateDate;
            }
            return null;
        }

    }
}