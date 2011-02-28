using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using MvcForum.Models;

namespace MvcForum
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Admin Overview panel
            routes.MapRoute(
                "Overview", // Route name
                "Admin/Overview/{page}", // URL with parameters
                new { controller = "Admin", action = "Overview", id = 0, page = 1 }); // Parameter defaults

            // Admin panel
            routes.MapRoute(
                "Admin", // Route name
                "Admin/{action}/{id}/{page}", // URL with parameters
                new { controller = "Admin", action = "Overview", id = 0, page = 1}); // Parameter defaults

            // MyPosts
            routes.MapRoute(
                "MyPosts", // Route name
                "MyPosts/{page}", // URL with parameters
                new { controller = "Forum", action = "MyPosts", page = 1 }); // Parameter defaults

            // Reply view
            routes.MapRoute(
                "Reply", // Route name
                "Reply/{id}/{QuoteId}", // URL with parameters
                new { controller = "Forum", action = "Reply", QuoteId = UrlParameter.Optional }); // Parameter defaults

            // Forum view
            routes.MapRoute(
                "Forum", // Route name
                "Forum/{id}/{page}", // URL with parameters
                new { controller = "Forum", action = "ViewCategory", page = 1} // Parameter defaults
            );

            // Account
            routes.MapRoute(
                "Account", // Route name
                "Account/{action}", // URL with parameters
                new { controller = "Account" } // Parameter defaults
            );
            
            // For permission errors
            routes.MapRoute(
                "AccessDenied", // Route name
                "AccessDenied", // URL with parameters
                new { controller = "Account", action="LogOn" } // Parameter defaults
            );

            // Miscellaneous
            routes.MapRoute(
                "ForumWithID", // Route name
                "{action}/{id}/{page}", // URL with parameters
                new { controller = "Forum", action = "ViewCategory", page=1 } // Parameter defaults
            );

            // Miscellaneous
            routes.MapRoute(
                "ForumActions", // Route name
                "{action}", // URL with parameters
                new { controller = "Forum", action = "ViewCategory", page = 1, id = (int)BuildInCategory.Root } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterRoutes(RouteTable.Routes);
            BeginRequest += new EventHandler(MvcApplication_BeginRequest);
            MvcForum.Helpers.PostParser.InitBBCodes();
        }

        void MvcApplication_BeginRequest(object sender, EventArgs e)
        {
            Response.AddHeader("X-Frame-Options", "DENY");
        }
    }
}