using EasyLink.Models;
using EasyLink.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Concurrent;

namespace EasyLink.Controllers;

/// <summary>
/// Контроллер для обработки запросов, связанных с Telegram
/// </summary>
[ApiController]
[Route("[controller]")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramService _telegramService;
    private readonly ILogger<TelegramController> _logger;
    
    // Simple in-memory rate limiting (для production лучше использовать Redis)
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
    private const int MaxRequestsPerMinute = 3;
    private const int MaxRequestsPerHour = 10;

    public TelegramController(
        ITelegramService telegramService, 
        ILogger<TelegramController> logger)
    {
        _telegramService = telegramService;
        _logger = logger;
    }

    /// <summary>
    /// Отправить обратную связь
    /// </summary>
    [HttpPost("feedback")]
    public async Task<IActionResult> SendFeedback([FromBody] FeedbackRequest request)
    {
        // Проверка rate limit
        var clientIp = GetClientIp();
        var rateLimitResult = CheckRateLimit(clientIp, "feedback");
        if (!rateLimitResult.Allowed)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IP}", clientIp);
            return StatusCode(429, new TelegramResponse 
            { 
                Success = false, 
                Message = rateLimitResult.Message 
            });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
                
            return BadRequest(new TelegramResponse 
            { 
                Success = false, 
                Message = string.Join("; ", errors) 
            });
        }

        _logger.LogInformation("Received feedback from {PlayerNick}", request.PlayerNick);
        
        var result = await _telegramService.SendFeedbackAsync(request);
        
        if (result.Success)
            return Ok(result);
        else
            return StatusCode(500, result);
    }

    /// <summary>
    /// Отправить заявку в команду
    /// </summary>
    [HttpPost("team-application")]
    public async Task<IActionResult> SendTeamApplication([FromBody] TeamApplicationRequest request)
    {
        // Проверка rate limit
        var clientIp = GetClientIp();
        var rateLimitResult = CheckRateLimit(clientIp, "team");
        if (!rateLimitResult.Allowed)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IP}", clientIp);
            return StatusCode(429, new TelegramResponse 
            { 
                Success = false, 
                Message = rateLimitResult.Message 
            });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
                
            return BadRequest(new TelegramResponse 
            { 
                Success = false, 
                Message = string.Join("; ", errors) 
            });
        }

        _logger.LogInformation("Received team application from {PlayerNick} for role {Role}", 
            request.PlayerNick, request.Role);
        
        var result = await _telegramService.SendTeamApplicationAsync(request);
        
        if (result.Success)
            return Ok(result);
        else
            return StatusCode(500, result);
    }

    /// <summary>
    /// Health check для Telegram сервиса
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private string GetClientIp()
    {
        // Проверяем заголовки от прокси/CloudFlare
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private (bool Allowed, string Message) CheckRateLimit(string clientIp, string action)
    {
        var key = $"{clientIp}:{action}";
        var now = DateTime.UtcNow;
        
        var info = _rateLimits.GetOrAdd(key, _ => new RateLimitInfo());
        
        lock (info)
        {
            // Очистка старых записей
            info.MinuteRequests.RemoveAll(t => (now - t).TotalMinutes > 1);
            info.HourRequests.RemoveAll(t => (now - t).TotalHours > 1);
            
            // Проверка лимитов
            if (info.MinuteRequests.Count >= MaxRequestsPerMinute)
            {
                return (false, "Слишком много запросов. Подождите минуту.");
            }
            
            if (info.HourRequests.Count >= MaxRequestsPerHour)
            {
                return (false, "Превышен лимит запросов в час. Попробуйте позже.");
            }
            
            // Добавляем текущий запрос
            info.MinuteRequests.Add(now);
            info.HourRequests.Add(now);
            
            return (true, string.Empty);
        }
    }

    private class RateLimitInfo
    {
        public List<DateTime> MinuteRequests { get; } = new();
        public List<DateTime> HourRequests { get; } = new();
    }
}
