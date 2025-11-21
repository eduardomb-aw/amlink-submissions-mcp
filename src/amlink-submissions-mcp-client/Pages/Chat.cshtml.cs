using AmLink.Submissions.Mcp.Client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AmLink.Submissions.Mcp.Client.Pages;

public class ChatModel : PageModel
{
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(IOpenAiService openAiService, ILogger<ChatModel> logger)
    {
        _openAiService = openAiService;
        _logger = logger;
    }

    public List<ChatMessage> Messages { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    [Required]
    public string Message { get; set; } = string.Empty;

    public void OnGet()
    {
        // Load messages from session if available
        var sessionMessages = HttpContext.Session.GetString("ChatMessages");
        if (!string.IsNullOrEmpty(sessionMessages))
        {
            try
            {
                Messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(sessionMessages) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize chat messages from session");
                Messages = new();
            }
        }
    }

    public async Task<IActionResult> OnPostSendMessageAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, error = "Message is required" });
        }

        try
        {
            // Load existing messages
            OnGet();

            // Add user message
            var userMessage = new ChatMessage
            {
                Content = Message,
                IsUser = true,
                Timestamp = DateTime.Now
            };
            Messages.Add(userMessage);

            // Get AI response
            var response = await _openAiService.SendChatMessageAsync(Message);

            // Add AI response
            var aiMessage = new ChatMessage
            {
                Content = response,
                IsUser = false,
                Timestamp = DateTime.Now
            };
            Messages.Add(aiMessage);

            // Save messages to session
            var serializedMessages = System.Text.Json.JsonSerializer.Serialize(Messages);
            HttpContext.Session.SetString("ChatMessages", serializedMessages);

            return new JsonResult(new { success = true, response = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message: {Message}", Message);
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public IActionResult OnPostClearChat()
    {
        HttpContext.Session.Remove("ChatMessages");
        return RedirectToPage();
    }
}

public class ChatMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}