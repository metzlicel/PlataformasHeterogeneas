using System.Globalization;
using Blog.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data
{
    /// <summary>
    /// Implementation of <see cref="IArticleRepository"/> using SQLite as a persistence solution.
    /// </summary>
    public class ArticleRepositoryContext : DbContext
    {
        public ArticleRepositoryContext(DbContextOptions<ArticleRepositoryContext> options)
            : base(options) { }

        public DbSet<Article> Articles => Set<Article>();
        public DbSet<Comment> Comments => Set<Comment>();
        
    }
}