namespace AuctionService.Models
{
    public class Auction
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // "Open", "Closing", "Sold"
        public string? ImageUrl { get; set; }
        public int OwnerId { get; set; }
        public User Owner { get; set; }
        public List<Bid> Bids { get; set; }
        public int? WinnerId { get; set; }
        public User? Winner { get; set; }
    }
}