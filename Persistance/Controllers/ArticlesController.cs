using Blog.Data;
using Blog.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Blog.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ArticleRepositoryContext _db;

        public ArticlesController(ArticleRepositoryContext db)
        {
            _db = db;
        }

        // GET: ArticlesController
        public ActionResult Index(
            string authorEmail,   // <- nuevo
            string title,   
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var query = _db.Articles.AsNoTracking();

            // Filtro fecha
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(a => a.PublishedDate >= startDate.Value &&
                                         a.PublishedDate <= endDate.Value);
            }
            
            // Filtro email autor
            if (!string.IsNullOrWhiteSpace(authorEmail))
            {
                var csEmail = authorEmail.Trim().ToLower();
                query = query.Where(a => a.AuthorEmail != null && a.AuthorEmail.ToLower() == csEmail);
            }
            
            // Filtro contains titulo
            if (!string.IsNullOrWhiteSpace(title))
            {
                var csTitle = title.Trim().ToLower();
                query = query.Where(a => a.Title != null && a.Title.ToLower().Contains(csTitle));

                // Alternativa con LIKE:
                // query = query.Where(a => EF.Functions.Like(a.Title.ToLower(), $"%{normalizedTitle}%"));
            }

            var results = query
                .AsEnumerable()
                .OrderByDescending(a => a.PublishedDate)
                .ToList();

            return View(results);
        }

        // GET: ArticlesController/Details/5
        public ActionResult Details(int id)
        {
            var article = _db.Articles.AsNoTracking().FirstOrDefault(a => a.Id == id);
            if (article == null) return NotFound();

            var comments = _db.Comments.AsNoTracking()
                .Where(c => c.ArticleId == id)
                .AsEnumerable()
                .OrderByDescending(c => c.PublishedDate)
                .ToList();

            var viewModel = new ArticleDetailsViewModel(article, comments);
            return View(viewModel);
        }

        // GET: ArticlesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ArticlesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Article article)
        {
            if (!ModelState.IsValid)
            {
                return View(article);
            }
            
            article.PublishedDate = DateTimeOffset.UtcNow;
            _db.Articles.Add(article);
            _db.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = article.Id });
        }

        [HttpPost]
        [Route("Articles/{articleId}/AddComment")]
        public ActionResult AddComment(int articleId, Comment comment)
        {
            var article = _db.Articles.AsNoTracking().FirstOrDefault(a => a.Id == articleId);
            
            if (article == null)
            {
                return NotFound();
            }
            if (string.IsNullOrEmpty(comment.Content))
            {
                return BadRequest();
            }

            comment.ArticleId = articleId;
            comment.PublishedDate = DateTimeOffset.UtcNow;
            
            _db.Comments.Add(comment);
            _db.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = articleId });
        }
    }
}