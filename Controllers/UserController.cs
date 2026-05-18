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
    public class UserController : Controller
    {
        private readonly DataContext dataContext;
        private JwtHelper jwtHelper;

        public UserController(DataContext dataContext, JwtHelper jwtHelper)
        {
            this.dataContext = dataContext;
            this.jwtHelper = jwtHelper;
        }
        [HttpPost("user/signin")]
        public async Task<IActionResult> SignIn([FromBody] CompletUserHelper model)
        {
            if (model is null || model.UserHelper is null || model.confidentialityHelper is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (!TestDateHelper.IsAnAdult(model.UserHelper.Birthday))
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Inscription refusée",
                        detail: "Vous devez être majeur pour créer un compte"
                    );
            }

            if (dataContext?.User is null || dataContext?.Confidentiality is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            bool emailExist = await dataContext.Confidentiality.AnyAsync(e => e.Email.Equals(model.confidentialityHelper.Email));
            if (emailExist)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Email existe",
                        detail: "Vous ne pouvez pas creer un compte avec cet email"
                    );
            }
            ConfidentialityModel newConfidentiality = new(){
                Email = model.confidentialityHelper!.Email,
                Password = EncryptionPasswordHelper.HashPassword(model.confidentialityHelper.Password!)
            };

            await dataContext.Confidentiality.AddAsync(newConfidentiality);

            UserModel newUser = new()
            {
                IdConfidentiality = newConfidentiality.Id.ToString().ToUpper(),
                FirstName = model.UserHelper.FirstName,
                LastName = model.UserHelper.LastName,
                Address = model.UserHelper.Address,
                Birthday = model.UserHelper.Birthday,
                AccountOk = true,
            };
            await dataContext.User.AddAsync(newUser);
            
            await dataContext.SaveChangesAsync();
            return Ok(true);
        }

        [HttpPost("user/signup")]
        public async Task<IActionResult> SignUp([FromBody] ConfidentialityHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }
            if (dataContext?.User is null || dataContext?.Confidentiality is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            var confidentiality = await dataContext.Confidentiality.FirstOrDefaultAsync(e => e.Email!.Equals(model.Email));
            if (confidentiality is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Email introuvable",
                        detail: "Email incorrect"
                    );
            }

            bool isPasswordOk = EncryptionPasswordHelper.VerifyPassword(model.Password!, confidentiality.Password!);
            if (!isPasswordOk)
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Echec de l'authentification",
                        detail: "Mot de passe incorrecte"
                    );
            }

            var user = await dataContext.User.FirstOrDefaultAsync(u => u.IdConfidentiality!.Equals(confidentiality.Id.ToString().ToUpper()));
            if(user is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );
            }
            //if(!user.AccountOk)
            //{
            //    return Problem(
            //            statusCode: StatusCodes.Status401Unauthorized,
            //            title: "Compte bloquée",
            //            detail: "Votre compte & été bloquée pour une fraude, veillez contacté le service PaySim pour le débloqué"
            //        );
            //}
            var token = jwtHelper.GenerateToken(user.Id, confidentiality.Email);
            int day = (model.Remeber) ? 7 : 1;

            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(day)
            });
            Response.Headers["Cache-Control"] = "no-store, no-cache,must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return Ok(user);
        }

        [Authorize]
        [HttpGet("user/me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid id))
            {
                return Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Utilisateur introuvable",
                        detail: "Vous n'avez pas l'acces à cet compte"
                    );
            }
            if (dataContext == null || dataContext?.User == null)
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );

            var user = await dataContext.User.FindAsync(id);
            if (user == null)
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );

            return Ok(user);
        }


        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            //suppression
            //Response.Cookies.Delete("jwtToken");


            Response.Cookies.Delete("jwtToken", new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
            return Ok(new { message = "Déconnexion réussie" });
        }
        [Authorize]
        [HttpPost("logout/project")]
        public IActionResult LogoutProject()
        {
            //suppression
            //Response.Cookies.Delete("jwtToken");


            Response.Cookies.Delete("jwtTokenApi", new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
            return Ok(new { message = "Déconnexion réussie" });
        }
    }
}
