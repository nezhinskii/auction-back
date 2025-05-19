using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Hubs;
using AuctionService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System;
using Microsoft.AspNetCore.Http.HttpResults;

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
        public async Task<IActionResult> GetActiveAuctions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("Invalid page number or page size");

            var query = _context.Auctions
                .Where(a => a.Status == "Open")
                .Include(a => a.Owner)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var auctions = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuctionDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Status = a.Status,
                    ImageUrl = a.ImageUrl,
                    OwnerId = a.OwnerId,
                    OwnerUsername = a.Owner.UserName,
                    WinnerId = null,
                    WinnerUsername = null,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    Price = a.Price
                })
                .ToListAsync();

            var result = new PagedAuctionResultDto
            {
                Auctions = auctions,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }

        [Authorize]
        [HttpGet("created")]
        public async Task<IActionResult> GetUserCreatedAuctions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("Invalid page number or page size");

            var userId = int.Parse(User.Identity.Name);

            var query = _context.Auctions
                .Where(a => a.OwnerId == userId)
                .Include(a => a.Owner)
                .Include(a => a.Winner)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var auctions = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuctionDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Status = a.Status,
                    ImageUrl = a.ImageUrl,
                    OwnerId = a.OwnerId,
                    OwnerUsername = a.Owner.UserName,
                    WinnerId = a.WinnerId,
                    WinnerUsername = a.Winner != null ? a.Winner.UserName : null,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    Price = a.Price
                })
                .ToListAsync();

            var result = new PagedAuctionResultDto
            {
                Auctions = auctions,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }

        [Authorize]
        [HttpGet("participated")]
        public async Task<IActionResult> GetUserParticipatedAuctions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("Invalid page number or page size");

            var userId = int.Parse(User.Identity.Name);

            var query = _context.Auctions
                .Where(a => a.Bids.Any(b => b.UserId == userId))
                .Include(a => a.Owner)
                .Include(a => a.Winner)
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var auctions = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuctionDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Status = a.Status,
                    ImageUrl = a.ImageUrl,
                    OwnerId = a.OwnerId,
                    OwnerUsername = a.Owner.UserName,
                    WinnerId = a.WinnerId,
                    WinnerUsername = a.Winner != null ? a.Winner.UserName : null,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    Price = a.Price
                })
                .ToListAsync();

            var result = new PagedAuctionResultDto
            {
                Auctions = auctions,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
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
                Bids = new List<Bid>(),
                Price = null
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

            var fetchedAuction = await _context.Auctions
                .Include(a => a.Owner)
                .FirstOrDefaultAsync(a => a.Id == auction.Id);

            return CreatedAtAction(nameof(GetAuctionById), new { id = auction.Id }, new AuctionDto
            {
                Id = fetchedAuction.Id,
                Title = fetchedAuction.Title,
                Description = fetchedAuction.Description,
                Status = fetchedAuction.Status,
                ImageUrl = fetchedAuction.ImageUrl,
                OwnerId = fetchedAuction.OwnerId,
                OwnerUsername = fetchedAuction.Owner?.UserName,
                WinnerId = fetchedAuction.WinnerId,
                WinnerUsername = null,
                CreatedAt = fetchedAuction.CreatedAt,
                UpdatedAt = fetchedAuction.UpdatedAt,
                Price = fetchedAuction.Price
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuctionById(int id)
        {
            var auction = await _context.Auctions
                .Include(a => a.Owner)
                .Include(a => a.Winner)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
                return NotFound();

            return Ok(new AuctionDto
            {
                Id = auction.Id,
                Title = auction.Title,
                Description = auction.Description,
                Status = auction.Status,
                ImageUrl = auction.ImageUrl,
                OwnerId = auction.OwnerId,
                OwnerUsername = auction.Owner?.UserName,
                WinnerId = auction.WinnerId,
                WinnerUsername = auction.Winner != null ? auction.Winner.UserName : null,
                CreatedAt = auction.CreatedAt,
                UpdatedAt = auction.UpdatedAt,
                Price = auction.Price
            });
        }

        [HttpGet("{auctionId}/bids")]
        public async Task<IActionResult> GetAuctionBids(int auctionId)
        {
            var bids = await _context.Bids
                .Where(b => b.AuctionId == auctionId)
                .Include(b => b.User)
                .OrderByDescending(b => b.BidTime)
                .ToListAsync();

            return Ok(bids.Select(b => new BidDto
            {
                Id = b.Id,
                Amount = b.Amount,
                BidTime = b.BidTime,
                UserId = b.User.Id,
                UserName = b.User.UserName,
                AuctionId = b.AuctionId
            }));
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

            await _hubContext.Clients.All.SendAsync("ReceiveAuctionStatusUpdate", id, "Closing");

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
            auction.Price = winningBid.Amount;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveAuctionStatusUpdate", id, "Sold");

            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/bid")]
        public async Task<IActionResult> PlaceBid(int id, [FromBody] PlaceBidDto dto)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
                return NotFound();

            if (auction.Status != "Open")
                return BadRequest("Auction is not open for bidding");

            var userId = int.Parse(User.Identity.Name);
            if (auction.OwnerId == userId)
                return BadRequest("Owner cannot bid on their own auction");

            var currentMaxBid = auction.Bids.Any() ? auction.Bids.Max(b => b.Amount) : 0;
            var previousMaxBid = auction.Bids.FirstOrDefault(b => b.Amount == currentMaxBid);

            var bid = new Bid
            {
                Amount = dto.Amount,
                AuctionId = id,
                UserId = userId,
                BidTime = DateTime.UtcNow
            };

            _context.Bids.Add(bid);
            auction.Bids.Add(bid);
            auction.Price = auction.Bids.Max(b => b.Amount);
            auction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            var bidDto = new BidDto
            {
                Id = bid.Id,
                Amount = bid.Amount,
                UserId = userId,
                UserName = user.UserName,
                BidTime = bid.BidTime,
                AuctionId = bid.AuctionId
            };

            await _hubContext.Clients.Group($"Auction_{id}").SendAsync("ReceiveBidUpdate", bidDto);

            if (previousMaxBid != null && dto.Amount > currentMaxBid && previousMaxBid.UserId != userId)
            {
                await _hubContext.Clients.User(previousMaxBid.UserId.ToString())
                    .SendAsync("ReceiveOutbidNotification", auction.Id, auction.Title, dto.Amount);
            }

            return Ok();
        }
    }
}