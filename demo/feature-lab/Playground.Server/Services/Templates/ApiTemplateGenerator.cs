using System;

namespace Playground.Server.Services.Templates
{
    public partial class ApiTemplateGenerator
    {
        public static string GenerateApiServerCode()
        {
            return @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace ApiServer
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(""v1"", new OpenApiInfo { Title = ""API Server"", Version = ""v1"" });
            });
            services.AddCors(options =>
            {
                options.AddPolicy(""AllowAll"", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint(""/swagger/v1/swagger.json"", ""API Server v1""));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(""AllowAll"");
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    [ApiController]
    [Route(""api/[controller]"")]
    public partial class UsersController : ControllerBase
    {
        private static List<User> users = new List<User>
        {
            new User { Id = 1, Name = ""John Doe"", Email = ""john@example.com"" },
            new User { Id = 2, Name = ""Jane Smith"", Email = ""jane@example.com"" }
        };

        [HttpGet]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            return Ok(users);
        }

        [HttpGet(""{id}"")]
        public ActionResult<User> GetUser(int id)
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public ActionResult<User> CreateUser([FromBody] CreateUserRequest request)
        {
            var user = new User
            {
                Id = users.Max(u => u.Id) + 1,
                Name = request.Name,
                Email = request.Email
            };
            users.Add(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut(""{id}"")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            user.Name = request.Name;
            user.Email = request.Email;
            return NoContent();
        }

        [HttpDelete(""{id}"")]
        public IActionResult DeleteUser(int id)
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            users.Remove(user);
            return NoContent();
        }
    }

    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public partial class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public partial class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}";
        }
    }
}
