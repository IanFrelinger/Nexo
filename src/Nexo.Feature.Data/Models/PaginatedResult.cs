namespace Nexo.Feature.Data.Models
{
    /// <summary>
    /// Paginated result wrapper for query results
    /// </summary>
    /// <typeparam name="T">The type of items in the result</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// The items in the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Whether this is the first page
        /// </summary>
        public bool IsFirstPage { get; set; }

        /// <summary>
        /// Whether this is the last page
        /// </summary>
        public bool IsLastPage { get; set; }

        /// <summary>
        /// First item number on this page (1-based)
        /// </summary>
        public int FirstItemOnPage { get; set; }

        /// <summary>
        /// Last item number on this page (1-based)
        /// </summary>
        public int LastItemOnPage { get; set; }

        /// <summary>
        /// Creates a new paginated result
        /// </summary>
        /// <param name="items">The items for the current page</param>
        /// <param name="page">Current page number</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="totalCount">Total number of items</param>
        public PaginatedResult(IEnumerable<T> items, int page, int pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;

            // Calculate derived properties
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            HasPreviousPage = page > 1;
            HasNextPage = page < TotalPages;
            IsFirstPage = page == 1;
            IsLastPage = page == TotalPages || TotalPages == 0;
            FirstItemOnPage = TotalCount == 0 ? 0 : ((page - 1) * pageSize) + 1;
            LastItemOnPage = Math.Min(page * pageSize, totalCount);
        }

        /// <summary>
        /// Creates an empty paginated result
        /// </summary>
        /// <param name="page">Current page number</param>
        /// <param name="pageSize">Number of items per page</param>
        public PaginatedResult(int page, int pageSize) : this(Enumerable.Empty<T>(), page, pageSize, 0)
        {
        }

        /// <summary>
        /// Gets the page numbers to display in pagination controls
        /// </summary>
        /// <param name="maxPagesToShow">Maximum number of page numbers to show</param>
        /// <returns>Collection of page numbers</returns>
        public IEnumerable<int> GetPageNumbers(int maxPagesToShow = 10)
        {
            if (TotalPages <= maxPagesToShow)
            {
                return Enumerable.Range(1, TotalPages);
            }

            var start = Math.Max(1, Page - (maxPagesToShow / 2));
            var end = Math.Min(TotalPages, start + maxPagesToShow - 1);

            if (end - start + 1 < maxPagesToShow)
            {
                start = Math.Max(1, end - maxPagesToShow + 1);
            }

            return Enumerable.Range(start, end - start + 1);
        }
    }
} 