using Microsoft.AspNetCore.Mvc;
using QueueDemo.Models;
using QueueDemo.Services;

namespace QueueDemo.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserQueue _queue;

    public UsersController(UserQueue queue)
    {
        _queue = queue;
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        await _queue.EnqueueAsync(user);

        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserEmail", user.Email);

        return Ok("User added to queue and session stored");
    }
}