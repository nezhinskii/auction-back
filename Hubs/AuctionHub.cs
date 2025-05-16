using Microsoft.AspNetCore.SignalR;

namespace AuctionService.Hubs
{
    public class AuctionHub : Hub
    {
        public async Task SendBidUpdate(int auctionId, string username, decimal amount)
        {
            await Clients.Group($"Auction_{auctionId}").SendAsync("ReceiveBidUpdate", username, amount);
        }

        public async Task SendAuctionStatusUpdate(int auctionId, string status)
        {
            await Clients.Group($"Auction_{auctionId}").SendAsync("ReceiveAuctionStatusUpdate", status);
        }

        public async Task JoinAuction(int auctionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Auction_{auctionId}");
        }

        public async Task LeaveAuction(int auctionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Auction_{auctionId}");
        }

        public async Task SendNewAuctionNotification(int auctionId, string title)
        {
            await Clients.All.SendAsync("ReceiveNewAuctionNotification", auctionId, title);
        }
    }
}