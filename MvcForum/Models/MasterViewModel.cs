using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcForum.Models
{
    public class AccessDeniedViewModel : MasterViewModel
    {
        public string ErrorMessage;
    }

    public class NotFoundViewModel : MasterViewModel
    {
        public string WhatItIsThatIsntFound;
    }

    public class MasterLink
    {
        public string Name;
        public string ActionName;
        public string ControllerName;
        public object RouteValues;
    }

    public class MasterViewModel
    {
        public bool IncludeIndex = true;
        public bool LastIsLink = false;
        public List<MasterLink> LinkList = new List<MasterLink>();

        public void AddNavigation(string Name)
        {
            MasterLink Link = new MasterLink();
            Link.Name = Name;
            LinkList.Add(Link);
        }

        public void AddNavigation(string Name, string Action, string ControllerName, object RouteValues)
        {
            MasterLink Link = new MasterLink()
            {
                ActionName = Action,
                Name = Name,
                RouteValues = RouteValues,
                ControllerName = ControllerName
            };
            LinkList.Add(Link);
        }

        public void AddNavigation(Forum_Thread Thread)
        {
            AddNavigation(Thread.Forum_Category);
            AddNavigation(Thread.Title, "ViewThread", "Forum", new { id = Thread.ThreadID, page = 1 });
        }

        public void AddNavigation(Forum_Category Category)
        {
            if (Category.Category1 != null)
                AddNavigation(Category.Category1);
            AddNavigation(Category.Name, "ViewCategory", "Forum", new { id=Category.CategoryID, page = 1});
        }

        public T DeriveTo<T>() where T : MasterViewModel, new()
        {
            return new T()
            {
                IncludeIndex = IncludeIndex,
                LastIsLink = LastIsLink,
                LinkList = LinkList
            };
        }
    }
}