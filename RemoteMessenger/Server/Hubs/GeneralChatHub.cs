using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using RemoteMessenger.Server.Repositories;
using RemoteMessenger.Server.Services;

namespace RemoteMessenger.Server.Hubs;

// TODO: Fix Authorization
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class GeneralChatHub : Hub
{
    public const string HubUrl = "/hubs/general_chat";
    private readonly ILogger<GeneralChatHub> _logger;
    private readonly UserRepository _userRepository;
    private readonly MessengerContext _context;
    
    public GeneralChatHub(ILogger<GeneralChatHub> logger, UserRepository userRepository, MessengerContext context)
    {
        _logger = logger;
        _userRepository = userRepository;
        _context = context;
    }
    
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"{Context.User.Identity.Name} connected");
        var lastMessages = await Task.Run(() =>
            _context.PublicMessages
                .Include(m => m.Sender)
                .OrderByDescending(x => x.Id)
                .Take(100));
        var result = await lastMessages.Reverse().ToListAsync();
        
        await Clients.Caller.SendAsync("OnConnect", result);
        await base.OnConnectedAsync();
    }
    
    public async Task SendMessage(string message, string username)
    {
        var user = await _userRepository.GetUserAsync(username);
        if (user is null) return; 
        var sentMessage = new PublicMessage
        {
            Sender = user,
            Body = message,
            SendTime = DateTime.UtcNow,
        };
        await _context.PublicMessages.AddAsync(sentMessage);
        await _context.SaveChangesAsync();
        await Clients.All.SendAsync("OnReceiveMessage", sentMessage);
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        _logger.LogInformation($"Disconnected {e?.Message} {Context.User.Identity.Name}");
        await base.OnDisconnectedAsync(e);
    }
}