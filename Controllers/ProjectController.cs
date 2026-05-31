using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API_PAYSIM.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly DataContext dataContext;

        public ProjectController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        [Authorize]
        [HttpPost("projet/new")]
        public async Task<IActionResult> NewDeveloper([FromBody] ProjectHelper model)
        {

            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (dataContext?.Project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                );
            }

            ProjectModel newproject = new()
            {
                IdDeveloper = model.IdDeveloper!.ToUpperInvariant(),
                ProjectName = model.ProjectName,
                Link = model.Link,
                ApiKey = ApiKeyHashHelper.HashApiKey(model.ApiKey!),
            };
            newproject.ApiKeyPrefix = newproject.ApiKey.Substring(0, 10);

            await dataContext.Project.AddAsync(newproject);


            await dataContext.SaveChangesAsync();
            return Ok(newproject);
        }

        [Authorize]
        [HttpGet("project/getall")]
        public async Task<IActionResult> ProjectGetAll()
        {
            if (dataContext?.Developer is null || dataContext.Project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid id))
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Utilisateur introuvable",
                        detail: "Vous n'avez pas l'acces à cet compte"
                    );
            }
            var developer = await dataContext.Developer.FirstOrDefaultAsync(d => d.IdUser.ToUpper().Equals(userId.ToUpper()));
            if (developer == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Compte developpeur introuvable",
                        detail: "Un problème est survenue lors de la recherche de votre compte"
                    );
            }
            var projects = await dataContext.Project.Where(p => p.IdDeveloper!.ToUpper().Equals(developer.Id.ToString().ToUpper())).ToListAsync();

            return Ok(projects);
        }

        [Authorize]
        [HttpGet("project/getone/{id}")]
        public async Task<IActionResult> ProjectGetOne(string id)
        {
            if (dataContext.Project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            var project = await dataContext.Project.FirstOrDefaultAsync(p => p.Id.ToString().ToUpper().Equals(id.ToUpper()));
            if (project == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "projet introuvable",
                        detail: "Un problème est survenue lors de la recherche de votre projet"
                    );
            }
            return Ok(project);
        }

        [HttpGet("project/getone/byapi/{api}")]
        public async Task<IActionResult> ProjectGetOneByApi(string api)
        {
            if (dataContext.Project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            var project = await dataContext.Project.FirstOrDefaultAsync(p => p.Id.ToString().ToUpper().Equals(api.ToUpper()));
            if (project == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "projet introuvable",
                        detail: "Un problème est survenue lors de la recherche de votre projet"
                    );
            }
            return Ok(project);
        }

        [Authorize]
        [HttpPut("project/update")]
        public async Task<IActionResult> UpdateProject([FromBody] ProjectHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (dataContext?.Project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                );
            }

            var project = await dataContext.Project.FirstOrDefaultAsync(p => p.Id.ToString().ToUpper().Equals(model.Id!.ToUpper()));
            if (project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Projet introuvable",
                        detail: "Aucun projet trouvée"
                );
            }

            project.ProjectName = model.ProjectName;
            project.Link = model.Link;
            if (!model.ApiKey!.Equals("N/A"))
            {
                project.ApiKey = model.ApiKey;
            }

            await dataContext.SaveChangesAsync();
            return Ok(project);
        }
        [Authorize]

        [HttpGet("generate/apikey")]
        public IActionResult GenerateApi()
        {
            var api = ApiKeyHashHelper.GenerateApiKey();
            ApiHelper newApi = new() { Api = api };
            return Ok(newApi);
        }

    }
}
