/*
using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.Helpers.KotlinTestHelper;
using API_PAYSIM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API_PAYSIM.Controllers
{
    public class KotlinTestController : Controller
    {
        private static int page = 0;
        private readonly DataContext dataContext;

        public KotlinTestController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        [HttpGet("test/linq")]
        public IActionResult LinqTest()
        {
            List<UserTestKotlin> users = new();
            users.Add(new() { Id = Guid.NewGuid(), Name = "Alice", Age = 30, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Berta", Age = 12, IsAdmin = true });
            users.Add(new() { Id = Guid.NewGuid(), Name = "David", Age = 18, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Eliane", Age = 25, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Caroline", Age = 18, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Justin", Age = 18, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Jade", Age = 30, IsAdmin = true });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Albertine", Age = 32, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Berto", Age = 19, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Millan", Age = 25, IsAdmin = false });
            users.Add(new() { Id = Guid.NewGuid(), Name = "Henri", Age = 45, IsAdmin = false });

            //############################ SELECT ########################################
            var objUser = users.Select(x => new { x.Name, x.Age }).ToList();
            //=>liste objet contient seulemnt Name et Age
            var noms = users.Select(n => n.Name).ToList();
            //=>liste des noms de l'objet UserTestKotlin

            //######################### ALL ##########################
            bool isAllOk = users.All(x => x.IsAdmin);
            //verifie si tout les utilisateurs sont admin

            //############## Count ####################
            int total = users.Count();
            int userAdult = users.Count(x => x.Age >= 18); //nombre des utilisateurs ont d'age 18 et plus
            //##### Order ########################
            var usersC = users.OrderBy(x => x.Age).ToList();
            var usersD = users.OrderByDescending(x => x.Age).ToList();
            // ########## take skip##############
            var usTake = users.Take(2).ToList(); //2premier user recuperer
            //var usSkip = users.Skip(2).ToList(); //Ignorer les 2 premier users
            //var userPage = users.Skip(3).Take(3).ToList();

            //#########  distinct (supprimer les doublons) ####################
            var userD = users.Select(u => u.Age).Distinct().ToList();

            //################### existe t il deja dans la liste ######################
            UserTestKotlin cont = new() { Id= Guid.NewGuid(), Name="David",Age = 25, IsAdmin=false };
            bool isExist = users.Contains(cont);

            //###################### groupe by ########################
            var grps = users.GroupBy(u => u.Age);

            //############# SUM- Average #################
            var totalAge = users.Sum(u => u.Age); //total age
            // => Average = moyenne
            // => Max = valeur max
            // => Min = valeur min
            if(page > users.Count())
            {
                page = 0;
                
            }
            var pagination = users.Skip(page).Take(3).ToList();
            page += 3;
            return Ok(pagination);
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
        [HttpGet("initial/historiq")]
        public async Task<IActionResult> InitHistoric()
        {

            if (dataContext?.Historical is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            int numref = 130;
            Random random = new();
            for (int i = 1; i <= 47; i++)
            {
                HistoricalModel hi = new()
                {
                    Id = Guid.NewGuid(),
                    IdCustomer = "ee1b3114-6be0-4bc6-8e15-08debf52a71b".ToUpper(),
                    IdPayment = Guid.NewGuid().ToString(),
                    IdDeveloper = "cbd09153-ca8a-422e-05e2-08dec58e7d16".ToUpper(),
                    Name_developer = "Amazon",
                    Reference = "ref-0-" + (numref + i),
                    Reason = GenerateApiKeyHelper.GenerateReason(),
                    Price = random.Next(100, 10000),
                    NumberDeveloper = "0342958848",
                    Created_at = DateTime.UtcNow,
                };

                await dataContext.Historical.AddAsync(hi);
                await dataContext.SaveChangesAsync();
            }



            return Ok("mety");
        }

    }
}
*/