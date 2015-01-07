﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB;
using MongoDB.Configuration;
using System.Linq.Expressions;

namespace Study.MongoDB.Core
{
   public class Repository<T> where T : class
    {
        Document doc = new Document();
        private string connectionString = "mongodb://localhost";
        private string databaseName = "myDatabase";
        private string collectionName = "hr_staff_info";
        private Mongo mongo;
        private MongoDatabase mongoDatabase;
        public MongoCollection<T> mongoCollection;

        public Repository()
        {
            Type t = typeof(T);
            string name= t.Name;
            mongo = GetMongo(); 
            mongoDatabase = mongo.GetDatabase(databaseName) as MongoDatabase;
            mongoCollection = mongoDatabase.GetCollection<T>(collectionName) as MongoCollection<T>;
        }

        private Mongo GetMongo()
        {
            var config = new MongoConfigurationBuilder();
            config.Mapping(mapping =>
            {
                mapping.DefaultProfile(profile =>
                {
                    profile.SubClassesAre(t => t.IsSubclassOf(typeof(T)));
                });
                mapping.Map<T>();
            });
            config.ConnectionString(connectionString);
            return new Mongo(config.BuildConfiguration());
        }

        public void Add(T value)
        {
            mongoCollection.Insert(value, true);
        }

        public void Delete(Expression<Func<T, bool>> func)
        {
            mongoCollection.Remove<T>(func);
        }

        public void Update(T t, Expression<Func<T, bool>> func)
        {
            mongoCollection.Update(t, func, true);
        }

        public T Single(Expression<Func<T, bool>> func)
        {
            return mongoCollection.Linq().FirstOrDefault(func);
        }

        public List<T> List(int pageIndex, int pageSize, Expression<Func<T, bool>> func, out int pageCount)
        {
            pageCount = 0;
            pageCount = Convert.ToInt32(mongoCollection.Count());
            var personList = mongoCollection.Linq().Where(func).Skip(pageSize * (pageIndex - 1))
            .Take(pageSize).Select(i => i).ToList();
            mongo.Disconnect();
            return personList;
        }

        public List<T> List(Expression<Func<T, bool>> func)
        {
            var list = mongoCollection.Linq().Where(func).ToList();
            return list;
        }

        public IEnumerable<T> FindAll()
        {
            return mongoCollection.FindAll().Documents;
        }

        public void Connect()
        {
            try
            {
                mongo.Connect();
            }
            catch (MongoConnectionException exception)
            {
                throw exception;
            }
        }

        public void CloseConnect()
        {
            mongo.Disconnect();
        }
    }
}