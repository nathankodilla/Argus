using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EntityFramework
{
	internal static class EntityFrameworkHelpers
	{
		private static Dictionary<IEntityType, IKey> PrimaryKeysCache = new Dictionary<IEntityType, IKey>();

		internal static bool IsProviderRelational(this DbContext context)
		{
			return ((IInfrastructure<IServiceProvider>)context.Database).Instance.GetService<IRelationalConnection>() != null;
		}

		internal static string GetConnectionID(this DbConnection connection)
		{
			SqlConnection sqlConnection = connection as SqlConnection;
			if (sqlConnection == null)
				return null;

			Guid connectionID = sqlConnection.ClientConnectionId;
			return connectionID != Guid.Empty ? connectionID.ToString() : null;
		}

		internal static string GetCurrentTransactionID(this DbContext context)
		{
			IDbContextTransaction currentTransaction = (context.GetInfrastructure().GetService<IDbContextTransactionManager>() as IRelationalConnection).CurrentTransaction;
			if (currentTransaction == null)
				return null;

			DbTransaction dbTransaction = currentTransaction.GetDbTransaction();

			PropertyInfo internalTransactionProperty = dbTransaction.GetType().GetTypeInfo().GetProperty("InternalTransaction", BindingFlags.NonPublic | BindingFlags.Instance);
			object internalTransaction = internalTransactionProperty.GetValue(dbTransaction);
			PropertyInfo transactionIDProperty = internalTransaction.GetType().GetTypeInfo().GetProperty("TransactionId", BindingFlags.NonPublic | BindingFlags.Instance);
			return ((long)transactionIDProperty.GetValue(internalTransaction)).ToString();
		}

		internal static Dictionary<string, object> GetPrimaryKeyValue(this IEntityType entityType, object entity)
		{
			Dictionary<string, object> primaryKey = new Dictionary<string, object>();

			IKey definition = null;
			if (!PrimaryKeysCache.TryGetValue(entityType, out definition))
			{
				definition = entityType.FindPrimaryKey();
				PrimaryKeysCache[entityType] = definition;
			}

			if (definition != null)
			{
				foreach (IProperty property in definition.Properties)
				{
					object value = entity.GetType().GetTypeInfo().GetProperty(property.Name).GetValue(entity);
					primaryKey.Add(property.Name, value);
				}
			}
			return primaryKey;
		}

		internal static string GetColumnName(this IProperty prop)
		{
			return prop.Name;
		}

		internal static PropertyInfo GetDbSetForType(this DbContext context, Type type)
		{
			return context.GetType().GetProperties().Where(o => o.PropertyType.GetTypeInfo().IsGenericType &&
																o.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
																o.PropertyType.GenericTypeArguments.Contains(type))
													.FirstOrDefault(); // TODO: this makes the assumption only one dbset per class
		}

		internal static DatabaseAction GetActionFromEntityState(EntityState state)
		{
			switch (state)
			{
				case EntityState.Added:
					return DatabaseAction.Insert;
				case EntityState.Modified:
					return DatabaseAction.Update;
				case EntityState.Deleted:
					return DatabaseAction.Delete;
				default:
					throw new ArgumentOutOfRangeException(nameof(state));
			}
		}
	}
}
