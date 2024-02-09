﻿// ---------------------------------------------------------------------------- //
//                                                                              //
//   Copyright 2024 Eppie (https://eppie.io)                                    //
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

using System.Diagnostics.CodeAnalysis;

using Eppie.CLI.Entities;
using Eppie.CLI.Services;
using Eppie.CLI.Tools;

using Finebits.Authorization.OAuth2.Abstractions;
using Finebits.Authorization.OAuth2.Types;

using Microsoft.Extensions.Logging;

using Tuvi;
using Tuvi.Core;
using Tuvi.Core.Entities;

namespace Eppie.CLI.Menu
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Class is instantiated via dependency injection")]
    internal class Actions(
        ILogger<Actions> logger,
        Application application,
        AuthorizationProvider authProvider,
        CoreProvider coreProvider)
    {
        private readonly ILogger<Actions> _logger = logger;
        private readonly Application _application = application;
        private readonly CoreProvider _coreProvider = coreProvider;
        private readonly AuthorizationProvider _authProvider = authProvider;

        internal void ExitAction()
        {
            _logger.LogMethodCall();
            _application.StopApplication();
        }

        internal async Task InitActionAsync()
        {
            _logger.LogMethodCall();

            bool isFirstTime = await _coreProvider.TuviMailCore.IsFirstApplicationStartAsync().ConfigureAwait(false);

            if (!isFirstTime)
            {
                _application.WriteSecondInitializationWarning();
                return;
            }

            ISecurityManager sm = _coreProvider.TuviMailCore.GetSecurityManager();
            string[] seedPhrase = await sm.CreateSeedPhraseAsync().ConfigureAwait(false);
            string password = _application.AskNewPassword();

            if (password.Length == 0 || password != _application.ConfirmPassword())
            {
                _application.WriteInvalidPasswordWarning();
                return;
            }

            bool success = await _coreProvider.TuviMailCore.InitializeApplicationAsync(password).ConfigureAwait(false);

            if (success)
            {
                _application.WriteApplicationInitializationMessage(seedPhrase);
            }
            else
            {
                _application.WriteImpossibleInitializationError();
            }
        }

        internal async Task ResetActionAsync()
        {
            _logger.LogMethodCall();

            if (_application.ConfirmReset())
            {
                await _coreProvider.ResetAsync().ConfigureAwait(false);
                _application.WriteApplicationResetMessage();
            }
        }

        internal async Task OpenActionAsync()
        {
            _logger.LogMethodCall();

            bool isFirstTime = await _coreProvider.TuviMailCore.IsFirstApplicationStartAsync().ConfigureAwait(false);

            if (isFirstTime)
            {
                _application.WriteUninitializedAppWarning();
                return;
            }

            bool success = await _coreProvider.TuviMailCore.InitializeApplicationAsync(_application.AskPassword()).ConfigureAwait(false);

            if (success)
            {
                _application.WriteApplicationOpenedMessage();
            }
            else
            {
                _application.WriteInvalidPasswordWarning();
            }
        }

        internal async Task ListAccountsActionAsync()
        {
            _logger.LogMethodCall();

            List<Account> accounts = await _coreProvider.TuviMailCore.GetAccountsAsync().ConfigureAwait(true);
            accounts.Sort((a, b) => a.Id.CompareTo(b.Id));

            _application.PrintAccounts(accounts);
        }

        internal Task AddAccountActionAsync(MenuCommand.CommandAddAccountOptions.AccountType type)
        {
            _logger.LogMethodCall();

            return type switch
            {
                MenuCommand.CommandAddAccountOptions.AccountType.Email => AddEmailAccountAsync(),
                MenuCommand.CommandAddAccountOptions.AccountType.Dec => AddDecAccountAsync(),
                MenuCommand.CommandAddAccountOptions.AccountType.Proton => AddProtonAccountAsync(),
                _ => throw new ArgumentException($"Account type '{type}' is not supported.", nameof(type)),
            };
        }

        internal async Task RestoreActionAsync()
        {
            _logger.LogMethodCall();

            bool isFirstTime = await _coreProvider.TuviMailCore.IsFirstApplicationStartAsync().ConfigureAwait(false);

            if (!isFirstTime)
            {
                if (!_application.ConfirmReset())
                {
                    return;
                }

                await _coreProvider.ResetAsync().ConfigureAwait(false);
            }

            string[] seedPhrase = _application.AskSeedPhrase().Split(new char[] { ' ', ';' });
            string password = _application.AskNewPassword();
            if (password.Length == 0 || password != _application.ConfirmPassword())
            {
                _application.WriteInvalidPasswordWarning();
                return;
            }

            await _coreProvider.TuviMailCore.GetSecurityManager().RestoreSeedPhraseAsync(seedPhrase).ConfigureAwait(false);
            bool success = await _coreProvider.TuviMailCore.InitializeApplicationAsync(password).ConfigureAwait(false);

            if (success)
            {
                //ToDo: temporary solution
                Uri restoreUri = new(_application.AskRestorePath());

                await _coreProvider.TuviMailCore.RestoreFromBackupIfNeededAsync(restoreUri).ConfigureAwait(false);
                _application.WriteSuccessfulRestoredMessage();
            }
            else
            {
                _application.WriteImpossibleInitializationError();
            }
        }

        internal async Task SendActionAsync(string sender, string receiver, string subject)
        {
            _logger.LogMethodCall();

            EmailAddress senderAddress = new(sender);
            EmailAddress receiverAddress = new(receiver);

            IAccountService accountService = await _coreProvider.TuviMailCore.GetAccountServiceAsync(senderAddress).ConfigureAwait(false);

            string body = _application.AskMessageBody();

            Message message = new()
            {
                Subject = subject,
                TextBody = body,
            };
            message.From.Add(senderAddress);
            message.To.Add(receiverAddress);

            await accountService.SendMessageAsync(message, false, false).ConfigureAwait(false);
        }

        internal async Task ListContactsActionAsync(int pageSize)
        {
            _logger.LogMethodCall();

            List<Contact> contacts = (await _coreProvider.TuviMailCore.GetContactsAsync().ConfigureAwait(true)).ToList();
            _application.PrintContacts(pageSize, contacts);
        }

        internal async Task ShowMessageActionAsync(string address, string folderName, uint id, int pk)
        {
            _logger.LogMethodCall();

            EmailAddress accountEmail = new(address);
            Account account = await _coreProvider.TuviMailCore.GetAccountAsync(accountEmail).ConfigureAwait(true);
            Folder? folder = account.FoldersStructure.FirstOrDefault(x => x.HasSameName(folderName));

            if (folder is null)
            {
                _application.WriteUnknownFolderWarning(address, folderName);
                return;
            }

            Message msg = new()
            {
                Pk = pk,
                Id = id,
                Folder = folder,
                FolderId = folder.Id
            };

            IAccountService accountService = await _coreProvider.TuviMailCore.GetAccountServiceAsync(accountEmail).ConfigureAwait(true);
            Message message = await accountService.GetMessageBodyAsync(msg).ConfigureAwait(true);
            _application.PrintMessage(message, false);
        }

        internal async Task ShowAllMessagesActionAsync(int pageSize)
        {
            _logger.LogMethodCall();

            await _application.PrintAllMessagesAsync(pageSize, (count, lastMsg) => GetMessages(count, lastMsg, _coreProvider.TuviMailCore)).ConfigureAwait(false);

            static async Task<IEnumerable<Message>> GetMessages(int count, Message lastMessage, ITuviMail tuviMail)
            {
                return await tuviMail.GetAllEarlierMessagesAsync(count, lastMessage).ConfigureAwait(false);
            }
        }

        internal async Task ShowFolderMessagesActionAsync(string accountAddress, string folderName, int pageSize)
        {
            _logger.LogMethodCall();

            EmailAddress email = new(accountAddress);
            Account account = await _coreProvider.TuviMailCore.GetAccountAsync(email).ConfigureAwait(false);
            Folder? folder = account.FoldersStructure.Find(x => x.HasSameName(folderName));

            if (folder is null)
            {
                _application.WriteUnknownFolderWarning(accountAddress, folderName);
                return;
            }

            await _application.PrintFolderMessagesAsync(accountAddress, folderName, pageSize, (count, lastMsg) => GetMessages(count, lastMsg, folder, _coreProvider.TuviMailCore)).ConfigureAwait(false);

            static async Task<IEnumerable<Message>> GetMessages(int count, Message lastMessage, Folder folder, ITuviMail tuviMail)
            {
                return await tuviMail.GetFolderEarlierMessagesAsync(folder, count, lastMessage).ConfigureAwait(false);
            }
        }

        internal async Task ShowContactMessagesActionAsync(string contactAddress, int pageSize)
        {
            _logger.LogMethodCall();

            EmailAddress contact = new(contactAddress);

            await _application.PrintContactMessagesAsync(contactAddress, pageSize, (count, lastMsg) => GetMessages(count, lastMsg, contact, _coreProvider.TuviMailCore)).ConfigureAwait(false);

            static async Task<IEnumerable<Message>> GetMessages(int count, Message lastMessage, EmailAddress email, ITuviMail tuviMail)
            {
                return await tuviMail.GetContactEarlierMessagesAsync(email, count, lastMessage).ConfigureAwait(false);
            }
        }

        internal async Task ImportKeyBundleFromFileAsync(FileInfo file)
        {
            _logger.LogMethodCall();
            await Task.Run(() => ImportBundle(file.FullName)).ConfigureAwait(false);
        }

        private void ImportBundle(string fileAddress)
        {
            using MemoryStream keyIn = new(File.ReadAllBytes(fileAddress));
            _coreProvider.TuviMailCore.GetSecurityManager().ImportPgpKeyRingBundle(keyIn);
        }

        private async Task AddEmailAccountAsync()
        {
            _logger.LogMethodCall();

            async Task<Account> CreateAccountAsync()
            {
                MailService mailService = _application.SelectOption(MailService.Other, true);

                return mailService == MailService.Other ? CreateDefaultAccount()
                                                        : await CreateOAuth2AccountAsync(mailService).ConfigureAwait(false);
            }

            try
            {
                Account account = await CreateAccountAsync().ConfigureAwait(false);
                await _coreProvider.TuviMailCore.AddAccountAsync(account).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //ToDo: Move string to resources
#pragma warning disable CA1303 // Retrieve the following string(s) from a resource table.
                Console.WriteLine("Authorization operation has been canceled.");
#pragma warning restore CA1303 // Do not catch general exception types
            }
        }

        private Account CreateDefaultAccount()
        {
            Account account = Account.Default;
            account.Email = new EmailAddress(_application.AskAccountAddress());

            BasicAuthData basicData = new()
            {
                Password = _application.AskAccountPassword()
            };

            account.AuthData = basicData;

            account.IncomingServerAddress = _application.AskIMAPServer();
            account.OutgoingServerAddress = _application.AskSMTPServer();

            return account;
        }

        private async Task<Account> CreateOAuth2AccountAsync(MailService mailService)
        {
            using CancellationTokenSource cancellationLogin = new();

            void CancelLogin(object? sender, ConsoleCancelEventArgs e)
            {
                cancellationLogin.Cancel();
            }

            try
            {
                Console.CancelKeyPress += CancelLogin;

                IAuthorizationClient authClient = _authProvider.GetAuthorizationClient(mailService);

                //ToDo: Move string to resources
                Console.WriteLine($"Authorization to {mailService} service. Press Ctrl+C to cancel the operation.");
                Token token = await authClient.LoginAsync(cancellationLogin.Token).ConfigureAwait(false);

                if (authClient is IProfileReader profileReader)
                {
                    IUserProfile profile = await profileReader.ReadProfileAsync(token).ConfigureAwait(false);
                    //ToDo: Move string to resources
                    Console.WriteLine($"Authorization of account '{profile.Email}' completed successfully.");

                    Account account = Account.Default;
                    account.Email = new EmailAddress(profile.Email);

                    OAuth2Data oauthData = new()
                    {
                        RefreshToken = token.RefreshToken,
                        AuthAssistantId = mailService.ToString()
                    };
                    account.AuthData = oauthData;

                    account.IncomingServerAddress = _application.AskIMAPServer();
                    account.OutgoingServerAddress = _application.AskSMTPServer();

                    return account;
                }
                else
                {
                    throw new InvalidOperationException("User data cannot be read");
                }
            }
            finally
            {
                Console.CancelKeyPress -= CancelLogin;
            }
        }

        private async Task AddProtonAccountAsync()
        {
            _logger.LogMethodCall();

            //ToDo: This old code. ProtonAuthData must be use.

            Account account = new()
            {
                Email = new EmailAddress(_application.AskAccountAddress())
            };

            BasicAuthData basicData = new()
            {
                Password = _application.AskAccountPassword()
            };

            account.AuthData = basicData;
            account.Type = (int)MailBoxType.Proton;

            await _coreProvider.TuviMailCore.AddAccountAsync(account).ConfigureAwait(false);
        }

        private async Task AddDecAccountAsync()
        {
            _logger.LogMethodCall();

            Account account = await _coreProvider.TuviMailCore.NewDecentralizedAccountAsync().ConfigureAwait(false);
            account.Type = (int)MailBoxType.Dec;
            await _coreProvider.TuviMailCore.AddAccountAsync(account).ConfigureAwait(false);
        }
    }
}
