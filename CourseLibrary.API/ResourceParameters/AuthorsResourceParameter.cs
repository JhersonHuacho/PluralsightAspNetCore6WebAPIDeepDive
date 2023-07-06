namespace CourseLibrary.API.ResourceParameters
{
    public class AuthorsResourceParameter
    {
        const int maxPagesSize = 20;
        public string? MainCategory { get; set; }
        public string? SearchQuery { get; set; }
        public int PageNumber { get; set; } = 1;
        
        //public int PageSize { get; set; } = 10;
        private int _pageSize = 10;
        public int PageSize 
        { 
            get => _pageSize; 
            set => _pageSize = (value > maxPagesSize) ? maxPagesSize : value; 
        }

        public string? OrderBy { get; set; } = "Name";
        public string? Fields { get; set; }
    }
}
