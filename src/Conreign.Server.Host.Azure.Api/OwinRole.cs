﻿using System;
using System.Net;
using System.Threading;
using Conreign.Server.Api;
using Conreign.Server.Api.Configuration;
using Microsoft.Azure;
using Microsoft.Owin.Hosting;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;

namespace Conreign.Server.Host.Azure.Api
{
    public class OwinRole : RoleEntryPoint
    {
        private readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private IDisposable _app;

        public static ConreignApi Api { get; private set; }

        public override void Run()
        {
            _exitEvent.WaitOne();
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 20;
            var env = CloudConfigurationManager.GetSetting("Environment");
            if (string.IsNullOrEmpty(env))
            {
                throw new InvalidOperationException("Unable to determine environment.");
            }
            var config = ConreignApiConfiguration.Load(Environment.CurrentDirectory, env);
            var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["PublicApi"];
            Api = ConreignApi.Create(config);
            Log.Logger = Api.Logger;
            // Starting appplication
            var baseUri = $"{endpoint.Protocol}://{endpoint.IPEndpoint}";
            _app = WebApp.Start<Startup>(baseUri);
            Log.Logger.Information("Conreign API ({Environment}) is started at {BaseUri}",
                Api.Configuration.Environment, baseUri);
            return base.OnStart();
        }

        public override void OnStop()
        {
            _app?.Dispose();
            base.OnStop();
        }
    }
}