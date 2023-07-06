
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Dynamic;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly IPropertyCheckerService _propertyCheckerService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        IPropertyMappingService propertyMappingService,
        IPropertyCheckerService propertyCheckerService,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _courseLibraryRepository = courseLibraryRepository ?? throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
        _propertyCheckerService = propertyCheckerService ?? throw new ArgumentNullException(nameof(propertyCheckerService));
        _problemDetailsFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
    }

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    //public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors(
    public async Task<ActionResult> GetAuthors(
        [FromQuery] AuthorsResourceParameter authorsResourceParameter)
    {
        //throw new Exception("Test Exception");
        
        if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameter.OrderBy))
        {
            return BadRequest();
        }

        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameter.Fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    statusCode: 400,
                    detail: $"Not all requested data shaping fields exist on " +
                    $"the resource: {authorsResourceParameter.Fields}"));
        }

        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorsResourceParameter);

        var previousPageLink = authorsFromRepo.HasPrevious 
            ? CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.PreviousPage) : null;

        var nextPageLink = authorsFromRepo.HasNext
            ? CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.NextPage) : null;

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            previousPageLink,
            nextPageLink
        };

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
        // return them
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameter.Fields));
    }

    private string? CreateAuthorsResourceUri(AuthorsResourceParameter authorsResourceParameter, ResourceUriType type)
    {
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameter.Fields,
                        orderBy = authorsResourceParameter.OrderBy,
                        pageNumber = authorsResourceParameter.PageNumber - 1,
                        pageSize = authorsResourceParameter.PageSize,
                        mainCategory = authorsResourceParameter.MainCategory,
                        searchQuery = authorsResourceParameter.SearchQuery
                    });                
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameter.Fields,
                        orderBy = authorsResourceParameter.OrderBy,
                        pageNumber = authorsResourceParameter.PageNumber + 1,
                        pageSize = authorsResourceParameter.PageSize,
                        mainCategory = authorsResourceParameter.MainCategory,
                        searchQuery = authorsResourceParameter.SearchQuery
                    });
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameter.Fields,
                        orderBy = authorsResourceParameter.OrderBy,
                        pageNumber = authorsResourceParameter.PageNumber,
                        pageSize = authorsResourceParameter.PageSize,
                        mainCategory = authorsResourceParameter.MainCategory,
                        searchQuery = authorsResourceParameter.SearchQuery
                    });                
        }
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    //public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId,
    public async Task<ActionResult> GetAuthor(Guid authorId,
        string? fields)
    {
        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    statusCode: 400,
                    detail: $"Not all requested data shaping fields exist on " +
                    $"the resource: {fields}"));
        }

        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        // return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields));
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync(); 

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            authorToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET,HEAD,OPTIONS,POST");
        return Ok();
    }
}
