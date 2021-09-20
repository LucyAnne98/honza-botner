﻿using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HonzaBotner.Discord.EventHandler;
using HonzaBotner.Discord.Services.Options;
using HonzaBotner.Services.Contract;
using HonzaBotner.Services.Contract.Dto;
using Microsoft.Extensions.Options;

namespace HonzaBotner.Discord.Services.EventHandlers
{
    public class VerificationEventHandler : IEventHandler<ComponentInteractionCreateEventArgs>
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IUrlProvider _urlProvider;
        private readonly ButtonOptions _buttonOptions;

        public VerificationEventHandler(IAuthorizationService authorizationService, IUrlProvider urlProvider, IOptions<ButtonOptions> options)
        {
            _authorizationService = authorizationService;
            _urlProvider = urlProvider;
            _buttonOptions = options.Value;
        }

        public async Task<EventHandlerResult> Handle(ComponentInteractionCreateEventArgs eventArgs)
        {
            // https://discordapp.com/channels/366970031445377024/507515506073403402/686745124885364770

            if (eventArgs.Id != _buttonOptions.VerificationId) return EventHandlerResult.Continue;

            DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder().AsEphemeral(true);

            string link = _urlProvider.GetAuthLink(eventArgs.User.Id, RolesPool.Auth);

            if (await _authorizationService.IsUserVerified(eventArgs.User.Id))
            {
                builder.Content = "Ahoj, už jsi ověřený.\nChceš aktualizovat role dle UserMap?";
                builder.AddComponents(new DiscordLinkButtonComponent(link, "Aktualizovat role", false,
                    new DiscordComponentEmoji("🔄")));
            }
            else
            {
                builder.Content = "Ahoj, pro ověření a přidělení rolí dle UserMap pokračuj na odkaz";
                builder.AddComponents(new DiscordLinkButtonComponent(link, "Ověřit se!", false,
                    new DiscordComponentEmoji("✅")));
            }

            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);

            return EventHandlerResult.Stop;
        }
    }
}
