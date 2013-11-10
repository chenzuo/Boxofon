﻿using System;
using System.Linq;
using System.Web.Configuration;
using Boxofon.Web.Helpers;
using Boxofon.Web.Membership;
using Boxofon.Web.Messages;
using Boxofon.Web.ViewModels;
using Nancy;
using Nancy.Security;
using TinyMessenger;
using Twilio;

namespace Boxofon.Web.Modules
{
    public class AccountModule : WebsiteBaseModule
    {
        private readonly ITinyMessengerHub _hub;
        private readonly IUserRepository _userRepository;

        public AccountModule(ITinyMessengerHub hub, IUserRepository userRepository) : base("/account")
        {
            if (hub == null)
            {
                throw new ArgumentNullException("hub");
            }
            _hub = hub;
            if (userRepository == null)
            {
                throw new ArgumentNullException("userRepository");
            }
            _userRepository = userRepository;

            this.RequiresAuthentication();

            Get["/"] = parameters =>
            {
                var user = this.GetCurrentUser();
                var viewModel = new ViewModels.AccountOverview
                {
                    IsTwilioAccountConnected = !string.IsNullOrEmpty(user.TwilioAccountSid),
                    TwilioConnectAuthorizationUrl = string.Format("https://www.twilio.com/authorize/{0}", WebConfigurationManager.AppSettings["twilio:ConnectAppSid"]),
                    BoxofonNumber = user.TwilioPhoneNumber,
                    PrivatePhoneNumber = user.PrivatePhoneNumber,
                    Email = user.Email
                };
                return View["Overview.cshtml", viewModel];
            };

            Get["/twilio/connect/authorize"] = parameters =>
            {
                var twilioAccountSid = Request.Query["AccountSid"];
                if (Request.Query["error"] == "unauthorized_client" || string.IsNullOrEmpty(twilioAccountSid))
                {
                    Request.AddAlertMessage("info", "Anslutning till Twilio-konto avbröts eller misslyckades.");
                    return Response.AsRedirect("/account");
                }

                var user = this.GetCurrentUser();
                if (!string.IsNullOrEmpty(user.TwilioAccountSid))
                {
                    Request.AddAlertMessage("error", "Du har redan anslutit ett Twilio-konto.");
                    return Response.AsRedirect("/account");
                }

                user.TwilioAccountSid = twilioAccountSid;
                _userRepository.Save(user);
                _hub.PublishAsync(new LinkedTwilioAccountToUser
                {
                    TwilioAccountSid = twilioAccountSid,
                    UserId = user.Id
                });
                Request.AddAlertMessage("success", "Twilio-kontot har anslutits.");
                return Response.AsRedirect("/account");
            };

            Get["/identities"] = parameters =>
            {
                var user = this.GetCurrentUser();
                var viewModel = new ViewModels.ExternalIdentities
                {
                    GoogleIdLinked = user.ExternalIdentities.Any(id => id.ProviderName == "google"),
                    TwitterIdLinked = user.ExternalIdentities.Any(id => id.ProviderName == "twitter"),
                    FacebookIdLinked = user.ExternalIdentities.Any(id => id.ProviderName == "facebook"),
                    WindowsLiveIdLinked = user.ExternalIdentities.Any(id => id.ProviderName == "windowslive")
                };
                return View["Identities.cshtml", viewModel];
            };
        }
    }
}