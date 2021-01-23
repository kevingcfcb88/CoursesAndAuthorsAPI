using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;
        private readonly IMapper _mapper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyChecker _propertyChecker;

        public AuthorsController(ICourseLibraryRepository courseLibraryRepository,
            IMapper mapper, IPropertyMappingService propertyMappingService,
            IPropertyChecker propertyChecker)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
            _propertyChecker = propertyChecker ?? throw new ArgumentNullException(nameof(propertyChecker));
        }

        [HttpGet(Name = "GetAuthors")]
        [HttpHead]
        public ActionResult GetAuthors(
            [FromQuery] AuthorsResourceParameters authorsResourceParameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_propertyChecker.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);

            var previousPageLink = authorsFromRepo.HasPrevious ?
                CreateAuthorResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;

            var nextPageLink = authorsFromRepo.HasNext ?
                CreateAuthorResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;

            var paginationMetada = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink,
                nextPageLink
            };

            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetada));

            return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).ShapeData(authorsResourceParameters.Fields));
        }

        [HttpGet("{authorId}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid authorId, string fields)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields));
        }

        [HttpPost]
        public ActionResult<AuthorDto> CreateAuthor(AuthorForCreationDto author)
        {
            var authorEntity = _mapper.Map<Entities.Author>(author);
            _courseLibraryRepository.AddAuthor(authorEntity);
            _courseLibraryRepository.Save();

            var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
            return CreatedAtRoute("GetAuthor",
                new { authorId = authorToReturn.Id },
                authorToReturn);
        }

        [HttpOptions]
        public IActionResult GetAuthorsOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }

        [HttpDelete("{authorId}")]
        public ActionResult DeleteAuthor(Guid authorId)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            _courseLibraryRepository.DeleteAuthor(authorFromRepo);

            _courseLibraryRepository.Save();

            return NoContent();
        }

        private string CreateAuthorResourceUri(AuthorsResourceParameters authorResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorResourceParameters.Fields,
                        orderBy = authorResourceParameters.OrderBy,
                        pageNumber = authorResourceParameters.PageNumber - 1,
                        pageSize = authorResourceParameters.PageSize,
                        mainCategory = authorResourceParameters.MainCategory,
                        searchQuery = authorResourceParameters.SearchQuery
                    });
                case ResourceUriType.NextPage:
                    return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorResourceParameters.Fields,
                        orderBy = authorResourceParameters.OrderBy,
                        pageNumber = authorResourceParameters.PageNumber + 1,
                        pageSize = authorResourceParameters.PageSize,
                        mainCategory = authorResourceParameters.MainCategory,
                        searchQuery = authorResourceParameters.SearchQuery
                    });
                default:
                    return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorResourceParameters.Fields,
                        orderBy = authorResourceParameters.OrderBy,
                        pageNumber = authorResourceParameters.PageNumber,
                        pageSize = authorResourceParameters.PageSize,
                        mainCategory = authorResourceParameters.MainCategory,
                        searchQuery = authorResourceParameters.SearchQuery
                    });
            }
        }
    }
}
