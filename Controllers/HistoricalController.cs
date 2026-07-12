using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.Helpers.Historical;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API_PAYSIM.Controllers
{
    public class HistoricalController : Controller
    {
        private readonly DataContext dataContext;
        public HistoricalController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        [HttpGet("get/historical/user/{page}/{step}")]
        public async Task<IActionResult> GetHistorical(int page, int step)
        {
            if (dataContext?.User is null ||dataContext?.Historical is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            var id = GetUserId();
            if(id == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );
            }

            var count = await dataContext.Historical.Where(h => h.IdCustomer.ToUpper().Equals(id)).CountAsync();
            var historical = await dataContext.Historical.Where(h => h.IdCustomer.ToUpper().Equals(id))
                .Skip(page * step).Take(step).ToListAsync();
            HistoricalHelper historicalHelper = new() { Count = count, Page = page, Historicals = historical };
            return Ok(historicalHelper);
        }
        [HttpPost("get/historical/user/seach")]
        public async Task<IActionResult> GetHistoricalSearch([FromBody] HistoricalSearchHelper model)
        {
            if (model is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Requête invalide",
                        detail: "La requête est invalide ou incomplète"
                    );
            }
            if (dataContext?.User is null || dataContext?.Historical is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }
            var id = GetUserId();
            if (id == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );
            }
            var query =  dataContext.Historical.Where(h => h.IdCustomer.ToUpper().Equals(id));
            if (!string.IsNullOrEmpty(model.Name_developer))
                query = query.Where(h => h.Name_developer.Contains(model.Name_developer));
            if (!string.IsNullOrEmpty(model.Number))
                query = query.Where(h => h.NumberDeveloper.Contains(model.Number));

            if (!string.IsNullOrEmpty(model.Reference))
                query = query.Where(h => h.Reference.Contains(model.Reference));

            if (!string.IsNullOrEmpty(model.Reason))
                query = query.Where(h => h.Reason.Contains(model.Reason));

            if (model.Price.HasValue)
                query = query.Where(h => h.Price.SansVirgule() == model.Price.Value.SansVirgule());

            if (model.Date.HasValue)
                query = query.Where(h => h.Created_at.Date == model.Date.Value.ToDateTime(TimeOnly.MinValue).Date);

            var count = await query.CountAsync();
            var historical = await query.Skip(model.Page * model.step).Take(model.step).ToListAsync();
            HistoricalHelper historicalHelper = new()
            {
                Count = count,
                Page = model.Page,
                Historicals = historical,
            };
            return Ok(historicalHelper);
        }

        [HttpGet("get/historical/dev/{page}")]
        public async Task<IActionResult> GetHistoricalDev(int page)
        {
            if (dataContext?.User is null || dataContext?.HistoricalSms is null || dataContext?.Developer is null)
            {
                return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erreur Serveur",
                        detail: "Le contexte de données est introuvable"
                    );
            }

            var id = GetUserId();
            if(id == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Utilisateur introuvable",
                        detail: "Aucun profil utilisateur associé à ce compte"
                    );
            }

            var developer = await dataContext.Developer.FirstOrDefaultAsync(d => d.IdUser.ToUpper().Equals(id));
            if(developer == null)
            {
                return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Developpeur introuvable",
                        detail: "Aucun profil developpeur trouvée"
                    );
            }
            var count = await dataContext.HistoricalSms.CountAsync();
            if (count == 0)
            {
                return Ok(new HistoricalSmsHelper());
            }
            var historicalSms = await dataContext.HistoricalSms
                .Where(h => h.Id_developer.ToUpper().Equals(developer.Id.ToString().ToUpper()))
                .Skip(page).Take(5).ToListAsync();
                
            HistoricalSmsHelper historicalSmsHelper = new() { Count = count, Page = page, HistoricalSms = historicalSms };
            return Ok(historicalSms);
        }


        


        private String GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userId, out Guid id))
                return id.ToString().ToUpper();
            return null;
        }
    }
}
