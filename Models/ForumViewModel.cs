namespace Propertease.Models
{
    public class ForumViewModel
    {
        public ForumPost ForumPost { get; set; } = new ForumPost();
        public IEnumerable<ForumPost> Posts { get; set; } = new List<ForumPost>();
        public ForumComment Comment { get; set; }

    }
}
