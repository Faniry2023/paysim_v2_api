using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.Models;
using API_PAYSIM.TestDeveloper;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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

        [HttpGet("insert/user/test")]
        public async Task<IActionResult> InitializeUser()
        {

            if (dataContext?.User is null || dataContext?.Confidentiality is null || dataContext.Project is null || dataContext.Developer is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            ConfidentialityModel conf = new()
            {
                Email = "mahazotiana08@gmail.com",
                Password = EncryptionPasswordHelper.HashPassword("Faniry,2001")
            };
            await dataContext.Confidentiality.AddAsync(conf);

            UserModel us = new()
            {
                IdConfidentiality = conf.Id.ToString().ToUpper(),
                FirstName = "rafanomezntsoa".ToUpper(),
                LastName = "Faniry",
                Address = "Fianarantsoa",
                Birthday = new DateTime(2001,2,8),
                AccountOk = true
            };
            await dataContext.User.AddAsync(us);

            DeveloperModel dev = new()
            {
                IdUser = us.Id.ToString().ToUpper(),
                Cin = "108301258027",
                NumberAirtel = "0338431013",
                NumberOrange = "N/A",
                NumberYas = "0344741133"
            };
            await dataContext.Developer.AddAsync(dev);

            var newapiKey = ApiKeyHashHelper.GenerateApiKey();
            ProjectModel project = new()
            {
                IdDeveloper = dev.Id.ToString().ToUpper(),
                ProjectName = "PaySim",
                Link = "https://paysim-yy8x.onrender.com",
                ApiKey = ApiKeyHashHelper.HashApiKey(newapiKey),
            };
            project.ApiKeyPrefix = project.ApiKey.Substring(0, 8);
            await dataContext.Project.AddAsync(project);

            await dataContext.SaveChangesAsync();
            InitializeBdDeveloper initialAccount = new()
            {
                Confidentiality = conf,
                User = us,
                Developer = dev,
                Project = project,
                apiKey = newapiKey
            };
            return Ok(initialAccount);
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
