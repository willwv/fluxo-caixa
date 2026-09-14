using FluxoCaixa.Consolidado.Application.SaldoDiario;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixa.Consolidado.Api.Controllers;

[ApiController]
[Authorize]
[Route("consolidado")]
public class ConsolidadoController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConsolidadoController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{data}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterSaldoDoDia(DateOnly data, CancellationToken cancellationToken)
    {
        var saldo = await _mediator.Send(new ObterSaldoDiarioQuery(data), cancellationToken);
        return saldo is null ? NotFound() : Ok(saldo);
    }
}
