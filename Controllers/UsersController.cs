using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserValidationService _validationService;

    public UsersController(IUserService userService, UserValidationService validationService)
    {
        _userService = userService;
        _validationService = validationService;
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_userService.GetAll());

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var user = _userService.GetById(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateUserRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        if (!_validationService.TryValidateUser(request.Name, request.Email, out var validationError))
        {
            return BadRequest(new { error = validationError });
        }

        var user = _userService.Create(request);
        return Created($"/users/{user.Id}", user);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] UpdateUserRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        if (!_validationService.TryValidateUser(request.Name, request.Email, out var validationError))
        {
            return BadRequest(new { error = validationError });
        }

        var updatedUser = _userService.Update(id, request);
        return updatedUser is null ? NotFound() : Ok(updatedUser);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var removed = _userService.Delete(id);
        return removed ? NoContent() : NotFound();
    }
}
