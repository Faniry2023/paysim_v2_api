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
            
        }
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong");
        }


        public async Task VerifiePaySeller(ContinuationPaymentHelper continuationPaymentHelper)
        {
            var sellerIsExist = TestForSeller(continuationPaymentHelper.Reason!);
            if (sellerIsExist != null)
            {
                continuationPayments.Remove(continuationPaymentHelper);
                sellecChecks.Remove(sellerIsExist);
                var paiment = await dataContext.Payment.FirstOrDefaultAsync(p => p.IdPayment.ToString().ToUpper().Equals(continuationPaymentHelper.IdPayment!.ToUpper()));
                if(paiment == null){
                    await Clients.Caller.SendAsync("Erreur", "aucun commande n'est ne correspond avec cette raison");
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
                return;
            }
            //verify price
            if (actionPayObjectHelper == null ||actionPayObjectHelper.continuationPaymentHelper == null ||actionPayObjectHelper.sellerCheckHelper == null)
            {
                await Clients.Caller.SendAsync("Error", "un problème est survenu lors du paiment");
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
            
            if (projectConnected.TryGetValue(actionPayObjectHelper.continuationPaymentHelper.IdProject!.ToUpper().ToString().ToUpper(), out string projectId))
            {
                projectConnectionId = projectId;
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
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Utilisateur non authentifié");

            return userId;
        }


        //############################################### T E S T    C O D E ###################################
        /*


        private static readonly Dictionary<string, string> _userConnections = new();
        private static readonly Dictionary<string, HashSet<string>> _groupMembers = new();
        private readonly ILogger<PayHub> _logger;

        public PayHub(ILogger<PayHub> logger)
        {
            _logger = logger;
        }

        // === CONNECTION MANAGEMENT ===


        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections[userId] = connectionId;
                _logger.LogInformation($"User {userId} connected with ID: {connectionId}");
            }

            // Envoyer la liste des utilisateurs connectés à tous
            await Clients.All.SendAsync("UsersOnline", _userConnections.Keys.ToList());
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.Remove(userId);

                // Retirer l'utilisateur de tous ses groupes
                var userGroups = _groupMembers.Keys.Where(g => _groupMembers[g].Contains(userId)).ToList();
                foreach (var group in userGroups)
                {
                    await LeaveGroup(group);
                }

                _logger.LogInformation($"User {userId} disconnected");
                await Clients.All.SendAsync("UsersOnline", _userConnections.Keys.ToList());
            }
            await base.OnDisconnectedAsync(exception);
        }

        // === 1. BROADCAST (TOUT LE MONDE) ===
        public async Task SendToAll(string message)
        {
            var userId = GetUserId();
            var userInfo = await GetUserInfo(userId);

            await Clients.All.SendAsync("ReceiveBroadcast", new
            {
                Id = Guid.NewGuid().ToString(),
                FromUserId = userId,
                FromUserName = userInfo.Name,
                Message = message,
                Timestamp = DateTime.Now,
                Type = "broadcast"
            });
        }

        // === 2. PRIVATE MESSAGE (1-TO-1) ===
        public async Task SendPrivateMessage(string targetUserId, string message)
        {
            var senderId = GetUserId();
            var senderInfo = await GetUserInfo(senderId);

            // Message pour le destinataire
            if (_userConnections.ContainsKey(targetUserId))
            {
                await Clients.Client(_userConnections[targetUserId]).SendAsync("ReceivePrivateMessage", new
                {
                    Id = Guid.NewGuid().ToString(),
                    FromUserId = senderId,
                    FromUserName = senderInfo.Name,
                    Message = message,
                    Timestamp = DateTime.Now,
                    Type = "private"
                });

                // Confirmation pour l'expéditeur
                await Clients.Caller.SendAsync("MessageSent", new
                {
                    ToUserId = targetUserId,
                    Message = message,
                    Timestamp = DateTime.Now,
                    Status = "delivered"
                });
            }
            else
            {
                await Clients.Caller.SendAsync("MessageError", new
                {
                    Error = "User is offline",
                    TargetUserId = targetUserId
                });
            }
        }

        // === 3. GROUP MANAGEMENT ===
        public async Task JoinGroup(string groupName)
        {
            var userId = GetUserId();
            var userInfo = await GetUserInfo(userId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            if (!_groupMembers.ContainsKey(groupName))
                _groupMembers[groupName] = new HashSet<string>();

            _groupMembers[groupName].Add(userId);

            // Notifier le groupe
            await Clients.Group(groupName).SendAsync("UserJoinedGroup", new
            {
                UserId = userId,
                UserName = userInfo.Name,
                GroupName = groupName,
                Timestamp = DateTime.Now
            });

            // Envoyer la liste des membres au nouveau membre
            var members = _groupMembers[groupName].Select(async id => await GetUserInfo(id)).Select(t => t.Result).ToList();
            await Clients.Caller.SendAsync("GroupMembers", members);

            _logger.LogInformation($"User {userId} joined group {groupName}");
        }

        public async Task LeaveGroup(string groupName)
        {
            var userId = GetUserId();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            if (_groupMembers.ContainsKey(groupName))
            {
                _groupMembers[groupName].Remove(userId);
                if (_groupMembers[groupName].Count == 0)
                    _groupMembers.Remove(groupName);
            }

            await Clients.Group(groupName).SendAsync("UserLeftGroup", new
            {
                UserId = userId,
                GroupName = groupName,
                Timestamp = DateTime.Now
            });
        }

        public async Task SendToGroup(string groupName, string message)
        {
            var userId = GetUserId();
            var userInfo = await GetUserInfo(userId);

            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", new
            {
                Id = Guid.NewGuid().ToString(),
                FromUserId = userId,
                FromUserName = userInfo.Name,
                GroupName = groupName,
                Message = message,
                Timestamp = DateTime.Now,
                Type = "group"
            });
        }

        // === 4. SPECIFIC CLIENTS (MULTI-CAST) ===
        public async Task SendToMultipleUsers(List<string> targetUserIds, string message)
        {
            var senderId = GetUserId();
            var senderInfo = await GetUserInfo(senderId);

            var connectedTargets = targetUserIds.Where(id => _userConnections.ContainsKey(id)).ToList();
            var clientIds = connectedTargets.Select(id => _userConnections[id]).ToList();

            if (clientIds.Any())
            {
                await Clients.Clients(clientIds).SendAsync("ReceiveMultiCast", new
                {
                    Id = Guid.NewGuid().ToString(),
                    FromUserId = senderId,
                    FromUserName = senderInfo.Name,
                    Message = message,
                    Timestamp = DateTime.Now,
                    Type = "multicast",
                    TargetUsers = connectedTargets
                });
            }
        }

        // === 5. TYPING INDICATOR ===
        public async Task SendTyping(string targetUserId, bool isTyping)
        {
            var senderId = GetUserId();
            var senderInfo = await GetUserInfo(senderId);

            if (_userConnections.ContainsKey(targetUserId))
            {
                await Clients.Client(_userConnections[targetUserId]).SendAsync("UserTyping", new
                {
                    UserId = senderId,
                    UserName = senderInfo.Name,
                    IsTyping = isTyping,
                    Timestamp = DateTime.Now
                });
            }
        }

        // === HELPER METHODS ===
        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Context.User?.FindFirst("sub")?.Value
                   ?? Context.ConnectionId;
        }

        private async Task<UserInfo> GetUserInfo(string userId)
        {
            // Récupérer depuis votre DB
            using var scope = Context.GetHttpContext()?.RequestServices.CreateScope();
            var db = scope?.ServiceProvider.GetRequiredService<DataContext>();
            var user = await db.Users.FindAsync(int.Parse(userId));

            return new UserInfo
            {
                Id = userId,
                Name = user?.Username ?? userId,
                Email = user?.Email ?? "",
                Avatar = user?.Avatar ?? ""
            };
        }
    }
    public class UserInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
    }



    */
    }

}
