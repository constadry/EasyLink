using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Web;
using EasyLink.Models;
using Microsoft.Extensions.Options;

namespace EasyLink.Services;

/// <summary>
/// Сервис для отправки сообщений в Telegram
/// </summary>
public interface ITelegramService
{
    Task<TelegramResponse> SendFeedbackAsync(FeedbackRequest request);
    Task<TelegramResponse> SendTeamApplicationAsync(TeamApplicationRequest request);
}

public class TelegramService : ITelegramService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramService> _logger;
    private const string TelegramApiBaseUrl = "https://api.telegram.org/bot";

    public TelegramService(
        HttpClient httpClient,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TelegramResponse> SendFeedbackAsync(FeedbackRequest request)
    {
        var message = $"""
            🎮 <b>Новое обращение с сайта ArcWeave</b>

            👤 <b>Игровой ник:</b> {EscapeHtml(request.PlayerNick)}
            📋 <b>Причина:</b> {EscapeHtml(request.Reason)}
            💬 <b>Связь:</b> {EscapeHtml(request.ContactMethod)} - {EscapeHtml(request.ContactInfo)}

            📝 <b>Сообщение:</b>
            {EscapeHtml(request.Message)}
            """;

        return await SendMessageAsync(message);
    }

    public async Task<TelegramResponse> SendTeamApplicationAsync(TeamApplicationRequest request)
    {
        var message = $"""
            📝 <b>НОВАЯ ЗАЯВКА В КОМАНДУ</b>

            👤 <b>Ник:</b> {EscapeHtml(request.PlayerNick)}
            🌐 <b>Сервер:</b> {EscapeHtml(request.Server)}
            🛠 <b>Роль:</b> {EscapeHtml(request.Role)}
            ⏳ <b>Часы:</b> {EscapeHtml(request.Hours)}
            🚫 <b>История наказаний:</b>
            {EscapeHtml(request.History)}

            📱 <b>Discord:</b> {EscapeHtml(request.Discord)}

            🎯 <b>Причина/Мотивация:</b>
            {EscapeHtml(request.Reason)}
            """;

        return await SendMessageAsync(message);
    }

    private async Task<TelegramResponse> SendMessageAsync(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.BotToken) || string.IsNullOrEmpty(_settings.ChatId))
            {
                _logger.LogError("Telegram settings are not configured properly");
                return new TelegramResponse 
                { 
                    Success = false, 
                    Message = "Настройки Telegram не сконфигурированы" 
                };
            }

            var url = $"{TelegramApiBaseUrl}{_settings.BotToken}/sendMessage";
            
            var payload = new
            {
                chat_id = _settings.ChatId,
                text = message,
                parse_mode = "HTML"
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent to Telegram successfully");
                return new TelegramResponse 
                { 
                    Success = true, 
                    Message = "Сообщение успешно отправлено" 
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Telegram API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                
                return new TelegramResponse 
                { 
                    Success = false, 
                    Message = "Ошибка при отправке в Telegram" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Telegram");
            return new TelegramResponse 
            { 
                Success = false, 
                Message = "Произошла ошибка при отправке сообщения" 
            };
        }
    }

    /// <summary>
    /// Экранирование HTML специальных символов для Telegram
    /// </summary>
    private static string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
            
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
