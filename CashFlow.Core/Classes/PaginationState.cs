namespace CashFlow.Core.Classes
{
    public enum QUERY_TYPE
    {
        TOTAL_COUNT,
        WHOLE_DATA,
        PAGE_DATA
    }

    public class PaginationState
    {
        public int TotalPages { get; set; }

        public int NextPage { get; set; }
        public int PreviousPage { get; set; }

        public int PaginationBarStart { get; set; }
        public int PaginationBarEnd { get; set; }

        public bool ShowPaginationBar { get; set; }

        public int RequestedPage { get; set; }

        public PaginationState() { }

        public int PAGES_PER_SCREEN { get; set; }
        public int LINES_PER_PAGE { get; set; }
    }
}
