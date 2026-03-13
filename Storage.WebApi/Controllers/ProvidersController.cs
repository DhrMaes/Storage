namespace Storage.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Storage.Core;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
	private readonly StorageOrchestrator _storage;
	private readonly ILogger<ProvidersController> _logger;

	public ProvidersController(StorageOrchestrator storage, ILogger<ProvidersController> logger)
	{
		_storage = storage;
		_logger = logger;
	}

	[HttpGet]
	public ActionResult<IReadOnlyDictionary<string, StorageProviderConfiguration>> GetAll()
	{
		var providers = _storage.GetConfiguredProviders();
		return Ok(providers);
	}

	[HttpGet("default")]
	public ActionResult<string> GetDefault()
	{
		var defaultProvider = _storage.GetDefaultProviderName();
		if (defaultProvider is null)
			return NotFound(new { error = "No default provider configured" });

		return Ok(new { defaultProvider });
	}
}
