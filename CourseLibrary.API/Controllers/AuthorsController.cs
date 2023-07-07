
using AutoMapper;
using CourseLibrary.API.ActionConstraints;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
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

        //var previousPageLink = authorsFromRepo.HasPrevious 
        //    ? CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.PreviousPage) : null;

        //var nextPageLink = authorsFromRepo.HasNext
        //    ? CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.NextPage) : null;

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            //previousPageLink,
            //nextPageLink
        };

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

        // create links
        var links = CreateLinksForAuthors(authorsResourceParameter, 
            authorsFromRepo.HasNext,
            authorsFromRepo.HasPrevious);

        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
                       .ShapeData(authorsResourceParameter.Fields);
        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object>;
            var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links
        };

        // return them
        return Ok(linkedCollectionResource);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(
        AuthorsResourceParameter authorsResourceParameter,
        bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>
        {
            // self
            new LinkDto(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.Current),
            "self",
            "GET")
        };

        if (hasNext)
        {
            links.Add(
                new(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.NextPage),
                "nextPage",
                "GET"));
        }
        if (hasPrevious)
        {
            links.Add(
                new(CreateAuthorsResourceUri(authorsResourceParameter, ResourceUriType.PreviousPage),
                "previousPage",
                "GET"));
        }

        return links;
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
            case ResourceUriType.Current:
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

    [Produces("application/json", 
        "application/vnd.marvin.hateoas+json", 
        "application/vnd.marvin.author.full+json", 
        "application/vnd.marvin.author.full.hateoas+json", 
        "application/vnd.marvin.author.friendly+json", 
        "application/vnd.marvin.author.friendly.hateoas+json")]
    [HttpGet("{authorId}", Name = "GetAuthor")]
    //public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId,
    public async Task<ActionResult> GetAuthor(Guid authorId,
        string? fields,
        [FromHeader(Name = "Accept")] string? mediaType)
    {
        // check if the inputted media type is a valid media type
        if (!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    statusCode: 400,
                    detail: $"Accept header media type value is not a valid mdeia type."));
        }

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

        var includeLinks = parsedMediaType.SubTypeWithoutSuffix
            .EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);
        IEnumerable<LinkDto> links = new List<LinkDto>();

        if (includeLinks)
        {
            links = CreateLinksForAuthor(authorId, fields);
        }

        var primaryMediaType = includeLinks
            ? parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)
            : parsedMediaType.SubTypeWithoutSuffix;

        // full author
        if (primaryMediaType == "vnd.marvin.author.full")
        {
            var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
                .ShapeData(fields) as IDictionary<string, object>;

            if (includeLinks)
            {
                fullResourceToReturn.Add("links", links);
            }

            return Ok(fullResourceToReturn);
        }

        // friendly author
        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields) as IDictionary<string, object>;

        if (includeLinks)
        {
            friendlyResourceToReturn.Add("links", links);
        }

        return Ok(friendlyResourceToReturn);

        //if (parsedMediaType.MediaType == "application/vnd.marvin.hateoas+json")
        //{
        //    // create links
        //    var links = CreateLinksForAuthor(authorId, fields);

        //    // add
        //    var linkedResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
        //        .ShapeData(fields) as IDictionary<string, object>;

        //    linkedResourceToReturn.Add("links", links);

        //    // return author
        //    return Ok(linkedResourceToReturn);
        //}

        //return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string? fields)
    {
        var links = new List<LinkDto>();
        
        if (string.IsNullOrWhiteSpace(fields))
        {
            links.Add(
                new LinkDto(Url.Link("GetAuthor", new { authorId }),
                "self",
                "GET"));
        }
        else
        {
            links.Add(
                new LinkDto(Url.Link("GetAuthor", new { authorId, fields }),
                    "self",
                    "GET"));
        }

        links.Add(
            new LinkDto(Url.Link("CreateCourseForAuthor", new { authorId }),
                "create_course_for_author",
                "POST"));

        links.Add(
            new LinkDto(Url.Link("GetCoursesForAuthor ", new { authorId }),
                "courses",
                "GET"));

        return links;
    }    

    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    [RequestHeaderMatchesMediaType("Content-Type",
        "application/vnd.marvin.authorforcreationwithdateofdeath+json")]        
    [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]              
    public async Task<ActionResult<AuthorDto>> CreateAuthorWithDateOfDeath(
        AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        // create links
        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        // add
        var linkedResourceToReturn = authorToReturn.ShapeData(null)
            as IDictionary<string, object?>;

        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);
    }

    [HttpPost(Name = "CreateAuthor")]
    [RequestHeaderMatchesMediaType("Content-Type",
        "application/json",
        "application/vnd.marvin.authorforcreation+json")]
    [Consumes("application/json",
        "application/vnd.marvin.authorforcreation+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        // create links
        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        // add
        var linkedResourceToReturn = authorToReturn.ShapeData(null)
            as IDictionary<string, object?>;

        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);
    }

    [HttpOptions]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET,HEAD,OPTIONS,POST");
        return Ok();
    }
}
