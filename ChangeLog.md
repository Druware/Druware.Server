# Changelog

## 2023-02-11

* Extended User to store both a session expiration and registration date
* Moved Tag from Content to Server
* Added license headers to all relevant files

* Added an Access Log to the Server space to enable access reporting and auditing


            /* - this is throwing an error, wonder if I have it backwards.
             * entity.HasOne(d => d.Article)
                .WithMany(p => p.ArticleTags)
                .HasForeignKey(d => d.ArticleId)
                .OnDelete(DeleteBehavior.ClientCascade)
                .HasConstraintName("fk_article_tags__news_article_article_id");
            */
