using Microsoft.AspNetCore.Mvc;
using WebApi.User;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]/")]
public class UserController : ControllerBase
{
    private readonly UserRepository _repository;

    public UserController(UserRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("Create")]
    public async Task<Guid> Create()
    {
        UserAggregate user = new();
        await _repository.Add(user);
        return new Guid(user.Id);
    }

    [HttpPost("UpdateName/{id:guid}/{name:alpha}")]
    public async Task UpdateName(Guid id, string name)
    {
        var user = await _repository.TryGet(id.ToString());
        if (user == null)
        {
            throw new Exception("Not found");
        }
        user.SetName(name);
        await _repository.Update(user);
    }

    [HttpPost("CreateWithName/{name:alpha}")]
    public async Task<Guid> CreateWithName(string name)
    {

        UserAggregate user = new();
        user.SetName(name);
        await _repository.Add(user);
        return new Guid(user.Id);
    }

    [HttpGet("Get/{id:guid}")]
    public async Task<UserDto> Get(Guid id)
    {
        var user = await _repository.TryGet(id.ToString());
        if (user == null)
        {
            throw new Exception("Not found");
        }
        UserDto userDto = new()
        {
            Id = new Guid(user.Id),
            Name = user.Name,
        };
        return userDto;
    }

    [HttpGet("GetProjection/{id:guid}")]
    public object GetProjection(Guid id)
    {
        var projection = _repository.GetUserProjection(id);
        return projection;
    }

    [HttpGet("GetUserNameProjection")]
    public object GetUserNameProjection()
    {
        var projection = _repository.GetUserNameProjections();
        return projection;
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
