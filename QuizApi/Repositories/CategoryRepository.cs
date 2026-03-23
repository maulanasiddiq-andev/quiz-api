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
using Google.Cloud.Firestore;

namespace QuizApi.Repositories
{
    public class CategoryRepository
    {
        private readonly FirestoreDb firestoreDb;
        private readonly IMapper mapper;
        private readonly string userId = "";
        private readonly string tableName = "Category";
        private readonly ActionModelHelper actionModelHelper;
        public CategoryRepository(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
        )
        {
            this.firestoreDb = firestoreDb;
            this.mapper = mapper;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            // 1. Initialize the Base Query
            Query query = firestoreDb.Collection("category")
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            // 2. Ordering (Firestore requires an index for this)
            if (searchRequest.OrderBy.Equals("createdTime", StringComparison.OrdinalIgnoreCase))
            {
                if (searchRequest.OrderDir.Equals("asc", StringComparison.OrdinalIgnoreCase))
                    query = query.OrderBy("CreatedTime");
                else
                    query = query.OrderByDescending("CreatedTime");
            }

            // 3. Firestore Search Limitation
            // Firestore does not support 'ILike' or '%search%'. 
            // You can only do 'StartsWith' using the range trick below.
            if (!string.IsNullOrWhiteSpace(searchRequest.Search))
            {
                string term = searchRequest.Search;
                query = query.WhereGreaterThanOrEqualTo("Title", term)
                            .WhereLessThanOrEqualTo("Title", term + "\uf8ff");
            }

            // 6. Execution & Manual Projection
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            
            // Total Items (Full count in Firestore is billed per document read or using Count() aggregation)
            var response = new SearchResponse
            {
                TotalItems = snapshot.Count,
                CurrentPage = searchRequest.CurrentPage,
                PageSize = searchRequest.PageSize
            };

            // Manual Pagination
            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var rawItems = snapshot.Documents
                .Skip(skip)
                .Take(searchRequest.PageSize)
                .Select(doc => doc.ConvertTo<CategoryModel>())
                .ToList();

            response.Items = mapper.Map<List<CategoryDto>>(rawItems);
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

            // 1. Check if category is not default
            if (category.IsMain == false)
            {
                // Check if there is at least one default (IsMain == true) category
                // We use the Count aggregation for efficiency
                Query mainCategoryQuery = firestoreDb.Collection("category")
                    .WhereEqualTo("IsMain", true)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

                AggregateQuerySnapshot snapshot = await mainCategoryQuery.Count().GetSnapshotAsync();
                
                if (snapshot.Count == 0)
                {
                    throw new KnownException("Pilih satu kategori sebagai kategori default");
                }
            }

            // 2. Assign metadata (IDs and Timestamps)
            // Note: Ensure tableName matches your Firestore collection name exactly
            actionModelHelper.AssignCreateModel(category, tableName, userId);

            // 3. Save to Firestore
            await firestoreDb.Collection("category").AddAsync(category);
        }

        public async Task<CategoryDto> UpdateDataAsync(string id, CategoryDto categoryDto)
        {
            // 1. Get the Document Reference
            var categoryCollection = firestoreDb.Collection("category");
            Query query = categoryCollection
                .WhereEqualTo("CategoryId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot targetDoc = snapshot.Documents[0];
            CategoryModel category = targetDoc.ConvertTo<CategoryModel>();

            // 2. Update local model values
            category.Name = categoryDto.Name;
            category.Description = categoryDto.Description ?? "";
            category.IsMain = categoryDto.IsMain;
            actionModelHelper.AssignUpdateModel(category, userId);

            // 3. Initialize a Batch
            WriteBatch batch = firestoreDb.StartBatch();

            if (category.IsMain)
            {
                // Find other categories that are currently 'Main' to unset them
                Query otherMainsQuery = categoryCollection.WhereEqualTo("IsMain", true);
                QuerySnapshot otherMains = await otherMainsQuery.GetSnapshotAsync();

                foreach (var doc in otherMains.Documents)
                {
                    if (doc.Id != targetDoc.Id)
                    {
                        batch.Update(doc.Reference, "IsMain", false);
                    }
                }
            }
            else
            {
                // Check if at least one Main exists (excluding this one)
                Query anyMainQuery = categoryCollection.WhereEqualTo("IsMain", true);
                QuerySnapshot mainCheck = await anyMainQuery.GetSnapshotAsync();
                
                // If the only main was this one, and we are turning it off, throw error
                if (mainCheck.Documents.Count == 1 && mainCheck.Documents[0].Id == targetDoc.Id)
                {
                    throw new KnownException("Pilih satu kategori sebagai kategori default");
                }
            }

            // 4. Commit the changes
            batch.Set(targetDoc.Reference, category); // Overwrites the doc with the updated model
            await batch.CommitAsync();

            return mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteDataAsync(string id)
        {
            // Find the document
            var categoryCollection = firestoreDb.Collection("category");
            Query query = categoryCollection.WhereEqualTo("CategoryId", id).Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            DocumentSnapshot? document = snapshot.Documents.FirstOrDefault();

            if (document is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot targetDoc = snapshot.Documents[0];
            CategoryModel category = document.ConvertTo<CategoryModel>();
            
            WriteBatch batch = firestoreDb.StartBatch();
            if (category.IsMain == false)
            {
                Query anyMainQuery = categoryCollection.WhereEqualTo("IsMain", true);
                QuerySnapshot mainCheck = await anyMainQuery.GetSnapshotAsync();
                
                // If the only main was this one, and we are turning it off, throw error
                if (mainCheck.Documents.Count == 1 && mainCheck.Documents[0].Id == targetDoc.Id)
                {
                    throw new KnownException("Pilih satu kategori sebagai kategori default");
                }
            }

            actionModelHelper.AssignDeleteModel(category, userId);

            batch.Set(targetDoc.Reference, category); // Overwrites the doc with the updated model
            await batch.CommitAsync();
        }

        private async Task<CategoryModel?> GetActiveCategoryByIdAsync(string id)
        {
            Query query = firestoreDb.Collection("category")
                .WhereEqualTo("CategoryId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                return snapshot.Documents[0].ConvertTo<CategoryModel>();
            }

            return null;
        }
    }
}