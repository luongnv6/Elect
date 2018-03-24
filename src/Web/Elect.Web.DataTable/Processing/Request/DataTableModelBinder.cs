﻿#region	License
//--------------------------------------------------
// <License>
//     <Copyright> 2018 © Top Nguyen </Copyright>
//     <Url> http://topnguyen.net/ </Url>
//     <Author> Top </Author>
//     <Project> Elect </Project>
//     <File>
//         <Name> DataTableModelBinder.cs </Name>
//         <Created> 24/03/2018 3:01:07 PM </Created>
//         <Key> c83cac8b-7833-4e2d-9c84-01f180f1b54a </Key>
//     </File>
//     <Summary>
//         DataTableModelBinder.cs is a part of Elect
//     </Summary>
// <License>
//--------------------------------------------------
#endregion License

using Elect.Core.DictionaryUtils;
using Elect.Core.ObjUtils;
using Elect.Web.DataTable.Models.Constants;
using Elect.Web.DataTable.Models.Request;
using Elect.Web.HttpUtils;
using Elect.Web.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elect.Web.DataTable.Processing.Request
{
    public class DataTableModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProvider = bindingContext.ValueProvider;

            // Depend on "iColumns" property is have or not, we will known this is legacy model or
            // latest style model. Binding the value to model by the legacy or new style mapping.

            int columns = GetValue<int>(valueProvider, PropertyConstants.Columns);

            DataTableParamModel dataTableParam = columns <= 0 ? BindModel(valueProvider) : BindLegacyModel(valueProvider, columns);

            // Keep all data to Data Property
            dataTableParam.Data = GetDataDictionary(bindingContext);

            // Bind data to result
            bindingContext.Result = ModelBindingResult.Success(dataTableParam);

            return Task.CompletedTask;
        }

        private static DataTableParamModel BindModel(IValueProvider valueProvider)
        {
            DataTableParamModel obj = new DataTableParamModel
            {
                DisplayStart = GetValue<int>(valueProvider, "start"),
                DisplayLength = GetValue<int>(valueProvider, "length"),
                Search = GetValue<string>(valueProvider, "search[value]"),
                EscapeRegex = GetValue<bool>(valueProvider, "search[regex]"),
                Echo = GetValue<int>(valueProvider, "draw")
            };

            int colIdx = 0;

            while (true)
            {
                string colPrefix = $"columns[{colIdx}]";
                string colName = GetValue<string>(valueProvider, $"{colPrefix}[data]");
                if (string.IsNullOrWhiteSpace(colName))
                {
                    break;
                }
                obj.ColumnNames.Add(colName);
                obj.ListIsSortable.Add(GetValue<bool>(valueProvider, $"{colPrefix}[orderable]"));
                obj.ListIsSearchable.Add(GetValue<bool>(valueProvider, $"{colPrefix}[searchable]"));
                obj.SearchValues.Add(GetValue<string>(valueProvider, $"{colPrefix}[search][value]"));
                obj.ListIsEscapeRegexColumn.Add(GetValue<bool>(valueProvider, $"{colPrefix}[searchable][regex]"));
                colIdx++;
            }
            obj.Columns = colIdx;
            colIdx = 0;

            while (true)
            {
                string colPrefix = $"order[{colIdx}]";

                int? orderColumn = GetValue<int?>(valueProvider, $"{colPrefix}[column]");

                if (orderColumn.HasValue)
                {
                    obj.SortCol.Add(orderColumn.Value);
                    obj.SortDir.Add(GetValue<string>(valueProvider, $"{colPrefix}[dir]"));
                    colIdx++;
                }
                else
                {
                    break;
                }
            }
            obj.SortingCols = colIdx;
            return obj;
        }

        private static DataTableParamModel BindLegacyModel(IValueProvider valueProvider, int columns)
        {
            DataTableParamModel obj = new DataTableParamModel(columns)
            {
                DisplayStart = GetValue<int>(valueProvider, PropertyConstants.DisplayStart),
                DisplayLength = GetValue<int>(valueProvider, PropertyConstants.DisplayLength),
                Search = GetValue<string>(valueProvider, PropertyConstants.Search),
                EscapeRegex = GetValue<bool>(valueProvider, PropertyConstants.EscapeRegex),
                SortingCols = GetValue<int>(valueProvider, PropertyConstants.SortingCols),
                Echo = GetValue<int>(valueProvider, PropertyConstants.Echo)
            };

            for (int i = 0; i < obj.Columns; i++)
            {
                obj.ListIsSortable.Add(GetValue<bool>(valueProvider, $"{PropertyConstants.Sortable}_{i}"));
                obj.ListIsSearchable.Add(GetValue<bool>(valueProvider, $"{PropertyConstants.Searchable}_{i}"));

                // Important Legacy DataTable bind sSearch for sSearchValues
                obj.SearchValues.Add(GetValue<string>(valueProvider, $"{PropertyConstants.Search}_{i}"));

                obj.ListIsEscapeRegexColumn.Add(GetValue<bool>(valueProvider, $"{PropertyConstants.EscapeRegex}_{i}"));
                obj.SortCol.Add(GetValue<int>(valueProvider, $"{PropertyConstants.SortCol}_{i}"));
                obj.SortDir.Add(GetValue<string>(valueProvider, $"{PropertyConstants.SortDir}_{i}"));
            }
            return obj;
        }

        private static Dictionary<string, object> GetDataDictionary(ModelBindingContext bindingContext)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            try
            {
                // Submit Form Request
                if (bindingContext.HttpContext.Request.ContentType?.Contains(ContentType.FormUrlEncoded) == true)
                {
                    var form = bindingContext.HttpContext.Request.Form;

                    var valueProvider = bindingContext.ValueProvider;

                    foreach (var key in form.Keys)
                    {
                        data.Add(key, valueProvider.GetValue(key));
                    }
                }
                else
                {
                    object submitData = bindingContext.HttpContext.Request.GetBody();
                    data = submitData?.ToDictionary() ?? new Dictionary<string, object>();
                }
            }
            catch
            {
                data = new Dictionary<string, object>();
            }
            return data;
        }

        private static T GetValue<T>(IValueProvider valueProvider, string key)
        {
            ValueProviderResult valueResult = valueProvider.GetValue(key);

            var result = valueResult.FirstValue.ConvertTo<T>();

            return result;
        }
    }

    public class DataTablesModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.Metadata.ModelType == typeof(DataTableParamModel) ? new DataTableModelBinder() : null;
        }
    }
}