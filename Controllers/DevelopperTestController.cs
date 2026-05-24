using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.TestDeveloper;
using Microsoft.AspNetCore.Mvc;

namespace API_PAYSIM.Controllers
{
    public class DevelopperTestController : Controller
    {
        private readonly DataContext dataContext;
        private JwtHelper jwtHelper;
        public DevelopperTestController(DataContext dataContext, JwtHelper jwtHelper)
        {
            this.dataContext = dataContext; 
            this.jwtHelper = jwtHelper;
        }
        [HttpPost("insert/user/test")]
        public IActionResult InitializeUser([FromBody] InitializeBdDeveloper model)
        {
            if (model is null || model.User is null || model.Confidentiality is null || model.Developer is null || model.Project is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }

            if (!TestDateHelper.IsAnAdult((DateTime)model.User.Birthday!))
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
            return View();
        }
        [HttpGet("calcul/second")]
        public String CalculDelta(int a, int b, int c)
        {
            //ax²+bx+c=0
            //delta = b² -4*a*c
            //si delta = 0 => X1=X2 = (-b/a²)
            //si delta > 0 => X1 = (b²-Racine(delta))/a²) et X2 = (b²+Racine(delta))/a²)
            double delta = (b * b) - (4 * a * c);
            return delta.ToString();
        }
    }
}
