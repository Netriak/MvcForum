using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq.SqlClient;
using System.Web;
using System.Web.Security;
using System.Text;

namespace MvcForum.Models
{
    public enum BuildInRole
    {
        Administrator = 1,
        RegisteredUser = 2,
        Everyone = 3
    }

    public enum BuildInUser
    {
        Guest = 11,
    }

    public enum BuildInCategory
    {
        Root = 7
    }

    public class ForumRespository : IDisposable
    {
        ForumDataContext db = new ForumDataContext();

        static void FillCategoryIDListRecursively(List<int> ListToFill, Forum_Category InitialCategory)
        {
            ListToFill.Add(InitialCategory.CategoryID);
            if (!InitialCategory.InheritPermissions) return;
            FillCategoryIDListRecursively(ListToFill, InitialCategory.Category1);
        }

        public Forum_Permission GetPermissionSetByID(int id)
        {
            return db.Forum_Permissions.SingleOrDefault(P => P.PermissionID == id);
        }

        public void DeletePermission(Forum_Permission ToDelete)
        {
            db.Forum_Permissions.DeleteOnSubmit(ToDelete);
        }

        public void AddPermission(Forum_Permission ToAdd)
        {
            db.Forum_Permissions.InsertOnSubmit(ToAdd);
        }

        public void AddPermissionsLink(Forum_PermissionsLink ToAdd)
        {
            db.Forum_PermissionsLinks.InsertOnSubmit(ToAdd);
        }

        public void DeletePermissionsLink(Forum_PermissionsLink ToDelete)
        {
            db.Forum_PermissionsLinks.DeleteOnSubmit(ToDelete);
        }

        public IQueryable<Forum_PermissionsLink> GetPermissionLinks()
        {
            return db.Forum_PermissionsLinks;
        }

        Dictionary<int, IEnumerable<Forum_Permission>> PermissionCache = new Dictionary<int,IEnumerable<Forum_Permission>>();

        public IQueryable<Forum_Permission> GetAllPermissionSets()
        {
            return db.Forum_Permissions;
        }

        public IQueryable<Forum_PermissionsLink> GetPermissionLinks(Forum_Category Category)
        {
            var CategoryIDList = new List<int>();
            FillCategoryIDListRecursively(CategoryIDList, Category);
            return from L in db.Forum_PermissionsLinks
                   where CategoryIDList.Contains(L.CategoryID)
                   select L;
        }

        public IEnumerable<Forum_Permission> GetPermissions(Forum_Category Category, Forum_User User)
        {
            if (!PermissionCache.ContainsKey(Category.CategoryID))
            {
                var CategoryIDList = new List<int>();
               FillCategoryIDListRecursively(CategoryIDList, Category);

                var Roles = GetUserRoles(User).Select(R => R.RoleID);
                PermissionCache.Add(Category.CategoryID, from L in db.Forum_PermissionsLinks
                                  where CategoryIDList.Contains(L.CategoryID) && Roles.Contains(L.RoleID)
                                  join P in db.Forum_Permissions on L.PermissionID equals P.PermissionID
                                  select P);
            }
            return PermissionCache[Category.CategoryID];
        }

        public Forum_Category GetCategoryByID(int id)
        {
            return db.Forum_Categories.SingleOrDefault(C => C.CategoryID == id);
        }

        public IQueryable<Forum_Thread> GetMatchingThreads(string MatchPattern)
        {
            return from T in db.Forum_Threads where SqlMethods.Like(T.Title, MatchPattern) select T;
        }

        public IQueryable<Forum_Post> GetMatchingPosts(string MatchPattern)
        {
            return from P in db.Forum_Posts where SqlMethods.Like(P.PostText, MatchPattern) select P;
        }

        public void SetLastPost(Forum_Thread Thread, Forum_User User, int PostNumber, bool IncreaseOnly = true)
        {
            var Last = db.Forum_ViewedPosts.SingleOrDefault(P => P.Forum_User == User && P.Forum_Thread == Thread);
            if (Last == null)
            {
                Last = new Forum_ViewedPost();
                Last.Forum_Thread = Thread;
                Last.Forum_User = User;
                db.Forum_ViewedPosts.InsertOnSubmit(Last);
            }
            else if (Last.LastPost >= PostNumber && IncreaseOnly) return;
            Last.LastPost = PostNumber;
        }

        public int GetLastPost(Forum_Thread Thread, Forum_User User)
        {
            var Last = db.Forum_ViewedPosts.SingleOrDefault(P => P.Forum_User == User && P.Forum_Thread == Thread);
            return Last == null ? 0 : Last.LastPost;
        }

        public void AddThread(Forum_Thread NewThread)
        {
            db.Forum_Threads.InsertOnSubmit(NewThread);
        }

        public void Save()
        {
            db.SubmitChanges();
        }

        public IQueryable<Forum_Thread> GetAllThreads()
        {
            return db.Forum_Threads;
        }

        public IQueryable<Forum_Category> GetAllCategories()
        {
            return db.Forum_Categories;
        }

        public void AddCategory(Forum_Category ToAdd)
        {
            db.Forum_Categories.InsertOnSubmit(ToAdd);
        }

        public IQueryable<Forum_Category> GetSortedCategories(int ParentID)
        {
            //return from C in db.Forum_Categories orderby C.Priority descending, C.Name select C;
            return from C in db.Forum_Categories where C.ParentID == ParentID orderby C.Priority descending, C.Name select C;
        }

        public IQueryable<Forum_Thread> GetSortedThreads(Forum_User Participant)
        {
            return (from P in db.Forum_Posts where P.Forum_User == Participant select P.Forum_Thread).Distinct().OrderByDescending(T => T.LastPostTime);
        }

        public IQueryable<Forum_Thread> GetSortedThreads()
        {
            return from T in db.Forum_Threads orderby T.LastPostTime descending select T;
        }

        public IQueryable<Forum_Thread> GetSortedThreads(int CategoryID)
        {
            return from T in db.Forum_Threads where T.CategoryID == CategoryID orderby T.LastPostTime descending select T;
        }

        public Forum_Post GetPostByID(int id)
        {
            return db.Forum_Posts.SingleOrDefault(P => P.PostID == id);
        }

        public Forum_Thread GetThreadByID(int id)
        {
            return db.Forum_Threads.SingleOrDefault(T => T.ThreadID == id);
        }

        public void DeletePost(Forum_Post ToDelete)
        {
            ToDelete.Forum_Thread.Posts--;
            db.Forum_Posts.DeleteOnSubmit(ToDelete);
        }

        public void DeleteThreads(IEnumerable<Forum_Thread> ThreadsToDelete)
        {
            db.Forum_ViewedPosts.DeleteAllOnSubmit(from V in db.Forum_ViewedPosts where ThreadsToDelete.Contains(V.Forum_Thread) select V);
            db.Forum_Posts.DeleteAllOnSubmit(from P in db.Forum_Posts where ThreadsToDelete.Contains(P.Forum_Thread) select P);
            db.Forum_Threads.DeleteAllOnSubmit(ThreadsToDelete);
        }

        public void DeleteThread(Forum_Thread ToDelete)
        {
            db.Forum_ViewedPosts.DeleteAllOnSubmit(from V in db.Forum_ViewedPosts where V.ThreadID == ToDelete.ThreadID select V);
            db.Forum_Posts.DeleteAllOnSubmit(ToDelete.Forum_Posts);
            db.Forum_Threads.DeleteOnSubmit(ToDelete);
        }

        public void DeleteCategory(Forum_Category ToDelete)
        {
            db.Forum_Categories.DeleteOnSubmit(ToDelete);
        }

        public bool UserExists(string UserName)
        {
            return db.Forum_Users.Any(u => u.Username == UserName);
        }

        public Forum_User GetUser(string UserName)
        {
            if (String.IsNullOrWhiteSpace(UserName)) return GetUserByID((int)BuildInUser.Guest);
            return db.Forum_Users.SingleOrDefault(u => u.Username == UserName);
        }

        public Forum_User GetUserByID(int nID)
        {
            return db.Forum_Users.SingleOrDefault(u => u.UserID == nID);
        }

        public void AddUser(Forum_User NewUser)
        {
            db.Forum_Users.InsertOnSubmit(NewUser);
        }

        public void DeleteUser(Forum_User ToDelete)
        {
            db.Forum_Users.DeleteOnSubmit(ToDelete);
        }

        public IQueryable<Forum_User> GetAllUsers(bool IncludeGuest = false)
        {
            IQueryable<Forum_User> Users = db.Forum_Users;
            if (!IncludeGuest)
                Users = Users.Where(U => U.UserID != (int)BuildInUser.Guest);
            return Users;
        }

        public IEnumerable<Forum_Role> GetUserRoles(Forum_User User)
        {
            var Roles = new List<Forum_Role>();
            Roles.Add(GetBuildInRole(BuildInRole.Everyone)); // Everyone includes everyone

            if (User == null || User.UserID == (int)BuildInUser.Guest) return Roles; // Guests are only part of Everyone.

            Roles.Add(GetBuildInRole(BuildInRole.RegisteredUser)); // Not a guest: Registered user at least

            var UserRoles = from L in db.Forum_UserRoleLinks where L.UserID == User.UserID join R in db.Forum_Roles on L.RoleID equals R.RoleID select R;
            return Roles.Concat(UserRoles);
        }

        public IQueryable<Forum_User> GetUsersInRole(Forum_Role Role, IQueryable<Forum_User> UsersToSearchIn)
        {
            return from L in db.Forum_UserRoleLinks where L.RoleID == Role.RoleID join U in UsersToSearchIn on L.UserID equals U.UserID select U;
        }

        public IQueryable<Forum_User> GetUsersInRole(Forum_Role Role)
        {
            return GetUsersInRole(Role, db.Forum_Users);
        }

        public bool UserInRole(Forum_User User, BuildInRole Role)
        {
            return UserInRole(User, (int)Role);
        }

        public bool UserInRole(Forum_User User, Forum_Role Role)
        {
            return UserInRole(User, Role.RoleID);
        }

        public bool UserInRole(Forum_User User, int RoleID)
        {
            if (RoleID == (int)BuildInRole.Everyone) return true;
            if (User == null) return false;
            if (RoleID == (int)BuildInRole.RegisteredUser) return true;
            return db.Forum_UserRoleLinks.SingleOrDefault(L => L.RoleID == RoleID && L.UserID == User.UserID) != null;
        }

        public Forum_Role GetBuildInRole(BuildInRole Role)
        {
            return db.Forum_Roles.SingleOrDefault(R => R.RoleID == (int)Role);
        }

        public Forum_Role GetRole(int id)
        {
            return db.Forum_Roles.SingleOrDefault(R => R.RoleID == id);
        }

        public IQueryable<Forum_Role> GetAllRoles()
        {
            return db.Forum_Roles;
        }

        public void DeleteRole(Forum_Role Role)
        {
            db.Forum_UserRoleLinks.DeleteAllOnSubmit(from L in db.Forum_UserRoleLinks where L.RoleID == Role.RoleID select L);
            db.Forum_Roles.DeleteOnSubmit(Role);
        }

        public void AddRole(Forum_Role Role)
        {
            db.Forum_Roles.InsertOnSubmit(Role);
        }

        public bool RoleHasUsers(Forum_Role Role)
        {
            return (from L in db.Forum_UserRoleLinks where L.RoleID == Role.RoleID select L).FirstOrDefault() != null;
        }

        public bool AddUserRoleLink(Forum_Role Role, Forum_User User)
        {
            if (UserInRole(User, Role)) return false;
            var Link = new Forum_UserRoleLink();
            Link.RoleID = Role.RoleID;
            Link.UserID = User.UserID;
            db.Forum_UserRoleLinks.InsertOnSubmit(Link);
            return true;
        }

        public void DeleteUserRoleLink(Forum_UserRoleLink Link)
        {
            db.Forum_UserRoleLinks.DeleteOnSubmit(Link);
        }

        public void RemoveUserFromRole(Forum_User User, Forum_Role Role)
        {
            Forum_UserRoleLink Link = db.Forum_UserRoleLinks.SingleOrDefault(L => L.Forum_Role == Role && L.Forum_User == User);
            if (Link == null) return;
            db.Forum_UserRoleLinks.DeleteOnSubmit(Link);
        }

        public void RemoveUsersFromRoles(IQueryable<Forum_User> Users, IQueryable<Forum_Role> Roles)
        {
            var UserLinks = from U in Users join L in db.Forum_UserRoleLinks on U.UserID equals L.UserID select L;
            var RoleLinks = from R in Roles join L in db.Forum_UserRoleLinks on R.RoleID equals L.RoleID select L;
            var Links = UserLinks.Intersect(RoleLinks);
            db.Forum_UserRoleLinks.DeleteAllOnSubmit(Links);
        }

        public IQueryable<Forum_User> FindUsersByName(string NamePattern)
        {
            return from U in db.Forum_Users where SqlMethods.Like(U.Username, NamePattern, '\\') select U;
        }

        public IQueryable<Forum_User> FindUsersByEmail(string EmailPattern)
        {
            return from U in db.Forum_Users where SqlMethods.Like(U.Email, EmailPattern, '\\') select U;
        }

        public void AddSessionToDB(Forum_Session ToAdd)
        {
            db.Forum_Sessions.InsertOnSubmit(ToAdd);
        }

        public Forum_Session GetSession(string SessionID)
        {
            return db.Forum_Sessions.SingleOrDefault(S => S.SessionGUID == SessionID);
        }

        public void RemoveUserSession(string SessionID)
        {
            db.Forum_Sessions.DeleteOnSubmit(GetSession(SessionID));
        }
        
        public void RemoveUserSessions(int UserID)
        {
            db.Forum_Sessions.DeleteAllOnSubmit(db.Forum_Sessions.Where(S => S.UserID == UserID));
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}