using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Web.Mvc;

namespace MvcForum.Models
{
    public class AdminViewModel : MasterViewModel
    {
        // For now, no admin specific shared information, but that may change.
    }

    public class AdminOverviewModel : AdminViewModel
    {
        public int page;
        public int LastPage;
        public List<AdminNamedID> Users;
        public List<AdminNamedID> UserGroups;
        public AdminCategory RootCategory;
        public List<AdminNamedID> PermissionSets;
    }

    public class AdminUpdateUser : AdminViewModel
    {
        [RegularExpression(@"^[-\w= ]*\w$", ErrorMessage = "Invalid Username")]
        [StringLength(25, MinimumLength = 3, ErrorMessage = "Too short")]
        [Remote("UserNameAvailable", "Account", ErrorMessage = "Username already exists")]
        public string Reg_Username { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", ErrorMessage = "Not a valid email address")]
        [StringLength(70, ErrorMessage = "Maximum of 70 characters")]
        public string Reg_Email { get; set; }

        [DisplayName("New Password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Minimum of 6 Characters")]
        public string Reg_Password { get; set; }

        [DisplayName("Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Reg_Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public int id { get; set; }

        public string OldUserName;
        public string OldEmail;
    }

    public class AdminNamedID
    {
        public string Name;
        public int ID;
    }

    public class AdminPermissionLink
    {
        public AdminNamedID Category, PermissionSet, UserGroup;
    }

    public class AdminPermissionLinkEditors : AdminViewModel
    {
        public List<AdminPermissionLink> PermissionLinkList;
        public List<AdminNamedID> UserGroups;
        public List<AdminNamedID> Categories;
        public List<AdminNamedID> PermissionSets;

        public AdminNamedID FixedNamedID;

        public FixedSet Fixed;

        public enum FixedSet
        {
            UserGroups,
            Categories,
            PermissionSets
        }
    }

    public class AdminPermissionSetModel : AdminPermissionLinkEditors
    {
        public Forum_Permission PermissionSet;
    }

    public class AdminCategory
    {
        public int Priority;
        public int id;
        public string Name;
        public bool AllowPosts;
        public bool InheritPermissions;
        public List<AdminCategory> Children = new List<AdminCategory>();
    }

    public class AdminCategoryModel : AdminPermissionLinkEditors
    {
        public List<AdminNamedID> MoveCategoryToOptions = new List<AdminNamedID>();
        public AdminCategory CurrentCategory;
        public AdminCategory Root;
    }

    public class AdminDeleteCategoryViewModel : AdminViewModel
    {
        public string CategoryName;
        public bool HasChildCategories;
        public List<AdminNamedID> MoveChildrenToOptions = new List<AdminNamedID>();
        public List<AdminNamedID> MovePostsToOptions = new List<AdminNamedID>();
        public int ThreadCount;
    }

    public class AdminUserGroupModel : AdminPermissionLinkEditors
    {
        public string Name;
        public int id;
        public List<AdminNamedID> CurrentGroupUsers;
        
        public int page;
        public int LastPage;
        public List<AdminNamedID> AllUsers;

        // BuildinGroup settings
        public bool CanBeDeleted;
        public bool HasMembers;
        public bool HasPermissions;

        // Permissions
        public bool IsAllowedSearch;
    }
}