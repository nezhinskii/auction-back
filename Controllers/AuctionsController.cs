using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Hubs;
using AuctionService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IHubContext<AuctionHub> _hubContext;

        public AuctionsController(AuctionDbContext context, IHubContext<AuctionHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveAuctions()
        {
            var auctions = await _context.Auctions
                .Where(a => a.Status == "Open")
                .Include(a => a.Owner)
                .ToListAsync();
            return Ok(auctions);
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAuction([FromForm] CreateAuctionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var auction = new Auction
            {
                Title = dto.Title,
                Description = dto.Description,
                OwnerId = int.Parse(User.Identity.Name),
                Status = "Open",
                Bids = new List<Bid>()
            };

            if (dto.Image != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Image.FileName)}";
                var filePath = Path.Combine("wwwroot/images", fileName);

                Directory.CreateDirectory("wwwroot/images");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(stream);
                }

                auction.ImageUrl = $"/images/{fileName}";
            }

            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNewAuctionNotification", auction.Id, auction.Title);

            return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, auction);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuctionById(int id)
        {
            var auction = await _context.Auctions
                .Include(a => a.Owner)
                .Include(a => a.Winner)
                .Include(a => a.Bids)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
                return NotFound();

            return Ok(auction);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            var auction = await _context.Auctions.FindAsync(id);
            if (auction == null)
                return NotFound();

            if (auction.OwnerId != int.Parse(User.Identity.Name))
                return Forbid();

            _context.Auctions.Remove(auction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPut("{id}/close")]
        public async Task<IActionResult> CloseAuction(int id)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
                return NotFound();

            if (auction.OwnerId != int.Parse(User.Identity.Name))
                return Forbid();

            if (auction.Status != "Open")
                return BadRequest("Auction is not in Open state");

            auction.Status = "Closing";
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"Auction_{id}").SendAsync("ReceiveAuctionStatusUpdate", id, "Closing");

            return Ok();
        }

        [Authorize]
        [HttpPut("{id}/sell")]
        public async Task<IActionResult> SellAuction(int id, [FromBody] SellAuctionDto dto)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
                return NotFound();

            if (auction.OwnerId != int.Parse(User.Identity.Name))
                return Forbid();

            if (auction.Status != "Closing")
                return BadRequest("Auction is not in Closing state");

            var winningBid = auction.Bids.FirstOrDefault(b => b.Id == dto.WinningBidId);
            if (winningBid == null)
                return BadRequest("Invalid winning bid ID");

            auction.Status = "Sold";
            auction.WinnerId = winningBid.UserId;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"Auction_{id}").SendAsync("ReceiveAuctionStatusUpdate", id, "Sold");

            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/bid")]
        public async Task<IActionResult> PlaceBid(int id, [FromBody] PlaceBidDto dto)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
                return NotFound();

            if (auction.Status != "Open")
                return BadRequest("Auction is not open for bidding");

            var userId = int.Parse(User.Identity.Name);
            if (auction.OwnerId == userId)
                return BadRequest("Owner cannot bid on their own auction");

            var bid = new Bid
            {
                Amount = dto.Amount,
                AuctionId = id,
                UserId = userId,
                BidTime = DateTime.UtcNow
            };

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            await _hubContext.Clients.Group($"Auction_{id}").SendAsync("ReceiveBidUpdate", id, user.UserName, dto.Amount);

            return Ok();
        }
    }
}