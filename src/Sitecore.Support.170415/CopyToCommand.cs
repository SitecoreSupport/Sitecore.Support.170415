using Sitecore.Buckets.Commands;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.Buckets.Search.SearchOperations
{
    [Serializable]
    internal class CopyToCommand : Command, IItemBucketsCommand
    {
        public override void Execute(CommandContext context)
        {
            List<SearchStringModel> searchStringModel = SearchStringModel.ExtractSearchQuery(context.Parameters.GetValues("url")[0].Replace("\"", string.Empty));
            if (context.Items.Length > 0)
            {
                Item item = context.Items[0];
                SitecoreIndexableItem sitecoreIndexableItem = item;
                if (sitecoreIndexableItem == null)
                {
                    Log.Error("Copy Items - Unable to cast current item - " + context.Items[0].GetType().FullName, this);
                    return;
                }
                using (IProviderSearchContext providerSearchContext = ContentSearchManager.GetIndex(sitecoreIndexableItem).CreateSearchContext(SearchSecurityOptions.Default))
                {
                    IQueryable<SitecoreUISearchResultItem> queryable = LinqHelper.CreateQuery<SitecoreUISearchResultItem>(providerSearchContext, searchStringModel, sitecoreIndexableItem, null);

                    List<Item> list = new List<Item>();
                    foreach (SitecoreUISearchResultItem current in queryable)
                    {
                        if (current.GetItem() != null)
                        {
                            list.Add(current.GetItem());
                        }
                    }
                    //Sitecore.Support.170415
                    Items.CopyTo(list.Where(o => o.Paths.IsDescendantOf(item)).ToArray());
                    //
                }
            }
        }
    }
}