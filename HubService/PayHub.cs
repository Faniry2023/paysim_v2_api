using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.Helpers.PayHelper;
using API_PAYSIM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API_PAYSIM.HubService
{
    [Authorize]
    
    public class PayHub : Hub
    {

        private static readonly HashSet<SellerCheckHelper> sellecChecks = new();
        private static readonly HashSet<ContinuationPaymentHelper> continuationPayments = new();
        private static readonly Dictionary<string, string> userConnected = new();
        private static readonly Dictionary<string, string> projectConnected = new();
        private readonly ILogger<PayHub> _logger;
        private IServiceScopeFactory _scopeFactory;

        public PayHub(ILogger<PayHub> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        //connexion v2

        public override async Task OnConnectedAsync()
        {
            var type = Context.GetHttpContext()?.Request.Query["type"].ToString();
            
            if(type == "project")
            {
                var id = Context.GetHttpContext()!.Request.Query["payId"].ToString().ToUpper();
                var id_connected = GetUserId().ToUpper();
                if (string.IsNullOrEmpty(id) || id_connected != id.ToUpper())
                {
                    await Clients.Caller.SendAsync("Error",
                        "problème de connexion entre le site et PaySim");
                    LogoutProject();
                    Context.Abort();
                    return;
                }
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                var payment = await db.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(id));
                if (payment == null)
                {
                    await Clients.Caller.SendAsync("Error", "problème de connexion entre le site et PaySim");
                    LogoutProject();
                    Context.Abort();

                    return;
                }
                projectConnected[id] = Context.ConnectionId;
            }
            else
            {
                var userId = GetUserId().ToUpper();
                userConnected[userId] = Context.ConnectionId;
            }
            // Envoyer la liste des utilisateurs connectés à tous
            await base.OnConnectedAsync();
            
        }
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong");
        }


        public async Task VerifiePaySeller(ContinuationPaymentHelper continuationPaymentHelper)
        {
            _logger.LogInformation("VerifiePaySeller appelé, helper null: {isNull}", continuationPaymentHelper == null);
            _logger.LogInformation("Reason: {reason}", continuationPaymentHelper?.Reason);
            var sellerIsExist = TestForSeller(continuationPaymentHelper.Reason!);
            if (sellerIsExist != null)
            {
                continuationPayments.Remove(continuationPaymentHelper);
                sellecChecks.Remove(sellerIsExist);
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                var paiment = await db.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(continuationPaymentHelper.IdPayment!.ToUpper()));
                if(paiment == null){
                    
                    await Clients.Caller.SendAsync("Erreur", "aucun commande n'est ne correspond avec cette raison");
                    LogoutProject();
                    return;
                }

                 ActionPayObjectHelper actionPayObjectHelper = new()
                 {
                     continuationPaymentHelper = continuationPaymentHelper,
                     sellerCheckHelper = sellerIsExist
                 };
                 await ActionPay(actionPayObjectHelper, db);
                //await Clients.Caller.SendAsync("Erreur", "Désolé, nous devrons bloquée votre compte, Votre action ne respecte pas nos condition d'utilisation");

            }
            else
            {
                continuationPayments.Add(continuationPaymentHelper);
            }
        }

        public async Task VerifieBuyer(SellerCheckHelper sellerCheckHelper)
        {
            var contPay = TestForContinuation(sellerCheckHelper.Reason!);
            if (contPay != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                var paiment = await db.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(contPay.IdPayment!.ToUpper()));
                if (paiment == null)
                {
                    
                    string connectionIdCustom = "";
                    if (userConnected.TryGetValue(contPay.IdCustomer!.ToUpper(), out string result))
                    {
                        connectionIdCustom = result;
                    }
                    await Clients.Client(connectionIdCustom).SendAsync("PaymentError", "Une erreur, nous devons bloqué votre compte pour une durée indeterminer");
                    LogoutProject();
                }
                continuationPayments.Remove(contPay);
                sellecChecks.Remove(sellerCheckHelper);
                ActionPayObjectHelper actionPayObjectHelper = new()
                {
                    continuationPaymentHelper = contPay,
                    sellerCheckHelper = sellerCheckHelper
                };
                await ActionPay(actionPayObjectHelper, db);
                return;
            }
            else
            {
                sellecChecks.Add(sellerCheckHelper);
            }
        }
        public async Task ActionPay(ActionPayObjectHelper actionPayObjectHelper,DataContext db)
        {
            if (db?.Historical is null || db.User is null || db.Payment is null || db.HistoricalSms is null)
            {
                
                _logger.LogError("Le contexte de données est introuvable");
                await Clients.Caller.SendAsync("ServerError", new
                {
                    StatusCode = 500,
                    Message = "Erreur Serveur: Contexte de données introuvable"
                });
                LogoutProject();
                return;
            }
            
            //verify price
            if (actionPayObjectHelper == null ||actionPayObjectHelper.continuationPaymentHelper == null ||actionPayObjectHelper.sellerCheckHelper == null)
            {
                
                await Clients.Caller.SendAsync("Error", "un problème est survenu lors du paiment");
                LogoutProject();
                return;
            }

            if(actionPayObjectHelper!.sellerCheckHelper!.Price.SansVirgule() != actionPayObjectHelper.continuationPaymentHelper!.Price.SansVirgule())
            {
                
                string connectionIdCustom = "";
                if(userConnected.TryGetValue(actionPayObjectHelper.continuationPaymentHelper.IdCustomer!.ToUpper(), out string result))
                {
                    connectionIdCustom = result;
                    
                }
                await Clients.Client(connectionIdCustom).SendAsync("Erreur", "Désolé, nous devrons bloquée votre compte, Votre action ne respecte pas nos condition d'utilisation");
                var user = await db.User.FirstOrDefaultAsync(u => u.Id.ToString().ToUpper() == actionPayObjectHelper.continuationPaymentHelper.IdCustomer.ToUpper());
                if(user == null)
                {
                    await Clients.Client(connectionIdCustom).SendAsync("Erreur", "Une erreur est survenu lors de la bloquage du compte");
                }
                user.AccountOk = false;
                
                await db.SaveChangesAsync();
                _logger.LogWarning($"Prix ne correspondent pas - Réf: {actionPayObjectHelper.sellerCheckHelper.Reference}");
                LogoutProject();
                return;
            }
            
            var developer = await db.Developer.FirstOrDefaultAsync(d => d.Id.ToString().ToUpper().Equals(actionPayObjectHelper.sellerCheckHelper.IdDeveloper.ToUpper()));
            if (developer == null)
            {
                await Clients.Caller.SendAsync("Error", "Développeur introuvable");
                return; 
            }
            var seller_user = await db.User.FirstOrDefaultAsync(u => u.Id.ToString().ToUpper().Equals(developer.IdUser.ToUpper()));
            if (seller_user == null)
            {
                await Clients.Caller.SendAsync("Error", "Utilisateur vendeur introuvable");
                return; 
            }
            string name_developer = seller_user.FirstName + " " + seller_user.LastName;
            HistoricalModel newHistorical = new()
            {
                IdCustomer = actionPayObjectHelper.continuationPaymentHelper.IdCustomer,
                IdPayment = actionPayObjectHelper.continuationPaymentHelper.IdPayment,
                IdDeveloper = actionPayObjectHelper.sellerCheckHelper.IdDeveloper!.ToUpper(),
                Name_developer = name_developer,
                Reference = actionPayObjectHelper.sellerCheckHelper.Reference,
                Reason = actionPayObjectHelper.continuationPaymentHelper.Reason,
                Price = actionPayObjectHelper.sellerCheckHelper.Price,
                NumberDeveloper = actionPayObjectHelper.continuationPaymentHelper.Number,
                Created_at = DateTime.UtcNow
            };

            try
            {
                await db.Historical.AddAsync(newHistorical);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur HistoricalSms: {msg} | Inner: {inner}",
                    ex.Message, ex.InnerException?.Message);
                await Clients.Caller.SendAsync("Error", $"Erreur sauvegarde SMS: {ex.InnerException?.Message ?? ex.Message}");
                return;
            }
            var user_customer = await db.User.FirstOrDefaultAsync(u => u.Id.ToString().ToUpper().Equals(actionPayObjectHelper.continuationPaymentHelper.IdCustomer));
            if (user_customer == null)
            {
                await Clients.Caller.SendAsync("Error", "Utilisateur acheteur introuvable");
                return;
            }
            string name_custom = user_customer.FirstName + " " + user_customer.LastName;
            
            decimal result_balance = actionPayObjectHelper.sellerCheckHelper.SellerBalance ?? 0;
            // COMMENCE ICI l ERREUR
            
            HistoricalSmsModel newSms = new()
            {
                Id_developer = actionPayObjectHelper.sellerCheckHelper.IdDeveloper,
                Id_user = actionPayObjectHelper.continuationPaymentHelper.IdCustomer,
                Name_customer = name_custom,
                Id_payement = actionPayObjectHelper.continuationPaymentHelper.IdPayment!.ToUpper(),
                BuyerNumber = actionPayObjectHelper.sellerCheckHelper.BuyerNumber,
                BuyerName = actionPayObjectHelper.sellerCheckHelper.BuyerName,
                Reference = actionPayObjectHelper.sellerCheckHelper.Reference,
                Price = actionPayObjectHelper.sellerCheckHelper.Price,
                Reason = actionPayObjectHelper.sellerCheckHelper.Reason,
                Balance_seller = result_balance,
                Created_at = DateTime.UtcNow,
            };

            try
            {
                _logger.LogInformation("SMS - IdDeveloper: {v1}, IdUser: {v2}, IdPayement: {v3}, BuyerNumber: {v4}, BuyerName: {v5}, Reference: {v6}, Price: {v7}, Balance: {v8}, Reason: {v9}",
                    actionPayObjectHelper.sellerCheckHelper.IdDeveloper,
                    actionPayObjectHelper.continuationPaymentHelper.IdCustomer,
                    actionPayObjectHelper.continuationPaymentHelper.IdPayment,
                    actionPayObjectHelper.sellerCheckHelper.BuyerNumber,
                    actionPayObjectHelper.sellerCheckHelper.BuyerName,
                    actionPayObjectHelper.sellerCheckHelper.Reference,
                    actionPayObjectHelper.sellerCheckHelper.Price,
                    result_balance,
                    actionPayObjectHelper.continuationPaymentHelper.Reason
                );

                await db.HistoricalSms.AddAsync(newSms);
                await db.SaveChangesAsync();

                _logger.LogInformation("SMS sauvegardé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError("ERREUR SMS: {msg} | Inner: {inner} | Stack: {stack}",
                    ex.Message, ex.InnerException?.Message, ex.StackTrace);
                await Clients.Caller.SendAsync("Error", $"SMS Error: {ex.InnerException?.Message ?? ex.Message}");
                return;
            }
            // ENVOI DES BOOLÉENS TRUE À L'ENVOYEUR ET AU RECEVEUR

            // 1. Récupérer les ConnectionId
            string buyerConnectionId = "";
            string projectConnectionId = "";
            var userIdCustom = GetUserId().ToUpper();

            // ConnectionId de l'acheteur (envoyeur)
            if (userConnected.TryGetValue(actionPayObjectHelper.continuationPaymentHelper.IdCustomer!.ToUpper(), out string buyerId))
            {
                buyerConnectionId = buyerId;
            }

            // ConnectionId du projet (receveur)
            
            if (projectConnected.TryGetValue(actionPayObjectHelper.continuationPaymentHelper!.IdPayment.ToUpper().ToString().ToUpper(), out string payId))
            {
                projectConnectionId = payId;
            }

            // 2. Envoyer le booléen true aux deux
            if (!string.IsNullOrEmpty(buyerConnectionId))
            {
                await Clients.Client(buyerConnectionId).SendAsync("PaymentSuccess", true);
                // Ou avec plus d'infos:
                // await Clients.Client(buyerConnectionId).SendAsync("PaymentSuccess", new { Success = true, Reference = actionPayObjectHelper.sellerCheckHelper.Reference });
            }
            if (!string.IsNullOrEmpty(projectConnectionId))
            {
                await Clients.Client(projectConnectionId).SendAsync("PaymentSuccess", true);
            }
            await Clients.Caller.SendAsync("PaymentCompleted", true);

            _logger.LogInformation($"Paiement réussi - Réf: {actionPayObjectHelper.sellerCheckHelper.Reference}");
           // LogoutProject();
        }

        public SellerCheckHelper TestForSeller(string reason)
        {
            return sellecChecks.FirstOrDefault(s => s.Reason!.ToUpper().Equals(reason.ToUpper()))!;
        }
        public ContinuationPaymentHelper TestForContinuation(string reason)
        {
            return continuationPayments.FirstOrDefault(c => c.Reason!.ToUpper().Equals(reason.ToUpper()))!;
        }


        // Méthode pour récupérer l'userId depuis le Context du Hub
        private string GetUserId()
        {
            // Dans Hub, c'est Context.User, pas User tout seul
            var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(id))
                throw new HubException("Utilisateur non authentifié");

            return id;
        }
        private void LogoutProject()
        {
            Context.GetHttpContext().Response.Cookies.Delete("jwtApiKey", new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
        }
    }

}
