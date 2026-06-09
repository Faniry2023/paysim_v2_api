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
        private readonly DataContext dataContext;

        public PayHub(ILogger<PayHub> logger, DataContext dataContext)
        {
            _logger = logger;
            this.dataContext = dataContext;
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
                var payment = await dataContext.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(id));
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

        //Coonexion v1
        /*
        public override async Task OnConnectedAsync()
        {
            var type = Context.GetHttpContext()?.Request.Query["type"].ToString();
            var id = Context.GetHttpContext()!.Request.Query["projectId"].ToString().ToUpper();
            if(type == "project")
            {
                var project = await dataContext.Project.FirstOrDefaultAsync(p => p.Id.ToString().ToUpper().Equals(id));
                if(project == null)
                {
                    await Clients.Caller.SendAsync("Error", "problème de connexion entre le site et PaySim");
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
            
        }*/
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
                var paiment = await dataContext.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(continuationPaymentHelper.IdPayment!.ToUpper()));
                if(paiment == null){
                    
                    await Clients.Caller.SendAsync("Erreur", "aucun commande n'est ne correspond avec cette raison");
                    LogoutProject();
                }

                 ActionPayObjectHelper actionPayObjectHelper = new()
                 {
                     continuationPaymentHelper = continuationPaymentHelper,
                     sellerCheckHelper = sellerIsExist
                 };
                 await ActionPay(actionPayObjectHelper);
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
                var paiment = await dataContext.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(contPay.IdPayment!.ToUpper()));
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
                await ActionPay(actionPayObjectHelper);
            }
            else
            {
                sellecChecks.Add(sellerCheckHelper);
            }
        }
        public async Task ActionPay(ActionPayObjectHelper actionPayObjectHelper)
        {
            if (dataContext?.Historical is null || dataContext.User is null || dataContext.Payment is null || dataContext.HistoricalSms is null)
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
            }

            if(actionPayObjectHelper!.sellerCheckHelper!.Price.SansVirgule() != actionPayObjectHelper.continuationPaymentHelper!.Price.SansVirgule())
            {
                
                string connectionIdCustom = "";
                if(userConnected.TryGetValue(actionPayObjectHelper.continuationPaymentHelper.IdCustomer!.ToUpper(), out string result))
                {
                    connectionIdCustom = result;
                    
                }
                await Clients.Client(connectionIdCustom).SendAsync("Erreur", "Désolé, nous devrons bloquée votre compte, Votre action ne respecte pas nos condition d'utilisation");
                var user = await dataContext.User.FirstOrDefaultAsync(u => u.Id.ToString().ToUpper() == actionPayObjectHelper.continuationPaymentHelper.IdCustomer.ToUpper());
                if(user == null)
                {
                    await Clients.Client(connectionIdCustom).SendAsync("Erreur", "Une erreur est survenu lors de la bloquage du compte");
                }
                user.AccountOk = false;

                await dataContext.SaveChangesAsync();
                _logger.LogWarning($"Prix ne correspondent pas - Réf: {actionPayObjectHelper.sellerCheckHelper.Reference}");
                LogoutProject();
            }
            HistoricalModel newHistorical = new()
            {
                IdCustomer = actionPayObjectHelper.continuationPaymentHelper.IdCustomer,
                IdPayment = actionPayObjectHelper.continuationPaymentHelper.IdPayment,
                ActionKey = actionPayObjectHelper.continuationPaymentHelper.ActionKey,
                IdDeveloper = actionPayObjectHelper.sellerCheckHelper.IdDeveloper!.ToUpper(),
                Reference = actionPayObjectHelper.sellerCheckHelper.Reference,
                Reason = actionPayObjectHelper.continuationPaymentHelper.Reason,
                Price = actionPayObjectHelper.sellerCheckHelper.Price,
                NumberDeveloper = actionPayObjectHelper.continuationPaymentHelper.Number,
                NumberCustomer = actionPayObjectHelper.sellerCheckHelper.BuyerNumber
            };

            await dataContext.Historical.AddAsync(newHistorical);

            HistoricalSmsModel newSms = new()
            {
                Id_payement = actionPayObjectHelper.continuationPaymentHelper.IdPayment!.ToUpper(),
                BuyerNumber = actionPayObjectHelper.sellerCheckHelper.BuyerNumber,
                BuyerName = actionPayObjectHelper.sellerCheckHelper.BuyerName,
                Reference = actionPayObjectHelper.sellerCheckHelper.Reference,
                Price = actionPayObjectHelper.sellerCheckHelper.Price
            };

            await dataContext.HistoricalSms.AddAsync(newSms);

            await dataContext.SaveChangesAsync();
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
