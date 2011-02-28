using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcForum.Models;
using System.Web.Routing;
using System.Web.Security;
using MvcForum.Helpers;

namespace MvcForum.Controllers
{
    public class AccountController : MVCForumController
    {
        public ActionResult Settings()
        {
            var model = new MasterViewModel();
            model.AddNavigation("User Panel");

            if (!Request.IsAuthenticated)
                return AuthenticationHelper.AccessDeniedView(model); // Regardless of permissions, requires an account by neccecity
            
            return View(model);
        }

        [HttpGet]
        public ActionResult LogOn()
        {
            return Redirect("/");
        }

        [HttpGet]
        public ActionResult Register()
        {
            var model = new RegisterViewModel();
            model.AddNavigation("Register");
            return View(model);
        }

        [HttpPost]
        public ActionResult LogOn(LogonViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    using (ForumRespository db = new ForumRespository())
                    {
                        Forum_User ForumUser = db.GetUser(model.UserName);

                        Authentication.AuthenticateUser(db, ForumUser.UserID);

                        return Redirect(Request.UrlReferrer.ToString());
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }
            model.AddNavigation("Log on");
            return View(model);
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus;
                Membership.CreateUser(model.Reg_Username, model.Reg_Password, model.Reg_Email, null, null, true, null, out createStatus);
                if (createStatus == MembershipCreateStatus.Success)
                {
                    using (ForumRespository db = new ForumRespository())
                    {
                        Authentication.AuthenticateUser(db, db.GetUser(model.Reg_Username).UserID);
                        return Redirect("/");
                    }
                }
                else
                {
                    ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
                }
            }
            model.AddNavigation("Register");
            return View(model);
        }

        public JsonResult UserNameAvailable(string Reg_Username)
        {
            using (var db = new ForumRespository())
            {
                return Json(!db.UserExists(Reg_Username), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult LogOff()
        {
            Authentication.LogOff();
            return Redirect(Request.UrlReferrer.ToString());
        }
    }
}
