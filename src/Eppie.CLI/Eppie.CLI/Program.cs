﻿// ---------------------------------------------------------------------------- //
//                                                                              //
//   Copyright 2023 Eppie(https://eppie.io)                                     //
//                                                                              //
//   Licensed under the Apache License, Version 2.0 (the "License"),            //
//   you may not use this file except in compliance with the License.           //
//   You may obtain a copy of the License at                                    //
//                                                                              //
//       http://www.apache.org/licenses/LICENSE-2.0                             //
//                                                                              //
//   Unless required by applicable law or agreed to in writing, software        //
//   distributed under the License is distributed on an "AS IS" BASIS,          //
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.   //
//   See the License for the specific language governing permissions and        //
//   limitations under the License.                                             //
//                                                                              //
// ---------------------------------------------------------------------------- //

using ComponentBuilder;
using Eppie.CLI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Globalization;
using Tuvi.Core;

namespace Eppie.CLI
{
    internal sealed class Program
    {
        public static IHost Host { get; private set; } = null!;

        public static ProgramConfiguration Configuration => Host.Services.GetRequiredService<ProgramConfiguration>();
        public static IHostEnvironment Environment => Host.Services.GetRequiredService<IHostEnvironment>();
        public static ResourceLoader ResourceLoader => Host.Services.GetRequiredService<ResourceLoader>();

        static async Task Main(string[] args)
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args).UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddLocalization()
                            .AddSingleton<ResourceLoader>()
                            .AddTransient<ProgramConfiguration>()
                            .AddHostedService<Application>();

                })
                .UseSerilog((context, configuration) =>
                {
                    configuration.ReadFrom.Configuration(context.Configuration);
                })
                .UseConsoleLifetime(options => options.SuppressStatusMessages = true)
                .Build();

            Log.Debug("====================================================================");
            InitializeConsole();

            _ = Components.CreateTuviMailCore("data.db", new ImplementationDetailsProvider("Eppie seed", "Eppie.Package", "backup@system.service.eppie.io"));

            await Host.RunAsync().ConfigureAwait(false);

            Log.CloseAndFlush();
        }

        static void InitializeConsole()
        {
            Console.OutputEncoding = Configuration.ConsoleEncoding;
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = Configuration.ConsoleCultureInfo;
            Log.Debug("OutputEncoding is {OutputEncoding}; CurrentCulture is {CurrentCulture}", Console.OutputEncoding, CultureInfo.CurrentCulture);

            Console.Title = ResourceLoader.AssemblyStrings.Title;
        }
    }
}
