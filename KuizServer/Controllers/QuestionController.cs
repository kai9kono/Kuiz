using Microsoft.AspNetCore.Mvc;
using KuizServer.Services;
using KuizServer.Models;

namespace KuizServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionController : ControllerBase
{
    private readonly QuestionService _questionService;

    public QuestionController(QuestionService questionService)
    {
        _questionService = questionService;
    }

    /// <summary>
    /// Get all questions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var questions = await _questionService.GetAllQuestionsAsync();
        return Ok(questions);
    }

    /// <summary>
    /// Get random questions for a game
    /// </summary>
    [HttpGet("random/{count}")]
    public async Task<IActionResult> GetRandom(int count)
    {
        if (count <= 0 || count > 100)
        {
            return BadRequest(new { error = "Count must be between 1 and 100" });
        }

        var questions = await _questionService.GetRandomQuestionsAsync(count);
        return Ok(questions);
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var question = await _questionService.GetQuestionByIdAsync(id);
        if (question == null)
        {
            return NotFound(new { error = "Question not found" });
        }
        return Ok(question);
    }

    /// <summary>
    /// Create a new question
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuestionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text) || string.IsNullOrWhiteSpace(dto.Answer))
        {
            return BadRequest(new { error = "Text and Answer are required" });
        }

        var question = await _questionService.CreateQuestionAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    /// <summary>
    /// Update an existing question
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateQuestionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text) || string.IsNullOrWhiteSpace(dto.Answer))
        {
            return BadRequest(new { error = "Text and Answer are required" });
        }

        var success = await _questionService.UpdateQuestionAsync(id, dto);
        if (!success)
        {
            return NotFound(new { error = "Question not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a question
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _questionService.DeleteQuestionAsync(id);
        if (!success)
        {
            return NotFound(new { error = "Question not found" });
        }

        return NoContent();
    }
}
