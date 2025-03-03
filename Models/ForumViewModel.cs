namespace Propertease.Models
{
    public class ForumViewModel
    {
        public ForumPost NewPost { get; set; } = new ForumPost();
        public IEnumerable<ForumPost> Posts { get; set; } = new List<ForumPost>();
    }
}
