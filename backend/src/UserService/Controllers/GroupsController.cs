using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly ICompanyGroupService _groupService;
    private readonly JwtHelper _jwtHelper;

    public GroupsController(ICompanyGroupService groupService, JwtHelper jwtHelper)
    {
        _groupService = groupService;
        _jwtHelper = jwtHelper;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompanyGroupResponse>>> GetGroups()
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var groups = await _groupService.GetByCompanyIdAsync(companyId.Value);
        return Ok(groups.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompanyGroupResponse>> GetGroup(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var group = await _groupService.GetByIdAsync(id, companyId.Value);
        if (group == null) return NotFound();

        return Ok(ToResponse(group));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyGroupResponse>> CreateGroup([FromBody] CreateCompanyGroupRequest request)
    {
        if (_jwtHelper.GetRoleFromToken(User) != "Boss") return Forbid();

        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var creatorId = _jwtHelper.GetUserIdFromToken(User);
        if (!companyId.HasValue || !creatorId.HasValue) return Unauthorized();

        if (request == null || string.IsNullOrWhiteSpace(request.Name) || request.LeaderUserId <= 0)
            return BadRequest(new { message = "Name and LeaderUserId are required" });

        try
        {
            var group = await _groupService.CreateAsync(
                companyId.Value,
                creatorId.Value,
                request.Name,
                request.LeaderUserId,
                request.MemberIds ?? new List<int>());

            return CreatedAtAction(nameof(GetGroup), new { id = group.Group.Id }, ToResponse(group));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { message = "Failed to create department chat", error = ex.Message });
        }
    }

    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddCompanyGroupMemberRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = _jwtHelper.GetUserIdFromToken(User);
        if (!companyId.HasValue || !userId.HasValue) return Unauthorized();

        var isBoss = _jwtHelper.GetRoleFromToken(User) == "Boss";
        var isLeader = await _groupService.IsGroupLeaderAsync(companyId.Value, id, userId.Value);
        if (!isBoss && !isLeader) return Forbid();

        if (request == null || request.UserId <= 0)
            return BadRequest(new { message = "UserId is required" });

        var ok = await _groupService.AddMemberAsync(companyId.Value, id, request.UserId);
        if (!ok) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}/members/{memberUserId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int memberUserId)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = _jwtHelper.GetUserIdFromToken(User);
        if (!companyId.HasValue || !userId.HasValue) return Unauthorized();

        var isBoss = _jwtHelper.GetRoleFromToken(User) == "Boss";
        var isLeader = await _groupService.IsGroupLeaderAsync(companyId.Value, id, userId.Value);
        if (!isBoss && !isLeader) return Forbid();

        var ok = await _groupService.RemoveMemberAsync(companyId.Value, id, memberUserId);
        if (!ok) return BadRequest(new { message = "Member cannot be removed from this group" });

        return NoContent();
    }

    private static CompanyGroupResponse ToResponse(CompanyGroupDetails details)
    {
        return new CompanyGroupResponse
        {
            Id = details.Group.Id,
            CompanyId = details.Group.CompanyId,
            Name = details.Group.Name,
            LeaderUserId = details.Group.LeaderUserId,
            ChatId = details.Group.ChatId,
            CreatedByUserId = details.Group.CreatedByUserId,
            CreatedAt = details.Group.CreatedAt,
            MemberIds = details.MemberIds
        };
    }
}

public class CreateCompanyGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public int LeaderUserId { get; set; }
    public List<int>? MemberIds { get; set; }
}

public class AddCompanyGroupMemberRequest
{
    public int UserId { get; set; }
}

public class CompanyGroupResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LeaderUserId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
