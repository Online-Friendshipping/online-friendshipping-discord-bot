﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using static SpeedDatingBot.Helpers;

namespace SpeedDatingBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private Program()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(new DatingSession())
                .BuildServiceProvider();
        }

        private async Task MainAsync()
        {
            _client.Log += LogAsync;
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _client.LoginAsync(TokenType.Bot, Env("TOKEN"));
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage msg)
        {
            await Console.Out.WriteLineAsync(msg.ToString());
            if (msg.Exception != null)
            {
                await Console.Error.WriteLineAsync(msg.Exception?.Message);
                await Console.Error.WriteLineAsync(msg.Exception?.StackTrace ?? "");
            }
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message)) return;
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot)
                return;

            await _commands.ExecuteAsync(new SocketCommandContext(_client, message), argPos, _services);
        }
    }
}