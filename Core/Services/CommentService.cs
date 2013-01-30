using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Domain;
using Core.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Core.Services
{
    public class CommentService
    {
        private readonly MongoHelper<Post> _posts;

        public CommentService()
        {
            _posts = new MongoHelper<Post>();
        }

        public void AddComment(ObjectId postId, Comment comment)
        {
            _posts.Collection.Update(Query.EQ("_id", postId),
                Update.PushWrapped("Comments", comment).Inc("TotalComments", 1));
        }

        public void RemoveComment(ObjectId postId, ObjectId commentId)
        {
            _posts.Collection.Update(Query.EQ("_id", postId),
                Update.Pull("Comments", Query.EQ("_id", commentId)).Inc("TotalComments", -1));
        }

        public IList<Comment> GetComments(ObjectId postId, int skip, int limit, int totalComments)
        {
            var newComments = GetTotalComments(postId) - totalComments;
            skip += newComments;

            var post = _posts.Collection.Find(Query.EQ("_id", postId)).SetFields(Fields.Exclude("Date", "Title", "Url", "Summary", "Details", "Author", "TotalComments").Slice("Comments", -skip, limit)).Single();
            return post.Comments.OrderByDescending(c => c.Date).ToList();
        }

        public int GetTotalComments(ObjectId postId)
        {
            var post = _posts.Collection.Find(Query.EQ("_id", postId)).SetFields(Fields.Include("TotalComments")).Single();
            return post.TotalComments;
        }
    }
}
