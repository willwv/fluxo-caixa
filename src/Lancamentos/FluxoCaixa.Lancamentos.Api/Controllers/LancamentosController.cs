using FluxoCaixa.Lancamentos.Application.Lancamentos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixa.Lancamentos.Api.Controllers;

[ApiController]
[Authorize]
[Route("lancamentos")]
public class LancamentosController : ControllerBase
{
    private readonly IMediator _mediator;

    public LancamentosController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarLancamentoCommand command, CancellationToken cancellationToken)
    {
        var lancamento = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = lancamento.Id }, lancamento);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateOnly? data,
        [FromQuery] int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _mediator.Send(new ObterLancamentosQuery(data, pagina), cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var lancamento = await _mediator.Send(new ObterLancamentoPorIdQuery(id), cancellationToken);
        return lancamento is null ? NotFound() : Ok(lancamento);
    }
}
