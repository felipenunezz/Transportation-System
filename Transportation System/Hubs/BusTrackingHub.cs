using Microsoft.AspNetCore.SignalR;
using Transportation_System.Models.Domain;

namespace Transportation_System.Hubs;

public class BusTrackingHub : Hub
{
 public async Task SendAsync(string Message, Bus bus)
 {
     await Clients.All.SendAsync("ReceiveMessage", Message, bus);
 }
}