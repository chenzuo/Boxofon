﻿using System;
using System.Linq;
using System.Web.Configuration;
using Boxofon.Web.Mailgun;
using Boxofon.Web.Membership;
using Boxofon.Web.Security;
using Boxofon.Web.Twilio;
using NLog;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.Session;
using Nancy.TinyIoc;
using SimpleAuthentication.Core;
using SimpleAuthentication.Core.Providers;
using TinyMessenger;

namespace Boxofon.Web
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override DiagnosticsConfiguration DiagnosticsConfiguration
        {
            get { return new DiagnosticsConfiguration { Password = WebConfigurationManager.AppSettings["nancy:DiagnosticsPassword"] }; }
        }
        
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            CookieBasedSessions.Enable(pipelines);
            Nancy.Security.Csrf.Enable(pipelines);

            pipelines.OnError += (ctx, ex) =>
            {
                Logger.ErrorException(ex.Message, ex);
                return null;
            };

            var formsAuthConfiguration = new Nancy.Authentication.Forms.FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/account/signin",
                RequiresSSL = true,
                UserMapper = container.Resolve<IUserMapper>()
                // TODO Configure cryptography!
            };
            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
            
            pipelines.AfterRequest.AddItemToEndOfPipeline(context =>
            {
                if (context.Items.ContainsKey(SecurityHooks.ForceHttpStatusCodeKey))
                {
                    context.Response = new Response { StatusCode = (HttpStatusCode)context.Items[SecurityHooks.ForceHttpStatusCodeKey] };
                }
            });
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            var twilioAccountLookup = new AzureStorageTwilioAccountLookup(container.Resolve<ITinyMessengerHub>());
            twilioAccountLookup.Initialize();
            container.Register<ITwilioAccountLookup>(twilioAccountLookup);

            var externalIdentityLookup = new AzureStorageExternalIdentityLookup(container.Resolve<ITinyMessengerHub>());
            externalIdentityLookup.Initialize();
            container.Register<IExternalIdentityLookup>(externalIdentityLookup);

            var userRepository = new AzureStorageUserRepository();
            userRepository.Initialize();
            container.Register<IUserRepository>(userRepository);

            var phoneNumberVerificationService = new PhoneNumberVerificationService(container.Resolve<ITwilioClientFactory>());
            phoneNumberVerificationService.Initialize();
            container.Register<IPhoneNumberVerificationService>(phoneNumberVerificationService);

            var emailVerificationService = new EmaillVerificationService(container.Resolve<IMailgunClient>());
            emailVerificationService.Initialize();
            container.Register<IEmailVerificationService>(emailVerificationService);
        }
    }
}