using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Sqordia.Application.Common.Interfaces;
using Sqordia.Contracts.Requests.Questionnaire;
using Sqordia.Contracts.Responses.Questionnaire;
using Sqordia.Contracts.Requests.Sections;
using Sqordia.Contracts.Responses.Sections;
using System.ClientModel;
using System.Text.Json;

namespace Sqordia.Infrastructure.Services;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only



public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
}

public class OpenAIService : IAIService
{
    private readonly ILogger<OpenAIService> _logger;
    private readonly OpenAISettings _settings;
    private readonly ChatClient? _chatClient;

    public OpenAIService(
        IOptions<OpenAISettings> settings,
        ILogger<OpenAIService> logger)
    {
        _logger = logger;
        _settings = settings.Value;

        _logger.LogInformation("OpenAI Settings - ApiKey configured: {HasKey}, Model: {Model}, ApiKey length: {Length}", 
            !string.IsNullOrEmpty(_settings.ApiKey), _settings.Model, 
            string.IsNullOrEmpty(_settings.ApiKey) ? 0 : _settings.ApiKey.Length);

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            try
            {
                _logger.LogInformation("Initializing OpenAI client with model: {Model}", _settings.Model);
                var openAIClient = new OpenAIClient(new ApiKeyCredential(_settings.ApiKey));
                _chatClient = openAIClient.GetChatClient(_settings.Model);

                _logger.LogInformation("OpenAI service initialized successfully with model: {Model}", _settings.Model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client: {ErrorMessage}", ex.Message);
            }
        }
        else
        {
            _logger.LogWarning("OpenAI API key not configured. AI features will be unavailable.");
        }
    }

    public async Task<string> GenerateContentAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 2000,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI service not configured");
            throw new InvalidOperationException("OpenAI service is not configured. Please provide a valid API key.");
        }

        try
        {
            _logger.LogInformation("Generating content with OpenAI (maxTokens: {MaxTokens}, temp: {Temperature})", maxTokens, temperature);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = maxTokens,
                Temperature = temperature
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            var content = response.Value.Content[0].Text;

            _logger.LogInformation("Content generated successfully. Length: {Length} characters", content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content with OpenAI");
            throw;
        }
    }

    public async Task<string> GenerateContentWithRetryAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 2000,
        float temperature = 0.7f,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                return await GenerateContentAsync(
                    systemPrompt,
                    userPrompt,
                    maxTokens,
                    temperature,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    _logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay} seconds...",
                        attempt, maxRetries, delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        _logger.LogError(lastException, "All {MaxRetries} attempts to generate content failed", maxRetries);
        throw new InvalidOperationException($"Failed to generate content after {maxRetries} attempts. See inner exception for details.", lastException);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI service unavailable: _chatClient is null. ApiKey configured: {HasKey}, ApiKey length: {Length}, Model: {Model}", 
                !string.IsNullOrEmpty(_settings.ApiKey), 
                string.IsNullOrEmpty(_settings.ApiKey) ? 0 : _settings.ApiKey.Length,
                _settings.Model);
            return false;
        }

        try
        {
            // Simple test to check if the service is accessible
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateUserMessage("Test")
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 10
            };

            await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI service availability check failed");
            return false;
        }
    }

    public async Task<QuestionSuggestionResponse> GenerateQuestionSuggestionsAsync(
        QuestionSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI service not configured");
            throw new InvalidOperationException("OpenAI service is not configured. Please provide a valid API key.");
        }

        try
        {
            _logger.LogInformation("Generating question suggestions for plan type: {PlanType}, question: {QuestionText}", 
                request.PlanType, request.QuestionText);

            var systemPrompt = GetQuestionSuggestionSystemPrompt(request.PlanType, request.Language);
            var userPrompt = BuildQuestionSuggestionUserPrompt(request);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1500,
                Temperature = 0.8f // Higher temperature for more creative suggestions
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = response.Value.Content[0].Text;

            _logger.LogInformation("Question suggestions generated successfully. Length: {Length} characters", content.Length);

            return ParseQuestionSuggestionResponse(request, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating question suggestions");
            throw;
        }
    }

    private string GetQuestionSuggestionSystemPrompt(string planType, string language = "fr")
    {
        var isFrench = language.ToLower() == "fr";
        
        var basePrompt = isFrench 
            ? @"Vous êtes un expert consultant en affaires aidant les utilisateurs à répondre aux questions de questionnaire pour la création de plans d'affaires.
Votre tâche est de générer des suggestions utiles, pertinentes et professionnelles pour les questions de questionnaire.

Directives:
- Fournir des suggestions pratiques et exploitables
- Considérer le contexte spécifique du type de plan d'affaires
- Inclure des options détaillées et concises
- S'assurer que les suggestions sont professionnelles et bien structurées
- Fournir un raisonnement pour chaque suggestion
- Retourner les suggestions au format JSON

Format de réponse:
{
  ""suggestions"": [
    {
      ""answer"": ""Texte de réponse détaillé"",
      ""confidence"": 0.85,
      ""reasoning"": ""Pourquoi cette suggestion est pertinente"",
      ""suggestionType"": ""Détaillé|Concis|Professionnel|Créatif""
    }
  ]
}"
            : @"You are an expert business consultant helping users answer questionnaire questions for business plan creation.
Your task is to generate helpful, relevant, and professional suggestions for questionnaire questions.

Guidelines:
- Provide practical, actionable suggestions
- Consider the specific business plan type context
- Include both detailed and concise options
- Ensure suggestions are professional and well-structured
- Provide reasoning for each suggestion
- Return suggestions in JSON format

Response format:
{
  ""suggestions"": [
    {
      ""answer"": ""Detailed answer text"",
      ""confidence"": 0.85,
      ""reasoning"": ""Why this suggestion is relevant"",
      ""suggestionType"": ""Detailed|Concise|Professional|Creative""
    }
  ]
}";

        var planTypeContext = isFrench
            ? planType.ToLower() switch
            {
                "businessplan" or "0" => "Il s'agit d'un plan d'affaires traditionnel (startup/PME) axé sur les revenus, l'analyse de marché et la rentabilité.",
                "strategicplan" or "1" => "Il s'agit d'un plan stratégique (organisme à but non lucratif/OBNL) axé sur la mission, l'impact, les subventions et les bénéficiaires.",
                "leancanvas" or "2" => "Il s'agit d'un lean canvas axé sur la validation rapide, le MVP et l'itération.",
                _ => "Il s'agit d'un plan d'affaires général."
            }
            : planType.ToLower() switch
            {
                "businessplan" or "0" => "This is for a traditional business plan (startup/SME) focusing on revenue, market analysis, and profitability.",
                "strategicplan" or "1" => "This is for a strategic plan (non-profit/OBNL) focusing on mission, impact, grants, and beneficiaries.",
                "leancanvas" or "2" => "This is for a lean canvas focusing on quick validation, MVP, and iteration.",
                _ => "This is for a general business plan."
            };

        var contextLabel = isFrench ? "Contexte" : "Context";
        return $"{basePrompt}\n\n{contextLabel}: {planTypeContext}";
    }

    private string BuildQuestionSuggestionUserPrompt(QuestionSuggestionRequest request)
    {
        var isFrench = request.Language.ToLower() == "fr";
        var questionLabel = isFrench ? "Question" : "Question";
        var existingResponseLabel = isFrench ? "Réponse existante" : "Existing response";
        var organizationContextLabel = isFrench ? "Contexte de l'organisation" : "Organization context";
        
        var prompt = $"{questionLabel}: {request.QuestionText}\n\n";
        
        if (!string.IsNullOrEmpty(request.ExistingResponse))
        {
            prompt += $"{existingResponseLabel}: {request.ExistingResponse}\n\n";
        }
        
        if (!string.IsNullOrEmpty(request.OrganizationContext))
        {
            prompt += $"{organizationContextLabel}: {request.OrganizationContext}\n\n";
        }
        
        var instruction = isFrench
            ? $"Veuillez générer {request.SuggestionCount} suggestions différentes pour répondre à cette question. " +
              "Fournissez des approches variées (détaillée, concise, professionnelle, créative) avec des scores de confiance et un raisonnement."
            : $"Please generate {request.SuggestionCount} different suggestions for answering this question. " +
              "Provide varied approaches (detailed, concise, professional, creative) with confidence scores and reasoning.";
        
        prompt += instruction;
        
        return prompt;
    }

    private QuestionSuggestionResponse ParseQuestionSuggestionResponse(
        QuestionSuggestionRequest request, 
        string aiResponse)
    {
        try
        {
            // Try to parse the JSON response
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsedResponse = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                
                if (parsedResponse.TryGetProperty("suggestions", out var suggestionsElement))
                {
                    var suggestions = new List<QuestionSuggestion>();
                    
                    foreach (var suggestionElement in suggestionsElement.EnumerateArray())
                    {
                        var suggestion = new QuestionSuggestion
                        {
                            Answer = suggestionElement.GetProperty("answer").GetString() ?? "",
                            Confidence = suggestionElement.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.8,
                            Reasoning = suggestionElement.GetProperty("reasoning").GetString() ?? "",
                            SuggestionType = suggestionElement.GetProperty("suggestionType").GetString() ?? "Professional"
                        };
                        suggestions.Add(suggestion);
                    }
                    
                    return new QuestionSuggestionResponse
                    {
                        QuestionText = request.QuestionText,
                        PlanType = request.PlanType,
                        Suggestions = suggestions,
                        GeneratedAt = DateTime.UtcNow,
                        Model = _settings.Model,
                        Language = request.Language
                    };
                }
            }
            
            // Fallback: create a single suggestion from the raw response
            return new QuestionSuggestionResponse
            {
                QuestionText = request.QuestionText,
                PlanType = request.PlanType,
                Suggestions = new List<QuestionSuggestion>
                {
                    new QuestionSuggestion
                    {
                        Answer = aiResponse.Trim(),
                        Confidence = 0.7,
                        Reasoning = "AI-generated response",
                        SuggestionType = "Professional"
                    }
                },
                GeneratedAt = DateTime.UtcNow,
                Model = _settings.Model,
                Language = request.Language
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON, using fallback");
            
            // Fallback response
            return new QuestionSuggestionResponse
            {
                QuestionText = request.QuestionText,
                PlanType = request.PlanType,
                Suggestions = new List<QuestionSuggestion>
                {
                    new QuestionSuggestion
                    {
                        Answer = aiResponse.Trim(),
                        Confidence = 0.7,
                        Reasoning = "AI-generated response",
                        SuggestionType = "Professional"
                    }
                },
                GeneratedAt = DateTime.UtcNow,
                Model = _settings.Model,
                Language = request.Language
            };
        }
    }

    public async Task<SectionImprovementResponse> ImproveSectionAsync(
        SectionImprovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI service not configured");
            throw new InvalidOperationException("OpenAI service is not configured. Please provide a valid API key.");
        }

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Improving section content for plan type: {PlanType}, language: {Language}", 
                request.PlanType, request.Language);

            var systemPrompt = GetSectionImprovementSystemPrompt(request.PlanType, request.Language);
            var userPrompt = BuildSectionImprovementUserPrompt(request);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxLength ?? 2000,
                Temperature = 0.7f
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = response.Value.Content[0].Text;
            var endTime = DateTime.UtcNow;

            _logger.LogInformation("Section improvement completed. Length: {Length} characters", content.Length);

            return new SectionImprovementResponse
            {
                OriginalContent = request.CurrentContent,
                ImprovedContent = content,
                ImprovementType = request.ImprovementType,
                Language = request.Language,
                PlanType = request.PlanType,
                Model = _settings.Model,
                GeneratedAt = endTime,
                Confidence = 0.85,
                ImprovementExplanation = "Content improved for better clarity and professionalism",
                FurtherSuggestions = new List<string>
                {
                    "Consider adding specific examples",
                    "Include relevant statistics or data",
                    "Add visual elements if appropriate"
                },
                WordCount = content.Split(' ').Length,
                ReadingLevel = "Professional",
                ProcessingTime = endTime - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving section content");
            throw;
        }
    }

    public async Task<SectionExpansionResponse> ExpandSectionAsync(
        SectionImprovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI service not configured");
            throw new InvalidOperationException("OpenAI service is not configured. Please provide a valid API key.");
        }

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Expanding section content for plan type: {PlanType}, language: {Language}", 
                request.PlanType, request.Language);

            var systemPrompt = GetSectionExpansionSystemPrompt(request.PlanType, request.Language);
            var userPrompt = BuildSectionExpansionUserPrompt(request);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxLength ?? 3000,
                Temperature = 0.8f
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = response.Value.Content[0].Text;
            var endTime = DateTime.UtcNow;

            _logger.LogInformation("Section expansion completed. Length: {Length} characters", content.Length);

            return new SectionExpansionResponse
            {
                OriginalContent = request.CurrentContent,
                ImprovedContent = content,
                ImprovementType = "expand",
                Language = request.Language,
                PlanType = request.PlanType,
                Model = _settings.Model,
                GeneratedAt = endTime,
                Confidence = 0.8,
                ImprovementExplanation = "Content expanded with additional details and subsections",
                FurtherSuggestions = new List<string>
                {
                    "Add more specific examples",
                    "Include market research data",
                    "Consider adding competitive analysis"
                },
                WordCount = content.Split(' ').Length,
                ReadingLevel = "Professional",
                ProcessingTime = endTime - startTime,
                AddedSubsections = new List<string>
                {
                    "Market Analysis",
                    "Competitive Landscape",
                    "Implementation Timeline"
                },
                ExpandedPoints = new List<string>
                {
                    "Target market definition",
                    "Revenue projections",
                    "Risk assessment"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding section content");
            throw;
        }
    }

    public async Task<SectionSimplificationResponse> SimplifySectionAsync(
        SectionImprovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("OpenAI service not configured");
            throw new InvalidOperationException("OpenAI service is not configured. Please provide a valid API key.");
        }

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Simplifying section content for plan type: {PlanType}, language: {Language}", 
                request.PlanType, request.Language);

            var systemPrompt = GetSectionSimplificationSystemPrompt(request.PlanType, request.Language);
            var userPrompt = BuildSectionSimplificationUserPrompt(request);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxLength ?? 1500,
                Temperature = 0.6f
            };

            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = response.Value.Content[0].Text;
            var endTime = DateTime.UtcNow;

            _logger.LogInformation("Section simplification completed. Length: {Length} characters", content.Length);

            return new SectionSimplificationResponse
            {
                OriginalContent = request.CurrentContent,
                ImprovedContent = content,
                ImprovementType = "simplify",
                Language = request.Language,
                PlanType = request.PlanType,
                Model = _settings.Model,
                GeneratedAt = endTime,
                Confidence = 0.9,
                ImprovementExplanation = "Content simplified for better readability and understanding",
                FurtherSuggestions = new List<string>
                {
                    "Use bullet points for key information",
                    "Add simple diagrams or charts",
                    "Include a summary at the beginning"
                },
                WordCount = content.Split(' ').Length,
                ReadingLevel = "General",
                ProcessingTime = endTime - startTime,
                SimplifiedTerms = new List<string>
                {
                    "Complex technical terms",
                    "Industry jargon",
                    "Financial terminology"
                },
                RemovedJargon = new List<string>
                {
                    "Technical acronyms",
                    "Industry-specific terms",
                    "Complex sentence structures"
                },
                OriginalComplexity = 0.8,
                NewComplexity = 0.3
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simplifying section content");
            throw;
        }
    }

    private string GetSectionImprovementSystemPrompt(string planType, string language)
    {
        var isFrench = language.ToLower() == "fr";
        
        var basePrompt = isFrench
            ? @"Vous êtes un expert consultant en affaires spécialisé dans l'amélioration de contenu de plans d'affaires.
Votre tâche est d'améliorer le contenu fourni pour le rendre plus professionnel, clair et persuasif.

Directives:
- Améliorer la clarté et la structure du contenu
- Rendre le contenu plus professionnel
- Améliorer la persuasivité et l'impact
- Maintenir la cohérence avec le type de plan d'affaires
- Utiliser un langage approprié pour le public cible
- Retourner uniquement le contenu amélioré"
            : @"You are an expert business consultant specializing in improving business plan content.
Your task is to improve the provided content to make it more professional, clear, and persuasive.

Guidelines:
- Improve clarity and structure of the content
- Make the content more professional
- Enhance persuasiveness and impact
- Maintain consistency with the business plan type
- Use appropriate language for the target audience
- Return only the improved content";

        var planTypeContext = isFrench
            ? planType.ToLower() switch
            {
                "businessplan" or "0" => "Il s'agit d'un plan d'affaires traditionnel (startup/PME) axé sur les revenus, l'analyse de marché et la rentabilité.",
                "strategicplan" or "1" => "Il s'agit d'un plan stratégique (organisme à but non lucratif/OBNL) axé sur la mission, l'impact, les subventions et les bénéficiaires.",
                "leancanvas" or "2" => "Il s'agit d'un lean canvas axé sur la validation rapide, le MVP et l'itération.",
                _ => "Il s'agit d'un plan d'affaires général."
            }
            : planType.ToLower() switch
            {
                "businessplan" or "0" => "This is for a traditional business plan (startup/SME) focusing on revenue, market analysis, and profitability.",
                "strategicplan" or "1" => "This is for a strategic plan (non-profit/OBNL) focusing on mission, impact, grants, and beneficiaries.",
                "leancanvas" or "2" => "This is for a lean canvas focusing on quick validation, MVP, and iteration.",
                _ => "This is for a general business plan."
            };

        var contextLabel = isFrench ? "Contexte" : "Context";
        return $"{basePrompt}\n\n{contextLabel}: {planTypeContext}";
    }

    private string GetSectionExpansionSystemPrompt(string planType, string language)
    {
        var isFrench = language.ToLower() == "fr";
        
        var basePrompt = isFrench
            ? @"Vous êtes un expert consultant en affaires spécialisé dans l'expansion de contenu de plans d'affaires.
Votre tâche est d'étendre le contenu fourni en ajoutant des détails, des exemples et des sous-sections pertinentes.

Directives:
- Étendre le contenu avec des détails supplémentaires
- Ajouter des exemples concrets et des cas d'usage
- Créer des sous-sections logiques
- Enrichir avec des données et des analyses
- Maintenir la cohérence avec le type de plan d'affaires
- Retourner uniquement le contenu étendu"
            : @"You are an expert business consultant specializing in expanding business plan content.
Your task is to expand the provided content by adding details, examples, and relevant subsections.

Guidelines:
- Expand content with additional details
- Add concrete examples and use cases
- Create logical subsections
- Enrich with data and analysis
- Maintain consistency with the business plan type
- Return only the expanded content";

        var planTypeContext = isFrench
            ? planType.ToLower() switch
            {
                "businessplan" or "0" => "Il s'agit d'un plan d'affaires traditionnel (startup/PME) axé sur les revenus, l'analyse de marché et la rentabilité.",
                "strategicplan" or "1" => "Il s'agit d'un plan stratégique (organisme à but non lucratif/OBNL) axé sur la mission, l'impact, les subventions et les bénéficiaires.",
                "leancanvas" or "2" => "Il s'agit d'un lean canvas axé sur la validation rapide, le MVP et l'itération.",
                _ => "Il s'agit d'un plan d'affaires général."
            }
            : planType.ToLower() switch
            {
                "businessplan" or "0" => "This is for a traditional business plan (startup/SME) focusing on revenue, market analysis, and profitability.",
                "strategicplan" or "1" => "This is for a strategic plan (non-profit/OBNL) focusing on mission, impact, grants, and beneficiaries.",
                "leancanvas" or "2" => "This is for a lean canvas focusing on quick validation, MVP, and iteration.",
                _ => "This is for a general business plan."
            };

        var contextLabel = isFrench ? "Contexte" : "Context";
        return $"{basePrompt}\n\n{contextLabel}: {planTypeContext}";
    }

    private string GetSectionSimplificationSystemPrompt(string planType, string language)
    {
        var isFrench = language.ToLower() == "fr";
        
        var basePrompt = isFrench
            ? @"Vous êtes un expert consultant en affaires spécialisé dans la simplification de contenu de plans d'affaires.
Votre tâche est de simplifier le contenu fourni pour le rendre plus accessible et facile à comprendre.

Directives:
- Simplifier le langage et la structure
- Remplacer le jargon technique par des termes simples
- Utiliser des phrases courtes et claires
- Organiser l'information de manière logique
- Maintenir l'essence du message original
- Retourner uniquement le contenu simplifié"
            : @"You are an expert business consultant specializing in simplifying business plan content.
Your task is to simplify the provided content to make it more accessible and easy to understand.

Guidelines:
- Simplify language and structure
- Replace technical jargon with simple terms
- Use short and clear sentences
- Organize information logically
- Maintain the essence of the original message
- Return only the simplified content";

        var planTypeContext = isFrench
            ? planType.ToLower() switch
            {
                "businessplan" or "0" => "Il s'agit d'un plan d'affaires traditionnel (startup/PME) axé sur les revenus, l'analyse de marché et la rentabilité.",
                "strategicplan" or "1" => "Il s'agit d'un plan stratégique (organisme à but non lucratif/OBNL) axé sur la mission, l'impact, les subventions et les bénéficiaires.",
                "leancanvas" or "2" => "Il s'agit d'un lean canvas axé sur la validation rapide, le MVP et l'itération.",
                _ => "Il s'agit d'un plan d'affaires général."
            }
            : planType.ToLower() switch
            {
                "businessplan" or "0" => "This is for a traditional business plan (startup/SME) focusing on revenue, market analysis, and profitability.",
                "strategicplan" or "1" => "This is for a strategic plan (non-profit/OBNL) focusing on mission, impact, grants, and beneficiaries.",
                "leancanvas" or "2" => "This is for a lean canvas focusing on quick validation, MVP, and iteration.",
                _ => "This is for a general business plan."
            };

        var contextLabel = isFrench ? "Contexte" : "Context";
        return $"{basePrompt}\n\n{contextLabel}: {planTypeContext}";
    }

    private string BuildSectionImprovementUserPrompt(SectionImprovementRequest request)
    {
        var isFrench = request.Language.ToLower() == "fr";
        var contentLabel = isFrench ? "Contenu à améliorer" : "Content to improve";
        var instructionsLabel = isFrench ? "Instructions spécifiques" : "Specific instructions";
        var audienceLabel = isFrench ? "Public cible" : "Target audience";
        var industryLabel = isFrench ? "Contexte industriel" : "Industry context";
        var toneLabel = isFrench ? "Ton souhaité" : "Desired tone";

        var prompt = $"{contentLabel}:\n{request.CurrentContent}\n\n";

        if (!string.IsNullOrEmpty(request.Instructions))
        {
            prompt += $"{instructionsLabel}: {request.Instructions}\n\n";
        }

        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            prompt += $"{audienceLabel}: {request.TargetAudience}\n\n";
        }

        if (!string.IsNullOrEmpty(request.IndustryContext))
        {
            prompt += $"{industryLabel}: {request.IndustryContext}\n\n";
        }

        if (!string.IsNullOrEmpty(request.Tone))
        {
            prompt += $"{toneLabel}: {request.Tone}\n\n";
        }

        var instruction = isFrench
            ? "Veuillez améliorer ce contenu en le rendant plus professionnel, clair et persuasif."
            : "Please improve this content to make it more professional, clear, and persuasive.";

        prompt += instruction;

        return prompt;
    }

    private string BuildSectionExpansionUserPrompt(SectionImprovementRequest request)
    {
        var isFrench = request.Language.ToLower() == "fr";
        var contentLabel = isFrench ? "Contenu à étendre" : "Content to expand";
        var instructionsLabel = isFrench ? "Instructions spécifiques" : "Specific instructions";
        var audienceLabel = isFrench ? "Public cible" : "Target audience";
        var industryLabel = isFrench ? "Contexte industriel" : "Industry context";

        var prompt = $"{contentLabel}:\n{request.CurrentContent}\n\n";

        if (!string.IsNullOrEmpty(request.Instructions))
        {
            prompt += $"{instructionsLabel}: {request.Instructions}\n\n";
        }

        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            prompt += $"{audienceLabel}: {request.TargetAudience}\n\n";
        }

        if (!string.IsNullOrEmpty(request.IndustryContext))
        {
            prompt += $"{industryLabel}: {request.IndustryContext}\n\n";
        }

        var instruction = isFrench
            ? "Veuillez étendre ce contenu en ajoutant des détails, des exemples et des sous-sections pertinentes."
            : "Please expand this content by adding details, examples, and relevant subsections.";

        prompt += instruction;

        return prompt;
    }

    private string BuildSectionSimplificationUserPrompt(SectionImprovementRequest request)
    {
        var isFrench = request.Language.ToLower() == "fr";
        var contentLabel = isFrench ? "Contenu à simplifier" : "Content to simplify";
        var instructionsLabel = isFrench ? "Instructions spécifiques" : "Specific instructions";
        var audienceLabel = isFrench ? "Public cible" : "Target audience";

        var prompt = $"{contentLabel}:\n{request.CurrentContent}\n\n";

        if (!string.IsNullOrEmpty(request.Instructions))
        {
            prompt += $"{instructionsLabel}: {request.Instructions}\n\n";
        }

        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            prompt += $"{audienceLabel}: {request.TargetAudience}\n\n";
        }

        var instruction = isFrench
            ? "Veuillez simplifier ce contenu pour le rendre plus accessible et facile à comprendre."
            : "Please simplify this content to make it more accessible and easy to understand.";

        prompt += instruction;

        return prompt;
    }
}

