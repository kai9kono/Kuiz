using Microsoft.AspNetCore.Mvc;
using KuizServer.Models;
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

    [HttpPost("create")]
    public ActionResult<string> CreateLobby([FromBody] CreateLobbyRequest request)
    {
        var code = _lobbyService.CreateLobby(request.HostName);
        return Ok(new { lobbyCode = code });
    }

    [HttpPost("join")]
    public ActionResult JoinLobby([FromBody] JoinLobbyRequest request)
    {
        var success = _lobbyService.JoinLobby(request.LobbyCode, request.PlayerName);
        if (!success)
        {
            return BadRequest(new { error = "Cannot join lobby" });
        }

        return Ok();
    }

    [HttpPost("leave")]
    public ActionResult LeaveLobby([FromBody] LeaveLobbyRequest request)
    {
        _lobbyService.LeaveLobby(request.LobbyCode, request.PlayerName);
        return Ok();
    }

    [HttpGet("{lobbyCode}")]
    public ActionResult<Lobby> GetLobby(string lobbyCode)
    {
        var lobby = _lobbyService.GetLobby(lobbyCode);
        if (lobby == null)
        {
            return NotFound();
        }

        return Ok(lobby);
    }

    [HttpPost("{lobbyCode}/settings")]
    public ActionResult UpdateSettings(string lobbyCode, [FromBody] GameSettings settings)
    {
        var success = _lobbyService.UpdateSettings(lobbyCode, settings);
        if (!success)
        {
            return NotFound();
        }

        return Ok();
    }
}

public record CreateLobbyRequest(string HostName);
public record JoinLobbyRequest(string LobbyCode, string PlayerName);
public record LeaveLobbyRequest(string LobbyCode, string PlayerName);
