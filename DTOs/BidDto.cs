namespace AuctionService.DTOs
{
    public class BidDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime BidTime { get; set; }
        public int AuctionId { get; set; }
    }
}