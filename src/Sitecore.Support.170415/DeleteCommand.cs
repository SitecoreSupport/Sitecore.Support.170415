using Sitecore.Buckets.Commands;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.Buckets.Search.SearchOperations
{
    [Serializable]
    internal class DeleteCommand : Command, IItemBucketsCommand
    {
        public override void Execute(CommandContext context)
        {
            List<SearchStringModel> list = SearchStringModel.ExtractSearchQuery(context.Parameters.GetValues("url")[0].Replace("\"", string.Empty));
            string jobName = Translate.Text("Applying Deletions");
            string title = Translate.Text("Deleting All Items");
            string icon = "~/icon/Applications/32x32/document_error.png";
            if (context.Items.Length > 0)
            {
                object[] parameters = new object[]
                {
                    context.Items[0],
                    list,
                    Context.User
                };
                ProgressBox.Execute(jobName, title, icon, new ProgressBoxMethod(this.StartProcess), parameters);
                SheerResponse.Alert(Translate.Text("Finished Deleting all items"), new string[0]);
            }
        }

        private void StartProcess(params object[] parameters)
        {
            Item item = (Item)parameters[0];
            SitecoreIndexableItem sitecoreIndexableItem = item;
            if (sitecoreIndexableItem == null)
            {
                Log.Error("Delete Items - Unable to cast current item - " + parameters[0].GetType().FullName, this);
                return;
            }
            List<SearchStringModel> list = (List<SearchStringModel>)parameters[1];
            foreach (SearchStringModel current in list)
            {
                if (current.Operation == null)
                {
                    current.Operation = "should";
                }
            }
            User account = (User)parameters[2];
            using (IProviderSearchContext providerSearchContext = ContentSearchManager.GetIndex(sitecoreIndexableItem).CreateSearchContext(SearchSecurityOptions.Default))
            {
                IQueryable<SitecoreUISearchResultItem> queryable = LinqHelper.CreateQuery<SitecoreUISearchResultItem>(providerSearchContext, list, sitecoreIndexableItem, null);
                foreach (SitecoreUISearchResultItem current2 in queryable)
                {
                    //Sitecore.Support.170415
                    Item item2 = current2.GetItem();
                    if (item2.Paths.IsDescendantOf(item))
                    {
                        if (item2 != null)
                        {
                            using (new SecurityEnabler())
                            {
                                if (!item2.Security.CanDelete(account))
                                {
                                    continue;
                                }
                            }
                            if (Context.Job != null)
                            {
                                Context.Job.Status.Messages.Add(item2.Paths.FullPath);
                            }
                            item2.Recycle();
                        }
                    }
                    //
                }
            }
        }
    }
}