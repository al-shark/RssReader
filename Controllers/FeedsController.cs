using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RssReader.Data;
using RssReader.Models;
using System.Threading.Tasks;

namespace RssReader.Controllers
{
    public class FeedsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedsController> _logger;

        public FeedsController(ApplicationDbContext context, ILogger<FeedsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Feeds
        public async Task<IActionResult> Index(string searchString)
        {
            var feeds = from f in _context.Feeds select f;

            if (!string.IsNullOrEmpty(searchString))
            {
                feeds = feeds.Where(f => f.Name.Contains(searchString));
            }

            return View(await feeds.ToListAsync());
        }

        // GET: Feeds/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Feeds/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Url")] Feed feed)
        {
            if (ModelState.IsValid)
            {
                _context.Add(feed);
                await _context.SaveChangesAsync();
                await LoadArticles(feed); // Initial load
                return RedirectToAction(nameof(Index));
            }
            return View(feed);
        }

        // GET: Feeds/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var feed = await _context.Feeds.FirstOrDefaultAsync(m => m.Id == id);
            if (feed == null) return NotFound();
            return View(feed);
        }

        // POST: Feeds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feed = await _context.Feeds.Include(f => f.Articles).FirstOrDefaultAsync(f => f.Id == id);
            if (feed != null)
            {
                _context.Articles.RemoveRange(feed.Articles);
                _context.Feeds.Remove(feed);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Feeds/BulkDelete
        [HttpPost]
        public async Task<IActionResult> BulkDelete(int[] feedIds)
        {
            if (feedIds != null && feedIds.Length > 0)
            {
                var feeds = await _context.Feeds.Where(f => feedIds.Contains(f.Id)).Include(f => f.Articles).ToListAsync();
                foreach (var feed in feeds)
                {
                    _context.Articles.RemoveRange(feed.Articles);
                    _context.Feeds.Remove(feed);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Feeds/Details/5
        public async Task<IActionResult> Details(int? id, DateTime? fromDate, DateTime? toDate, string searchTitle)
        {
            if (id == null) return NotFound();
            var feed = await _context.Feeds.Include(f => f.Articles).FirstOrDefaultAsync(m => m.Id == id);
            if (feed == null) return NotFound();

            var articles = feed.Articles.AsQueryable();

            if (fromDate.HasValue)
            {
                articles = articles.Where(a => a.PubDate >= fromDate);
            }
            if (toDate.HasValue)
            {
                articles = articles.Where(a => a.PubDate <= toDate.Value.AddDays(1)); // Include end of day
            }
            if (!string.IsNullOrEmpty(searchTitle))
            {
                articles = articles.Where(a => a.Title.Contains(searchTitle));
            }

            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["SearchTitle"] = searchTitle;

            return View(new FeedDetailsViewModel { Feed = feed, Articles = articles.ToList() });
        }

        // POST: Feeds/Reload/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reload(int id)
        {
            var feed = await _context.Feeds.Include(f => f.Articles).FirstOrDefaultAsync(f => f.Id == id);
            if (feed == null) return NotFound();

            // Clear existing articles
            _context.Articles.RemoveRange(feed.Articles);
            await _context.SaveChangesAsync();

            // Reload
            await LoadArticles(feed);
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<bool> LoadArticles(Feed feed)
        {
            try
            {
                var settings = new XmlReaderSettings
                {
                    XmlResolver = null, // Prevent XXE attacks
                    DtdProcessing = DtdProcessing.Prohibit
                };

                using var reader = XmlReader.Create(feed.Url, settings);
                var syndicationFeed = SyndicationFeed.Load(reader);

                // Process articles...
                return true;
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "XML parsing failed for feed {FeedUrl}", feed.Url);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error loading feed {FeedUrl}", feed.Url);
                return false;
            }
        }
    }

    public class FeedDetailsViewModel
    {
        public Feed Feed { get; set; }
        public List<Article> Articles { get; set; }
    }
}