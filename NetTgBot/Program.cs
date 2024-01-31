﻿using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WebSocket4Net;

public class Program
{
    private WebSocket? _socket;

    public static async Task Main(params string[] args)
    {
        if (args.Length < 2)
        {
            await Console.Out.WriteLineAsync("you must enter two params (botId, wws url)");
            return;
        }

        await new Program().Init(args[0], args[1]);
    }

    public async Task Init(string botId, string url)
    {
        ITelegramBotClient botClient = new TelegramBotClient(botId);

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
            },

            ThrowPendingUpdates = true,
        };

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cts.Token);

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"{me.FirstName} was started!");

        using (_socket = new WebSocket($"ws://{url}:8080"))
        {
            _socket.Opened += (sender, e) => Console.WriteLine("Connect");

           /* ws.MessageReceived += (sender, e) =>
                              Console.WriteLine("Laputa says: " + e.Message);*/

            _socket.Open();
            Console.ReadKey(true);
        }

        await Task.Delay(-1);
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            //var chatId = message.Chat.Id;
            //var messageTextRecive = message.Text;
            //Message sentMessage = await botClient.SendTextMessageAsync(chatId, text: "You said:\n" + messageText, cancellationToken: cancellationToken);
            // Message sentMessage = await botClient.SendTextMessageAsync(chatId, text: "You said:\n" + messageText);
            Console.WriteLine(message.Text);

            string req = CreateRequest(message);
            _socket?.Send(req);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static string CreateRequest(Message message)
    {
        var date = message.Date.ToLocalTime();

        return $"{{\"payload\":" +
            $"{{\"payload\":{{\"text\":\"{message.Text}\", " +
            $"\"date\":{{" +
            $"\"hour\":{date.Hour}, " +
            $"\"minutes\":{date.Minute}," +
            $"\"seconds\":{date.Second}}}}}}}}}";
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}