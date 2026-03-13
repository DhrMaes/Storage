namespace Storage.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Storage.Core;
using Storage.Plugin.Contracts;

[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
	private readonly StorageOrchestrator _storage;
	private readonly ILogger<StorageController> _logger;

	public StorageController(StorageOrchestrator storage, ILogger<StorageController> logger)
	{
		_storage = storage;
		_logger = logger;
	}

	[HttpGet("list")]
	public async Task<ActionResult<IReadOnlyList<StorageItem>>> List([FromQuery] string path = "/")
	{
		try
		{
			var items = await _storage.ListAsync(path);
			return Ok(items);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to list path: {Path}", path);
			return StatusCode(500, new { error = ex.Message });
		}
	}

	[HttpGet("info")]
	public async Task<ActionResult<StorageItem>> GetInfo([FromQuery] string path)
	{
		try
		{
			var info = await _storage.GetInfoAsync(path);
			if (info is null)
				return NotFound(new { error = $"Path not found: {path}" });

			return Ok(info);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get info for path: {Path}", path);
			return StatusCode(500, new { error = ex.Message });
		}
	}

	[HttpGet("download")]
	public async Task<IActionResult> Download([FromQuery] string path)
	{
		try
		{
			var stream = await _storage.ReadAsync(path);
			var fileName = Path.GetFileName(path);
			return File(stream, "application/octet-stream", fileName);
		}
		catch (FileNotFoundException)
		{
			return NotFound(new { error = $"File not found: {path}" });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to download file: {Path}", path);
			return StatusCode(500, new { error = ex.Message });
		}
	}

	[HttpPost("upload")]
	public async Task<IActionResult> Upload([FromQuery] string path, IFormFile file)
	{
		try
		{
			if (file is null || file.Length == 0)
				return BadRequest(new { error = "No file provided" });

			var targetPath = path.EndsWith('/') ? path + file.FileName : path;

			await using var stream = file.OpenReadStream();
			await _storage.WriteAsync(targetPath, stream, overwrite: true);

			return Ok(new { message = "File uploaded successfully", path = targetPath });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to upload file to: {Path}", path);
			return StatusCode(500, new { error = ex.Message });
		}
	}

	[HttpDelete("delete")]
	public async Task<IActionResult> Delete([FromQuery] string path)
	{
		try
		{
			await _storage.DeleteAsync(path);
			return Ok(new { message = "Deleted successfully", path });
		}
		catch (FileNotFoundException)
		{
			return NotFound(new { error = $"Path not found: {path}" });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to delete path: {Path}", path);
			return StatusCode(500, new { error = ex.Message });
		}
	}

	[HttpPost("move")]
	public async Task<IActionResult> Move([FromBody] MoveRequest request)
	{
		try
		{
			await _storage.MoveAsync(request.SourcePath, request.DestinationPath);
			return Ok(new { message = "Moved successfully", from = request.SourcePath, to = request.DestinationPath });
		}
		catch (FileNotFoundException)
		{
			return NotFound(new { error = $"Source path not found: {request.SourcePath}" });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to move from {Source} to {Dest}", request.SourcePath, request.DestinationPath);
			return StatusCode(500, new { error = ex.Message });
		}
	}

	[HttpGet("exists")]
	public async Task<ActionResult<bool>> Exists([FromQuery] string path)
	{
		try
		{
			var exists = await _storage.ExistsAsync(path);
			return Ok(new { path, exists });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to check existence of path: {Path}", path);
			return StatusCode(500, new { error = ex.Message });
		}
	}
}

public record MoveRequest(string SourcePath, string DestinationPath);
