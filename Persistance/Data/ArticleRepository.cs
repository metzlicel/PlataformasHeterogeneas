using System.Globalization;
using Blog.Models;
using Microsoft.Data.Sqlite;

namespace Blog.Data
{
    /// <summary>
    /// Implementation of <see cref="IArticleRepository"/> using SQLite as a persistence solution.
    /// </summary>
    public class ArticleRepository : IArticleRepository
    {
        private readonly string _connectionString;

        public ArticleRepository(DatabaseConfig _config)
        {
            _connectionString = _config.DefaultConnectionString ?? throw new ArgumentNullException("Connection string not found");
        }

        private const string SQL_SCHEMA = @"
            CREATE TABLE IF NOT EXISTS Articles (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            AuthorName   TEXT NOT NULL,
            AuthorEmail  TEXT NOT NULL,
            Title        TEXT NOT NULL CHECK(length(Title) <= 100),
            Content      TEXT NOT NULL,
            PublishedDate TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Comments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ArticleId     INTEGER NOT NULL,
            Content       TEXT NOT NULL,
            PublishedDate TEXT NOT NULL,
            FOREIGN KEY (ArticleId) REFERENCES Articles(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_Articles_PublishedDate ON Articles(PublishedDate);
            CREATE INDEX IF NOT EXISTS IX_Comments_ArticleId   ON Comments(ArticleId);";

            private const string SQL_SELECT_ALL = @"
            SELECT Id, AuthorName, AuthorEmail, Title, Content, PublishedDate
            FROM Articles
            ORDER BY PublishedDate DESC;";

            private const string SQL_SELECT_RANGE = @"
            SELECT Id, AuthorName, AuthorEmail, Title, Content, PublishedDate
            FROM Articles
            WHERE PublishedDate >= $start AND PublishedDate <= $end
            ORDER BY PublishedDate DESC;";

            private const string SQL_SELECT_BY_ID = @"
            SELECT Id, AuthorName, AuthorEmail, Title, Content, PublishedDate
            FROM Articles
            WHERE Id = $id;";

            private const string SQL_INSERT_ARTICLE = @"
            INSERT INTO Articles (AuthorName, AuthorEmail, Title, Content, PublishedDate)
            VALUES ($name, $email, $title, $content, $date);";

            private const string SQL_SELECT_COMMENTS_BY_ARTICLE = @"
            SELECT ArticleId, Content, PublishedDate
            FROM Comments
            WHERE ArticleId = $id
            ORDER BY PublishedDate DESC;";

            private const string SQL_INSERT_COMMENT = @"
            INSERT INTO Comments (ArticleId, Content, PublishedDate)
            VALUES ($articleId, $content, $date);";
            
            private const string SQL_CHECK_ARTICLE_EXISTS = @"SELECT 1 FROM Articles WHERE Id = $id LIMIT 1;";
        
            private SqliteConnection Open()
            {
                var c = new SqliteConnection(_connectionString);
                c.Open();
                using var pragma = c.CreateCommand();
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
                return c;
            }
            
        /// <summary>
        /// Creates the necessary tables for this application if they don't exist already.
        /// Should be called once when starting the service.
        /// </summary>
        public void EnsureCreated()
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL_SCHEMA; 
            cmd.ExecuteNonQuery();        
        }

        public IEnumerable<Article> GetAll()
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL_SELECT_ALL;

            var list = new List<Article>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(MapArticle(rd));
            }
            return list;
        }

        public IEnumerable<Article> GetByDateRange(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL_SELECT_RANGE;
            cmd.Parameters.AddWithValue("$start", ToIso(startDate));
            cmd.Parameters.AddWithValue("$end", ToIso(endDate));

            var list = new List<Article>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(MapArticle(rd));
            }
            return list;
        }

        public Article? GetById(int id)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL_SELECT_BY_ID;
            cmd.Parameters.AddWithValue("$id", id);

            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapArticle(rd) : null;
        }

        public Article Create(Article article)
        {
            if (article == null) throw new ArgumentNullException(nameof(article));
            var date = article.PublishedDate.ToUniversalTime().ToString("o");

            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Articles (AuthorName, AuthorEmail, Title, Content, PublishedDate)
                VALUES ($name, $email, $title, $content, $date);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$name", article.AuthorName ?? "");
            cmd.Parameters.AddWithValue("$email", article.AuthorEmail ?? "");
            cmd.Parameters.AddWithValue("$title", article.Title ?? "");
            cmd.Parameters.AddWithValue("$content", article.Content ?? "");
            cmd.Parameters.AddWithValue("$date", date);

            var newId = (long)cmd.ExecuteScalar()!;
            article.Id = (int)newId;
            return article;
        }

        public void AddComment(Comment comment)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));

            using var conn = Open();

            using (var chk = conn.CreateCommand())
            {
                chk.CommandText = SQL_CHECK_ARTICLE_EXISTS;
                chk.Parameters.AddWithValue("$id", comment.ArticleId);
                var exists = chk.ExecuteScalar();
                if (exists == null)
                    throw new ArgumentException("No article exists with the specified ID.", nameof(comment.ArticleId));
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SQL_INSERT_COMMENT;
                cmd.Parameters.AddWithValue("$articleId", comment.ArticleId);
                cmd.Parameters.AddWithValue("$content", comment.Content?? string.Empty);
                cmd.Parameters.AddWithValue("$date", ToIso(comment.PublishedDate));
                cmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<Comment> GetCommentsByArticleId(int articleId)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SQL_SELECT_COMMENTS_BY_ARTICLE;
            cmd.Parameters.AddWithValue("$id", articleId);

            var list = new List<Comment>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(MapComment(rd));
            }
            return list;
        }
        
        private static Article MapArticle(SqliteDataReader rd)
        {
            var id = rd.GetInt32(0);
            var name = rd.GetString(1);
            var email= rd.GetString(2);
            var title= rd.GetString(3);
            var content = rd.GetString(4);
            var dateStr= rd.GetString(5);

            return new Article
            {
                Id = id,
                AuthorName = name,
                AuthorEmail = email,
                Title = title,
                Content = content,
                PublishedDate = DateTimeOffset.Parse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }

        private static Comment MapComment(SqliteDataReader rd)
        {
            var articleId = rd.GetInt32(0);
            var content= rd.GetString(1);
            var dateStr= rd.GetString(2);

            return new Comment
            {
                ArticleId = articleId,
                Content = content,
                PublishedDate = DateTimeOffset.Parse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }
        
        private static string ToIso(DateTimeOffset dto)
            => dto.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
    }
}
