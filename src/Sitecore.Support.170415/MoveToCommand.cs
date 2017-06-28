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
using System.Collections.Specialized;
using System.Linq;

namespace Sitecore.Support.Buckets.Search.SearchOperations
{
    [Serializable]
    internal class MoveToCommand : Command, IItemBucketsCommand
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
                    Log.Error("Move Items - Unable to cast current item - " + context.Items[0].GetType().FullName, this);
                    return;
                }
                using (IProviderSearchContext providerSearchContext = ContentSearchManager.GetIndex(sitecoreIndexableItem).CreateSearchContext(SearchSecurityOptions.Default))
                {
                    Item[] items = (from o in (from item2 in LinqHelper.CreateQuery<SitecoreUISearchResultItem>(providerSearchContext, searchStringModel, sitecoreIndexableItem, null)
                                               select item2.GetItem()).AsEnumerable<Item>()
                                    where o != null && o.Security.CanWrite(Context.User)
                                    //Sitecore.Support.170415
                                    where o.Paths.IsDescendantOf(item)
                                    //
                                    select o).ToArray<Item>();
                    using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
                    {
                        Items.MoveTo(items, new NameValueCollection
                        {
                            {
                                "searchRootId",
                                item.ID.ToString()
                            }
                        });
                    }
                }
            }
        }
    }
}