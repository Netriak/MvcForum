using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcForum.Models;
using System.Web.Security;
using MvcForum.Helpers;

namespace MvcForum.Controllers
{
    public class AdminController : MVCForumController
    {
        const int UsersPerPage = 4;

        new public ActionResult User(AdminUpdateUser model)
        {
            model.AddNavigation("Admin Panel", "Overview", "Admin", null);
            model.AddNavigation("Update User");
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);

                if (!UserIdentity.IsAdmin)
                    return AuthenticationHelper.AccessDeniedView(model); // Administrating the forum requires the user to be an Admin.

                Forum_User UpdateUser = db.GetUserByID(model.id);

                if (UpdateUser == null)
                    return NotFoundView("User");

                model.OldEmail = UpdateUser.Email;
                model.OldUserName = UpdateUser.Username;
                
                if (IsHttpPost && ModelState.IsValid)
                {
                    if (!AntiForgeryTokenValid)
                    {
                        ModelState.AddModelError("AntiForgery", "The antiforgery token was invalid.");
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(model.Reg_Username))
                            UpdateUser.Username = model.Reg_Username;

                        if (!String.IsNullOrEmpty(model.Reg_Email))
                            UpdateUser.Email = model.Reg_Email;

                        if (!String.IsNullOrEmpty(model.Reg_Password))
                        {
                            var NewPassword = ((ForumMembershipProvider)Membership.Provider).CalculateSaltedPasswordHash(model.Reg_Password, UpdateUser.Salt);
                            UpdateUser.PasswordHash = NewPassword;
                        }

                        db.Save();

                        return RedirectToAction("Overview");
                    }
                } 

                return View(model);
            }
        }

        AdminCategory RecursivelyFillCategoryTree(ForumRespository db, Forum_Category ToFill, AdminCategoryModel model = null, int ID = 0)
        {
            AdminCategory newCategory = new AdminCategory()
            {
                AllowPosts = ToFill.AllowPosts,
                id = ToFill.CategoryID,
                InheritPermissions = ToFill.InheritPermissions,
                Name = ToFill.Name,
                Priority = ToFill.Priority
            };

            if (newCategory.id == ID)
                model.CurrentCategory = newCategory;

            var SortedCategories = db.GetSortedCategories(ToFill.CategoryID);

            if (!ToFill.AllowPosts)
                SortedCategories = SortedCategories.OrderByDescending(C => C.AllowPosts);

            foreach (var Child in SortedCategories)
                newCategory.Children.Add(RecursivelyFillCategoryTree(db, Child, model, ID));

            return newCategory;
        }

        void HandlePermissionsLinkUpdates()
        {
            if (!IsHttpPost) return;
            if (!AntiForgeryTokenValid) return;
            var Form = HttpContext.Request.Form;
            string Button = Form["permissionslinkbutton"];
            if (String.IsNullOrWhiteSpace(Button)) return;
            
            int CategoryID, PermissionsID, RoleID;

            try {
                CategoryID = Convert.ToInt32(Form["Category"]);
                PermissionsID = Convert.ToInt32(Form["PermissionSet"]);
                RoleID = Convert.ToInt32(Form["UserGroup"]);
            } catch (FormatException)
            {
                ModelState.AddModelError("InvalidData", "Numbers are invalid");
                return;
            } catch (OverflowException)
            {
                ModelState.AddModelError("InvalidData", "Numbers are too big");
                return;
            }

            using (ForumRespository db = new ForumRespository())
            {
                var PermissionsLink = db.GetPermissionLinks().SingleOrDefault(L => L.CategoryID == CategoryID && L.PermissionID == PermissionsID && L.RoleID == RoleID);

                if (Button == "Add")
                {
                    if (PermissionsLink != null)
                    {
                        ModelState.AddModelError("AddLinkError", "Permission link already exists");
                        return;
                    }

                    db.AddPermissionsLink(new Forum_PermissionsLink() { CategoryID = CategoryID, PermissionID = PermissionsID, RoleID = RoleID });
                        
                    try
                    {
                        db.Save();
                    }
                    catch
                    {
                        ModelState.AddModelError("AddLinkError", "Unable to add new permissions link");
                    }
                }
                else if (Button == "Delete")
                {
                    if (PermissionsLink == null)
                    {
                        ModelState.AddModelError("DeleteLinkError", "Permission link doesn't exists");
                        return;
                    }

                    db.DeletePermissionsLink(PermissionsLink);
                    try
                    {
                        db.Save();        
                    }
                    catch
                    {
                        ModelState.AddModelError("DeleteLinkError", "Unable to delete permissions link");
                    }
                }
            }
        }



        public ActionResult DeleteCategory(int id, int? MovePostsDestination, string MovePostOption, int? MoveChildrenDestination)
        {
            var model = new AdminDeleteCategoryViewModel();
            model.AddNavigation("Admin Panel", "Overview", "Admin", null);
            model.AddNavigation("Delete Category");
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);

                if (!UserIdentity.IsAdmin)
                    return AuthenticationHelper.AccessDeniedView(model); // Administrating the forum requires the user to be an Admin.
                
                Forum_Category CurrentCategory = db.GetCategoryByID(id);

                if (CurrentCategory == null)
                    return NotFoundView("Category");

                if (CurrentCategory.CategoryID == (int)BuildInCategory.Root)
                    return RedirectToAction("Overview");

                if (IsHttpPost && AntiForgeryTokenValid)
                {
                    db.DeleteCategory(CurrentCategory);

                    if (CurrentCategory.AllowPosts)
                    {
                        bool DeletePosts = String.Equals(MovePostOption, "Delete", StringComparison.Ordinal);
                        if (!DeletePosts && MovePostsDestination == null)
                            return RedirectToAction("Overview");

                        if (!DeletePosts)
                        {
                            foreach (var Thread in CurrentCategory.Forum_Threads)
                            {
                                Thread.CategoryID = (int)MovePostsDestination;
                            }
                        }
                        else
                        {
                            db.DeleteThreads(CurrentCategory.Forum_Threads);
                        }
                    }

                    if (CurrentCategory.Forum_Categories.Count > 0)
                    {
                        var NewParent = db.GetCategoryByID((int)MoveChildrenDestination);

                        if (NewParent == null)
                            return RedirectToAction("Overview");

                        foreach (var Child in CurrentCategory.Forum_Categories)
                        {
                            Child.ParentID = NewParent.CategoryID;
                        }
                    }

                    db.Save();
                    return RedirectToAction("Category");
                }

                model.ThreadCount = CurrentCategory.Forum_Threads.Count;
                model.HasChildCategories = CurrentCategory.Forum_Categories.Count > 0;
                model.CategoryName = CurrentCategory.Name;

                foreach (var Category in db.GetAllCategories())
                {
                    if (Category.CategoryID != (int)BuildInCategory.Root & Category.CategoryID != CurrentCategory.CategoryID)
                        model.MovePostsToOptions.Add(new AdminNamedID() { Name = Category.Name, ID = Category.CategoryID });
                    var Parent = Category;
                    while (Parent != null)
                    {
                        if (Parent == CurrentCategory) break;
                        Parent = Parent.Category1;
                    }
                    if (Parent != null) continue;

                    model.MoveChildrenToOptions.Add(new AdminNamedID() { Name = Category.Name, ID = Category.CategoryID });
                }
                
                return View(model);
            }
        }

        public ActionResult Category(int id, string NewCategory, string UpdateCategory, string MoveCategory)
        {
            var model = new AdminCategoryModel();
            model.AddNavigation("Admin Panel", "Overview", "Admin", null);
            model.AddNavigation("Edit Category");
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);

                if (!UserIdentity.IsAdmin)
                    return AuthenticationHelper.AccessDeniedView(model); // Administrating the forum requires the user to be an Admin.

                HandlePermissionsLinkUpdates();

                Forum_Category Root = db.GetCategoryByID((int)BuildInCategory.Root);

                Forum_Category CurrentCategory = id != 0 ? db.GetCategoryByID(id) : Root;

                if (CurrentCategory == null)
                    return NotFoundView("Category");

                bool IsRoot = Root == CurrentCategory;

                if (IsHttpPost && AntiForgeryTokenValid)
                {
                    if (!String.IsNullOrEmpty(NewCategory))
                    {
                        Forum_Category NewForumCategory = new Forum_Category();
                        NewForumCategory.ParentID = CurrentCategory.CategoryID;
                        NewForumCategory.Name = "Untitled Category";
                        NewForumCategory.InheritPermissions = true;
                        NewForumCategory.AllowPosts = false;
                        db.AddCategory(NewForumCategory);
                        db.Save();
                        return RedirectToAction("Category", new { id = NewForumCategory.CategoryID });
                    }
                    if (!String.IsNullOrEmpty(UpdateCategory) && CurrentCategory != Root)
                    {
                        var Form = Request.Form;
                        string NewName = Form["CurrentCategory.Name"];
                        bool InheritPermissions = !String.IsNullOrEmpty(Form["Inherit Permissions"]);
                        bool AllowPosts = !String.IsNullOrEmpty(Form["Allow Posts"]);
                        CurrentCategory.AllowPosts = AllowPosts;
                        CurrentCategory.InheritPermissions = InheritPermissions;
                        try
                        {
                            CurrentCategory.Priority = Convert.ToInt32(Form["CurrentCategory.Priority"]);
                        } catch { }
                        if (!String.IsNullOrWhiteSpace(NewName))
                            CurrentCategory.Name = NewName.Trim();

                        db.Save();
                    }
                    if (!String.IsNullOrEmpty(MoveCategory) && CurrentCategory != Root)
                    {
                        var Form = Request.Form;
                        int DestinationID = 0;
                        try
                        {
                            DestinationID = Convert.ToInt32(Form["MoveToDestination"]);
                        }
                        catch 
                        {}
                        
                        var Parent = db.GetCategoryByID(DestinationID);
                        if (Parent != null)
                        {
                            while (Parent != null)
                            {
                                if (Parent == CurrentCategory)
                                    break;
                                Parent = Parent.Category1;
                            }
                            if (Parent == null)
                            {
                                CurrentCategory.ParentID = DestinationID;
                                db.Save();
                            }
                        }
                    }
                }

                foreach (var Category in db.GetAllCategories())
                {
                    var Parent = Category;
                    while (Parent != null)
                    {
                        if (Parent == CurrentCategory) break;
                        Parent = Parent.Category1;
                    }
                    if (Parent != null) continue;
                    if (Category == CurrentCategory.Category1) continue;

                    model.MoveCategoryToOptions.Add(new AdminNamedID() { Name = Category.Name, ID = Category.CategoryID });
                }
                
                model.Root = RecursivelyFillCategoryTree(db, Root, model, CurrentCategory.CategoryID);

                model.UserGroups = db.GetAllRoles().Where(R => R.RoleID != (int)BuildInRole.Administrator).ToClassList(R => new AdminNamedID() { ID = R.RoleID, Name = R.Name });
                model.PermissionSets = db.GetAllPermissionSets().ToClassList(P => new AdminNamedID() { ID = P.PermissionID, Name = P.Name });
                model.Fixed = AdminPermissionLinkEditors.FixedSet.Categories;

                model.FixedNamedID = new AdminNamedID() { ID = model.CurrentCategory.id, Name = model.CurrentCategory.Name };
                model.PermissionLinkList = db.GetPermissionLinks(CurrentCategory).OrderBy(L => L.CategoryID).ToClassList(L => new AdminPermissionLink()
                {
                    Category = new AdminNamedID() { ID = L.CategoryID, Name = db.GetCategoryByID(L.CategoryID).Name },
                    UserGroup = new AdminNamedID() { ID = L.RoleID, Name = db.GetRole(L.RoleID).Name },
                    PermissionSet = new AdminNamedID() { ID = L.PermissionID, Name = db.GetPermissionSetByID(L.PermissionID).Name },
                });

                return View(model);
            }
        }

        public ActionResult PermissionSet(int id, string UpdatePermissions, string DeletePermissions, string NewPermissions)
        {
            var model = new AdminPermissionSetModel();
            model.AddNavigation("Admin Panel", "Overview", "Admin", null);
            model.AddNavigation("Edit Permission Set");
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);

                if (!UserIdentity.IsAdmin)
                    return AuthenticationHelper.AccessDeniedView(model); // Administrating the forum requires the user to be an Admin.

                HandlePermissionsLinkUpdates();

                Forum_Permission CurrentPermissionSet;

                if (id == 0)
                    CurrentPermissionSet = db.GetAllPermissionSets().First();
                else
                    CurrentPermissionSet = db.GetPermissionSetByID(id);

                if (CurrentPermissionSet == null)
                    return NotFoundView("Permission Set");

                if (IsHttpPost && AntiForgeryTokenValid)
                {
                    if (!String.IsNullOrEmpty(UpdatePermissions))
                    {
                        UpdateModel(CurrentPermissionSet, "PermissionSet");
                        db.Save();
                    } else if (!String.IsNullOrEmpty(DeletePermissions) && CurrentPermissionSet.Forum_PermissionsLinks.Count == 0 && db.GetAllPermissionSets().Count() > 1)
                    {
                        db.DeletePermission(CurrentPermissionSet);
                        db.Save();
                        return RedirectToAction("PermissionSet", new { id = 0 });
                    }
                    else if (!String.IsNullOrEmpty(NewPermissions))
                    {
                        var NewPermissionSet = new Forum_Permission();
                        NewPermissionSet.Name = "Unnamed";
                        db.AddPermission(NewPermissionSet);
                        db.Save();
                        return RedirectToAction("PermissionSet", new { id = NewPermissionSet.PermissionID });
                    }
                }

                model.PermissionSet = CurrentPermissionSet;

                model.UserGroups = db.GetAllRoles().Where(R => R.RoleID != (int)BuildInRole.Administrator).ToClassList(R => new AdminNamedID() { ID = R.RoleID, Name = R.Name });
                model.Categories = db.GetAllCategories().ToClassList(C => new AdminNamedID() { ID = C.CategoryID, Name = C.Name });
                model.PermissionSets = db.GetAllPermissionSets().ToClassList(P => new AdminNamedID() { ID = P.PermissionID, Name = P.Name });
                model.Fixed = AdminPermissionLinkEditors.FixedSet.PermissionSets;

                model.FixedNamedID = new AdminNamedID() { ID = CurrentPermissionSet.PermissionID, Name = CurrentPermissionSet.Name };
                model.PermissionLinkList = db.GetPermissionLinks().Where(L => L.PermissionID == CurrentPermissionSet.PermissionID).OrderBy(L => L.CategoryID).ToClassList(L => new AdminPermissionLink()
                {
                    Category = new AdminNamedID() { ID = L.CategoryID, Name = db.GetCategoryByID(L.CategoryID).Name },
                    UserGroup = new AdminNamedID() { ID = L.RoleID, Name = db.GetRole(L.RoleID).Name },
                    PermissionSet = model.FixedNamedID
                });

                return View(model);
            }
        }

        public ActionResult UserGroup(int id, int page, string UsersAddButton, string UsersRemoveButton, string NewGroup, string UpdateGroup, string DeleteGroup)
        {
            var model = new AdminUserGroupModel(){page = page};
            model.AddNavigation("Admin Panel", "Overview", "Admin", null);
            model.AddNavigation("Edit User Groups");
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);

                if (!UserIdentity.IsAdmin)
                    return AuthenticationHelper.AccessDeniedView(model); // Administrating the forum requires the user to be an Admin.

                HandlePermissionsLinkUpdates();

                Forum_Role CurrentRole;

                if (id != 0)
                    CurrentRole = db.GetRole(id);
                else
                    CurrentRole = db.GetAllRoles().First();

                if (CurrentRole == null)
                    return NotFoundView("User Group");


                model.id = CurrentRole.RoleID;
                model.CanBeDeleted = CurrentRole.CanBeDeleted;
                model.HasMembers = CurrentRole.RoleID != (int)BuildInRole.Everyone &&
                                   CurrentRole.RoleID != (int)BuildInRole.RegisteredUser;
                model.HasPermissions = CurrentRole.RoleID != (int)BuildInRole.Administrator;

                if (IsHttpPost && AntiForgeryTokenValid)
                {
                    if (!String.IsNullOrEmpty(UsersAddButton) && model.HasMembers)
                    {
                        int nID;
                        Forum_User ToAdd;
                        foreach (var ID in Request.Form.GetValues("Users"))
                        {
                            try
                            {
                                nID = Convert.ToInt32(ID);
                            }
                            catch
                            {
                                continue;
                            }
                            if (nID == (int)BuildInUser.Guest) continue;
                            ToAdd = db.GetUserByID(nID);
                            if (ToAdd == null) continue;
                            if (db.UserInRole(ToAdd, CurrentRole)) continue;
                            db.AddUserRoleLink(CurrentRole, ToAdd);
                        }
                        db.Save();
                    }
                    else if (!String.IsNullOrEmpty(UsersRemoveButton))
                    {
                        int nID;
                        var RemovedUsers = Request.Form.GetValues("Users");
                        if (RemovedUsers != null)
                        {
                            foreach (var ID in RemovedUsers)
                            {
                                try
                                {
                                    nID = Convert.ToInt32(ID);
                                }
                                catch
                                {
                                    continue;
                                }
                                if (CurrentRole.RoleID == (int)BuildInRole.Administrator && nID == CurrentUser.UserID) continue;
                                db.RemoveUserFromRole(db.GetUserByID(nID), CurrentRole);
                            }
                            db.Save();
                        }
                    }
                    else if (!String.IsNullOrEmpty(UpdateGroup) && model.HasPermissions)
                    {
                        if (model.CanBeDeleted)
                            CurrentRole.Name = Request.Form["Name"];
                        CurrentRole.AllowSearch = Request.Form["IsAllowedSearch"] != "false";
                        db.Save();
                    }
                    else if (!String.IsNullOrEmpty(NewGroup))
                    {
                        var NewRole = new Forum_Role();
                        NewRole.Name = "Unnamed";
                        NewRole.CanBeDeleted = true;
                        db.AddRole(NewRole);
                        db.Save();
                        return RedirectToAction("UserGroup", new { id = NewRole.RoleID });
                    }
                    else if (!String.IsNullOrEmpty(DeleteGroup) && model.CanBeDeleted && CurrentRole.Forum_UserRoleLinks.Count == 0)
                    {
                        db.DeleteRole(CurrentRole);
                        db.Save();
                        return RedirectToAction("UserGroup", new { id = 0 });
                    }
                }

                if (model.HasMembers)
                {
                    model.CurrentGroupUsers = db.GetUsersInRole(CurrentRole).ToClassList(U => new AdminNamedID(){ID = U.UserID, Name = U.Username});
                }
                
                model.UserGroups = db.GetAllRoles().ToClassList(R => new AdminNamedID() { ID = R.RoleID, Name = R.Name });
                model.Categories = db.GetAllCategories().ToClassList(C => new AdminNamedID() { ID = C.CategoryID, Name = C.Name });
                model.PermissionSets = db.GetAllPermissionSets().ToClassList(P => new AdminNamedID() { ID = P.PermissionID, Name = P.Name });
                model.Fixed = AdminPermissionLinkEditors.FixedSet.UserGroups;

                var Users = db.GetAllUsers().OrderBy(U => U.Username);
                model.LastPage = (Users.Count() - 1) / UsersPerPage + 1;

                model.AllUsers = Users.Skip((page - 1) * UsersPerPage).Take(UsersPerPage).ToClassList(U => new AdminNamedID() { ID = U.UserID, Name = U.Username });

                if (model.HasPermissions)
                {
                    model.FixedNamedID = new AdminNamedID() { ID = CurrentRole.RoleID, Name = CurrentRole.Name};
                    model.PermissionLinkList = db.GetPermissionLinks().Where(L => L.RoleID == id).OrderBy(L => L.CategoryID).ToClassList(L => new AdminPermissionLink()
                    {
                        Category = new AdminNamedID() { ID = L.CategoryID, Name = db.GetCategoryByID(L.CategoryID).Name },
                        PermissionSet = new AdminNamedID() { ID = L.PermissionID, Name = db.GetPermissionSetByID(L.PermissionID).Name },
                        UserGroup = model.FixedNamedID
                    });
                }

                model.Name = CurrentRole.Name;
                
                model.IsAllowedSearch = CurrentRole.AllowSearch;

                return View(model);
            }
        }


        public ActionResult Overview(int page)
        {
            var model = new AdminOverviewModel();
            model.AddNavigation("Admin Panel");
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);

                if (!UserIdentity.IsAdmin)
                    return AuthenticationHelper.AccessDeniedView(model); // Administrating the forum requires the user to be an Admin.

                model.page = page;

                var Users = db.GetAllUsers().OrderBy(U => U.Username);
                model.LastPage = (Users.Count() - 1) / UsersPerPage + 1;
                model.Users = Users.Skip((page - 1) * UsersPerPage).Take(UsersPerPage).ToClassList(U => new AdminNamedID() { ID = U.UserID, Name = U.Username });
                model.UserGroups = db.GetAllRoles().ToClassList(R => new AdminNamedID() { ID = R.RoleID, Name = R.Name });
                model.PermissionSets = db.GetAllPermissionSets().ToClassList(P => new AdminNamedID() { ID = P.PermissionID, Name = P.Name });
                model.RootCategory = RecursivelyFillCategoryTree(db, db.GetCategoryByID((int)BuildInCategory.Root));

                return View(model);
            }
        }

    }
}
