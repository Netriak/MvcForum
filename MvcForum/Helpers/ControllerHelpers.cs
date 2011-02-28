using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;
using System.Text;

namespace MvcForum.Controllers
{
    public static class Extentions
    {
        public static List<C> ToClassList<C, T>(this IQueryable<T> Data, Func<T, C> ConvertionFunction)
        {
            var NewList = new List<C>();
            foreach (var DataPoint in Data)
            {
                NewList.Add(ConvertionFunction(DataPoint));
            }
            return NewList;
        }
    }
    public class RedirectToRouteResultEx : RedirectToRouteResult
    {

        public RedirectToRouteResultEx(RouteValueDictionary values)
            : base(values)
        {
        }

        public RedirectToRouteResultEx(string routeName, RouteValueDictionary values)
            : base(routeName, values)
        {
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var destination = new StringBuilder();

            var helper = new UrlHelper(context.RequestContext);
            destination.Append(helper.RouteUrl(RouteName, RouteValues));

            //Add href fragment if set
            if (!string.IsNullOrEmpty(Fragment))
            {
                destination.AppendFormat("#{0}", Fragment);
            }

            context.HttpContext.Response.Redirect(destination.ToString(), false);
        }
        
        public string Fragment { get; set; }
    }

    public static class RedirectToRouteResultExtensions
    {
        public static RedirectToRouteResultEx AddFragment(this RedirectToRouteResult result, string fragment)
        {
            return new RedirectToRouteResultEx(result.RouteName, result.RouteValues)
            {
                Fragment = fragment
            };
        }
    }

}