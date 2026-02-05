using System.ComponentModel.DataAnnotations;

namespace EasyLink.Models;

/// <summary>
/// Настройки Telegram бота
/// </summary>
public class TelegramSettings
{
    /// <summary>
    /// Токен бота Telegram (из BotFather)
    /// </summary>
    public string BotToken { get; set; } = string.Empty;
    
    /// <summary>
    /// ID чата для отправки сообщений
    /// </summary>
    public string ChatId { get; set; } = string.Empty;
}

/// <summary>
/// Запрос обратной связи
/// </summary>
public class FeedbackRequest
{
    [Required(ErrorMessage = "Игровой ник обязателен")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ник должен быть от 2 до 50 символов")]
    public string PlayerNick { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите причину обращения")]
    [StringLength(100, ErrorMessage = "Причина не должна превышать 100 символов")]
    public string Reason { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите способ связи")]
    [StringLength(50, ErrorMessage = "Способ связи не должен превышать 50 символов")]
    public string ContactMethod { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите контактные данные")]
    [StringLength(100, ErrorMessage = "Контактные данные не должны превышать 100 символов")]
    public string ContactInfo { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Сообщение обязательно")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Сообщение должно быть от 10 до 2000 символов")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Заявка в команду
/// </summary>
public class TeamApplicationRequest
{
    [Required(ErrorMessage = "Игровой ник обязателен")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ник должен быть от 2 до 50 символов")]
    public string PlayerNick { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите сервер")]
    [StringLength(50, ErrorMessage = "Сервер не должен превышать 50 символов")]
    public string Server { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите желаемую роль")]
    [StringLength(50, ErrorMessage = "Роль не должна превышать 50 символов")]
    public string Role { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите количество часов")]
    [StringLength(50, ErrorMessage = "Часы не должны превышать 50 символов")]
    public string Hours { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "История наказаний не должна превышать 1000 символов")]
    public string History { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите Discord")]
    [StringLength(100, ErrorMessage = "Discord не должен превышать 100 символов")]
    public string Discord { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Укажите причину/мотивацию")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Причина должна быть от 20 до 2000 символов")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Результат отправки сообщения
/// </summary>
public class TelegramResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
