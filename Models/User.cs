using Microsoft.AspNetCore.Identity;

namespace AuctionService.Models
{
    public class User : IdentityUser<int>
    {
        public List<Auction> CreatedAuctions { get; set; } = new List<Auction>();
        public List<Auction> WonAuctions { get; set; } = new List<Auction>();
        public List<Bid> Bids { get; set; } = new List<Bid>();
    }
}