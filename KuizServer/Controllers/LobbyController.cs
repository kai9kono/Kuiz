using Microsoft.AspNetCore.Mvc;
using KuizServer.Services;

namespace KuizServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LobbyController : ControllerBase
{
    private readonly LobbyService _lobbyService;

    public LobbyController(LobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    [HttpGet("{lobbyCode}")]
    public IActionResult GetLobby(string lobbyCode)
    {
        var lobby = _lobbyService.GetLobbyState(lobbyCode);
        return Ok(lobby);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "lobby", timestamp = DateTime.UtcNow });
    }
}
