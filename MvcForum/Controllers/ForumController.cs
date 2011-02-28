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
    public class ForumController : MVCForumController
    {
        #region search

        [HttpGet]
        public ActionResult MyPosts(int Page)
        {
            using (ForumRespository db = new ForumRespository())
            {
                
                var Model = new MyPostsViewModel();
                Model.AddNavigation("My Posts");

                if (!Request.IsAuthenticated)
                    return AuthenticationHelper.AccessDeniedView(Model); // Regardless of permissions, requires an account by neccecity

                Forum_User CurrentUser = GetCurrentUser(db);

                var Threads = db.GetSortedThreads(CurrentUser);

                Model.PageCount = (Threads.Count() - 1) / THREADS_PER_PAGE + 1;
                Model.Page = Page;

                ThreadInfoModelFromThread(db, CurrentUser, Threads.Skip(THREADS_PER_PAGE * (Page - 1)).Take(THREADS_PER_PAGE), Model.ThreadInfoList);

                return View(Model);
            }
        }

        [HttpGet]
        public ActionResult RecentPosts()
        {
            using (ForumRespository db = new ForumRespository())
            {
                var Model = new RecentPostsViewModel();

                Model.AddNavigation("Recent Posts");

                Forum_User CurrentUser = GetCurrentUser(db);

                List<int> VisibleCategoryIDList = new List<int>();

                var Categories = db.GetAllCategories();

                foreach (var Category in Categories)
                {
                    if (db.CheckCategoryPermissions(Category, CurrentUser, P => P.AllowView))
                        VisibleCategoryIDList.Add(Category.CategoryID);
                }

                var Threads = db.GetSortedThreads().Where(T => VisibleCategoryIDList.Contains(T.CategoryID));

                ThreadInfoModelFromThread(db, CurrentUser, Threads.Take(THREADS_PER_PAGE), Model.ThreadInfoList);

                return View(Model);
            }
        }

        [HttpGet]
        public ActionResult Search()
        {
            return Redirect("/");
        }

        [HttpPost]
        public ActionResult Search(string Search)
        {
            using (ForumRespository db = new ForumRespository())
            {
                var Threads = db.GetMatchingThreads(Search);
                var Model = new SearchResultsViewModel() { SearchString = Search };

                Model.AddNavigation("Search Results");
                Model.ResultCount = Threads.Count();

                Forum_User CurrentUser = GetCurrentUser(db);

                if (!db.CheckRolePermissions(CurrentUser, R => R.AllowSearch))
                    return AuthenticationHelper.AccessDeniedView(Model);

                ThreadInfoModelFromThread(db, CurrentUser, Threads.Take(THREADS_PER_PAGE), Model.ThreadInfoList);

                return View("SearchResults", Model);
            }
        }

        #endregion

        #region View

        /// <summary>
        /// Ajax based preview functionality.
        /// </summary>
        [HttpPost]
        public ActionResult Preview(string data)
        {
            return Json(PostParser.Parse(data).ToString(), JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// Fills the list with subcategories found in the database, and fills the subcategories with their subcategories, until MaxRecursion
        /// level is reached, or all are accounted for. 
        /// </summary>
        /// <param name="db">The repository. Since read-only, any will do, but don't create one needlessly</param>
        /// <param name="ModelList">The list to fill</param>
        /// <param name="id">The id of the category, or null if primary categories are to be found</param>
        /// <param name="MaxRecursionLevel">0 means no recursion, only fill the current list.</param>
        private void ViewCategory_RecursivelyFillSubcategories(ForumRespository db, Forum_User CategoryReader, List<SubCategoryModel> ModelList, int id, int MaxRecursionLevel)
        {
            if (MaxRecursionLevel < 0) return;
            foreach (var SubCategory in db.GetSortedCategories(id))
            {
                if (!SubCategory.InheritPermissions && !db.CheckCategoryPermissions(SubCategory, CategoryReader, P => P.AllowView))
                    continue;
                SubCategoryModel SubModel = new SubCategoryModel() {
                    id = SubCategory.CategoryID,
                    Name = SubCategory.Name,
                    AllowPosts = SubCategory.AllowPosts
                };
                
                ModelList.Add(SubModel);
                if (!SubCategory.AllowPosts)
                {
                    ViewCategory_RecursivelyFillSubcategories(db, CategoryReader, SubModel.SubCategories, SubModel.id, MaxRecursionLevel - 1);
                }
                else
                {
                    IQueryable<Forum_Thread> Threads = db.GetSortedThreads(SubCategory.CategoryID);
                    SubModel.ThreadCount = Threads.Count();
                    if (SubModel.ThreadCount > 0)
                    {
                        SubModel.PostCount = Threads.Sum(T => T.Posts);
                        SubModel.LastPostTime = Threads.First().LastPostTime;
                    }
                }
            }
            
        }

        void ThreadInfoModelFromThread(ForumRespository db, Forum_User User, IEnumerable<Forum_Thread> Threads, ICollection<ThreadInfoViewModel> InfoCollection)
        {
            foreach (var Thread in Threads)
            {
                ThreadInfoViewModel NewThreadInfo = new ThreadInfoViewModel() 
                { 
                    ThreadTitle = Thread.Title,
                    ThreadID = Thread.ThreadID,
                    PostCount = Thread.Posts
                };

                InfoCollection.Add(NewThreadInfo);

                if (User == null)
                    NewThreadInfo.LastViewedPost = Thread.Posts;
                else
                    NewThreadInfo.LastViewedPost = db.GetLastPost(Thread, User);

                NewThreadInfo.Locked = Thread.Locked;

                NewThreadInfo.PageCount = (Thread.Posts - 1) / POSTS_PER_PAGE + 1;
                NewThreadInfo.LastViewedPostPage = NewThreadInfo.LastViewedPost / POSTS_PER_PAGE + 1;
                NewThreadInfo.LastViewedPostID = NewThreadInfo.LastViewedPost % POSTS_PER_PAGE + 1;

                Forum_Post LastPost = Thread.Forum_Posts.Last();
                NewThreadInfo.LastPoster = new UserViewModel();
                NewThreadInfo.LastPoster.Name = LastPost.Forum_User.Username;
                NewThreadInfo.LastPoster.UserID = LastPost.Forum_User.UserID;
                NewThreadInfo.LastPostTime = LastPost.TimeStamp;
            }
            
        }

        [HttpGet]
        public ActionResult ViewCategory(CategoryViewModel model)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_User CurrentUser = GetCurrentUser(db);
                Forum_Category Category =  db.GetCategoryByID(model.id);

                if (Category == null)
                    return NotFoundView("Category");

                if (model.id != (int)BuildInCategory.Root)
                {
                    model.AddNavigation(Category);
                    model.Name = Category.Name;
                }
                else
                {
                    model.IncludeIndex = false;
                    model.AddNavigation("Index");
                    model.Name = "Index";
                }

                if (!db.CheckCategoryPermissions(Category, CurrentUser, P => P.AllowView))
                    return AuthenticationHelper.AccessDeniedView(model);

                ViewCategory_RecursivelyFillSubcategories(db, CurrentUser, model.SubCategories, model.id, Category.AllowPosts ? 0 : 1);

                if (Category.AllowPosts)
                {
                    IQueryable<Forum_Thread> SortedThreads = db.GetSortedThreads(Category.CategoryID);
                    model.PageCount = (SortedThreads.Count() - 1) / THREADS_PER_PAGE + 1;

                    model.AllowPosts = true;
                    ThreadInfoModelFromThread(db, CurrentUser, SortedThreads.Skip(THREADS_PER_PAGE * (model.page - 1)).Take(THREADS_PER_PAGE), model.Threads);
                }

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult ViewThread(ThreadViewModel model)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_Thread Thread = db.GetThreadByID(model.Id);

                if (Thread == null)
                {
                    return NotFoundView("Thread");
                }

                if (model.Page < 1) return RedirectToAction("ViewThread", new { id = model.Id, page = 1}); // page less than 0 for existing thread equals redirect to valid page.

                model.AddNavigation(Thread);

                Forum_User ThreadViewUser = GetCurrentUser(db);

                if (!db.CheckCategoryPermissions(Thread.Forum_Category, ThreadViewUser, P => P.AllowView))
                    return AuthenticationHelper.AccessDeniedView(model);

                model.AllowEditThread = db.CheckCategoryPermissions(Thread.Forum_Category, ThreadViewUser, P => (P.AllowDeleteOwnThread && Thread.Forum_Posts[0].PosterID == ThreadViewUser.UserID && Thread.Forum_Posts[0].PosterID != (int)BuildInUser.Guest) || P.AllowDeleteAllThread || P.AllowMoveThread || P.AllowLockThread);
                model.Locked = Thread.Locked;
                model.ThreadTitle = Thread.Title;

                int UserID = 0;
                Forum_User U = GetCurrentUser(db);
                if (U != null)
                {
                    UserID = U.UserID;
                    db.SetLastPost(Thread, U, Math.Min(model.Page * POSTS_PER_PAGE, Thread.Posts));
                    db.Save();
                }

                model.LastPage = (Thread.Posts - 1) / POSTS_PER_PAGE + 1;
                if (model.Page > model.LastPage) return RedirectToAction("ViewThread", new { id = model.Id, page = model.LastPage }); // page greater than what exists equals redirect to last page.
                IEnumerable<Forum_Post> Posts = Thread.Forum_Posts.Skip((model.Page - 1)* POSTS_PER_PAGE).Take(POSTS_PER_PAGE);

                foreach (Forum_Post Post in Posts)
                {
                    PostViewModel PostModel = new PostViewModel();
                    PostModel.PostText = PostParser.Parse(Post.PostText);
                    PostModel.PostTime = Post.TimeStamp;
                    PostModel.Poster = new UserViewModel();
                    PostModel.PostID = Post.PostID;
                    PostModel.Poster.Name = Post.Forum_User.Username;
                    PostModel.Poster.UserID = Post.PosterID;
                    PostModel.AllowDelete = db.CheckCategoryPermissions(Thread.Forum_Category, ThreadViewUser, P => (P.AllowDeleteOwnPost && Post.PosterID == ThreadViewUser.UserID && Post.PosterID != (int)BuildInUser.Guest) || P.AllowDeleteAllPosts);
                    PostModel.AllowEdit = db.CheckCategoryPermissions(Thread.Forum_Category, ThreadViewUser, P => (P.AllowEditOwnPost && Post.PosterID == ThreadViewUser.UserID && Post.PosterID != (int)BuildInUser.Guest) || P.AllowEditAllPosts);
                    model.PostList.Add(PostModel);
                }
                return View(model);
            }
        }

        #endregion

        #region Edit

        public ActionResult Edit(WritePostViewModel model, string button)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_Post Post = db.GetPostByID(model.id);
                if (Post == null) return NotFoundView("Post");

                var Editor = GetCurrentUser(db);

                if (!db.CheckCategoryPermissions(Post.Forum_Thread.Forum_Category, Editor, P => (P.AllowEditOwnPost && Post.PosterID == Editor.UserID && Post.PosterID != (int)BuildInUser.Guest) || P.AllowEditAllPosts))
                    return AuthenticationHelper.AccessDeniedView(model);

                if (Post.Forum_Thread.Locked)
                    return AuthenticationHelper.AccessDeniedView(model);

                if (IsHttpPost)
                {
                    if (String.Equals(button, "preview", StringComparison.InvariantCultureIgnoreCase))
                    {
                        model.ShowPost = true;
                        model.PostHtml = PostParser.Parse(model.PostText);
                        ModelState.Clear();
                    }
                    else
                    {
                        if (!AntiForgeryTokenValid)
                        {
                            ModelState.AddModelError("AntiForgery", "The antiforgery token was invalid.");
                        }
                        else if (ModelState.IsValid)
                        {
                            Post.TimeStamp = DateTime.Now;
                            Post.PostText = model.PostText;
                            Post.Forum_Thread.LastPostTime = Post.TimeStamp;
                            if (Post == Post.Forum_Thread.Forum_Posts[0] && !String.IsNullOrEmpty(Post.Forum_Thread.Title))
                            {
                                Post.Forum_Thread.Title = model.ThreadTitle;
                            }
                            // Save to database
                            db.Save();

                            int PostIndex = Post.Forum_Thread.Forum_Posts.IndexOf(Post);
                            int EditedPostPage = PostIndex / POSTS_PER_PAGE + 1;
                            int EditedPostNumber = PostIndex % POSTS_PER_PAGE + 1;

                            return RedirectToAction("ViewThread", new { id = Post.ThreadID, page = EditedPostPage }).AddFragment(String.Format("Post_{0}", EditedPostNumber));
                        }
                    }
                }
                else
                {
                    model.PostText = Post.PostText;
                    ModelState.Clear();
                }
                if (Post == Post.Forum_Thread.Forum_Posts[0])
                    model.EditTitle = true;

                model.ThreadID = Post.ThreadID;
                model.Title = "Edit Post";
                model.ThreadTitle = Post.Forum_Thread.Title;
                model.AddNavigation(Post.Forum_Thread);
                model.AddNavigation("Edit Post");
                return View("WritePost", model);
            }
        }

        #endregion

        #region New

        public ActionResult NewThread(WritePostViewModel model, string button)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_Category Category = db.GetCategoryByID(model.id);
                if (Category == null) return NotFoundView("Category");

                model.AddNavigation(Category);
                model.AddNavigation("New thread");

                Forum_User Poster = GetCurrentUser(db);

                if (!db.CheckCategoryPermissions(Category, Poster, P => P.AllowNewThread))
                    return AuthenticationHelper.AccessDeniedView(model);
                
                if (String.Equals(button, "preview", StringComparison.InvariantCultureIgnoreCase))
                {
                    model.ShowPost = true;
                    model.PostHtml = PostParser.Parse(model.PostText);
                    ModelState.Clear();
                }
                else 
                if (IsHttpPost)
                {
                    if (!AntiForgeryTokenValid)
                    {
                        ModelState.AddModelError("AntiForgery", "The antiforgery token was invalid.");
                    }
                    if (String.IsNullOrEmpty(model.ThreadTitle))
                    {
                        ModelState.AddModelError("ThreadTitle", "A thread title is required.");
                    }
                    if (ModelState.IsValid)
                    {
                        Forum_Thread NewThread = new Forum_Thread();
                        NewThread.Title = model.ThreadTitle;

                        NewThread.PosterID = Poster.UserID;

                        Forum_Post InitialPost = new Forum_Post();
                        InitialPost.TimeStamp = DateTime.Now;
                        InitialPost.PosterID = NewThread.PosterID;
                        InitialPost.PostText = model.PostText;
                        NewThread.Forum_Posts.Add(InitialPost);
                        NewThread.Posts = 1;
                        NewThread.LastPostTime = InitialPost.TimeStamp;
                        NewThread.CategoryID = model.id;
                        // Save and add thread to database
                        db.AddThread(NewThread);
                        db.SetLastPost(NewThread, Poster, 1);
                        db.Save();
                        return RedirectToAction("ViewCategory", new { id = model.id });
                    }
                }
                else
                {
                    ModelState.Clear();
                }
                model.EditTitle = true;
                model.Title = "Post new Thread";
                return View("WritePost", model);
            }
        }

        public ActionResult Reply(WritePostViewModel model, string button, int QuoteId = 0)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_Thread RepliedToThread = db.GetThreadByID(model.id);
                if (RepliedToThread == null) return NotFoundView("Thread");

                model.AddNavigation(RepliedToThread);
                model.AddNavigation("Reply to thread");

                Forum_User Replier = GetCurrentUser(db);

                if (!db.CheckCategoryPermissions(RepliedToThread.Forum_Category, Replier, P => P.AllowReply))
                    return AuthenticationHelper.AccessDeniedView(model);

                if (RepliedToThread.Locked)
                    return AuthenticationHelper.AccessDeniedView(model);

                if (IsHttpPost)
                {
                    if (String.Equals(button, "preview", StringComparison.InvariantCultureIgnoreCase))
                    {
                        model.ShowPost = true;
                        model.PostHtml = PostParser.Parse(model.PostText);
                        ModelState.Clear();
                    } else if (!AntiForgeryTokenValid)
                    {
                        ModelState.AddModelError("AntiForgery", "The antiforgery token was invalid.");
                    }
                    else if (ModelState.IsValid)
                    {
                        Forum_Post ReplyPost = new Forum_Post();
                        ReplyPost.TimeStamp = DateTime.Now;
                        ReplyPost.PosterID = Replier.UserID;
                        ReplyPost.PostText = model.PostText;
                        RepliedToThread.Forum_Posts.Add(ReplyPost);
                        RepliedToThread.LastPostTime = ReplyPost.TimeStamp;
                        RepliedToThread.Posts = RepliedToThread.Forum_Posts.Count;
                        // Save to database
                        db.Save();

                        int PostIndex = RepliedToThread.Forum_Posts.IndexOf(ReplyPost);
                        int NewPostPage = PostIndex / POSTS_PER_PAGE + 1;
                        int NewPostNumber = PostIndex % POSTS_PER_PAGE + 1;

                        return RedirectToAction("ViewThread", new { id = RepliedToThread.ThreadID, page = NewPostPage }).AddFragment(String.Format("Post_{0}", NewPostNumber));
                    }
                }
                else
                {
                    ModelState.Clear();
                    Forum_Post QuotedPost = db.GetPostByID(QuoteId);
                    if (QuotedPost != null)
                    {
                        model.PostText = String.Format("[quote={0}]{1}[/quote]", QuotedPost.Forum_User.Username, QuotedPost.PostText);
                    }
                }

                model.ThreadID = model.id;
                model.Title = "Reply to Thread";
                return View("WritePost", model);
            }
        }

        #endregion

        #region Delete

        public ActionResult DeletePost(int id, int page, string confirmbutton)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_Post ToDelete = db.GetPostByID(id);

                var model = new MasterViewModel();
                model.AddNavigation(ToDelete.Forum_Thread);
                model.AddNavigation("Delete Post");

                if (ToDelete == null)
                    return NotFoundView("Post");
                if (ToDelete.Forum_Thread.Forum_Posts[0] == ToDelete)
                    return RedirectToAction("ViewThread", new { id = ToDelete.ThreadID });

                if (ToDelete.Forum_Thread.Locked)
                    return AuthenticationHelper.AccessDeniedView(model);
                
                var Category = ToDelete.Forum_Thread.Forum_Category;

                var Deleter = GetCurrentUser(db);

                if (!db.CheckCategoryPermissions(Category, Deleter, P => (P.AllowDeleteOwnPost && ToDelete.PosterID == Deleter.UserID && ToDelete.PosterID != (int)BuildInUser.Guest) || P.AllowDeleteAllPosts))
                    return AuthenticationHelper.AccessDeniedView(model);

                if (IsHttpPost)
                {
                    if (!AntiForgeryTokenValid)
                    {
                        ModelState.AddModelError("AntiForgery", "The antiforgery token was invalid.");
                    }
                    else
                    {
                        int ThreadID = ToDelete.ThreadID;
                        db.DeletePost(ToDelete);
                        db.Save();
                        return RedirectToAction("ViewThread", new { id = ThreadID, page = page });
                    }
                }

                return View(model);
            }
        }

        public ActionResult EditThread(int id, int? MoveTo, string Lock, string Delete)
        {
            using (ForumRespository db = new ForumRespository())
            {
                Forum_Thread EditedThread = db.GetThreadByID(id);
                if (EditedThread == null)
                    return NotFoundView("Post");

                var model = new EditThreadViewModel();
                var Category = EditedThread.Forum_Category;

                model.AddNavigation(EditedThread);
                model.AddNavigation("Edit Thread");

                var Editor = GetCurrentUser(db);

                model.AllowDelete = db.CheckCategoryPermissions(Category, Editor, P => (P.AllowDeleteOwnThread && EditedThread.Forum_Posts[0].PosterID == Editor.UserID && EditedThread.PosterID != (int)BuildInUser.Guest) || P.AllowDeleteAllThread);
                model.AllowMove = db.CheckCategoryPermissions(Category, Editor, P => P.AllowMoveThread);
                model.AllowLock = db.CheckCategoryPermissions(Category, Editor, P => P.AllowLockThread);

                if (!model.AllowDelete && !model.AllowLock && !model.AllowMove)
                    return AuthenticationHelper.AccessDeniedView(model);

                model.id = id;
                model.ThreadName = EditedThread.Title;
                model.CategoryID = Category.CategoryID;
                model.CategoryName = Category.Name;

                model.IsLocked = EditedThread.Locked;

                foreach (var MoveToCategory in db.GetAllCategories())
                {
                    if (MoveToCategory == Category) continue; // Cannot move the where the thread is already
                    if (!MoveToCategory.AllowPosts) continue; // Cannot move to a category that does not allow posts
                    if (!db.CheckCategoryPermissions(MoveToCategory, Editor, P => P.AllowNewThread)) continue; // Cannot move to a category where you are not allowed to create new threads.
                    model.ValidMoveDestinations.Add(new AdminNamedID() { ID = MoveToCategory.CategoryID, Name = MoveToCategory.Name});
                }

                if (IsHttpPost)
                {
                    if (!AntiForgeryTokenValid)
                    {
                        ModelState.AddModelError("AntiForgery", "The antiforgery token was invalid.");
                    }
                    else
                    {
                        if (model.AllowDelete && !String.IsNullOrEmpty(Delete))
                        {
                            db.DeleteThread(EditedThread);
                            db.Save();
                            return RedirectToAction("ViewCategory", new { id = model.CategoryID });
                        }
                        if (model.AllowMove)
                        {
                            var Destination = db.GetCategoryByID((int)MoveTo);
                            if (Destination != null && model.ValidMoveDestinations.Exists(D => D.ID == Destination.CategoryID))
                            {
                                EditedThread.Forum_Category = Destination;
                            }
                        }
                        if (model.AllowLock)
                            EditedThread.Locked = !String.IsNullOrEmpty(Lock);
                        db.Save();
                        return RedirectToAction("ViewThread", new { id = model.id });
                    }
                }
                return View(model);
            }
        }
        #endregion

    }
}
