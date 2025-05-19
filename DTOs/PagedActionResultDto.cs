namespace AuctionService.DTOs
{
    public class PagedAuctionResultDto
    {
        public List<AuctionDto> Auctions { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class AuctionDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string? ImageUrl { get; set; }
        public int OwnerId { get; set; }
        public string OwnerUsername { get; set; }
        public int? WinnerId { get; set; }
        public string? WinnerUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal? Price { get; set; }
    }
}