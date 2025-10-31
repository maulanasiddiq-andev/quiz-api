using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.Quiz;
using QuizApi.DTOs.Request;
using QuizApi.Models;
using QuizApi.Models.Quiz;
using QuizApi.Responses;
using QuizApi.Extensions;
using QuizApi.Exceptions;
using QuizApi.Helpers;

namespace QuizApi.Repositories
{
    public class CategoryRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        private readonly string userId = "";
        private readonly string tableName = "Category";
        private readonly ActionModelHelper actionModelHelper;
        public CategoryRepository(
            QuizAppDBContext dBContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            IQueryable<CategoryModel> listCategoryQuery = dBContext.Category
                .Where(x => x.RecordStatus.ToLower().Equals(RecordStatusConstant.Active.ToLower()))
                .AsQueryable();

            #region Ordering
            string orderBy = searchRequest.OrderBy;
            string orderDir = searchRequest.OrderDir;

            if (orderBy.Equals("createdTime"))
            {
                if (orderDir.Equals("asc"))
                {
                    listCategoryQuery = listCategoryQuery.OrderBy(x => x.CreatedTime).AsQueryable();
                }
                else if (orderDir.Equals("desc"))
                {
                    listCategoryQuery = listCategoryQuery.OrderByDescending(x => x.CreatedTime).AsQueryable();
                }
            }
            #endregion

            var response = new SearchResponse();
            response.TotalItems = await listCategoryQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listCategory = await listCategoryQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<CategoryDto>>(listCategory);

            return response;
        }

        public async Task<CategoryDto> GetDataByIdAsync(string id)
        {
            CategoryModel? category = await GetActiveCategoryByIdAsync(id);

            if (category is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            CategoryDto categoryDto = mapper.Map<CategoryDto>(category);

            return categoryDto;
        }

        public async Task CreateDataAsync(CategoryDto categoryDto)
        {
            CategoryModel category = mapper.Map<CategoryModel>(categoryDto);

            actionModelHelper.AssignCreateModel(category, tableName, userId);

            await dBContext.AddAsync(category);
            await dBContext.SaveChangesAsync();
        }

        public async Task<CategoryDto> UpdateDataAsync(string id, CategoryDto categoryDto)
        {
            CategoryModel? category = await GetActiveCategoryByIdAsync(id);

            if (category is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            category.Name = categoryDto.Name;
            category.Description = categoryDto.Description ?? "";

            actionModelHelper.AssignUpdateModel(category, userId);
            dBContext.Update(category);
            await dBContext.SaveChangesAsync();

            return mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteDataAsync(string id)
        {
            CategoryModel? category = await GetActiveCategoryByIdAsync(id);

            if (category is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            actionModelHelper.AssignDeleteModel(category, userId);

            dBContext.Update(category);
            await dBContext.SaveChangesAsync();
        }

        private async Task<CategoryModel?> GetActiveCategoryByIdAsync(string id)
        {
            return await dBContext.Category
                .Where(x => x.CategoryId.Equals(id) && x.RecordStatus.ToLower().Equals(RecordStatusConstant.Active.ToLower()))
                .FirstOrDefaultAsync();
        }
    }
}