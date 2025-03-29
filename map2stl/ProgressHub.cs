using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace map2stl
{

    public class ProgressHub : Hub
    {
        // Optionally, add methods to join groups or register the user.
        public override Task OnConnectedAsync()
        {
            // You can store Context.ConnectionId for later reference.
            System.Console.WriteLine($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
        // Add this method so clients can get the connection ID.
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
