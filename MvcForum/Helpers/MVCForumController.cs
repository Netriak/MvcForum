using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcForum.Models;
using System.Web.Security;
using MvcForum.Helpers;

namespace MvcForum.Helpers
{
    [ValidateInput(false), HandleError]
    public class MVCForumController : Controller
    {
        // temporary constants until such a time where such data is saved in the database.
        protected const int POSTS_PER_PAGE = 4;
        protected const int THREADS_PER_PAGE = 10;

        protected ForumIdentity UserIdentity
        {
            get
            {
                return Authentication.Identity;
            }
        }

        protected ActionResult NotFoundView(string WhatItIsThatIsntFound)
        {
            var Model = new NotFoundViewModel(){ WhatItIsThatIsntFound = WhatItIsThatIsntFound };
            return View("NotFound", Model);
        }

        protected Forum_User GetCurrentUser(ForumRespository db)
        {
            return db.GetUser(User.Identity.Name);
        }

        protected bool IsHttpPost
        {
            get { return Request.RequestType == "POST"; }
        }

        protected bool AntiForgeryTokenValid
        {
            get
            {
                var AntiForgery = new ValidateAntiForgeryTokenAttribute();
#pragma warning disable 618
                try
                {
                    AntiForgery.OnAuthorization(new AuthorizationContext(ControllerContext));
                }
#pragma warning restore 618
                catch
                {
                    return false;
                }
                return true;
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Authentication.AuthenticateRequest();
            base.OnActionExecuting(filterContext);
        }
    }
}