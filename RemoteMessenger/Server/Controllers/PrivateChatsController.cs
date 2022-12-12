using Microsoft.AspNetCore.Mvc;
using RemoteMessenger.Server.Util;
using RemoteMessenger.Shared.Models;

namespace RemoteMessenger.Server.Controllers;

[Route("/api/private_chats")]
[Authorize]
public class PrivateChatsController : ControllerBase
{
    private readonly MessengerContext _context;

    public PrivateChatsController(MessengerContext context)
    {
        _context = context;
    }
    
    [HttpGet("getMyChats")]
    public async Task<ActionResult<List<PrivateChat>>> GetMyChats()
    {
        var username = await HttpContext.User.GetUniqueNameAsync();
        var result =
            await Task.Run(() => _context.PrivateChats
                .Include(chat => chat.Messages)
                .Where(chat => chat.IsUserInChat(username).Result)
                .OrderBy(chat => chat.Id)
                .ToList());
        return Ok(result);
    }

    [HttpGet("{chatId:int}")]
    public async Task<ActionResult<List<PrivateMessage>>> GetPrivateMessages(int chatId)
    {
        var username = await HttpContext.User.GetUniqueNameAsync();
        var chat = await _context.PrivateChats.FirstOrDefaultAsync(chat => chat.Id == chatId);
        if (chat is null) return BadRequest("No such chat exists");
        if (!await chat.IsUserInChat(username)) return Unauthorized("User is not in chat");
        return Ok(chat.Messages);
    }
    
    [HttpGet("{username}")]
    public async Task<ActionResult<List<PrivateMessage>>> GetPrivateMessages(string username)
    {
        var myUsername = await HttpContext.User.GetUniqueNameAsync();
        var chat = await _context.PrivateChats.FirstOrDefaultAsync(
            chat => chat.FirstUser.Username == username ||  chat.SecondUser.Username == username
            && chat.FirstUser.Username == myUsername ||  chat.SecondUser.Username == myUsername);
        if (chat is null) return NotFound("No such chat exists");
        if (!await chat.IsUserInChat(myUsername)) return BadRequest("User is not in chat");
        return Ok(chat.Messages);
    }
    
    // TODO: New DMS Controller
}