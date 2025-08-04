using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Query builder for converting LINQ expressions to SQL queries
    /// </summary>
    public class QueryBuilder
    {
        private readonly ILogger<QueryBuilder> _logger;
        private readonly Dictionary<string, object> _parameters;
        private int _parameterCounter;

        public QueryBuilder(ILogger<QueryBuilder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parameters = new Dictionary<string, object>();
            _parameterCounter = 0;
        }

        /// <summary>
        /// Builds a WHERE clause from a predicate expression
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="predicate">LINQ predicate expression</param>
        /// <returns>SQL WHERE clause and parameters</returns>
        public (string WhereClause, Dictionary<string, object> Parameters) BuildWhereClause<T>(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return (string.Empty, new Dictionary<string, object>());

            try
            {
                _parameters.Clear();
                _parameterCounter = 0;

                var whereClause = VisitExpression(predicate.Body);
                return (whereClause, new Dictionary<string, object>(_parameters));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building WHERE clause for predicate");
                return (string.Empty, new Dictionary<string, object>());
            }
        }

        /// <summary>
        /// Builds an ORDER BY clause from an ordering expression
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="orderBy">LINQ ordering expression</param>
        /// <param name="ascending">True for ascending order, false for descending</param>
        /// <returns>SQL ORDER BY clause</returns>
        public string BuildOrderByClause<T, TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            if (orderBy == null)
                return string.Empty;

            try
            {
                var columnName = GetMemberName(orderBy.Body);
                var direction = ascending ? "ASC" : "DESC";
                return $"ORDER BY {columnName} {direction}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building ORDER BY clause");
                return string.Empty;
            }
        }

        private string VisitExpression(Expression expression)
        {
            return expression.NodeType switch
            {
                ExpressionType.AndAlso => VisitBinaryExpression((BinaryExpression)expression, "AND"),
                ExpressionType.OrElse => VisitBinaryExpression((BinaryExpression)expression, "OR"),
                ExpressionType.Equal => VisitBinaryExpression((BinaryExpression)expression, "="),
                ExpressionType.NotEqual => VisitBinaryExpression((BinaryExpression)expression, "!="),
                ExpressionType.GreaterThan => VisitBinaryExpression((BinaryExpression)expression, ">"),
                ExpressionType.GreaterThanOrEqual => VisitBinaryExpression((BinaryExpression)expression, ">="),
                ExpressionType.LessThan => VisitBinaryExpression((BinaryExpression)expression, "<"),
                ExpressionType.LessThanOrEqual => VisitBinaryExpression((BinaryExpression)expression, "<="),
                ExpressionType.MemberAccess => VisitMemberExpression((MemberExpression)expression),
                ExpressionType.Constant => VisitConstantExpression((ConstantExpression)expression),
                ExpressionType.Call => VisitMethodCallExpression((MethodCallExpression)expression),
                _ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported")
            };
        }

        private string VisitBinaryExpression(BinaryExpression expression, string operatorSymbol)
        {
            var left = VisitExpression(expression.Left);
            var right = VisitExpression(expression.Right);

            return $"({left} {operatorSymbol} {right})";
        }

        private string VisitMemberExpression(MemberExpression expression)
        {
            if (expression.Expression?.NodeType == ExpressionType.Parameter)
            {
                return expression.Member.Name;
            }

            var value = GetExpressionValue(expression);
            var parameterName = $"@p{++_parameterCounter}";
            _parameters[parameterName] = value ?? DBNull.Value;
            return parameterName;
        }

        private string VisitConstantExpression(ConstantExpression expression)
        {
            var parameterName = $"@p{++_parameterCounter}";
            _parameters[parameterName] = expression.Value ?? DBNull.Value;
            return parameterName;
        }

        private string VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name == "Contains" && expression.Arguments.Count == 2)
            {
                var member = VisitExpression(expression.Object!);
                var value = VisitExpression(expression.Arguments[0]);
                return $"{member} LIKE '%' + {value} + '%'";
            }

            if (expression.Method.Name == "StartsWith" && expression.Arguments.Count == 1)
            {
                var member = VisitExpression(expression.Object!);
                var value = VisitExpression(expression.Arguments[0]);
                return $"{member} LIKE {value} + '%'";
            }

            if (expression.Method.Name == "EndsWith" && expression.Arguments.Count == 1)
            {
                var member = VisitExpression(expression.Object!);
                var value = VisitExpression(expression.Arguments[0]);
                return $"{member} LIKE '%' + {value}";
            }

            throw new NotSupportedException($"Method {expression.Method.Name} is not supported");
        }

        private string GetMemberName(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new NotSupportedException("Complex member expressions are not supported");
        }

        private object? GetExpressionValue(Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
            {
                return constantExpression.Value;
            }

            if (expression is MemberExpression memberExpression)
            {
                var value = GetExpressionValue(memberExpression.Expression!);
                if (value != null)
                {
                    var property = memberExpression.Member as PropertyInfo;
                    return property?.GetValue(value);
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a complete SELECT query with WHERE and ORDER BY clauses
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="tableName">Table name</param>
        /// <param name="predicate">WHERE predicate</param>
        /// <param name="orderBy">ORDER BY expression</param>
        /// <param name="ascending">Sort direction</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Complete SQL query and parameters</returns>
        public (string Query, Dictionary<string, object> Parameters) BuildQuery<T>(
            string tableName,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool ascending = true,
            int? page = null,
            int? pageSize = null)
        {
            var query = new StringBuilder($"SELECT * FROM {tableName}");
            var parameters = new Dictionary<string, object>();

            // Add WHERE clause
            if (predicate != null)
            {
                var (whereClause, whereParams) = BuildWhereClause(predicate);
                if (!string.IsNullOrEmpty(whereClause))
                {
                    query.Append($" WHERE {whereClause}");
                    foreach (var param in whereParams)
                    {
                        parameters[param.Key] = param.Value;
                    }
                }
            }

            // Add ORDER BY clause
            if (orderBy != null)
            {
                var orderByClause = BuildOrderByClause(orderBy, ascending);
                if (!string.IsNullOrEmpty(orderByClause))
                {
                    query.Append($" {orderByClause}");
                }
            }

            // Add pagination
            if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
            {
                var offset = (page.Value - 1) * pageSize.Value;
                query.Append($" OFFSET {offset} ROWS FETCH NEXT {pageSize.Value} ROWS ONLY");
            }

            return (query.ToString(), parameters);
        }
    }
} 