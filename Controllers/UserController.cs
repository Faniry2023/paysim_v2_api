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

        /// <summary>
        /// Creates a new user account
        /// </summary>
        /// <param name="model">
        /// Complete user registration information including profile and credentials
        /// </param>
        /// <returns>
        /// Returns true if the account has been successfully created
        /// </returns>
        /// <response code="200">
        /// Account successfully created
        /// </response>
        /// <response code="400">
        /// Invalid request, user is underage, or email already exists
        /// </response>
        /// <response code="500">
        /// Internal server error
        /// </response>
        [HttpPost("user/signin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                FirstName = model.UserHelper.FirstName!.ToUpper(),
                LastName = model.UserHelper.LastName,
                Address = model.UserHelper.Address,
                Birthday = model.UserHelper.Birthday,
                AccountOk = true,
            };
            await dataContext.User.AddAsync(newUser);
            
            await dataContext.SaveChangesAsync();
            return Ok(true);
        }


        /// <summary>
        /// Authenticates a user and creates a JWT session
        /// </summary>
        /// <param name="model">
        /// User login credentials
        /// </param>
        /// <returns>
        /// Returns authenticated user information and stores JWT token in cookies
        /// </returns>
        /// <response code="200">
        /// Authentication successful
        /// </response>
        /// <response code="400">
        /// Invalid request
        /// </response>
        /// <response code="401">
        /// Invalid email or password
        /// </response>
        /// <response code="404">
        /// User profile not found
        /// </response>
        /// <response code="500">
        /// Internal server error
        /// </response>
        [HttpPost("user/signup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Returns the authenticated user profile
        /// </summary>
        /// <returns>
        /// Returns the currently authenticated user information
        /// </returns>
        /// <response code="200">
        /// User successfully retrieved
        /// </response>
        /// <response code="401">
        /// Unauthorized access
        /// </response>
        /// <response code="404">
        /// User not found
        /// </response>
        /// <response code="500">
        /// Internal server error
        /// </response>
        [Authorize]
        [HttpGet("user/me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
        [HttpGet("user/conf")]
        public async Task<IActionResult> GetConfidentiality()
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
            if (dataContext == null || dataContext?.User == null || dataContext?.Confidentiality == null)
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
            var confidentiality = await dataContext.Confidentiality.FirstOrDefaultAsync(c => c.Id.ToString().ToUpper().Equals(user.IdConfidentiality!.ToUpper()));
            if (confidentiality == null)
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Confidentiality introuvable",
                        detail: "Une erreur sur le chargement des données de confidentialité"
                    );
            confidentiality.Password = string.Empty;
            return Ok(confidentiality);
        }


        [HttpPut("user/update")]
        public async Task<IActionResult> UpdateUser([FromBody]UserHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }
            if (dataContext?.User is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            var user = await dataContext.User.FirstOrDefaultAsync(u => u.Id.ToString().ToUpper().Equals(model.Id!.ToUpper()));
            if (user is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );
            }
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.Birthday = model.Birthday;

            await dataContext.SaveChangesAsync();

            return Ok(user);
        }
        /*
        [HttpPut("confidentiality/update")]
        public IActionResult UpdateConfidentiality([FromBody]ConfidentialityHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }
            if (dataContext?.User is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
        }*/


        /// <summary>
        /// Logs out the authenticated user
        /// </summary>
        /// <returns>
        /// Removes the authentication JWT cookie
        /// </returns>
        /// <response code="200">
        /// Logout successful
        /// </response>
        /// <response code="401">
        /// Unauthorized access
        /// </response>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Logs out a project session
        /// </summary>
        /// <remarks>
        /// This endpoint is currently not functional and is reserved for future project session management.
        /// </remarks>
        /// <returns>
        /// Removes the project authentication cookie
        /// </returns>
        /// <response code="200">
        /// Logout request processed
        /// </response>
        /// <response code="401">
        /// Unauthorized access
        /// </response>
        [Authorize]
        [HttpPost("logout/project")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
