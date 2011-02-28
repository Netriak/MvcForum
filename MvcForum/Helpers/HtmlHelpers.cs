using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Text;
using MvcForum.Models;

namespace MvcForum.Helpers
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString ActionLinkButton(this HtmlHelper Html, string Buttontext, string ActionName, object routeValues, bool Disabled = false)
        {
            UrlHelper U = new UrlHelper(Html.ViewContext.RequestContext);
            string URL = U.Action(ActionName, routeValues);
            string Button = String.Format("<button type=\"button\"{1}>{0}</button>", Buttontext, Disabled ? " disabled=\"disabled\"" : "");
            string HtmlString;
            if (!Disabled) 
                HtmlString = String.Format("<a class=\"buttonlink\" href=\"{0}\">{1}</a>", URL, Button);
            else
                HtmlString = Button;

            return MvcHtmlString.Create(HtmlString);
        }

        public static MvcHtmlString NavigationMenu(this HtmlHelper Html, MasterViewModel Model)
        {
            if (Model == null || Model.LinkList == null || Model.LinkList.Count == 0)
            {
                return MvcHtmlString.Create("<a href=\"/\">Index</a>");
            }
            StringBuilder Builder = new StringBuilder();
            if (Model.IncludeIndex)
                Builder.Append("<a href=\"/\">Index</a>");
            UrlHelper U = new UrlHelper(Html.ViewContext.RequestContext);
            var Last = Model.LinkList.Last();
            bool First = true;
            foreach (var Link in Model.LinkList)
            {
                if (Model.IncludeIndex || !First)
                    Builder.Append(" &gt; ");
                First = false;
                if (Link != Last || Model.LastIsLink)
                {
                    Builder.Append(String.Format("<a href=\"{0}\">{1}</a>", U.Action(Link.ActionName,Link.ControllerName, Link.RouteValues), Html.Encode(Link.Name)));
                } else
                {
                    Builder.Append(String.Format("{0}", Html.Encode(Link.Name)));
                }
            }
            return MvcHtmlString.Create(Builder.ToString());
        }

        public static MvcHtmlString PageNavigationList(this HtmlHelper Html, string Action, int CurrentPage, int LastPage, int ID, string Class)
        {
            if (String.IsNullOrEmpty(Action))
                Action = Html.ViewContext.RouteData.Values["action"].ToString();

            var U = new UrlHelper(Html.ViewContext.RequestContext);
            var output = new StringBuilder();
            output.Append(String.Format("<ul class=\"pagemenu {0}\">", Class));
            for (int x = 1; x <= LastPage; x++)
            {
                object Param;   // Since the anonymous classes are not of the same type, the object Param variable cannot be filled with an inline if without explicit casting
                if (ID != 0) Param = new { id = ID, page = x }; else Param = new { page = x };
                if (x != CurrentPage)
                    output.Append(string.Format("<li><a href=\"{0}\">{1}</a></li>", U.Action(Action, Param), x));
                else
                    output.Append(String.Format("<li><span>{0}</span></li>", x));
            }
            output.Append("</ul>");
            return MvcHtmlString.Create(output.ToString());
        }

        static CultureInfo Culture = CultureInfo.CreateSpecificCulture("en-US");

        public static string ToForumDateString(this DateTime Time)
        {
            return Time.ToString("f", Culture);
        }

        public static MvcHtmlString ImageLink(string Title, string Image, string Url)
        {
            string Link = String.Format("<a href=\"{0}\" class=\"ImageLink\"><img src=\"/Content/{1}\" title=\"{2}\" alt=\"{1}\" /></a>", 
                HttpUtility.HtmlAttributeEncode(HttpUtility.UrlPathEncode(Url)),
                HttpUtility.HtmlAttributeEncode(HttpUtility.UrlPathEncode(Image)),
                HttpUtility.HtmlAttributeEncode(Title));

            return MvcHtmlString.Create(Link);
        }

        public static MvcHtmlString Image(this HtmlHelper Html, string Title, string Image)
        {
            string Link = String.Format("<img src=\"/Content/{0}\" title=\"{1}\" alt=\"{1}\" />",
                 HttpUtility.HtmlAttributeEncode(HttpUtility.UrlPathEncode(Image)),
                 HttpUtility.HtmlAttributeEncode(Title));

            return MvcHtmlString.Create(Link);
        }

        public static MvcHtmlString ImageActionLink(this HtmlHelper Html, string Title, string Image, string ActionName, object RouteValues, string Fragment)
        {
            UrlHelper U = new UrlHelper(Html.ViewContext.RequestContext);
            return ImageLink(Title, Image, String.Format("{0}#{1}", U.Action(ActionName, RouteValues), Fragment));
        }

        public static MvcHtmlString ImageActionLink(this HtmlHelper Html, string Title, string Image, string ActionName, object RouteValues)
        {
            UrlHelper U = new UrlHelper(Html.ViewContext.RequestContext);
            return ImageLink(Title, Image, U.Action(ActionName, RouteValues));
        }

        public static MvcHtmlString ImageActionLink(this HtmlHelper Html, string Title, string Image, string ActionName)
        {
            UrlHelper U = new UrlHelper(Html.ViewContext.RequestContext);
            return ImageLink(Title, Image, U.Action(ActionName));
        }
    }

}