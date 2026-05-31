using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.Helpers.PayHelper;
using API_PAYSIM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API_PAYSIM.Controllers
{
   
    public class DeveloperAndProjectController : Controller
    {
        private readonly DataContext dataContext;
        private JwtHelper jwtHelper;
        public DeveloperAndProjectController(DataContext dataContext, JwtHelper jwtHelper)
        {
            this.dataContext = dataContext;
            this.jwtHelper = jwtHelper;
        }
        [Authorize]
        [HttpPost("developer/new")]
        public async Task<IActionResult> NewDeveloper([FromBody] DeveloperHelper model)
        {

            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (dataContext?.Developer is null)
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
            DeveloperModel newDeveloper = new()
            {
                IdUser = id.ToString().ToUpper(),
                Cin = model.Cin,
                NumberYas = model.NumberYas,
                NumberAirtel = model.NumberAirtel,
                NumberOrange = model.NumberOrange,
            };

            await dataContext.Developer.AddAsync(newDeveloper);


            await dataContext.SaveChangesAsync();
            return Ok(newDeveloper);
        }
        [Authorize]
        [HttpGet("developer/get")]
        public async Task<IActionResult> GetDeveloper()
        {
            if (dataContext?.Developer is null)
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
            var developer = await dataContext.Developer.FirstOrDefaultAsync(d => d.IdUser!.ToUpper().Equals(userId.ToUpper()));
            if(developer == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Compte developpeur introuvable",
                        detail: "Un problème est survenue lors de la recherche de votre compte"
                    );
            }
            return Ok(developer);
        }
        [Authorize]
        [HttpPut("developer/update")]
        public async Task<IActionResult> UpdateDeveloper([FromBody]DeveloperHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (dataContext?.Developer is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            var developer = await dataContext.Developer.FirstOrDefaultAsync(d => d.Id.ToString().ToUpper().Equals(model.Id!.ToString()));

            if (developer is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Compte developpeur introuvable",
                        detail: "Nous n'avons pas pu trouver votre compte développeur"
                    );
            }
            developer.Cin = model.Cin;
            developer.NumberAirtel = model.NumberAirtel;
            developer.NumberOrange = model.NumberOrange;
            developer.NumberYas = model.NumberYas;

            await dataContext.SaveChangesAsync();

            return Ok(developer);
        }
        


        [HttpPost("developer/info/setup")]
        [EnableRateLimiting("api")]
        public async Task<IActionResult> DeveloperInformationSetup([FromBody]InfoPaiDevHelper model)
        {

            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (dataContext?.Payment is null || dataContext.Project is null || dataContext.Developer is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            var prefix = model.ApiKey!.Substring(0, 10);
            var projects = await dataContext.Project.Where(p => p.ApiKeyPrefix == prefix).ToListAsync();
            var projetct = projects.FirstOrDefault(p => ApiKeyHashHelper.VerifierApiKey(model.ApiKey!, p.ApiKey!));
            //var projetct = await dataContext.Project.FirstOrDefaultAsync(p => p.ApiKey!.Equals(model.ApiKey));
            if(projetct is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Erreur authentification du projet",
                        detail: "Votre clé api est incorrecte"
                    );
            }
            var develper = await dataContext.Developer.FirstOrDefaultAsync(d => d.Id.ToString().ToUpper().Equals(projetct.IdDeveloper!.ToUpper()));
            if (develper is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Developpeur introuvable",
                        detail: "Une erreur est survenu lors de la recupération des informations"
                    );
            }
            string number = string.Empty;
            if(model.InfoNumber is not null && develper!.NumberYas is not null && develper!.NumberOrange is not null && develper!.NumberAirtel is not null)
            {
                number = model.InfoNumber switch
                {
                    "yas" => develper.NumberYas,
                    "org" => develper.NumberOrange,
                    "air" => develper.NumberAirtel,
                    _ => develper.NumberYas
                };
            }

            PaymentModel newPay = new()
            {
                IdProject = projetct.Id.ToString().ToUpper(),
                ActionKey = model.IdOrder,
                Reason = GenerateApiKeyHelper.GenerateReason(),
                Number = number,
                Price = model.Totalprice,
            };
            await dataContext.Payment.AddAsync(newPay);
            await dataContext.SaveChangesAsync();

            
            ValueQr valueQr = new()
            {
                ValueKey = "id:" + newPay.IdPayment.ToString() +
                            "/id_proj:" + newPay.IdProject +
                            "/actionkey:" + newPay.ActionKey +
                            "/reason:" + newPay.Reason +
                            "/number:" + newPay.Number +
                            "/Price:" + newPay.Price
            };
            return Ok(valueQr);
        }

    }
}
