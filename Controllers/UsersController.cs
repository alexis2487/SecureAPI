using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SecureAPI.Models;

namespace SecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]


    public class UsersController : ControllerBase
    {
        private static List<User> _users = new List<User>
        {
            new User { Id = 1, Name = "Alexis Martinez", Email = "alexis@empresa.com", Role = "admin" },
            new User { Id = 2, Name = "Juan Perez", Email = "juan@empresa.com", Role = "user" },
            new User { Id = 3, Name = "Maria Lopez", Email = "maria@empresa.com", Role = "user" }
        };

        // GET api/users
        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAll()
        {
            return Ok(_users);
        }

        // GET api/users/1
        [HttpGet("{id}")]
        public ActionResult<User> GetById(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado" });
            return Ok(user);
        }

        // POST api/users
        [HttpPost]
        public ActionResult<User> Create([FromBody] User newUser)
        {
            newUser.Id = _users.Max(u => u.Id) + 1;
            newUser.CreatedAt = DateTime.UtcNow;
            _users.Add(newUser);
            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, newUser);
        }

        // DELETE api/users/1
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado" });
            _users.Remove(user);
            return NoContent();
        }
    }
}