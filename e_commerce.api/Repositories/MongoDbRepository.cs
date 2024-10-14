using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using e_commerce.api.Models;

namespace e_commerce.api.Data
{
    public interface IMongoDBRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<T> FindOneAsync(Expression<Func<T, bool>> filterExpression);
        Task<IEnumerable<T>> FilterByAsync(Expression<Func<T, bool>> filterExpression);
        Task InsertOneAsync(T document);
        Task UpdateOneAsync(string id, T document);
        Task DeleteOneAsync(string id);
        Task UpdateOneAsync(Expression<Func<T, bool>> filterExpression, T document);
        Task<T> FindByIdAsync(string id);
        Task ReplaceOneAsync(T document);
    }

    public class MongoDBRepository<T> : IMongoDBRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;
        private readonly ILogger<MongoDBRepository<T>> _logger;

        public MongoDBRepository(IConfiguration configuration, ILogger<MongoDBRepository<T>> logger, string collectionName)
        {
            _logger = logger;
            try
            {
                var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
                var database = client.GetDatabase(configuration["Database:Name"]);
                _collection = database.GetCollection<T>(collectionName);
                _logger.LogInformation($"MongoDB repository initialized for collection: {collectionName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initializing MongoDB repository for collection: {collectionName}");
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<T> FindOneAsync(Expression<Func<T, bool>> filterExpression)
        {
            return await _collection.Find(filterExpression).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FilterByAsync(Expression<Func<T, bool>> filterExpression)
        {
            return await _collection.Find(filterExpression).ToListAsync();
        }

        public async Task InsertOneAsync(T document)
    {
        try
        {
            await _collection.InsertOneAsync(document);
            _logger.LogInformation($"Document inserted: {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error inserting document: {typeof(T).Name}");
            throw;
        }
    }

        public async Task UpdateOneAsync(string id, T document)
        {
            await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("Id", id), document);
        }

        public async Task DeleteOneAsync(string id)
        {
            await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("Id", id));
        }

        public async Task UpdateOneAsync(Expression<Func<T, bool>> filterExpression, T document)
        {
            await _collection.ReplaceOneAsync(filterExpression, document);
        }

        public async Task<T> FindByIdAsync(string id)
        {
            return await GetByIdAsync(id);
        }

        public async Task ReplaceOneAsync(T document)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var id = idProperty.GetValue(document) as string;
                await UpdateOneAsync(id, document);
            }
        }
    }
}