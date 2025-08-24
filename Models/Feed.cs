using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RssReader.Models
{
    public class Feed
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Url(ErrorMessage = "Please enter a valid RSS feed URL")]
        [RegularExpression(@"^https?://.*", ErrorMessage = "URL must use HTTP or HTTPS")]
        public string Url { get; set; }

        public ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}