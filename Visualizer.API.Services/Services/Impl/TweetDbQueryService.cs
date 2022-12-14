using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Visualizer.Shared.Models;

namespace Visualizer.API.Services.Services.Impl;

internal class TweetDbQueryService : ITweetDbQueryService
{
    private readonly RedisConnectionProvider _redisConnectionProvider;
    private readonly ILogger<TweetDbQueryService> _logger;

    public TweetDbQueryService(RedisConnectionProvider redisConnectionProvider, ILogger<TweetDbQueryService> logger)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _logger = logger;
    }

    public async Task<TweetModelsPage> FindTweets(FindTweetsInputDto inputDto)
    {
        var tweetCollection = _redisConnectionProvider.RedisCollection<TweetModel>();

        if (!string.IsNullOrWhiteSpace(inputDto.TweetId))
        {
            tweetCollection = tweetCollection.Where(t => t.Id == inputDto.TweetId);
        }

        if (!string.IsNullOrWhiteSpace(inputDto.AuthorId))
        {
            tweetCollection = tweetCollection.Where(t => t.AuthorId == inputDto.AuthorId);
        }

        if (!string.IsNullOrWhiteSpace(inputDto.Username))
        {
            tweetCollection = tweetCollection.Where(t => t.Username == inputDto.Username);
        }

        if (!string.IsNullOrWhiteSpace(inputDto.SearchTerm))
        {
            tweetCollection = tweetCollection.Where(t => t.Text == inputDto.SearchTerm);
        }

        if (inputDto.StartingFrom is not null)
        {
            var startingFromTicks = inputDto.StartingFrom.Value.ToUniversalTime().Ticks;
            tweetCollection = tweetCollection.Where(t => t.CreatedAt >= startingFromTicks);
        }

        if (inputDto.UpTo is not null)
        {
            var upToTicks = inputDto.UpTo.Value.ToUniversalTime().Ticks;
            tweetCollection = tweetCollection.Where(t => t.CreatedAt <= upToTicks);
        }

        if (inputDto.Hashtags is not null && inputDto.Hashtags.Length > 0)
        {
            foreach (var requiredHashtag in inputDto.Hashtags.Where(h => !string.IsNullOrEmpty(h)))
            {
                tweetCollection = tweetCollection.Where(t => t.Entities.Hashtags.Contains(requiredHashtag));
            }
        }

        // GEO
        if (inputDto.OnlyWithGeo.HasValue || inputDto.GeoFilter is not null)
        {
            var hasGeoLocString = inputDto.OnlyWithGeo.Value ? "1" : "0";
            tweetCollection = tweetCollection.Where(t => t.HasGeoLoc == hasGeoLocString);

            if (inputDto.GeoFilter is not null)
            {
                tweetCollection = tweetCollection.GeoFilter(model => model.GeoLoc, inputDto.GeoFilter.Longitude, inputDto.GeoFilter.Latitude, inputDto.GeoFilter.RadiusKm, GeoLocDistanceUnit.Kilometers);
            }
        }

        // Get the total count of the filtered tweets.
        var count = await tweetCollection.CountAsync().ConfigureAwait(false);

        // Sort tweets.
        var sortField = inputDto.SortField ?? SortField.CreatedAt;
        var orderByDirection = inputDto.SortOrder ?? SortOrder.Descending;
        tweetCollection = (orderByDirection, sortField) switch
        {
            (SortOrder.Ascending, SortField.Username) => tweetCollection.OrderBy(model => model.Username),
            (SortOrder.Ascending, SortField.CreatedAt) => tweetCollection.OrderBy(model => model.CreatedAt),
            (SortOrder.Ascending, SortField.PublicMetricsLikesCount) => tweetCollection.OrderBy(model => model.PublicMetricsLikeCount),
            (SortOrder.Ascending, SortField.PublicMetricsRepliesCount) => tweetCollection.OrderBy(model => model.PublicMetricsReplyCount),
            (SortOrder.Ascending, SortField.PublicMetricsRetweetsCount) => tweetCollection.OrderBy(model => model.PublicMetricsRetweetCount),
            (SortOrder.Descending, SortField.Username) => tweetCollection.OrderByDescending(model => model.Username),
            (SortOrder.Descending, SortField.CreatedAt) => tweetCollection.OrderByDescending(model => model.CreatedAt),
            (SortOrder.Descending, SortField.PublicMetricsLikesCount) => tweetCollection.OrderByDescending(model => model.PublicMetricsLikeCount),
            (SortOrder.Descending, SortField.PublicMetricsRepliesCount) => tweetCollection.OrderByDescending(model => model.PublicMetricsReplyCount),
            (SortOrder.Descending, SortField.PublicMetricsRetweetsCount) => tweetCollection.OrderByDescending(model => model.PublicMetricsRetweetCount),
            _ => tweetCollection
        };

        // Paginate tweets.
        var pageSize = inputDto.PageSize ?? 10;
        var pageNumber = inputDto.PageNumber ?? 0;
        var skipAmount = pageSize * pageNumber > count ? 0 : pageSize * pageNumber;
        tweetCollection = tweetCollection.Skip(skipAmount).Take(pageSize);

        // Produce tweets.
        var tweetsIlist = await tweetCollection.ToListAsync();
        var tweetModels = tweetsIlist.ToList();
        return new TweetModelsPage(count, tweetModels);
    }

    public async Task<TweetModelsPage> FindTweetsWithExpression(FindTweetsInputDto inputDto)
    {
        var tweetCollection = _redisConnectionProvider.RedisCollection<TweetModel>();

        Expression expression = null;

        if (!string.IsNullOrWhiteSpace(inputDto.TweetId))
        {
            Expression<Func<TweetModel, bool>> filterByTweetIdExpression = tweet => tweet.Id == inputDto.TweetId;
            expression = expression is null ? filterByTweetIdExpression.Body : Expression.AndAlso(expression, filterByTweetIdExpression.Body);
        }

        if (!string.IsNullOrWhiteSpace(inputDto.AuthorId))
        {
            Expression<Func<TweetModel, bool>> filterByAuthorIdExpression = tweet => tweet.AuthorId == inputDto.AuthorId;
            expression = expression is null ? filterByAuthorIdExpression.Body : Expression.AndAlso(expression, filterByAuthorIdExpression.Body);
        }

        if (!string.IsNullOrWhiteSpace(inputDto.Username))
        {
            Expression<Func<TweetModel, bool>> filterByUsernameExpression = tweet => tweet.Username == inputDto.Username;
            expression = expression is null ? filterByUsernameExpression.Body : Expression.AndAlso(expression, filterByUsernameExpression.Body);
        }

        if (!string.IsNullOrWhiteSpace(inputDto.SearchTerm))
        {
            Expression<Func<TweetModel, bool>> filterBySearchTerm = tweet => tweet.Text == inputDto.SearchTerm;
            expression = expression is null ? filterBySearchTerm.Body : Expression.AndAlso(expression, filterBySearchTerm.Body);
        }

        if (inputDto.StartingFrom is not null)
        {
            var startingFromTicks = inputDto.StartingFrom.Value.ToUniversalTime().Ticks;
            Expression<Func<TweetModel, bool>> filterByStartingFromExpression = tweet => tweet.CreatedAt >= startingFromTicks;
            expression = expression is null ? filterByStartingFromExpression.Body : Expression.AndAlso(expression, filterByStartingFromExpression.Body);
        }

        if (inputDto.UpTo is not null)
        {
            var upToTicks = inputDto.UpTo.Value.ToUniversalTime().Ticks;
            Expression<Func<TweetModel, bool>> filterByUpToExpression = tweet => tweet.CreatedAt <= upToTicks;
            expression = expression is null ? filterByUpToExpression.Body : Expression.AndAlso(expression, filterByUpToExpression.Body);
        }

        if (inputDto.Hashtags is not null && inputDto.Hashtags.Length > 0)
        {
            // ToDo: providing only the hashtags will crash the query generation. Providing the search term and the hashtags works fine.
            Expression<Func<TweetModel, bool>> dummy = tweet => tweet.CreatedAt > int.MinValue;
            expression = expression is null ? dummy.Body : Expression.AndAlso(expression, dummy.Body);
            foreach (var requiredHashtag in inputDto.Hashtags.Where(h => !string.IsNullOrEmpty(h)))
            {
                Expression<Func<TweetModel, bool>> hashtagEx = tweet => tweet.Entities.Hashtags.Contains(requiredHashtag);
                expression = expression is null ? hashtagEx.Body : Expression.AndAlso(expression, hashtagEx.Body);
            }
        }

        // GEO
        if (inputDto.OnlyWithGeo.HasValue || inputDto.GeoFilter is not null)
        {
            var hasGeoLocString = inputDto.OnlyWithGeo.Value ? "1" : "0";
            Expression<Func<TweetModel, bool>> filterByGeo = tweet => tweet.HasGeoLoc == hasGeoLocString;
            expression = expression is null ? filterByGeo.Body : Expression.AndAlso(expression, filterByGeo.Body);

            if (inputDto.GeoFilter is not null)
            {
                tweetCollection = tweetCollection.GeoFilter(model => model.GeoLoc, inputDto.GeoFilter.Longitude, inputDto.GeoFilter.Latitude, inputDto.GeoFilter.RadiusKm, GeoLocDistanceUnit.Kilometers);
            }
        }

        if (expression is not null)
        {
            var tweetParameter = Expression.Parameter(typeof(TweetModel), "tweet");
            var whereExpression = Expression.Lambda<Func<TweetModel, bool>>(expression, new ParameterExpression[] {tweetParameter});
            tweetCollection = tweetCollection.Where(whereExpression);
        }


        // Get the total count of the filtered tweets.
        var count = await tweetCollection.CountAsync();

        // Get the filtered, sorted and paginated tweets.
        var sortField = inputDto.SortField ?? SortField.CreatedAt;
        var orderByDirection = inputDto.SortOrder ?? SortOrder.Descending;
        tweetCollection = (orderByDirection, sortField) switch
        {
            (SortOrder.Ascending, SortField.Username) => tweetCollection.OrderBy(model => model.Username),
            (SortOrder.Ascending, SortField.CreatedAt) => tweetCollection.OrderBy(model => model.CreatedAt),
            (SortOrder.Ascending, SortField.PublicMetricsLikesCount) => tweetCollection.OrderBy(model => model.PublicMetricsLikeCount),
            (SortOrder.Ascending, SortField.PublicMetricsRepliesCount) => tweetCollection.OrderBy(model => model.PublicMetricsReplyCount),
            (SortOrder.Ascending, SortField.PublicMetricsRetweetsCount) => tweetCollection.OrderBy(model => model.PublicMetricsRetweetCount),
            (SortOrder.Descending, SortField.Username) => tweetCollection.OrderByDescending(model => model.Username),
            (SortOrder.Descending, SortField.CreatedAt) => tweetCollection.OrderByDescending(model => model.CreatedAt),
            (SortOrder.Descending, SortField.PublicMetricsLikesCount) => tweetCollection.OrderByDescending(model => model.PublicMetricsLikeCount),
            (SortOrder.Descending, SortField.PublicMetricsRepliesCount) => tweetCollection.OrderByDescending(model => model.PublicMetricsReplyCount),
            (SortOrder.Descending, SortField.PublicMetricsRetweetsCount) => tweetCollection.OrderByDescending(model => model.PublicMetricsRetweetCount),
            _ => tweetCollection
        };

        var pageSize = inputDto.PageSize ?? 10;
        var pageNumber = inputDto.PageNumber ?? 0;
        var skipAmount = pageSize * pageNumber > count ? 0 : pageSize * pageNumber;
        tweetCollection = tweetCollection.Skip(skipAmount).Take(pageSize);
        _logger.LogInformation("Generated Redis query {Query}", tweetCollection.Expression.ToString());


        var tweetsIlist = await tweetCollection.ToListAsync();
        var tweetModels = tweetsIlist.ToList();
        return new TweetModelsPage(count, tweetModels);
    }
}

public class FindTweetsInputDto
{
    public string TweetId { get; set; }
    public string AuthorId { get; set; }
    public string Username { get; set; }
    public string SearchTerm { get; set; }
    public string[] Hashtags { set; get; }

    public bool? OnlyWithGeo { get; set; }
    public GeoFilter? GeoFilter { get; set; }

    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }

    public DateTime? StartingFrom { get; set; }
    public DateTime? UpTo { get; set; }

    public SortField? SortField { get; set; }
    public SortOrder? SortOrder { get; set; }
}

public record GeoFilter(double Latitude, double Longitude, double RadiusKm);

public enum SortField
{
    CreatedAt,
    Username,
    PublicMetricsLikesCount,
    PublicMetricsRetweetsCount,
    PublicMetricsRepliesCount
}

public enum SortOrder
{
    Ascending,
    Descending
}

public record TweetModelsPage(int Total, List<TweetModel> Tweets);
