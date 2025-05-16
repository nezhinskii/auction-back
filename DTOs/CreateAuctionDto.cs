namespace AuctionService.DTOs
{
    public class CreateAuctionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}