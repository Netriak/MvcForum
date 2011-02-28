using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Principal;
using MvcForum.Helpers;
using System.Web.Routing;

namespace MvcForum.Models
{
    public class ForumIdentity : IIdentity
    {
        string _UserName = "";
        int _UserID = 0;
        bool _IsAdmin;
        string _Session;

        public ForumIdentity(string UserName, int UserID, bool IsAdmin, string Session)
        {
            _UserName = UserName;
            _UserID = UserID;
            _IsAdmin = IsAdmin;
            _Session = Session;
        }

        #region IIdentity Members

        public string AuthenticationType
        {
            get { return "MvcForumAuthentication"; }
        }

        public bool IsAuthenticated
        {
            get { return _UserID > 0; }
        }

        public string Name
        {
            get { return _UserName; }
        }

        #endregion

        public int UserID
        {
            get { return _UserID; }
        }

        public bool IsAdmin
        {
            get { return _IsAdmin; }
        }

        public string SessionID
        {
            get { return _Session; }
        }
    }

    public class ForumPrinciple : IPrincipal
    {
        IIdentity _Identity;

        public ForumPrinciple()
        {
            _Identity = new ForumIdentity("", 0, false, "");
        }

        public ForumPrinciple(string UserName, int UserID, bool IsAdmin, string SessionID)
        {
            _Identity = new ForumIdentity(UserName, UserID, IsAdmin, SessionID);
        }

        #region IPrincipal Members

        public IIdentity Identity
        {
            get { return _Identity; }
        }

        public bool IsInRole(string role)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    public static class Authentication
    {
        public static ForumIdentity Identity
        {
            get
            {
                return HttpContext.Current.User.Identity as ForumIdentity; 
            }
        }

        public static void AuthenticateUser(ForumRespository db, int UserID)
        {
            string SessionID = Guid.NewGuid().ToString();
            
            HttpCookie SessionCookie = new HttpCookie("MvcForum", SessionID);
            SessionCookie.HttpOnly = true;
            SessionCookie.Expires = DateTime.Now.AddYears(50);
            HttpContext.Current.Response.Cookies.Add(SessionCookie);
            var SessionStore = new Forum_Session() { UserID = UserID, SessionGUID = SessionID };
            db.AddSessionToDB(SessionStore);
            db.Save();
        }

        public static void AuthenticateRequest()
        {
            var Context = HttpContext.Current;
            var Cookie = Context.Request.Cookies["MvcForum"];
            if (Cookie != null)
            {
                using (ForumRespository db = new ForumRespository())
                {
                    var Session = db.GetSession(Cookie.Value);
                    if (Session != null)
                    {
                        var ForumUser = Session.Forum_User;
                        bool Admin = db.UserInRole(ForumUser, BuildInRole.Administrator);
                        Context.User = new ForumPrinciple(ForumUser.Username, ForumUser.UserID, Admin, Session.SessionGUID);
                        return;
                    }
                }
            }
            Context.User = new ForumPrinciple();
        }

        public static void LogOff()
        {
            if (!Identity.IsAuthenticated) return;
            using (ForumRespository db = new ForumRespository())
            {
                db.RemoveUserSession(Identity.SessionID);
                db.Save();
            }
        }

        public static bool CheckCategoryPermissions(this ForumRespository db, Forum_Category Category, Forum_User User, Func<Forum_Permission, bool> PermissionCheck)
        {
            if (Identity.IsAdmin) return true; // Since Administrators can change rules, they might as well ignore them.

            var Permissions = db.GetPermissions(Category, User);
            foreach (var P in Permissions)
            {
                if (PermissionCheck(P)) return true;
            }
            return false;
        }

        public static bool CheckRolePermissions(this ForumRespository db, Forum_User User, Func<Forum_Role, bool> PermissionCheck)
        {
            if (Identity.IsAdmin) return true; // Since Administrators can change rules, they might as well ignore them.

            var Roles = db.GetUserRoles(User);
            foreach (var R in Roles)
            {
                if (PermissionCheck(R)) return true;
            }
            return false;
        }



    }
}
