using API_PAYSIM.Data;
using API_PAYSIM.Helpers.KotlinTestHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API_PAYSIM.Controllers
{
    [Authorize]
    public class KotlinTestController : Controller
    {
        private readonly DataContext dataContext;

        public KotlinTestController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        [HttpPost("kotlin/test/new")]
        public async Task<IActionResult> NewUser([FromBody] UserTestKotlinHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (dataContext?.UserKotlin is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            UserTestKotlin userTest = new()
            {
                Name = model.Name,
                Age = model.Age,
                IsAdmin = model.IsAdmin
            };

            await dataContext.UserKotlin.AddAsync(userTest);

            await dataContext.SaveChangesAsync();

            return Ok(userTest);
        }

        [HttpGet("kotlin/test/getall")]
        public async Task<IActionResult> GetAll()
        {
            if (dataContext?.UserKotlin is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            var users = await dataContext.UserKotlin.ToListAsync();

            return Ok(users);
        }

    }
}
