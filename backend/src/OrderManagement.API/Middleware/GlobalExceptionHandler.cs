using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.API.Middleware;

// GlobalExceptionHandler (Tratador global de exceções):
// intercepta exceções lançadas pela aplicação e retorna respostas padronizadas no formato ProblemDetails (RFC 7807).
public class GlobalExceptionHandler : IExceptionHandler
{
    // _problemDetailsService (serviço de ProblemDetails): formata e escreve o JSON padrão de erro HTTP.
    private readonly IProblemDetailsService _problemDetailsService;

    // _logger (registrador de logs): grava erros inesperados para diagnóstico no servidor.
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // Construtor com injeção de dependências do serviço de ProblemDetails e do logger.
    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    // TryHandleAsync (Tentar tratar exceção): converte exceções de negócio no status HTTP correspondente.
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Mapeamento direto da exceção para (StatusCode, Título exibido no ProblemDetails).
        var (statusCode, title, detail) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Erro de validação",
                validationEx.Message),

            ResourceNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Recurso não encontrado",
                notFoundEx.Message),

            InsufficientStockException stockEx => (
                StatusCodes.Status409Conflict,
                "Estoque insuficiente",
                stockEx.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno no servidor",
                "Ocorreu um erro inesperado ao processar a requisição.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        // problemDetails (detalhes do problema): estrutura padrão RFC 7807 devolvida no corpo da resposta.
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
