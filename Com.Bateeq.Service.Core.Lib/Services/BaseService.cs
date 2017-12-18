﻿using Com.Moonlay.NetCore.Lib.Service;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Dynamic.Core;
using Com.Moonlay.Models;
using Newtonsoft.Json;
using System.Reflection;
using Com.Moonlay.NetCore.Lib;

namespace Com.Bateeq.Service.Core.Lib.Services
{
    public abstract class BaseService<TDbContext, TModel> : StandardEntityService<TDbContext, TModel>
        where TDbContext : DbContext
        where TModel : StandardEntity, IValidatableObject
    {
        public BaseService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        //Create Data
        public virtual async Task<int> CreateModel(TModel Model)
        {
            return await this.CreateAsync(Model);
        }

        //Get All Data with Pagination, Sort, Keyword & Order
        public virtual Tuple<List<TModel>, int, Dictionary<string, string>, List<string>> ReadModel(int page, int size, string order, List<string> select, string keyword)
        {
            IQueryable<TModel> query = DbContext.Set<TModel>();
            Dictionary<string, string> orders = JsonConvert.DeserializeObject<Dictionary<string, string>>(order);
            string dynamicQuery = "";

            if (!string.IsNullOrEmpty(keyword))
            {
                var keywords = keyword.Split(" ");

                foreach (var word in keywords)
                {
                    if (string.IsNullOrEmpty(dynamicQuery))
                    {
                        dynamicQuery = string.Format("Code = \"{0}\"", word);
                        dynamicQuery += string.Format(" OR Name = \"{0}\"", word);
                    } 
                    else
                    {
                        dynamicQuery += string.Format(" OR Code = \"{0}\"", word);
                        dynamicQuery += string.Format(" OR Name = \"{0}\"", word);
                    }
                }
                
                query = query.Where(dynamicQuery);
            }

            // Order
            if (orders.Count.Equals(0))
            {
                orders.Add("_updatedDate", "desc");

                query = query.OrderByDescending(b => b._LastModifiedUtc); /* Default Order */
            }
            else
            {
                string Key = orders.Keys.First();
                string OrderType = orders[Key];
                string TransformKey = Key;

                BindingFlags IgnoreCase = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

                query = OrderType.Equals("asc") ?
                    query.OrderBy(b => b.GetType().GetProperty(TransformKey, IgnoreCase).GetValue(b)) :
                    query.OrderByDescending(b => b.GetType().GetProperty(TransformKey, IgnoreCase).GetValue(b));
            }

            // Pagination
            Pageable<TModel> pageable = new Pageable<TModel>(query, page - 1, size);
            List<TModel> pageableData = pageable.Data.ToList<TModel>();
            int totalData = pageable.TotalCount;

            return Tuple.Create(pageableData, totalData, orders, select);
        }

        //Get Data ById
        public virtual async Task<TModel> ReadModelById(int Id)
        {
            return await GetAsync(Id);
        }

        //Get Data By_id for older data
        public virtual async Task<TModel> ReadModelById(string _id)
        {
            var model = Task.Run(() => GetModelById(_id));

            return await model;
        }

        //Get Model By Id
        private TModel GetModelById(string _id)
        {
            IQueryable<TModel> query = DbContext.Set<TModel>().Where("it._id.Contains(@0)", _id);

            return query.FirstOrDefault();
        }

        //Update Data
        public virtual async Task<int> UpdateModel(int Id, TModel Model)
        {
            return await this.UpdateAsync(Id, Model);
        }

        //Delete Data
        public virtual async Task<int> DeleteModel(int Id)
        {
            return await this.DeleteAsync(Id);
        }
    }
}
