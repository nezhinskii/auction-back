using AuctionService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data
{
    public class AuctionDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
        {
        }

        public DbSet<Auction> Auctions { get; set; }
        public DbSet<Bid> Bids { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Owner)
                .WithMany(u => u.CreatedAuctions)
                .HasForeignKey(a => a.OwnerId);

            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Winner)
                .WithMany(u => u.WonAuctions)
                .HasForeignKey(a => a.WinnerId);

            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Auction)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId);

            modelBuilder.Entity<Bid>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bids)
                .HasForeignKey(b => b.UserId);
        }
    }
}