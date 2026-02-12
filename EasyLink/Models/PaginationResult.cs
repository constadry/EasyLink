namespace EasyLink.Models
{
    public class PaginationResult<T>
    {
        public List<T> Items { get; set; }
        public Pagination Pagination { get; set; }
    }
}