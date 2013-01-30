using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Domain;
using Core.Services;
using MongoDBTutorial.Helpers;
using MongoDB.Bson;

namespace MongoDBTutorial.Controllers
{
    public class PostController : Controller
    {
        private readonly PostService _postService;
        private readonly CommentService _commentService;

        public PostController()
        {
            _postService = new PostService();
            _commentService = new CommentService();
        }

        public ActionResult Index()
        {
            return View(_postService.GetPosts());
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View(new Post());
        }

        [HttpPost]
        public ActionResult Create(Post post)
        {
            if (ModelState.IsValid)
            {
                post.Url = post.Title.GenerateSlug();
                post.Author = User.Identity.Name;
                post.Date = DateTime.Now;

                _postService.Create(post);

                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpGet]
        public ActionResult Update(string id)
        {
            return View(_postService.GetPost(id));
        }

        [HttpGet]
        public ActionResult Delete(ObjectId id)
        {
            return View(_postService.GetPost(id));
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult ConfirmDelete(ObjectId id)
        {
            _postService.Delete(id);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Update(Post post)
        {
            if (ModelState.IsValid)
            {
                post.Url = post.Title.GenerateSlug();

                if (_postService.Edit(post))
                {
                    return RedirectToAction("Index");    
                }

                ModelState.AddModelError("ConcurrencyError", "This post has been updated since you started editing it. Please reload the post to get the latest changes.");
                return View(post);
            }

            return View(post);
        }

        [HttpGet]
        public ActionResult Detail(string id)
        {
            var post = _postService.GetPost(id);
            ViewBag.PostId = post.PostId;

            ViewBag.TotalComments = post.TotalComments;
            ViewBag.LoadedComments = 5;

            return View(post);
        }

        [HttpPost]
        public ActionResult AddComment(ObjectId postId, Comment comment)
        {
            if (ModelState.IsValid)
            {
                var newComment = new Comment()
                                        {
                                            CommentId = ObjectId.GenerateNewId(),
                                            Author = User.Identity.Name,
                                            Date = DateTime.Now,
                                            Detail = comment.Detail
                                        };

                _commentService.AddComment(postId, newComment);

                ViewBag.PostId = postId;
                return Json(
                    new
                        {
                            Result = "ok",
                            CommentHtml = RenderPartialViewToString("Comment", newComment),
                            FormHtml = RenderPartialViewToString("AddComment", new Comment())
                        });
            }

            ViewBag.PostId = postId;
            return Json(
                new
                    {
                        Result = "fail",
                        FormHtml = RenderPartialViewToString("AddComment", comment)
                    });
        }

        public ActionResult RemoveComment(ObjectId postId, ObjectId commentId)
        {
            _commentService.RemoveComment(postId, commentId);
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult CommentList(ObjectId postId, int skip, int limit, int totalComments)
        {
            ViewBag.TotalComments = totalComments;
            ViewBag.LoadedComments = skip + limit;
            return PartialView(_commentService.GetComments(postId, ViewBag.LoadedComments, limit, totalComments));
        }

        /// <summary>
        /// http://craftycodeblog.com/2010/05/15/asp-net-mvc-render-partial-view-to-string/
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        protected string RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }
    }
}