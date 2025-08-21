using System;
using System.ComponentModel.DataAnnotations;

namespace RssReader.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Link { get; set; }

        public DateTime? PubDate { get; set; }

        public int FeedId { get; set; }
        public Feed Feed { get; set; }
    }
}