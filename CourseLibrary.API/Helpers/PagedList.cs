using Microsoft.EntityFrameworkCore;

namespace CourseLibrary.API.Helpers
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public int PageSize { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)System.Math.Ceiling(count / (double)pageSize);

            AddRange(items);
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = await source.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
