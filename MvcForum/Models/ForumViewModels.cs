using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Web.Mvc;

namespace MvcForum.Models
{
    public class EditThreadViewModel : MasterViewModel
    {
        public bool AllowDelete;
        public bool AllowMove;
        public bool AllowLock;
        public bool IsLocked;
        public int CategoryID;
        public string CategoryName;
        public List<AdminNamedID> ValidMoveDestinations = new List<AdminNamedID>();
        public string ThreadName;
        public int id;
    }

    public class MyPostsViewModel : MasterViewModel
    {
        public int Page;
        public int PageCount;
        public List<ThreadInfoViewModel> ThreadInfoList = new List<ThreadInfoViewModel>();
    }

    public class RecentPostsViewModel : MasterViewModel
    {
        public List<ThreadInfoViewModel> ThreadInfoList = new List<ThreadInfoViewModel>();
    }

    public class SearchResultsViewModel : MasterViewModel
    {
        public string SearchString;
        public int ResultCount;
        public List<ThreadInfoViewModel> ThreadInfoList = new List<ThreadInfoViewModel>();
        public List<PostWithThread> PostInfoList = new List<PostWithThread>();
    }

    public class PostWithThread
    {
        public string ThreadName;
        public PostViewModel Post;
    }

    public class PostViewModel
    {
        public bool Locked;
        public int Page;
        public int ThreadID;
        public int PostNumber;
        public int PostID;
        public MvcHtmlString PostText;
        public DateTime PostTime;
        public UserViewModel Poster;
        public bool AllowEdit;
        public bool AllowDelete;
    }

    public class UserViewModel
    {
        public string Name;
        public int UserID;
    }

    public class WritePostViewModel : MasterViewModel
    {
        public int ResultPage;
        public int ResultPost;
        public bool EditTitle = false;
        public bool ShowPost = false;
        public int id { get; set; }
        public int ThreadID;
        public string Title;
        public MvcHtmlString PostHtml;

        [DisplayName("Title:")]
        [RegularExpression(@"^(\S{1,20}\s)*\S{1,20}$", ErrorMessage = "Invalid Title")]
        [StringLength(100, ErrorMessage = "The title of a thread may not exceed 100 characters")]
        public string ThreadTitle { get; set; }

        [Required(ErrorMessage="The post cannot be empty")]
        public string PostText { get; set; } 
    }

    public class ThreadViewModel : MasterViewModel
    {
        public int LastPage;
        public int Page { get; set; }
        public int Id { get; set; }
        public string ThreadTitle;
        public bool Locked;
        public bool AllowEditThread;
        public List<PostViewModel> PostList = new List<PostViewModel>();
    }

    public class ThreadInfoViewModel
    {
        public string ThreadTitle;
        public int ThreadID;
        public UserViewModel LastPoster;
        public DateTime LastPostTime;
        public int PostCount;
        public int PageCount;
        public bool Locked;
        public int LastViewedPost;
        public int LastViewedPostPage;
        public int LastViewedPostID;
    }

    public class CategoryViewModel : MasterViewModel
    {
        public string Name;
        public int id { get; set; }
        public int page { get; set; }
        public int PageCount;
        public bool AllowPosts = false; 
        public List<SubCategoryModel> SubCategories = new List<SubCategoryModel>();
        public List<ThreadInfoViewModel> Threads = new List<ThreadInfoViewModel>();
    }

    public class SubCategoryModel
    {
        public DateTime LastPostTime;
        public bool AllowPosts;
        public int ThreadCount;
        public int PostCount;
        public string Name;
        public int id;
        public List<SubCategoryModel> SubCategories = new List<SubCategoryModel>();
    }
}